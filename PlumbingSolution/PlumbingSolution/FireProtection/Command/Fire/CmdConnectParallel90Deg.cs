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

namespace PlumbingSolution.FireProtection.Command.Fire
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdConnectParallel90Deg : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            var doc = Global.UIDoc.Document;

            try
            {
                MEPCurve mainMEPCurve = doc.GetElement(Global.UIDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve main :")) as MEPCurve;
                if (mainMEPCurve == null)
                    return Result.Cancelled;

                List<MEPCurve> lsMEPCurveBranchs = Global.UIDoc.Selection.PickObjects(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve branch :")
                                                                        .Select(x => doc.GetElement(x) as MEPCurve).Where(x => x.Id != mainMEPCurve.Id).ToList();
                if (lsMEPCurveBranchs == null || lsMEPCurveBranchs.Count == 0)
                    return Result.Cancelled;

                TransactionGroup tranGroup = new TransactionGroup(doc, "ConnectParallelType");

                try
                {
                    CmdConnectParallelSameElevation.OrderBranchMEPCurve(mainMEPCurve, ref lsMEPCurveBranchs, out XYZ orgPoint);

                    tranGroup.Start();

                    foreach (var parallelMEPCurve in lsMEPCurveBranchs)
                    {
                        CmdConnectParallel45Deg.ConnectParallelTypeFitting(doc, ref mainMEPCurve, parallelMEPCurve, orgPoint, false);
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
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }
    }
}