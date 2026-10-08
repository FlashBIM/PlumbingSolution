using System;

namespace PlumbingSolution.LoginLicense.Runtime
{
    /// <summary>
    /// Các hằng số nhịp của luồng license.
    ///
    /// CỐ Ý là hằng số chứ không phải file cấu hình: bản Dirit đọc từ
    /// <c>dirit.config.json</c> cạnh DLL, nhưng PlumbingSolution vốn đã gắn cứng địa chỉ
    /// server bằng <c>#if</c>, nên thêm một file cấu hình chỉ tạo thêm một thứ nữa có
    /// thể sai lệch ở máy khách mà không ai biết.
    /// </summary>
    public static class LicenseOptions
    {
        /// <summary>
        /// Khoảng cách giữa hai lần gọi server. Đây chính là ĐỘ TRỄ THU HỒI: admin khoá
        /// license lúc T thì máy khách biết chậm nhất lúc T+5 phút.
        /// </summary>
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

        /// <summary>Nhịp vòng lặp khi đang có license — chỉ cần giữ token tươi.</summary>
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(45);

        /// <summary>
        /// Nhịp vòng lặp khi CHƯA có license. Dò dồn để người dùng vừa nhập key xong là
        /// ribbon mở ngay, không phải khởi động lại Revit.
        /// </summary>
        public static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Mất liên lạc liên tục quá ngưỡng này thì báo cho người dùng MỘT lần. Vẫn KHÔNG
        /// khoá tool: token còn hạn thì vẫn còn quyền dùng.
        /// </summary>
        public static readonly TimeSpan ConnectionLostAfter = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Vùng đệm sau khi token hết hạn mà tool vẫn chạy, chờ nối lại được server.
        /// Chỉ che được token hết hạn TỰ NHIÊN do mất mạng kéo dài — license bị thu hồi
        /// thật luôn xoá trạng thái trước khi báo, nên không bao giờ hưởng vùng đệm này.
        /// </summary>
        public static readonly TimeSpan GraceWindow = TimeSpan.FromMinutes(5);

        /// <summary>Trần cứng của vùng đệm, kẹp ngay tại nơi dùng.</summary>
        public const long MaxGraceSeconds = 900;
    }
}
