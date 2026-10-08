using PlumbingSolution.LoginLicense.Client;

namespace PlumbingSolution.LoginLicense
{
    /// <summary>
    /// Chuyển mã lỗi license thành câu người dùng đọc được.
    ///
    /// VÌ SAO CẦN, khi server đã trả thông điệp đã dịch sẵn: chỉ nhánh KÍCH HOẠT mới có
    /// thông điệp đó. Vòng nhịp làm mới chạy nền phát <c>Lost("MACHINE_LIMIT")</c> —
    /// trong tay chỉ có MÃ, không có câu chữ nào. Không có bảng này thì hộp thoại lúc
    /// license bị thu hồi giữa phiên sẽ hiện đúng chữ "MACHINE_LIMIT".
    ///
    /// Song ngữ theo cùng cơ chế <c>#if</c> mà <see cref="Ultis.Define"/> đang dùng, để
    /// không sinh ra cách làm thứ hai trong cùng một dự án.
    /// </summary>
    public static class LicenseMessages
    {
#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
        private const string InvalidKey = "Thông tin đăng nhập không đúng. Vui lòng kiểm tra lại tài khoản hoặc key kích hoạt.";
        private const string LicenseTypeNotSupport = "Loại license này không dùng để kích hoạt được. Vui lòng liên hệ chúng tôi.";
        private const string Expired = "Giấy phép của bạn đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng.";
        private const string Locked = "Giấy phép của bạn đang bị khoá. Vui lòng liên hệ chúng tôi để được hỗ trợ.";
        private const string MachineLimit = "Giấy phép đã dùng hết số máy cho phép. Hãy gỡ bớt một máy trên trang quản lý, hoặc mua thêm.";
        private const string Revoked = "Giấy phép đã bị thu hồi. Vui lòng liên hệ chúng tôi.";
        private const string ProductMismatch = "Giấy phép này không dành cho sản phẩm đang chạy.";
        private const string InvalidActivation = "Kích hoạt trên máy này không còn hiệu lực. Vui lòng đăng nhập lại.";
        private const string NotActivated = "Chưa kích hoạt. Vui lòng đăng nhập để sử dụng.";
        private const string LoggedOut = "Bạn đã đăng xuất.";
        private const string RateLimited = "Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.";
        private const string Network = "Không kết nối được máy chủ. Kiểm tra đường truyền rồi thử lại.";
        private const string ServerError = "Máy chủ đang bận. Vui lòng thử lại sau ít phút.";
        private const string Malformed = "Máy chủ trả về dữ liệu không hợp lệ. Vui lòng thử lại.";
        private const string InvalidToken = "Không xác thực được giấy phép trên máy này. Vui lòng đăng nhập lại.";
        private const string Unknown = "Có lỗi không xác định. Vui lòng thử lại hoặc liên hệ chúng tôi.";

        /// <summary>Hiện khi mất liên lạc quá lâu. KHÔNG phải khoá tool — chỉ báo.</summary>
        public const string ConnectionLost =
            "Không kết nối được máy chủ giấy phép. Tool vẫn dùng được một thời gian ngắn; " +
            "vui lòng kiểm tra đường truyền.";
#else
        private const string InvalidKey = "Login information is incorrect. Please check your account or activation key.";
        private const string LicenseTypeNotSupport = "This license type cannot be activated. Please contact us.";
        private const string Expired = "Your license has expired. Please renew to continue using our product.";
        private const string Locked = "Your license has been locked. Please contact us for support.";
        private const string MachineLimit = "This license has reached its device limit. Remove a device in your account page, or purchase more.";
        private const string Revoked = "This license has been revoked. Please contact us.";
        private const string ProductMismatch = "This license is not for the product you are running.";
        private const string InvalidActivation = "The activation on this machine is no longer valid. Please sign in again.";
        private const string NotActivated = "Not activated. Please sign in to continue.";
        private const string LoggedOut = "You have been signed out.";
        private const string RateLimited = "Too many attempts. Please try again in a few minutes.";
        private const string Network = "Cannot reach the server. Check your connection and try again.";
        private const string ServerError = "The server is busy. Please try again in a few minutes.";
        private const string Malformed = "The server returned invalid data. Please try again.";
        private const string InvalidToken = "Could not verify the license on this machine. Please sign in again.";
        private const string Unknown = "An unexpected error occurred. Please try again or contact us.";

        public const string ConnectionLost =
            "Cannot reach the license server. The tool keeps working for a short while; " +
            "please check your connection.";
#endif

        /// <summary>Thông điệp cho lỗi trả về từ lần kích hoạt.</summary>
        public static string ForError(ActivationError error)
        {
            switch (error)
            {
                case ActivationError.LicenseTypeNotSupport: return LicenseTypeNotSupport;
                case ActivationError.InvalidKey: return InvalidKey;
                case ActivationError.InvalidActivation: return InvalidActivation;
                case ActivationError.ProductMismatch: return ProductMismatch;
                case ActivationError.Expired: return Expired;
                case ActivationError.Locked: return Locked;
                case ActivationError.MachineLimit: return MachineLimit;
                case ActivationError.Revoked: return Revoked;
                case ActivationError.RateLimited: return RateLimited;
                case ActivationError.Network: return Network;
                case ActivationError.ServerError: return ServerError;
                case ActivationError.MalformedResponse: return Malformed;
                default: return Unknown;
            }
        }

        /// <summary>
        /// Thông điệp cho lý do mất license do vòng nhịp nền phát ra. Tham số là MÃ, không
        /// phải câu chữ — đây chính là chỗ bảng ánh xạ này là bắt buộc.
        /// </summary>
        public static string ForLostReason(string reason)
        {
            switch (reason)
            {
                case "NOT_ACTIVATED": return NotActivated;
                case "LOGGED_OUT": return LoggedOut;
                case "INVALID_TOKEN": return InvalidToken;
                case null: return Unknown;
                default: return ForError(ActivationErrorExtensions.FromServerCode(reason));
            }
        }

        /// <summary>
        /// Lỗi này có nên kèm liên kết tới website bán hàng không. Chỉ đúng với những lỗi
        /// mà người dùng tự xử lý được bằng cách mua hoặc gia hạn — gắn link vào lỗi mạng
        /// chỉ khiến người ta bực.
        /// </summary>
        public static bool ShowsWebsiteLink(ActivationError error)
        {
            return error == ActivationError.Expired || error == ActivationError.MachineLimit;
        }

        /// <summary>Như trên, nhưng theo mã lý do của vòng nhịp nền.</summary>
        public static bool ShowsWebsiteLinkForReason(string reason)
        {
            return reason == "EXPIRED" || reason == "MACHINE_LIMIT";
        }
    }
}
