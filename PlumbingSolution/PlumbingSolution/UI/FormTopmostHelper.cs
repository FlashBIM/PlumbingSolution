using System.Windows.Forms;

namespace PlumbingSolution.UI
{
    /// <summary>
    /// Đưa 1 form của tool lên ĐỈNH của "topmost band" để dialog mở SAU luôn nằm TRÊN dialog mở TRƯỚC.
    /// Các form của tool đều đặt TopMost=true, nhưng khi có ≥2 form cùng topmost thì form show sau KHÔNG
    /// tự nhảy lên đỉnh band ⇒ bị che. Toggle TopMost false→true buộc SetWindowPos(HWND_TOPMOST) đặt cửa sổ
    /// lên đầu band — hoạt động cả khi form được show từ ExternalEvent (nơi Activate/SetForegroundWindow
    /// có thể bị Windows từ chối vì thiếu quyền foreground).
    /// </summary>
    internal static class FormTopmostHelper
    {
        public static void BringToTop(Form f)
        {
            if (f == null || f.IsDisposed || !f.IsHandleCreated) return;
            try
            {
                if (f.WindowState == FormWindowState.Minimized)
                    f.WindowState = FormWindowState.Normal;
                f.BringToFront();
                // Re-assert TOPMOST (false→true) để nhảy lên đỉnh band. Toggle bắt buộc vì gán cùng giá trị (true→true)
                // sẽ bị WinForms bỏ qua, không phát lại SetWindowPos.
                f.TopMost = false;
                f.TopMost = true;
                f.Activate();
            }
            catch { /* best-effort */ }
        }
    }
}
