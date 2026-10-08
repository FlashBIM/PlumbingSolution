using System;
using System.IO;
using System.Text;

namespace PlumbingSolution.LoginLicense
{
    /// <summary>
    /// Nhật ký phẳng cho luồng license.
    ///
    /// Chạy ở MỌI cấu hình build, không gắn <c>#if DEBUG</c>: bản Release ở máy khách
    /// mới chính là bản cần dấu vết khi có sự cố. Khách gửi file này về là dựng lại
    /// được chuỗi sự kiện.
    ///
    /// ⚠ TUYỆT ĐỐI không ghi token, license key, mật khẩu hay componentsJson vào đây.
    /// Chỉ ghi tên trạng thái và lý do từ chối.
    ///
    /// Mọi lỗi ghi file đều bị nuốt: không ghi được nhật ký thì mất dấu vết, còn ném
    /// lên là làm hỏng phiên Revit của khách.
    /// </summary>
    public static class LicenseLog
    {
        private const long MaxBytes = 1024 * 1024;
        private static readonly object Gate = new object();
        private static string _path;

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    string path = ResolvePath();
                    if (path == null)
                    {
                        return;
                    }

                    RollIfTooLarge(path);

                    string line = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss'Z' ") + message + Environment.NewLine;
                    File.AppendAllText(path, line, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Nhật ký là thứ tốt-thì-có. Không bao giờ được làm hỏng luồng chính.
            }
        }

        /// <summary>Đường dẫn file nhật ký, để hiển thị khi hướng dẫn khách gửi log.</summary>
        public static string CurrentPath()
        {
            try
            {
                return ResolvePath();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ResolvePath()
        {
            if (_path != null)
            {
                return _path;
            }

            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlashBIM", "PlumbingSolution");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "license.log");
            return _path;
        }

        // Giữ đúng một file cũ: đủ để điều tra sự cố mà không ăn dần đĩa của khách.
        private static void RollIfTooLarge(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < MaxBytes)
            {
                return;
            }

            string previous = path + ".1";
            if (File.Exists(previous))
            {
                File.Delete(previous);
            }

            File.Move(path, previous);
        }
    }
}
