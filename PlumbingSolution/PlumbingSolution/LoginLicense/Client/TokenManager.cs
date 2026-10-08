using System;
using System.Threading;
using System.Threading.Tasks;
using PlumbingSolution.LoginLicense.Models.User;

namespace PlumbingSolution.LoginLicense.Client
{
    /// <summary>Kết quả thô của một lần gọi server. Việc diễn giải thuộc về lớp gọi.</summary>
    public enum RefreshOutcome
    {
        Succeeded,

        /// <summary>Lỗi hạ tầng — giữ token, thử lại.</summary>
        FailedTransient,

        /// <summary>Server từ chối vì lý do nghiệp vụ.</summary>
        FailedLogic,
    }

    /// <summary>
    /// Kết quả một lần làm mới. Phải kèm cả mã lỗi: nơi gọi cần phân biệt lỗi nào đáng
    /// thu hồi license với lỗi nào chỉ nên báo rồi thôi.
    /// </summary>
    public struct RefreshReport
    {
        public RefreshReport(RefreshOutcome outcome, ActivationError? error)
        {
            Outcome = outcome;
            Error = error;
        }

        public RefreshOutcome Outcome { get; private set; }

        public ActivationError? Error { get; private set; }
    }

    /// <summary>
    /// Giữ token trong bộ nhớ và thực hiện việc làm mới.
    ///
    /// CỐ Ý không có máy trạng thái ở đây. Bản thiết kế đầu để cả lớp này lẫn lớp điều
    /// phối cùng định nghĩa Active/Degraded/Revoked; hai bản sao của một bộ luật chắc
    /// chắn sẽ trôi lệch nhau. Lớp này chỉ báo cáo sự kiện thô.
    ///
    /// Token CHỈ tồn tại trong RAM — không bao giờ chạm đĩa.
    /// </summary>
    public sealed class TokenManager : IDisposable
    {
        private readonly ILicenseApiClient _api;
        private readonly Func<DateTimeOffset> _nowUtc;
        private readonly object _gate = new object();

        private string _token;
        private DateTimeOffset _expiresAt;
        private string _activationId;
        private string _machineId;
        private string _productCode;
        private LicenseData _license;
        private ActivationMethod _method;

        public TokenManager(ILicenseApiClient api, Func<DateTimeOffset> nowUtc = null)
        {
            _api = api;
            _nowUtc = nowUtc ?? DefaultNowUtc;
        }

        /// <summary>Token còn hiệu lực, hoặc null. Hết hạn theo đồng hồ máy cũng trả null.</summary>
        public string CurrentToken
        {
            get
            {
                lock (_gate)
                {
                    if (_token == null || _expiresAt <= _nowUtc())
                    {
                        return null;
                    }

                    return _token;
                }
            }
        }

        public LicenseData License
        {
            get { lock (_gate) { return _license; } }
        }

        public ActivationMethod Method
        {
            get { lock (_gate) { return _method; } }
        }

        public bool HasActivation
        {
            get { lock (_gate) { return _activationId != null; } }
        }

        /// <summary>
        /// Bộ ba đủ để gọi làm mới. Trả null nếu chưa kích hoạt. Dùng để ghi nhớ ra đĩa —
        /// CỐ Ý không trả token: token sống mười lăm phút và không bao giờ được ghi xuống đĩa.
        /// </summary>
        public ActivationRecord GetActivation()
        {
            lock (_gate)
            {
                if (_activationId == null || _machineId == null || _productCode == null)
                {
                    return null;
                }

                return new ActivationRecord
                {
                    ActivationId = _activationId,
                    MachineId = _machineId,
                    ProductCode = _productCode,
                    ActivatedBy = _method.ToString(),
                };
            }
        }

        /// <summary>
        /// Nạp lại bộ ba đã lưu để <see cref="RefreshAsync"/> chạy được mà không cần kích
        /// hoạt lại. KHÔNG đặt token — token luôn phải lấy mới từ server.
        /// </summary>
        public void RestoreActivation(ActivationRecord record)
        {
            lock (_gate)
            {
                _activationId = record.ActivationId;
                _machineId = record.MachineId;
                _productCode = record.ProductCode;

                ActivationMethod method;
                _method = TryParseMethod(record.ActivatedBy, out method) ? method : ActivationMethod.Key;
            }
        }

        public async Task<ActivationError?> ActivateAsync(ActivateRequest request, CancellationToken ct)
        {
            ActivationOutcome outcome = await _api.ActivateAsync(request, ct).ConfigureAwait(false);
            if (!outcome.IsSuccess)
            {
                return outcome.Error ?? ActivationError.UnknownLogicError;
            }

            ActivationResult result = outcome.Value;
            lock (_gate)
            {
                _token = result.Token;
                _expiresAt = result.ExpiresAt;
                _activationId = result.ActivationId;
                _license = result.License;
                _method = result.ActivatedBy;
                _machineId = request.MachineId;
                _productCode = request.ProductCode;
            }

            return null;
        }

        /// <summary>Làm mới bằng activationId. Không bao giờ gửi lại key hay mật khẩu.</summary>
        public async Task<RefreshReport> RefreshAsync(CancellationToken ct)
        {
            RefreshRequest request;
            lock (_gate)
            {
                if (_activationId == null || _machineId == null || _productCode == null)
                {
                    return new RefreshReport(RefreshOutcome.FailedLogic, ActivationError.InvalidActivation);
                }

                request = new RefreshRequest
                {
                    ActivationId = _activationId,
                    MachineId = _machineId,
                    ProductCode = _productCode,
                };
            }

            ActivationOutcome outcome = await _api.RefreshAsync(request, ct).ConfigureAwait(false);
            if (outcome.IsSuccess)
            {
                ActivationResult result = outcome.Value;
                lock (_gate)
                {
                    _token = result.Token;
                    _expiresAt = result.ExpiresAt;
                    if (result.License != null)
                    {
                        _license = result.License;
                    }
                }

                return new RefreshReport(RefreshOutcome.Succeeded, null);
            }

            ActivationError error = outcome.Error ?? ActivationError.UnknownLogicError;

            // Token hiện tại được GIỮ NGUYÊN khi gặp lỗi hạ tầng: mất mạng vài giây không
            // phải lý do để khoá tính năng của người đang dựng mô hình.
            return error.IsTransient()
                ? new RefreshReport(RefreshOutcome.FailedTransient, error)
                : new RefreshReport(RefreshOutcome.FailedLogic, error);
        }

        /// <summary>
        /// Xoá token khỏi bộ nhớ. Lớp gọi phải làm việc này TRƯỚC khi báo mất license —
        /// nếu không, một nhịp chen vào ngay sau đó vẫn lấy được token cũ còn chữ ký hợp
        /// lệ và mở khoá tính năng như thường.
        /// </summary>
        public void Clear()
        {
            lock (_gate)
            {
                _token = null;
                _expiresAt = default(DateTimeOffset);
                _activationId = null;
                _license = null;
            }
        }

        private static bool TryParseMethod(string value, out ActivationMethod method)
        {
            method = ActivationMethod.Key;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (string.Equals(value, "Account", StringComparison.OrdinalIgnoreCase))
            {
                method = ActivationMethod.Account;
                return true;
            }

            return string.Equals(value, "Key", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTimeOffset DefaultNowUtc()
        {
            return DateTimeOffset.UtcNow;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
