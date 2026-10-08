using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI;
using PlumbingSolution.FireProtection.RequestForm;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.Service_A;
using PlumbingSolution.FireProtection.UI.Service_E;
using PlumbingSolution.FireProtection.UI.Service_J;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;

namespace PlumbingSolution.FireProtection
{
    /// <summary>
    /// Phần App của Dirit mà các tool đầu phun cần (form modeless + mở form), ghép từ
    /// AddinDiritProject.App. Đặt trong namespace FireProtection nên code Dirit gọi "App.xxx"
    /// tự trỏ về đây, không đụng tới PlumbingSolution.App (ribbon/license).
    /// </summary>
    public static class App
    {
        /// <summary>Cột ngôn ngữ trong Language\Language.csv: 1 = English, 2 = Tiếng Việt.</summary>
        public static int LanguageIndex => 1;

        public static WindowHandle hWndRevit = null;

        public static UI_ConnectSprinkle m_ConnectSprinkleForm = null;
        public static UI_SprinklerDown m_SprinklerDownForm = null;
        public static UI_FlexSprinkler m_FlexSprinklerForm = null;
        public static UI_TwinSprinkler m_TwinSprinklerForm = null;
        public static UI_PlaceVerticalPipe m_PlaceVerticalPipeForm = null;
        public static FrmCreateBranchPipeFire m_CreateBranchPipeFireFrm = null;

        private static void EnsureRevitWindow()
        {
            if (null == hWndRevit)
                hWndRevit = new WindowHandle(Process.GetCurrentProcess().MainWindowHandle);
        }

        public static bool ShowConnectSprinkleForm()
        {
            try
            {
                EnsureRevitWindow();
                if (m_ConnectSprinkleForm == null || m_ConnectSprinkleForm.IsDisposed)
                {
                    RequestHandler handler = new RequestHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    m_ConnectSprinkleForm = new UI_ConnectSprinkle(exEvent, handler);
                    m_ConnectSprinkleForm.Show(hWndRevit);
                }
                else if (Common.IsFormSameOpen(m_ConnectSprinkleForm.Name))
                {
                    return false;
                }

                DisplayService.SetFocus(new HandleRef(null, m_ConnectSprinkleForm.Handle));
                return true;
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return false;
            }
        }

        public static bool ShowSprinklerDownForm()
        {
            try
            {
                EnsureRevitWindow();
                if (m_SprinklerDownForm == null || m_SprinklerDownForm.IsDisposed)
                {
                    RequestHandler handler = new RequestHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    m_SprinklerDownForm = new UI_SprinklerDown(Global.UIDoc.Document, exEvent, handler);
                    m_SprinklerDownForm.Show(hWndRevit);
                }
                else if (Common.IsFormSameOpen(m_SprinklerDownForm.Name))
                {
                    return false;
                }

                DisplayService.SetFocus(new HandleRef(null, m_SprinklerDownForm.Handle));
                return true;
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return false;
            }
        }

        public static bool ShowFlexSprinklerForm()
        {
            try
            {
                EnsureRevitWindow();
                if (m_FlexSprinklerForm == null || m_FlexSprinklerForm.IsDisposed)
                {
                    RequestHandler handler = new RequestHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    m_FlexSprinklerForm = new UI_FlexSprinkler(exEvent, handler);
                    m_FlexSprinklerForm.Show(hWndRevit);
                }
                else if (Common.IsFormSameOpen(m_FlexSprinklerForm.Name))
                {
                    return false;
                }

                DisplayService.SetFocus(new HandleRef(null, m_FlexSprinklerForm.Handle));
                return true;
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return false;
            }
        }

        public static bool ShowCreateBranchPipeFireForm(UIApplication uiapp)
        {
            try
            {
                EnsureRevitWindow();
                if (m_CreateBranchPipeFireFrm == null || m_CreateBranchPipeFireFrm.IsDisposed)
                {
                    RequestHandler handler = new RequestHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    m_CreateBranchPipeFireFrm = new FrmCreateBranchPipeFire(exEvent, handler);
                    m_CreateBranchPipeFireFrm.Show(hWndRevit);
                }

                DisplayService.SetFocus(new HandleRef(null, m_CreateBranchPipeFireFrm.Handle));
                return true;
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return false;
            }
        }

        public static bool ShowTwinSprinklerForm()
        {
            try
            {
                EnsureRevitWindow();
                if (m_TwinSprinklerForm == null || m_TwinSprinklerForm.IsDisposed)
                {
                    RequestHandler handler = new RequestHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    m_TwinSprinklerForm = new UI_TwinSprinkler(exEvent, handler);
                    m_TwinSprinklerForm.Show(hWndRevit);
                }
                else if (Common.IsFormSameOpen(m_TwinSprinklerForm.Name))
                {
                    return false;
                }

                DisplayService.SetFocus(new HandleRef(null, m_TwinSprinklerForm.Handle));
                return true;
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return false;
            }
        }

        public static bool ShowPlaceVerticalPipeForm()
        {
            try
            {
                EnsureRevitWindow();
                if (m_PlaceVerticalPipeForm == null || m_PlaceVerticalPipeForm.IsDisposed)
                {
                    RequestHandler handler = new RequestHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    m_PlaceVerticalPipeForm = new UI_PlaceVerticalPipe(exEvent, handler);
                    m_PlaceVerticalPipeForm.Show(hWndRevit);
                }
                else if (Common.IsFormSameOpen(m_PlaceVerticalPipeForm.Name))
                {
                    return false;
                }

                DisplayService.SetFocus(new HandleRef(null, m_PlaceVerticalPipeForm.Handle));
                return true;
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return false;
            }
        }
    }
}
