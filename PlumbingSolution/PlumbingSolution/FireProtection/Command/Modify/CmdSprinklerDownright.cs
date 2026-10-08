using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.ProcessBarForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    [Transaction(TransactionMode.Manual)]
    public class CmdSprinklerDownright : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            return Result.Succeeded;
        }


        public static Result ProcessType1()
        {
            try
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                    App.m_SprinklerDownForm.Hide();

                List<FamilyInstance> sprinklers = sr.SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return Result.Cancelled;

                List<Pipe> pipes = PickPipes();
                if (pipes == null || pipes.Count == 0)
                    return Result.Cancelled;

                var pipeIds = (from Pipe p in pipes
                               where p.Id != ElementId.InvalidElementId
                               select p.Id).ToList();

                var height = App.m_SprinklerDownForm.Height_;

                double radius = App.m_SprinklerDownForm.MainPipeSprinklerDistance;
                var ft = Common.mmToFT * radius;

                // Status cancel export : default = false
                bool isCancelExport = false;

                // Count type imported
                int nCount = 0;

                // Initialize progress bar
                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;

                var strings = new List<string>()
                {
                    "PendentSprinklerConnectionDownProgress",
                };
                var convertedTexts = Common.GetTextLanguage(strings);

                FrmProcessbar progressBar = new FrmProcessbar(convertedTexts[0], Define.MessageFinish, DiritIconTool.Mep);
                progressBar.prgSingle.Minimum = 1;
                progressBar.prgSingle.Maximum = sprinklers.Count;
                progressBar.prgSingle.Value = 1;
                WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                helper.Owner = intPtr;
                progressBar.Show();
                progressBar.Topmost = true;

                using (TransactionGroup trGr = new TransactionGroup(Global.UIDoc.Document, "Sprinkler Down"))
                {
                    trGr.Start();
                    //Find pipe
                    foreach (FamilyInstance instance in sprinklers)
                    {
                        HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                        Transaction tran = new Transaction(Global.UIDoc.Document, "CreateConnector");
                        try
                        {
                            tran.Start();
                            var sprinkle_point = (instance.Location as LocationPoint).Point;

                            //Check connect
                            var connects = instance.MEPModel.ConnectorManager.Connectors;

                            string dPercent = string.Empty;

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
                            (int, int) justification = GetJustification(pipe);
                            SetJustification(pipe, (0, 0));
                            var curve = (pipe.Location as LocationCurve).Curve;

                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            double dTemp = 1000;

                            var line2d = Line.CreateBound(new XYZ(p0.X, p0.Y, 0), new XYZ(p1.X, p1.Y, 0));
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
                                    pipeIds.Add(pipe2.Id);
                            }

                            //Set d = 25
                            Line tempLine = (pipe1.Location as LocationCurve).Curve as Line;

                            var dFt = Common.mmToFT * App.m_SprinklerDownForm.PipeSizeC3;

                            var ft_h = Common.mmToFT * height;

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_v1 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

                            var line_v1 = Line.CreateUnbound(p, XYZ.BasisZ * ft_h * 2);

                            XYZ tmpPoint = line_v1.Evaluate(ft_h, false);

                            p = curve.Project(tmpPoint).XYZPoint;

                            if (!CheckPipeIsEnd1(pipe1, sprinklers, instance, sprinkle_point))
                                p = Line.CreateUnbound((curve as Line).Origin, (curve as Line).Direction).Project(tmpPoint).XYZPoint;

                            line_v1 = Line.CreateBound(p, tmpPoint);
                            (pipe_v1.Location as LocationCurve).Curve = line_v1;

                            pipe_v1.LookupParameter("Diameter").Set(dFt);
                            listIdConnect.Add(pipe_v1.Id);

                            ////Hor
                            var v_v = (new XYZ(sprinkle_point.X, sprinkle_point.Y, 0) - new XYZ(p.X, p.Y, 0)).Normalize();

                            var ft_v = (new XYZ(sprinkle_point.X, sprinkle_point.Y, 0) - new XYZ(p.X, p.Y, 0)).GetLength();

                            var line_Extend = Line.CreateUnbound(line_v1.GetEndPoint(1), ft_v * v_v * 2);

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_hor = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;
                            var line_hor = Line.CreateBound(line_v1.GetEndPoint(1), line_Extend.Evaluate(ft_v, false));

                            (pipe_hor.Location as LocationCurve).Curve = line_hor;

                            pipe_hor.LookupParameter("Diameter").Set(dFt);
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

                            //Vertical 2
                            var line_v2 = Line.CreateBound(line_hor.GetEndPoint(1), sprinkle_point);

                            newPlace = new XYZ(0, 0, 0);
                            elemIds = ElementTransformUtils.CopyElement(
                             Global.UIDoc.Document, pipe1.Id, newPlace);

                            var pipe_v2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;
                            //var center = line_v2.Evaluate((line_v2.GetEndParameter(0) + line_v2.GetEndParameter(1)) / 2, false);
                            XYZ tmpPnt = line_hor.GetEndPoint(1) + XYZ.BasisZ.Negate() * ((line_v2.GetEndParameter(0) + line_v2.GetEndParameter(1)) / 2);

                            (pipe_v2.Location as LocationCurve).Curve = Line.CreateBound(line_v2.GetEndPoint(0), tmpPnt);

                            pipe_v2.LookupParameter("Diameter").Set(dFt);
                            listIdConnect.Add(pipe_v2.Id);
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe1, p);
                                var c3 = Common.GetConnectorClosestTo(pipe_v1, p);

                                if (App.m_SprinklerDownForm.isTeeTap)
                                {
                                    if (GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee && split)
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
                                        var fml = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                        listIdConnect.Add(fml.Id);
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
                                var c1 = Common.GetConnectorClosestTo(pipe_v1, line_v1.GetEndPoint(1));
                                var c2 = Common.GetConnectorClosestTo(pipe_hor, line_v1.GetEndPoint(1));

                                Global.UIDoc.Document.Regenerate();

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c2, c1);
                                listIdConnect.Add(elbow.Id);
                            }
                            catch (System.Exception)
                            {
                            }

                            //Connect
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe_hor, line_hor.GetEndPoint(1));
                                var c2 = Common.GetConnectorClosestTo(pipe_v2, line_hor.GetEndPoint(1));

                                Global.UIDoc.Document.Regenerate();

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c2, c1);
                                listIdConnect.Add(elbow.Id);
                            }
                            catch (System.Exception ex)
                            {
                            }

                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(pipe_v2, tmpPnt);
                                var c2 = Common.GetConnectorClosestTo(instance, tmpPnt);

                                Global.UIDoc.Document.Regenerate();

                                var fml = Global.UIDoc.Document.Create.NewTransitionFitting(c1, c2);

                                listIdConnect.Add(fml.Id);

                                //Common.MovePipeTemporarily(Global.UIDoc.Document, pipe_v2, new XYZ(0, 0, 5));

                                //(pipe_v2.Location as LocationCurve).Curve = Line.CreateBound(line_v2.GetEndPoint(0), tmpPnt);

                                Global.UIDoc.Document.Regenerate();

                                var lc = instance.Location as LocationPoint;
                                if (lc != null)
                                {
                                    var vectorMove = (sprinkle_point - lc.Point).Normalize();
                                    ElementTransformUtils.MoveElement(Global.UIDoc.Document, instance.Id, vectorMove * sprinkle_point.DistanceTo(lc.Point));
                                }
                            }
                            catch (System.Exception ex)
                            {
                            }

                            CmdDeleteSprinker.CreateSchema(instance, listIdConnect.Select(x => x.ToInt().ToString()).ToList());

                            SetJustification(pipe, justification);
                            SetJustification(pipe1, justification);
                            SetJustification(pipe2, justification);
                            SetJustification(pipe_v1, justification);
                            SetJustification(pipe_v2, justification);
                            SetJustification(pipe_hor, justification);

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

                            Global.UIDoc.RefreshActiveView();

                            tran.Commit();
                        }
                        catch (Exception)
                        {
                            tran.RollBack();
                            continue;
                        }
                    }
                    trGr.Assimilate();
                }

                if (isCancelExport == false)
                    progressBar.Dispose();
            }
            catch (Exception)
            { }
            finally
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                {
                    App.m_SprinklerDownForm.Show(App.hWndRevit);
                }
                DisplayService.SetFocus(new HandleRef(null, App.m_SprinklerDownForm.Handle));
            }

            return Result.Succeeded;
        }

        public static (int, int) GetJustification(Pipe pipe)
        {
            if (pipe == null || pipe.IsValidObject == false)
                return (-1, -1);
            Parameter paraVec = pipe.get_Parameter(BuiltInParameter.RBS_CURVE_VERT_OFFSET_PARAM);
            Parameter paraHoz = pipe.get_Parameter(BuiltInParameter.RBS_CURVE_HOR_OFFSET_PARAM);
            if (paraVec != null && paraHoz != null)
                return (paraVec.AsInteger(), paraHoz.AsInteger());
            else
                return (-1, -1);
        }

        public static void SetJustification(Pipe pipe, (int, int) vec_hoz)
        {
            if (pipe == null || pipe.IsValidObject == false)
                return;
            Parameter paraVec = pipe.get_Parameter(BuiltInParameter.RBS_CURVE_VERT_OFFSET_PARAM);
            if (paraVec != null && !paraVec.IsReadOnly && vec_hoz.Item1 != -1)
                paraVec.Set(vec_hoz.Item1);

            Parameter paraHoz = pipe.get_Parameter(BuiltInParameter.RBS_CURVE_HOR_OFFSET_PARAM);
            if (paraHoz != null && !paraHoz.IsReadOnly && vec_hoz.Item2 != -1)
                paraHoz.Set(vec_hoz.Item2);
        }

        public static List<Pipe> PickPipes()
        {
            //Pick pipe
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

        public static Pipe ProcessPipes(List<Element> pipes, XYZ sprinkle_point, out bool bSplit)
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

                if (pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE).AsDouble() > 0.2)
                {
                    continue;
                }

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

                var pipeCheck = pipeNear;
                Line lineCheck = (pipeCheck.Location as LocationCurve).Curve as Line;
                lineCheck = Line.CreateBound(Common.To2D(lineCheck.GetEndPoint(0)), Common.To2D(lineCheck.GetEndPoint(1)));
                Plane planeCheck = Plane.CreateByNormalAndOrigin(lineCheck.Direction.CrossProduct(XYZ.BasisZ), Common.To2D(lineCheck.GetEndPoint(0)));

                XYZ pProject = Common.ProjectPointOnPlane(planeCheck, Common.PointTo2D(sprinkle_point));
                double minVal = double.MaxValue;
                if (Common.PointTo2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(0))) <= minVal)
                    minVal = Common.PointTo2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(0)));

                if (Common.PointTo2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(1))) <= minVal)
                    minVal = Common.PointTo2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(0)));

                //if (!Common.IsBetween2Point(Common.To2D(lineCheck.GetEndPoint(0)), Common.To2D(lineCheck.GetEndPoint(1)), pProject))
                //{
                //    foreach (var pair in keyValuePairs)
                //    {
                //        if (pair.Key.Id != pipeNear.Id)
                //        {
                //            pipeCheck = pair.Key;
                //            lineCheck = (pipeCheck.Location as LocationCurve).Curve as Line;

                //            if (Common.ToPoint2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(0))) <= minVal)
                //            {
                //                minVal = Common.ToPoint2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(0)));
                //                pipeNear = pair.Key;
                //            }

                //            if (Common.ToPoint2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(1))) <= minVal)
                //            {
                //                minVal = Common.ToPoint2D(sprinkle_point).DistanceTo(Common.To2D(lineCheck.GetEndPoint(0)));
                //                pipeNear = pair.Key;
                //            }
                //        }
                //    }
                //}

                var curve = (pipeNear.Location as LocationCurve).Curve;

                var d = (curve as Line).Direction;

                var project = curve.Project(sprinkle_point);

                var p = project.XYZPoint;

                if (p.DistanceTo(curve.GetEndPoint(0)) != 0 && p.DistanceTo(curve.GetEndPoint(1)) != 0)
                    bSplit = true;
            }
            return pipeNear;
        }

        public static void ProcessStartSidePipe(Pipe pipe, out Pipe pipe2, XYZ pOn, out bool isDauOng, bool flagSplit = true)
        {
            var curve = (pipe.Location as LocationCurve).Curve;

            //Create plane
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            //Check co phai dau cuu hoa o gan dau cua ong ko : check trong pham vi 1m - 400mm
            double kc_mm = 1000;
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
                if (IsIntersect(p0) == false && !CheckPipeIsEnd(pipe, pOn) && !App.m_SprinklerDownForm.isTeeTap)
                {
                    isDauOng = true;
                    far = 1;
                }
            }

            if (d2 < km_ft)
            {
                if (IsIntersect(p1) == false && !CheckPipeIsEnd(pipe, pOn) && !App.m_SprinklerDownForm.isTeeTap)
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

        public static void SplitPipe(Pipe pipeOrigin, XYZ splitPoint, out Pipe pipe1, out Pipe pipe2)
        {
            var curve = (pipeOrigin.Location as LocationCurve).Curve;

            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            pipe1 = pipeOrigin;

            //Split
            (pipe1.Location as LocationCurve).Curve = Line.CreateBound(p0, splitPoint);

            var newPlace = new XYZ(0, 0, 0);
            var elemIds = ElementTransformUtils.CopyElement(
              Global.UIDoc.Document, pipeOrigin.Id, newPlace);

            pipe2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

            (pipe2.Location as LocationCurve).Curve = Line.CreateBound(splitPoint, p1);

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
        /// Get Preferred Junction Type
        /// </summary>
        /// <param name="pipe"></param>
        /// <returns></returns>
        public static PreferredJunctionType GetPreferredJunctionType(Pipe pipe)
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



        public static Result ProcessType2()
        {
            try
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                    App.m_SprinklerDownForm.Hide();

                // Get selected sprinkler
                List<FamilyInstance> selSprinklers = sr.SelectSprinklers();
                if (selSprinklers == null || selSprinklers.Count == 0)
                    return Result.Cancelled;

                // Get selected main pipe
                List<Pipe> selPipes = PickPipes();
                if (selPipes == null || selPipes.Count == 0)
                    return Result.Cancelled;

                // Get selected main pipe id
                List<ElementId> selPipeIds = selPipes.Where(item => item.Id != ElementId.InvalidElementId).Select(item => item.Id).ToList();

                // Process

                try
                {
                    double invalidRadius_mm = App.m_SprinklerDownForm.MainPipeSprinklerDistance;
                    double invalidRadius_ft = Common.mmToFT * invalidRadius_mm;

                    // Status cancel export : default = false
                    bool isCancelExport = false;

                    // Count type imported
                    int nCount = 0;
                    var strings = new List<string>()
                    {
                        "PendentSprinklerConnectionDownProgress",
                    };
                    var convertedTexts = Common.GetTextLanguage(strings);

                    // Initialize progress bar
                    FrmProcessbar progressBar = new FrmProcessbar(convertedTexts[0], Define.MessageFinish, DiritIconTool.Mep);
                    progressBar.prgSingle.Minimum = 1;
                    progressBar.prgSingle.Maximum = selSprinklers.Count;
                    progressBar.prgSingle.Value = 1;
                    progressBar.Show();
                    progressBar.Topmost = true;

                    using (TransactionGroup trGr = new TransactionGroup(Global.UIDoc.Document, "SprinklerDown"))
                    {
                        trGr.Start();
                        foreach (FamilyInstance sprinkler in selSprinklers)
                        {
                            HashSet<ElementId> listIdConnect = new HashSet<ElementId>();

                            Transaction reTrans = new Transaction(Global.UIDoc.Document, "SPRINKLER_DOWN_RIGHT_TYPE_3");
                            reTrans.Start();

                            string dPercent = string.Empty;
                            // Location sprinkler
                            XYZ locSprinkler = (sprinkler.Location as LocationPoint).Point;
                            XYZ locSprinkler1 = (sprinkler.Location as LocationPoint).Point;

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

                            List<Connector> lstConnectorSprinkler = cntSetOfIns?.Cast<Connector>().ToList();

                            Connector cntOfIns_1 = Common.ToList(sprinkler.MEPModel.ConnectorManager.Connectors).OrderByDescending(x => x.Origin.Z).FirstOrDefault();

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
                            var curve = (processPipe.Location as LocationCurve).Curve;

                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            var sprinker2d = Common.PointTo2D(locSprinkler);
                            var p02d = Common.PointTo2D(p0);
                            var p12d = Common.PointTo2D(p1);

                            var resultPipe = Line.CreateUnbound((curve as Line).Origin, (curve as Line).Direction).Project(locSprinkler);

                            XYZ pointProject = resultPipe.XYZPoint;
                            XYZ pointProject2d = Common.PointTo2D(pointProject);

                            if ((double)Common.GetValueParameterByBuilt(processPipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0 && App.m_SprinklerDownForm.isTeeTap
                                 && !Common.IsParallel((curve as Line).Direction, XYZ.BasisX) && !Common.IsParallel((curve as Line).Direction, XYZ.BasisY))
                                pointProject2d = new XYZ(pointProject2d.X + 0.001, pointProject2d.Y, pointProject2d.Z);
                            var distancePoint2d = pointProject2d.DistanceTo(sprinker2d);
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
                            if (!Common.IsEqual(distancePoint2d, 0) && distancePoint2d <= UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS))
                            {
                                var vector = pointProject2d - sprinker2d;

                                ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, vector.Normalize() * pointProject2d.DistanceTo(sprinker2d));
                                Global.UIDoc.Document.Regenerate();

                                locSprinkler = (sprinkler.Location as LocationPoint).Point;
                            }
#else
                            if (!Common.IsEqual(distancePoint2d, 0) && distancePoint2d <= UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters))
                            {
                                var vector = pointProject2d - sprinker2d;

                                ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, vector.Normalize() * pointProject2d.DistanceTo(sprinker2d));
                                Global.UIDoc.Document.Regenerate();

                                locSprinkler = (sprinkler.Location as LocationPoint).Point;
                            }
#endif

                            // Process main pipe
                            Curve curveProcessPipe = (processPipe.Location as LocationCurve).Curve;

                            XYZ firstPnt_ProcessPipe = curveProcessPipe.GetEndPoint(0);
                            XYZ secondPnt_ProcessPipe = curveProcessPipe.GetEndPoint(1);

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

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curveExpand.Intersect(curveProcessPipe_crossProduct_2d, CurveIntersectResultOption.Detailed);
    inter = intersectResult.Result;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
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
    // Lấy điểm giao trên bản mới
    var p2d = intersectResult.GetOverlaps()[0].Point;
#else
                                // Lấy điểm giao trên bản cũ
                                var p2d = intRetArr.get_Item(0).XYZPoint;
#endif

                                var p3d = new XYZ(p2d.X, p2d.Y, locSprinkler.Z);

                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTempEvaluate));
                                var curveExtend3d_temp = Line.CreateUnbound(curveProcessPipe.GetEndPoint(index), (curveProcessPipe as Line).Direction * 100);

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    intersectResult = curveExtend3d_temp.Intersect(line3d, CurveIntersectResultOption.Detailed);
    inter = intersectResult.Result;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                intRetArr = new IntersectionResultArray();
                                inter = curveExtend3d_temp.Intersect(line3d, out intRetArr);
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
    // Lấy điểm giao trên bản mới
    finalIntPnt = intersectResult.GetOverlaps()[0].Point;
#else
                                // Lấy điểm giao trên bản cũ
                                finalIntPnt = intRetArr.get_Item(0).XYZPoint;
#endif

                                if (curveProcessPipe.GetEndPoint(0).DistanceTo(finalIntPnt) < curveProcessPipe.GetEndPoint(1).DistanceTo(finalIntPnt))
                                    (processPipe.Location as LocationCurve).Curve = Line.CreateBound(finalIntPnt, curveProcessPipe.GetEndPoint(1));
                                else
                                    (processPipe.Location as LocationCurve).Curve = Line.CreateBound(curveProcessPipe.GetEndPoint(0), finalIntPnt);
                            }
                            else
                            {
                                SetComparisonResult inter;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curveProcessPipe_crossProduct_2d.Intersect(curveProcessPipe_2d, CurveIntersectResultOption.Detailed);
    inter = intersectResult.Result;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
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
    // Lấy điểm giao trên bản mới
    var p2d = intersectResult.GetOverlaps()[0].Point;
#else
                                // Lấy điểm giao trên bản cũ
                                var p2d = intRetArr.get_Item(0).XYZPoint;
#endif

                                var p3d = new XYZ(p2d.X, p2d.Y, locSprinkler.Z);

                                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTempEvaluate));

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    intersectResult = curveProcessPipe.Intersect(line3d, CurveIntersectResultOption.Detailed);
    inter = intersectResult.Result;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
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
    // Lấy điểm giao trên bản mới
    finalIntPnt = intersectResult.GetOverlaps()[0].Point;
