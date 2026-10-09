using PlumbingSolution.FireProtection.RequestForm;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using Form = System.Windows.Forms.Form;
using GroupBox = System.Windows.Forms.GroupBox;

namespace PlumbingSolution.FireProtection.UI.Service_E
{
    public partial class UI_ConnectSprinkle : Form
    {
        private Request m_request;
        private RequestHandler m_handler;
        private ExternalEvent m_exEvent;

        public UI_ConnectSprinkle(ExternalEvent exEvent, RequestHandler handler)
        {
            InitializeComponent();

            m_handler = handler;
            m_exEvent = exEvent;

            SettingLanguage();
        }

        private void SettingLanguage()
        {
            Dictionary<string, System.Windows.Forms.Control> keyControl = new Dictionary<string, System.Windows.Forms.Control>();
            keyControl.Add("btnsprinklerUp", this);
            keyControl.Add("ConnectionType", groupBox2);
            keyControl.Add("VerticalPipe", groupBox1);
            keyControl.Add("ElbowConnection", ckbConnectCo90);
            keyControl.Add("Type2", rbtnOptions2);
            keyControl.Add("Type1", rbtnOptions1);
            keyControl.Add("Type3", rdnOption3);
            keyControl.Add("Type4", rbtnOptions4);
            keyControl.Add("VerticalPipeType", label1);
            keyControl.Add("VerticalPipeDiameter", label2);
            keyControl.Add("OK", btnRun);
            keyControl.Add("Cancel", btnCancel);
            Common.SettingLanguage(keyControl);
        }

        public double MainPipeSprinklerDistance
        {
            get => 1000;
        }

        public double EndPointSprinklerDistance
        {
            get => 1000;
        }


        public bool isConnectTee = false;
        public bool isConnectNipple = false;
        public bool isElbow = false;
        public bool isOption3 = false;

        public FamilySymbol fmlNipple = null;

        /// <summary>Type 4: chiều cao A (mm) của đoạn ống đứng trên ống chính.</summary>
        public double HeightA_ => double.TryParse(tbA.Text.Trim(), out double v) ? v : double.MinValue;

        /// <summary>Type 4: tích Preview thì A chỉnh bằng con trỏ trong view thay vì nhập.</summary>
        public bool IsPreview => rbtnOptions4.Checked && ckbPreview.Checked;

        /// <summary>Ghi lại A đã chốt trong chế độ Preview để lần sau mở form thấy đúng giá trị.</summary>
        public void SetHeightA(double mm)
        {
            tbA.Text = Math.Round(mm).ToString();
            AppUtils.sa(tbA);
        }

        public ElementId FamilyTypeC2
        {
            get
            {
                return (cboC2PipeType.SelectedItem as ObjectItem).ObjectId;
            }
        }

        public double PipeSizeC2
        {
            get
            {
                var value = cboC2PypeSize.SelectedItem.ToString();

                value = value.Replace(" mm", "");

                double d = 0;
                if (double.TryParse(value, out d) == false)
                    return double.MaxValue;

                return d;
            }
        }



        public void PressCancel(int count = 2)
        {
            IWin32Window _revit_window = new WindowHandle(ComponentManager.ApplicationWindow);

            for (int i = 0; i < count; i++)
            {
                Press.PostMessage(_revit_window.Handle, (uint)Press.KEYBOARD_MSG.WM_KEYDOWN, (uint)Keys.Escape, 0);
            }
        }

        private void SetFocus()
        {
            IntPtr hBefore = DisplayService.GetForegroundWindow();
            DisplayService.SetForegroundWindow(ComponentManager.ApplicationWindow);
        }

        public void MakeRequest(RequestId request)
        {
            m_handler.Request.Make(request);
            m_exEvent.Raise();
        }



        private void cboC2PipeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var familyTypeId = (cboC2PipeType.SelectedItem as ObjectItem).ObjectId;

            var familyType = Global.UIDoc.Document.GetElement(familyTypeId) as MEPCurveType;

            if (familyType.RoutingPreferenceManager != null)
            {
                PipeSegment CurrentSegment = null;
                int count = familyType.RoutingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);

                for (int i = 0; i < count; i++)
                {
                    var rule = familyType.RoutingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Segments, i);

                    CurrentSegment = Global.UIDoc.Document.GetElement(rule.MEPPartId) as PipeSegment;
                }

