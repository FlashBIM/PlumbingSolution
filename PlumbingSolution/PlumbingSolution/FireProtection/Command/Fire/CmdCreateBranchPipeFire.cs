using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection;
using PlumbingSolution.FireProtection.Command.Fire.DiritConnectPipes.Service_B;
using PlumbingSolution.FireProtection.Command.Mep.MainPipe;
using PlumbingSolution.FireProtection.Command.Modify;
using PlumbingSolution.FireProtection.SelectionFilters;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.ProcessBarForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.Command.Drain;
using PlumbingSolution.FireProtection.Extensions;
using PlumbingSolution.FireProtection.ItemData;
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
using System.Text;
using System.Windows.Controls;
using System.Windows.Interop;
using DisableWarning = PlumbingSolution.FireProtection.Ultis.DisableWarning;
using ParameterUtils = PlumbingSolution.FireProtection.Ultis.ParameterUtils;

namespace PlumbingSolution.FireProtection.Command.Fire
{
    [Transaction(TransactionMode.Manual)]
    public class CmdCreateBranchPipeFire : IExternalCommand
    {
        public const string SCHEMA_IS_TEE_NAME = "IsTee";
        public static Guid SCHEMA_IS_TEE_GUID = Guid.Parse("5576072d-9d27-45de-834f-ad5b3f6437df");

        public const string SCHEMA_IS_ELEMENT_ORIGIN_NAME = "IsElementOrigin";
        public static Guid SCHEMA_IS_ELEMENT_ORIGIN_GUID = Guid.Parse("49695f15-fe99-41a2-8c7b-1f95e787b268");

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            if (!App.ShowCreateBranchPipeFireForm(commandData.Application))
            {
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }

