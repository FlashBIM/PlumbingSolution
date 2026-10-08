using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.Ultis
{
    internal class Define
    {
        #region ClassNames

        //Cmd License Login
        public const string CmdLoginClassName = "PlumbingSolution.Commands.Login.CmdLogin";

        public const string CmdAboutUsClassName = "PlumbingSolution.Commands.Login.CmdAboutUs";

        #endregion ClassNames

        #region License Message

        public const string LoginLicenseTabName = "My License";
        public const string RibbonTabName = "Plumbing Solution";
        public const string UnitMM = "Millimeter";
        public const string UnitInch = "FeetInch";
        public const string UnknowLicense = "Error of unknown cause";
        public const string NotExistKeyLicense = "Key does not exist";
        public const string ExpiredKeyLicense = "Expired key";
        public const string HardwareDiffLicense = "The trigger device and the requested device do not overlap";
        public const string RejectLicense = "License denied";

#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
        public const string LicenseStatus = "Trạng thái License";

        public const string LicenseAccountNotActive = "Tài khoản của bạn chưa được kích hoạt. Vui lòng đăng ký dùng thử để kích hoạt tài khoản";
        public const string LicenseAccountExpired = "Thời hạn giấy phép của bạn đã hết hạn. Vui lòng truy cập trang web của chúng tôi: https://flashbim.vn để mua sản phẩm";
        public const string LicenseAccountLocked = "Tài khoản của bạn đã bị khóa vì một số lý do. Vui lòng liên hệ chúng tôi qua email info@flashbim.vn hoặc gửi ticket trên trang WEB";
        public const string LicenseAccountDeleted = "License của bạn đã bị xóa vì số lý do. Vui lòng liên hệ chúng tôi qua email info@flashbim.vn hoặc gửi ticket trên trang WEB";

        public const string LicenseStatusLogOut = "Đăng xuất";
        public const string LicenseStatusLogIn = "Đăng nhập";

        public const string LicenseAccountLogOut = "Bạn đã đăng xuất thành công";
        public const string LiceseAccountLogInFalse = "Đăng nhập không thành công, vui lòng thử lại!";
        public const string LicenseAccountLogInSuccess = "Bạn đã đăng nhập thành công. Bấm OK để sử dụng addin của chúng tôi";
        public const string LicenseAccountLogInErr = "Lỗi đăng nhập! Vui lòng thử lại.";
#else
        public const string LicenseStatus = "License Status";

        public const string LicenseAccountNotActive = "Your account has not been activated. Please request for a free trial to activate your account";
        public const string LicenseAccountExpired = "Your license period expired. Please visit our website : https://flashbim.com to buy product";
        public const string LicenseAccountLocked = "Your licence has been locked by Admin. Please contact us via email info@flashbim.com for our support";
        public const string LicenseAccountDeleted = "Your license has been deleted by Admin. Please contact us via email info@flashbim.com or request a ticket on our website";

        public const string LicenseStatusLogOut = "Logout";
        public const string LicenseStatusLogIn = "Login";

        public const string LicenseAccountLogOut = "You have successfully logged out";
        public const string LiceseAccountLogInFalse = "Login failed, please try again!";
        public const string LicenseAccountLogInSuccess = "You logged in successfully. Press OK to use our addin";
        public const string LicenseAccountLogInErr = "Login Error! Please try again.";
#endif

        public const string ProductName = LoginLicense.Verify.LicenseTokenVerifier.ProductCode;

        #endregion License Message
    }
}