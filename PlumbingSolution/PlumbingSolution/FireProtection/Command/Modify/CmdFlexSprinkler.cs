using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.ProcessBarForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    [Transaction(TransactionMode.Manual)]
    public class CmdFlexSprinkler : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            //Show form
            if (App.ShowFlexSprinklerForm() == false)
                return Result.Cancelled;

            return Result.Succeeded;
        }

        public static Result Process()
        {
            try
            {
                if (App.m_FlexSprinklerForm != null && App.m_FlexSprinklerForm.IsDisposed == false)
                {
                    App.m_FlexSprinklerForm.Hide();
                }

                if (App.m_FlexSprinklerForm.IsCheckedType1)
                {
                    ProcessType1();
                }
                else if (App.m_FlexSprinklerForm.IsCheckedType2)
                {
                    ProcessType2();
                }
                else if (App.m_FlexSprinklerForm.IsCheckedType3)
                {
                    ProcessType3();
                }
                else if (App.m_FlexSprinklerForm.IsCheckedType4)
                {
                    ProcessType4();
                }

                return Result.Succeeded;
            }
            catch (Exception)
            { }
            finally
            {
                if (App.m_FlexSprinklerForm != null && App.m_FlexSprinklerForm.IsDisposed == false)
                {
                    App.m_FlexSprinklerForm.Show(App.hWndRevit);
                }
                DisplayService.SetFocus(new HandleRef(null, App.m_FlexSprinklerForm.Handle));
            }
            return Result.Cancelled;
        }

        public static bool ProcessType1()
        {
            try
            {
                List<FamilyInstance> sprinklers = sr.SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return false;

                List<Pipe> pipes = SelectPipes();
                if (pipes == null || pipes.Count == 0)
                    return false;

                var pipeIds = (from Pipe p in pipes
                               where p.Id != ElementId.InvalidElementId
                               select p.Id).ToList();

                var height = App.m_FlexSprinklerForm.VerticalPipeLengthL2;

                double radius = App.m_FlexSprinklerForm.MainPipeSprinklerDistance/*400*/; //mm : sua thanh 500 theo yeu cau cua a Cuong
                var ft = Common.mmToFT * radius;

                // Status cancel export : default = false
                bool isCancelExport = false;

                // Count type imported
                int nCount = 0;

                // Initialize progress bar
                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;

                string title = Common.GetTextLanguage("PendentFlexibleSprinklerConnectionProgress");
                FrmProcessbar progressBar = new FrmProcessbar(title, Define.MessageFinish, DiritIconTool.Mep);
                progressBar.prgSingle.Minimum = 1;
                progressBar.prgSingle.Maximum = sprinklers.Count;
                progressBar.prgSingle.Value = 1;
                WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                helper.Owner = intPtr;
                progressBar.Show();

                using (TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "FlexSprinkler"))
                {
                    tranGr.Start();
                    foreach (FamilyInstance instance in sprinklers)
                    {
                        HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                        Transaction tran = new Transaction(Global.UIDoc.Document, "CreateConnector");
                        tran.Start();
                        try
                        {
                            string dPercent = string.Empty;
                            var sprinkle_point = (instance.Location as LocationPoint).Point;

                            //Check connect
                            var connects = instance.MEPModel.ConnectorManager.Connectors;

                            if (connects.Size == 0)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            var connect = Common.ToList(instance.MEPModel.ConnectorManager.Connectors).FirstOrDefault();

                            if (connect.IsConnected == true)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            var solid = Common.CreateCylindricalVolume(sprinkle_point, ft * 5, ft, true);
                            if (solid == null)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            //Find intersection

                            FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document, pipeIds);
                            collector.OfClass(typeof(Pipe));
                            collector.WherePasses(new ElementIntersectsSolidFilter(solid)); // Apply intersection filter to find matches
                            if (collector.GetElementCount() == 0)
                            {
                                tran.RollBack();
                                continue;
                            }

                            var pipeList = collector.ToElements();

                            //Global.m_uiDoc.Selection.SetElementIds(collector.ToElementIds());

                            bool split = false;
                            var pipe = ProcessPipes(pipeList.ToList(), sprinkle_point, out split);
                            (int, int) justification = CmdSprinklerDownright.GetJustification(pipe);
                            CmdSprinklerDownright.SetJustification(pipe, (0, 0));
                            var curve = (pipe.Location as LocationCurve).Curve;

                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
                            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
                            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
                            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);

                            double dTemp = 1000;

                            var v = line2d.Direction.CrossProduct(XYZ.BasisZ).Normalize();

                            var lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), v * dTemp);

                            var p11 = lineTemp.Evaluate(dTemp, false);

                            lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), -v * dTemp);

                            var p22 = lineTemp.Evaluate(dTemp, false);

                            lineTemp = Line.CreateBound(p11, p22);

                            XYZ newPlace = new XYZ(0, 0, 0);
                            ICollection<ElementId> elemIds = null;
                            var pipe1 = pipe;
                            Pipe pipe2 = null;
                            XYZ p = null;

                            bool isDauOng = false;
                            IntersectionResultArray arr = new IntersectionResultArray();
                            if (split == false)
                            {
                                //truong hop o dau ong

                                //expand
                                var index = line2d.GetEndPoint(0).DistanceTo(sprinkle_point) < line2d.GetEndPoint(1).DistanceTo(sprinkle_point) ? 0 : 1;
                                var curveExpand = Line.CreateUnbound(line2d.GetEndPoint(index), line2d.Direction * 100);

                                SetComparisonResult inter;

                                // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult1 = curveExpand.Intersect(lineTemp, CurveIntersectResultOption.Detailed);
                                inter = intersectResult1.Result;
#else
                                // Cấu hình bản cũ
                                arr = new IntersectionResultArray();
                                inter = curveExpand.Intersect(lineTemp, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                var p2d = arr.get_Item(0).XYZPoint;
#endif

                                // --- 2. DỰNG HÌNH 3D ---
                                var p3d = new XYZ(p2d.X, p2d.Y, sprinkle_point.Z);
                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTemp));
                                var curveExtend3d_temp = Line.CreateUnbound(curve.GetEndPoint(index), (curve as Line).Direction * 100);

                                // --- 3. TÌM GIAO ĐIỂM 3D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult2 = curveExtend3d_temp.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult2.Result;
#else
                                // Cấu hình bản cũ
                                arr = new IntersectionResultArray();
                                inter = curveExtend3d_temp.Intersect(line3d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                p = intersectResult2.GetOverlaps()[0].Point;
#else
                                p = arr.get_Item(0).XYZPoint;
#endif
                            }
                            else
                            {
                                SetComparisonResult inter;

                                // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult1 = lineTemp.Intersect(line2d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult1.Result;
#else
                                // Cấu hình bản cũ (Giả định arr đã được khai báo trước đó trong scope của bạn)
                                inter = lineTemp.Intersect(line2d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                var p2d = arr.get_Item(0).XYZPoint;
#endif

                                // --- 2. DỰNG HÌNH 3D ---
                                var p3d = new XYZ(p2d.X, p2d.Y, sprinkle_point.Z);
                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTemp));

                                // --- 3. TÌM GIAO ĐIỂM 3D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult2 = curve.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult2.Result;
