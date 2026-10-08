using System;
using PlumbingSolution.LoginLicense.Runtime;
using PlumbingSolution.LoginLicense.Verify;

namespace PlumbingSolution.LoginLicense.Gate
{
    /// <summary>
    /// Giữ license đã xác thực của phiên hiện tại.
    ///
    /// BẢO MẬT: <see cref="Set"/> và <see cref="Clear"/> là <c>internal</c> — chỉ đường
    /// dẫn kiểm chữ ký bên trong add-in mới đặt được trạng thái. Không có cờ boolean công
    /// khai nào để lật, nên không có cách nào mở khoá tool mà không có token thật.
    ///
    /// "Còn hiệu lực" được tính TẠI THỜI ĐIỂM HỎI từ hạn của token, không cache — một cờ
    /// đã cache sẽ vẫn đúng một lúc sau khi token đã hết hạn.
    /// </summary>
    public sealed class LicenseState
    {
        private readonly object _gate = new object();
        private readonly Func<long> _nowUnix;
        private readonly long _graceSeconds;
        private VerifiedLicense _current;

        public LicenseState(Func<long> nowUnixProvider = null, TimeSpan? graceWindow = null)
        {
            _nowUnix = nowUnixProvider ?? DefaultNowUnix;

            long seconds = (long)(graceWindow ?? TimeSpan.Zero).TotalSeconds;
            if (seconds < 0)
            {
                seconds = 0;
            }

            // Kẹp NGAY TẠI ĐÂY chứ không chỉ ở nơi cấu hình: hàm dựng là công khai, đừng
            // để một nơi gọi nào đó trong tương lai quên kẹp là mở toang vùng đệm.
            if (seconds > LicenseOptions.MaxGraceSeconds)
            {
                seconds = LicenseOptions.MaxGraceSeconds;
            }

            _graceSeconds = seconds;
        }

        /// <summary>License hiện tại đã xác thực, hoặc null. Chỉ đọc.</summary>
        public VerifiedLicense Current
        {
            get { lock (_gate) { return _current; } }
        }

        /// <summary>Có license đã xác thực VÀ token chưa hết hạn.</summary>
        public bool IsActive
        {
            get
            {
                lock (_gate)
                {
                    return _current != null && _nowUnix() < _current.TokenExpUnix;
                }
            }
        }

        /// <summary>
        /// Đang trong vùng đệm SAU KHI token hết hạn tự nhiên — để một đợt mất mạng dài
        /// không khoá tool ngay giữa lúc người dùng đang làm việc.
        ///
        /// KHÔNG áp cho thu hồi, đăng xuất hay chưa-kích-hoạt: mọi đường đó đều gọi
        /// <see cref="Clear"/> trước khi báo, nên điều kiện <c>_current != null</c> ở đây
        /// tự loại chúng ra.
        ///
        /// CỐ Ý tách khỏi <see cref="IsActive"/>: IsActive giữ nguyên nghĩa cũ để vòng lặp
        /// vẫn dồn nhịp ngay khi token hết hạn, kể cả đang trong vùng đệm.
        /// </summary>
        public bool IsWithinGrace
        {
            get
            {
                lock (_gate)
                {
                    if (_current == null || _graceSeconds <= 0)
                    {
                        return false;
                    }

                    return _nowUnix() < _current.TokenExpUnix + _graceSeconds;
                }
            }
        }

        /// <summary>Đặt license sau khi kiểm chữ ký thành công. Internal — không công khai.</summary>
        internal void Set(VerifiedLicense license)
        {
            if (license == null) throw new ArgumentNullException("license");
            lock (_gate)
            {
                _current = license;
            }
        }

        /// <summary>Xoá license (thu hồi / không có / token hỏng). Khoá tính năng.</summary>
        internal void Clear()
        {
            lock (_gate)
            {
                _current = null;
            }
        }

        private static long DefaultNowUnix()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
