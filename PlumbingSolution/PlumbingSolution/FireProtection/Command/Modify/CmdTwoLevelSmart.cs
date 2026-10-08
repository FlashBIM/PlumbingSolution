using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PlumbingSolution.FireProtection.Command.Modify;
using ParameterUtils = PlumbingSolution.FireProtection.Ultis.ParameterUtils;

namespace PlumbingSolution.FireProtection.Command.Fire.DiritConnectPipes.Service_B
{
    public class CmdTwoLevelSmart
    {
        private static List<ElementId> m_mainPipeIds = new List<ElementId>();

        public const string SCHEMA_IS_TEE_NAME = "IsTee";
        public static Guid SCHEMA_IS_TEE_GUID = Guid.Parse("5576072d-9d27-45de-834f-ad5b3f6437df");

        public const string SCHEMA_IS_ELEMENT_ORIGIN_NAME = "IsElementOrigin";
        public static Guid SCHEMA_IS_ELEMENT_ORIGIN_GUID = Guid.Parse("49695f15-fe99-41a2-8c7b-1f95e787b268");

        /// <summary>
        /// Select multies pipe
        /// </summary>
        /// <returns></returns>
        public static List<Pipe> SelectPipes(List<ElementId> lstElementIds = null)
        {
            List<Pipe> retVal = new List<Pipe>();
            try
            {
                var pickedObjs = Global.UIDoc.Selection.PickObjects(ObjectType.Element, new MEPCurveFilter(typeof(Pipe), lstElementIds), "SELECT PIPES : ");

                foreach (Reference pickedObj in pickedObjs)
                {
                    var pipe = Global.UIDoc.Document.GetElement(pickedObj) as Pipe;

                    if (pipe != null)
                        retVal.Add(pipe);
                }
            }
            catch (System.Exception ex)
            {
            }
            return retVal;
        }

        public static bool IsHasSchema(Element element, Guid guid, string name)
        {
            object valueEntity = StorageUtility.GetValue(element, Schema.Lookup(guid), name, typeof(string));
            if (valueEntity != null && !string.IsNullOrEmpty(valueEntity as string))
                return true;
            return false;
        }

        /// <summary>
        /// Pair pipe
        /// </summary>
        /// <param name="pipes"></param>
        /// <returns></returns>
        public static List<PairPipes> PairPipe(List<Pipe> pipes)
        {
            double temp = 100;

            List<PairPipes> pairs = new List<PairPipes>();

            foreach (Pipe pipe in pipes)
            {
                var curve_sub = pipe.GetCurve();

                var line = Line.CreateBound(curve_sub.GetEndPoint(0), curve_sub.GetEndPoint(1));

                //Expand
                var p0_ex = line.Evaluate(line.GetEndParameter(0) - temp, false);
                var p1_ex = line.Evaluate(line.GetEndParameter(1) + temp, false);

                var expand = Line.CreateBound(p0_ex, p1_ex);

                foreach (Pipe pipe2 in pipes)
                {
                    if (pipe.Id == pipe2.Id)
                        continue;

                    var curve_sub2 = pipe2.GetCurve();
                    SetComparisonResult result;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curve_sub2.Intersect(expand, CurveIntersectResultOption.Detailed);
    result = intersectResult.Result;
#else
                    // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                    IntersectionResultArray arr = new IntersectionResultArray();
                    result = curve_sub2.Intersect(expand, out arr);
#endif

                    if (result == SetComparisonResult.Equal)
                    {
                        //Dong tam
                        var find = pairs.Find(item => IsEqualPipe(item, pipe, pipe2));
                        if (find == null)
                        {
                            var newPair = new PairPipes(pipe, pipe2);
                            pairs.Add(newPair);
                        }
                    }
                }
            }

            List<ElementId> allAddeds1 = (from PairPipes pair in pairs
                                          where pair.Pipe1 != null
                                          select pair.Pipe1.Id).ToList();

            List<ElementId> allAddeds2 = (from PairPipes pair in pairs
                                          where pair.Pipe2 != null
                                          select pair.Pipe2.Id).ToList();

            List<ElementId> rest = (from Pipe pipe in pipes
                                    where allAddeds1.Contains(pipe.Id) == false && allAddeds2.Contains(pipe.Id) == false
                                    select pipe.Id).ToList();

            foreach (ElementId id in rest)
            {
                var pipe = Global.UIDoc.Document.GetElement(id) as Pipe;
                if (pipe == null)
                    continue;

                var newPair = new PairPipes(pipe, null);
                pairs.Add(newPair);
            }

            return pairs;
        }

        public static List<DuoBranchPipe> PairingPipesPlus(List<Pipe> pipes)
        {
            List<InforPie> inforPipes = new List<InforPie>();
            pipes.ForEach(item => inforPipes.Add(new InforPie(item)));

            List<DuoBranchPipe> pairs = new List<DuoBranchPipe>();

            try
            {
                foreach (InforPie inforPipe1 in inforPipes)
                {
                    var curve_sub = inforPipe1.CurveSourcePipe;

                    var expand = inforPipe1.CurveSourcePipe_Extend;

                    foreach (InforPie inforPipe2 in inforPipes)
                    {
                        if (inforPipe1.SourcePipe.Id == inforPipe2.SourcePipe.Id)
                            continue;

                        var curve_sub2 = inforPipe2.CurveSourcePipe_Extend;
                        SetComparisonResult result;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = curve_sub2.Intersect(expand, CurveIntersectResultOption.Detailed);
    result = intersectResult.Result;
#else
                        // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                        IntersectionResultArray arr = new IntersectionResultArray();
                        result = curve_sub2.Intersect(expand, out arr);
#endif

                        double distance = Common.GetPointProjectOnLine(expand, curve_sub2.Origin).DistanceTo(curve_sub2.Origin);

                        if (result != SetComparisonResult.Disjoint || Common.IsEqual(distance, 0, 10 / 304.8))
                        {
                            //Dong tam
                            var find = pairs.Find(item => IsEqualPipe(item, inforPipe1, inforPipe2));
                            if (find == null)
                            {
                                var newPair = new DuoBranchPipe(inforPipe1, inforPipe2);
                                pairs.Add(newPair);
                            }
                        }
                    }
                }

                List<ElementId> allAddeds1 = (from DuoBranchPipe pair in pairs
                                              where pair.FirstPipe.SourcePipe != null
                                              select pair.FirstPipe.SourcePipe.Id).ToList();

                List<ElementId> allAddeds2 = (from DuoBranchPipe pair in pairs
                                              where pair.SecondPipe.SourcePipe != null
                                              select pair.SecondPipe.SourcePipe.Id).ToList();

                List<ElementId> rest = (from Pipe pipe in pipes
                                        where allAddeds1.Contains(pipe.Id) == false && allAddeds2.Contains(pipe.Id) == false
                                        select pipe.Id).ToList();

                foreach (ElementId id in rest)
                {
                    var pipe = Global.UIDoc.Document.GetElement(id) as Pipe;
                    if (pipe == null)
                        continue;

                    var newPair = new DuoBranchPipe(new InforPie(pipe), null);
                    pairs.Add(newPair);
                }
            }
            catch (Exception)
            {
            }

            return pairs;
        }

        /// <summary>
        /// Is Equal Pipe
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="pipe1"></param>
        /// <param name="pipe2"></param>
        /// <returns></returns>
        private static bool IsEqualPipe(PairPipes pair, Pipe pipe1, Pipe pipe2)
        {
            if (pair.Pipe1.Id == pipe1.Id && pair.Pipe2.Id == pipe2.Id)
                return true;
            if (pair.Pipe1.Id == pipe2.Id && pair.Pipe2.Id == pipe1.Id)
                return true;

            return false;
        }

        private static bool IsEqualPipe(DuoBranchPipe pair, InforPie pipe1, InforPie pipe2)
        {
            if (pair.FirstPipe.SourcePipe.Id == pipe1.SourcePipe.Id && pair.SecondPipe.SourcePipe.Id == pipe2.SourcePipe.Id)
                return true;
            if (pair.FirstPipe.SourcePipe.Id == pipe2.SourcePipe.Id && pair.SecondPipe.SourcePipe.Id == pipe1.SourcePipe.Id)
                return true;

            return false;
        }

