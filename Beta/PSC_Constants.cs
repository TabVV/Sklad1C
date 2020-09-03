using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;

using PDA;
using PDA.Service;
using PDA.BarCode;
using ScannerAll;

using FRACT = System.Decimal;

namespace PDA.Service
{
    public partial class AppC
    {
        // ���������� ����������
        public const int APP4GP   = 1;
        public const int APP4MC   = 2;

        // ���������� ����������
        public static int AppScope = 
#if APPMC
        // ��� ���������
        APP4MC;
#else
// ��� ������ ������� ���������
        APP4GP;
#endif

        // ����� ��� ��� �����������
        public const string GUEST = "000";
        //public const int RC_ADM_ONLY = 1;

        // ��� ������
        public const int RC_NODATA = 301;

        // � ������ ��������������
        public const int RC_EDITEND     = 6;
        public const int RC_WARN        = 7;
        public const int RC_SAMEVES     = 8;

        public const int RC_NOEAN       = 10;           // ��������� �����������
        public const int RC_NOEANEMK    = 11;           // ��������� ������ ������� �����������
        public const int RC_ALREADY     = 12;           // ������ �� ��������� ���������
        public const int RC_ZVKONLY     = 13;           // ������ ���� ���� ������
        public const int RC_BADTABLE    = 14;           // ������������ �������
        public const int RC_BADSCAN     = 15;           // ��������� �� �������� � ������
        public const int RC_BADPODD     = 16;           // ��������� �� ������ �������
        public const int RC_UNVISPOD    = 17;           // ��������� �� ����������� �������� View
        public const int RC_NOAUTO      = 18;           // ��������� �� ����������� ���������
        public const int RC_BADDVR      = 19;           // ���� ��������� �� ��������
        public const int RC_NOSSCC      = 20;           // SSCC �����������
        public const int RC_MANYEAN     = 21;           // SSCC �������������� � ������
        public const int RC_BADPARTY    = 22;           // �� �� ������, ��� � ������
        public const int RC_BADDTG      = 23;           // ���� �������� �� ��������

        public const int RC_NOTADR      = 24;           // SSCC �� ������������ ��� �����
        public const int RC_BADDATE     = 25;           // ���� �������� (��� ���������) �� ��������
        public const int RC_NOTALLDATA  = 26;           // �������� ������

        public const int RC_OPNOTREADY  = 30;           // �������� ��� �� ������

        public const int RC_CONTINUE    = 40;           // ���������� ���������

        // ���� �������� �������
        public const int RC_NEEDPARS    = 50;           // ��� ������ ��������� ���������� ���������
        public const int RC_TASKS       = 60;           // ������� �� �������
        public const int RC_HALFOK = 99;           // �� ��� � �������, ����� ��������� ��������

        // ������ ������ � ��������

        public const int DT_ESC = -1;       // ������ ��������

        public const int DT_SHOW = 0;
        public const int DT_ADDNEW = 1;
        public const int DT_CHANGE = 2;

        public const int DT_LOAD_DOC = 10;  // �������� ����������
        public const int DT_UPLD_DOC = 20;  // �������� ����������

        // ������ ������ ����������
        public const int REG_DOC        = 1;    // ��������������
        public const int REG_OPR        = 2;    // ������������

        // ���� ����������
        public const int TYPD_VOZV      = 0;    // ������� �� ����������
        public const int TYPD_VOZVPST   = 1;    // ������� ����������
        public const int TYPD_MARK      = 2;    // ����������
        public const int TYPD_ZAKAZP    = 3;    // ����� ����������
        public const int TYPD_INV       = 4;    // ��������������
        public const int TYPD_PRIHNKLD  = 5;    // ��������� ���������
        public const int TYPD_RASHNKLD  = 6;    // ��������� ���������

        public const int TYPD_MOVINT    = 7;    // ����������� ��������������� (����� -> �����)
        public const int TYPD_ZAKAZV    = 8;    // ����������� (����������)����� ����������

        public const int TYPD_CORRECT   = 9;    // ������������
        public const int TYPD_SVOD      = 10;   // ���������� ���� (����)
        public const int TYPD_PARTINV   = 41;   // ��������� ��������������


        // ---------- ���� �����
        public const int TYPD_PRIHGP    = 16;    // ������ GP
        public const int TYPD_PERMGP    = 17;    // ����������� GP
        public const int TYPD_RASHGP    = 18;    // ������ GP
        public const int TYPD_BRAK      = 19;    // �������� (��� �����)
        public const int TYPD_KOMPLK    = 20;    // �������� (��� �����)


        // ���� ��������
        public const int TYPOP_PRMK     = 1;    // ������� � ������������
        public const int TYPOP_MARK     = 2;    // ����������
        public const int TYPOP_OTGR     = 3;    // ��������
        public const int TYPOP_MOVE     = 4;    // ����������� �� ������
        public const int TYPOP_DOCUM    = 5;    // ������ ����� ��������� (�������)
        public const int TYPOP_KMPL     = 6;    // ������������

        // �������
        public const int F_CTRLDOC      = 9;    // �������� ���������
        public const int F_CHG_GSTYLE   = 10;   // ����� ����� ����
        public const int F_ADD_SCAN     = 11;   // �������������� ��������������� ������
        public const int F_MAINPAGE     = 12;   // ������� �� ������� �������
        public const int F_NEXTDOC      = 22;   // ��������� ��������
        public const int F_PREVDOC      = 23;   // ���������� ��������
        public const int F_CHG_SORT     = 24;   // ����� ����������
        public const int F_TOT_MEST     = 25;   // ����� ����
        public const int F_SAMEKMC      = 26;   // ����� �� ��� � ���/������
        public const int F_LASTHELP     = 27;   // �������� ��������� ����
        public const int F_CHGSCR       = 28;   // ����� ������������� ������
        public const int F_FLTVYP       = 29;   // ������ ����������� ������
        public const int F_EASYEDIT     = 31;   // ������ ����
        public const int F_PODD         = 32;   // ���� ��������
        //public const int F_PODDPLUS     = 33;   // ��������� ����_��_�������
        //public const int F_PODDMIN      = 34;   // ��������� ����_��_�������
        public const int F_ZVK2TTN      = 37;   // ������� ��������� �� ������ � �����������
        public const int F_BRAKED       = 38;   // ���� �����
        public const int F_SHLYUZ       = 39;   // ���� �������� � �������� ��� ��������
        public const int F_OPROVER      = 44;   // ������������� ���������� ��������
        public const int F_LOADKPL      = 45;   // �������� ������������
        public const int F_SETPODD      = 46;   // ��������� ������/ID �������
        public const int F_LOADOTG      = 47;   // �������� ��������

        public const int F_SETADRZONE   = 48;   // ��������� �������������� ������

        public const int F_PRNDOC       = 49;   // ������ ���������
        public const int F_SETPRN       = 50;   // ��������� �������� ��������

        public const int F_KMCINF       = 51;   // ���������� � ���������� ���������
        public const int F_CELLINF      = 52;   // ���������� � ���������� ������
        public const int F_PRNBLK       = 53;   // ������ ������

        public const int F_TMPMARK      = 54;   // ��������� �������� ����������
        public const int F_TMPMOV       = 55;   // ��������� �������� �����������
        public const int F_TMPOVER      = 59;   // ��������� �������� �����������
        public const int F_NEWOPER      = 60;   // ����� ��������

        public const int F_GENFUNC      = 63;   // ����� ������������ �������
        public const int F_GENSCAN      = 65;   // ������ ������������
        public const int F_CLRCELL      = 67;   // ������� ����������� ������
        public const int F_CNTSSCC      = 68;   // ���������� SSCC
        public const int F_CHKSSCC      = 69;   // �������� SSCC

        public const int F_VES_CONF     = 150;  // ������������� ����
        public const int F_LOGOFF       = 200;  // ����� ������������
        //public const int F_DEBUG        = 500;  // ��� �������
        public const int F_SIMSCAN      = 500;  // �������� ����� ��� �������

        // ���� �������, �� ����������
        public const int F_INITREG = 99999; // ������������� �������/������
        public const int F_INITRUN = 99988; // ������������� � ������ ����������/������
        public const int F_OVERREG = 88888; // ���������� �������/������


        // ������ ���������� ������
        internal const string   COM_ZSPR    = "ZSPR";       // �������� ������������
        internal const string   COM_ZDOC    = "ZDOC";       // �������� ���������
        internal const string   COM_VDOC    = "VDOC";       // �������� ���������

        internal const string   COM_VVPER   = "VOTV";       // �������� ���������� �����������
        internal const string   COM_ZZVK    = "ZTTN";       // �������� ������
        internal const string   COM_VTTN    = "VTTN";       // �������� ���

        public const string     COM_ZOTG    = "ZOTG";       // �������� �������� � ��������/������
        public const string     COM_VOTG    = "VOTG";       // �������� �������� � ��������/������

        public const string     COM_ZPRP    = "ZPRP";       // �������� �������� � ��������/������ (�������)
        public const string     COM_CCELL   = "CLRCELL";    // ������� ������
        public const string     COM_CELLI   = "CELLINF";    // ���������� � ������
        public const string     COM_KMCI    = "KMCINF";     // ���������� � ���������� ���������
        public const string     COM_CKCELL  = "CELLCHK";    // �������� ����������� ���������� � ������

        public const string     COM_ADR2CNT = "CELLCTNT";   // �� ������ ������ �������� ����������

        public const string     COM_VOPR    = "VOPR";       // �������� ��������
        public const string     COM_VMRK    = "VMRK";       // �������� ����������

        internal const string   COM_ZKMPLST = "ZLSTZKZ";    // �������� ������ ������� �� ������������ ��� ������
        internal const string   COM_ZKMPD   = "ZZKZ";       // �������� ������ �� ������������
        internal const string   COM_VKMPL   = "VZKZ";       // �������� ������ �� ������������
        internal const string   COM_UNLDZKZ = "UNLDZKZ";    // ����� �� �������������� ������ �� ������������
        internal const string   COM_ZSC2LST = "SSCC2LST";   // �������� ������ ��������� �� SSCC

        internal const string   COM_LST2SSCC= "LST2SSCC";     // ������ ������ ���������
        internal const string   COM_GETPRN  = "GETPRN";     // �������� ������ ��������� ���������
        public const string     COM_PRNBLK  = "BLKPRN";     // ������ ������������� ���������
        public const string     COM_UNKBC   = "UNKBC";      // ������� ������������ ��������

        public const string     COM_GENFUNC = "GENFUNC";    //  ��������� � ������� ������������� ���������

        internal const string   COM_CHKSCAN = "CONFSCAN";   // ������ ������� �� ������������ ������

        public const string     COM_LOGON   = "LOGON";      // ������ ������� �� �����������

        // ���������� ��� �������
        public static byte[] baTermCom = { 13, 10 };
        // ���������� ��� ������������ ������
        public static byte[] baTermMsg = { 13, 10, 0x2E, 13, 10 };

        // ���� ���������
        public const int PRODTYPE_SHT = 0;                    // �������
        public const int PRODTYPE_VES = 1;                    // �������

        public const int KRKMC_MIX = 69;                        // ������� ��� ��� �������� �������

        // ���� ��������� ������
        internal const int TYP_VES_UNK = 0;
        internal const int TYP_VES_1ED = 1;
        internal const int TYP_VES_TUP = 2;
        internal const int TYP_VES_PAL = 3;

        // ���� ����������
        internal const int TYP_BC_OLD   = 1;
        internal const int TYP_BC_NEW = 2;

        internal const int TYP_PALET = 11;

        // ���������� ��� ����� ��� ������
        internal const int SANTA_CODE = 1;

        // ���������� ��� ��� �� ��� ������
        internal const int OAOSP_CODE = 99;


