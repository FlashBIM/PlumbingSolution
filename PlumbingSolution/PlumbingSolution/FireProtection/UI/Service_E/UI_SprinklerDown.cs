using PlumbingSolution.FireProtection.RequestForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Windows.Forms;
using Form = System.Windows.Forms.Form;
using System.Windows.Controls;
using GroupBox = System.Windows.Forms.GroupBox;
using System.Drawing;
using PlumbingSolution.FireProtection.Services;
using Autodesk.Windows;
using System.Collections.Generic;
using System.Linq;
using PlumbingSolution.FireProtection.Extensions;
using PlumbingSolution.FireProtection.ItemData;
using System.IO;

namespace PlumbingSolution.FireProtection.UI.Service_E
{
    public partial class UI_SprinklerDown : Form
    {
        private Request m_request;
        private RequestHandler m_handler;
        private ExternalEvent m_exEvent;
        private Document _doc;

        public double MainPipeSprinklerDistance
        {
            get => 1000;
        }

        public double EndPointSprinklerDistance
        {
            get => 1000;
        }

        private string _previewFolder = string.Empty;

        public UI_SprinklerDown(Document doc, ExternalEvent exEvent, RequestHandler handler)
        {
            InitializeComponent();

            Common.SettingTemplate(this);
            _previewFolder = Common.GetMEPPreviewFolder();
            _doc = doc;
            m_handler = handler;
            m_exEvent = exEvent;
            InitFamilyElbow();
            SettingLanguage();
        }

        private void InitFamilyElbow()
        {
            //type z
            string folderCableTayOffsets = DirectoryUtils.GetFamilyCategoryFolder("Pipe Fittings");

            List<string> lstFamilyNameElbow = Directory.GetFiles(folderCableTayOffsets, "*.rfa", SearchOption.AllDirectories).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();

            List<FamilySymbol> symbols = new FilteredElementCollector(Global.UIDoc.Document)
               .OfCategory(BuiltInCategory.OST_PipeFitting)
               .OfClass(typeof(FamilySymbol))
               .Cast<FamilySymbol>()
               .Where(x => lstFamilyNameElbow.Contains(x.FamilyName))
               .OrderBy(x => x.Name).ToList();

            if (symbols != null && symbols.Count > 0)
            {
                var all_FamilyFitting = new List<ItemFamilySymbol>();

                foreach (var item in symbols)
                {
                    all_FamilyFitting.Add(new ItemFamilySymbol(item));
                }

                cbbElbow.DataSource = all_FamilyFitting;
                cbbElbow.DisplayMember = "Name";
                cbbElbow.ValueMember = "Symbol";

                cbbElbow.SelectedIndex = 0;
            }
        }

        private void SettingLanguage()
        {
            Dictionary<string, System.Windows.Forms.Control> keyControl = new Dictionary<string, System.Windows.Forms.Control>();
            keyControl.Add("btnsprinklerDown", this);
            keyControl.Add("ConnectionType", groupBox2);
            keyControl.Add("VerticalPipe", groupBox1);
            keyControl.Add("ElbowConnection", ckbConnectCo90);
            keyControl.Add("Type2", rbtnOptions2);
            keyControl.Add("Type1", rbtnOptions1);
            keyControl.Add("Type3", rbtnOptions3);
            keyControl.Add("Type4", rbtnOptions4);
            keyControl.Add("VerticalPipeType", label1);
            keyControl.Add("VerticalPipeDiameter", label2);
            keyControl.Add("OK", btnRun);
            keyControl.Add("Cancel", btnCancel);
            Common.SettingLanguage(keyControl);
        }

        private void UI_SprinklerDown_Load(object sender, EventArgs e)
        {
            //C3
            ckbConnectCo90.Checked = false;
            rbtnOptions1.Checked = true;

            AddFamilyTypeC3();

            AppUtils.ff(cboC3PipeType);
            AppUtils.ff(cboC3PipeSize);
            AppUtils.ff(rbtnOptions1);
            AppUtils.ff(rbtnOptions2);
            AppUtils.ff(rbtnOptions3);
            AppUtils.ff(rbtnOptions4);

            if (rbtnOptions1.Checked)
                tbC4L2.Enabled = true;
            else
                tbC4L2.Enabled = false;

            if (rbtnOptions4.Checked)
            {
                ckbConnectCo90.Checked = true;
                ckbConnectCo90.Enabled = false;
            }
            else
                ckbConnectCo90.Enabled = true;
        }


        public bool isTeeTap = false;

        public double PipeSizeC3
        {
            get
            {
                var value = cboC3PipeSize.SelectedItem.ToString();

                value = value.Replace(" mm", "");

                double d = 0;
                if (double.TryParse(value, out d) == false)
                    return double.MaxValue;

                return d;
            }
        }

