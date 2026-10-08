using System;
using System.Collections.Generic;

namespace PlumbingSolution.LoginLicense.Verify
{
    /// <summary>Lý do một token bị từ chối. Chỉ để ghi nhật ký và chẩn đoán.</summary>
    public enum LicenseRejectReason
    {
        None = 0,
        Malformed,
        UnknownKid,
        BadAlg,
        BadSignature,
        Expired,
        NotYetValid,
        MachineMismatch,
        ProductMismatch,
        IssuerMismatch,
    }

    /// <summary>
    /// License đã qua đủ sáu bước kiểm. Chỉ được tạo bởi <see cref="LicenseTokenVerifier"/>
    /// — không có đường nào khác dựng được đối tượng này, nên không có cách nào giả một
    /// license hợp lệ mà không có chữ ký thật.
    /// </summary>
    public sealed class VerifiedLicense
    {
        private const string FeatureAll = "all";

        internal VerifiedLicense(
            string plan, IList<string> features, long licenseExpUnix, long tokenExpUnix, string machineId)
        {
            Plan = plan ?? string.Empty;
            Features = features ?? new List<string>();
            LicenseExpUnix = licenseExpUnix;
            TokenExpUnix = tokenExpUnix;
            MachineId = machineId;
        }

        public string Plan { get; private set; }

        public IList<string> Features { get; private set; }

        /// <summary>Hạn của LICENSE (unix giây). 0 = vĩnh viễn. Chỉ để hiển thị.</summary>
        public long LicenseExpUnix { get; private set; }

        /// <summary>Hạn của TOKEN (unix giây). Đây mới là thứ quyết định tool còn mở hay không.</summary>
        public long TokenExpUnix { get; private set; }

        public string MachineId { get; private set; }

        /// <summary>Server hiện luôn trả "all"; nhánh theo từng tính năng để dành cho sau này.</summary>
        public bool HasAllFeatures
        {
            get
            {
                for (int i = 0; i < Features.Count; i++)
                {
                    if (string.Equals(Features[i], FeatureAll, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    /// <summary>Kết quả kiểm một token: hoặc là license đã xác thực, hoặc là lý do từ chối.</summary>
    public sealed class LicenseVerifyResult
    {
        private LicenseVerifyResult(VerifiedLicense license, LicenseRejectReason reason)
        {
            License = license;
            Reason = reason;
        }

        public VerifiedLicense License { get; private set; }

        public LicenseRejectReason Reason { get; private set; }

        public bool Ok
        {
            get { return License != null && Reason == LicenseRejectReason.None; }
        }

        internal static LicenseVerifyResult Success(VerifiedLicense license)
        {
            return new LicenseVerifyResult(license, LicenseRejectReason.None);
        }

        internal static LicenseVerifyResult Fail(LicenseRejectReason reason)
        {
            return new LicenseVerifyResult(null, reason);
        }
    }
}
