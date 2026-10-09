using PlumbingSolution.FireProtection.RequestForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.Extensions;
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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.UI.Service_E
{
    public partial class UI_TwinSprinkler : System.Windows.Forms.Form
    {
        private RequestHandler m_handler;

        private ExternalEvent m_exEvent;


        public FamilySymbol fmlNipple = null;

        public bool IsCheckedTeeC5
        {
            get => !chkC5TeeTap.Checked;
        }

        public bool IsCheckedType1C5
        {
            get => rbC5Type1.Checked;
        }

        public bool IsCheckedType2C5
        {
            get => rbC5Type2.Checked;
        }

        public bool IsCheckedType3C5
        {
            get => rbC5Type3.Checked;
        }

        public bool IsCheckedType4C5
        {
            get => rbC5Type4.Checked;
        }

        public bool IsSprinklerSizeC5
        {
            get => true;
        }

        public ElementId FamilyTypeC5
        {
            get
            {
                return (cboC5PipeType.SelectedItem as ObjectItem).ObjectId;
            }
        }

        public bool IsCheckedC5Type5
        {
            get => rbC5Type5.Checked;
        }

        public bool IsCheckedVerticalTeeOffset
        {
            get => chkVerticalTeeOffset.Checked;
        }

        public bool IsCheckedNipple
        {
            get => chkUseNipple.Checked;
        }

        public double VerticalTeeOffset
        {
            get
            {
                var value = txtVerticalTeeOffset.Text;

                double d = 0;
                if (double.TryParse(value, out d) == false)
                    return double.MaxValue;

                return d;
            }
        }

        public double PipeSizeC5
        {
            get
            {
                var value = cboC5PipeSize.SelectedItem.ToString();

                value = value.Replace(" mm", "");

                double d = 0;
                if (double.TryParse(value, out d) == false)
                    return double.MaxValue;

                return d;
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

        public double MainPipeSprinklerDistance
        {
            get
            {
                double dHeight = 0;

                if (double.TryParse("1000", out dHeight) == true)
                {
                    return dHeight;
                }
                return double.MinValue;
            }
        }

        public double EndPointSprinklerDistance
        {
            get
            {
                double dHeight = 0;

                if (double.TryParse("700", out dHeight) == true)
                {
                    return dHeight;
                }
                return double.MinValue;
            }
        }

        public bool IsSprinklerSize
        {
            get => true;
        }

        public UI_TwinSprinkler(ExternalEvent exEvent, RequestHandler handler)
        {
            InitializeComponent();

            m_handler = handler;
            m_exEvent = exEvent;

            InitType6();
            Common.SettingTemplate(this);

            this.StartPosition = FormStartPosition.CenterScreen;

            if (chkVerticalTeeOffset.Checked)
                txtVerticalTeeOffset.Enabled = true;
            else
                txtVerticalTeeOffset.Enabled = false;

            if (chkUseNipple.Checked)
                cbNipple.Enabled = true;
            else
                cbNipple.Enabled = false;

            AddNippleC5();

            //C5
            AddFamilyTypeC5();

            AppUtils.ff(tbC4L1);
            AppUtils.ff(tbC4L);
            AppUtils.ff(cboC5PipeType);
            AppUtils.ff(cboC5PipeSize);
            AppUtils.ff(rbC5Type1);
            AppUtils.ff(rbC5Type2);
            AppUtils.ff(rbC5Type3);
            AppUtils.ff(rbC5Type4);
            AppUtils.ff(rbC5Type5);
            AppUtils.ff(rbC5Type6);
            AppUtils.ff(chkUseNipple);
            AppUtils.ff(txtVerticalTeeOffset);
            AppUtils.ff(cbNipple);

            if (rbC5Type5.Checked)
            {
                chkVerticalTeeOffset.Enabled = false;
                chkUseNipple.Enabled = false;
                cbNipple.Enabled = false;
            }
            else
            {
                chkVerticalTeeOffset.Enabled = true;
                chkUseNipple.Enabled = true;
                cbNipple.Enabled = true;
            }
            DisableControl();

            CheckPreviewImages();
            RefreshType6State();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            AppUtils.sa(tbC4L1);
            AppUtils.sa(tbC4L);
            AppUtils.sa(cboC5PipeType);
            AppUtils.sa(cboC5PipeSize);
            AppUtils.sa(rbC5Type1);
            AppUtils.sa(rbC5Type2);
            AppUtils.sa(rbC5Type3);
            AppUtils.sa(rbC5Type4);
            AppUtils.sa(rbC5Type5);
            AppUtils.sa(chkVerticalTeeOffset);
            AppUtils.sa(chkUseNipple);
            AppUtils.sa(txtVerticalTeeOffset);
            AppUtils.sa(cbNipple);
            SaveType6();

            if (rbC5Type6.Checked)
            {
                string error = ValidateType6();
                if (error != null)
                {
                    MessageBox.Show(this, error, "Twin Sprinkler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MakeRequest(RequestId.TwinSprinkerType6_RUN);
                return;
            }

            fmlNipple = cbNipple.SelectedItem as FamilySymbol;
            if (fmlNipple != null)
            {
                if (!fmlNipple.IsActive)
                    fmlNipple.Activate();
            }

            MakeRequest(RequestId.TwinSprinker_RUN);
        }

        public void MakeRequest(RequestId request)
        {
            m_handler.Request.Make(request);
            m_exEvent.Raise();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddFamilyTypeC5()
        {
            cboC5PipeType.Items.Clear();
            FilteredElementCollector pipeTypes = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(PipeType));
            foreach (MEPCurveType type in pipeTypes)
            {
                ObjectItem item = new ObjectItem(type.Name, type.Id);
                cboC5PipeType.Items.Add(item);
            }

            AppUtils.ff(cboC5PipeType, null);

            if (cboC5PipeType.SelectedItem == null && cboC5PipeType.Items.Count != 0)
                cboC5PipeType.SelectedIndex = 0;
        }

        private void AddDiameterC5(Segment segment)
        {
            cboC5PipeSize.Items.Clear();

            foreach (MEPSize size in segment.GetSizes())
            {
                var value = Common.FeetToMmString(size.NominalDiameter) + " mm";

                cboC5PipeSize.Items.Add(value);
            }

            AppUtils.ff(cboC5PipeSize);

            if (cboC5PipeSize.SelectedItem == null && cboC5PipeSize.Items.Count != 0)
                cboC5PipeSize.SelectedIndex = 0;
        }

        private void cboC5PipeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var familyTypeId = (cboC5PipeType.SelectedItem as ObjectItem).ObjectId;

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
                    AddDiameterC5(CurrentSegment);
            }
        }

        private void DisableControl()
        {
            if (rbC5Type1.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = false;
            }
            if (rbC5Type2.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = true;
            }
            if (rbC5Type3.Checked)
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
            }
            if (rbC5Type4.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = false;
            }
            if (rbC5Type5.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = false;
            }
        }

        private void rbC5Type1_CheckedChanged(object sender, EventArgs e)
        {
            CheckPreviewImages();

            if (rbC5Type1.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = false;
            }
            else
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
            }
        }

        private void rbC5Type2_CheckedChanged(object sender, EventArgs e)
        {
            CheckPreviewImages();
            if (rbC5Type2.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = true;
            }
            else
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
            }
        }

        private void rbC5Type3_CheckedChanged(object sender, EventArgs e)
        {
            CheckPreviewImages();
            if (rbC5Type3.Checked)
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
            }
            else
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
            }
        }

        private void rbC5Type4_CheckedChanged(object sender, EventArgs e)
        {
            CheckPreviewImages();
            if (rbC5Type4.Checked)
            {
                tbC4L1.Enabled = false;
                tbC4L.Enabled = false;
            }
            else
            {
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
            }
        }

        private void rbC5Type5_CheckedChanged(object sender, EventArgs e)
        {
            CheckPreviewImages();
            if (rbC5Type5.Checked)
            {
                chkVerticalTeeOffset.Enabled = false;
                chkUseNipple.Enabled = false;
                cbNipple.Enabled = false;
                tbC4L1.Enabled = false;
                tbC4L.Enabled = false;
            }
            else
            {
                chkVerticalTeeOffset.Enabled = true;
                chkUseNipple.Enabled = true;
                cbNipple.Enabled = true;
                tbC4L1.Enabled = true;
                tbC4L.Enabled = true;
            }
        }

        private void chkVerticalTeeOffset_CheckedChanged(object sender, EventArgs e)
        {
            if (chkVerticalTeeOffset.Checked)
                txtVerticalTeeOffset.Enabled = true;
            else
                txtVerticalTeeOffset.Enabled = false;
        }

        private void chkUseNipple_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseNipple.Checked)
                cbNipple.Enabled = true;
            else
                cbNipple.Enabled = false;
        }

        private void AddNippleC5()
        {
            //type nipple
            string folderNipples = DirectoryUtils.GetFamilyCategoryFolder("Pipe Fittings");

            List<string> lstFamilyNameNipples = Directory.GetFiles(folderNipples, "*.rfa", SearchOption.AllDirectories).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();

            var lstFmlNipple = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.Category.Id.ToInt() == (int)BuiltInCategory.OST_PipeFitting)
                .Where(x => lstFamilyNameNipples.Contains(x.FamilyName))
                .ToList();

            foreach (FamilySymbol fmlNipple in lstFmlNipple)
            {
                cbNipple.Items.Add(fmlNipple);
            }

            cbNipple.DisplayMember = "FamilyName";

            AppUtils.ff(cbNipple, null);
            if (cbNipple.SelectedItem == null && cbNipple.Items.Count != 0)
                cbNipple.SelectedIndex = 0;
        }

        private void txbC3Length_KeyPress(object sender, KeyPressEventArgs e)
        {
            Common.NumberCheck(sender, e, false);
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            if (btnPreview.Text == "Preview <<")
            {
                var left = this.Left;
                this.Size = this.MaximumSize;
                this.Left = left - (this.MaximumSize.Width - this.MinimumSize.Width);
                btnPreview.Text = "Preview >>";
            }
            else if (btnPreview.Text == "Preview >>")
            {
                var left = this.Left;
                this.Size = this.MinimumSize;
                this.Left = left + (this.MaximumSize.Width - this.MinimumSize.Width);
                btnPreview.Text = "Preview <<";
            }
        }

        private void CheckPreviewImages()
        {
            var previewPath = Common.GetFirePreviewFolder();
            try
            {
                if (rbC5Type1.Checked)
                {
                    this.pictureBox1.Image = System.Drawing.Image.FromFile(Path.Combine(previewPath, "Twin sprinkler_1.jpg"));
                }
                if (rbC5Type2.Checked)
                {
                    this.pictureBox1.Image = System.Drawing.Image.FromFile(Path.Combine(previewPath, "Twin sprinkler_2.jpg"));
                }
                if (rbC5Type3.Checked)
                {
                    this.pictureBox1.Image = System.Drawing.Image.FromFile(Path.Combine(previewPath, "Twin sprinkler_3.jpg"));
                }
                if (rbC5Type4.Checked)
                {
                    this.pictureBox1.Image = System.Drawing.Image.FromFile(Path.Combine(previewPath, "Twin sprinkler_4.jpg"));
                }
                if (rbC5Type5.Checked)
                {
                    this.pictureBox1.Image = System.Drawing.Image.FromFile(Path.Combine(previewPath, "Twin sprinkler_5.jpg"));
                }
                if (rbC5Type6 != null && rbC5Type6.Checked)
                {
                    ShowType6Preview();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
