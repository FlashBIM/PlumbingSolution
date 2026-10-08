using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.LoginLicense.Client
{
    /// <summary>
    /// Định dạng license key <c>XXXXX-XXXXX-XXXXX-XXXXX-XXXXX</c>.
    /// Logic thuần, không dính giao diện, nên kiểm thử được mọi cách gõ và dán.
    /// </summary>
    public static class LicenseKeyFormat
    {
        public const int GroupSize = 5;
        public const int GroupCount = 5;
        public const int RawLength = GroupSize * GroupCount;

        /// <summary>
        /// GIỮ NGUYÊN những gì người dùng gõ hoặc dán, kể cả dấu gạch và kiểu chữ; chỉ
        /// bỏ khoảng trắng.
        ///
        /// CỐ Ý không tự chèn gạch, không viết hoa, không cắt bớt: sửa ký tự trong lúc
        /// người ta đang gõ làm con trỏ nhảy chỗ và khiến họ tưởng mình gõ sai. Việc
        /// chuẩn hoá để gửi lên server là của <see cref="Strip"/>.
        /// </summary>
        public static string Format(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var result = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (!char.IsWhiteSpace(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        public static bool IsComplete(string input)
        {
            return Strip(input).Length == RawLength;
        }

        /// <summary>
        /// Chuẩn hoá để GỬI LÊN SERVER: bỏ khoảng trắng, viết hoa, GIỮ NGUYÊN dấu gạch.
        ///
        /// Phải giữ gạch: <c>License.Code</c> lưu trong DB đúng dạng
        /// <c>XXXXX-XXXXX-XXXXX-XXXXX-XXXXX</c> (xem server <c>LicenseHelper.GenerateLicense</c>),
        /// và <c>LicenseActivationService</c> so khớp bằng <c>Code.ToUpper() == licenseKey</c> —
        /// so một chuỗi đã bị xoá gạch với cột có gạch thì KHÔNG BAO GIỜ khớp.
        ///
        /// Khác <see cref="Strip"/>: hàm đó xoá cả gạch, chỉ dùng nội bộ cho việc đếm độ
        /// dài (<see cref="IsComplete"/>) và tự nhóm lại ký tự để che key
        /// (<see cref="Mask"/>) — không dùng để gửi đi.
        /// </summary>
        public static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (!char.IsWhiteSpace(c))
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Bỏ MỌI ký tự không phải chữ/số (kể cả gạch) và viết hoa — chỉ dùng NỘI BỘ để
        /// đếm độ dài (<see cref="IsComplete"/>) và tự nhóm lại 5 ký tự một khi che key
        /// (<see cref="Mask"/>). KHÔNG dùng giá trị trả về của hàm này để gửi lên server —
        /// dùng <see cref="Normalize"/>.
        /// </summary>
        public static string Strip(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(RawLength);
            foreach (char c in input)
            {
                if (IsAsciiLetterOrDigit(c))
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Che key để hiển thị: giữ nhóm đầu và nhóm cuối. Tự nhóm lại từ
        /// <see cref="Strip"/> chứ không dựa vào <see cref="Format"/>, vì Format giữ
        /// nguyên những gì người dùng gõ nên không bảo đảm có dấu gạch.
        /// </summary>
        public static string Mask(string input)
        {
            string raw = Strip(input);
            if (raw.Length == 0)
            {
                return string.Empty;
            }

            var groups = new List<string>();
            for (int i = 0; i < raw.Length; i += GroupSize)
            {
                groups.Add(raw.Substring(i, Math.Min(GroupSize, raw.Length - i)));
            }

            if (groups.Count < 3)
            {
                // Quá ngắn để vừa che vừa còn ý nghĩa đối chiếu.
                return string.Join("-", groups.ToArray());
            }

            for (int i = 1; i < groups.Count - 1; i++)
            {
                groups[i] = new string('X', groups[i].Length);
            }

            return string.Join("-", groups.ToArray());
        }

        // net48 không có char.IsAsciiLetterOrDigit (chỉ từ .NET 7).
        private static bool IsAsciiLetterOrDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
    }
}
