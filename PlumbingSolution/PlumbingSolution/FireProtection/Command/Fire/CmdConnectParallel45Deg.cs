using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Extensions;
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
    internal class CmdConnectParallel45Deg : IExternalCommand
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
                        ConnectParallelTypeFitting(doc, ref mainMEPCurve, parallelMEPCurve, orgPoint);
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

        public static void ConnectParallelTypeFitting(Document doc, ref MEPCurve mainMEPCurve,
    MEPCurve parallelMEPCurve, XYZ orgPoint, bool isElbow45 = true)
        {
            if (mainMEPCurve == null || parallelMEPCurve == null)
                return;

            TransactionGroup tranGroup = new TransactionGroup(doc, "ConnectParallelTypeFittings");
            Transaction tran = new Transaction(doc, "ConnectParallelTypeFitting");

            try
            {
                List<Element> lstElementCreated = new List<Element>();

                Connector con = CmdConnectParallelSameElevation.GetConnector(parallelMEPCurve);

                if (con == null)
                    return;

                Line lineMain = ((LocationCurve)mainMEPCurve.Location).Curve as Line;

                XYZ pointProjectCon = Common.GetPointProjectOnLine(lineMain, con.Origin);

                if (!Common.IsBetweenLine(lineMain, pointProjectCon))
                    return;

                //double space = Common.To2D(pointProjectCon).DistanceTo(Common.To2D(con.Origin));

                XYZ direction = (Common.To2D(pointProjectCon) - Common.To2D(con.Origin)).Normalize();

                tranGroup.Start();
                tran.Start();
                FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                DisableWarning preproccessor = new DisableWarning();
                options.SetClearAfterRollback(true);
                options.SetFailuresPreprocessor(preproccessor);
                tran.SetFailureHandlingOptions(options);

                ElementId systemTypeId = parallelMEPCurve.MEPSystem.GetTypeId();

                Pipe branchMEPCurve = Pipe.Create(doc, mainMEPCurve.GetTypeId(), mainMEPCurve.ReferenceLevel.Id, con, Common.GetPointOnVector(con.Origin, direction, 8 * con.Radius));

                ConnectorUtils.GetConnectorClosedTo(branchMEPCurve.ConnectorManager, parallelMEPCurve.ConnectorManager, out Connector con1, out Connector con2);

                FamilyInstance elbow = null;
                if (con1 != null && con2 != null)
                    elbow = doc.Create.NewElbowFitting(con1, con2);

                tran.Commit();

                if (!ConnectBranchTypeFitting(doc, ref mainMEPCurve, branchMEPCurve, orgPoint, isElbow45))
                {
                    if (tranGroup.HasStarted())
                        tranGroup.RollBack();
                    return;
                }

                if (elbow != null && elbow.IsValidObject)
                {
                    if (!Common.AllComponentsOnPipeTruss(doc, elbow.Id, true).Select(x => x.Id.ToInt()).Contains(mainMEPCurve.Id.ToInt()))
                    {
                        if (tranGroup.HasStarted())
                            tranGroup.RollBack();
                        return;
                    }
                }

                tranGroup.Assimilate();
            }
            catch (Exception)
            {
                if (tran.HasStarted())
                    tran.RollBack();
                if (tranGroup.HasStarted())
                    tranGroup.RollBack();
            }
        }

        public static bool ConnectBranchTypeFitting(Document doc, ref MEPCurve mainMEPCurve,
  MEPCurve branchMEPCurve, XYZ orgPoint, bool isElbow45 = true)
        {
            if (mainMEPCurve == null || branchMEPCurve == null)
                return false;

            Transaction tran = new Transaction(doc, "ConnectBranchTypeFitting");

            try
            {
                List<Element> lstElementCreated = new List<Element>();

                tran.Start();
                FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                DisableWarning preproccessor = new DisableWarning();
                options.SetClearAfterRollback(true);
                options.SetFailuresPreprocessor(preproccessor);
                tran.SetFailureHandlingOptions(options);

                ElementId systemTypeId = branchMEPCurve.MEPSystem.GetTypeId();
                CmdConnectParallelSameElevation.SplitMEPCurve(doc, ref mainMEPCurve, ref branchMEPCurve, false, true);

                doc.Regenerate();

                Line lineMain = ((LocationCurve)mainMEPCurve.Location).Curve as Line;
                Line lineBranch = ((LocationCurve)branchMEPCurve.Location).Curve as Line;

                List<Connector> connectorDuctBranchs = ConnectorUtils.ToList(branchMEPCurve.ConnectorManager);

                XYZ pointProject = Common.GetPointProjectOnLine(lineMain.Clone() as Line, connectorDuctBranchs[0].Origin);

                Connector conBranch1 = ConnectorUtils.GetConnectorNearest(pointProject, branchMEPCurve.ConnectorManager, out Connector conBranch2);

                XYZ vector = Common.GetVector45Degree(Line.CreateBound(conBranch1.Origin, conBranch2.Origin), pointProject);

                double height = Math.Abs((conBranch1.Origin.Z - pointProject.Z)) * Math.Sqrt(2);

                if (!isElbow45)
                {
                    vector = Common.GetVector90Degree(Line.CreateBound(conBranch1.Origin, conBranch2.Origin), pointProject);
                    height = Math.Abs((conBranch1.Origin.Z - pointProject.Z));
                }

                XYZ endPoint = Common.GetPointOnVector(conBranch1.Origin, vector, height);

                Pipe mEPCurveCheo = Pipe.Create(doc, branchMEPCurve.GetTypeId(), branchMEPCurve.ReferenceLevel.Id, conBranch1, endPoint);
                mEPCurveCheo.SetSystemType(systemTypeId);

                if (mEPCurveCheo != null && mEPCurveCheo.IsValidObject)
                {
                    lstElementCreated.Add(mEPCurveCheo);
                }
                else
                {
                    if (tran.HasStarted())
                        tran.RollBack();
                    return false;
                }

                ConnectorUtils.GetConnectorClosedTo(branchMEPCurve.ConnectorManager, mEPCurveCheo.ConnectorManager, out Connector con1, out Connector con2);

                FamilyInstance elbow1 = doc.Create.NewElbowFitting(con1, con2);

                if (elbow1 != null && elbow1.IsValidObject)
                {
                    lstElementCreated.Add(elbow1);
                }
                else
                {
                    if (tran.HasStarted())
                        tran.RollBack();
                    return false;
                }

                Connector conductCheo1 = ConnectorUtils.GetConnectorNearest(endPoint, mEPCurveCheo.ConnectorManager, out Connector conductCheo2);

                XYZ pConductCheo1 = Common.GetPointProjectOnLine(lineMain, conductCheo1.Origin);

                ElementTransformUtils.MoveElement(doc, mEPCurveCheo.Id, pConductCheo1 - conductCheo1.Origin);

                doc.Regenerate();

                FamilyInstance fitting = null;

                if (CmdConnectParallelSameElevation.IsCreateTee(mainMEPCurve))
                {
                    MEPCurve splitMEPCurve = null;

                    fitting = CmdConnectParallelSameElevation.CreateTee(doc, mainMEPCurve, mEPCurveCheo, out splitMEPCurve);
                    if (fitting == null)
                        fitting = CmdConnectParallelSameElevation.CreateTeeWye(doc, mainMEPCurve, mEPCurveCheo, out splitMEPCurve);

                    mainMEPCurve = (splitMEPCurve == null) ? mainMEPCurve : CmdConnectParallelSameElevation.GetNextMEPCurve(mainMEPCurve, splitMEPCurve, orgPoint);
                }
                else
                {
                    fitting = doc.Create.NewTakeoffFitting(conductCheo1, mainMEPCurve);
                }
                doc.Regenerate();
                if (fitting != null && fitting.IsValidObject)
                {
                    lstElementCreated.Add(fitting);
                }
                else
                {
                    if (tran.HasStarted())
                        tran.RollBack();
                    return false;
                }

                ElementTransformUtils.MoveElement(doc, branchMEPCurve.Id, XYZ.BasisZ * 1 / 304.8);
                doc.Regenerate();
                ElementTransformUtils.MoveElement(doc, branchMEPCurve.Id, XYZ.BasisZ * -1 / 304.8);

                foreach (var ele in lstElementCreated)
                {
                    Connector conNotConected = null;
                    if (ele is FamilyInstance insCreate)
                    {
                        conNotConected = ConnectorUtils.ToList(insCreate.MEPModel.ConnectorManager).FirstOrDefault(x => !x.IsConnected);
                    }
                    else if (ele is Autodesk.Revit.DB.Mechanical.Duct ductCreate)
                    {
                        conNotConected = ConnectorUtils.ToList(ductCreate.ConnectorManager).FirstOrDefault(x => !x.IsConnected);
                    }

                    if (conNotConected != null && conNotConected.IsValidObject)
                    {
                        if (tran.HasStarted())
                            tran.RollBack();

                        return false;
                    }
                }
                tran.Commit();
            }
            catch (Exception)
            {
                if (tran.HasStarted())
                    tran.RollBack();
                return false;
            }
            return true;
        }
    }
}