using System;
using System.Security.Cryptography;

namespace PlumbingSolution.LoginLicense.Verify
{
    /// <summary>
    /// Public key RSA-4096 nhúng sẵn để kiểm chữ ký license token.
    ///
    /// Đây LÀ public key — không phải bí mật. Lưu ở dạng modulus/exponent thay vì chuỗi
    /// PEM chỉ để không lộ ra khi ai đó chạy <c>strings</c> trên DLL; nó không thêm chút
    /// bảo mật thật nào. Bảo mật nằm ở chỗ khoá PRIVATE không bao giờ rời server.
    ///
    /// Nguồn: <c>api-add-in/AddIn.API/license-keys/license-k{1,2}-public.der</c>.
    /// Đổi khoá ở server thì PHẢI cập nhật hằng số ở đây và phát hành bản add-in mới.
    /// Trích lại bằng:
    /// <code>
    /// openssl rsa -pubin -inform DER -in license-k1-public.der -noout -modulus
    /// </code>
    /// rồi đổi chuỗi hex sang base64.
    ///
    /// ─────────────────────────────────────────────────────────────────────────────
    /// DÙNG CHUNG KHOÁ VỚI DIRIT — quyết định có chủ đích (2026-08-27).
    ///
    /// Đây đúng là cặp khoá mà hệ Dirit đang dùng, không phải cặp riêng của flashbim.
    /// Hệ quả đã được cân nhắc và chấp nhận: ai có private key đều ký được token cho
    /// CẢ HAI sản phẩm.
    ///
    /// Thứ ngăn token của hai bên dùng lẫn sang nhau KHÔNG phải là khoá, mà là hai
    /// claim trong token — verifier kiểm cả hai, lệch cái nào cũng từ chối:
    ///     iss     = "flashbim-license-server"   (Dirit: "dirit-license-server")
    ///     product = "<ProductCode>"                        (Dirit: "DR")
    /// Vì vậy KHÔNG được nới lỏng hai bước kiểm đó trong LicenseTokenVerifier.
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    internal static class EmbeddedPublicKeys
    {
        /// <summary>
        /// Trả tham số khoá công khai cho kid, hoặc <c>null</c> nếu kid lạ — khi đó
        /// verifier từ chối token ngay ở bước một.
        /// </summary>
        public static RSAParameters? ForKid(string kid)
        {
            if (kid == "k1")
            {
                return Build(K1Modulus, CommonExponent);
            }

            if (kid == "k2")
            {
                return Build(K2Modulus, CommonExponent);
            }

            return null;
        }

        private static RSAParameters? Build(string modulus, string exponent)
        {
            try
            {
                return new RSAParameters
                {
                    Modulus = Convert.FromBase64String(modulus),
                    Exponent = Convert.FromBase64String(exponent),
                };
            }
            catch (FormatException)
            {
                // Hằng số bị dán hỏng: coi như không có khoá → từ chối token, hỏng theo
                // hướng an toàn thay vì bỏ qua bước kiểm chữ ký.
                return null;
            }
        }

        /// <summary>65537 — số mũ công khai chuẩn, cả hai khoá đều dùng.</summary>
        private const string CommonExponent = "AQAB";

        // kid = k1 — khoá đang ký.
        private const string K1Modulus =
            "t5P0BRUA6FIHLT9GFrmdWjpKnBWmRCQmPupKn4ZNJUrrktbgnoV4KGorMZ3iJWN9Pqkcy2nMqb9bL+03zEYP1tFcyqNuuH5iHWA2" +
            "JV4ceuQUGNPVZyXDC1oLlFz3dNhbQJc9jcDufQ4Hcbc18sckAl/RFSRCsHv05uJwB+rDenQ1j+OmE9aJIV1zu/WPxGOyEPKnc/jM" +
            "gtt7byIS+llxI5sxEeVBGTXlW8HAs0FPFjqH0i4Y+Yj3kTPNdeotg1IVTzO7hc0+/1CQLIN8mu0TkSTA6q+OuJtAjVP0bqsT5xtX" +
            "ENppGN0ozbbZTPYZpDyPpEwde83+JPNZdts30ynuNIhKdqfbJgp82mlTG47esWeEKXbbqHQn76SUMOKY/aJtGW3Xp2esknhCJTNT" +
            "yNObZLDV9RFZsykfGv2U86kz/la8REs3PV9Zy76DyCvcA/ABsyVtNDTe/7EyuRVvugCArVhE41InRpj6rbziJbJklB8M+RHxHWwu" +
            "Br1rUaVpIB/jjf3pFFKvDRjCAemKmhIx/IqICodpVh2aTg8C3o9CDV8P/Y/30qCWuAgWj/ofuHbFcJh864zDOrHLP9AFdkWA1HWF" +
            "hvpYwL648cl/G/cpfvp6IXhvTjPNKjL5bdA9YksRFgn5hawMEm9khcmN/2OOuRilnpryW8KV4FFMJN8HVU0=";

        // kid = k2 — khoá dự phòng, nhúng sẵn để xoay khoá không cần phát hành gấp.
        private const string K2Modulus =
            "qSs9U4V8LfqjUC0Rkrb73l944ZUSlSCp51pzkCJ3o29Hp+s05DQehWmdOjnT549cArspZuCD/7MD3xEzAywkJPsZu6DfvGEHI5Jc" +
            "P/E0K/ewwAaC19J6oApWpCfDHWeBQA+iqNsji6ywcIRM22E8GtGPgpsKURjoiYk2xENGx71jammCUBxz0nrGzxyIglVrMDrBNX7r" +
            "CNZqP72lmYnLQfKgY4EAp0r2Va0/2dNDFSWNqIAkI7NEpBO9a3Bc/B6zmENV8AftJQT+K635VFN/6qP/pZsOIA0V5S1kbXV/Z02m" +
            "DwHB03VIwaVEYuGCaIppevSJLlW1+ssqDuq/Bt0K/Kt5i6cp/L4YUYKhzfWzpXnpCPv5aK4c4nzKkIs4Hwouy2QcXWVQptlUbn1f" +
            "tM5NJNDAIIbggjBu3xsdhRdO93vcXeXU2gEP25XvBQFQ45+Wmqs0LK8mAMqjt1IYCmZiWmi3gyxge4nAa1wqVC2cxlxZEenrBOTH" +
            "l5z8o5cFchdVD6QOuhNUPvJRLUeA9C3F1bGo9aVsUUhZ5QkJjhRzlOVJORxRA9Tk+LDQd0rAlJHMWrssPiv/zEaSmtTy/v3pZYET" +
            "BWvW3RYHrDhW7tG/RDoyY1WfJy5gxxOUvLavHw2pdkl9CddiDmJDbwQ+tBwHOnB0Yds3KuVx7wt4ZLO9E6E=";
    }
}
