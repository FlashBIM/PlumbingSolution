using Autodesk.Revit.UI;
using PlumbingSolution.LoginLicense.Enums;
using PlumbingSolution.LoginLicense.Models.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.Ultis
{
    internal class RevitUtils
    {
        public static void DisableItemRibbonLocked(UIControlledApplication app)
        {
            var ribbonPanels = app.GetRibbonPanels(Define.RibbonTabName);
            foreach (var item in ribbonPanels)
            {
                foreach (var ribbonItem in item.GetItems())
                {
                    if (ribbonItem.Name != "btnLogin")
                        ribbonItem.Enabled = false;
                }
            }
        }
        public static void DisableItemRibbon(UIControlledApplication app)
        {
            App.SetImgRibbonButton(Define.RibbonTabName, Define.LoginLicenseTabName, "My License", "Login", "Signin.psd-01.png");
            var ribbonPanels = app.GetRibbonPanels(Define.RibbonTabName);
            foreach (var item in ribbonPanels)
            {
                foreach (var ribbonItem in item.GetItems())
                {
                    if (ribbonItem.Name == "btnLogin")
                        ribbonItem.ItemText = "Login";
                    else
                        ribbonItem.Enabled = false;
                }
            }
        }

        public static void EnableItemRibbon(UIControlledApplication app, LicenseData licenseData)
        {
            App.SetImgRibbonButton(Define.RibbonTabName, Define.LoginLicenseTabName, "Login", "My License", "Lincense.psd-01.png");
            var ribbonPanels = app.GetRibbonPanels(Define.RibbonTabName);

            foreach (var item in ribbonPanels)
            {
                foreach (var ribbonItem in item.GetItems())
                {
                    if (ribbonItem.Name == "btnLogin")
                        ribbonItem.ItemText = "My License";
                    else
                    {
                        if (licenseData.Status == LicenseStatus.Active)
                            ribbonItem.Enabled = true;
                    }
                }
            }
        }
    }
}