        // ���� �������� ��� ����������
        public enum MOVTYPE : int
        {
            PRIHOD      = 1,        // ������
            RASHOD      = 2,        // ������ 
            AVAIL       = 3,        // �������
            MOVEMENT    = 4         // ����������
        }


        [Flags]
        public enum TYP_TARA
        {
            UNKNOWN,                                    // �� ����������
            TARA_POTREB,                                  // ����������� �� ������
            TARA_TRANSP,                                  // ����������� �� ������
            TARA_PODDON                                  // ����������� �� ������
        }

        [Flags]
        public enum OPR_STATE : int
        {
            OPR_EMPTY       = 0,                            // �������� ��� �� ����������
            OPR_SRC_SET     = 1,                            // �������� ����������
            OPR_DST_SET     = 2,                            // �������� ����������
            OPR_SRV_SET     = 4,                            // ����� � ������� ����������
            OPR_OBJ_SET     = 8,                            // �������� ������
            OPR_READY       = 16,                           // �������� ��������
            OPR_TRANSFERED  = 32,                           // �������� ���������
            OPR_EDITING     = 64                            // �������� �������������
        }


        // ������ �������� �����/�����
        //public enum WAIT_WIN : int
        //{
        //    NO_WAIT = 0,                                // ��� ����
        //    SCAN_SSCC = 1,                                // ���� SSCC
        //    ANY_SCAN = 2,                                // ��������� �� �������
        //    SW_SET = 3                                 // ������������� ���������
        //}


        // ������ ������������ ������
        public enum REG_SWITCH : int
        {
            SW_NEXT     = 0,                                // ��������� �� �������
            SW_CLEAR    = 1,                                // ������������� �����
            SW_SET      = 2                                 // ������������� ���������
        }

        // ��� ������� � ��������� ������
        [Flags]
        public enum OBJ_IN_DROW : int
        {
            OBJ_NONE,                                    // �� ����������
            OBJ_EAN,                                    // EAN
            OBJ_SSCCINT,                                    //
            OBJ_SSCC                                    // 
        }
        
        // ���� ������������� ��������� ����� ������
        public enum IDDET4CTRL : int
        {
            KRKMC,        // ������
            NPP,        // ������ 
            GUID,        // �������
            SAPCODE         // ����������
        }

        // ����� ������ ����������
        //public enum APPMODE : int
        //{
        //    NET_BLANKS = 1,
        //    LOCAL_BLANKS = 2,
        //    NOBLANKS = 3
        //}

        // ����� � ���� Help
        internal const int HELPLINES = 19;


        // ����� ����������� ������� ������� (�������������)
        internal const string FEXT_DOC_CONTROL = "ControlDoc";                 // �������� ���������� ���������
        /// object xRet = xDocControl.run.ExecFunc
        /// ���������: DOC_CONTROL, 
        /// new object[] { dr, childRowsZVK, childRowsTTN, lstStr }, actDocControl);
        /// nRet = (int)xRet;
        internal const string FEXT_SCAN_OVER = "ScanOver";   // ����� ��������� ����������� ������������
        /// object xRet = xDocControl.run.ExecFunc(DOC_CONTROL, new object[] { dr, childRowsZVK, childRowsTTN, 
        /// ���������: SCAN_OVER
        ///                                        lstStr }, actDocControl);
        /// nRet = (int)xRet;
        internal const string FEXT_ADR_NAME = "NameAdr";   // ���������� ������������� ������
        /// (string)ExecFunc("NameAdr", new object[] { m_Sklad, Addr });
        internal const string FEXT_CONF_SCAN = "ConfScan";   // ������������� ����� �� �������


        // ������ ��������
        public const int FX_PRPSK = 1;         // �� ��������
        public const int FX_PTLST = 2;         // �� �������� �����

        // ����� ������ ����� ������ � ��������
        public const int R_BLANK = 1;           // ����� ������
        public const int R_PARS = 2;            // ��������� ���������� �� �������

        public static Dictionary<string, SkladRM.DocTypeDef>
                        xDocTDef;


    }
}

namespace SkladRM
{
    
    public sealed class AppPars
    {
        public static int 
            MAXDocType = 9;

        // ������� ������� � �������
        public static int MAXProductsType = 2;
        public static int MAXFields = 8;

        public struct ParsForMType
        {
            //public bool bAddNewRow;
            public bool bMestConfirm;
            public bool bMAX_Kol_EQ_Poddon;
            public int nDefEmkVar;
            public bool b1stPoddon;
        }

        // ��������� ��� ���� ��������� (������� ��� �������)
        public struct OneVesPars
        {
            public bool bScan;
            public bool bEdit;
            public bool bVvod;
            public string sDefVal;

            public OneVesPars(bool bV)
            {
                bScan = false; 
                bEdit = false; 
                bVvod = bV; 
                sDefVal = "";
            }

            public OneVesPars(bool bS, bool bE, bool bV, string sDV)
            {
                bScan = bS; bEdit = bE; bVvod = bV; sDefVal = sDV;
            }


        }

        // ��������� ��� ���� ���������
        public struct ParsForDoc
        {
            public bool bShowFromZ;
            public bool bTestBefUpload;
            public bool bSumVes;
        }

        public class FieldDef
        {
            public string sFieldName;
            public OneVesPars[] aVes = new OneVesPars[MAXProductsType];

            public void SetFieldDef(string sF, OneVesPars[] aP)
            {
                sFieldName = sF;
                aVes = aP;
            }
        }

        //// ��������� ��� ��������
        //public struct ParsForSrvOp
        //{
        //    public string sOper;
        //    public bool bUse;
        //}

        public class ServerPool
        {
            public string sSrvComment;
            public string sSrvHost;
            public bool bActive;
            public int nPort;
            public WiFiStat.CONN_TYPE ConType;
            public string sProfileWiFi;

            //public ParsForSrvOp[] aSrvOp = new ParsForSrvOp[7];
        }


        //===***===
        
        private string 
            m_AppStore,                                 // ���� � ��������� �����
            m_NSIPath,                                  // ���� � ���
            m_DataPath,                                 // ���� � ������
            m_Host,                                     // HOST-m_Name �������
            m_NTP,                                      // NTP-������
            m_AppVer = "",                              // � ������
            m_MAC = "000000000000";                     // MAC-�����
        
        private int 
            m_SrvPort,                                  // � ����� ������� (����� �������)
            m_SrvPortM,                                 // � ����� ������� (����� �����������)
            m_CurDocType,                               // ������� ��� ���������
            m_CurField,                                 // ������� ���� (������)
            m_CurVesType,                               // ������� ��� ��������� (������)
            m_Days2Save,                                // ���� �������� ����������
            m_ReLogon,                                  // ������� ���������� ������ (�����)
            m_NTSD = 0;                                 // � ���������

        private bool
            m_WaitSock,                                 // ���/���� ����� ����������� � ��������
            m_AutoSave = false,                         // ��������������
            m_UseSrvG = false,                          // ������ ��������
            m_ArrowsWithShift = false,                  // ������� ���������� �������� ������ � Shift
            m_OpAutoUpl = true,                         // ����-�������� ��� ��������
            m_ConfScan = false,                         // ����������� ������ ����� ������������
            m_UseAdr4DocMode = false,                   // ������������� ������� � �������������� ������
            m_UseList4ManyEAN = true,
            //m_IsDateOfProd = false,
            m_WarnNewScan = false,                      // ������ �� ���������� ����� ��� ������ ������������
            m_HidUpl = false,                           // �������� ����������� ���������
            m_UseSSCCLists = false,                     // ������������ ������� SSCC
            m_UseDTV = true,                            // ������������ ���� ��������� ��� �����������
            m_UseDTG = false,                           // ������������ ���� �������� ��� �����������
            m_PKeyIsGUID = true;                        // ���� ���������� �����


        private SerlzDict<string, DocTypeDef> 
            m_DocTypes = null;

        private AppC.IDDET4CTRL
            m_IDInProtocol = AppC.IDDET4CTRL.KRKMC;

        //-----*****-----*****-----
        
        
       
       

        //===***===
        // ����-�������� ��� ��������
        //private bool m_OpAutoUpl = true;

        // ������ ���������� ��������
        //private int m_OpOver = AppC.OPOV_SCPROD;

        // �������� ������ ��� ��������
        //private bool m_OpChkAdr = true;


        /// ������ �����
        /// 

        #region ����� ����������� ��� ������ �������
        // ������������� ���� ��� �������� ������
        public static bool bVesNeedConfirm = true;

        // ������������ ���� � ������ ����������
        public static bool bUseHours = false;

        #endregion

        // ���������� ������ ��� �������� ������
        //public bool parVvodVESNewRec = true;

        // ���������� ������ ��� �������� ������
        //public bool parVvodSHTNewRec = false;

        // ����������� ����� ���������� ������ � ������
        public bool parVvodShowExact = true;

        /// ������ ������ � �����������
        // �������� ���������� ��� ����������� � ����
        //public bool parDocControl = false;



        // ������� � �����������
        //private static string sFilePars = NSI.sPathBD + "TermPars.xml";
        private static string sFilePars = "TermPars.xml";
        //private static NSI xNSI = null;

        public AppPars()
        {
            m_AppStore = @"\Application\OAO_SP\SkladRM";
            m_NSIPath = @"\Application\BDGPRM\";
            m_DataPath = @"\Application\BDGPRM\";

            m_Host = "BPR_SERV3";
            m_NTP  = "10.0.0.221";
            m_SrvPort = 11020;
            m_SrvPortM = 11001;
            m_WaitSock = false;
       
            m_UseSrvG = false;

            CurVesType = CurDocType = CurField = 0;

            aFields[0] = new FieldDef();
            aFields[0].SetFieldDef("tKMC", new OneVesPars[2] { 
                new OneVesPars(true), 
                new OneVesPars(true) });

            aFields[1] = new FieldDef();
            aFields[1].SetFieldDef("tParty", new OneVesPars[2] { 
                new OneVesPars(true), 
                new OneVesPars(true) });

            aFields[2] = new FieldDef();
            aFields[2].sFieldName = "tEAN";

            aFields[3] = new FieldDef();
            aFields[3].SetFieldDef("tDatMC", new OneVesPars[2] { 
                new OneVesPars(true), 
                new OneVesPars(true) });

            aFields[4] = new FieldDef();
            aFields[4].SetFieldDef("tMest", new OneVesPars[2] { 
                new OneVesPars(true, true, true, "1"), 
                new OneVesPars(true, true, true, "1") });

            aFields[5] = new FieldDef();
            aFields[5].SetFieldDef("tEmk", new OneVesPars[2] { 
                new OneVesPars(true), 
                new OneVesPars(true) });

            aFields[6] = new FieldDef();
            aFields[6].SetFieldDef("tVsego", new OneVesPars[2] { 
                new OneVesPars(true), 
                new OneVesPars(true) });

            aFields[7] = new FieldDef();
            aFields[7].SetFieldDef("tKolPal", new OneVesPars[2] { 
                new OneVesPars(true), 
                new OneVesPars(true) });


            // ��������� �� ����� ����������
            //--- �������
            //aParsTypes[0].bAddNewRow = false;
            aParsTypes[AppC.PRODTYPE_SHT].bMestConfirm = true;
            aParsTypes[AppC.PRODTYPE_SHT].bMAX_Kol_EQ_Poddon = true;
            aParsTypes[AppC.PRODTYPE_SHT].nDefEmkVar = 0;
            aParsTypes[AppC.PRODTYPE_SHT].b1stPoddon = true;

            aParsTypes[AppC.PRODTYPE_VES].bMestConfirm = true;
            aParsTypes[AppC.PRODTYPE_VES].bMAX_Kol_EQ_Poddon = true;
            aParsTypes[AppC.PRODTYPE_VES].nDefEmkVar = 20;
            aParsTypes[AppC.PRODTYPE_VES].b1stPoddon = true;

            m_WarnNewScan = true;

            // ��������� �� ����� ����������
            SetArrDoc(ref this.aDocPars);

            m_Days2Save = 100;
            ReLogon = -1;
            m_HidUpl = false;
            //OpAutoUpl = true;
            //AppParsVer = null;
        }

