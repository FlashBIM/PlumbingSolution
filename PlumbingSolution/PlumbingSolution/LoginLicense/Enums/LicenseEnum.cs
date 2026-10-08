using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.LoginLicense.Enums
{
    public enum LicenseType
    {
        Unknown = -1,
        SingleLicense,
        MultiLicense,
        SingleKey,
        Group,
    }

    public enum LicenseStatus
    {
        Unknown = -1,
        Active,
        Paused,
        Locked,
        Expired,
        Cancel
    }

    public enum CheckLicenseStatus
    {
        Ok,
        Locked,
        Expired,
        DifferentProductType,
        NoneData
    }

    public enum ProductType
    {
        NotApply = -1,
        Standard = 0,
        Professional = 1,
    }
    public enum UserStatus
    {
        Unknown = -1,
        Active,
        Newest,
        Locked,
        EmailPending,
    }
}