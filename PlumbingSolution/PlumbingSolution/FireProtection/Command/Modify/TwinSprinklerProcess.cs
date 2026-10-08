using PlumbingSolution.FireProtection.UI.Service_E;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    public class TwinSprinklerProcess
    {
        private static Document _doc;
        private static Transaction _trans;
        private static UI_TwinSprinkler _form;

        public TwinSprinklerProcess(Document document, Transaction transaction, UI_TwinSprinkler form)
        {
            _doc = document;
            _trans = transaction;
            _form = form;
        }

        public static void Process(ref List<FamilyInstance> listSprinkler, List<Pipe> listPipe, Pipe pipe)
        {
            Dictionary<FamilyInstance, FamilyInstance> dictSprinkler = new Dictionary<FamilyInstance, FamilyInstance>();
            if (_form.IsCheckedType4C5 || _form.IsCheckedC5Type5)
            {
                dictSprinkler = GetDictSprinklerByPipe2(pipe, listSprinkler, _form.IsCheckedType4C5);
            }
            else
            {
                dictSprinkler = GetDictSprinklerByPipe(pipe, listSprinkler);
            }

            if (dictSprinkler.Count == 0)
                return;

            if (_form.IsCheckedType1C5)
            {
                ProcessType1(pipe, dictSprinkler);
            }
            else if (_form.IsCheckedType2C5)
            {
                ProcessType2(pipe, dictSprinkler);
            }
            else if (_form.IsCheckedType3C5)
            {
                ProcessType3(pipe, dictSprinkler);
            }
            else if (_form.IsCheckedType4C5)
            {
                ProcessType5(pipe, dictSprinkler);
            }
            else if (_form.IsCheckedC5Type5)
            {
                ProcessType6(pipe, dictSprinkler);
            }

            foreach (var item in dictSprinkler)
            {
                if (item.Key != null)
                {
                    var check = Common.GetConnectorNotConnnected(item.Key.MEPModel.ConnectorManager);
                    if (check == null)
                        listSprinkler.Remove(item.Key);
                }
                if (item.Value != null)
                {
                    var check = Common.GetConnectorNotConnnected(item.Value.MEPModel.ConnectorManager);
                    if (check == null)
                        listSprinkler.Remove(item.Value);
                }
            }
        }

        private static void ProcessType1(Pipe pipeMain, Dictionary<FamilyInstance, FamilyInstance> dictSprinkler)
        {
            var curve = (pipeMain.Location as LocationCurve).Curve;
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            double radius = 100; //mm
            if (_form.IsCheckedVerticalTeeOffset)
                radius = _form.VerticalTeeOffset;

            var ft = Common.mmToFT * radius;
            ElementId pipeTypeId = pipeMain.GetTypeId();
            ElementId systemTypeId = pipeMain.MEPSystem.GetTypeId();
            List<ElementId> listPipeId = new List<ElementId>() { pipeMain.Id };
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, DisplayUnitType.DUT_MILLIMETERS);
#else
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, UnitTypeId.Millimeters);
#endif
            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);

            FamilyInstance teeConnect1 = null;
            FamilyInstance teeConnect2 = null;

            foreach (var pair in dictSprinkler)
            {
                HashSet<ElementId> listIdConnectToUp = new HashSet<ElementId>();
                HashSet<ElementId> listIdConnectToDown = new HashSet<ElementId>();
                FamilyInstance nipple = null;

                FamilyInstance sprinklerUp = pair.Key;
                FamilyInstance sprinklerDown = pair.Value;

                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;

                var connectorSprinklerUp = Common.GetConnectorNotConnnected(sprinklerUp.MEPModel.ConnectorManager);
                var connectorSprinklerDown = Common.GetConnectorNotConnnected(sprinklerDown.MEPModel.ConnectorManager);

                XYZ locationConnectorUp = connectorSprinklerUp.Origin;
                XYZ locationConnectorDown = connectorSprinklerDown.Origin;

                XYZ locationSprinklerUp2D = Common.To2D(locationSprinklerUp);
                XYZ locationSprinklerDown2D = Common.To2D(locationSprinklerDown);

                bool split = false;
                var listPipeCheck = new List<Element>() { pipeMain };
                if (listPipeId.Count != 0)
                    listPipeCheck.AddRange(listPipeId.Select(x => _doc.GetElement(x)));

                var pipe = ProcessPipes(listPipeCheck, locationSprinklerUp, out split);

                List<Pipe> listPipeCut = new List<Pipe>();
                listPipeCut.Add(pipe);

                var isEnd = CheckPipeIsEnd(pipe, locationSprinklerUp);

                XYZ pOn = Common.ProjectPointOnPlane(planeCheck, locationConnectorUp);

                Line locationCurvePipe = (pipe.Location as LocationCurve).Curve as Line;
                Line locationCurvePipeUnbound = Line.CreateUnbound(locationCurvePipe.GetEndPoint(0), locationCurvePipe.Direction);

                Pipe pipe2 = null;
                if (!split)
                {
                    if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                    else
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);
                }
                else
                {
                    if (!_form.IsCheckedTeeC5 && !isEnd)
                    {
                        if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                        else
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);
                    }
                    else
                    {
                        pipe2 = CreateTeeByPipe(pipe, pOn, false);

                        if (pipe2 != null)
                            listPipeCut.Add(pipe2);
                    }
                }

                //Create pipe Z
                var newPipeZ = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pOn, locationConnectorUp);

                newPipeZ.LookupParameter("Diameter").Set(dFt);
                var pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                if (pipeType != null)
                    newPipeZ.PipeType = pipeType;

                var conCheck = Common.GetConnectorClosestTo(newPipeZ, locationConnectorUp);
                conCheck.ConnectTo(connectorSprinklerUp);

                listIdConnectToUp.Add(newPipeZ.Id);

                if (pipe2 == null)
                {
                    if (!isEnd && !_form.IsCheckedTeeC5)
                    {
                        var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                        var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);
                        var elbowNew = _doc.Create.NewElbowFitting(conStart, conTee);
                        listIdConnectToUp.Add(elbowNew.Id);
                        teeConnect1 = elbowNew;
                    }
                    else if (split)
                    {
                        var tap = CreateTapPipe(pipe, newPipeZ);
                        listIdConnectToUp.Add(tap.Id);
                        teeConnect1 = tap;
                    }
                    else
                    {
                        _trans.RollBack();
                        _trans.Start("Roll back");
                        continue;
                    }
                }
                else
                {
                    var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, pOn);
                    var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);

                    var tee = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    teeConnect1 = tee;
                    listIdConnectToUp.Add(tee.Id);
                }

                var connCheck = Common.GetConnectorClosestTo(newPipeZ, pOn);
                XYZ newPointTee = connCheck.Origin + normal3d * ft;
                XYZ newPointSprinklerDown = new XYZ(locationSprinklerDown.X, locationSprinklerDown.Y, newPointTee.Z);

                var newPipeConnect1 = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeType.Id, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), newPointTee, newPointSprinklerDown);

                newPipeConnect1.LookupParameter("Diameter").Set(dFt);
                listIdConnectToDown.Add(newPipeConnect1.Id);

                pipe2 = CreateTeeByPipe(newPipeZ, newPointTee, true);
                if (pipe2 != null)
                {
                    listIdConnectToUp.Add(pipe2.Id);
                    listPipeCut.Add(pipe2);
                }

                if (pipe2 == null)
                {
                    var tap = CreateTapPipe(newPipeZ, newPipeConnect1);
                    listIdConnectToDown.Add(tap.Id);
                    teeConnect2 = tap;
                }
                else
                {
                    var conStart = Common.GetConnectorClosestTo(newPipeZ, newPointTee);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, newPointTee);
                    var conTee = Common.GetConnectorClosestTo(newPipeConnect1, newPointTee);

                    var tee = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    teeConnect2 = tee;
                    listIdConnectToDown.Add(tee.Id);

                    if (_form.IsCheckedNipple)
                    {
                        nipple = ConnectNipple(_form.fmlNipple, tee);
                        listIdConnectToUp.Add(nipple.Id);
                        ConnectorUtils.GetConnectorOppositeNearestClosedTo(nipple.MEPModel.ConnectorManager, teeConnect1.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                        if (con1 != null && con2 != null)
                        {
                            con1.ConnectTo(con2);
                        }
                    }
                }

                var connector1 = Common.GetConnectorClosestTo(newPipeConnect1, newPointSprinklerDown);
                var pipeEnd = Pipe.Create(Global.UIDoc.Document, pipeType.Id, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), connector1, connectorSprinklerDown);

                var conStart1 = Common.GetConnectorClosestTo(newPipeConnect1, newPointSprinklerDown);
                var conEnd1 = Common.GetConnectorClosestTo(pipeEnd, newPointSprinklerDown);
                var elbow = _doc.Create.NewElbowFitting(conStart1, conEnd1);

                listIdConnectToDown.Add(pipeEnd.Id);
                listIdConnectToDown.Add(elbow.Id);

                if (listPipeCut.Count != 0)
                    listPipeId.AddRange(listPipeCut.Where(x => x.IsValidObject).Select(x => x.Id).ToList());

                var listUp = listIdConnectToUp.Select(x => x.ToInt().ToString()).ToList();
                var listDown = listIdConnectToDown.Select(x => x.ToInt().ToString()).ToList();
                listUp.AddRange(listDown);
                listUp.Add("IsDown");
                listUp.Add(pair.Value.Id.ToString());
                CmdDeleteSprinker.CreateSchema(pair.Key, listUp);
                CmdDeleteSprinker.CreateSchema(pair.Value, listDown);

                if (_form.IsCheckedVerticalTeeOffset)
                    SetLenghtMEPCurve(Global.UIDoc.Document, teeConnect1, teeConnect2, ft);
                else
                    SetLenghtMEPCurve(Global.UIDoc.Document, teeConnect1, teeConnect2);
            }
        }

        private static void ProcessType2(Pipe pipeMain, Dictionary<FamilyInstance, FamilyInstance> dictSprinkler)
        {
            FamilyInstance teeConnect1 = null;
            FamilyInstance teeConnect2 = null;

            var curve = (pipeMain.Location as LocationCurve).Curve;
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            double radius = 100; //mm
            if (_form.IsCheckedVerticalTeeOffset)
                radius = _form.VerticalTeeOffset;
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            double distanceL1 = UnitUtils.ConvertToInternalUnits(_form.HorizontalPipeLengthL, DisplayUnitType.DUT_MILLIMETERS);
#else
            double distanceL1 = UnitUtils.ConvertToInternalUnits(_form.HorizontalPipeLengthL, UnitTypeId.Millimeters);
#endif
            var ft = Common.mmToFT * radius;
            ElementId pipeTypeId = pipeMain.GetTypeId();
            ElementId systemTypeId = pipeMain.MEPSystem.GetTypeId();
            List<ElementId> listPipeId = new List<ElementId>() { pipeMain.Id };
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, DisplayUnitType.DUT_MILLIMETERS);
#else
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, UnitTypeId.Millimeters);
#endif
            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);

            foreach (var pair in dictSprinkler)
            {
                HashSet<ElementId> listIdConnectToUp = new HashSet<ElementId>();
                HashSet<ElementId> listIdConnectToDown = new HashSet<ElementId>();

                FamilyInstance sprinklerUp = pair.Key;
                FamilyInstance sprinklerDown = pair.Value;

                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;

                var connectorSprinklerUp = Common.GetConnectorNotConnnected(sprinklerUp.MEPModel.ConnectorManager);
                var connectorSprinklerDown = Common.GetConnectorNotConnnected(sprinklerDown.MEPModel.ConnectorManager);

                XYZ locationConnectorUp = connectorSprinklerUp.Origin;
                XYZ locationConnectorDown = connectorSprinklerDown.Origin;

                XYZ locationSprinklerUp2D = Common.To2D(locationSprinklerUp);
                XYZ locationSprinklerDown2D = Common.To2D(locationSprinklerDown);

                bool split = false;
                var listPipeCheck = new List<Element>() { pipeMain };
                if (listPipeId.Count != 0)
                    listPipeCheck.AddRange(listPipeId.Select(x => _doc.GetElement(x)));

                var pipe = ProcessPipes(listPipeCheck, locationSprinklerUp, out split);
                var isEnd = CheckPipeIsEnd(pipe, locationSprinklerUp);

                List<Pipe> listPipeCut = new List<Pipe>();
                listPipeCut.Add(pipe);

                XYZ pOn = Common.ProjectPointOnPlane(planeCheck, locationConnectorUp);

                Line locationCurvePipe = (pipe.Location as LocationCurve).Curve as Line;
                Line locationCurvePipeUnbound = Line.CreateUnbound(locationCurvePipe.GetEndPoint(0), locationCurvePipe.Direction);

                Line lineUnbound = Line.CreateUnbound(locationConnectorUp, normal3d);
                SetComparisonResult intersec;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = lineUnbound.Intersect(locationCurvePipeUnbound, CurveIntersectResultOption.Detailed);
    intersec = intersectResult.Result;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray arr = new IntersectionResultArray();
                intersec = lineUnbound.Intersect(locationCurvePipeUnbound, out arr);
#endif

                if (intersec == SetComparisonResult.Overlap)
                {
                    // Lấy điểm giao cắt
#if Debug_2027 || Release_2027 || Release_2026
    pOn = intersectResult.GetOverlaps()[0].Point;
#else
                    pOn = arr.get_Item(0).XYZPoint;
#endif
                }
                else
                {
                    // Hàm Project vẫn giữ nguyên cú pháp trên mọi phiên bản
                    pOn = locationCurvePipeUnbound.Project(locationConnectorUp).XYZPoint;
                }

                Pipe pipe2 = null;
                if (!split)
                {
                    if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                    else
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);
                }
                else
                {
                    if (!_form.IsCheckedTeeC5 && !isEnd)
                    {
                        if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                        else
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);
                    }
                    else
                    {
                        pipe2 = CreateTeeByPipe(pipe, pOn, false);

                        if (pipe2 != null)
                            listPipeCut.Add(pipe2);
                    }
                }

                //Create pipe Z
                var newPipeZ = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pOn, locationConnectorUp);

                newPipeZ.LookupParameter("Diameter").Set(dFt);
                var pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                if (pipeType != null)
                    newPipeZ.PipeType = pipeType;

                var conCheck = Common.GetConnectorClosestTo(newPipeZ, locationConnectorUp);
                conCheck.ConnectTo(connectorSprinklerUp);

                listIdConnectToUp.Add(newPipeZ.Id);

                if (pipe2 == null)
                {
                    if (!isEnd && !_form.IsCheckedTeeC5)
                    {
                        var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                        var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);
                        var elbow1 = _doc.Create.NewElbowFitting(conStart, conTee);
                        listIdConnectToUp.Add(elbow1.Id);
                        teeConnect1 = elbow1;
                    }
                    else if (split)
                    {
                        var tap = CreateTapPipe(pipe, newPipeZ);
                        listIdConnectToUp.Add(tap.Id);
                        teeConnect1 = tap;
                    }
                    else
                    {
                        _trans.RollBack();
                        _trans.Start("Roll back");
                        continue;
                    }
                }
                else
                {
                    var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, pOn);
                    var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);

                    var tee1 = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    listIdConnectToUp.Add(tee1.Id);
                    teeConnect1 = tee1;
                }

                var connCheck = Common.GetConnectorClosestTo(newPipeZ, pOn);
                XYZ newPointTee = connCheck.Origin + normal3d * ft;
                XYZ newPointSprinklerDown = new XYZ(locationSprinklerDown.X, locationSprinklerDown.Y, newPointTee.Z);

                if ((newPointSprinklerDown - newPointTee).Normalize().IsAlmostEqualTo(XYZ.Zero) || Common.IsParallel((newPointSprinklerDown - newPointTee).Normalize(), line2d.Direction))
                    newPointSprinklerDown = newPointTee + normal2d * distanceL1;
                else
                    newPointSprinklerDown = newPointTee + (newPointSprinklerDown - newPointTee).Normalize() * distanceL1;

                var newPipeConnect1 = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeType.Id, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), newPointTee, newPointSprinklerDown);

                newPipeConnect1.LookupParameter("Diameter").Set(dFt);

                pipe2 = CreateTeeByPipe(newPipeZ, newPointTee, true);
                if (pipe2 != null)
                {
                    listIdConnectToUp.Add(pipe2.Id);
                    listPipeCut.Add(pipe2);
                }

                if (pipe2 != null)
                {
                    var conStart = Common.GetConnectorClosestTo(newPipeZ, newPointTee);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, newPointTee);
                    var conTee = Common.GetConnectorClosestTo(newPipeConnect1, newPointTee);

                    var tee = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    listIdConnectToDown.Add(tee.Id);

                    teeConnect2 = tee;
                    if (_form.IsCheckedNipple)
                    {
                        var nipple = ConnectNipple(_form.fmlNipple, tee);
                        listIdConnectToUp.Add(nipple.Id);
                        ConnectorUtils.GetConnectorOppositeNearestClosedTo(nipple.MEPModel.ConnectorManager, teeConnect1.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                        if (con1 != null && con2 != null)
                        {
                            con1.ConnectTo(con2);
                        }
                    }
                }

                listIdConnectToDown.Add(newPipeConnect1.Id);

                FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                var connector1 = Common.GetConnectorClosestTo(newPipeConnect1, newPointSprinklerDown);
                List<XYZ> pnts = new List<XYZ>();
                pnts.Add(connector1.Origin);
                pnts.Add(connectorSprinklerDown.Origin);

                var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                //Set the diameter of flex.
                var splinkerDiameter = connectorSprinklerDown.Radius * 2;

                if (_form.IsSprinklerSize)
                    flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                else
                    flexPipe.LookupParameter("Diameter").Set(dFt);

                flexPipe.StartTangent = connector1.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                flexPipe.EndTangent = connectorSprinklerDown.CoordinateSystem.BasisZ.Negate();

                var c1_flexPipe = Common.GetConnectorClosestTo(flexPipe, connector1.Origin);
                var c2_flexPipe = Common.GetConnectorClosestTo(flexPipe, connectorSprinklerDown.Origin);

                FamilyInstance union = null;
                if (!_form.IsSprinklerSize)
                    union = Global.UIDoc.Document.Create.NewUnionFitting(c1_flexPipe, connector1);
                else
                    union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, connectorSprinklerDown);

                if (!_form.IsSprinklerSize)
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;

                    int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions);

                    List<RoutingPreferenceRule> lstRule = new List<RoutingPreferenceRule>();
                    for (int i = 0; i < numberOfRule; i++)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Transitions, i);

                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        lstRule.Add(rule);
                    }

                    lstRule = lstRule.OrderBy(x => (x.GetCriterion(0) as PrimarySizeCriterion).MinimumSize).ToList();

                    FamilySymbol familySymbol = null;
                    for (int i = 0; i < lstRule.Count; i++)
                    {
                        PrimarySizeCriterion primarySizeCriterion = lstRule[i].GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (dFt >= minimumSize && dFt <= maximumSize)
                        {
                            familySymbol = Global.UIDoc.Document.GetElement(lstRule[i].MEPPartId) as FamilySymbol;
                            break;
                        }
                    }

                    var paraTransition = flexPipeType.get_Parameter(BuiltInParameter.RBS_CURVETYPE_DEFAULT_TRANSITION_PARAM);
                    ElementId olId = null;
                    if (paraTransition != null && !paraTransition.IsReadOnly)
                    {
                        olId = paraTransition.AsElementId();

                        paraTransition.Set(familySymbol.Id);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                        if (a != null)
                            a.Symbol = familySymbol;

                        paraTransition.Set(olId);
                    }
                }
                else
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    if (!con1.IsConnectedTo(con2))
                        con1.ConnectTo(con2);

                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, newPipeConnect1.ConnectorManager, out con1, out con2);

                    RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;

                    int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions);

                    List<RoutingPreferenceRule> lstRule = new List<RoutingPreferenceRule>();
                    for (int i = 0; i < numberOfRule; i++)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Transitions, i);

                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        lstRule.Add(rule);
                    }

                    lstRule = lstRule.OrderBy(x => (x.GetCriterion(0) as PrimarySizeCriterion).MinimumSize).ToList();

                    FamilySymbol familySymbol = null;
                    for (int i = 0; i < lstRule.Count; i++)
                    {
                        PrimarySizeCriterion primarySizeCriterion = lstRule[i].GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (dFt >= minimumSize && dFt <= maximumSize)
                        {
                            familySymbol = Global.UIDoc.Document.GetElement(lstRule[i].MEPPartId) as FamilySymbol;
                            break;
                        }
                    }

                    var paraTransition = flexPipeType.get_Parameter(BuiltInParameter.RBS_CURVETYPE_DEFAULT_TRANSITION_PARAM);
                    ElementId olId = null;
                    if (paraTransition != null && !paraTransition.IsReadOnly)
                    {
                        olId = paraTransition.AsElementId();

                        paraTransition.Set(familySymbol.Id);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                        if (a != null)
                            a.Symbol = familySymbol;

                        listIdConnectToDown.Add(a.Id);

                        paraTransition.Set(olId);
                    }
                }

                listIdConnectToDown.Add(flexPipe.Id);
                if (union != null)
                    listIdConnectToDown.Add(union.Id);

                listPipeId.AddRange(listPipeCut.Where(x => x.IsValidObject).Select(x => x.Id).ToList());

                var listUp = listIdConnectToUp.Select(x => x.ToInt().ToString()).ToList();
                var listDown = listIdConnectToDown.Select(x => x.ToInt().ToString()).ToList();
                listUp.AddRange(listDown);
                listUp.Add("IsDown");
                listUp.Add(pair.Value.Id.ToString());
                CmdDeleteSprinker.CreateSchema(pair.Key, listUp);
                CmdDeleteSprinker.CreateSchema(pair.Value, listDown);

                if (_form.IsCheckedVerticalTeeOffset)
                    SetLenghtMEPCurve(Global.UIDoc.Document, teeConnect1, teeConnect2, ft);
                else
                    SetLenghtMEPCurve(Global.UIDoc.Document, teeConnect1, teeConnect2);
            }
        }

        private static void ProcessType3(Pipe pipeMain, Dictionary<FamilyInstance, FamilyInstance> dictSprinkler)
        {
            FamilyInstance teeConnect1 = null;
            FamilyInstance teeConnect2 = null;

            var curve = (pipeMain.Location as LocationCurve).Curve;
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            double radius = 100; //mm
            if (_form.IsCheckedVerticalTeeOffset)
                radius = _form.VerticalTeeOffset;

            var ft = Common.mmToFT * radius;
            ElementId pipeTypeId = pipeMain.GetTypeId();
            ElementId systemTypeId = pipeMain.MEPSystem.GetTypeId();
            List<ElementId> listPipeId = new List<ElementId>() { pipeMain.Id };
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, DisplayUnitType.DUT_MILLIMETERS);
            double distanceL1 = UnitUtils.ConvertToInternalUnits(_form.HorizontalPipeLengthL, DisplayUnitType.DUT_MILLIMETERS);
            double distanceL2 = UnitUtils.ConvertToInternalUnits(_form.ExtendPipeLengthL1, DisplayUnitType.DUT_MILLIMETERS);