        //---***---***---
        #region ����� ���������

        // ���� � ��������� �����
        public string sAppStore
        {
            get { return m_AppStore; }
            set { m_AppStore = value; }
        }
        // ���� � ���
        public string sNSIPath
        {
            get { return m_NSIPath; }
            set { m_NSIPath = value; }
        }
        // ���� � ������
        public string sDataPath
        {
            get { return m_DataPath; }
            set { m_DataPath = value; }
        }

        // HOST-m_Name �������
        public string sHostSrv
        {
            get { return m_Host; }
            set { m_Host = value; }
        }
        // � ����� ������� (����� �������)
        public int nSrvPort
        {
            get { return m_SrvPort; }
            set { m_SrvPort = value; }
        }

        // NTP-������
        public string NTPSrv
        {
            get { return m_NTP; }
            set { m_NTP = value; }
        }
        // � ���������
        public int NomTerm
        {
            get { return m_NTSD; }
            set { m_NTSD = value; }
        }

        // � ����� ������� (����� �����������)
        public int nSrvPortM
        {
            get { return m_SrvPortM; }
            set { m_SrvPortM = value; }
        }
        // ���/���� ����� ����������� � ��������
        public bool bWaitSock
        {
            get { return m_WaitSock; }
            set { m_WaitSock = value; }
        }
        // ��������������
        public bool bAutoSave
        {
            get { return m_AutoSave; }
            set { m_AutoSave = value; }
        }
        // ������ ��������
        public bool bUseSrvG
        {
            get { return m_UseSrvG; }
            set { m_UseSrvG = value; }
        }

        // ������� � Shift
        public bool bArrowsWithShift 
        {
            get { return m_ArrowsWithShift; }
            set { m_ArrowsWithShift = value; }
        }


        // MAC-�����
        public string MACAdr
        {
            get { return m_MAC; }
            set { m_MAC = value; }
        }

        // ��������� ���� � ����������� ��������� 
        public bool PKeyIsGUID
        {
            get { return m_PKeyIsGUID; }
            set { m_PKeyIsGUID = value; }
        }

        public bool UseList4ManyEAN
        {
            get { return m_UseList4ManyEAN; }
            set { m_UseList4ManyEAN = value; }
        }

        // ������ �����-����������
        public string AppParsVer
        {
            get { return m_AppVer; }
            set { m_AppVer = value; }
        }

        // ������������� ������� � �������������� ������
        public bool UseAdr4DocMode
        {
            get { return m_UseAdr4DocMode; }
            set { m_UseAdr4DocMode = value; }
        }

        #endregion
        //-----*****-----*****-----
        #region ��������� �����

        // �������� ����� ������ ��� ����� ���������
        public ParsForMType[] aParsTypes = new ParsForMType[MAXProductsType];

        // �������� ����� ������ ��� �����
        public FieldDef[] aFields = new FieldDef[MAXFields];

        // ������� ����
        public int CurField
        {
            get { return m_CurField; }
            set { m_CurField = value; }
        }
        // ������� ���
        public int CurVesType
        {
            get { return m_CurVesType; }
            set { m_CurVesType = value; }
        }

        // ���� ��������� ����� ������� (����� � �����������)
        //public bool bAddNewRow
        //{
        //    get { return aParsTypes[CurVesType].bAddNewRow; }
        //    set { aParsTypes[CurVesType].bAddNewRow = value; }
        //}

        // ������������� ���� ��� �����
        public bool bConfMest
        {
            get { return aParsTypes[CurVesType].bMestConfirm; }
            set { aParsTypes[CurVesType].bMestConfirm = value; }
        }
        // ������������ ���������� - ������
        public bool bMaxKolEQPodd
        {
            get { return aParsTypes[CurVesType].bMAX_Kol_EQ_Poddon; }
            set { aParsTypes[CurVesType].bMAX_Kol_EQ_Poddon = value; }
        }

        // ������� ���������� ���� ������ �����
        public int MaxVesVar
        {
            get { return aParsTypes[CurVesType].nDefEmkVar; }
            set { aParsTypes[CurVesType].nDefEmkVar = value; }
        }

        // � ������ ���������� �������� (false - ������� �� �������, true - ������ �������)
        public bool bStart1stPoddon
        {
            get { return aParsTypes[CurVesType].b1stPoddon; }
            set { aParsTypes[CurVesType].b1stPoddon = value; }
        }


        // ����������� ���� ����� ������������
        public bool bAfterScan
        {
            get { return aFields[CurField].aVes[CurVesType].bScan; }
            set { aFields[CurField].aVes[CurVesType].bScan = value; }
        }
        // ����������� ���� ��� ��������������
        public bool bEdit
        {
            get { return aFields[CurField].aVes[CurVesType].bEdit; }
            set { aFields[CurField].aVes[CurVesType].bEdit = value; }
        }
        // ����������� ���� ��� �����
        public bool bManual
        {
            get { return aFields[CurField].aVes[CurVesType].bVvod; }
            set { aFields[CurField].aVes[CurVesType].bVvod = value; }
        }
        // ������ �� ���������� ����� ��� ������ ������������
        public bool WarnNewScan
        {
            get { return m_WarnNewScan; }
            set { m_WarnNewScan = value; }
        }

        //// ����� ���� ������������ (��������� ��� ��������)
        //public bool IsDateOfProd
        //{
        //    get { return m_IsDateOfProd; }
        //    set { m_IsDateOfProd = value; }
        //}

        // ���� ��������� ������������
        public bool UseDTProizv
        {
            get { return m_UseDTV; }
            set { m_UseDTV = value; }
        }

        // ���� �������� ������������
        public bool UseDTGodn
        {
            get { return m_UseDTG; }
            set { m_UseDTG = value; }
        }

        // ID ��� ���������
        public AppC.IDDET4CTRL ID4Protocol
        {
            get { return m_IDInProtocol ; }
            set { m_IDInProtocol = value; }
        }

        #endregion

        #region ��������� ����������

        // �������� ��� ����� ���������
        //public ParsForDoc[] aDocPars = new ParsForDoc[MAXDocType + 1];
        public ParsForDoc[] 
            aDocPars = new ParsForDoc[0];

        // ������� ��� ���������
        public SerlzDict<string, DocTypeDef> DocTypes
        {
            get { return m_DocTypes; }
            set { m_DocTypes = value; }
        }


        // ������� ��� ���������
        public int CurDocType
        {
            get { return m_CurDocType; }
            set { m_CurDocType = value; }
        }

        // ���������� �� ��������� �� ������
        public bool bKolFromZvk
        {
            get 
            { 
                return aDocPars[CurDocType].bShowFromZ; 
            }
            set 
            { 
                aDocPars[CurDocType].bShowFromZ = value; 
            }
        }

        // �������� ��������� ����� ���������
        public bool bTestBeforeUpload
        {
            get { return aDocPars[CurDocType].bTestBefUpload; }
            set { aDocPars[CurDocType].bTestBefUpload = value; }
        }

        // ����������� ������� ���������
        public bool bSumVesProd
        {
            get { return aDocPars[CurDocType].bSumVes; }
            set { aDocPars[CurDocType].bSumVes = value; }
        }

        // ���� �������� ���������
        public int Days2Save
        {
            get { return m_Days2Save; }
            set { m_Days2Save = value; }
        }

        // ������� ���������� ������ (�����)
        public int ReLogon
        {
            get { return m_ReLogon; }
            set { m_ReLogon = value; }
        }

        // ����������� ������ ����� ������������
        public bool ConfScan
        {
            get { return m_ConfScan; }
            set { m_ConfScan = value; }
        }




        // �������� ����������� ���������
        public bool bHideUploaded
        {
            get { return m_HidUpl; }
            set { m_HidUpl = value; }
        }

        #endregion



        #region ��������� ������������� ������
        //===***===
        // ����-�������� ��� ��������
        public bool OpAutoUpl
        {
            get { return m_OpAutoUpl; }
            set { m_OpAutoUpl = value; }
        }

        // ������ ���������� ��������
        //public int OpOver
        //{
        //    get { return m_OpOver; }
        //    set { m_OpOver = value; }
        //}

        // �������� ������ ��� ��������
        //public bool OpChkAdr
        //{
        //    get { return m_OpChkAdr; }
        //    set { m_OpChkAdr = value; }
        //}

        #endregion



        #region ��������� ��������

        public ServerPool[] aSrvG;

        #endregion




        private static bool SetArrDoc(ref ParsForDoc[] aP)
        {
            bool ret = AppC.RC_CANCELB;
            int 
                nOldLen = aP.Length;

            MAXDocType = 0;
            foreach (DocTypeDef xD in AppC.xDocTDef.Values)
                if (xD.NumCode > MAXDocType)
                    MAXDocType = xD.NumCode;

            if (nOldLen < (MAXDocType + 1))
            {
                ParsForDoc[] aDP = new ParsForDoc[MAXDocType + 1];
                aP.CopyTo(aDP, 0);
                // ��������� �� ����� ����������
                nOldLen--;
                if (nOldLen < AppC.TYPD_VOZV)
                {
                    aDP[AppC.TYPD_VOZV].bShowFromZ = true;
                }
                //if (nOldLen < AppC.TYPD_PRIHGP)
                //{
                //    aDP[AppC.TYPD_PRIHGP].bShowFromZ = true;
                //}
                //if (nOldLen < AppC.TYPD_RASHGP)
                //{
                //    aDP[AppC.TYPD_RASHGP].bShowFromZ = true;
                //}
                //if (nOldLen < AppC.TYPD_PERMGP)
                //{
                //    aDP[AppC.TYPD_PERMGP].bShowFromZ = true;
                //}
                if (nOldLen < AppC.TYPD_INV)
                {
                    aDP[AppC.TYPD_INV].bShowFromZ = false;
                    aDP[AppC.TYPD_INV].bSumVes = true;
                }
                if (nOldLen < AppC.TYPD_PRIHNKLD)
                {
                    aDP[AppC.TYPD_PRIHNKLD].bShowFromZ = false;
                }
                if (nOldLen < AppC.TYPD_RASHNKLD)
                {
                    aDP[AppC.TYPD_RASHNKLD].bShowFromZ = false;
                }
                if (nOldLen < AppC.TYPD_SVOD)
                {
                    aDP[AppC.TYPD_SVOD].bShowFromZ = true;
                }
                //if (nOldLen < AppC.TYPD_BRAK)
                //{
                //    aDP[AppC.TYPD_BRAK].bShowFromZ = false;
                //}
                //if (nOldLen < AppC.TYPD_KOMPLK)
                //{
                //    aDP[AppC.TYPD_BRAK].bShowFromZ = true;
                //}
                aP = aDP;
                ret = AppC.RC_OKB;
            }
            return (ret);
        }
        private static bool SetArrField(ref FieldDef[] aP)
        {
            bool 
                ret = AppC.RC_CANCELB;
            int
                i,
                nOldLen = aP.Length;

            if (nOldLen < AppPars.MAXFields) 
            {
                FieldDef[] aPNew = new FieldDef[AppPars.MAXFields];
                aP.CopyTo(aPNew, 0);
                for (i = nOldLen; i < AppPars.MAXFields; i++)
                    aPNew[i] = aP[nOldLen - 1];
                aP = aPNew;
                ret = AppC.RC_OKB;
            }
            return (ret);
        }