        public double Height_
        {
            get
            {
                double height = 0;

                if (double.TryParse(tbC4L2.Text.Trim(), out height) == true)
                {
                    return height;
                }

                return double.MinValue;
            }
        }

        public FamilySymbol ElbowFamilySymbol
        {
            get
            {
                return ((ItemFamilySymbol)cbbElbow.SelectedItem)?.Symbol;
            }
        }



        private void cboC3PipeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var familyTypeId = (cboC3PipeType.SelectedItem as ObjectItem).ObjectId;

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
                    AddDiameterC3(CurrentSegment);
            }
        }

        private void tbC4L2_KeyPress(object sender, KeyPressEventArgs e)
        {
            Common.NumberCheck(sender, e, false);
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            AppUtils.sa(cboC3PipeType);
            AppUtils.sa(cboC3PipeSize);
            AppUtils.sa(rbtnOptions1);
            AppUtils.sa(rbtnOptions2);
            AppUtils.sa(rbtnOptions3);
            AppUtils.sa(rbtnOptions4);
            AppUtils.sa(ckbConnectCo90);
            AppUtils.sa(tbC4L2);

            if (Height_ == double.MinValue)
                return;

            isTeeTap = !ckbConnectCo90.Checked;

            if (rbtnOptions1.Checked)
            {
                SetFocus();
                MakeRequest(RequestId.SprinklerDownType1_RUN);
            }
            else if (rbtnOptions2.Checked)
            {
                SetFocus();
                MakeRequest(RequestId.SprinklerDownType3_RUN);
            }
            else if (rbtnOptions3.Checked)
            {
                SetFocus();
                MakeRequest(RequestId.SprinklerDownType2_RUN);
            }
            else if (rbtnOptions4.Checked)
            {
                SetFocus();
                MakeRequest(RequestId.SprinklerDownType4_RUN);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void rbtnOptions1_CheckedChanged(object sender, EventArgs e)
        {
            RadioButtonCheckedChange();
        }

        private void rbtnOptions3_CheckedChanged(object sender, EventArgs e)
        {
            RadioButtonCheckedChange();
        }

        private void rbtnOptions2_CheckedChanged(object sender, EventArgs e)
        {
            RadioButtonCheckedChange();
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

        private void AddFamilyTypeC3()
        {
            cboC3PipeType.Items.Clear();
            FilteredElementCollector pipeTypes = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(PipeType));
            foreach (MEPCurveType type in pipeTypes)
            {
                ObjectItem item = new ObjectItem(type.Name, type.Id);
                cboC3PipeType.Items.Add(item);
            }

            AppUtils.ff(cboC3PipeType, null);

            if (cboC3PipeType.SelectedItem == null && cboC3PipeType.Items.Count != 0)
                cboC3PipeType.SelectedIndex = 0;
        }

        private void AddDiameterC3(Segment segment)
        {
            cboC3PipeSize.Items.Clear();

            foreach (MEPSize size in segment.GetSizes())
            {
                var value = Common.FeetToMmString(size.NominalDiameter) + " mm";

                cboC3PipeSize.Items.Add(value);
            }

            AppUtils.ff(cboC3PipeSize);

            if (cboC3PipeSize.SelectedItem == null && cboC3PipeSize.Items.Count != 0)
                cboC3PipeSize.SelectedIndex = 0;
        }

        private void CheckPreviewSprinkerDown()
        {
            string folder = Common.GetFirePreviewFolder();

            if (rbtnOptions1.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "Phuongan1.png"));
            else if (rbtnOptions2.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "Phuongan3.png"));
            else if (rbtnOptions3.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "Phuongan2.png"));
            else if (rbtnOptions4.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(_previewFolder, "A_Dau phun huong xuong_Type 4.jpg"));
        }

        private void RadioButtonCheckedChange()
        {
            if (rbtnOptions1.Checked)
                tbC4L2.Enabled = true;
            else
                tbC4L2.Enabled = false;

            if (rbtnOptions4.Checked)
            {
                ckbConnectCo90.Checked = true;
                ckbConnectCo90.Enabled = false;
            }
            else
            {
                ckbConnectCo90.Enabled = true;
            }

            // Family Elbow chỉ dùng cho Phương án 5 cũ của Dirit (đã bỏ theo sheet Fire Protection);
            // hàng này đang ẩn trong Designer, giữ lại cho chế độ Reducing Elbow sắp làm.
            cbbElbow.Enabled = false;

            CheckPreviewSprinkerDown();
        }

    }
}
