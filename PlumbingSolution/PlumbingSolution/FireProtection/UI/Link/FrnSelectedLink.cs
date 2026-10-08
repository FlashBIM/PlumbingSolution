using PlumbingSolution.FireProtection.Ultis;
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
    public partial class FrnSelectedLink : System.Windows.Forms.Form
    {
        private List<RevitLinkInstance> _revitLinkInstances;
        private List<string> _selectedLinks;
        public List<string> SelectedLinks { get; private set; } = new List<string>();

        public FrnSelectedLink(List<RevitLinkInstance> revitLinkInstances, List<string> selectedLinks)
        {
            InitializeComponent();
            Common.SettingTemplate(this);
            _revitLinkInstances = revitLinkInstances;
            _selectedLinks = selectedLinks;
            Init();
        }

        private void Init()
        {
            dgvLink.Rows.Clear();
            foreach (var revitLink in _revitLinkInstances)
            {
                var name = revitLink.GetLinkDocument().Title;

                bool isChecked = _selectedLinks.Contains(name);

                var index = dgvLink.Rows.Add(isChecked, name);
            }
        }

        private void btnAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dgvLink.Rows.Count; i++)
            {
                dgvLink.Rows[i].Cells[0].Value = true;
            }
        }

        private void btnUnCheck_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dgvLink.Rows.Count; i++)
            {
                dgvLink.Rows[i].Cells[0].Value = false;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dgvLink.Rows.Count; i++)
            {
                bool isChecked = Convert.ToBoolean(dgvLink.Rows[i].Cells[0].Value);
                if (isChecked)
                {
                    string name = dgvLink.Rows[i].Cells[1].Value.ToString();
                    SelectedLinks.Add(name);
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void dgvLink_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        }
    }
}