        public static object InitPars(string sPath)
        {
            bool 
                bNeedSave = false;
            int 
                nRet = AppC.RC_OK;
            object 
                xx = null;
            AppPars 
                xNew = null;

            sFilePars = sPath + "\\" + sFilePars;

            nRet = Srv.ReadXMLObj(typeof(AppPars), out xx, sFilePars);
            xNew = (AppPars)xx;
            if (nRet != AppC.RC_OK)
            {
                if (xNew == null)
                {
                    bNeedSave = true;
                    xNew = new AppPars();
                }
            }
            else
            {// ��������� � �����
                xNew = (AppPars)xx;
                bNeedSave = SetArrDoc(ref xNew.aDocPars);
                bNeedSave |= SetArrField(ref xNew.aFields);

                if (xNew.DocTypes == null)
                {
                    xNew.DocTypes = new SerlzDict<string, DocTypeDef>();
                    foreach (KeyValuePair<string, DocTypeDef> kvp in AppC.xDocTDef)
                        xNew.DocTypes.Add(kvp.Key, kvp.Value);
                    bNeedSave = true;
                }
                else
                    AppC.xDocTDef = xNew.DocTypes;


                // ���������� �� ������� �� ��������� ��� ������� ����
                if (xNew.aParsTypes[AppC.PRODTYPE_VES].nDefEmkVar == 0)
                    xNew.aParsTypes[AppC.PRODTYPE_VES].nDefEmkVar = 20;
            }

            Version
                verPars,
                verApp = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            try
            {
                verPars = new Version(xNew.AppParsVer);
            }
            catch
            {
                verPars = new Version("0.0.0.0");
            }
            if (verPars.CompareTo(verApp) < 0)
            {
                bNeedSave = true;
                xNew.AppParsVer = verApp.ToString();
            }

            if (bNeedSave)
                SavePars(xNew);
            return (xNew);
        }

        public static int SavePars(AppPars x)
        {
            return (Srv.WriteXMLObjTxt(typeof(AppPars), x, sFilePars));
        }


    }

