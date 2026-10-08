using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlumbingSolution.LoginLicense.Constants;
using PlumbingSolution.LoginLicense.Models;
using Newtonsoft.Json;

namespace PlumbingSolution.LoginLicense.Client
{
    /// <summary>Kết quả một lần gọi server: hoặc thành công, hoặc một <see cref="ActivationError"/>.</summary>
    public struct ActivationOutcome
    {
        private ActivationOutcome(ActivationResult value, ActivationError? error)
        {
            Value = value;
            Error = error;
        }

        public ActivationResult Value { get; private set; }

        public ActivationError? Error { get; private set; }

        public bool IsSuccess
        {
            get { return Error == null && Value != null; }
        }

        public static ActivationOutcome Ok(ActivationResult value)
        {
            return new ActivationOutcome(value, null);
        }

        public static ActivationOutcome Fail(ActivationError error)
        {
            return new ActivationOutcome(null, error);
        }
    }

    public interface ILicenseApiClient
    {
        Task<ActivationOutcome> ActivateAsync(ActivateRequest request, CancellationToken ct);

        Task<ActivationOutcome> RefreshAsync(RefreshRequest request, CancellationToken ct);
    }

    /// <summary>
    /// Gọi hai endpoint license. Chỉ thử lại lỗi hạ tầng, đọc phản hồi có giới hạn, và
    /// từ chối token có hạn dài bất thường.
    /// </summary>
    public sealed class LicenseApiClient : ILicenseApiClient, IDisposable
    {
        /// <summary>
        /// Token sống 15 phút. Phản hồi hứa hạn dài hơn nhiều là dấu hiệu server bị giả
        /// mạo hoặc hỏng — từ chối thay vì tin, vì một hạn xa vô tận khiến vòng làm mới
        /// không bao giờ phát hiện license đã bị thu hồi.
        /// </summary>
        private static readonly TimeSpan MaxTokenLifetime = TimeSpan.FromMinutes(35);

        private const int MaxResponseBytes = 64 * 1024;

