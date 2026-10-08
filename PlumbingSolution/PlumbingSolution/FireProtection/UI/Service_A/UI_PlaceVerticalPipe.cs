using PlumbingSolution.FireProtection.RequestForm;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.UI.Service_A
{
    public partial class UI_PlaceVerticalPipe : System.Windows.Forms.Form
    {
        private MEPType m_MEPCurrent = MEPType.Pipe;
        private List<ElementId> FamilyTypes = new List<ElementId>();
        private List<ElementId> MEPSystemTypes = new List<ElementId>();
        private Segment CurrentSegment = null;
        private Dictionary<string, object> DictionaryMEPSizes = new Dictionary<string, object>();

        private Request m_request = null;
        private RequestHandler m_handler = null;
        private ExternalEvent m_exEvent = null;

        public ElementId FamilyType
        {
            get
            {
                return (cboFamilyType.SelectedItem as ObjectItem).ObjectId;
            }
        }

        public ElementId SystemType
        {
            get
            {
                return (cboSystemType.SelectedItem as ObjectItem).ObjectId;
            }
        }

        public MEPType MEPType_
        {
            get
            {
                return ParseMEPType(cboMEPObjects.SelectedItem.ToString());
            }
        }

        public ElementId LevelTopId
        {
            get
            {
                if (cboLevelTop.SelectedItem == null)
                    return ElementId.InvalidElementId;
                return (cboLevelTop.SelectedItem as ObjectItem).ObjectId;
            }
        }

        public ElementId LevelBottomId
        {
            get
            {
                if (cboLevelBottom.SelectedItem == null)
                    return ElementId.InvalidElementId;
                return (cboLevelBottom.SelectedItem as ObjectItem).ObjectId;
            }
        }

        public double OffsetTop
        {
            get
            {
                double value = 0;
                if (double.TryParse(txtOffsetTop.Text.Trim(), out value) == false)
                {
                    return double.MinValue;
                }

                return value;
            }
        }

        public double OffsetBottom
        {
            get
            {
                double value = 0;
                if (double.TryParse(txtOffsetBottom.Text.Trim(), out value) == false)
                {
                    return double.MinValue;
                }

                return value;
            }
        }

        public string ServiceType
        {
            get
            {
                return txtServiceType.Text.Trim();
            }
        }

        public double MEP_Width
        {
            get
            {
                var value = cboWidth.Text/*SelectedText*/.ToString().Trim();

                value = value.Replace(" mm", "");

                double dvalue = 0;
                if (value != string.Empty && double.TryParse(value, out dvalue) == false)
                {
                    return double.MaxValue;
                }

                return dvalue;
            }
        }

        public double MEP_Height
        {
            get
            {
                var value = cboHeight.Text.ToString().Trim();
                value = value.Replace(" mm", "");

                double dvalue = 0;
                if (value != string.Empty && double.TryParse(value, out dvalue) == false)
                {
                    return double.MaxValue;
                }

                return dvalue;
            }
        }

        public object MEPSize_
        {
            get
            {
                var value = cboDiameter.SelectedItem.ToString();

                if (DictionaryMEPSizes.ContainsKey(value) == true)
                    return DictionaryMEPSizes[value];
                return null;
            }
        }

        public UI_PlaceVerticalPipe(ExternalEvent exEvent, RequestHandler handler)
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
            keyControl.Add("btnPlaceVerticalPipe", this);
            keyControl.Add("BaseOffset", label8);
            keyControl.Add("BaseLevel", label7);
            keyControl.Add("TopOffset", label6);
            keyControl.Add("TopLevel", label5);
            keyControl.Add("BaseTopElevation", groupBox2);
            keyControl.Add("Height", label10);
            keyControl.Add("Width", label4);
            keyControl.Add("SystemType", lblServiceType);
            keyControl.Add("MEPType", label2);
            keyControl.Add("MEPCategory", label1);
            keyControl.Add("MEPCategoryGroup", groupBox1);
            keyControl.Add("OK", btnRun);
            keyControl.Add("Cancel", btnCancel);

            Common.SettingLanguage(keyControl);
        }

        private void UI_CreateVerticalPipe_Load(object sender, EventArgs e)
        {
            AddMEPType();
            AddTopLevel();
            AddBottomLevel();

            AppUtils.ff(txtOffsetBottom);
            AppUtils.ff(txtOffsetTop);
            AppUtils.ff(cboDiameter);

            AppUtils.ff(this);
        }

        private void AddMEPType()
        {
            cboMEPObjects.Items.Clear();

            cboMEPObjects.Items.Add(MEPTypeText(MEPType.Pipe));
            cboMEPObjects.Items.Add(MEPTypeText(MEPType.Rectangular_Duct));
            cboMEPObjects.Items.Add(MEPTypeText(MEPType.Round_Duct));

            cboMEPObjects.Items.Add(MEPTypeText(MEPType.CableTray));
            cboMEPObjects.Items.Add(MEPTypeText(MEPType.Conduit));

            AppUtils.ff(cboMEPObjects, null, null);

            if (cboMEPObjects.SelectedItem == null && cboMEPObjects.Items.Count != 0)
                cboMEPObjects.SelectedIndex = 0;

            //Get current
            m_MEPCurrent = ParseMEPType(cboMEPObjects.SelectedItem.ToString());
        }

        // Tên hiển thị trong ô MEP Category: "Rectangular Duct", "Cable Tray"... (tên enum có dấu gạch dưới).
        private static string MEPTypeText(MEPType type)
        {
            return type == MEPType.CableTray ? "Cable Tray" : type.ToString().Replace('_', ' ');
        }

        private static MEPType ParseMEPType(string text)
        {
            foreach (MEPType type in Enum.GetValues(typeof(MEPType)))
                if (MEPTypeText(type) == text || type.ToString() == text)
                    return type;
            return MEPType.Pipe;
        }

        private void AddTopLevel()
        {
            cboLevelTop.Items.Clear();

            var coll = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(Level));

            if (coll.GetElementCount() == 0)
                return;

            foreach (Level level in coll.ToElements())
            {
                ObjectItem item = new ObjectItem(level.Name, level.Id);
                cboLevelTop.Items.Add(item);
            }

            AppUtils.ff(cboLevelTop);

            if (cboLevelTop.SelectedItem == null && cboLevelTop.Items.Count != 0)
                cboLevelTop.SelectedIndex = 0;
        }

        private void AddBottomLevel()
        {
            cboLevelBottom.Items.Clear();

            var coll = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(Level));

            if (coll.GetElementCount() == 0)
                return;

            foreach (Level level in coll.ToElements())
            {
                ObjectItem item = new ObjectItem(level.Name, level.Id);
                cboLevelBottom.Items.Add(item);
            }

            AppUtils.ff(cboLevelBottom);

            if (cboLevelBottom.SelectedItem == null && cboLevelBottom.Items.Count != 0)
                cboLevelBottom.SelectedIndex = 0;
        }

        private void SaveControl()
        {
            AppUtils.sa(cboLevelBottom);
            AppUtils.sa(cboLevelTop);
            AppUtils.sa(cboMEPObjects);

            AppUtils.sa(txtOffsetBottom);
            AppUtils.sa(txtOffsetTop);
            AppUtils.sa(cboDiameter);

            AppUtils.sa(this);
            SetFocus();
        }

        private void SetFocus()
        {
            IntPtr hBefore = DisplayService.GetForegroundWindow();
            DisplayService.SetForegroundWindow(ComponentManager.ApplicationWindow);
        }

        private void cboMEPObjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_MEPCurrent = ParseMEPType(cboMEPObjects.SelectedItem.ToString());

            DisplayType();

            AppUtils.ff(txtServiceType, null, m_MEPCurrent.ToString());
        }

        private Type GetSytemType(MEPType enumType)
        {
            if (enumType == MEPType.Pipe)
                return typeof(PipingSystemType);
            else if (enumType == MEPType.Oval_Duct || enumType == MEPType.Rectangular_Duct || enumType == MEPType.Round_Duct)
                return typeof(MechanicalSystemType);
            else
                return null;
        }

        private void DisplayType()
        {
            AddFamilyType(m_MEPCurrent);

            AddSystemType(m_MEPCurrent);
        }

        private void AddSystemType(MEPType enumType)
        {
            cboSystemType.Items.Clear();

            var typeClass = GetSytemType(enumType);
            if (typeClass == null)
                return;

            FilteredElementCollector pipeTypes = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeClass);
            foreach (MEPSystemType type in pipeTypes)
            {
                ObjectItem item = new ObjectItem(type.Name, type.Id);
                cboSystemType.Items.Add(item);

                FamilyTypes.Add(type.Id);
            }

            AppUtils.ff(cboSystemType, null, m_MEPCurrent.ToString());

            if (cboSystemType.SelectedItem == null && cboSystemType.Items.Count != 0)
                cboSystemType.SelectedIndex = 0;
        }

        private Type GetType(MEPType enumType)
        {
            if (enumType == MEPType.Pipe)
                return typeof(PipeType);
            else if (enumType == MEPType.CableTray)
                return typeof(CableTrayType);
            else if (enumType == MEPType.Oval_Duct || enumType == MEPType.Rectangular_Duct || enumType == MEPType.Round_Duct)
                return typeof(DuctType);
            else
                return typeof(ConduitType);
        }

        private void AddFamilyType(MEPType enumType)
        {
            cboFamilyType.Items.Clear();
            FilteredElementCollector pipeTypes = new FilteredElementCollector(Global.UIDoc.Document).OfClass(GetType(enumType));
            foreach (MEPCurveType type in pipeTypes)
            {
                if (enumType == MEPType.Oval_Duct || enumType == MEPType.Rectangular_Duct || enumType == MEPType.Round_Duct)
                {
                    DuctShape shape = DuctShape.Oval;

                    var familyName = type.LookupParameter("Family Name").AsString();

                    if (familyName.Contains("Rectangular"))
                    {
                        shape = DuctShape.Rectangular;
                    }
                    else if (familyName.Contains("Round"))
                    {
                        shape = DuctShape.Round;
                    }

                    if (enumType == MEPType.Oval_Duct && shape != DuctShape.Oval)
                        continue;

                    if (enumType == MEPType.Rectangular_Duct && shape != DuctShape.Rectangular)
                        continue;

                    if (enumType == MEPType.Round_Duct && shape != DuctShape.Round)
                        continue;
                }

                ObjectItem item = new ObjectItem(type.Name, type.Id);
                cboFamilyType.Items.Add(item);

                FamilyTypes.Add(type.Id);
            }

            AppUtils.ff(cboFamilyType, null, m_MEPCurrent.ToString());

            if (cboFamilyType.SelectedItem == null && cboFamilyType.Items.Count != 0)
                cboFamilyType.SelectedIndex = 0;
        }

        private void cboFamilyType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboFamilyType.SelectedItem == null)
                return;

            var familyTypeId = (cboFamilyType.SelectedItem as ObjectItem).ObjectId;

            var familyType = Global.UIDoc.Document.GetElement(familyTypeId) as MEPCurveType;

            if (familyType is PipeType)
            {
                if (familyType.RoutingPreferenceManager != null)
                {
                    CurrentSegment = null;
                    int count = familyType.RoutingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);

                    for (int i = 0; i < count; i++)
                    {
                        var rule = familyType.RoutingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Segments, i);

                        CurrentSegment = Global.UIDoc.Document.GetElement(rule.MEPPartId) as PipeSegment;
                    }

                    if (CurrentSegment != null)
                        AddDiameter(CurrentSegment);
                }

                lblServiceType.Text = "System Type";

                txtServiceType.Visible = false;
                cboSystemType.Visible = true;

                cboDiameter.Enabled = true;
                cboWidth.Enabled = false;
                cboHeight.Enabled = false;
            }
            else if (familyType is ConduitType)
            {
                var setings = ConduitSizeSettings.GetConduitSizeSettings(Global.UIDoc.Document);

                var standardId = familyType.LookupParameter("Standard").AsElementId();

                if (standardId != ElementId.InvalidElementId)
                {
                    var standard = Global.UIDoc.Document.GetElement(standardId) as ElementType;

                    AddDiameter(setings, standard.Name);
                }
                lblServiceType.Text = "Service Type";

                txtServiceType.Visible = true;
                cboSystemType.Visible = false;

                cboDiameter.Enabled = true;
                cboWidth.Enabled = false;
                cboHeight.Enabled = false;
            }
            else if (familyType is CableTrayType)
            {
                CableTraySizes sizes = CableTraySizes.GetCableTraySizes(Global.UIDoc.Document);

                AddCableTraySizes(sizes);

                lblServiceType.Text = "Service Type";

                txtServiceType.Visible = true;
                cboSystemType.Visible = false;

                cboDiameter.Enabled = false;
                cboWidth.Enabled = true;
                cboHeight.Enabled = true;
            }
            else if (familyType is DuctType)
            {
                var settings = DuctSizeSettings.GetDuctSizeSettings(Global.UIDoc.Document);
                DuctShape shape = DuctShape.Oval;

                var familyName = familyType.LookupParameter("Family Name").AsString();

                if (familyName.Contains("Rectangular"))
                {
                    shape = DuctShape.Rectangular;

                    txtServiceType.Visible = false;
                    cboSystemType.Visible = true;

                    cboDiameter.Enabled = false;
                    cboWidth.Enabled = true;
                    cboHeight.Enabled = true;
                }
                else if (familyName.Contains("Round"))
                {
                    shape = DuctShape.Round;

                    txtServiceType.Visible = false;
                    cboSystemType.Visible = true;

                    cboDiameter.Enabled = true;
                    cboWidth.Enabled = false;
                    cboHeight.Enabled = false;
                }

                AddDuctSize(settings, shape);

                lblServiceType.Text = "System Type";
            }
        }

        private void AddDuctSize(DuctSizeSettings settings, DuctShape shape)
        {
            foreach (KeyValuePair<DuctShape, DuctSizes> keyPair in settings)
            {
                if (keyPair.Key != shape)
                    continue;

                if (keyPair.Key == DuctShape.Round)
                {
                    cboDiameter.Items.Clear();
                    DictionaryMEPSizes.Clear();

                    foreach (MEPSize size in keyPair.Value)
                    {
                        var value = FeetToMmString(size.NominalDiameter) + " mm";

                        cboDiameter.Items.Add(value);

                        DictionaryMEPSizes.Add(value, size);
                    }

                    AppUtils.ff(cboDiameter, null, m_MEPCurrent.ToString());

                    if (cboDiameter.SelectedItem == null && cboDiameter.Items.Count != 0)
                        cboDiameter.SelectedIndex = 0;
                }
                else
                {
                    cboWidth.Items.Clear();
                    cboHeight.Items.Clear();

                    DictionaryMEPSizes.Clear();

                    foreach (MEPSize size in keyPair.Value)
                    {
                        var value = FeetToMmString(size.NominalDiameter) + " mm";

                        cboWidth.Items.Add(value);
                        cboHeight.Items.Add(value);
                        DictionaryMEPSizes.Add(value, size);
                    }

                    AppUtils.ff(cboWidth, null, m_MEPCurrent.ToString());
                    AppUtils.ff(cboHeight, null, m_MEPCurrent.ToString());

                    if (cboWidth.SelectedItem == null && cboWidth.Items.Count != 0)
                        cboWidth.SelectedIndex = 0;

                    if (cboHeight.SelectedItem == null && cboHeight.Items.Count != 0)
                        cboHeight.SelectedIndex = 0;
                }
            }
        }

        private void AddDiameter(ConduitSizeSettings settings, string standardName)
        {
            cboDiameter.Items.Clear();
            DictionaryMEPSizes.Clear();

            foreach (KeyValuePair<string, ConduitSizes> keyPair in settings)
            {
                if (keyPair.Key != standardName)
                    continue;

                foreach (ConduitSize size in keyPair.Value)
                {
                    var value = FeetToMmString(size.NominalDiameter) + " mm";

                    cboDiameter.Items.Add(value);

                    DictionaryMEPSizes.Add(value, size);
                }
            }

            AppUtils.ff(cboDiameter, null, m_MEPCurrent.ToString());

            if (cboDiameter.SelectedItem == null && cboDiameter.Items.Count != 0)
                cboDiameter.SelectedIndex = 0;
        }

        private void AddDiameter(Segment segment)
        {
            cboDiameter.Items.Clear();
            DictionaryMEPSizes.Clear();

            foreach (MEPSize size in segment.GetSizes())
            {
                var value = FeetToMmString(size.NominalDiameter) + " mm";

                cboDiameter.Items.Add(value);

                DictionaryMEPSizes.Add(value, size);
            }

            AppUtils.ff(cboDiameter, null, m_MEPCurrent.ToString());

            if (cboDiameter.SelectedItem == null && cboDiameter.Items.Count != 0)
                cboDiameter.SelectedIndex = 0;
        }

        public string FeetToMmString(double a)
        {
            return (a / Common.mmToFT).ToString("0.##");
        }

        private void AddCableTraySizes(CableTraySizes sizes)
        {
            cboWidth.Items.Clear();
            cboHeight.Items.Clear();

            DictionaryMEPSizes.Clear();

            foreach (MEPSize size in sizes)
            {
                var value = FeetToMmString(size.NominalDiameter) + " mm";

                cboWidth.Items.Add(value);
                cboHeight.Items.Add(value);
                DictionaryMEPSizes.Add(value, size);
            }

            AppUtils.ff(cboWidth, null, m_MEPCurrent.ToString());
            AppUtils.ff(cboHeight, null, m_MEPCurrent.ToString());

            if (cboWidth.SelectedItem == null && cboWidth.Items.Count != 0)
                cboWidth.SelectedIndex = 0;

            if (cboHeight.SelectedItem == null && cboHeight.Items.Count != 0)
                cboHeight.SelectedIndex = 0;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            PressCancel();
            SaveControl();

            PressCancel();

            AppUtils.sa(this);

            App.m_PlaceVerticalPipeForm = null;

            this.Close();
        }

        public void PressCancel(int count = 2)
        {
            IWin32Window _revit_window = new WindowHandle(ComponentManager.ApplicationWindow);

            for (int i = 0; i < count; i++)
            {
                Press.PostMessage(_revit_window.Handle, (uint)Press.KEYBOARD_MSG.WM_KEYDOWN, (uint)Keys.Escape, 0);
            }
        }

        private void txtOffsetTop_KeyPress(object sender, KeyPressEventArgs e)
        {
            NumberCheck(sender, e, true);
        }

        public void NumberCheck(object sender, KeyPressEventArgs e, bool allowNegativeValue = false) // < 0
        {
            if (allowNegativeValue == false)
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
                {
                    e.Handled = true;
                }

                // only allow one decimal point
                if ((e.KeyChar == '.') && ((sender as System.Windows.Forms.TextBox).Text.IndexOf('.') > -1))
                {
                    e.Handled = true;
                }
            }
            else
            {
                if (!char.IsControl(e.KeyChar) && (!char.IsDigit(e.KeyChar)) && (e.KeyChar != '.') && (e.KeyChar != '-'))
                    e.Handled = true;

                // only allow one decimal point
                if (e.KeyChar == '.' && (sender as System.Windows.Forms.TextBox).Text.IndexOf('.') > -1)
                    e.Handled = true;

                // only allow minus sign at the beginning
                if (e.KeyChar == '-' && (sender as System.Windows.Forms.TextBox).Text.IndexOf('-') > -1)
                    e.Handled = true;
            }
        }

        private void txtOffsetBottom_KeyPress(object sender, KeyPressEventArgs e)
        {
            NumberCheck(sender, e, true);
        }

        public void MakeRequest(RequestId request)
        {
            m_handler.Request.Make(request);
            m_exEvent.Raise();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            SaveControl();
            SetFocus();

            MakeRequest(RequestId.PlaceVerticalPipe);
        }
    }
}