#else
                                // Cấu hình bản cũ
                                arr = new IntersectionResultArray();
                                inter = curve.Intersect(line3d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                p = intersectResult2.GetOverlaps()[0].Point;
#else
                                p = arr.get_Item(0).XYZPoint;
#endif

                                pipe2 = null;
                                bool flagCreateTee = true;
                                if (GetPreferredJunctionType(pipe) != PreferredJunctionType.Tee)
                                {
                                    flagCreateTee = false;
                                }

                                ProcessStartSidePipe(pipe, out pipe2, p, out isDauOng, flagCreateTee);

                                if (pipe2 != null)
                                {
                                    pipeIds.Add(pipe2.Id);
                                }
                            }

                            //Set d = 25

                            double dFt = App.m_FlexSprinklerForm.PipeSizeC4 * Common.mmToFT;

                            var ft_h = Common.mmToFT * height;

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_v1 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

                            var line_v1 = Line.CreateUnbound(p, XYZ.BasisZ * ft_h * 2);

                            line_v1 = Line.CreateBound(p, line_v1.Evaluate(ft_h, false));

                            XYZ tmpPoint = line_v1.Evaluate(ft_h, false);
                            p = curve.Project(tmpPoint).XYZPoint;
                            if (!CheckPipeIsEnd1(pipe1, sprinklers, instance, sprinkle_point))
                                p = Line.CreateUnbound((curve as Line).Origin, (curve as Line).Direction).Project(tmpPoint).XYZPoint;

                            line_v1 = Line.CreateBound(p, tmpPoint);

                            (pipe_v1.Location as LocationCurve).Curve = line_v1;

                            pipe_v1.LookupParameter("Diameter").Set(dFt);

                            var pipeType = Global.UIDoc.Document.GetElement(App.m_FlexSprinklerForm.FamilyTypeC4) as PipeType;
                            if (pipeType != null)
                                pipe_v1.PipeType = pipeType;

                            listIdConnect.Add(pipe_v1.Id);

                            //Hor
                            XYZ dir = XYZ.Zero;
                            if (Common.To2D(sprinkle_point).DistanceTo(Common.To2D(p)) <= Global.UIApp.Application.ShortCurveTolerance)
                                dir = normal2d;
                            else
                                dir = (Common.To2D(sprinkle_point) - Common.To2D(p)).Normalize();
                            var v_v = dir;

                            var ft_v = App.m_FlexSprinklerForm.HorizontalPipeLengthL * Common.mmToFT;

                            var line_Extend = Line.CreateUnbound(line_v1.GetEndPoint(1), ft_v * v_v * 2);

                            newPlace = new XYZ(0, 0, 0);

                            var line_hor = Line.CreateBound(line_v1.GetEndPoint(1), line_Extend.Evaluate(ft_v, false));

                            var pipe_hor = Pipe.Create(Global.UIDoc.Document, pipe.MEPSystem.GetTypeId(), pipe.GetTypeId(), pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), line_hor.GetEndPoint(0), line_hor.GetEndPoint(1));

                            pipe_hor.LookupParameter("Diameter").Set(dFt);
                            if (pipeType != null)
                                pipe_hor.PipeType = pipeType;
                            listIdConnect.Add(pipe_hor.Id);

                            if ((double)Common.GetValueParameterByBuilt(pipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0
                            && !Common.IsParallel((curve as Line).Direction, XYZ.BasisX)
                            && !Common.IsParallel((curve as Line).Direction, XYZ.BasisY))
                            {
                                XYZ tmpVector = line_hor.GetEndPoint(1) - tmpPoint;
                                tmpPoint = tmpPoint + tmpVector.Normalize() * 0.001;
                                line_v1 = Line.CreateBound(p, tmpPoint);
                                (pipe_v1.Location as LocationCurve).Curve = line_v1;
                            }

                            //Connect
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe1, p);
                                var c3 = Common.GetConnectorClosestTo(pipe_v1, p);

                                if (App.m_FlexSprinklerForm.IsCheckedTee)
                                {
                                    if (GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee)
                                    {
                                        CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
                                    }
                                    else
                                    {
                                        if (pipe2 != null)
                                        {
                                            var c2 = Common.GetConnectorClosestTo(pipe2, p);
                                            var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                            listIdConnect.Add(fitting.Id);
                                        }
                                        else
                                        {
                                            tran.RollBack();
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    if (!isDauOng)
                                    {
                                        if (GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee)
                                        {
                                            if (CheckPipeIsEnd(pipe1, sprinkle_point))
                                                CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                        else
                                        {
                                            if (pipe2 != null)
                                            {
                                                var c2 = Common.GetConnectorClosestTo(pipe2, p);
                                                var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                                listIdConnect.Add(fitting.Id);
                                            }
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                        listIdConnect.Add(elbow.Id);
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                tran.RollBack();
                                continue;
                            }

                            try
                            {
                                var c1 = ConnectorUtils.GetConnectorNotConnnected(instance.MEPModel.ConnectorManager);
                                var c2 = Common.GetConnectorClosestTo(pipe_hor, line_hor.GetEndPoint(1));

                                FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                                List<XYZ> pnts = new List<XYZ>();
                                pnts.Add(c1.Origin);
                                pnts.Add(c2.Origin);

                                var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                                //Set the diameter of flex.
                                var splinkerDiameter = instance.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault().Radius * 2;

                                if (App.m_FlexSprinklerForm.IsSprinklerSize)
                                    flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                                else
                                    flexPipe.LookupParameter("Diameter").Set(dFt);

                                flexPipe.StartTangent = c1.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                                flexPipe.EndTangent = c2.CoordinateSystem.BasisZ.Negate();

                                var c1_flexPipe = GetConnectorClosestTo(flexPipe, c1.Origin);
                                var c2_flexPipe = GetConnectorClosestTo(flexPipe, c2.Origin);

                                FamilyInstance union = null;
                                if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                    union = Global.UIDoc.Document.Create.NewUnionFitting(c2_flexPipe, c2);
                                else
                                    union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, c2);

                                if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                {
                                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, instance.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

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
                                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, instance.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                                    if (!con1.IsConnectedTo(con2))
                                        con1.ConnectTo(con2);
                                }

                                listIdConnect.Add(flexPipe.Id);
                                listIdConnect.Add(union.Id);

                                Global.UIDoc.Document.Regenerate();
                            }
                            catch (Exception)
                            {
                                tran.RollBack();
                                continue;
                            }

                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe_v1, line_v1.GetEndPoint(1));
                                var c2 = Common.GetConnectorClosestTo(pipe_hor, line_v1.GetEndPoint(1));

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                listIdConnect.Add(elbow.Id);
                            }
                            catch (System.Exception ex)
                            {
                                tran.RollBack();
                                continue;
                            }

                            CmdSprinklerDownright.SetJustification(pipe, justification);
                            CmdSprinklerDownright.SetJustification(pipe1, justification);
                            CmdSprinklerDownright.SetJustification(pipe2, justification);
                            CmdSprinklerDownright.SetJustification(pipe_v1, justification);
                            CmdSprinklerDownright.SetJustification(pipe_hor, justification);

                            nCount++;

                            dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                            progressBar.tbxMessage.Text = dPercent;

                            progressBar.IncrementProgressBar();

                            CmdDeleteSprinker.CreateSchema(instance, listIdConnect.Select(x => x.ToInt().ToString()).ToList());
                        }
                        catch (Exception ex)
                        {
                            tran.RollBack();
                            continue;
                        }

                        tran.Commit();
                    }

                    if (isCancelExport == false)
                        progressBar.Dispose();

                    tranGr.Assimilate();
                }

                //Find pipe
            }
            catch (Exception)
            { }
            return false;
        }

        public static bool CheckPipeIsEnd1(Pipe pipe, List<FamilyInstance> lstIns, FamilyInstance familyInstance, XYZ point)
        {
            bool retVal = true;
            Dictionary<FamilyInstance, double> keyValuePairs = new Dictionary<FamilyInstance, double>();

            var con = Common.GetConnectorClosestTo(pipe, point);

            var con2d = Common.PointTo2D(con.Origin);

            foreach (var item in lstIns)
            {
                var lcPoint = item.Location as LocationPoint;
                if (lcPoint == null)
                    continue;

                var lcPoint2d = Common.PointTo2D(lcPoint.Point);
                var dis = lcPoint2d.DistanceTo(con2d);

                keyValuePairs.Add(item, dis);
            }

            var min = keyValuePairs.Values.Min();

            var dic = keyValuePairs.FirstOrDefault(x => x.Value == min);

            if (!con.IsConnected && dic.Key.Id == familyInstance.Id)
            {
                retVal = false;
            }

            return retVal;
        }

        public static bool ProcessType2()
        {
            try
            {
                List<FamilyInstance> sprinklers = sr.SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return false;

                List<Pipe> pipes = SelectPipes();
                if (pipes == null || pipes.Count == 0)
                    return false;

                var pipeIds = (from Pipe p in pipes
                               where p.Id != ElementId.InvalidElementId
                               select p.Id).ToList();

                var height = App.m_FlexSprinklerForm.VerticalPipeLengthL2;

                double radius = App.m_FlexSprinklerForm.MainPipeSprinklerDistance/*400*/; //mm : sua thanh 500 theo yeu cau cua a Cuong
                var ft = Common.mmToFT * radius;

                // Status cancel export : default = false
                bool isCancelExport = false;

                // Count type imported
                int nCount = 0;

                // Initialize progress bar
                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;

                string title = Common.GetTextLanguage("PendentFlexibleSprinklerConnectionProgress");
                FrmProcessbar progressBar = new FrmProcessbar(title, Define.MessageFinish, DiritIconTool.Mep);
                progressBar.prgSingle.Minimum = 1;
                progressBar.prgSingle.Maximum = sprinklers.Count;
                progressBar.prgSingle.Value = 1;
                WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                helper.Owner = intPtr;
                progressBar.Show();

                using (TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "FlexSprinkler"))
                {
                    tranGr.Start();
                    //Find pipe
                    foreach (FamilyInstance instance in sprinklers)
                    {
                        HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                        Transaction tran = new Transaction(Global.UIDoc.Document, "CreateConnector");
                        tran.Start();

                        try
                        {
                            string dPercent = string.Empty;
                            var sprinkle_point = (instance.Location as LocationPoint).Point;

                            //Check connect
                            var connects = instance.MEPModel.ConnectorManager.Connectors;

                            if (connects.Size == 0)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            var connect = Common.ToList(instance.MEPModel.ConnectorManager.Connectors).FirstOrDefault();

                            if (connect.IsConnected == true)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            var solid = Common.CreateCylindricalVolume(sprinkle_point, ft * 5, ft, true);
                            if (solid == null)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            ////////////////////////////////////////////////////////////////////////////

                            //Find intersection

                            FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document, pipeIds);
                            collector.OfClass(typeof(Pipe));
                            collector.WherePasses(new ElementIntersectsSolidFilter(solid)); // Apply intersection filter to find matches
                            if (collector.GetElementCount() == 0)
                            {
                                tran.RollBack();
                                continue;
                            }
                            var pipeList = collector.ToElements();

                            //Global.m_uiDoc.Selection.SetElementIds(collector.ToElementIds());

                            bool split = false;
                            var pipe = ProcessPipes(pipeList.ToList(), sprinkle_point, out split);

                            var curve = (pipe.Location as LocationCurve).Curve;

                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            double dTemp = 1000;

                            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
                            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
                            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
                            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);

                            var v = line2d.Direction.CrossProduct(XYZ.BasisZ).Normalize();

                            var lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), v * dTemp);

                            var p11 = lineTemp.Evaluate(dTemp, false);

                            lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), -v * dTemp);

                            var p22 = lineTemp.Evaluate(dTemp, false);

                            lineTemp = Line.CreateBound(p11, p22);

                            XYZ newPlace = new XYZ(0, 0, 0);
                            ICollection<ElementId> elemIds = null;
                            var pipe1 = pipe;
                            Pipe pipe2 = null;
                            XYZ p = null;

                            bool isDauOng = false;
                            IntersectionResultArray arr = new IntersectionResultArray();
                            if (split == false)
                            {
                                //truong hop o dau ong

                                //expand
                                var index = line2d.GetEndPoint(0).DistanceTo(sprinkle_point) < line2d.GetEndPoint(1).DistanceTo(sprinkle_point) ? 0 : 1;
                                var curveExpand = Line.CreateUnbound(line2d.GetEndPoint(index), line2d.Direction * 100);

                                SetComparisonResult inter;

                                // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult1 = curveExpand.Intersect(lineTemp, CurveIntersectResultOption.Detailed);
                                inter = intersectResult1.Result;
#else
                                // Cấu hình bản cũ
                                inter = curveExpand.Intersect(lineTemp, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                var p2d = arr.get_Item(0).XYZPoint;
#endif

                                // --- 2. DỰNG HÌNH 3D ---
                                var p3d = new XYZ(p2d.X, p2d.Y, sprinkle_point.Z);
                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTemp));
                                var curveExtend3d_temp = Line.CreateUnbound(curve.GetEndPoint(index), (curve as Line).Direction * 100);

                                // --- 3. TÌM GIAO ĐIỂM 3D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult2 = curveExtend3d_temp.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult2.Result;