        public static Result Process()
        {
            var form = App.m_CreateBranchPipeFireFrm;

            if (form != null && form.IsDisposed == false)
                form.Hide();

            try
            {
                Reference pipeRef = Global.UIDoc.Selection.PickObject(
                ObjectType.Element,
                new PipeSelectionFilter(),
                "Select one main pipe. Press ESC to cancel.");

                var mainPipe = Global.UIDoc.Document.GetElement(pipeRef) as Pipe;

                // BƯỚC 2: Quét chọn/Pick nhiều FamilyInstance
                IList<Reference> instanceRefs = Global.UIDoc.Selection.PickObjects(
                    ObjectType.Element,
                    new SprinklerSelectionFilter(),
                    "Select sprinklers, then click Finish (or ESC).");

                List<FamilyInstance> instances = new List<FamilyInstance>();
                foreach (Reference refId in instanceRefs)
                {
                    FamilyInstance inst = Global.UIDoc.Document.GetElement(refId) as FamilyInstance;
                    if (inst != null)
                    {
                        instances.Add(inst);
                    }
                }

                using (TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "Create Branch Pipe"))
                {
                    try
                    {
                        tranGr.Start();

                        var listSprinkler = GroupAndSortInstances(mainPipe, instances);

                        List<Pipe> lstBranchConnect = new List<Pipe>();

                        foreach (var sprinklers in listSprinkler)
                        {
                            using (Transaction tran = new Transaction(Global.UIDoc.Document, "Create"))
                            {
                                try
                                {
                                    tran.Start();
                                    Dictionary<double, int> dicSegment = new Dictionary<double, int>();
                                    if (IsLeftMainPipe(mainPipe, sprinklers.LastOrDefault()))
                                        dicSegment = CalculateSegmentSizes(sprinklers.Count, App.m_CreateBranchPipeFireFrm.ConfigData.LeftRules);
                                    else
                                        dicSegment = CalculateSegmentSizes(sprinklers.Count, App.m_CreateBranchPipeFireFrm.ConfigData.RightRules);
                                    List<PipeSegmentRange> ranges = CalculateRanges(dicSegment, sprinklers);
                                    var lstPipeBranch = CreateBranchPipe(mainPipe, sprinklers, ranges, ref lstBranchConnect);

                                    ConnectPipe(lstPipeBranch);

                                    tran.Commit();

                                    if (App.m_CreateBranchPipeFireFrm.ConfigData.IsConnectSprinkler)
                                    {
                                        switch (App.m_CreateBranchPipeFireFrm.ConfigData.TypeConnection)
                                        {
                                            case "Type 1":
                                                ConnectSprinklerType1(tran,
                                                                      lstPipeBranch,
                                                                      sprinklers,
                                                                      mainPipe.GetTypeId(),
                                                                      App.m_CreateBranchPipeFireFrm.ConfigData.Diameter,
                                                                      App.m_CreateBranchPipeFireFrm.ConfigData.IsElbow90);
                                                break;

                                            case "Type 2":
                                                ConnectSprinklerType2(tran, lstPipeBranch,
                                                                      sprinklers,
                                                                      App.m_CreateBranchPipeFireFrm.ConfigData.Diameter,
                                                                      !App.m_CreateBranchPipeFireFrm.ConfigData.IsElbow90);
                                                break;

                                            case "Type 3":
                                                ConnectSprinklerType3(tran, lstPipeBranch,
                                                                     sprinklers,
                                                                     App.m_CreateBranchPipeFireFrm.ConfigData.Diameter,
                                                                     !App.m_CreateBranchPipeFireFrm.ConfigData.IsElbow90);
                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    if (tran.HasStarted())
                                        tran.RollBack();
                                    continue;
                                }
                            }
                        }

                        using (Transaction tran = new Transaction(Global.UIDoc.Document, "Connect Branch Pipe"))
                        {
                            try
                            {
                                if (App.m_CreateBranchPipeFireFrm.ConfigData.IsConnectBranch)
                                {
                                    switch (App.m_CreateBranchPipeFireFrm.ConfigData.TypeConnectBranch)
                                    {
                                        case 1:
                                            ConnectBranch1T(tran, lstBranchConnect, mainPipe as MEPCurve);
                                            break;

                                        case 2:
                                            ConnectBranch1TE45(tran, lstBranchConnect, mainPipe as MEPCurve);
                                            break;

                                        case 3:
                                            ConnectBranch1TE90(tran, lstBranchConnect, mainPipe as MEPCurve);
                                            break;

                                        case 4:
                                            ConnectBranch2T(lstBranchConnect, mainPipe);
                                            break;

                                        default:
                                            break;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }

                        tranGr.Assimilate();
                    }
                    catch (Exception)
                    {
                        tranGr.RollBack();
                    }
                }
            }
            catch (Exception)
            {
            }

            form.Show();

            return Result.Succeeded;
        }

        public static bool ConnectBranch2T(List<Pipe> branchPipes, Pipe mainPipe)
        {
            try
            {
                List<ElementId> m_mainPipeIds = new List<ElementId>();
                List<Pipe> mainPipes = new List<Pipe>() { mainPipe };

                List<InforPie> inforMainPipes = new List<InforPie>();
                inforMainPipes.Add(new InforPie(mainPipe));

                // Filter branch pipes
                branchPipes = branchPipes.Where(item => (mainPipes.Find(item_1 => item_1.Id == item.Id) == null)).Select(item => item).ToList();

                // Pair pipe
                List<DuoBranchPipe> pairPies_1 = CmdTwoLevelSmart.PairingPipesPlus(branchPipes);
                if (pairPies_1 == null || pairPies_1.Count() <= 0)
                    return false;

                List<PairPipes> pairPies = CmdTwoLevelSmart.PairPipe(branchPipes);
                if (pairPies == null || pairPies.Count() <= 0)
                    return false;

                //if (App.m_ConnectBranch.DialogResultData.IsElevationDiffirence)
                {
                    if (CmdTwoLevelSmart.GetPreferredJunctionType(mainPipes[0]) == PreferredJunctionType.Tap /*&& App.m_ConnectBranch.DialogResultData.IsCheckedTeeTap*/)
                    {
                        ProcessWithTap(inforMainPipes, pairPies_1, mainPipe.GetTypeId(), App.m_CreateBranchPipeFireFrm.ConfigData.DiameterBranch, App.m_CreateBranchPipeFireFrm.ConfigData.IsElbow90Branch2T);
                    }
                    else if (CmdTwoLevelSmart.GetPreferredJunctionType(mainPipes[0]) == PreferredJunctionType.Tee /*&& App.m_ConnectBranch.DialogResultData.IsCheckedTeeTap*/)
                    {
                        ProcessWithTee(inforMainPipes, pairPies_1, mainPipe.GetTypeId(), App.m_CreateBranchPipeFireFrm.ConfigData.DiameterBranch, App.m_CreateBranchPipeFireFrm.ConfigData.IsElbow90Branch2T);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ProcessWithTee(List<InforPie> inforMainPipes, List<DuoBranchPipe> pairPies, ElementId pipeTypeId, double pipeSize, bool isElbow)
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
                                if (CmdTwoLevelSmart.RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt1
                                    };

                                    sourceMain.Branches.Add(duoBranchPipe);
                                }

                                if (CmdTwoLevelSmart.RealityIntersect(flatten_mainCurve_Extend, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt2))
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
                                if (CmdTwoLevelSmart.RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt1
                                    };

                                    sourceMain.Branches.Add(duoBranchPipe);
                                    sourceMain.Branches_Special.Add(duoBranchPipe);
                                }
                                else if (CmdTwoLevelSmart.RealityIntersect(flatten_mainCurve_Extend, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt2))
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

                    if (isElbow)
                        sourceMain.Initialize();
                    sourceMainPipes.Add(sourceMain);
                }

                pipeSize = pipeSize * Common.mmToFT;
                PipeType pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;
                foreach (var sourceMain in sourceMainPipes)
                {
                    using (Transaction reTrans = new Transaction(Global.UIDoc.Document, "STEP_1_TWO_LEVEL_SMART"))
                    {
                        reTrans.Start();
                        FailureHandlingOptions fhOpts = reTrans.GetFailureHandlingOptions();
                        List<DuoBranchPipe> allVerticalPipes = new List<DuoBranchPipe>();
                        List<DuoBranchPipe> norVerticalPipes = new List<DuoBranchPipe>();
                        if (CmdTwoLevelSmart.GetPreferredJunctionType(sourceMain.MainPipe.SourcePipe) == PreferredJunctionType.Tee)
                        {
                            // Create vertical Pipe
                            if (isElbow)
                            {
                                // Create elbow of 2 end of the line
                                if (sourceMain.IntDuoBranch_1 != null)
                                {
                                    Pipe verticalPipe_1 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_1, pipeTypeId, pipeSize);
                                    if (verticalPipe_1 != null)
                                    {
                                        DuoBranchPipe duoBranchPipe = sourceMain.IntDuoBranch_1.Clone() as DuoBranchPipe;
                                        duoBranchPipe.VerticalPipe = new InforPie(verticalPipe_1);
                                        allVerticalPipes.Add(duoBranchPipe);
                                    }
                                }

                                if (sourceMain.IntDuoBranch_2 != null)
                                {
                                    Pipe verticalPipe_2 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_2, pipeTypeId, pipeSize);
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
                                CreateTopTee(duoBranch, pipeSize);
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
                                        Pipe validMainPipe = CmdTwoLevelSmart.ProcessMainPipe(mainPipes, pipe1, out bool flagSplit, out XYZ intPnt);

                                        XYZ inter_main1 = null;
                                        XYZ p0_sub1 = null;
                                        XYZ p1_sub1 = null;
                                        XYZ inters_sub1 = null;
                                        Pipe main = null;

                                        foreach (ElementId pId in mainPipeIds)
                                        {
                                            var m = Global.UIDoc.Document.GetElement(pId) as Pipe;
                                            if (CmdTwoLevelSmart.DivideCase_2(m, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1) == true)
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

                                            CmdTwoLevelSmart.SetEntity(pipe1);
                                            CmdTwoLevelSmart.SetEntity(pipe2);
                                        }

                                        CmdTwoLevelSmart.DivideCase_2(main, pipe1, twoPipes, out inter_main1, out p0_sub1, out p1_sub1, out inters_sub1);

                                        XYZ inter_main2 = null;
                                        XYZ p0_sub2 = null;
                                        XYZ p1_sub2 = null;
                                        XYZ inters_sub2 = null;

                                        if (CmdTwoLevelSmart.DivideCase_2(main, pipe2, true, out inter_main2, out p0_sub2, out p1_sub2, out inters_sub2) == false)
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

                                        vertical.PipeType = pipeType;

                                        //vertical.LookupParameter("Diameter").Set(diamterPipe);

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

                                            var fitting = CmdTwoLevelSmart.CreateTee(main as Pipe, vertical as Pipe, inter_main1, out main2);
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
                                        if (CmdTwoLevelSmart.CompareDouble(pipeSize, pipe1.Diameter) && CmdTwoLevelSmart.CompareDouble(pipeSize, pipe2.Diameter))
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
                                            if (CmdTwoLevelSmart.CompareDouble(pipeSize, pipe1.Diameter) == false)
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
                                            if (CmdTwoLevelSmart.CompareDouble(pipeSize, pipe2.Diameter) == false)
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

                                                var mep1 = CmdTwoLevelSmart.ee(main1.AllRefs);
                                                var mep2 = CmdTwoLevelSmart.ee(main2.AllRefs);

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

                                                        CmdTwoLevelSmart.ll(pipe1, futher, p);

                                                        topReducer_1 = CreateFittingForMEPUtils.ctt(pipe1, pipe_moi_1);
                                                    }
                                                    else if (mep2 != null && pipe_moi_1.Id == mep2.Id)
                                                    {
                                                        var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                                        var p = line.Evaluate(d10, false);
                                                        (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                                        var futher = inters_sub1.DistanceTo(p0_sub1) > inters_sub1.DistanceTo(p1_sub1) ? p0_sub1 : p1_sub1;

                                                        CmdTwoLevelSmart.ll(pipe1, futher, p);

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

                                                        CmdTwoLevelSmart.ll(pipe2, futher, p);

                                                        topReducer_2 = CreateFittingForMEPUtils.ctt(pipe2, pipe_moi_2);
                                                    }
                                                    else if (mep2 != null && pipe_moi_2.Id == mep2.Id)
                                                    {
                                                        var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                                        var p = line.Evaluate(d10, false);
                                                        (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                                        var futher = inters_sub2.DistanceTo(p0_sub2) > inters_sub2.DistanceTo(p1_sub2) ? p0_sub2 : p1_sub2;

                                                        CmdTwoLevelSmart.ll(pipe2, futher, p);

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

        public static bool ProcessWithTap(List<InforPie> inforMainPipes, List<DuoBranchPipe> pairPies, ElementId pipeTypeId, double pipeSize, bool isElbow)
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
                                if (CmdTwoLevelSmart.RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
                                {
                                    DuoBranchPipe duoBranchPipe = new DuoBranchPipe(duoBranchs.FirstPipe, duoBranchs.SecondPipe)
                                    {
                                        FlattenIntersectPoint = flattenIntPnt1
                                    };

                                    sourceMain.Branches.Add(duoBranchPipe);
                                }

                                if (CmdTwoLevelSmart.RealityIntersect(flatten_mainCurve_Extend, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt2))
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
                                if (CmdTwoLevelSmart.RealityIntersect(flatten_mainCurve, duoBranchs.FlattenValidCurve, out XYZ flattenIntPnt1))
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
                    if (isElbow)
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
                        if (CmdTwoLevelSmart.GetPreferredJunctionType(sourceMain.MainPipe.SourcePipe) == PreferredJunctionType.Tap)
                        {
                            if (isElbow)
                            {
                                // Create elbow of 2 end of the line
                                if (sourceMain.IntDuoBranch_1 != null)
                                {
                                    Pipe verticalPipe_1 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_1, pipeTypeId, pipeSize);
                                    if (verticalPipe_1 != null)
                                    {
                                        DuoBranchPipe duoBranchPipe = sourceMain.IntDuoBranch_1.Clone() as DuoBranchPipe;
                                        duoBranchPipe.VerticalPipe = new InforPie(verticalPipe_1);
                                        allVerticalPipes.Add(duoBranchPipe);
                                    }
                                }

                                if (sourceMain.IntDuoBranch_2 != null)
                                {
                                    Pipe verticalPipe_2 = CreateVerticalPipeWithElbowLastBranch(sourceMain.MainPipe, sourceMain.IntDuoBranch_2, pipeTypeId, pipeSize);
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
                                Pipe verticalPipe = CreateVerticalPipeWithTap(sourceMain.MainPipe, duoBrandPipe, pipeTypeId, pipeSize);
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
                                if (!CmdTwoLevelSmart.CreateTap(sourceMain.MainPipe.SourcePipe as MEPCurve, verPipe.VerticalPipe.SourcePipe as MEPCurve))
                                {
                                    allVerticalPipes.Remove(allVerticalPipes.Where(item => item.VerticalPipe.SourcePipe.Id == verPipe.VerticalPipe.SourcePipe.Id).FirstOrDefault());
                                    Global.UIDoc.Document.Delete(verPipe.VerticalPipe.SourcePipe.Id);
                                }
                            }

                            Global.UIDoc.Document.Regenerate();

                            //Create Top Tee
                            foreach (var duoBranch in allVerticalPipes)
                            {
                                CreateTopTee(duoBranch, pipeSize);
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

        private static void CreateTopTee(DuoBranchPipe duoBranchPipe, double pipeSize)
        {
            pipeSize = pipeSize * Common.mmToFT;

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

            CmdTwoLevelSmart.GetInforBasePointTopTee(verticalPipe, pipe_1, twoPipes, out p0_sub1, out p1_sub1, out inters_sub1);
            if (duoBranchPipe.SecondPipe == null)
            {
                var temp_pipe1 = pipe_1;
                try
                {
                    var temp_pipe2_id = PlumbingUtils.BreakCurve(Global.UIDoc.Document, temp_pipe1.Id, inters_sub1);
                    var temp_pipe2 = Global.UIDoc.Document.GetElement(temp_pipe2_id) as Pipe;
                    pipe_1 = temp_pipe1;
                    pipe_2 = temp_pipe2;

                    CmdTwoLevelSmart.SetEntity(pipe_1);
                    CmdTwoLevelSmart.SetEntity(pipe_2);
                }
                catch (Exception)
                {
                    Global.UIDoc.Document.Delete(verticalPipe.Id);
                    return;
                }
            }

            CmdTwoLevelSmart.GetInforBasePointTopTee(verticalPipe, pipe_1, true, out p0_sub1, out p1_sub1, out inters_sub1);

            XYZ p0_sub2 = null;
            XYZ p1_sub2 = null;
            XYZ inters_sub2 = null;

            CmdTwoLevelSmart.GetInforBasePointTopTee(verticalPipe, pipe_2, true, out p0_sub2, out p1_sub2, out inters_sub2);

            if (inters_sub1.DistanceTo(inters_sub2) > 0.001)
            {
                return;
            }

            //Process for sub pipes
            var c5 = Common.GetConnectorClosestTo(verticalPipe, inters_sub1);

            double dMoi = 15 * Common.mmToFT;

            double d10 = 10 * Common.mmToFT;
            double dtemp = 2;

            if (CmdTwoLevelSmart.CompareDouble(pipeSize, pipe_1.Diameter) && CmdTwoLevelSmart.CompareDouble(pipeSize, pipe_2.Diameter))
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

                    if (CmdTwoLevelSmart.CreateTee(c3, c4, c5) == null)
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
                    if (CmdTwoLevelSmart.CompareDouble(pipeSize, pipe_1.Diameter) == false)
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

                    if (CmdTwoLevelSmart.CompareDouble(pipeSize, pipe_2.Diameter) == false)
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

                        var mep1 = CmdTwoLevelSmart.GetPipeFromConnector(main1.AllRefs);
                        var mep2 = CmdTwoLevelSmart.GetPipeFromConnector(main2.AllRefs);

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

                                CmdTwoLevelSmart.ll(pipe_1, futher, p);

                                topReducer_1 = CreateFittingForMEPUtils.ctt(pipe_1, pipe_moi_1);
                            }
                            else if (mep2 != null && pipe_moi_1.Id == mep2.Id)
                            {
                                var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                var p = line.Evaluate(d10, false);
                                (pipe_moi_1.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                var futher = inters_sub1.DistanceTo(p0_sub1) > inters_sub1.DistanceTo(p1_sub1) ? p0_sub1 : p1_sub1;

                                CmdTwoLevelSmart.ll(pipe_1, futher, p);

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

                                CmdTwoLevelSmart.ll(pipe_2, futher, p);

                                topReducer_2 = CreateFittingForMEPUtils.ctt(pipe_2, pipe_moi_2);
                            }
                            else if (mep2 != null && pipe_moi_2.Id == mep2.Id)
                            {
                                var line = Line.CreateUnbound(main2_p, v_m_2 * 10);

                                var p = line.Evaluate(d10, false);
                                (pipe_moi_2.Location as LocationCurve).Curve = Line.CreateBound(main2_p, p);

                                var futher = inters_sub2.DistanceTo(p0_sub2) > inters_sub2.DistanceTo(p1_sub2) ? p0_sub2 : p1_sub2;

                                CmdTwoLevelSmart.ll(pipe_2, futher, p);

                                topReducer_2 = CreateFittingForMEPUtils.ctt(pipe_2, pipe_moi_2);
                            }
                        }

                        reSubTrans.Commit();
                    }
                }
            }
        }

        private static Pipe CreateVerticalPipeWithElbowLastBranch(InforPie mainPipe, DuoBranchPipe duoBranchPipe, ElementId pipeTypeId, double pipeSize)
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

                using (SubTransaction reSubTrans = new SubTransaction(Global.UIDoc.Document))
                {
                    reSubTrans.Start();

                    PipeType pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;

                    CmdTwoLevelSmart.SetEntity(duoBranchPipe.FirstPipe?.SourcePipe);
                    CmdTwoLevelSmart.SetEntity(duoBranchPipe.SecondPipe?.SourcePipe);

                    var verticalPipe = Common.Clone(mainPipe.SourcePipe) as Pipe;

                    verticalPipe.PipeType = pipeType;

                    //verticalPipe.LookupParameter("Diameter").Set(diameter);

                    double diamter1 = (duoBranchPipe.FirstPipe != null && duoBranchPipe.FirstPipe.SourcePipe != null) ? duoBranchPipe.FirstPipe.SourcePipe.Diameter : 0.0;

                    double diamter2 = (duoBranchPipe.SecondPipe != null && duoBranchPipe.SecondPipe.SourcePipe != null) ? duoBranchPipe.SecondPipe.SourcePipe.Diameter : 0.0;

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

        private static Pipe CreateVerticalPipeWithTap(InforPie mainPipe, DuoBranchPipe duoBranchPipe, ElementId pipeTypeId, double pipeSize)
        {
            try
            {
                pipeSize = pipeSize * Common.mmToFT;

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

                    CmdTwoLevelSmart.SetEntity(duoBranchPipe.FirstPipe?.SourcePipe);
                    CmdTwoLevelSmart.SetEntity(duoBranchPipe.SecondPipe?.SourcePipe);

                    var verticalPipe = Common.Clone(mainPipe.SourcePipe) as Pipe;

                    PipeType pipeType = Global.UIDoc.Document.GetElement(pipeTypeId) as PipeType;

                    verticalPipe.PipeType = pipeType;

                    //verticalPipe.LookupParameter("Diameter").Set(diameter);

                    double diamter1 = (duoBranchPipe.FirstPipe != null && duoBranchPipe.FirstPipe.SourcePipe != null) ? duoBranchPipe.FirstPipe.SourcePipe.Diameter : 0.0;

                    double diamter2 = (duoBranchPipe.SecondPipe != null && duoBranchPipe.SecondPipe.SourcePipe != null) ? duoBranchPipe.SecondPipe.SourcePipe.Diameter : 0.0;

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

        public static bool ConnectBranch1TE45(Transaction tran, List<Pipe> lstPipe, MEPCurve mainPipe)
        {
            try
            {
                var lstMepCurve = lstPipe.Select(x => x as MEPCurve).ToList();

                CmdConnectBranchSameElevation.OrderBranchMEPCurve(mainPipe, ref lstMepCurve, out XYZ orgPoint);

                foreach (var branchMEPCurve in lstMepCurve)
                {
                    if (!CmdConnectBranchSameElevation.IsInSide(mainPipe, branchMEPCurve))
                        continue;

                    ConnectBranchTypeFitting(Global.UIDoc.Document, tran, ref mainPipe, branchMEPCurve, orgPoint);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ConnectBranch1TE90(Transaction tran, List<Pipe> lstPipe, MEPCurve mainPipe)
        {
            try
            {
                var lstMepCurve = lstPipe.Select(x => x as MEPCurve).ToList();

                CmdConnectBranchSameElevation.OrderBranchMEPCurve(mainPipe, ref lstMepCurve, out XYZ orgPoint);

                foreach (var branchMEPCurve in lstMepCurve)
                {
                    if (!CmdConnectBranchSameElevation.IsInSide(mainPipe, branchMEPCurve))
                        continue;

                    ConnectBranchTypeFitting(Global.UIDoc.Document, tran, ref mainPipe, branchMEPCurve, orgPoint, false);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ConnectBranchTypeFitting(Document doc, Transaction tran, ref MEPCurve mainMEPCurve,
MEPCurve branchMEPCurve, XYZ orgPoint, bool isElbow45 = true, List<ElementId> toDelete = null)
        {
            if (mainMEPCurve == null || branchMEPCurve == null)
                return false;

            try
            {
                List<Element> lstElementCreated = new List<Element>();

                tran.Start();
                FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                PlumbingSolution.FireProtection.Ultis.DisableWarning preproccessor = new PlumbingSolution.FireProtection.Ultis.DisableWarning();
                options.SetClearAfterRollback(true);
                options.SetFailuresPreprocessor(preproccessor);
                tran.SetFailureHandlingOptions(options);

                if (toDelete?.Count > 0)
                    doc.Delete(toDelete);

                ElementId systemTypeId = branchMEPCurve.MEPSystem.GetTypeId();
                CmdConnectE45.SplitMEPCurve(doc, ref mainMEPCurve, ref branchMEPCurve, false, true);

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

                if (CmdConnectBranchSameElevation.IsCreateTee(mainMEPCurve))
                {
                    MEPCurve splitMEPCurve = null;

                    fitting = CmdConnectBranchSameElevation.CreateTee(doc, mainMEPCurve, mEPCurveCheo, out splitMEPCurve);
                    if (fitting == null)
                        fitting = CmdConnectBranchSameElevation.CreateTeeWye(doc, mainMEPCurve, mEPCurveCheo, out splitMEPCurve);

                    mainMEPCurve = (splitMEPCurve == null) ? mainMEPCurve : CmdConnectBranchSameElevation.GetNextMEPCurve(mainMEPCurve, splitMEPCurve, orgPoint);
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

        public static bool ConnectBranch1T(Transaction tran, List<Pipe> lstPipe, MEPCurve mainPipe)
        {
            try
            {
                var lstMepCurve = lstPipe.Select(x => x as MEPCurve).ToList();

                CmdConnectBranchSameElevation.OrderBranchMEPCurve(mainPipe, ref lstMepCurve, out XYZ orgPoint);

                foreach (var branchMEPCurve in lstMepCurve)
                {
                    if (!CmdConnectBranchSameElevation.IsInSide(mainPipe, branchMEPCurve))
                        continue;

                    ConnectBranchTypeInheritElevation(Global.UIDoc.Document, tran, ref mainPipe, branchMEPCurve, orgPoint);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ConnectBranchTypeInheritElevation(Document doc, Transaction tran, ref MEPCurve mainMEPCurve,
  MEPCurve branchMEPCurve, XYZ orgPoint, List<ElementId> toDelete = null)
        {
            if (mainMEPCurve == null || branchMEPCurve == null)
                return false;

            List<Element> lstElementCreated = new List<Element>();

            try
            {
                tran.Start();

                if (toDelete?.Count > 0)
                    doc.Delete(toDelete);

                doc.Regenerate();

                ParameterUtils.SetValueParameterByBuiltIn(branchMEPCurve, BuiltInParameter.RBS_OFFSET_PARAM, ParameterUtils.GetValueParameterByBuilt(mainMEPCurve, BuiltInParameter.RBS_OFFSET_PARAM));

                ElementId systemTypeId = branchMEPCurve.MEPSystem.GetTypeId();

                CmdConnectBranchSameElevation.SplitMEPCurve(doc, ref mainMEPCurve, ref branchMEPCurve, false, true, false);
                doc.Regenerate();

                Line lineMain = ((LocationCurve)mainMEPCurve.Location).Curve as Line;
                Line lineBranch = ((LocationCurve)branchMEPCurve.Location).Curve as Line;

                List<Connector> connectorDuctBranchs = ConnectorUtils.ToList(branchMEPCurve.ConnectorManager);

                XYZ pointProject = Common.GetPointProjectOnLine(lineMain.Clone() as Line, connectorDuctBranchs[0].Origin);

                Connector conBranch1 = ConnectorUtils.GetConnectorNearest(pointProject, branchMEPCurve.ConnectorManager, out Connector conBranch2);

                FamilyInstance fitting = null;

                if (CmdConnectBranchSameElevation.IsCreateTee(mainMEPCurve))
                {
                    List<MEPCurve> lstMainMEPCurves = new List<MEPCurve>();

                    MEPCurve splitMEPCurve = null;

                    fitting = CmdConnectBranchSameElevation.CreateTee(doc, mainMEPCurve, branchMEPCurve, out splitMEPCurve);
                    if (fitting == null)
                        fitting = CmdConnectBranchSameElevation.CreateTeeWye(doc, mainMEPCurve, branchMEPCurve, out splitMEPCurve);

                    mainMEPCurve = (splitMEPCurve == null) ? mainMEPCurve : CmdConnectBranchSameElevation.GetNextMEPCurve(mainMEPCurve, splitMEPCurve, orgPoint);

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

        public static bool ConnectSprinklerType3(Transaction tran, List<Pipe> pipes, List<FamilyInstance> sprinklers, double pipeSize, bool isTeeTap)
        {
            try
            {
                // Get selected main pipe id
                List<ElementId> selPipeIds = pipes.Where(item => item.Id != ElementId.InvalidElementId).Select(item => item.Id).ToList();

                double invalidRadius_ft = Common.mmToFT * 1000;

                foreach (FamilyInstance sprinkler in sprinklers)
                {
                    HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                    try
                    {
                        tran.Start();
                        string dPercent = string.Empty;
                        // Location sprinkler
                        XYZ locSprinkler = (sprinkler.Location as LocationPoint).Point;

                        // Check valid connect
                        ConnectorSet cntSetOfIns = sprinkler.MEPModel.ConnectorManager.Connectors;

                        if (cntSetOfIns.Size == 0)
                        {
                            tran.RollBack();
                            continue;
                        }

                        Connector cntOfIns_1 = Common.ToList(sprinkler.MEPModel.ConnectorManager.Connectors).FirstOrDefault();

                        if (cntOfIns_1.IsConnected == true)
                        {
                            tran.RollBack();
                            continue;
                        }

                        // Find intersection with sprinkler
                        var cylindricalFromIns = Common.CreateCylindricalVolume(locSprinkler, invalidRadius_ft * 5, invalidRadius_ft, true);
                        if (cylindricalFromIns == null)
                        {
                            tran.RollBack();
                            continue;
                        }

                        FilteredElementCollector filterCollector = new FilteredElementCollector(Global.UIDoc.Document, selPipeIds).OfClass(typeof(Pipe)).WherePasses(new ElementIntersectsSolidFilter(cylindricalFromIns));
                        if (filterCollector == null || filterCollector.GetElementCount() <= 0)
                        {
                            tran.RollBack();
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
#if Debug_2027 || Release_2027 || Release_2026
    var intersectResult1 = curveExpand.Intersect(curveProcessPipe_crossProduct_2d, CurveIntersectResultOption.Detailed);
    inter = intersectResult1.Result;
#else
                            inter = curveExpand.Intersect(curveProcessPipe_crossProduct_2d, out intRetArr);
#endif

                            if (inter != SetComparisonResult.Overlap)
                            {
                                tran.RollBack();
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
                                tran.RollBack();
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
                                tran.RollBack();
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
                                tran.RollBack();
                                continue;
                            }

#if Debug_2027 || Release_2027 || Release_2026
    finalIntPnt = intersectResult4.GetOverlaps()[0].Point;
#else
                            finalIntPnt = intRetArr.get_Item(0).XYZPoint;
#endif

                            temp_processPipe_2 = null;
                            bool flagCreateTee = true;
                            if (CmdSprinklerDownright.GetPreferredJunctionType(processPipe) != PreferredJunctionType.Tee)
                            {
                                flagCreateTee = false;
                            }

                            CmdSprinklerDownright.ProcessStartSidePipe(processPipe, out temp_processPipe_2, finalIntPnt, out isDauOng, flagCreateTee);

                            if (temp_processPipe_2 != null)
                            {
                                selPipeIds.Add(temp_processPipe_2.Id);
                            }
                        }

                        //Set pipe size
                        var dPipeSizeFt = Common.mmToFT * pipeSize;

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

                            if (isTeeTap)
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
                                        tran.RollBack();
                                        continue;
                                    }
                                }
                            }
                            else
                            {
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
                        }
                        catch (System.Exception ex)
                        {
                            tran.RollBack();
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
                            tran.RollBack();
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
                            tran.RollBack();
                            continue;
                        }

                        CmdSprinklerDownright.SetJustification(pipe_v2, justification);
                        CmdSprinklerDownright.SetJustification(horizontal_pipe, justification);
                        CmdSprinklerDownright.SetJustification(processPipe, justification);
                        CmdSprinklerDownright.SetJustification(temp_processPipe_2, justification);
                        CmdSprinklerDownright.SetJustification(temp_processPipe_1, justification);

                        CmdDeleteSprinker.CreateSchema(sprinkler, listIdConnect.Select(x => x.ToInt().ToString()).ToList());

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.RollBack();
                        continue;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ConnectSprinklerType2(Transaction tran, List<Pipe> pipes, List<FamilyInstance> sprinklers, double pipeSize, bool isTeeTap)
        {
            try
            {
                var pipeIds = (from Pipe p in pipes
                               where p.Id != ElementId.InvalidElementId
                               select p.Id).ToList();

                var height = 150;

                var ft = Common.mmToFT * 1000;

                // Initialize progress bar

                //Find pipe
                foreach (FamilyInstance instance in sprinklers)
                {
                    HashSet<ElementId> listIdConnect = new HashSet<ElementId>();
                    try
                    {
                        tran.Start();
                        var sprinkle_point = (instance.Location as LocationPoint).Point;

                        //Check connect
                        var connects = instance.MEPModel.ConnectorManager.Connectors;

                        string dPercent = string.Empty;

                        var connect = Common.ToList(instance.MEPModel.ConnectorManager.Connectors).FirstOrDefault();

                        if (connect.IsConnected == true)
                        {
                            tran.RollBack();
                            continue;
                        }

                        var solid = Common.CreateCylindricalVolume(sprinkle_point, ft * 5, ft, true);
                        if (solid == null)
                        {
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
                        var pipe = CmdSprinklerDownright.ProcessPipes(pipeList.ToList(), sprinkle_point, out split);
                        (int, int) justification = CmdSprinklerDownright.GetJustification(pipe);
                        CmdSprinklerDownright.SetJustification(pipe, (0, 0));
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
                            if (CmdSprinklerDownright.GetPreferredJunctionType(pipe) != PreferredJunctionType.Tee)
                            {
                                flagCreateTee = false;
                            }

                            CmdSprinklerDownright.ProcessStartSidePipe(pipe, out pipe2, p, out isDauOng, flagCreateTee);

                            if (pipe2 != null)
                                pipeIds.Add(pipe2.Id);
                        }

                        //Set d = 25
                        Line tempLine = (pipe1.Location as LocationCurve).Curve as Line;

                        var dFt = Common.mmToFT * pipeSize;

                        var ft_h = Common.mmToFT * height;

                        newPlace = new XYZ(0, 0, 0);
                        elemIds = ElementTransformUtils.CopyElement(
                         Global.UIDoc.Document, pipe1.Id, newPlace);

                        var pipe_v1 = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]) as Pipe;

                        var line_v1 = Line.CreateUnbound(p, XYZ.BasisZ * ft_h * 2);

                        XYZ tmpPoint = line_v1.Evaluate(ft_h, false);

                        p = curve.Project(tmpPoint).XYZPoint;

                        if (!CmdSprinklerDownright.CheckPipeIsEnd1(pipe1, sprinklers, instance, sprinkle_point))
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

                            if (isTeeTap)
                            {
                                if (CmdSprinklerDownright.GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee && split)
                                {
                                    CmdSprinklerDownright.CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
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
                                    if (CmdSprinklerDownright.GetPreferredJunctionType(pipe1) != PreferredJunctionType.Tee)
                                    {
                                        if (CmdSprinklerDownright.CheckPipeIsEnd1(pipe1, sprinklers, instance, sprinkle_point))
                                            CmdSprinklerDownright.CreateTap(pipe1 as MEPCurve, pipe_v1 as MEPCurve, ref listIdConnect);
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

                        CmdSprinklerDownright.SetJustification(pipe, justification);
                        CmdSprinklerDownright.SetJustification(pipe1, justification);
                        CmdSprinklerDownright.SetJustification(pipe2, justification);
                        CmdSprinklerDownright.SetJustification(pipe_v1, justification);
                        CmdSprinklerDownright.SetJustification(pipe_v2, justification);
                        CmdSprinklerDownright.SetJustification(pipe_hor, justification);

                        Global.UIDoc.RefreshActiveView();

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.RollBack();
                        continue;
                    }
                }

                return true;
            }
            catch (Exception)
            { return false; }
            finally
            {
            }
        }

        private static bool ConnectSprinklerType1(Transaction tran, List<Pipe> lstPipe, List<FamilyInstance> sprinklers, ElementId pipeTypeId, double pipeSize, bool isElbow)
        {
            var dict = CmdSprinklerUpright.DictSprinker(lstPipe, sprinklers);

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

                        tran.Start();
                        try
                        {
                            CreateSprinklerUp(tran,
                                  sprinklers,
                                  instance,
                                  ref listPipeIds, isElbow, pipeSize, pipeTypeId,
                                  true);

                            tran.Commit();
                        }
                        catch (Exception ex)
                        {
                            tran.RollBack();
                            continue;
                        }
                    }
                }
            }
            catch (Exception)
            { }
            finally
            {
            }
            return true;
        }

        public static bool CreateSprinklerUp(Transaction tran, List<FamilyInstance> lstInstance, FamilyInstance instance, ref List<ElementId> selectedIds, bool isElbow, double pipeSize, ElementId pipeTypeId, bool isUp)
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

            var pipeCheck = selectedIds.Select(x => Global.UIDoc.Document.GetElement(x) as Pipe).ToList().FirstOrDefault(x => sr.IsRightPipe(instance, x));
            if (pipeCheck == null)
                return false;

            List<Element> pipeList = new List<Element>() { pipeCheck };

            FamilyInstance elbowFittingConnected1 = null;
            FamilyInstance elbowFittingConnected2 = null;

            foreach (Pipe pipe in pipeList)
            {
                List<Pipe> listPipeCut = new List<Pipe>();
                listPipeCut.Add(pipe);
                var isEnd = sr.CheckPipeIsEnd(pipe, lstInstance, instance, sprinkle_point);
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
                    if (isElbow)
                    {
                        if (isEnd)
                        {
                            if (sr.GetPreferredJunctionType(pipe) == PreferredJunctionType.Tee)
                            {
                                var curvePipeMain = GetCurve(pipe);

                                var curvePipeDung = GetCurve(newPipeZ);

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

                                var fitting = sr.CreateTeeFitting(pipe, newPipeZ, point, out Pipe pipe1);
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
    var intersectResult = curve.Intersect(GetCurve(newPipeZ), CurveIntersectResultOption.Detailed);
    result1 = intersectResult.Result;
#else
                                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                                IntersectionResultArray resultArray;
                                result1 = curve.Intersect(GetCurve(newPipeZ), out resultArray);
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
                                        var tap = sr.se(pipe as MEPCurve, newPipeZ as MEPCurve);
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
                    else
                    {
                        if (sr.GetPreferredJunctionType(pipe) == PreferredJunctionType.Tee)
                        {
                            var curvePipeMain = GetCurve(pipe);

                            var curvePipeDung = GetCurve(newPipeZ);

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

                            var fitting = sr.CreateTeeFitting(pipe, newPipeZ, point, out Pipe pipe1);
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
    var intersectResult = curve.Intersect(GetCurve(newPipeZ), CurveIntersectResultOption.Detailed);
    result1 = intersectResult.Result;
#else
                            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                            IntersectionResultArray resultArray;
                            result1 = curve.Intersect(GetCurve(newPipeZ), out resultArray);
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
                                    var tap = sr.se(pipe as MEPCurve, newPipeZ as MEPCurve);
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

                    if ((double)Common.GetValueParameterByBuilt(pipe, BuiltInParameter.RBS_PIPE_SLOPE) == 0 && !isElbow
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

        public static Curve GetCurve(Element element)
        {
            Debug.Assert(null != element.Location,
              "Expected an element with a valid Location");

            LocationCurve locationCurve = element.Location as LocationCurve;

            Debug.Assert(null != locationCurve,
              "Expected an element with a valid LocationCurve");

            return locationCurve.Curve;
        }

        private static XYZ GetSprinklerSideDirection(
     Pipe mainPipe,
     FamilyInstance sprinkler,
     Autodesk.Revit.DB.View view, bool isLeft = true)
        {
            if (mainPipe == null)
                throw new ArgumentNullException(nameof(mainPipe));

            if (sprinkler == null)
                throw new ArgumentNullException(nameof(sprinkler));

            if (view == null)
                throw new ArgumentNullException(nameof(view));

            if (!(mainPipe.Location is LocationCurve pipeLocation) ||
                !(pipeLocation.Curve is Line mainLine))
            {
                throw new InvalidOperationException(
                    "The main pipe is not straight.");
            }

            var locCurveMainPipe = mainPipe.Location as LocationCurve;

            XYZ sprinklerPoint = (sprinkler.Location as LocationPoint).Point;

            // Hai trục của màn hình.
            XYZ screenRight = view.RightDirection.Normalize();
            XYZ screenUp = view.UpDirection.Normalize();

            // Chuyển line chính sang hệ tọa độ màn hình 2D.
            XYZ pipeStart2D = ToScreen2D(
                mainLine.GetEndPoint(0),
                screenRight,
                screenUp);

            XYZ pipeEnd2D = ToScreen2D(
                mainLine.GetEndPoint(1),
                screenRight,
                screenUp);

            XYZ sprinklerPoint2D = ToScreen2D(
                sprinklerPoint,
                screenRight,
                screenUp);

            XYZ pipeDirection2D = NormalizeGlobalDirection((locCurveMainPipe.Curve as Line).Direction);

            if (pipeDirection2D.GetLength() < 1e-9)
            {
                throw new InvalidOperationException(
                    "Cannot determine the main pipe direction in this view.");
            }

            pipeDirection2D = pipeDirection2D.Normalize();

            /*
             * Project sprinkler lên đường thẳng chính trong hệ tọa độ màn hình.
             */

            Line lineUn2d = Line.CreateUnbound(pipeStart2D, pipeDirection2D);

            var result = lineUn2d.Project(sprinklerPoint2D);

            XYZ projectedPoint2D = new XYZ();
            if (result != null)
                projectedPoint2D = result.XYZPoint;

            /*
             * Hướng từ ống chính đến sprinkler trên màn hình.
             *
             * Trong hình của Linh:
             * sprinklerDirection2D gần bằng (0, 1, 0),
             * tức là hướng lên trên màn hình.
             */
            XYZ sprinklerDirection2D =
            NormalizeGlobalDirection((sprinklerPoint2D - projectedPoint2D).Normalize());

            if (sprinklerDirection2D.GetLength() < 1e-9)
            {
                throw new InvalidOperationException(
                    "The sprinkler lies on the main pipe axis, " +
                    "so left/right cannot be determined.");
            }

            /*
             * Xoay vector 90 độ trong hệ tọa độ màn hình.
             *
             * Vector (X, Y):
             *
             * Left  = (-Y, X)
             * Right = (Y, -X)
             */
            XYZ sideDirection2D;

            if (isLeft)
            {
                sideDirection2D = new XYZ(
                    -sprinklerDirection2D.Y,
                    sprinklerDirection2D.X,
                    0.0);
            }
            else
            {
                sideDirection2D = new XYZ(
                    sprinklerDirection2D.Y,
                    -sprinklerDirection2D.X,
                    0.0);
            }

            /*
             * Chuyển vector màn hình 2D trở lại vector XYZ của model.
             */
            XYZ modelDirection =
                screenRight.Multiply(sideDirection2D.X) +
                screenUp.Multiply(sideDirection2D.Y);

            if (modelDirection.GetLength() < 1e-9)
                return XYZ.Zero;

            return modelDirection.Normalize();
        }

        /// <summary>
        /// Chuyển điểm trong model sang tọa độ 2D của màn hình.
        ///
        /// X là hướng sang phải màn hình.
        /// Y là hướng lên trên màn hình.
        /// </summary>
        private static XYZ ToScreen2D(
            XYZ point,
            XYZ screenRight,
            XYZ screenUp)
        {
            return new XYZ(
                point.DotProduct(screenRight),
                point.DotProduct(screenUp),
                0.0);
        }

        private static bool IsLeftMainPipe(Pipe mainPipe, FamilyInstance sprinkler)
        {
            var locCurveMainPipe = mainPipe.Location as LocationCurve;
            var locSprinkler = sprinkler.Location as LocationPoint;

            if (locCurveMainPipe == null || locSprinkler == null)
                return false;

            var mainLine2d = RevitUtils.ProjectLineToPlane(locCurveMainPipe.Curve as Line);
            var sprinklerPoint2d = Common.To2D(locSprinkler.Point);

            var mainDir = NormalizeGlobalDirection((locCurveMainPipe.Curve as Line).Direction);

            var result = mainLine2d.Project(sprinklerPoint2d);
            if (result != null)
            {
                var dir = (sprinklerPoint2d - result.XYZPoint).Normalize();
                if (dir.DotProduct(mainDir.CrossProduct(XYZ.BasisZ)) < 0)
                {
                    return true; // Sprinkler nằm bên trái ống chính
                }
            }

            return false;
        }

        private static XYZ NormalizeGlobalDirection(
       XYZ direction)
        {
            bool isMoreVertical =
                Math.Abs(direction.Y) >= Math.Abs(direction.X);

            if (isMoreVertical)
            {
                return direction.Y < 0
                    ? direction.Negate()
                    : direction;
            }

            return direction.X < 0
                ? direction.Negate()
                : direction;
        }

        private static bool ConnectPipe(List<Pipe> lstPipeBranch)
        {
            for (int index = 0; index < lstPipeBranch.Count - 1; index++)
            {
                if (index == lstPipeBranch.Count - 1)
                    continue;

                Pipe currentResult =
                    lstPipeBranch[index];

                Pipe nextResult =
                    lstPipeBranch[index + 1];

                ConnectorUtils.GetConnectorClosedTo(currentResult.ConnectorManager, nextResult.ConnectorManager, out Connector con1, out Connector con2);

                if (con1 != null && con2 != null)
                {
                    Global.UIDoc.Document.Create.NewTransitionFitting(con1, con2);
                }
            }

            return true;
        }

        private static List<Pipe> CreateBranchPipe(Pipe mainPipe, List<FamilyInstance> sprinklers, List<PipeSegmentRange> ranges, ref List<Pipe> lstBranchConnect)
        {
            List<Pipe> retVal = new List<Pipe>();

            var locCurveMainPipe = mainPipe.Location as LocationCurve;

            double eleMainPipe = 0;
            var eleMainPipeParam = mainPipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
            if (eleMainPipeParam != null)
                eleMainPipe = eleMainPipeParam.AsDouble();

            XYZ currentPipeStartPoint = null;

            Line lineBranch2d = null;
            XYZ stP = new XYZ();
            var loc2d = RevitUtils.ProjectLineToPlane(locCurveMainPipe.Curve as Line);
            loc2d.MakeUnbound();

            XYZ dir = new XYZ();
            switch (App.m_CreateBranchPipeFireFrm.ConfigData.TypeLocation)
            {
                case 1: // Lệch tâm trái
                    dir = GetSprinklerSideDirection(mainPipe, sprinklers.FirstOrDefault(), Global.UIDoc.ActiveGraphicalView) * App.m_CreateBranchPipeFireFrm.ConfigData.OffsetDauPhun / 304.8;
                    break;

                case 2: // Lệch tâm phải
                    dir = GetSprinklerSideDirection(mainPipe, sprinklers.FirstOrDefault(), Global.UIDoc.ActiveGraphicalView, false) * App.m_CreateBranchPipeFireFrm.ConfigData.OffsetDauPhun / 304.8;
                    break;

                case 3: // Đồng tâm
                    dir = XYZ.Zero;
                    break;

                default:
                    break;
            }

            var sprinkStart2d = Common.To2D((sprinklers.FirstOrDefault().Location as LocationPoint).Point) + dir;
            var result = loc2d.Project(sprinkStart2d);
            if (result != null)
            {
                stP = new XYZ(result.XYZPoint.X, result.XYZPoint.Y, (sprinklers.FirstOrDefault().Location as LocationPoint).Point.Z);
                lineBranch2d = Line.CreateUnbound(sprinkStart2d, (sprinkStart2d - result.XYZPoint).Normalize());
            }

            int index = 0;
            foreach (PipeSegmentRange range in ranges)
            {
                XYZ startSprinklerPoint = new XYZ();
                XYZ endSprinklerPoint = new XYZ();

                if (range.StartIndex < 0)
                {
                    startSprinklerPoint = stP;
                }
                else
                {
                    FamilyInstance startSprinkler =
                    sprinklers[range.StartIndex];
                    startSprinklerPoint =
                 GetSprinklerPoint(startSprinkler, lineBranch2d);
                }

                FamilyInstance endSprinkler =
                    sprinklers[range.EndIndex];

                endSprinklerPoint =
                   GetSprinklerPoint(endSprinkler, lineBranch2d);

                /*
                 * Hướng offset luôn được xác định từ vị trí sprinkler đầu
                 * đến vị trí sprinkler cuối của range hiện tại.
                 *
                 * Không lấy hướng từ currentPipeStartPoint vì điểm đó
                 * có thể đã được offset từ đoạn trước.
                 */
                XYZ segmentDirection = GetDirection(
                    startSprinklerPoint,
                    endSprinklerPoint);

                /*
                 * Điểm cuối Pipe:
                 *
                 * endPoint =
                 * sprinklerEndPoint + direction * offset
                 */
                XYZ currentPipeEndPoint = new XYZ();

                if (index == ranges.Count - 1)
                    currentPipeEndPoint = endSprinklerPoint;
                else
                    currentPipeEndPoint =
                    endSprinklerPoint +
                    segmentDirection.Multiply(App.m_CreateBranchPipeFireFrm.ConfigData.OffsetCon / 304.8);

                /*
                 * Đoạn đầu bắt đầu tại sprinkler đầu.
                 *
                 * Các đoạn tiếp theo bắt đầu chính xác tại
                 * điểm cuối của đoạn trước.
                 */
                if (currentPipeStartPoint == null)
                    currentPipeStartPoint = startSprinklerPoint;

                if (currentPipeStartPoint.IsAlmostEqualTo(
                        currentPipeEndPoint))
                {
                    continue;
                }

                Pipe pipe = Pipe.Create(
                    Global.UIDoc.Document,
                    GetSystemTypeId(mainPipe),
                    mainPipe.GetTypeId(),
                    mainPipe.ReferenceLevel.Id,
                    currentPipeStartPoint,
                    currentPipeEndPoint);

                SetPipeDiameter(
                    pipe,
                    range.DiameterMm / 304.8);

                var eleParam = pipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                switch (App.m_CreateBranchPipeFireFrm.ConfigData.Elevation)
                {
                    case 1:

                        if (eleParam != null && !eleParam.IsReadOnly)
                            eleParam.Set(eleMainPipe);

                        break;

                    case 2:

                        if (eleParam != null && !eleParam.IsReadOnly)
                            eleParam.Set(eleMainPipe + App.m_CreateBranchPipeFireFrm.ConfigData.OffsetElevation / 304.8);

                        break;

                    default:
                        break;
                }

                if (index == 0)
                    lstBranchConnect.Add(pipe);

                retVal.Add(pipe);

                /*
                 * Điểm cuối đoạn hiện tại chính là điểm đầu
                 * của đoạn tiếp theo.
                 */
                currentPipeStartPoint = currentPipeEndPoint;
                index++;
            }

            return retVal;
        }

        public static ElementId GetSystemTypeId(Element element)
        {
            if (element == null)
                return ElementId.InvalidElementId;

            BuiltInParameter systemTypeParameter =
                        BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM;

            Parameter parameter =
                element.get_Parameter(systemTypeParameter);

            if (parameter == null ||
                parameter.StorageType != StorageType.ElementId)
            {
                return ElementId.InvalidElementId;
            }

            return parameter.AsElementId();
        }

        /// <summary>
        /// Tính vector hướng chuẩn hóa từ sprinkler đầu
        /// đến sprinkler cuối.
        /// </summary>
        private static XYZ GetDirection(
            XYZ startPoint,
            XYZ endPoint)
        {
            XYZ direction = endPoint - startPoint;

            if (direction.GetLength() < 1e-9)
            {
                throw new InvalidOperationException(
                    "Cannot compute the direction because the first and " +
                    "last sprinklers are at the same location.");
            }

            return direction.Normalize();
        }

        /// <summary>
        /// Lấy vị trí sprinkler.
        ///
        /// Ưu tiên Connector. Nếu không có Connector thì dùng LocationPoint.
        /// </summary>
        private static XYZ GetSprinklerPoint(
            FamilyInstance sprinkler, Line lineBranch)
        {
            var locP = sprinkler.Location as LocationPoint;

            if (lineBranch == null)
                return locP.Point;

            var sprinkStart2d = Common.To2D(locP.Point);
            var result = lineBranch.Project(sprinkStart2d);
            if (result != null)
            {
                return new XYZ(result.XYZPoint.X, result.XYZPoint.Y, locP.Point.Z);
            }
            if (sprinkler.Location is LocationPoint locationPoint)
                return locationPoint.Point;

            throw new InvalidOperationException(
                $"Cannot get the location of sprinkler Id: {sprinkler.Id}.");
        }

        /// <summary>
        /// Tính các khoảng sprinkler cho từng kích thước ống.
        ///
        /// Ví dụ:
        /// Ø80, Count = 2 => 0 -> 1
        /// Ø50, Count = 3 => 1 -> 3
        /// </summary>
        public static List<PipeSegmentRange> CalculateRanges(Dictionary<double, int> segmentRule, List<FamilyInstance> lstSprinkler)
        {
            var ranges = new List<PipeSegmentRange>();

            if (lstSprinkler.Count < 0)
                return ranges;

            int lastProcessedIndex = -1;

            foreach (var rule in segmentRule)
            {
                // Đã xử lý hết tất cả sprinkler.
                if (lastProcessedIndex >= lstSprinkler.Count - 1)
                    break;

                /*
                 * Sprinkler mới đầu tiên của rule hiện tại.
                 *
                 * Ví dụ:
                 * Rule trước kết thúc tại sprinkler 1
                 * thì rule hiện tại bắt đầu xử lý sprinkler 2.
                 */
                int firstCoveredIndex = lastProcessedIndex + 1;

                /*
                 * Index sprinkler cuối mà rule muốn xử lý.
                 *
                 * Ví dụ:
                 * firstCoveredIndex = 2
                 * SprinklerCount = 3
                 *
                 * requestedEndIndex = 2 + 3 - 1 = 4.
                 */
                int requestedEndIndex =
                    firstCoveredIndex + rule.Value - 1;

                // Nếu vượt danh sách thì dừng tại sprinkler cuối.
                int actualEndIndex = Math.Min(
                    requestedEndIndex,
                    lstSprinkler.Count - 1);

                /*
                 * Sprinkler dùng làm điểm tham chiếu hướng.
                 *
                 * Rule đầu:
                 * Không có sprinkler trước đó nên dùng
                 * InitialPipeStartPoint để tính hướng.
                 *
                 * Các rule tiếp theo:
                 * Dùng sprinkler cuối của rule trước làm sprinkler start.
                 */
                int directionStartIndex = lastProcessedIndex;

                ranges.Add(
                    new PipeSegmentRange(
                        rule.Key, rule.Value, directionStartIndex,
                        actualEndIndex
                        ));

                /*
                 * Sprinkler cuối đoạn trước là sprinkler bắt đầu
                 * để xác định hướng cho đoạn tiếp theo.
                 *
                 * Ví dụ:
                 * 0 -> 1
                 * 1 -> 3
                 */
                lastProcessedIndex = actualEndIndex;
            }

            return ranges;
        }

        private static void SetPipeDiameter(Pipe pipe, double diameterFeet)
        {
            Parameter diaParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (diaParam != null && !diaParam.IsReadOnly)
            {
                diaParam.Set(diameterFeet);
            }
        }

        /// <summary>
        /// Tính toán kích thước ống chạy xuôi và trả về Dictionary
        /// Key (int): Vị trí đoạn ống / số thứ tự đầu phun (bắt đầu từ 0)
        /// Value (double): Kích thước ống (Pipe Size)
        /// </summary>
        private static Dictionary<double, int> CalculateSegmentSizes(int totalHeads, List<PipeSizingRule> rules)
        {
            // Đảo lại: Key = PipeSize, Value = Số lượng đầu phun (SprinklerCount)
            Dictionary<double, int> segmentCounts = new Dictionary<double, int>();

            // Biến lưu số đầu phun còn lại cần xử lý
            int remainingHeads = totalHeads;

            foreach (var rule in rules)
            {
                if (remainingHeads <= 0) break;

                // Lấy số nhỏ hơn giữa "Sức chứa của Rule" và "Số đầu phun còn lại"
                int headsToTake = Math.Min(rule.SprinklerCount, remainingHeads);

                if (segmentCounts.ContainsKey(rule.PipeSize))
                {
                    segmentCounts[rule.PipeSize] += headsToTake;
                }
                else
                {
                    segmentCounts.Add(rule.PipeSize, headsToTake);
                }

                // Trừ đi số lượng đã gán cho đoạn này
                remainingHeads -= headsToTake;
            }

            // Xử lý ngoại lệ: Nếu đã duyệt hết bảng Rule mà vẫn còn dư đầu phun
            // Lấy size cuối cùng (nhỏ nhất) gánh nốt toàn bộ phần còn lại
            if (remainingHeads > 0)
            {
                double smallestSize = rules.LastOrDefault()?.PipeSize ?? 25.0;
                if (segmentCounts.ContainsKey(smallestSize))
                {
                    segmentCounts[smallestSize] += remainingHeads;
                }
                else
                {
                    segmentCounts.Add(smallestSize, remainingHeads);
                }
            }

            return segmentCounts;
        }

        /// <summary>
        /// Hàm chia nhóm và sắp xếp Sprinkler theo từng nhánh rẽ
        /// </summary>
        public static List<List<FamilyInstance>> GroupAndSortInstances(Pipe mainPipe, List<FamilyInstance> instances)
        {
            List<List<FamilyInstance>> finalGroups = new List<List<FamilyInstance>>();

            // 1. Lấy trục tâm của ống chính
            LocationCurve pipeLoc = mainPipe.Location as LocationCurve;
            Line boundPipeLine = pipeLoc?.Curve as Line;
            if (boundPipeLine == null) return finalGroups;

            // Tạo đường thẳng vô hạn từ ống chính để chiếu điểm
            XYZ pipeStartPoint = boundPipeLine.GetEndPoint(0);
            XYZ pipeDirection = boundPipeLine.Direction;
            Line unboundPipeLine = Line.CreateUnbound(Common.To2D(pipeStartPoint), pipeDirection);

            // Đổi 50mm sang hệ Feet của Revit
            double toleranceFeet = 1000.0 / 304.8;

            // 2. Thu thập và tiền xử lý dữ liệu hình học
            List<SprinklerData> dataList = new List<SprinklerData>();
            foreach (var inst in instances)
            {
                LocationPoint locPoint = inst.Location as LocationPoint;
                if (locPoint == null) continue;

                XYZ point = Common.To2D(locPoint.Point);
                IntersectionResult result = unboundPipeLine.Project(point);
                if (result == null) continue;

                XYZ projectedPoint = result.XYZPoint;
                XYZ vectorFromPipe = point - projectedPoint;
                double distance = vectorFromPipe.GetLength();

                // Lấy vector hướng chuẩn hóa (hướng đâm ra của nhánh)
                XYZ direction = distance > 1e-6 ? vectorFromPipe.Normalize() : XYZ.Zero;

                dataList.Add(new SprinklerData
                {
                    Instance = inst,
                    Point = point,
                    Direction = direction,
                    DistanceToPipe = distance
                });
            }

            // 3. Phân nhóm
            List<List<SprinklerData>> groupedData = new List<List<SprinklerData>>();

            foreach (var data in dataList)
            {
                bool isAddedToGroup = false;

                if (!instances.Select(x => x.Id.ToInt()).Contains(data.Instance.Id.ToInt()))
                    continue;

                var lstData = new List<SprinklerData>();

                lstData.Add(data); // Thêm bản thân vào nhóm tạm)

                foreach (var group in dataList)
                {
                    if (group.Instance.Id.ToInt() == data.Instance.Id.ToInt())
                        continue;

                    SprinklerData refData = group; // Điểm mốc của nhóm

                    // ĐIỀU KIỆN 1: CÙNG HƯỚNG
                    bool isSameDirection = false;
                    if (data.DistanceToPipe <= 1e-6 && refData.DistanceToPipe <= 1e-6)
                    {
                        isSameDirection = true; // Trùng khít trên ống
                    }
                    else
                    {
                        // Tích vô hướng > 0.999 nghĩa là cùng hướng đâm ra (dung sai rất nhỏ)
                        double dotProduct = data.Direction.DotProduct(refData.Direction);
                        if (dotProduct > 0.999)
                        {
                            isSameDirection = true;
                        }
                    }

                    if (isSameDirection)
                    {
                        // ĐIỀU KIỆN 2: THẲNG HÀNG DỌC THEO NHÁNH (DUNG SAI 50mm)
                        XYZ branchDir = refData.Direction;

                        // Xử lý an toàn: Nếu mốc nằm ngay tâm ống (Vector Zero), lấy tạm vector vuông góc với ống
                        if (branchDir.IsAlmostEqualTo(XYZ.Zero))
                        {
                            XYZ up = XYZ.BasisZ;
                            if (pipeDirection.IsAlmostEqualTo(XYZ.BasisZ) || pipeDirection.IsAlmostEqualTo(-XYZ.BasisZ))
                                up = XYZ.BasisY;
                            branchDir = pipeDirection.CrossProduct(up).Normalize();
                        }

                        // Kẻ đường thẳng mốc chạy dọc theo NHÁNH (vuông góc với ống chính)
                        Line refLine = Line.CreateUnbound(refData.Point, branchDir);
                        IntersectionResult projOnRef = refLine.Project(data.Point);

                        if (projOnRef != null)
                        {
                            double offsetDistance = data.Point.DistanceTo(projOnRef.XYZPoint);

                            if (offsetDistance <= toleranceFeet) // Cách trục nhánh <= 50mm
                            {
                                lstData.Add(group);
                                instances.Remove(data.Instance); // Loại bỏ đã xét
                                instances.Remove(group.Instance); // Loại bỏ đã xét
                            }
                        }
                    }
                }

                groupedData.Add(lstData);
            }

            // 4. Sắp xếp trong nội bộ nhóm và xuất kết quả
            foreach (var group in groupedData)
            {
                // Sắp xếp theo DistanceToPipe tăng dần (từ gần ống chính nhất đến xa nhất)
                var sortedGroup = group.OrderBy(d => d.DistanceToPipe)
                                       .Select(d => d.Instance)
                                       .ToList();

                finalGroups.Add(sortedGroup);
            }

            return finalGroups;
        }
    }
}
