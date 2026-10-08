using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.LoginLicense.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        /// <summary>
        /// Mã lỗi máy-đọc-được của luồng license (INVALID_KEY, MACHINE_LIMIT, EXPIRED...).
        /// Null ở mọi endpoint khác. Add-in BẮT BUỘC phân loại lỗi theo mã này chứ không
        /// theo thông điệp: thông điệp đã được dịch theo ngôn ngữ nên không so khớp được.
        /// </summary>
        public string ErrorCode { get; set; }
        public IList<string> Errors { get; set; }
        public T Result { get; set; }
    }
}