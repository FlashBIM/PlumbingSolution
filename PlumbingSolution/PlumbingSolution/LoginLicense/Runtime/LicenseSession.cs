using System;
using System.Threading;
using System.Threading.Tasks;
using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.LoginLicense.Verify;

namespace PlumbingSolution.LoginLicense.Runtime
{
    /// <summary>Kết quả một nhịp.</summary>
    internal enum PulseOutcome
    {
        /// <summary>Nhận được token hợp lệ, trạng thái đã cập nhật.</summary>
        Active,

        /// <summary>Server nói rõ không có license — đã xoá trạng thái.</summary>
        Lost,

        /// <summary>Lỗi hạ tầng — GIỮ token cũ, để nó tự hết hạn.</summary>
        TransportError,
    }

    /// <summary>Nguồn cấp token cho vòng nhịp. Trả token, hoặc lý do không có.</summary>
    internal sealed class LicenseResult
    {
        private LicenseResult(string token, string noLicenseReason)
        {
            Token = token;
            NoLicenseReason = noLicenseReason;
        }

        public string Token { get; private set; }

        public string NoLicenseReason { get; private set; }

        public bool HasToken
        {
            get { return !string.IsNullOrEmpty(Token); }
        }

        public static LicenseResult WithToken(string token)
        {
            return new LicenseResult(token, null);
        }

        public static LicenseResult None(string reason)
        {
            return new LicenseResult(null, reason ?? "NO_LICENSE");
        }
    }

    /// <summary>
    /// Vòng đời license phía add-in: gọi nguồn token theo nhịp, kiểm chữ ký, cập nhật
    /// trạng thái, và phát sự kiện cho lớp trên bật/tắt ribbon.
    ///
    /// KHÔNG gọi API Revit ở đây — chạy trên luồng nền, chỉ phát sự kiện. Lớp trên chịu
    /// trách nhiệm đưa việc về luồng giao diện.
    ///
    /// Bản này gộp <c>LicenseSession</c> và <c>HeartbeatLoop</c> của hệ Dirit và bỏ phần
    /// dành riêng cho chế độ chạy qua launcher (mở kênh, nối lại, trạng thái ACTIVATING).
    /// Ở đây không có kênh nào để đứt — nguồn token chạy ngay trong tiến trình.
    ///
    /// NHỮNG ĐIỀU KIỆN DƯỚI ĐÂY LÀ KẾT QUẢ CỦA CÁC LẦN SỬA LỖI THẬT, ĐỪNG RÚT GỌN:
    ///   • Lọc <c>when (ct.IsCancellationRequested)</c> khi bắt huỷ: quá thời gian chờ
    ///     của một yêu cầu HTTP cũng ném cùng loại ngoại lệ nhưng KHÔNG kèm thẻ huỷ này.
    ///     Thiếu bộ lọc thì một nhịp quá hạn sẽ giết vòng lặp vĩnh viễn — token hết hạn
    ///     sau tối đa 15 phút và mọi thao tác bị khoá câm lặng trong khi ribbon vẫn sáng.
    ///   • Khử trùng lặp sự kiện: chỉ phát khi CHUYỂN trạng thái. Phát mỗi nhịp là dội
    ///     ExternalEvent vào luồng giao diện Revit vài chục lần một phút.
    ///   • Lỗi hạ tầng KHÔNG xoá trạng thái. Mất mạng không phải lý do khoá tool của
    ///     người đang dựng mô hình.
    /// </summary>
    internal sealed class LicenseSession : IDisposable
    {
        private readonly Func<CancellationToken, Task<LicenseResult>> _pulse;
        private readonly Func<string, LicenseVerifyResult> _verify;
        private readonly LicenseState _state;
        private readonly TimeSpan _activeInterval;
        private readonly TimeSpan _idleInterval;
        private readonly TimeSpan _connectionLostAfter;

        private CancellationTokenSource _cts;
        private int _started;

