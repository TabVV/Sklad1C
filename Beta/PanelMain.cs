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
using System.Reflection;

using ScannerAll;
using ExprDll;
using SavuSocket;
using PDA.OS;
using PDA.Service;

using FRACT = System.Decimal;

namespace SkladRM
{
    public partial class MainF : Form
    {

        // ��������� �� ������� ���� �����
        MainMenu 
            mmSaved;

        // ������ ����������
        public AppPars 
            xPars;

        // ������-������
        public BarcodeScanner 
            xBCScanner = null;
        public BarcodeScanner.BarcodeScanEventHandler 
            ehScan = null;
        
        // ������ ������ �� ��������
        private ScrMode 
            //xScrDoc, 
            xScrDet;

        // ������� ��������� �������
        public FuncDic 
            xFuncs;

        // ����� ������ �� �������������
        public NSI 
            xNSI;

        // ������� �����
        public Smena 
            xSm;

        // ������� � ������� ����
        public Dictionary<string, Srv.ExprAct> 
            xExpDic = new Dictionary<string, Srv.ExprAct>();
        public Expr 
            xGExpr = new Expr();

        public FuncPanel 
            xFPan;

        // ������� �������� ������ ����������
        public CurDoc 
            xCDoc = null;

        // ��������� �������
        private BATT_INF 
            xBBI;

        // ������� �������
        public const int PG_DOC     = 0;
        public const int PG_SCAN    = 1;
        public const int PG_SSCC    = 2;
        public const int PG_NSI     = 3;
        public const int PG_PAR     = 4;
        public const int PG_SRV     = 5;

        // ����� ��� ��������� ������������� �����
        private bool 
            bSkipChar = false;                 // �� ������������ ��������� ������

        // ���� ������ ��������������
        public bool 
            bEditMode = false;

        // ���������� ������ ��� ������� �������
        //delegate bool CurrFuncKeyHandler(int nPal, KeyEventArgs e);
        Srv.CurrFuncKeyHandler 
            ehCurrFunc = null;

        public static System.IO.StreamWriter 
            swProt;

        // ������ � ������� �� ��������� ������ (�����)
        public object[] 
            aAvtoPark = null;

        // ������ � ����������� ��� ������� ����
        public object 
            xDLLPars = null;
        public object[] 
            xDLLAPars = null;

        public string 
            sExeDir;

        // ������ � ����������� ��� ������ �� ���� � �������
        public ServerExchange 
            xExchg = null;

        private Srv.HelpShow 
            xHelpS;
        List<string> 
            xInf;


        private StreamWriter ProtStream(string sFName)
        {
            return (ProtStream(sFName, 500));
        }

        private StreamWriter ProtStream(string sFName, int nMaxLength)
        {
            bool
                bAppendWhenOpen = true;
            int
                MAX_PROT = 1024 * nMaxLength;
            string
                s = "";

            StreamWriter
                sw = null;

            try
            {
                if (File.Exists(sFName))
                {
                    using (FileStream fs = System.IO.File.OpenRead(sFName))
                    {
                        if (fs.Length > MAX_PROT)
                        {
                            byte[] fileData = new byte[fs.Length];
                            fs.Read(fileData, 0, (int)fs.Length);
                            s = Encoding.UTF8.GetString(fileData, 0, fileData.Length);
                            s = DateTime.Now.ToString("") + " - ����������\n" + s.Substring(s.Length - s.Length / 5);
                            bAppendWhenOpen = false;
                        }
                        fs.Close();
                    }
                }
                sw = new StreamWriter(sFName, bAppendWhenOpen);
                sw.WriteLine(s);
            }
            catch
            {
                sw = null;
            }
            return (sw);
        }



        private void InitializeDop(BarcodeScanner xSc, Size BatSize, Point BatLoc)
        {
            string 
                sExePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;

            sExeDir = System.IO.Path.GetDirectoryName(sExePath) + "\\";
            xBCScanner = xSc;
            long nMACNow = long.Parse(xBCScanner.WiFi.MACAddreess, System.Globalization.NumberStyles.HexNumber);
#if PSC
                if ((xBCScanner.nTermType == TERM_TYPE.PSC4220) || (xBCScanner.nTermType == TERM_TYPE.PSC4410))
                    ((ScannerAll.PSC.PSCBarcodeScanner)xBCScanner).SetScanHandler(this);
#endif
#if NRDMERLIN
            ((ScannerAll.Nordic.Nordics)xBCScanner).BarCodeScanKey =
            (int)ScannerAll.Nordic.Nordics.VK_NRD.VK_SCAN;
            //((ScannerAll.Nordic.NordicMerlin)xBCScanner).RFIDScanKey = W32.VK_F13;
            xBCScanner.Start();
#endif
            // ��������� ����������� ������� �� ������� ����������� ���������
            SetMainFuncDict(xBCScanner.nTermType, sExeDir);
            SetDocTypes();

            xFPan = new FuncPanel(this, this.pnLoadDocG);

            xPars = (AppPars)AppPars.InitPars(System.IO.Path.GetDirectoryName(sExePath));

            if ((nMACNow > 0) && (xPars.MACAdr != xBCScanner.WiFi.MACAddreess))
            {
                xPars.MACAdr = xBCScanner.WiFi.MACAddreess;
                AppPars.SavePars(xPars);
            }
#if SYMBOL
            if (xBCScanner.nTermType == TERM_TYPE.SYMBOL)
            {
                if (xPars.bArrowsWithShift == true)
                {
                    ((ScannerAll.Symbol.SymbolBarcodeScanner)xBCScanner).SetSpecKeyALP(true, AlpHandle);
                }
            }
#endif

            SetBindAppPars();
            //SetParAppFields();

            //if (xPars.DocTypes == null)
            //{
            //    xPars.DocTypes = new SerlzDict<string, DocTypeDef>();
            //    foreach (KeyValuePair<string, DocTypeDef> kvp in AppC.xDocTDef)
            //        xPars.DocTypes.Add(kvp.Key, kvp.Value);
            //}

            TimeSync.SyncAsync(xPars.NTPSrv, 10);

            xNSI = new NSI(xPars, this, new string[]{NSI.NS_USER, NSI.NS_SKLAD, NSI.NS_SUSK});
            xNSI.ConnDTGrid(dgDoc, dgDet);
            xNSI.InitTableSSCC(dgSSCC);
            FiltForDocs(xPars.bHideUploaded, xNSI.DT[NSI.BD_DOCOUT]);

            Smena.ReadSm(ref xSm, xPars.sDataPath);

            // ������� ��������� �������
            //xBBI = new BATT_INF(pnPars, BatSize, BatLoc);
            if (BatSize.Height + BatSize.Width == 0)
            {
                BatSize.Height = panBatt.Height - 1;
                BatSize.Width = panBatt.Width - 1;
            }
            xBBI = new BATT_INF(panBatt, BatSize, BatLoc);
            xBBI.BIFont = 8F;

            lFCgh.Text = "\xAD\xAF";
        }

