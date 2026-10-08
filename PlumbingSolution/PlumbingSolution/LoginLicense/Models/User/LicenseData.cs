using PlumbingSolution.LoginLicense.Enums;
using System;

namespace PlumbingSolution.LoginLicense.Models.User
{
    public class LicenseData
    {
        public Guid Id { get; set; }
        public UserData User { get; set; }
        public ProductData Product { get; set; }
        public string Code { get; set; }
        public LicenseType Type { get; set; }
        public LicenseStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsTrial { get; set; }

        public Guid? GroupId { get; set; }

        // ---- Các trường luồng kích hoạt mới bổ sung (luồng cũ để trống, không ảnh hưởng) ----

        /// <summary>Email chủ license — cửa sổ My License hiển thị "Kích hoạt cho ...".
        /// Trước đây lấy từ registry; giờ lấy thẳng từ server nên không cần lưu gì trên máy.</summary>
        public string Email { get; set; }

        /// <summary>Key đã che dạng <c>*****-AB12</c>. Server không bao giờ trả key đầy đủ.</summary>
        public string CodeMasked { get; set; }

        /// <summary>Số máy tối đa được kích hoạt.</summary>
        public int MaxDevice { get; set; }

        /// <summary>Số máy đang kích hoạt.</summary>
        public int ActiveDeviceCount { get; set; }
    }
}