        /// <summary>
        /// Process
        /// </summary>
        /// <returns></returns>
        public static Result Process()
        {
            //if (!LicenseGate.IsToolAllowed(this))
            //    return Result.Cancelled;

            try
            {
                if (App.m_ConnectBranchForm != null && App.m_ConnectBranchForm.IsDisposed == false)
                {
                    App.m_ConnectBranchForm.Hide();
                }

                m_mainPipeIds.Clear();

                // Select main pipes
                List<Pipe> mainPipes = SelectPipes();
                if (mainPipes == null || mainPipes.Count <= 0)
                    return Result.Cancelled;

                List<InforPie> inforMainPipes = new List<InforPie>();
                mainPipes.ForEach(item => inforMainPipes.Add(new InforPie(item)));

                // Select branch pipes
                List<Pipe> branchPipes = SelectPipes(mainPipes.Select(x => x.Id).ToList());
                if (branchPipes == null || branchPipes.Count <= 0)
                    return Result.Cancelled;

                // Filter branch pipes
                branchPipes = branchPipes.Where(item => (mainPipes.Find(item_1 => item_1.Id == item.Id) == null)).Select(item => item).ToList();

                // Pair pipe
                List<DuoBranchPipe> pairPies_1 = PairingPipesPlus(branchPipes);
                if (pairPies_1 == null || pairPies_1.Count() <= 0)
                    return Result.Cancelled;

                List<PairPipes> pairPies = PairPipe(branchPipes);
                if (pairPies == null || pairPies.Count() <= 0)
                    return Result.Cancelled;

                TransactionGroup reTransGrp = new TransactionGroup(Global.UIDoc.Document, "TWO_LEVEL_SMART_COMMAND");
                try
                {
                    reTransGrp.Start();

                    //if (App.m_ConnectBranch.DialogResultData.IsElevationDiffirence)
                    {
                        if (GetPreferredJunctionType(mainPipes[0]) == PreferredJunctionType.Tap /*&& App.m_ConnectBranch.DialogResultData.IsCheckedTeeTap*/)
                        {
                            ProcessWithTap(inforMainPipes, pairPies_1, App.m_ConnectBranchForm.DialogResultData);
                        }
                        else if (GetPreferredJunctionType(mainPipes[0]) == PreferredJunctionType.Tee /*&& App.m_ConnectBranch.DialogResultData.IsCheckedTeeTap*/)
                        {
                            ProcessWithTee(inforMainPipes, pairPies_1, App.m_ConnectBranchForm.DialogResultData);
                        }
                        //else if (GetPreferredJunctionType(mainPipes[0]) == PreferredJunctionType.Tap && App.m_2LevelSmartForm.DialogResultData.IsCheckedTeeTap)
                        //{
                        //}
                        else
                        {
                            // Get ElementId main pipes
                            List<ElementId> mainPipeIds = mainPipes.Where(item => item.Id != ElementId.InvalidElementId).Select(item => item.Id).ToList();
                            m_mainPipeIds.AddRange(mainPipeIds);

                            HandleProcessTwoLevelSmartCommand handleProcess = new HandleProcessTwoLevelSmartCommand(m_mainPipeIds, pairPies, App.m_ConnectBranchForm.DialogResultData);
                        }
                    }
                    //else
                    //{
                    //    List<ElementId> mainPipeIds = mainPipes.Where(item => item.Id != ElementId.InvalidElementId).Select(item => item.Id).ToList();
                    //    m_mainPipeIds.AddRange(mainPipeIds);

                    //    HandleProcessTwoLevelSmartCommand handleProcess = new HandleProcessTwoLevelSmartCommand(m_mainPipeIds, pairPies, App.m_ConnectBranch.DialogResultData);
                    //}

                    reTransGrp.Assimilate();
                }
                catch (Exception)
                {
                    reTransGrp.RollBack();
                }
            }
            catch (Exception)
            { }
            finally
            {
                if (App.m_ConnectBranchForm != null && App.m_ConnectBranchForm.IsDisposed == false)
                {
                    App.m_ConnectBranchForm.Show(App.hWndRevit);
                }
                DisplayService.SetFocus(new HandleRef(null, App.m_ConnectBranchForm.Handle));
            }
            return Result.Cancelled;
        }

        //private static bool ProcessSameElevation(List<InforPie> inforMainPipes, List<DuoBranchPipe> pairPies, TwoLevelSmartDialogData dialogResultData)
        //{
        //    try
        //    {
        //        List<InforPie> process_InforMains = new List<InforPie>(inforMainPipes);
        //        List<DuoBranchPipe> process_duoBranchPipes = new List<DuoBranchPipe>(pairPies);

        //        List<SourceMainPipe> sourceMainPipes = new List<SourceMainPipe>();

        //        foreach (var inforMain in process_InforMains)
        //        {
        //            var flatten_mainCurve = inforMain.CurveSourcePipe_Flatten;
        //            var flatten_mainCurve_Extend = inforMain.CuvreSourcePipe_FlattenExtend;
        //            SourceMainPipe sourceMain = new SourceMainPipe(inforMain);

        //            foreach (var duoBranchs in process_duoBranchPipes)
        //            {
        //                try
        //                {
        //                    if (duoBranchs.SecondPipe != null)
        //                    {
        //                        if (RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
        //                        {
        //                            DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
        //                            {
        //                                FlattenIntersectPoint = flattenIntPnt1
        //                            };

        //                            sourceMain.Branches.Add(duoBranchPipe);
        //                        }

        //                        if (RealityIntersect(flatten_mainCurve_Extend, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt2))
        //                        {
        //                            DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
        //                            {
        //                                FlattenIntersectPoint = flattenIntPnt2
        //                            };
        //                            sourceMain.Branches_Special.Add(duoBranchPipe);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
        //                        {
        //                            DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
        //                            {
        //                                FlattenIntersectPoint = flattenIntPnt1
        //                            };

        //                            sourceMain.Branches.Add(duoBranchPipe);
        //                            sourceMain.Branches_Special.Add(duoBranchPipe);
        //                        }
        //                    }
        //                }
        //                catch (Exception)
        //                {
        //                }
        //                continue;

        //                //using (Transaction reTrans = new Transaction(Global.UIDoc.Document, "ZZZ"))
        //                //{
        //                //    reTrans.Start();
        //                //    Global.UIDoc.Document.Create.NewDetailCurve(Global.UIDoc.ActiveView, duoBranchs.FlattenValidCurve);
        //                //    Global.UIDoc.Document.Create.NewDetailCurve(Global.UIDoc.ActiveView, flatten_mainCurve_Extend);
        //                //    reTrans.Commit();
        //                //}
        //            }
        //            if (dialogResultData.IsElbowConnection)
        //                sourceMain.Initialize();
        //            sourceMainPipes.Add(sourceMain);
        //        }

        //        foreach (var sourceMain in sourceMainPipes)
        //        {
        //            using (Transaction reTrans = new Transaction(Global.UIDoc.Document, "STEP_1_TWO_LEVEL_SMART"))
        //            {
        //                reTrans.Start();
        //                FailureHandlingOptions fhOpts = reTrans.GetFailureHandlingOptions();
        //                List<DuoBranchPipe> allVerticalPipes = new List<DuoBranchPipe>();
        //                List<DuoBranchPipe> norVerticalPipes = new List<DuoBranchPipe>();

        //                // Create elbow of 2 end of the line
        //                if (sourceMain.IntDuoBranch_1 != null)
        //                {
        //                    if (sourceMain.IntDuoBranch_1.FirstPipe.SourcePipe != null)
        //                    {
        //                    }
        //                }

        //                if (sourceMain.IntDuoBranch_2 != null)
        //                {
        //                }

        //                // Create vertical pipe
        //                foreach (var duoBrandPipe in sourceMain.Branches_Special)
        //                {
        //                    Pipe verticalPipe = CreateVerticalPipeWithTap(sourceMain.MainPipe, duoBrandPipe, dialogResultData.PipeSize * Common.mmToFT);
        //                    if (verticalPipe != null)
        //                    {
        //                        DuoBranchPipe duoBranchPipe = duoBrandPipe.Clone() as DuoBranchPipe;
        //                        duoBranchPipe.VerticalPipe = new InforPie(verticalPipe);
        //                        allVerticalPipes.Add(duoBranchPipe);
        //                        norVerticalPipes.Add(duoBranchPipe);
        //                    }
        //                }
        //                Global.UIDoc.Document.Regenerate();

        //                foreach (var verPipe in norVerticalPipes)
        //                {
        //                    if (!CreateTap(sourceMain.MainPipe.SourcePipe as MEPCurve, verPipe.VerticalPipe.SourcePipe as MEPCurve))
        //                    {
        //                        allVerticalPipes.Remove(allVerticalPipes.Where(item => item.VerticalPipe.SourcePipe.Id == verPipe.VerticalPipe.SourcePipe.Id).FirstOrDefault());
        //                        Global.UIDoc.Document.Delete(verPipe.VerticalPipe.SourcePipe.Id);
        //                    }
        //                }

        //                Global.UIDoc.Document.Regenerate();

        //                //Create Top Tee
        //                foreach (var duoBranch in allVerticalPipes)
        //                {
        //                    CreateTopTee(duoBranch, dialogResultData);
        //                }

        //                //System.Windows.Forms.MessageBox.Show(sourceMain.MainPipe.SourcePipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString());

        //                GetInfoWarning supWarning = new GetInfoWarning(true);
        //                fhOpts.SetFailuresPreprocessor(supWarning);
        //                reTrans.SetFailureHandlingOptions(fhOpts);
        //                reTrans.Commit();
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    { }
        //    return false;
        //}

        /// <summary>
        ///
        /// </summary>
        /// <param name="pipe"></param>
        /// <returns></returns>
        public static PreferredJunctionType GetPreferredJunctionType(Pipe pipe)
        {
            var pipeType = pipe.PipeType as PipeType;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType;
        }

        private static bool ProcessWithTee(List<InforPie> inforMainPipes, List<DuoBranchPipe> pairPies, TwoLevelSmartDialogData dialogResultData)
        {
            try
            {
                List<InforPie> process_InforMains = new List<InforPie>(inforMainPipes);
                List<DuoBranchPipe> process_duoBranchPipes = new List<DuoBranchPipe>(pairPies);

                List<SourceMainPipe> sourceMainPipes = new List<SourceMainPipe>();

                foreach (var inforMain in process_InforMains)
                {
                    var flatten_mainCurve = inforMain.CurveSourcePipe_Flatten;
                    var flatten_mainCurve_Extend = inforMain.CuvreSourcePipe_FlattenExtend;
                    SourceMainPipe sourceMain = new SourceMainPipe(inforMain);

                    foreach (var duoBranchs in process_duoBranchPipes)
                    {
                        try
                        {
                            if (duoBranchs.SecondPipe != null)
                            {
                                if (RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt1
                                    };

                                    sourceMain.Branches.Add(duoBranchPipe);
                                }

                                if (RealityIntersect(flatten_mainCurve_Extend, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt2))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt2
                                    };
                                    sourceMain.Branches_Special.Add(duoBranchPipe);
                                }
                            }
                            else
                            {
                                if (RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt1
                                    };

                                    sourceMain.Branches.Add(duoBranchPipe);
                                    sourceMain.Branches_Special.Add(duoBranchPipe);
                                }
                                else if (RealityIntersect(flatten_mainCurve_Extend, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt2))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt2
                                    };
                                    sourceMain.Branches_Special.Add(duoBranchPipe);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                        continue;
                    }
                    if (dialogResultData.IsElbowConnection)
                        sourceMain.Initialize();
                    sourceMainPipes.Add(sourceMain);
                }