        /// <summary>
        /// Khử trùng lặp: -1 chưa báo gì, 0 đã báo mất, 1 đã báo có. Vòng lặp gọi
        /// RaiseLost/RaiseActivated thoải mái mỗi nhịp; chỉ chuyển trạng thái mới phát.
        /// </summary>
        private int _lastNotified = -1;

        /// <summary>
        /// Chuông đánh thức vòng lặp đang ngủ. Đây là đường để ribbon mở NGAY sau khi
        /// người dùng nhập key, thay vì phải đợi hết nhịp hoặc khởi động lại Revit.
        /// </summary>
        private TaskCompletionSource<bool> _wake = NewWake();

        public LicenseSession(
            Func<CancellationToken, Task<LicenseResult>> pulse,
            Func<string, LicenseVerifyResult> verify,
            LicenseState state,
            TimeSpan? activeInterval = null,
            TimeSpan? idleInterval = null,
            TimeSpan? connectionLostAfter = null)
        {
            if (pulse == null) throw new ArgumentNullException("pulse");
            if (verify == null) throw new ArgumentNullException("verify");
            if (state == null) throw new ArgumentNullException("state");

            _pulse = pulse;
            _verify = verify;
            _state = state;
            _activeInterval = activeInterval ?? LicenseOptions.HeartbeatInterval;
            _idleInterval = idleInterval ?? LicenseOptions.IdleInterval;
            _connectionLostAfter = connectionLostAfter ?? LicenseOptions.ConnectionLostAfter;
        }

        /// <summary>License trở nên hợp lệ → lớp trên mở ribbon.</summary>
        public event Action Activated;

        /// <summary>License mất hoặc chưa có → lớp trên khoá ribbon. Tham số là lý do.</summary>
        public event Action<string> Lost;

        /// <summary>Mất liên lạc quá lâu trong khi token còn hạn → chỉ báo, KHÔNG khoá.</summary>
        public event Action ConnectionLost;

        public LicenseState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Chạy một nhịp ngay rồi khởi động vòng lặp nền. Trả về true nếu license hợp lệ
        /// ngay lúc này. KHÔNG ném cho các tình huống thông thường.
        ///
        /// Vòng lặp nền chạy ở MỌI nhánh, kể cả khi nhịp đầu thất bại: nếu chỉ khởi động
        /// vòng lặp ở nhánh thành công thì người dùng mở Revit lúc chưa có license sẽ
        /// không có đường nào biết mình vừa nhập key xong — phải khởi động lại Revit.
        /// </summary>
        public async Task<bool> StartAsync(CancellationToken ct)
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
            {
                return _state.IsActive;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            PulseOutcome first;
            try
            {
                first = await PulseAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception)
            {
                first = PulseOutcome.TransportError;
            }

            StartLoop();
            return first == PulseOutcome.Active;
        }

        private void StartLoop()
        {
            CancellationToken token = _cts.Token;
            Task.Run(delegate { return RunAsync(token); });
        }

        /// <summary>Đánh thức vòng lặp để chạy nhịp ngay. An toàn gọi từ luồng khác.</summary>
        public void WakeNow()
        {
            _wake.TrySetResult(true);
        }

        /// <summary>Khoá ngay lập tức (đăng xuất). Xoá trạng thái rồi báo mất.</summary>
        public void ApplyRevoked(string reason)
        {
            _state.Clear();
            RaiseLost(reason ?? "REVOKED");
        }