#else
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, UnitTypeId.Millimeters);
            double distanceL1 = UnitUtils.ConvertToInternalUnits(_form.HorizontalPipeLengthL, UnitTypeId.Millimeters);
            double distanceL2 = UnitUtils.ConvertToInternalUnits(_form.ExtendPipeLengthL1, UnitTypeId.Millimeters);
#endif
            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);

            foreach (var pair in dictSprinkler)
            {
                HashSet<ElementId> listIdConnectToUp = new HashSet<ElementId>();
                HashSet<ElementId> listIdConnectToDown = new HashSet<ElementId>();

                FamilyInstance sprinklerUp = pair.Key;
                FamilyInstance sprinklerDown = pair.Value;

                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;

                var connectorSprinklerUp = Common.GetConnectorNotConnnected(sprinklerUp.MEPModel.ConnectorManager);
                var connectorSprinklerDown = Common.GetConnectorNotConnnected(sprinklerDown.MEPModel.ConnectorManager);

                XYZ locationConnectorUp = connectorSprinklerUp.Origin;
                XYZ locationConnectorDown = connectorSprinklerDown.Origin;

                XYZ locationSprinklerUp2D = Common.To2D(locationSprinklerUp);
                XYZ locationSprinklerDown2D = Common.To2D(locationSprinklerDown);

                bool split = false;
                var listPipeCheck = new List<Element>() { pipeMain };
                if (listPipeId.Count != 0)
                    listPipeCheck.AddRange(listPipeId.Select(x => _doc.GetElement(x)));

                var pipe = ProcessPipes(listPipeCheck, locationSprinklerUp, out split);

                List<Pipe> listPipeCut = new List<Pipe>();
                listPipeCut.Add(pipe);

                var isEnd = CheckPipeIsEnd(pipe, locationSprinklerUp);

                XYZ pOn = Common.ProjectPointOnPlane(planeCheck, locationConnectorUp);

                Line locationCurvePipe = (pipe.Location as LocationCurve).Curve as Line;

                Pipe pipe2 = null;
                if (!split)
                {
                    if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                    else
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);
                }
                else
                {
                    if (!_form.IsCheckedTeeC5 && !isEnd)
                    {
                        if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                        else
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);
                    }
                    else
                    {
                        pipe2 = CreateTeeByPipe(pipe, pOn, false);

                        if (pipe2 != null)
                            listPipeCut.Add(pipe2);
                    }
                }

                //Create pipe Z
                var newPipeZ = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pOn, locationConnectorUp);

                newPipeZ.LookupParameter("Diameter").Set(dFt);
                var pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                if (pipeType != null)
                    newPipeZ.PipeType = pipeType;

                var conCheck = Common.GetConnectorClosestTo(newPipeZ, locationConnectorUp);
                conCheck.ConnectTo(connectorSprinklerUp);

                listIdConnectToUp.Add(newPipeZ.Id);

                if (pipe2 == null)
                {
                    if (!isEnd && !_form.IsCheckedTeeC5)
                    {
                        var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                        var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);
                        var elbow = _doc.Create.NewElbowFitting(conStart, conTee);
                        listIdConnectToUp.Add(elbow.Id);
                        teeConnect1 = elbow;
                    }
                    else if (split)
                    {
                        var tap = CreateTapPipe(pipe, newPipeZ);
                        listIdConnectToUp.Add(tap.Id);
                        teeConnect1 = tap;
                    }
                    else
                    {
                        _trans.RollBack();
                        _trans.Start("Roll back");
                        continue;
                    }
                }
                else
                {
                    var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, pOn);
                    var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);

                    var tee = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    listIdConnectToUp.Add(tee.Id);
                    teeConnect1 = tee;
                }

                var connCheck = Common.GetConnectorClosestTo(newPipeZ, pOn);
                XYZ newPointTee = connCheck.Origin + normal3d * ft;
                XYZ newPointSprinklerDown = new XYZ(locationSprinklerDown.X, locationSprinklerDown.Y, newPointTee.Z);

                if ((newPointSprinklerDown - newPointTee).Normalize().IsAlmostEqualTo(XYZ.Zero) || Common.IsParallel((newPointSprinklerDown - newPointTee).Normalize(), line2d.Direction))
                    newPointSprinklerDown = newPointTee + normal2d * distanceL1;
                else
                    newPointSprinklerDown = newPointTee + (newPointSprinklerDown - newPointTee).Normalize() * distanceL1;

                var newPipeConnect1 = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeType.Id, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), newPointTee, newPointSprinklerDown);

                newPipeConnect1.LookupParameter("Diameter").Set(dFt);

                pipe2 = CreateTeeByPipe(newPipeZ, newPointTee, true);
                if (pipe2 != null)
                {
                    listIdConnectToUp.Add(pipe2.Id);
                    listPipeCut.Add(pipe2);
                }

                if (pipe2 != null)
                {
                    var conStart = Common.GetConnectorClosestTo(newPipeZ, newPointTee);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, newPointTee);
                    var conTee = Common.GetConnectorClosestTo(newPipeConnect1, newPointTee);

                    var tee = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    listIdConnectToDown.Add(tee.Id);
                    listIdConnectToDown.Add(newPipeConnect1.Id);
                    teeConnect2 = tee;
                    if (_form.IsCheckedNipple)
                    {
                        var nipple = ConnectNipple(_form.fmlNipple, tee);
                        listIdConnectToUp.Add(nipple.Id);
                        ConnectorUtils.GetConnectorOppositeNearestClosedTo(nipple.MEPModel.ConnectorManager, teeConnect1.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                        if (con1 != null && con2 != null)
                        {
                            con1.ConnectTo(con2);
                        }
                    }
                }

                if (_form.IsCheckedVerticalTeeOffset)
                    SetLenghtMEPCurve(Global.UIDoc.Document, teeConnect1, teeConnect2, ft);
                else
                    SetLenghtMEPCurve(Global.UIDoc.Document, teeConnect1, teeConnect2);

                ElementTransformUtils.MoveElement(Global.UIDoc.Document, teeConnect2.Id, (teeConnect2.Location as LocationPoint).Point - (teeConnect2.Location as LocationPoint).Point + normal2d * 0.2);
                ElementTransformUtils.MoveElement(Global.UIDoc.Document, teeConnect2.Id, (teeConnect2.Location as LocationPoint).Point - (teeConnect2.Location as LocationPoint).Point + normal2d.Negate() * 0.2);

                FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                var connector1 = Common.GetConnectorClosestTo(newPipeConnect1, newPointSprinklerDown);
                var newPointEnd = connector1.Origin + XYZ.BasisZ.Negate() * distanceL2;

                var newPipeEnd = Pipe.Create(Global.UIDoc.Document, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), connector1, newPointEnd);

                newPipeEnd.LookupParameter("Diameter").Set(dFt);
                newPipeEnd.PipeType = pipeType;

                var conStart1 = Common.GetConnectorClosestTo(newPipeConnect1, connector1.Origin);
                var conEnd1 = Common.GetConnectorClosestTo(newPipeEnd, connector1.Origin);
                var elbow1 = _doc.Create.NewElbowFitting(conStart1, conEnd1);

                var connectorEnd = Common.GetConnectorClosestTo(newPipeEnd, newPointEnd);

                List<XYZ> pnts = new List<XYZ>();
                pnts.Add(connectorEnd.Origin);
                pnts.Add(connectorSprinklerDown.Origin);

                var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                //Set the diameter of flex.
                var splinkerDiameter = connectorSprinklerDown.Radius * 2;

                if (_form.IsSprinklerSize)
                    flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                else
                    flexPipe.LookupParameter("Diameter").Set(dFt);

                flexPipe.StartTangent = connectorEnd.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                flexPipe.EndTangent = connectorSprinklerDown.CoordinateSystem.BasisZ.Negate();

                var c1_flexPipe = Common.GetConnectorClosestTo(flexPipe, connectorEnd.Origin);
                var c2_flexPipe = Common.GetConnectorClosestTo(flexPipe, connectorSprinklerDown.Origin);

                FamilyInstance union = null;
                if (!_form.IsSprinklerSize)
                    union = Global.UIDoc.Document.Create.NewUnionFitting(c1_flexPipe, connectorEnd);
                else
                    union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, connectorSprinklerDown);

                if (!_form.IsSprinklerSize)
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;

                    int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions);

                    List<RoutingPreferenceRule> lstRule = new List<RoutingPreferenceRule>();
                    for (int i = 0; i < numberOfRule; i++)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Transitions, i);

                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        lstRule.Add(rule);
                    }

                    lstRule = lstRule.OrderBy(x => (x.GetCriterion(0) as PrimarySizeCriterion).MinimumSize).ToList();

                    FamilySymbol familySymbol = null;
                    for (int i = 0; i < lstRule.Count; i++)
                    {
                        PrimarySizeCriterion primarySizeCriterion = lstRule[i].GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (dFt >= minimumSize && dFt <= maximumSize)
                        {
                            familySymbol = Global.UIDoc.Document.GetElement(lstRule[i].MEPPartId) as FamilySymbol;
                            break;
                        }
                    }

                    var paraTransition = flexPipeType.get_Parameter(BuiltInParameter.RBS_CURVETYPE_DEFAULT_TRANSITION_PARAM);
                    ElementId olId = null;
                    if (paraTransition != null && !paraTransition.IsReadOnly)
                    {
                        olId = paraTransition.AsElementId();

                        paraTransition.Set(familySymbol.Id);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                        listIdConnectToDown.Add(a.Id);
                        if (a != null)
                            a.Symbol = familySymbol;

                        paraTransition.Set(olId);
                    }
                }
                else
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    if (!con1.IsConnectedTo(con2))
                        con1.ConnectTo(con2);

                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, newPipeEnd.ConnectorManager, out con1, out con2);

                    RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;

                    int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions);

                    List<RoutingPreferenceRule> lstRule = new List<RoutingPreferenceRule>();
                    for (int i = 0; i < numberOfRule; i++)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Transitions, i);

                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        lstRule.Add(rule);
                    }

                    lstRule = lstRule.OrderBy(x => (x.GetCriterion(0) as PrimarySizeCriterion).MinimumSize).ToList();

                    FamilySymbol familySymbol = null;
                    for (int i = 0; i < lstRule.Count; i++)
                    {
                        PrimarySizeCriterion primarySizeCriterion = lstRule[i].GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (dFt >= minimumSize && dFt <= maximumSize)
                        {
                            familySymbol = Global.UIDoc.Document.GetElement(lstRule[i].MEPPartId) as FamilySymbol;
                            break;
                        }
                    }

                    var paraTransition = flexPipeType.get_Parameter(BuiltInParameter.RBS_CURVETYPE_DEFAULT_TRANSITION_PARAM);
                    ElementId olId = null;
                    if (paraTransition != null && !paraTransition.IsReadOnly)
                    {
                        olId = paraTransition.AsElementId();

                        paraTransition.Set(familySymbol.Id);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                        listIdConnectToDown.Add(a.Id);
                        if (a != null)
                            a.Symbol = familySymbol;

                        paraTransition.Set(olId);
                    }
                }

                listIdConnectToDown.Add(newPipeEnd.Id);
                listIdConnectToDown.Add(flexPipe.Id);
                listIdConnectToDown.Add(elbow1.Id);
                if (union != null)
                    listIdConnectToDown.Add(union.Id);

                listPipeId.AddRange(listPipeCut.Where(x => x.IsValidObject).Select(x => x.Id).ToList());

                var listUp = listIdConnectToUp.Select(x => x.ToInt().ToString()).ToList();
                var listDown = listIdConnectToDown.Select(x => x.ToInt().ToString()).ToList();
                listUp.AddRange(listDown);
                listUp.Add("IsDown");
                listUp.Add(pair.Value.Id.ToString());
                CmdDeleteSprinker.CreateSchema(pair.Key, listUp);
                CmdDeleteSprinker.CreateSchema(pair.Value, listDown);
            }
        }

        private static void ProcessType4(Pipe pipeMain, Dictionary<FamilyInstance, FamilyInstance> dictSprinkler)
        {
            FamilyInstance teeConnect1 = null;
            FamilyInstance teeConnect2 = null;

            var curve = (pipeMain.Location as LocationCurve).Curve;
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            double radius = 100; //mm
            var ft = Common.mmToFT * radius;
            XYZ normal = Line.CreateBound(Common.To2D(p0), Common.To2D(p1)).Direction.CrossProduct(XYZ.BasisZ);
            ElementId pipeTypeId = pipeMain.GetTypeId();
            ElementId systemTypeId = pipeMain.MEPSystem.GetTypeId();
            List<ElementId> listPipeId = new List<ElementId>() { pipeMain.Id };
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, DisplayUnitType.DUT_MILLIMETERS);
            double distanceL1 = UnitUtils.ConvertToInternalUnits(_form.HorizontalPipeLengthL, DisplayUnitType.DUT_MILLIMETERS);
            double distanceL2 = UnitUtils.ConvertToInternalUnits(_form.ExtendPipeLengthL1, DisplayUnitType.DUT_MILLIMETERS);
