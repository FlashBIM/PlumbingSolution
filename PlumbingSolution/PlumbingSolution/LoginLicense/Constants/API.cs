using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.LoginLicense.Constants
{
    public class API
    {
        // // Local
        // public const string BaseUrl = "http://localhost:6060";

        // Server
#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
        public const string BaseUrl = "https://api.flashbim.vn";
#else
        public const string BaseUrl = "https://api.flashbim.com";
#endif
        //public const string BaseUrl = "https://7838-2402-800-61c7-cac2-1af-b062-cb20-1d8e.ngrok-free.app";

        // public const string RealtimeUri = "/realtime?token={0}"; // SOCKET
        public const string RealtimeUri = "/api/socket"; // SOCKET

        public const string LoginApi = "api/v1/auth/add-in/sign-in"; // POST
        public const string GetLicenseData = "api/v1/auth/add-in/get-license"; // POST

        // ---- Luồng kích hoạt mới (token RS256). Endpoint cũ ở trên GIỮ NGUYÊN cho
        //      các bản add-in đã phát hành ngoài thị trường. ----

        /// <summary>POST — kích hoạt bằng license key hoặc tài khoản, trả activationId + token.</summary>
        public const string ActivateApi = "api/v1/license/activate";

        /// <summary>POST — làm mới token bằng activationId. Add-in gọi mỗi 5 phút.</summary>
        public const string RefreshApi = "api/v1/license/refresh";

        /// <summary>
        /// Gờ giảm tốc chống máy quét cho hai endpoint ẩn danh ở trên.
        /// ⚠ KHÔNG PHẢI xác thực: giá trị này nằm trong file nhị phân, trích ra được bằng
        /// strings hay bất kỳ trình dịch ngược nào. Bảo mật thật là chữ ký RS256 và ràng
        /// buộc mã máy. Phải khớp cấu hình LicenseActivation:CheckKey phía server; để
        /// trống thì cả hai bên cùng bỏ qua.
        /// </summary>
        public const string CheckKeyHeader = "X-Header-Check-Key";
        public const string CheckKeyValue = "E4F49D7D249747D2B4326C92CF24E0A7";
    }

    public class SocketConstant
    {
        public const string GetFirstData = "GetFirstData";
        public const string LockLicense = "LockLicense";
        public const string DeleteLicense = "DeleteLicense";
        public const string LicenseExpired = "LicenseExpired";
    }
}