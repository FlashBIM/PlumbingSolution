using System;
using System.Threading;
using System.Threading.Tasks;
using PlumbingSolution.LoginLicense.Client;

namespace PlumbingSolution.LoginLicense.Runtime
{
    /// <summary>
    /// Nguồn token cho <see cref="LicenseSession"/>: gói <see cref="TokenManager"/>,
    /// <see cref="IActivationStore"/> và nhịp làm mới 5 phút thành một hàm duy nhất.
    ///
    /// BẢNG LUẬT — đây là bộ luật của cả hệ, đọc kỹ trước khi sửa:
    ///
    ///   chưa kích hoạt                      → None("NOT_ACTIVATED"), KHÔNG gọi server
    ///   token còn hạn &amp; chưa tới mốc 5'     → trả token hiện có, KHÔNG gọi server
    ///   làm mới thành công                  → token mới, DỜI mốc
    ///   làm mới thất bại                    → KHÔNG dời mốc (nhịp sau tự thử lại)
    ///   thất bại nghiệp vụ + thuộc nhóm thu hồi → xoá token + None(mã) → khoá NGAY
    ///   thất bại nghiệp vụ + CHƯA TỪNG có token → xoá bản ghi + None("NOT_ACTIVATED")
    ///   còn lại                             → NÉM → lỗi hạ tầng → GIỮ token tới hết hạn
    ///
    /// TUYỆT ĐỐI không trả None khi token trong tay còn hạn: None đưa vòng lặp vào nhánh
    /// mất-license và xoá trạng thái, phá vỡ cam kết giữ-token-tới-hết-hạn.
    ///
    /// KHÔNG có bộ đếm giờ riêng — nhịp chính là nhịp của vòng lặp. Token CHỈ ở RAM.
    /// </summary>
    internal sealed class DirectTokenSource
    {
        private readonly TokenManager _tokens;
        private readonly IActivationStore _store;
        private readonly Func<string> _machineIdProvider;
        private readonly string _productCode;
        private readonly TimeSpan _refreshInterval;
        private readonly Func<DateTimeOffset> _nowUtc;
        private readonly object _gate = new object();

        /// <summary>Mốc lần làm mới THÀNH CÔNG gần nhất. Null = chưa từng.</summary>
        private DateTimeOffset? _lastSuccessfulRefreshUtc;

        public DirectTokenSource(
            TokenManager tokens,
            IActivationStore store,
            Func<string> machineIdProvider,
            string productCode,
            TimeSpan refreshInterval,
            Func<DateTimeOffset> nowUtc = null)
        {
            if (tokens == null) throw new ArgumentNullException("tokens");
            if (store == null) throw new ArgumentNullException("store");
            if (machineIdProvider == null) throw new ArgumentNullException("machineIdProvider");

            _tokens = tokens;
            _store = store;
            _machineIdProvider = machineIdProvider;
            _productCode = productCode;
            _refreshInterval = refreshInterval;
            _nowUtc = nowUtc ?? DefaultNowUtc;
        }

        public TokenManager Tokens
        {
            get { return _tokens; }
        }

        public IActivationStore Store
        {
            get { return _store; }
        }

        /// <summary>
        /// Nạp lại bản ghi kích hoạt từ đĩa. Mã máy LỆCH thì XOÁ bản ghi — phần cứng đã
        /// đổi nên ràng buộc cũ không còn giá trị, giữ lại chỉ khiến mọi lần làm mới sau
        /// đều bị từ chối.
        /// </summary>
        public void ResumeFromStore()
        {
            ActivationRecord record = _store.Load();
            if (record == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_productCode)
                && !string.Equals(record.ProductCode, _productCode, StringComparison.OrdinalIgnoreCase))
            {
                // Bản ghi của sản phẩm KHÁC: không nạp và tuyệt đối KHÔNG xoá — đừng phá
                // kích hoạt của add-in khác trên cùng máy.
                return;
            }

            string currentMachineId;
            try
            {
                currentMachineId = _machineIdProvider();
            }
            catch (Exception)
            {
                // hwid.dll lỗi nhất thời: đừng xoá bản ghi oan. Nhịp sau sẽ thử lại.
                return;
            }

            if (!string.Equals(record.MachineId, currentMachineId, StringComparison.OrdinalIgnoreCase))
            {
                _store.Delete();
                return;
            }

            _tokens.RestoreActivation(record);
        }