#else
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, UnitTypeId.Millimeters);
            double distanceL1 = UnitUtils.ConvertToInternalUnits(_form.HorizontalPipeLengthL, UnitTypeId.Millimeters);
            double distanceL2 = UnitUtils.ConvertToInternalUnits(_form.ExtendPipeLengthL1, UnitTypeId.Millimeters);
#endif
            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);
            var fitting1 = Common.GetFittingConnected(pipeMain.ConnectorManager.Lookup(0));
            var fitting2 = Common.GetFittingConnected(pipeMain.ConnectorManager.Lookup(1));

            XYZ p1Dir = null;
            XYZ p2Dir = null;

            ElementId levelId = pipeMain.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();

            if (fitting1 != null && fitting1.MEPModel != null && fitting1.MEPModel is MechanicalFitting mechanical1 && mechanical1.PartType == PartType.Transition)
            {
                p1Dir = (fitting1.Location as LocationPoint).Point;
                p1Dir = Common.ProjectPointOnPlane(planeCheck, p1Dir);
            }

            if (fitting2 != null && fitting2.MEPModel != null && fitting2.MEPModel is MechanicalFitting mechanical2 && mechanical2.PartType == PartType.Transition)
            {
                p2Dir = (fitting2.Location as LocationPoint).Point;
                p2Dir = Common.ProjectPointOnPlane(planeCheck, p2Dir);
            }

            foreach (var pair in dictSprinkler)
            {
                HashSet<ElementId> listIdConnectToUp = new HashSet<ElementId>();
                HashSet<ElementId> listIdConnectToDown = new HashSet<ElementId>();

                FamilyInstance sprinklerUp = pair.Key;
                FamilyInstance sprinklerDown = pair.Value;

                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;

                var connectorSprinklerUp = Common.GetConnectorNotConnnected(sprinklerUp.MEPModel.ConnectorManager);
                var connectorSprinklerDown = Common.GetConnectorNotConnnected(sprinklerDown.MEPModel.ConnectorManager);

                XYZ locationConnectorUp = connectorSprinklerUp.Origin;
                XYZ locationConnectorDown = connectorSprinklerDown.Origin;

                XYZ locationSprinklerUp2D = Common.To2D(locationSprinklerUp);
                XYZ locationSprinklerDown2D = Common.To2D(locationSprinklerDown);

                bool split = false;
                var listPipeCheck = new List<Element>() { pipeMain };
                if (listPipeId.Count != 0)
                    listPipeCheck.AddRange(listPipeId.Select(x => _doc.GetElement(x)));

                var pipe = ProcessPipes(listPipeCheck, locationSprinklerUp, out split);

                var locationP = pipe.GetCurve();
                XYZ p0Pipe = locationP.GetEndPoint(0);
                XYZ p1Pipe = locationP.GetEndPoint(1);

                List<Pipe> listPipeCut = new List<Pipe>();

                var isEnd = CheckPipeIsEnd(pipe, locationSprinklerUp);

                XYZ pOn = Common.ProjectPointOnPlane(planeCheck, locationConnectorUp);

                Line locationCurvePipe = (pipe.Location as LocationCurve).Curve as Line;
                Line locationCurvePipeUnbound = Line.CreateUnbound(locationCurvePipe.GetEndPoint(0), locationCurvePipe.Direction);

                Pipe pipe2 = null;
                if (!split)
                {
                    if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                    else
                        (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);

                    curve = (pipe.Location as LocationCurve).Curve;
                }
                else
                {
                    if (!_form.IsCheckedTeeC5 && !isEnd)
                    {
                        if (locationCurvePipe.GetEndPoint(0).DistanceTo(pOn) < locationCurvePipe.GetEndPoint(1).DistanceTo(pOn))
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, locationCurvePipe.GetEndPoint(1));
                        else
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pOn);

                        curve = (pipe.Location as LocationCurve).Curve;
                    }
                    else
                    {
                        pipe2 = CreateTeeByPipe(pipe, pOn, false);

                        if (pipe2 != null)
                            listPipeCut.Add(pipe2);
                    }
                }

                //Create pipe Z
                var newPipeZ = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pOn, locationConnectorUp);

                newPipeZ.LookupParameter("Diameter").Set(dFt);
                var pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                if (pipeType != null)
                    newPipeZ.PipeType = pipeType;

                listIdConnectToUp.Add(newPipeZ.Id);

                var conCheck = Common.GetConnectorClosestTo(newPipeZ, locationConnectorUp);
                conCheck.ConnectTo(connectorSprinklerUp);

                FamilyInstance teeFitting = null;
                FamilyInstance elbowFitting = null;

                if (pipe2 == null)
                {
                    if (!isEnd && !_form.IsCheckedTeeC5)
                    {
                        var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                        var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);
                        elbowFitting = _doc.Create.NewElbowFitting(conStart, conTee);
                        listIdConnectToUp.Add(elbowFitting.Id);
                        teeConnect1 = elbowFitting;
                    }
                    else if (split)
                    {
                        var tap = CreateTapPipe(pipe, newPipeZ);
                        listIdConnectToUp.Add(tap.Id);
                        teeConnect1 = tap;
                    }
                    else
                    {
                        _trans.RollBack();
                        _trans.Start("Roll back");
                        continue;
                    }
                }
                else
                {
                    var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, pOn);
                    var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);

                    teeFitting = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    listIdConnectToUp.Add(teeFitting.Id);
                    teeConnect1 = teeFitting;
                }
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
                double offset2 = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);
