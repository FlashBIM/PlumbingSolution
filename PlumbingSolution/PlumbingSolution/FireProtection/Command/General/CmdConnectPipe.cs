using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlumbingSolution.FireProtection.Command.Drain;
using PlumbingSolution.FireProtection.Command.Fire;
using PlumbingSolution.FireProtection.Command.Mep.MainPipe;
using PlumbingSolution.FireProtection.UI.GeneralUI;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.LoginLicense.Gate;
using System.Diagnostics;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.Command.General
{
    /// <summary>
    /// Connect Pipe: một form chọn Route Type + Connection Type (+ Direction), OK thì chạy
    /// tool nối tương ứng của Dirit (giữ nguyên logic từng tool):
    ///   Branch   - Elbow 45 / Elbow 90 / Same Elevation  : Nối ngang Tê 1T1E45 / 1T1E90 / 1T
    ///   Elbow    - Elbow 45 / Elbow 90 / Same Elevation  : Nối ngang Elbow 1E45-1E90 / 2E90 / 1E
    ///   Parallel - Elbow 45 / Elbow 90                   : Nối song song 1T-1E90-1E45 / 1T-2E90
    ///   Parallel - Same Elevation - Horizontal / Vertical: Nối song song 1T-1E (Plan) / (Elevation)
    ///   Straight : chờ ghép từ SMEP.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CmdConnectPipe : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            IExternalCommand tool;
            using (var form = new FrmConnectPipe())
            {
                var owner = new WindowHandle(Process.GetCurrentProcess().MainWindowHandle);
                if (form.ShowDialog(owner) != DialogResult.OK)
                    return Result.Cancelled;

                tool = GetTool(form.RouteType, form.FittingType, form.Direction);
            }

            if (tool == null)
                return Result.Cancelled;

            return tool.Execute(commandData, ref message, elements);
        }

        private static IExternalCommand GetTool(ConnectRouteType route, ConnectFittingType fitting, ConnectDirection direction)
        {
            switch (route)
            {
                case ConnectRouteType.Branch:
                    if (fitting == ConnectFittingType.Elbow45) return new CmdConnect1T1E45();
                    if (fitting == ConnectFittingType.Elbow90) return new CmdConnect1T1E90();
                    return new CmdConnectBranchSameElevation();

                case ConnectRouteType.Elbow:
                    if (fitting == ConnectFittingType.Elbow45) return new CmdConnectE45();
                    if (fitting == ConnectFittingType.Elbow90) return new CmdConnectE90();
                    return new CmdConnectElbowTypeSameElevation();

                case ConnectRouteType.Parallel:
                    if (fitting == ConnectFittingType.Elbow45) return new CmdConnectParallel45Deg();
                    if (fitting == ConnectFittingType.Elbow90) return new CmdConnectParallel90Deg();
                    if (direction == ConnectDirection.Vertical) return new CmdConnectParallel();
                    return new CmdConnectParallelSameElevation();

                default:
                    return null;
            }
        }
    }
}
