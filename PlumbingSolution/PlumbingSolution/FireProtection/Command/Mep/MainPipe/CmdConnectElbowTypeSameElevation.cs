using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ParameterUtils = PlumbingSolution.FireProtection.Ultis.ParameterUtils;

namespace PlumbingSolution.FireProtection.Command.Mep.MainPipe
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdConnectElbowTypeSameElevation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

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
                    Reference pickedRefMain = uidoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve main :");

                    if (pickedRefMain != null)
                    {
                        MEPCurve mainMEPCurve = doc.GetElement(pickedRefMain) as MEPCurve;
                        if (mainMEPCurve == null)
                            return;

                        XYZ pickedPointMain = pickedRefMain.GlobalPoint;

                        Reference pickedRefBranch = uidoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve branch :");

                        if (pickedRefBranch != null)
                        {
                            MEPCurve branchMEPCurve = doc.GetElement(pickedRefBranch) as MEPCurve;

                            XYZ pickedPointBranch = pickedRefBranch.GlobalPoint;

                            if (branchMEPCurve == null)
                                return;

                            if (mainMEPCurve.Id == branchMEPCurve.Id) return;

                            try
                            {
                                ConnectElbowTypeInheritElevation(doc, mainMEPCurve, branchMEPCurve, pickedPointMain, pickedPointBranch);
                            }
                            catch (Exception ex)
                            {
                                continue;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public static bool ConnectElbowTypeInheritElevation(Document doc, MEPCurve mainMEPCurve, MEPCurve brachMEPCurve, XYZ pickedPointMain, XYZ pickedPoint, List<ElementId> toDelete = null)
        {
            if (mainMEPCurve == null || brachMEPCurve == null)
                return false;

            Transaction tran = new Transaction(doc, "ConnectBranchTypeInheritElevation");

            try
            {
                tran.Start();

                if (toDelete?.Count > 0)
                    doc.Delete(toDelete);

                doc.Regenerate();

                ParameterUtils.SetValueParameterByBuiltIn(brachMEPCurve, BuiltInParameter.RBS_OFFSET_PARAM, ParameterUtils.GetValueParameterByBuilt(mainMEPCurve, BuiltInParameter.RBS_OFFSET_PARAM));

                ElementId systemTypeId = mainMEPCurve.MEPSystem.GetTypeId();

                SplitMEPCurve(doc, ref mainMEPCurve, ref brachMEPCurve, pickedPointMain, pickedPoint, false, true, false);

                doc.Regenerate();

                //ConnectorUtils.

                GetConnectorClosedTo(mainMEPCurve.ConnectorManager, brachMEPCurve.ConnectorManager, out Connector con1, out Connector con2);

                if (con1 != null && con2 != null && !con1.IsConnected && !con2.IsConnected)
                {
                    doc.Create.NewElbowFitting(con1, con2);
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

        public static void SplitMEPCurve(Document doc, ref MEPCurve mEPCurve1, ref MEPCurve mEPCurve2, XYZ pickedPointMain, XYZ pickedPoint, bool isSplitDuct1, bool isSplitDuct2, bool isCutDuct2 = true)
        {
            if (doc != null && mEPCurve1 != null && mEPCurve1.Location is LocationCurve lc1
                && mEPCurve2 != null && mEPCurve2.Location is LocationCurve lc2)
            {
                Line lineMepMain = Common.To2D(lc1.Curve as Line);
                Line lineMepBranch = Common.To2D(lc2.Curve as Line);

                if (lineMepMain == null || lineMepBranch == null)
                    return;

                XYZ pointIntersection = Common.Intersection(lineMepMain, lineMepBranch);
                if (pointIntersection != null)
                {
                    Line lineIntersectPointToZ = Line.CreateUnbound(pointIntersection, XYZ.BasisZ);

                    XYZ point1 = Common.LineIntersection(lc1.Curve as Line, lineIntersectPointToZ, true);

                    XYZ point2 = Common.LineIntersection(lc2.Curve as Line, lineIntersectPointToZ, true);

                    //if (isSplitDuct1)
                    //{
                    //    SplitMEPCurveAtPoint(doc, ref mEPCurve1, point1, pointIntersection);
                    //    doc.Regenerate();
                    //}

                    if (isSplitDuct2)
                    {
                        SplitMEPCurveAtPoint(doc, ref mEPCurve1, pickedPointMain, pointIntersection);
                        SplitMEPCurveAtPoint(doc, ref mEPCurve2, pickedPoint, pointIntersection);

                        doc.Regenerate();

                        //if (isCutDuct2)
                        //{
                        //    Connector con1 = ConnectorUtils.GetConnectorNearest(point2, mEPCurve2.ConnectorManager, out Connector con2);

                        //    if (con1 != null && con1.IsValidObject)
                        //    {
                        //        double distance = 2 * point1.DistanceTo(point2);

                        //        XYZ pointOffset = Common.GetPointOnVector(point2, con1.CoordinateSystem.BasisZ.Negate(), distance);

                        //        SplitMEPCurveAtPoint(doc, ref mEPCurve2, pointOffset, pointIntersection);
                        //    }
                        //}
                    }
                }
            }
        }

        public static bool SplitMEPCurveAtPoint(Document doc, ref MEPCurve mEPCurve, XYZ pointPicked, XYZ pointCheckDistance)
        {
            if (doc != null && mEPCurve != null && mEPCurve.Location is LocationCurve lc && pointPicked != null && pointCheckDistance != null)
            {
                Line lineMEPCurve = lc.Curve as Line;

                if (Common.IsBetweenLine(lineMEPCurve, pointPicked))
                {
                    XYZ centerMEP1 = (lineMEPCurve.GetEndPoint(0) + pointPicked) / 2;

                    XYZ centerMEP2 = (lineMEPCurve.GetEndPoint(1) + pointPicked) / 2;

                    double lenght = pointCheckDistance.DistanceTo(centerMEP1);

                    double lenghtNew = pointCheckDistance.DistanceTo(centerMEP2);

                    if (lenght > lenghtNew && lineMEPCurve.GetEndPoint(0).DistanceTo(pointPicked) < 2 * mEPCurve.Diameter)
                        return false;

                    if (lenghtNew > lenght && lineMEPCurve.GetEndPoint(1).DistanceTo(pointPicked) < 2 * mEPCurve.Diameter)
                        return false;

                    ElementId idDuctNew = BreakMEPCurve(doc, mEPCurve.Id, pointCheckDistance);

                    MEPCurve newMEPCurve = doc.GetElement(idDuctNew) as MEPCurve;

                    doc.Regenerate();

                    if (newMEPCurve != null)
                    {
                        Line newLine = newMEPCurve.GetCurve() as Line;

                        var NewPointProject = newLine.Project(pointPicked);

                        if (Common.IsPerpendicular((NewPointProject.XYZPoint - pointPicked).Normalize(), newLine.Direction))
                        {
                            doc.Delete(mEPCurve.Id);

                            mEPCurve = newMEPCurve;
                        }
                        else
                        {
                            doc.Delete(newMEPCurve.Id);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// BreakMEPCurve
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mEPCurve"></param>
        /// <param name="fittingWye"></param>
        /// <returns></returns>
        //private static List<MEPCurve> BreakMEPCurve(Document doc, MEPCurve mEPCurve, FamilyInstance fittingWye)
        //{
        //    List<MEPCurve> retval = new List<MEPCurve>();
        //    if (mEPCurve != null && fittingWye != null)
        //    {
        //        Common.GetInformationConectorWye(fittingWye, null, out Connector conFittingStart, out Connector conFittingEnd, out Connector conNhanhWye);

        //        XYZ stFitting = conFittingStart.Origin;
        //        XYZ endFitting = conFittingEnd.Origin;

        //        Line line = ((LocationCurve)mEPCurve.Location).Curve as Line;

        //        if (mEPCurve != null && mEPCurve.IsValidObject && !retval.Select(x => x.Id).Contains(mEPCurve.Id))
        //            retval.Add(mEPCurve);

        //        if (Common.IsBetweenLine(line, stFitting))
        //        {
        //            ElementId newMEPCurveId = BreakMEPCurve(doc, mEPCurve.Id, stFitting);
        //            if (newMEPCurveId != ElementId.InvalidElementId)
        //            {
        //                MEPCurve mEPCurveSplit = doc.GetElement(newMEPCurveId) as MEPCurve;
        //                if (mEPCurveSplit != null && mEPCurveSplit.IsValidObject && !retval.Select(x => x.Id).Contains(mEPCurveSplit.Id))
        //                    retval.AddRange(BreakMEPCurve(doc, mEPCurveSplit, fittingWye));
        //            }
        //        }

        //        if (Common.IsBetweenLine(line, endFitting))
        //        {
        //            ElementId newMEPCurveId = BreakMEPCurve(doc, mEPCurve.Id, endFitting);
        //            if (newMEPCurveId != ElementId.InvalidElementId)
        //            {
        //                MEPCurve mEPCurveSplit = doc.GetElement(newMEPCurveId) as MEPCurve;
        //                if (mEPCurveSplit != null && mEPCurveSplit.IsValidObject && !retval.Select(x => x.Id).Contains(mEPCurveSplit.Id))
        //                    retval.AddRange(BreakMEPCurve(doc, mEPCurveSplit, fittingWye));
        //            }
        //        }
        //    }

        //    return retval;
        //}

        private static ElementId BreakMEPCurve(Document doc, ElementId mepCurveId, XYZ breakPoint)
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

        private static void CreateLine(Document doc, XYZ startP, XYZ endP)
        {
            if (doc == null)
                return;

            Line line = Line.CreateBound(startP, endP);
            doc.Create.NewDetailCurve(doc.ActiveView, line);
        }

        public static void GetConnectorClosedTo(ConnectorManager connectorManager1, ConnectorManager connectorManager2, out Connector con1, out Connector con2)
        {
            con1 = null;
            con2 = null;

            if (connectorManager1 != null && connectorManager2 != null)

            {
                double distanceMin = double.MaxValue;

                foreach (Connector item1 in connectorManager1.Connectors)
                {
                    if (item1.IsConnected)
                        continue;

                    foreach (Connector item2 in connectorManager2.Connectors)
                    {
                        if (item2.IsConnected) continue;

                        double distance = item1.Origin.DistanceTo(item2.Origin);
                        if (distance < distanceMin)
                        {
                            con1 = item1;
                            con2 = item2;
                            distanceMin = distance;
                        }
                    }
                }
            }
        }
    }
}