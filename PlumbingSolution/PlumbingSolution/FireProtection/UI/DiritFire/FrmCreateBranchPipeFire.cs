using PlumbingSolution.FireProtection.RequestForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.ItemData;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rectangle = System.Drawing.Rectangle;

namespace PlumbingSolution.FireProtection.UI.Service_J
{
    public partial class FrmCreateBranchPipeFire : System.Windows.Forms.Form
    {
        private RequestHandler m_handler = null;
        private ExternalEvent m_exEvent = null;
        public CreateBranchPipeFireConfigData ConfigData;

        public double Diameter
        {
            get
            {
                double diameter;
                if (double.TryParse(cbbDiameter.SelectedItem.ToString(), out diameter))
                {
                    return diameter;
                }
                else
                {
                    return 25; // Default value if parsing fails
                }
            }
        }

        public double DiameterBranch
        {
            get
            {
                double diameter;
                if (double.TryParse(cbbDiameterBranch.SelectedItem.ToString(), out diameter))
                {
                    return diameter;
                }
                else
                {
                    return 25; // Default value if parsing fails
                }
            }
        }

        public FrmCreateBranchPipeFire(ExternalEvent exEvent, RequestHandler handler)
        {
            InitializeComponent();

            m_handler = handler;
            m_exEvent = exEvent;
            //var iconPath = Common.GetIconFolder();
            //this.Icon = new System.Drawing.Icon(Path.Combine(iconPath, "Logo_diritsolution_64x64.ico"));
            Common.SettingTemplate(this);
            ConfigData = new CreateBranchPipeFireConfigData();
            dgv.CellPainting += DataGridView1_CellPainting;
            Init();
            GetSetting();
            DisableColumnSorting(dgv);
            DisableControls();
        }

        private void chkConnectSprinkler_CheckedChanged(object sender, EventArgs e)
        {
            DisableControls();
        }

        private void chkConnectBranch_CheckedChanged(object sender, EventArgs e)
        {
            DisableControls();
        }

        private void rdnDiffElevation_CheckedChanged(object sender, EventArgs e)
        {
            DisableControls();
        }

        private void rdnCenter_CheckedChanged(object sender, EventArgs e)
        {
            DisableControls();
        }

        private void rdn2T_CheckedChanged(object sender, EventArgs e)
        {
            DisableControls();
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;

            if (e.RowIndex > -1 && e.ColumnIndex > -1)
            {
                dgv.BeginEdit(true);
                if (dgv.EditingControl is System.Windows.Forms.TextBox textBox)
                {
                    textBox.KeyDown += PreventCopy;
                    textBox.KeyPress += Validate;
                }
                else if (dgv.EditingControl is System.Windows.Forms.ComboBox cbBox)
                {
                    cbBox.DroppedDown = true;
                }
            }
        }

        private void DisableControls()
        {
            cbbTypeConnection.Enabled = chkConnectSprinkler.Checked;
            cbbDiameter.Enabled = chkConnectSprinkler.Checked;
            chkElbow90.Enabled = chkConnectSprinkler.Checked;
            rdn1T.Enabled = chkConnectBranch.Checked;
            rdn1TE45.Enabled = chkConnectBranch.Checked;
            rdn1TE90.Enabled = chkConnectBranch.Checked;
            rdn2T.Enabled = chkConnectBranch.Checked;
            tbElevation.Enabled = rdnDiffElevation.Checked;
            tbOffset.Enabled = !rdnPipeCenter.Checked;
            cbbDiameterBranch.Enabled = rdn2T.Checked && chkConnectBranch.Checked;
            chkElbow90Branch.Enabled = rdn2T.Checked && chkConnectBranch.Checked;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbOffsetCon.Text.Trim())
                || (!rdnPipeCenter.Checked && string.IsNullOrEmpty(tbOffset.Text.Trim()))
                || (rdnDiffElevation.Checked && string.IsNullOrEmpty(tbElevation.Text.Trim())))
            {
                IO.ShowWarning("Please enter a value.");
                return;
            }

            SaveSetting();
            MakeRequest(RequestId.CreateBranchPipeFire);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                // Chỉ xử lý vẽ cho hàng Tiêu đề (RowIndex == -1)
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    e.PaintBackground(e.CellBounds, true);

