using Autodesk.Revit.UI;
using PlumbingSolution.Ultis;
using System.Windows.Forms;

namespace PlumbingSolution.LoginLicense
{
    public class MyExternalEvent : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            RevitUtils.DisableItemRibbon(App._AppCache);
        }

        public string GetName()
        {
            return "PlumbingSolution";
        }
    }
}