        private async Task RunAsync(CancellationToken ct)
        {
            DateTime? failingSinceUtc = null;
            bool connectionLostNotified = false;

            while (!ct.IsCancellationRequested)
            {
                // Chưa có license, hoặc đang trong đợt mất liên lạc → dò dồn.
                TimeSpan wait = _state.IsActive && failingSinceUtc == null
                    ? _activeInterval
                    : _idleInterval;

                try
                {
                    Task delay = Task.Delay(wait, ct);
                    Task woken = _wake.Task;
                    Task first = await Task.WhenAny(delay, woken).ConfigureAwait(false);
                    if (first == woken)
                    {
                        // Thay chuông mới TRƯỚC khi chạy nhịp: tín hiệu đến trong lúc đang
                        // chạy sẽ reo chuông mới và vòng sau dậy ngay, không mất tín hiệu.
                        Interlocked.Exchange(ref _wake, NewWake());
                        LicenseLog.Write("wake: chạy nhịp ngay");
                    }

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                PulseOutcome outcome;
                try
                {
                    outcome = await PulseAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (OperationCanceledException)
                {
                    // Quá thời gian chờ của một yêu cầu lọt tới đây. Phòng thủ tầng hai —
                    // KHÔNG được để vòng lặp chết.
                    LicenseLog.Write("nhịp: yêu cầu quá thời gian chờ");
                    outcome = PulseOutcome.TransportError;
                }
                catch (Exception)
                {
                    outcome = PulseOutcome.TransportError;
                }

                if (outcome == PulseOutcome.TransportError)
                {
                    if (failingSinceUtc == null)
                    {
                        failingSinceUtc = DateTime.UtcNow;
                    }
                    else if (!connectionLostNotified
                             && DateTime.UtcNow - failingSinceUtc.Value >= _connectionLostAfter)
                    {
                        // Đứt đủ lâu → báo người dùng MỘT lần. Tool VẪN mở: khoá thật chỉ
                        // xảy ra khi token hết hạn hoặc server nói rõ.
                        connectionLostNotified = true;
                        RaiseConnectionLost();
                    }
                }
                else
                {
                    // Server có trả lời: kết thúc đợt đứt, đợt sau được báo lại từ đầu.
                    failingSinceUtc = null;
                    connectionLostNotified = false;
                }
            }
        }

        /// <summary>Một nhịp: lấy token → kiểm chữ ký → cập nhật trạng thái.</summary>
        private async Task<PulseOutcome> PulseAsync(CancellationToken ct)
        {
            LicenseResult result;
            try
            {
                result = await _pulse(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // CHỈ ném lại khi chính vòng lặp bị dừng (Revit đang tắt).
                throw;
            }
            catch (Exception)
            {
                // KHÔNG xoá token: để token đang có chạy tới hết hạn của nó.
                LicenseLog.Write("nhịp: lỗi hạ tầng");
                return PulseOutcome.TransportError;
            }

            if (result != null && result.HasToken)
            {
                LicenseVerifyResult verified = _verify(result.Token);
                if (verified.Ok)
                {
                    _state.Set(verified.License);
                    RaiseActivated();
                    return PulseOutcome.Active;
                }

                // Có token nhưng chữ ký không hợp lệ → không tin, khoá.
                _state.Clear();
                RaiseLost("INVALID_TOKEN");
                return PulseOutcome.Lost;
            }

            _state.Clear();
            string reason = result != null ? result.NoLicenseReason : "NO_LICENSE";
            LicenseLog.Write("nhịp: mất license (" + reason + ")");
            RaiseLost(reason);
            return PulseOutcome.Lost;
        }

        private void RaiseActivated()
        {
            if (Interlocked.Exchange(ref _lastNotified, 1) == 1)
            {
                return;
            }

            LicenseLog.Write("đã kích hoạt → mở ribbon");
            Action handler = Activated;
            if (handler != null)
            {
                handler();
            }
        }

        private void RaiseLost(string reason)
        {
            // Lý do vẫn ghi nhật ký kể cả khi bị khử trùng lặp — chẩn đoán cần chuỗi đầy đủ.
            LicenseLog.Write("mất: " + reason);
            if (Interlocked.Exchange(ref _lastNotified, 0) == 0)
            {
                return;
            }

            Action<string> handler = Lost;
            if (handler != null)
            {
                handler(reason);
            }
        }

        private void RaiseConnectionLost()
        {
            LicenseLog.Write("mất liên lạc quá ngưỡng — báo người dùng (KHÔNG khoá)");
            Action handler = ConnectionLost;
            if (handler != null)
            {
                try
                {
                    handler();
                }
                catch (Exception)
                {
                }
            }
        }

        private static TaskCompletionSource<bool> NewWake()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Dispose()
        {
            try
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }
            }
            catch (Exception)
            {
                // Dispose không được ném.
            }
        }
    }
}
