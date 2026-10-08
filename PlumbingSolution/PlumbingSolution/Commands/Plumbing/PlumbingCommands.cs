using Autodesk.Revit.Attributes;

namespace PlumbingSolution.Commands.Plumbing
{
    // ---- Panel Fire Protection ----
    [Transaction(TransactionMode.Manual)]
    public class CmdHosereelConnect : PlumbingCommandBase { protected override string Title => "Hosereel Connect"; }

    // ---- Panel Pipe Connect ----
    [Transaction(TransactionMode.Manual)]
    public class CmdVerticalPipe : PlumbingCommandBase { protected override string Title => "Vertical Pipe"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdConnectPipe : PlumbingCommandBase { protected override string Title => "Connect Pipe"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdParallelPipe : PlumbingCommandBase { protected override string Title => "Parallel Pipe"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdChangeConnect : PlumbingCommandBase { protected override string Title => "Change Connect"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdBranchAlign : PlumbingCommandBase { protected override string Title => "Branch Align"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdElbowAlign : PlumbingCommandBase { protected override string Title => "Elbow Align"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdTrimStraight : PlumbingCommandBase { protected override string Title => "Trim Straight"; }

    // ---- Panel Auto Route ----
    [Transaction(TransactionMode.Manual)]
    public class CmdAutoChiller : PlumbingCommandBase { protected override string Title => "Auto Chiller"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdAutoWS : PlumbingCommandBase { protected override string Title => "Auto WS"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdPipeRoute : PlumbingCommandBase { protected override string Title => "Pipe Route"; }

    // ---- Panel Pipe Fittings ----
    [Transaction(TransactionMode.Manual)]
    public class CmdConnectPump : PlumbingCommandBase { protected override string Title => "Connect Pump"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdPGConnect : PlumbingCommandBase { protected override string Title => "PG Connect"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdValveAssembly : PlumbingCommandBase { protected override string Title => "Valve Assembly"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdPlaceValve : PlumbingCommandBase { protected override string Title => "Place Valve"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdCreateFlange : PlumbingCommandBase { protected override string Title => "Create Flange"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdCreateCoupling : PlumbingCommandBase { protected override string Title => "Create Coupling"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdCreateTee : PlumbingCommandBase { protected override string Title => "Create Tee"; }

    // ---- Panel QUICK ----
    [Transaction(TransactionMode.Manual)]
    public class CmdPipeUpdown : PlumbingCommandBase { protected override string Title => "Pipe Updown"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdDeleteFitting : PlumbingCommandBase { protected override string Title => "Delete Fitting"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdCreatePipe : PlumbingCommandBase { protected override string Title => "Create Pipe"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdQuickPipe : PlumbingCommandBase { protected override string Title => "Quick Pipe"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdConnectFittings : PlumbingCommandBase { protected override string Title => "Connect Fittings"; }

    [Transaction(TransactionMode.Manual)]
    public class CmdHMEPDistance : PlumbingCommandBase { protected override string Title => "H.MEP Distance"; }
}
