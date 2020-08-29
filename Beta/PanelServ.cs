using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.IO;


using PDA.Service;

namespace SkladRM
{
    public partial class MainF : Form
    {


        // при активации панели сервиса
        private void EnterInServ(){
        }

        // установка доступности полей
        private void EnableServF(int nT, bool b)
        {
        }

        // куда поставить фокус ввода
        private void SetFocusPServ()
        {
        }
        

        private void SetPServlFields()
        {

        }

        private void TestEmm()
        {
            int
                nE,
                nWithNE;

            DataTable 
                dtM = xNSI.DT[NSI.NS_MC].dt;
            DataRelation 
                myRelation = dtM.ChildRelations[NSI.REL2EMK];
            DataRow[] 
                childRows;

                
            WriteProt(String.Format("========== Test NS_MC & EMK {0}", DateTime.Now));

            nWithNE = 0;
            foreach (DataRow dr in dtM.Rows)
            {
                childRows = dr.GetChildRows(myRelation);
                nE = childRows.Length;
                if ((nE > 1) || (nE < 1))
                {
                        nWithNE++;
                        WriteProt(String.Format("KRKMC={0} E={1} EAN={3} {2} ", dr["KRKMC"], nE, dr["SNM"], dr["EAN13"]));
                }

            }
            WriteProt(String.Format("========== Total = {0}", nWithNE));
            Srv.ErrorMsg(nWithNE.ToString());
        }


        private void btMem_Click(object sender, EventArgs e)
        {
            //ServClass.MemInfo();
            TestEmm();
        }

        //private Thread thScan;
        //private static bool bInScan = false;
        //private delegate void SCSt();
        //private static SCSt dgSc;

        //private void ScEmp()
        //{
        //    ScannerAll.TERM_TYPE tt = xBCScanner.nTermType;
        //    if (bInScan == true)
        //    {
        //        bInScan = false;
        //    }
        //    else
        //    {
        //        bInScan = true;
        //        switch (tt)
        //        {
        //            case ScannerAll.TERM_TYPE.SYMBOL:
        //                dgSc = new SCSt(  ((ScannerAll.Symbol.SymbolBarcodeScanner)xBCScanner).SoftStart );
        //                break;
        //            case ScannerAll.TERM_TYPE.DL_SCORP:
        //                dgSc = new SCSt( ((ScannerAll.DL.DLBarcodeScanner)xBCScanner).SoftStart );
        //                break;
        //            case ScannerAll.TERM_TYPE.PSC4410:
        //                break;
        //            default:
        //                break;
        //        }
        //        thScan = new Thread(new ThreadStart(ScanThrSym));
        //        thScan.Start();
        //    }
        //}

        //private static void ScanThrSym()
        //{
        //    int nWait = (10) * 1000;
        //    while (bInScan == true)
        //    {
        //        dgSc();
        //        Thread.Sleep(nWait);
        //    }
        //    nWait = 1;
        //}

        //private static void ScanThr()
        //{
        //    int nWait = (10) * 1000;
        //    while (bInScan == true)
        //    {
        //        dgSc();
        //        //((ScannerAll.DL.DLBarcodeScanner)xBCScanner).SoftStart();
        //        Thread.Sleep(nWait);
        //    }
        //    nWait = 1;
        //}

        private void btQuit_Click_1(object sender, EventArgs e)
        {
            //ShowTotMest();
            this.Close();
            //Application.Exit();
        }



        public static int nB = 0;
        private static bool bNetUse = true;
        private Thread t;
        public static string sStat = "";
        public static void WRFileNet()
        {
            int NDig = 10000;
            int iC = 0;
            string sPath = @"\\Ats3\ATS3Temp\sample.xml";
            long fsLen = 0;
            double nTotal;
            //string sPath = "sample.3xml";
            FileStream fs;

            while (bNetUse == true)
            {
                fs = new FileStream(sPath, FileMode.Create);
                // Create the writer for sBarCode.
                BinaryWriter w = new BinaryWriter(fs);
                // Write sBarCode to Test.sBarCode.
                for (int i = 0; i < NDig; i++)
                {
                    w.Write((int)i);
                }
                fsLen = fs.Length;
                w.Close();
                fs.Close();

                string sR = "";
                // Create the reader for sBarCode.
                fs = new FileStream(sPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                //StreamReader r = new StreamReader(sPath);

                BinaryReader r = new BinaryReader(fs);
                // Read sBarCode from Test.sBarCode.
                for (int i = 0; i < NDig; i++)
                {
                    sR = (r.ReadInt32()).ToString() + "-";
                }
                r.Close();
                iC = iC + 1;
                nB = nB + 1;
                Thread.Sleep(15000);

            }
            nTotal = (fsLen * iC);
            nTotal = nTotal / (1024 * 1024);
            MessageBox.Show("W/R cycles - " + iC.ToString() + " Total W/R -" + nTotal.ToString("N") + "M");
            bNetUse = true;
            nB = 0;
            sStat = "";

        }
        private void button4_Click(object sender, EventArgs e)
        {
            //ThreadStart ts = new ThreadStart( WRFileNet );
            if (sStat == "")
            {
                sStat = DateTime.Now.TimeOfDay.ToString();
                t = new Thread(new ThreadStart(WRFileNet));
                t.Start();
            }
            //label1.Text = sStat + " - " + nB.ToString() + " Write/Read files";

        }

        private void button5_Click(object sender, EventArgs e)
        {
            bNetUse = false;
            //Thread.Sleep(1000);
            //MessageBox.Show("***");
        }





    }
}