#else
                                // Lấy điểm giao trên bản cũ
                                finalIntPnt = intRetArr.get_Item(0).XYZPoint;
#endif

                                temp_processPipe_2 = null;
                                bool flagCreateTee = true;
                                if (GetPreferredJunctionType(processPipe) != PreferredJunctionType.Tee)
                                {
                                    flagCreateTee = false;
                                }

                                temp_processPipe_2 = CreateTeeByPipe(processPipe, finalIntPnt, flagCreateTee);

                                if (temp_processPipe_2 != null)
                                {
                                    selPipeIds.Add(temp_processPipe_2.Id);
                                }
                            }

                            //Set pipe size
                            var dPipeSizeFt = Common.mmToFT * App.m_SprinklerDownForm.PipeSizeC3;

                            // Generate Pipe Horizontal
                            Line curveProcessPipeUnb = Line.CreateUnbound(curveProcessPipe.GetEndPoint(0), (curveProcessPipe as Line).Direction);
                            var finalIntPnt1 = curveProcessPipeUnb.Project(locSprinkler).XYZPoint;

                            //var line_Extend = Line.CreateBound(finalIntPnt1, locSprinkler);

                            Pipe horizontal_pipe = Pipe.Create(Global.UIDoc.Document, temp_processPipe_1.MEPSystem.GetTypeId(), temp_processPipe_1.GetTypeId(), temp_processPipe_1.ReferenceLevel.Id, finalIntPnt1, cntOfIns_1.Origin);
                            horizontal_pipe.LookupParameter("Diameter").Set(dPipeSizeFt);

                            listIdConnect.Add(horizontal_pipe.Id);

                            Global.UIDoc.Document.Regenerate();
                            // Connect horizontal pipe with main pipe
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(temp_processPipe_1, finalIntPnt);
                                var c2 = Common.GetConnectorClosestTo(horizontal_pipe, finalIntPnt);

                                if (App.m_SprinklerDownForm.isTeeTap)
                                {
                                    if (GetPreferredJunctionType(temp_processPipe_1) != PreferredJunctionType.Tee && isSplit == true)
                                    {
                                        CreateTap(temp_processPipe_1 as MEPCurve, horizontal_pipe as MEPCurve, ref listIdConnect);
                                    }
                                    else
                                    {
                                        if (temp_processPipe_2 != null)
                                        {
                                            var c3 = Common.GetConnectorClosestTo(temp_processPipe_2, finalIntPnt);

                                            Global.UIDoc.Document.Regenerate();

                                            bool isPipePen = IsPipePerpendicular(temp_processPipe_1, horizontal_pipe);
                                            bool isPipePen2 = IsPipePerpendicular(temp_processPipe_2, horizontal_pipe);

                                            FamilyInstance fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c3, c2);

                                            if (fitting != null)
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
                                            if (CheckPipeIsEnd1(processPipe, selSprinklers, sprinkler, locSprinkler))
                                                CreateTap(temp_processPipe_1 as MEPCurve, horizontal_pipe as MEPCurve, ref listIdConnect);
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                        else
                                        {
                                            if (temp_processPipe_2 != null)
                                            {
                                                var c3 = Common.GetConnectorClosestTo(temp_processPipe_2, finalIntPnt);

                                                var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c3, c2);

                                                listIdConnect.Add(fitting.Id);
                                            }
                                            else
                                            {
                                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                                listIdConnect.Add(elbow.Id);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                        listIdConnect.Add(elbow.Id);
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                reTrans.RollBack();
                                continue;
                            }

                            // Connect vertical pipe 2 with sprinkler
                            try
                            {
                                //Connect horizontal pipe with vertical pipe 2
                                FamilyInstance fml = null;

                                Connector c1 = Common.GetConnectorClosestTo(horizontal_pipe, locSprinkler);
                                Connector c2 = Common.GetConnectorClosestTo(sprinkler, locSprinkler);

                                Connector connector1 = lstConnectorSprinkler.OrderByDescending(x => x.Origin.Z).FirstOrDefault();

                                fml = Global.UIDoc.Document.Create.NewTransitionFitting(c1, connector1);

                                Global.UIDoc.Document.Regenerate();

                                if (fml != null)
                                    listIdConnect.Add(fml.Id);

                                if ((double)Common.GetValueParameterByBuilt(processPipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0
                                 && !Common.IsParallel((curve as Line).Direction, XYZ.BasisX) && !Common.IsParallel((curve as Line).Direction, XYZ.BasisY))
                                {
                                    ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, locSprinkler1 - locSprinkler);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                reTrans.RollBack();
                                continue;
                            }

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
                            reTrans.Commit();
                        }
                        trGr.Assimilate();
                    }

                    if (isCancelExport == false)
                        progressBar.Dispose();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                {
                    App.m_SprinklerDownForm.Show(App.hWndRevit);
                }

                DisplayService.SetFocus(new HandleRef(null, App.m_SprinklerDownForm.Handle));
            }

            return Result.Succeeded;
        }

        public static Result ProcessType4()
        {
            try
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                    App.m_SprinklerDownForm.Hide();

                while (true)
                {
                    var sprinkler = Global.UIDoc.Document.GetElement(Global.UIDoc.Selection.PickObject(ObjectType.Element, new SprinklerFilter(), "Pick Sprinklers: ")) as FamilyInstance;
                    if (sprinkler == null)
                        return Result.Cancelled;

                    var pipe = Global.UIDoc.Document.GetElement(Global.UIDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter/*PipeFilter*/(), "Pick pipes: ")) as Pipe;

                    // Get selected main pipe id
                    List<ElementId> selPipeIds = new List<ElementId>() { pipe.Id };

                    // Process

                    try
                    {
                        double invalidRadius_mm = App.m_SprinklerDownForm.MainPipeSprinklerDistance;
                        double invalidRadius_ft = Common.mmToFT * invalidRadius_mm;

                        // Status cancel export : default = false
                        bool isCancelExport = false;

                        using (TransactionGroup trGr = new TransactionGroup(Global.UIDoc.Document, "SprinklerDown"))
                        {
                            trGr.Start();

                            HashSet<ElementId> listIdConnect = new HashSet<ElementId>();

                            Transaction reTrans = new Transaction(Global.UIDoc.Document, "SPRINKLER_DOWN_RIGHT_TYPE_3");
                            reTrans.Start();

                            string dPercent = string.Empty;
                            // Location sprinkler
                            XYZ locSprinkler = (sprinkler.Location as LocationPoint).Point;
                            XYZ locSprinkler1 = (sprinkler.Location as LocationPoint).Point;

                            // Check valid connect
                            ConnectorSet cntSetOfIns = sprinkler.MEPModel.ConnectorManager.Connectors;

                            if (cntSetOfIns.Size == 0)
                            {
                                reTrans.RollBack();
                            }

                            List<Connector> lstConnectorSprinkler = cntSetOfIns?.Cast<Connector>().ToList();

                            Connector cntOfIns_1 = Common.ToList(sprinkler.MEPModel.ConnectorManager.Connectors).OrderByDescending(x => x.Origin.Z).FirstOrDefault();

                            if (cntOfIns_1.IsConnected == true)
                            {
                                reTrans.RollBack();
                            }

                            // Find intersection with sprinkler
                            var cylindricalFromIns = Common.CreateCylindricalVolume(locSprinkler, invalidRadius_ft * 5, invalidRadius_ft, true);
                            if (cylindricalFromIns == null)
                            {
                                reTrans.RollBack();
                            }

                            FilteredElementCollector filterCollector = new FilteredElementCollector(Global.UIDoc.Document, selPipeIds).OfClass(typeof(Pipe)).WherePasses(new ElementIntersectsSolidFilter(cylindricalFromIns));
                            if (filterCollector == null || filterCollector.GetElementCount() <= 0)
                            {
                                reTrans.RollBack();
                            }

                            IList<Element> validPipes = filterCollector.ToElements();

                            // Check pipe nesscesary split
                            bool isSplit = false;

                            Pipe processPipe = ProcessPipes(validPipes.ToList(), locSprinkler, out isSplit);
                            var curve = (processPipe.Location as LocationCurve).Curve;

                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            var sprinker2d = Common.PointTo2D(locSprinkler);
                            var p02d = Common.PointTo2D(p0);
                            var p12d = Common.PointTo2D(p1);

                            var resultPipe = Line.CreateUnbound((curve as Line).Origin, (curve as Line).Direction).Project(locSprinkler);

                            XYZ pointProject = resultPipe.XYZPoint;
                            XYZ pointProject2d = Common.PointTo2D(pointProject);

                            if ((double)Common.GetValueParameterByBuilt(processPipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0 && App.m_SprinklerDownForm.isTeeTap
                                 && !Common.IsParallel((curve as Line).Direction, XYZ.BasisX) && !Common.IsParallel((curve as Line).Direction, XYZ.BasisY))
                                pointProject2d = new XYZ(pointProject2d.X + 0.001, pointProject2d.Y, pointProject2d.Z);
                            var distancePoint2d = pointProject2d.DistanceTo(sprinker2d);
#if Debug_2020 || Debug_2021 || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
                            if (!Common.IsEqual(distancePoint2d, 0) && distancePoint2d <= UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS))
                            {
                                var vector = pointProject2d - sprinker2d;

                                ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, vector.Normalize() * pointProject2d.DistanceTo(sprinker2d));
                                Global.UIDoc.Document.Regenerate();

                                locSprinkler = (sprinkler.Location as LocationPoint).Point;
                            }
#else
                            if (!Common.IsEqual(distancePoint2d, 0) && distancePoint2d <= UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters))
                            {
                                var vector = pointProject2d - sprinker2d;

                                ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, vector.Normalize() * pointProject2d.DistanceTo(sprinker2d));
                                Global.UIDoc.Document.Regenerate();

                                locSprinkler = (sprinkler.Location as LocationPoint).Point;
                            }
