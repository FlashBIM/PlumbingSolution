using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.UI
{
    /// <summary>
    /// Giao diện chung cho các form đầu phun: nền trắng, chữ Segoe UI, nhóm viền xám mảnh với
    /// tiêu đề màu teal (lấy từ icon PlumbingSolution), nút chính nền teal, dải nút dưới nền xám nhạt.
    /// Gọi một lần sau InitializeComponent (qua Common.SettingTemplate).
    /// </summary>
    public static class FormStyle
    {
        public static readonly Color Accent = Color.FromArgb(15, 124, 123);
        public static readonly Color AccentHover = Color.FromArgb(11, 99, 98);
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color TextMuted = Color.FromArgb(75, 85, 99);
        public static readonly Color Border = Color.FromArgb(215, 220, 227);
        public static readonly Color Surface = Color.FromArgb(246, 248, 250);
        public static readonly Color SecondaryHover = Color.FromArgb(240, 243, 246);

        private static readonly string[] PrimaryButtonNames = { "btnRun", "btnApply", "btnOK", "btnOk" };

        public static void Apply(Form form)
        {
            form.SuspendLayout();
            form.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            form.BackColor = Color.White;
            form.ForeColor = Text;
            form.ShowIcon = false;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;

            Control footer = null;
            foreach (Control c in Descendants(form))
            {
                StyleControl(c);
                if (footer == null && c is Button && c.Name == "btnCancel")
                    footer = c.Parent;
            }

            if (footer != null)
            {
                footer.BackColor = Surface;
                footer.Paint += PaintTopRule;
            }

            form.ResumeLayout(true);
        }

        private static void StyleControl(Control c)
        {
            switch (c)
            {
                case Button b:
                    StyleButton(b);
                    break;

                case GroupBox g:
                    g.BackColor = Color.White;
                    g.ForeColor = Text;
                    g.Padding = new Padding(8, 6, 8, 6);
                    g.Paint -= PaintGroupBox;
                    g.Paint += PaintGroupBox;
                    break;

                case PictureBox p:
                    p.BackColor = Surface;
                    p.BorderStyle = BorderStyle.None;
                    p.SizeMode = PictureBoxSizeMode.Zoom;
                    p.Padding = new Padding(6);
                    p.Paint -= PaintFrame;
                    p.Paint += PaintFrame;
                    break;

                case TextBox t:
                    t.BorderStyle = BorderStyle.FixedSingle;
                    t.BackColor = Color.White;
                    t.ForeColor = Text;
                    CenterInCell(t);
                    break;

                case ComboBox cb:
                    cb.BackColor = Color.White;
                    cb.ForeColor = Text;
                    CenterInCell(cb);
                    break;

                case Label l:
                    l.ForeColor = TextMuted;
                    l.BackColor = Color.Transparent;
                    if (l.Parent is TableLayoutPanel && l.Dock == DockStyle.None)
                        l.Anchor = AnchorStyles.Left;
                    break;

                case CheckBox _:
                case RadioButton _:
                    c.ForeColor = Text;
                    c.BackColor = Color.Transparent;
                    break;

                case TabPage tp:
                    tp.BackColor = Color.White;
                    break;

                case DataGridView dg:
                    dg.BackgroundColor = Color.White;
                    dg.BorderStyle = BorderStyle.FixedSingle;
                    dg.GridColor = Border;
                    dg.EnableHeadersVisualStyles = false;
                    dg.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                    dg.ColumnHeadersDefaultCellStyle.BackColor = Surface;
                    dg.ColumnHeadersDefaultCellStyle.ForeColor = Text;
                    dg.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 240, 239);
                    dg.DefaultCellStyle.SelectionForeColor = Text;
                    break;

                case TableLayoutPanel _:
                case Panel _:
                    c.BackColor = Color.Transparent;
                    break;
            }
        }

        // Ô nhập Dock=Fill trong hàng cao bị dính lên mép trên, lệch với nhãn đã căn giữa: neo trái-phải để căn giữa dọc.
        private static void CenterInCell(Control c)
        {
            if (c.Parent is TableLayoutPanel && c.Dock == DockStyle.Fill && !(c is TextBox t && t.Multiline))
            {
                c.Dock = DockStyle.None;
                c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            }
        }

        private static void StyleButton(Button b)
        {
            bool primary = PrimaryButtonNames.Contains(b.Name) || b.FindForm()?.AcceptButton == b;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            b.MinimumSize = new Size(88, 30);
            if (primary)
            {
                b.BackColor = Accent;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = AccentHover;
                b.FlatAppearance.MouseDownBackColor = AccentHover;
            }
            else
            {
                b.BackColor = Color.White;
                b.ForeColor = Text;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Border;
                b.FlatAppearance.MouseOverBackColor = SecondaryHover;
                b.FlatAppearance.MouseDownBackColor = Border;
            }
        }

        // Viền xám bo góc + tiêu đề teal đậm, thay viền 3D mặc định của GroupBox.
        private static void PaintGroupBox(object sender, PaintEventArgs e)
        {
            GroupBox g = (GroupBox)sender;
            Graphics gr = e.Graphics;
            gr.Clear(g.BackColor);
            gr.SmoothingMode = SmoothingMode.AntiAlias;

            using (Font titleFont = new Font(g.Font, FontStyle.Bold))
            {
                Size ts = TextRenderer.MeasureText(gr, g.Text, titleFont);
                int top = ts.Height / 2;
                Rectangle r = new Rectangle(0, top, g.Width - 1, g.Height - top - 1);
                using (GraphicsPath path = Rounded(r, 4))
                using (Pen pen = new Pen(Border))
                    gr.DrawPath(pen, path);

                if (!string.IsNullOrEmpty(g.Text))
                {
                    Rectangle tr = new Rectangle(8, 0, ts.Width + 4, ts.Height);
                    using (SolidBrush bg = new SolidBrush(g.BackColor))
                        gr.FillRectangle(bg, tr);
                    TextRenderer.DrawText(gr, g.Text, titleFont, new Point(10, 0), Accent);
                }
            }
        }

        private static void PaintFrame(object sender, PaintEventArgs e)
        {
            Control c = (Control)sender;
            using (Pen pen = new Pen(Border))
                e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        }

        private static void PaintTopRule(object sender, PaintEventArgs e)
        {
            Control c = (Control)sender;
            using (Pen pen = new Pen(Border))
                e.Graphics.DrawLine(pen, 0, 0, c.Width, 0);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (Control d in Descendants(c))
                    yield return d;
            }
        }
    }
}
