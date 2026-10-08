using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace PlumbingSolution.LoginLicense.Client
{
    /// <summary>
    /// Những gì được phép ghi xuống đĩa để nhớ lần kích hoạt trước.
    ///
    /// KHÔNG chứa token (sống 15 phút, không bao giờ chạm đĩa), KHÔNG chứa license key,
    /// KHÔNG chứa mật khẩu. So với bản cũ lưu thẳng email và mật khẩu dạng văn bản trong
    /// registry, đây là điểm khác biệt quan trọng nhất của cả đợt thay đổi.
    /// </summary>
    public sealed class ActivationRecord
    {
        [JsonProperty("activationId")]
        public string ActivationId { get; set; }

        [JsonProperty("machineId")]
        public string MachineId { get; set; }

        [JsonProperty("productCode")]
        public string ProductCode { get; set; }

        /// <summary>"Key" hoặc "Account" — chỉ để cửa sổ My License hiển thị đúng dòng.</summary>
        [JsonProperty("activatedBy")]
        public string ActivatedBy { get; set; }

        [JsonIgnore]
        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrEmpty(ActivationId)
                       && !string.IsNullOrEmpty(MachineId)
                       && !string.IsNullOrEmpty(ProductCode);
            }
        }
    }

    /// <summary>Nơi cất bản ghi kích hoạt. Tách giao diện để kiểm thử không đụng đĩa thật.</summary>
    public interface IActivationStore
    {
        ActivationRecord Load();

        void Save(ActivationRecord record);

        void Delete();
    }

    /// <summary>
    /// Lưu bản ghi kích hoạt vào <c>%LOCALAPPDATA%\FlashBIM\activation-&lt;ProductCode&gt;.dat</c>, mã hoá
    /// bằng DPAPI phạm vi người dùng hiện tại.
    ///
    /// DPAPI chặn việc bê file sang máy khác hay tài khoản khác. Kể cả bị giải mã, giá
    /// trị bên trong vẫn vô dụng ở nơi khác: token do server cấp mang mã máy trong chữ
    /// ký, nên add-in ở máy khác tự từ chối.
    ///
    /// Ghi NGUYÊN TỬ (ghi file tạm rồi thay thế) và có khoá liên tiến trình: nhiều phiên
    /// Revit mở cùng lúc là chuyện bình thường, và một file ghi dở sẽ khiến người dùng
    /// phải nhập lại key.
    /// </summary>
    public sealed class ActivationStore : IActivationStore
    {
        private readonly string _path;
        private readonly string _mutexName;

        public ActivationStore(string path = null)
        {
            _path = path ?? DefaultPath();
            _mutexName = "Global\\FlashBIM.PlumbingSolution.ActivationStore." + HashPath(_path);
        }

        public static string DefaultPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlashBIM");
            return Path.Combine(dir, "activation-" + Verify.LicenseTokenVerifier.ProductCode + ".dat");
        }

        public ActivationRecord Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return null;
                }

                byte[] plain = ProtectedData.Unprotect(
                    File.ReadAllBytes(_path), null, DataProtectionScope.CurrentUser);
                ActivationRecord record = JsonConvert.DeserializeObject<ActivationRecord>(
                    Encoding.UTF8.GetString(plain));

                return record != null && record.IsComplete ? record : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                                      || e is JsonException || e is InvalidOperationException
                                      || e is CryptographicException || e is FormatException)
            {
                // File hỏng, sai tài khoản, hay bị sửa tay: coi như chưa từng kích hoạt.
                // Tuyệt đối không ném lên giao diện — người dùng chỉ cần nhập lại key.
                return null;
            }
        }

        public void Save(ActivationRecord record)
        {
            try
            {
                string json = JsonConvert.SerializeObject(record);
                byte[] cipher = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);

                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                WithMutex(delegate { WriteAtomic(cipher); });
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                                      || e is InvalidOperationException || e is CryptographicException)
            {
                // Không ghi được thì mất tính năng nhớ, nhưng KHÔNG được làm hỏng lần
                // kích hoạt vừa thành công.
            }
        }

        public void Delete()
        {
            try
            {
                bool ran = WithMutex(delegate
                {
                    if (File.Exists(_path))
                    {
                        File.Delete(_path);
                    }
                });

                // Khoá kẹt quá 2 giây: Save bỏ qua được, nhưng Delete thì KHÔNG — sót bản
                // ghi nghĩa là đăng xuất bị "hồi sinh" ở phiên Revit sau.
                if (!ran && File.Exists(_path))
                {
                    File.Delete(_path);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
            }
        }

        /// <summary>
        /// Ghi file tạm cùng thư mục rồi thay thế — tiến trình khác chỉ thấy hoặc bản cũ
        /// trọn vẹn hoặc bản mới trọn vẹn, không bao giờ thấy file ghi dở.
        /// </summary>
        private void WriteAtomic(byte[] cipher)
        {
            string tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(tmp, cipher);
            try
            {
                if (File.Exists(_path))
                {
                    File.Replace(tmp, _path, null);
                }
                else
                {
                    File.Move(tmp, _path);
                }
            }
            catch (IOException)
            {
                // Thua cuộc đua với tiến trình khác: bản ghi của họ cũng hợp lệ và mới
                // ngang mình — bỏ bản của mình đi.
                TryDelete(tmp);
            }
            catch (UnauthorizedAccessException)
            {
                TryDelete(tmp);
            }
        }

        /// <summary>
        /// Chạy hành động trong khoá liên tiến trình. Không lấy được khoá trong 2 giây
        /// thì BỎ QUA và trả false: treo Revit của khách để chờ ghi một file nhớ-key thì
        /// không đáng.
        /// </summary>
        private bool WithMutex(Action action)
        {
            Mutex mutex = null;
            bool owned = false;
            try
            {
                mutex = new Mutex(false, _mutexName);
                try
                {
                    owned = mutex.WaitOne(TimeSpan.FromSeconds(2));
                }
                catch (AbandonedMutexException)
                {
                    owned = true; // tiến trình trước chết giữa chừng — mình vẫn cầm khoá.
                }

                if (owned)
                {
                    action();
                }

                return owned;
            }
            catch (UnauthorizedAccessException)
            {
                // Không tạo/mở được mutex Global (danh sách quyền lạ) — thà ghi không
                // khoá còn hơn mất bản ghi.
                action();
                return true;
            }
            finally
            {
                if (owned && mutex != null)
                {
                    try
                    {
                        mutex.ReleaseMutex();
                    }
                    catch (ApplicationException)
                    {
                    }
                }

                if (mutex != null)
                {
                    mutex.Dispose();
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
            }
        }

        // Tên mutex phải hợp lệ và ổn định theo đường dẫn (không phân biệt hoa thường
        // giống hệ thống tệp).
        private static string HashPath(string path)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}
