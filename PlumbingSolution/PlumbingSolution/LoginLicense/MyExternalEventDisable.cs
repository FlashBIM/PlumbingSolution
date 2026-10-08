using Autodesk.Revit.UI;
using PlumbingSolution.Ultis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.LoginLicense
{
    public class MyExternalEventDisable : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            RevitUtils.DisableItemRibbonLocked(App._AppCache);
        }

        public string GetName()
        {
            return "PlumbingSolution";
        }
    }
}