                if (CurrentSegment != null)
                    AddDiameterC2(CurrentSegment);
            }
        }



        private void AddDiameterC2(Segment segment)
        {
            cboC2PypeSize.Items.Clear();

            foreach (MEPSize size in segment.GetSizes())
            {
                var value = Common.FeetToMmString(size.NominalDiameter) + " mm";

                cboC2PypeSize.Items.Add(value);
            }

            AppUtils.ff(cboC2PypeSize);

            if (cboC2PypeSize.SelectedItem == null && cboC2PypeSize.Items.Count != 0)
                cboC2PypeSize.SelectedIndex = 0;
        }

        private void AddFamilyTypeC2()
        {
            cboC2PipeType.Items.Clear();
            FilteredElementCollector pipeTypes = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(PipeType));
            foreach (MEPCurveType type in pipeTypes)
            {
                ObjectItem item = new ObjectItem(type.Name, type.Id);
                cboC2PipeType.Items.Add(item);
            }

            AppUtils.ff(cboC2PipeType, null);

            if (cboC2PipeType.SelectedItem == null && cboC2PipeType.Items.Count != 0)
                cboC2PipeType.SelectedIndex = 0;
        }


        private void btnRun_Click(object sender, EventArgs e)
        {
            AppUtils.sa(rbtnOptions1);
            AppUtils.sa(rbtnOptions2);
            AppUtils.sa(rdnOption3);
            AppUtils.sa(ckbConnectCo90);
            AppUtils.sa(cboC2PipeType);
            AppUtils.sa(cboC2PypeSize);

            AppUtils.sa(rbtnOptions4);
            AppUtils.sa(tbA);
            AppUtils.sa(ckbPreview);

            if (PipeSizeC2 == double.MaxValue)
                return;

            if (rbtnOptions4.Checked)
            {
                if (!IsPreview && HeightA_ <= 0)
                {
                    MessageBox.Show(this, "A must be greater than 0.", "Upright Sprinkler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                isElbow = ckbConnectCo90.Checked;
                SetFocus();
                MakeRequest(RequestId.SprinklerUpType4_RUN);
                return;
            }

            isConnectTee = rbtnOptions2.Checked;
            isElbow = ckbConnectCo90.Checked;
            isOption3 = rdnOption3.Checked;

            SetFocus();
            MakeRequest(RequestId.SprinklerUp_Aplly);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UI_ConnectSprinkle_Load(object sender, EventArgs e)
        {
            AddFamilyTypeC2();

            Common.SettingTemplate(this);

            ckbConnectCo90.Checked = false;

            string folder = Common.GetFirePreviewFolder();
            this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "C5_Sprinkler_Up_Connect_to_Tee.jpg"));
            rbtnOptions1.Checked = true;

            AppUtils.ff(rbtnOptions1);
            AppUtils.ff(rbtnOptions2);
            AppUtils.ff(rdnOption3);
            AppUtils.ff(ckbConnectCo90);
            AppUtils.ff(cboC2PipeType);
            AppUtils.ff(cboC2PypeSize);
            AppUtils.ff(rbtnOptions4);
            AppUtils.ff(tbA);
            AppUtils.ff(ckbPreview);
            UpdateOptionState();
        }

        private void rbtnOptions1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateOptionState();
        }

        // A chỉ dùng cho Type 4 (và mờ hẳn khi tích Preview). Elbow Connection dùng cho mọi type: đầu phun gần đầu ống chính
        // còn hở thì nối bằng co, cắt bỏ đoạn thừa, thay vì tee để lại đoạn cụt.
        private void UpdateOptionState()
        {
            ckbPreview.Enabled = rbtnOptions4.Checked;
            lblA.Enabled = tbA.Enabled = rbtnOptions4.Checked && !ckbPreview.Checked;

            string folder = Common.GetFirePreviewFolder();
            string type3 = Path.Combine(Common.GetPreviewFolder(), "PlumbingSolution", "Upright_Type3.png");
            string type4 = Path.Combine(Common.GetPreviewFolder(), "PlumbingSolution", "Upright_Type4.png");
            if (rbtnOptions1.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "Phuonganlen1.png"));
            else if (rdnOption3.Checked && File.Exists(type3))
                this.picPreview.Image = System.Drawing.Image.FromFile(type3);
            else if (rbtnOptions4.Checked && File.Exists(type4))
                this.picPreview.Image = System.Drawing.Image.FromFile(type4);
            else
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "Phuonganlen2.png"));
        }

        private void tbA_KeyPress(object sender, KeyPressEventArgs e)
        {
            Common.NumberCheck(sender, e, false);
        }
    }
}
