using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
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
    public class CmdConnectE45 : IExternalCommand
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
                        ConnectElbowTypeFitting(doc, mainMEPCurve, branchMEPCurve, true);
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

        public static void ConnectElbowTypeFitting(Document doc, MEPCurve mainMEPCurve,
      MEPCurve branchMEPCurve, bool isElbow45 = true, List<ElementId> toDelete = null, double offset = 0)
        {
            Transaction tran = new Transaction(doc, "ConnectElbowTypeFitting45DegRotation");

            try
            {
                List<Element> lstElementCreated = new List<Element>();

                tran.Start();
                FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                DisableWarning preproccessor = new DisableWarning();
                options.SetClearAfterRollback(true);
                options.SetFailuresPreprocessor(preproccessor);
                tran.SetFailureHandlingOptions(options);

                if (toDelete?.Count > 0)
                {
                    doc.Delete(toDelete);
                    // Lấy Middle Elevation của Pipe1 (đơn vị internal - feet)
                    var paraEle1 = mainMEPCurve
                        .get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                    if (paraEle1 != null)
                    {
                        var pipe1Elevation = paraEle1.AsDouble();
                        // Z mới = Elevation Pipe1 + offset
                        double newElevation = pipe1Elevation + offset;

                        // Set trực tiếp vào Pipe2
                        var paraEle2 = branchMEPCurve.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                        if (paraEle2 != null)
                        {
                            paraEle2.Set(newElevation);
                        }
                    }
                }

                doc.Regenerate();

                ElementId systemTypeId = branchMEPCurve.MEPSystem.GetTypeId();
                SplitMEPCurve(doc, ref mainMEPCurve, ref branchMEPCurve, true, true);

                doc.Regenerate();

                ExtendMEPCurve(doc, mainMEPCurve, branchMEPCurve, true, false);

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
                    return;
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
                    return;
                }

                Connector conductCheo1 = ConnectorUtils.GetConnectorNearest(endPoint, mEPCurveCheo.ConnectorManager, out Connector conductCheo2);

                XYZ pConductCheo1 = Common.GetPointProjectOnLine(lineMain, conductCheo1.Origin);

                ElementTransformUtils.MoveElement(doc, mEPCurveCheo.Id, pConductCheo1 - conductCheo1.Origin);

                doc.Regenerate();

                ConnectorUtils.GetConnectorClosedTo(mainMEPCurve.ConnectorManager, mEPCurveCheo.ConnectorManager, out con1, out con2);

                FamilyInstance elbow2 = doc.Create.NewElbowFitting(con1, con2);

                if (elbow2 != null && elbow2.IsValidObject)
                {
                    lstElementCreated.Add(elbow2);
                }
                else
                {
                    if (tran.HasStarted())
                        tran.RollBack();
                    return;
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

                        return;
                    }
                }
                tran.Commit();
            }
            catch (Exception)
            {
                if (tran.HasStarted())
                    tran.RollBack();
            }
        }

        public static void ExtendMEPCurve(Document doc, MEPCurve mEPCurve1, MEPCurve mEPCurve2, bool isExteddDuct1, bool isExteddDuct2)
        {
            try
            {
                if (doc != null && mEPCurve1 != null && mEPCurve1.Location is LocationCurve lc1
              && mEPCurve2 != null && mEPCurve2.Location is LocationCurve lc2)
                {
                    Line line1 = Common.To2D(lc1.Curve as Line);
                    Line line2 = Common.To2D(lc2.Curve as Line);

                    if (line1 == null || line2 == null)
                        return;

                    XYZ point = Common.Intersection(line1, line2);
                    if (point != null)
                    {
                        Line line3 = Line.CreateUnbound(point, XYZ.BasisZ);

                        XYZ point1 = Common.LineIntersection(lc1.Curve as Line, line3, true);

                        XYZ point2 = Common.LineIntersection(lc2.Curve as Line, line3, true);

                        if (isExteddDuct1 && point1 != null)
                        {
                            List<Connector> connectors = ConnectorUtils.ToList(mEPCurve1.ConnectorManager);

                            Connector con1 = ConnectorUtils.GetConnectorNearest(point1, mEPCurve1.ConnectorManager, out Connector con2);

                            FamilyInstance fitting = ConnectorUtils.GetElementConnectedWithConnector(con2) as FamilyInstance;

                            if (fitting != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo(fitting.MEPModel.ConnectorManager, mEPCurve1.ConnectorManager, out Connector con11, out Connector con22);
                                if (con11 != null && con22 != null && con11.IsConnectedTo(con22))
                                    con11.DisconnectFrom(con22);
                            }

                            ((LocationCurve)mEPCurve1.Location).Curve = Line.CreateBound(con2.Origin, point1);

                            if (fitting != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo(fitting.MEPModel.ConnectorManager, mEPCurve1.ConnectorManager, out Connector con11, out Connector con22);
                                if (con11 != null && con22 != null && !con11.IsConnectedTo(con22))
                                    con11.ConnectTo(con22);
                            }
                        }

                        if (isExteddDuct2 && point2 != null)
                        {
                            List<Connector> connectors = ConnectorUtils.ToList(mEPCurve2.ConnectorManager);

                            Connector con1 = ConnectorUtils.GetConnectorNearest(point2, mEPCurve2.ConnectorManager, out Connector con2);

                            FamilyInstance fitting = ConnectorUtils.GetElementConnectedWithConnector(con2) as FamilyInstance;

                            if (fitting != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo(fitting.MEPModel.ConnectorManager, mEPCurve2.ConnectorManager, out Connector con11, out Connector con22);
                                if (con11 != null && con22 != null && con11.IsConnectedTo(con22))
                                    con11.DisconnectFrom(con22);
                            }

                            ((LocationCurve)mEPCurve2.Location).Curve = Line.CreateBound(con2.Origin, point2);

                            if (fitting != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo(fitting.MEPModel.ConnectorManager, mEPCurve2.ConnectorManager, out Connector con11, out Connector con22);
                                if (con11 != null && con22 != null && !con11.IsConnectedTo(con22))
                                    con11.ConnectTo(con22);
                            }
                        }

                        doc.Regenerate();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static void SplitMEPCurve(Document doc, ref MEPCurve mEPCurve1, ref MEPCurve mEPCurve2, bool isSplitDuct1, bool isSplitDuct2, bool isCutDuct2 = true)
        {
            if (doc != null && mEPCurve1 != null && mEPCurve1.Location is LocationCurve lc1
                && mEPCurve2 != null && mEPCurve2.Location is LocationCurve lc2)
            {
                Line line1 = Common.To2D(lc1.Curve as Line);
                Line line2 = Common.To2D(lc2.Curve as Line);

                if (line1 == null || line2 == null)
                    return;

                XYZ point = Common.Intersection(line1, line2);
                if (point != null)
                {
                    Line line3 = Line.CreateUnbound(point, XYZ.BasisZ);

                    XYZ point1 = Common.LineIntersection(lc1.Curve as Line, line3, true);

                    XYZ point2 = Common.LineIntersection(lc2.Curve as Line, line3, true);

                    if (isSplitDuct1)
                    {
                        SplitMEPCurve(doc, ref mEPCurve1, point1, point);
                        doc.Regenerate();
                    }

                    if (isSplitDuct2)
                    {
                        SplitMEPCurve(doc, ref mEPCurve2, point2, point);

                        doc.Regenerate();

                        if (isCutDuct2)
                        {
                            Connector con1 = ConnectorUtils.GetConnectorNearest(point2, mEPCurve2.ConnectorManager, out Connector con2);

                            if (con1 != null && con1.IsValidObject)
                            {
                                double distance = 2 * point1.DistanceTo(point2);

                                XYZ pointOffset = Common.GetPointOnVector(point2, con1.CoordinateSystem.BasisZ.Negate(), distance);

                                SplitMEPCurve(doc, ref mEPCurve2, pointOffset, point);
                            }
                        }
                    }
                }
            }
        }

        public static bool SplitMEPCurve(Document doc, ref MEPCurve mEPCurve, XYZ pointSplit, XYZ pointCheckDistance)
        {
            if (doc != null && mEPCurve != null && mEPCurve.Location is LocationCurve lc && pointSplit != null && pointCheckDistance != null)
            {
                Line lineMEPCurve = lc.Curve as Line;

                if (Common.IsBetweenLine(lineMEPCurve, pointSplit))
                {
                    XYZ centerMEP1 = (lineMEPCurve.GetEndPoint(0) + pointSplit) / 2;

                    XYZ centerMEP2 = (lineMEPCurve.GetEndPoint(1) + pointSplit) / 2;

                    double lenght = pointCheckDistance.DistanceTo(centerMEP1);

                    double lenghtNew = pointCheckDistance.DistanceTo(centerMEP2);

                    if (lenght > lenghtNew && lineMEPCurve.GetEndPoint(0).DistanceTo(pointSplit) < 2 * mEPCurve.Diameter)
                        return false;

                    if (lenghtNew > lenght && lineMEPCurve.GetEndPoint(1).DistanceTo(pointSplit) < 2 * mEPCurve.Diameter)
                        return false;

                    ElementId idDuctNew = BreakMEPCurve(doc, mEPCurve.Id, pointSplit);

                    MEPCurve newMEPCurve = doc.GetElement(idDuctNew) as MEPCurve;

                    doc.Regenerate();

                    XYZ centerMEPCurve = Common.GetCenterElement(mEPCurve);

                    XYZ centerMEPCurveNew = Common.GetCenterElement(newMEPCurve);

                    lenght = pointCheckDistance.DistanceTo(centerMEPCurve);

                    lenghtNew = pointCheckDistance.DistanceTo(centerMEPCurveNew);

                    if (lenght > lenghtNew)
                    {
                        doc.Delete(newMEPCurve.Id);
                    }
                    else
                    {
                        doc.Delete(mEPCurve.Id);

                        mEPCurve = newMEPCurve;
                    }
                    return true;
                }
            }

            return false;
        }

        public static ElementId BreakMEPCurve(Document doc, ElementId mepCurveId, XYZ breakPoint)
        {
            ElementId newMEPCurveId = ElementId.InvalidElementId;
            try
            {
                if (doc != null && mepCurveId != ElementId.InvalidElementId && breakPoint != null)
                {
                    MEPCurve mepCurve = doc.GetElement(mepCurveId) as MEPCurve;

                    if (mepCurve != null && mepCurve.Location is LocationCurve lc)
                    {
                        Line line = lc.Curve as Line;

                        XYZ project = Common.GetPointProjectOnLine(line, breakPoint);

                        if (!Common.IsBetweenLine(line, project))
                            return newMEPCurveId;

                        if (mepCurve is Autodesk.Revit.DB.Mechanical.Duct duct)
                        {
                            newMEPCurveId = MechanicalUtils.BreakCurve(doc, duct.Id, project);
                        }
                        else if (mepCurve is Pipe pipe)
                        {
                            newMEPCurveId = PlumbingUtils.BreakCurve(doc, pipe.Id, project);
                        }
                        else
                        {
                            //copy mepCurveToOptimize as newPipe and move to brkPoint

                            var start = line.GetEndPoint(0);
                            var end = line.GetEndPoint(1);

                            Connector con1 = ConnectorUtils.GetConnectorNearest(start, mepCurve.ConnectorManager, out Connector con2);

                            ConnectorUtils.DisconnectFrom(con1, out Element fitting1);
                            ConnectorUtils.DisconnectFrom(con2, out Element fitting2);

                            var copiedEls = ElementTransformUtils.CopyElement(doc, mepCurve.Id, breakPoint - start);

                            newMEPCurveId = copiedEls.First();

                            MEPCurve newCableTray = doc.GetElement(newMEPCurveId) as MEPCurve;

                            if (!start.IsAlmostEqualTo(breakPoint))
                            {
                                ((LocationCurve)mepCurve.Location).Curve = Line.CreateBound(start, breakPoint);
                                if (mepCurve != null && fitting1 != null && fitting1 is FamilyInstance instance1)
                                {
                                    ConnectorUtils.GetConnectorClosedTo(mepCurve.ConnectorManager, instance1.MEPModel?.ConnectorManager, out Connector con11, out Connector con22);
                                    if (con11 != null && con22 != null && !con11.IsConnected && !con22.IsConnected)
                                        con11.ConnectTo(con22);
                                }
                            }

                            if (!end.IsAlmostEqualTo(breakPoint))
                            {
                                ((LocationCurve)newCableTray.Location).Curve = Line.CreateBound(breakPoint, end);

                                if (newCableTray != null && fitting2 != null && fitting2 is FamilyInstance instance2)
                                {
                                    ConnectorUtils.GetConnectorClosedTo(newCableTray.ConnectorManager, instance2.MEPModel?.ConnectorManager, out Connector con11, out Connector con22);
                                    if (con11 != null && con22 != null && !con11.IsConnected && !con22.IsConnected)
                                        con11.ConnectTo(con22);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return ElementId.InvalidElementId;
            }

            return newMEPCurveId;
        }
    }

    public class DisableWarning : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var messages = failuresAccessor.GetFailureMessages();
            if (messages.Count() > 0)
            {
                foreach (FailureMessageAccessor message in messages)
                {
                    //var lstId = message.GetFailingElementIds();
                    failuresAccessor.DeleteWarning(message);
                }
            }

            return FailureProcessingResult.Continue;
        }
    }
}
