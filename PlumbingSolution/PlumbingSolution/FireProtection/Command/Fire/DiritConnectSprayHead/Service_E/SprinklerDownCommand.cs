using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlumbingSolution.FireProtection.Command.Fire.DiritConnectSprayHead.Service_E
{
    [Transaction(TransactionMode.Manual)]
    public class SprinklerDownCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            //Show form
            if (App.ShowSprinklerDownForm() == false)
                return Result.Cancelled;

            return Result.Succeeded;
        }
    }
}
