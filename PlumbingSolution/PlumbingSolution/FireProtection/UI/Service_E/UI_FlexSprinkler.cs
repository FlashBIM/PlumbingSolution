using PlumbingSolution.FireProtection.RequestForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
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
using System.Windows.Controls;
using System.Windows.Forms;
using Form = System.Windows.Forms.Form;
using GroupBox = System.Windows.Forms.GroupBox;

namespace PlumbingSolution.FireProtection.UI.Service_E
{
    public partial class UI_FlexSprinkler : Form
    {
        private Request m_request;

        private RequestHandler m_handler;

        private ExternalEvent m_exEvent;

        public UI_FlexSprinkler(ExternalEvent exEvent, RequestHandler handler)
        {
            InitializeComponent();
            Common.SettingTemplate(this);

            m_handler = handler;
            m_exEvent = exEvent;

            SettingLanguage();
        }

        private void SettingLanguage()
        {
            Dictionary<string, System.Windows.Forms.Control> keyControl = new Dictionary<string, System.Windows.Forms.Control>();
            keyControl.Add("btnsprinklerFlex", this);
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


        public double MainPipeSprinklerDistance
        {
            get => 1000;
        }

        public double EndPointSprinklerDistance
        {
            get => 1000;
        }

        public bool IsCheckedTee
        {
            get => !ckbConnectCo90.Checked;
        }

        public bool IsCheckedType1
        {
            get => rbtnOptions1.Checked;
        }

        public bool IsCheckedType2
        {
            get => rbtnOptions2.Checked;
        }

        public bool IsCheckedType3
        {
            get => rbtnOptions3.Checked;
        }

        public bool IsCheckedType4
        {
            get => rbtnOptions4.Checked;
        }

        public bool IsSprinklerSize
        {
            get => true;
        }

        public ElementId FamilyTypeC4
        {
            get
            {
                return (cboC4PipeType.SelectedItem as ObjectItem).ObjectId;
            }
        }

        public double PipeSizeC4
        {
            get
            {
                var value = cboC4PipeSize.SelectedItem.ToString();

                value = value.Replace(" mm", "");

                double d = 0;
                if (double.TryParse(value, out d) == false)
                    return double.MaxValue;

                return d;
            }
        }

        public double VerticalPipeLengthL2
        {
            get
            {
                double dHeight = 0;

                if (double.TryParse(tbC4L2.Text.Trim(), out dHeight) == true)
                {
                    return dHeight;
                }
                return double.MinValue;
            }
        }

        public double HorizontalPipeLengthL
        {
            get
            {
                double dHeight = 0;

                if (double.TryParse(tbC4L.Text.Trim(), out dHeight) == true)
                {
                    return dHeight;
                }
                return double.MinValue;
            }
        }

        public double ExtendPipeLengthL1
        {
            get
            {
                double dHeight = 0;

                if (double.TryParse(tbC4L1.Text.Trim(), out dHeight) == true)
                {
                    return dHeight;
                }
                return double.MinValue;
            }
        }



        private void UI_FlexSprinkler_Load(object sender, EventArgs e)
        {
            AddFamilyTypeC4();

            rbtnOptions1.Checked = true;
            ckbConnectCo90.Checked = false;

            AppUtils.ff(cboC4PipeType);
            AppUtils.ff(cboC4PipeSize);
            AppUtils.ff(rbtnOptions1);
            AppUtils.ff(rbtnOptions2);
            AppUtils.ff(rbtnOptions3);
            AppUtils.ff(rbtnOptions4);
            AppUtils.ff(tbC4L1);
            AppUtils.ff(tbC4L);
            AppUtils.ff(tbC4L2);
            AppUtils.ff(ckbConnectCo90);
        }

        private void tbC4L2_KeyPress(object sender, KeyPressEventArgs e)
        {
            Common.NumberCheck(sender, e, false);
        }

        private void tbC4L_KeyPress(object sender, KeyPressEventArgs e)
        {
            Common.NumberCheck(sender, e, false);
        }

        private void tbC4L1_KeyPress(object sender, KeyPressEventArgs e)
        {
            Common.NumberCheck(sender, e, false);
        }

        private void rbC4Type1_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnOptions1.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = true;
                tbC4L2.Enabled = true;
            }
            CheckPreviewSprinkerDown();
        }

        private void rbC4Type2_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnOptions2.Checked)
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
                tbC4L2.Enabled = true;
            }
            CheckPreviewSprinkerDown();
        }

        private void rbC4Type3_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnOptions3.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = true;
                tbC4L2.Enabled = false;
            }
            CheckPreviewSprinkerDown();
        }

        private void rbtnOptions4_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnOptions4.Checked)
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
                tbC4L2.Enabled = false;
            }
            CheckPreviewSprinkerDown();
        }

        private void cboC4PipeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var familyTypeId = (cboC4PipeType.SelectedItem as ObjectItem).ObjectId;

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
                    AddDiameterC4(CurrentSegment);
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            AppUtils.sa(cboC4PipeType);
            AppUtils.sa(cboC4PipeSize);
            AppUtils.sa(rbtnOptions1);
            AppUtils.sa(rbtnOptions2);
            AppUtils.sa(rbtnOptions3);
            AppUtils.sa(rbtnOptions4);
            AppUtils.sa(tbC4L1);
            AppUtils.sa(tbC4L);
            AppUtils.sa(tbC4L2);
            AppUtils.sa(ckbConnectCo90);

            if (VerticalPipeLengthL2 == double.MinValue && tbC4L2.Enabled)
                return;

            if (ExtendPipeLengthL1 == double.MinValue && tbC4L1.Enabled)
                return;

            if (HorizontalPipeLengthL == double.MinValue && tbC4L.Enabled)
                return;

            MakeRequest(RequestId.FlexSprinker_RUN);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }



        private void CheckPreviewSprinkerDown()
        {
            string folder = Common.GetFirePreviewFolder();

            if (rbtnOptions1.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "PhuongAnMem1.png"));
            else if (rbtnOptions2.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "PhuongAnMem2.png"));
            else if (rbtnOptions3.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "PhuongAnMem3.png"));
            else if (rbtnOptions4.Checked)
                this.picPreview.Image = System.Drawing.Image.FromFile(Path.Combine(folder, "PhuongAnMem4.png"));
        }

        private void AddFamilyTypeC4()
        {
            cboC4PipeType.Items.Clear();
            FilteredElementCollector pipeTypes = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(PipeType));
            foreach (MEPCurveType type in pipeTypes)
            {
                ObjectItem item = new ObjectItem(type.Name, type.Id);
                cboC4PipeType.Items.Add(item);
            }

            AppUtils.ff(cboC4PipeType, null);

            if (cboC4PipeType.SelectedItem == null && cboC4PipeType.Items.Count != 0)
                cboC4PipeType.SelectedIndex = 0;
        }

        private void AddDiameterC4(Segment segment)
        {
            cboC4PipeSize.Items.Clear();

            foreach (MEPSize size in segment.GetSizes())
            {
                var value = Common.FeetToMmString(size.NominalDiameter) + " mm";

                cboC4PipeSize.Items.Add(value);
            }

            AppUtils.ff(cboC4PipeSize);

            if (cboC4PipeSize.SelectedItem == null && cboC4PipeSize.Items.Count != 0)
                cboC4PipeSize.SelectedIndex = 0;
        }

        public void MakeRequest(RequestId request)
        {
            m_handler.Request.Make(request);
            m_exEvent.Raise();
        }

    }
}