#else
                                // Cấu hình bản cũ
                                arr = new IntersectionResultArray();
                                inter = curveExtend3d_temp.Intersect(line3d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                p = intersectResult2.GetOverlaps()[0].Point;
#else
                                p = arr.get_Item(0).XYZPoint;
#endif
                            }
                            else
                            {
                                SetComparisonResult inter;

                                // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult1 = lineTemp.Intersect(line2d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult1.Result;
#else
                                // Cấu hình bản cũ
                                inter = lineTemp.Intersect(line2d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                var p2d = arr.get_Item(0).XYZPoint;
#endif

                                // --- 2. DỰNG HÌNH 3D ---
                                var p3d = new XYZ(p2d.X, p2d.Y, sprinkle_point.Z);
                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTemp));

                                // --- 3. TÌM GIAO ĐIỂM 3D TRÊN CURVE ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult2 = curve.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult2.Result;
#else
                                // Cấu hình bản cũ
                                arr = new IntersectionResultArray();
                                inter = curve.Intersect(line3d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;
                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();
                                    progressBar.tbxMessage.Text = dPercent;
                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                p = intersectResult2.GetOverlaps()[0].Point;
#else
                                p = arr.get_Item(0).XYZPoint;
#endif

                                pipe2 = null;
                                bool flagCreateTee = true;
                                if (GetPreferredJunctionType(pipe) != PreferredJunctionType.Tee)
                                {
                                    flagCreateTee = false;
                                }

                                ProcessStartSidePipe(pipe, out pipe2, p, out isDauOng, flagCreateTee);

                                if (pipe2 != null)
                                {
                                    pipeIds.Add(pipe2.Id);
                                }
                            }

                            //Set d = 25

                            var dFt = App.m_FlexSprinklerForm.PipeSizeC4 * Common.mmToFT;

                            var ft_h = Common.mmToFT * height;

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_v1 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

                            var line_v1 = Line.CreateUnbound(p, XYZ.BasisZ * ft_h * 2);

                            line_v1 = Line.CreateBound(p, line_v1.Evaluate(ft_h, false));

                            XYZ tmpPoint = line_v1.Evaluate(ft_h, false);
                            p = Line.CreateUnbound(curve.GetEndPoint(0), (curve as Line).Direction).Project(tmpPoint).XYZPoint;
                            line_v1 = Line.CreateBound(p, tmpPoint);

                            (pipe_v1.Location as LocationCurve).Curve = line_v1;

                            pipe_v1.LookupParameter("Diameter").Set(dFt);
                            var pipeType = Global.UIDoc.Document.GetElement(App.m_FlexSprinklerForm.FamilyTypeC4) as PipeType;
                            if (pipeType != null)
                                pipe_v1.PipeType = pipeType;

                            listIdConnect.Add(pipe_v1.Id);
                            //Connect
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe1, p);
                                var c3 = Common.GetConnectorClosestTo(pipe_v1, p);

                                if (App.m_FlexSprinklerForm.IsCheckedTee)
                                {
                                    if (GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee)
                                    {
                                        CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
                                    }
                                    else
                                    {
                                        if (pipe2 != null)
                                        {
                                            var c2 = Common.GetConnectorClosestTo(pipe2, p);
                                            var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                            listIdConnect.Add(fitting.Id);
                                        }
                                        else
                                        {
                                            tran.RollBack();
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    if (!isDauOng)
                                    {
                                        if (GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee)
                                        {
                                            if (CheckPipeIsEnd1(pipe1, sprinklers, instance, sprinkle_point))
                                                CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                        else
                                        {
                                            if (pipe2 != null)
                                            {
                                                var c2 = Common.GetConnectorClosestTo(pipe2, p);
                                                var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                                listIdConnect.Add(fitting.Id);
                                            }
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                        listIdConnect.Add(elbow.Id);
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                tran.RollBack();
                                continue;
                            }

                            //Hor
                            XYZ dir = XYZ.Zero;
                            if (Common.To2D(sprinkle_point).DistanceTo(Common.To2D(p)) <= Global.UIApp.Application.ShortCurveTolerance)
                                dir = normal2d;
                            else
                                dir = (Common.To2D(sprinkle_point) - Common.To2D(p)).Normalize();
                            var v_v = dir;

                            var ft_v = App.m_FlexSprinklerForm.HorizontalPipeLengthL * Common.mmToFT;

                            var line_Extend = Line.CreateUnbound(line_v1.GetEndPoint(1), ft_v * v_v * 2);

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_hor = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;
                            var line_hor = Line.CreateBound(line_v1.GetEndPoint(1), line_Extend.Evaluate(ft_v, false));

                            (pipe_hor.Location as LocationCurve).Curve = line_hor;

                            pipe_hor.LookupParameter("Diameter").Set(dFt);
                            if (pipeType != null)
                                pipe_hor.PipeType = pipeType;

                            listIdConnect.Add(pipe_hor.Id);

                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe_v1, line_v1.GetEndPoint(1));
                                var c2 = Common.GetConnectorClosestTo(pipe_hor, line_v1.GetEndPoint(1));

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                listIdConnect.Add(elbow.Id);
                            }
                            catch (System.Exception ex)
                            {
                                tran.RollBack();
                                continue;
                            }

                            //Vertical 2

                            XYZ directionTemp = (sprinkle_point - line_hor.GetEndPoint(1)).Normalize();
                            var line_v2 = Line.CreateBound(line_hor.GetEndPoint(1), line_hor.GetEndPoint(1) + XYZ.BasisZ.Negate() * App.m_FlexSprinklerForm.ExtendPipeLengthL1 * Common.mmToFT);

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_v2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;
                            var center = line_hor.GetEndPoint(1) + directionTemp * App.m_FlexSprinklerForm.ExtendPipeLengthL1 * Common.mmToFT;

                            (pipe_v2.Location as LocationCurve).Curve = line_v2;

                            pipe_v2.LookupParameter("Diameter").Set(dFt);
                            if (pipeType != null)
                                pipe_v2.PipeType = pipeType;

                            listIdConnect.Add(pipe_v2.Id);
                            //Connect
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe_hor, line_hor.GetEndPoint(1));
                                var c2 = Common.GetConnectorClosestTo(pipe_v2, line_hor.GetEndPoint(1));

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                listIdConnect.Add(elbow.Id);
                            }
                            catch (System.Exception ex)
                            {
                                tran.RollBack();
                                continue;
                            }

                            try
                            {
                                var c1 = ConnectorUtils.GetConnectorNotConnnected(instance.MEPModel.ConnectorManager);
                                var c2 = ConnectorUtils.GetConnectorNotConnnected2(pipe_v2.ConnectorManager);

                                FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                                List<XYZ> pnts = new List<XYZ>();
                                pnts.Add(c1.Origin);
                                pnts.Add(c2.Origin);

                                var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                                //Set the diameter of flex.
                                var splinkerDiameter = instance.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault().Radius * 2;

                                if (App.m_FlexSprinklerForm.IsSprinklerSize)
                                    flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                                else
                                    flexPipe.LookupParameter("Diameter").Set(dFt);

                                flexPipe.StartTangent = c1.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                                flexPipe.EndTangent = c2.CoordinateSystem.BasisZ.Negate();

                                var c1_flexPipe = GetConnectorClosestTo(flexPipe, c1.Origin);
                                var c2_flexPipe = GetConnectorClosestTo(flexPipe, c2.Origin);

                                FamilyInstance union = null;
                                if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                    union = Global.UIDoc.Document.Create.NewUnionFitting(c2_flexPipe, c2);
                                else
                                    union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, c2);

                                if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                {
                                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, instance.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

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
                                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, instance.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                                    if (!con1.IsConnectedTo(con2))
                                    {
                                        con1.ConnectTo(con2);
                                    }
                                }

                                listIdConnect.Add(flexPipe.Id);
                                listIdConnect.Add(union.Id);

                                Global.UIDoc.Document.Regenerate();
                            }
                            catch (Exception)
                            {
                                tran.RollBack();
                                continue;
                            }

                            // If click cancel button when exporting
                            if (progressBar.IsCancel)
                            {
                                isCancelExport = true;
                                break;
                            }

                            nCount++;

                            dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                            progressBar.tbxMessage.Text = dPercent;

                            progressBar.IncrementProgressBar();

                            CmdDeleteSprinker.CreateSchema(instance, listIdConnect.Select(x => x.ToInt().ToString()).ToList());
                        }
                        catch (Exception ex)
                        {
                            tran.RollBack();
                            continue;
                        }

                        tran.Commit();
                    }

                    if (isCancelExport == false)
                        progressBar.Dispose();
                    tranGr.Assimilate();
                }
            }
            catch (Exception)
            { }
            return false;
        }

        public static bool ProcessType3()
        {
            try
            {
                // Get selected sprinkler
                List<FamilyInstance> selSprinklers = sr.SelectSprinklers();
                if (selSprinklers == null || selSprinklers.Count == 0)
                    return false;

                // Get selected main pipe
                List<Pipe> selPipes = SelectPipes();
                if (selPipes == null || selPipes.Count == 0)
                    return false;

                // Get selected main pipe id
                List<ElementId> selPipeIds = selPipes.Where(item => item.Id != ElementId.InvalidElementId).Select(item => item.Id).ToList();

                // Process

                try
                {
                    double invalidRadius_mm = App.m_FlexSprinklerForm.MainPipeSprinklerDistance;
                    double invalidRadius_ft = Common.mmToFT * invalidRadius_mm;

                    // Status cancel export : default = false
                    bool isCancelExport = false;

                    // Count type imported
                    int nCount = 0;

                    // Initialize progress bar
                    System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                    IntPtr intPtr = process.MainWindowHandle;

                    string title = Common.GetTextLanguage("PendentFlexibleSprinklerConnectionProgress");
                    FrmProcessbar progressBar = new FrmProcessbar(title, Define.MessageFinish, DiritIconTool.Mep);

                    progressBar.prgSingle.Minimum = 1;
                    progressBar.prgSingle.Maximum = selSprinklers.Count;
                    progressBar.prgSingle.Value = 1;
                    WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                    helper.Owner = intPtr;
                    progressBar.Show();

                    var dFt = App.m_FlexSprinklerForm.PipeSizeC4 * Common.mmToFT;
                    using (TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "FlexSprinkler"))
                    {
                        tranGr.Start();
                        foreach (FamilyInstance sprinkler in selSprinklers)
                        {
                            HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                            Transaction reTrans = new Transaction(Global.UIDoc.Document, "SPRINKLER_DOWN_RIGHT_TYPE_3");
                            try
                            {
                                reTrans.Start();

                                try
                                {
                                    string dPercent = string.Empty;
                                    // Location sprinkler
                                    XYZ locSprinkler = (sprinkler.Location as LocationPoint).Point;

                                    // Check valid connect
                                    ConnectorSet cntSetOfIns = sprinkler.MEPModel.ConnectorManager.Connectors;

                                    if (cntSetOfIns.Size == 0)
                                    {
                                        nCount++;

                                        dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();

                                        progressBar.tbxMessage.Text = dPercent;

                                        progressBar.IncrementProgressBar();
                                        reTrans.RollBack();
                                        continue;
                                    }

                                    Connector cntOfIns_1 = Common.ToList(sprinkler.MEPModel.ConnectorManager.Connectors).FirstOrDefault();

                                    if (cntOfIns_1.IsConnected == true)
                                    {
                                        nCount++;

                                        dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();

                                        progressBar.tbxMessage.Text = dPercent;

                                        progressBar.IncrementProgressBar();
                                        reTrans.RollBack();
                                        continue;
                                    }

                                    // Find intersection with sprinkler
                                    var cylindricalFromIns = Common.CreateCylindricalVolume(locSprinkler, invalidRadius_ft * 5, invalidRadius_ft, true);
                                    if (cylindricalFromIns == null)
                                    {
                                        nCount++;

                                        dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();

                                        progressBar.tbxMessage.Text = dPercent;

                                        progressBar.IncrementProgressBar();
                                        reTrans.RollBack();
                                        continue;
                                    }

                                    FilteredElementCollector filterCollector = new FilteredElementCollector(Global.UIDoc.Document, selPipeIds).OfClass(typeof(Pipe)).WherePasses(new ElementIntersectsSolidFilter(cylindricalFromIns));
                                    if (filterCollector == null || filterCollector.GetElementCount() <= 0)
                                    {
                                        nCount++;

                                        dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();

                                        progressBar.tbxMessage.Text = dPercent;

                                        progressBar.IncrementProgressBar();
                                        reTrans.RollBack();
                                        continue;
                                    }

                                    IList<Element> validPipes = filterCollector.ToElements();

                                    // Check pipe nesscesary split
                                    bool isSplit = false;
                                    Pipe processPipe = ProcessPipes(validPipes.ToList(), locSprinkler, out isSplit);
                                    (int, int) justification = CmdSprinklerDownright.GetJustification(processPipe);
                                    CmdSprinklerDownright.SetJustification(processPipe, (0, 0));
                                    // Process main pipe
                                    Curve curveProcessPipe = (processPipe.Location as LocationCurve).Curve;

                                    XYZ firstPnt_ProcessPipe = curveProcessPipe.GetEndPoint(0);
                                    XYZ secondPnt_ProcessPipe = curveProcessPipe.GetEndPoint(1);

                                    Line line2d = Line.CreateBound(Common.PointTo2D(firstPnt_ProcessPipe), Common.PointTo2D(secondPnt_ProcessPipe));
                                    XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
                                    XYZ normal3d = normal2d.CrossProduct((curveProcessPipe as Line).Direction);
                                    Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, firstPnt_ProcessPipe);

                                    double dTempEvaluate = 1000;

                                    var curveProcessPipe_2d = Line.CreateBound(new XYZ(firstPnt_ProcessPipe.X, firstPnt_ProcessPipe.Y, 0), new XYZ(secondPnt_ProcessPipe.X, secondPnt_ProcessPipe.Y, 0));
                                    XYZ dirCrossProduct = curveProcessPipe_2d.Direction.CrossProduct(XYZ.BasisZ).Normalize();

                                    var curveProcessPipe_crossProduct_2d = Line.CreateUnbound(new XYZ(locSprinkler.X, locSprinkler.Y, 0), dirCrossProduct * dTempEvaluate);

                                    XYZ p11 = curveProcessPipe_crossProduct_2d.Evaluate(dTempEvaluate, false);

                                    curveProcessPipe_crossProduct_2d = Line.CreateUnbound(new XYZ(locSprinkler.X, locSprinkler.Y, 0), -dirCrossProduct * dTempEvaluate);

                                    XYZ p22 = curveProcessPipe_crossProduct_2d.Evaluate(dTempEvaluate, false);

                                    curveProcessPipe_crossProduct_2d = Line.CreateBound(p11, p22);

                                    // Find intersection point
                                    XYZ newPlace = new XYZ(0, 0, 0);
                                    ICollection<ElementId> elemIds = null;
                                    var temp_processPipe_1 = processPipe;
                                    Pipe temp_processPipe_2 = null;
                                    XYZ finalIntPnt = null;

                                    IntersectionResultArray intRetArr = new IntersectionResultArray();

                                    bool isDauOng = false;
                                    //Truong hop dau ong
                                    if (isSplit == false)
                                    {
                                        //Expand
                                        var index = curveProcessPipe_2d.GetEndPoint(0).DistanceTo(locSprinkler) < curveProcessPipe_2d.GetEndPoint(1).DistanceTo(locSprinkler) ? 0 : 1;
                                        var curveExpand = Line.CreateUnbound(curveProcessPipe_2d.GetEndPoint(index), curveProcessPipe_2d.Direction * 100);

                                        SetComparisonResult inter;

                                        // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                        // Cấu hình bản mới
                                        var intersectResult1 = curveExpand.Intersect(curveProcessPipe_crossProduct_2d, CurveIntersectResultOption.Detailed);
                                        inter = intersectResult1.Result;
#else
                                        // Cấu hình bản cũ (Giả định intRetArr đã được khai báo trước đó)
                                        inter = curveExpand.Intersect(curveProcessPipe_crossProduct_2d, out intRetArr);
#endif

                                        if (inter != SetComparisonResult.Overlap)
                                        {
                                            nCount++;
                                            dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();
                                            progressBar.tbxMessage.Text = dPercent;
                                            progressBar.IncrementProgressBar();
                                            reTrans.RollBack();
                                            continue;
                                        }

#if Debug_2027 || Release_2027 || Release_2026
                                        var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                        var p2d = intRetArr.get_Item(0).XYZPoint;
#endif

                                        // --- 2. DỰNG HÌNH 3D ---
                                        var p3d = new XYZ(p2d.X, p2d.Y, locSprinkler.Z);
                                        var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTempEvaluate));
                                        var curveExtend3d_temp = Line.CreateUnbound(curveProcessPipe.GetEndPoint(index), (curveProcessPipe as Line).Direction * 100);

                                        // --- 3. TÌM GIAO ĐIỂM 3D ---
#if Debug_2027 || Release_2027 || Release_2026
                                        // Cấu hình bản mới
                                        var intersectResult2 = curveExtend3d_temp.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                        inter = intersectResult2.Result;
#else
                                        // Cấu hình bản cũ
                                        intRetArr = new IntersectionResultArray();
                                        inter = curveExtend3d_temp.Intersect(line3d, out intRetArr);
#endif

                                        if (inter != SetComparisonResult.Overlap)
                                        {
                                            nCount++;
                                            dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();
                                            progressBar.tbxMessage.Text = dPercent;
                                        }
                                    }
                                    // Truong hop o giua ong
                                    else
                                    {
                                        SetComparisonResult inter;

                                        // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                        // Cấu hình bản mới
                                        var intersectResult1 = curveProcessPipe_crossProduct_2d.Intersect(curveProcessPipe_2d, CurveIntersectResultOption.Detailed);
                                        inter = intersectResult1.Result;
#else
                                        // Cấu hình bản cũ
                                        inter = curveProcessPipe_crossProduct_2d.Intersect(curveProcessPipe_2d, out intRetArr);
#endif

                                        if (inter != SetComparisonResult.Overlap)
                                        {
                                            nCount++;
                                            dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();
                                            progressBar.tbxMessage.Text = dPercent;
                                            progressBar.IncrementProgressBar();
                                            reTrans.RollBack();
                                            continue;
                                        }

#if Debug_2027 || Release_2027 || Release_2026
                                        var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                        var p2d = intRetArr.get_Item(0).XYZPoint;
#endif

                                        // --- 2. DỰNG HÌNH 3D ---
                                        var p3d = new XYZ(p2d.X, p2d.Y, locSprinkler.Z);
                                        var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTempEvaluate));

                                        // --- 3. TÌM GIAO ĐIỂM 3D TRÊN ỐNG CHÍNH ---
#if Debug_2027 || Release_2027 || Release_2026
                                        // Cấu hình bản mới
                                        var intersectResult2 = curveProcessPipe.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                        inter = intersectResult2.Result;
#else
                                        // Cấu hình bản cũ
                                        intRetArr = new IntersectionResultArray();
                                        inter = curveProcessPipe.Intersect(line3d, out intRetArr);
#endif

                                        if (inter != SetComparisonResult.Overlap)
                                        {
                                            nCount++;
                                            dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();
                                            progressBar.tbxMessage.Text = dPercent;
                                            progressBar.IncrementProgressBar();
                                            reTrans.RollBack();
                                            continue;
                                        }

#if Debug_2027 || Release_2027 || Release_2026
                                        finalIntPnt = intersectResult2.GetOverlaps()[0].Point;
#else
                                        finalIntPnt = intRetArr.get_Item(0).XYZPoint;
#endif

                                        temp_processPipe_2 = null;
                                        bool flagCreateTee = true;
                                        if (GetPreferredJunctionType(processPipe) != PreferredJunctionType.Tee)
                                        {
                                            flagCreateTee = false;
                                        }

                                        ProcessStartSidePipe(processPipe, out temp_processPipe_2, finalIntPnt, out isDauOng, flagCreateTee);

                                        if (temp_processPipe_2 != null)
                                        {
                                            selPipeIds.Add(temp_processPipe_2.Id);
                                        }
                                    }

                                    // Generate Pipe Horizontal
                                    XYZ dir = XYZ.Zero;
                                    if (Common.To2D(locSprinkler).DistanceTo(Common.To2D(finalIntPnt)) <= Global.UIApp.Application.ShortCurveTolerance)
                                        dir = normal2d;
                                    else
                                        dir = (Common.To2D(locSprinkler) - Common.To2D(finalIntPnt)).Normalize();
                                    var v_v = dir;
                                    var ft_v = App.m_FlexSprinklerForm.HorizontalPipeLengthL * Common.mmToFT;
                                    var line_Extend = Line.CreateUnbound(finalIntPnt, ft_v * v_v * 2);

                                    newPlace = new XYZ(0, 0, 0);
                                    elemIds = ElementTransformUtils.CopyElement(
                                     Global.UIDoc.Document, temp_processPipe_1.Id, newPlace);

                                    var pipeType = Global.UIDoc.Document.GetElement(App.m_FlexSprinklerForm.FamilyTypeC4) as PipeType;

                                    var horizontal_pipe = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;
                                    var hor_line = Line.CreateBound(finalIntPnt, line_Extend.Evaluate(App.m_FlexSprinklerForm.HorizontalPipeLengthL * Common.mmToFT, false));
                                    (horizontal_pipe.Location as LocationCurve).Curve = hor_line;
                                    horizontal_pipe.LookupParameter("Diameter").Set(App.m_FlexSprinklerForm.PipeSizeC4 * Common.mmToFT);
                                    if (pipeType != null)
                                        horizontal_pipe.PipeType = pipeType;

                                    listIdConnect.Add(horizontal_pipe.Id);

                                    Global.UIDoc.Document.Regenerate();
                                    // Connect horizontal pipe with main pipe
                                    try
                                    {
                                        var c1 = Common.GetConnectorClosestTo(temp_processPipe_1, finalIntPnt);
                                        var c3 = Common.GetConnectorClosestTo(horizontal_pipe, finalIntPnt);
                                        if (App.m_FlexSprinklerForm.IsCheckedTee)
                                        {
                                            if (GetPreferredJunctionType(temp_processPipe_1) != PreferredJunctionType.Tee && isSplit == true)
                                            {
                                                CreateTap(temp_processPipe_1 as MEPCurve, horizontal_pipe as MEPCurve, ref listIdConnect);
                                            }
                                            else
                                            {
                                                if (temp_processPipe_2 != null)
                                                {
                                                    var c2 = Common.GetConnectorClosestTo(temp_processPipe_2, finalIntPnt);
                                                    var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                                    listIdConnect.Add(fitting.Id);
                                                }
                                                else
                                                {
                                                    reTrans.RollBack();
                                                    continue;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!isDauOng)
                                            {
                                                if (GetPreferredJunctionType(temp_processPipe_1) != PreferredJunctionType.Tee && isSplit == true)
                                                {
                                                    if (CheckPipeIsEnd(temp_processPipe_1, locSprinkler))
                                                        CreateTap(temp_processPipe_1 as MEPCurve, horizontal_pipe as MEPCurve, ref listIdConnect);
                                                    else
                                                    {
                                                        var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                        listIdConnect.Add(elbow.Id);
                                                    }
                                                }
                                                else
                                                {
                                                    if (temp_processPipe_2 != null)
                                                    {
                                                        var c2 = Common.GetConnectorClosestTo(temp_processPipe_2, finalIntPnt);
                                                        var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                                        listIdConnect.Add(fitting.Id);
                                                    }
                                                    else
                                                    {
                                                        var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                        listIdConnect.Add(elbow.Id);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        reTrans.RollBack();
                                        continue;
                                    }

                                    //  Generate vertical pipe 2
                                    var line_v2 = Line.CreateBound(hor_line.GetEndPoint(1), locSprinkler);

                                    var center = line_v2.Evaluate((line_v2.GetEndParameter(0) + line_v2.GetEndParameter(1)) / 2, false);

                                    //Connect horizontal pipe with vertical pipe 2
                                    try
                                    {
                                        var c1 = ConnectorUtils.GetConnectorNotConnnected(sprinkler.MEPModel.ConnectorManager);
                                        var c2 = ConnectorUtils.GetConnectorNotConnnected2(horizontal_pipe.ConnectorManager);

                                        FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();
                                        if (flexPipeType != null)
                                        {
                                            List<XYZ> pnts = new List<XYZ>();
                                            pnts.Add(c1.Origin);
                                            pnts.Add(c2.Origin);

                                            var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                                            //Set the diameter of flex.
                                            var splinkerDiameter = sprinkler.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault().Radius * 2;

                                            if (App.m_FlexSprinklerForm.IsSprinklerSize)
                                                flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                                            else
                                                flexPipe.LookupParameter("Diameter").Set(dFt);

                                            flexPipe.StartTangent = c1.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                                            flexPipe.EndTangent = c2.CoordinateSystem.BasisZ.Negate();

                                            var c1_flexPipe = GetConnectorClosestTo(flexPipe, c1.Origin);
                                            var c2_flexPipe = GetConnectorClosestTo(flexPipe, c2.Origin);

                                            FamilyInstance union = null;
                                            if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                                union = Global.UIDoc.Document.Create.NewUnionFitting(c2_flexPipe, c2);
                                            else
                                                union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, c2);

                                            if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                            {
                                                Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinkler.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

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
                                                Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinkler.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                                                if (!con1.IsConnectedTo(con2))
                                                {
                                                    con1.ConnectTo(con2);
                                                }
                                            }

                                            listIdConnect.Add(flexPipe.Id);
                                            listIdConnect.Add(union.Id);
                                            Global.UIDoc.Document.Regenerate();
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        reTrans.RollBack();
                                        continue;
                                    }

                                    CmdSprinklerDownright.SetJustification(horizontal_pipe, justification);
                                    CmdSprinklerDownright.SetJustification(processPipe, justification);
                                    CmdSprinklerDownright.SetJustification(temp_processPipe_2, justification);
                                    CmdSprinklerDownright.SetJustification(temp_processPipe_1, justification);

                                    // If click cancel button when exporting
                                    if (progressBar.IsCancel)
                                    {
                                        isCancelExport = true;
                                        break;
                                    }

                                    nCount++;

                                    dPercent = nCount.ToString() + "/" + selSprinklers.Count.ToString();

                                    progressBar.tbxMessage.Text = dPercent;

                                    progressBar.IncrementProgressBar();

                                    CmdDeleteSprinker.CreateSchema(sprinkler, listIdConnect.Select(x => x.ToInt().ToString()).ToList());
                                }
                                catch (Exception ex)
                                {
                                    reTrans.RollBack();
                                }

                                reTrans.Commit();
                            }
                            catch (Exception)
                            {
                                reTrans.RollBack();
                                continue;
                            }
                        }

                        if (isCancelExport == false)
                            progressBar.Dispose();
                        tranGr.Assimilate();
                    }
                }
                catch
                {
                }
            }
            catch (Exception)
            { }
            return false;
        }

        /// <summary>
        /// Get Preferred Junction Type
        /// </summary>
        /// <param name="pipe"></param>
        /// <returns></returns>
        private static PreferredJunctionType GetPreferredJunctionType(Pipe pipe)
        {
            var pipeType = pipe.PipeType as PipeType;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType;
        }

        /// <summary>
        /// Create Tap
        /// </summary>
        /// <param name="mepCurveSplit1"></param>
        /// <param name="mepCurveSplit2"></param>
        /// <returns></returns>
        public static bool CreateTap(MEPCurve mepCurveSplit1, MEPCurve mepCurveSplit2, ref HashSet<ElementId> listIdConnect)
        {
            var locationCurve1 = mepCurveSplit1.GetCurve();
            var line1 = locationCurve1 as Line;

            var locationCurve2 = mepCurveSplit2.GetCurve();
            var line2 = locationCurve2 as Line;

            var p10 = line2.GetEndPoint(0);
            var p11 = line2.GetEndPoint(1);

            var inter1 = locationCurve1.Project(p10);
            var inter2 = locationCurve1.Project(p11);

            if (inter1 == null || inter2 == null)
                return false;

            var d1 = inter1.XYZPoint.DistanceTo(p10);
            var d2 = inter2.XYZPoint.DistanceTo(p11);

            if (d1 < d2)
            {
                var con = GetConnectorClosestTo(mepCurveSplit2, p10);
                var tap = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                listIdConnect.Add(tap.Id);
            }
            else
            {
                var con = GetConnectorClosestTo(mepCurveSplit2, p11);
                var tap = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                listIdConnect.Add(tap.Id);
            }

            return true;
        }

        /// <summary>
        /// Select Pipes
        /// </summary>
        /// <returns></returns>
        public static List<Pipe> SelectPipes()
        {
            List<Pipe> pipes = new List<Pipe>();
            try
            {
                var pickedObjs = Global.UIDoc.Selection.PickObjects(ObjectType.Element, new MEPCurveFilter/*PipeFilter*/(), "Pick pipes: ");

                foreach (Reference pickedObj in pickedObjs)
                {
                    var pipe = Global.UIDoc.Document.GetElement(pickedObj) as Pipe;

                    if (pipe != null)
                        pipes.Add(pipe);
                }
            }
            catch (System.Exception ex)
            {
            }

            return pipes;
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

        /// <summary>
        /// Process Start Side Pipe
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="pipe2"></param>
        /// <param name="pOn"></param>
        /// <param name="flagSplit"></param>
        public static void ProcessStartSidePipe(Pipe pipe, out Pipe pipe2, XYZ pOn, out bool isDauOng, bool flagSplit = true)
        {
            var curve = (pipe.Location as LocationCurve).Curve;

            //Create plane
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            //Check co phai dau cuu hoa o gan dau cua ong ko : check trong pham vi 1m - 400mm
            double kc_mm = App.m_FlexSprinklerForm.EndPointSprinklerDistance /*1000*/;
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
                if (IsIntersect(p0) == false && !CheckPipeIsEnd(pipe, pOn) && !App.m_FlexSprinklerForm.IsCheckedTee)
                {
                    isDauOng = true;
                    far = 1;
                }
            }
            if (d2 < km_ft)
            {
                if (IsIntersect(p1) == false && !CheckPipeIsEnd(pipe, pOn) && !App.m_FlexSprinklerForm.IsCheckedTee)
                {
                    isDauOng = true;
                    far = 0;
                }
            }

            Pipe pipe1 = pipe;
            pipe2 = null;

            if (flagSplit == true)
            {
                if (isDauOng == false)
                {
                    SplitPipe(pipe, pOn, out pipe1, out pipe2);
                }
                else if (far != -1)
                {
                    if (far == 1)
                        (pipe1.Location as LocationCurve).Curve = Line.CreateBound(pOn, p1);
                    else
                        (pipe1.Location as LocationCurve).Curve = Line.CreateBound(p0, pOn);
                }
            }
        }

        public static bool CheckPipeIsEnd(Pipe pipe, XYZ point)
        {
            var con = Common.GetConnectorClosestTo(pipe, point);

            return con.IsConnected;
        }

        /// <summary>
        /// Split Pipe
        /// </summary>
        /// <param name="pipeOrigin"></param>
        /// <param name="splitPoint"></param>
        /// <param name="pipe1"></param>
        /// <param name="pipe2"></param>
        public static void SplitPipe(Pipe pipeOrigin, XYZ splitPoint, out Pipe pipe1, out Pipe pipe2)
        {
            var curve = (pipeOrigin.Location as LocationCurve).Curve;

            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            pipe1 = pipeOrigin;
            pipe2 = null;


            Pipe tempPipe_1 = pipeOrigin;
            Pipe tempPipe_2 = null;

            var tempPipe2Id = PlumbingUtils.BreakCurve(Global.UIDoc.Document, tempPipe_1.Id, splitPoint);
            if (tempPipe2Id != ElementId.InvalidElementId)
            {
                tempPipe_2 = Global.UIDoc.Document.GetElement(tempPipe2Id) as Pipe;
                Line curveTempPipe_2 = tempPipe_2.GetCurve() as Line;

                //if ((Common.IsEqual(curveTempPipe_2.GetEndPoint(0), splitPoint) && Common.Equals(curveTempPipe_2.GetEndPoint(1), p1))
                //    || (Common.IsEqual(curveTempPipe_2.GetEndPoint(1), splitPoint) && Common.Equals(curveTempPipe_2.GetEndPoint(0), p1)))
                //{
                //    pipe1 = tempPipe_1;
                //    pipe2 = tempPipe_2;
                //}
                //else
                //{
                //    pipe1 = tempPipe_2;
                //    pipe2 = tempPipe_1;
                //}
                if (tempPipe_1.Id == pipeOrigin.Id)
                {
                    pipe1 = tempPipe_1;
                    pipe2 = tempPipe_2;
                }
                else
                {
                    pipe2 = tempPipe_1;
                    pipe1 = tempPipe_2;
                }
            }


            //Find
            double ft = 0.001;
            var solid = Common.CreateCylindricalVolume(p1, ft, ft, false);
            if (solid != null)
            {
                //Find intersection wit fitting
                var fittingBuilt = new ElementId(BuiltInCategory.OST_PipeFitting);
                FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document);
                collector.OfClass(typeof(FamilyInstance));
                collector.OfCategoryId(fittingBuilt);
                collector.WherePasses(new ElementIntersectsSolidFilter(solid)); // Apply intersection filter to find matches

                if (collector.GetElementCount() != 0)
                {
                    var elements = collector.ToElements();

                    var c1 = Common.GetConnectorClosestTo(pipe2, p1);

                    foreach (FamilyInstance fitting in elements)
                    {
                        var c11 = Common.GetConnectorClosestTo(fitting, p1);

                        if (c1 != null && c11 != null)
                        {
                            if (c1.Origin.DistanceTo(c11.Origin) < ft)
                            {
                                if (c1.IsConnectedTo(c11) == false)
                                    c1.ConnectTo(c11);
                            }
                        }
                    }
                }
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

        /// <summary>
        /// Get Connector Closest To
        /// </summary>
        /// <param name="e"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static Connector GetConnectorClosestTo(Element e,
                                                XYZ p)
        {
            ConnectorManager cm = GetConnectorManager(e);

            return null == cm
              ? null
              : GetConnectorClosestTo(cm.Connectors, p);
        }

        /// <summary>
        /// Get Connector Closest To
        /// </summary>
        /// <param name="connectors"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static Connector GetConnectorClosestTo(ConnectorSet connectors,
                                                XYZ p)
        {
            Connector targetConnector = null;
            double minDist = double.MaxValue;

            foreach (Connector c in connectors)
            {
                double d = c.Origin.DistanceTo(p);

                if (d < minDist)
                {
                    targetConnector = c;
                    minDist = d;
                }
            }
            return targetConnector;
        }

        /// <summary>
        /// GetConnectorManager
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static ConnectorManager GetConnectorManager(Element e)
        {
            MEPCurve mc = e as MEPCurve;
            FamilyInstance fi = e as FamilyInstance;

            if (null == mc && null == fi)
            {
                throw new ArgumentException(
                  "Element is neither an MEP curve nor a fitting.");
            }

            return null == mc
              ? fi.MEPModel.ConnectorManager
              : mc.ConnectorManager;
        }

        public static bool ProcessType4()
        {
            try
            {
                List<FamilyInstance> sprinklers = sr.SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return false;

                List<Pipe> pipes = SelectPipes();
                if (pipes == null || pipes.Count == 0)
                    return false;

                var pipeIds = (from Pipe p in pipes
                               where p.Id != ElementId.InvalidElementId
                               select p.Id).ToList();

                var height = App.m_FlexSprinklerForm.HorizontalPipeLengthL;

                double radius = App.m_FlexSprinklerForm.MainPipeSprinklerDistance/*400*/; //mm : sua thanh 500 theo yeu cau cua a Cuong
                var ft = Common.mmToFT * radius;

                // Status cancel export : default = false
                bool isCancelExport = false;

                // Count type imported
                int nCount = 0;

                // Initialize progress bar
                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;

                string title = Common.GetTextLanguage("PendentFlexibleSprinklerConnectionProgress");
                FrmProcessbar progressBar = new FrmProcessbar(title, Define.MessageFinish, DiritIconTool.Mep);

                progressBar.prgSingle.Minimum = 1;
                progressBar.prgSingle.Maximum = sprinklers.Count;
                progressBar.prgSingle.Value = 1;
                WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                helper.Owner = intPtr;
                progressBar.Show();

                using (TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "FlexSprinkler"))
                {
                    tranGr.Start();

                    //Find pipe
                    foreach (FamilyInstance instance in sprinklers)
                    {
                        HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                        Transaction tran = new Transaction(Global.UIDoc.Document, "CreateConnector");
                        tran.Start();
                        try
                        {
                            string dPercent = string.Empty;
                            var sprinkle_point = (instance.Location as LocationPoint).Point;

                            //Check connect
                            var connects = instance.MEPModel.ConnectorManager.Connectors;

                            if (connects.Size == 0)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            var connect = Common.ToList(instance.MEPModel.ConnectorManager.Connectors).FirstOrDefault();

                            if (connect.IsConnected == true)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            var solid = Common.CreateCylindricalVolume(sprinkle_point, ft * 5, ft, true);
                            if (solid == null)
                            {
                                nCount++;

                                dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                progressBar.tbxMessage.Text = dPercent;

                                progressBar.IncrementProgressBar();
                                tran.RollBack();
                                continue;
                            }

                            //Find intersection

                            FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document, pipeIds);
                            collector.OfClass(typeof(Pipe));
                            collector.WherePasses(new ElementIntersectsSolidFilter(solid)); // Apply intersection filter to find matches
                            if (collector.GetElementCount() == 0)
                            {
                                tran.RollBack();
                                continue;
                            }
                            var pipeList = collector.ToElements();

                            //Global.m_uiDoc.Selection.SetElementIds(collector.ToElementIds());

                            bool split = false;
                            var pipe = ProcessPipes(pipeList.ToList(), sprinkle_point, out split);

                            var curve = (pipe.Location as LocationCurve).Curve;

                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            double dTemp = 1000;

                            Line line2d = Line.CreateBound(Common.PointTo2D(p0), Common.PointTo2D(p1));
                            XYZ normal2d = line2d.Direction.CrossProduct(XYZ.BasisZ);
                            XYZ normal3d = normal2d.CrossProduct((curve as Line).Direction);
                            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal3d, p0);

                            var v = line2d.Direction.CrossProduct(XYZ.BasisZ).Normalize();

                            var lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), v * dTemp);

                            var p11 = lineTemp.Evaluate(dTemp, false);

                            lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), -v * dTemp);

                            var p22 = lineTemp.Evaluate(dTemp, false);

                            lineTemp = Line.CreateBound(p11, p22);

                            //Common.CreateModelLine(line2d.GetEndPoint(0), line2d.GetEndPoint(1));
                            //Common.CreateModelLine(p11, p22);

                            XYZ newPlace = new XYZ(0, 0, 0);
                            ICollection<ElementId> elemIds = null;
                            var pipe1 = pipe;
                            Pipe pipe2 = null;
                            XYZ p = null;

                            bool isDauOng = false;
                            IntersectionResultArray arr = new IntersectionResultArray();
                            if (split == false)
                            {
                                //truong hop o dau ong

                                //expand
                                var index = line2d.GetEndPoint(0).DistanceTo(sprinkle_point) < line2d.GetEndPoint(1).DistanceTo(sprinkle_point) ? 0 : 1;
                                var curveExpand = Line.CreateUnbound(line2d.GetEndPoint(index), line2d.Direction * 100);

                                SetComparisonResult inter;

                                // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult1 = curveExpand.Intersect(lineTemp, CurveIntersectResultOption.Detailed);
                                inter = intersectResult1.Result;
#else
                                // Cấu hình bản cũ
                                inter = curveExpand.Intersect(lineTemp, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;

                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                    progressBar.tbxMessage.Text = dPercent;

                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                var p2d = arr.get_Item(0).XYZPoint;
#endif

                                // --- 2. DỰNG HÌNH 3D ---
                                var p3d = new XYZ(p2d.X, p2d.Y, sprinkle_point.Z);
                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTemp));
                                var curveExtend3d_temp = Line.CreateUnbound(curve.GetEndPoint(index), (curve as Line).Direction * 100);

                                // --- 3. TÌM GIAO ĐIỂM 3D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult2 = curveExtend3d_temp.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult2.Result;