#else
                double offset2 = UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters);
#endif
                Pipe newPipeH = null;
                XYZ newPointSprinklerH = null;
                if (pipe2 != null)
                {
                    Common.GetInformationConectorWye(teeFitting, null, out Connector conS, out Connector conE, out Connector conT);
                    XYZ center = (conS.Origin + conE.Origin) / 2;
                    XYZ dir = (curve as Line).Direction;
                    if (p1Dir != null && p2Dir != null)
                    {
                        if (center.DistanceTo(p1Dir) <= center.DistanceTo(p2Dir))
                        {
                            if (p1Dir.DistanceTo(p0Pipe) <= p1Dir.DistanceTo(p1Pipe))
                                dir = Line.CreateBound(p0Pipe, p1Pipe).Direction;
                            else
                                dir = Line.CreateBound(p1Pipe, p0Pipe).Direction;
                        }
                        else
                        {
                            if (p2Dir.DistanceTo(p0Pipe) <= p2Dir.DistanceTo(p1Pipe))
                                dir = Line.CreateBound(p0Pipe, p1Pipe).Direction;
                            else
                                dir = Line.CreateBound(p1Pipe, p0Pipe).Direction;
                        }
                    }
                    else
                    {
                        if (p1Dir != null)
                        {
                            if (p1Dir.DistanceTo(p0Pipe) <= p1Dir.DistanceTo(p1Pipe))
                                dir = Line.CreateBound(p0Pipe, p1Pipe).Direction;
                            else
                                dir = Line.CreateBound(p1Pipe, p0Pipe).Direction;
                        }

                        if (p2Dir != null)
                        {
                            if (p2Dir.DistanceTo(p0Pipe) <= p2Dir.DistanceTo(p1Pipe))
                                dir = Line.CreateBound(p0Pipe, p1Pipe).Direction;
                            else
                                dir = Line.CreateBound(p1Pipe, p0Pipe).Direction;
                        }
                    }
                    XYZ newP = center + dir * (conS.Origin.DistanceTo(conE.Origin) + offset2);
                    Plane plane = Plane.CreateByNormalAndOrigin(normal, locationSprinklerDown);
                    XYZ pointProject = Common.ProjectPointOnPlane(plane, newP);

                    if ((pointProject - newP).Normalize().IsAlmostEqualTo(XYZ.Zero) || Common.IsParallel((pointProject - newP).Normalize(), line2d.Direction))
                        newPointSprinklerH = newP + normal2d * distanceL1;
                    else
                        newPointSprinklerH = newP + (pointProject - newP).Normalize() * distanceL1;

                    newPipeH = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), newP, newPointSprinklerH);
                    newPipeH.LookupParameter("Diameter").Set(dFt);
                    newPipeH.PipeType = pipeType;
                    listIdConnectToDown.Add(newPipeH.Id);

                    var linePipe2 = (pipe2.Location as LocationCurve).Curve;

                    if (Common.IsBetween2Point(linePipe2.GetEndPoint(0), linePipe2.GetEndPoint(1), newP))
                    {
                        pipe = pipe2;
                        pipe2 = null;
                        pipe2 = CreateTeeByPipe(pipe, newP, false);

                        if (pipe2 != null)
                            listPipeCut.Add(pipe2);
                    }
                    else
                    {
                        pipe2 = null;
                        pipe2 = CreateTeeByPipe(pipe, newP, false);

                        if (pipe2 != null)
                            listPipeCut.Add(pipe2);
                    }

                    if (pipe2 == null)
                    {
                        var tap = CreateTapPipe(pipe2, newPipeH);
                        listIdConnectToDown.Add(tap.Id);
                        teeConnect2 = tap;
                    }
                    else
                    {
                        var conStart = Common.GetConnectorClosestTo(pipe, newP);
                        var conEnd = Common.GetConnectorClosestTo(pipe2, newP);
                        var conTee = Common.GetConnectorClosestTo(newPipeH, newP);

                        var teeFitting1 = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);

                        if (_form.IsCheckedNipple)
                        {
                            var nipple = ConnectNipple2(_form.fmlNipple, teeFitting, teeFitting1);
                            listIdConnectToDown.Add(nipple.Id);
                            ConnectorUtils.GetConnectorOppositeNearestClosedTo(nipple.MEPModel.ConnectorManager, teeConnect1.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                            if (con1 != null && con2 != null)
                            {
                                con1.ConnectTo(con2);
                            }
                        }
                        teeFitting = teeFitting1;
                        listIdConnectToDown.Add(teeFitting.Id);
                        teeConnect2 = teeFitting;
                    }
                }
                else
                {
                    var locationCurve = pipe.GetCurve();
                    XYZ newP = null;

                    var conCheck1 = Common.GetConnectorClosestTo(pipe, pOn);

                    XYZ dir = (curve as Line).Direction;
                    if (p1Dir != null && p2Dir != null)
                    {
                        if (pOn.DistanceTo(p1Dir) <= pOn.DistanceTo(p2Dir))
                            dir = Line.CreateBound(p1Dir, pOn).Direction;
                        else
                            dir = Line.CreateBound(p2Dir, pOn).Direction;
                    }
                    else
                    {
                        if (p1Dir != null)
                        {
                            if (p1Dir.DistanceTo(p0Pipe) <= p1Dir.DistanceTo(p1Pipe))
                                dir = Line.CreateBound(p0Pipe, p1Pipe).Direction;
                            else
                                dir = Line.CreateBound(p1Pipe, p0Pipe).Direction;
                        }

                        if (p2Dir != null)
                        {
                            if (p2Dir.DistanceTo(p0Pipe) <= p2Dir.DistanceTo(p1Pipe))
                                dir = Line.CreateBound(p0Pipe, p1Pipe).Direction;
                            else
                                dir = Line.CreateBound(p1Pipe, p0Pipe).Direction;
                        }
                    }

                    if (teeFitting != null)
                    {
                        pOn = conCheck1.Origin;
                        offset2 = offset2 * 5;
                    }
                    Common.GetInformationConectorWye(teeFitting, null, out Connector _, out Connector _, out Connector conTCheck);

                    if (conTCheck != null)
                        newP = pOn + dir * offset2;
                    else
                        newP = pOn + dir * (offset2);

                    if (!Common.IsBetween2Point(locationCurve.GetEndPoint(0), locationCurve.GetEndPoint(1), newP))
                        newP = pOn + dir.Negate() * offset2;

                    Plane plane = Plane.CreateByNormalAndOrigin(normal, locationSprinklerDown);
                    XYZ pointProject = Common.ProjectPointOnPlane(plane, newP);

                    if ((pointProject - newP).Normalize().IsAlmostEqualTo(XYZ.Zero))
                        newPointSprinklerH = newP + normal2d * distanceL1;
                    else
                        newPointSprinklerH = newP + (pointProject - newP).Normalize() * distanceL1;

                    newPipeH = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), newP, newPointSprinklerH);
                    newPipeH.LookupParameter("Diameter").Set(dFt);
                    newPipeH.PipeType = pipeType;
                    listIdConnectToDown.Add(newPipeH.Id);

                    pipe2 = null;
                    pipe2 = CreateTeeByPipe(pipe, newP, false);

                    if (pipe2 != null)
                        listPipeCut.Add(pipe2);

                    if (pipe2 == null)
                    {
                        teeFitting = CreateTapPipe(pipe, newPipeH);
                        listIdConnectToDown.Add(teeFitting.Id);
                        teeConnect2 = teeFitting;
                    }
                    else
                    {
                        var conStart = Common.GetConnectorClosestTo(pipe, newP);
                        var conEnd = Common.GetConnectorClosestTo(pipe2, newP);
                        var conTee = Common.GetConnectorClosestTo(newPipeH, newP);

                        teeFitting = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                        if (_form.IsCheckedNipple)
                        {
                            var nipple = ConnectNipple2(_form.fmlNipple, teeConnect1, teeFitting);
                            listIdConnectToDown.Add(nipple.Id);
                            //ConnectorUtils.GetConnectorOppositeNearestClosedTo(nipple.MEPModel.ConnectorManager, teeConnect1.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                            //if (con1 != null && con2 != null)
                            //{
                            //    con1.ConnectTo(con2);
                            //}
                        }
                        listIdConnectToDown.Add(teeFitting.Id);
                        teeConnect2 = teeFitting;
                    }
                }

                XYZ newPointSprinklerDown = newPointSprinklerH + XYZ.BasisZ.Negate() * distanceL2;
                var newPipeDown = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, levelId, newPointSprinklerH, newPointSprinklerDown);
                newPipeDown.LookupParameter("Diameter").Set(dFt);
                newPipeDown.PipeType = pipeType;

                var connector1 = Common.GetConnectorClosestTo(newPipeH, newPointSprinklerH);
                var connector2 = Common.GetConnectorClosestTo(newPipeDown, newPointSprinklerH);
                var elbow = _doc.Create.NewElbowFitting(connector1, connector2);

                var connectorEnd = Common.GetConnectorClosestTo(newPipeDown, newPointSprinklerDown);

                FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                List<XYZ> pnts = new List<XYZ>();
                pnts.Add(connectorEnd.Origin);
                pnts.Add(connectorSprinklerDown.Origin);

                var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                //Set the diameter of flex.
                var splinkerDiameter = connectorSprinklerDown.Radius * 2;

                if (_form.IsSprinklerSize)
                    flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                else
                    flexPipe.LookupParameter("Diameter").Set(dFt);

                flexPipe.StartTangent = connectorEnd.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                flexPipe.EndTangent = connectorSprinklerDown.CoordinateSystem.BasisZ.Negate();

                var c1_flexPipe = Common.GetConnectorClosestTo(flexPipe, connectorEnd.Origin);
                var c2_flexPipe = Common.GetConnectorClosestTo(flexPipe, connectorSprinklerDown.Origin);

                c1_flexPipe.ConnectTo(connectorEnd);
                FamilyInstance union = null;
                if (!_form.IsSprinklerSize)
                    union = Global.UIDoc.Document.Create.NewUnionFitting(c1_flexPipe, connectorEnd);
                else
                    union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, connectorSprinklerDown);

                if (!_form.IsSprinklerSize)
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;

                    int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions);

                    List<RoutingPreferenceRule> lstRule = new List<RoutingPreferenceRule>();
                    for (int i = 0; i < numberOfRule; i++)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Transitions, i);

                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        lstRule.Add(rule);
                    }

                    lstRule = lstRule.OrderBy(x => (x.GetCriterion(0) as PrimarySizeCriterion).MinimumSize).ToList();

                    FamilySymbol familySymbol = null;
                    for (int i = 0; i < lstRule.Count; i++)
                    {
                        PrimarySizeCriterion primarySizeCriterion = lstRule[i].GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (dFt >= minimumSize && dFt <= maximumSize)
                        {
                            familySymbol = Global.UIDoc.Document.GetElement(lstRule[i].MEPPartId) as FamilySymbol;
                            break;
                        }
                    }

                    var paraTransition = flexPipeType.get_Parameter(BuiltInParameter.RBS_CURVETYPE_DEFAULT_TRANSITION_PARAM);
                    ElementId olId = null;
                    if (paraTransition != null && !paraTransition.IsReadOnly)
                    {
                        olId = paraTransition.AsElementId();

                        paraTransition.Set(familySymbol.Id);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                        if (a != null)
                            a.Symbol = familySymbol;
                        listIdConnectToDown.Add(a.Id);

                        paraTransition.Set(olId);
                    }
                }
                else
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    if (!con1.IsConnectedTo(con2))
                        con1.ConnectTo(con2);

                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, newPipeDown.ConnectorManager, out con1, out con2);

                    RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;

                    int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions);

                    List<RoutingPreferenceRule> lstRule = new List<RoutingPreferenceRule>();
                    for (int i = 0; i < numberOfRule; i++)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Transitions, i);

                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        lstRule.Add(rule);
                    }

                    lstRule = lstRule.OrderBy(x => (x.GetCriterion(0) as PrimarySizeCriterion).MinimumSize).ToList();

                    FamilySymbol familySymbol = null;
                    for (int i = 0; i < lstRule.Count; i++)
                    {
                        PrimarySizeCriterion primarySizeCriterion = lstRule[i].GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (dFt >= minimumSize && dFt <= maximumSize)
                        {
                            familySymbol = Global.UIDoc.Document.GetElement(lstRule[i].MEPPartId) as FamilySymbol;
                            break;
                        }
                    }

                    var paraTransition = flexPipeType.get_Parameter(BuiltInParameter.RBS_CURVETYPE_DEFAULT_TRANSITION_PARAM);
                    ElementId olId = null;
                    if (paraTransition != null && !paraTransition.IsReadOnly)
                    {
                        olId = paraTransition.AsElementId();

                        paraTransition.Set(familySymbol.Id);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                        if (a != null)
                            a.Symbol = familySymbol;
                        listIdConnectToDown.Add(a.Id);

                        paraTransition.Set(olId);
                    }
                }
                listIdConnectToDown.Add(newPipeDown.Id);
                listIdConnectToDown.Add(flexPipe.Id);
                if (union != null)
                    listIdConnectToDown.Add(union.Id);
                listIdConnectToDown.Add(elbow.Id);

                listPipeId.AddRange(listPipeCut.Where(x => x.IsValidObject).Select(x => x.Id).ToList());

                var listUp = listIdConnectToUp.Select(x => x.ToInt().ToString()).ToList();
                var listDown = listIdConnectToDown.Select(x => x.ToInt().ToString()).ToList();
                CmdDeleteSprinker.CreateSchema(pair.Key, listUp);
                CmdDeleteSprinker.CreateSchema(pair.Value, listDown);

                SetLenghtMEPCurve(Global.UIDoc.Document, teeConnect1, teeConnect2);
            }
        }

        private static void ProcessType5(Pipe pipeMain, Dictionary<FamilyInstance, FamilyInstance> dictSprinkler)
        {
            var curve = (pipeMain.Location as LocationCurve).Curve;
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            double radius = 100; //mm
            if (_form.IsCheckedVerticalTeeOffset)
            {
                radius = _form.VerticalTeeOffset;
            }

            var ft = Common.mmToFT * radius;
            ElementId pipeTypeId = pipeMain.GetTypeId();
            ElementId systemTypeId = pipeMain.MEPSystem.GetTypeId();
            List<ElementId> listPipeId = new List<ElementId>() { pipeMain.Id };
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, DisplayUnitType.DUT_MILLIMETERS);
#else
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, UnitTypeId.Millimeters);
#endif

            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);

            foreach (var pair in dictSprinkler)
            {
                HashSet<ElementId> listIdConnectToUp = new HashSet<ElementId>();
                HashSet<ElementId> listIdConnectToDown = new HashSet<ElementId>();

                FamilyInstance sprinklerUp = pair.Key;
                FamilyInstance sprinklerDown = pair.Value;

                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;

                var connectorSprinklerUp = Common.GetConnectorNotConnnected(sprinklerUp.MEPModel.ConnectorManager);
                var connectorSprinklerDown = Common.GetConnectorNotConnnected(sprinklerDown.MEPModel.ConnectorManager);

                XYZ locationConnectorUp = connectorSprinklerUp.Origin;
                XYZ locationConnectorDown = connectorSprinklerDown.Origin;

                XYZ locationSprinklerUp2D = Common.To2D(locationSprinklerUp);
                XYZ locationSprinklerDown2D = Common.To2D(locationSprinklerDown);

                bool split = false;
                var listPipeCheck = new List<Element>() { pipeMain };
                if (listPipeId.Count != 0)
                    listPipeCheck.AddRange(listPipeId.Select(x => _doc.GetElement(x)));

                var pipe = ProcessPipes(listPipeCheck, locationSprinklerUp, out split);

                List<Pipe> listPipeCut = new List<Pipe>();
                listPipeCut.Add(pipe);

                var isEnd = CheckPipeIsEnd(pipe, locationSprinklerUp);
                if (isEnd == true)
                    return;

                XYZ pOn = Common.ProjectPointOnPlane(planeCheck, locationConnectorUp);

                Line locationCurvePipe = (pipe.Location as LocationCurve).Curve as Line;
                Line locationCurvePipeUnbound = Line.CreateUnbound(locationCurvePipe.GetEndPoint(0), locationCurvePipe.Direction);
                Line locationCurveSprinklerUnbound = Line.CreateUnbound(locationSprinklerDown2D, XYZ.BasisZ);
                SetComparisonResult intersec;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = locationCurvePipeUnbound.Intersect(locationCurveSprinklerUnbound, CurveIntersectResultOption.Detailed);
    intersec = intersectResult.Result;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray arrr;
                intersec = locationCurvePipeUnbound.Intersect(locationCurveSprinklerUnbound, out arrr);
#endif

                if (intersec != SetComparisonResult.Overlap)
                    return;

                // Trích xuất điểm giao cắt ra một biến chung để code bên dưới gọn gàng hơn
#if Debug_2027 || Release_2027 || Release_2026
    var pntIntersec = intersectResult.GetOverlaps()[0].Point;
#else
                var pntIntersec = arrr.get_Item(0).XYZPoint;
#endif

                // Logic so sánh và cập nhật lại LocationCurve của bạn
                if (locationCurvePipe.GetEndPoint(0).DistanceTo(pntIntersec) <= locationCurvePipe.GetEndPoint(1).DistanceTo(pntIntersec))
                {
                    (pipe.Location as LocationCurve).Curve = Line.CreateBound(pntIntersec, locationCurvePipe.GetEndPoint(1));
                }
                else
                {
                    (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pntIntersec);
                }

                Pipe pipe2 = null;

                pipe2 = CreateTeeByPipe(pipe, pOn, false);

                if (pipe2 != null)
                    listPipeCut.Add(pipe2);

                //Create pipe Z
                var newPipeZ = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pOn, locationConnectorUp);

                newPipeZ.LookupParameter("Diameter").Set(dFt);
                var pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                if (pipeType != null)
                    newPipeZ.PipeType = pipeType;

                var conCheck = Common.GetConnectorClosestTo(newPipeZ, locationConnectorUp);
                conCheck.ConnectTo(connectorSprinklerUp);

                listIdConnectToUp.Add(newPipeZ.Id);

                if (pipe2 == null)
                {
                    if (split || pipeMain.PipeType.RoutingPreferenceManager.PreferredJunctionType == PreferredJunctionType.Tap)
                    {
                        var elem = CreateTapPipe(pipe, newPipeZ);
                        listIdConnectToUp.Add(elem.Id);
                    }
                    else
                    {
                        _trans.RollBack();
                        _trans.Start("Roll back");
                        continue;
                    }
                }
                else
                {
                    var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, pOn);
                    var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);

                    var tee = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    listIdConnectToUp.Add(tee.Id);
                }

                var newPipeConnect1 = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeType.Id, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pntIntersec, connectorSprinklerDown.Origin);

                newPipeConnect1.LookupParameter("Diameter").Set(dFt);
                listIdConnectToDown.Add(newPipeConnect1.Id);

                Connector conStart1 = null;
                Connector conEnd1 = null;

                conStart1 = Common.GetConnectorClosestTo(newPipeConnect1, pntIntersec);
                if (pipe2 == null)
                    conEnd1 = Common.GetConnectorClosestTo(pipe, pntIntersec);
                else
                {
                    if (pipe2.GetCurve().GetEndPoint(0).DistanceTo(pntIntersec) <= pipe.GetCurve().GetEndPoint(0).DistanceTo(pntIntersec))
                        conEnd1 = Common.GetConnectorClosestTo(pipe2, pntIntersec);
                    else
                        conEnd1 = Common.GetConnectorClosestTo(pipe, pntIntersec);
                }

                var elbow = _doc.Create.NewElbowFitting(conStart1, conEnd1);

                listIdConnectToDown.Add(elbow.Id);

                listPipeId.AddRange(listPipeCut.Select(x => x.Id).ToList());

                conEnd1 = Common.GetConnectorClosestTo(newPipeConnect1, connectorSprinklerDown.Origin);
                conEnd1.ConnectTo(connectorSprinklerDown);

                var listUp = listIdConnectToUp.Select(x => x.ToInt().ToString()).ToList();
                var listDown = listIdConnectToDown.Select(x => x.ToInt().ToString()).ToList();
                listUp.AddRange(listDown);
                listUp.Add("IsDown");
                listUp.Add(pair.Value.Id.ToString());
                CmdDeleteSprinker.CreateSchema(pair.Key, listUp);
                CmdDeleteSprinker.CreateSchema(pair.Value, listDown);
            }
        }

        private static void ProcessType6(Pipe pipeMain, Dictionary<FamilyInstance, FamilyInstance> dictSprinkler)
        {
            var curve = (pipeMain.Location as LocationCurve).Curve;
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            double radius = 100; //mm
            if (_form.IsCheckedVerticalTeeOffset)
            {
                radius = _form.VerticalTeeOffset;
            }

            var ft = Common.mmToFT * radius;
            ElementId pipeTypeId = pipeMain.GetTypeId();
            ElementId systemTypeId = pipeMain.MEPSystem.GetTypeId();
            List<ElementId> listPipeId = new List<ElementId>() { pipeMain.Id };
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, DisplayUnitType.DUT_MILLIMETERS);
#else
            var dFt = UnitUtils.ConvertToInternalUnits(_form.PipeSizeC5, UnitTypeId.Millimeters);
