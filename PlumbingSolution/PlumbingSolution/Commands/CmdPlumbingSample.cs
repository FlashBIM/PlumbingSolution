using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlumbingSolution.LoginLicense.Gate;

namespace PlumbingSolution.Commands
{
    /// <summary>
    /// Lệnh mẫu cho thấy cách gắn cổng license. MỌI lệnh mới của PlumbingSolution phải mở
    /// đầu Execute bằng LicenseGate.IsToolAllowed(this) — ribbon mờ không chặn được phím tắt,
    /// Dynamo hay macro.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CmdPlumbingSample : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            TaskDialog.Show("Plumbing Solution", "License hợp lệ — lệnh đã chạy.");
            return Result.Succeeded;
        }
    }
}
