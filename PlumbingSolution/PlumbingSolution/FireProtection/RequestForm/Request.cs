using PlumbingSolution.FireProtection.ItemData;
using System.Collections.Generic;
using Revit = Autodesk.Revit;

namespace PlumbingSolution.FireProtection.RequestForm
{
    public enum RequestId : int
    {
        None = -1,

        //Service A1
        PlaceVerticalPipe,

        ConnectFlange,
        CreateBlindFlange,
        CreateBranchPipeFire,

        PinUpinElement,
        HideUnhideElement,

        //Service A2
        CreateVerticalPipe,

        //Service B
        VerticalPipe1Y1E,

        CreateSectionBox,
        ConnectWC,
        HorizontalPipeConnection2,

        //Service D1
        PipeVertical1_1Y3E,

        //Service D2
        PipeVertical2_1Y2E,

        //Service D3
        PipeVertical3_1Y3E,

        //Service F
        PipeVentilation2_1Y2E,

        //Service G1
        Ventilation1_2Y,

        //Service G2
        Ventilation2_4Y,

        //Service G5
        HorizontalVentilationTee,

        //Service I1
        CreatePipeEndCap,

        //Service I2
        CreateUnclogVertical,

        //Service I3
        CreateUnclogPipe,

        //Service J1
        CreatePipeByAccesssories,

        //Service J3
        CreatePipeTee,

        //Service J7
        ChangeToSiphon,

        //Service B3
        ConnectBranch,

        //Service E1
        SprinklerUp_Aplly,

        //Service E2
        SprinklerDownType1_RUN,

        SprinklerDownType2_RUN,
        SprinklerDownType3_RUN,
        SprinklerDownType4_RUN,
        SprinklerDownType5_RUN,

        //Service E2
        FlexSprinker_RUN,

        TwinSprinker_RUN,

        //Service H1
        PlaceValve,

        //Service H2
        CreateNipple,

        //Service I4
        QuickPlaceTee,

        //Service I5
        QuickPlaceElbow,

        //Service I6
        ExtendPipe,

        //Service e7
        DuctConnectionBranchInherit,

        //Service H2
        CreateNippe,

        //Service H4
        CreateFlange,

        //Service H6
        CreateAdapter,

        //Service I2
        CreateCoupling,

        PlaceGrill,
        DuctConnection,
        CableTrayConnection,
        CreateTapDuct,
        CreateGrillConnection,
        AutoElevation,
        AutoElevationUp,
        ConnectParallel,
        AutoElevationDown,
        AutoElevationMoveUp,
        AutoElevationMoveDown,
        AutoElevationMoveLeft,
        AutoElevationMoveRight,
        ConnectMEP,
        CreateParallelMEPFromLine,
        CreateParallelMEPUp,
        CreateParallelMEPDown,
        ReverseParallelMEP,
        ChangeConnect,
        StraightHorizontal,
        StraightVertical,
        CreateTap,
        ConnectTransition,
        HolyUpDown,
        HolyLeftRight,

        HolyUpDown_UpStep,
        HolyUpDown_DownStep,

        HolyUpDown_MoveFitting_Minus,
        HolyUpDown_MoveFitting_Plus,
        HolyLeftRight_UpStep,
        HolyLeftRight_DownStep,
        ApplyMovePositive,
        ApplyMoveNegative,
        ApplyRotate,
        ApplyRotatePositive,
        ApplyRotatePositiveFrmCreateTee,
        ApplyRotateNegative,
        ApplyRotateNegativeFrmCreateTee,
        CopyGroup,
        ApplyAlign,
        DiffuserConnection,
        PlaceDamper,
        PickFamily,
        LoadFamily,
        VerticalPipe,

        VerticalVentilation_1Y1E,
        VerticalVentilation_2Y2E,

        RotateAccessoryAdd,
        RotateAccessorySub,
        MepDistance,
        InstanceDistance,

        PickLine,
        ConnectVerticalMep_1Y1E,
        ConnectPump,
        DeletePump,
        ConnectVerticalMep_2E,
        HorizontalConnection3,
        HorizontalConnection4,
    }

    public class Request
    {
        // Member variables


        public Revit.DB.ElementId _selectedTag = Revit.DB.ElementId.InvalidElementId;

        // Storing the value as a plain Int makes using the interlocking mechanism simpler
        private int _request = (int)RequestId.None;

        public FamilyData fmlData;
        public List<string> lstFolderFml;
        public string selectedCategory;


        //Member Functions


        public RequestId Take()
        {
            return (RequestId)System.Threading.Interlocked.Exchange(ref _request, (int)RequestId.None);
        }

        public void Make(RequestId request)
        {
            System.Threading.Interlocked.Exchange(ref _request, (int)request);
        }

    }
}