#endif

            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);
            Plane planeCheck2d = Plane.CreateByNormalAndOrigin(normal2d, p0);

            foreach (var pair in dictSprinkler)
            {
                HashSet<ElementId> listIdConnectToUp = new HashSet<ElementId>();
                HashSet<ElementId> listIdConnectToDown = new HashSet<ElementId>();

                FamilyInstance sprinklerUp = pair.Key;
                FamilyInstance sprinklerDown = pair.Value;

                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;

                var connectorSprinklerUp = Common.GetConnectorNotConnnected(sprinklerUp.MEPModel.ConnectorManager);
                var connectorSprinklerDown = Common.GetConnectorNotConnnected(sprinklerDown.MEPModel.ConnectorManager);

                XYZ locationConnectorUp = connectorSprinklerUp.Origin;
                XYZ locationConnectorDown = connectorSprinklerDown.Origin;

                XYZ locationSprinklerUp2D = Common.To2D(locationSprinklerUp);
                XYZ locationSprinklerDown2D = Common.To2D(locationSprinklerDown);

                bool split = false;
                var listPipeCheck = new List<Element>() { pipeMain };
                if (listPipeId.Count != 0)
                    listPipeCheck.AddRange(listPipeId.Select(x => _doc.GetElement(x)));

                var pipe = ProcessPipes(listPipeCheck, locationSprinklerUp, out split);

                List<Pipe> listPipeCut = new List<Pipe>();
                listPipeCut.Add(pipe);

                var isEnd = CheckPipeIsEnd(pipe, locationSprinklerUp);
                if (isEnd == true)
                    return;

                XYZ pOn = Common.ProjectPointOnPlane(planeCheck, locationConnectorUp);

                Line locationCurvePipe = (pipe.Location as LocationCurve).Curve as Line;
                Line locationCurvePipeUnbound = Line.CreateUnbound(locationCurvePipe.GetEndPoint(0), locationCurvePipe.Direction);
                Line locationCurveSprinklerUnbound = Line.CreateUnbound(Common.ProjectPointOnPlane(planeCheck2d, locationSprinklerDown2D), XYZ.BasisZ);

                SetComparisonResult intersec;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = locationCurvePipeUnbound.Intersect(locationCurveSprinklerUnbound, CurveIntersectResultOption.Detailed);
    intersec = intersectResult.Result;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray arrr;
                intersec = locationCurvePipeUnbound.Intersect(locationCurveSprinklerUnbound, out arrr);
#endif

                if (intersec != SetComparisonResult.Overlap)
                    return;

                // Trích xuất điểm giao cắt ra một biến chung để code bên dưới gọn gàng hơn
#if Debug_2027 || Release_2027 || Release_2026
    var pntIntersec = intersectResult.GetOverlaps()[0].Point;
#else
                var pntIntersec = arrr.get_Item(0).XYZPoint;
#endif

                // Logic so sánh và cập nhật lại LocationCurve của bạn
                if (locationCurvePipe.GetEndPoint(0).DistanceTo(pntIntersec) <= locationCurvePipe.GetEndPoint(1).DistanceTo(pntIntersec))
                {
                    (pipe.Location as LocationCurve).Curve = Line.CreateBound(pntIntersec, locationCurvePipe.GetEndPoint(1));
                }
                else
                {
                    (pipe.Location as LocationCurve).Curve = Line.CreateBound(locationCurvePipe.GetEndPoint(0), pntIntersec);
                }

                Pipe pipe2 = null;

                pipe2 = CreateTeeByPipe(pipe, pOn, false);

                if (pipe2 != null)
                    listPipeCut.Add(pipe2);

                //Create pipe Z
                var newPipeZ = Pipe.Create(Global.UIDoc.Document, systemTypeId, pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pOn, locationConnectorUp);

                newPipeZ.LookupParameter("Diameter").Set(dFt);
                var pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                if (pipeType != null)
                    newPipeZ.PipeType = pipeType;

                var conCheck = Common.GetConnectorClosestTo(newPipeZ, locationConnectorUp);
                conCheck.ConnectTo(connectorSprinklerUp);

                listIdConnectToUp.Add(newPipeZ.Id);

                if (pipe2 == null)
                {
                    if (split || pipeMain.PipeType.RoutingPreferenceManager.PreferredJunctionType == PreferredJunctionType.Tap)
                    {
                        var elem = CreateTapPipe(pipe, newPipeZ);
                        listIdConnectToUp.Add(elem.Id);
                    }
                    else
                    {
                        _trans.RollBack();
                        _trans.Start("Roll back");
                        continue;
                    }
                }
                else
                {
                    var conStart = Common.GetConnectorClosestTo(pipe, pOn);
                    var conEnd = Common.GetConnectorClosestTo(pipe2, pOn);
                    var conTee = Common.GetConnectorClosestTo(newPipeZ, pOn);

                    var tee = _doc.Create.NewTeeFitting(conStart, conEnd, conTee);
                    listIdConnectToUp.Add(tee.Id);
                }

                var c1 = ConnectorUtils.GetConnectorNotConnnected(sprinklerDown.MEPModel.ConnectorManager);
                Connector c2 = null;
                if (pipe2 != null)
                {
                    var c2Check1 = Common.GetConnectorClosestTo(pipe2, pntIntersec);
                    var c2Check2 = Common.GetConnectorClosestTo(pipe, pntIntersec);

                    if (c2Check1.Origin.DistanceTo(pntIntersec) < c2Check2.Origin.DistanceTo(pntIntersec))
                        c2 = c2Check1;
                    else
                        c2 = c2Check2;
                }
                else
                {
                    c2 = Common.GetConnectorClosestTo(pipe, pntIntersec);
                }

                FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                List<XYZ> pnts = new List<XYZ>();
                pnts.Add(c1.Origin);
                pnts.Add(c2.Origin);

                var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);
                listIdConnectToUp.Add(flexPipe.Id);
                //Set the diameter of flex.
                var splinkerDiameter = sprinklerDown.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault().Radius * 2;

                if (_form.IsSprinklerSize)
                    flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                else
                    flexPipe.LookupParameter("Diameter").Set(conCheck.Radius * 2);

                flexPipe.StartTangent = c1.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                flexPipe.EndTangent = c2.CoordinateSystem.BasisZ.Negate();

                var c1_flexPipe = Common.GetConnectorClosestTo(flexPipe, c1.Origin);
                var c2_flexPipe = Common.GetConnectorClosestTo(flexPipe, c2.Origin);

                FamilyInstance union = null;
                if (!_form.IsSprinklerSize)
                    union = Global.UIDoc.Document.Create.NewUnionFitting(c2_flexPipe, c2);
                else
                    union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, c2);
                if (union != null)
                    listIdConnectToUp.Add(union.Id);

                if (!_form.IsSprinklerSize)
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;

                    int numberOfRule = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions);

                    List<RoutingPreferenceRule> lstRule = new List<RoutingPreferenceRule>();
                    for (int i = 0; i < numberOfRule; i++)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(RoutingPreferenceRuleGroupType.Transitions, i);

                        PrimarySizeCriterion primarySizeCriterion = rule.GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        lstRule.Add(rule);
                    }

                    lstRule = lstRule.OrderBy(x => (x.GetCriterion(0) as PrimarySizeCriterion).MinimumSize).ToList();

                    FamilySymbol familySymbol = null;
                    for (int i = 0; i < lstRule.Count; i++)
                    {
                        PrimarySizeCriterion primarySizeCriterion = lstRule[i].GetCriterion(0) as PrimarySizeCriterion;

                        double minimumSize = primarySizeCriterion.MinimumSize;

                        double maximumSize = primarySizeCriterion.MaximumSize;

                        if (dFt >= minimumSize && dFt <= maximumSize)
                        {
                            familySymbol = Global.UIDoc.Document.GetElement(lstRule[i].MEPPartId) as FamilySymbol;
                            break;
                        }
                    }

                    var paraTransition = flexPipeType.get_Parameter(BuiltInParameter.RBS_CURVETYPE_DEFAULT_TRANSITION_PARAM);
                    ElementId olId = null;
                    if (paraTransition != null && !paraTransition.IsReadOnly)
                    {
                        olId = paraTransition.AsElementId();
                        paraTransition.Set(familySymbol.Id);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                        if (a != null)
                            a.Symbol = familySymbol;

                        paraTransition.Set(olId);
                    }
                }
                else
                {
                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinklerDown.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                    if (!con1.IsConnectedTo(con2))
                    {
                        con1.ConnectTo(con2);
                    }
                }

                Global.UIDoc.Document.Regenerate();

                var listUp = listIdConnectToUp.Select(x => x.ToInt().ToString()).ToList();
                listUp.Add("Type6");
                listUp.Add(pair.Key.Id.ToString());
                CmdDeleteSprinker.CreateSchema(pair.Key, listUp);
                CmdDeleteSprinker.CreateSchema(pair.Value, listUp);
            }
        }

        public static FamilyInstance ConnectNipple2(FamilySymbol fmlNipple, FamilyInstance fitting1, FamilyInstance fitting2)
        {
            Common.GetInformationConectorWye(fitting1, null, out Connector conS1, out Connector conE1, out Connector _);
            Common.GetInformationConectorWye(fitting2, null, out Connector conS2, out Connector conE2, out Connector _);

            Connector conCheck1 = null;
            Connector conCheck2 = null;

            Pipe newPipeZ = null;
            var pipeCheck1 = Common.GetPipeConnected(conS1);
            var pipeCheck2 = Common.GetPipeConnected(conE1);

            if (Common.GetPipeConnected(conS2).Id == pipeCheck1.Id || Common.GetPipeConnected(conE2).Id == pipeCheck1.Id)
                newPipeZ = pipeCheck1;
            else
                newPipeZ = pipeCheck2;

            if (Common.GetPipeConnected(conS1).Id == newPipeZ.Id)
                conCheck1 = conS1;
            else
                conCheck1 = conE1;

            if (Common.GetPipeConnected(conS2).Id == newPipeZ.Id)
                conCheck2 = conS2;
            else
                conCheck2 = conE2;

            Line lineLocation = (newPipeZ.Location as LocationCurve).Curve as Line;
            var nipple = Global.UIDoc.Document.Create.NewFamilyInstance((lineLocation.GetEndPoint(0) + lineLocation.GetEndPoint(1)) / 2, fmlNipple, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            if (nipple != null)
            {
                var paraDia = newPipeZ.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                nipple.LookupParameter("Nominal Diameter").Set(paraDia);
            }

            Common.RotateLineC2(Global.UIDoc.Document, nipple, lineLocation);
            var connectors = Common.ToList(nipple.MEPModel.ConnectorManager.Connectors);

            var consPipe = Common.ToList(newPipeZ.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).FirstOrDefault();
            var fittingCheck = Common.GetFittingConnected(consPipe);
            var conElbow1 = Common.GetConnectorClosestTo(newPipeZ, conCheck2.Origin);

            var cConnectElbow1 = Common.GetConnectorClosestTo(nipple, conCheck1.Origin);

            if (cConnectElbow1 != null)
                conCheck1.ConnectTo(cConnectElbow1);

            Connector cNipple1 = Common.GetConnectorClosestTo(nipple, conCheck2.Origin);

            var vector = conElbow1.Origin - cNipple1.Origin;

            ElementTransformUtils.MoveElement(Global.UIDoc.Document, nipple.Id, vector.Normalize() * conElbow1.Origin.DistanceTo(cNipple1.Origin));
            cNipple1.ConnectTo(conCheck2);
            Global.UIDoc.Document.Delete(newPipeZ.Id);

            return nipple;
        }

        private static bool SetLenghtMEPCurve(Document doc, FamilyInstance fittingPin, FamilyInstance fittingMove, double distance = 10 / 304.8)
        {
            if (fittingPin != null && fittingMove != null)
            {
                ConnectorUtils.GetConnectorOppositeNearestClosedTo(fittingPin.MEPModel.ConnectorManager, fittingMove.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                if (con1 != null && con2 != null)
                {
                    XYZ point = Common.GetPointOnVector(con1.Origin, con1.CoordinateSystem.BasisZ, distance);
                    ElementTransformUtils.MoveElement(doc, fittingMove.Id, point - con2.Origin);
                }
                else
                {
                    Common.GetInformationConectorWye(fittingPin, null, out Connector conS, out Connector conE, out Connector conT);

                    Common.GetInformationConectorWye(fittingMove, null, out Connector conSE, out Connector conEE, out Connector _);

                    Pipe pipeCheck1 = null;
                    Pipe pipeCheck2 = null;
                    Pipe pipeCheck3 = null;
                    Pipe pipeCheck4 = null;
                    Line line = null;

                    pipeCheck1 = Common.GetPipeConnected(conS);
                    pipeCheck2 = Common.GetPipeConnected(conE);
                    pipeCheck3 = Common.GetPipeConnected(conSE);
                    pipeCheck4 = Common.GetPipeConnected(conEE);

                    if (pipeCheck1.Id == pipeCheck3.Id)
                    {
                        con1 = conS;
                        con2 = conSE;
                        line = pipeCheck1.GetCurve() as Line;
                    }
                    else if (pipeCheck1.Id == pipeCheck4.Id)
                    {
                        con1 = conS;
                        con2 = conEE;
                        line = pipeCheck1.GetCurve() as Line;
                    }
                    else if (pipeCheck2.Id == pipeCheck3.Id)
                    {
                        con1 = conE;
                        con2 = conSE;
                        line = pipeCheck2.GetCurve() as Line;
                    }
                    else if (pipeCheck2.Id == pipeCheck4.Id)
                    {
                        con1 = conE;
                        con2 = conEE;
                        line = pipeCheck2.GetCurve() as Line;
                    }

                    XYZ point = Common.GetPointOnVector(con1.Origin, con1.CoordinateSystem.BasisZ, distance);
                    ElementTransformUtils.MoveElement(doc, fittingMove.Id, point - con2.Origin);
                }
            }

            return false;
        }

        public static FamilyInstance ConnectNipple(FamilySymbol fmlNipple, FamilyInstance fitting)
        {
            var conElbow = Common.ToList(fitting.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).FirstOrDefault();

            var conBottom = Common.ToList(fitting.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).LastOrDefault();
            var newPipeZ = Common.GetPipeConnected(conElbow);
            Line lineLocation = (newPipeZ.Location as LocationCurve).Curve as Line;
            var nipple = Global.UIDoc.Document.Create.NewFamilyInstance((lineLocation.GetEndPoint(0) + lineLocation.GetEndPoint(1)) / 2, fmlNipple, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            if (nipple != null)
            {
                var paraDia = newPipeZ.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                nipple.LookupParameter("Nominal Diameter").Set(paraDia);
            }

            Common.RotateLineC2(Global.UIDoc.Document, nipple, lineLocation);
            var connectors = Common.ToList(nipple.MEPModel.ConnectorManager.Connectors);

            var consPipe = Common.ToList(newPipeZ.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).FirstOrDefault();
            var fittingCheck = Common.GetFittingConnected(consPipe);
            var conElbow1 = Common.ToList(fittingCheck.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).LastOrDefault();

            Global.UIDoc.Document.Delete(newPipeZ.Id);

            var cConnectElbow = Common.GetConnectorClosestTo(nipple, conElbow.Origin);

            if (cConnectElbow != null)
                conElbow.ConnectTo(cConnectElbow);

            Connector cNipple1 = connectors.OrderBy(x => x.Origin.Z).FirstOrDefault();

            var vector = conElbow1.Origin - cNipple1.Origin;

            ElementTransformUtils.MoveElement(Global.UIDoc.Document, nipple.Id, vector.Normalize() * conElbow1.Origin.DistanceTo(cNipple1.Origin));

            return nipple;
        }

        public static FamilyInstance CreateTapPipe(MEPCurve mepCurveSplit1, MEPCurve mepCurveSplit2)
        {
            try
            {
                FamilyInstance familyInstance = null;

                var locationCurve1 = mepCurveSplit1.GetCurve();
                var line1 = locationCurve1 as Line;

                var locationCurve2 = mepCurveSplit2.GetCurve();
                var line2 = locationCurve2 as Line;

                var p10 = line2.GetEndPoint(0);
                var p11 = line2.GetEndPoint(1);

                var inter1 = locationCurve1.Project(p10);
                var inter2 = locationCurve1.Project(p11);

                if (inter1 == null || inter2 == null)
                    return null;

                var d1 = inter1.XYZPoint.DistanceTo(p10);
                var d2 = inter2.XYZPoint.DistanceTo(p11);

                double radius = mepCurveSplit1.Diameter / 2;
                Connector con = null;
                if (d1 < d2)
                    con = Common.GetConnectorClosestTo(mepCurveSplit2, p10);
                else
                    con = Common.GetConnectorClosestTo(mepCurveSplit2, p11);

                familyInstance = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);

                return familyInstance;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Handler Process Pipes
        /// </summary>
        /// <param name="pippes"></param>
        /// <param name="sprinkle_point"></param>
        /// <param name="bSplit"></param>
        /// <returns></returns>
        private static Pipe ProcessPipes(List<Element> pipes, XYZ sprinkle_point, out bool bSplit)
        {
            bSplit = false;

            if (pipes.Count == 0)
                return null;

            Pipe pipeNear = null;

            Dictionary<Pipe, double> keyValuePairs = new Dictionary<Pipe, double>();

            foreach (Element pipe in pipes)
            {
                if (pipe as Pipe == null)

                    continue;
                var curve = (pipe.Location as LocationCurve).Curve;

                if (curve is Line == false)
                    continue;

                var d = (curve as Line).Direction;

                if (Common.IsParallel(d, XYZ.BasisZ, 0))
                    continue;

                var project = curve.Project(sprinkle_point);
                if (project == null)
                    continue;

                var p = project.XYZPoint;

                var disFml = Common.PointTo2D(p).DistanceTo(Common.PointTo2D(sprinkle_point));

                keyValuePairs.Add(pipe as Pipe, disFml);
            }

            var min = keyValuePairs.Min(x => x.Value);

            var pairs = keyValuePairs.FirstOrDefault(x => x.Value == min);
            if (pairs.Key != null)
            {
                pipeNear = pairs.Key;
                var curve = (pipeNear.Location as LocationCurve).Curve;

                var d = (curve as Line).Direction;

                var project = curve.Project(sprinkle_point);

                var p = project.XYZPoint;

                if (p.DistanceTo(curve.GetEndPoint(0)) != 0 && p.DistanceTo(curve.GetEndPoint(1)) != 0)
                    bSplit = true;
            }

            return pipeNear;
        }

        private static Dictionary<FamilyInstance, FamilyInstance> GetDictSprinklerByPipe(Pipe pipe, List<FamilyInstance> listSprinkler)
        {
            Dictionary<FamilyInstance, FamilyInstance> val = new Dictionary<FamilyInstance, FamilyInstance>();
            List<FamilyInstance> listSprinklerUp = new List<FamilyInstance>();
            List<FamilyInstance> listSprinklerDown = new List<FamilyInstance>();
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            double offset = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);
            double offset3 = UnitUtils.ConvertToInternalUnits(_form.EndPointSprinklerDistance, DisplayUnitType.DUT_MILLIMETERS);
            double offset2 = UnitUtils.ConvertToInternalUnits(_form.MainPipeSprinklerDistance, DisplayUnitType.DUT_MILLIMETERS);
#else
            double offset = UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters);
            double offset3 = UnitUtils.ConvertToInternalUnits(_form.EndPointSprinklerDistance, UnitTypeId.Millimeters);
            double offset2 = UnitUtils.ConvertToInternalUnits(_form.MainPipeSprinklerDistance, UnitTypeId.Millimeters);
#endif

            Line locationPipe = (pipe.Location as LocationCurve).Curve as Line;
            XYZ p1 = Common.To2D(locationPipe.GetEndPoint(0));
            XYZ p2 = Common.To2D(locationPipe.GetEndPoint(1));
            Line locationPipe2D = Line.CreateBound(p1, p2);

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)))
                p1 += locationPipe2D.Direction.Negate() * offset3;

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
                p2 += locationPipe2D.Direction * offset3;

            locationPipe2D = Line.CreateBound(p1, p2);

            XYZ normal = locationPipe2D.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = locationPipe.Direction.CrossProduct(normal);

            Plane planePipe = Plane.CreateByNormalAndOrigin(normal, p1);
            Plane planePipe3d = Plane.CreateByNormalAndOrigin(normal3d, p1);

            var connectorsCheck = Common.GetListConnectorNotConnnected(pipe.ConnectorManager);

            foreach (var sprinkler in listSprinkler)
            {
                XYZ locationSprinkler = (sprinkler.Location as LocationPoint).Point;
                XYZ locationSprinkler2D = Common.To2D(locationSprinkler);

                var check = Common.GetConnectorNotConnnected(sprinkler.MEPModel.ConnectorManager);
                if (check == null)
                    continue;

                Line lineUnbound = Line.CreateUnbound(locationSprinkler2D, normal);
                SetComparisonResult intersec;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = lineUnbound.Intersect(locationPipe2D, CurveIntersectResultOption.Detailed);
    intersec = intersectResult.Result;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                intersec = lineUnbound.Intersect(locationPipe2D);
#endif

                if (intersec != SetComparisonResult.Overlap)
                    continue;

                XYZ pointProject = Common.ProjectPointOnPlane(planePipe, locationSprinkler);

                if (locationSprinkler.DistanceTo(pointProject) > offset2)
                    continue;

                if (locationSprinkler.DistanceTo(pointProject) > offset)
                {
                    if (locationSprinkler.Z <= locationPipe.GetEndPoint(0).Z)
                        listSprinklerDown.Add(sprinkler);
                }
                else
                {
                    if (locationSprinkler.Z <= locationPipe.GetEndPoint(0).Z)
                        listSprinklerDown.Add(sprinkler);
                    else
                    {
                        listSprinklerUp.Add(sprinkler);
                        (sprinkler.Location as LocationPoint).Point = pointProject;
                    }
                }
            }

            foreach (var sprinklerUp in listSprinklerUp)
            {
                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                Plane plane = Plane.CreateByNormalAndOrigin(locationPipe2D.Direction, locationSprinklerUp);

                double distance = double.MaxValue;
                foreach (var sprinklerDown in listSprinklerDown)
                {
                    XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;
                    XYZ pointProject = Common.ProjectPointOnPlane(plane, locationSprinklerDown);
                    if (Common.To2D(locationSprinklerUp).DistanceTo(Common.To2D(locationSprinklerDown)) <= distance)
                        distance = Common.To2D(locationSprinklerUp).DistanceTo(Common.To2D(locationSprinklerDown));
                }

                var a = listSprinklerDown.FirstOrDefault(x => Common.To2D(locationSprinklerUp).DistanceTo(Common.To2D((x.Location as LocationPoint).Point)) == distance);
                if (a == null)
                    return val;

                listSprinklerDown.Remove(a);
                val.Add(sprinklerUp, a);
                XYZ pointProjec1 = Common.ProjectPointOnPlane(plane, (a.Location as LocationPoint).Point);
                (a.Location as LocationPoint).Point = pointProjec1;
            }

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)))
            {
                val = val.OrderBy(x => Common.To2D((x.Key.Location as LocationPoint).Point).DistanceTo(Common.To2D(locationPipe.GetEndPoint(0)))).ToDictionary(x => x.Key, y => y.Value);
            }
            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
            {
                val = val.OrderBy(x => Common.To2D((x.Key.Location as LocationPoint).Point).DistanceTo(Common.To2D(locationPipe.GetEndPoint(1)))).ToDictionary(x => x.Key, y => y.Value);
            }
            return val;
        }

        private static Dictionary<FamilyInstance, FamilyInstance> GetDictSprinklerByPipe2(Pipe pipe, List<FamilyInstance> listSprinkler, bool isType5)
        {
            Dictionary<FamilyInstance, FamilyInstance> val = new Dictionary<FamilyInstance, FamilyInstance>();
            List<FamilyInstance> listSprinklerUp = new List<FamilyInstance>();
            List<FamilyInstance> listSprinklerDown = new List<FamilyInstance>();
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            double offset = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);
            double offset3 = UnitUtils.ConvertToInternalUnits(_form.EndPointSprinklerDistance, DisplayUnitType.DUT_MILLIMETERS);
            double offset2 = UnitUtils.ConvertToInternalUnits(_form.MainPipeSprinklerDistance, DisplayUnitType.DUT_MILLIMETERS);
            double offset200 = UnitUtils.ConvertToInternalUnits(200, DisplayUnitType.DUT_MILLIMETERS);