#else
                                // Cấu hình bản cũ
                                arr = new IntersectionResultArray();
                                inter = curveExtend3d_temp.Intersect(line3d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;

                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                    progressBar.tbxMessage.Text = dPercent;

                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                p = intersectResult2.GetOverlaps()[0].Point;
#else
                                p = arr.get_Item(0).XYZPoint;
#endif
                            }
                            else
                            {
                                SetComparisonResult inter;

                                // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult1 = lineTemp.Intersect(line2d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult1.Result;
#else
                                // Cấu hình bản cũ
                                inter = lineTemp.Intersect(line2d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;

                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                    progressBar.tbxMessage.Text = dPercent;

                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                                var p2d = arr.get_Item(0).XYZPoint;
#endif

                                // --- 2. DỰNG HÌNH 3D ---
                                var p3d = new XYZ(p2d.X, p2d.Y, sprinkle_point.Z);
                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTemp));

                                // --- 3. TÌM GIAO ĐIỂM 3D TRÊN CURVE ---
#if Debug_2027 || Release_2027 || Release_2026
                                // Cấu hình bản mới
                                var intersectResult2 = curve.Intersect(line3d, CurveIntersectResultOption.Detailed);
                                inter = intersectResult2.Result;
#else
                                // Cấu hình bản cũ
                                arr = new IntersectionResultArray();
                                inter = curve.Intersect(line3d, out arr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    nCount++;

                                    dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                                    progressBar.tbxMessage.Text = dPercent;

                                    progressBar.IncrementProgressBar();
                                    tran.RollBack();
                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
                                p = intersectResult2.GetOverlaps()[0].Point;
#else
                                p = arr.get_Item(0).XYZPoint;
#endif

                                pipe2 = null;
                                bool flagCreateTee = true;
                                if (GetPreferredJunctionType(pipe) != PreferredJunctionType.Tee)
                                {
                                    flagCreateTee = false;
                                }

                                ProcessStartSidePipe(pipe, out pipe2, p, out isDauOng, flagCreateTee);

                                if (pipe2 != null)
                                {
                                    pipeIds.Add(pipe2.Id);
                                }
                            }

                            //Set d = 25

                            var dFt = App.m_FlexSprinklerForm.PipeSizeC4 * Common.mmToFT;

                            var ft_h = Common.mmToFT * height;

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_v1 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;
                            XYZ dir = XYZ.Zero;
                            if (Common.To2D(sprinkle_point).DistanceTo(Common.To2D(p)) <= Global.UIApp.Application.ShortCurveTolerance)
                                dir = normal2d;
                            else
                                dir = (Common.To2D(sprinkle_point) - Common.To2D(p)).Normalize();
                            var v_v = dir;
                            var line_hor = Line.CreateUnbound(p, ft_h * v_v * 2);

                            line_hor = Line.CreateBound(p, line_hor.Evaluate(ft_h, false));

                            XYZ tmpPoint = line_hor.Evaluate(ft_h, false);
                            p = Line.CreateUnbound(curve.GetEndPoint(0), (curve as Line).Direction).Project(tmpPoint).XYZPoint;
                            line_hor = Line.CreateBound(p, tmpPoint);

                            (pipe_v1.Location as LocationCurve).Curve = line_hor;

                            pipe_v1.LookupParameter("Diameter").Set(dFt);
                            var pipeType = Global.UIDoc.Document.GetElement(App.m_FlexSprinklerForm.FamilyTypeC4) as PipeType;
                            if (pipeType != null)
                                pipe_v1.PipeType = pipeType;

                            listIdConnect.Add(pipe_v1.Id);
                            //Connect
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe1, p);
                                var c3 = Common.GetConnectorClosestTo(pipe_v1, p);

                                if (App.m_FlexSprinklerForm.IsCheckedTee)
                                {
                                    if (GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee)
                                    {
                                        CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
                                    }
                                    else
                                    {
                                        if (pipe2 != null)
                                        {
                                            var c2 = Common.GetConnectorClosestTo(pipe2, p);
                                            var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                            listIdConnect.Add(fitting.Id);
                                        }
                                        else
                                        {
                                            tran.RollBack();
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    if (!isDauOng)
                                    {
                                        if (GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee)
                                        {
                                            if (CheckPipeIsEnd1(pipe1, sprinklers, instance, sprinkle_point))
                                                CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                        else
                                        {
                                            if (pipe2 != null)
                                            {
                                                var c2 = Common.GetConnectorClosestTo(pipe2, p);
                                                var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                                listIdConnect.Add(fitting.Id);
                                            }
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                        listIdConnect.Add(elbow.Id);
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                tran.RollBack();
                                continue;
                            }

                            //Vertical 2
                            XYZ directionTemp = (sprinkle_point - line_hor.GetEndPoint(1)).Normalize();
                            var line_v2 = Line.CreateBound(line_hor.GetEndPoint(1), line_hor.GetEndPoint(1) + XYZ.BasisZ.Negate() * App.m_FlexSprinklerForm.ExtendPipeLengthL1 * Common.mmToFT);

                            newPlace = new XYZ(0, 0, 0);

                            var pipe_v2 = Pipe.Create(Global.UIDoc.Document, pipe_v1.MEPSystem.GetTypeId(), pipe_v1.GetTypeId(), pipe_v1.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), line_v2.GetEndPoint(0), line_v2.GetEndPoint(1));
                            var center = line_hor.GetEndPoint(1) + directionTemp * App.m_FlexSprinklerForm.ExtendPipeLengthL1 * Common.mmToFT;

                            pipe_v2.LookupParameter("Diameter").Set(dFt);
                            if (pipeType != null)
                                pipe_v2.PipeType = pipeType;
                            listIdConnect.Add(pipe_v2.Id);

                            //Connect
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe_v1, line_hor.GetEndPoint(1));
                                var c2 = Common.GetConnectorClosestTo(pipe_v2, line_hor.GetEndPoint(1));

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                listIdConnect.Add(elbow.Id);
                            }
                            catch (System.Exception ex)
                            {
                                tran.RollBack();
                                continue;
                            }

                            try
                            {
                                var c1 = ConnectorUtils.GetConnectorNotConnnected(instance.MEPModel.ConnectorManager);
                                var c2 = Common.GetConnectorClosestTo(pipe_v2, line_v2.GetEndPoint(1));

                                FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                                List<XYZ> pnts = new List<XYZ>();
                                pnts.Add(c1.Origin);
                                pnts.Add(c2.Origin);

                                var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                                //Set the diameter of flex.
                                var splinkerDiameter = instance.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault().Radius * 2;

                                if (App.m_FlexSprinklerForm.IsSprinklerSize)
                                    flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                                else
                                    flexPipe.LookupParameter("Diameter").Set(dFt);

                                flexPipe.StartTangent = c1.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                                flexPipe.EndTangent = c2.CoordinateSystem.BasisZ.Negate();

                                var c1_flexPipe = GetConnectorClosestTo(flexPipe, c1.Origin);
                                var c2_flexPipe = GetConnectorClosestTo(flexPipe, c2.Origin);

                                FamilyInstance union = null;
                                if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                    union = Global.UIDoc.Document.Create.NewUnionFitting(c2_flexPipe, c2);
                                else
                                    union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, c2);

                                if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                {
                                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, instance.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

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
                                    Common.GetConnectorClosedTo(flexPipe.ConnectorManager, instance.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                                    if (!con1.IsConnectedTo(con2))
                                    {
                                        con1.ConnectTo(con2);
                                    }
                                }

                                listIdConnect.Add(flexPipe.Id);
                                listIdConnect.Add(union.Id);

                                Global.UIDoc.Document.Regenerate();
                            }
                            catch (Exception)
                            {
                                tran.RollBack();
                                continue;
                            }

                            // If click cancel button when exporting
                            if (progressBar.IsCancel)
                            {
                                isCancelExport = true;
                                break;
                            }

                            nCount++;

                            dPercent = nCount.ToString() + "/" + sprinklers.Count.ToString();

                            progressBar.tbxMessage.Text = dPercent;

                            progressBar.IncrementProgressBar();

                            CmdDeleteSprinker.CreateSchema(instance, listIdConnect.Select(x => x.ToInt().ToString()).ToList());
                        }
                        catch (Exception ex)
                        {
                            tran.RollBack();
                            continue;
                        }

                        tran.Commit();
                    }

                    if (isCancelExport == false)
                        progressBar.Dispose();

                    tranGr.Assimilate();
                }
            }
            catch (Exception)
            { }
            return false;
        }

        public static bool ProcessType5()
        {
            try
            {
                List<FamilyInstance> sprinklers = sr.SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return false;

                List<Pipe> pipes = SelectPipes();
                if (pipes == null || pipes.Count == 0)
                    return false;

                var pipeIds = (from Pipe p in pipes
                               where p.Id != ElementId.InvalidElementId
                               select p.Id).ToList();

                var height = App.m_FlexSprinklerForm.HorizontalPipeLengthL;

                double radius = App.m_FlexSprinklerForm.MainPipeSprinklerDistance;
                var ft = Common.mmToFT * radius;

                // Status cancel export : default = false
                bool isCancelExport = false;

                // Count type imported
                int nCount = 0;

                // Initialize progress bar
                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;

                string title = Common.GetTextLanguage("PendentFlexibleSprinklerConnectionProgress");
                FrmProcessbar progressBar = new FrmProcessbar(title, Define.MessageFinish, DiritIconTool.Mep);

                progressBar.prgSingle.Minimum = 1;
                progressBar.prgSingle.Maximum = sprinklers.Count;
                progressBar.prgSingle.Value = 1;
                WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                helper.Owner = intPtr;
                progressBar.Show();

                var dFt = App.m_FlexSprinklerForm.PipeSizeC4 * Common.mmToFT;

                using (TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "FlexSprinkler"))
                {
                    tranGr.Start();

                    //Find pipe
                    foreach (Pipe pipe in pipes)
                    {
                        Transaction tran = new Transaction(Global.UIDoc.Document, "CreateConnector");
                        tran.Start();
                        try
                        {
                            // If click cancel button when exporting
                            if (progressBar.IsCancel)
                            {
                                isCancelExport = true;
                                break;
                            }

                            string dPercent = string.Empty;

                            HashSet<ElementId> listIdConnect = new HashSet<ElementId>();

                            var listSprinkler = GetDictSprinklerByPipe(pipe, sprinklers);
                            var locationCurve = (pipe.Location as LocationCurve).Curve;
                            var pipeType = pipe.PipeType;
                            foreach (var sprinkler in listSprinkler)
                            {
                                XYZ locationSprinkler = (sprinkler.Location as LocationPoint).Point;
                                var connectorSprinkler = Common.GetConnectorNotConnnected(sprinkler.MEPModel.ConnectorManager);
                                XYZ locationConnector = connectorSprinkler.Origin;
                                XYZ locationSprinkler2D = Common.To2D(locationSprinkler);

                                Connector conCheck = null;
                                if (locationSprinkler.DistanceTo(locationCurve.GetEndPoint(0)) <= locationSprinkler.DistanceTo(locationCurve.GetEndPoint(1)))
                                    conCheck = Common.GetConnectorClosestTo(pipe, locationCurve.GetEndPoint(0));
                                else
                                    conCheck = Common.GetConnectorClosestTo(pipe, locationCurve.GetEndPoint(1));

                                if (conCheck.IsConnected)
                                    continue;

                                try
                                {
                                    var c1 = ConnectorUtils.GetConnectorNotConnnected(sprinkler.MEPModel.ConnectorManager);
                                    var c2 = conCheck;

                                    FlexPipeType flexPipeType = new FilteredElementCollector(Global.UIDoc.Document).OfCategory(BuiltInCategory.OST_FlexPipeCurves).OfClass(typeof(FlexPipeType)).WhereElementIsElementType().Cast<FlexPipeType>().FirstOrDefault();

                                    List<XYZ> pnts = new List<XYZ>();
                                    pnts.Add(c1.Origin);
                                    pnts.Add(c2.Origin);

                                    var flexPipe = Global.UIDoc.Document.Create.NewFlexPipe(pnts, flexPipeType);

                                    //Set the diameter of flex.
                                    var splinkerDiameter = sprinkler.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault().Radius * 2;

                                    if (App.m_FlexSprinklerForm.IsSprinklerSize)
                                        flexPipe.LookupParameter("Diameter").Set(splinkerDiameter);
                                    else
                                        flexPipe.LookupParameter("Diameter").Set(conCheck.Radius * 2);

                                    flexPipe.StartTangent = c1.CoordinateSystem.BasisZ; //Set Tangent to Conn Direction.
                                    flexPipe.EndTangent = c2.CoordinateSystem.BasisZ.Negate();

                                    var c1_flexPipe = GetConnectorClosestTo(flexPipe, c1.Origin);
                                    var c2_flexPipe = GetConnectorClosestTo(flexPipe, c2.Origin);

                                    FamilyInstance union = null;
                                    if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                        union = Global.UIDoc.Document.Create.NewUnionFitting(c2_flexPipe, c2);
                                    else
                                        union = Global.UIDoc.Document.Create.NewTransitionFitting(c2_flexPipe, c2);

                                    if (!App.m_FlexSprinklerForm.IsSprinklerSize)
                                    {
                                        Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinkler.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

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
                                        Common.GetConnectorClosedTo(flexPipe.ConnectorManager, sprinkler.MEPModel.ConnectorManager, out Connector con1, out Connector con2);

                                        if (!con1.IsConnectedTo(con2))
                                        {
                                            con1.ConnectTo(con2);
                                        }
                                    }

                                    listIdConnect.Add(flexPipe.Id);
                                    listIdConnect.Add(union.Id);

                                    Global.UIDoc.Document.Regenerate();
                                }
                                catch (Exception ex)
                                {
                                    tran.RollBack();
                                    continue;
                                }

                                CmdDeleteSprinker.CreateSchema(sprinkler, listIdConnect.Select(x => x.ToInt().ToString()).ToList());
                            }

                            tran.Commit();

                            // If click cancel button when exporting
                            if (progressBar.IsCancel)
                            {
                                isCancelExport = true;
                                break;
                            }

                            nCount++;

                            dPercent = nCount.ToString() + "/" + pipes.Count().ToString();
                            progressBar.tbxMessage.Text = dPercent;

                            progressBar.IncrementProgressBar();
                        }
                        catch (Exception ex)
                        {
                            tran.RollBack();
                            continue;
                        }
                    }

                    tranGr.Assimilate();
                    progressBar.Dispose();
                }
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        private static List<FamilyInstance> GetDictSprinklerByPipe(Pipe pipe, List<FamilyInstance> listSprinkler)
        {
            List<FamilyInstance> listVal = new List<FamilyInstance>();
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020  || Bundle_2021
            double offset = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);
            double offset3 = UnitUtils.ConvertToInternalUnits(App.m_FlexSprinklerForm.EndPointSprinklerDistance, DisplayUnitType.DUT_MILLIMETERS);
            double offset2 = UnitUtils.ConvertToInternalUnits(App.m_FlexSprinklerForm.MainPipeSprinklerDistance, DisplayUnitType.DUT_MILLIMETERS);
            double offset50 = UnitUtils.ConvertToInternalUnits(50, DisplayUnitType.DUT_MILLIMETERS);
#else
            double offset = UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters);
            double offset3 = UnitUtils.ConvertToInternalUnits(App.m_FlexSprinklerForm.EndPointSprinklerDistance, UnitTypeId.Millimeters);
            double offset2 = UnitUtils.ConvertToInternalUnits(App.m_FlexSprinklerForm.MainPipeSprinklerDistance, UnitTypeId.Millimeters);
            double offset50 = UnitUtils.ConvertToInternalUnits(50, UnitTypeId.Millimeters);
