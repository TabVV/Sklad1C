using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

using ScannerAll;

namespace SkladRM
{
    public partial class MainF : Form
    {

        public MainF(BarcodeScanner xSc)
        {
            InitializeComponent();
            xSc.BCInvoker = this;

            Point p;
            Size s;
            switch (xSc.nTermType)
            {
                case TERM_TYPE.HWELL6100:
                case TERM_TYPE.DL_SCORP:
                    p = new Point(136, 140);
                    s = new Size(72, 20);
                    break;
                case TERM_TYPE.UNKNOWN:
                case TERM_TYPE.HWELLHX2:
                    p = new Point(216, 60);
                    s = new Size(72, 18);
                    break;
                case TERM_TYPE.SYMBOL:
                    p = new Point(267, 72);
                    s = new Size(46, 22);
                    break;
                default:
                    p = new Point(137, 141);
                    s = new Size(70, 18);
                    break;
            }
            InitializeDop(xSc, s, p);
        }

        private void tNsiLoadHost_Validated(object sender, EventArgs e)
        {

        }

        private void tDatMC_TextChanged(object sender, EventArgs e)
        {

        }







    }
}