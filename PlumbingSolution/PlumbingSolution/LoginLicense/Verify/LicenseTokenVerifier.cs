using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PlumbingSolution.LoginLicense.Verify
{
    /// <summary>
    /// Kiểm license token JWT RS256 ngay trên máy khách — sáu bước, fail-closed.
    ///
    /// Dùng <see cref="RSACng"/> (có sẵn từ .NET Framework 4.6, Windows-only — Revit
    /// vốn chỉ chạy Windows) thay vì thêm phụ thuộc BouncyCastle: bớt một DLL phải đóng
    /// gói và cài đặt.
    ///
    /// ⚠ KHÔNG đổi sang <c>RSACryptoServiceProvider</c>: nhà cung cấp mặc định của lớp
    /// đó (PROV_RSA_FULL) không hỗ trợ SHA-256, và mọi lần kiểm sẽ thất bại một cách
    /// khó hiểu.
    ///
    /// Khoá nhúng ở dạng modulus/exponent nên không cần phân tích ASN.1 —
    /// <c>ImportSubjectPublicKeyInfo</c> chỉ có trên .NET Core 3.0 trở lên.
    /// </summary>
    public sealed class LicenseTokenVerifier
    {
        /// <summary>Phải khớp đúng chuỗi server ký.</summary>
        public const string ExpectedIssuer = "flashbim-license-server";

        /// <summary>
        /// Mã sản phẩm của PlumbingSolution — PHẢI khớp product code đăng ký trên license server.
        /// Không được dùng chung "ST" với SmartTag, nếu không key SmartTag sẽ mở được
        /// PlumbingSolution và file kích hoạt hai add-in đè nhau.
        /// </summary>
        public const string ProductCode = "PS";

        /// <summary>Biên độ lệch đồng hồ chấp nhận được, hai chiều.</summary>
        private const int ClockSkewSeconds = 120;

        private readonly Func<string, RSAParameters?> _keyResolver;
        private readonly string _expectedProduct;
        private readonly string _expectedIssuer;
        private readonly Func<string> _machineIdProvider;
        private readonly Func<long> _nowUnixProvider;

        public LicenseTokenVerifier(
            Func<string, RSAParameters?> keyResolver,
            string expectedProduct,
            string expectedIssuer,
            Func<string> machineIdProvider,
            Func<long> nowUnixProvider = null)
        {
            if (keyResolver == null) throw new ArgumentNullException("keyResolver");
            if (machineIdProvider == null) throw new ArgumentNullException("machineIdProvider");

            _keyResolver = keyResolver;
            _expectedProduct = expectedProduct;
            _expectedIssuer = expectedIssuer ?? ExpectedIssuer;
            _machineIdProvider = machineIdProvider;
            _nowUnixProvider = nowUnixProvider ?? DefaultNowUnix;
        }

        /// <summary>Cấu hình mặc định cho add-in thật: khoá nhúng, mã máy từ hwid.dll.</summary>
        public static LicenseTokenVerifier CreateDefault(Func<string> machineIdProvider)
        {
            return new LicenseTokenVerifier(
                EmbeddedPublicKeys.ForKid, ProductCode, ExpectedIssuer, machineIdProvider);
        }

        public LicenseVerifyResult Verify(string jwt)
        {
            try
            {
                LicenseVerifyResult result = VerifyInternal(jwt);
                if (!result.Ok)
                {
                    // Chỉ ghi tên lý do — không bao giờ ghi chính token.
                    LicenseLog.Write("verify từ chối: " + result.Reason);
                }

                return result;
            }
            catch (Exception)
            {
                // Bất kỳ lỗi ngoài dự kiến nào cũng coi là token hỏng. Không có nhánh
                // "không chắc thì cho qua".
                LicenseLog.Write("verify từ chối: ngoại lệ (Malformed)");
                return LicenseVerifyResult.Fail(LicenseRejectReason.Malformed);
            }
        }

        private LicenseVerifyResult VerifyInternal(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.Malformed);
            }

            string[] parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.Malformed);
            }

            JObject header = JObject.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));

            // Bước 1: chọn khoá theo kid. kid lạ là từ chối luôn.
            string kid = (string)header["kid"];
            RSAParameters? key = string.IsNullOrEmpty(kid) ? null : _keyResolver(kid);
            if (key == null)
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.UnknownKid);
            }

            // Bước 2: alg PHẢI là RS256, kiểm TRƯỚC khi verify. Bỏ bước này thì một
            // token đặt alg="none" và bỏ trống chữ ký sẽ được coi là hợp lệ.
            string alg = (string)header["alg"];
            if (!string.Equals(alg, "RS256", StringComparison.Ordinal))
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.BadAlg);
            }

            // Bước 3: chữ ký.
            string signingInput = parts[0] + "." + parts[1];
            byte[] signature = Base64UrlDecode(parts[2]);
            if (signature.Length == 0 || !VerifySignature(signingInput, signature, key.Value))
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.BadSignature);
            }

            JObject payload = JObject.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));

            // Bước 4: hạn token, có tính lệch đồng hồ.
            long now = _nowUnixProvider();
            long exp;
            if (!TryGetUnix(payload["exp"], out exp) || now > exp + ClockSkewSeconds)
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.Expired);
            }

            long iat;
            if (TryGetUnix(payload["iat"], out iat) && iat > now + ClockSkewSeconds)
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.NotYetValid);
            }

            // Bước 5a: mã máy phải khớp máy đang chạy — chặn mang token sang máy khác.
            string machineClaim = (string)payload["machine"];
            string localMachine = _machineIdProvider();
            if (string.IsNullOrEmpty(machineClaim)
                || !string.Equals(machineClaim, localMachine, StringComparison.OrdinalIgnoreCase))
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.MachineMismatch);
            }

            // Bước 5b: mã sản phẩm — chặn dùng token của add-in khác.
            string productClaim = (string)payload["product"];
            if (string.IsNullOrEmpty(productClaim)
                || !string.Equals(productClaim, _expectedProduct, StringComparison.OrdinalIgnoreCase))
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.ProductMismatch);
            }

            // Bước 6: người phát hành.
            string iss = (string)payload["iss"];
            if (!string.Equals(iss, _expectedIssuer, StringComparison.Ordinal))
            {
                return LicenseVerifyResult.Fail(LicenseRejectReason.IssuerMismatch);
            }

            long licenseExp;
            TryGetUnix(payload["licenseExp"], out licenseExp);

            var verified = new VerifiedLicense(
                (string)payload["plan"] ?? string.Empty,
                ExtractFeatures(payload["features"]),
                licenseExp,
                exp,
                machineClaim);

            return LicenseVerifyResult.Success(verified);
        }

        private static bool VerifySignature(string signingInput, byte[] signature, RSAParameters key)
        {
            try
            {
                using (var rsa = new RSACng())
                {
                    rsa.ImportParameters(key);
                    byte[] input = Encoding.ASCII.GetBytes(signingInput);
                    return rsa.VerifyData(input, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Server có thể ký "features" là chuỗi đơn hoặc mảng — chấp nhận cả hai.</summary>
        private static IList<string> ExtractFeatures(JToken token)
        {
            var list = new List<string>();
            var array = token as JArray;
            if (array != null)
            {
                foreach (JToken item in array)
                {
                    string s = (string)item;
                    if (!string.IsNullOrEmpty(s))
                    {
                        list.Add(s);
                    }
                }
            }
            else if (token != null && token.Type == JTokenType.String)
            {
                string s = (string)token;
                if (!string.IsNullOrEmpty(s))
                {
                    list.Add(s);
                }
            }

            return list;
        }

        // exp/iat là số theo chuẩn JWT; licenseExp được server ký dưới dạng chuỗi.
        private static bool TryGetUnix(JToken token, out long value)
        {
            value = 0;
            if (token == null)
            {
                return false;
            }

            if (token.Type == JTokenType.Integer)
            {
                value = (long)token;
                return true;
            }

            return long.TryParse((string)token, out value);
        }

        private static long DefaultNowUnix()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private static byte[] Base64UrlDecode(string s)
        {
            string t = s.Replace('-', '+').Replace('_', '/');
            switch (t.Length % 4)
            {
                case 2: t += "=="; break;
                case 3: t += "="; break;
            }

            return Convert.FromBase64String(t);
        }
    }
}