#else
            double offset = UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters);
            double offset3 = UnitUtils.ConvertToInternalUnits(_form.EndPointSprinklerDistance, UnitTypeId.Millimeters);
            double offset2 = UnitUtils.ConvertToInternalUnits(_form.MainPipeSprinklerDistance, UnitTypeId.Millimeters);
            double offset200 = UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters);
#endif

            Line locationPipe = (pipe.Location as LocationCurve).Curve as Line;
            XYZ p1 = Common.To2D(locationPipe.GetEndPoint(0));
            XYZ p2 = Common.To2D(locationPipe.GetEndPoint(1));
            Line locationPipe2D = Line.CreateBound(p1, p2);
            Line locationPipe2DOrigin = Line.CreateBound(p1, p2);

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)))
                p1 += locationPipe2D.Direction.Negate() * offset3;

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
                p2 += locationPipe2D.Direction * offset3;

            locationPipe2D = Line.CreateBound(p1, p2);

            XYZ normal = locationPipe2D.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3d = locationPipe.Direction.CrossProduct(normal);

            Plane planePipe = Plane.CreateByNormalAndOrigin(normal, p1);
            Plane planePipe3d = Plane.CreateByNormalAndOrigin(normal3d, p1);

            var connectorsCheck = Common.GetListConnectorNotConnnected(pipe.ConnectorManager);

            foreach (var sprinkler in listSprinkler)
            {
                XYZ locationSprinkler = (sprinkler.Location as LocationPoint).Point;
                XYZ locationSprinkler2D = Common.To2D(locationSprinkler);

                var check = Common.GetConnectorNotConnnected(sprinkler.MEPModel.ConnectorManager);
                if (check == null)
                    continue;

                Line lineUnbound = Line.CreateUnbound(locationSprinkler2D, normal);
                SetComparisonResult intersec;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = lineUnbound.Intersect(locationPipe2D, CurveIntersectResultOption.Detailed);
    intersec = intersectResult.Result;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                intersec = lineUnbound.Intersect(locationPipe2D);
#endif

                if (intersec != SetComparisonResult.Overlap)
                    continue;

                XYZ pointProject = Common.ProjectPointOnPlane(planePipe, locationSprinkler);

                if (locationSprinkler.DistanceTo(pointProject) > offset2)
                    continue;

                if (locationSprinkler.DistanceTo(pointProject) > offset)
                {
                    if (locationSprinkler.Z <= locationPipe.GetEndPoint(0).Z)
                        listSprinklerDown.Add(sprinkler);
                }
                else
                {
                    if (locationSprinkler.Z <= locationPipe.GetEndPoint(0).Z)
                        listSprinklerDown.Add(sprinkler);
                    else
                    {
                        listSprinklerUp.Add(sprinkler);
                        (sprinkler.Location as LocationPoint).Point = pointProject;
                    }
                }
            }

            for (int i = 0; i < listSprinklerDown.Count; i++)
            {
                var sprinklerDown = listSprinklerDown[i];
                XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;
                XYZ pointProject = Common.ProjectPointOnPlane(planePipe, locationSprinklerDown);

                if (Math.Round(Common.To2D(pointProject).DistanceTo(Common.To2D(locationPipe.GetEndPoint(0))), 2) > Math.Round(offset3, 2) && Math.Round(Common.To2D(pointProject).DistanceTo(Common.To2D(locationPipe.GetEndPoint(1))), 2) > Math.Round(offset3, 2))
                {
                    listSprinklerDown.RemoveAt(i);
                    i--;
                }
            }

            foreach (var sprinklerUp in listSprinklerUp)
            {
                XYZ locationSprinklerUp = (sprinklerUp.Location as LocationPoint).Point;
                XYZ pointProjectUp = Common.ProjectPointOnPlane(planePipe, locationSprinklerUp);

                if (Math.Round(Common.To2D(pointProjectUp).DistanceTo(Common.To2D(locationPipe.GetEndPoint(0))), 2) > Math.Round(offset3, 2) && Math.Round(Common.To2D(pointProjectUp).DistanceTo(Common.To2D(locationPipe.GetEndPoint(1))), 2) > Math.Round(offset3, 2))
                {
                    continue;
                }

                double distance = double.MaxValue;
                foreach (var sprinklerDown in listSprinklerDown)
                {
                    XYZ locationSprinklerDown = (sprinklerDown.Location as LocationPoint).Point;
                    XYZ pointProject = Common.ProjectPointOnPlane(planePipe, locationSprinklerDown);
                    if (Common.To2D(locationSprinklerUp).DistanceTo(Common.To2D(locationSprinklerDown)) <= distance)
                    {
                        distance = Common.To2D(locationSprinklerUp).DistanceTo(Common.To2D(locationSprinklerDown));
                    }
                }

                var a = listSprinklerDown.FirstOrDefault(x => Common.To2D(locationSprinklerUp).DistanceTo(Common.To2D((x.Location as LocationPoint).Point)) == distance);

                if (a == null)
                    return val;

                listSprinklerDown.Remove(a);
                val.Add(sprinklerUp, a);
                if (isType5)
                {
                    XYZ pointProjec1 = Common.ProjectPointOnPlane(planePipe, (a.Location as LocationPoint).Point);
                    (a.Location as LocationPoint).Point = pointProjec1;
                }
            }

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)))
            {
                val = val.OrderBy(x => Common.To2D((x.Key.Location as LocationPoint).Point).DistanceTo(Common.To2D(locationPipe.GetEndPoint(0)))).ToDictionary(x => x.Key, y => y.Value);
            }
            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
            {
                val = val.OrderBy(x => Common.To2D((x.Key.Location as LocationPoint).Point).DistanceTo(Common.To2D(locationPipe.GetEndPoint(1)))).ToDictionary(x => x.Key, y => y.Value);
            }
            return val;
        }

        public static Pipe CreateTeeByPipe(Pipe pipeMain, XYZ point, bool isChangeToTee)
        {
            Pipe pipe2 = null;
            var old = pipeMain.PipeType.RoutingPreferenceManager.PreferredJunctionType;
            if (GetPreferredJunctionType(pipeMain, false) == PreferredJunctionType.Tee || isChangeToTee)
                ProcessStartSidePipe(pipeMain, out pipe2, point, out bool isDauOng, true);

            if (isChangeToTee)
                pipeMain.PipeType.RoutingPreferenceManager.PreferredJunctionType = old;

            return pipe2;
        }

        /// <summary>
        /// Get Preferred Junction Type
        /// </summary>
        /// <param name="pipe"></param>
        /// <returns></returns>
        private static PreferredJunctionType GetPreferredJunctionType(Pipe pipe, bool isChangeToTee)
        {
            var pipeType = pipe.PipeType as PipeType;

            if (isChangeToTee)
                pipeType.RoutingPreferenceManager.PreferredJunctionType = PreferredJunctionType.Tee;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType;
        }

        /// <summary>
        /// Process Start Side Pipe
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="pipe2"></param>
        /// <param name="pOn"></param>
        /// <param name="flagSplit"></param>
        private static void ProcessStartSidePipe(Pipe pipe, out Pipe pipe2, XYZ pOn, out bool isDauOng, bool flagSplit = true)
        {
            var curve = (pipe.Location as LocationCurve).Curve;
            //Create plane
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            //Check co phai dau cuu hoa o gan dau cua ong ko : check trong pham vi 1m - 400mm
            double kc_mm = App.m_TwinSprinklerForm.EndPointSprinklerDistance /*1000*/;
            double km_ft = Common.mmToFT * kc_mm;

            var p02d = new XYZ(p0.X, p0.Y, 0);
            var p12d = new XYZ(p1.X, p1.Y, 0);
            var pOn2d = new XYZ(pOn.X, pOn.Y, 0);

            var d1 = p02d.DistanceTo(pOn2d);
            var d2 = p12d.DistanceTo(pOn2d);

            isDauOng = false;
            int far = -1;
            if (d1 < km_ft)
            {
                if (IsIntersect(p0) == false && !CheckPipeIsEnd(pipe, pOn) && !App.m_TwinSprinklerForm.IsCheckedTeeC5)
                {
                    isDauOng = true;
                    far = 1;
                }
            }
            else if (d2 < km_ft)
            {
                if (IsIntersect(p1) == false && !CheckPipeIsEnd(pipe, pOn) && !App.m_TwinSprinklerForm.IsCheckedTeeC5)
                {
                    isDauOng = true;
                    far = 0;
                }
            }

            Pipe pipe1 = pipe;
            pipe2 = null;

            if (flagSplit == true)
            {
                CmdFlexSprinkler.SplitPipe(pipe, pOn, out pipe1, out pipe2);
            }
        }

        /// <summary>
        /// Is Intersect
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool IsIntersect(XYZ point)
        {
            double ft = 0.001;
            var solid = Common.CreateCylindricalVolume(point, ft, ft, false);
            if (solid != null)
            {
                var fittingBuilt = new ElementId(BuiltInCategory.OST_PipeFitting);
                FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document);
                collector.OfClass(typeof(FamilyInstance));
                collector.OfCategoryId(fittingBuilt);
                collector.WherePasses(new ElementIntersectsSolidFilter(solid));

                if (collector.GetElementCount() != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckPipeIsEnd(Pipe pipe, XYZ point)
        {
            bool retVal = true;
            Dictionary<FamilyInstance, double> keyValuePairs = new Dictionary<FamilyInstance, double>();
            XYZ pointCheck = XYZ.Zero;
            Connector con = null;
            if (point.DistanceTo(pipe.GetCurve().GetEndPoint(0)) <= point.DistanceTo(pipe.GetCurve().GetEndPoint(1)))
                con = Common.GetConnectorClosestTo(pipe, pipe.GetCurve().GetEndPoint(0));
            else
                con = Common.GetConnectorClosestTo(pipe, pipe.GetCurve().GetEndPoint(1));

            if (!con.IsConnected)
                retVal = false;
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
            double radius = UnitUtils.ConvertToInternalUnits(10, DisplayUnitType.DUT_MILLIMETERS);
#else
            double radius = UnitUtils.ConvertToInternalUnits(10, UnitTypeId.Millimeters);
#endif
            var bbox = pipe.get_BoundingBox(pipe.Document.ActiveView);
            XYZ p1 = bbox.Min;
            XYZ p2 = bbox.Max;
            XYZ p1L = pipe.GetCurve().GetEndPoint(0);
            XYZ p2L = pipe.GetCurve().GetEndPoint(1);
            var line = Line.CreateBound(p1, p2);
            p1 = new XYZ(con.Origin.X, con.Origin.Y, bbox.Min.Z) + line.Direction.Negate() * radius;
            p2 = new XYZ(con.Origin.X, con.Origin.Y, bbox.Max.Z) + line.Direction * radius;

            Outline myOutLn = new Outline(p1, p2);
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);

            FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document);
            List<FamilyInstance> elements = collector.WhereElementIsNotElementType().OfClass(typeof(FamilyInstance)).WherePasses(filter).Cast<FamilyInstance>().Where(x => x.MEPModel != null && x.MEPModel is MechanicalFitting).ToList();

            if (elements.Count == 0)
                return false;
            else
                return true;

            return retVal;
        }
    }
}
