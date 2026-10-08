using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PlumbingSolution.LoginLicense.Client;
using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.LoginLicense.Hwid;
using PlumbingSolution.LoginLicense.Models.User;
using PlumbingSolution.LoginLicense.Verify;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace PlumbingSolution.LoginLicense.Runtime
{
    /// <summary>
    /// Điểm vào DUY NHẤT của luồng license cho add-in.
    ///
    /// Cách dùng:
    /// <code>
    /// _runtime = LicenseRuntime.Start();
    /// LicenseGate.Bind(_runtime.State, OnToolBlocked);
    /// _runtime.Activated += ...;  _runtime.Lost += ...;
    /// Task.Run(() =&gt; _runtime.StartSessionAsync(CancellationToken.None));
    /// </code>
    ///
    /// <see cref="Start"/> CHỈ dựng và nối dây, không chạy gì — để nơi gọi kịp gắn sự
    /// kiện trước khi nhịp đầu tiên bắn.
    ///
    /// So với bản Dirit: bỏ toàn bộ chế độ chạy qua launcher (named pipe, spawn tiến
    /// trình). PlumbingSolution không có launcher; mọi thứ chạy ngay trong tiến trình Revit.
    /// </summary>
    public sealed class LicenseRuntime : IDisposable
    {
        private LicenseApiClient _api;
        private TokenManager _tokens;
        private DirectTokenSource _source;
        private LicenseSession _session;
        private LicenseState _state;
        private CancellationTokenSource _cts;
        private PowerModeChangedEventHandler _powerHandler;
        private int _disposed;

        private LicenseRuntime()
        {
        }

        /// <summary>Trạng thái license của phiên — nối vào <see cref="LicenseGate"/>.</summary>
        public LicenseState State
        {
            get { return _state; }
        }

        /// <summary>License đang hợp lệ?</summary>
        public bool IsActive
        {
            get { return _state != null && _state.IsActive; }
        }

        /// <summary>Thông tin license để hiển thị. Null khi chưa kích hoạt.</summary>
        public LicenseData LicenseInfo
        {
            get { return _tokens == null ? null : _tokens.License; }
        }

        /// <summary>Kích hoạt bằng key hay bằng tài khoản — cửa sổ My License hiển thị khác nhau.</summary>
        public ActivationMethod ActivatedBy
        {
            get { return _tokens == null ? ActivationMethod.Key : _tokens.Method; }
        }

        public event Action Activated;

        public event Action<string> Lost;

        public event Action ConnectionLost;

        public static LicenseRuntime Start(IActivationStore store = null, ILicenseApiClient api = null)
        {
            var runtime = new LicenseRuntime();
            runtime._cts = new CancellationTokenSource();
            runtime._state = new LicenseState(null, LicenseOptions.GraceWindow);

            Func<string> machineId = HwidInterop.GetMachineId;

            if (api == null)
            {
                runtime._api = new LicenseApiClient();
                api = runtime._api;
            }

            runtime._tokens = new TokenManager(api);
            runtime._source = new DirectTokenSource(
                runtime._tokens,
                store ?? new ActivationStore(),
                machineId,
                LicenseTokenVerifier.ProductCode,
                LicenseOptions.RefreshInterval);

            var verifier = LicenseTokenVerifier.CreateDefault(machineId);

            runtime._session = new LicenseSession(
                runtime._source.PulseAsync, verifier.Verify, runtime._state);

            runtime._session.Activated += runtime.OnActivated;
            runtime._session.Lost += runtime.OnLost;
            runtime._session.ConnectionLost += runtime.OnConnectionLost;

            // Máy thức dậy sau khi ngủ: token gần như chắc chắn đã hết hạn, chạy nhịp
            // ngay thay vì để người dùng nhấp vào một nút đã âm thầm bị khoá.
            // BẮT BUỘC gỡ handler khi Dispose: SystemEvents là static, quên gỡ là giữ
            // sống cả AppDomain của add-in.
            runtime._powerHandler = runtime.OnPowerModeChanged;
            SystemEvents.PowerModeChanged += runtime._powerHandler;

            return runtime;
        }

        /// <summary>Chạy vòng đời license. Gọi MỘT lần, từ luồng nền.</summary>
        public Task<bool> StartSessionAsync(CancellationToken externalCt)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, externalCt))
            {
                return _session.StartAsync(linked.Token);
            }
        }

        /// <summary>Kích hoạt bằng license key. Trả null nếu thành công.</summary>
        public Task<ActivationError?> ActivateAsync(string licenseKey, CancellationToken ct)
        {
            if (!LicenseKeyFormat.IsComplete(licenseKey))
            {
                return Task.FromResult<ActivationError?>(ActivationError.InvalidKey);
            }

            return ActivateCoreAsync(
                delegate(ActivateRequest r) { r.LicenseKey = LicenseKeyFormat.Normalize(licenseKey); }, ct);
        }

        /// <summary>
        /// Kích hoạt bằng tài khoản. Mật khẩu chỉ đi qua đây MỘT lần rồi biến mất khỏi
        /// bộ nhớ — không lưu vào registry, không lưu ra đĩa.
        /// </summary>
        public Task<ActivationError?> ActivateAsync(string username, string password, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return Task.FromResult<ActivationError?>(ActivationError.InvalidKey);
            }

            return ActivateCoreAsync(
                delegate(ActivateRequest r)
                {
                    r.Username = username.Trim();
                    r.Password = password;
                }, ct);
        }

        private async Task<ActivationError?> ActivateCoreAsync(
            Action<ActivateRequest> applyCredential, CancellationToken ct)
        {
            string machineIdValue;
            string componentsJson;
            int algoVersion;
            try
            {
                machineIdValue = HwidInterop.GetMachineId();
                componentsJson = HwidInterop.GetComponentsJson();
                algoVersion = HwidInterop.GetAlgoVersion();
            }
            catch (Exception e)
            {
                // hwid.dll thiếu hoặc hỏng: không gửi được ràng buộc máy nên không kích
                // hoạt được. Báo như lỗi cục bộ chứ không đổ cho server.
                LicenseLog.Write("kích hoạt: lỗi hwid — " + e.Message);
                return ActivationError.UnknownLogicError;
            }

            var request = new ActivateRequest
            {
                MachineId = machineIdValue,
                HwidAlgoVersion = algoVersion,
                ProductCode = LicenseTokenVerifier.ProductCode,
                ClientVersion = ClientVersion(),
            };

            try
            {
                request.Components = JToken.Parse(componentsJson);
            }
            catch (Exception)
            {
                // hwid.dll trả JSON hỏng: vẫn kích hoạt được, server chỉ mất dữ liệu chẩn đoán.
                request.Components = null;
            }

            applyCredential(request);

            ActivationError? error = await _tokens.ActivateAsync(request, ct).ConfigureAwait(false);
            if (error != null)
            {
                return error;
            }

            // Nhớ lần kích hoạt để lần mở Revit sau không phải nhập lại.
            // Chỉ activationId/machineId/productCode — KHÔNG token, KHÔNG key, KHÔNG mật khẩu.
            ActivationRecord record = _tokens.GetActivation();
            if (record != null)
            {
                _source.Store.Save(record);
            }

            _source.MarkFreshToken();
            _session.WakeNow();
            return null;
        }

        /// <summary>
        /// Đăng xuất — CHỈ tác động cục bộ: xoá token và bản ghi, khoá phiên NÀY ngay.
        ///
        /// CỐ Ý không gọi server: suất máy được giữ nguyên nên đăng nhập lại không tốn
        /// thêm suất. Muốn giải phóng suất thì gỡ máy qua trang quản lý.
        ///
        /// Thứ tự BẮT BUỘC: xoá token TRƯỚC khi báo — nếu không, một nhịp chen vào giữa
        /// vẫn lấy được token cũ còn hạn.
        /// </summary>
        public void Logout()
        {
            _tokens.Clear();
            _source.Store.Delete();
            _source.ResetCadence();
            _session.ApplyRevoked("LOGGED_OUT");
            LicenseLog.Write("đăng xuất (cục bộ, suất máy giữ nguyên)");
        }

        /// <summary>Đánh thức vòng nhịp ngay.</summary>
        public void WakeNow()
        {
            LicenseSession session = _session;
            if (session != null)
            {
                session.WakeNow();
            }
        }

        private void OnActivated()
        {
            Action handler = Activated;
            if (handler != null)
            {
                handler();
            }
        }

        private void OnLost(string reason)
        {
            Action<string> handler = Lost;
            if (handler != null)
            {
                handler(reason);
            }
        }

        private void OnConnectionLost()
        {
            Action handler = ConnectionLost;
            if (handler != null)
            {
                handler();
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                LicenseLog.Write("máy thức dậy — chạy nhịp ngay");
                WakeNow();
            }
        }

        private static string ClientVersion()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch (Exception)
            {
                return "0.0.0";
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (_powerHandler != null)
            {
                SystemEvents.PowerModeChanged -= _powerHandler;
                _powerHandler = null;
            }

            try
            {
                _cts.Cancel();
            }
            catch (Exception)
            {
            }

            if (_session != null)
            {
                _session.Dispose();
            }

            if (_tokens != null)
            {
                _tokens.Dispose();
            }

            if (_api != null)
            {
                _api.Dispose();
            }

            _cts.Dispose();
        }
    }
}