                double diamterPipe = dialogResultData.PipeSize * Common.mmToFT;
                PipeType pipeType = Global.UIDoc.Document.GetElement(dialogResultData.PipeTypeId) as PipeType;
                foreach (var sourceMain in sourceMainPipes)
                {
                    using (Transaction reTrans = new Transaction(Global.UIDoc.Document, "STEP_1_TWO_LEVEL_SMART"))
                    {
                        reTrans.Start();
                        FailureHandlingOptions fhOpts = reTrans.GetFailureHandlingOptions();
                        List<DuoBranchPipe> allVerticalPipes = new List<DuoBranchPipe>();
                        List<DuoBranchPipe> norVerticalPipes = new List<DuoBranchPipe>();
                        if (GetPreferredJunctionType(sourceMain.MainPipe.SourcePipe) == PreferredJunctionType.Tee)
                        {
                            // Create vertical Pipe
                            if (dialogResultData.IsElbowConnection)
                            {
                                // Create elbow of 2 end of the line
                                if (sourceMain.IntDuoBranch_1 != null)
                                {
                                    Pipe verticalPipe_1 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_1, dialogResultData);
                                    if (verticalPipe_1 != null)
                                    {
                                        DuoBranchPipe duoBranchPipe = sourceMain.IntDuoBranch_1.Clone() as DuoBranchPipe;
                                        duoBranchPipe.VerticalPipe = new InforPie(verticalPipe_1);
                                        allVerticalPipes.Add(duoBranchPipe);
                                    }
                                }

                                if (sourceMain.IntDuoBranch_2 != null)
                                {
                                    Pipe verticalPipe_2 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_2, dialogResultData);
                                    if (verticalPipe_2 != null)
                                    {
                                        DuoBranchPipe duoBranchPipe = sourceMain.IntDuoBranch_2.Clone() as DuoBranchPipe;
                                        duoBranchPipe.VerticalPipe = new InforPie(verticalPipe_2);
                                        allVerticalPipes.Add(duoBranchPipe);
                                    }
                                }
                            }

                            //Create Top Tee Last Branch
                            foreach (var duoBranch in allVerticalPipes)
                            {
                                CreateTopTee(duoBranch, dialogResultData);
                            }

                            List<ElementId> mainPipeIds = new List<ElementId>();
                            mainPipeIds.Add(sourceMain.MainPipe.SourcePipe.Id);

                            // Create vertical pipe
                            foreach (var duoBrandPipe in sourceMain.Branches_Special)
                            {
                                Pipe pipe1 = duoBrandPipe.FirstPipe.SourcePipe;
                                Pipe pipe2 = duoBrandPipe.SecondPipe != null ? duoBrandPipe.SecondPipe.SourcePipe : null;

                                using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
                                {
                                    reSubTrans.Start();
                                    try
                                    {
                                        List<Pipe> mainPipes = new List<Pipe>();
                                        mainPipeIds.ForEach(item =>
                                        {
                                            mainPipes.Add(Global.UIDoc.Document.GetElement(item) as Pipe);
                                        });

                                        bool twoPipes = pipe2 != null ? true : false;
                                        Pipe validMainPipe = ProcessMainPipe(mainPipes, pipe1, out bool flagSplit, out XYZ intPnt);

                                        XYZ inter_main1 = null;
                                        XYZ p0_sub1 = null;
                                        XYZ p1_sub1 = null;
                                        XYZ inters_sub1 = null;
                                        Pipe main = null;

                                        foreach (ElementId pId in mainPipeIds)
                                        {
                                            var m = Global.UIDoc.Document.GetElement(pId) as Pipe;
                                            if (DivideCase_2(m, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1) == true)
                                            {
                                                main = m;
                                                break;
                                            }
                                        }

                                        if (pipe2 == null)
                                        {
                                            var temp_pipe1 = pipe1;
                                            var temp_pipe2_id = PlumbingUtils.BreakCurve(Global.UIDoc.Document, temp_pipe1.Id, inters_sub1);
                                            var temp_pipe2 = Global.UIDoc.Document.GetElement(temp_pipe2_id) as Pipe;
                                            pipe1 = temp_pipe1;
                                            pipe2 = temp_pipe2;

                                            SetEntity(pipe1);
                                            SetEntity(pipe2);
                                        }

                                        DivideCase_2(main, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1);

                                        XYZ inter_main2 = null;
                                        XYZ p0_sub2 = null;
                                        XYZ p1_sub2 = null;
                                        XYZ inters_sub2 = null;

                                        if (DivideCase_2(main, pipe2, true, out inter_main2, out p0_sub2, out p1_sub2, out inters_sub2) == false)
                                        {
                                            reSubTrans.RollBack();
                                            continue;
                                        }

                                        if (inter_main1.DistanceTo(inter_main2) > 0.001)
                                        {
                                            reSubTrans.RollBack();
                                            continue; //Check
                                        }

                                        if (inters_sub1.DistanceTo(inters_sub2) > 0.001)
                                        {
                                            reSubTrans.RollBack();
                                            continue; //Check
                                        }

                                        //Create vertical pipe
                                        var vertical = Common.Clone(sourceMain.MainPipe.SourcePipe) as Pipe;

                                        if (!dialogResultData.IsAutoDelectPipeType)
                                            vertical.PipeType = pipeType;

                                        //vertical.LookupParameter("Diameter").Set(diamterPipe);

                                        double pipeSize = 0.0;

                                        if (dialogResultData.IsVerticalPipeSizeEqual)
                                        {
                                            pipeSize = Math.Max(pipe1.Diameter, pipe2.Diameter);
                                        }
                                        else if (dialogResultData.IsVerticalPipeSizeBiggger)
                                        {
                                            double size = Math.Max(pipe1.Diameter, pipe2.Diameter);

                                            List<double> listSize = Common.GetPipeSizes(Global.UIDoc.Document, pipeType);

                                            int index = listSize.IndexOf(size);

                                            if (index > -1 && index + 1 < listSize.Count)
                                                pipeSize = listSize[index + 1];
                                            else
                                                pipeSize = Math.Max(pipe1.Diameter, pipe2.Diameter);
                                        }
                                        else
                                            pipeSize = diamterPipe;

                                        ParameterUtils.SetValueParameterByBuiltIn(vertical, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, pipeSize);

                                        (vertical.Location as LocationCurve).Curve = Line.CreateBound(inter_main1, inters_sub1);

                                        if (pipe1 != null && pipe1.IsValidObject)

                                        {
                                            List<Element> elements = Common.AllComponentsOnPipeTruss(pipe1.Document, pipe1.Id, true);

                                            foreach (var item in elements)
                                            {
                                                StorageUtility.AddEntity(item, SCHEMA_IS_ELEMENT_ORIGIN_GUID, SCHEMA_IS_ELEMENT_ORIGIN_NAME, true.ToString());
                                            }
                                        }

                                        if (pipe2 != null && pipe2.IsValidObject)

                                        {
                                            List<Element> elements = Common.AllComponentsOnPipeTruss(pipe2.Document, pipe2.Id, true);

                                            foreach (var item in elements)
                                            {
                                                StorageUtility.AddEntity(item, SCHEMA_IS_ELEMENT_ORIGIN_GUID, SCHEMA_IS_ELEMENT_ORIGIN_NAME, true.ToString());
                                            }
                                        }

                                        //Conect to main pipe

                                        //Try
                                        if (main as Pipe != null && vertical as Pipe != null && inter_main1 != null)
                                        {
                                            Pipe main2 = null;

                                            var fitting = CreateTee(main as Pipe, vertical as Pipe, inter_main1, out main2);
                                            if (main2 != null && fitting != null)
                                            {
                                                StorageUtility.AddEntity(fitting, SCHEMA_IS_TEE_GUID, SCHEMA_IS_TEE_NAME, true.ToString());

                                                mainPipeIds.Add(main2.Id);
                                            }
                                            else
                                            {
                                                reSubTrans.RollBack();
                                                continue;
                                            }
                                        }

                                        //Process for sub pipes
                                        var c5 = Common.GetConnectorClosestTo(vertical, inters_sub1);

                                        double dMoi = 15 * Common.mmToFT;

                                        double d10 = 10 * Common.mmToFT;
                                        double dtemp = 2;

                                        //if (CompareDouble(diamterPipe, pipe1.Diameter) && CompareDouble(diamterPipe, pipe2.Diameter))
                                        if (CompareDouble(pipeSize, pipe1.Diameter) && CompareDouble(pipeSize, pipe2.Diameter))
                                        {
                                            //Connect to sub
                                            (pipe1.Location as LocationCurve).Curve = Line.CreateBound(p0_sub1, p1_sub1);
                                            (pipe2.Location as LocationCurve).Curve = Line.CreateBound(p0_sub2, p1_sub2);

                                            //Connect
                                            var c3 = Common.GetConnectorClosestTo(pipe1, inters_sub1);
                                            var c4 = Common.GetConnectorClosestTo(pipe2, inters_sub1);

                                            if (CreateFittingForMEPUtils.CreateTee(c3, c4, c5) == null)
                                            {
                                                reSubTrans.RollBack();
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            Connector c3 = null;
                                            Pipe pipe_moi_1 = null;

                                            XYZ v = null;
                                            //if (CompareDouble(diamterPipe, pipe1.Diameter) == false)
                                            if (CompareDouble(pipeSize, pipe1.Diameter) == false)
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

                                            //if (CompareDouble(diamterPipe, pipe2.Diameter) == false)
                                            if (CompareDouble(pipeSize, pipe2.Diameter) == false)
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
                                                reSubTrans.RollBack();
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
                                            }
                                        }
                                        reSubTrans.Commit();
                                    }
                                    catch (Exception)
                                    {
                                        reSubTrans.RollBack();
                                    }
                                }
                            }
                            Global.UIDoc.Document.Regenerate();
                        }
                        DisableWarning supWarning = new DisableWarning();
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        reTrans.SetFailureHandlingOptions(fhOpts);
                        reTrans.Commit();
                    }
                }
            }
            catch (Exception)
            { }
            return false;
        }

        private static bool ProcessWithTap(List<InforPie> inforMainPipes, List<DuoBranchPipe> pairPies, TwoLevelSmartDialogData dialogResultData)
        {
            try
            {
                List<InforPie> process_InforMains = new List<InforPie>(inforMainPipes);
                List<DuoBranchPipe> process_duoBranchPipes = new List<DuoBranchPipe>(pairPies);

                List<SourceMainPipe> sourceMainPipes = new List<SourceMainPipe>();

                foreach (var inforMain in process_InforMains)
                {
                    var flatten_mainCurve = inforMain.CurveSourcePipe_Flatten;
                    var flatten_mainCurve_Extend = inforMain.CuvreSourcePipe_FlattenExtend;
                    SourceMainPipe sourceMain = new SourceMainPipe(inforMain);

                    foreach (var duoBranchs in process_duoBranchPipes)
                    {
                        try
                        {
                            if (duoBranchs.SecondPipe != null)
                            {
                                if (RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt1
                                    };

                                    sourceMain.Branches.Add(duoBranchPipe);
                                }

                                if (RealityIntersect(flatten_mainCurve_Extend, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt2))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt2
                                    };
                                    sourceMain.Branches_Special.Add(duoBranchPipe);
                                }
                            }
                            else
                            {
                                if (RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt1
                                    };

                                    sourceMain.Branches.Add(duoBranchPipe);
                                    sourceMain.Branches_Special.Add(duoBranchPipe);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                        continue;

                        //using (Transaction reTrans = new Transaction(Global.UIDoc.Document, "ZZZ"))
                        //{
                        //    reTrans.Start();
                        //    Global.UIDoc.Document.Create.NewDetailCurve(Global.UIDoc.ActiveView, duoBranchs.FlattenValidCurve);
                        //    Global.UIDoc.Document.Create.NewDetailCurve(Global.UIDoc.ActiveView, flatten_mainCurve_Extend);
                        //    reTrans.Commit();
                        //}
                    }
                    if (dialogResultData.IsElbowConnection)
                        sourceMain.Initialize();
                    sourceMainPipes.Add(sourceMain);
                }

                foreach (var sourceMain in sourceMainPipes)
                {
                    using (Transaction reTrans = new Transaction(Global.UIDoc.Document, "STEP_1_TWO_LEVEL_SMART"))
                    {
                        reTrans.Start();
                        FailureHandlingOptions fhOpts = reTrans.GetFailureHandlingOptions();
                        List<DuoBranchPipe> allVerticalPipes = new List<DuoBranchPipe>();
                        List<DuoBranchPipe> norVerticalPipes = new List<DuoBranchPipe>();
                        if (GetPreferredJunctionType(sourceMain.MainPipe.SourcePipe) == PreferredJunctionType.Tap)
                        {
                            // Create vertical Pipe
                            if (dialogResultData.IsElbowConnection)
                            {
                                // Create elbow of 2 end of the line
                                if (sourceMain.IntDuoBranch_1 != null)
                                {
                                    Pipe verticalPipe_1 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_1, dialogResultData);
                                    if (verticalPipe_1 != null)
                                    {
                                        DuoBranchPipe duoBranchPipe = sourceMain.IntDuoBranch_1.Clone() as DuoBranchPipe;
                                        duoBranchPipe.VerticalPipe = new InforPie(verticalPipe_1);
                                        allVerticalPipes.Add(duoBranchPipe);
                                    }
                                }

                                if (sourceMain.IntDuoBranch_2 != null)
                                {
                                    Pipe verticalPipe_2 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_2, dialogResultData);
                                    if (verticalPipe_2 != null)
                                    {
                                        DuoBranchPipe duoBranchPipe = sourceMain.IntDuoBranch_2.Clone() as DuoBranchPipe;
                                        duoBranchPipe.VerticalPipe = new InforPie(verticalPipe_2);
                                        allVerticalPipes.Add(duoBranchPipe);
                                    }
                                }
                            }

                            // Create vertical pipe
                            foreach (var duoBrandPipe in sourceMain.Branches_Special)
                            {
                                Pipe verticalPipe = CreateVerticalPipeWithTap(sourceMain.MainPipe, duoBrandPipe, dialogResultData);
                                if (verticalPipe != null)
                                {
                                    DuoBranchPipe duoBranchPipe = duoBrandPipe.Clone() as DuoBranchPipe;
                                    duoBranchPipe.VerticalPipe = new InforPie(verticalPipe);
                                    allVerticalPipes.Add(duoBranchPipe);
                                    norVerticalPipes.Add(duoBranchPipe);
                                }
                            }
                            Global.UIDoc.Document.Regenerate();

                            foreach (var verPipe in norVerticalPipes)
                            {
                                if (!CreateTap(sourceMain.MainPipe.SourcePipe as MEPCurve, verPipe.VerticalPipe.SourcePipe as MEPCurve))
                                {
                                    allVerticalPipes.Remove(allVerticalPipes.Where(item => item.VerticalPipe.SourcePipe.Id == verPipe.VerticalPipe.SourcePipe.Id).FirstOrDefault());
                                    Global.UIDoc.Document.Delete(verPipe.VerticalPipe.SourcePipe.Id);
                                }
                            }

                            Global.UIDoc.Document.Regenerate();

                            //Create Top Tee
                            foreach (var duoBranch in allVerticalPipes)
                            {
                                CreateTopTee(duoBranch, dialogResultData);
                            }

                            //System.Windows.Forms.MessageBox.Show(sourceMain.MainPipe.SourcePipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString());
                        }
                        DisableWarning supWarning = new DisableWarning();
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        reTrans.SetFailureHandlingOptions(fhOpts);
                        reTrans.Commit();
                    }
                }
            }
            catch (Exception)
            { }
            return false;
        }

        public static bool RealityIntersect(Line mainLine, Line checkLine, out XYZ intersectPnt)
        {
            intersectPnt = null;
            try
            {
                SetComparisonResult intsRet;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = mainLine.Intersect(checkLine, CurveIntersectResultOption.Detailed);
    intsRet = intersectResult.Result;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray intsRetArr = new IntersectionResultArray();
                intsRet = mainLine.Intersect(checkLine, out intsRetArr);
#endif

                if (intsRet == SetComparisonResult.Overlap)
                {
#if Debug_2027 || Release_2027 || Release_2026
    intersectPnt = intersectResult.GetOverlaps()[0].Point;
#else
                    intersectPnt = intsRetArr.get_Item(0).XYZPoint;
#endif
                    return true;
                }
            }
            catch (Exception)
            { }
            return false;
        }

        private static Pipe CreateVerticalPipeWithElbowLastBranch(InforPie mainPipe, DuoBranchPipe duoBranchPipe, TwoLevelSmartDialogData dialogResultData)
        {
            try
            {
                if (mainPipe == null || duoBranchPipe == null)
                    return null;

                Pipe retVal = null;

                var lineZ = Line.CreateUnbound(duoBranchPipe.FlattenIntersectPoint, XYZ.BasisZ);

                SetComparisonResult setComRet;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = mainPipe.CurveSourcePipe_Unbound.Intersect(lineZ, CurveIntersectResultOption.Detailed);
    setComRet = intersectResult.Result;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray intArr = new IntersectionResultArray();
                setComRet = mainPipe.CurveSourcePipe_Unbound.Intersect(lineZ, out intArr);
#endif

                if (setComRet != SetComparisonResult.Overlap)
                    return null;

#if Debug_2027 || Release_2027 || Release_2026
    var intMainPipe = intersectResult.GetOverlaps()[0].Point;
#else
                var intMainPipe = intArr.get_Item(0).XYZPoint;
#endif

                var intBranchPipe = new XYZ(intMainPipe.X, intMainPipe.Y, duoBranchPipe.FirstPipe.CurveSourcePipe.Origin.Z);

                double diameter = dialogResultData.PipeSize * Common.mmToFT;

                using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
                {
                    reSubTrans.Start();

                    PipeType pipeType = Global.UIDoc.Document.GetElement(dialogResultData.PipeTypeId) as PipeType;

                    SetEntity(duoBranchPipe.FirstPipe?.SourcePipe);
                    SetEntity(duoBranchPipe.SecondPipe?.SourcePipe);

                    var verticalPipe = Common.Clone(mainPipe.SourcePipe) as Pipe;

                    if (!dialogResultData.IsAutoDelectPipeType)
                        verticalPipe.PipeType = pipeType;

                    //verticalPipe.LookupParameter("Diameter").Set(diameter);

                    double pipeSize = 0.0;

                    double diamter1 = (duoBranchPipe.FirstPipe != null && duoBranchPipe.FirstPipe.SourcePipe != null) ? duoBranchPipe.FirstPipe.SourcePipe.Diameter : 0.0;

                    double diamter2 = (duoBranchPipe.SecondPipe != null && duoBranchPipe.SecondPipe.SourcePipe != null) ? duoBranchPipe.SecondPipe.SourcePipe.Diameter : 0.0;

                    if (dialogResultData.IsVerticalPipeSizeEqual)
                    {
                        pipeSize = Math.Max(diamter1, diamter2);
                    }
                    else if (dialogResultData.IsVerticalPipeSizeBiggger)
                    {
                        double size = Math.Max(diamter1, diamter2);

                        List<double> listSize = Common.GetPipeSizes(Global.UIDoc.Document, mainPipe.SourcePipe.PipeType);

                        int index = listSize.IndexOf(size);

                        if (index > -1 && index + 1 < listSize.Count)
                            pipeSize = listSize[index + 1];
                        else
                            pipeSize = Math.Max(diamter1, diamter2);
                    }
                    else
                        pipeSize = diameter;

                    ParameterUtils.SetValueParameterByBuiltIn(verticalPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, pipeSize);

                    (verticalPipe.Location as LocationCurve).Curve = Line.CreateBound(intMainPipe, intBranchPipe);

                    try
                    {
                        Connector cnt_1 = Common.GetConnectorClosestTo(verticalPipe, intMainPipe);

                        Connector cnt_2 = Common.GetConnectorClosestTo(mainPipe.SourcePipe, intMainPipe);
                        if (cnt_2.IsConnected)
                        {
                            cnt_2 = Common.GetConnectorNotConnnected(mainPipe.SourcePipe.ConnectorManager);
                        }

                        if (cnt_1 != null && !cnt_1.IsConnected && cnt_2 != null && !cnt_2.IsConnected)
                        {
                            var elbow = Global.UIDoc.Document.Create.NewElbowFitting(cnt_1, cnt_2);

                            StorageUtility.AddEntity(elbow, SCHEMA_IS_TEE_GUID, SCHEMA_IS_TEE_NAME, false.ToString());

                            retVal = verticalPipe;
                            reSubTrans.Commit();
                        }
                        else
                        {
                            reSubTrans.RollBack();
                        }
                    }
                    catch (Exception)
                    {
                        reSubTrans.RollBack();
                    }
                }

                return retVal;
            }
            catch (Exception)
            { }
            return null;
        }

        private static Pipe CreateVerticalPipeWithTap(InforPie mainPipe, DuoBranchPipe duoBranchPipe, TwoLevelSmartDialogData dialogResultData)
        {
            try
            {
                double diameter = dialogResultData.PipeSize * Common.mmToFT;

                if (mainPipe == null || duoBranchPipe == null)
                    return null;

                Pipe retVal = null;
                double temp = 200;

                var lineZ = Line.CreateBound(new XYZ(duoBranchPipe.FlattenIntersectPoint.X, duoBranchPipe.FlattenIntersectPoint.Y, duoBranchPipe.FlattenIntersectPoint.Z - temp)
                    , new XYZ(duoBranchPipe.FlattenIntersectPoint.X, duoBranchPipe.FlattenIntersectPoint.Y, duoBranchPipe.FlattenIntersectPoint.Z + temp));
                SetComparisonResult setComRet;

                // --- 1. TÌM GIAO ĐIỂM TRÊN ỐNG CHÍNH ---
#if Debug_2027 || Release_2027 || Release_2026
    // Cấu hình bản mới
    var intersectResult1 = mainPipe.CurveSourcePipe_Unbound.Intersect(lineZ, CurveIntersectResultOption.Detailed);
    setComRet = intersectResult1.Result;
#else
                // Cấu hình bản cũ
                IntersectionResultArray intArr = new IntersectionResultArray();
                setComRet = mainPipe.CurveSourcePipe_Unbound.Intersect(lineZ, out intArr);
#endif

                if (setComRet != SetComparisonResult.Overlap)
                    return null;

#if Debug_2027 || Release_2027 || Release_2026
    var intMainPipe = intersectResult1.GetOverlaps()[0].Point;
#else
                var intMainPipe = intArr.get_Item(0).XYZPoint;
#endif

                // --- 2. TẠO LINE UNBOUND CHO ỐNG NHÁNH ---
                Line lineUn = null;
                if (duoBranchPipe.SecondPipe == null)
                    lineUn = Line.CreateUnbound(duoBranchPipe.FirstPipe.CurveSourcePipe.Origin, duoBranchPipe.FirstPipe.CurveSourcePipe.Direction);
                else
                    lineUn = Line.CreateUnbound(duoBranchPipe.SecondPipe.CurveSourcePipe.Origin, duoBranchPipe.SecondPipe.CurveSourcePipe.Direction);

                // --- 3. TÌM GIAO ĐIỂM TRÊN ỐNG NHÁNH ---
#if Debug_2027 || Release_2027 || Release_2026
    // Cấu hình bản mới
    var intersectResult2 = lineUn.Intersect(lineZ, CurveIntersectResultOption.Detailed);
    setComRet = intersectResult2.Result;
#else
                // Cấu hình bản cũ
                intArr = new IntersectionResultArray();
                setComRet = lineUn.Intersect(lineZ, out intArr);
#endif

                if (setComRet != SetComparisonResult.Overlap)
                    return null;

#if Debug_2027 || Release_2027 || Release_2026
    var intBranchPipe = intersectResult2.GetOverlaps()[0].Point;
#else
                var intBranchPipe = intArr.get_Item(0).XYZPoint;
#endif
                intMainPipe = mainPipe.CurveSourcePipe_Unbound.Project(intBranchPipe).XYZPoint;

                using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
                {
                    reSubTrans.Start();

                    SetEntity(duoBranchPipe.FirstPipe?.SourcePipe);
                    SetEntity(duoBranchPipe.SecondPipe?.SourcePipe);

                    var verticalPipe = Common.Clone(mainPipe.SourcePipe) as Pipe;

                    PipeType pipeType = Global.UIDoc.Document.GetElement(dialogResultData.PipeTypeId) as PipeType;

                    if (!dialogResultData.IsAutoDelectPipeType)
                        verticalPipe.PipeType = pipeType;

                    //verticalPipe.LookupParameter("Diameter").Set(diameter);

                    double pipeSize = 0.0;

                    double diamter1 = (duoBranchPipe.FirstPipe != null && duoBranchPipe.FirstPipe.SourcePipe != null) ? duoBranchPipe.FirstPipe.SourcePipe.Diameter : 0.0;

                    double diamter2 = (duoBranchPipe.SecondPipe != null && duoBranchPipe.SecondPipe.SourcePipe != null) ? duoBranchPipe.SecondPipe.SourcePipe.Diameter : 0.0;

                    if (dialogResultData.IsVerticalPipeSizeEqual)
                    {
                        pipeSize = Math.Max(diamter1, diamter2);
                    }
                    else if (dialogResultData.IsVerticalPipeSizeBiggger)
                    {
                        double size = Math.Max(diamter1, diamter2);

                        List<double> listSize = Common.GetPipeSizes(Global.UIDoc.Document, mainPipe.SourcePipe.PipeType);

                        int index = listSize.IndexOf(size);

                        if (index > -1 && index + 1 < listSize.Count)
                            pipeSize = listSize[index + 1];
                        else
                            pipeSize = Math.Max(diamter1, diamter2);
                    }
                    else
                        pipeSize = diameter;

                    ParameterUtils.SetValueParameterByBuiltIn(verticalPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, pipeSize);

                    (verticalPipe.Location as LocationCurve).Curve = Line.CreateBound(intMainPipe, intBranchPipe);

                    retVal = verticalPipe;

                    reSubTrans.Commit();
                }

                return retVal;
            }
            catch (Exception)
            { }
            return null;
        }

        public static void SetEntity(Pipe pipe)
        {
            if (pipe != null && pipe.IsValidObject)

            {
                List<Element> elements = Common.AllComponentsOnPipeTruss(pipe.Document, pipe.Id, true);

                foreach (var item in elements)
                {
                    StorageUtility.AddEntity(item, SCHEMA_IS_ELEMENT_ORIGIN_GUID, SCHEMA_IS_ELEMENT_ORIGIN_NAME, true.ToString());
                }
            }
        }

        public static bool CreateTap(MEPCurve mepCurveSplit1, MEPCurve mepCurveSplit2)
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
                    var con = Common.GetConnectorClosestTo(mepCurveSplit2, p10);
                    var elbow = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);

                    StorageUtility.AddEntity(elbow, SCHEMA_IS_TEE_GUID, SCHEMA_IS_TEE_NAME, false.ToString());

                    Global.UIDoc.Document.Regenerate();
                }
                else
                {
                    var con = Common.GetConnectorClosestTo(mepCurveSplit2, p11);
                    var elbow = Global.UIDoc.Document.Create.NewTakeoffFitting(con, mepCurveSplit1);

                    StorageUtility.AddEntity(elbow, SCHEMA_IS_TEE_GUID, SCHEMA_IS_TEE_NAME, false.ToString());

                    Global.UIDoc.Document.Regenerate();
                }

                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// kiểm tra khi hai ống phụ nằm trùng lên nhau
        /// sửa lại vị trí 2 ống phụ để nó không giao vào nhau
        /// </summary>
        /// <param name="duoBranchPipe"></param>
        /// <returns></returns>
        private static DuoBranchPipe ValidatePair(DuoBranchPipe duoBranchPipe)
        {
            DuoBranchPipe ret = duoBranchPipe;
            try
            {
                Document doc = Global.UIDoc.Document;
                if (duoBranchPipe.FirstPipe != null && duoBranchPipe.SecondPipe != null && duoBranchPipe.FlattenIntersectPoint != null)
                {
                    Line exLineZ = Line.CreateUnbound(duoBranchPipe.FlattenIntersectPoint, XYZ.BasisZ);
                    XYZ iP = GeometryUtils.GetUnBoundIntersection(duoBranchPipe.FirstPipe.CurveSourcePipe, exLineZ);
                    if (iP == null)
                        iP = GeometryUtils.GetUnBoundIntersection(duoBranchPipe.SecondPipe.CurveSourcePipe, exLineZ);
                    if (iP != null)
                    {
                        List<(XYZ, XYZ)> points = new List<(XYZ, XYZ)>() {
                   ( duoBranchPipe.FirstPipe.SourcePipe_Cont_1.Origin,duoBranchPipe.FirstPipe.CurveSourcePipe .Direction),
                   ( duoBranchPipe.FirstPipe.SourcePipe_Cont_2.Origin,duoBranchPipe.FirstPipe.CurveSourcePipe .Direction),
                   ( duoBranchPipe.SecondPipe.SourcePipe_Cont_1.Origin,duoBranchPipe.SecondPipe.CurveSourcePipe .Direction),
                   ( duoBranchPipe.SecondPipe.SourcePipe_Cont_2.Origin,duoBranchPipe.SecondPipe.CurveSourcePipe .Direction),
                    };

                        List<(XYZ, double)> dic = points.ConvertAll(x => (x.Item1, (x.Item1 - iP).Normalize().IsAlmostEqualTo(x.Item2) ? x.Item1.DistanceTo(iP) : (-x.Item1.DistanceTo(iP))));
                        dic = dic.OrderBy(x => x.Item2).ToList();
                        XYZ p1 = dic.FirstOrDefault().Item1;
                        XYZ p2 = dic.LastOrDefault().Item1;
                        if (duoBranchPipe.FirstPipe.CurveSourcePipe.Distance(p1) > duoBranchPipe.FirstPipe.CurveSourcePipe.Distance(p2))
                        {
                            var tg = p1;
                            p1 = p2;
                            p2 = tg;
                        }

                        Line newL1 = Line.CreateBound(iP, p1);
                        if (newL1.Direction.IsAlmostEqualTo(duoBranchPipe.FirstPipe.CurveSourcePipe.Direction) == false)
                            newL1 = newL1.CreateReversed() as Line;
                        (duoBranchPipe.FirstPipe.SourcePipe.Location as LocationCurve).Curve = newL1;
                        doc.Regenerate();
                        InforPie inforPie1 = new InforPie(duoBranchPipe.FirstPipe.SourcePipe);

                        Line newL2 = Line.CreateBound(iP, p2);
                        if (newL2.Direction.IsAlmostEqualTo(duoBranchPipe.SecondPipe.CurveSourcePipe.Direction) == false)
                            newL2 = newL2.CreateReversed() as Line;
                        (duoBranchPipe.SecondPipe.SourcePipe.Location as LocationCurve).Curve = newL2;
                        doc.Regenerate();
                        InforPie inforPie2 = new InforPie(duoBranchPipe.SecondPipe.SourcePipe);
                        ret = new DuoBranchPipe(inforPie1, inforPie2);
                    }
                }
            }
            catch (Exception ex)
            {
                PlumbingSolution.FireProtection.Utils.IO.LogException(ex);
            }
            return ret;
        }

        private static void CreateTopTee(DuoBranchPipe duoBranchPipe, TwoLevelSmartDialogData dialogData)
        {
            double diamterPipe = dialogData.PipeSize * Common.mmToFT;

            Pipe pipe_1 = null;
            Pipe pipe_2 = null;
            Pipe verticalPipe = null;

            if (duoBranchPipe.FirstPipe == null)
                return;
            //   ValidatePair(duoBranchPipe);

            pipe_1 = duoBranchPipe.FirstPipe.SourcePipe;

            if (duoBranchPipe.SecondPipe != null)
                pipe_2 = duoBranchPipe.SecondPipe.SourcePipe;

            if (duoBranchPipe.VerticalPipe == null)
                return;
            verticalPipe = duoBranchPipe.VerticalPipe.SourcePipe;

            XYZ p0_sub1 = null;
            XYZ p1_sub1 = null;
            XYZ inters_sub1 = null;
            bool twoPipes = duoBranchPipe.SecondPipe != null ? true : false;

            GetInforBasePointTopTee(verticalPipe, pipe_1, twoPipes, out p0_sub1, out p1_sub1, out inters_sub1);
            if (duoBranchPipe.SecondPipe == null)
            {
                var temp_pipe1 = pipe_1;
                try
                {
                    var temp_pipe2_id = PlumbingUtils.BreakCurve(Global.UIDoc.Document, temp_pipe1.Id, inters_sub1);
                    var temp_pipe2 = Global.UIDoc.Document.GetElement(temp_pipe2_id) as Pipe;
                    pipe_1 = temp_pipe1;
                    pipe_2 = temp_pipe2;

                    SetEntity(pipe_1);
                    SetEntity(pipe_2);
                }
                catch (Exception)
                {
                    Global.UIDoc.Document.Delete(verticalPipe.Id);
                    return;
                }
            }

            GetInforBasePointTopTee(verticalPipe, pipe_1, true, out p0_sub1, out p1_sub1, out inters_sub1);

            XYZ p0_sub2 = null;
            XYZ p1_sub2 = null;
            XYZ inters_sub2 = null;

            GetInforBasePointTopTee(verticalPipe, pipe_2, true, out p0_sub2, out p1_sub2, out inters_sub2);

            if (inters_sub1.DistanceTo(inters_sub2) > 0.001)
            {
                return;
            }

            //Process for sub pipes
            var c5 = Common.GetConnectorClosestTo(verticalPipe, inters_sub1);

            double dMoi = 15 * Common.mmToFT;

            double d10 = 10 * Common.mmToFT;
            double dtemp = 2;

            double pipeSize = 0.0;

            if (dialogData.IsVerticalPipeSizeEqual)
            {
                pipeSize = Math.Max(pipe_1.Diameter, pipe_2.Diameter);
            }
            else if (dialogData.IsVerticalPipeSizeBiggger)
            {
                double size = Math.Max(pipe_1.Diameter, pipe_2.Diameter); ;

                List<double> listSize = Common.GetPipeSizes(Global.UIDoc.Document, pipe_1.PipeType);

                int index = listSize.IndexOf(size);

                if (index > -1 && index + 1 < listSize.Count)
                    pipeSize = listSize[index + 1];
                else
                    pipeSize = Math.Max(duoBranchPipe.FirstPipe.SourcePipe.Diameter, duoBranchPipe.SecondPipe.SourcePipe.Diameter);
            }
            else
                pipeSize = diamterPipe;

            if (CompareDouble(pipeSize, pipe_1.Diameter) && CompareDouble(pipeSize, pipe_2.Diameter))
            {
                using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
                {
                    reSubTrans.Start();
                    //Connect to sub
                    (pipe_1.Location as LocationCurve).Curve = Line.CreateBound(p0_sub1, p1_sub1);
                    (pipe_2.Location as LocationCurve).Curve = Line.CreateBound(p0_sub2, p1_sub2);

                    //Connect
                    var c3 = Common.GetConnectorClosestTo(pipe_1, inters_sub1);
                    var c4 = Common.GetConnectorClosestTo(pipe_2, inters_sub1);

                    if (CreateTee(c3, c4, c5) == null)
                    {
                        reSubTrans.RollBack();
                    }
                    else
                        reSubTrans.Commit();
                }
            }
            else
            {
                using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
                {
                    reSubTrans.Start();
                    Connector c3 = null;
                    Pipe pipe_moi_1 = null;

                    XYZ v = null;
                    if (CompareDouble(pipeSize, pipe_1.Diameter) == false)
                    {
                        //Tao mot ong mồi
                        pipe_moi_1 = Common.Clone(verticalPipe) as Pipe;
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
                        (pipe_1.Location as LocationCurve).Curve = line;
                        c3 = Common.GetConnectorClosestTo(pipe_1, inters_sub1);

                        v = line.Direction;
                    }

                    Connector c4 = null;
                    Pipe pipe_moi_2 = null;

                    if (CompareDouble(pipeSize, pipe_2.Diameter) == false)
                    {
                        //Tao mot ong mồi
                        pipe_moi_2 = Common.Clone(verticalPipe) as Pipe;
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
                        (pipe_2.Location as LocationCurve).Curve = Line.CreateBound(p0_sub2, p1_sub2);
                        c4 = Common.GetConnectorClosestTo(pipe_2, inters_sub1);
                    }

                    //Connect
                    FamilyInstance fitting = CreateFittingForMEPUtils.CreateTee(c3, c4, c5);
                    if (fitting == null)
                    {
                        reSubTrans.RollBack/*Commit*/();
                    }
                    else
                    {
                        Connector main1 = null;
                        Connector main2 = null;
                        Connector tee = null;
                        Common.GetInfo(fitting, v, out main1, out main2, out tee);

                        var mep1 = GetPipeFromConnector(main1.AllRefs);
                        var mep2 = GetPipeFromConnector(main2.AllRefs);

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

                                ll(pipe_1, futher, p);

                                topReducer_1 = CreateFittingForMEPUtils.ctt(pipe_1, pipe_moi_1);
                            }
                            else if (mep2 != null && pipe_moi_1.Id == mep2.Id)
                            {
                                var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                var p = line.Evaluate(d10, false);
                                (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                var futher = inters_sub1.DistanceTo(p0_sub1) > inters_sub1.DistanceTo(p1_sub1) ? p0_sub1 : p1_sub1;

                                ll(pipe_1, futher, p);

                                topReducer_1 = CreateFittingForMEPUtils.ctt(pipe_1, pipe_moi_1);
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

                                ll(pipe_2, futher, p);

                                topReducer_2 = CreateFittingForMEPUtils.ctt(pipe_2, pipe_moi_2);
                            }
                            else if (mep2 != null && pipe_moi_2.Id == mep2.Id)
                            {
                                var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                var p = line.Evaluate(d10, false);
                                (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                var futher = inters_sub2.DistanceTo(p0_sub2) > inters_sub2.DistanceTo(p1_sub2) ? p0_sub2 : p1_sub2;

                                ll(pipe_2, futher, p);

                                topReducer_2 = CreateFittingForMEPUtils.ctt(pipe_2, pipe_moi_2);
                            }
                        }

                        reSubTrans.Commit();
                    }
                }
            }
        }

        public static bool CompareDouble(double d1, double d2)
        {
            if (Math.Abs(d1 - d2) < 0.001)
                return true;

            return false;
        }

        public static bool GetInforBasePointTopTee(Pipe verticalPipe, Pipe branchPipe, bool twoPipe, out XYZ p0_sub, out XYZ p1_sub, out XYZ inters_sub)
        {
            inters_sub = verticalPipe.GetCurve().GetEndPoint(1);
            var curve_sub = branchPipe.GetCurve();
            if (twoPipe)
            {
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

        public static FamilyInstance CreateTee(Connector c3, Connector c4, Connector c5)
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

        public static Pipe GetPipeFromConnector(ConnectorSet cs)
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

        public static void ll(Pipe pipe, XYZ pOn, XYZ pOther)
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

        private static List<Connector> GetConnectors(ConnectorSet connectorSet, bool filter = false)
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

        public static Pipe ProcessMainPipe(List<Pipe> mainPipes, Pipe processPipe, out bool flagSplit, out XYZ processIntPnt)
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

        public static void ProcessStartSidePipe(Pipe pipe, out Pipe pipe2, XYZ pOn, bool flagSplit, out bool isDauOngChinh)
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

        public static bool CheckPipeIsEnd(Pipe pipe, XYZ point)
        {
            var con = Common.GetConnectorClosestTo(pipe, point);

            return con.IsConnected;
        }

        private static dynamic GetParameterValueByName(Element elem, string paramName)
        {
            if (elem != null)
            {
                Parameter parameter = elem.LookupParameter(paramName);
                return GetParameterValue(parameter);
            }
            return null;
        }

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

        public static Pipe ee(ConnectorSet cs)
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

        public static FamilyInstance CreateTee(Pipe pipeMain, Pipe pipeCurrent, XYZ splitPoint, out Pipe main2)
        {
            //var curve = (pipeMain.Location as LocationCurve).Curve;

            //var p0 = curve.GetEndPoint(0);
            //var p1 = curve.GetEndPoint(1);

            var pipeTempMain1 = pipeMain;

            //var line1 = Line.CreateBound(p0, splitPoint);
            //(pipeTempMain1.Location as LocationCurve).Curve = line1;

            //var newPlace = new XYZ(0, 0, 0);
            //var elemIds = ElementTransformUtils.CopyElement(
            //   Global.UIDoc.Document, pipeTempMain1.Id, newPlace);

            //main2 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

            //var line2 = Line.CreateBound(splitPoint, p1);
            //(main2.Location as LocationCurve).Curve = line2;

            ElementId elementId = CmdCopyGroup.BreakMEPCurve(pipeMain.Document, pipeMain.Id, splitPoint);

            main2 = pipeMain.Document.GetElement(elementId) as Pipe;

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

        public static bool DivideCase_2(Pipe main, Pipe pipeSub, bool twoPipes, out XYZ inters_main, out XYZ p0_sub, out XYZ p1_sub, out XYZ inters_sub)
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

            //Check intersection
            SetComparisonResult result;

            // --- 1. TÌM GIAO ĐIỂM 2D ---
#if Debug_2027 || Release_2027 || Release_2026
            // Cấu hình bản mới
            var intersectResult1 = curve_main_2d.Intersect(curve2d, CurveIntersectResultOption.Detailed);
            result = intersectResult1.Result;
#else
            // Cấu hình bản cũ
            IntersectionResultArray arr = new IntersectionResultArray();
            result = curve_main_2d.Intersect(curve2d, out arr);
#endif

            if (result != SetComparisonResult.Overlap)
                return false;

#if Debug_2027 || Release_2027 || Release_2026
            var pInter_2d = intersectResult1.GetOverlaps()[0].Point;
#else
            var pInter_2d = arr.get_Item(0).XYZPoint;
#endif

            //Find 3d
            double temp = 200;
            var lineZ = Line.CreateBound(new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z - temp), new XYZ(pInter_2d.X, pInter_2d.Y, pInter_2d.Z + temp));

            // --- 2. TÌM GIAO ĐIỂM 3D TRÊN ỐNG CHÍNH ---
#if Debug_2027 || Release_2027 || Release_2026
            var intersectResult2 = lineZ.Intersect(curve_main, CurveIntersectResultOption.Detailed);
            result = intersectResult2.Result;
#else
            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve_main, out arr);
#endif

            if (result != SetComparisonResult.Overlap)
                return false;

#if Debug_2027 || Release_2027 || Release_2026
            inters_main = intersectResult2.GetOverlaps()[0].Point;
#else
            inters_main = arr.get_Item(0).XYZPoint;
#endif

            //Find 3d on two sub pipe
            // --- 3. TÌM GIAO ĐIỂM 3D TRÊN ỐNG PHỤ ---
#if Debug_2027 || Release_2027 || Release_2026
            var intersectResult3 = lineZ.Intersect(curve3d, CurveIntersectResultOption.Detailed);
            result = intersectResult3.Result;
#else
            arr = new IntersectionResultArray();
            result = lineZ.Intersect(curve3d, out arr);
#endif

            if (result != SetComparisonResult.Overlap)
                return false;

#if Debug_2027 || Release_2027 || Release_2026
            inters_sub = intersectResult3.GetOverlaps()[0].Point;
#else
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
    }

    public class InforPie
    {
        public Pipe SourcePipe { get; set; }

        public Connector SourcePipe_Cont_1
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject && CurveSourcePipe != null)
                    return Common.GetConnectorClosestTo(SourcePipe, CurveSourcePipe.GetEndPoint(0));
                return null;
            }
        }

        public Connector SourcePipe_Cont_2
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject && CurveSourcePipe != null)
                    return Common.GetConnectorClosestTo(SourcePipe, CurveSourcePipe.GetEndPoint(1));
                return null;
            }
        }

        public Line CurveSourcePipe
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject)
                {
                    return SourcePipe.GetCurve() as Line;
                }
                return null;
            }
        }

        public Line CurveSourcePipe_Unbound
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject && CurveSourcePipe != null)
                {
                    return Line.CreateUnbound(CurveSourcePipe.GetEndPoint(0), CurveSourcePipe.Direction);
                }
                return null;
            }
        }

        public Line CurveSourcePipe_Extend
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject && SourcePipe_Cont_1 != null && SourcePipe_Cont_2 != null)
                {
                    if (!SourcePipe_Cont_1.IsConnected && SourcePipe_Cont_2.IsConnected)
                    {
                        XYZ newStartPoint = CurveSourcePipe.GetEndPoint(0) + (SourcePipe_Cont_1.Origin - SourcePipe_Cont_2.Origin).Normalize() * 1000 * Common.mmToFT;
                        XYZ newEndPoint = CurveSourcePipe.GetEndPoint(1);

                        //newStartPoint = new XYZ(Math.Round(newStartPoint.X, 7), Math.Round(newStartPoint.Y, 7), Math.Round(newStartPoint.Z, 7));
                        //newEndPoint = new XYZ(Math.Round(newEndPoint.X, 7), Math.Round(newEndPoint.Y, 7), Math.Round(newEndPoint.Z, 7));
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                    else if (SourcePipe_Cont_1.IsConnected && !SourcePipe_Cont_2.IsConnected)
                    {
                        XYZ newStartPoint = CurveSourcePipe.GetEndPoint(0);
                        XYZ newEndPoint = CurveSourcePipe.GetEndPoint(1) + (SourcePipe_Cont_2.Origin - SourcePipe_Cont_1.Origin).Normalize() * 1000 * Common.mmToFT;

                        //newEndPoint = new XYZ(Math.Round(newEndPoint.X, 7), Math.Round(newEndPoint.Y, 7), Math.Round(newEndPoint.Z, 7));
                        //newStartPoint = new XYZ(Math.Round(newStartPoint.X, 7), Math.Round(newStartPoint.Y, 7), Math.Round(newStartPoint.Z, 7));
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                    else if (SourcePipe_Cont_1.IsConnected && SourcePipe_Cont_2.IsConnected)
                    {
                        return CurveSourcePipe;
                    }
                    else
                    {
                        XYZ newStartPoint = CurveSourcePipe.Evaluate(CurveSourcePipe.GetEndParameter(0) - 1000 * Common.mmToFT, false);
                        XYZ newEndPoint = CurveSourcePipe.Evaluate(CurveSourcePipe.GetEndParameter(1) + 1000 * Common.mmToFT, false);
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                }
                return null;
            }
        }

        public Line CurveSourcePipe_Extend_Round
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject && SourcePipe_Cont_1 != null && SourcePipe_Cont_2 != null)
                {
                    if (!SourcePipe_Cont_1.IsConnected && SourcePipe_Cont_2.IsConnected)
                    {
                        XYZ newStartPoint = CurveSourcePipe.GetEndPoint(0) + (SourcePipe_Cont_1.Origin - SourcePipe_Cont_2.Origin).Normalize() * 1000 * Common.mmToFT;
                        XYZ newEndPoint = CurveSourcePipe.GetEndPoint(1);

                        newStartPoint = new XYZ(Math.Round(newStartPoint.X, 2), Math.Round(newStartPoint.Y, 2), Math.Round(newStartPoint.Z, 2));
                        newEndPoint = new XYZ(Math.Round(newEndPoint.X, 2), Math.Round(newEndPoint.Y, 2), Math.Round(newEndPoint.Z, 2));
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                    else if (SourcePipe_Cont_1.IsConnected && !SourcePipe_Cont_2.IsConnected)
                    {
                        XYZ newStartPoint = CurveSourcePipe.GetEndPoint(0);
                        XYZ newEndPoint = CurveSourcePipe.GetEndPoint(1) + (SourcePipe_Cont_2.Origin - SourcePipe_Cont_1.Origin).Normalize() * 1000 * Common.mmToFT;

                        newEndPoint = new XYZ(Math.Round(newEndPoint.X, 2), Math.Round(newEndPoint.Y, 2), Math.Round(newStartPoint.Z, 2));
                        newStartPoint = new XYZ(Math.Round(newStartPoint.X, 2), Math.Round(newStartPoint.Y, 2), Math.Round(newEndPoint.Z, 2));
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                    else if (SourcePipe_Cont_1.IsConnected && SourcePipe_Cont_2.IsConnected)
                    {
                        return CurveSourcePipe;
                    }
                    else
                    {
                        XYZ newStartPoint = CurveSourcePipe.Evaluate(CurveSourcePipe.GetEndParameter(0) - 1000 * Common.mmToFT, false);
                        XYZ newEndPoint = CurveSourcePipe.Evaluate(CurveSourcePipe.GetEndParameter(1) + 1000 * Common.mmToFT, false);
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                }
                return null;
            }
        }

        public Line CurveSourcePipe_Flatten
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject)
                {
                    return SourcePipe.GetFlattenCurve() as Line;
                }
                return null;
            }
        }

        public Tuple<XYZ, XYZ> ExtendValidPnts { get; set; }

        public Line CuvreSourcePipe_FlattenExtend
        {
            get
            {
                if (SourcePipe != null && SourcePipe.IsValidObject && SourcePipe_Cont_1 != null && SourcePipe_Cont_2 != null)
                {
                    if (!SourcePipe_Cont_1.IsConnected && SourcePipe_Cont_2.IsConnected)
                    {
                        XYZ newStartPoint = CurveSourcePipe_Flatten.Evaluate(CurveSourcePipe_Flatten.GetEndParameter(0) - 600 * Common.mmToFT, false);
                        XYZ newEndPoint = CurveSourcePipe_Flatten.GetEndPoint(1);
                        ExtendValidPnts = new Tuple<XYZ, XYZ>(newStartPoint, null);
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                    else if (SourcePipe_Cont_1.IsConnected && !SourcePipe_Cont_2.IsConnected)
                    {
                        XYZ newStartPoint = CurveSourcePipe_Flatten.GetEndPoint(0);
                        XYZ newEndPoint = CurveSourcePipe_Flatten.Evaluate(CurveSourcePipe_Flatten.GetEndParameter(1) + 600 * Common.mmToFT, false);
                        ExtendValidPnts = new Tuple<XYZ, XYZ>(newEndPoint, null);
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                    else if (SourcePipe_Cont_1.IsConnected && SourcePipe_Cont_2.IsConnected)
                    {
                        ExtendValidPnts = new Tuple<XYZ, XYZ>(null, null);
                        return CurveSourcePipe_Flatten;
                    }
                    else
                    {
                        XYZ newStartPoint = CurveSourcePipe_Flatten.Evaluate(CurveSourcePipe_Flatten.GetEndParameter(0) - 600 * Common.mmToFT, false);
                        XYZ newEndPoint = CurveSourcePipe_Flatten.Evaluate(CurveSourcePipe_Flatten.GetEndParameter(1) + 600 * Common.mmToFT, false);
                        ExtendValidPnts = new Tuple<XYZ, XYZ>(newStartPoint, newEndPoint);
                        return Line.CreateBound(newStartPoint, newEndPoint);
                    }
                }
                return null;
            }
        }

        public InforPie(Pipe sourcePipe)
        {
            SourcePipe = sourcePipe;
        }

        public double TruncateDouble(double number)
        {
            return Math.Truncate(number * 10000) / 10000;
        }
    }

    public class DuoBranchPipe : ICloneable
    {
        public InforPie FirstPipe { get; set; }
        public InforPie SecondPipe { get; set; }
        public XYZ FlattenIntersectPoint { get; set; }
        public InforPie VerticalPipe { get; set; }

        public DuoBranchPipe(InforPie firstPipe, InforPie secondPipe)
        {
            FirstPipe = firstPipe;
            SecondPipe = secondPipe;
        }

        public Line FlattenValidCurve
        {
            get
            {
                if (FirstPipe != null && SecondPipe != null && FirstPipe.SourcePipe != null && SecondPipe.SourcePipe != null)
                {
                    XYZ f_p1 = FirstPipe.SourcePipe_Cont_1.Origin.FlattenPoint();
                    XYZ f_p2 = FirstPipe.SourcePipe_Cont_2.Origin.FlattenPoint();
                    XYZ s_p1 = SecondPipe.SourcePipe_Cont_1.Origin.FlattenPoint();
                    XYZ s_p2 = SecondPipe.SourcePipe_Cont_2.Origin.FlattenPoint();
                    List<XYZ> xYZs = new List<XYZ>() { f_p1, f_p2, s_p1, s_p2 };

                    XYZ point1 = xYZs[0];
                    XYZ point2 = xYZs[1];
                    double maxDistance = point1.DistanceTo(point2);
                    for (int i = 0; i < xYZs.Count; i++)
                    {
                        for (int j = i + 1; j < xYZs.Count; j++)
                        {
                            double distance = xYZs[i].DistanceTo(xYZs[j]);
                            if (distance > maxDistance)
                            {
                                maxDistance = distance;
                                point1 = xYZs[i];
                                point2 = xYZs[j];
                            }
                        }
                    }

                    return Line.CreateBound(point1.FlattenPoint(), point2.FlattenPoint());
                }
                else if (FirstPipe != null && SecondPipe == null && FirstPipe.SourcePipe != null)
                {
                    return FirstPipe.CurveSourcePipe_Flatten;
                }
                return null;
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class SourceMainPipe
    {
        public InforPie MainPipe { get; set; }
        public List<DuoBranchPipe> Branches { get; set; }
        public List<DuoBranchPipe> Branches_Special { get; set; }
        public XYZ Flatten_ExtendValidPnt_1 { get; set; }
        public XYZ Flatten_ExtendValidPnt_2 { get; set; }
        public DuoBranchPipe IntDuoBranch_1 { get; set; }
        public DuoBranchPipe IntDuoBranch_2 { get; set; }

        public SourceMainPipe(InforPie mainPipe)
        {
            MainPipe = mainPipe;
            Branches = new List<DuoBranchPipe>();
            Branches_Special = new List<DuoBranchPipe>();
            if (mainPipe.CuvreSourcePipe_FlattenExtend != null && mainPipe.ExtendValidPnts != null)
            {
                Flatten_ExtendValidPnt_1 = mainPipe.ExtendValidPnts.Item1;
                Flatten_ExtendValidPnt_2 = mainPipe.ExtendValidPnts.Item2;
            }
        }

        public void Initialize()
        {
            if (Branches_Special != null && Branches_Special.Count > 0)
            {
                if (Flatten_ExtendValidPnt_1 != null)
                {
                    try
                    {
                        XYZ closetPnt1 = FindClosestPoint(Flatten_ExtendValidPnt_1, Branches_Special);
                        IntDuoBranch_1 = Branches_Special.Where(item => Common.IsEqual(item.FlattenIntersectPoint, closetPnt1)).First();
                        if (IntDuoBranch_1 != null)
                        {
                            Branches_Special.Remove(IntDuoBranch_1);
                        }
                    }
                    catch (Exception)
                    { }
                }

                if (Flatten_ExtendValidPnt_2 != null)
                {
                    try
                    {
                        XYZ closetPnt2 = FindClosestPoint(Flatten_ExtendValidPnt_2, Branches_Special);
                        IntDuoBranch_2 = Branches_Special.Where(item => Common.IsEqual(item.FlattenIntersectPoint, closetPnt2)).First();
                        if (IntDuoBranch_2 != null)
                        {
                            Branches_Special.Remove(IntDuoBranch_2);
                        }
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        public XYZ FindClosestPoint(XYZ origin, List<DuoBranchPipe> points)
        {
            if (points == null || points.Count <= 0)
                return null;

            double minDistance = double.MaxValue;
            XYZ closestPoint = null;

            foreach (var point in points)
            {
                double distance = origin.DistanceTo(point.FlattenIntersectPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = point.FlattenIntersectPoint;
                }
            }

            return closestPoint;
        }
    }

    public static class PipeExtension
    {
        public static XYZ FlattenPoint(this XYZ point3d, double z = 0)
        {
            return new XYZ(point3d.X, point3d.Y, z);
        }

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

        public static Line GetFlattenCurve(this Element element)
        {
            Curve realCurve = element.GetCurve();
            Line retLine = Line.CreateBound(new XYZ(realCurve.GetEndPoint(0).X, realCurve.GetEndPoint(0).Y, 0), new XYZ(realCurve.GetEndPoint(1).X, realCurve.GetEndPoint(1).Y, 0));
            return retLine;
        }

        public static Line GetExpandFlattenCurve(this Element element, double expandVal = 0)
        {
            Curve realCurve = element.GetFlattenCurve();
            Line lineFromCurve = realCurve as Line;

            XYZ newStartPoint = lineFromCurve.Evaluate(lineFromCurve.GetEndParameter(0) - expandVal, false);
            XYZ newEndPoint = lineFromCurve.Evaluate(lineFromCurve.GetEndParameter(1) + expandVal, false);

            return Line.CreateBound(newStartPoint, newEndPoint);
        }
    }
}
