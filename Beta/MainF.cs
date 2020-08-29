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
                    //p = new Point(136, 140);
                    p = new Point(1, 1);
                    s = new Size(82, 21);
                    break;
                case TERM_TYPE.UNKNOWN:
                case TERM_TYPE.HWELLHX2:
                    p = new Point(216, 60);
                    s = new Size(72, 18);
                    break;
                case TERM_TYPE.SYMBOL:
                default:
                    p = new Point(1, 1);
                    s = new Size(0, 0);
                    break;
            }
            InitializeDop(xSc, s, p);
        }


    }
}