        /// <summary>Một nhịp của vòng lặp. Xem bảng luật ở đầu lớp.</summary>
        public async Task<LicenseResult> PulseAsync(CancellationToken ct)
        {
            if (!_tokens.HasActivation)
            {
                // Bản ghi có thể vừa được tạo bởi một phiên Revit khác, hoặc vừa bị xoá.
                ResumeFromStore();
                if (!_tokens.HasActivation)
                {
                    return LicenseResult.None("NOT_ACTIVATED");
                }
            }

            string current = _tokens.CurrentToken;
            if (current != null)
            {
                lock (_gate)
                {
                    if (_lastSuccessfulRefreshUtc != null
                        && _nowUtc() - _lastSuccessfulRefreshUtc.Value < _refreshInterval)
                    {
                        return LicenseResult.WithToken(current);
                    }
                }
            }

            RefreshReport report = await _tokens.RefreshAsync(ct).ConfigureAwait(false);

            if (report.Outcome == RefreshOutcome.Succeeded)
            {
                string fresh = _tokens.CurrentToken;
                if (fresh == null)
                {
                    // Server trả token đã hết hạn ngay theo đồng hồ máy (lệch giờ nặng):
                    // coi như lỗi hạ tầng, giữ nguyên những gì đang có.
                    throw new LicenseRefreshTransientException(ActivationError.MalformedResponse);
                }

                lock (_gate)
                {
                    _lastSuccessfulRefreshUtc = _nowUtc();
                }

                return LicenseResult.WithToken(fresh);
            }

            ActivationError error = report.Error ?? ActivationError.UnknownLogicError;

            if (report.Outcome == RefreshOutcome.FailedLogic && error.RevokesLicense())
            {
                // Xoá token TRƯỚC khi báo — nếu không, một nhịp chen vào ngay sau đó vẫn
                // lấy được token cũ còn chữ ký hợp lệ.
                _tokens.Clear();
                return LicenseResult.None(ServerCodeFromError(error));
            }

            bool everHadToken;
            lock (_gate)
            {
                everHadToken = _lastSuccessfulRefreshUtc != null;
            }

            if (report.Outcome == RefreshOutcome.FailedLogic && current == null && !everHadToken)
            {
                // Đường KHÔI PHỤC (chưa từng cầm token trong phiên này): server nói bản
                // ghi này vô giá trị → xoá đi và xin key lại.
                //
                // Điều kiện everHadToken là BẮT BUỘC: "current == null" KHÔNG đồng nghĩa
                // với khôi phục — token cũng trả null khi vừa hết hạn giữa phiên. Thiếu
                // nó thì một mã 403 kèm trang HTML (proxy công ty chặn) rơi đúng vào đây
                // và XOÁ bản ghi kích hoạt của người đang dựng mô hình.
                _tokens.Clear();
                _store.Delete();
                return LicenseResult.None("NOT_ACTIVATED");
            }

            // Lỗi hạ tầng, hoặc lỗi nghiệp vụ mơ hồ khi đang cầm token → NÉM để vòng lặp
            // coi là lỗi hạ tầng và giữ token tới hết hạn.
            throw new LicenseRefreshTransientException(error);
        }

        /// <summary>
        /// Gọi sau khi kích hoạt: token vừa cấp còn mới tinh — dời mốc để nhịp ngay sau
        /// đó dùng luôn, không gọi server lần hai.
        /// </summary>
        public void MarkFreshToken()
        {
            lock (_gate)
            {
                _lastSuccessfulRefreshUtc = _nowUtc();
            }
        }

        /// <summary>Gọi khi đăng xuất: đặt lại mốc để lần kích hoạt sau làm mới ngay.</summary>
        public void ResetCadence()
        {
            lock (_gate)
            {
                _lastSuccessfulRefreshUtc = null;
            }
        }

        private static string ServerCodeFromError(ActivationError error)
        {
            switch (error)
            {
                case ActivationError.Expired:
                    return "EXPIRED";
                case ActivationError.Locked:
                    return "LOCKED";
                case ActivationError.MachineLimit:
                    return "MACHINE_LIMIT";
                default:
                    return "REVOKED";
            }
        }

        private static DateTimeOffset DefaultNowUtc()
        {
            return DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Ném từ <see cref="DirectTokenSource.PulseAsync"/> cho những lỗi KHÔNG được phép
    /// khoá tool. Vòng lặp bắt mọi ngoại lệ thường và coi là lỗi hạ tầng → giữ token.
    /// </summary>
    internal sealed class LicenseRefreshTransientException : Exception
    {
        public LicenseRefreshTransientException(ActivationError error)
            : base("làm mới thất bại (giữ token): " + error)
        {
            Error = error;
        }

        public ActivationError Error { get; private set; }
    }
}