#endif

                            // Process main pipe
                            Curve curveProcessPipe = (processPipe.Location as LocationCurve).Curve;

                            XYZ firstPnt_ProcessPipe = curveProcessPipe.GetEndPoint(0);
                            XYZ secondPnt_ProcessPipe = curveProcessPipe.GetEndPoint(1);

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
                                // Cấu hình bản cũ
                                inter = curveExpand.Intersect(curveProcessPipe_crossProduct_2d, out intRetArr);
#endif

                                if (inter != SetComparisonResult.Overlap)
                                {
                                    reTrans.RollBack();
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
                                    reTrans.RollBack();
                                }

#if Debug_2027 || Release_2027 || Release_2026
    finalIntPnt = intersectResult2.GetOverlaps()[0].Point;
#else
                                finalIntPnt = intRetArr.get_Item(0).XYZPoint;
#endif

                                if (curveProcessPipe.GetEndPoint(0).DistanceTo(finalIntPnt) < curveProcessPipe.GetEndPoint(1).DistanceTo(finalIntPnt))
                                    (processPipe.Location as LocationCurve).Curve = Line.CreateBound(finalIntPnt, curveProcessPipe.GetEndPoint(1));
                                else
                                    (processPipe.Location as LocationCurve).Curve = Line.CreateBound(curveProcessPipe.GetEndPoint(0), finalIntPnt);
                            }
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
                                    reTrans.RollBack();
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
                                    reTrans.RollBack();
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

                                temp_processPipe_2 = CreateTeeByPipe(processPipe, finalIntPnt, flagCreateTee);

                                if (temp_processPipe_2 != null)
                                {
                                    selPipeIds.Add(temp_processPipe_2.Id);
                                }
                            }

                            //Set pipe size
                            var dPipeSizeFt = Common.mmToFT * App.m_SprinklerDownForm.PipeSizeC3;

                            // Generate Pipe Horizontal
                            Line curveProcessPipeUnb = Line.CreateUnbound(curveProcessPipe.GetEndPoint(0), (curveProcessPipe as Line).Direction);
                            var finalIntPnt1 = curveProcessPipeUnb.Project(locSprinkler).XYZPoint;

                            //var line_Extend = Line.CreateBound(finalIntPnt1, locSprinkler);

                            Pipe horizontal_pipe = Pipe.Create(Global.UIDoc.Document, temp_processPipe_1.MEPSystem.GetTypeId(), temp_processPipe_1.GetTypeId(), temp_processPipe_1.ReferenceLevel.Id, finalIntPnt1, cntOfIns_1.Origin);
                            horizontal_pipe.LookupParameter("Diameter").Set(dPipeSizeFt);

                            listIdConnect.Add(horizontal_pipe.Id);

                            Global.UIDoc.Document.Regenerate();
                            // Connect horizontal pipe with main pipe
                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(temp_processPipe_1, finalIntPnt);
                                var c2 = Common.GetConnectorClosestTo(horizontal_pipe, finalIntPnt);

                                if (App.m_SprinklerDownForm.isTeeTap)
                                {
                                    if (GetPreferredJunctionType(temp_processPipe_1) != PreferredJunctionType.Tee && isSplit == true)
                                    {
                                        CreateTap(temp_processPipe_1 as MEPCurve, horizontal_pipe as MEPCurve, ref listIdConnect);
                                    }
                                    else
                                    {
                                        if (temp_processPipe_2 != null)
                                        {
                                            var c3 = Common.GetConnectorClosestTo(temp_processPipe_2, finalIntPnt);

                                            Global.UIDoc.Document.Regenerate();

                                            bool isPipePen = IsPipePerpendicular(temp_processPipe_1, horizontal_pipe);
                                            bool isPipePen2 = IsPipePerpendicular(temp_processPipe_2, horizontal_pipe);

                                            FamilyInstance fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c3, c2);

                                            if (fitting != null)
                                                listIdConnect.Add(fitting.Id);
                                        }
                                        else
                                        {
                                            reTrans.RollBack();
                                        }
                                    }
                                }
                                else
                                {
                                    var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c2);
                                    listIdConnect.Add(elbow.Id);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                reTrans.RollBack();
                                break;
                            }

                            // Connect vertical pipe 2 with sprinkler
                            try
                            {
                                //Connect horizontal pipe with vertical pipe 2
                                FamilyInstance fml = null;

                                Connector c1 = Common.GetConnectorClosestTo(horizontal_pipe, locSprinkler);
                                Connector c2 = Common.GetConnectorClosestTo(sprinkler, locSprinkler);

                                Connector connector1 = lstConnectorSprinkler.OrderByDescending(x => x.Origin.Z).FirstOrDefault();

                                fml = Global.UIDoc.Document.Create.NewTransitionFitting(c1, connector1);

                                Global.UIDoc.Document.Regenerate();

                                if (fml != null)
                                    listIdConnect.Add(fml.Id);

                                if ((double)Common.GetValueParameterByBuilt(processPipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0
                                 && !Common.IsParallel((curve as Line).Direction, XYZ.BasisX) && !Common.IsParallel((curve as Line).Direction, XYZ.BasisY))
                                {
                                    ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, locSprinkler1 - locSprinkler);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                reTrans.RollBack();
                            }

                            CmdDeleteSprinker.CreateSchema(sprinkler, listIdConnect.Select(x => x.ToInt().ToString()).ToList());
                            reTrans.Commit();

                            trGr.Assimilate();
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                {
                    App.m_SprinklerDownForm.Show(App.hWndRevit);
                }

                DisplayService.SetFocus(new HandleRef(null, App.m_SprinklerDownForm.Handle));
            }

            return Result.Succeeded;
        }

        public static Pipe CreateTeeByPipe(Pipe pipeMain, XYZ point, bool isChangeToTee)
        {
            Pipe pipe2 = null;
            var old = pipeMain.PipeType.RoutingPreferenceManager.PreferredJunctionType;
            if (GetPreferredJunctionType(pipeMain) == PreferredJunctionType.Tee || isChangeToTee)
                ProcessStartSidePipe(pipeMain, out pipe2, point, out bool isDauOng, true);

            if (isChangeToTee)
                pipeMain.PipeType.RoutingPreferenceManager.PreferredJunctionType = old;

            return pipe2;
        }

        private static bool IsPipePerpendicular(Pipe p1, Pipe p2)
        {
            Document doc = p1.Document;

            LocationCurve locCurve1 = p1.Location as LocationCurve;
            LocationCurve locCurve2 = p2.Location as LocationCurve;

            if (locCurve1 == null || locCurve2 == null)
                return false;

            XYZ dir1 = (locCurve1.Curve as Line)?.Direction;
            XYZ dir2 = (locCurve2.Curve as Line)?.Direction;

            if (dir1 == null || dir2 == null)
                return false;

            double dotProduct = dir1.DotProduct(dir2);

            return Math.Abs(dotProduct) < 1e-6;
        }

        /// <summary>
        /// get parameter value based on its storage type
        /// </summary>
        private static dynamic GetParameterValue(Parameter parameter)
        {
            if (parameter != null && parameter.HasValue)
            {
                switch (parameter.StorageType)
                {
                    case StorageType.Double:
                        return parameter.AsDouble();

                    case StorageType.ElementId:
                        return parameter.AsElementId();

                    case StorageType.Integer:
                        return parameter.AsInteger();

                    case StorageType.String:
                        return parameter.AsString();
                }
            }
            return null;
        }

        /// <summary>
        /// Get parameter value by name
        /// </summary>
        private static dynamic GetParameterValueByName(Element elem, string paramName)
        {
            if (elem != null)
            {
                Parameter parameter = elem.LookupParameter(paramName);
                return GetParameterValue(parameter);
            }
            return null;
        }

        public static Pipe ConnectTee(Pipe pipe, Pipe newPipeZ)
        {
            Pipe pipe1 = null;
            var curvePipeMain = pipe.GetCurve();

            var curvePipeDung = newPipeZ.GetCurve();

            //Line line = Line.CreateUnbound((curvePipeDung as Line).Origin, (curvePipeDung as Line).Direction * 100);

            //IntersectionResultArray array;

            //line.Intersect(curvePipeMain, out array);
            //var point = array.get_Item(0).XYZPoint;

            var result = curvePipeMain.Project((curvePipeDung as Line).Origin);

            var point = result.XYZPoint;

            sr.CreateTeeFitting(pipe, newPipeZ, point, out pipe1);

            return pipe1;
        }



        public static Result ProcessType3()
        {
            try
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                    App.m_SprinklerDownForm.Hide();

                // Get selected sprinkler
                List<FamilyInstance> selSprinklers = sr.SelectSprinklers();
                if (selSprinklers == null || selSprinklers.Count == 0)
                    return Result.Cancelled;

                // Get selected main pipe
                List<Pipe> selPipes = PickPipes();
                if (selPipes == null || selPipes.Count == 0)
                    return Result.Cancelled;

                // Get selected main pipe id
                List<ElementId> selPipeIds = selPipes.Where(item => item.Id != ElementId.InvalidElementId).Select(item => item.Id).ToList();

                // Process

                try
                {
                    double invalidRadius_mm = App.m_SprinklerDownForm.MainPipeSprinklerDistance;
                    double invalidRadius_ft = Common.mmToFT * invalidRadius_mm;

                    // Status cancel export : default = false
                    bool isCancelExport = false;

                    // Count type imported
                    int nCount = 0;

                    var strings = new List<string>()
                    {
                        "PendentSprinklerConnectionDownProgress",
                    };
                    var convertedTexts = Common.GetTextLanguage(strings);

                    // Initialize progress bar
                    FrmProcessbar progressBar = new FrmProcessbar(convertedTexts[0], Define.MessageFinish, DiritIconTool.Mep);
                    progressBar.prgSingle.Minimum = 1;
                    progressBar.prgSingle.Maximum = selSprinklers.Count;
                    progressBar.prgSingle.Value = 1;
                    progressBar.Show();
                    progressBar.Topmost = true;
                    using (TransactionGroup trGr = new TransactionGroup(Global.UIDoc.Document, "SprinklerDown"))
                    {
                        trGr.Start();
                        foreach (FamilyInstance sprinkler in selSprinklers)
                        {
                            HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                            Transaction reTrans = new Transaction(Global.UIDoc.Document, "SPRINKLER_DOWN_RIGHT_TYPE_3");
                            try
                            {
                                reTrans.Start();
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
                                (int, int) justification = GetJustification(processPipe);
                                SetJustification(processPipe, (0, 0));
                                // Process main pipe
                                Curve curveProcessPipe = (processPipe.Location as LocationCurve).Curve;

                                XYZ firstPnt_ProcessPipe = curveProcessPipe.GetEndPoint(0);
                                XYZ secondPnt_ProcessPipe = curveProcessPipe.GetEndPoint(1);

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
#if Debug_2027 || Release_2027 || Release_2026
    var intersectResult1 = curveExpand.Intersect(curveProcessPipe_crossProduct_2d, CurveIntersectResultOption.Detailed);
    inter = intersectResult1.Result;
#else
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

                                    var p3d = new XYZ(p2d.X, p2d.Y, locSprinkler.Z);
                                    var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTempEvaluate));
                                    var curveExtend3d_temp = Line.CreateUnbound(curveProcessPipe.GetEndPoint(index), (curveProcessPipe as Line).Direction * 100);

#if Debug_2027 || Release_2027 || Release_2026
    var intersectResult2 = curveExtend3d_temp.Intersect(line3d, CurveIntersectResultOption.Detailed);
    inter = intersectResult2.Result;
#else
                                    intRetArr = new IntersectionResultArray();
                                    inter = curveExtend3d_temp.Intersect(line3d, out intRetArr);
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
                                }

                                // Truong hop o giua ong
                                else
                                {
                                    SetComparisonResult inter;
#if Debug_2027 || Release_2027 || Release_2026
    var intersectResult3 = curveProcessPipe_crossProduct_2d.Intersect(curveProcessPipe_2d, CurveIntersectResultOption.Detailed);
    inter = intersectResult3.Result;
#else
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
    var p2d = intersectResult3.GetOverlaps()[0].Point;
#else
                                    var p2d = intRetArr.get_Item(0).XYZPoint;
#endif

                                    var p3d = new XYZ(p2d.X, p2d.Y, locSprinkler.Z);
                                    var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + dTempEvaluate));

#if Debug_2027 || Release_2027 || Release_2026
    var intersectResult4 = curveProcessPipe.Intersect(line3d, CurveIntersectResultOption.Detailed);
    inter = intersectResult4.Result;
#else
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
    finalIntPnt = intersectResult4.GetOverlaps()[0].Point;
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

                                //Set pipe size
                                var dPipeSizeFt = Common.mmToFT * App.m_SprinklerDownForm.PipeSizeC3;

                                // Generate Pipe Horizontal
                                var v_v = (new XYZ(locSprinkler.X, locSprinkler.Y, 0) - new XYZ(finalIntPnt.X, finalIntPnt.Y, 0)).Normalize();
                                var ft_v = (new XYZ(locSprinkler.X, locSprinkler.Y, 0) - new XYZ(finalIntPnt.X, finalIntPnt.Y, 0)).GetLength();
                                var line_Extend = Line.CreateUnbound(finalIntPnt, ft_v * v_v * 2);

                                newPlace = new XYZ(0, 0, 0);
                                elemIds = ElementTransformUtils.CopyElement(
                                 Global.UIDoc.Document, temp_processPipe_1.Id, newPlace);

                                var horizontal_pipe = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

                                var hor_line = Line.CreateBound(finalIntPnt, line_Extend.Evaluate(ft_v, false));
                                (horizontal_pipe.Location as LocationCurve).Curve = hor_line;
                                horizontal_pipe.LookupParameter("Diameter").Set(dPipeSizeFt);
                                listIdConnect.Add(horizontal_pipe.Id);
                                // Connect horizontal pipe with main pipe
                                try
                                {
                                    var c1 = Common.GetConnectorClosestTo(temp_processPipe_1, finalIntPnt);
                                    var c3 = Common.GetConnectorClosestTo(horizontal_pipe, finalIntPnt);

                                    if (App.m_SprinklerDownForm.isTeeTap)
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

                                newPlace = new XYZ(0, 0, 0);
                                elemIds = ElementTransformUtils.CopyElement(
                                 Global.UIDoc.Document, temp_processPipe_1.Id, newPlace);

                                var pipe_v2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;
                                XYZ tmpPnt = hor_line.GetEndPoint(1) + XYZ.BasisZ.Negate() * ((line_v2.GetEndParameter(0) + line_v2.GetEndParameter(1)) / 2);

                                (pipe_v2.Location as LocationCurve).Curve = Line.CreateBound(line_v2.GetEndPoint(0), tmpPnt);

                                pipe_v2.LookupParameter("Diameter").Set(dPipeSizeFt);
                                listIdConnect.Add(pipe_v2.Id);
                                //Connect horizontal pipe with vertical pipe 2
                                try
                                {
                                    var c1 = Common.GetConnectorClosestTo(horizontal_pipe, hor_line.GetEndPoint(1));
                                    var c2 = Common.GetConnectorClosestTo(pipe_v2, hor_line.GetEndPoint(1));

                                    var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c2, c1);
                                    listIdConnect.Add(elbow.Id);
                                }
                                catch (System.Exception ex)
                                {
                                    reTrans.RollBack();
                                    continue;
                                }
                                // Connect vertical pipe 2 with sprinkler
                                try
                                {
                                    var c1 = Common.GetConnectorClosestTo(pipe_v2, tmpPnt);
                                    var c2 = Common.GetConnectorClosestTo(sprinkler, tmpPnt);

                                    var fml = Global.UIDoc.Document.Create.NewTransitionFitting(c1, c2);
                                    listIdConnect.Add(fml.Id);

                                    //Common.MovePipeTemporarily(Global.UIDoc.Document, pipe_v2, new XYZ(0, 0, 5));

                                    //(pipe_v2.Location as LocationCurve).Curve = Line.CreateBound(line_v2.GetEndPoint(0), tmpPnt);

                                    Global.UIDoc.Document.Regenerate();

                                    var lc = sprinkler.Location as LocationPoint;
                                    if (lc != null)
                                    {
                                        var vectorMove = (locSprinkler - lc.Point).Normalize();
                                        ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, vectorMove * locSprinkler.DistanceTo(lc.Point));
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    reTrans.RollBack();
                                    continue;
                                }

                                SetJustification(pipe_v2, justification);
                                SetJustification(horizontal_pipe, justification);
                                SetJustification(processPipe, justification);
                                SetJustification(temp_processPipe_2, justification);
                                SetJustification(temp_processPipe_1, justification);

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

                                reTrans.Commit();
                            }
                            catch (Exception)
                            {
                                reTrans.RollBack();
                                continue;
                            }
                        }

                        trGr.Assimilate();
                    }

                    if (isCancelExport == false)
                        progressBar.Dispose();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                {
                    App.m_SprinklerDownForm.Show(App.hWndRevit);
                }
                DisplayService.SetFocus(new HandleRef(null, App.m_SprinklerDownForm.Handle));
            }

            return Result.Succeeded;
        }



        public static Result ProcessType5()
        {
            try
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                    App.m_SprinklerDownForm.Hide();

                // Process

                try
                {
                    double invalidRadius_mm = App.m_SprinklerDownForm.MainPipeSprinklerDistance;
                    double invalidRadius_ft = Common.mmToFT * invalidRadius_mm;

                    while (true)
                    {
                        using (TransactionGroup trGr = new TransactionGroup(Global.UIDoc.Document, "SprinklerDown"))
                        {
                            trGr.Start();

                            var sprinkler = Global.UIDoc.Document.GetElement(Global.UIDoc.Selection.PickObject(ObjectType.Element, new SprinklerFilter(), "Pick Sprinklers: ")) as FamilyInstance;
                            if (sprinkler == null)
                                return Result.Cancelled;

                            var pipe = Global.UIDoc.Document.GetElement(Global.UIDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter/*PipeFilter*/(), "Pick pipes: ")) as Pipe;

                            HashSet<ElementId> listIdConnect = new HashSet<ElementId>();

                            Transaction reTrans = new Transaction(Global.UIDoc.Document, "SPRINKLER_DOWN_RIGHT_TYPE_3");
                            reTrans.Start();

                            string dPercent = string.Empty;
                            // Location sprinkler
                            XYZ locSprinkler = (sprinkler.Location as LocationPoint).Point;
                            XYZ locSprinkler1 = (sprinkler.Location as LocationPoint).Point;

                            // Check valid connect
                            ConnectorSet cntSetOfIns = sprinkler.MEPModel.ConnectorManager.Connectors;

                            if (cntSetOfIns.Size == 0)
                            {
                                reTrans.RollBack();
                                break;
                            }

                            List<Connector> lstConnectorSprinkler = cntSetOfIns?.Cast<Connector>().ToList();

                            Connector cntOfIns_1 = Common.ToList(sprinkler.MEPModel.ConnectorManager.Connectors).OrderByDescending(x => x.Origin.Z).FirstOrDefault();

                            if (cntOfIns_1.IsConnected == true)
                            {
                                reTrans.RollBack();
                                break;
                            }

                            var curve = (pipe.Location as LocationCurve).Curve;

                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            var sprinker2d = Common.PointTo2D(locSprinkler);
                            var p02d = Common.PointTo2D(p0);
                            var p12d = Common.PointTo2D(p1);

                            var resultPipe = Line.CreateUnbound((curve as Line).Origin, (curve as Line).Direction).Project(locSprinkler);

                            XYZ pointProject = resultPipe.XYZPoint;
                            XYZ pointProject2d = Common.PointTo2D(pointProject);

                            var curve2d = RevitUtils.ProjectLineToPlane(curve as Line);
                            curve2d.MakeUnbound();
                            var result = curve2d.Project(pointProject2d);
                            if (result != null)
                            {
                                XYZ vectorMove = (result.XYZPoint - sprinker2d).Normalize();
                                ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, vectorMove * result.XYZPoint.DistanceTo(sprinker2d));
                            }

                            if (curve.GetEndPoint(0).DistanceTo(pointProject) < curve.GetEndPoint(1).DistanceTo(pointProject))
                                (pipe.Location as LocationCurve).Curve = Line.CreateBound(pointProject, curve.GetEndPoint(1));
                            else
                                (pipe.Location as LocationCurve).Curve = Line.CreateBound(curve.GetEndPoint(0), pointProject);

                            //Set pipe size
                            var dPipeSizeFt = Common.mmToFT * App.m_SprinklerDownForm.PipeSizeC3;

                            // Connect vertical pipe

                            try
                            {
                                Pipe vertical_pipe = Pipe.Create(Global.UIDoc.Document, pipe.MEPSystem.GetTypeId(), pipe.GetTypeId(), pipe.ReferenceLevel.Id, cntOfIns_1.Origin, new XYZ(cntOfIns_1.Origin.X, cntOfIns_1.Origin.Y, pointProject.Z));
                                vertical_pipe.LookupParameter("Diameter").Set(dPipeSizeFt);

                                var c1Hor = Common.GetConnectorClosestTo(pipe, new XYZ(cntOfIns_1.Origin.X, cntOfIns_1.Origin.Y, pointProject.Z));
                                var c2Ver = Common.GetConnectorClosestTo(vertical_pipe, new XYZ(cntOfIns_1.Origin.X, cntOfIns_1.Origin.Y, pointProject.Z));

                                var elbow1 = Global.UIDoc.Document.Create.NewElbowFitting(c1Hor, c2Ver);
                                listIdConnect.Add(elbow1.Id);

                                Global.UIDoc.Document.Delete(vertical_pipe.Id);

                                elbow1.ChangeTypeId(App.m_SprinklerDownForm.ElbowFamilySymbol.Id);

                                Connector cElbowNotConnect = ConnectorUtils.GetConnectorNotConnnected(elbow1.MEPModel.ConnectorManager);
                                Connector cElbowConnect = ConnectorUtils.GetConnectorConnnected(elbow1.MEPModel.ConnectorManager);
                                Connector c2Sprinkler = Common.GetConnectorClosestTo(sprinkler, locSprinkler);

                                var typeSprinkler = Global.UIDoc.Document.GetElement(sprinkler.GetTypeId());
                                if (typeSprinkler != null)
                                {
                                    double radiusSprinkler = 0;
                                    var paraConnection = typeSprinkler.LookupParameter("Connection");
                                    if (paraConnection != null)
                                        radiusSprinkler = paraConnection.AsDouble();

                                    var paraRadius1 = elbow1.LookupParameter("Nominal Radius 1");
                                    if (paraRadius1 != null)
                                    {
                                        paraRadius1.Set(radiusSprinkler / 2);
                                        var paraRadius2 = elbow1.LookupParameter("Nominal Radius 2");
                                        if (paraRadius2 != null)
                                        {
                                            paraRadius2.Set(dPipeSizeFt / 2);
                                        }
                                    }
                                    else
                                    {
                                        cElbowConnect.Radius = dPipeSizeFt / 2;
                                        cElbowNotConnect.Radius = radiusSprinkler / 2;
                                    }
                                }

                                ElementTransformUtils.MoveElement(Global.UIDoc.Document, sprinkler.Id, cElbowNotConnect.Origin - c2Sprinkler.Origin);

                                cElbowNotConnect.ConnectTo(c2Sprinkler);
                            }
                            catch (Exception)
                            {
                                reTrans.RollBack();
                                break;
                            }

                            CmdDeleteSprinker.CreateSchema(sprinkler, listIdConnect.Select(x => x.ToInt().ToString()).ToList(), locSprinkler.Z);
                            reTrans.Commit();

                            trGr.Assimilate();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (App.m_SprinklerDownForm != null && App.m_SprinklerDownForm.IsDisposed == false)
                {
                    App.m_SprinklerDownForm.Show(App.hWndRevit);
                }

                DisplayService.SetFocus(new HandleRef(null, App.m_SprinklerDownForm.Handle));
            }

            return Result.Succeeded;
        }


        public static bool CheckPipeIsEnd(Pipe pipe, XYZ point)
        {
            var con = Common.GetConnectorClosestTo(pipe, point);

            return con.IsConnected;
        }

        public static bool CheckPipeIsEnd1(Pipe pipe, List<FamilyInstance> lstIns, FamilyInstance familyInstance, XYZ point)
        {
            bool retVal = true;
            Dictionary<FamilyInstance, double> keyValuePairs = new Dictionary<FamilyInstance, double>();

            var con1 = Common.GetConnectorClosestTo(pipe, pipe.GetCurve().GetEndPoint(0));
            var con2 = Common.GetConnectorClosestTo(pipe, pipe.GetCurve().GetEndPoint(1));
            Connector con = null;
            if (con1.Origin.DistanceTo(point) < con2.Origin.DistanceTo(point))
            {
                con = pipe.ConnectorManager.Lookup(0);
            }
            else
            {
                con = pipe.ConnectorManager.Lookup(1);
            }
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
    }
}