    /// ������������� Dictionary
    [XmlRoot("Dictionary")]
    public class SerlzDict<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                this.Add(key, value);
                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
        #endregion
    }

    /// ����� ����������� (� �������������)
    //public class StrAndInt
    //{
    //    private string
    //        m_Name,
    //        m_NameAdd1 = "",
    //        m_NameAdd2 = "";
    //    private int
    //        m_Code,
    //        m_CodeAdd1 = -1,
    //        m_CodeAdd2 = -1;
    //    private FRACT
    //        m_Dec;

    //    public StrAndInt() { }

    //    public StrAndInt(string s, int i)
    //    {
    //        SName = s;
    //        IntCode = i;
    //    }
    //    public StrAndInt(string s, string sa1, string sa2, int i, int ia1, int ia2)
    //    {
    //        SName = s;
    //        IntCode = i;
    //    }

    //    public string SName
    //    {
    //        get { return m_Name; }
    //        set { m_Name = value; }
    //    }

    //    public string SNameAdd1
    //    {
    //        get { return m_NameAdd1; }
    //        set { m_NameAdd1 = value; }
    //    }

    //    public string SNameAdd2
    //    {
    //        get { return m_NameAdd2; }
    //        set { m_NameAdd2 = value; }
    //    }

    //    public int IntCode
    //    {
    //        get { return m_Code; }
    //        set { m_Code = value; }
    //    }

    //    public int INumber
    //    {
    //        get { return m_Code; }
    //        set { m_Code = value; }
    //    }

    //    public int IntCodeAdd1
    //    {
    //        get { return m_CodeAdd1; }
    //        set { m_CodeAdd1 = value; }
    //    }

    //    public int IntCodeAdd2
    //    {
    //        get { return m_CodeAdd2; }
    //        set { m_CodeAdd2 = value; }
    //    }

    //    public FRACT DecDat
    //    {
    //        get { return m_Dec; }
    //        set { m_Dec = value; }
    //    }
    //}


    /// ����� ����������� (� �������������)
    public class StrAndInt
    {
        private string
            m_Name,
            m_NameAdd1 = "",
            m_NameAdd2 = "";
        private int
            m_Code,
            m_CodeAdd1 = -1,
            m_CodeAdd2 = -1,
            m_CodeAdd3 = -1;
        private FRACT
            m_Dec;
        private DataRow
            m_DRow = null;

        public StrAndInt() { }

        public StrAndInt(string s, int i)
        {
            SName = s;
            IntCode = i;
        }
        public StrAndInt(string s, object i, object sa1, object sa2, object ia1, object ia2)
        {
            SName = s;
            IntCode = (i is int) ? (int)i : 0;

            //SNameAdd1 = (sa1 is string) ? (string)sa1 : "";
            if (sa1 is string)
            {
                SNameAdd1 = (string)sa1;
                m_CodeAdd3 = 0;
            }
            else
            {
                SNameAdd1 = "";
                //m_CodeAdd3 = (int)sa1;
                m_CodeAdd3 = (sa1 is int) ? (int)sa1 : 0;
            }

            //SNameAdd2 = (sa2 is string) ? (string)sa2 : "";
            if (sa2 is string)
            {
                SNameAdd2 = (string)sa2;
                m_DRow = null;
            }
            else
            {
                SNameAdd2 = "";
                //m_DRow = (DataRow)sa2;
                m_DRow = (sa2 is DataRow) ? (DataRow)sa2 : null;
            }

            IntCodeAdd1 = (ia1 is int) ? (int)ia1 : 0;
            IntCodeAdd2 = (ia2 is int) ? (int)ia2 : 0;
        }

        public string SName
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        public string SNameAdd1
        {
            get { return m_NameAdd1; }
            set { m_NameAdd1 = value; }
        }
        public string SNameAdd2
        {
            get { return m_NameAdd2; }
            set { m_NameAdd2 = value; }
        }

        public int INumber
        {
            get { return m_Code; }
            set { m_Code = value; }
        }
        public int IntCode
        {
            get { return m_Code; }
            set { m_Code = value; }
        }
        public int IntCodeAdd1
        {
            get { return m_CodeAdd1; }
            set { m_CodeAdd1 = value; }
        }
        public int IntCodeAdd2
        {
            get { return m_CodeAdd2; }
            set { m_CodeAdd2 = value; }
        }
        public int IntCodeAdd3
        {
            get { return m_CodeAdd3; }
            set { m_CodeAdd3 = value; }
        }
        public FRACT DecDat
        {
            get { return m_Dec; }
            set { m_Dec = value; }
        }
        public DataRow NSIRow
        {
            get { return m_DRow; }
            set { m_DRow = value; }
        }
    }


    // �������� ���� ���������
    public class DocTypeDef
    {
        private string 
            m_Sighn,
            m_Name;

        private int
            m_NumCode;

        private bool
            m_AdrFromNeed = true,
            m_AdrToNeed = false,
            m_TryGetFromServer = true,

            m_QuantFromZ = true,
            m_bTestBefUpload = false,
            m_SumVes = false;


        private AppC.MOVTYPE
            m_MoveType = AppC.MOVTYPE.RASHOD;


        public DocTypeDef() { }

        public DocTypeDef(string s, int nC)
        {
            DocSighn = s;
            NumCode = nC;
        }

        public DocTypeDef(string sSig, int nC, string sN, AppC.MOVTYPE nMT)
        {
            DocSighn = sSig;
            NumCode = nC;
            Name = sN;
            MoveType = nMT;
        }

        public DocTypeDef(string sSig, int nC, string sN, bool A1, bool A2, AppC.MOVTYPE nMT)
        {
            DocSighn = sSig;
            NumCode = nC;
            Name = sN;
            AdrFromNeed = A1;
            AdrToNeed = A2;
            MoveType = nMT;
        }


        // ����������� (������������-2 �������) ����
        public string DocSighn
        {
            get { return m_Sighn; }
            set { m_Sighn = value; }
        }

        // ��� ����
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        // �������� ��� ���� (��� �����)
        public int NumCode
        {
            get { return m_NumCode; }
            set { m_NumCode = value; }
        }

        public AppC.MOVTYPE MoveType
        {
            get { return m_MoveType; }
            set { m_MoveType = value; }
        }

        // �������������� ������-���������
        public bool AdrFromNeed
        {
            get { return m_AdrFromNeed; }
            set { m_AdrFromNeed = value; }
        }

        // �������������� ������-���������
        public bool AdrToNeed
        {
            get { return m_AdrToNeed; }
            set { m_AdrToNeed = value; }
        }

        // ���������� �� ���������
        public bool TryFromServer
        {
            get { return m_TryGetFromServer; }
            set { m_TryGetFromServer = value; }
        }

        // ���������� �� ������
        public bool QuantFromZ 
        {
            get { return m_QuantFromZ ; }
            set { m_QuantFromZ = value; }
        }

        // ����������� �������
        public bool SumVes 
        {
            get { return m_SumVes ; }
            set { m_SumVes = value; }
        }

    }


    // ������ ������������-�����-������
    public class RowObj
    {
        public RowObj(DataRow d) { WhatObjInDRow(d); }

        public bool 
            IsEAN = false,
            IsSSCC = false,
            IsSSCCINT = false;

        public string 
            KMC = "",
            EAN13 = "",
            sSSCC = "",
            sSSCCINT = "";

        public AppC.OBJ_IN_DROW 
            AllFlags = AppC.OBJ_IN_DROW.OBJ_NONE;

        public RowObj WhatObjInDRow(DataRow drC)
        {
            if (drC["EAN13"] is string)
                EAN13 = drC["EAN13"].ToString();
            if (drC["KMC"] is string)
                KMC = drC["KMC"].ToString();

            if (drC["SSCC"] is string)
                sSSCC = drC["SSCC"].ToString();
                
            if (drC["SSCCINT"] is string)
                sSSCCINT = drC["SSCCINT"].ToString();

            if ((EAN13.Length + KMC.Length) > 0)
            {
                IsEAN = true;
                AllFlags |= AppC.OBJ_IN_DROW.OBJ_EAN;
            }
            if (sSSCCINT.Length > 0)
            {
                IsSSCCINT = true;
                AllFlags |= AppC.OBJ_IN_DROW.OBJ_SSCCINT;
            }
            if (sSSCC.Length > 0)
            {
                IsSSCC = true;
                AllFlags |= AppC.OBJ_IN_DROW.OBJ_SSCC;
            }
            return (this);
        }
    }

    public class Smena
    {

        // ����� �������������
        public enum USERRIGHTS : int
        {
            USER_KLAD = 1,                        // ���������
            USER_BOSS_SMENA = 10,                       // ��������� �����
            USER_BOSS_SKLAD = 100,                      // ��������� ������
            USER_ADMIN = 1000,                     // ��������� �����
            USER_SUPER = 2000                      // ��������, �����
        }

        public struct ObjInf
        {
            public string ObjName;
        }

        // ����������� ������� ����������� ��� ��������
        public static int MIN_TIMEOUT = 2;

        public static DateTime
            DateDef;                            // ���� �� ���������

        public static int
            m_SkladID = 0,                        // ��� ������
            UchDef = 0,                        // ��� �������
            TypDef = AppC.TYPD_VOZV;           // ��� ������ ��������� �� ���������

        public static string
            SmenaDef = "";                       // ��� �����

        //public static Dictionary<string, ExprAct> xDD = null;


        private string 
            m_OldUser;                       // ��� ����������� ������������

        private string 
            m_SDate = "";                        // ���� �� ���������
        // ���� ����������

        public string DocData
        {
            get { return m_SDate; }
            set
            {
                try
                {
                    DateDef = DateTime.ParseExact(value, "dd.MM.yy", null);
                }
                catch
                {
                    DateDef = DateTime.Now;
                }
                m_SDate = DateDef.ToString("dd.MM.yy");
            }
        }



        private static string 
            sXML            = "CS.XML";                 // ��� ����� � ����������� ������������/�����

        private int
            m_PageBefTmpMove = -1,                      // ��������, �� ������� ������� ��������� �����������
            m_CurrNum4Invent = 0,                       // ������� ����� ��� ��������� �������
            m_RegApp = AppC.REG_DOC;                    // ����� ������  �� ��������� - � �����������

        private string
            m_LstUch = "";                              // ������ �������� ��� ������������

        private DataRow
            m_DocBefTmpMove = null;                     // ����� ���������, �� �������� ������� ��������� �����������

        // ������ �������� ��� ������������
        private List<int> 
            aUch = null;


        // ������� �������
        //private int m_CurPrn = -1;

        // ������ ���������
        //public ObjInf[] aPrn = null;

        // ������� ��������� �������
        private string m_CurPrnMOB = "";

        // ������� ������������ �������
        private string m_CurPrnSTC = "";


            
        public static BindingList<StrAndInt> bl;

        // ��������� ������� ����� ������������
        public string 
            sUser = "",                       // ��� ������������ (Login)
            sUName = "",                      // ���
            sUserPass = "",
            sUserTabNom = "";
        public USERRIGHTS 
            urCur;                        // ������� �����    


        // ����� (� �������) �� ����� ����� (������ ������)
        public TimeSpan tMinutes2SmEnd = TimeSpan.FromMinutes(0);
        //public DateTime dtSmEnd;

        // ������ �� ����� ����� ��� ������������
        public Timer xtmSmEnd = null;

        
        public Timer 
            xtmTOut = null;                             // ������ �� ������� ��������� ������ �����
        
        public DateTime
            dtLoadNS,                                   // ����-����� ��������� �������� ���� ������������
            dBeg,                                       // ������ �����
            dEnd;                                       // ��������� �����

        public bool 
            bInLoadUpLoad = false;

        public int 
            nDocs = -1,
            nMSecondsTOut = 0,                          // ������������ �������� � ��������
            nLogins;

        // ��� ������
        public int nSklad
        {
            get { return m_SkladID; }
            set { m_SkladID = value; }
        }

        // ��� �������
        public int nUch
        {
            get { return UchDef; }
            set
            {
                UchDef = value;
                Uch2Lst(value, true);
            }
        }

        // ��� �����
        public string DocSmena
        {
            get { return SmenaDef; }
            set { SmenaDef = value; }
        }

        // ����� ������ ����������
        public int RegApp
        {
            get { return m_RegApp; }
            set { m_RegApp = value; }
        }

        // ������ �������� ��� ������������
        public string LstUchKompl
        {
            get { return m_LstUch; }
            set { m_LstUch = value; }
        }

        public MainF.AddrInfo
            xAdrForSpec = null,
            xAdrFix1 = null;


        // ������� �������
        //public int CurPrinter
        //{
        //    get { return m_CurPrn; }
        //    set { m_CurPrn = value; }
        //}

        // ��� �������� ��������
        //public string CurPrinterName
        //{
        //    get { return ((m_CurPrn >= 0)?aPrn[m_CurPrn].ObjName : ""); }
        //}

        // ��� �������� (����������) ��������
        public string CurPrinterMOBName
        {
            get { return m_CurPrnMOB; }
            set { m_CurPrnMOB = value; }
        }

        // ��� �������� (�������������) ��������
        public string CurPrinterSTCName
        {
            get { return m_CurPrnSTC; }
            set { m_CurPrnSTC = value; }
        }

        public int Curr4Invent
        {
            get { return m_CurrNum4Invent; }
            set { m_CurrNum4Invent = value; }
        }

        // ����� ���������, �� �������� ������� ��������� ��������
        public DataRow DocBeforeTmpMove(object drSave, ref int nPage)
        {
            DataRow
                ret = m_DocBefTmpMove;

            if (!(drSave is int))
            {// DataRow | null - ��������� �������� ��������
                m_DocBefTmpMove = (DataRow)drSave;
                m_PageBefTmpMove = nPage;
            }
            else
            {
                ret = m_DocBefTmpMove;
                nPage = m_PageBefTmpMove;
            }
            return (ret);
        }



        public static int ReadSm(ref Smena xS, string sPath)
        {
            object x;
        
            bl = new BindingList<StrAndInt>();
            bl.Add(new StrAndInt("��������������", AppC.REG_DOC));
            bl.Add(new StrAndInt("������������", AppC.REG_OPR));

            int nRet = Srv.ReadXMLObj(typeof(Smena), out x, sPath + sXML);
            if (nRet == AppC.RC_OK)
            {
                xS = (Smena)x;
                xS.m_OldUser = xS.sUser;
                xS.sUser = "";
                xS.sUserPass = "";
                //xS.CurPrinter = -1;
                xS.xtmTOut = null;
                xS.xtmSmEnd = null;
                xS.nMSecondsTOut = 0;
                xS.nDocs = -1;

                xS.CurPrinterMOBName = xS.CurPrinterSTCName = "";
                //xS.xExpDic = null;
            }
            else
                xS = new Smena();
            
            return (nRet);
        }

        public int SaveCS(string sP, int nD)
        {
            this.nDocs = nD;
            //this.xExpDic = null;
            return( Srv.WriteXMLObjTxt(typeof(Smena), this, sP + sXML) );
        }


        public int Uch2Lst(int nU)
        {
            return (Uch2Lst(nU, false));
        }

        public int Uch2Lst(int nU, bool bSet1)
        {
            int nRet = 0;

            if (bSet1)
                aUch = null;

            if (aUch == null)
            {
                aUch = new List<int>(0);
            }

            if (nU > 0)
            {
                if (!aUch.Contains(nU))
                {
                    aUch.Add(nU);
                    aUch.Sort();
                }


                nRet = aUch.Count;
            }
            LstUchKompl = "";
            for (int i = 0; i < aUch.Count; i++)
            {
                if (i > 0)
                    LstUchKompl += ",";
                LstUchKompl += aUch[i].ToString();
            }


            return (nRet);
        }

        public NSI.FILTRDET FilterTTN = NSI.FILTRDET.UNFILTERED;
        public NSI.FILTRDET FilterZVK = NSI.FILTRDET.UNFILTERED;

    }


    public class DocPars
    {

        private int
            m_TypeOper = AppC.TYPOP_DOCUM,       // ��� ��������
            m_TypeDoc = AppC.EMPTY_INT;            // ��� ��������� (���)

        private DocTypeDef              // �������� ����
            m_DType = null;

        public int 
            nUch,                       // ��� �������
            nSklad,                     // ��� ������
            nPol,                       // ��� ���������� 
            nEks;                       // ��� �����������
        public string 
            //sTypD = "",                 // ��� ��������� (������������)
            sNomDoc = "",               // � ���������
            sSklad = "",                // ������������ ������
            sBC_Doc = "",
            sBC_ML = "",
            sSmena = "",           // ��� �����
            sPol = "",             // ������������ ���������� 
            sEks = "";             // ��� �����������

        public DateTime 
            dDatDoc;        // ���� ���������

        public long 
            lSysN;              // � ���������


        private DocTypeDef SetDTByNum(int nT)
        {
            DocTypeDef
                xR = null;
            foreach (DocTypeDef xD in AppC.xDocTDef.Values)
            {
                if (xD.NumCode == nT)
                {
                    xR = xD;
                    break;
                }
            }
            return(xR);
        }


        public DocPars(int nReg):this(nReg, null){}

        public DocPars(int nReg, Smena xS)
        {
            nSklad = Smena.m_SkladID;
            nUch = Smena.UchDef;
            dDatDoc = Smena.DateDef;
            sSmena = Smena.SmenaDef;
            sNomDoc = "";
            nEks = AppC.EMPTY_INT;
            sEks = sPol = "";
            if (xS != null)
            {
                nPol = AppC.TYPOP_PRMK;
                nNumTypD = AppC.TYPD_RASHNKLD;
            }
            else
            {
                nPol = AppC.EMPTY_INT;
                nNumTypD = Smena.TypDef;
            }
        }


        // ��� ��������� (���)
        public int nNumTypD
        {
            get { return m_TypeDoc; }
            set
            {
                //var x = SetDT(value);
                //if (x != null)
                //{
                //    m_DType = x;
                //    m_TypD = value;
                //}
                m_DType = SetDTByNum(value);
                m_TypeDoc = value;
                if (m_TypeDoc == AppC.TYPD_MOVINT)
                    m_TypeOper = AppC.TYPOP_MOVE;
                else
                    m_TypeOper = AppC.TYPOP_DOCUM;
            }
        }

        // �������������� ���� ��������� (���)
        public DocTypeDef DType
        {
            get { return m_DType; }
            set
            {
                m_DType = value;
                m_TypeDoc = m_DType.NumCode;
                if (m_TypeDoc == AppC.TYPD_MOVINT)
                    m_TypeOper = AppC.TYPOP_MOVE;
                else
                    m_TypeOper = AppC.TYPOP_DOCUM;
            }
        }

        public int TypOper
        {
            get { return m_TypeOper; }
            set { m_TypeOper = value; }
        }

        //public static string TypName(ref int nTD)
        //{
        //    string s = "����������";
        //    switch (nTD)
        //    {
        //        case AppC.TYPD_OVPACK:
        //            s = "�������";
        //            break;
        //        case AppC.TYPD_PRIHGP:
        //            s = "������ ��";
        //            break;
        //        case AppC.TYPD_RASHGP:
        //            s = "������ ��";
        //            break;
        //        case AppC.TYPD_PERMGP:
        //            s = "����������� ��";
        //            break;
        //        case AppC.TYPD_INV:
        //            s = "��������������";
        //            break;
        //        case AppC.TYPD_PRIHNKLD:
        //            s = "��������� ���������";
        //            break;
        //        case AppC.TYPD_RASHNKLD:
        //            s = "��������� ���������";
        //            break;
        //        case AppC.TYPD_SVOD:
        //            s = "���������� ����";
        //            break;

        //        case AppC.TYPD_BRAK:
        //            s = "����";
        //            break;
        //        case AppC.TYPD_KOMPLK:
        //            s = "������������";
        //            break;

        //        case AppC.EMPTY_INT:
        //            s = "";
        //            break;
        //        default:
        //            nTD = AppC.EMPTY_INT;
        //            break;
        //    }
        //    return (s);
        //}

        public static string OPRName(ref int nOpr)
        {
            string s = "����������";
            switch (nOpr)
            {
                case AppC.TYPOP_PRMK:
                    s = "����� � ������������";
                    break;
                case AppC.TYPOP_MARK:
                    s = "����������";
                    break;
                case AppC.TYPOP_KMPL:
                    s = "������������";
                    break;
                case AppC.TYPOP_OTGR:
                    s = "��������";
                    break;
                case AppC.TYPOP_MOVE:
                    s = "����������� �� ������";
                    break;
                default:
                    nOpr = AppC.EMPTY_INT;
                    break;
            }
            return (s);
        }
    }


    public class PoddonInfo
    {
        private bool m_Complected;
        private bool m_UpLoaded;
        private string m_SSCC;

        public bool IsComplected
        {
            get { return m_Complected; }
            set { m_Complected = value; }
        }

        public bool IsUpLoaded
        {
            get { return m_UpLoaded; }
            set { m_UpLoaded = value; }
        }

        public string SSCC
        {
            get { return m_SSCC; }
            set { m_SSCC = value; }
        }

    }

    public class PoddonList : SortedList<int, PoddonInfo>
    {
        private int
            m_CurI = -1;

        public int Current
        {
            get { return ((m_CurI >= 0) ? base.Keys[m_CurI] : 0); }
            set
            {
                int i = base.IndexOfKey(value);
                if (i != -1)
                    m_CurI = i;
            }
        }

        // ������� �������� �� ���������
        public int TryNext(bool bSetCur, bool bForward)
        {
            int i = -1;

            if (base.Count > 0)
            {
                if (bForward)
                    i = ((m_CurI == -1) || (m_CurI == (base.Count - 1))) ? 0 : m_CurI++;
                else
                    i = ((m_CurI == -1) || (m_CurI == 0)) ? base.Count - 1 : m_CurI--;
            }
            if (i >= 0)
            {
                if (bSetCur)
                    m_CurI = i;
                i = base.Keys[i];
            }
            else
                i = 0;
            return (i);
        }

        // �������� �������
        public string RangeN()
        {
            string s = "";
            if (base.Count > 0)
            {
                s = base.Keys[0].ToString();
                if (base.Count > 1)
                    s += "-" + base.Keys[base.Count - 1].ToString();
            }
            return (s);
        }


    }







    // ������� ��������
    public class CurOper
    {
        private string
            m_SSCC_Src = "",
            m_SSCC_Dst = "";

        private MainF.AddrInfo
            m_xAdrSrc = null,
            m_xAdrDst_Srv = null,                       // ����� � �������
            m_xAdrDst = null;

        private DataRow
            m_drObj = null;

        private DocTypeDef
            m_DT = null;



        public CurOper(DocTypeDef x)
        {
            m_DT = x;
        }

        public static MainF
            m_xMF = null;

        // ������������� ������� �������� ����� ���������
        private bool SetOperState(DocTypeDef xT)
        {
            bool
                bRet = false;

            AppC.MOVTYPE
                MT = xT.MoveType;

            if ((nOperState & AppC.OPR_STATE.OPR_OBJ_SET) == AppC.OPR_STATE.OPR_OBJ_SET)
            {
                switch (MT)
                {
                    case AppC.MOVTYPE.AVAIL:        // ��������������
                        if ((nOperState & AppC.OPR_STATE.OPR_SRC_SET) == AppC.OPR_STATE.OPR_SRC_SET)
                            bRet = true;
                        break;
                    case AppC.MOVTYPE.RASHOD:       // ��������� ���������
                        if ((nOperState & AppC.OPR_STATE.OPR_SRC_SET) == AppC.OPR_STATE.OPR_SRC_SET)
                            bRet = true;
                        break;
                    case AppC.MOVTYPE.PRIHOD:       // ��������� �����������
                        if ((nOperState & AppC.OPR_STATE.OPR_DST_SET) == AppC.OPR_STATE.OPR_DST_SET)
                            bRet = true;
                        break;
                    case AppC.MOVTYPE.MOVEMENT:     // ��������� �����������
                        if (IsFillSrc() && IsFillDst())
                            bRet = true;
                        break;
                    default:
                        bRet = true;
                        if (m_DT.AdrFromNeed)
                        {// �����-�������� ����� ?
                            if ((nOperState & AppC.OPR_STATE.OPR_SRC_SET) != AppC.OPR_STATE.OPR_SRC_SET)
                                bRet = false;
                        }
                        if (bRet)
                        {
                            if (m_DT.AdrToNeed)
                            {// �����-�������� ����� ?
                                if ((nOperState & AppC.OPR_STATE.OPR_DST_SET) != AppC.OPR_STATE.OPR_DST_SET)
                                    bRet = false;
                            }
                        }
                        break;
                }
            }
            else
            {
                bRet = false;
            }

            if (bRet)
            {
                nOperState |= AppC.OPR_STATE.OPR_READY;
                OperObj["STATE"] = nOperState;
                //Srv.PlayMelody(PDA.OS.W32.MB_4HIGH_FLY);
            }
            else
            {
                nOperState &= ~AppC.OPR_STATE.OPR_READY;
                if (bObjOperScanned)
                {
                    OperObj["STATE"] = nOperState;
                }
            }
            m_xMF.ShowOperState(this);
            return (bRet);
        }

        public bool
            bObjOperScanned = false;

        public AppC.OPR_STATE
            nOperState = AppC.OPR_STATE.OPR_EMPTY;

        // ��� ���������
        public DocTypeDef DType
        {
            get { return m_DT; }
            set { m_DT = value; }
        }

        // �����-��������, ����������� � ��������
        public MainF.AddrInfo xAdrSrc
        {
            get { return m_xAdrSrc; }
        }

        // ��������� ������ � �������� ����������
        public void SetOperSrc(MainF.AddrInfo xA, DocTypeDef xT)
        {
            m_xAdrSrc = xA;
            m_DT = xT;
            if (m_xAdrSrc != null)
            {
                nOperState |= AppC.OPR_STATE.OPR_SRC_SET;
                if (bObjOperScanned)
                {
                    m_drObj["ADRFROM"] = xAdrSrc.Addr;
                    m_drObj["TIMEOV"] = DateTime.Now;
                }
            }
            else
            {
                nOperState &= ~AppC.OPR_STATE.OPR_SRC_SET;
                if (bObjOperScanned)
                {
                    m_drObj["ADRFROM"] = "";
                    m_drObj["TIMEOV"] = DateTime.Now;
                }
            }
            SetOperState(m_DT);
        }

        // �����-��������, ����������� � ��������
        public MainF.AddrInfo xAdrDst 
        {
            get { return m_xAdrDst; }
        }

        // ��������� ������-��������� � �������� ����������
        public void SetOperDst(MainF.AddrInfo xA, DocTypeDef xT)
        {
            m_xAdrDst = xA;
            m_DT = xT;
            if (bObjOperScanned)
            {
                if (m_xAdrDst != null)
                {
                    nOperState |= AppC.OPR_STATE.OPR_DST_SET;
                    m_drObj["ADRTO"] = xA.Addr;
                }
                else
                {
                    nOperState &= ~AppC.OPR_STATE.OPR_DST_SET;
                    m_drObj["ADRTO"] = "";
                }
                m_drObj["TIMEOV"] = DateTime.Now;
            }
            else
            {
                if (m_xAdrDst != null)
                    nOperState |= AppC.OPR_STATE.OPR_DST_SET;
                else
                    nOperState &= ~AppC.OPR_STATE.OPR_DST_SET;
            }
            SetOperState(m_DT);
        }

        // �����, ������������� �������� (�� ������ �� ������)
        public MainF.AddrInfo xAdrDst_Srv
        {
            get { return m_xAdrDst_Srv; }
            set
            {
                m_xAdrDst_Srv = value;
                if (m_xAdrDst_Srv != null)
                {
                    nOperState |= AppC.OPR_STATE.OPR_SRV_SET;
                }
                else
                {
                    nOperState &= ~AppC.OPR_STATE.OPR_SRV_SET;
                }
            }
        }

        // ������ (���������), ����������� � ��������
        public DataRow OperObj
        {
            get { return m_drObj; }
        }

        // ��������� ������-��������� � �������� ����������
        public void SetOperObj(DataRow xDR, DocTypeDef xT)
        {
            m_drObj = xDR;
            m_DT = xT;
            if (m_drObj != null)
            {
                bObjOperScanned = true;
                nOperState |= AppC.OPR_STATE.OPR_OBJ_SET;
                m_drObj["TIMEOV"] = DateTime.Now;
            }
            else
            {
                bObjOperScanned = false;
                nOperState &= ~AppC.OPR_STATE.OPR_OBJ_SET;
            }
            SetOperState(m_DT);
        }


        // SSCC, ����������� � ��������
        public string SSCC
        {
            get { return m_SSCC_Src; }
            set { m_SSCC_Src = value; }
        }

        // SSCC-����������, ����������� � ��������
        public string SSCC_Dst
        {
            get { return m_SSCC_Dst; }
            set { m_SSCC_Dst = value; }
        }


        public bool _IsFillAll(DocTypeDef xDT)
        {
            bool
                bRet = true;

            if (xDT.AdrFromNeed)
                {// �����-�������� ����� ?
                if (this.GetSrc(false).Length == 0)
                {
                    bRet = false;
                }
            }
            if (bRet)
            {
                if (xDT.AdrToNeed)
                {// �����-�������� ����� ?
                    if (this.GetDst(false).Length == 0)
                    {
                        bRet = false;
                    }
                }
            }
            if (bRet)
            {// ������ ������
                bRet = bObjOperScanned;
                if (bObjOperScanned)
                    nOperState = AppC.OPR_STATE.OPR_READY;
            }

            return (bRet);
        }

        public bool IsFillSrc()
        {
            return (((xAdrSrc != null) && (xAdrSrc.Addr != "")));
        }
        public bool IsFillDst()
        {
            return (((xAdrDst != null) && (xAdrDst.Addr != "")));
        }

        public string GetSrc(bool bAdrName)
        {
            return ((xAdrSrc != null) ? (bAdrName) ? xAdrSrc.AddrShow : xAdrSrc.Addr : "");
        }
        public string GetDst(bool bAdrName)
        {
            return ((xAdrDst != null) ? (bAdrName) ? xAdrDst.AddrShow : xAdrDst.Addr : "");
        }

    }

    // ������� ��������
    public class CurDoc
    {
        public DataRow
            drCurSSCC = null,
            drCurRow = null;                            // ������� ������ � ������� ����������

        public int 
            nId = AppC.EMPTY_INT;                       // ��� ���������

        public bool bSpecCond;                          // ������ ������� ��� ��������� �����

        public int nDocSrc;                             // ������������� ��������� (�������� ��� ������)
        public int nStrokZ;                             // ����� � ������
        public int nStrokV;                             // ����� �������

        public DocPars xDocP;                           // ��������� ��������� (���, �����,...)

        public string sSSCC = "";                       // ������� SSCC �������
        //public int nCurNomPodd;         // ������� ����� ������� �� ������
        //public List<int> lstNomsFromZkz;    // ������ ������� ��������
        //public string sLstNoms = "";          // ������ ������� �������� 
        //public ListNwCur xNomPs;                          //  ������ ������� ��������
        public PoddonList 
            xNPs;                                       //  ������ ������� ��������
        
        public string sLstUchNoms = "";                 // ������ ������� ��������

        //public NSI.FILTRDET FilterTTN = NSI.FILTRDET.UNFILTERED;
        //public NSI.FILTRDET FilterZVK = NSI.FILTRDET.UNFILTERED;

        public bool bEasyEdit = false;
        public bool bTmpEdit = false;

        // ������� ��������
        public CurOper
            xOper = null;

        // ���� �������� �� ������� ���������������/��������� ������
        public bool bConfScan = false;

        // ���� ��������� ������������
        public bool bFreeKMPL = false;

        public CurDoc(Smena xS) : this(xS, AppC.DT_SHOW) { }

        public CurDoc(Smena xS, int nReg){

            //nTypOp = (xS.RegApp == AppC.REG_DOC) ? AppC.TYPOP_DOCUM : AppC.TYPOP_PRMK;
            if (xS.RegApp == AppC.REG_DOC)
            {
                //nTypOp = AppC.TYPOP_DOCUM;
                xDocP = new DocPars(nReg);
            }
            //else
            //{
            //    nTypOp = AppC.TYPOP_PRMK;
            //    xDocP = new DocPars(nReg, xS);
            //}
            InitNew();
        }

        public void InitNew()
        {
            xOper = new CurOper(xDocP.DType);
        
            sLstUchNoms = "";                 // ������ ������� ��������
            xNPs = new PoddonList();
        
            //FilterTTN = NSI.FILTRDET.UNFILTERED;
            //FilterZVK = NSI.FILTRDET.UNFILTERED;
        
            bEasyEdit = false;
            bTmpEdit = false;
        }



        public string DefDetFilter()
        {
            string 
                sF = "";
            try
            {
                sF = String.Format("(SYSN={0})", nId);
            }
            catch { sF = "(TRUE)"; }
            return (sF);
        }


        
    }

    // ������� ��������
    public class CurLoad
    {
        //����� ��������
        public IntRegsAvail ilLoad;

        // ������� ��������
        public int nCommand = 0;

        // ��������� �������
        public DocPars xLP;

        // ��������� ������� ��������
        public string sSSCC="";

        // ��������� ��������
        public DataSet dsZ;

        // ��������� �������� (������� �� ���������� BD_DOUTD)
        public DataTable dtZ = null;

        // ���������� ��������� �������
        public string sFilt;


        // ������ � ����������� ��� ��������
        public DataRow drPars4Load = null;

        // ������ � 1-� ����������� ����������
        public DataRow dr1st = null;

        // ���������� ������� ��� �������
        public string sComLoad;

        public MainF.ServerExchange
            xLastSE = null;

        public CurLoad()
            : this(AppC.UPL_CUR) {}
        public CurLoad(int nRegLoad)
        {
            xLP = new DocPars(AppC.DT_LOAD_DOC);
            ilLoad = new IntRegsAvail(nRegLoad);
        }
    }

    // ��������� �������� �������
    public class IntRegsAvail
    {
        private struct RegAttr
        {
            public int RegValue;
            public string RegName;
            public bool bRegAvail;

            public RegAttr(int RV, string RN, bool RA)
            {
                RegValue = RV;
                RegName = RN;
                bRegAvail = RA;
            }
        }

        private List<RegAttr> lRegs;
        private int nI;

        public IntRegsAvail() : this(AppC.UPL_CUR) { }

        public IntRegsAvail(int nSetCur)
        {
            lRegs = new List<RegAttr>(5);
            lRegs.Add(new RegAttr(AppC.UPL_CUR, "�������", true));
            lRegs.Add(new RegAttr(AppC.UPL_ALL, "���", false));
            lRegs.Add(new RegAttr(AppC.UPL_FLT, "�� �������", false));

            nI = 0;
            CurReg = nSetCur;
        }

        // ����� �� ��������� ��������
        private int FindByVal(int V)
        {
            int ret = -1;
            int nK = 0;
            foreach (RegAttr ra in lRegs)
            {
                if (ra.RegValue == V)
                {
                    ret = nK;
                    break;
                }
                nK++;
            }
            return (ret);
        }

        // ������� �����
        public int CurReg {
            get { return (lRegs[nI].RegValue); }
            set
            {
                int nK = FindByVal(value);
                if (nK >= 0)
                    nI = nK;
            }
        }

        // ������������ �������� ������
        public string CurRegName
        {
            get { return (lRegs[nI].RegName); }
        }

        // ���������� ����������� �������� ������
        public bool CurRegAvail
        {
            get { return (lRegs[nI].bRegAvail); }
            set { 
                RegAttr ra = lRegs[nI];
                ra.bRegAvail = value;
                lRegs[nI] = ra;
            }
        }

        // ���������� ���������/���������� ��������� ������
        public string NextReg(bool bUp)
        {
            int nK;

            if (bUp == true)
            {// ����� ����������
                nK = (nI == lRegs.Count - 1) ? 0: nI + 1;
                while ((nK < lRegs.Count) && (nK != nI))
                {
                    if (lRegs[nK].bRegAvail == true)
                    {
                        nI = nK;
                        break;
                    }
                    nK++;
                    if (nK == lRegs.Count)
                        nK = 0;
                }
            }
            else
            {
                nK = (nI == 0)? lRegs.Count - 1 : nI - 1;
                while ((nK >= 0) && (nK != nI))
                {
                    if (lRegs[nK].bRegAvail == true)
                    {
                        nI = nK;
                        break;
                    }
                    if (nK == 0)
                        nK = lRegs.Count - 1;
                    else
                        nK--;
                }
            }

            return (lRegs[nI].RegName);
        }

        // ���� ����������� ��� ����
        public void SetAllAvail(bool bFlag)
        {
            for (int i = 0; i < lRegs.Count; i++ )
            {
                RegAttr ra = lRegs[i];
                ra.bRegAvail = bFlag;
                lRegs[i] = ra;
            }
        }

        // ���������� ����������� �����������
        public bool SetAvail(int nReg, bool v)
        {
            bool ret = false;
            int nK = FindByVal(nReg);
            if (nK >= 0)
            {
                RegAttr ra = lRegs[nK];
                ra.bRegAvail = v;
                lRegs[nK] = ra;
                ret = true;
            }
            return (ret);
        }


    }




    //public class ServerInf
    //{
    //    private string m_SrvComment;
    //    private string m_SrvHost;
    //    private int m_SrvPort;

    //    public ServerInf() { }

    //    public ServerInf(string sH, int nP)
    //    {
    //    }


    //}


    // ������ ��������
    //public class GroupServers
    //{

    //    private AppPars xPApp;

    //    // ������ ������� � ������
    //    public int nSrvGind;

    //    private BindingList<ServerInf> blSrvG;

    //    private List<string> lSrvG;

    //    public List<int> naComms;



    //    public GroupServers()
    //        : this(AppC.UPL_CUR, null) { }

    //    public GroupServers(AppPars xP)
    //        : this(AppC.UPL_CUR, xP) { }

    //    public GroupServers(int nRegUpl, AppPars xP)
    //    {
    //        xPApp = xP;
    //        nSrvGind = -1;
    //        lSrvG = new List<string>();
    //        lSrvG.Clear();
    //        if (xP != null)
    //        {
    //            if (xP.bUseSrvG)
    //            {
    //                if (xP.aSrvG.Length > 1)
    //                {
    //                    lSrvG.Add("���");
    //                    nSrvGind = 1;
    //                }
    //                else
    //                    nSrvGind = 0;
    //                foreach (AppPars.ServerPool xS in xP.aSrvG)
    //                {
    //                    lSrvG.Add(xS.sSrvComment);
    //                }

    //            }
    //        }
    //    }


    //    public string CurSrv
    //    {
    //        get { return (nSrvGind >= 0) ? lSrvG[nSrvGind] : xPApp.sHostSrv; }
    //    }
    //    public int NextSrv()
    //    {
    //        if (nSrvGind >= 0)
    //        {
    //            nSrvGind = (lSrvG.Count - 1 == nSrvGind) ? 1 : nSrvGind + 1;
    //        }
    //        return (nSrvGind);
    //    }
    //}

    // ������� ��������
    public class CurUpLoad
    {
        //����� ��������
        public IntRegsAvail ilUpLoad;

        // ������ ������� � ������
        public int nSrvGind;

        private List<string> lSrvG;
        private AppPars xParsApp;


        // ��������� �������
        public DocPars xLP;

        public List<int> naComms;

        // ������� ������� ��������
        public string sCurUplCommand = "";

        // �������� ������ ������� ������ (��� ��������)
        public bool bOnlyCurRow = false;

        public DataRow drForUpl = null;

        // �������������� ������ �������� (��������� ������)
        //public byte[] aAddDat = null;

        public CurUpLoad()
            : this(AppC.UPL_CUR, null) {}

        public CurUpLoad(AppPars xP)
            : this(AppC.UPL_CUR, xP) { }

        public CurUpLoad(int nRegUpl, AppPars xP)
        {
            xParsApp = xP;
            xLP = new DocPars(AppC.DT_UPLD_DOC);
            ilUpLoad = new IntRegsAvail(nRegUpl);
            nSrvGind = -1;
            lSrvG = new List<string>();
            lSrvG.Clear();
            if (xP != null)
            {
                if (xP.bUseSrvG)
                {
                    if (xP.aSrvG.Length > 1)
                    {
                        lSrvG.Add("���");
                        nSrvGind = 1;
                    }
                    else
                        nSrvGind = 0;
                    foreach (AppPars.ServerPool xS in xP.aSrvG)
                    {
                        lSrvG.Add(xS.sSrvComment);
                    }

                }
            }

        }

        //public DataRow SetFiltInRow(NSI xNSI)
        public string SetFiltInRow()
        {

            string sF = String.Format("(TD={0}) AND (DT={1}) AND (KSK={2})",
                xLP.nNumTypD, xLP.dDatDoc.ToString("yyyyMMdd"), xLP.nSklad);

            if (xLP.nUch != AppC.EMPTY_INT)
                sF += "AND(NUCH=" + xLP.nUch.ToString() + ")";

            if (xLP.sSmena != "")
                sF += "AND(KSMEN='" + xLP.sSmena + "')";

            if (xLP.nEks != AppC.EMPTY_INT)
                sF += "AND(KEKS=" + xLP.nEks.ToString() + ")";

            if (xLP.nPol != AppC.EMPTY_INT)
                sF += "AND(KRKPP=" + xLP.nPol.ToString() + ")";
            return ("(" + sF + ")");

        }

        public string CurSrv
        {
            get { return (nSrvGind >= 0) ? lSrvG[nSrvGind] : xParsApp.sHostSrv; }
        }
        public int NextSrv()
        {
            if (nSrvGind >= 0)
            {
                nSrvGind = (lSrvG.Count - 1 == nSrvGind) ? 1 : nSrvGind + 1;
            }
            return (nSrvGind);
        }
    }



    public sealed class PSC_Types
    {

        public struct ScDat
        {
            // ���������� ������������
            public ScannerAll.BCId 
                ci;                             // ��� �����-����

            public string
                s,                              // �����-���
                nParty,                         // ������
                sDataGodn,                      // ���� ��������(���������)
                sDataIzg,                       // ���� ������������ (���������)
                sSSCC;

            public DateTime
                dDataGodn,                      // ���� ��������
                dDataMrk,                       // ���� ����������
                dDataIzg;                       // ���� ������������

            public FRACT 
                fEmk,                           // ������� � ������ (��� ��������) ��� 
                                                // ��� �������� (��� ��������); 0 - ��������� �����
                fVes,                           // ���
                fVsego,                         // ����� ���� /���
                fVesGross;                      // ��� ������

            public int 
                nZaklMT,                       // � ���������� ��� ���������
                nTara,                          // ������� ��� ����(N(2))
                nPalet,                         // ���������� ��������
                nMestPal,                       // ���������� ���� �� �������
                nKolSht,                        // ���������� � ������ (��� ��������)
                nMest;                          // ���������� ����


            public int 
                nEANs,                          // ���������� EAN ��� ���������
                nPrzvFil,                       // ��� ���������������� ��������
                nTypVes;                        // ��� �������� (TYP_VES_TUP,...)

            // ����� ����� -???
            //public float nKolVes;           // ���������� (���)

            public bool
                bReWrite,                       // ���������� ������� ������ � ����������
                bFindNSI,                       // ������� ����� � ���
                bSetAccurCode;                  // ���������� Primary Key � ��� ����������

            //--- ����������� ������
            public FRACT fKolE_alr;         // ��� ������� ������ ������� ���� (���� = 0)
            public int nKolM_alr;           // ��� ������� ���� ������� ����
            public FRACT fMKol_alr;         // ��� ������� ���������� ��������� (���� != 0)
            //--- ����������� ������ (������ ����������)
            public FRACT 
                fKolE_alrT;        // ��� ������� ������ ������� ���� (���� = 0)
            public int
                nMAlr_NPP,                  // ��� ������� ���� ������� ���� (��� ������������)
                nKolM_alrT;          // ��� ������� ���� ������� ����
            public FRACT
                fVAlr_NPP,                  // ��� ������� ������ ������� ���� (��� ������������)
                fMKol_alrT;        // ��� ������� ���������� ��������� (���� != 0)

            //--- ������ - ����������� ������
            public FRACT fKolE_zvk;         // ��������� ������ ������� ���� �����
            public int nKolM_zvk;           // ���� ������� ���� � ������� �� ������ �����

            // ������ (���)
            public System.Data.DataRow drEd;            // ���� ����������� ������� � ���
            public System.Data.DataRow drMest;          // ���� ����������� ����� � ���

            // ������ �� ������
            public System.Data.DataRow drTotKey;        // ������ �� ����� � ���������� �������
            public System.Data.DataRow drPartKey;       // ������ �� ����� � ����� �������
            public System.Data.DataRow drTotKeyE;       // ������ �� �������� � ���������� �������
            public System.Data.DataRow drPartKeyE;      // ������ �� �������� � ����� �������

            public System.Data.DataRow drMC;            // ������ � ����������� ������������
            // �� ����������� ������������
            public string sKMC;             // ������ ���
            public int nKrKMC;              // ������� ���
            public string sN;               // ������������
            public int nSrok;               // ���� ���������� (����)
            public bool bVes;               // ������� ��������
            public string 
                sGTIN,
                sEAN;                       // EAN-��� ���������
            //public string sGrK;             // ��������� ��� ���������
            public FRACT fEmk_s;            // ��� �������������� ������� ��� ������������� ����=0
            //public int EmkPod;

            // ���������� ������
            public NSI.DESTINPROD nDest;                // ��� �� ������ ����������� (����� ��� ������ �����)
            // ������������� ������
            public int nRecSrc;
            public DateTime dtScan;

            // ��������� �������� �� ������� ����-�������
            public int nDocCtrlResult;

            // ���� ����������� ���� ����������
            public bool bAlienMC;
            public bool bNewAlienPInf;
            public string sIntKod;

            public int nNomPodd;
            public int nNomMesta;
            public AppC.TYP_TARA tTyp;

            // ��������� �� ������ ��� ������������
            public string sErr;

            // ��������� ������� ��� �������� ZVK/TTN
            public string sFilt4View;

            // ������ ����� ������, ������� ������������ ����������� ������� �������������
            public List<DataRow> lstAvailInZVK;
            public int nCurAvail;

            //public CurOper xOp;

            public Srv.Collect4Show<StrAndInt> 
                xEANs,
                xEmks;

            public CurOper 
                xOp;

            public ScanVarRM
                xSCD;

            public ScDat(ScannerAll.BarcodeScannerEventArgs e) : this(e, null) { }

            public ScDat(ScannerAll.BarcodeScannerEventArgs e, ScanVarRM xScanDat)
            {
                ci = e.nID;                         // ��� �����-����
                s = e.Data;                         // �����-���
                if (xScanDat == null)
                    xScanDat = new ScanVarRM(e);
                xSCD = xScanDat;

                //nParty = AppC.EMPTY_INT;
                nParty = "";
                sDataIzg = 
                    sDataGodn = "";
                dDataIzg = dDataGodn = dDataMrk =
                    DateTime.MinValue;

                nMest = 
                    nZaklMT =
                    0;
                nMestPal = 0;
                fEmk = 0;
                fVsego = 0;
                nPalet = 0;

                fVes = fVesGross = 0;

                nKolSht = AppC.EMPTY_INT;

                nTypVes = AppC.TYP_VES_UNK;
                bFindNSI = false;

                nPrzvFil = 0;
                nEANs = 0;

                drEd = null;
                drMest = null;
                drMC = null;

                fKolE_alr = 0;
                nKolM_alr = 0;
                fMKol_alr = 0;

                fKolE_alrT = 0;
                nKolM_alrT = 0;
                fMKol_alrT = 0;

                fKolE_zvk = 0;       // ������ ������� ���� ����
                nKolM_zvk = 0;       // ���� ������� ����  �� ������

                nMAlr_NPP = 0;
                fVAlr_NPP = 0;

                drTotKey = null;     // ������ �� ����� � ���������� �������
                drPartKey = null;    // ������ �� ����� � ����� �������
                drTotKeyE = null;    // ������ �� �������� � ���������� �������
                drPartKeyE = null;   // ������ �� �������� � ����� �������

                sKMC = sSSCC = "";
                nKrKMC = AppC.EMPTY_INT;
                sN = "<����������>";
                nSrok = 0;
                nTara = 0;
                bVes = false;
                sEAN = 
                    sGTIN = "";
                //sGrK = "";
                fEmk_s = 0;
                //EmkPod = 0;

                nDest = NSI.DESTINPROD.GENCASE;
                nDocCtrlResult = AppC.RC_CANCEL;

                nRecSrc = (int)NSI.SRCDET.SCAN;
                dtScan = DateTime.Now;
            
                bAlienMC = false;
                bNewAlienPInf = false;
                sIntKod = "";
                sErr = "";
                sFilt4View = "";
                lstAvailInZVK = new List<DataRow>();
                lstAvailInZVK.Clear();
                nCurAvail = -1;
            
                nNomPodd = 0;
                nNomMesta = 0;
                tTyp = AppC.TYP_TARA.UNKNOWN;
                //xOp = (x == null)?new CurOper():x;
                bSetAccurCode = false;
                bReWrite = false;

                xEANs = new Srv.Collect4Show<StrAndInt>(new StrAndInt[0]);
                xEmks = new Srv.Collect4Show<StrAndInt>(new StrAndInt[0]);
                xOp = new CurOper(new DocTypeDef());

            }

            // ��������� ��������� ����� ��� ������
            public void ZeroZEvals()
            {
                fKolE_zvk = 0;       // ������ ������� ���� ����
                nKolM_zvk = 0;       // ���� ������� ����  �� ������

                drTotKey = null;     // ������ �� ����� � ���������� �������
                drPartKey = null;    // ������ �� ����� � ����� �������
                drTotKeyE = null;    // ������ �� �������� � ���������� �������
                drPartKeyE = null;   // ������ �� �������� � ����� �������

                sErr = "";
                sFilt4View = "";
                lstAvailInZVK.Clear();
                nCurAvail = -1;
            }

            /// �������� ������ �� ����������� �� EAN ��� ����
            public bool GetFromNSI(string s, DataRow dr, ref int nPrzvPl)
            {
                return(GetFromNSI(s, dr, ref nPrzvPl, true));
            }


            /// �������� ������ �� ����������� �� EAN ��� ����
            public bool GetFromNSI(string s, DataRow dr, ref int nPrzvPl, bool bFullInfo)
            {
                int
                    nFoundGTIN,
                    nDefEmk;
                string
                    sF;
                //    sDopDate = "";
                //DateTime 
                //    dReal;
                bFindNSI = false;
                
                if (dr != null)
                {
                    bFindNSI = true;
                    drMC = dr;
                    sKMC = dr["KMC"].ToString();
                    try
                    {
                        nKrKMC = int.Parse(dr["KRKMC"].ToString());
                    }
                    catch
                    {
                        nKrKMC = 0;
                        dr["KRKMC"] = 0;
                    }
                    sEAN = ((string)dr["EAN13"]).Trim();

                    sN = dr["SNM"].ToString();
                    try
                    {
                        nSrok = int.Parse(dr["SRR"].ToString());
                    }
                    catch
                    {
                        nSrok = 0;
                        dr["SRR"] = 0;
                    }
                    try
                    {
                        bVes = (int.Parse(dr["SRP"].ToString()) > 0) ? true : false;
                    }
                    catch { bVes = false; }

                    try
                    {
                        if (nPrzvPl >= 0)
                            nPrzvFil = (int)dr["KSK"];
                    }
                    catch { nPrzvFil = -1; }

                    //sGrK = dr["GKMC"].ToString();

                    //if (fEmk == 0)
                    //{
                    //    sF = String.Format("(KMC='{0}')AND(EMK>0)", sKMC);
                    //    DataView dv = new DataView(dtE, sF, "EMK", DataViewRowState.CurrentRows);
                    //    if (dv.Count == 1)
                    //    {
                    //        fEmk_s = fEmk = (FRACT)dv[0].Row["EMK"];
                    //        if (tTyp != AppC.TYP_TARA.TARA_PODDON)
                    //            nMestPal = (int)dv[0].Row["EMKPOD"];
                    //    }
                    //}
                    
                    if (bFullInfo)
                    {// ������� ��������� ��� ������� ������ ���������� �� ���������

                        //if (dDataIzg != DateTime.MinValue)
                        //{
                        //    DateTime dReal = dDataIzg.AddHours((double)nSrok);
                        //    sDataIzg = dDataIzg.ToString("dd.MM.yy") + "/";
                        //    if (AppPars.bUseHours == true)
                        //        sDataIzg += dReal.ToString("HH").Substring(0, 2) + "� ";
                        //    sDataIzg += dReal.ToString("dd.MM");
                        //}

                        // ����� ������� �� ���� ��������� � �������� ���������� ����
                        DataRow[] draEmk = drMC.GetChildRows(dr.Table.ChildRelations[NSI.REL2EMK]);
                        xEmks = new Srv.Collect4Show<StrAndInt>(GetEmk4KMC(dr, draEmk, out nDefEmk, out nFoundGTIN));
                        if (xEmks.Count > 0)
                        {
                            if (xEmks.Count == 1)
                            {// ��������� ������, ������ ���� �������
                                //drSEMK = draEmk[0];
                                xEmks.CurrIndex = 0;
                            }
                            else
                            {
                                if (nFoundGTIN < 0)
                                {
                                    if (nDefEmk >= 0)
                                        nFoundGTIN = nDefEmk;
                                }
                                nFoundGTIN = Math.Max(nFoundGTIN, 0);
                                xEmks.CurrIndex = nFoundGTIN;
                            }
                            StrAndInt xS = (StrAndInt)xEmks.Current;
                            fEmk = fEmk_s = xS.DecDat;
                            if (nMestPal <= 0)
                                nMestPal = xS.IntCode;
                            nTara = xS.IntCodeAdd3;
                            nKolSht = xS.IntCodeAdd1;
                        }
                    }








                }
                else
                {
                    if (s.Length > 0)
                        sN = s + "-???";
                }
                return (bFindNSI);
            }


            // ��������� ������ ��������
            public StrAndInt[] GetEmk4KMC(DataRow drMC, DataRow[] draE, out int nDefaultEmk, out int nFGTIN)
            {
                int
                    i;
                StrAndInt[]
                    siTmp;

                nDefaultEmk = nFGTIN = -1;

                siTmp = new StrAndInt[draE.Length];

                //if (IsTara("", this.nKrKMC))
                //    return (new StrAndInt[0]);
                try
                {
                    if (draE.Length > 0)
                    {
                        for (i = 0; i < draE.Length; i++)
                        {
                            siTmp[i] = new StrAndInt(i.ToString(),
                                draE[i]["EMKPOD"],
                                draE[i]["KT"],
                                draE[i]["ITF14"].ToString(),
                                draE[i]["KRK"],
                                draE[i]["PR"]);
                            siTmp[i].DecDat = (draE[i]["EMK"] is FRACT) ? (FRACT)draE[i]["EMK"] : 0;

                            if (siTmp[i].IntCodeAdd2 > 0)
                                nDefaultEmk = i;
                            if (bSetAccurCode)
                            {
                                if (sGTIN == siTmp[i].SNameAdd2)
                                    nFGTIN = i;
                            }
                            else
                            {
                                if (siTmp[i].DecDat == this.fEmk)
                                {
                                    nFGTIN = i;
                                }
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    i = e.Message.Length;
                }
                return (siTmp);
            }



        }


        public struct FuncKey
        {
            public int nF;
            public int nKeyValue;
            public Keys kMod;
            public FuncKey(int f, int v, Keys m)
            {
                nF = f;
                nKeyValue = v;
                kMod = m;
            }
        }
    }
}
