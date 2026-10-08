using Autodesk.Revit.UI;
using PlumbingSolution.LoginLicense.Models.User;
using PlumbingSolution.Ultis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.LoginLicense
{
    public class MyExternalEventEnable : IExternalEventHandler
    {
        public LicenseData m_licenseData;

        public void Execute(UIApplication app)
        {
            RevitUtils.EnableItemRibbon(App._AppCache, m_licenseData);
        }

        public string GetName()
        {
            return "PlumbingSolution";
        }
    }
}