        /// <summary>Số lần thử thêm cho lỗi hạ tầng.</summary>
        private static readonly TimeSpan[] DefaultRetryBackoff =
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8),
        };

        private readonly HttpClient _http;
        private readonly bool _ownsHttp;
        private readonly Func<DateTimeOffset> _nowUtc;
        private readonly TimeSpan[] _backoff;

        static LicenseApiClient()
        {
            // net48 tuỳ cấu hình Windows có thể chưa bật TLS 1.2, và server HTTPS hiện
            // đại từ chối bắt tay cũ. Bật một lần cho cả tiến trình.
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch (NotSupportedException)
            {
            }
        }

        /// <param name="retryBackoff">
        /// Ghi đè lịch thử lại. Kiểm thử truyền mảng rỗng để bỏ thử lại — nếu không, mỗi
        /// bài kiểm chạm nhánh lỗi hạ tầng sẽ ngủ thật 14 giây.
        /// </param>
        public LicenseApiClient(
            HttpClient http = null, Func<DateTimeOffset> nowUtc = null, TimeSpan[] retryBackoff = null)
        {
            if (http == null)
            {
                _http = new HttpClient();
                _http.BaseAddress = new Uri(API.BaseUrl.EndsWith("/") ? API.BaseUrl : API.BaseUrl + "/");
                // 30 giây là trần cho mỗi yêu cầu: vòng nhịp chạy tuần tự, một yêu cầu
                // treo tới 100 giây mặc định là cả nhịp đứng.
                _http.Timeout = TimeSpan.FromSeconds(30);
                _ownsHttp = true;
            }
            else
            {
                _http = http;
            }

            _nowUtc = nowUtc ?? DefaultNowUtc;
            _backoff = retryBackoff ?? DefaultRetryBackoff;
        }

        public Task<ActivationOutcome> ActivateAsync(ActivateRequest request, CancellationToken ct)
        {
            string json = JsonConvert.SerializeObject(request);
            return WithRetryAsync(json, API.ActivateApi, ct);
        }

        public Task<ActivationOutcome> RefreshAsync(RefreshRequest request, CancellationToken ct)
        {
            string json = JsonConvert.SerializeObject(request);
            return WithRetryAsync(json, API.RefreshApi, ct);
        }

        /// <summary>
        /// Chỉ thử lại lỗi hạ tầng. Lỗi nghiệp vụ (key sai, hết suất máy) thử lại bao
        /// nhiêu lần cũng ra kết quả cũ, mà còn đốt thêm hạn mức tần suất của chính
        /// license đó.
        /// </summary>
        private async Task<ActivationOutcome> WithRetryAsync(string json, string path, CancellationToken ct)
        {
            ActivationOutcome outcome = await PostAsync(path, json, ct).ConfigureAwait(false);

            for (int attempt = 0; attempt < _backoff.Length; attempt++)
            {
                if (outcome.IsSuccess || outcome.Error == null || !outcome.Error.Value.IsTransient())
                {
                    return outcome;
                }

                await Task.Delay(_backoff[attempt], ct).ConfigureAwait(false);
                outcome = await PostAsync(path, json, ct).ConfigureAwait(false);
            }

            return outcome;
        }

        private async Task<ActivationOutcome> PostAsync(string path, string json, CancellationToken ct)
        {
            HttpResponseMessage response;
            try
            {
                // Nội dung phải tạo mới mỗi lần: HttpContent đã gửi không dùng lại được.
                using (var message = new HttpRequestMessage(HttpMethod.Post, path))
                {
                    message.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    if (!string.IsNullOrEmpty(API.CheckKeyValue))
                    {
                        message.Headers.Add(API.CheckKeyHeader, API.CheckKeyValue);
                    }

                    response = await _http.SendAsync(message, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return ActivationOutcome.Fail(ActivationError.Network); // hết thời gian chờ
            }
            catch (HttpRequestException)
            {
                return ActivationOutcome.Fail(ActivationError.Network);
            }

            using (response)
            {
                if ((int)response.StatusCode == 429)
                {
                    return ActivationOutcome.Fail(ActivationError.RateLimited);
                }

                if ((int)response.StatusCode >= 500)
                {
                    return ActivationOutcome.Fail(ActivationError.ServerError);
                }

                string body;
                try
                {
                    body = await ReadBoundedAsync(response, ct).ConfigureAwait(false);
                }
                catch (Exception e) when (e is IOException || e is InvalidOperationException
                                          || e is HttpRequestException || e is ObjectDisposedException)
                {
                    return ActivationOutcome.Fail(ActivationError.Network);
                }

                return response.IsSuccessStatusCode ? ParseSuccess(body) : ParseFailure(body);
            }
        }

        /// <summary>Đọc có giới hạn: một phản hồi khổng lồ không được phép làm cạn bộ nhớ Revit.</summary>
        private static async Task<string> ReadBoundedAsync(HttpResponseMessage response, CancellationToken ct)
        {
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
            {
                throw new InvalidOperationException("phản hồi quá lớn");
            }

            using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                var buffer = new byte[MaxResponseBytes + 1];
                int total = 0;
                while (total < buffer.Length)
                {
                    int read = await stream.ReadAsync(buffer, total, buffer.Length - total, ct)
                        .ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }

                    total += read;
                }

                if (total > MaxResponseBytes)
                {
                    throw new InvalidOperationException("phản hồi quá lớn");
                }

                return Encoding.UTF8.GetString(buffer, 0, total);
            }
        }

        private ActivationOutcome ParseSuccess(string body)
        {
            ApiResponse<ActivationResult> envelope;
            try
            {
                envelope = JsonConvert.DeserializeObject<ApiResponse<ActivationResult>>(body);
            }
            catch (JsonException)
            {
                return ActivationOutcome.Fail(ActivationError.MalformedResponse);
            }

            if (envelope == null || !envelope.Success || envelope.Result == null)
            {
                return ActivationOutcome.Fail(ActivationError.MalformedResponse);
            }

            ActivationResult result = envelope.Result;
            if (string.IsNullOrEmpty(result.Token) || string.IsNullOrEmpty(result.ActivationId))
            {
                return ActivationOutcome.Fail(ActivationError.MalformedResponse);
            }

            DateTimeOffset now = _nowUtc();
            if (result.ExpiresAt <= now || result.ExpiresAt - now > MaxTokenLifetime)
            {
                return ActivationOutcome.Fail(ActivationError.MalformedResponse);
            }

            return ActivationOutcome.Ok(result);
        }

        /// <summary>
        /// Lỗi 4xx mang mã máy đọc được ở <c>errorCode</c> trong cùng envelope
        /// <see cref="ApiResponse{T}"/>.
        ///
        /// ⚠ KHÔNG phải RFC7807 <c>{"detail":"CODE"}</c> như hệ Dirit — đọc nhầm khoá
        /// khiến mọi lỗi nghiệp vụ rơi về UnknownLogicError, và license bị thu hồi thật
        /// sẽ không bao giờ khoá được tool.
        /// </summary>
        private static ActivationOutcome ParseFailure(string body)
        {
            try
            {
                var envelope = JsonConvert.DeserializeObject<ApiResponse<object>>(body);
                return ActivationOutcome.Fail(
                    ActivationErrorExtensions.FromServerCode(envelope == null ? null : envelope.ErrorCode));
            }
            catch (JsonException)
            {
                // 4xx không phân tích được (thường là trang chặn của proxy công ty) vẫn
                // là lỗi nghiệp vụ, nhưng KHÔNG đủ căn cứ để huỷ license đang chạy.
                return ActivationOutcome.Fail(ActivationError.UnknownLogicError);
            }
        }

        private static DateTimeOffset DefaultNowUtc()
        {
            return DateTimeOffset.UtcNow;
        }

        public void Dispose()
        {
            if (_ownsHttp && _http != null)
            {
                _http.Dispose();
            }
        }
    }
}