                    // Kích thước của ô sub-header (nửa dưới)
                    Rectangle rectSubHeader = e.CellBounds;
                    rectSubHeader.Y += e.CellBounds.Height / 2;
                    rectSubHeader.Height -= e.CellBounds.Height / 2;

                    // Kích thước của ô group-header (nửa trên)
                    Rectangle rectGroupHeader = e.CellBounds;
                    rectGroupHeader.Height = e.CellBounds.Height / 2;

                    // Vẽ viền (Border) cho các ô
                    Pen gridPen = new Pen(dgv.GridColor);
                    e.Graphics.DrawRectangle(gridPen, rectSubHeader);
                    e.Graphics.DrawRectangle(gridPen, rectGroupHeader);

                    // Định dạng căn giữa chữ
                    StringFormat format = new StringFormat()
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    // 1. Vẽ text cho Sub-header (Ví dụ: Pipe Size, Số đầu phun)
                    e.Graphics.DrawString(e.Value?.ToString(), e.CellStyle.Font,
                        new SolidBrush(e.CellStyle.ForeColor), rectSubHeader, format);

                    // 2. Vẽ Group Header (Left / Right)
                    if (e.ColumnIndex == 0 || e.ColumnIndex == 1) // Cột Left
                    {
                        Rectangle r1 = dgv.GetCellDisplayRectangle(0, -1, true);
                        Rectangle r2 = dgv.GetCellDisplayRectangle(1, -1, true);
                        Rectangle rectLeft = new Rectangle(r1.X, r1.Y, r1.Width + r2.Width, r1.Height / 2);

                        e.Graphics.FillRectangle(new SolidBrush(e.CellStyle.BackColor), rectLeft);
                        e.Graphics.DrawRectangle(gridPen, rectLeft);

                        // TẠO FONT IN ĐẬM VÀ VẼ CHỮ
                        using (Font boldFont = new Font(e.CellStyle.Font, FontStyle.Bold))
                        {
                            e.Graphics.DrawString("Left", boldFont, new SolidBrush(e.CellStyle.ForeColor), rectLeft, format);
                        }
                    }
                    else if (e.ColumnIndex == 2 || e.ColumnIndex == 3) // Cột Right
                    {
                        Rectangle r1 = dgv.GetCellDisplayRectangle(2, -1, true);
                        Rectangle r2 = dgv.GetCellDisplayRectangle(3, -1, true);
                        Rectangle rectRight = new Rectangle(r1.X, r1.Y, r1.Width + r2.Width, r1.Height / 2);

                        e.Graphics.FillRectangle(new SolidBrush(e.CellStyle.BackColor), rectRight);
                        e.Graphics.DrawRectangle(gridPen, rectRight);

                        // TẠO FONT IN ĐẬM VÀ VẼ CHỮ
                        using (Font boldFont = new Font(e.CellStyle.Font, FontStyle.Bold))
                        {
                            e.Graphics.DrawString("Right", boldFont, new SolidBrush(e.CellStyle.ForeColor), rectRight, format);
                        }
                    }