        // �������� �������� �����
        private void SetDocTypes()
        {
            DocTypeDef
                xD;
            AppC.xDocTDef = new Dictionary<string, DocTypeDef>();

            CurOper.m_xMF = this;

            xD = new DocTypeDef("VP", AppC.TYPD_VOZV,       "�������", false, true, AppC.MOVTYPE.PRIHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("VZ", AppC.TYPD_VOZVPST,    "����������(���)", true, false, AppC.MOVTYPE.RASHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("SP", AppC.TYPD_ZAKAZV,     "����� ����������", true, false, AppC.MOVTYPE.RASHOD);
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("RG", AppC.TYPD_ZAKAZP,     "����� ����������", true, false, AppC.MOVTYPE.RASHOD);
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("AI", AppC.TYPD_INV,        "��������������", true, false, AppC.MOVTYPE.AVAIL); //
            xD.TryFromServer = false;
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("PP", AppC.TYPD_PRIHNKLD,   "�����������", false, true, AppC.MOVTYPE.PRIHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("RN", AppC.TYPD_RASHNKLD,   "��������� ���������", true, false, AppC.MOVTYPE.RASHOD);
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("PD", AppC.TYPD_MOVINT,       "���������������", true, true, AppC.MOVTYPE.MOVEMENT);
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("PR", AppC.TYPD_PRIHNKLD,   "�����������", false, true, AppC.MOVTYPE.PRIHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("MK", AppC.TYPD_MARK,       "����������", false, false, AppC.MOVTYPE.AVAIL);
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("ML", AppC.TYPD_SVOD,       "���������� ����", true, false, AppC.MOVTYPE.RASHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);


            xD = new DocTypeDef("I1", AppC.TYPD_PARTINV, "������������������", true, false, AppC.MOVTYPE.AVAIL); //
            xD.TryFromServer = false;
            AppC.xDocTDef.Add(xD.DocSighn, xD);


            xD = new DocTypeDef("GP", 61, "��������������", true, false, AppC.MOVTYPE.RASHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("GR", 62, "��������������", true, false, AppC.MOVTYPE.RASHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("GN", 63, "���������������", true, false, AppC.MOVTYPE.RASHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("G1", 64, "�����������_1", true, false, AppC.MOVTYPE.RASHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);

            xD = new DocTypeDef("G3", 65, "�����������_3", true, false, AppC.MOVTYPE.RASHOD); //
            AppC.xDocTDef.Add(xD.DocSighn, xD);
        }


        public string TName(int nIntCode)
        {
            string
                ret = "";
            foreach (DocTypeDef dd in AppC.xDocTDef.Values)
            {
                if (dd.NumCode == nIntCode)
                {
                    ret = dd.Name;
                    break;
                }
            }
            return (ret);
        }


        private DialogResult xLogonResult;
        public ManualResetEvent evReadNSI = null;

        private void Form1_Load(object sender, EventArgs e)
        {
            //xScrDoc = new ScrMode(dgDoc);
            xScrDet = new ScrMode(dgDet, tNameSc);

            swProt = ProtStream(xPars.sDataPath + "ProtTerm.txt", 200);

            // ������ - � ����� �����������
            evReadNSI = new ManualResetEvent(false);

            xDLLPars = AppC.AVT_LOGON;
            xLogonResult = CallDllForm(sExeDir + "SGPF-Avtor.dll", false);

            SetEditMode(false);

            if (xLogonResult != DialogResult.OK)
            {
                evReadNSI.Set();
                this.Close();
            }
            else
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    evReadNSI.WaitOne();
                    evReadNSI = null;
                    ehScan = new BarcodeScanner.BarcodeScanEventHandler(OnScan);
                    xBCScanner.BarcodeScan += ehScan;
                    AfterAddScan += new ScanProceededEventHandler(OnPoddonReady);
                    //ssListen = new SocketStream(11001);
                    //_processCommand = new ProcessSocketCommandHandler(ProcessSocketCommand);
                    //ssListen.MessageFromServerRecived += new MessageFromServerEventHandler(cs_MessageFromServerRecived);
                    //xNSI.AllNsiInf(true);
                    //xNSI.LoadLocNSI(new string[] { NSI.BD_TINF }, NSI.LOAD_ANY);
                    if (xNSI.DT[NSI.BD_TINF].nState == NSI.DT_STATE_READ)
                        xNSI.DT[NSI.BD_TINF].dt.AcceptChanges();

                    if (!AfterAuth(AppC.AVT_LOGON))
                        this.Close();
                    else
                    {
                        // �������� ��������� �������
                        xBBI.EnableShow = true;

                        xNSI.DSRestore(xPars.sDataPath, Smena.DateDef, xPars.Days2Save, true);
                        xSm.nDocs = xNSI.DT[NSI.BD_DOCOUT].dt.Rows.Count;

                        WriteAllToReg();
                        //Srv.LoadInterCode(xGExpr, xExpDic, xNSI.DT[NSI.BD_PASPORT]);
                        xHelpS = new Srv.HelpShow(this);
                        EnterInDoc();
                    }
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        // ������� �� ����� �� ���������
        private bool AfterAuth(int nReg)
        {
            bool bRet = AppC.RC_OKB;

            //if (xSm.urCur != Smena.USERRIGHTS.USER_SUPER)
            //{
            //    if ((xPars.ReLogon > 0) && !ResetTimerSmEnd())
            //    {// ����������� ����������� ��������
            //        Srv.ErrorMsg("����� ��������!", true);
            //        bRet = false;
            //    }
            //    else
            //        ResetTimerReLogon(false);
            //}
            return (bRet);
        }

        // ���������� ��������� �����
        //void xtmSmEnd_Tick(object sender, EventArgs e)
        //{
        //    bool bQuit = false;

        //    xSm.xtmSmEnd.Enabled = false;

        //    if (!bInScanProceed)
        //    {
        //        if (xSm.xtmTOut != null)
        //            // ������ ��������� � ��� ���������
        //            xSm.xtmTOut.Enabled = false;
        //        xDLLPars = AppC.AVT_LOGOFF;
        //        DialogResult xDRslt = CallDllForm(sExeDir + "SGPF-Avtor.dll", true);
        //        bQuit = (xDRslt != DialogResult.OK) ? true : !AfterAuth(AppC.AVT_LOGOFF);
        //        if (bQuit)
        //            this.Close();
        //    }
        //    else
        //    {// ��������� ��� ��� ����� 10 ������
        //        xSm.xtmSmEnd.Interval = 10 * 1000;
        //        xSm.xtmSmEnd.Enabled = false;
        //    }
        //}

        // ���������� �������� �� ����������� ���������
        //void xtmTOut_Tick(object sender, EventArgs e)
        //{
        //    bool bQuit = false;

        //    xSm.xtmTOut.Enabled = false;
        //    if (!bInScanProceed)
        //    {
        //        if (xSm.xtmSmEnd != null)
        //            // ������ ��������� � ��� ���������
        //            xSm.xtmSmEnd.Enabled = false;
        //        xDLLPars = AppC.AVT_TOUT;
        //        DialogResult xDRslt = CallDllForm(sExeDir + "SGPF-Avtor.dll", true);
        //        bQuit = (xDRslt != DialogResult.OK) ? true : !AfterAuth(AppC.AVT_TOUT);
        //        if (bQuit)
        //            this.Close();
        //    }
        //    else
        //    {// ��������� ��� ��� ����� 10 ������
        //        xSm.xtmTOut.Interval = 10 * 1000;
        //        xSm.xtmTOut.Enabled = true;
        //    }
        //}

        // ����� �������� �� ���������� ����� ������������
        //private bool ResetTimerSmEnd()
        //{
        //    bool bTimerStarted = true;
        //    if (xSm.tMinutes2SmEnd.TotalMinutes > 0)
        //    {
        //        // ����������� ����������� �������� � ����� ����������� �� �������
        //        if (xSm.xtmSmEnd != null)
        //        {
        //            xSm.xtmSmEnd.Enabled = false;
        //            //xSm.xtmSmEnd = null;
        //        }
        //        else
        //        {
        //            xSm.xtmSmEnd = new System.Windows.Forms.Timer();
        //            xSm.xtmSmEnd.Tick += new EventHandler(xtmSmEnd_Tick);
        //        }
        //        xSm.xtmSmEnd.Interval = (int)xSm.tMinutes2SmEnd.TotalMilliseconds;
        //        xSm.xtmSmEnd.Enabled = true;
        //    }
        //    return (bTimerStarted);
        //}

        // �����/���������� ������� ����������� ���������
        //private bool ResetTimerReLogon(bool bRestart)
        //{
        //    bool bTimerStarted = false;
        //    int nMinutesReLogon = Math.Abs(xPars.ReLogon);

        //    if (xSm.urCur == Smena.USERRIGHTS.USER_SUPER)
        //        return (false);

        //    if ((bRestart) && (xSm.xtmTOut != null))
        //    {// ������ ����� ���� ��������
        //        if (xSm.xtmTOut.Enabled == false)
        //            return (false);
        //    }

        //    if (nMinutesReLogon >= Smena.MIN_TIMEOUT)
        //    {// ������� ��� ���������� �� 5 �����
        //        if (xSm.xtmTOut != null)
        //        {
        //            xSm.xtmTOut.Enabled = false;
        //            //xSm.xtmTOut = null;
        //        }
        //        else
        //        {
        //            xSm.nMSecondsTOut = nMinutesReLogon * 60 * 1000;
        //            xSm.xtmTOut = new System.Windows.Forms.Timer();
        //            xSm.xtmTOut.Tick += new EventHandler(xtmTOut_Tick);

        //        }
        //        xSm.xtmTOut.Interval = xSm.nMSecondsTOut;
        //        xSm.xtmTOut.Enabled = true;
        //        bTimerStarted = true;
        //    }
        //    return(bTimerStarted);
        //}














        //private bool WriteAllToReg()
        //{
        //    bool ret = true;
        //    AssemblyName xAN = Assembly.GetExecutingAssembly().GetName();
        //    const string sApp = "SkladGP";

        //    ret &= Srv.WriteRegInfo("FIO", xSm.sUser + " " + xSm.sUName, sApp);           // ��� ������������, ������������ ���������
        //    ret &= Srv.WriteRegInfo("SkladgpLastRunTime", xSm.dBeg.ToString(), sApp);     // ����� ���������� ������� ���������
        //    ret &= Srv.WriteRegInfo("SkladgpVer", xAN.Version.ToString(), sApp);          // ������ ��������� SkladRM
        //    ret &= Srv.WriteRegInfo("SkladgpReg", (xSm.RegApp == AppC.REG_DOC)?"DOC":"OPR", sApp);   // ����� ������ SkladRM
        //    return (ret);
        //}

        private bool WriteAllToReg()
        {
            const string sApp = "SkladGP";
            bool
                ret = true;
            string
                sUserFlag = xSm.sUser + " " + xSm.sUName,
                sAppVer = (string)Srv.AppVerDT()[0];

                ret &= Srv.WriteRegInfo("SkladgpLastRunTime", xSm.dBeg.ToString(), sApp);     // ����� ���������� ������� ���������
                ret &= Srv.WriteRegInfo("SkladgpVer", sAppVer, sApp);           // ������ ��������� SkladGP
                ret &= Srv.WriteRegInfo("SkladgpReg", "DOC", sApp);             // ����� ������ SkladGP
                //(xSm.RegApp == AppC.REG_DOC) ? "DOC" : "MRK", sApp);          // ����� ������ SkladGP
                ret &= Srv.WriteRegInfo("FIO", sUserFlag, sApp);                // ��� ������������, ������������ ���������

            return (ret);
        }

        // ����� ������ ������ �������
//        public class FuncPanel
//        {
//            private MainF 
//                xF;
//            private Point 
//                pInvisible,
//                pVisible;
//            private bool 
//                bActive;

//            private Panel 
//                xPan;
//            private Control 
//                xTReg;
//            private Label 
//                xLabH, xLabF;

//            // �������� �� WinMobile �����
//            private int nWMDelta;


//            public FuncPanel(MainF f, Panel xPl)
//            {
//                xF = f;
//                if (xPl == null)
//                {
//                    xPan = xF.pnLoadDocG;
//                }
//                else
//                {
//                    xPan = xPl;
//                }

//                xTReg = xF.tbPanP1G;
//                xLabH = xF.lFuncNamePanG;
//                xLabF = xF.lpnLoadInfG;
//                xLabF.Text = "<Enter> - ������";
//                pInvisible = xPan.Location;
//#if (DOLPH7850 || DOLPH9950)
//                nWMDelta = -23;
//#else
//                nWMDelta = 0;
//#endif

//                pVisible = (xF.tcMain.SelectedIndex == PG_DOC) ? new Point(6, 60) : new Point(6, 90);
//                bActive = false;
//                xPan.Parent = xF;
//            }

//            private void ShowPNow(int x, int y, string sH, string sR)
//            {
//                if (bActive == false)
//                {
//                    bActive = true;
//                    xPan.SuspendLayout();
//                    xPan.Left = x;
//                    xPan.Top = y + nWMDelta;
//                    xLabH.Text = sH;
//                    xTReg.Text = sR;

//                    xPan.Visible = true;
//                    xPan.Enabled = true;
//                    xPan.BringToFront();
//                    xPan.Refresh();
//                    xPan.ResumeLayout();
//                }
//            }

//            public void ShowP(string s, string sR)
//            {
//                ShowPNow(pVisible.X, pVisible.Y, s, sR);
//            }

//            public void ShowP(int x, int y, string s, string sR)
//            {
//                ShowPNow(x, y, s, sR);
//            }

//            public void UpdateHead(string s)
//            {
//                xLabH.Text = s;
//                xLabH.Refresh();
//            }

//            public void UpdateReg(string s)
//            {
//                xTReg.Text = s;
//                xTReg.Refresh();
//            }
//            public void UpdateSrv(string s)
//            {
//                xF.tbPanP2G.Text = s;
//                xF.tbPanP2G.Refresh();
//            }

//            public void UpdateHelp(string s)
//            {
//                xLabF.Text = s;
//                xLabF.Refresh();
//            }


//            public void HideP()
//            {
//                if (bActive == true)
//                {
//                    bActive = false;
//                    xPan.SuspendLayout();
//                    xPan.Location = pInvisible;
//                    xPan.Visible = false;
//                    xPan.Enabled = false;
//                    xPan.ResumeLayout();
//                }
//            }

//            public void IFaceReset(bool bClear)
//            {
//                if (bClear)
//                {
//                    xF.lSrvGName.Text = "";
//                    xF.tbPanP2G.Text = "";
//                    xF.lFCgh.Text = "";
//                    xF.lpnLoadInfG.Text = "";
//                }
//                else
//                {
//                    xF.lSrvGName.Text = "������";
//                    xF.lFCgh.Text = "\xAD\xAF";
//                    xF.lpnLoadInfG.Text = "<Enter> - ������";
//                }
//            }

//            private int
//                m_OldHeight = -1;
//            public void InfoHeightUp(bool bSetNew, int nKoeff)
//            {
//                int 
//                    h;
//                if (bSetNew)
//                {
//                    h = (m_OldHeight != -1) ? m_OldHeight : xF.tbPanP1G.Height;
//                    m_OldHeight = h;
//                    xF.tbPanP1G.Height = h * nKoeff;
//                    xF.tbPanP1G.BringToFront();
//                }
//                else
//                {
//                    if (m_OldHeight != -1)
//                    {
//                        xF.tbPanP1G.Height = m_OldHeight;
//                        m_OldHeight = -1;
//                        xF.tbPanP1G.SendToBack();
//                    }
//                }
//            }



//            public bool IsShown
//            {
//                get {return bActive;}
//            }

//            public string RegInf
//            {
//                get { return xTReg.Text; }
//                set
//                {
//                    xTReg.Text = value;
//                    xTReg.Refresh();
//                }
//            }
//        }





        /// ����� ������ ������ �������
        public class FuncPanel
        {
            private const int
                C_REGH = 22;

            private bool
                bActive;
            private int
                nWMDelta;                       // �������� �� WinMobile �����

            private MainF
                xF;
            private Point
                pInvisible,
                pVisible;

            private Panel
                xPan;
            private Control
                xRetHere = null,
                xTReg;
            private Label
                xLabH, xLabF;

            public FuncPanel(MainF f, Panel xPl)
            {
                xF = f;
                if (xPl == null)
                {
                    xPan = xF.pnLoadDocG;
                }
                else
                {
                    xPan = xPl;
                }

                xTReg = xF.tbPanP1G;
                xLabH = xF.lFuncNamePanG;
                xLabF = xF.lpnLoadInfG;
                xLabF.Text = "<Enter> - ������";
                pInvisible = xPan.Location;
#if (DOLPH7850 || DOLPH9950)
                nWMDelta = -23;
#else
                nWMDelta = 0;
#endif

                pVisible = (xF.tcMain.SelectedIndex == PG_DOC) ? new Point(6, 60) : new Point(6, 90);
                bActive = false;
            }

            private void ShowPNow(int x, int y, string sH, string sR)
            {
                if (bActive == false)
                {
                    bActive = true;
                    xPan.SuspendLayout();
                    xPan.Left = x;
                    xPan.Top = y + nWMDelta;
                    xLabH.Text = sH;
                    xTReg.Text = sR;

                    xPan.Visible = true;
                    xPan.Enabled = true;
                    xPan.ResumeLayout();
                    xPan.Refresh();
                }
            }

            public void ShowP(string s, string sR)
            {
                ShowPNow(pVisible.X, pVisible.Y, s, sR);
            }

            public void ShowP(int x, int y, string s, string sR)
            {
                ShowPNow(x, y, s, sR);
            }

            public void ShowP(int x, int y, string s, string sR, Control cWhereBack)
            {
                xRetHere = cWhereBack;
                ShowPNow(x, y, s, sR);
            }


            public void UpdateHead(string s)
            {
                xLabH.Text = s;
                xLabH.Refresh();
            }

            public void UpdateReg(string s)
            {
                xTReg.Text = s;
                xTReg.Refresh();
            }
            public void UpdateSrv(string s)
            {
                xF.tbPanP2G.Text = s;
                xF.tbPanP2G.Refresh();
            }

            public void UpdateHelp(string s)
            {
                xLabF.Text = s;
                xLabF.Refresh();
            }

            public void HideP()
            {
                HideP(null);
            }

            public void HideP(Control cFocus)
            {
                if (bActive == true)
                {
                    bActive = false;
                    xPan.Location = pInvisible;
                    xPan.Visible = false;
                    xPan.Enabled = false;
                    if (cFocus != null)
                        cFocus.Focus();
                    else if (xRetHere != null)
                        xRetHere.Focus();
                    xRetHere = null;
                }
            }

            public void IFaceReset(bool bClear)
            {
                if (bClear)
                {
                    xF.lSrvGName.Text = "";
                    xF.tbPanP2G.Text = "";
                    xF.lFCgh.Text = "";
                    xF.lpnLoadInfG.Text = "";
                }
                else
                {
                    xF.lSrvGName.Text = "������";
                    xF.lFCgh.Text = "\xAD\xAF";
                    xF.lpnLoadInfG.Text = "<Enter> - ������";
                    xTReg.Height = C_REGH;
                }
            }

            private int
                m_OldHeight = -1;
            public void InfoHeightUp(bool bSetNew, int nKoeff)
            {
                int
                    h;
                if (bSetNew)
                {
                    h = (m_OldHeight != -1) ? m_OldHeight : xF.tbPanP1G.Height;
                    m_OldHeight = h;
                    xF.tbPanP1G.Height = h * nKoeff;
                    xF.tbPanP1G.BringToFront();
                }
                else
                {
                    if (m_OldHeight != -1)
                    {
                        xF.tbPanP1G.Height = m_OldHeight;
                        m_OldHeight = -1;
                        xF.tbPanP1G.SendToBack();
                    }
                }
            }


            public bool IsShown
            {
                get { return bActive; }
            }

            public string RegInf
            {
                get { return xTReg.Text; }
                set
                {
                    xTReg.Text = value;
                    xTReg.Refresh();
                }
            }

            public Control RegControl
            {
                get { return xTReg; }
            }

        }



        private void SelAllTextF(object sender, EventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }



        private void SaveCurData(bool bFinalSaving)
        {
            DataTable dtU = xNSI.DT[NSI.BD_TINF].dt.GetChanges(DataRowState.Unchanged);
            try
            {
                if ((dtU == null) ||
                    (dtU.Rows.Count != xNSI.DT[NSI.BD_TINF].dt.Rows.Count))
                    // ���-�� ��� �� ��������
                    xNSI.DT[NSI.BD_TINF].dt.WriteXml(xPars.sNSIPath + xNSI.DT[NSI.BD_TINF].sXML);

                // ���������� ������� ������ (���� ����)
                if (xLogonResult == DialogResult.OK)
                {// ����������� ������ �������
                    if (bFinalSaving)
                        xSm.SaveCS(xPars.sDataPath, xNSI.DT[NSI.BD_DOCOUT].dt.Rows.Count);
                    xNSI.DSSave(xPars.sDataPath);
                }
                if (bFinalSaving)
                {
                    if (swProt != null)
                        swProt.Close();
                }
            }
            catch { }
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            if (xBCScanner != null)
                xBCScanner.Dispose();
            SaveCurData(true);
            if (swProt != null)
                swProt.Close();

            Cursor.Current = Cursors.Default;
        }

        // �������������� ������� ������ (��� �������������)
        //private void TryRestoreUserDat()
        //{
        //    //Smena xSaved = null;
        //    //int nRet = Smena.ReadSm(ref xSaved, xPars.sDataPath);

        //    //int nRet = Smena.ReadSm(ref xSaved, xPars.sDataPath);
        //    //if (nRet == AppC.RC_OK)
        //    //{
        //        if (xSm.nDocs > 0)
        //        {// ������ ������������� ����
        //            //nRet = xNSI.DSRestore(false, xPars.sDataPath, Smena.DateDef, xPars.Days2Save);
        //            xNSI.DSRestore(xPars.sDataPath, Smena.DateDef, xPars.Days2Save);
        //            xSm.nDocs = xNSI.DT[NSI.BD_DOCOUT].dt.Rows.Count;
        //        }
        //    //}
        //}



        private int nPrevTab = -1;
        private bool bPrevMode;

        private void tcMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (nPrevTab == PG_PAR)
            {
                nPrevTab = -1;
                SetEditMode(bPrevMode);
            }

            switch (tcMain.SelectedIndex)
            {
                case PG_DOC:
                    EnterInDoc();
                    break;
                case PG_SCAN:
                    EnterInScan();
                    break;
                case PG_SSCC:
                    EnterInSSCC();
                    break;
                case PG_NSI:
                    EnterInNSI();
                    break;
                case PG_PAR:
                    bPrevMode = bEditMode;
                    nPrevTab = PG_PAR;
                    EnterInPars();
                    break;
                case PG_SRV:
                    EnterInServ();
                    break;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            bool bAlreadyProceed = false,   // ������� ��� ����������
                bHandledKey = false;
            int i = -1,
                nF = 0;

            #region ������ Scan-�������
            if (e.Modifiers == Keys.None)
            {// Scan-������� ��� Honeywell �� ������������
#if DOLPH9950
                if (e.KeyValue == 42)
                {
                    xBCScanner.Start();
                    e.Handled = true;
                    bSkipChar = e.Handled;
                    return;
                }
#endif
#if DOLPH7850
                if (e.KeyValue == 42)
                {
                    e.Handled = true;
                    bSkipChar = e.Handled;
                    return;
                }
#endif
#if DOLPH6100
                if (e.KeyValue == 193)
                {
                    e.Handled = true;
                    bSkipChar = e.Handled;
                    return;
                }
#endif
#if NRDMERLIN
                if (e.KeyValue == (int)ScannerAll.Nordic.Nordics.VK_NRD.VK_SCAN)
                {
                    e.Handled = true;
                    bSkipChar = e.Handled;
                    ((ScannerAll.Nordic.Nordics)xBCScanner).PerformScan();
                    return;
                }
#endif

            }
            #endregion

            bSkipChar = ServClass.HandleSpecMode(e, bEditMode, xBCScanner, xPars.bArrowsWithShift);
            if (bSkipChar == true)
                return;

            nF = xFuncs.TryGetFunc(e);


            if (bEditMode || bInEasyEditWait || (nSpecAdrWait == AppC.F_SIMSCAN))
            {// ��� ������ ��������������
                if ( Srv.IsDigKey(e, ref i) ||
                    (e.KeyValue == W32.VK_BACK) || 
                    (e.KeyValue == W32.VK_PERIOD))
                {
                    if (e.Modifiers == Keys.None)
                        nF = 0;
                }
            }
            try
            {
                if (ehCurrFunc != null)
                {// ������� ����� ���� �� �������
                    bAlreadyProceed = ehCurrFunc(nF, e, ref ehCurrFunc);
                }
                else
                {
                    // ��������� ������� � ������ � ������ �������� Control
                    if (tcMain.SelectedIndex == PG_DOC)
                        bAlreadyProceed = Doc_KeyDown(nF, e);
                    else if (tcMain.SelectedIndex == PG_SCAN)
                        bAlreadyProceed = Vvod_KeyDown(nF, e);
                    else if (tcMain.SelectedIndex == PG_NSI)
                        bAlreadyProceed = NSI_KeyDown(nF, e);
                    else if (tcMain.SelectedIndex == PG_PAR)
                        bAlreadyProceed = AppPars_KeyDown(nF, e);
                }

                if ((nF > 0) && (bAlreadyProceed == false))
                {// ����� ��������� �������
                    bHandledKey = ProceedFunc(nF, e, sender);
                }
            }
            catch
            {
                Srv.ErrorMsg("������ ���������", true);
            }

            // � ����� - ������ �������
            e.Handled = bAlreadyProceed || bHandledKey;
            if ((bAlreadyProceed == false) && (bHandledKey == false))
            {
                switch (e.KeyValue)     // ��� ���� �������
                {
                    case W32.VK_ENTER:
                        e.Handled = true;
                        break;
                }
            }
            //bSkipChar = e.Handled;

            bSkipChar = e.Handled || bAlreadyProceed || bHandledKey;
            //ResetTimerReLogon(true);
        }

        private void MainF_KeyUp(object sender, KeyEventArgs e)
        {

            try
            {
                if (e.KeyCode == (Keys)42)
                {
#if DOLPH9950
                    //--- If Still Trying to Decode, Cancel the Operation ---
                    //oDecodeAssembly.CancelScanBarcode();
                    xBCScanner.Stop();

                    //--- Add the KeyDown Event Handler ---
                    //this.KeyDown += new KeyEventHandler(Form1_KeyDown);

                    //--- The Key was Handled ---
                    e.Handled = true;
#endif
                }
            }
            catch
            {
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
           if (bSkipChar == true)
            {
                e.Handled = true;
                bSkipChar = false;
            }
        }
        private DialogResult CallDllForm(string sAs, bool bClearScHdlr)
        {
            return(CallDllForm(sAs, bClearScHdlr, null));
        }

        private DialogResult CallDllForm(string sAs, bool bClearScHdlr, object[] xPars4Form)
        {
            DialogResult xRet = DialogResult.None;
            Assembly xAs;
            Form xDllForm = null;
            bool bOldTOut = false;

            if (bClearScHdlr)
                xBCScanner.BarcodeScan -= ehScan;
            try
            {
                if (xSm.xtmTOut != null)
                {
                    bOldTOut = xSm.xtmTOut.Enabled;
                    xSm.xtmTOut.Enabled = false;
                }
                xAs = Assembly.LoadFrom(sAs);
                xDllForm = (Form)Activator.CreateInstance(xAs.GetTypes()[0]);
                // �������� ���������� � �����
                if (xPars4Form == null)
                    xDllForm.Tag = this;
                else
                    xDllForm.Tag = xPars4Form;
                xRet = xDllForm.ShowDialog();
            }
            catch(Exception e)
            {
                Srv.ErrorMsg(String.Format("����� {0}\n{1}",sAs, e.Message), true);
            }
            finally
            {
                if (xDllForm != null)
                {
                    xDLLPars = xDllForm.Tag;
                    xDllForm.Dispose();
                }
                if (bClearScHdlr)
                    xBCScanner.BarcodeScan += ehScan;
                xAs = null;
                if (xSm.xtmTOut != null)
                    xSm.xtmTOut.Enabled = bOldTOut;
            }
            return xRet;
        }













        private BindingSource SetBlankList()
        {
            BindingSource
                bsBlanks = new BindingSource();
            try
            {
                string sRf = String.Format("(TD={0})OR(TD<0)OR(ISNULL(TD,-1)<0)", xCDoc.xDocP.nNumTypD);
                DataView dv = new DataView(xNSI.DT[NSI.NS_BLANK].dt, sRf,
                    "TD DESC", DataViewRowState.CurrentRows);
                bsBlanks.DataSource = dv;
            }
            catch { }
            return (bsBlanks);
        }



        private int CallFrmPars()
        {
            int
                nRegCall,
                nRet = AppC.RC_CANCEL;
            DataRow
                drD = null;
            DialogResult xDRslt;
            BindingSource
                bsBlanks;

            bsBlanks = SetBlankList();
            if (bsBlanks.Count > 0)
            {
                if (bsBlanks.Count == 1)
                {
                    nRegCall = AppC.R_PARS;
                }
                else
                {
                    nRegCall = AppC.R_BLANK;
                }
                if (tcMain.SelectedIndex == PG_SCAN)
                    drD = drDet;

                //nRegCall = AppC.R_BLANK;
                Srv.ExchangeContext.ExchgReason = AppC.EXCHG_RSN.USER_COMMAND;
                xDRslt = CallDllForm(sExeDir + "SGPF-Univ.dll", true,
                    new object[] {this, AppC.COM_PRNBLK, nRegCall,
                            xSm.CurPrinterSTCName, xSm.CurPrinterMOBName, drD, bsBlanks});
                if (xDRslt == DialogResult.OK)
                {
                    xDLLAPars = (object[])xDLLPars;
                    xSm.CurPrinterSTCName = (string)xDLLAPars[0];
                    xSm.CurPrinterMOBName = (string)xDLLAPars[1];
                }
                Srv.ExchangeContext.ExchgReason = AppC.EXCHG_RSN.NO_EXCHG;

                if (tcMain.SelectedIndex == PG_SCAN)
                    dgDet.Focus();
                else
                    dgDoc.Focus();
            }
            else
            {
                Srv.ErrorMsg("��� �������!", true);
            }
            return (nRet);
        }


















        // ��������� ���������� ������� (��� �������)
        private bool ProceedFunc(int nFunc, KeyEventArgs e, object sender)
        {
            bool ret = false;
            //DataRow drD = null;
            DialogResult xDRslt;

            if (bEditMode == false)
            {// ������� ������ ��� ������ ���������
                switch (nFunc)     // ��� ���� �������
                {
                    case AppC.F_MENU:
                        CreateMMenu();              // ������� ����
                        ret = true;
                        break;
                    case AppC.F_LOAD_DOC:           // �������� ����������
                        LoadDocFromServer(AppC.F_INITREG, e, ref ehCurrFunc);
                        ret = true;
                        break;
                    case AppC.F_UPLD_DOC:           // �������� ����������
                        UploadDocs2Server(AppC.F_INITREG, e, ref ehCurrFunc);
                        ret = true;
                        break;
                    case AppC.F_VES_CONF:
                        string sMsg = "������������� ����� - ";
                        if (AppPars.bVesNeedConfirm == true)
                            sMsg += "���������";
                        else
                            sMsg += "��������";

                        AppPars.bVesNeedConfirm = !AppPars.bVesNeedConfirm;
                        MessageBox.Show(sMsg);
                        //if (tcMain.SelectedIndex == PG_SCAN)
                        //    ShowRegVvod();
                        ret = true;
                        break;
                    case AppC.F_CNTSSCC:
                        WhatSSCCContent();
                        ret = true;
                        break;
                    case AppC.F_TMPMOV:
                    case AppC.F_TMPMARK:
                        TempOperStartEnd(nFunc);
                        ret = true;
                        break;
                    case AppC.F_TMPOVER:
                        RetAfterTempMove();
                        ret = true;
                        break;
                    case AppC.F_CHKSSCC:
                        // �������� SSCC � ������
                        ret = true;
                        WaitScan4Func(AppC.F_CHKSSCC, "�������� SSCC", "������������ SSCC");
                        break;
                }
            }


            switch (nFunc)     // ��� ���� �������
            {
                case AppC.F_HELP:
                    // ����� ������ c ����� ������
                    //ShowInf(xFuncs.GetFHelp());
                    xHelpS.ShowInfo(xFuncs.GetFHelp(), ref ehCurrFunc);

                    ret = true;
                    break;
                case AppC.F_LASTHELP:
                    //ShowInf(null);
                    xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                    ret = true;
                    break;
                case AppC.F_VIEW_DOC:
                // �������� ��������� �����
                    if (tcMain.SelectedIndex == PG_DOC)
                        tcMain.SelectedIndex = PG_SCAN;
                    else
                        tcMain.SelectedIndex = PG_DOC;
                    ret = true;
                    break;
                case AppC.F_PREVPAGE:
                    // ���������� ��������
                    if (tcMain.SelectedIndex == 0)
                        tcMain.SelectedIndex = tcMain.TabPages.Count - 1;
                    else
                        tcMain.SelectedIndex--;
                    ret = true;
                    break;
                case AppC.F_NEXTPAGE:
                    // ��������� ��������
                    if (tcMain.SelectedIndex == (tcMain.TabPages.Count - 1))
                        tcMain.SelectedIndex = 0;
                    else
                        tcMain.SelectedIndex++;
                    ret = true;
                    break;
                case AppC.F_LOGOFF:
                    ret = true;
                    break;
                case AppC.F_SIMSCAN:
                    //OnScan(null, new BarcodeScannerEventArgs(BCId.Code128, "00946300037900000135"));

                    if ((tcMain.SelectedIndex == PG_SCAN)
                        || (tcMain.SelectedIndex == PG_DOC)
                        || (tcMain.SelectedIndex == PG_SSCC))
                    {
                        WaitScan4Func(AppC.F_SIMSCAN, "������ ������������", "");
                    }

                    ret = true;
                    break;
                case AppC.F_QUIT:
                    ExitApp();
                    break;
                //case AppC.F_PRNBLK:
                //    xDLLPars = AppC.FX_PRPSK;
                //    xDRslt = CallDllForm(sExeDir + "SGPF-Prn.dll", true);
                //    switch (tcMain.SelectedIndex)
                //    {
                //        case PG_DOC:
                //            dgDoc.Focus();
                //            break;
                //        case PG_SCAN:
                //            dgDet.Focus();
                //            break;
                //    }
                //    ret = true;
                //    break;
                case AppC.F_PRNBLK:
                case AppC.F_GENFUNC:
                    CallFrmPars();
                    ret = true;
                    break;
            }
            e.Handled |= ret;
            return (ret);
        }

        private void ExitApp()
        {
            DialogResult dr = MessageBox.Show(" ����� ?  (Enter)\n���������� ������ (ESC)",
                "���������� ������", MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.OK)
                this.Close();
        }

        private void SetEditMode(bool bEdit)
        {
            bEditMode = bEdit;
#if SYMBOL
            if ((xBCScanner.nTermType == TERM_TYPE.SYMBOL) && (xBCScanner.nKeys == 48))
            {
                if (xPars.bArrowsWithShift == true)
                {
                    ((ScannerAll.Symbol.SymbolBarcodeScanner)xBCScanner).bALP = !bEdit;
                }
            }
#endif
        }

        // ������ �������� ����
        private MenuItem
            miServ = new MenuItem(),
            miExch = new MenuItem(),
            miNsi = new MenuItem();

        private void Create1MenuItem(MenuItem xMI, int nN, string sMName, EventHandler eH)
        {
            xMI.MenuItems.Add(new MenuItem());
            xMI.MenuItems[nN].Click += new EventHandler(eH);
            xMI.MenuItems[nN].Text = String.Format("&{0} {1}", nN + 1, sMName);
        }

        private void CreateMMenu()
        {
            int
                nSrv = 0,
                nYForClick;


#if WMOBILE
            nYForClick = Screen.PrimaryScreen.Bounds.Height - 7;
#else
            nYForClick = 7;
#endif


            if (this.mmSaved == null)
            {// ������ �������� ����
                miExch.Text = "&���������";
                miNsi.Text  = "&���";
                miServ.Text = "&������";

                // ���� ���������
                Create1MenuItem(miExch, nSrv++, "���������",        MMenuClick_SaveCur);
                Create1MenuItem(miExch, nSrv++, "������������",     MMenuClick_RestDat);
                Create1MenuItem(miExch, nSrv++, "��������",         MMenuClick_WriteSock);
                Create1MenuItem(miExch, nSrv++, "��������",         MMenuClick_Load);
                Create1MenuItem(miExch, nSrv++, "�������������",    MMenuClick_Corr);
                Create1MenuItem(miExch, nSrv++, "���",              MMenuClick_LoadNSI);
                Create1MenuItem(miExch, nSrv++, "���������",        MMenuClick_SessPars);

                miExch.MenuItems.Add(new MenuItem());
                miExch.MenuItems[nSrv++].Text = "-";
                Create1MenuItem(miExch, nSrv++, "�����",            MMenuClick_Exit);

                // ���� ���
                Create1MenuItem(miNsi, 0, "�������� ���",           MMenuClick_LoadNSI);

                // ���� ��������� �������
                nSrv = 0;
                if (xSm.urCur > Smena.USERRIGHTS.USER_KLAD)
                {
                    Create1MenuItem(miServ, nSrv++, "��������� �������", MMenuClick_SetTime);
                }
                Create1MenuItem(miServ, nSrv++, "����������� � ����", MMenuClick_Reconnect);
                Create1MenuItem(miServ, nSrv++, "������������", MMenuClick_DoScan);
                if (xSm.urCur > Smena.USERRIGHTS.USER_KLAD)
                {
                    Create1MenuItem(miServ, nSrv++, "��������� ����������", MMenuClick_KeyMap);
                }
                Create1MenuItem(miServ, nSrv++, "������", MMenuClick_AppVer);

                // Create a MainMenu and assign MenuItem objects.
                MainMenu mainMenu1 = new MainMenu();
                mainMenu1.MenuItems.Add(miExch);
                mainMenu1.MenuItems.Add(miNsi);
                mainMenu1.MenuItems.Add(miServ);

                // Bind the MainMenu to Form1.
                this.mmSaved = mainMenu1;
            }
            this.SuspendLayout();
            if (this.Menu == null)
            {
                this.Menu = this.mmSaved;
                W32.SimulMouseClick(7, nYForClick, this);
            }
            else
                this.Menu = null;
            this.ResumeLayout();
        }

        private void MMenuClick_SaveCur(object sender, EventArgs e)
        {// ��������� ������� ������
            Cursor crsOld = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                SaveCurData(false);
                MessageBox.Show("���������...", "����������");
            }
            finally
            {
                Cursor.Current = crsOld;
                CreateMMenu();
            }

        }

        private void MMenuClick_Load(object sender, EventArgs e)
        {
            LoadDocFromServer(AppC.F_INITREG, new KeyEventArgs(Keys.Enter), ref ehCurrFunc);
            //StatAllDoc();
            CreateMMenu();
        }

        private void MMenuClick_WriteSock(object sender, EventArgs e)
        {
            UploadDocs2Server(AppC.F_INITREG, new KeyEventArgs(Keys.Enter), ref ehCurrFunc);
            //StatAllDoc();
            CreateMMenu();
        }

        private void MMenuClick_Corr(object sender, EventArgs e)
        {
            // �������������
            AddOrChangeDoc(AppC.F_CHG_REC);
            CreateMMenu();
        }


        private void Go1stLast(DataGrid dg, int nWhatPage)
        {
            CurrencyManager cmDoc = (CurrencyManager)BindingContext[dg.DataSource];
            if (cmDoc.Count > 0)
            {
                cmDoc.Position = (nWhatPage == AppC.F_GOFIRST) ? 0 : cmDoc.Count - 1;
                dg.Refresh();
            }
        }

        private void MMenuClick_RestDat(object sender, EventArgs e)
        {
                DialogResult drQ = MessageBox.Show("������������ ������(Enter)?\n(ESC) - ������", "��������������",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (drQ == DialogResult.OK)
                {
                    if (tcMain.SelectedIndex != PG_DOC)
                        tcMain.SelectedIndex = PG_DOC;
                    Cursor crsOld = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    //xNSI.DSRestore(xPars.sDataPath, Smena.DateDef, xPars.Days2Save, false);
                    if (AppC.RC_OK == xNSI.DSRestore(xPars.sDataPath, Smena.DateDef, xPars.Days2Save, false))
                    {
                        Go1stLast(dgDoc, AppC.F_GOFIRST);
                        MessageBox.Show("���������...", "��������������");
                    }
                    else
                    {
                        //dgDoc.Refresh();
                        RestShowDoc(false);
                        Srv.ErrorMsg("��� ������ ��������������!");
                    }
                    Cursor.Current = crsOld;
                }
            CreateMMenu();
        }

        private void MMenuClick_SessPars(object sender, EventArgs e)
        {
            xDLLPars = AppC.AVT_PARS;
            DialogResult xDRslt = CallDllForm(sExeDir + "SGPF-Avtor.dll", true);
            SetParFields(xCDoc.xDocP);
            CreateMMenu();
        }

        private void MMenuClick_Exit(object sender, EventArgs e)
        {
            CreateMMenu();
            ExitApp();
        }

        private void MMenuClick_LoadNSI(object sender, EventArgs e)
        {
            //LoadNsiMenu(false, new string[] { });
            CheckNSIState(true);
            CreateMMenu();
        }

        private void MMenuClick_SetTime(object sender, EventArgs e)
        {
            string sHead = String.Format("�������: {0}", DateTime.Now.TimeOfDay.ToString()),
                sAfter = "������ �������������";

            Cursor.Current = Cursors.WaitCursor;
            if (TimeSync.Sync(xPars.NTPSrv, 123, 10000, 3600))
                sAfter = String.Format("����� �����: {0}", DateTime.Now.TimeOfDay.ToString());

            if (xBCScanner != null)
                xBCScanner.Dispose();
            xBCScanner = null;
            Thread.Sleep(1500);
            xBCScanner = BarcodeScannerFacade.GetBarcodeScanner(null);
            xBCScanner.BarcodeScan += ehScan;

            sAfter += "\n������ �����������...";
            MessageBox.Show(sAfter, sHead);
            Cursor.Current = Cursors.Default;

            CreateMMenu();
        }

        private void MMenuClick_Reconnect(object sender, EventArgs e)
        {
            ServerExchange xSE = new ServerExchange(this);
            //xBCScanner.WiFi.IsEnabled = true;
            xBCScanner.WiFi.ShowWiFi(pnLoadDocG, true);
            xFPan.ShowP(6, 50, "��������������� � ����", "Wi-Fi");
            if (!xSE.TestConn(true, xBCScanner, xFPan))
                Srv.ErrorMsg("�� ������� ������������");
            else
            {
                //MessageBox.Show("���������...", "������������� Wi-Fi");
                Thread.Sleep(4000);
                string sI = xBCScanner.WiFi.WiFiInfo();
                string sFM = String.Format("���� �����������: {0}\nMAC: {1}", sI, xPars.MACAdr);
                MessageBox.Show(sFM, "������������� Wi-Fi");
            }
            xFPan.HideP();
            CreateMMenu();
        }


        // ��������� ������ ������������
        private void MMenuClick_DoScan(object sender, EventArgs e)
        {
            WaitScan4Func(AppC.F_GENSCAN, "��������� ������������", "");
            CreateMMenu();
        }

        // ��������� ��������� ����������
        private void MMenuClick_KeyMap(object sender, EventArgs e)
        {
            if (xSm.urCur > Smena.USERRIGHTS.USER_KLAD)
            {
                if (xFuncs.SaveKMap() == AppC.RC_OK)
                {
                    Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
                    MessageBox.Show("��������� ���������", "����������");
                }
            }
            else
                Srv.ErrorMsg("������ �������������!", true);

            CreateMMenu();
        }

        // ������ �������
        private void MMenuClick_AppVer(object sender, EventArgs e)
        {
            object[]
                xV = Srv.AppVerDT();

            Srv.ErrorMsg(String.Format("������ �� - {0}\n�� {1}\nT = {2}", xV[1], ((DateTime)xV[2]).ToString("dd.MM.yyyy"), System.Net.Dns.GetHostName()), "����������", true);
            CreateMMenu();
        }

        // �������
        //private void MMenuClick_ClearCell(object sender, EventArgs e)
        //{
        //    int nRet = AppC.RC_OK;
        //    string sQ, sErr;
        //    ServerExchange xSE = new ServerExchange(this);

        //    if ((xCDoc != null) && (xCDoc.nTypOp == AppC.TYPOP_MOVE))
        //    {// ������ ��� �������� �����������
        //        if (xCDoc.xOper.IsFillSrc())
        //        {
        //            sQ = String.Format("�������� \"��������\"\n{0} (Enter)?\n(ESC) - ������", xCDoc.xOper.xAdrSrc.Addr);
        //            DialogResult drQ = MessageBox.Show(sQ, "������� ������!",
        //                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
        //            if (drQ == DialogResult.OK)
        //            {
        //                sQ = String.Format("(KSK={0},ADRCELL={1})", xSm.nSklad, xCDoc.xOper.xAdrSrc.Addr);
        //                sErr = xSE.ExchgSrv(AppC.COM_CCELL, sQ, "", null, null, ref nRet);
        //                if (xSE.ServerRet == AppC.RC_OK)
        //                {
        //                    Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
        //                    Srv.ErrorMsg(sQ, "�������...", false);
        //                }
        //                else
        //                    Srv.ErrorMsg(sErr, "������!", true);
        //            }
        //        }               
        //    }

        //    CreateMMenu();
        //}

        private void FiltForDocs(bool bHide, NSI.TableDef di)
        {
            string s;
            if (bHide == true)
            {
                s = String.Format("(SOURCE<>{0})", NSI.DOCSRC_UPLD);
                //tDocCtrlState.Text = "�";
            }
            else
            {
                s = "";
                //tDocCtrlState.Text = "";
            }
            di.sTFilt = s;
            di.dt.DefaultView.RowFilter = di.sTFilt;

        }


        //private void ToPageHeader(TabPage pgT)
        //{
        //    Control cTab0 = ServClass.GetPageControl(pgT, 0);
        //    cTab0.Focus();

        //    W32.keybd_event(W32.VK_SHIFT, W32.VK_SHIFT,  W32.KEYEVENTF_SILENT, 0);
        //    W32.keybd_event(W32.VK_TAB, W32.VK_TAB,  W32.KEYEVENTF_SILENT, 0);
        //    W32.keybd_event(W32.VK_TAB, W32.VK_TAB, W32.KEYEVENTF_KEYUP | W32.KEYEVENTF_SILENT, 0);
        //    W32.keybd_event(W32.VK_SHIFT, W32.VK_SHIFT, W32.KEYEVENTF_KEYUP | W32.KEYEVENTF_SILENT, 0);
        //}

        //delegate void ProcessSocketCommandHandler(object sBarCode);
        //ProcessSocketCommandHandler _processCommand;


        // �������� ���������
        //void cs_MessageFromServerRecived(object sender)
        //{
        //    object[] args = { sender };
        //    Invoke(_processCommand, args);
        //}

        //static int nServCall = 0;

        //private void ProcessSocketCommand(object sBarCode)
        //{
        //    string s1;
        //    byte[] bbuf = new byte[256];
        //    string sTypDoc = (string)sBarCode;
        //    MessageBox.Show("���-�� �����!\r\n" + sTypDoc);
        //    if (++nServCall > 5)
        //        nServCall = 0;
        //    switch(nServCall){
        //        case 0:
        //            s1 = "�����!";
        //            break;
        //        case 1:
        //            s1 = "�� ����?!";
        //            break;
        //        case 2:
        //            s1 = "������� ���!";
        //            break;
        //        case 3:
        //        case 4:
        //        case 5:
        //            s1 = "����...�� !!!";
        //            break;
        //        default:
        //            s1 = "�� �������?...";
        //            break;
        //    }
        //    m_ssExchg.Connect();
        //    System.IO.Stream stm = m_ssExchg.GetStream();
        //    bbuf = Encoding.UTF8.GetBytes(s1);
        //    stm.Write(bbuf, 0, bbuf.Length);
        //    m_ssExchg.Disconnect();

        //}


        // ��������� ��������� �� ������� "����"
        private void dgDet_LostFocus(object sender, EventArgs e)
        {
            //if ((nCurVvodState == VV_STATE_SHOW) && (tcMain.SelectedIndex == PG_SCAN))
            //    ToPageHeader(tpScan);
        }

        private void tFiction_GotFocus(object sender, EventArgs e)
        {
            int j, k;
            j = 7;
            k = 99;
            j = k - 5;
            k = j + 56;
        }









        // ���������� DataSet ��� ������ � �������� � �������(GENFUNC)
        public DataSet DocDataSet4GF(DataRow dr, CurUpLoad xU, int nDet4Upload)
        {
            DataSet ds1Rec = null;
            if (dr != null)
            {
                DataTable dtMastNew = xNSI.DT[NSI.BD_DOCOUT].dt.Clone();
                DataTable dtDetNew = xNSI.DT[NSI.BD_DOUTD].dt.Clone();
                DataTable dtBNew = xNSI.DT[NSI.BD_SPMC].dt.Clone();
                DataRow[] aDR, childRows;


                dtMastNew.LoadDataRow(dr.ItemArray, true);
                ds1Rec = new DataSet("dsMOne");
                ds1Rec.Tables.Add(dtMastNew);

                if (nDet4Upload >= 1)
                {
                    if (nDet4Upload == 1)
                    {

                        childRows = dr.GetChildRows(NSI.REL2TTN);
                        foreach (DataRow chRow in childRows)
                        {
                            dtDetNew.LoadDataRow(chRow.ItemArray, true);
                            aDR = chRow.GetChildRows(NSI.REL2BRK);
                            foreach (DataRow bR in aDR)
                                dtBNew.LoadDataRow(bR.ItemArray, true);
                        }
                    }
                    else if (nDet4Upload == 2)
                    {
                        if (xCDoc.drCurRow != null)
                        {
                            if (Srv.ExchangeContext.dr4Prn != null)
                            {
                                dtDetNew.LoadDataRow(Srv.ExchangeContext.dr4Prn.ItemArray, true);
                                aDR = Srv.ExchangeContext.dr4Prn.GetChildRows(NSI.REL2BRK);
                                foreach (DataRow bR in aDR)
                                    dtBNew.LoadDataRow(bR.ItemArray, true);

                            }
                            else
                            {
                                Srv.ErrorMsg("��� ������!");
                                return (null);
                            }
                        }
                        else
                        {
                            Srv.ErrorMsg("��� ���������!");
                            return (null);
                        }
                    }
                    ds1Rec.Tables.Add(dtDetNew);
                    ds1Rec.Tables.Add(dtBNew);
                }

            }
            return (ds1Rec);
        }

        private int nSound = 0;
        //private void button1_Click(object sender, EventArgs e)
        //{
        //    int
        //        nRet = AppC.RC_OK;
        //    DataSet dsD = DocDataSet4GF(xCDoc.drCurRow, xCUpLoad, Srv.ExchangeContext.FlagDetailRows);
        //    //LoadFromSrv dgRead = new LoadFromSrv(LoadParList4GF);
        //    LoadFromSrv dgRead = null;

        //    string sPar = String.Format("PAR=(FUNC={0},BLANK={1},PRN={2},PRNMOB={3})",
        //                AppC.COM_PRNBLK, "0301000005", "", "");
                    
        //    xCUpLoad = new CurUpLoad();
        //    //xCUpLoad.aAddDat = null;

        //    //string sErr = ExchgSrv(AppC.COM_GENFUNC, sPar, "", dgRead, dsD, ref nRet);

        //}


        #region NOT_Used_yet

        /*
            // --- ��� ��������� �������� ������������
            //nnSS = xNSI;
            //thReadNSI = new Thread(new ThreadStart(ReadInThread));
            //thReadNSI.Start();

        private Thread thReadNSI;
        private void Form1_Activated(object sender, EventArgs e)
        {
            //fAv.ShowDialog();
            if (xNSI.DT[NSI.NS_MC].nState == NSI.DT_STATE_INIT)
            {// ���������� ������������ ��� �� �������
                nnSS = xNSI;
                thReadNSI = new Thread(new ThreadStart(ReadInThread));
                thReadNSI.Start();
            }
        }
         * 
         private static NSI nnSS;
        private static void ReadInThread()
        {
            nnSS.LoadLocNSI(new int[] {}, 0);
        }
         * 
         * 
         * 
         * 
        //private RUN xRUN;
        private Expr xExp;
        // ���������� ��� ������ � ���������������
        private int IT_Prep(string sF)
        {
            int ret = 0;
            //string sIT_Is = IT_ReadProg(sF);
            //if (sIT_Is != "")
            //{
            //    xRUN = new RUN();
            //    Expr xExp = new Expr(xRUN);

            //    xExp.Run(sIT_Is);
            //    xRUN.exec(xExp.GetAction());
            //}
            return (ret);
        }


        // ������ ���������
        private string IT_ReadProg(string sFile)
        {
            string ret = "";

            if (System.IO.File.Exists(sFile))
            {
                try
                {
                    using (System.IO.StreamReader sr = System.IO.File.OpenText(sFile))
                    {
                        ret = sr.ReadToEnd();


                        String input;
                        int i = 0;
                        while ((input = sr.ReadLine()) != null)
                        {
                            i++;
                        }
                        sr.Close();
                    }
                }
                catch { }
            }

            return (ret);
        }

        // ������ ��������������
        private void btIT_Run_Click(object sender, EventArgs e)
        {

            //int ret = 0;
            //string sIT_Is = IT_ReadProg(tIT_Path.Text);
            string sIT_Is = (string)xNSI.DT[NSI.BD_PASPORT].dt.Rows[0]["MD"];
            if (sIT_Is != "")
            {
                //xRUN = new RUN();
                Expr xExp = new Expr();

                if (chbIT_Run.Checked == true)
                {
                    Action a = xExp.Parse(sIT_Is);

                    xExp.run.ExecFunc("ControlDoc", new object[] { xNSI.DT[NSI.BD_DOCOUT].dt, xNSI.DT[NSI.BD_DIND].dt, xNSI.DT[NSI.BD_DOUTD].dt, 0 }, a);
                    //xExp.Run.Exec(a);
                    //Action xAct = xExp.GetAction();
                    //xAct.SetE("fMain", this);
                    //xAct.SetE("dfMain", this);
                    //xRUN.exec(xAct);
                }
            }
        }

         * * 
        // ��������������� ��� �������� ����������
        private Expr xDocControl = null;
        private Action actDocControl = null;
        private string sDocCtrlMsg = "";

        private void LoadInterCode(bool bTranslateMD)
        {
            int nRet = 0;
            string sIT_Is = "";

            if (bTranslateMD != true)
            {
                nRet = LoadAllNSISrv(NSI.I_PASPORT, null, false);
            }
            if (nRet == 0)
            {
                try
                {
                    if (xNSI.DT[NSI.BD_PASPORT].nState == NSI.DT_STATE_READ)
                    {
                        if (xNSI.DT[NSI.BD_PASPORT].dt.Rows.Count > 0)
                        {
                            sIT_Is = (string)xNSI.DT[NSI.BD_PASPORT].dt.Rows[0]["MD"];
                        }
                    }
                }
                catch
                {
                    if (bTranslateMD != true)
                        Srv.ErrorMsg("������ �������� ��������!");
                }

                if (sIT_Is != "")
                {
                    xDocControl = new Expr();
                    try
                    {
                        actDocControl = xDocControl.Parse(sIT_Is);
                    }
                    catch
                    {
                        MessageBox.Show("������ ����������!");
                    }
                }
            }
        }

        // ������ ��������
        private void MMenuClick_RunControl(object sender, EventArgs e)
        {
            if (xCDoc.drCurRow != null)
            {
                RunDocControl(xCDoc.drCurRow);
            }
            CreateMMenu();
        }



        // �������� ����������� ��������
        private void MMenuClick_SeeControl(object sender, EventArgs e)
        {
            MessageBox.Show(sDocCtrlMsg, "���������� ��������");
            CreateMMenu();
        }


        private int RunDocControl(DataRow dr)
        {
            int t1,
                nRet = 0;
            TimeSpan tsDiff;

            if (xDocControl != null)
            {
                //xDocControl.run.ExecFunc("ControlDoc", new object[] { xNSI.DT[NSI.BD_DOCOUT].dt, 
                //    xNSI.DT[NSI.BD_DIND].dt, xNSI.DT[NSI.BD_DOUTD].dt, 0 }, actDocControl);
                try
                {
                    List<string> lstStr = new List<string>();

                    //DataRow[] childRowsZVK = dr.GetChildRows(NSI.REL2ZVK);
                    //DataRow[] childRowsTTN = dr.GetChildRows(NSI.REL2TTN);

                    string sRf = String.Format("(SYSN={0})", dr["SYSN"]),
                        sSort = "KRKMC,EMK DESC";

                    // ��� ��������� �� ������ �� ���������
                    DataView dvZ = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, sSort, 
                        DataViewRowState.CurrentRows);

                    // ��� ��������� �� ��� �� ���������
                    DataView dvT = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, sSort, 
                        DataViewRowState.CurrentRows);



                    t1 = Environment.TickCount;

                    //object xRet = xDocControl.run.ExecFunc(AppC.DOC_CONTROL, 
                    //    new object[] { dr, childRowsZVK, childRowsTTN, lstStr }, actDocControl);

                    object xRet = xDocControl.run.ExecFunc(AppC.DOC_CONTROL,
                        new object[] { dr, dvZ, dvT, lstStr, this}, actDocControl);

                    tsDiff = new TimeSpan(0, 0, 0, 0, Environment.TickCount - t1);

                    nRet = (int)xRet;

                    lstStr.Add(String.Format("���������� - {0}", tsDiff.TotalSeconds));

                    if (nRet != 0)
                    {
                        tDocCtrlState.BackColor = Color.Tomato;
                        ShowInf(lstStr);

                        //sDocCtrlMsg = "";
                        //int nStrInMB = System.Math.Min(10, lstStr.Count);
                        //int nOst = (lstStr.Count > 10) ? lstStr.Count - 10 : 0;
                        //for (int i = 0; i < nStrInMB; i++)
                        //    sDocCtrlMsg += lstStr[i] + "\r\n";

                        //if (nOst > 0)
                        //    sDocCtrlMsg += nOst.ToString() + " ��������� ���...";
                    }
                    else
                    {
                        tDocCtrlState.BackColor = Color.Gainsboro;
                        ShowInf(lstStr);
                    }

                }
                catch (Exception ex) {
                MessageBox.Show(ex.Message);
                }
            }
            return (nRet);
        }
         * 
* 
 * 
 */

        #endregion


        // ��������������� ��� �������� ����������
        //private Expr xDocControl = null;
        //private Action actDocControl = null;


        //public List<ExprList> xExpDic;

        //public void LoadInterCode(bool bTranslateMD)
        //{
        //    string sCurBlk;
        //    Expr xEx;
        //    Action xAct;

        //    if (xNSI.DT[NSI.BD_PASPORT].nState == NSI.DT_STATE_READ)
        //    {
        //        xExpDic = new Dictionary<string, Srv.ExprAct>();
        //        foreach (DataRow dr in xNSI.DT[NSI.BD_PASPORT].dt.Rows)
        //        {
        //            sCurBlk = (string)dr["KD"];
        //            xEx = new Expr();
        //            try
        //            {
        //                if (bTranslateMD)
        //                    xAct = xEx.Parse((string)dr["MD"]);
        //                else
        //                    xAct = xEx.Parse((string)dr["MD"]);
        //                xExpDic.Add(sCurBlk, new Srv.ExprAct(xEx, xAct));

        //                //object xRet = xDocControl.run.ExecFunc("NameAdr",
        //                //    new object[] { xSm.nSklad, "0123456789" }, actDocControl);

        //            }
        //            catch
        //            {
        //                Srv.ErrorMsg("������ ����������! " + sCurBlk);
        //            }
        //        }
        //        //Smena.xDD = xSm.xExpDic;
        //    }

        //}



    }
}