#endif
            Line locationPipe = (pipe.Location as LocationCurve).Curve as Line;
            XYZ p1 = Common.To2D(locationPipe.GetEndPoint(0));
            XYZ p2 = Common.To2D(locationPipe.GetEndPoint(1));

            XYZ p1Origin = locationPipe.GetEndPoint(0);
            XYZ p2Origin = locationPipe.GetEndPoint(1);

            if (CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)) && CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
            {
                return listVal;
            }

            if (p1.DistanceTo(p2) <= Global.UIApp.Application.ShortCurveTolerance)
            {
                return listVal;
            }
            Line locationPipe2DOrigin = Line.CreateBound(p1, p2);

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)))
            {
                p1 += locationPipe2DOrigin.Direction.Negate() * offset3;
            }

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
            {
                p2 += locationPipe2DOrigin.Direction * offset3;
            }

            var locationPipe2D = Line.CreateBound(p1, p2);

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

                listVal.Add(sprinkler);
            }

            if (listVal.Count == 1 || listVal.Count == 0)
            {
                Line lineLocation = pipe.GetCurve() as Line;
                var sprinkler = listVal.FirstOrDefault();
                XYZ locationSprinkler = (sprinkler.Location as LocationPoint).Point;
                Plane planeCheck = Plane.CreateByNormalAndOrigin(lineLocation.Direction, locationSprinkler);

                if (p1Origin.DistanceTo(locationSprinkler) <= p2Origin.DistanceTo(locationSprinkler))
                {
                    XYZ p1Prj = Common.ProjectPointOnPlane(planeCheck, p1Origin);
                    XYZ p1New = p1Prj + (p2Origin - p1Origin).Normalize() * offset50;
                    (pipe.Location as LocationCurve).Curve = Line.CreateBound(p1New, p2Origin);
                }
                else
                {
                    XYZ p2Prj = Common.ProjectPointOnPlane(planeCheck, p2Origin);
                    XYZ p2New = p2Prj + (p1Origin - p2Origin).Normalize() * offset50;
                    (pipe.Location as LocationCurve).Curve = Line.CreateBound(p1Origin, p2New);
                }

                return listVal;
            }
            else
            {
                List<FamilyInstance> listSprinklerNew = new List<FamilyInstance>();
                if (!CheckPipeIsEnd(pipe, p1Origin))
                {
                    double minDistance1 = listVal.Min(x => Common.To2D((x.Location as LocationPoint).Point).DistanceTo(locationPipe.GetEndPoint(0)));

                    var sprinkler = listVal.FirstOrDefault(x => Common.To2D((x.Location as LocationPoint).Point).DistanceTo(locationPipe.GetEndPoint(0)) == minDistance1);

                    if (sprinkler != null)
                    {
                        listSprinklerNew.Add(sprinkler);
                        listVal.Remove(sprinkler);

                        Line lineLocation = pipe.GetCurve() as Line;
                        XYZ locationSprinkler = (sprinkler.Location as LocationPoint).Point;
                        Plane planeCheck = Plane.CreateByNormalAndOrigin(lineLocation.Direction, locationSprinkler);

                        if (p1Origin.DistanceTo(locationSprinkler) <= p2Origin.DistanceTo(locationSprinkler))
                        {
                            XYZ p1Prj = Common.ProjectPointOnPlane(planeCheck, p1Origin);
                            XYZ p1New = p1Prj + (p2Origin - p1Origin).Normalize() * offset50;
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(p1New, p2Origin);
                        }
                        else
                        {
                            XYZ p2Prj = Common.ProjectPointOnPlane(planeCheck, p2Origin);
                            XYZ p2New = p2Prj + (p1Origin - p2Origin).Normalize() * offset50;
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(p1Origin, p2New);
                        }
                    }
                }

                if (!CheckPipeIsEnd(pipe, p2Origin))
                {
                    double minDistance2 = listVal.Min(x => Common.To2D((x.Location as LocationPoint).Point).DistanceTo(p2Origin));
                    var sprinkler = listVal.FirstOrDefault(x => Common.To2D((x.Location as LocationPoint).Point).DistanceTo(p2Origin) == minDistance2);
                    if (sprinkler != null)
                    {
                        listSprinklerNew.Add(sprinkler);
                        listVal.Remove(sprinkler);

                        Line lineLocation = pipe.GetCurve() as Line;
                        XYZ locationSprinkler = (sprinkler.Location as LocationPoint).Point;
                        Plane planeCheck = Plane.CreateByNormalAndOrigin(lineLocation.Direction, locationSprinkler);

                        if (p1Origin.DistanceTo(locationSprinkler) <= p2Origin.DistanceTo(locationSprinkler))
                        {
                            XYZ p1Prj = Common.ProjectPointOnPlane(planeCheck, p1Origin);
                            XYZ p1New = p1Prj + (p2Origin - p1Origin).Normalize() * offset50;
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(p1New, p2Origin);
                        }
                        else
                        {
                            XYZ p2Prj = Common.ProjectPointOnPlane(planeCheck, p2Origin);
                            XYZ p2New = p2Prj + (p1Origin - p2Origin).Normalize() * offset50;
                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(p1Origin, p2New);
                        }
                    }
                }
                return listSprinklerNew;
            }
        }
    }
}
