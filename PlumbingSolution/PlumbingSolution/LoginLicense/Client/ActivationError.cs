namespace PlumbingSolution.LoginLicense.Client
{
    /// <summary>
    /// Phân loại thất bại khi gọi server license. Cách phân nhóm ở đây quyết định hành
    /// vi của toàn bộ hệ thống — xem <see cref="ActivationErrorExtensions.RevokesLicense"/>.
    /// </summary>
    public enum ActivationError
    {
        /// <summary>Mất mạng, hết thời gian chờ, DNS hỏng, lỗi TLS.</summary>
        Network,

        /// <summary>Lỗi 5xx — server hỏng hoặc đang triển khai bản mới.</summary>
        ServerError,

        /// <summary>429 — chạm giới hạn tần suất. KHÔNG phải license có vấn đề.</summary>
        RateLimited,

        /// <summary>Không phân tích được phản hồi, hoặc phản hồi có giá trị vô lý.</summary>
        MalformedResponse,

        LicenseTypeNotSupport,
        InvalidKey,
        InvalidActivation,
        ProductMismatch,

        Expired,
        Locked,
        MachineLimit,
        Revoked,

        /// <summary>Server báo lỗi nghiệp vụ nhưng không kèm mã nhận dạng được.</summary>
        UnknownLogicError,
    }

    public static class ActivationErrorExtensions
    {
        /// <summary>
        /// CHỈ những lỗi này mới được phép huỷ license đang chạy (xoá token, khoá ribbon
        /// ở mọi phiên Revit đang mở).
        ///
        /// Danh sách CỐ Ý hẹp. Bản thiết kế đầu từng xếp 429 và 5xx vào nhóm "lỗi nghiệp
        /// vụ"; khi đó một lần chạm giới hạn tần suất sẽ khoá sạch Revit của khách hàng
        /// có license hoàn toàn hợp lệ. Đừng thêm gì vào đây mà không cân nhắc kỹ.
        /// </summary>
        public static bool RevokesLicense(this ActivationError error)
        {
            switch (error)
            {
                case ActivationError.Expired:
                case ActivationError.Locked:
                case ActivationError.MachineLimit:
                case ActivationError.Revoked:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Lỗi hạ tầng: GIỮ NGUYÊN token đang có và thử lại sau.</summary>
        public static bool IsTransient(this ActivationError error)
        {
            switch (error)
            {
                case ActivationError.Network:
                case ActivationError.ServerError:
                case ActivationError.RateLimited:
                case ActivationError.MalformedResponse:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Ánh xạ mã lỗi server sang enum. Mã thiếu hoặc lạ đều rơi về
        /// <see cref="ActivationError.UnknownLogicError"/> — dừng lại nhưng KHÔNG huỷ
        /// license đang chạy, vì một lỗi mơ hồ không đủ căn cứ để khoá tool của người
        /// đang dựng mô hình.
        /// </summary>
        public static ActivationError FromServerCode(string errorCode)
        {
            switch (errorCode)
            {
                case "LICENSE_TYPE_NOT_SUPPORT":
                    return ActivationError.LicenseTypeNotSupport;
                case "INVALID_KEY":
                    return ActivationError.InvalidKey;
                case "INVALID_ACTIVATION":
                    return ActivationError.InvalidActivation;
                case "PRODUCT_MISMATCH":
                    return ActivationError.ProductMismatch;
                case "EXPIRED":
                    return ActivationError.Expired;
                case "LOCKED":
                    return ActivationError.Locked;
                case "MACHINE_LIMIT":
                    return ActivationError.MachineLimit;
                case "REVOKED":
                    return ActivationError.Revoked;
                default:
                    return ActivationError.UnknownLogicError;
            }
        }
    }
}
