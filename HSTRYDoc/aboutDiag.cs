using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class aboutDiag : Form
    {
        public aboutDiag()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/BlackFoX1991",
                UseShellExecute = true
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
