using PlumbingSolution.Ultis;
using System;
using System.Windows.Forms;

namespace PlumbingSolution.UI.BeginUI
{
    public partial class FormMessage : Form
    {
        public FormMessage(string text, string title)
        {
            InitializeComponent();
            txtMessage.Text = text;
            this.Text = title;
        }

        public FormMessage()
        {
            InitializeComponent();

            txtMessage.Text = Define.LicenseAccountNotActive;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}