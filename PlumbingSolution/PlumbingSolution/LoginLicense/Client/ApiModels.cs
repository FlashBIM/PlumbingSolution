using System;
using PlumbingSolution.LoginLicense.Models.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlumbingSolution.LoginLicense.Client
{
    /// <summary>
    /// Yêu cầu kích hoạt. Gửi HOẶC <see cref="LicenseKey"/> HOẶC cặp
    /// <see cref="Username"/>/<see cref="Password"/> — server từ chối nếu có cả hai.
    ///
    /// Đây là lần DUY NHẤT mật khẩu hoặc license key rời khỏi máy trong cả vòng đời cài
    /// đặt. Sau đó chỉ còn <c>activationId</c> đi lại.
    /// </summary>
    public sealed class ActivateRequest
    {
        [JsonProperty("licenseKey", NullValueHandling = NullValueHandling.Ignore)]
        public string LicenseKey { get; set; }

        [JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)]
        public string Username { get; set; }

        [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }

        [JsonProperty("machineId")]
        public string MachineId { get; set; }

        /// <summary>JSON thô từ hwid.dll, gửi nguyên văn để server tự đối chiếu.</summary>
        [JsonProperty("components")]
        public JToken Components { get; set; }

        [JsonProperty("hwidAlgoVersion")]
        public int HwidAlgoVersion { get; set; }

        [JsonProperty("productCode")]
        public string ProductCode { get; set; }

        [JsonProperty("clientVersion")]
        public string ClientVersion { get; set; }
    }

    /// <summary>
    /// Yêu cầu làm mới. CỐ Ý không mang license key lẫn mật khẩu: endpoint này chạy mỗi
    /// 5 phút, gửi kèm bí mật ở nhịp đó là biến chúng thành thứ mà bất kỳ proxy nào trên
    /// đường truyền cũng thu hoạch được.
    /// </summary>
    public sealed class RefreshRequest
    {
        [JsonProperty("activationId")]
        public string ActivationId { get; set; }

        [JsonProperty("machineId")]
        public string MachineId { get; set; }

        [JsonProperty("productCode")]
        public string ProductCode { get; set; }
    }

    /// <summary>Cách một lần kích hoạt được xác thực. Khớp enum cùng tên phía server.</summary>
    public enum ActivationMethod
    {
        Key = 0,
        Account = 1,
    }

    /// <summary>
    /// Kết quả kích hoạt/làm mới.
    ///
    /// ⚠ Envelope của server này là <c>{"success":...}</c>. Hệ Dirit dùng
    /// <c>{"succeeded":...}</c> — đọc nhầm khoá khiến MỌI phản hồi thành
    /// <see cref="ActivationError.MalformedResponse"/>. Vì vậy ở đây dùng lại
    /// <see cref="Models.ApiResponse{T}"/> sẵn có của add-in, không tự định nghĩa lại.
    /// </summary>
    public sealed class ActivationResult
    {
        [JsonProperty("activationId")]
        public string ActivationId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("expiresAt")]
        public DateTimeOffset ExpiresAt { get; set; }

        [JsonProperty("activatedBy")]
        public ActivationMethod ActivatedBy { get; set; }

        /// <summary>
        /// Dùng lại <see cref="LicenseData"/> sẵn có nên cửa sổ "My License" không phải
        /// đổi cách đọc dữ liệu. Server trả đúng hình dạng này.
        /// </summary>
        [JsonProperty("license")]
        public LicenseData License { get; set; }
    }
}
