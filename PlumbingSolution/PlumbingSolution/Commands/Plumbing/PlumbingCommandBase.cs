using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlumbingSolution.LoginLicense.Gate;

namespace PlumbingSolution.Commands.Plumbing
{
    /// <summary>
    /// Lớp nền cho mọi lệnh trên ribbon PlumbingSolution: kiểm tra license TRƯỚC rồi mới
    /// chạy Run. Ribbon mờ không chặn được phím tắt, Dynamo hay macro — cổng này mới chặn.
    /// Lệnh nào chưa viết thì để Run mặc định: báo "đang phát triển".
    /// </summary>
    public abstract class PlumbingCommandBase : IExternalCommand
    {
        /// <summary>Tên hiển thị trên ribbon, dùng cho tiêu đề thông báo.</summary>
        protected abstract string Title { get; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            return Run(commandData.Application, ref message, elements);
        }

        protected virtual Result Run(UIApplication uiapp, ref string message, ElementSet elements)
        {
            TaskDialog.Show(Title, "Chức năng \"" + Title + "\" đang được phát triển.");
            return Result.Succeeded;
        }
    }
}
