using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlumbingSolution.FireProtection.Command.Fire.DiritConnectPipes.Service_B
{

    public class PairPipes
    {
        public Pipe Pipe1 = null;

        public Pipe Pipe2 = null;

        public PairPipes(Pipe p1, Pipe p2)
        {
            Pipe1 = p1;
            Pipe2 = p2;
        }
    }

    public class TwoLevelSmartDialogData
    {
        public bool IsElevationDiffirence { get; set; }

        public bool IsSameEle { get; set; }

        public bool IsCheckedTeeTap { get; set; }

        public bool IsCheckedElbow { get; set; }

        public ElementId PipeTypeId { get; set; }

        public double PipeSize { get; set; }

        public bool IsVerticalPipeSizeEqual { get; set; }

        public bool IsVerticalPipeSizeBiggger { get; set; }

        public bool IsElbowConnection { get; set; }

        public bool IsAutoDelectPipeType { get; set; }

        public bool IsNippleCreating { get; set; }
        public FamilySymbol NippleFamily { get; set; }

        public TwoLevelSmartDialogData()
        {
        }
    }

    public class HandleProcessTwoLevelSmartCommand
    {
        public List<ElementId> MainPipeIds { get; set; }
        public List<Pipe> BranchPipes { get; set; }
        public List<PairPipes> PairBranchPipes { get; set; }
        public TwoLevelSmartDialogData ResultData { get; set; }

        public HandleProcessTwoLevelSmartCommand(List<ElementId> mainPipeIds, List<PairPipes> pairBranchPipes, TwoLevelSmartDialogData dialogResultData)
        {
            BeginInitialize();

            if (mainPipeIds != null
               && mainPipeIds.Count > 0
               && pairBranchPipes != null
               && pairBranchPipes.Count > 0
               && dialogResultData != null)
            {
                MainPipeIds = mainPipeIds;
                PairBranchPipes = pairBranchPipes;
                ResultData = dialogResultData;

                PairBranchPipes.ForEach(item =>
                {
                    BranchPipes.Add(item.Pipe1);
                    if (item.Pipe2 != null)
                        BranchPipes.Add(item.Pipe2);
                });

                //if (ResultData.IsElevationDiffirence)
                {
                    InitializeElevationDifference();
                }
                //else
                //{
                //    InitializeSameElevation();
                //}
            }
        }

        private void BeginInitialize()
        {
            MainPipeIds = new List<ElementId>();
            PairBranchPipes = new List<PairPipes>();
            BranchPipes = new List<Pipe>();
            ResultData = null;
        }

        private void InitializeElevationDifference()
        {
            double diamterPipe = ResultData.PipeSize * Common.mmToFT;
            PipeType pipeType = Global.UIDoc.Document.GetElement(ResultData.PipeTypeId) as PipeType;

            //if (ResultData.IsCheckedElbow)
            //{
            //    List<Tuple<Pipe, Pipe, XYZ>> tuplesFlagElbowLast = new List<Tuple<Pipe, Pipe, XYZ>>();
            //    foreach (Pipe branchPipe in BranchPipes)
            //    {
            //        Transaction reTrans = new Transaction(Global.UIDoc.Document, "TWO_LEVEL_SMART_PROCESS_1");
            //        reTrans.Start();

            //        try
            //        {
            //            List<Pipe> mainPipes = new List<Pipe>();
            //            MainPipeIds.ForEach(item =>
            //            {
            //                mainPipes.Add(Global.UIDoc.Document.GetElement(item) as Pipe);
            //            });

            //            Pipe validMainPipe = ProcessMainPipe(mainPipes, branchPipe, out bool flagSplit, out XYZ intPnt);
            //            // Process main pipe
            //            Curve curveMainPipe = (validMainPipe.Location as LocationCurve).Curve;
            //            Curve curveBranchPipe = (branchPipe.Location as LocationCurve).Curve;

            //            XYZ firstPnt_MainPipe = curveMainPipe.GetEndPoint(0);
            //            XYZ secondPnt_MainPipe = curveMainPipe.GetEndPoint(1);

            //            XYZ firstPnt_ProcessPipe = curveBranchPipe.GetEndPoint(0);
            //            XYZ secondPnt_ProcessPipe = curveBranchPipe.GetEndPoint(1);

            //            double dTempEvaluate = 1000;

            //            var curveMainPipeFlatten = Line.CreateBound(new XYZ(firstPnt_MainPipe.X, firstPnt_MainPipe.Y, 0), new XYZ(secondPnt_MainPipe.X, secondPnt_MainPipe.Y, 0));

            //            XYZ dir = (secondPnt_ProcessPipe.FlattenPoint() - firstPnt_MainPipe.FlattenPoint()).Normalize();

            //            var curveProcessFlatten = Line.CreateUnbound(firstPnt_ProcessPipe.FlattenPoint(), (branchPipe.GetFlattenCurve()).Direction * 100);

            //            // Find intersection point
            //            XYZ newPlace = new XYZ(0, 0, 0);
            //            Pipe temp_processPipe_1 = validMainPipe;
            //            Pipe temp_processPipe_2 = null;
            //            XYZ finalIntPntMainPipe = null;
            //            XYZ finalIntPntBranchPipe = null;
            //            Curve curveMainPipeExtend3d_temp = Line.CreateUnbound(curveMainPipe.GetEndPoint(0), (curveMainPipe as Line).Direction * 100); ;
            //            IntersectionResultArray intRetArr = new IntersectionResultArray();

            //            //Truong hop dau ong
            //            if (flagSplit == false)
            //            {
            //                //Expand
            //                var index = curveMainPipeFlatten.GetEndPoint(0).DistanceTo(intPnt) < curveMainPipeFlatten.GetEndPoint(1).DistanceTo(intPnt) ? 0 : 1;
            //                var curveExpand = Line.CreateUnbound(curveMainPipeFlatten.GetEndPoint(index), curveMainPipeFlatten.Direction * 100);
            //                var inter = curveExpand.Intersect(curveProcessFlatten, out intRetArr);
            //                if (inter != SetComparisonResult.Overlap)
            //                {
            //                    reTrans.RollBack();
            //                    continue;
            //                }

            //                var p2d = intRetArr.get_Item(0).XYZPoint;
            //                var p3d = new XYZ(p2d.X, p2d.Y, -100);
            //                var lineZ = Line.CreateUnbound(p3d, XYZ.BasisZ);
            //                curveMainPipeExtend3d_temp = Line.CreateUnbound(curveMainPipe.GetEndPoint(index), (curveMainPipe as Line).Direction * 100);

            //                intRetArr = new IntersectionResultArray();
            //                inter = curveMainPipeExtend3d_temp.Intersect(lineZ, out intRetArr);
            //                if (inter != SetComparisonResult.Overlap)
            //                {
            //                    reTrans.RollBack();
            //                    continue;
            //                }

            //                finalIntPntMainPipe = intRetArr.get_Item(0).XYZPoint;
            //                var curveBranchPipeExtend3d_temp = Line.CreateUnbound(curveBranchPipe.GetEndPoint(0), (curveBranchPipe as Line).Direction * 100);
            //                intRetArr = new IntersectionResultArray();
            //                inter = lineZ.Intersect(curveBranchPipeExtend3d_temp, out intRetArr);
            //                if (inter != SetComparisonResult.Overlap)
            //                {
            //                    reTrans.RollBack();
            //                    continue;
            //                }

            //                finalIntPntBranchPipe = intRetArr.get_Item(0).XYZPoint;
            //            }
            //            else
            //            {
            //                var inter = curveMainPipeFlatten.Intersect(curveProcessFlatten, out intRetArr);
            //                if (inter != SetComparisonResult.Overlap)
            //                {
            //                    reTrans.RollBack();
            //                    continue;
            //                }

            //                var p2d = intRetArr.get_Item(0).XYZPoint;

            //                var p3d = new XYZ(p2d.X, p2d.Y, intPnt.Z);

            //                var lineZ = Line.CreateUnbound(p3d, XYZ.BasisZ);

            //                intRetArr = new IntersectionResultArray();
            //                inter = curveMainPipe.Intersect(lineZ, out intRetArr);
            //                if (inter != SetComparisonResult.Overlap)
            //                {
            //                    reTrans.RollBack();
            //                    continue;
            //                }

            //                finalIntPntMainPipe = intRetArr.get_Item(0).XYZPoint;
            //                var curveBranchPipeExtend3d_temp = Line.CreateUnbound(curveBranchPipe.GetEndPoint(0), (curveBranchPipe as Line).Direction * 100);
            //                intRetArr = new IntersectionResultArray();
            //                inter = lineZ.Intersect(curveBranchPipeExtend3d_temp, out intRetArr);
            //                if (inter != SetComparisonResult.Overlap)
            //                {
            //                    reTrans.RollBack();
            //                    continue;
            //                }

            //                finalIntPntBranchPipe = intRetArr.get_Item(0).XYZPoint;
            //                temp_processPipe_2 = null;
            //                bool flagCreateTee = true;
            //                if (GetPreferredJunctionType(validMainPipe) != PreferredJunctionType.Tee)
            //                {
            //                    flagCreateTee = false;
            //                }
            //                ProcessStartSidePipeCaseElbow(validMainPipe, out temp_processPipe_2, finalIntPntMainPipe, flagCreateTee, ResultData.IsElbowConnection);
            //                if (temp_processPipe_2 != null)
            //                {
            //                    MainPipeIds.Add(temp_processPipe_2.Id);
            //                }
            //            }

            //            if (finalIntPntMainPipe == null || finalIntPntBranchPipe == null)
            //            {
            //                reTrans.RollBack();
            //                continue;
            //            }

            //            finalIntPntMainPipe = curveMainPipeExtend3d_temp.Project(finalIntPntBranchPipe).XYZPoint;

            //            //if (ResultData.FlagAddElbowLastBranch == false && flagSplit == false)
            //            //{
            //            //    reTrans.RollBack();
            //            //    continue;
            //            //}
            //            var verticalPipe = Common.Clone(branchPipe) as Pipe;
            //            verticalPipe.PipeType = Global.UIDoc.Document.GetElement(validMainPipe.GetTypeId()) as PipeType;
            //            (verticalPipe.Location as LocationCurve).Curve = Line.CreateBound(finalIntPntMainPipe, finalIntPntBranchPipe);

            //            try
            //            {
            //                var c1 = Common.GetConnectorClosestTo(branchPipe, finalIntPntBranchPipe);
            //                var c3 = Common.GetConnectorClosestTo(verticalPipe, finalIntPntBranchPipe);

            //                try
            //                {
            //                    Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
            //                }
            //                catch
            //                {
            //                }
            //            }
            //            catch (System.Exception ex)
            //            {
            //            }

            //            try
            //            {
            //                if ((double)Common.GetValueParameterByBuilt(validMainPipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0
            //           && !Common.IsParallel((curveMainPipe as Line).Direction, XYZ.BasisX)
            //           && !Common.IsParallel((curveMainPipe as Line).Direction, XYZ.BasisY))
            //                {
            //                    XYZ tmpVector = curveMainPipe.GetEndPoint(0) - finalIntPntMainPipe;
            //                    finalIntPntBranchPipe = finalIntPntBranchPipe + tmpVector.Normalize() * 0.001;

            //                    (verticalPipe.Location as LocationCurve).Curve = Line.CreateBound(finalIntPntMainPipe, finalIntPntBranchPipe); ;
            //                }

            //                var c1 = Common.GetConnectorClosestTo(temp_processPipe_1, finalIntPntMainPipe);
            //                var c3 = Common.GetConnectorClosestTo(verticalPipe, finalIntPntMainPipe);

            //                if (GetPreferredJunctionType(temp_processPipe_1) != PreferredJunctionType.Tee && flagSplit == true)
            //                {
            //                    if (!ResultData.IsElbowConnection)
            //                        CreateTap(temp_processPipe_1 as MEPCurve, verticalPipe as MEPCurve);
            //                    else
            //                    {
            //                        if (CheckPipeIsEnd(temp_processPipe_1, verticalPipe.GetCurve().GetEndPoint(0)))
            //                            CreateTap(temp_processPipe_1 as MEPCurve, verticalPipe as MEPCurve);
            //                        else
            //                            Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
            //                    }
            //                }
            //                else
            //                {
            //                    if (temp_processPipe_2 != null)
            //                    {
            //                        var c2 = Common.GetConnectorClosestTo(temp_processPipe_2, finalIntPntMainPipe);
            //                        var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
            //                    }
            //                    else
            //                    {
            //                        if (ResultData.IsElbowConnection)
            //                        {
            //                            //double diameter = (double)GetParameterValueByName(validMainPipe, "Diameter");
            //                            //verticalPipe.LookupParameter("Diameter").Set(diameter);
            //                            if (!CheckPipeIsEnd(temp_processPipe_1, c3.Origin))
            //                                Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
            //                            else if (GetPreferredJunctionType(temp_processPipe_1) == PreferredJunctionType.Tap)
            //                            {
            //                                CreateTap(temp_processPipe_1 as MEPCurve, verticalPipe as MEPCurve);
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch (System.Exception ex)
            //            {
            //                reTrans.RollBack();
            //            }

            //            reTrans.Commit();
            //        }
            //        catch (Exception)
            //        {
            //            reTrans.RollBack();
            //        }
            //    }
            //}
            //else
            {
                foreach (PairPipes pairBranchPipe in PairBranchPipes)
                {
                    Pipe pipe1 = pairBranchPipe.Pipe1;
                    Pipe pipe2 = pairBranchPipe.Pipe2;

                    Transaction reTrans = new Transaction(Global.UIDoc.Document, "TOPTEE");
                    reTrans.Start();

                    try
                    {
                        List<Pipe> mainPipes = new List<Pipe>();
                        MainPipeIds.ForEach(item =>
                        {
                            mainPipes.Add(Global.UIDoc.Document.GetElement(item) as Pipe);
                        });

                        bool twoPipes = pipe2 != null ? true : false;
                        Pipe validMainPipe = ProcessMainPipe(mainPipes, pipe1, out bool flagSplit, out XYZ intPnt);
                        if (ResultData.IsElbowConnection == true)
                        {
                            // Process main pipe
                            Curve curveMainPipe = (validMainPipe.Location as LocationCurve).Curve;
                            Curve curveBranchPipe = (pipe1.Location as LocationCurve).Curve;

                            XYZ firstPnt_MainPipe = curveMainPipe.GetEndPoint(0);
                            XYZ secondPnt_MainPipe = curveMainPipe.GetEndPoint(1);

                            XYZ firstPnt_ProcessPipe = curveBranchPipe.GetEndPoint(0);
                            XYZ secondPnt_ProcessPipe = curveBranchPipe.GetEndPoint(1);

                            double dTempEvaluate = 1000;

                            var curveMainPipeFlatten = Line.CreateBound(new XYZ(firstPnt_MainPipe.X, firstPnt_MainPipe.Y, 0), new XYZ(secondPnt_MainPipe.X, secondPnt_MainPipe.Y, 0));

                            XYZ dir = (secondPnt_ProcessPipe.FlattenPoint() - firstPnt_MainPipe.FlattenPoint()).Normalize();

                            var curveProcessFlatten = Line.CreateUnbound(firstPnt_ProcessPipe.FlattenPoint(), (pipe1.GetFlattenCurve()).Direction * 100);

                            var curveMainPipeExtend3d_temp = Line.CreateUnbound(curveMainPipe.GetEndPoint(0), (curveMainPipe as Line).Direction * 100);
                            // Find intersection point
                            XYZ newPlace = new XYZ(0, 0, 0);
                            Pipe temp_processPipe_1 = validMainPipe;
                            Pipe temp_processPipe_2 = null;
                            XYZ finalIntPntMainPipe = null;
                            XYZ finalIntPntBranchPipe = null;

                            IntersectionResultArray intRetArr = new IntersectionResultArray();

                            bool flagDauOng_CaseTap = false;
                            //Truong hop dau ong
                            if (flagSplit == false)
                            {
                                //Expand
                                var index = curveMainPipeFlatten.GetEndPoint(0).DistanceTo(intPnt) < curveMainPipeFlatten.GetEndPoint(1).DistanceTo(intPnt) ? 0 : 1;
                                var curveExpand = Line.CreateUnbound(curveMainPipeFlatten.GetEndPoint(index), curveMainPipeFlatten.Direction * 100);

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---

    // 1. Giao điểm đầu tiên giữa curveExpand và curveProcessFlatten
    var interResult1 = curveExpand.Intersect(curveProcessFlatten, CurveIntersectResultOption.Detailed);
    if (interResult1.Result != SetComparisonResult.Overlap)
    {
        continue;
    }

    var intRetArr1 = interResult1.GetOverlaps();
    if (intRetArr1 == null || intRetArr1.Count == 0) continue;

    var p2d = intRetArr1.First().Point;
    var p3d = new XYZ(p2d.X, p2d.Y, intPnt.Z);
    var lineZ = Line.CreateUnbound(p3d, XYZ.BasisZ);
    curveMainPipeExtend3d_temp = Line.CreateUnbound(curveMainPipe.GetEndPoint(index), (curveMainPipe as Line).Direction * 100);

    // 2. Giao điểm thứ hai giữa curveMainPipeExtend3d_temp và lineZ
    var interResult2 = curveMainPipeExtend3d_temp.Intersect(lineZ, CurveIntersectResultOption.Detailed);
    if (interResult2.Result != SetComparisonResult.Overlap)
    {
        continue;
    }

     intRetArr1 = interResult2.GetOverlaps();
    if (intRetArr1 == null || intRetArr1.Count == 0) continue;

    finalIntPntMainPipe = intRetArr1.First().Point;
    var curveBranchPipeExtend3d_temp = Line.CreateUnbound(curveBranchPipe.GetEndPoint(0), (curveBranchPipe as Line).Direction * 100);

    // 3. Giao điểm thứ ba giữa lineZ và curveBranchPipeExtend3d_temp
    var interResult3 = lineZ.Intersect(curveBranchPipeExtend3d_temp, CurveIntersectResultOption.Detailed);
    if (interResult3.Result != SetComparisonResult.Overlap)
    {
        continue;
    }

       finalIntPntBranchPipe = intRetArr1.First().Point;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                var inter = curveExpand.Intersect(curveProcessFlatten, out intRetArr);
                                if (inter != SetComparisonResult.Overlap)
                                {
                                    continue;
                                }

                                var p2d = intRetArr.get_Item(0).XYZPoint;
                                var p3d = new XYZ(p2d.X, p2d.Y, intPnt.Z);
                                var lineZ = Line.CreateUnbound(p3d, XYZ.BasisZ);
                                curveMainPipeExtend3d_temp = Line.CreateUnbound(curveMainPipe.GetEndPoint(index), (curveMainPipe as Line).Direction * 100);

                                intRetArr = new IntersectionResultArray();
                                inter = curveMainPipeExtend3d_temp.Intersect(lineZ, out intRetArr);
                                if (inter != SetComparisonResult.Overlap)
                                {
                                    continue;
                                }

                                finalIntPntMainPipe = intRetArr.get_Item(0).XYZPoint;
                                var curveBranchPipeExtend3d_temp = Line.CreateUnbound(curveBranchPipe.GetEndPoint(0), (curveBranchPipe as Line).Direction * 100);
                                intRetArr = new IntersectionResultArray();
                                inter = lineZ.Intersect(curveBranchPipeExtend3d_temp, out intRetArr);
                                if (inter != SetComparisonResult.Overlap)
                                {
                                    continue;
                                }
                                finalIntPntBranchPipe = intRetArr.get_Item(0).XYZPoint;

#endif
                            }
                            else
                            {
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---

    // 1. Tìm giao điểm phẳng giữa curveMainPipeFlatten và curveProcessFlatten
    var interResult1 = curveMainPipeFlatten.Intersect(curveProcessFlatten, CurveIntersectResultOption.Detailed);
    if (interResult1.Result != SetComparisonResult.Overlap)
    {
        continue;
    }

    var intRetArr1 = interResult1.GetOverlaps();
    if (intRetArr1 == null || intRetArr1.Count == 0) continue;

    var p2d = intRetArr1.First().Point;
    var p3d = new XYZ(p2d.X, p2d.Y, intPnt.Z);
    var lineZ = Line.CreateUnbound(p3d, XYZ.BasisZ);

    // 2. Tìm giao điểm 3D giữa curveMainPipe và trục đứng lineZ
    var interResult2 = curveMainPipe.Intersect(lineZ, CurveIntersectResultOption.Detailed);
    if (interResult2.Result != SetComparisonResult.Overlap)
    {
        continue;
    }

    intRetArr1 = interResult2.GetOverlaps();
    if (intRetArr1 == null || intRetArr1.Count == 0) continue;

    finalIntPntMainPipe = intRetArr1.First().Point;
    var curveBranchPipeExtend3d_temp = Line.CreateUnbound(curveBranchPipe.GetEndPoint(0), (curveBranchPipe as Line).Direction * 100);

    // 3. Tìm giao điểm 3D giữa trục đứng lineZ và curveBranchPipeExtend3d_temp
    var interResult3 = lineZ.Intersect(curveBranchPipeExtend3d_temp, CurveIntersectResultOption.Detailed);
    if (interResult3.Result != SetComparisonResult.Overlap)
    {
        continue;
    }

    intRetArr1 = interResult3.GetOverlaps();
    if (intRetArr1 == null || intRetArr1.Count == 0) continue;

    finalIntPntBranchPipe = intRetArr1.First().Point;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                var inter = curveMainPipeFlatten.Intersect(curveProcessFlatten, out intRetArr);
                                if (inter != SetComparisonResult.Overlap)
                                {
                                    continue;
                                }

                                var p2d = intRetArr.get_Item(0).XYZPoint;
                                var p3d = new XYZ(p2d.X, p2d.Y, intPnt.Z);
                                var lineZ = Line.CreateUnbound(p3d, XYZ.BasisZ);

                                intRetArr = new IntersectionResultArray();
                                inter = curveMainPipe.Intersect(lineZ, out intRetArr);
                                if (inter != SetComparisonResult.Overlap)
                                {
                                    continue;
                                }

                                finalIntPntMainPipe = intRetArr.get_Item(0).XYZPoint;
                                var curveBranchPipeExtend3d_temp = Line.CreateUnbound(curveBranchPipe.GetEndPoint(0), (curveBranchPipe as Line).Direction * 100);

                                intRetArr = new IntersectionResultArray();
                                inter = lineZ.Intersect(curveBranchPipeExtend3d_temp, out intRetArr);
                                if (inter != SetComparisonResult.Overlap)
                                {
                                    continue;
                                }

                                finalIntPntBranchPipe = intRetArr.get_Item(0).XYZPoint;
#endif
                                temp_processPipe_2 = null;
                                bool flagCreateTee = true;
                                if (GetPreferredJunctionType(validMainPipe) != PreferredJunctionType.Tee)
                                {
                                    flagCreateTee = false;
                                }
                                ProcessStartSidePipe(validMainPipe, out temp_processPipe_2, finalIntPntMainPipe, flagCreateTee, out flagDauOng_CaseTap);
                                if (temp_processPipe_2 != null)
                                {
                                    MainPipeIds.Add(temp_processPipe_2.Id);
                                }
                            }

                            if (finalIntPntMainPipe == null || finalIntPntBranchPipe == null)
                            {
                                reTrans.RollBack();
                                continue;
                            }

                            finalIntPntMainPipe = curveMainPipeExtend3d_temp.Project(finalIntPntBranchPipe).XYZPoint;
                            var verticalPipe = Common.Clone(pipe1) as Pipe;
                            verticalPipe.PipeType = pipeType;
                            verticalPipe.LookupParameter("Diameter").Set(diamterPipe);
                            (verticalPipe.Location as LocationCurve).Curve = Line.CreateBound(finalIntPntMainPipe, finalIntPntBranchPipe);

                            try
                            {
                                var c1 = Common.GetConnectorClosestTo(temp_processPipe_1, finalIntPntMainPipe);
                                var c3 = Common.GetConnectorClosestTo(verticalPipe, finalIntPntMainPipe);

                                if (GetPreferredJunctionType(temp_processPipe_1) != PreferredJunctionType.Tee && flagSplit == true)
                                {
                                    if (flagDauOng_CaseTap == false)
                                    {
                                        CreateTap(temp_processPipe_1 as MEPCurve, verticalPipe as MEPCurve);
                                    }
                                    else
                                    {
                                        //double diameter = (double)GetParameterValueByName(validMainPipe, "Diameter");
                                        //verticalPipe.LookupParameter("Diameter").Set(diameter);
                                        Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                    }
                                }
                                else
                                {
                                    if (temp_processPipe_2 != null)
                                    {
                                        var c2 = Common.GetConnectorClosestTo(temp_processPipe_2, finalIntPntMainPipe);
                                        var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c1, c2, c3);
                                    }
                                    else
                                    {
                                        if (ResultData.IsElbowConnection)
                                        {
                                            //double diameter = (double)GetParameterValueByName(validMainPipe, "Diameter");
                                            //verticalPipe.LookupParameter("Diameter").Set(diameter);
                                            Global.UIDoc.Document.Create.NewElbowFitting(c1, c3);
                                        }
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                            }

                            XYZ inter_main1 = null;
                            XYZ p0_sub1 = null;
                            XYZ p1_sub1 = null;
                            XYZ inters_sub1 = null;

                            DivideCase_2_Special(validMainPipe, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1);

                            if (pipe2 == null)
                            {
                                var temp_pipe1 = pipe1;
                                var temp_pipe2_id = PlumbingUtils.BreakCurve(Global.UIDoc.Document, temp_pipe1.Id, inters_sub1);
                                var temp_pipe2 = Global.UIDoc.Document.GetElement(temp_pipe2_id) as Pipe;
                                pipe1 = temp_pipe1;
                                pipe2 = temp_pipe2;
                                DivideCase_2_Special(validMainPipe, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1);
                            }

                            XYZ inter_main2 = null;
                            XYZ p0_sub2 = null;
                            XYZ p1_sub2 = null;
                            XYZ inters_sub2 = null;

                            if (DivideCase_2_Special(validMainPipe, pipe2, true, out inter_main2, out p0_sub2, out p1_sub2, out inters_sub2) == false)
                            {
                                reTrans.RollBack();
                                continue;
                            }
                            //Common.CreateModelLine(p1_sub2, Common.NewPoint(p1_sub2));

                            if (inter_main1.DistanceTo(inter_main2) > 0.001)
                            {
                                reTrans.RollBack();
                                continue; //Check
                            }

                            if (inters_sub1.DistanceTo(inters_sub2) > 0.001)
                            {
                                reTrans.RollBack();
                                continue; //Check
                            }

                            //Process for sub pipes
                            var c5 = Common.GetConnectorClosestTo(verticalPipe, inters_sub1);

                            double dMoi = 15 * Common.mmToFT;

                            double d10 = 10 * Common.mmToFT;
                            double dtemp = 2;

                            if (g(diamterPipe, pipe1.Diameter) && g(diamterPipe, pipe2.Diameter))
                            {
                                //Connect to sub
                                (pipe1.Location as LocationCurve).Curve = Line.CreateBound(p0_sub1, p1_sub1);
                                (pipe2.Location as LocationCurve).Curve = Line.CreateBound(p0_sub2, p1_sub2);

                                //Connect
                                var c3 = Common.GetConnectorClosestTo(pipe1, inters_sub1);
                                var c4 = Common.GetConnectorClosestTo(pipe2, inters_sub1);

                                if (CreateFittingForMEPUtils.CreateTee(c3, c4, c5) == null)
                                {
                                    reTrans.RollBack();
                                    continue;
                                }
                            }
                            else
                            {
                                Connector c3 = null;
                                Pipe pipe_moi_1 = null;

                                XYZ v = null;
                                if (g(diamterPipe, pipe1.Diameter) == false)
                                {
                                    //Tao mot ong mồi
                                    pipe_moi_1 = Common.Clone(verticalPipe) as Pipe;
                                    Common.DisconnectFrom(pipe_moi_1);
                                    pipe_moi_1.LookupParameter("Diameter").Set(diamterPipe);

                                    var line = Line.CreateBound(p0_sub1, p1_sub1);

                                    var p1 = p0_sub1;// line.Evaluate(dtemp, false);

                                    if (p0_sub1.DistanceTo(inters_sub1) < 0.01)
                                    {
                                        p1 = p1_sub1;
                                    }

                                    (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(inters_sub1, p1);

                                    c3 = Common.GetConnectorClosestTo(pipe_moi_1, inters_sub1);

                                    v = line.Direction;
                                }
                                else
                                {
                                    //Connect to sub

                                    var line = Line.CreateBound(p0_sub1, p1_sub1);
                                    (pipe1.Location as LocationCurve).Curve = line;
                                    c3 = Common.GetConnectorClosestTo(pipe1, inters_sub1);

                                    v = line.Direction;
                                }

                                Connector c4 = null;
                                Pipe pipe_moi_2 = null;

                                if (g(diamterPipe, pipe2.Diameter) == false)
                                {
                                    //Tao mot ong mồi
                                    pipe_moi_2 = Common.Clone(verticalPipe) as Pipe;
                                    Common.DisconnectFrom(pipe_moi_2);
                                    pipe_moi_2.LookupParameter("Diameter").Set(diamterPipe);

                                    var line = Line.CreateBound(p0_sub2, p1_sub2);

                                    var p1 = p0_sub2;// line.Evaluate(dtemp, false);
                                    if (p0_sub2.DistanceTo(inters_sub2) < 0.01)
                                    {
                                        p1 = p1_sub2;
                                    }

                                    (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(inters_sub2, p1);

                                    c4 = Common.GetConnectorClosestTo(pipe_moi_2, inters_sub2);
                                }
                                else
                                {
                                    (pipe2.Location as LocationCurve).Curve = Line.CreateBound(p0_sub2, p1_sub2);
                                    c4 = Common.GetConnectorClosestTo(pipe2, inters_sub1);
                                }

                                //Connect
                                FamilyInstance fitting = CreateFittingForMEPUtils.CreateTee(c3, c4, c5);
                                if (fitting == null)
                                {
                                    reTrans.RollBack/*Commit*/();
                                    continue;
                                }

                                if (fitting != null)
                                {
                                    Connector main1 = null;
                                    Connector main2 = null;
                                    Connector tee = null;
                                    Common.GetInfo(fitting, v, out main1, out main2, out tee);

                                    var mep1 = ee(main1.AllRefs);
                                    var mep2 = ee(main2.AllRefs);

                                    var main1_p = main1.Origin;
                                    var v_m_1 = main1.CoordinateSystem.BasisZ;

                                    var main2_p = main2.Origin;
                                    var v_m_2 = main2.CoordinateSystem.BasisZ;

                                    FamilyInstance topReducer_1 = null;
                                    FamilyInstance topReducer_2 = null;
                                    //1
                                    if (pipe_moi_1 != null)
                                    {
                                        if (mep1 != null && pipe_moi_1.Id == mep1.Id)
                                        {
                                            var line = Line.CreateUnbound(main1_p, v_m_1 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(main1_p, p);

                                            var futher = inters_sub1.DistanceTo(p0_sub1) > inters_sub1.DistanceTo(p1_sub1) ? p0_sub1 : p1_sub1;

                                            ll(pipe1, futher, p);

                                            topReducer_1 = CreateFittingForMEPUtils.ctt(pipe1, pipe_moi_1);
                                        }
                                        else if (mep2 != null && pipe_moi_1.Id == mep2.Id)
                                        {
                                            var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                            var futher = inters_sub1.DistanceTo(p0_sub1) > inters_sub1.DistanceTo(p1_sub1) ? p0_sub1 : p1_sub1;

                                            ll(pipe1, futher, p);

                                            topReducer_1 = CreateFittingForMEPUtils.ctt(pipe1, pipe_moi_1);
                                        }
                                    }

                                    //2
                                    if (pipe_moi_2 != null)
                                    {
                                        if (mep1 != null && pipe_moi_2.Id == mep1.Id)
                                        {
                                            var line = Line.CreateUnbound(main1_p, v_m_1 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(main1_p, p);

                                            var futher = inters_sub2.DistanceTo(p0_sub2) > inters_sub2.DistanceTo(p1_sub2) ? p0_sub2 : p1_sub2;

                                            ll(pipe2, futher, p);

                                            topReducer_2 = CreateFittingForMEPUtils.ctt(pipe2, pipe_moi_2);
                                        }
                                        else if (mep2 != null && pipe_moi_2.Id == mep2.Id)
                                        {
                                            var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                            var futher = inters_sub2.DistanceTo(p0_sub2) > inters_sub2.DistanceTo(p1_sub2) ? p0_sub2 : p1_sub2;

                                            ll(pipe2, futher, p);

                                            topReducer_2 = CreateFittingForMEPUtils.ctt(pipe2, pipe_moi_2);
                                        }
                                    }

                                    if (ResultData.IsNippleCreating == true && ResultData.NippleFamily != null)
                                    {
                                        // Nipple Family
                                        HandlerConnectAccessoryNipple(pipe_moi_1, topReducer_1, fitting, ResultData.NippleFamily);
                                        HandlerConnectAccessoryNipple(pipe_moi_2, topReducer_2, fitting, ResultData.NippleFamily);
                                    }
                                }
                            }
                        }
                        else
                        {
                            XYZ inter_main1 = null;
                            XYZ p0_sub1 = null;
                            XYZ p1_sub1 = null;
                            XYZ inters_sub1 = null;
                            Pipe main = null;

                            foreach (ElementId pId in MainPipeIds)
                            {
                                var m = Global.UIDoc.Document.GetElement(pId) as Pipe;
                                if (DivideCase_2(m, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1) == true)
                                {
                                    main = m;
                                    break;
                                }
                            }

                            // Common.CreateModelLine(p1_sub1, Common.NewPoint(p1_sub1));

                            if (pipe2 == null)
                            {
                                var temp_pipe1 = pipe1;
                                var temp_pipe2_id = PlumbingUtils.BreakCurve(Global.UIDoc.Document, temp_pipe1.Id, inters_sub1);
                                var temp_pipe2 = Global.UIDoc.Document.GetElement(temp_pipe2_id) as Pipe;
                                pipe1 = temp_pipe1;
                                pipe2 = temp_pipe2;
                            }

                            DivideCase_2(main, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1);

                            XYZ inter_main2 = null;
                            XYZ p0_sub2 = null;
                            XYZ p1_sub2 = null;
                            XYZ inters_sub2 = null;

                            if (DivideCase_2(main, pipe2, true, out inter_main2, out p0_sub2, out p1_sub2, out inters_sub2) == false)
                            {
                                reTrans.RollBack();
                                continue;
                            }
                            //Common.CreateModelLine(p1_sub2, Common.NewPoint(p1_sub2));

                            if (inter_main1.DistanceTo(inter_main2) > 0.001)
                            {
                                reTrans.RollBack();
                                continue; //Check
                            }

                            if (inters_sub1.DistanceTo(inters_sub2) > 0.001)
                            {
                                reTrans.RollBack();
                                continue; //Check
                            }

                            //Create vertical pipe
                            var vertical = Common.Clone(pipe1) as Pipe;
                            vertical.PipeType = pipeType;

                            vertical.LookupParameter("Diameter").Set(diamterPipe);

                            (vertical.Location as LocationCurve).Curve = Line.CreateBound(inter_main1, inters_sub1);

                            //Conect to main pipe
                            if (gr(main) == PreferredJunctionType.Tee)
                            {
                                //Try
                                if (main as Pipe != null && vertical as Pipe != null && inter_main1 != null)
                                {
                                    Pipe main2 = null;

                                    var fitting = ee(main as Pipe, vertical as Pipe, inter_main1, out main2);
                                    if (main2 != null && fitting != null)
                                    {
                                        MainPipeIds.Add(main2.Id);
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
                                if (CreateFittingForMEPUtils.se(main, vertical) == false)
                                {
                                    reTrans.RollBack();
                                    continue;
                                }
                            }

                            //Process for sub pipes
                            var c5 = Common.GetConnectorClosestTo(vertical, inters_sub1);

                            double dMoi = 15 * Common.mmToFT;

                            double d10 = 10 * Common.mmToFT;
                            double dtemp = 2;

                            if (g(diamterPipe, pipe1.Diameter) && g(diamterPipe, pipe2.Diameter))
                            {
                                //Connect to sub
                                (pipe1.Location as LocationCurve).Curve = Line.CreateBound(p0_sub1, p1_sub1);
                                (pipe2.Location as LocationCurve).Curve = Line.CreateBound(p0_sub2, p1_sub2);

                                //Connect
                                var c3 = Common.GetConnectorClosestTo(pipe1, inters_sub1);
                                var c4 = Common.GetConnectorClosestTo(pipe2, inters_sub1);

                                if (CreateFittingForMEPUtils.CreateTee(c3, c4, c5) == null)
                                {
                                    reTrans.RollBack();
                                    continue;
                                }
                            }
                            else
                            {
                                Connector c3 = null;
                                Pipe pipe_moi_1 = null;

                                XYZ v = null;
                                if (g(diamterPipe, pipe1.Diameter) == false)
                                {
                                    //Tao mot ong mồi
                                    pipe_moi_1 = Common.Clone(vertical) as Pipe;
                                    Common.DisconnectFrom(pipe_moi_1);
                                    var line = Line.CreateBound(p0_sub1, p1_sub1);

                                    var p1 = p0_sub1;// line.Evaluate(dtemp, false);

                                    if (p0_sub1.DistanceTo(inters_sub1) < 0.01)
                                    {
                                        p1 = p1_sub1;
                                    }

                                    (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(inters_sub1, p1);

                                    c3 = Common.GetConnectorClosestTo(pipe_moi_1, inters_sub1);

                                    v = line.Direction;
                                }
                                else
                                {
                                    //Connect to sub

                                    var line = Line.CreateBound(p0_sub1, p1_sub1);
                                    (pipe1.Location as LocationCurve).Curve = line;
                                    c3 = Common.GetConnectorClosestTo(pipe1, inters_sub1);

                                    v = line.Direction;
                                }

                                Connector c4 = null;
                                Pipe pipe_moi_2 = null;

                                if (g(diamterPipe, pipe2.Diameter) == false)
                                {
                                    //Tao mot ong mồi
                                    pipe_moi_2 = Common.Clone(vertical) as Pipe;
                                    Common.DisconnectFrom(pipe_moi_2);
                                    var line = Line.CreateBound(p0_sub2, p1_sub2);

                                    var p1 = p0_sub2;// line.Evaluate(dtemp, false);
                                    if (p0_sub2.DistanceTo(inters_sub2) < 0.01)
                                    {
                                        p1 = p1_sub2;
                                    }

                                    (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(inters_sub2, p1);

                                    c4 = Common.GetConnectorClosestTo(pipe_moi_2, inters_sub2);
                                }
                                else
                                {
                                    (pipe2.Location as LocationCurve).Curve = Line.CreateBound(p0_sub2, p1_sub2);
                                    c4 = Common.GetConnectorClosestTo(pipe2, inters_sub1);
                                }

                                //Connect
                                FamilyInstance fitting = CreateFittingForMEPUtils.CreateTee(c3, c4, c5);
                                if (fitting == null)
                                {
                                    reTrans.RollBack/*Commit*/();
                                    continue;
                                }

                                if (fitting != null)
                                {
                                    Connector main1 = null;
                                    Connector main2 = null;
                                    Connector tee = null;
                                    Common.GetInfo(fitting, v, out main1, out main2, out tee);

                                    var mep1 = ee(main1.AllRefs);
                                    var mep2 = ee(main2.AllRefs);

                                    var main1_p = main1.Origin;
                                    var v_m_1 = main1.CoordinateSystem.BasisZ;

                                    var main2_p = main2.Origin;
                                    var v_m_2 = main2.CoordinateSystem.BasisZ;

                                    FamilyInstance topReducer_1 = null;
                                    FamilyInstance topReducer_2 = null;
                                    //1
                                    if (pipe_moi_1 != null)
                                    {
                                        if (mep1 != null && pipe_moi_1.Id == mep1.Id)
                                        {
                                            var line = Line.CreateUnbound(main1_p, v_m_1 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(main1_p, p);

                                            var futher = inters_sub1.DistanceTo(p0_sub1) > inters_sub1.DistanceTo(p1_sub1) ? p0_sub1 : p1_sub1;

                                            ll(pipe1, futher, p);

                                            topReducer_1 = CreateFittingForMEPUtils.ctt(pipe1, pipe_moi_1);
                                        }
                                        else if (mep2 != null && pipe_moi_1.Id == mep2.Id)
                                        {
                                            var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                            var futher = inters_sub1.DistanceTo(p0_sub1) > inters_sub1.DistanceTo(p1_sub1) ? p0_sub1 : p1_sub1;

                                            ll(pipe1, futher, p);

                                            topReducer_1 = CreateFittingForMEPUtils.ctt(pipe1, pipe_moi_1);
                                        }
                                    }

                                    //2
                                    if (pipe_moi_2 != null)
                                    {
                                        if (mep1 != null && pipe_moi_2.Id == mep1.Id)
                                        {
                                            var line = Line.CreateUnbound(main1_p, v_m_1 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(main1_p, p);

                                            var futher = inters_sub2.DistanceTo(p0_sub2) > inters_sub2.DistanceTo(p1_sub2) ? p0_sub2 : p1_sub2;

                                            ll(pipe2, futher, p);

                                            topReducer_2 = CreateFittingForMEPUtils.ctt(pipe2, pipe_moi_2);
                                        }
                                        else if (mep2 != null && pipe_moi_2.Id == mep2.Id)
                                        {
                                            var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                            var p = line.Evaluate(d10, false);
                                            (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                            var futher = inters_sub2.DistanceTo(p0_sub2) > inters_sub2.DistanceTo(p1_sub2) ? p0_sub2 : p1_sub2;

                                            ll(pipe2, futher, p);

                                            topReducer_2 = CreateFittingForMEPUtils.ctt(pipe2, pipe_moi_2);
                                        }
                                    }

                                    if (ResultData.IsNippleCreating == true && ResultData.NippleFamily != null)
                                    {
                                        // Nipple Family
                                        HandlerConnectAccessoryNipple(pipe_moi_1, topReducer_1, fitting, ResultData.NippleFamily);
                                        HandlerConnectAccessoryNipple(pipe_moi_2, topReducer_2, fitting, ResultData.NippleFamily);
                                    }
                                }
                            }
                        }

                        reTrans.Commit();
                        continue;
                    }
                    catch (System.Exception ex)
                    {
                        reTrans.RollBack();
                        continue;
                    }
                }
            }
        }

        private bool DivideCase_2(Pipe main, Pipe pipeSub, bool twoPipes, out XYZ inters_main, out XYZ p0_sub, out XYZ p1_sub, out XYZ inters_sub)
        {
            inters_main = null;
            p0_sub = null;
            p1_sub = null;
            inters_sub = null;

            var curve_main = main.GetCurve();
            var curve_main_2d = Line.CreateBound(Common.PointTo2D(curve_main.GetEndPoint(0)), Common.PointTo2D(curve_main.GetEndPoint(1)));
            var curve_main_expand = Line.CreateUnbound(curve_main.GetEndPoint(0), (curve_main as Line).Direction * 100);

            var curve_sub = pipeSub.GetCurve();

            Curve curve2d = null;
            Curve curve3d = null;

            if (twoPipes)
            {
                //Expand
                double t = 10;
                var p0_ex1 = curve_sub.Evaluate(curve_sub.GetEndParameter(0) - t, false);
                var p1_ex2 = curve_sub.Evaluate(curve_sub.GetEndParameter(1) + t, false);
                curve3d = Line.CreateBound(p0_ex1, p1_ex2);

                var expand_2d = Line.CreateBound(Common.PointTo2D(p0_ex1), Common.PointTo2D(p1_ex2));

                curve2d = expand_2d;
            }
            else
            {
                curve2d = Line.CreateBound(Common.PointTo2D(curve_sub.GetEndPoint(0)), Common.PointTo2D(curve_sub.GetEndPoint(1)));
                curve3d = curve_sub;
            }

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curve_main_2d.Intersect(curve2d, CurveIntersectResultOption.Detailed);

    // Sử dụng thuộc tính .Result để kiểm tra trạng thái trùng nhau
    if (intersectResult.Result != SetComparisonResult.Overlap)
        return false;

    // Dùng hàm GetOverlaps() mới để lấy danh sách điểm giao
    var overlaps = intersectResult.GetOverlaps();
    if (overlaps == null || overlaps.Count == 0)
        return false;

    // Lấy tọa độ điểm đầu tiên bằng thuộc tính .Point của CurveOverlapPoint
    var pInter_2d = overlaps[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            IntersectionResultArray arr = new IntersectionResultArray();
            var result = curve_main_2d.Intersect(curve2d, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            var pInter_2d = arr.get_Item(0).XYZPoint;
#endif

            //Find 3d
            double temp = 200;

            var lineZ = Line.CreateBound(new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z - temp), new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z + temp));

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    intersectResult = lineZ.Intersect(curve_main, CurveIntersectResultOption.Detailed);

    if (intersectResult.Result != SetComparisonResult.Overlap)
        return false;

    // Sử dụng GetOverlaps() và lấy phần tử đầu tiên
    overlaps = intersectResult.GetOverlaps();
    if (overlaps == null || overlaps.Count == 0)
        return false;

    inters_main = overlaps[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve_main, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            inters_main = arr.get_Item(0).XYZPoint;
#endif

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
     intersectResult = lineZ.Intersect(curve3d, CurveIntersectResultOption.Detailed);

    if (intersectResult.Result != SetComparisonResult.Overlap)
        return false;

    // Sử dụng GetOverlaps() để lấy danh sách điểm giao mới
     overlaps = intersectResult.GetOverlaps();
    if (overlaps == null || overlaps.Count == 0)
        return false;

    // Lấy tọa độ điểm đầu tiên bằng thuộc tính .Point thay cho .XYZPoint
    inters_sub = overlaps[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve3d, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            inters_sub = arr.get_Item(0).XYZPoint;
#endif
            inters_main = curve_main_expand.Project(inters_sub).XYZPoint;
            if (twoPipes)
            {
                //Get father point
                int index = inters_sub.DistanceTo(curve_sub.GetEndPoint(0)) > inters_sub.DistanceTo(curve_sub.GetEndPoint(1)) ? 0 : 1;

                if (index == 0)
                {
                    p0_sub = curve_sub.GetEndPoint(index);
                    p1_sub = inters_sub;
                }
                else
                {
                    p0_sub = inters_sub;
                    p1_sub = curve_sub.GetEndPoint(index);
                }
            }
            else
            {
                p0_sub = curve_sub.GetEndPoint(0);
                p1_sub = curve_sub.GetEndPoint(1);
            }

            return true;
        }

        private bool DivideCase_2_Special(Pipe main, Pipe pipeSub, bool twoPipes, out XYZ inters_main, out XYZ p0_sub, out XYZ p1_sub, out XYZ inters_sub)
        {
            inters_main = null;
            p0_sub = null;
            p1_sub = null;
            inters_sub = null;

            var curve_main = main.GetCurve();
            var curve_main_2d = Line.CreateBound(Common.PointTo2D(curve_main.GetEndPoint(0)), Common.PointTo2D(curve_main.GetEndPoint(1)));
            var curve_main_expand = Line.CreateUnbound(curve_main.GetEndPoint(0), (curve_main as Line).Direction * 100);
            var curve_main_2d_expand = main.GetExpandFlattenCurve(500 * Common.mmToFT);

            var curve_sub = pipeSub.GetCurve();

            Curve curve2d = null;
            Curve curve3d = null;

            if (twoPipes)
            {
                //Expand
                double t = 10;
                var p0_ex1 = curve_sub.Evaluate(curve_sub.GetEndParameter(0) - t, false);
                var p1_ex2 = curve_sub.Evaluate(curve_sub.GetEndParameter(1) + t, false);
                curve3d = Line.CreateBound(p0_ex1, p1_ex2);

                var expand_2d = Line.CreateBound(Common.PointTo2D(p0_ex1), Common.PointTo2D(p1_ex2));

                curve2d = expand_2d;
            }
            else
            {
                curve2d = Line.CreateBound(Common.PointTo2D(curve_sub.GetEndPoint(0)), Common.PointTo2D(curve_sub.GetEndPoint(1)));
                curve3d = curve_sub;
            }

            //Check intersection
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curve_main_2d_expand.Intersect(curve2d, CurveIntersectResultOption.Detailed);

    if (intersectResult.Result != SetComparisonResult.Overlap)
        return false;

    // Sử dụng GetOverlaps() mới để lấy danh sách điểm giao
    var overlaps = intersectResult.GetOverlaps();
    if (overlaps == null || overlaps.Count == 0)
        return false;

    // Lấy tọa độ điểm đầu tiên bằng thuộc tính .Point
    var pInter_2d = overlaps[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            IntersectionResultArray arr = new IntersectionResultArray();
            var result = curve_main_2d_expand.Intersect(curve2d, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            var pInter_2d = arr.get_Item(0).XYZPoint;
#endif

            //Find 3d
            double temp = 200;

            var lineZ = Line.CreateBound(new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z - temp), new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z + temp));

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
     intersectResult = lineZ.Intersect(curve_main_expand, CurveIntersectResultOption.Detailed);

    if (intersectResult.Result != SetComparisonResult.Overlap)
        return false;

    // Sử dụng GetOverlaps() mới để lấy danh sách điểm giao
     overlaps = intersectResult.GetOverlaps();
    if (overlaps == null || overlaps.Count == 0)
        return false;

    // Lấy tọa độ điểm đầu tiên bằng thuộc tính .Point
    inters_main = overlaps[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve_main_expand, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            inters_main = arr.get_Item(0).XYZPoint;
#endif

            //Find 3d on two sub pipe
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
     intersectResult = lineZ.Intersect(curve3d, CurveIntersectResultOption.Detailed);

    if (intersectResult.Result != SetComparisonResult.Overlap)
        return false;

    // Sử dụng GetOverlaps() mới để lấy danh sách điểm giao
     overlaps = intersectResult.GetOverlaps();
    if (overlaps == null || overlaps.Count == 0)
        return false;

    // Lấy tọa độ điểm đầu tiên bằng thuộc tính .Point
    inters_sub = overlaps[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve3d, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            inters_sub = arr.get_Item(0).XYZPoint;
#endif
            inters_main = curve_main_expand.Project(inters_sub).XYZPoint;

            if (twoPipes)
            {
                //Get father point
                int index = inters_sub.DistanceTo(curve_sub.GetEndPoint(0)) > inters_sub.DistanceTo(curve_sub.GetEndPoint(1)) ? 0 : 1;

                if (index == 0)
                {
                    p0_sub = curve_sub.GetEndPoint(index);
                    p1_sub = inters_sub;
                }
                else
                {
                    p0_sub = inters_sub;
                    p1_sub = curve_sub.GetEndPoint(index);
                }
            }
            else
            {
                p0_sub = curve_sub.GetEndPoint(0);
                p1_sub = curve_sub.GetEndPoint(1);
            }

            return true;
        }

        private PreferredJunctionType gr(Pipe pipe)
        {
            var pipeType = pipe.PipeType as PipeType;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType;
        }

        public FamilyInstance ee(Pipe pipeMain, Pipe pipeCurrent, XYZ splitPoint, out Pipe main2)
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
            var c5 = Common.GetConnectorClosestTo(pipeCurrent, splitPoint);

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

        private void ll(Pipe pipe, XYZ pOn, XYZ pOther)
        {
            var p0 = pipe.GetCurve().GetEndPoint(0);
            var p1 = pipe.GetCurve().GetEndPoint(1);

            if (p0.DistanceTo(pOn) < p1.DistanceTo(pOn))
            {
                (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, pOther);
            }
            else
            {
                (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOther, pOn);
            }
        }

        private Pipe ee(ConnectorSet cs)
        {
            foreach (Connector c in cs)
            {
                Element e = c.Owner;

                if (null != e && e as Pipe != null)
                {
                    return e as Pipe;
                }
            }

            return null;
        }

        private bool g(double d1, double d2)
        {
            if (Math.Abs(d1 - d2) < 0.001)
                return true;

            return false;
        }

        /// <summary>
        /// Handler Connect Accessory Nipple
        /// </summary>
        /// <param name="addNipple"></param>
        /// <param name="primerPipe_1"></param>
        /// <param name="reducer_1"></param>
        /// <param name="tee"></param>
        /// <returns></returns>
        private bool HandlerConnectAccessoryNipple(Pipe primerPipe_1, FamilyInstance reducer_1, FamilyInstance tee, FamilySymbol familySymbol)
        {
            try
            {
                Tuple<Connector, Connector> cntsNipple_1 = PipeToPairConnector(primerPipe_1, reducer_1, tee);

                if (cntsNipple_1.Item1 == null || cntsNipple_1.Item2 == null)
                    return false;

                Line lineNipple = Line.CreateBound(cntsNipple_1.Item1.Origin, cntsNipple_1.Item2.Origin);

                XYZ center = MiddlePoint(cntsNipple_1.Item1.Origin, cntsNipple_1.Item2.Origin);

                FamilyInstance nipple = Global.UIDoc.Document.Create.NewFamilyInstance(center, familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                if (nipple != null)
                {
                    var paraDia = primerPipe_1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                    nipple.LookupParameter("Nominal Diameter").Set(paraDia);
                    Global.UIDoc.Document.Delete(primerPipe_1.Id);
                }

                RotateLine(Global.UIDoc.Document, nipple, lineNipple);

                List<Connector> temp_tee = GetConnectors(tee.MEPModel.ConnectorManager.Connectors, true);
                temp_tee.Remove(temp_tee.OrderBy(item => item.Origin.Z).First());

                List<Connector> temp1 = GetConnectors(nipple.MEPModel.ConnectorManager.Connectors, true);

                Connector cnt_tee_1 = temp_tee[0];
                Connector cnt_tee_2 = temp_tee[1];

                Connector cnt_nipple_1 = temp1[0];
                Connector cnt_nipple_2 = temp1[1];

                List<Tuple<Tuple<Connector, Connector>, double>> distanceConts = new List<Tuple<Tuple<Connector, Connector>, double>>();
                distanceConts.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_tee_1, cnt_nipple_1), cnt_tee_1.Origin.DistanceTo(cnt_nipple_1.Origin)));
                distanceConts.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_tee_1, cnt_nipple_2), cnt_tee_1.Origin.DistanceTo(cnt_nipple_2.Origin)));
                distanceConts.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_tee_2, cnt_nipple_1), cnt_tee_2.Origin.DistanceTo(cnt_nipple_1.Origin)));
                distanceConts.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_tee_2, cnt_nipple_2), cnt_tee_2.Origin.DistanceTo(cnt_nipple_2.Origin)));

                Tuple<Tuple<Connector, Connector>, double> minDistanceCnt = distanceConts.OrderBy(item => item.Item2).First();
                XYZ translation = minDistanceCnt.Item1.Item1.Origin - minDistanceCnt.Item1.Item2.Origin;
                ElementTransformUtils.MoveElement(Global.UIDoc.Document, nipple.Id, translation);

                List<Connector> temp_reducer = GetConnectors(reducer_1.MEPModel.ConnectorManager.Connectors, true);

                List<Connector> temp2 = GetConnectors(nipple.MEPModel.ConnectorManager.Connectors, true);

                Connector cnt_reducer_1 = temp_reducer[0];
                Connector cnt_reducer_2 = temp_reducer[1];

                cnt_nipple_1 = temp2[0];
                cnt_nipple_2 = temp2[1];

                List<Tuple<Tuple<Connector, Connector>, double>> distanceContsReducer = new List<Tuple<Tuple<Connector, Connector>, double>>();
                distanceContsReducer.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_nipple_1, cnt_reducer_1), cnt_nipple_1.Origin.DistanceTo(cnt_reducer_1.Origin)));
                distanceContsReducer.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_nipple_1, cnt_reducer_2), cnt_nipple_1.Origin.DistanceTo(cnt_reducer_2.Origin)));
                distanceContsReducer.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_nipple_2, cnt_reducer_1), cnt_nipple_2.Origin.DistanceTo(cnt_reducer_1.Origin)));
                distanceContsReducer.Add(new Tuple<Tuple<Connector, Connector>, double>(new Tuple<Connector, Connector>(cnt_nipple_2, cnt_reducer_2), cnt_nipple_2.Origin.DistanceTo(cnt_reducer_2.Origin)));

                Tuple<Tuple<Connector, Connector>, double> minDistanceCntReducer = distanceContsReducer.OrderBy(item => item.Item2).First();
                XYZ translation_2 = minDistanceCntReducer.Item1.Item1.Origin - minDistanceCntReducer.Item1.Item2.Origin;
                ElementTransformUtils.MoveElement(Global.UIDoc.Document, reducer_1.Id, translation_2);

                List<Connector> temp3 = GetConnectors(nipple.MEPModel.ConnectorManager.Connectors, true).Where(item => item.IsConnected == false).ToList();
                List<Connector> temp3_tee = GetConnectors(tee.MEPModel.ConnectorManager.Connectors, true).ToList();
                temp3_tee.Remove(temp3_tee.OrderBy(item => item.Origin.Z).FirstOrDefault());
                temp3_tee = temp3_tee.Where(item => item.IsConnected == false).ToList();
                List<Connector> temp3_reducer = GetConnectors(reducer_1.MEPModel.ConnectorManager.Connectors, true).Where(item => item.IsConnected == false).ToList();

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //// Error

                foreach (Connector cnt in temp3_reducer)
                {
                    bool flag = false;
                    foreach (Connector cnt1 in temp3)
                    {
                        if (Common.IsEqual(cnt1.Origin, cnt.Origin) && cnt.IsConnected == false)
                        {
                            cnt1.ConnectTo(cnt);
                            flag = true;
                        }
                    }
                    if (flag == true)
                        break;
                }

                foreach (Connector cnt in temp3_tee)
                {
                    bool flag = false;
                    foreach (Connector cnt1 in temp3)
                    {
                        if (Common.IsEqual(cnt1.Origin, cnt.Origin) && cnt.IsConnected == false)
                        {
                            cnt1.ConnectTo(cnt);
                            flag = true;
                        }
                    }
                    if (flag == true)
                        break;
                }

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
            catch (Exception)
            { }
            return false;
        }

        private Tuple<Connector, Connector> PipeToPairConnector(Pipe pipe, FamilyInstance reducer, FamilyInstance tee)
        {
            try
            {
                if (pipe == null || reducer == null || tee == null)
                    return new Tuple<Connector, Connector>(null, null);

                List<Connector> cntOfPipe = GetConnectors(pipe.ConnectorManager.Connectors, true);
                List<Connector> cntOfReducer = GetConnectors(reducer.MEPModel.ConnectorManager.Connectors, true);
                List<Connector> cntOfTee = GetConnectors(tee.MEPModel.ConnectorManager.Connectors, true);

                if (cntOfPipe.Count >= 2)
                {
                    Connector cntPipe_1 = cntOfPipe[0];
                    Connector cntPipe_2 = cntOfPipe[1];

                    Connector retCnt_1 = cntOfReducer.Where(item => Common.IsEqual(item.Origin, cntPipe_1.Origin) || Common.IsEqual(item.Origin, cntPipe_2.Origin)).FirstOrDefault();
                    Connector retCnt_2 = cntOfTee.Where(item => Common.IsEqual(item.Origin, cntPipe_1.Origin) || Common.IsEqual(item.Origin, cntPipe_2.Origin)).FirstOrDefault();

                    if (retCnt_1 != null && retCnt_2 != null)
                    {
                        return new Tuple<Connector, Connector>(retCnt_1, retCnt_2);
                    }
                }
            }
            catch (Exception)
            { }
            return new Tuple<Connector, Connector>(null, null);
        }

        private List<Connector> GetConnectors(ConnectorSet connectorSet, bool filter = false)
        {
            try
            {
                List<Connector> retVal = new List<Connector>();
                foreach (Connector connector in connectorSet)
                {
                    if (connector.ConnectorType != ConnectorType.End && filter == true)
                        continue;
                    retVal.Add(connector);
                }
                return retVal;
            }
            catch (Exception)
            { }
            return new List<Connector>();
        }

        private XYZ MiddlePoint(XYZ xYZ_1, XYZ xYZ_2) => new XYZ((xYZ_1.X + xYZ_2.X) * 0.5, (xYZ_1.Y + xYZ_2.Y) * 0.5, (xYZ_1.Z + xYZ_2.Z) * 0.5);

        private void RotateLine(Document doc, FamilyInstance wye, Line axisLine)
        {
            var lst = Common.ToList(wye.MEPModel.ConnectorManager.Connectors);

            Connector connector2 = lst[0];
            Connector connector3 = lst[1];

            Line rotateLine = Line.CreateBound(connector2.Origin, connector3.Origin);

            if (Common.IsParallel(axisLine.Direction, rotateLine.Direction))
                return;

            XYZ vector = rotateLine.Direction.CrossProduct(axisLine.Direction);
            XYZ intersection = GetUnBoundIntersection(rotateLine, axisLine);

            double angle = rotateLine.Direction.AngleTo(axisLine.Direction);

            Line line = Line.CreateUnbound(intersection, vector);

            ElementTransformUtils.RotateElement(doc, wye.Id, line, angle);
            doc.Regenerate();
        }

        private XYZ GetUnBoundIntersection(Line Line1, Line Line2)
        {
            if (Line1 != null && Line2 != null)
            {
                Curve ExtendedLine1 = Line.CreateUnbound(Line1.Origin, Line1.Direction);
                Curve ExtendedLine2 = Line.CreateUnbound(Line2.Origin, Line2.Direction);
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = ExtendedLine1.Intersect(ExtendedLine2, CurveIntersectResultOption.Detailed);

    // Lấy danh sách điểm giao bằng hàm GetOverlaps()
    var overlaps = intersectResult.GetOverlaps();
    if (overlaps != null && overlaps.Count > 0)
    {
        foreach (var overlapPoint in overlaps)
        {
            if (overlapPoint != null)
                return overlapPoint.Point; // Sử dụng .Point thay cho .XYZPoint
        }
    }
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                SetComparisonResult setComparisonResult = ExtendedLine1.Intersect(ExtendedLine2, out IntersectionResultArray resultArray);
                if (resultArray != null && resultArray.Size > 0)
                {
                    foreach (IntersectionResult result in resultArray)
                    {
                        if (result != null)
                            return result.XYZPoint;
                    }
                }
#endif
                else
                {
                    if (Line1.IsBound && Line2.IsBound)
                    {
                        if (Line1.GetEndPoint(0).IsAlmostEqualTo(Line2.GetEndPoint(0)) ||
                  Line1.GetEndPoint(0).IsAlmostEqualTo(Line2.GetEndPoint(1)))
                            return Line1.GetEndPoint(0);
                        else
                            if (Line1.GetEndPoint(1).IsAlmostEqualTo(Line2.GetEndPoint(0)) ||
                           Line1.GetEndPoint(1).IsAlmostEqualTo(Line2.GetEndPoint(1)))
                                return Line1.GetEndPoint(1);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Set Location Line
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="pOn"></param>
        /// <param name="pOther"></param>
        private void SetLocationLine(Pipe pipe, XYZ pOn, XYZ pOther)
        {
            var p0 = pipe.GetCurve().GetEndPoint(0);
            var p1 = pipe.GetCurve().GetEndPoint(1);

            if (p0.DistanceTo(pOn) < p1.DistanceTo(pOn))
            {
                (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOn, pOther);
            }
            else
            {
                (pipe.Location as LocationCurve).Curve = Line.CreateBound(pOther, pOn);
            }
        }

        /// <summary>
        /// Creat Transition Fitting
        /// </summary>
        /// <param name="mep1"></param>
        /// <param name="mep2"></param>
        /// <param name="checkDistance"></param>
        /// <returns></returns>
        private FamilyInstance CreatTransitionFitting(MEPCurve mep1, MEPCurve mep2, bool checkDistance = false)
        {
            if (mep1 == null || mep2 == null)
                return null;

            var same = iss(mep1, mep2);
            if (same == true)
                return null;

            try
            {
                List<Connector> connectors = Common.GetConnectionNearest(mep1, mep2);
                if (connectors != null && connectors.Count == 2)
                {
                    var c0 = connectors[0];
                    var c1 = connectors[1];

                    if (checkDistance == true)
                    {
                        if (c0.Origin.DistanceTo(c1.Origin) > 1000 * Common.mmToFT)
                            return null;
                    }

                    if ((mep1 as Autodesk.Revit.DB.Mechanical.Duct != null || mep1 as Pipe != null) && (c0.IsConnected == true || c1.IsConnected == true))
                    {
                        return null;
                    }

                    var transition = Global.UIDoc.Document.Create.NewTransitionFitting(connectors[0], connectors[1]);
                    return transition;
                }
            }
            catch (System.Exception ex)
            {
            }
            return null;
        }

        /// <summary>
        /// Is Same
        /// </summary>
        /// <param name="mep1"></param>
        /// <param name="mep2"></param>
        /// <returns></returns>
        private bool iss(MEPCurve mep1, MEPCurve mep2)
        {
            //Check same type
            var shape1 = Common.GetShape(mep1);
            var shape2 = Common.GetShape(mep2);

            if (shape1 != shape2)
                return false;

            //Phai song song
            if (Common.IsParallel(mep1, mep2) == false)
            {
                return false;
            }

            //Check size: Size khac nhau moi tao

            if (mep1 is Autodesk.Revit.DB.Mechanical.Duct || mep1 is CableTray)
            {
                bool width = false;
                var paraW1 = mep1.LookupParameter("Width");
                var paraW2 = mep2.LookupParameter("Width");
                if (paraW1 != null && paraW2 != null)
                {
                    var d1 = paraW1.AsDouble();
                    var d2 = paraW2.AsDouble();
                    if (d1 == d2)
                        width = true;
                }

                bool height = false;
                var paraH1 = mep1.LookupParameter("Height");
                var paraH2 = mep2.LookupParameter("Height");
                if (paraH1 != null && paraH2 != null)
                {
                    var d1 = paraH1.AsDouble();
                    var d2 = paraH2.AsDouble();
                    if (d1 == d2)
                        height = true;
                }

                if (width == true && height == true)
                    return true;
            }
            else
            {
                var paraD1 = mep1.LookupParameter("Diameter");
                var paraD2 = mep2.LookupParameter("Diameter");
                if (paraD1 != null && paraD2 != null)
                {
                    var d1 = paraD1.AsDouble();
                    var d2 = paraD2.AsDouble();
                    if (d1 == d2)
                        return true;
                }

                paraD1 = mep1.LookupParameter("Diameter(Trade Size)");
                paraD2 = mep2.LookupParameter("Diameter(Trade Size)");
                if (paraD1 != null && paraD2 != null)
                {
                    var d1 = paraD1.AsDouble();
                    var d2 = paraD2.AsDouble();
                    if (d1 == d2)
                        return true;
                }
            }

            return false;
        }

        private void InitializeSameElevation()
        {
            double diamterPipe = ResultData.PipeSize * Common.mmToFT;

            foreach (PairPipes pair in PairBranchPipes)
            {
                var pipe1 = pair.Pipe1;
                var pipe2 = pair.Pipe2;

                //if (App.m_ConnectBranch.DialogResultData.IsCheckedTeeTap)
                //{
                if (pipe1 != null && pipe2 != null)
                    continue;
                //}

                PipeType pipeType = Global.UIDoc.Document.GetElement(pipe1.GetTypeId()) as PipeType;

                SourcePipesDataSameElevation sourcePipesData = new SourcePipesDataSameElevation(MainPipeIds,
                                                                                                pipe1,
                                                                                                pipe2,
                                                                                                pipeType,
                                                                                                diamterPipe);
            }
        }

        /// <summary>
        /// Get builtin parameter value based on its storage type
        /// </summary>
        public dynamic GetBuiltInParameterValue(Element elem, BuiltInParameter paramId)
        {
            if (elem != null)
            {
                Parameter parameter = elem.get_Parameter(paramId);
                return GetParameterValue(parameter);
            }
            return null;
        }

        /// <summary>
        /// get parameter value based on its storage type
        /// </summary>
        private dynamic GetParameterValue(Parameter parameter)
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
        private dynamic GetParameterValueByName(Element elem, string paramName)
        {
            if (elem != null)
            {
                Parameter parameter = elem.LookupParameter(paramName);
                return GetParameterValue(parameter);
            }
            return null;
        }

        /// <summary>
        /// Process Main Pipe
        /// </summary>
        /// <param name="mainPipes"></param>
        /// <param name="processPipe"></param>
        /// <param name="flagSplit"></param>
        /// <param name="processIntPnt"></param>
        /// <returns></returns>

        private Pipe ProcessMainPipe(List<Pipe> mainPipes, Pipe processPipe, out bool flagSplit, out XYZ processIntPnt)
        {
            flagSplit = false;
            processIntPnt = null;
            try
            {
                Line flattenCurve_processPipe = Line.CreateUnbound(processPipe.GetFlattenCurve().GetEndPoint(0), processPipe.GetFlattenCurve().Direction);
                XYZ originPnt = processPipe.GetFlattenCurve().GetEndPoint(0);

                List<Tuple<Pipe, XYZ>> validMainPipes_Expand = new List<Tuple<Pipe, XYZ>>();
                List<Tuple<Pipe, XYZ>> validMainPipes_Real = new List<Tuple<Pipe, XYZ>>();

                Dictionary<Pipe, double> dictMainPipes_Expand = new Dictionary<Pipe, double>();
                Dictionary<Pipe, double> dictMainPipes_Real = new Dictionary<Pipe, double>();
                foreach (var mainPipe in mainPipes)
                {
                    XYZ intPnt;
                    Line flattenCurve_mainPipe = mainPipe.GetFlattenCurve();
                    if (RealityIntersect(flattenCurve_processPipe, flattenCurve_mainPipe, out intPnt))
                    {
                        validMainPipes_Real.Add(new Tuple<Pipe, XYZ>(mainPipe, intPnt));
                        dictMainPipes_Real.Add(mainPipe, originPnt.DistanceTo(intPnt));
                    }

                    Line flattenCurve_mainPipe_Expand = mainPipe.GetExpandFlattenCurve(700 * Common.mmToFT);
                    if (RealityIntersect(flattenCurve_processPipe, flattenCurve_mainPipe_Expand, out intPnt))
                    {
                        validMainPipes_Expand.Add(new Tuple<Pipe, XYZ>(mainPipe, intPnt));
                        dictMainPipes_Expand.Add(mainPipe, originPnt.DistanceTo(intPnt));
                    }
                }

                if (validMainPipes_Real.Count > 0)
                {
                    double minDist = dictMainPipes_Real.Min(item => item.Value);

                    var validDic = dictMainPipes_Real.FirstOrDefault(item => item.Value == minDist);

                    Pipe pipeNear = null;

                    foreach (var validDataMainPipe in validMainPipes_Real)
                    {
                        if (validDataMainPipe.Item1.Id != validDic.Key.Id)
                            continue;

                        var curve = (validDataMainPipe.Item1.Location as LocationCurve).Curve;

                        if (curve is Line == false)
                            continue;

                        var d = (curve as Line).Direction;

                        if (Common.IsParallel(d, XYZ.BasisZ, 0))
                            continue;

                        var project = curve.Project(validDataMainPipe.Item2);
                        if (project == null)
                            continue;

                        var p = project.XYZPoint;
                        processIntPnt = p;

                        if (p.DistanceTo(curve.GetEndPoint(0)) != 0 && p.DistanceTo(curve.GetEndPoint(1)) != 0)
                        {
                            flagSplit = true;

                            return validDataMainPipe.Item1;
                        }
                        else
                        {
                            pipeNear = validDataMainPipe.Item1;
                        }
                    }
                    return pipeNear;
                }
                else
                {
                    if (validMainPipes_Expand.Count <= 0)
                        return null;

                    double minDist = dictMainPipes_Expand.Min(item => item.Value);

                    var validDic = dictMainPipes_Expand.FirstOrDefault(item => item.Value == minDist);

                    Pipe pipeNear = null;

                    foreach (var validDataMainPipe in validMainPipes_Expand)
                    {
                        if (validDataMainPipe.Item1.Id != validDic.Key.Id)
                            continue;

                        var curve = (validDataMainPipe.Item1.Location as LocationCurve).Curve;

                        if (curve is Line == false)
                            continue;

                        var d = (curve as Line).Direction;

                        if (Common.IsParallel(d, XYZ.BasisZ, 0))
                            continue;

                        var project = curve.Project(validDataMainPipe.Item2);
                        if (project == null)
                            continue;

                        var p = project.XYZPoint;
                        processIntPnt = p;

                        if (p.DistanceTo(curve.GetEndPoint(0)) != 0 && p.DistanceTo(curve.GetEndPoint(1)) != 0)
                        {
                            flagSplit = true;

                            return validDataMainPipe.Item1;
                        }
                        else
                        {
                            pipeNear = validDataMainPipe.Item1;
                        }
                    }
                    return pipeNear;
                }
            }
            catch (Exception)
            { }
            return null;
        }

        public void ProcessStartSidePipe(Pipe pipe, out Pipe pipe2, XYZ pOn, bool flagSplit, out bool isDauOngChinh)
        {
            isDauOngChinh = false;
            var curve = (pipe.Location as LocationCurve).Curve;

            //Create plane
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            //Check co phai dau cuu hoa o gan dau cua ong ko : check trong pham vi 1m - 400mm
            double kc_mm = 400 /*1000*/;

            if ((double)GetParameterValueByName(pipe, "Diameter") / Common.mmToFT >= 90)
                kc_mm = 1100;
            else if ((double)GetParameterValueByName(pipe, "Diameter") / Common.mmToFT >= 50)
                kc_mm = 600;

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
                if (IsIntersect(p0) == false && !CheckPipeIsEnd(pipe, pOn))
                {
                    isDauOng = true;
                    far = 1;
                }
            }
            else if (d2 < km_ft)
            {
                if (IsIntersect(p1) == false && !CheckPipeIsEnd(pipe, pOn))
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
            else
            {
                if (isDauOng == false)
                { }
                else if (far != -1)
                {
                    isDauOngChinh = true;
                    if (far == 1)
                        (pipe1.Location as LocationCurve).Curve = Line.CreateBound(pOn, p1);
                    else
                        (pipe1.Location as LocationCurve).Curve = Line.CreateBound(p0, pOn);
                }
            }
        }

        public bool CheckPipeIsEnd(Pipe pipe, XYZ point)
        {
            var con = Common.GetConnectorClosestTo(pipe, point);

            return con.IsConnected;
        }

        public void ProcessStartSidePipeCaseElbow(Pipe pipe, out Pipe pipe2, XYZ pOn, bool flagSplit = true, bool flagCreateLastElbow = false)
        {
            var curve = (pipe.Location as LocationCurve).Curve;

            //Create plane
            var p0 = curve.GetEndPoint(0);
            var p1 = curve.GetEndPoint(1);

            //Check co phai dau cuu hoa o gan dau cua ong ko : check trong pham vi 1m - 400mm
            double kc_mm = 500 /*1000*/;

            if ((double)GetParameterValueByName(pipe, "Diameter") / Common.mmToFT >= 90)
                kc_mm = 1100;
            else if ((double)GetParameterValueByName(pipe, "Diameter") / Common.mmToFT >= 50)
                kc_mm = 600;

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
                if (IsIntersect(p0) == false)
                {
                    isDauOng = true;
                    far = 1;
                }
            }
            else if (d2 < km_ft)
            {
                if (IsIntersect(p1) == false)
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
                else if (flagCreateLastElbow == false)
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

        public void SplitPipe(Pipe pipeOrigin, XYZ splitPoint, out Pipe pipe1, out Pipe pipe2)
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

        public bool IsIntersect(XYZ point)
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
        private PreferredJunctionType GetPreferredJunctionType(Pipe pipe)
        {
            var pipeType = pipe.PipeType as PipeType;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType;
        }

        private bool RealityIntersect(Line mainLine, Line checkLine, out XYZ intersectPnt)
        {
            intersectPnt = null;
            try
            {
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = mainLine.Intersect(checkLine, CurveIntersectResultOption.Detailed);

    if (intersectResult.Result == SetComparisonResult.Overlap)
    {
        // Dùng hàm GetOverlaps() mới để lấy danh sách điểm giao
        var overlaps = intersectResult.GetOverlaps();
        if (overlaps != null && overlaps.Count > 0)
        {
            intersectPnt = overlaps[0].Point; // Lấy điểm bằng .Point
            return true;
        }
    }
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray intsRetArr = new IntersectionResultArray();
                var intsRet = mainLine.Intersect(checkLine, out intsRetArr);
                if (intsRet == SetComparisonResult.Overlap)
                {
                    intersectPnt = intsRetArr.get_Item(0).XYZPoint;
                    return true;
                }
#endif
            }
            catch (Exception)
            { }
            return false;
        }

        /// <summary>
        /// Create Tap
        /// </summary>
        /// <param name="mepCurveSplit1"></param>
        /// <param name="mepCurveSplit2"></param>
        /// <returns></returns>
        public static FamilyInstance CreateTap(MEPCurve mepCurveSplit1, MEPCurve mepCurveSplit2)
        {
            FamilyInstance retVal = null;
            try
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
                    return retVal;

                var d1 = inter1.XYZPoint.DistanceTo(p10);
                var d2 = inter2.XYZPoint.DistanceTo(p11);

                if (d1 < d2)
                {
                    var con = Common.GetConnectorClosestTo(mepCurveSplit2, p10);
                    retVal = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                }
                else
                {
                    var con = Common.GetConnectorClosestTo(mepCurveSplit2, p11);
                    retVal = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                }

                return retVal;
            }
            catch (System.Exception ex)
            {
                return retVal;
            }
        }
    }

    public class SourcePipesDataSameElevation
    {

        private Pipe m_firstPipe = null;
        private Pipe m_secondPipe = null;
        private Pipe m_mainPipe = null;
        private PipeType m_pipeTypeProcess = null;
        private double m_diameter = double.MinValue;
        private List<ElementId> m_mainPipes = new List<ElementId>();
        private Pipe m_verticalPipe = null;

        private XYZ m_intsMain1_pnt = null;
        private XYZ m_sub1_pnt0 = null;
        private XYZ m_sub1_pnt1 = null;
        private XYZ m_sub1Ints_pnt = null;

        private XYZ m_intsMain2_pnt = null;
        private XYZ m_sub2_pnt0 = null;
        private XYZ m_sub2_pnt1 = null;
        private XYZ m_sub2Ints_pnt = null;

        private bool m_flagCutPipe = false;

        private FamilyInstance m_bottomTee = null;
        private FamilyInstance m_bottomElbow = null;

        private FamilyInstance m_topTee = null;

        private FamilyInstance m_topReducer1 = null;
        private FamilyInstance m_topReducer2 = null;

        private bool m_flagTopElbowConnect = false;
        private FamilyInstance m_topElbow = null;

        private bool m_twoPipes = false;



        public Pipe FirstPipe { get => m_firstPipe; set => m_firstPipe = value; }
        public Pipe SecondPipe { get => m_secondPipe; set => m_secondPipe = value; }
        public Pipe MainPipe { get => m_mainPipe; set => m_mainPipe = value; }
        public PipeType PipeTypeProcess { get => m_pipeTypeProcess; set => m_pipeTypeProcess = value; }
        public List<ElementId> MainPipes { get => m_mainPipes; set => m_mainPipes = value; }
        public double Diameter { get => m_diameter; set => m_diameter = value; }

        public Pipe VerticalPipe { get => m_verticalPipe; set => m_verticalPipe = value; }



        public SourcePipesDataSameElevation(List<ElementId> mainPipes, Pipe firstPipe, Pipe secondPipe, PipeType pipeType, double pipeDiameter)
        {
            if (mainPipes != null)
                MainPipes = mainPipes;

            FirstPipe = firstPipe;
            SecondPipe = secondPipe;
            PipeTypeProcess = pipeType;
            Diameter = pipeDiameter;

            if (SecondPipe != null)
                m_twoPipes = true;

            Initialize();
        }



        private void Initialize()
        {
            using (Transaction reTrans = new Transaction(Global.UIDoc.Document, "STEP"))
            {
                try
                {
                    reTrans.Start();

                    if (BeforeProcess() == false)
                    {
                        reTrans.RollBack();
                        return;
                    }

                    //if (HandleVerticalPipe() == false)
                    //{
                    //    reTrans.RollBack();
                    //    return;
                    //}

                    //if (HandlerBottom() == false)
                    //{
                    //    reTrans.RollBack();
                    //    return;
                    //}

                    if (HandlerTop() == false)
                    {
                        reTrans.RollBack();
                        return;
                    }

                    if (HandlerFinal() == false)
                    {
                        reTrans.RollBack();
                        return;
                    }

                    reTrans.Commit();
                }
                catch (Exception)
                {
                    reTrans.RollBack();
                }
            }
        }

        private bool BeforeProcess()
        {
            try
            {
                // Check has two pipe valid
                bool flagTwoPipe = SecondPipe != null ? true : false;

                // Get main pipe
                MainPipe = null;
                if (MainPipes == null || MainPipes.Count == 0)
                    return false;

                bool flag = false;
                foreach (ElementId eId in m_mainPipes)
                {
                    Pipe pipeLoop = Global.UIDoc.Document.GetElement(eId) as Pipe;
                    if (pipeLoop == null)
                        continue;

                    SourceRotatePipe sourceRotatePipe = new SourceRotatePipe(pipeLoop, FirstPipe, SecondPipe);
                    if (sourceRotatePipe.IsValid)
                    {
                        if (sourceRotatePipe.FlagExtendPipe)
                        {
                            (pipeLoop.Location as LocationCurve).Curve = sourceRotatePipe.NewCurveMainPipe;
                        }
                        if (sourceRotatePipe.FlagCreateElbow)
                        {
                            (FirstPipe.Location as LocationCurve).Curve = sourceRotatePipe.NewCurveFirstPipe;
                            m_flagTopElbowConnect = true;
                            flag = true;
                            MainPipe = pipeLoop;
                            break;
                        }
                        else
                        {
                            if (HandlerDivide(pipeLoop, FirstPipe, flagTwoPipe, out m_intsMain1_pnt, out m_sub1_pnt0, out m_sub1_pnt1, out m_sub1Ints_pnt) == true)
                            {
                                MainPipe = pipeLoop;
                                break;
                            }
                        }
                    }
                }

                // Rotate sub pipe by main pipe
                //HandleBranchPipePerpendiculerMainPipe();

                if (HandlerDivide(MainPipe, FirstPipe, flagTwoPipe, out m_intsMain1_pnt, out m_sub1_pnt0, out m_sub1_pnt1, out m_sub1Ints_pnt) == true)
                {
                    if (flag == false)
                    {
                        if (SecondPipe == null)
                        {
                            var temp_pipe1 = FirstPipe;

                            FirstPipe = temp_pipe1;

                            //if (!App.m_ConnectBranch.DialogResultData.IsCheckedTeeTap)
                            //{
                            //    var temp_pipe2_id = PlumbingUtils.BreakCurve(Global.UIDoc.Document, temp_pipe1.Id, m_sub1Ints_pnt);
                            //    var temp_pipe2 = Global.UIDoc.Document.GetElement(temp_pipe2_id) as Pipe;
                            //    SecondPipe = temp_pipe2;
                            //}

                            if (SecondPipe != null)
                            {
                                HandlerDivide(MainPipe, FirstPipe, true, out m_intsMain1_pnt, out m_sub1_pnt0, out m_sub1_pnt1, out m_sub1Ints_pnt);
                            }
                        }

                        if (SecondPipe != null)
                        {
                            if (HandlerDivide(MainPipe, SecondPipe, true, out m_intsMain2_pnt, out m_sub2_pnt0, out m_sub2_pnt1, out m_sub2Ints_pnt) == false)
                            {
                                return false;
                            }

                            if (m_intsMain1_pnt.DistanceTo(m_intsMain2_pnt) > 0.001)
                            {
                                return false;
                            }

                            if (m_sub1Ints_pnt.DistanceTo(m_sub2Ints_pnt) > 0.001)
                            {
                                return false;
                            }

                            m_flagCutPipe = true;
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            { }

            return false;
        }

        private bool HandleVerticalPipe()
        {
            try
            {
                if (m_flagTopElbowConnect == true)
                {
                    Diameter = (double)GetParameterValueByName(FirstPipe, "Diameter");
                }

                VerticalPipe = Common.Clone(FirstPipe) as Pipe;
                VerticalPipe.PipeType = PipeTypeProcess;
                VerticalPipe.LookupParameter("Diameter").Set(Diameter);
                (VerticalPipe.Location as LocationCurve).Curve = Line.CreateBound(m_intsMain1_pnt, m_sub1Ints_pnt);

                return true;
            }
            catch (Exception)
            { }
            return false;
        }

        private bool HandlerFinal()
        {
            try
            {
                double middleElevation = double.MinValue;
                if (MainPipe != null)
                    middleElevation = GetBuiltInParameterValue(MainPipe, BuiltInParameter.RBS_OFFSET_PARAM)
                                    != null ? (double)GetBuiltInParameterValue(MainPipe, BuiltInParameter.RBS_OFFSET_PARAM) : double.MinValue;

                if (middleElevation != double.MinValue)
                {
                    if (FirstPipe != null)
                    {
                        //SetBuiltinParameterValue(FirstPipe, BuiltInParameter.RBS_OFFSET_PARAM, middleElevation);
                        MovePipe(FirstPipe, m_intsMain1_pnt, m_sub1Ints_pnt);
                    }

                    if (SecondPipe != null)
                    {
                        //SetBuiltinParameterValue(SecondPipe, BuiltInParameter.RBS_OFFSET_PARAM, middleElevation);
                        MovePipe(SecondPipe, m_intsMain2_pnt, m_sub2Ints_pnt);
                    }

                    if (GetPreferredJunctionType(MainPipe) == PreferredJunctionType.Tee)
                    {
                        //if (App.m_ConnectBranch.DialogResultData.IsCheckedTeeTap)
                        //{
                        if (MainPipe as Pipe != null && FirstPipe as Pipe != null && m_intsMain1_pnt != null)
                        {
                            Pipe main2 = null;

                            m_bottomTee = CreateTeeFitting(MainPipe as Pipe, FirstPipe as Pipe, m_intsMain1_pnt, out main2);
                            MainPipes.Add(main2.Id);
                        }

                        if (MainPipe as Pipe != null && SecondPipe as Pipe != null && m_intsMain1_pnt != null)
                        {
                            Pipe main2 = null;

                            m_bottomTee = CreateTeeFitting(MainPipe as Pipe, SecondPipe as Pipe, m_intsMain1_pnt, out main2);
                            MainPipes.Add(main2.Id);
                        }
                        //}
                        //else
                        //{
                        //    Pipe main2 = null;
                        //    CreateCrossFitting(MainPipe, FirstPipe, SecondPipe, m_intsMain1_pnt, out main2);
                        //    MainPipes.Add(main2.Id);
                        //}
                    }
                    else if (GetPreferredJunctionType(MainPipe) == PreferredJunctionType.Tap)
                    {
                        bool flag = true;
                        if (FirstPipe != null)
                        {
                            flag = se(MainPipe, FirstPipe);
                        }

                        if (flag == false)
                            return false;

                        if (SecondPipe != null)
                        {
                            flag = se(MainPipe, SecondPipe);
                        }

                        if (flag == false)
                            return false;
                    }

                    if (VerticalPipe != null)
                        Global.UIDoc.Document.Delete(VerticalPipe.Id);

                    return true;
                }
            }
            catch (Exception)
            { }
            return false;
        }

        private void MovePipe(Pipe processPipe, XYZ finalPoint, XYZ beginPoint)
        {
            XYZ tranMove = finalPoint - beginPoint;
            //Move element
            ElementTransformUtils.MoveElement(Global.UIDoc.Document, processPipe.Id, tranMove);
        }

        public int IsLeftMath(XYZ Startpoint, XYZ Endpoint, XYZ P)
        {
            double Ans = ((Endpoint.X - Startpoint.X) * (P.Y - Startpoint.Y) -
              (P.X - Startpoint.X) * (Endpoint.Y - Startpoint.Y));
            if (Math.Abs(Ans) < 1.0e-8)
            { return 0; } //P is on the line
            else
            {
                if (Ans > 0)
                { return 1; } //P is left of the line (CW)
                else
                { return -1; } //P is right of the line (CCW)
            }
        }

        private bool HandlerBottom()
        {
            try
            {
                if (MainPipe as Pipe != null && VerticalPipe as Pipe != null && m_intsMain1_pnt != null)
                {
                    //Pipe main2 = null;

                    //var curve = (MainPipe.Location as LocationCurve).Curve;

                    //var p0 = curve.GetEndPoint(0);
                    //var p1 = curve.GetEndPoint(1);

                    //var pipeTempMain1 = MainPipe;

                    //var line1 = Line.CreateBound(p0, m_intsMain1_pnt);
                    //(pipeTempMain1.Location as LocationCurve).Curve = line1;

                    //var newPlace = new XYZ(0, 0, 0);
                    //var elemIds = ElementTransformUtils.CopyElement(
                    //   Global.UIDoc.Document, pipeTempMain1.Id, newPlace);

                    //main2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

                    //var line2 = Line.CreateBound(m_intsMain1_pnt, p1);
                    //(main2.Location as LocationCurve).Curve = line2;

                    //MainPipes.Add(main2.Id);

                    return true;
                }
            }
            catch (Exception)
            { }
            return false;
        }

        private bool HandlerTop()
        {
            try
            {
                // If Diameter vertical pipe = Diameter main pipe
                if (FirstPipe != null)
                {
                    var firstLine = Line.CreateBound(m_sub1_pnt0, m_sub1_pnt1);
                    (FirstPipe.Location as LocationCurve).Curve = firstLine;
                }

                if (SecondPipe != null)
                {
                    var secondLine = Line.CreateBound(m_sub2_pnt0, m_sub2_pnt1);
                    (SecondPipe.Location as LocationCurve).Curve = secondLine;
                }

                return true;
            }
            catch (Exception)
            { }
            return false;
        }

        /// <summary>
        /// Handler Divide
        /// </summary>
        /// <param name="mainPipe"></param>
        /// <param name="subPipe"></param>
        /// <param name="flagTwoPipe"></param>
        /// <param name="intsMain_pnt"></param>
        /// <param name="sub_pnt0"></param>
        /// <param name="sub_pnt1"></param>
        /// <param name="subInts_pnt"></param>
        /// <returns></returns>
        private bool HandlerDivide(Pipe mainPipe, Pipe subPipe, bool flagTwoPipe, out XYZ intsMain_pnt, out XYZ sub_pnt0, out XYZ sub_pnt1, out XYZ subInts_pnt)
        {
            Global.UIDoc.Document.Regenerate();
            intsMain_pnt = null;
            sub_pnt0 = null;
            sub_pnt1 = null;
            subInts_pnt = null;

            var curve_main = mainPipe.GetCurve();
            var curve_main_2d = Line.CreateBound(Common.PointTo2D(curve_main.GetEndPoint(0)), Common.PointTo2D(curve_main.GetEndPoint(1)));

            var curve_sub = subPipe.GetCurve();

            Curve curve2d = null;
            Curve curve3d = null;

            if (flagTwoPipe)
            {
                //Expand
                double t = 10;
                var p0_ex1 = curve_sub.Evaluate(curve_sub.GetEndParameter(0) - t, false);
                var p1_ex2 = curve_sub.Evaluate(curve_sub.GetEndParameter(1) + t, false);
                curve3d = Line.CreateBound(p0_ex1, p1_ex2);

                var expand_2d = Line.CreateBound(Common.PointTo2D(p0_ex1), Common.PointTo2D(p1_ex2));

                curve2d = expand_2d;
            }
            else
            {
                curve2d = Line.CreateBound(Common.PointTo2D(curve_sub.GetEndPoint(0)), Common.PointTo2D(curve_sub.GetEndPoint(1)));
                curve3d = curve_sub;
            }

            //Check intersection
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---

    // 1. Tìm giao điểm 2D
    var intersectResult1 = curve_main_2d.Intersect(curve2d, CurveIntersectResultOption.Detailed);
    if (intersectResult1.Result != SetComparisonResult.Overlap)
        return false;

    var overlaps1 = intersectResult1.GetOverlaps();
    if (overlaps1 == null || overlaps1.Count == 0)
        return false;

    var pInter_2d = overlaps1[0].Point;

    // --- Dựng trục Z 3D ---
    double temp = 200;
    var lineZ = Line.CreateBound(new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z - temp), new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z + temp));

    // 2. Tìm giao điểm 3D trên ống chính (curve_main)
    var intersectResult2 = lineZ.Intersect(curve_main, CurveIntersectResultOption.Detailed);
    if (intersectResult2.Result != SetComparisonResult.Overlap)
        return false;

    var overlaps2 = intersectResult2.GetOverlaps();
    if (overlaps2 == null || overlaps2.Count == 0)
        return false;

    intsMain_pnt = overlaps2[0].Point;

    // 3. Tìm giao điểm 3D trên hai ống phụ (curve3d)
    var intersectResult3 = lineZ.Intersect(curve3d, CurveIntersectResultOption.Detailed);
    if (intersectResult3.Result != SetComparisonResult.Overlap)
        return false;

    var overlaps3 = intersectResult3.GetOverlaps();
    if (overlaps3 == null || overlaps3.Count == 0)
        return false;

    subInts_pnt = overlaps3[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            IntersectionResultArray arr = new IntersectionResultArray();
            var result = curve_main_2d.Intersect(curve2d, out arr);

            if (result != SetComparisonResult.Overlap)
                return false;

            var pInter_2d = arr.get_Item(0).XYZPoint;

            //Find 3d
            double temp = 200;
            var lineZ = Line.CreateBound(new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z - temp), new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z + temp));

            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve_main, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            intsMain_pnt = arr.get_Item(0).XYZPoint;

            //Find 3d on two sub pipe
            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve3d, out arr);
            if (result != SetComparisonResult.Overlap)
                return false;

            subInts_pnt = arr.get_Item(0).XYZPoint;
#endif

            if (flagTwoPipe)
            {
                //Get father point
                int index = subInts_pnt.DistanceTo(curve_sub.GetEndPoint(0)) > subInts_pnt.DistanceTo(curve_sub.GetEndPoint(1)) ? 0 : 1;

                if (index == 0)
                {
                    sub_pnt0 = curve_sub.GetEndPoint(index);
                    sub_pnt1 = subInts_pnt;
                }
                else
                {
                    sub_pnt0 = subInts_pnt;
                    sub_pnt1 = curve_sub.GetEndPoint(index);
                }
            }
            else
            {
                sub_pnt0 = curve_sub.GetEndPoint(0);
                sub_pnt1 = curve_sub.GetEndPoint(1);
            }

            return true;
        }

        /// <summary>
        /// Get Preferred Junction Type
        /// </summary>
        /// <param name="pipe"></param>
        /// <returns></returns>
        private PreferredJunctionType GetPreferredJunctionType(Pipe pipe)
        {
            var pipeType = pipe.PipeType as PipeType;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType;
        }

        /// <summary>
        /// Create Tee Fitting
        /// </summary>
        /// <param name="pipeMain"></param>
        /// <param name="pipeCurrent"></param>
        /// <param name="splitPoint"></param>
        /// <param name="main2"></param>
        /// <returns></returns>
        private FamilyInstance CreateTeeFitting(Pipe pipeMain, Pipe pipeCurrent, XYZ splitPoint, out Pipe main2)
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
            var c3 = GetConnectorClosestTo(pipeTempMain1, splitPoint);
            var c4 = GetConnectorClosestTo(main2, splitPoint);
            var c5 = GetConnectorClosestTo(pipeCurrent, splitPoint);
            FamilyInstance retVal = null;

            using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
            {
                try
                {
                    reSubTrans.Start();

                    var fitting = Global.UIDoc.Document.Create.NewTeeFitting(c3, c4, c5);

                    retVal = fitting;

                    reSubTrans.Commit();
                }
                catch (System.Exception ex)
                {
                    reSubTrans.RollBack();
                }
            }

            return retVal;
        }

        /// <summary>
        /// Create Tee Fitting
        /// </summary>
        /// <param name="pipeMain"></param>
        /// <param name="pipeCurrent"></param>
        /// <param name="splitPoint"></param>
        /// <param name="main2"></param>
        /// <returns></returns>
        private FamilyInstance CreateCrossFitting(Pipe pipeMain, Pipe pipe1, Pipe pipe2, XYZ splitPoint, out Pipe main2)
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
            var c3 = GetConnectorClosestTo(pipeTempMain1, splitPoint);
            var c4 = GetConnectorClosestTo(main2, splitPoint);
            var c5 = GetConnectorClosestTo(pipe1, splitPoint);
            var c6 = GetConnectorClosestTo(pipe2, splitPoint);
            FamilyInstance retVal = null;

            using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
            {
                try
                {
                    reSubTrans.Start();

                    var fitting = Global.UIDoc.Document.Create.NewCrossFitting(c3, c4, c5, c6);

                    retVal = fitting;

                    reSubTrans.Commit();
                }
                catch (System.Exception ex)
                {
                    reSubTrans.RollBack();
                }
            }

            return retVal;
        }

        /// <summary>
        /// Create Elbow Fitting
        /// </summary>
        /// <param name="pipeMain"></param>
        /// <param name="pipeCurrent"></param>
        /// <param name="splitPoint"></param>
        /// <param name="main2"></param>
        /// <returns></returns>
        private FamilyInstance CreateElbowFitting(Pipe pipeMain, Pipe pipeCurrent, XYZ splitPoint, out Pipe main2)
        {
            main2 = null;
            try
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
                var c3 = GetConnectorClosestTo(pipeTempMain1, splitPoint);
                var c4 = GetConnectorClosestTo(main2, splitPoint);
                var c5 = GetConnectorClosestTo(pipeCurrent, splitPoint);

                try
                {
                    FamilyInstance retFitting = null;
                    if (main2.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < pipeTempMain1.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble())
                    {
                        retFitting = Global.UIDoc.Document.Create.NewElbowFitting(c3, c5);

                        Global.UIDoc.Document.Delete(main2.Id);
                        //main2 = null;
                    }
                    else
                    {
                        retFitting = Global.UIDoc.Document.Create.NewElbowFitting(c4, c5);
                        Global.UIDoc.Document.Delete(pipeTempMain1.Id);
                    }
                    return retFitting;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            { }
            return null;
        }

        /// <summary>
        /// Get Connector Closest To
        /// </summary>
        /// <param name="e"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Connector GetConnectorClosestTo(Element e,
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
        private Connector GetConnectorClosestTo(ConnectorSet connectors,
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
        private ConnectorManager GetConnectorManager(Element e)
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

        /// <summary>
        /// s e
        /// </summary>
        /// <param name="mepCurveSplit1"></param>
        /// <param name="mepCurveSplit2"></param>
        /// <returns></returns>
        public bool se(MEPCurve mepCurveSplit1, MEPCurve mepCurveSplit2)
        {
            try
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
                    var elbow = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                }
                else
                {
                    var con = GetConnectorClosestTo(mepCurveSplit2, p11);
                    var elbow = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Create Tee
        /// </summary>
        /// <param name="c3"></param>
        /// <param name="c4"></param>
        /// <param name="c5"></param>
        /// <returns></returns>
        public FamilyInstance CreatTee(Connector c3, Connector c4, Connector c5)
        {
            if (c3 == null || c4 == null || c5 == null)
                return null;
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

        /// <summary>
        /// Create Tee
        /// </summary>
        /// <param name="c3"></param>
        /// <param name="c4"></param>
        /// <param name="c5"></param>
        /// <returns></returns>
        public FamilyInstance CreateElbow(Connector c3, Connector c4)
        {
            if (c3 == null || c4 == null)
                return null;
            try
            {
                var fitting = Global.UIDoc.Document.Create.NewElbowFitting(c3, c4);

                return fitting;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// g
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        private bool g(double d1, double d2)
        {
            if (Math.Abs(d1 - d2) < 0.001)
                return true;

            return false;
        }

        private List<Connector> GetConnectors(ConnectorSet connectorSet, bool filter = false)
        {
            try
            {
                List<Connector> retVal = new List<Connector>();
                foreach (Connector connector in connectorSet)
                {
                    if (connector.ConnectorType != ConnectorType.End && filter == true)
                        continue;
                    retVal.Add(connector);
                }
                return retVal;
            }
            catch (Exception)
            { }
            return new List<Connector>();
        }

        private XYZ MiddlePoint(XYZ xYZ_1, XYZ xYZ_2) => new XYZ((xYZ_1.X + xYZ_2.X) * 0.5, (xYZ_1.Y + xYZ_2.Y) * 0.5, (xYZ_1.Z + xYZ_2.Z) * 0.5);

        private void RotateLine(Document doc, FamilyInstance wye, Line axisLine)
        {
            var lst = Common.ToList(wye.MEPModel.ConnectorManager.Connectors);

            Connector connector2 = lst[0];
            Connector connector3 = lst[1];

            Line rotateLine = Line.CreateBound(connector2.Origin, connector3.Origin);

            if (Common.IsParallel(axisLine.Direction, rotateLine.Direction))
                return;

            XYZ vector = rotateLine.Direction.CrossProduct(axisLine.Direction);
            XYZ intersection = GetUnBoundIntersection(rotateLine, axisLine);

            double angle = rotateLine.Direction.AngleTo(axisLine.Direction);

            Line line = Line.CreateUnbound(intersection, vector);

            ElementTransformUtils.RotateElement(doc, wye.Id, line, angle);
            doc.Regenerate();
        }

        private XYZ GetUnBoundIntersection(Line Line1, Line Line2)
        {
            if (Line1 != null && Line2 != null)
            {
                Curve ExtendedLine1 = Line.CreateUnbound(Line1.Origin, Line1.Direction);
                Curve ExtendedLine2 = Line.CreateUnbound(Line2.Origin, Line2.Direction);
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = ExtendedLine1.Intersect(ExtendedLine2, CurveIntersectResultOption.Detailed);

    // Lấy danh sách điểm giao bằng hàm GetOverlaps()
    var overlaps = intersectResult.GetOverlaps();
    if (overlaps != null && overlaps.Count > 0)
    {
        foreach (var overlap in overlaps)
        {
            if (overlap != null)
                return overlap.Point; // Lấy tọa độ bằng thuộc tính .Point
        }
    }
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                SetComparisonResult setComparisonResult = ExtendedLine1.Intersect(ExtendedLine2, out IntersectionResultArray resultArray);
                if (resultArray != null && resultArray.Size > 0)
                {
                    foreach (IntersectionResult result in resultArray)
                    {
                        if (result != null)
                            return result.XYZPoint;
                    }
                }
#endif
                else
                {
                    if (Line1.IsBound && Line2.IsBound)
                    {
                        if (Line1.GetEndPoint(0).IsAlmostEqualTo(Line2.GetEndPoint(0)) ||
                  Line1.GetEndPoint(0).IsAlmostEqualTo(Line2.GetEndPoint(1)))
                            return Line1.GetEndPoint(0);
                        else
                            if (Line1.GetEndPoint(1).IsAlmostEqualTo(Line2.GetEndPoint(0)) ||
                           Line1.GetEndPoint(1).IsAlmostEqualTo(Line2.GetEndPoint(1)))
                                return Line1.GetEndPoint(1);
                    }
                }
            }
            return null;
        }

        private bool SetBuiltinParameterValue(Element elem, BuiltInParameter paramId, object value)
        {
            Parameter param = elem.get_Parameter(paramId);
            return SetParameterValue(param, value);
        }

        /// <summary>
        /// Set value to parameter based on its storage type
        /// </summary>
        private bool SetParameterValue(Parameter param, object value)
        {
            if (param != null
                && !param.IsReadOnly
                && value != null)
            {
                try
                {
                    switch (param.StorageType)
                    {
                        case StorageType.Integer:
                            param.Set((int)value);
                            break;

                        case StorageType.Double:
                            param.Set((double)value);
                            break;

                        case StorageType.String:
                            param.Set((string)value);
                            break;

                        case StorageType.ElementId:
                            param.Set((ElementId)value);
                            break;
                    }
                    return true;
                }
                catch (Exception) { }
            }
            return false;
        }

        /// <summary>
        /// Get builtin parameter value based on its storage type
        /// </summary>
        private dynamic GetBuiltInParameterValue(Element elem, BuiltInParameter paramId)
        {
            if (elem != null)
            {
                Parameter parameter = elem.get_Parameter(paramId);
                return GetParameterValue(parameter);
            }
            return null;
        }

        /// <summary>
        /// get parameter value based on its storage type
        /// </summary>
        private dynamic GetParameterValue(Parameter parameter)
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
        private dynamic GetParameterValueByName(Element elem, string paramName)
        {
            if (elem != null)
            {
                Parameter parameter = elem.LookupParameter(paramName);
                return GetParameterValue(parameter);
            }
            return null;
        }

        /// <summary>
        /// All Components On Pipe Truss
        /// </summary>
        /// <param name="document"></param>
        /// <param name="elementMainId"></param>
        /// <param name="addFirst"></param>
        /// <param name="rmvElement"></param>
        /// <returns></returns>
        private List<Element> AllComponentsOnPipeTruss(Autodesk.Revit.DB.Document document, ElementId elementMainId, bool addFirst = false, List<Element> rmvElement = null)
        {
            List<Element> components = new List<Element>();
            Element eleMain = document.GetElement(elementMainId);
            List<Element> rmvElement_1 = null;
            if (addFirst == true)
            {
                rmvElement_1 = new List<Element> { eleMain };
                components.Add(eleMain);
            }
            else
            {
                rmvElement_1 = new List<Element>(rmvElement);
            }
            if (eleMain != null)
            {
                List<Connector> connectors = new List<Connector>();
                if (eleMain is MEPCurve processMEPCurve && processMEPCurve.ConnectorManager != null)
                {
                    foreach (Connector connector in processMEPCurve.ConnectorManager.Connectors)
                    {
                        if (connector.ConnectorType != ConnectorType.End)
                            continue;
                        connectors.Add(connector);
                    }
                }
                else if (eleMain is FamilyInstance fmlIns && fmlIns.MEPModel.ConnectorManager != null)
                {
                    foreach (Connector connector in fmlIns.MEPModel.ConnectorManager.Connectors)
                    {
                        if (connector.ConnectorType != ConnectorType.End && connector.IsConnected == true)
                            continue;
                        connectors.Add(connector);
                    }
                }

                if (connectors.Count > 0)
                {
                    foreach (Connector cnt1 in connectors)
                    {
                        foreach (Connector cnt2 in cnt1.AllRefs)
                        {
                            Element eleCheck = cnt2.Owner;
                            if (null != eleCheck && (eleCheck is MEPCurve || eleCheck is FamilyInstance))
                            {
                                if (rmvElement_1 != null && rmvElement_1.Any(item => item.Id == eleCheck.Id))
                                    continue;
                                else
                                {
                                    components.Add(eleCheck);
                                    List<Element> rmvElement_2 = new List<Element>(rmvElement_1);
                                    rmvElement_2.Add(eleCheck);

                                    components.AddRange(AllComponentsOnPipeTruss(document, eleCheck.Id, false, rmvElement_2));
                                }
                            }
                        }
                    }
                }
            }

            return components;
        }

    }

    public class SourceRotatePipe
    {
        public Pipe MainPipe { get; set; }
        public Line CurveMainPipe { get; set; }
        public Line CurveMainPipe_2d { get; set; }
        public Pipe FirstPipe { get; set; }
        public Line CurveFirstPipe { get; set; }
        public Line CurveFirstPipe_2d { get; set; }

        public Line NewCurveFirstPipe { get; set; }

        public Line NewCurveFirstPipe_2d { get; set; }

        public Line NewCurveMainPipe { get; set; }
        public Line NewCurveMainPipe_2d { get; set; }

        public Pipe SecondPipe { get; set; }
        public Line CurveSecondPipe { get; set; }
        public Line CurveSecondPipe_2d { get; set; }
        public bool IsValid { get; set; }
        public bool FlagCreateTopTee { get; set; }
        public bool FlagCreateElbow { get; set; }

        public XYZ IntsMain { get; set; }
        public XYZ SubMain { get; set; }

        public bool FlagExtendPipe { get; set; }

        public SourceRotatePipe(Pipe mainPipe, Pipe firstPipe, Pipe secondPipe)
        {
            IsValid = false;
            if (mainPipe != null && firstPipe != null)
            {
                MainPipe = mainPipe;
                FirstPipe = firstPipe;
                SecondPipe = secondPipe;
                if (Initialize())
                    IsValid = true;
            }
        }

        public bool Initialize()
        {
            try
            {
                CurveMainPipe = MainPipe.GetCurve() as Line;
                CurveMainPipe_2d = Line.CreateBound(ToPoint2D(CurveMainPipe.GetEndPoint(0)), ToPoint2D(CurveMainPipe.GetEndPoint(1)));

                CurveFirstPipe = FirstPipe.GetCurve() as Line;
                CurveFirstPipe_2d = Line.CreateBound(ToPoint2D(CurveFirstPipe.GetEndPoint(0)), ToPoint2D(CurveFirstPipe.GetEndPoint(1)));

                if (SecondPipe != null && SecondPipe.IsValidObject)
                {
                    CurveSecondPipe = SecondPipe.GetCurve() as Line;
                    CurveSecondPipe_2d = Line.CreateBound(ToPoint2D(CurveSecondPipe.GetEndPoint(0)), ToPoint2D(CurveSecondPipe.GetEndPoint(1)));
                }

                if (FirstPipe != null && SecondPipe != null)
                {
                    if (RealityIntersect(CurveMainPipe_2d, CurveFirstPipe_2d) || RealityIntersect(CurveMainPipe_2d, CurveSecondPipe_2d))
                        return true;
                    else if (!RealityIntersect(CurveMainPipe_2d, CurveFirstPipe_2d) && !RealityIntersect(CurveMainPipe_2d, CurveSecondPipe_2d))
                    {
                        XYZ expanDirectionn = (ToPoint2D(CurveFirstPipe_2d.GetEndPoint(1)) - ToPoint2D(CurveFirstPipe_2d.GetEndPoint(0))).Normalize();

                        var expandFirstPipe = Line.CreateBound(ToPoint2D(CurveFirstPipe_2d.GetEndPoint(0)) - expanDirectionn * 550 * mmToFT, ToPoint2D(CurveFirstPipe_2d.GetEndPoint(1) + expanDirectionn * 550 * mmToFT));
                        if (RealityIntersect(expandFirstPipe, CurveMainPipe_2d))
                            return true;

                        List<Connector> cntMainPipe = new List<Connector>();
                        foreach (Connector connector in MainPipe.ConnectorManager.Connectors)
                        {
                            if (connector.ConnectorType != ConnectorType.End)
                                continue;
                            cntMainPipe.Add(connector);
                        }

                        if (cntMainPipe.Any(item => item.IsConnected == true) == false)
                        {
                            return true;
                        }
                        if (cntMainPipe.Count >= 2)
                        {
                            var directionExpandPipe_2d = ToPoint2D(cntMainPipe[1].Origin) - ToPoint2D(cntMainPipe[0].Origin);

                            if (cntMainPipe[0].IsConnected == false)
                            {
                                NewCurveMainPipe_2d = Line.CreateBound(ToPoint2D(cntMainPipe[0].Origin - directionExpandPipe_2d * 100 * mmToFT), ToPoint2D(cntMainPipe[1].Origin));
                            }
                            else if (cntMainPipe[1].IsConnected == false)
                            {
                                NewCurveMainPipe_2d = Line.CreateBound(ToPoint2D(cntMainPipe[0].Origin), ToPoint2D(cntMainPipe[1].Origin + directionExpandPipe_2d * 100 * mmToFT));
                            }
                            else
                                return true;

                            if (RealityIntersect(NewCurveMainPipe_2d, expandFirstPipe))
                            {
                                FlagExtendPipe = true;
                                var directionExpandPipe = cntMainPipe[1].Origin - cntMainPipe[0].Origin;
                                if (cntMainPipe[0].IsConnected == false)
                                {
                                    NewCurveMainPipe = Line.CreateBound(cntMainPipe[0].Origin - directionExpandPipe * 100 * mmToFT, cntMainPipe[1].Origin);
                                }
                                else if (cntMainPipe[1].IsConnected == false)
                                {
                                    NewCurveMainPipe = Line.CreateBound(cntMainPipe[0].Origin, cntMainPipe[1].Origin + directionExpandPipe * 100 * mmToFT);
                                }
                                return true;
                            }
                        }

                        return true;
                    }
                }
                else if (FirstPipe != null && SecondPipe == null)
                {
                    if (RealityIntersect(CurveMainPipe_2d, CurveFirstPipe_2d))
                        return true;
                    else
                    {
                        return SpecialProcesFirstPipe();
                    }
                }
            }
            catch (Exception)
            { }
            return false;
        }

        private bool RealityIntersect(Line mainLine, Line checkLine)
        {
            try
            {
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = mainLine.Intersect(checkLine, CurveIntersectResultOption.Detailed);

    if (intersectResult.Result == SetComparisonResult.Overlap)
        return true;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray intsRetArr = new IntersectionResultArray();
                var intsRet = mainLine.Intersect(checkLine, out intsRetArr);
                if (intsRet == SetComparisonResult.Overlap)
                    return true;
#endif
            }
            catch (Exception)
            { }
            return false;
        }

        public double mmToFT = 0.0032808399;

        private bool SpecialProcesFirstPipe()
        {
            try
            {
                if (FirstPipe == null)
                    return false;

                // Check direction firte pipe valid
                // Get connector
                List<Connector> cntOfPipe = new List<Connector>();
                foreach (Connector connector in FirstPipe.ConnectorManager.Connectors)
                {
                    if (connector.ConnectorType != ConnectorType.End)
                        continue;
                    if (connector.IsConnected == true)
                        continue;
                    cntOfPipe.Add(connector);
                }

                if (cntOfPipe.Count <= 0)
                    return false;

                XYZ directionExpand = (ToPoint2D(CurveFirstPipe.GetEndPoint(1)) - ToPoint2D(CurveFirstPipe.GetEndPoint(0))).Normalize();
                XYZ validContOrg = cntOfPipe[0].Origin;

                if (cntOfPipe.Count == 2 && CurveMainPipe_2d.Distance(ToPoint2D(cntOfPipe[0].Origin)) > CurveMainPipe_2d.Distance(ToPoint2D(cntOfPipe[1].Origin)))
                {
                    validContOrg = cntOfPipe[1].Origin;
                }
                bool flag = false;

                if (IsEqual(CurveFirstPipe.GetEndPoint(0), validContOrg))
                {
                    NewCurveFirstPipe_2d = Line.CreateBound(ToPoint2D(CurveFirstPipe.GetEndPoint(0)) - directionExpand * 550 * mmToFT, ToPoint2D(CurveFirstPipe.GetEndPoint(1)));
                }
                else
                {
                    NewCurveFirstPipe_2d = Line.CreateBound(ToPoint2D(CurveFirstPipe.GetEndPoint(0)), ToPoint2D(CurveFirstPipe.GetEndPoint(1)) + directionExpand * 550 * mmToFT);

                    flag = true;
                }

                if (RealityIntersect(CurveMainPipe_2d, NewCurveFirstPipe_2d))
                {
                    // 1. Lấy điểm giao 2D ban đầu
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult2D = CurveMainPipe_2d.Intersect(NewCurveFirstPipe_2d, CurveIntersectResultOption.Detailed);
    var intsPnt_2d = intersectResult2D.GetOverlaps()[0].Point;
#else
                    // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                    IntersectionResultArray intsRetArr = new IntersectionResultArray();
                    var intsRet = CurveMainPipe_2d.Intersect(NewCurveFirstPipe_2d, out intsRetArr);
                    var intsPnt_2d = intsRetArr.get_Item(0).XYZPoint;
#endif

                    // 2. Project lấy điểm mới (Hàm Project không bị thay đổi trong API mới)
                    var temp_Direction = Line.CreateUnbound(CurveFirstPipe.GetEndPoint(0), CurveFirstPipe.GetEndPoint(1) - CurveFirstPipe.GetEndPoint(0));
                    IntersectionResult porjectLine = temp_Direction.Project(intsPnt_2d);
                    XYZ newPnt = porjectLine.XYZPoint;

                    // 3. Dựng trục Z 3D (Dùng chung cho cả 2 trường hợp)
                    double temp = 200;
                    var lineZ = Line.CreateBound(new XYZ(intsPnt_2d.X, intsPnt_2d.Y, intsPnt_2d.Z - temp), new XYZ(intsPnt_2d.X, intsPnt_2d.Y, intsPnt_2d.Z + temp));

                    // 4. Bắt đầu tìm giao điểm 3D
#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var resZDir = lineZ.Intersect(temp_Direction, CurveIntersectResultOption.Detailed);
    if (resZDir.Result != SetComparisonResult.Overlap) return false;
    newPnt = resZDir.GetOverlaps()[0].Point;

    // Phân nhánh logic dựa theo flag
    if (flag)
        NewCurveFirstPipe = Line.CreateBound(CurveFirstPipe.GetEndPoint(0), newPnt);
    else
        NewCurveFirstPipe = Line.CreateBound(newPnt, CurveFirstPipe.GetEndPoint(1));

    var resMain = lineZ.Intersect(CurveMainPipe, CurveIntersectResultOption.Detailed);
    if (resMain.Result != SetComparisonResult.Overlap) return false;
    IntsMain = resMain.GetOverlaps()[0].Point;

    var resSub = lineZ.Intersect(NewCurveFirstPipe, CurveIntersectResultOption.Detailed);
    if (resSub.Result != SetComparisonResult.Overlap) return false;
    SubMain = resSub.GetOverlaps()[0].Point;

#else
                    // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                    var arr = new IntersectionResultArray();
                    var result = lineZ.Intersect(temp_Direction, out arr);
                    if (result != SetComparisonResult.Overlap) return false;
                    newPnt = arr.get_Item(0).XYZPoint;

                    // Phân nhánh logic dựa theo flag
                    if (flag)
                        NewCurveFirstPipe = Line.CreateBound(CurveFirstPipe.GetEndPoint(0), newPnt);
                    else
                        NewCurveFirstPipe = Line.CreateBound(newPnt, CurveFirstPipe.GetEndPoint(1));

                    arr = new IntersectionResultArray();
                    result = lineZ.Intersect(CurveMainPipe, out arr);
                    if (result != SetComparisonResult.Overlap) return false;
                    IntsMain = arr.get_Item(0).XYZPoint;

                    arr = new IntersectionResultArray();
                    result = lineZ.Intersect(NewCurveFirstPipe, out arr);
                    if (result != SetComparisonResult.Overlap) return false;
                    SubMain = arr.get_Item(0).XYZPoint;
#endif

                    FlagCreateElbow = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            { }
            return false;
        }

        private XYZ ToPoint2D(XYZ point3d, double z = 0)
        {
            return new XYZ(point3d.X, point3d.Y, z);
        }

        public bool IsEqual(double first, double second, double tolerance = 10e-5)
        {
            double result = Math.Abs(first - second);
            return result < tolerance;
        }

        public bool IsEqual(XYZ first, XYZ second)
        {
            return IsEqual(first.X, second.X)
                && IsEqual(first.Y, second.Y)
                && IsEqual(first.Z, second.Z);
        }
    }
}
