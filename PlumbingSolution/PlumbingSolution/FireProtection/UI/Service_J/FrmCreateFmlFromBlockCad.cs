using PlumbingSolution.FireProtection.ItemData;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.Service_J;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.UI.Service_J
{
    public partial class FrmCreateFmlFromBlockCad : System.Windows.Forms.Form
    {
        private CreateFamilyFromCADServices _services;
        private CreateFamilyFromCADSettingData _settingData;

        private bool isInitCommboBox = true;

        public LinkCADData SelectedLink
        {
            get
            {
                if (tbLinkCadName.Text.Trim().Length == 0)
                    return null;
                else
                    return _services.LinkCADDatas.FirstOrDefault(x => x.Name == tbLinkCadName.Text.Trim());
            }
        }

        private LinkCADData _linkCADData;
        public PickOptions PickOptions;

        public FrmCreateFmlFromBlockCad(CreateFamilyFromCADSettingData settingData,
                                      CreateFamilyFromCADServices services, LinkCADData linkCADData,
                                      string pickedBlockName,
                                      string pickedlinkName,
                                      PickOptions pickOptions,
                                      bool isAfterPick)
        {
            InitializeComponent();

            Common.SettingTemplate(this);
            _linkCADData = linkCADData;
            _services = services;
            _settingData = settingData;
            Init();

            if (isAfterPick)
            {
                if (pickOptions == PickOptions.PickLink)
                {
                    tbLinkCadName.Text = pickedlinkName;
                    rbtnPickBlockFromList.Checked = true;
                }
                else if (pickOptions == PickOptions.PickBlock)
                {
                    tbLinkCadName.Text = settingData.LinkName;
                    tbBlockName.Text = pickedBlockName;
                }
            }
            SettingLanguage();
        }

        private void SettingLanguage()
        {
            Dictionary<string, System.Windows.Forms.Control> keyControl = new Dictionary<string, System.Windows.Forms.Control>();
            keyControl.Add("btnCreateFamilyFromCAD", this);
            keyControl.Add("BlockName", label5);
            keyControl.Add("OtherSetting", grbStructure);
            keyControl.Add("SelectFamily", groupBox2);
            keyControl.Add("SelectCADBlock", grbBlock);
            keyControl.Add("SelectLinkCAD", groupBox5);
            keyControl.Add("StructuralObject", grbStructure);
            keyControl.Add("SelectCADBlockFromList", rbtnPickBlockFromList);
            keyControl.Add("PickCADBlock", rbtnPickBlock);
            keyControl.Add("FamilyElevation", label4);
            keyControl.Add("CADLinkName", label6);
            keyControl.Add("FamilyCategory", label7);
            keyControl.Add("InputElevation", rbtnManualElevation);
            keyControl.Add("CeilingElevation", rbtnCeilingElevation);
            keyControl.Add("FacebasedFamily", rbtnFaceElevation);
            keyControl.Add("FloorBottomElevation", rbtnFloorElevation);
            keyControl.Add("ProjectElement", rbtnProjectElement);
            keyControl.Add("OK", btnRun);
            keyControl.Add("Cancel", btnCancel);

            Common.SettingLanguage(keyControl);
        }

        private void Init()
        {
            InitLinkGroup();
            InitBlockGroup();
            InitFamilyGroup();
            InitOtherGroup();
        }

        private void InitLinkGroup()
        {
            if (_settingData.IsPickBlockFromList)
                rbtnPickBlockFromList.Checked = true;
            else
                rbtnPickBlock.Checked = true;

            isInitCommboBox = false;

            tbLinkCadName.Text = _linkCADData.Name;
        }

        private void InitBlockGroup()
        {
            if (SelectedLink != null)
            {
                var blockNames = SelectedLink.Blocks.Select(x => x.Name).Distinct().ToList();
                cbbBlockName.DataSource = blockNames;

                if (blockNames.Any(x => x == _settingData.BlockName))
                    cbbBlockName.SelectedItem = blockNames.FirstOrDefault(x => x == _settingData.BlockName);
                else
                    cbbBlockName.SelectedItem = blockNames.FirstOrDefault();

                if (rbtnPickBlockFromList.Checked)
                    tbBlockName.Text = cbbBlockName.SelectedItem == null ? string.Empty : cbbBlockName.SelectedItem.ToString();
                else
                    tbBlockName.Text = _settingData.BlockName;

                SetEnableBlockGroup(SelectedLink.IsExplodedGroup);
            }
        }

        private void InitFamilyGroup()
        {
            var categoryMapping = _services.CategoryMapping.Keys.ToList();
            cbbCategoryFamily.DataSource = categoryMapping;

            if (categoryMapping.Contains(_settingData.FamilyCategory))
                cbbCategoryFamily.SelectedItem = _settingData.FamilyCategory;
            else
                cbbCategoryFamily.SelectedItem = categoryMapping.FirstOrDefault();

            var families = cbbFamily.DataSource as List<string>;
            if (families == null)
            {
                var categoryId = _services.CategoryMapping[cbbCategoryFamily.SelectedItem.ToString()];
                families = _services.Families.Where(x => x.FamilyCategory.Id.ToInt() == categoryId).Select(x => x.Name).ToList();

                cbbFamily.DataSource = null;
                cbbFamily.DataSource = families.Count > 0 ? families : null;
                cbbFamily.DisplayMember = "Name";
            }

            if (families != null && families.Count > 0)
            {
                if (families.Any(x => x == _settingData.Family))
                    cbbFamily.SelectedItem = _settingData.Family;
                else
                    cbbFamily.SelectedItem = families.FirstOrDefault();
            }

            var types = cbbFamilyType.DataSource as List<string>;
            if (types == null)
            {
                if (cbbFamily.SelectedItem != null)
                    cbbFamilyType.DataSource = GetAllTypesName(cbbFamily.SelectedItem.ToString());
                else
                    cbbFamilyType.DataSource = null;
            }

            if (types != null)
            {
                if (types.Any(x => x == _settingData.FamilyType))
                    cbbFamilyType.SelectedItem = _settingData.FamilyType;
                else
                    cbbFamilyType.SelectedItem = types.FirstOrDefault();
            }

            cbbLevel.DataSource = _services.Levels;
            cbbLevel.DisplayMember = "Name";
            if (_services.Levels.Any(x => x.Name == _settingData.Level))
                cbbLevel.SelectedItem = _services.Levels.FirstOrDefault(x => x.Name == _settingData.Level);
            else
                cbbLevel.SelectedItem = _services.Levels.FirstOrDefault();

            tbElevation.Text = _settingData.ManualElevation;
        }

        private void InitOtherGroup()
        {
            switch (_settingData.ElevationType)
            {
                case ElevationType.ManualElevation:
                    rbtnManualElevation.Checked = true;
                    break;

                case ElevationType.ByFloorElevation:
                    rbtnFloorElevation.Checked = true;
                    break;

                case ElevationType.ByFace:
                    rbtnFaceElevation.Checked = true;
                    break;

                case ElevationType.ByCeilingElevation:
                    rbtnCeilingElevation.Checked = true;
                    break;
            }

            if (_settingData.IsProjectElement)
                rbtnProjectElement.Checked = true;
            else
                rbtnLinkElement.Checked = true;

            btnLink.Enabled = rbtnLinkElement.Checked;
            grbStructure.Enabled = !rbtnManualElevation.Checked;
        }

        private void rbtnLinkElement_CheckedChanged(object sender, EventArgs e)
        {
            btnLink.Enabled = rbtnLinkElement.Checked;
        }

        private void cbbCategoryFamily_SelectedIndexChanged(object sender, EventArgs e)
        {
            var categoryId = _services.CategoryMapping[cbbCategoryFamily.SelectedItem.ToString()];
            List<string> families = _services.Families.Where(x => x.FamilyCategory.Id.ToInt() == categoryId).Select(x => x.Name).ToList();

            cbbFamily.DataSource = null;
            cbbFamily.DataSource = families.Count > 0 ? families : null;
            cbbFamily.DisplayMember = "Name";
        }

        private void tbLinkCadName_TextChanged(object sender, EventArgs e)
        {
            //hide last link cad
        }

        private void btnExploreCAD_Click(object sender, EventArgs e)
        {
            var lastDisplayLink = _services.LinkCADDatas.FirstOrDefault(x => x.IsDisplayInRevit);
            if (lastDisplayLink != null)
                _services.LinkCadDisplayControl(lastDisplayLink, false);

            if (SelectedLink != null)
            {
                if (!isInitCommboBox)
                {
                    if (!SelectedLink.IsExplodedLine)
                        _services.ExploreDWGImport(SelectedLink);
                }

                var blockNames = SelectedLink.Blocks.Select(x => x.Name).Distinct().ToList();
                cbbBlockName.DataSource = blockNames;
                SetEnableBlockGroup(true);

                _services.LinkCadDisplayControl(SelectedLink, true);
            }
            else
            {
                cbbBlockName.DataSource = null;
                SetEnableBlockGroup(false);
            }

            if (SelectedLink != null)
            {
                if (_services.CreateBlockGroup(SelectedLink))
                {
                    btnExploreCAD.Enabled = false;
                    SetEnableBlockGroup(true);
                }
            }
        }

        private void btnPickBlock_Click(object sender, EventArgs e)
        {
            SaveSetting();
            PickOptions = PickOptions.PickBlock;
            DialogResult = DialogResult.Abort;
        }

        private void cbbFamily_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbbFamily.SelectedItem != null)
                cbbFamilyType.DataSource = GetAllTypesName(cbbFamily.SelectedItem.ToString());
            else
                cbbFamilyType.DataSource = null;
        }

        private void cbbFamily_DataSourceChanged(object sender, EventArgs e)
        {
            if (cbbFamily.DataSource == null)
                cbbFamilyType.DataSource = null;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (IsValidInput())
            {
                SaveSetting();
                DialogResult = DialogResult.OK;
            }
        }

        private void rbtnManualElevation_CheckedChanged(object sender, EventArgs e)
        {
            grbStructure.Enabled = !rbtnManualElevation.Checked;
            tbElevation.Enabled = true;
        }

        private void rbtnCeilingElevation_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnCeilingElevation.Checked)
                tbElevation.Enabled = false;
        }

        private void rbtnFaceElevation_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnFaceElevation.Checked)
                tbElevation.Enabled = false;
        }

        private void rbtnFloorElevation_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnFloorElevation.Checked)
                tbElevation.Enabled = false;
        }

        private void SaveSetting()
        {
            _settingData.LinkName = tbLinkCadName.Text;
            _settingData.IsPickBlockFromList = rbtnPickBlockFromList.Checked;
            _settingData.BlockName = tbBlockName.Text.Trim();
            _settingData.FamilyCategory = cbbCategoryFamily.SelectedItem.ToString();
            _settingData.Family = cbbFamily.SelectedItem == null ? string.Empty : cbbFamily.SelectedItem.ToString();
            _settingData.FamilyType = cbbFamilyType.SelectedItem == null ? string.Empty : cbbFamilyType.SelectedItem.ToString();
            _settingData.Level = (cbbLevel.SelectedItem as Level).Name;
            _settingData.ManualElevation = tbElevation.Text.Trim();
            _settingData.IsProjectElement = rbtnProjectElement.Checked;

            _settingData.ElevationType = rbtnManualElevation.Checked ? ElevationType.ManualElevation :
                                         rbtnFloorElevation.Checked ? ElevationType.ByFloorElevation :
                                         rbtnCeilingElevation.Checked ? ElevationType.ByCeilingElevation :
                                         ElevationType.ByFace;

            _settingData.IsCreateBlockInOrigin = false;
            DirectoryUtils.WriteCreateFamilyFromCADData(_settingData);
        }

        private bool IsValidInput()
        {
            if (tbLinkCadName.Text.Trim().Length == 0)
            {
                IO.ShowWarning("Select a CAD link to continue.");
                return false;
            }

            if (rbtnManualElevation.Checked && tbElevation.Text.Trim().Length == 0)
            {
                IO.ShowWarning("Enter an elevation to continue.");
                tbElevation.Focus();
                return false;
            }

            if (cbbFamily.SelectedItem == null ||
                cbbFamilyType.SelectedItem == null ||
                cbbLevel.SelectedItem == null)
            {
                IO.ShowWarning("Invalid input.");
                return false;
            }

            if (_services.Families.FirstOrDefault(x => x.Name == cbbFamily.SelectedItem.ToString()) is Family family)
            {
                if (rbtnFaceElevation.Checked && family.FamilyPlacementType != FamilyPlacementType.WorkPlaneBased)
                {
                    IO.ShowWarning("This family cannot be used for face-based placement.");
                    return false;
                }

                if (!rbtnFaceElevation.Checked && family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased)
                {
                    IO.ShowWarning("The selected family is face-based. Choose \"Face-based family\" to continue.");
                    return false;
                }
            }

            if (grbStructure.Enabled && rbtnLinkElement.Checked && _settingData.SelectedLinks.Count == 0)
            {
                IO.ShowWarning("Select a Revit link to continue.");
                return false;
            }

            return true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void cbo_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (sender is System.Windows.Forms.ComboBox comboBox)
                {
                    object[] items = new object[comboBox.Items.Count];
                    comboBox.Items.CopyTo(items, 0);
                    comboBox.DropDownWidth = items.Select(obj => TextRenderer.MeasureText(comboBox.GetItemText(obj), comboBox.Font).Width).Max();
                }
            }
            catch { }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && (!char.IsDigit(e.KeyChar))
                && (e.KeyChar != '.'))
                e.Handled = true;

            // only allow one decimal point
            if (e.KeyChar == '.' && (sender as System.Windows.Forms.TextBox).Text.IndexOf('.') > -1)
                e.Handled = true;

            // only allow one decimal point
            if (e.KeyChar == '.' && (sender as System.Windows.Forms.TextBox).Text.IndexOf('.') > -1)
                e.Handled = true;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Control == true && e.KeyCode == Keys.V) ||
                (e.Shift == true && e.KeyCode == Keys.Insert))
            {
                if (!double.TryParse(Clipboard.GetText(), out double result) || result < 0)
                    e.SuppressKeyPress = true;
            }
        }

        private void SetEnableBlockGroup(bool isEnable)
        {
            rbtnPickBlock.Enabled = isEnable;
            tbBlockName.Enabled = isEnable;
            btnPickBlock.Enabled = isEnable;
        }

        private List<string> GetAllTypesName(string familyName)
        {
            var family = _services.Families.FirstOrDefault(x => x.Name == familyName);
            return family.GetFamilySymbolIds()
                         .Select(x => family.Document.GetElement(x).Name)
                         .ToList();
        }

        private void btnLink_Click(object sender, EventArgs e)
        {
            FrnSelectedLink frnSelectedLink = new FrnSelectedLink(_services.RevitLinks, _settingData.SelectedLinks);
            if (frnSelectedLink.ShowDialog() == DialogResult.OK)
            {
                _settingData.SelectedLinks = frnSelectedLink.SelectedLinks;
            }
        }
    }
}
