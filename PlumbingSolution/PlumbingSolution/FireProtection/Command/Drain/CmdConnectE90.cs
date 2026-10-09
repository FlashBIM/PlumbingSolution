using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.FireProtection.Command.Drain
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdConnectE90 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            ConnectElbowType(Global.UIDoc);

            return Result.Succeeded;
        }

        public static void ConnectElbowType(UIDocument uidoc)
        {
            if (uidoc == null || uidoc.Document == null)
                return;

            Document doc = uidoc.Document;

            try
            {
                while (true)
                {
                    MEPCurve mainMEPCurve = doc.GetElement(uidoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve main :")) as MEPCurve;
                    if (mainMEPCurve == null)
                        return;

                    MEPCurve branchMEPCurve = doc.GetElement(uidoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve branch :")) as MEPCurve;
                    if (branchMEPCurve == null)
                        return;

                    try
                    {
                        CmdConnectE45.ConnectElbowTypeFitting(doc, mainMEPCurve, branchMEPCurve, false);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}