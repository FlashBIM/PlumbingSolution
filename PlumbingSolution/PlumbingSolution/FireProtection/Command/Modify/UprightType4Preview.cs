using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using PlumbingSolution.FireProtection.Extensions;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    /// <summary>
    /// Chế độ Preview của Upright Type 4: vẽ ống tạm (DirectContext3D, không phải phần tử Revit) cho mọi đầu phun đã chọn.
    /// Rê chuột gần một đầu → đầu đó sáng cam, click để chọn làm đầu gốc; sau đó con trỏ lên/xuống đổi A (bước 10 mm),
    /// các đầu khác đi theo cùng A. Click → tạo ống thật, Tab → chọn lại đầu gốc, Esc → hủy.
    /// Revit API không có sự kiện di chuột nên mỗi lần Idling đọc vị trí con trỏ (Win32) rồi quy đổi sang tọa độ model
    /// qua UIView.GetZoomCorners; chỉ refresh view khi trạng thái đổi.
    /// Chỉ chạy trong view 3D trực giao hoặc section/elevation (ở mặt bằng con trỏ lên/xuống không phải cao độ).
    /// </summary>
    internal class UprightType4Preview : IDirectContext3DServer
    {
        private static readonly Guid ServerId = new Guid("6B0E3F4A-2C5D-4E8B-9A71-3F2D8C4B5E60");
        private static UprightType4Preview s_active;

        private static readonly ColorWithTransparency ColRoot = new ColorWithTransparency(255, 140, 0, 0);
        private static readonly ColorWithTransparency ColFollow = new ColorWithTransparency(90, 190, 185, 0);
        private static readonly ColorWithTransparency ColBad = new ColorWithTransparency(225, 60, 60, 0);
        private static readonly ColorWithTransparency ColDim = new ColorWithTransparency(220, 30, 30, 0);

        private const double SnapMm = 10;
        private const double HitPixels = 25;

        private readonly UIApplication m_uiapp;
        private readonly Document m_doc;
        private readonly ElementId m_viewId;
        private readonly List<FamilyInstance> m_sprinklers;
        private readonly List<ElementId> m_mainIds;
        private readonly ElementId m_pipeTypeId;
        private readonly double m_sizeFt;
        private readonly List<CmdSprinklerUpright.Type4Plan> m_plans;   // chỉ các đầu có connector
        private readonly Outline m_outline;
        private readonly IntPtr m_revitHwnd;

        private readonly object m_lock = new object();
        private bool m_pickRoot = true;
        private int m_root = -1;
        private int m_hover = -1;
        private double m_aFt;
        private int m_version;
        private int m_builtVersion = -1;
        private readonly List<GpuBuffer> m_buffers = new List<GpuBuffer>();

        private bool m_prevLButton, m_prevEsc, m_prevTab;
        private readonly PreviewTip m_tip;

        private UprightType4Preview(UIApplication uiapp, View view, List<FamilyInstance> sprinklers, List<ElementId> mainIds,
                                    ElementId pipeTypeId, double sizeFt, List<CmdSprinklerUpright.Type4Plan> plans, double aFt)
        {
            m_uiapp = uiapp;
            m_doc = view.Document;
            m_viewId = view.Id;
            m_sprinklers = sprinklers;
            m_mainIds = mainIds;
            m_pipeTypeId = pipeTypeId;
            m_sizeFt = sizeFt;
            m_plans = plans;
            m_aFt = aFt;
            m_revitHwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            m_tip = new PreviewTip(m_revitHwnd);

            double margin = plans.Max(p => p.Radius) * 12 + 1;
            XYZ min = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            XYZ max = new XYZ(double.MinValue, double.MinValue, double.MinValue);
            foreach (var p in plans)
            {
                foreach (XYZ q in new[] { p.Head, p.Tee ?? p.Head })
                {
                    min = new XYZ(Math.Min(min.X, q.X), Math.Min(min.Y, q.Y), Math.Min(min.Z, q.Z));
                    max = new XYZ(Math.Max(max.X, q.X), Math.Max(max.Y, q.Y), Math.Max(max.Z, q.Z));
                }
            }
            m_outline = new Outline(min - new XYZ(margin, margin, margin), max + new XYZ(margin, margin, margin));
        }

        // ------------------------------------------------------------------ vòng đời

        public static bool Start(UIApplication uiapp, List<FamilyInstance> sprinklers, List<ElementId> mainIds,
                                 ElementId pipeTypeId, double sizeFt, double aFt)
        {
            if (s_active != null)
                s_active.Finish(false);

            UIDocument uidoc = uiapp.ActiveUIDocument;
            View view = uidoc.ActiveView;
            if (!IsSupportedView(view))
            {
                TaskDialog.Show("Upright Sprinkler",
                    "Preview works in an orthographic 3D view or a section/elevation view.\n" +
                    "Switch to one of those views and run again, or untick Preview to enter A.");
                return false;
            }

            Document doc = uidoc.Document;
            var plans = new List<CmdSprinklerUpright.Type4Plan>();
            var skipped = new List<string>();
            foreach (FamilyInstance s in sprinklers)
            {
                CmdSprinklerUpright.Type4Plan plan;
                Autodesk.Revit.DB.Plumbing.Pipe main;
                string reason = CmdSprinklerUpright.PlanType4(doc, s, mainIds, pipeTypeId, sizeFt, out plan, out main);
                if (plan.Head != null && plan.Tee != null)
                    plans.Add(plan);
                if (reason != null)
                    skipped.Add(s.Id.ToInt() + ": " + reason);
            }

            if (!plans.Any(p => p.Reason == null))
            {
                TaskDialog.Show("Upright Sprinkler", "No sprinkler can be connected.\n" + string.Join("\n", skipped.Take(15)));
                return false;
            }

            var preview = new UprightType4Preview(uiapp, view, sprinklers, mainIds, pipeTypeId, sizeFt, plans, aFt);

            var service = (MultiServerService)ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
            if (service.IsRegisteredServerId(ServerId))
                service.RemoveServer(ServerId);
            service.AddServer(preview);
            IList<Guid> active = service.GetActiveServerIds();
            active.Add(ServerId);
            service.SetActiveServers(active);

            s_active = preview;
            uiapp.Idling += preview.OnIdling;
            uidoc.RefreshActiveView();
            return true;
        }

        private static bool IsSupportedView(View view)
        {
            if (view == null || Math.Abs(view.ViewDirection.Z) > 0.99)
                return false;
            if (view is View3D v3)
                return !v3.IsPerspective;
            return view.ViewType == ViewType.Section || view.ViewType == ViewType.Elevation || view.ViewType == ViewType.Detail;
        }

        /// <summary>Gỡ server, bỏ Idling, đóng tooltip. Hủy (không commit) thì hiện lại form ngay.</summary>
        private void Finish(bool committing)
        {
            if (s_active != this)
                return;
            s_active = null;

            m_uiapp.Idling -= OnIdling;
            try
            {
                var service = (MultiServerService)ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
                IList<Guid> active = service.GetActiveServerIds();
                active.Remove(ServerId);
                service.SetActiveServers(active);
                if (service.IsRegisteredServerId(ServerId))
                    service.RemoveServer(ServerId);
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
            }

            lock (m_lock)
            {
                foreach (GpuBuffer b in m_buffers)
                    b.Dispose();
                m_buffers.Clear();
            }

            m_tip.Close();
            m_tip.Dispose();
            m_uiapp.ActiveUIDocument?.RefreshActiveView();

            if (!committing)
                CmdSprinklerUpright.ShowType4Form();
        }

        private void Commit()
        {
            double aFt = m_aFt;
            Finish(true);

            try
            {
                CmdSprinklerUpright.CreateType4(m_doc, m_sprinklers, m_mainIds, m_pipeTypeId, m_sizeFt, aFt);
                m_uiapp.ActiveUIDocument?.Selection.SetElementIds(new List<ElementId>());
                App.m_ConnectSprinkleForm?.SetHeightA(aFt / Common.mmToFT);
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
            }
            finally
            {
                CmdSprinklerUpright.ShowType4Form();
            }
        }

        // ------------------------------------------------------------------ con trỏ / phím

        private void OnIdling(object sender, IdlingEventArgs e)
        {
            e.SetRaiseWithoutDelay();
            try
            {
                Tick();
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                Finish(false);
            }
        }

        private void Tick()
        {
            UIDocument uidoc = m_uiapp.ActiveUIDocument;
            if (uidoc == null || !uidoc.Document.Equals(m_doc) || uidoc.ActiveView.Id != m_viewId)
            {
                Finish(false);   // đổi view / đổi file → thoát preview
                return;
            }

            View view = uidoc.ActiveView;
            UIView uiview = uidoc.GetOpenUIViews().FirstOrDefault(v => v.ViewId == m_viewId);
            if (uiview == null)
            {
                Finish(false);
                return;
            }

            bool revitActive = RootOwner(Native.GetForegroundWindow()) == m_revitHwnd;
            bool esc = revitActive && KeyDown(Native.VK_ESCAPE);
            bool tab = revitActive && KeyDown(Native.VK_TAB);
            bool lb = KeyDown(Native.VK_LBUTTON);
            bool escPressed = esc && !m_prevEsc;
            bool tabPressed = tab && !m_prevTab;
            bool released = !lb && m_prevLButton;
            m_prevEsc = esc;
            m_prevTab = tab;
            m_prevLButton = lb;

            if (escPressed)
            {
                Finish(false);
                return;
            }

            Native.POINT pt;
            Native.GetCursorPos(out pt);
            Rectangle rect = uiview.GetWindowRectangle();
            bool inside = revitActive
                          && pt.X > rect.Left && pt.X < rect.Right && pt.Y > rect.Top && pt.Y < rect.Bottom
                          && RootOwner(Native.WindowFromPoint(pt)) == m_revitHwnd;
            if (!inside)
            {
                m_tip.Hide();
                return;
            }

            // Con trỏ → điểm trên mặt phẳng view (model), tia nhìn theo -ViewDirection.
            IList<XYZ> corners = uiview.GetZoomCorners();
            XYZ right = view.RightDirection, up = view.UpDirection, toEye = view.ViewDirection;
            XYZ diag = corners[1] - corners[0];
            double wFt = diag.DotProduct(right), hFt = diag.DotProduct(up);
            double fx = (pt.X - rect.Left) / (double)(rect.Right - rect.Left);
            double fy = (rect.Bottom - pt.Y) / (double)(rect.Bottom - rect.Top);
            XYZ p = corners[0] + right * (fx * wFt) + up * (fy * hFt);
            double pxPerFt = (rect.Right - rect.Left) / Math.Abs(wFt);

            bool changed = false;
            if (m_pickRoot)
            {
                int hover = HitTest(p, right, up, pxPerFt);
                if (released && hover >= 0)
                {
                    m_root = hover;
                    m_pickRoot = false;
                    m_aFt = Clamp(m_plans[m_root], m_aFt);
                    changed = true;
                }
                else if (hover != m_hover)
                {
                    m_hover = hover;
                    changed = true;
                }
            }
            else if (tabPressed)
            {
                m_pickRoot = true;
                m_hover = m_root;
                changed = true;
            }
            else
            {
                double a = AFromCursor(m_plans[m_root], p, toEye);
                if (!double.IsNaN(a))
                {
                    a = Clamp(m_plans[m_root], a);
                    if (Math.Abs(a - m_aFt) > 1e-9)
                    {
                        m_aFt = a;
                        changed = true;
                    }
                }

                if (released)
                {
                    Commit();
                    return;
                }
            }

            UpdateTip(pt);
            if (changed)
            {
                lock (m_lock)
                    m_version++;
                uidoc.RefreshActiveView();
            }
        }

        /// <summary>Đầu phun có ống tạm gần con trỏ nhất (theo pixel trên màn hình), -1 nếu không có.</summary>
        private int HitTest(XYZ cursor, XYZ right, XYZ up, double pxPerFt)
        {
            double cu = cursor.DotProduct(right), cv = cursor.DotProduct(up);
            int best = -1;
            double bestPx = HitPixels;
            for (int i = 0; i < m_plans.Count; i++)
            {
                if (m_plans[i].Reason != null)
                    continue;
                List<XYZ> pts = m_plans[i].Points(Clamp(m_plans[i], m_aFt));
                for (int k = 0; k < pts.Count - 1; k++)
                {
                    double d = SegmentDistance(cu, cv, pts[k].DotProduct(right), pts[k].DotProduct(up),
                                               pts[k + 1].DotProduct(right), pts[k + 1].DotProduct(up)) * pxPerFt;
                    if (d < bestPx)
                    {
                        bestPx = d;
                        best = i;
                    }
                }
            }
            return best;
        }

        private static double SegmentDistance(double px, double py, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax, dy = by - ay;
            double len2 = dx * dx + dy * dy;
            double t = len2 < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((px - ax) * dx + (py - ay) * dy) / len2));
            double ex = ax + t * dx - px, ey = ay + t * dy - py;
            return Math.Sqrt(ex * ex + ey * ey);
        }

        /// <summary>A theo cao độ con trỏ: cắt tia nhìn với mặt phẳng đứng qua tee của đầu gốc, vuông góc hướng nhìn.</summary>
        private static double AFromCursor(CmdSprinklerUpright.Type4Plan root, XYZ p, XYZ toEye)
        {
            XYZ n = new XYZ(toEye.X, toEye.Y, 0);
            if (n.GetLength() < 1e-6)
                return double.NaN;
            n = n.Normalize();
            XYZ d = toEye.Negate();
            double den = d.DotProduct(n);
            if (Math.Abs(den) < 1e-9)
                return double.NaN;
            double t = (root.Tee - p).DotProduct(n) / den;
            return p.Z + t * d.Z - root.Tee.Z;
        }

        private static double Clamp(CmdSprinklerUpright.Type4Plan plan, double aFt)
        {
            double step = SnapMm * Common.mmToFT;
            double a = Math.Round(aFt / step) * step;
            double min = Math.Ceiling(plan.MinA / step) * step;
            double max = Math.Floor(plan.MaxA / step) * step;
            return max < min ? plan.MinA : Math.Max(min, Math.Min(max, a));
        }

        private void UpdateTip(Native.POINT pt)
        {
            if (m_pickRoot)
            {
                m_tip.ShowAt(pt.X, pt.Y, "Pick root sprinkler",
                             "Hover a sprinkler and click to set it as root\nEsc: cancel");
                return;
            }

            int ok = m_plans.Count(pl => CmdSprinklerUpright.CheckA(pl, m_aFt) == null);
            m_tip.ShowAt(pt.X, pt.Y, string.Format("A = {0:0} mm", m_aFt / Common.mmToFT),
                         string.Format("Connectable {0}/{1}\nClick: create   Tab: change root   Esc: cancel", ok, m_sprinklers.Count));
        }

        private IntPtr RootOwner(IntPtr hwnd) => hwnd == IntPtr.Zero ? IntPtr.Zero : Native.GetAncestor(hwnd, Native.GA_ROOTOWNER);

        private static bool KeyDown(int vk) => (Native.GetAsyncKeyState(vk) & 0x8000) != 0;

        // ------------------------------------------------------------------ IDirectContext3DServer

        public Guid GetServerId() => ServerId;
        public ExternalServiceId GetServiceId() => ExternalServices.BuiltInExternalServices.DirectContext3DService;
        public string GetName() => "PlumbingSolution Upright Sprinkler Preview";
        public string GetVendorId() => "FlashBIM";
        public string GetDescription() => "Live preview of Upright Sprinkler Type 4 connections.";
        public string GetApplicationId() => "";
        public string GetSourceId() => "";
        public bool UsesHandles() => false;
        public bool UseInTransparentPass(View dBView) => false;
        public Outline GetBoundingBox(View dBView) => m_outline;

        public bool CanExecute(View dBView) => s_active == this && dBView.Id == m_viewId && dBView.Document.Equals(m_doc);

        public void RenderScene(View dBView, DisplayStyle displayStyle)
        {
            try
            {
                lock (m_lock)
                {
                    if (m_builtVersion != m_version || m_buffers.Count == 0)
                    {
                        foreach (GpuBuffer b in m_buffers)
                            b.Dispose();
                        m_buffers.Clear();
                        Build(dBView.RightDirection);
                        m_builtVersion = m_version;
                    }

                    foreach (GpuBuffer b in m_buffers)
                        b.Flush();
                }
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
            }
        }

        // ------------------------------------------------------------------ dựng lưới

        private void Build(XYZ viewRight)
        {
            var mesh = new MeshBuilder(m_buffers);
            int highlight = m_pickRoot ? m_hover : m_root;

            for (int i = 0; i < m_plans.Count; i++)
            {
                var plan = m_plans[i];
                double a = plan.Reason == null ? Clamp(plan, m_aFt) : m_aFt;
                bool bad = CmdSprinklerUpright.CheckA(plan, m_pickRoot ? a : m_aFt) != null;
                ColorWithTransparency col = i == highlight ? ColRoot : bad ? ColBad : ColFollow;
                double r = plan.Radius;
                List<XYZ> pts = plan.Points(m_pickRoot ? a : m_aFt);

                for (int k = 0; k < pts.Count - 1; k++)
                    mesh.Cylinder(pts[k], pts[k + 1], r, col);
                mesh.Sphere(pts[0], r * 1.6, col);          // tee
                mesh.Sphere(pts[1], r * 1.3, col);          // co
                mesh.Sphere(pts[2], r * 1.3, col);          // co
                mesh.Sphere(pts[3], r * 1.15, col);         // nối đầu phun

                if (i == highlight)
                {
                    // Vòng sáng quanh đầu gốc.
                    mesh.Circle(plan.Head + XYZ.BasisZ * r, r * 5, ColRoot);
                    mesh.Circle(plan.Head + XYZ.BasisZ * r, r * 6.5, ColRoot);
                    mesh.Circle(plan.Head + XYZ.BasisZ * r, r * 6.6, ColRoot);
                }

                if (!m_pickRoot && i == m_root)
                {
                    // Kích thước A cạnh ống đứng của đầu gốc.
                    XYZ off = viewRight.Negate() * (r * 9);
                    XYZ b0 = pts[0] + off, b1 = pts[1] + off;
                    mesh.Line(b0, b1, ColDim);
                    mesh.Line(pts[0] + off * 0.4, pts[0] + off * 1.3, ColDim);
                    mesh.Line(pts[1] + off * 0.4, pts[1] + off * 1.3, ColDim);
                    double h = Math.Min(r * 3, (b1 - b0).GetLength() / 3);
                    XYZ side = viewRight * (r * 1.2);
                    mesh.Line(b1, b1 - XYZ.BasisZ * h + side, ColDim);
                    mesh.Line(b1, b1 - XYZ.BasisZ * h - side, ColDim);
                    mesh.Line(b0, b0 + XYZ.BasisZ * h + side, ColDim);
                    mesh.Line(b0, b0 + XYZ.BasisZ * h - side, ColDim);
                }
            }

            mesh.Flush();
        }

        /// <summary>Gom tam giác/đường vào buffer, tự cắt khi gần giới hạn chỉ số 16 bit.</summary>
        private class MeshBuilder
        {
            private const int MaxVertices = 60000;
            private const int Segments = 16;
            private readonly List<GpuBuffer> m_out;
            private readonly List<VertexPositionNormalColored> m_tv = new List<VertexPositionNormalColored>();
            private readonly List<IndexTriangle> m_tri = new List<IndexTriangle>();
            private readonly List<VertexPositionColored> m_lv = new List<VertexPositionColored>();
            private readonly List<IndexLine> m_lines = new List<IndexLine>();

            public MeshBuilder(List<GpuBuffer> output)
            {
                m_out = output;
            }

            private void Reserve(int n)
            {
                if (m_tv.Count + n > MaxVertices)
                    FlushTriangles();
            }

            public void Cylinder(XYZ a, XYZ b, double r, ColorWithTransparency col)
            {
                XYZ axis = b - a;
                if (axis.GetLength() < 1e-6)
                    return;
                XYZ w = axis.Normalize();
                XYZ u = w.CrossProduct(Math.Abs(w.Z) < 0.9 ? XYZ.BasisZ : XYZ.BasisX).Normalize();
                XYZ v = w.CrossProduct(u);

                Reserve((Segments + 1) * 2);
                int start = m_tv.Count;
                for (int i = 0; i <= Segments; i++)
                {
                    double ang = 2 * Math.PI * i / Segments;
                    XYZ dir = u * Math.Cos(ang) + v * Math.Sin(ang);
                    m_tv.Add(new VertexPositionNormalColored(a + dir * r, dir, col));
                    m_tv.Add(new VertexPositionNormalColored(b + dir * r, dir, col));
                }
                for (int i = 0; i < Segments; i++)
                {
                    int i0 = start + i * 2;
                    m_tri.Add(new IndexTriangle(i0, i0 + 2, i0 + 1));
                    m_tri.Add(new IndexTriangle(i0 + 1, i0 + 2, i0 + 3));
                }
            }

            public void Sphere(XYZ c, double r, ColorWithTransparency col)
            {
                const int lat = 8, lon = 16;
                Reserve((lat + 1) * (lon + 1));
                int start = m_tv.Count;
                for (int i = 0; i <= lat; i++)
                {
                    double th = Math.PI * i / lat;
                    for (int j = 0; j <= lon; j++)
                    {
                        double ph = 2 * Math.PI * j / lon;
                        XYZ n = new XYZ(Math.Sin(th) * Math.Cos(ph), Math.Sin(th) * Math.Sin(ph), Math.Cos(th));
                        m_tv.Add(new VertexPositionNormalColored(c + n * r, n, col));
                    }
                }
                for (int i = 0; i < lat; i++)
                {
                    for (int j = 0; j < lon; j++)
                    {
                        int a = start + i * (lon + 1) + j, b = a + lon + 1;
                        m_tri.Add(new IndexTriangle(a, b, a + 1));
                        m_tri.Add(new IndexTriangle(a + 1, b, b + 1));
                    }
                }
            }

            public void Line(XYZ a, XYZ b, ColorWithTransparency col)
            {
                int start = m_lv.Count;
                m_lv.Add(new VertexPositionColored(a, col));
                m_lv.Add(new VertexPositionColored(b, col));
                m_lines.Add(new IndexLine(start, start + 1));
            }

            public void Circle(XYZ c, double r, ColorWithTransparency col)
            {
                const int n = 48;
                for (int i = 0; i < n; i++)
                {
                    double a0 = 2 * Math.PI * i / n, a1 = 2 * Math.PI * (i + 1) / n;
                    Line(c + new XYZ(Math.Cos(a0), Math.Sin(a0), 0) * r, c + new XYZ(Math.Cos(a1), Math.Sin(a1), 0) * r, col);
                }
            }

            public void Flush()
            {
                FlushTriangles();
                if (m_lines.Count > 0)
                {
                    m_out.Add(GpuBuffer.Lines(m_lv, m_lines));
                    m_lv.Clear();
                    m_lines.Clear();
                }
            }

            private void FlushTriangles()
            {
                if (m_tri.Count == 0)
                    return;
                m_out.Add(GpuBuffer.Triangles(m_tv, m_tri));
                m_tv.Clear();
                m_tri.Clear();
            }
        }

        private class GpuBuffer : IDisposable
        {
            private VertexBuffer m_vb;
            private IndexBuffer m_ib;
            private VertexFormat m_format;
            private EffectInstance m_effect;
            private PrimitiveType m_type;
            private int m_vertexCount, m_indexCount, m_primitiveCount;

            public static GpuBuffer Triangles(List<VertexPositionNormalColored> verts, List<IndexTriangle> tris)
            {
                int vSize = verts.Count * VertexPositionNormalColored.GetSizeInFloats();
                var vb = new VertexBuffer(vSize);
                vb.Map(vSize);
                VertexStreamPositionNormalColored vs = vb.GetVertexStreamPositionNormalColored();
                foreach (var v in verts)
                    vs.AddVertex(v);
                vb.Unmap();

                int iSize = tris.Count * IndexTriangle.GetSizeInShortInts();
                var ib = new IndexBuffer(iSize);
                ib.Map(iSize);
                IndexStreamTriangle ts = ib.GetIndexStreamTriangle();
                foreach (var t in tris)
                    ts.AddTriangle(t);
                ib.Unmap();

                return new GpuBuffer
                {
                    m_vb = vb,
                    m_ib = ib,
                    m_format = new VertexFormat(VertexFormatBits.PositionNormalColored),
                    m_effect = new EffectInstance(VertexFormatBits.PositionNormalColored),
                    m_type = PrimitiveType.TriangleList,
                    m_vertexCount = verts.Count,
                    m_indexCount = iSize,
                    m_primitiveCount = tris.Count,
                };
            }

            public static GpuBuffer Lines(List<VertexPositionColored> verts, List<IndexLine> lines)
            {
                int vSize = verts.Count * VertexPositionColored.GetSizeInFloats();
                var vb = new VertexBuffer(vSize);
                vb.Map(vSize);
                VertexStreamPositionColored vs = vb.GetVertexStreamPositionColored();
                foreach (var v in verts)
                    vs.AddVertex(v);
                vb.Unmap();

                int iSize = lines.Count * IndexLine.GetSizeInShortInts();
                var ib = new IndexBuffer(iSize);
                ib.Map(iSize);
                IndexStreamLine ls = ib.GetIndexStreamLine();
                foreach (var l in lines)
                    ls.AddLine(l);
                ib.Unmap();

                return new GpuBuffer
                {
                    m_vb = vb,
                    m_ib = ib,
                    m_format = new VertexFormat(VertexFormatBits.PositionColored),
                    m_effect = new EffectInstance(VertexFormatBits.PositionColored),
                    m_type = PrimitiveType.LineList,
                    m_vertexCount = verts.Count,
                    m_indexCount = iSize,
                    m_primitiveCount = lines.Count,
                };
            }

            public void Flush()
            {
                DrawContext.FlushBuffer(m_vb, m_vertexCount, m_ib, m_indexCount, m_format, m_effect, m_type, 0, m_primitiveCount);
            }

            public void Dispose()
            {
                m_vb?.Dispose();
                m_ib?.Dispose();
                m_format?.Dispose();
                m_effect?.Dispose();
            }
        }

        // ------------------------------------------------------------------ tooltip cạnh con trỏ

        /// <summary>Ô chú thích nhỏ đi theo con trỏ: không lấy focus, chuột xuyên qua.</summary>
        private class PreviewTip : System.Windows.Forms.Form
        {
            private static readonly System.Drawing.Font TitleFont = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            private static readonly System.Drawing.Font BodyFont = new System.Drawing.Font("Segoe UI", 9F);
            private readonly IntPtr m_owner;
            private string m_title = "", m_body = "";

            public PreviewTip(IntPtr owner)
            {
                m_owner = owner;
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                TopMost = true;
                BackColor = System.Drawing.Color.FromArgb(255, 255, 225);
                DoubleBuffered = true;
            }

            protected override bool ShowWithoutActivation => true;

            protected override System.Windows.Forms.CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x08000000 /*NOACTIVATE*/ | 0x80 /*TOOLWINDOW*/ | 0x20 /*TRANSPARENT*/ | 0x8 /*TOPMOST*/;
                    return cp;
                }
            }

            public void ShowAt(int x, int y, string title, string body)
            {
                if (title != m_title || body != m_body)
                {
                    m_title = title;
                    m_body = body;
                    var t = System.Windows.Forms.TextRenderer.MeasureText(title, TitleFont);
                    var b = System.Windows.Forms.TextRenderer.MeasureText(body, BodyFont);
                    Size = new System.Drawing.Size(Math.Max(t.Width, b.Width) + 18, t.Height + b.Height + 14);
                    Invalidate();
                }

                Location = new System.Drawing.Point(x + 18, y + 22);
                if (!Visible)
                    Show(new WindowHandle(m_owner));
            }

            protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
            {
                var g = e.Graphics;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(150, 150, 150)))
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                var t = System.Windows.Forms.TextRenderer.MeasureText(m_title, TitleFont);
                System.Windows.Forms.TextRenderer.DrawText(g, m_title, TitleFont, new System.Drawing.Point(8, 5),
                                                          System.Drawing.Color.FromArgb(15, 124, 123));
                System.Windows.Forms.TextRenderer.DrawText(g, m_body, BodyFont, new System.Drawing.Point(8, 7 + t.Height),
                                                          System.Drawing.Color.FromArgb(60, 60, 60));
            }
        }

        private static class Native
        {
            public const int VK_LBUTTON = 0x01, VK_TAB = 0x09, VK_ESCAPE = 0x1B;
            public const uint GA_ROOTOWNER = 3;

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
            }

            [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT pt);
            [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
            [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
            [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT pt);
        }
    }
}
