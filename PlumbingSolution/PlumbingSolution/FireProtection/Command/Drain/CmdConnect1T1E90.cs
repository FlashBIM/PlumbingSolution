using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlumbingSolution.FireProtection.Command.Drain
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdConnect1T1E90 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            ConnectBranchType(Global.UIDoc);

            return Result.Succeeded;
        }

        public static void ConnectBranchType(UIDocument uidoc)
        {
            if (uidoc == null || uidoc.Document == null)
                return;

            Document doc = uidoc.Document;

            try
            {
                MEPCurve mainMEPCurve = doc.GetElement(uidoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve main :")) as MEPCurve;
                if (mainMEPCurve == null)
                    return;

                List<MEPCurve> lsMEPCurveBranchs = uidoc.Selection.PickObjects(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve branch :")
                                                                        .Select(x => doc.GetElement(x) as MEPCurve).ToList();
                if (lsMEPCurveBranchs == null || lsMEPCurveBranchs.Count == 0)
                    return;
                TransactionGroup tranGroup = new TransactionGroup(doc, "ConnectBranchType");
                try
                {
                    CmdConnect1T1E45.OrderBranchMEPCurve(mainMEPCurve, ref lsMEPCurveBranchs, out XYZ orgPoint);

                    tranGroup.Start();
                    foreach (var branchMEPCurve in lsMEPCurveBranchs)
                    {
                        if (!CmdConnect1T1E45.IsInSide(mainMEPCurve, branchMEPCurve))
                            continue;

                        CmdConnect1T1E45.ConnectBranchTypeFitting(doc, ref mainMEPCurve, branchMEPCurve, orgPoint, false);
                    }
                    tranGroup.Assimilate();
                }
                catch (Exception ex)
                {
                    if (tranGroup.HasStarted())
                        tranGroup.RollBack();
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}