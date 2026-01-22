using System;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class Chooser : Form
    {
        public Chooser()
        {
            InitializeComponent();

            AcceptButton = btnNew;
            CancelButton = btnExit;
        }
    }
}
