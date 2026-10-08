using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.ProcessBarForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    [Transaction(TransactionMode.Manual)]
    public class CmdSprinklerUpright : IExternalCommand
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
            if (App.ShowConnectSprinkleForm() == false)
                return Result.Cancelled;

            return Result.Succeeded;
        }

        public static Dictionary<Pipe, List<FamilyInstance>> DictSprinker(List<Pipe> listPipe, List<FamilyInstance> listSprinker)
        {
            Dictionary<Pipe, List<FamilyInstance>> val = new Dictionary<Pipe, List<FamilyInstance>>();
            foreach (Pipe pipe in listPipe)
            {
                var bbox = pipe.get_BoundingBox(null);
                XYZ max = new XYZ(bbox.Max.X + (1000 * Common.mmToFT), bbox.Max.Y + (1000 * Common.mmToFT), 1000);
                XYZ min = new XYZ(bbox.Min.X - (1000 * Common.mmToFT), bbox.Min.Y - (1000 * Common.mmToFT), -1000);
                Outline outline = new Outline(min, max);

                BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);

                FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document, listSprinker.Select(x => x.Id).ToList());
                var sprinklers = collector.WherePasses(filter).Cast<FamilyInstance>().ToList();

                try
                {
                    var locationPipe = pipe.GetCurve();
                    List<FamilyInstance> listIns = new List<FamilyInstance>();
                    foreach (var sprinkler in sprinklers)
                    {
                        if (sr.IsRightPipe(sprinkler, pipe))
                            listIns.Add(sprinkler);
                    }

                    if (listIns.Count != 0)
                    {
                        if (!sr.CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)))
                        {
                            listIns = listIns.OrderBy(x => Common.To2D((x.Location as LocationPoint).Point).DistanceTo(Common.To2D(locationPipe.GetEndPoint(0)))).ToList();
                        }
                        if (!sr.CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
                        {
                            listIns = listIns.OrderBy(x => Common.To2D((x.Location as LocationPoint).Point).DistanceTo(Common.To2D(locationPipe.GetEndPoint(1)))).ToList();
                        }

                        val.Add(pipe, listIns);
                        listIns.ForEach(x => listSprinker.Remove(x));
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }

            return val;
        }

        public static Result Process()
        {
            try
            {
                if (App.m_ConnectSprinkleForm != null && App.m_ConnectSprinkleForm.IsDisposed == false)
                    App.m_ConnectSprinkleForm.Hide();

                if (App.m_ConnectSprinkleForm.isOption3)
                {
                    ProcessType3();
                }
                else
                {
                    List<FamilyInstance> sprinklers = sr.SelectSprinklers();
                    if (sprinklers == null || sprinklers.Count == 0)
                        return Result.Cancelled;

                    List<Pipe> pipes = PickPipe();
                    if (pipes == null || pipes.Count == 0)
                        return Result.Cancelled;

                    var pipeIds = (from Pipe p in pipes
                                   where p.Id != ElementId.InvalidElementId
                                   select p.Id).ToList();

                    // Status cancel export : default = false
                    bool isCancelExport = false;

                    // Count type imported
                    int nCount = 0;
                    var dict = DictSprinker(pipes, sprinklers);

                    System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                    IntPtr intPtr = process.MainWindowHandle;

                    string title = Common.GetTextLanguage("PendentSprinklerConnectionUpProgress");
                    FrmProcessbar progressBar = new FrmProcessbar(title, Define.MessageFinish, DiritIconTool.Mep);

                    progressBar.prgSingle.Minimum = 1;
                    progressBar.prgSingle.Maximum = dict.Count;
                    progressBar.prgSingle.Value = 1;
                    WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                    helper.Owner = intPtr;
                    progressBar.Show();

                    TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "CreateConnector");
                    tranGr.Start();

                    //Find pipe
                    try
                    {
                        foreach (var pair in dict)
                        {
                            string dPercent = string.Empty;
                            List<ElementId> listPipeIds = new List<ElementId>() { pair.Key.Id };
                            foreach (var instance in pair.Value)
                            {
                                var check = Common.GetConnectorNotConnnected(instance.MEPModel.ConnectorManager);
                                if (check == null)
                                    continue;

                                Transaction tran = new Transaction(Global.UIDoc.Document, "CreateConnector");
                                tran.Start();
                                try
                                {
                                    sr.cc(tran,
                                          sprinklers,
                                          instance,
                                          ref listPipeIds,
                                          App.m_ConnectSprinkleForm.isConnectNipple,
                                          App.m_ConnectSprinkleForm.isConnectTee,
                                          App.m_ConnectSprinkleForm.fmlNipple,
                                          App.m_ConnectSprinkleForm.PipeSizeC2,
                                          App.m_ConnectSprinkleForm.FamilyTypeC2,
                                          true);

                                    tran.Commit();
                                }
                                catch (Exception ex)
                                {
                                    tran.RollBack();
                                }

                                // If click cancel button when exporting
                                if (progressBar.IsCancel)
                                {
                                    isCancelExport = true;
                                    break;
                                }
                            }

                            nCount++;

                            dPercent = nCount.ToString() + "/" + dict.Count.ToString();

                            progressBar.tbxMessage.Text = dPercent;

                            progressBar.IncrementProgressBar();
                        }
                    }
                    catch (Exception)
                    { }
                    finally
                    {
                        progressBar.Dispose();
                    }

                    tranGr.Assimilate();
                }

                return Result.Succeeded;
            }
            catch (System.Exception)
            { }
            finally
            {
                if (App.m_ConnectSprinkleForm != null && App.m_ConnectSprinkleForm.IsDisposed == false)
                {
                    App.m_ConnectSprinkleForm.Show(App.hWndRevit);
                }
                DisplayService.SetFocus(new HandleRef(null, App.m_ConnectSprinkleForm.Handle));
            }
            return Result.Cancelled;
        }

        public static Result ProcessType3()
        {
            try
            {
                // Get selected sprinkler
                List<FamilyInstance> selSprinklers = sr.SelectSprinklers();
                if (selSprinklers == null || selSprinklers.Count == 0)
                    return Result.Cancelled;

                // Get selected main pipe
                List<Pipe> selPipes = CmdSprinklerDownright.PickPipes();
                if (selPipes == null || selPipes.Count == 0)
                    return Result.Cancelled;

                // Get selected main pipe id
                List<ElementId> selPipeIds = selPipes.Where(item => item.Id != ElementId.InvalidElementId).Select(item => item.Id).ToList();

                // Process

                try
                {
                    double invalidRadius_ft = Common.mmToFT * 2000;

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
                    FrmProcessbar progressBar = new FrmProcessbar("", Define.MessageFinish, DiritIconTool.Mep);
                    //FrmProcessbar progressBar = new FrmProcessbar(convertedTexts[0], Define.MessageFinish, DiritIconTool.Mep);
                    progressBar.prgSingle.Minimum = 1;
                    progressBar.prgSingle.Maximum = selSprinklers.Count;
                    progressBar.prgSingle.Value = 1;
                    progressBar.Show();
                    progressBar.Topmost = true;
                    using (TransactionGroup trGr = new TransactionGroup(Global.UIDoc.Document, "SprinklerUP"))
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
                                var cylindricalFromIns = Common.CreateCylindricalVolume(locSprinkler, invalidRadius_ft * 5, invalidRadius_ft, false);
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
                                Pipe processPipe = CmdSprinklerDownright.ProcessPipes(validPipes.ToList(), locSprinkler, out isSplit);
                                (int, int) justification = CmdSprinklerDownright.GetJustification(processPipe);
                                CmdSprinklerDownright.SetJustification(processPipe, (0, 0));
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
                                    var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z - dTempEvaluate));
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
                                    var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z - dTempEvaluate));

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
                                    if (CmdSprinklerDownright.GetPreferredJunctionType(processPipe) != PreferredJunctionType.Tee)
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
                                var dPipeSizeFt = Common.mmToFT * App.m_ConnectSprinkleForm.PipeSizeC2;

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

                                    if (!isDauOng)
                                    {
                                        if (CmdSprinklerDownright.GetPreferredJunctionType(temp_processPipe_1) != PreferredJunctionType.Tee && isSplit == true)
                                        {
                                            CmdSprinklerDownright.CreateTap(temp_processPipe_1 as MEPCurve, horizontal_pipe as MEPCurve, ref listIdConnect);
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

                                (pipe_v2.Location as LocationCurve).Curve = line_v2;

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
                                    var c1 = Common.GetConnectorClosestTo(pipe_v2, locSprinkler);
                                    var c2 = Common.GetConnectorClosestTo(sprinkler, locSprinkler);

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

                                CmdSprinklerDownright.SetJustification(pipe_v2, justification);
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

            return Result.Succeeded;
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
                if (CmdSprinklerDownright.IsIntersect(p0) == false && !CmdSprinklerDownright.CheckPipeIsEnd(pipe, pOn) && App.m_ConnectSprinkleForm.isElbow)
                {
                    isDauOng = true;
                    far = 1;
                }
            }

            if (d2 < km_ft)
            {
                if (CmdSprinklerDownright.IsIntersect(p1) == false && !CmdSprinklerDownright.CheckPipeIsEnd(pipe, pOn) && App.m_ConnectSprinkleForm.isElbow)
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
                    CmdSprinklerDownright.SplitPipe(pipe, pOn, out pipe1, out pipe2);
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

        public static List<Pipe> PickPipe()
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
    }

    public class sr
    {
        public static List<FamilyInstance> SelectSprinklers()
        {
            List<FamilyInstance> list = new List<FamilyInstance>();
            try
            {
                var pickedObjs = Global.UIDoc.Selection.PickObjects(ObjectType.Element, new SprinklerFilter(), "Pick Sprinklers: ");

                foreach (Reference reference in pickedObjs)
                {
                    var familyInstance = Global.UIDoc.Document.GetElement(reference) as FamilyInstance;
                    if (familyInstance == null)
                        continue;
                    list.Add(familyInstance);
                }
            }
            catch (System.Exception ex)
            {
            }
            return list;
        }

        public static void XuLyDauong(Pipe pipe, out Pipe pipe2, XYZ pOn)
        {
            var curve = (pipe.Location as LocationCurve).Curve;

            //Create plane
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            //Check co phai dau cuu hoa o gan dau cua ong ko : check trong pham vi 1m
            double kc_mm = 400 /*1000*/;
            double km_ft = Common.mmToFT * kc_mm;

            var p02d = new XYZ(p0.X, p0.Y, 0);
            var p12d = new XYZ(p1.X, p1.Y, 0);
            var pOn2d = new XYZ(pOn.X, pOn.Y, 0);

            var d1 = p02d.DistanceTo(pOn2d);
            var d2 = p12d.DistanceTo(pOn2d);

            bool isDauOng = false;
            int far = -1;
            if (d1 < km_ft)
            {
                //Check connect o dau ong
                //var c = Utils.GetConnectorClosestTo(pipe, p0);
                //if (c.IsConnected == false)
                if (ci(p0) == false)
                {
                    isDauOng = true;
                    far = 1;
                }
            }
            else if (d2 < km_ft)
            {
                //var c = Utils.GetConnectorClosestTo(pipe, p1);
                //if (c.IsConnected == false)
                if (ci(p1) == false)
                {
                    isDauOng = true;
                    far = 0;
                }
            }

            Pipe pipe1 = pipe;
            pipe2 = null;

            if (isDauOng == false)
            {
                s(pipe, pOn, out pipe1, out pipe2);
            }
            else if (far != -1)
            {
                if (far == 1)
                    (pipe1.Location as LocationCurve).Curve = Line.CreateBound(pOn, p1);
                else
                    (pipe1.Location as LocationCurve).Curve = Line.CreateBound(p0, pOn);
            }
        }

        public static void s(Pipe pipeOrigin, XYZ splitPoint, out Pipe pipe1, out Pipe pipe2)
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

        public static bool ci(XYZ point)
        {
            double ft = 0.001;
            var solid = Common.CreateCylindricalVolume(point, ft, ft, false);
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
                    return true;
                }
            }

            return false;
        }

        public static void ge(Pipe pipe, out Connector c0, out Connector c1)
        {
            var curve = (pipe.Location as LocationCurve).Curve;

            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            c0 = Common.GetConnectorClosestTo(pipe, p0);
            c1 = Common.GetConnectorClosestTo(pipe, p1);
        }

        public static bool cc(Transaction tran, List<FamilyInstance> lstInstance, FamilyInstance instance, ref List<ElementId> selectedIds, bool isConnectNipple, bool isConnectTee, FamilySymbol fmlNipple, double pipeSize, ElementId pipeTypeId, bool isUp)
        {
            HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
#if Debug_2020 || Release_2020 || Bundle_2020
            double d = UnitUtils.ConvertToInternalUnits(70, DisplayUnitType.DUT_MILLIMETERS);
#else
            double d = UnitUtils.ConvertToInternalUnits(70, UnitTypeId.Millimeters);
#endif
            bool result = false;

            XYZ direction = isUp ? -XYZ.BasisZ : XYZ.BasisZ;

            //Set d = 25
            //double d25 = 25;
            var dFt = Common.mmToFT * pipeSize;

            var sprinkle_point = (instance.Location as LocationPoint).Point;
            var sprinkle_pointClone = (instance.Location as LocationPoint).Point;

            XYZ newPlace = new XYZ(0, 0, 0);
            ICollection<ElementId> elemIds = null;

            double radius = 100; //mm
            var ft = Common.mmToFT * radius;

            bool isPipe = false;

            var pipeCheck = selectedIds.Select(x => Global.UIDoc.Document.GetElement(x) as Pipe).ToList().FirstOrDefault(x => IsRightPipe(instance, x));
            if (pipeCheck == null)
                return false;

            List<Element> pipeList = new List<Element>() { pipeCheck };

            FamilyInstance elbowFittingConnected1 = null;
            FamilyInstance elbowFittingConnected2 = null;

            foreach (Pipe pipe in pipeList)
            {
                List<Pipe> listPipeCut = new List<Pipe>();
                listPipeCut.Add(pipe);
                var isEnd = CheckPipeIsEnd(pipe, lstInstance, instance, sprinkle_point);
                //Ngắt kết nối với fitting
                var conStPipeHor = (pipe as MEPCurve).ConnectorManager.Lookup(0);
                var conEndPipeHor = (pipe as MEPCurve).ConnectorManager.Lookup(1);

                if (conStPipeHor != null && conEndPipeHor != null)
                {
                    if (conStPipeHor.IsConnected || conEndPipeHor.IsConnected)
                    {
                        Connector connectorIsConnecting = (conStPipeHor.IsConnected) ? conStPipeHor : conEndPipeHor;

                        elbowFittingConnected1 = Common.GetFittingConnected(connectorIsConnecting);
                        if (elbowFittingConnected1 != null)
                        {
                            ConnectorUtils.GetConnectorClosedTo((pipe as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);
                            if (conPipeHor1 != null && conFitHor != null && conPipeHor1.IsConnectedTo(conFitHor))
                                conPipeHor1.DisconnectFrom(conFitHor);
                        }
                    }

                    if (conStPipeHor.IsConnected || conEndPipeHor.IsConnected)
                    {
                        Connector connectorIsConnecting = (conStPipeHor.IsConnected) ? conStPipeHor : conEndPipeHor;

                        elbowFittingConnected2 = Common.GetFittingConnected(connectorIsConnecting);
                        if (elbowFittingConnected2 != null)
                        {
                            ConnectorUtils.GetConnectorClosedTo((pipe as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);
                            if (conPipeHor1 != null && conFitHor != null && conPipeHor1.IsConnectedTo(conFitHor))
                                conPipeHor1.DisconnectFrom(conFitHor);
                        }
                    }
                }

                var curve = (pipe.Location as LocationCurve).Curve;

                var p0 = curve.GetEndPoint(0);
                var p1 = curve.GetEndPoint(1);

                //Move sprinker về điểm gần nhất với pipe
                var sprinker2d = Common.PointTo2D(sprinkle_point);
                var p02d = Common.PointTo2D(p0);
                var p12d = Common.PointTo2D(p1);

                XYZ vectorMove = new XYZ();

                Line lineCheck = Line.CreateBound(Common.PointTo2D(curve.GetEndPoint(0)), Common.PointTo2D(curve.GetEndPoint(1)));
                Plane plane = Plane.CreateByNormalAndOrigin(lineCheck.Direction.CrossProduct(XYZ.BasisZ), (curve as Line).Origin);

                sprinkle_point = Common.ProjectPointOnPlane(plane, sprinkle_point);

                (instance.Location as LocationPoint).Point = sprinkle_point;

                var line2d = Line.CreateBound(new XYZ(p0.X, p0.Y, 0), new XYZ(p1.X, p1.Y, 0));
                var v = line2d.Direction.CrossProduct(XYZ.BasisZ).Normalize();

                double dTemp = 1000;

                var lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), v * dTemp);

                var p11 = lineTemp.Evaluate(dTemp, false);

                lineTemp = Line.CreateUnbound(new XYZ(sprinkle_point.X, sprinkle_point.Y, 0), -v * dTemp);

                var p22 = lineTemp.Evaluate(dTemp, false);

                lineTemp = Line.CreateBound(p11, p22);

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
                IntersectionResultArray arr = new IntersectionResultArray();
                inter = curveExpand.Intersect(lineTemp, out arr);
#endif

                if (inter != SetComparisonResult.Overlap)
                {
                    // Join lại fitting
                    if (elbowFittingConnected1 != null)
                    {
                        for (int i = 0; i < listPipeCut.Count; i++)
                        {
                            ElementId id = listPipeCut[i].Id;
                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                            if (id != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                {
                                    conPipeHor1.ConnectTo(conFitHor);
                                }
                            }
                        }
                    }

                    if (elbowFittingConnected2 != null)
                    {
                        for (int i = 0; i < listPipeCut.Count; i++)
                        {
                            ElementId id = listPipeCut[i].Id;
                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                            if (id != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                {
                                    conPipeHor1.ConnectTo(conFitHor);
                                }
                            }
                        }
                    }
                    continue;
                }

#if Debug_2027 || Release_2027 || Release_2026
    var p2d = intersectResult1.GetOverlaps()[0].Point;
#else
                var p2d = arr.get_Item(0).XYZPoint;
#endif

                // --- 2. DỰNG HÌNH 3D ---
                var p3d = new XYZ(p2d.X, p2d.Y, sprinkle_point.Z);
                var line3d = Line.CreateBound(p3d, new XYZ(p3d.X, p3d.Y, p3d.Z + (isUp ? -1 : 1) * dTemp));
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
                    // Join lại fitting
                    if (elbowFittingConnected1 != null)
                    {
                        for (int i = 0; i < listPipeCut.Count; i++)
                        {
                            ElementId id = listPipeCut[i].Id;
                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                            if (id != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                {
                                    conPipeHor1.ConnectTo(conFitHor);
                                }
                            }
                        }
                    }

                    if (elbowFittingConnected2 != null)
                    {
                        for (int i = 0; i < listPipeCut.Count; i++)
                        {
                            ElementId id = listPipeCut[i].Id;
                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                            if (id != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                {
                                    conPipeHor1.ConnectTo(conFitHor);
                                }
                            }
                        }
                    }
                    continue;
                }

#if Debug_2027 || Release_2027 || Release_2026
    var pOn = intersectResult2.GetOverlaps()[0].Point;
#else
                var pOn = arr.get_Item(0).XYZPoint;
#endif

                //Create pipe Z
                newPlace = new XYZ(0, 0, 0);

                lineTemp = Line.CreateBound(pOn, sprinkle_point);

                var pcenter = lineTemp.Evaluate((lineTemp.GetEndParameter(0) + lineTemp.GetEndParameter(1)) / 2, false);

                pOn = curveExtend3d_temp.Project(pcenter).XYZPoint;

                var newPipeZ = Pipe.Create(Global.UIDoc.Document, pipe.MEPSystem.GetTypeId(), pipeTypeId, pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pOn, pcenter);

                newPipeZ.LookupParameter("Diameter").Set(dFt);

                var pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                if (pipeType != null)
                    newPipeZ.PipeType = pipeType;

                Global.UIDoc.Document.Regenerate();

                listIdConnect.Add(newPipeZ.Id);

                FamilyInstance reducer = null;

                try
                {
                    if (App.m_ConnectSprinkleForm.isElbow)
                    {
                        if (isEnd)
                        {
                            if (isConnectNipple)
                            {
                                var pipeCut = ConnectTeeAndNipple(pipe, newPipeZ, instance, fmlNipple, pOn, pcenter, sprinkle_point, ref listIdConnect);
                                if (pipeCut != null)
                                {
                                    selectedIds.Add(pipeCut.Id);
                                    listPipeCut.Add(pipeCut);
                                }
                            }
                            else if (isConnectTee)
                            {
                                var isPipeCut = ConnectTee(pipe, newPipeZ, instance, ref listIdConnect, out Pipe pipeCut);
                                if (!isPipeCut)
                                {
                                    Global.UIDoc.Document.Delete(newPipeZ.Id);

                                    // Join lại fitting
                                    if (elbowFittingConnected1 != null)
                                    {
                                        for (int i = 0; i < listPipeCut.Count; i++)
                                        {
                                            ElementId id = listPipeCut[i].Id;

                                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                            if (id != null)
                                            {
                                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                {
                                                    conPipeHor1.ConnectTo(conFitHor);
                                                }
                                            }
                                        }
                                    }

                                    if (elbowFittingConnected2 != null)
                                    {
                                        for (int i = 0; i < listPipeCut.Count; i++)
                                        {
                                            ElementId id = listPipeCut[i].Id;

                                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                            if (id != null)
                                            {
                                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                    conPipeHor1.ConnectTo(conFitHor);
                                            }
                                        }
                                    }

                                    continue;
                                }
                                else
                                    listPipeCut.Add(pipeCut);
                            }
                            else
                            {
                                if (GetPreferredJunctionType(pipe) == PreferredJunctionType.Tee)
                                {
                                    var curvePipeMain = pipe.GetCurve();

                                    var curvePipeDung = newPipeZ.GetCurve();

                                    Line line = Line.CreateUnbound((curvePipeDung as Line).Origin, (curvePipeDung as Line).Direction);
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = line.Intersect(curvePipeMain, CurveIntersectResultOption.Detailed);

    // Nếu không cắt nhau thì tương đương với array == null ở bản cũ
    if (intersectResult.Result != SetComparisonResult.Overlap)
#else
                                    // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                    IntersectionResultArray array;
                                    line.Intersect(curvePipeMain, out array);

                                    if (array == null)
#endif
                                    {
                                        Global.UIDoc.Document.Delete(newPipeZ.Id);

                                        // Join lại fitting
                                        if (elbowFittingConnected1 != null)
                                        {
                                            for (int i = 0; i < listPipeCut.Count; i++)
                                            {
                                                ElementId id = listPipeCut[i].Id;

                                                Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                                if (id != null)
                                                {
                                                    ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                    if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                    {
                                                        conPipeHor1.ConnectTo(conFitHor);
                                                    }
                                                }
                                            }
                                        }

                                        if (elbowFittingConnected2 != null)
                                        {
                                            for (int i = 0; i < listPipeCut.Count; i++)
                                            {
                                                ElementId id = listPipeCut[i].Id;

                                                Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                                if (id != null)
                                                {
                                                    ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                    if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                    {
                                                        conPipeHor1.ConnectTo(conFitHor);
                                                    }
                                                }
                                            }
                                        }

                                        continue;
                                    }

#if Debug_2027 || Release_2027 || Release_2026
    var point = intersectResult.GetOverlaps()[0].Point;
#else
                                    var point = array.get_Item(0).XYZPoint;
#endif

                                    var fitting = CreateTeeFitting(pipe, newPipeZ, point, out Pipe pipe1);
                                    listIdConnect.Add(fitting.Id);
                                    if (pipe1 != null)
                                    {
                                        selectedIds.Add(pipe1.Id);
                                        listPipeCut.Add(pipe1);
                                    }
                                }
                                else
                                {
                                    SetComparisonResult result1;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curve.Intersect(newPipeZ.GetCurve(), CurveIntersectResultOption.Detailed);
    result1 = intersectResult.Result;
#else
                                    // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                    IntersectionResultArray resultArray;
                                    result1 = curve.Intersect(newPipeZ.GetCurve(), out resultArray);
#endif

                                    if (result1 == SetComparisonResult.Overlap)
                                    {
                                        // Lấy tọa độ điểm giao
#if Debug_2027 || Release_2027 || Release_2026
    var point = intersectResult.GetOverlaps()[0].Point;
#else
                                        var point = resultArray.get_Item(0).XYZPoint;
#endif

                                        // Logic kiểm tra điểm mút và tạo Tap giữ nguyên
                                        if (!Common.Equals(curve.GetEndPoint(0), point) && !Common.Equals(curve.GetEndPoint(1), point))
                                        {
                                            var tap = se(pipe as MEPCurve, newPipeZ as MEPCurve);
                                            if (tap != null)
                                                listIdConnect.Add(tap.Id);
                                        }
                                        else
                                        {
                                            tran.RollBack();
                                            tran.Start();
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        Global.UIDoc.Document.Delete(newPipeZ.Id);

                                        // Join lại fitting
                                        if (elbowFittingConnected1 != null)
                                        {
                                            for (int i = 0; i < listPipeCut.Count; i++)
                                            {
                                                ElementId id = listPipeCut[i].Id;

                                                Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                                if (id != null)
                                                {
                                                    ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                    if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                    {
                                                        conPipeHor1.ConnectTo(conFitHor);
                                                    }
                                                }
                                            }
                                        }

                                        if (elbowFittingConnected2 != null)
                                        {
                                            for (int i = 0; i < listPipeCut.Count; i++)
                                            {
                                                ElementId id = listPipeCut[i].Id;

                                                Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                                if (id != null)
                                                {
                                                    ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                    if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                    {
                                                        conPipeHor1.ConnectTo(conFitHor);
                                                    }
                                                }
                                            }
                                        }

                                        continue;
                                    }
                                }

                                Connector c4 = Common.GetConnectorClosestTo(instance, pcenter);
                                Connector c5 = Common.GetConnectorClosestTo(newPipeZ, sprinkle_point);

                                reducer = Global.UIDoc.Document.Create.NewTransitionFitting(c5, c4);
                                listIdConnect.Add(reducer.Id);
                            }
                        }
                        else
                        {
                            if (isConnectNipple)
                            {
                                ConnectNipple(pipe, newPipeZ, instance, fmlNipple, pOn, pcenter, sprinkle_point, ref listIdConnect);
                            }
                            else if (isConnectTee)
                            {
                                Connector c1 = Common.GetConnectorClosestTo(pipe, pOn);
                                Connector c3 = Common.GetConnectorClosestTo(newPipeZ, pOn);
                                // Get Comector vertical pipe bottom

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                listIdConnect.Add(elbow.Id);

                                Global.UIDoc.Document.Delete(newPipeZ.Id);
                                var con1 = instance.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault();
                                var con2 = Common.GetConnectorClosestTo(elbow, con1.Origin);

                                if (con1 != null && con2 != null)
                                {
                                    ElementTransformUtils.MoveElement(Global.UIDoc.Document, instance.Id, Line.CreateBound(con1.Origin, con2.Origin).Direction * con1.Origin.DistanceTo(con2.Origin));
                                    con1.ConnectTo(con2);
                                }
                            }
                            else
                            {
                                Connector c4 = Common.GetConnectorClosestTo(instance, pcenter);
                                Connector c5 = Common.GetConnectorClosestTo(newPipeZ, sprinkle_point);

                                reducer = Global.UIDoc.Document.Create.NewTransitionFitting(c5, c4);
                                listIdConnect.Add(reducer.Id);

                                Connector c1 = Common.GetConnectorClosestTo1(pipe, pOn);
                                Connector c3 = Common.GetConnectorClosestTo(newPipeZ, pOn);

                                var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                listIdConnect.Add(elbow.Id);
                            }
                        }
                    }
                    else
                    {
                        if (isConnectNipple)
                        {
                            var pipeCut = ConnectTeeAndNipple(pipe, newPipeZ, instance, fmlNipple, pOn, pcenter, sprinkle_point, ref listIdConnect);
                            if (pipeCut != null)
                            {
                                selectedIds.Add(pipeCut.Id);
                                listPipeCut.Add(pipeCut);
                            }
                        }
                        else if (isConnectTee)
                        {
                            var isPipeCut = ConnectTee(pipe, newPipeZ, instance, ref listIdConnect, out Pipe pipeCut);
                            if (!isPipeCut)
                            {
                                Global.UIDoc.Document.Delete(newPipeZ.Id);

                                // Join lại fitting
                                if (elbowFittingConnected1 != null)
                                {
                                    for (int i = 0; i < listPipeCut.Count; i++)
                                    {
                                        ElementId id = listPipeCut[i].Id;

                                        Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                        if (id != null)
                                        {
                                            ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                            if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                            {
                                                conPipeHor1.ConnectTo(conFitHor);
                                            }
                                        }
                                    }
                                }

                                if (elbowFittingConnected2 != null)
                                {
                                    for (int i = 0; i < listPipeCut.Count; i++)
                                    {
                                        ElementId id = listPipeCut[i].Id;

                                        Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                        if (id != null)
                                        {
                                            ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                            if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                conPipeHor1.ConnectTo(conFitHor);
                                        }
                                    }
                                }

                                continue;
                            }
                            else
                                listPipeCut.Add(pipeCut);
                        }
                        else
                        {
                            if (GetPreferredJunctionType(pipe) == PreferredJunctionType.Tee)
                            {
                                var curvePipeMain = pipe.GetCurve();

                                var curvePipeDung = newPipeZ.GetCurve();

                                Line line = Line.CreateUnbound((curvePipeDung as Line).Origin, (curvePipeDung as Line).Direction);

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = line.Intersect(curvePipeMain, CurveIntersectResultOption.Detailed);

    // Nếu không cắt nhau thì tương đương với array == null ở bản cũ
    if (intersectResult.Result != SetComparisonResult.Overlap)
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                IntersectionResultArray array;
                                line.Intersect(curvePipeMain, out array);

                                if (array == null)
#endif
                                {
                                    Global.UIDoc.Document.Delete(newPipeZ.Id);

                                    // Join lại fitting
                                    if (elbowFittingConnected1 != null)
                                    {
                                        for (int i = 0; i < listPipeCut.Count; i++)
                                        {
                                            ElementId id = listPipeCut[i].Id;

                                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                            if (id != null)
                                            {
                                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                {
                                                    conPipeHor1.ConnectTo(conFitHor);
                                                }
                                            }
                                        }
                                    }

                                    if (elbowFittingConnected2 != null)
                                    {
                                        for (int i = 0; i < listPipeCut.Count; i++)
                                        {
                                            ElementId id = listPipeCut[i].Id;

                                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                            if (id != null)
                                            {
                                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                {
                                                    conPipeHor1.ConnectTo(conFitHor);
                                                }
                                            }
                                        }
                                    }

                                    continue;
                                }

#if Debug_2027 || Release_2027 || Release_2026
    var point = intersectResult.GetOverlaps()[0].Point;
#else
                                var point = array.get_Item(0).XYZPoint;
#endif

                                var fitting = CreateTeeFitting(pipe, newPipeZ, point, out Pipe pipe1);
                                listIdConnect.Add(fitting.Id);
                                if (pipe1 != null)
                                {
                                    selectedIds.Add(pipe1.Id);
                                    listPipeCut.Add(pipe1);
                                }
                            }
                            else
                            {
                                SetComparisonResult result1;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curve.Intersect(newPipeZ.GetCurve(), CurveIntersectResultOption.Detailed);
    result1 = intersectResult.Result;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                IntersectionResultArray resultArray;
                                result1 = curve.Intersect(newPipeZ.GetCurve(), out resultArray);
#endif

                                if (result1 == SetComparisonResult.Overlap)
                                {
                                    // Lấy tọa độ điểm giao
#if Debug_2027 || Release_2027 || Release_2026
    var point = intersectResult.GetOverlaps()[0].Point;
#else
                                    var point = resultArray.get_Item(0).XYZPoint;
#endif

                                    // Logic kiểm tra điểm mút và tạo Tap giữ nguyên
                                    if (!Common.Equals(curve.GetEndPoint(0), point) && !Common.Equals(curve.GetEndPoint(1), point))
                                    {
                                        var tap = se(pipe as MEPCurve, newPipeZ as MEPCurve);
                                        if (tap != null)
                                            listIdConnect.Add(tap.Id);
                                    }
                                    else
                                    {
                                        tran.RollBack();
                                        tran.Start();
                                        continue;
                                    }
                                }
                                else
                                {
                                    Global.UIDoc.Document.Delete(newPipeZ.Id);

                                    // Join lại fitting
                                    if (elbowFittingConnected1 != null)
                                    {
                                        for (int i = 0; i < listPipeCut.Count; i++)
                                        {
                                            ElementId id = listPipeCut[i].Id;

                                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                            if (id != null)
                                            {
                                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                {
                                                    conPipeHor1.ConnectTo(conFitHor);
                                                }
                                            }
                                        }
                                    }

                                    if (elbowFittingConnected2 != null)
                                    {
                                        for (int i = 0; i < listPipeCut.Count; i++)
                                        {
                                            ElementId id = listPipeCut[i].Id;

                                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                                            if (id != null)
                                            {
                                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                                {
                                                    conPipeHor1.ConnectTo(conFitHor);
                                                }
                                            }
                                        }
                                    }

                                    continue;
                                }
                            }

                            Connector c4 = Common.GetConnectorClosestTo(instance, pcenter);
                            Connector c5 = Common.GetConnectorClosestTo(newPipeZ, sprinkle_point);

                            reducer = Global.UIDoc.Document.Create.NewTransitionFitting(c5, c4);
                            listIdConnect.Add(reducer.Id);
                        }
                    }

                    if ((double)Common.GetValueParameterByBuilt(pipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0 && !App.m_ConnectSprinkleForm.isElbow
                            && !Common.IsParallel((curve as Line).Direction, XYZ.BasisX) && !Common.IsParallel((curve as Line).Direction, XYZ.BasisY))
                    {
                        var resultPipe = curve.Project(sprinkle_pointClone);

                        XYZ pointProject = resultPipe.XYZPoint;
                        XYZ pointProject2d = Common.PointTo2D(pointProject);
                        var sprinker2d1 = Common.PointTo2D(sprinkle_pointClone);
                        var vector = pointProject2d - sprinker2d1;

                        ElementTransformUtils.MoveElement(Global.UIDoc.Document, instance.Id, vector.Normalize() * pointProject2d.DistanceTo(sprinker2d1));

                        Global.UIDoc.Document.Delete(reducer.Id);
                        Connector c44 = Common.GetConnectorClosestTo(instance, pcenter);
                        Connector c55 = Common.GetConnectorClosestTo(newPipeZ, sprinkle_point);

                        var a = Global.UIDoc.Document.Create.NewTransitionFitting(c55, c44);
                        listIdConnect.Add(a.Id);
                    }

                    // Join lại fitting
                    if (elbowFittingConnected1 != null)
                    {
                        for (int i = 0; i < listPipeCut.Count; i++)
                        {
                            ElementId id = listPipeCut[i].Id;

                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                            if (id != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                {
                                    conPipeHor1.ConnectTo(conFitHor);
                                }
                            }
                        }
                    }

                    if (elbowFittingConnected2 != null)
                    {
                        for (int i = 0; i < listPipeCut.Count; i++)
                        {
                            ElementId id = listPipeCut[i].Id;

                            Element mepCurve = Global.UIDoc.Document.GetElement(id);

                            if (id != null)
                            {
                                ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                {
                                    conPipeHor1.ConnectTo(conFitHor);
                                }
                            }
                        }
                    }

                    CmdDeleteSprinker.CreateSchema(instance, listIdConnect.Select(x => x.ToInt().ToString()).ToList());

                    result = true;

                    selectedIds.AddRange(listPipeCut.ToHashSet().Select(x => x.Id).ToList());
                }
                catch (System.Exception ex)
                {
                    tran.RollBack();
                    tran.Start();
                    continue;
                }
            }

            return result;
        }

        public static bool IsRightPipe(FamilyInstance familyInstance, Pipe pipe)
        {
#if Debug_2020 || Release_2020 || Bundle_2020
            double offset3 = UnitUtils.ConvertToInternalUnits(App.m_ConnectSprinkleForm.EndPointSprinklerDistance, DisplayUnitType.DUT_MILLIMETERS);
#else
            double offset3 = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);
#endif

            XYZ location = (familyInstance.Location as LocationPoint).Point;
            var curve = pipe.GetCurve();
            var newCurve = Line.CreateBound(Common.To2D(curve.GetEndPoint(0)), Common.To2D(curve.GetEndPoint(1)));
            XYZ normal2D = newCurve.Direction.CrossProduct(XYZ.BasisZ);
            XYZ normal3D = newCurve.Direction.CrossProduct(normal2D);

            Plane planeCheck = Plane.CreateByNormalAndOrigin(normal2D, curve.GetEndPoint(0));
            Plane planeCheckZ = Plane.CreateByNormalAndOrigin(normal3D, curve.GetEndPoint(0));

            XYZ pPrjZ = Common.ProjectPointOnPlane(planeCheckZ, location);
            XYZ pPrj = Common.ProjectPointOnPlane(planeCheck, location);

            Line locationPipe = (pipe.Location as LocationCurve).Curve as Line;
            XYZ p1 = Common.To2D(locationPipe.GetEndPoint(0));
            XYZ p2 = Common.To2D(locationPipe.GetEndPoint(1));
            Line locationPipe2D = Line.CreateBound(p1, p2);

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(0)))
                p1 += locationPipe2D.Direction.Negate() * offset3;

            if (!CheckPipeIsEnd(pipe, locationPipe.GetEndPoint(1)))
                p2 += locationPipe2D.Direction * offset3;

            if (Line.CreateBound(pPrjZ, location).Direction.Z <= 0)
                return false;

            if (pPrj.DistanceTo(location) > 100 * Common.mmToFT)
                return false;

            if (!Common.IsBetween2Point(Common.To2D(p1), Common.To2D(p2), Common.To2D(pPrj)))
                return false;

            return true;
        }

        public static List<Element> GetPipes(List<ElementId> selectedIds, XYZ sprinklerPoint)
        {
            Dictionary<Pipe, double> keyValuePairs = new Dictionary<Pipe, double>();
            List<Element> pipes = new List<Element>();
            foreach (ElementId elementId in selectedIds)
            {
                var pipePick = Global.UIDoc.Document.GetElement(elementId) as Pipe;
                if (pipePick == null)
                    continue;

                var con = Common.GetConnectorClosestTo(pipePick, sprinklerPoint);
                if (con == null)
                    continue;

                var conOrigin2D = Common.PointTo2D(con.Origin);
                var sprinklerPoint2D = Common.PointTo2D(sprinklerPoint);

                var distance = conOrigin2D.DistanceTo(sprinklerPoint2D);
                keyValuePairs.Add(pipePick, distance);
            }

            if (keyValuePairs.Count > 0)
            {
                var min = keyValuePairs.Min(x => x.Value);

                var pipeCloset = keyValuePairs.FirstOrDefault(x => x.Value == min);

                if (pipeCloset.Key != null)
                    pipes.Add(pipeCloset.Key);
            }

            return pipes;
        }

        public static bool CheckPipeIsEnd(Pipe pipe, List<FamilyInstance> lstIns, FamilyInstance familyInstance, XYZ point)
        {
            bool retVal = true;
            Dictionary<FamilyInstance, double> keyValuePairs = new Dictionary<FamilyInstance, double>();

            var con = Common.GetConnectorClosestTo(pipe, point);

            if (!con.IsConnected)
            {
                retVal = false;
            }
#if Debug_2020 || Release_2020 || Bundle_2020
            double radius = UnitUtils.ConvertToInternalUnits(5, DisplayUnitType.DUT_MILLIMETERS);
#else
            double radius = UnitUtils.ConvertToInternalUnits(5, UnitTypeId.Millimeters);
#endif
            var bbox = pipe.get_BoundingBox(null);
            XYZ p1 = bbox.Min;
            XYZ p2 = bbox.Max;
            var line = Line.CreateBound(p1, p2);
            p1 += line.Direction.Negate() * radius;
            p2 += line.Direction * radius;

            // Create a Outline, uses a minimum and maximum XYZ point to initialize the outline.
            Outline myOutLn = new Outline(p1, p2);

            // Create a BoundingBoxIntersects filter with this Outline
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);

            // Apply the filter to the elements in the active document
            // This filter excludes all objects derived from View and objects derived from ElementType
            FilteredElementCollector collector = new FilteredElementCollector(Global.UIDoc.Document);
            List<FamilyInstance> elements = collector.WhereElementIsNotElementType().OfClass(typeof(FamilyInstance)).WherePasses(filter).Cast<FamilyInstance>().Where(x => x.MEPModel != null && x.MEPModel is MechanicalFitting).ToList();

            if (elements.Count == 0)
                return false;

            return retVal;
        }

        public static bool CheckPipeIsEnd(Pipe pipe, XYZ point)
        {
            bool retVal = true;
            Dictionary<FamilyInstance, double> keyValuePairs = new Dictionary<FamilyInstance, double>();

            var con = Common.GetConnectorClosestTo(pipe, point);

            if (!con.IsConnected)
                retVal = false;
#if Debug_2020 || Release_2020 || Bundle_2020
            double radius = UnitUtils.ConvertToInternalUnits(10, DisplayUnitType.DUT_MILLIMETERS);
#else
            double radius = UnitUtils.ConvertToInternalUnits(10, UnitTypeId.Millimeters);
#endif

            var bbox = pipe.get_BoundingBox(null);
            XYZ p1 = bbox.Min;
            XYZ p2 = bbox.Max;
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
            {
                return true;
            }

            return retVal;
        }

        public static FamilyInstance se(MEPCurve mepCurveSplit1, MEPCurve mepCurveSplit2)
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

                if (d1 < d2)
                {
                    var con = Common.GetConnectorClosestTo(mepCurveSplit2, p10);
                    familyInstance = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                }
                else
                {
                    var con = Common.GetConnectorClosestTo(mepCurveSplit2, p11);
                    familyInstance = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                }

                return familyInstance;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        public static void ConnectNipple(Pipe pipe, Pipe newPipeZ, FamilyInstance instance, FamilySymbol fmlNipple, XYZ pOn, XYZ pcenter, XYZ sprinkle_point, ref HashSet<ElementId> listIdConnect)
        {
            var nipple = Global.UIDoc.Document.Create.NewFamilyInstance(Line.CreateBound(pOn, pcenter).Origin, fmlNipple, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            if (nipple != null)
            {
                var paraDia = newPipeZ.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                nipple.LookupParameter("Nominal Diameter").Set(paraDia);
                listIdConnect.Add(nipple.Id);
            }

            Common.RotateLineC2(Global.UIDoc.Document, nipple, Line.CreateBound(pOn, pcenter));

            var connectors = Common.ToList(nipple.MEPModel.ConnectorManager.Connectors);
            Connector cNipple = connectors.OrderBy(x => x.Origin.Z).FirstOrDefault();

            Connector c1 = Common.GetConnectorClosestTo(pipe, pOn);
            Connector c3 = Common.GetConnectorClosestTo(newPipeZ, pOn);
            // Get Comector vertical pipe bottom

            var elbow = Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
            listIdConnect.Add(elbow.Id);
            Connector c4 = Common.GetConnectorClosestTo(instance, pcenter);
            Connector c5 = Common.GetConnectorClosestTo(newPipeZ, sprinkle_point);

            var reducer = Global.UIDoc.Document.Create.NewTransitionFitting(c5, c4);
            listIdConnect.Add(reducer.Id);

            //Move Nipple va Reducer

            var conElbow = Common.ToList(elbow.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).LastOrDefault();
            var vector = conElbow.Origin - cNipple.Origin;

            ElementTransformUtils.MoveElement(Global.UIDoc.Document, nipple.Id, vector.Normalize() * conElbow.Origin.DistanceTo(cNipple.Origin));

            var cReducer = Common.ToList(reducer.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).FirstOrDefault();
            Connector cNip = connectors.OrderBy(x => x.Origin.Z).LastOrDefault();

            var vectorMoveReducer = cNip.Origin - cReducer.Origin;

            ElementTransformUtils.MoveElement(Global.UIDoc.Document, reducer.Id, vectorMoveReducer.Normalize() * cNip.Origin.DistanceTo(cReducer.Origin));

            Global.UIDoc.Document.Delete(newPipeZ.Id);

            var cConnectElbow = Common.GetConnectorClosestTo(nipple, conElbow.Origin);

            if (cConnectElbow != null)
                conElbow.ConnectTo(cConnectElbow);

            var cConnectReducer = Common.GetConnectorClosestTo(nipple, cReducer.Origin);

            if (cConnectReducer != null)
                cReducer.ConnectTo(cConnectReducer);
        }

        public static PreferredJunctionType GetPreferredJunctionType(Pipe pipe)
        {
            var pipeType = pipe.PipeType as PipeType;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType;
        }

        public static Pipe ConnectTeeAndNipple(Pipe pipe, Pipe newPipeZ, FamilyInstance instance, FamilySymbol fmlNipple, XYZ pOn, XYZ pcenter, XYZ sprinkle_point, ref HashSet<ElementId> listIdConnect)
        {
            Pipe pipe1 = null;
            var curvePipeMain = pipe.GetCurve();

            var curvePipeDung = newPipeZ.GetCurve();

            Line line = Line.CreateUnbound((curvePipeDung as Line).Origin, (curvePipeDung as Line).Direction);
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = line.Intersect(curvePipeMain, CurveIntersectResultOption.Detailed);
    var point = intersectResult.GetOverlaps()[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            IntersectionResultArray array;
            line.Intersect(curvePipeMain, out array);
            var point = array.get_Item(0).XYZPoint;
#endif

            FamilyInstance tee = null;

            if (GetPreferredJunctionType(pipe) == PreferredJunctionType.Tee)
                tee = CreateTeeFitting(pipe, newPipeZ, point, out pipe1);
            else
                tee = se(pipe as MEPCurve, newPipeZ as MEPCurve);
            if (tee != null)
                listIdConnect.Add(tee.Id);

            Connector c4 = Common.GetConnectorClosestTo(instance, pcenter);
            Connector c5 = Common.GetConnectorClosestTo(newPipeZ, sprinkle_point);

            var reducer = Global.UIDoc.Document.Create.NewTransitionFitting(c5, c4);

            var nipple = Global.UIDoc.Document.Create.NewFamilyInstance(Line.CreateBound(pOn, pcenter).Origin, fmlNipple, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            if (nipple != null)
            {
                var paraDia = newPipeZ.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                nipple.LookupParameter("Nominal Diameter").Set(paraDia);
                listIdConnect.Add(nipple.Id);
            }

            Common.RotateLineC2(Global.UIDoc.Document, nipple, Line.CreateBound(pOn, pcenter));

            var connectors = Common.ToList(nipple.MEPModel.ConnectorManager.Connectors);

            Connector cNipple = connectors.OrderBy(x => x.Origin.Z).FirstOrDefault();

            var conElbow = Common.ToList(tee.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).LastOrDefault();
            var vector = conElbow.Origin - cNipple.Origin;

            ElementTransformUtils.MoveElement(Global.UIDoc.Document, nipple.Id, vector.Normalize() * conElbow.Origin.DistanceTo(cNipple.Origin));

            var cReducer = Common.ToList(reducer.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).FirstOrDefault();
            Connector cNip = connectors.OrderBy(x => x.Origin.Z).LastOrDefault();

            var vectorMoveReducer = cNip.Origin - cReducer.Origin;

            ElementTransformUtils.MoveElement(Global.UIDoc.Document, reducer.Id, vectorMoveReducer.Normalize() * cNip.Origin.DistanceTo(cReducer.Origin));

            Global.UIDoc.Document.Delete(newPipeZ.Id);

            var cConnectElbow = Common.GetConnectorClosestTo(nipple, conElbow.Origin);

            if (cConnectElbow != null)
                conElbow.ConnectTo(cConnectElbow);

            var cConnectReducer = Common.GetConnectorClosestTo(nipple, cReducer.Origin);

            if (cConnectReducer != null)
                cReducer.ConnectTo(cConnectReducer);

            return pipe1;
        }

        public static bool ConnectTee(Pipe pipe, Pipe newPipeZ, FamilyInstance instance, ref HashSet<ElementId> listIdConnect, out Pipe pipeCut)
        {
            pipeCut = null;
            var curvePipeMain = pipe.GetCurve();

            var curvePipeDung = newPipeZ.GetCurve();

            Line line = Line.CreateUnbound((curvePipeDung as Line).Origin, (curvePipeDung as Line).Direction);

            IntersectionResultArray array;

            SetComparisonResult intersec;
            XYZ point = XYZ.Zero;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = line.Intersect(curvePipeMain, CurveIntersectResultOption.Detailed);
    intersec = intersectResult.Result;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            // Giả định biến 'array' đã được bạn khai báo trước đó (IntersectionResultArray array;)
            intersec = line.Intersect(curvePipeMain, out array);
#endif

            if (intersec == SetComparisonResult.Overlap)
            {
#if Debug_2027 || Release_2027 || Release_2026
    point = intersectResult.GetOverlaps()[0].Point;
#else
                point = array.get_Item(0).XYZPoint;
#endif
            }
            else
                return false;

            var cSprinkle = Common.ToList(instance.MEPModel.ConnectorManager.Connectors).FirstOrDefault();

            FamilyInstance fitting = null;

            if (GetPreferredJunctionType(pipe) == PreferredJunctionType.Tee)
                fitting = CreateTeeFitting(pipe, newPipeZ, point, out pipeCut);
            else
                fitting = se(pipe as MEPCurve, newPipeZ as MEPCurve);
            if (fitting != null)
                listIdConnect.Add(fitting.Id);
            Global.UIDoc.Document.Delete(newPipeZ.Id);

            var cTee = Common.ToList(fitting.MEPModel.ConnectorManager.Connectors).OrderBy(x => x.Origin.Z).LastOrDefault();

            var vector = cTee.Origin - cSprinkle.Origin;

            ElementTransformUtils.MoveElement(Global.UIDoc.Document, instance.Id, vector);

            cTee.ConnectTo(cSprinkle);

            Common.GetInformationConectorWye(fitting, null, out Connector _, out Connector _, out Connector conTee);

            if (conTee != null)
            {
                Common.SetRadiusConnector(fitting, conTee, cSprinkle.Radius);
                //conTee.Radius = cSprinkle.Radius;
            }

            return true;
        }

        public static FamilyInstance CreateTeeFitting(Pipe pipeMain, Pipe pipeCurrent, XYZ splitPoint, out Pipe main2)
        {
            Line line1 = null;
            Line line2 = null;
            var curve = (pipeMain.Location as LocationCurve).Curve;

            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            var pipeTempMain1 = pipeMain;

            if ((curve as Line).Direction.IsAlmostEqualTo(XYZ.BasisX))
            {
                line2 = Line.CreateBound(p0, splitPoint);
                line1 = Line.CreateBound(splitPoint, p1);
            }
            else
            {
                line1 = Line.CreateBound(p0, splitPoint);
                line2 = Line.CreateBound(splitPoint, p1);
            }

            (pipeTempMain1.Location as LocationCurve).Curve = line1;

            var newPlace = new XYZ(0, 0, 0);
            var elemIds = ElementTransformUtils.CopyElement(
               Global.UIDoc.Document, pipeTempMain1.Id, newPlace);

            main2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

            (main2.Location as LocationCurve).Curve = line2;

            //Connect
            var c3 = Common.GetConnectorClosestTo(pipeTempMain1, splitPoint);
            var c4 = Common.GetConnectorClosestTo(main2, splitPoint);
            var c5 = Common.GetConnectorClosestTo(pipeCurrent, splitPoint);

            var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c3, c4, c5);
            return fitting;
        }

        public static FamilyInstance CreateTeeFittingSprinkler(Pipe pipeMain, FamilyInstance instance, XYZ splitPoint, out Pipe main2)
        {
            var curve = (pipeMain.Location as LocationCurve).Curve;

            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            var pipeTempMain1 = pipeMain;

            var line1 = Line.CreateBound(p0, splitPoint);
            (pipeTempMain1.Location as LocationCurve).Curve = line1;

            var newPlace = new XYZ(0, 0, 0);
            var elemIds = ElementTransformUtils.CopyElement(
               Global.UIDoc.Document, pipeTempMain1.Id, newPlace);

            main2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

            var line2 = Line.CreateBound(splitPoint, p1);
            (main2.Location as LocationCurve).Curve = line2;

            //Connect
            var c3 = Common.GetConnectorClosestTo(pipeTempMain1, splitPoint);
            var c4 = Common.GetConnectorClosestTo(main2, splitPoint);
            var c5 = Common.GetConnectorClosestTo(instance, splitPoint);

            try
            {
                var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c3, c4, c5);

                return fitting;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }
    }

    public class SprinklerFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            if (element != null
                && element.Category != null
                && (BuiltInCategory)element.Category.Id.ToInt() == BuiltInCategory.OST_Sprinklers)
                return true;

            if (element != null
                                && element.Category != null
                                && (BuiltInCategory)element.Category.Id.ToInt() == BuiltInCategory.OST_Sprinklers)
                return true;

            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }

    public static class PipeExtension
    {

        public static List<Connector> GetConectors(this ConnectorSet connectors)
        {
            List<Connector> connects = new List<Connector>();
            foreach (Connector connector in connectors)
            {
                connects.Add(connector);
            }
            return connects;
        }

        public static Curve GetCurve(this Element element)
        {
            Debug.Assert(null != element.Location,
              "Expected an element with a valid Location");

            LocationCurve locationCurve = element.Location as LocationCurve;

            Debug.Assert(null != locationCurve,
              "Expected an element with a valid LocationCurve");

            return locationCurve.Curve;
        }
    }
}
