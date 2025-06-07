using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FenceReplanner
{
    public partial class FenceReplannerUI : Form
    {
        public FenceReplannerUI()
        {
            InitializeComponent();
        }

        private void but_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FenceReplannerUI_Load(object sender, EventArgs e)
        {

        }

        private void lbl_GitHubLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/yuri-rage/mission-planner-fence-replanner");
        }
    }
}
