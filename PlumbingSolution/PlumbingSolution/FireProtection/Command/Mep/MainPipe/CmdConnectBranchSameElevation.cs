using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Command.Fire.DiritConnectPipes.Service_B;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
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
using ParameterUtils = PlumbingSolution.FireProtection.Ultis.ParameterUtils;

namespace PlumbingSolution.FireProtection.Command.Mep.MainPipe
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdConnectBranchSameElevation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            Document doc = Global.UIDoc.Document;

            try
            {
                MEPCurve mainMEPCurve = doc.GetElement(Global.UIDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve main :")) as MEPCurve;
                if (mainMEPCurve == null)
                    return Result.Cancelled;

                List<MEPCurve> lsMEPCurveBranchs = Global.UIDoc.Selection.PickObjects(ObjectType.Element, new MEPCurveFilter(typeof(Pipe)), "Pick MEPCurve branch :")
                                                                        .Select(x => doc.GetElement(x) as MEPCurve).ToList();
                if (lsMEPCurveBranchs == null || lsMEPCurveBranchs.Count == 0)
                    return Result.Cancelled; ;
                TransactionGroup tranGroup = new TransactionGroup(doc, "ConnectBranchType");
                try
                {
                    OrderBranchMEPCurve(mainMEPCurve, ref lsMEPCurveBranchs, out XYZ orgPoint);

                    tranGroup.Start();
                    foreach (var branchMEPCurve in lsMEPCurveBranchs)
                    {
                        if (!IsInSide(mainMEPCurve, branchMEPCurve))
                            continue;

                        ConnectBranchTypeInheritElevation(doc, ref mainMEPCurve, branchMEPCurve, orgPoint);
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

        public static bool ConnectBranchTypeInheritElevation(Document doc, ref MEPCurve mainMEPCurve,
         MEPCurve branchMEPCurve, XYZ orgPoint, List<ElementId> toDelete = null)
        {
            if (mainMEPCurve == null || branchMEPCurve == null)
                return false;

            Transaction tran = new Transaction(doc, "ConnectBranchTypeInheritElevation");
            List<Element> lstElementCreated = new List<Element>();

            try
            {
                tran.Start();

                if (toDelete?.Count > 0)
                    doc.Delete(toDelete);

                doc.Regenerate();

                ParameterUtils.SetValueParameterByBuiltIn(branchMEPCurve, BuiltInParameter.RBS_OFFSET_PARAM, ParameterUtils.GetValueParameterByBuilt(mainMEPCurve, BuiltInParameter.RBS_OFFSET_PARAM));

                ElementId systemTypeId = branchMEPCurve.MEPSystem.GetTypeId();

                SplitMEPCurve(doc, ref mainMEPCurve, ref branchMEPCurve, false, true, false);
                doc.Regenerate();

                Line lineMain = ((LocationCurve)mainMEPCurve.Location).Curve as Line;
                Line lineBranch = ((LocationCurve)branchMEPCurve.Location).Curve as Line;

                List<Connector> connectorDuctBranchs = ConnectorUtils.ToList(branchMEPCurve.ConnectorManager);

                XYZ pointProject = Common.GetPointProjectOnLine(lineMain.Clone() as Line, connectorDuctBranchs[0].Origin);

                Connector conBranch1 = ConnectorUtils.GetConnectorNearest(pointProject, branchMEPCurve.ConnectorManager, out Connector conBranch2);

                FamilyInstance fitting = null;

                if (IsCreateTee(mainMEPCurve))
                {
                    List<MEPCurve> lstMainMEPCurves = new List<MEPCurve>();

                    MEPCurve splitMEPCurve = null;

                    fitting = CreateTee(doc, mainMEPCurve, branchMEPCurve, out splitMEPCurve);
                    if (fitting == null)
                        fitting = CreateTeeWye(doc, mainMEPCurve, branchMEPCurve, out splitMEPCurve);

                    mainMEPCurve = (splitMEPCurve == null) ? mainMEPCurve : GetNextMEPCurve(mainMEPCurve, splitMEPCurve, orgPoint);

                    if (mainMEPCurve == null || !mainMEPCurve.IsValidObject)
                    {
                        if (tran.HasStarted())
                            tran.RollBack();
                        return false;
                    }
                }
                else
                {
                    fitting = HandleProcessTwoLevelSmartCommand.CreateTap(mainMEPCurve as MEPCurve, branchMEPCurve as MEPCurve);
                }

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

        public static FamilyInstance CreateTee(Document doc, MEPCurve mainMEPCurve, MEPCurve branchMEPCurve, out MEPCurve spliMEPCurve)
        {
            FamilyInstance tee = null;

            spliMEPCurve = null;

            if (doc == null || mainMEPCurve == null || branchMEPCurve == null)
                return tee;
            SubTransaction subTran = new SubTransaction(doc);
            try
            {
                Line lineMain = ((LocationCurve)mainMEPCurve.Location).Curve as Line;

                Line lineBranch = ((LocationCurve)branchMEPCurve.Location).Curve as Line;

                XYZ project = Common.GetPointProjectOnLine(lineMain, lineBranch.Origin);

                if (!Common.IsBetweenLine(lineMain, project))
                    return tee;

                subTran.Start();

                ElementId elementId = BreakMEPCurve(doc, mainMEPCurve.Id, project);

                spliMEPCurve = doc.GetElement(elementId) as MEPCurve;

                if (mainMEPCurve != null && mainMEPCurve.IsValidObject && spliMEPCurve != null && spliMEPCurve.IsValidObject && branchMEPCurve != null)
                {
                    ConnectorUtils.GetConnectorClosedTo(mainMEPCurve.ConnectorManager, spliMEPCurve.ConnectorManager, out Connector conMain1, out Connector conMain2);

                    ConnectorUtils.GetConnectorClosedTo(spliMEPCurve.ConnectorManager, branchMEPCurve.ConnectorManager, out Connector con, out Connector conBranch);
                    tee = doc.Create.NewTeeFitting(conMain1, conMain2, conBranch);

                    doc.Regenerate();
                }
                subTran.Commit();
            }
            catch (Exception)
            {
                if (subTran.HasStarted())
                    subTran.RollBack();
            }
            return tee;
        }

        /// <summary>
        /// Lấy symbol wye của ống
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="ele"></param>
        /// <returns></returns>
        public static FamilySymbol GetFamilySymbolTee(Document doc, Element ele)
        {
            try
            {
                RoutingPreferenceManager rpm = null;

                double diameter = -1;
                if (ele is CableTray cableTray)
                {
                    CableTrayType cableTrayType = doc.GetElement(cableTray.GetTypeId()) as CableTrayType;

                    return cableTrayType.Tee;
                }
                else if (ele is Conduit conduit)
                {
                    ConduitType conduitType = doc.GetElement(conduit.GetTypeId()) as ConduitType;

                    return conduitType.Tee;
                }
                else if (ele is Pipe pipe)
                {
                    diameter = pipe.Diameter;
                    rpm = pipe.PipeType.RoutingPreferenceManager;
                }
                else if (ele is Autodesk.Revit.DB.Mechanical.Duct duct)
                {
                    rpm = duct.DuctType.RoutingPreferenceManager;
                }

                int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Junctions);
                for (int i = 0; i < numberOfRule; i++)
                {
                    RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Junctions, i);

                    FamilySymbol symbol = doc.GetElement(rule.MEPPartId) as FamilySymbol;

                    if (symbol != null && IsValidSymbol(doc, symbol))
                    {
                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (minimumSize < diameter && diameter < maximumSize)
                            return symbol;
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsValidSymbol(Document doc, FamilySymbol symbol)
        {
            SubTransaction subTran = new SubTransaction(doc);

            try
            {
                subTran.Start();

                if (!symbol.IsActive)
                    symbol.Activate();
                FamilyInstance retval = doc.Create.NewFamilyInstance(XYZ.Zero, symbol, StructuralType.NonStructural);

                doc.Regenerate();

                List<Connector> connectors = ConnectorUtils.ToList(retval.MEPModel.ConnectorManager);

                if (connectors.Count == 3)
                    return true;
            }
            finally
            {
                if (subTran.HasStarted())
                    subTran.RollBack();
            }

            return false;
        }

        public static FamilyInstance CreateTeeWye(Document doc, MEPCurve mEPCurveMain, MEPCurve mEPCurveBranch, out MEPCurve splitMEPCurve)

        {
            SubTransaction subTran = new SubTransaction(doc);
            FamilyInstance retval = null;
            splitMEPCurve = null;
            try
            {
                FamilySymbol familySymbol = GetFamilySymbolTee(doc, mEPCurveMain);

                if (familySymbol == null)
                    return retval;

                ConnectorUtils.GetConnectorClosedTo(mEPCurveMain.ConnectorManager, mEPCurveBranch.ConnectorManager, out Connector con1, out Connector con2);

                Line lineMain = ((LocationCurve)mEPCurveMain.Location).Curve as Line;
                Line lineBranch = ((LocationCurve)mEPCurveBranch.Location).Curve as Line;

                XYZ location = Common.GetPointProjectOnLine(lineMain, con2.Origin);

                if (!Common.IsBetweenLine(Common.GetLineExtend(lineMain, -100 / 304.8), location))
                    return retval;

                subTran.Start();

                if (!familySymbol.IsActive)
                    familySymbol.Activate();

                retval = doc.Create.NewFamilyInstance(location, familySymbol, mEPCurveMain.ReferenceLevel, StructuralType.NonStructural);

                if (retval != null)
                {
                    ParameterUtils.SetValueParameterByBuiltIn(retval, BuiltInParameter.INSTANCE_ELEVATION_PARAM, location.Z - mEPCurveMain.ReferenceLevel?.Elevation);

                    Common.GetInformationConectorWye(retval, null, out Connector main1, out Connector main2, out Connector conY);

                    ConnectorProfileType shape = main1.Shape;

                    if (shape == ConnectorProfileType.Round)
                    {
                        Common.SetRadiusConnector(retval, main1, mEPCurveMain.Diameter / 2);
                        Common.SetRadiusConnector(retval, main2, mEPCurveMain.Diameter / 2);
                        Common.SetRadiusConnector(retval, conY, mEPCurveBranch.Diameter / 2);
                        //main1.Radius = mEPCurveMain.Diameter / 2;
                        //main2.Radius = mEPCurveMain.Diameter / 2;
                        //conY.Radius = mEPCurveBranch.Diameter / 2;
                    }
                    else if (shape == ConnectorProfileType.Oval || shape == ConnectorProfileType.Rectangular)
                    {
                        main1.Height = mEPCurveMain.Height;
                        main1.Width = mEPCurveMain.Width;

                        main2.Height = mEPCurveMain.Height;
                        main2.Width = mEPCurveMain.Width;

                        conY.Height = mEPCurveBranch.Height;
                        conY.Width = mEPCurveBranch.Width;
                    }

                    double valueAngle = GetAngleWYE(mEPCurveBranch, mEPCurveMain);

                    ParameterUtils.SetValueParameterByName(retval, "Angle", valueAngle);

                    doc.Regenerate();

                    Common.GetInformationConectorWye(retval, null, out main1, out main2, out conY);

                    if (main1 == null || main2 == null || conY == null)
                    {
                        if (subTran.HasStarted())
                            subTran.RollBack();
                        return retval;
                    }

                    Line axisDestination = Line.CreateBound(main1.Origin, main2.Origin);
                    Common.RotateLine(doc, retval, lineMain);
                    doc.Regenerate();

                    if (!Common.IsEqual(valueAngle, Math.PI / 2) && IsFlipFitting(mEPCurveBranch, retval))
                    {
                        FlipFitting(doc, retval);
                        doc.Regenerate();
                    }

                    doc.Regenerate();

                    double angle = GetAngleRotate(mEPCurveBranch, retval, lineMain);

                    ElementTransformUtils.RotateElement(doc, retval.Id, lineMain, angle);
                    doc.Regenerate();

                    MoveFitting(mEPCurveBranch, retval);
                    doc.Regenerate();

                    Common.GetInformationConectorWye(retval, null, out main1, out main2, out conY);
                    Connector con = ConnectorUtils.GetConnectorNearest(conY.Origin, mEPCurveBranch.ConnectorManager, out Connector cc);
                    if (con != null && conY != null)
                        con.ConnectTo(conY);

                    if (!SplitMEPCurve(doc, mEPCurveMain, retval, out splitMEPCurve))
                    {
                        if (subTran.HasStarted())
                            subTran.RollBack();
                        return retval;
                    }

                    //Common.GetInformationConectorWye(retval, null, out main1, out main2, out conY);
                    //if (!main1.IsConnected || !main2.IsConnected || !conY.IsConnected)
                    //{
                    //    if (subTran.HasStarted())
                    //        subTran.RollBack();
                    //    return retval;
                    //}

                    ElementTransformUtils.MoveElement(doc, retval.Id, lineMain.Direction.Normalize() * 1 / 304.8);
                    doc.Regenerate();
                    ElementTransformUtils.MoveElement(doc, retval.Id, lineMain.Direction.Normalize() * -1 / 304.8);
                    doc.Regenerate();

                    subTran.Commit();
                }

                return retval;
            }
            catch (Exception)
            {
                if (subTran.HasStarted())
                    subTran.RollBack();
                return retval;
            }
        }

        /// <summary>
        /// BreakMEPCurve
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mEPCurve"></param>
        /// <param name="fittingWye"></param>
        /// <returns></returns>
        private static List<MEPCurve> BreakMEPCurve(Document doc, MEPCurve mEPCurve, FamilyInstance fittingWye)
        {
            List<MEPCurve> retval = new List<MEPCurve>();
            if (mEPCurve != null && fittingWye != null)
            {
                Common.GetInformationConectorWye(fittingWye, null, out Connector conFittingStart, out Connector conFittingEnd, out Connector conNhanhWye);

                XYZ stFitting = conFittingStart.Origin;
                XYZ endFitting = conFittingEnd.Origin;

                Line line = ((LocationCurve)mEPCurve.Location).Curve as Line;

                if (mEPCurve != null && mEPCurve.IsValidObject && !retval.Select(x => x.Id).Contains(mEPCurve.Id))
                    retval.Add(mEPCurve);

                if (Common.IsBetweenLine(line, stFitting))
                {
                    ElementId newMEPCurveId = BreakMEPCurve(doc, mEPCurve.Id, stFitting);
                    if (newMEPCurveId != ElementId.InvalidElementId)
                    {
                        MEPCurve mEPCurveSplit = doc.GetElement(newMEPCurveId) as MEPCurve;
                        if (mEPCurveSplit != null && mEPCurveSplit.IsValidObject && !retval.Select(x => x.Id).Contains(mEPCurveSplit.Id))
                            retval.AddRange(BreakMEPCurve(doc, mEPCurveSplit, fittingWye));
                    }
                }

                if (Common.IsBetweenLine(line, endFitting))
                {
                    ElementId newMEPCurveId = BreakMEPCurve(doc, mEPCurve.Id, endFitting);
                    if (newMEPCurveId != ElementId.InvalidElementId)
                    {
                        MEPCurve mEPCurveSplit = doc.GetElement(newMEPCurveId) as MEPCurve;
                        if (mEPCurveSplit != null && mEPCurveSplit.IsValidObject && !retval.Select(x => x.Id).Contains(mEPCurveSplit.Id))
                            retval.AddRange(BreakMEPCurve(doc, mEPCurveSplit, fittingWye));
                    }
                }
            }

            return retval;
        }

        /// <summary>
        /// SplitMEPCurveAtPoint
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mEPCurve"></param>
        /// <param name="fittingWye"></param>
        /// <returns></returns>
        public static bool SplitMEPCurve(Document doc, MEPCurve mEPCurve, FamilyInstance fittingWye, out MEPCurve splitMEPCurve)
        {
            splitMEPCurve = null;
            try
            {
                if (mEPCurve != null && fittingWye != null)
                {
                    Common.GetInformationConectorWye(fittingWye, null, out Connector main1, out Connector main2, out Connector conTee);

                    Line lineFitting = Line.CreateBound(main1.Origin, main2.Origin);
                    List<MEPCurve> lstMEPCurveSplits = BreakMEPCurve(doc, mEPCurve, fittingWye);

                    foreach (var item in lstMEPCurveSplits)
                    {
                        if (item != null && item.IsValidObject)
                        {
                            XYZ centerMEPCurve = ((LocationCurve)item.Location).Curve.Evaluate(0.5, true);

                            if (Common.IsBetweenLine(lineFitting, centerMEPCurve))
                                doc.Delete(item.Id);
                        }
                    }

                    MEPCurve mepCurve1 = lstMEPCurveSplits.FirstOrDefault(x => x != null && x.IsValidObject);
                    MEPCurve mepCurve2 = lstMEPCurveSplits.LastOrDefault(x => x != null && x.IsValidObject);

                    if (mepCurve1 != null)
                    {
                        ConnectorUtils.GetConnectorClosedTo(fittingWye.MEPModel.ConnectorManager, mepCurve1.ConnectorManager, out Connector con1, out Connector con2);
                        if (con1 != null && con2 != null && !con1.IsConnectedTo(con2))
                            con1.ConnectTo(con2);
                    }

                    if (mepCurve2 != null)
                    {
                        ConnectorUtils.GetConnectorClosedTo(fittingWye.MEPModel.ConnectorManager, mepCurve2.ConnectorManager, out Connector con1, out Connector con2);
                        if (con1 != null && con2 != null && !con1.IsConnectedTo(con2))
                            con1.ConnectTo(con2);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private static double GetAngleRotate(MEPCurve mEPCurve, FamilyInstance fittingY, Line axisRotate)
        {
            if (mEPCurve != null && fittingY != null
                && mEPCurve.Location is LocationCurve locationCurve
                && fittingY.Location is LocationPoint locationPoint)
            {
                Common.GetInformationConectorWye(fittingY, null, out Connector main1, out Connector main2, out Connector conY);

                if (main1 != null && main2 != null && conY != null)
                {
                    Plane plane = Plane.CreateByNormalAndOrigin(main1.CoordinateSystem.BasisZ, main1.Origin);

                    XYZ point1 = Common.GetPointProjectOnPlane(plane, locationPoint.Point);
                    XYZ point2 = Common.GetPointProjectOnPlane(plane, conY.Origin);
                    XYZ vec1 = (point2 - point1).Normalize();

                    XYZ point3 = Common.GetPointProjectOnPlane(plane, locationCurve.Curve.GetEndPoint(0));
                    XYZ point4 = Common.GetPointProjectOnPlane(plane, locationCurve.Curve.GetEndPoint(1));

                    double distance1 = point3.DistanceTo(point1);
                    double distance2 = point4.DistanceTo(point1);

                    if (distance1 > distance2)
                    {
                        XYZ temp = point3;
                        point3 = point4;
                        point4 = temp;
                    }

                    XYZ vec2 = (point4 - point3).Normalize();

                    //return vec1.AngleTo(vec2);

                    double angle = vec1.AngleOnPlaneTo(vec2, main1.CoordinateSystem.BasisZ);

                    double distanceToY1 = double.MaxValue;
                    double distanceToY2 = double.MaxValue;

                    SubTransaction subTransaction = new SubTransaction(fittingY.Document);
                    try
                    {
                        subTransaction.Start();

                        ElementTransformUtils.RotateElement(fittingY.Document, fittingY.Id, axisRotate, angle);
                        fittingY.Document.Regenerate();

                        Connector con11 = ConnectorUtils.GetConnectorNearest(conY.Origin, mEPCurve.ConnectorManager, out Connector con12);

                        distanceToY1 = con11.Origin.DistanceTo(conY.Origin);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        if (subTransaction.HasStarted())
                            subTransaction.RollBack();
                    }

                    Common.GetInformationConectorWye(fittingY, null, out main1, out main2, out conY);
                    try
                    {
                        subTransaction.Start();

                        ElementTransformUtils.RotateElement(fittingY.Document, fittingY.Id, axisRotate, -angle);
                        fittingY.Document.Regenerate();

                        Connector con11 = ConnectorUtils.GetConnectorNearest(conY.Origin, mEPCurve.ConnectorManager, out Connector con12);

                        distanceToY2 = con11.Origin.DistanceTo(conY.Origin);
                    }
                    finally
                    {
                        if (subTransaction.HasStarted())
                            subTransaction.RollBack();
                    }

                    if (distanceToY1 > distanceToY2)
                        return -angle;

                    return angle;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Calculation of the angle of branch pipe and main pipe
        /// </summary>
        /// <param name="mEPCurve1"></param>
        /// <param name="mEPCurve2"></param>
        /// <returns></returns>
        public static double GetAngleWYE(MEPCurve mEPCurve1, MEPCurve mEPCurve2)
        {
            if (mEPCurve1 != null && mEPCurve2 != null
               && mEPCurve1.Location is LocationCurve locationCurve1
               && mEPCurve2.Location is LocationCurve locationCurve2)
            {
                Line line1 = locationCurve1.Curve as Line;
                Line line2 = locationCurve2.Curve as Line;

                Plane plane = Plane.CreateByNormalAndOrigin(line1.Direction.CrossProduct(line2.Direction), line1.Origin);

                line1 = Common.GetLineProjectOnPlane(plane, line1);
                line2 = Common.GetLineProjectOnPlane(plane, line2);

                List<XYZ> list1 = new List<XYZ>() { line1.GetEndPoint(0), line1.GetEndPoint(1) };
                List<XYZ> list2 = new List<XYZ>() { line2.GetEndPoint(0), line2.GetEndPoint(1) };

                list1 = list1.OrderBy(x => x.X).ThenBy(x => x.Y).ThenBy(x => x.Z).ToList();
                list2 = list2.OrderBy(x => x.X).ThenBy(x => x.Y).ThenBy(x => x.Z).ToList();

                XYZ direction1 = (list1.LastOrDefault() - list1.FirstOrDefault()).Normalize();

                XYZ direction2 = (list2.LastOrDefault() - list2.FirstOrDefault()).Normalize();

                return direction1.AngleTo(direction2);
            }

            return Math.PI / 4;
        }

        private static void FlipFitting(Document doc, FamilyInstance fitting)
        {
            Common.GetInformationConectorWye(fitting, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

            if (conSt == null || conEnd == null || conNhanhWye == null)
                return;

            XYZ locationBeforeSt = conSt.Origin;

            ConnectorUtils.DisconnectFrom(fitting, out Connector connectedSt, out Connector connectedEnd, out Element eleSt, out Element eleEnd);

            Line axis = Line.CreateBound(conSt.Origin, conEnd.Origin);

            XYZ projectPoint = Common.GetPointProjectOnLine(axis, conNhanhWye.Origin);

            XYZ direction = (projectPoint - conNhanhWye.Origin).Normalize();

            Line axisFlip = Line.CreateUnbound(projectPoint, direction);

            fitting.Location.Rotate(axisFlip, Math.PI);

            if (conSt != null && connectedEnd != null && !conSt.IsConnectedTo(connectedEnd))
                conSt.ConnectTo(connectedEnd);

            if (conEnd != null && connectedSt != null && !conEnd.IsConnectedTo(connectedSt))
                conEnd.ConnectTo(connectedSt);

            XYZ locationAfterEnd = conEnd.Origin;

            XYZ translation = locationBeforeSt - locationAfterEnd;

            ElementTransformUtils.MoveElement(doc, fitting.Id, translation);
        }

        private static void MoveFitting(MEPCurve mEPCurveBrach, FamilyInstance fitting)
        {
            if (mEPCurveBrach != null && fitting != null
             && mEPCurveBrach.Location is LocationCurve locationCurve
             && fitting.Location is LocationPoint locationPoint)
            {
                Common.GetInformationConectorWye(fitting, null, out Connector main1, out Connector main2, out Connector conTee);

                Line lineMain = Line.CreateUnbound(main1.Origin, main2.CoordinateSystem.BasisZ);

                Line lineBranch = locationCurve.Curve as Line;

                //double distance1 = lineMain.Distance(lineBranch.GetEndPoint(0));
                //double distance2 = lineMain.Distance(lineBranch.GetEndPoint(1));

                //XYZ pointBranch = (distance1 < distance2) ? lineBranch.GetEndPoint(0) : lineBranch.GetEndPoint(1);

                XYZ pointBranch = GetPointNearest(lineMain, lineBranch);

                XYZ projectPointBranch = Common.GetPointProjectOnLine(lineMain, pointBranch);

                XYZ projectPointTee = Common.GetPointProjectOnLine(lineMain, conTee.Origin);

                ElementTransformUtils.MoveElement(fitting.Document, fitting.Id, projectPointBranch - projectPointTee);
                fitting.Document.Regenerate();
            }
        }

        private static bool IsFlipFitting(MEPCurve mEPCurve, FamilyInstance fitting)
        {
            if (mEPCurve != null && fitting != null
              && mEPCurve.Location is LocationCurve locationCurve
              && fitting.Location is LocationPoint locationPoint)
            {
                Common.GetInformationConectorWye(fitting, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

                if (conSt != null && conEnd != null && conNhanhWye != null)
                {
                    Line axis = Line.CreateBound(conSt.Origin, conEnd.Origin);

                    XYZ point1 = Common.GetPointProjectOnLine(axis, conNhanhWye.Origin);

                    XYZ vec1 = (point1 - locationPoint.Point).Normalize();

                    Connector con1 = ConnectorUtils.GetConnectorNearest(locationPoint.Point, mEPCurve.ConnectorManager, out Connector con2);

                    XYZ point11 = Common.GetPointProjectOnLine(axis, con1.Origin);
                    XYZ point12 = Common.GetPointProjectOnLine(axis, con2.Origin);

                    XYZ vec2 = (point12 - point11).Normalize();

                    if (vec1.DotProduct(vec2) > 0)//|| Common.IsParallel(vec1, vec2))
                        return false;
                }
            }

            return true;
        }

        private static XYZ GetPointNearest(Line lineSource, Line line)
        {
            double distance1 = lineSource.Distance(line.GetEndPoint(0));
            double distance2 = lineSource.Distance(line.GetEndPoint(1));

            XYZ reval = (distance1 < distance2) ? line.GetEndPoint(0) : line.GetEndPoint(1);

            return reval;
        }

        private static XYZ GetVector45Degree(Line line, XYZ point)
        {
            if (line == null || point == null)
                return null;

            Line lineCopy = line.Clone() as Line;

            XYZ pointProject = Common.GetPointProjectOnLine(lineCopy.Clone() as Line, point);

            XYZ vector = (point - pointProject).Normalize();

            XYZ point1 = Common.GetPointOnVector(line.Origin, line.Direction.Negate(), 100 / 304.8);

            XYZ point2 = Common.GetPointOnVector(point1, vector, 100 / 304.8);

            return (point2 - line.Origin).Normalize();
        }

        private static XYZ GetVector90Degree(Line line, XYZ point)
        {
            if (line == null || point == null)
                return null;

            Line lineCopy = line.Clone() as Line;

            XYZ pointProject = Common.GetPointProjectOnLine(lineCopy, point);

            return (point - pointProject).Normalize();
        }

        public static bool IsCreateTee(MEPCurve mEPCurve)
        {
            if (mEPCurve is Pipe pipe)
            {
                var pipeType = pipe.PipeType as PipeType;

                if (pipeType.RoutingPreferenceManager.PreferredJunctionType == PreferredJunctionType.Tap)
                    return false;
            }
            else if (mEPCurve is Autodesk.Revit.DB.Mechanical.Duct duct)
            {
                var ductType = duct.DuctType as DuctType;

                if (ductType.RoutingPreferenceManager.PreferredJunctionType == PreferredJunctionType.Tap)
                    return false;
            }

            return true;
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

        public static bool IsInSide(MEPCurve mainMEPCurve, MEPCurve branchMEPCurve)
        {
            if (mainMEPCurve != null && mainMEPCurve.Location is LocationCurve lc1 && branchMEPCurve != null && branchMEPCurve.Location is LocationCurve lc2)
            {
                Line lineMain = lc1.Curve as Line;

                var lineMain2d = RevitUtils.ProjectLineToPlane(lineMain);

                Line lineBranch = lc2.Curve as Line;

                var lineBranch2d = RevitUtils.ProjectLineToPlane(lineBranch);

                XYZ pointProject = Common.GetPointProjectOnLine(lineMain2d, lineBranch2d.Origin);

                return Common.IsBetweenLine(lineMain2d, pointProject);
            }
            return false;
        }

        public static void OrderBranchMEPCurve(MEPCurve mainMEPCurve, ref List<MEPCurve> lstMEPCurveBranchs, out XYZ pointOrigin)
        {
            pointOrigin = null;

            if (mainMEPCurve != null && mainMEPCurve.Location is LocationCurve lc && lstMEPCurveBranchs.Count > 0)
            {
                Line lineMain = lc.Curve as Line;
                List<XYZ> points = new List<XYZ>() { lineMain.GetEndPoint(0), lineMain.GetEndPoint(1) };

                points = points.OrderBy(x => x.X).ThenBy(x => x.Y).ThenBy(x => x.Z).ToList();

                pointOrigin = points.FirstOrDefault();

                List<Tuple<MEPCurve, XYZ>> tuples = new List<Tuple<MEPCurve, XYZ>>();

                foreach (var item in lstMEPCurveBranchs)
                {
                    Line lineBranch = ((LocationCurve)item.Location).Curve as Line;

                    XYZ pointProject = Common.GetPointProjectOnLine(lineMain, lineBranch.Evaluate(0.5, true));

                    tuples.Add(Tuple.Create<MEPCurve, XYZ>(item, pointProject));
                }

                lstMEPCurveBranchs = tuples.OrderBy(x => x.Item2.X).ThenBy(x => x.Item2.Y).ThenBy(x => x.Item2.Z).Select(x => x.Item1).ToList();
            }
        }

        /// <summary>
        /// Lấy ra pipe mới được tạo ra
        /// </summary>
        /// <param name="mainMEPCurve"></param>
        /// <param name="splitMEPCurve"></param>
        /// <param name="orgPoint"></param>
        /// <returns></returns>
        public static MEPCurve GetNextMEPCurve(MEPCurve mainMEPCurve, MEPCurve splitMEPCurve, XYZ orgPoint)
        {
            double distance1 = mainMEPCurve.ConnectorManager.Lookup(0).Origin.DistanceTo(orgPoint);
            double distance2 = mainMEPCurve.ConnectorManager.Lookup(1).Origin.DistanceTo(orgPoint);
            double distance3 = splitMEPCurve.ConnectorManager.Lookup(0).Origin.DistanceTo(orgPoint);
            double distance4 = splitMEPCurve.ConnectorManager.Lookup(1).Origin.DistanceTo(orgPoint);

            List<double> distances = new List<double>() { distance1, distance2, distance3, distance4 };
            double max = distances.Max(x => x);
            if (max == distance1 || max == distance2)
            {
                return mainMEPCurve;
            }
            else
            {
                return splitMEPCurve;
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
    }
}