                    // Báo cho WinForms biết là ta đã tự vẽ xong, không cần vẽ mặc định nữa
                    e.Handled = true;
                }
            }
            catch (Exception)
            {
            }
        }

        private void DisableColumnSorting(DataGridView dataGridView)
        {
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void Init()
        {
            List<string> pipeSizes = new List<string> { "None", "80", "65", "50", "40", "32", "25" };

            for (int i = 1; i < pipeSizes.Count; i++)
            {
                var index = dgv.Rows.Add();

                //Pipe Type
                DataGridViewComboBoxCell comboCellPipeTypeLeft = dgv.Rows[index].Cells[0] as DataGridViewComboBoxCell;

                comboCellPipeTypeLeft.Items.AddRange(pipeSizes.ToArray());

                DataGridViewComboBoxCell comboCellPipeTyperigth = dgv.Rows[index].Cells[2] as DataGridViewComboBoxCell;

                comboCellPipeTyperigth.Items.AddRange(pipeSizes.ToArray());

                dgv.Rows[index].Cells[0].Value = pipeSizes[i];
                dgv.Rows[index].Cells[2].Value = pipeSizes[i];
                dgv.Rows[index].Cells[1].Value = "1";
                dgv.Rows[index].Cells[3].Value = "1";
            }

            pipeSizes.RemoveAt(0);

            cbbDiameter.DataSource = pipeSizes.OrderBy(x => double.Parse(x)).ToList();
            cbbDiameter.SelectedItem = "25";

            cbbDiameterBranch.DataSource = pipeSizes.OrderBy(x => double.Parse(x)).ToList();
            cbbDiameterBranch.SelectedItem = "25";

            List<string> typeConnections = new List<string> { "Type 1", "Type 2", "Type 3" };
            cbbTypeConnection.DataSource = typeConnections;
            cbbTypeConnection.SelectedIndex = 0;
        }

        /// <summary>
        /// Prevent invalid copy paste
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PreventCopy(object sender, KeyEventArgs e)
        {
            if (e.Control == true && e.KeyCode == Keys.V)
            {
                if (!double.TryParse(Clipboard.GetText(), out double result) ||
                    Clipboard.GetText().Contains(",") ||
                    Clipboard.GetText().Contains("-"))
                    e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Validate textbox digit input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Validate(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && (!char.IsDigit(e.KeyChar))
                && (e.KeyChar != '.') && (e.KeyChar != '-'))
                e.Handled = true;

            if (sender is System.Windows.Forms.TextBox)
            {
                // only allow one decimal point
                if (e.KeyChar == '.' && (sender as System.Windows.Forms.TextBox).Text.IndexOf('.') > -1)
                    e.Handled = true;

                // only allow minus sign at the beginning
                if (e.KeyChar == '-' && (sender as System.Windows.Forms.TextBox).SelectionStart > 0)
                    e.Handled = true;
            }
            else if (sender is System.Windows.Forms.ComboBox)
            {
                // only allow one decimal point
                if (e.KeyChar == '.' && (sender as System.Windows.Forms.ComboBox).Text.IndexOf('.') > -1)
                    e.Handled = true;

                // only allow minus sign at the beginning
                if (e.KeyChar == '-' && (sender as System.Windows.Forms.ComboBox).SelectionStart > 0)
                    e.Handled = true;
            }
        }

        private void GetSetting()
        {
            ConfigData = new CreateBranchPipeFireConfigData();
            CreateBranchPipeFireConfigData.ReadConfigData(out ConfigData);

            if (!string.IsNullOrEmpty(ConfigData.TypeConnection))
                cbbTypeConnection.SelectedItem = ConfigData.TypeConnection;

            chkElbow90.Checked = ConfigData.IsElbow90;
            chkElbow90Branch.Checked = ConfigData.IsElbow90Branch2T;
            switch (ConfigData.TypeLocation)
            {
                case 1:
                    rdnPipeLeft.Checked = true;
                    break;

                case 2:
                    rdnPipeRight.Checked = true;
                    break;

                case 3:
                    rdnPipeCenter.Checked = true;
                    break;

                default:
                    break;
            }

            tbOffset.Text = ConfigData.OffsetDauPhun.ToString();
            tbElevation.Text = ConfigData.OffsetElevation.ToString();
            tbOffsetCon.Text = ConfigData.OffsetCon.ToString();
            if (ConfigData.Elevation == 1)
                rdnSameElevation.Checked = true;
            else
                rdnDiffElevation.Checked = true;
            chkConnectBranch.Checked = ConfigData.IsConnectBranch;
            chkConnectSprinkler.Checked = ConfigData.IsConnectSprinkler;

            switch (ConfigData.TypeConnectBranch)
            {
                case 1:
                    rdn1T.Checked = true;
                    break;

                case 2:
                    rdn1TE45.Checked = true;
                    break;

                case 3:
                    rdn1TE90.Checked = true;
                    break;

                case 4:
                    rdn2T.Checked = true;
                    break;

                default:
                    break;
            }

            List<int> lstNoneLeft = new List<int>();
            List<int> lstNoneRight = new List<int>();

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].Cells[0].Value == null || dgv.Rows[i].Cells[2].Value == null)
                    continue;

                if (dgv.Rows[i].Cells[0].Value.ToString() == "None")
                    lstNoneLeft.Add(i);

                if (dgv.Rows[i].Cells[2].Value.ToString() == "None")
                    lstNoneRight.Add(i);
            }

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].Cells[0].Value == null)
                    continue;

                if (ConfigData.LeftRules?.Count > 0)
                {
                    var left = ConfigData.LeftRules.FirstOrDefault(x => x.PipeSize.ToString() == dgv.Rows[i].Cells[0].Value.ToString());
                    if (left != null)
                        dgv.Rows[i].Cells[1].Value = left.SprinklerCount;
                    else
                        dgv.Rows[i].Cells[0].Value = "None";
                }

                if (ConfigData.RightRules?.Count > 0)
                {
                    var right = ConfigData.RightRules.FirstOrDefault(x => x.PipeSize.ToString() == dgv.Rows[i].Cells[2].Value.ToString());
                    if (right != null)
                        dgv.Rows[i].Cells[3].Value = right.SprinklerCount;
                    else
                        dgv.Rows[i].Cells[2].Value = "None";
                }
            }
        }

        private void SaveSetting()
        {
            ConfigData = new CreateBranchPipeFireConfigData();

            ConfigData.TypeConnection = cbbTypeConnection.SelectedItem.ToString();
            ConfigData.Diameter = Diameter;
            ConfigData.DiameterBranch = DiameterBranch;
            ConfigData.IsElbow90 = chkElbow90.Checked;
            ConfigData.IsElbow90Branch2T = chkElbow90Branch.Checked;
            ConfigData.TypeLocation = rdnPipeLeft.Checked ? 1 : (rdnPipeRight.Checked ? 2 : 3);
            ConfigData.OffsetDauPhun = double.TryParse(tbOffset.Text, out double offsetDauPhun) ? offsetDauPhun : 0;
            ConfigData.OffsetElevation = double.TryParse(tbElevation.Text, out double elevation) ? elevation : 0;
            ConfigData.OffsetCon = double.TryParse(tbOffsetCon.Text, out double offsetCon) ? offsetCon : 0;
            ConfigData.Elevation = rdnSameElevation.Checked ? 1 : (rdnDiffElevation.Checked ? 2 : 3);
            ConfigData.IsConnectBranch = chkConnectBranch.Checked;
            ConfigData.IsConnectSprinkler = chkConnectSprinkler.Checked;
            ConfigData.TypeConnectBranch = rdn1T.Checked ? 1 : (rdn1TE45.Checked ? 2 : (rdn1TE90.Checked ? 3 : 4));

            List<int> lstNoneLeft = new List<int>();
            List<int> lstNoneRight = new List<int>();

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].Cells[0].Value == null || dgv.Rows[i].Cells[2].Value == null)
                    continue;

                if (dgv.Rows[i].Cells[0].Value.ToString() == "None")
                    lstNoneLeft.Add(i);

                if (dgv.Rows[i].Cells[2].Value.ToString() == "None")
                    lstNoneRight.Add(i);
            }

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].Cells[0].Value == null || dgv.Rows[i].Cells[1].Value == null || lstNoneLeft.Contains(i))
                    continue;

                PipeSizingRule pipeSizingRule = new PipeSizingRule
                {
                    PipeSize = double.Parse(dgv.Rows[i].Cells[0].Value.ToString()),
                    SprinklerCount = int.TryParse(dgv.Rows[i].Cells[1].Value.ToString(), out int soDauPhun) ? soDauPhun : 1
                };

                ConfigData.LeftRules.Add(pipeSizingRule);
            }

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].Cells[2].Value == null || dgv.Rows[i].Cells[3].Value == null || lstNoneRight.Contains(i))
                    continue;

                PipeSizingRule pipeSizingRule = new PipeSizingRule
                {
                    PipeSize = double.Parse(dgv.Rows[i].Cells[2].Value.ToString()),
                    SprinklerCount = int.TryParse(dgv.Rows[i].Cells[3].Value.ToString(), out int soDauPhun) ? soDauPhun : 1
                };

                ConfigData.RightRules.Add(pipeSizingRule);
            }

            CreateBranchPipeFireConfigData.WriteConfigData(ref ConfigData);
        }

        private void dgv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        }

        private void dgv_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        public void MakeRequest(RequestId request)
        {
            m_handler.Request.Make(request);
            m_exEvent.Raise();
        }
    }
}
