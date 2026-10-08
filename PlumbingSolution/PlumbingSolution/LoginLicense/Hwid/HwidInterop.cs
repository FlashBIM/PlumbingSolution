using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PlumbingSolution.LoginLicense.Hwid
{
    /// <summary>Không lấy được mã máy. Luôn ném — KHÔNG BAO GIỜ trả chuỗi rỗng.</summary>
    public sealed class HwidException : Exception
    {
        public HwidException(string message) : base(message) { }
    }

    /// <summary>
    /// Cầu nối tới hwid.dll — nguồn duy nhất của mã máy.
    ///
    /// Viết cho net48 (Revit 2024): KHÔNG dùng NativeLibrary, LibraryImport,
    /// SHA256.HashData hay function pointer (đều chỉ có trên .NET Core).
    ///
    /// Hai quy tắc bảo mật, đừng nới lỏng cái nào:
    ///   1. Nạp bằng đường dẫn TUYỆT ĐỐI — không để thứ tự tìm DLL của Windows quyết định.
    ///   2. Kiểm SHA-256 TRƯỚC khi gọi bất kỳ hàm nào.
    ///
    /// So với mã máy kiểu cũ (<c>Common.GetHardwareId</c> = CPUID + "-" + serial bo mạch):
    /// hàm cũ trả về đúng chuỗi "-" khi cả hai truy vấn WMI thất bại, nghĩa là MỌI máy
    /// gặp lỗi đều dùng chung một mã máy. Ở đây thất bại là ném, không có giá trị rơi về.
    /// </summary>
    public static partial class HwidInterop
    {
        private const string DllName = "hwid.dll";
        private const int ComponentsBufferSize = 4096;
        private const int MachineIdBufferSize = 128;

        // Mã lỗi native, mọi giá trị < 0.
        private const int ErrInvalidArg = -1;
        private const int ErrBufferTooSmall = -2;
        private const int ErrInternal = -3;

        // Cờ LoadLibraryEx: chỉ tìm phụ thuộc ở System32 và thư mục chứa chính DLL đó.
        private const uint LoadLibrarySearchSystem32 = 0x00000800;
        private const uint LoadLibrarySearchDllLoadDir = 0x00000100;

        private static readonly object Gate = new object();
        private static IntPtr _module = IntPtr.Zero;
        private static GetBufferDelegate _getComponents;
        private static GetBufferDelegate _getMachineId;
        private static GetAlgoVersionDelegate _getAlgoVersion;
        private static string _baseDirectory;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetBufferDelegate([Out] byte[] buf, int bufLen);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetAlgoVersionDelegate();

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>Thư mục chứa hwid.dll. Mặc định là thư mục assembly add-in.</summary>
        public static string BaseDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_baseDirectory))
                {
                    _baseDirectory = ResolveDefaultBaseDirectory();
                }

                return _baseDirectory;
            }
            set { _baseDirectory = value; }
        }

        /// <summary>JSON thô các thành phần phần cứng — gửi kèm khi kích hoạt.</summary>
        public static string GetComponentsJson()
        {
            EnsureLoaded();
            return Invoke(_getComponents, ComponentsBufferSize, "components");
        }

        /// <summary>Mã máy: 64 ký tự hex thường (SHA-256 của chuỗi canonical).</summary>
        public static string GetMachineId()
        {
            EnsureLoaded();
            string id = Invoke(_getMachineId, MachineIdBufferSize, "machineId");
            if (id.Length != 64)
            {
                throw new HwidException("machineId dài " + id.Length + " ký tự, phải là 64");
            }

            return id;
        }

        public static int GetAlgoVersion()
        {
            EnsureLoaded();
            return _getAlgoVersion();
        }

        /// <summary>Chỉ dùng cho kiểm thử: giải phóng module để đổi <see cref="BaseDirectory"/>.</summary>
        internal static void ResetForTests()
        {
            lock (Gate)
            {
                if (_module != IntPtr.Zero)
                {
                    FreeLibrary(_module);
                    _module = IntPtr.Zero;
                }

                _getComponents = null;
                _getMachineId = null;
                _getAlgoVersion = null;
                _baseDirectory = null;
            }
        }

        private static void EnsureLoaded()
        {
            if (_module != IntPtr.Zero)
            {
                return;
            }

            lock (Gate)
            {
                if (_module != IntPtr.Zero)
                {
                    return;
                }

                string path = Path.Combine(BaseDirectory, DllName);
                if (!File.Exists(path))
                {
                    throw new HwidException("Không tìm thấy " + DllName + " tại " + path);
                }

                VerifyHash(path);

                IntPtr module = LoadLibraryEx(
                    path, IntPtr.Zero, LoadLibrarySearchSystem32 | LoadLibrarySearchDllLoadDir);
                if (module == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new HwidException("Nạp " + DllName + " thất bại (Win32 " + err + ")");
                }

                _getComponents = GetExport<GetBufferDelegate>(module, "hwid_get_components");
                _getMachineId = GetExport<GetBufferDelegate>(module, "hwid_get_machine_id");
                _getAlgoVersion = GetExport<GetAlgoVersionDelegate>(module, "hwid_get_algo_version");
                _module = module;
            }
        }

        private static T GetExport<T>(IntPtr module, string name) where T : class
        {
            IntPtr proc = GetProcAddress(module, name);
            if (proc == IntPtr.Zero)
            {
                throw new HwidException(DllName + " thiếu export " + name);
            }

            return (T)(object)Marshal.GetDelegateForFunctionPointer(proc, typeof(T));
        }

        private static void VerifyHash(string path)
        {
            byte[] actual;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                actual = sha.ComputeHash(stream);
            }

            if (!FixedTimeEquals(ToHexLower(actual), ExpectedSha256))
            {
                throw new HwidException(DllName + " không khớp chữ ký đã biết — từ chối nạp");
            }
        }

        private static string Invoke(GetBufferDelegate fn, int bufferSize, string what)
        {
            var buffer = new byte[bufferSize];
            int written = fn(buffer, bufferSize);
            if (written < 0)
            {
                // Giá trị âm là lỗi hệ thống. Tuyệt đối không rơi về chuỗi rỗng: mã máy
                // tính từ dữ liệu rỗng sẽ giống hệt nhau trên mọi máy gặp lỗi.
                string reason;
                if (written == ErrInvalidArg) reason = "tham số không hợp lệ";
                else if (written == ErrBufferTooSmall) reason = "buffer quá nhỏ";
                else if (written == ErrInternal) reason = "lỗi nội bộ";
                else reason = "mã " + written;

                throw new HwidException("hwid.dll không trả được " + what + ": " + reason);
            }

            return Encoding.UTF8.GetString(buffer, 0, written);
        }

        private static string ResolveDefaultBaseDirectory()
        {
            // Add-in chạy bên trong Revit.exe nên thư mục ứng dụng là của Revit, KHÔNG
            // phải của add-in. Phải lấy thư mục assembly của chính add-in — nơi hwid.dll
            // được cài đặt đặt cạnh.
            string location = typeof(HwidInterop).Assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                string dir = Path.GetDirectoryName(location);
                if (!string.IsNullOrEmpty(dir))
                {
                    return dir;
                }
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string ToHexLower(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }

            return sb.ToString();
        }

        // So sánh không rẽ nhánh theo nội dung (net48 không có CryptographicOperations).
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
