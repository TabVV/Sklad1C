using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using PDA.Service;
using PDA.OS;

using FRACT = System.Decimal;
using SkladAll;
//using PDA.Service;

//using System.Globalization;

namespace SkladRM
{
    public sealed partial class NSI : NSIAll
    {
        public const string NS_MC       = "NS_MC";
        public const string NS_PP       = "NS_PP";
        public const string NS_EKS      = "NS_EKS";
        public const string NS_USER     = "NS_USER";
        public const string NS_SMEN     = "NS_SMEN";
        public const string NS_SKLAD    = "NS_SKLAD";
        public const string NS_SUSK     = "NS_SUSK";
        public const string NS_SEMK     = "NS_SEMK";
        public const string NS_PRPR     = "NS_PRPR";
        public const string NS_KRUS     = "NS_KRUS";
        public const string NS_AI       = "NS_AI";
        public const string NS_ADR      = "NS_ADR";
        public const string NS_BLANK    = "NS_BLANK";

        public const string BD_TINF     = "BD_TINF";

        public const string BD_PASPORT  = "BD_PASPORT";

        public const string BD_DOCOUT   = "BD_DOCOUT";
        public const string BD_DOUTD    = "BD_DOUTD";
        public const string BD_DIND     = "BD_DIND";
        public const string BD_SPMC     = "BD_SPMC";
        public const string BD_SOTG     = "BD_SOTG";
        public const string BD_ADRKMC   = "BD_ADRKMC";
        public const string BD_SSCC     = "BD_SSCC";
        public const string BD_SSCCI    = "BD_SSCCI";

        // ������� ��� ������
        public const string BD_ZDOC     = "BD_ZTTN";
        public const string BD_ZDET     = "BD_STTN";

        public const string BD_KMPL     = "BD_KMPL";

        // ����� ����� ���������
        public const string REL2TTN     = "DOC2TTN";
        public const string REL2ZVK     = "DOC2ZVK";
        public const string REL2SSCC    = "DOC2SSCC";
        public const string REL2SSCCI   = "DOC2SSCCI";

        public const string REL2ADR     = "KMC2ADR";

        public const string REL2BRK     = "TTN2BRK";
        public const string REL2EMK     = "KMC2EMK";

        // ���� ���������� ��������� �����
        public new enum TABLESORT : int
        {
            NO          = 0,                            // ��� ����������
            NAMEMC      = 1,                            // �� ������������
            ADRES       = 2,                            // �� �������� ����
            ADRESTO     = 3,                            // �� �������� ����
            KRKMC       = 4,                            // �� �������� ����
            RECSTATE    = 5,                            // �� ������� ������
            MAXDET      = 6                             // ������������ ��������
        }

        // ������ ������
        public enum READINESS : int
        {
            NO          = 0,
            PART_READY  = 20,                           // �������� ���������
            FULL_READY = 100                            // ��������� ���������
        }

        // �������������� ������� �� ������� ������
        public enum SPECCOND : int
        {
            NO              = 0,
            DATE_V_SET      = 4,                            // �� ������ ��������� ���� ���������
            DATE_G_SET      = 16,                           // �� ������ ��������� ���� ��������
            DATE_SET        = 32,                           // ���-�� �� ���� ��� ������
            DATE_SET_EXACT  = 64,                           // ������ ���������� ����
            PARTY_SET       = 128,                          // ������ ������ � ����� ���������
            SSCC_INT        = 256,
            SSCC            = 512
        }

        // ���������� ����� ��������� �����
        public enum DESTINPROD: int
        {
            UNKNOWN         = 0,
            GENCASE         = 1,                                // ����� ������
            TOTALZ          = 2,                                // ������ ������������ ������ (EAN-EMK-NP)
            PARTZ           = 3,                                // ��������� ������������ ������
            USER            = 10,                               // ���������� User
        }

        // ������������� ��������� �����
        public enum SRCDET : int
        {
            SCAN            = 1,                        // �������������
            FROMZ           = 2,                        // ����������� �� ������
            HANDS           = 3,                        // ������� �������
            SSCCT           = 4,                        // ��������� ����� SSCC
            FROMADR         = 5,                        // ��������� ����� �����
            FROMADR_BUTTON  = 6,                        // ��������� ����� �����
            CR4CTRL         = 7                         // ������� ��� �������� ���������
        }

        // ���������� ��������� �����
        [Flags]
        public enum FILTRDET
        {
            UNFILTERED = 0,                             // ��� �������
            READYZ,                                     // �� ���������� ������
            NPODD,                                      // �� ������� ��������
            SSCC                                        // �� SSCC ��������
        }

        // ��������� �������� ���������
        public enum DOCCTRL : int
        {
            UNKNOWN = 0,                                // �������� �� ����������
            OK = 1,                                     // ������ ������������ ������
            WARNS = 2,                                  // ���� ��������������
            ERRS = 3                                    // ���� ������
        }


        // ������� ������ ��� ������
        //internal const int GDOC_CENTR = 0;              // ��� ������������
        //internal const int GDOC_SAM   = 1;              // ��� ����������
        internal const int GDOC_INV   = 0;              // ��� ��������������

        internal const int GDOC_NEXT  = 999;            // ��������� �� ������

        // ������� ������ ��� ��������� �����
        internal const int GDET_SCAN        = 0;        // ��� �������������
        internal const int GDET_ZVK         = 1;        // ��� ������
        internal const int GDET_ZVK_KMPL    = 2;        // ��� ��������������

        // ������������� ���������
        internal const int DOCSRC_LOAD = 1;             // ��������
        internal const int DOCSRC_CRTD = 2;             // ������ �������
        internal const int DOCSRC_UPLD = 3;             // ��������

        public DataSet dsM;
        public DataSet dsNSI;

        public static MainF xFF;

        public NSI(AppPars xP, MainF xF, string[] aNSINames)
        {
            sPathNSI = xP.sNSIPath;
            sPathBD = xP.sDataPath;
            xFF = xF;

            CreateTables();

            if (aNSINames != null)
                LoadLocNSI(aNSINames, SkladAll.NSIAll.LOAD_EMPTY);

            dsNSI = new DataSet("dsNSI");
            dsNSI.Tables.Add(DT[NS_MC].dt);
            dsNSI.Tables.Add(DT[NS_SEMK].dt);
            try
            {
                dsNSI.Relations.Add(REL2EMK, DT[NS_MC].dt.Columns["KMC"], DT[NS_SEMK].dt.Columns["KMC"]);
            }
            catch { }

            dsM = new DataSet("dsM");
            dsM.Tables.Add(DT[BD_DOCOUT].dt);
            dsM.Tables.Add(DT[BD_DOUTD].dt);
            dsM.Tables.Add(DT[BD_DIND].dt);
            dsM.Tables.Add(DT[BD_ADRKMC].dt);
            dsM.Tables.Add(DT[BD_SPMC].dt);

            DataColumn 
                dcDocHeader = DT[BD_DOCOUT].dt.Columns["SYSN"];

            dsM.Relations.Add(REL2TTN, dcDocHeader, DT[BD_DOUTD].dt.Columns["SYSN"]);
            dsM.Relations.Add(REL2ZVK, dcDocHeader, DT[BD_DIND].dt.Columns["SYSN"]);

            dsM.Relations.Add(REL2ADR, 
                new DataColumn[]{
                DT[BD_DIND].dt.Columns["SYSN"],DT[BD_DIND].dt.Columns["ID"]},
                new DataColumn[]{
                DT[BD_ADRKMC].dt.Columns["SYSN"], DT[BD_ADRKMC].dt.Columns["ID"]}
                );

            dsM.Relations.Add(REL2BRK, DT[BD_DOUTD].dt.Columns["ID"], 
                                       DT[BD_SPMC].dt.Columns["ID"]);
        }

        // �������� ��� �� ��������� (���������) NEW!!!
        // nReg - LOAD_EMPTY ��� LOAD_ANY (������� ��-������)
        public void LoadLocNSI(string[] aI, int nR)
        {
            float fLoadAll = 0;

            if (aI.Length == 0)
            {
                aI = new string[DT.Keys.Count];
                DT.Keys.CopyTo(aI, 0);
            }
            foreach(string sTN in aI)
            {
                if (Read1NSI(DT[sTN], nR))
                {
                    fLoadAll += float.Parse(DT[sTN].sDTStat);
                    AfterLoadNSI(DT[sTN].dt.TableName, false, "");
                }
            }
        }

        // �������� ������
        private void CreateTables()
        {
            DT = new Dictionary<string, TableDef>();

            // ���������� � ������������
            DT.Add(BD_TINF, new TableDef(BD_TINF, new DataColumn[]{
                new DataColumn("DT_NAME", typeof(string)),              // ��� �������
                new DataColumn("LASTLOAD", typeof(DateTime)),           // ���� ��������� ������� ��������
                new DataColumn("LOAD_HOST", typeof(string)),            // Host (IP) ������� ��������
                new DataColumn("LOAD_PORT", typeof(int)),               // ���� ������� ��������
                new DataColumn("FLAG_LOAD", typeof(string)),            // ����� �������� � �������
                new DataColumn("MD5", typeof(string)) }));              // ����������� ����� MD5
            DT[BD_TINF].dt.PrimaryKey = new DataColumn[] { DT[BD_TINF].dt.Columns["DT_NAME"] };
            DT[BD_TINF].nType = TBLTYPE.NSI | TBLTYPE.INTERN;           // ������ ���
            DT[BD_TINF].dt.Columns["LOAD_HOST"].DefaultValue = "";
            DT[BD_TINF].dt.Columns["LOAD_PORT"].DefaultValue = 0;
            DT[BD_TINF].dt.Columns["MD5"].DefaultValue = "";

            // ������� ������
            DT.Add(BD_PASPORT, new TableDef(BD_PASPORT, new DataColumn[]{
                new DataColumn("KD", typeof(string)),                   // ��� ������
                new DataColumn("TD", typeof(string)),                   // ��� ������
                new DataColumn("NAME", typeof(string)),                 // ������������
                new DataColumn("SD", typeof(string)),                   // ��������
                new DataColumn("MD", typeof(string)) }));               // ��������
            DT[BD_PASPORT].dt.PrimaryKey = new DataColumn[] { DT[BD_PASPORT].dt.Columns["KD"] };
            DT[BD_PASPORT].nType = TBLTYPE.PASPORT | TBLTYPE.NSI | TBLTYPE.LOAD;
            //DT[BD_PASPORT].nType = TBLTYPE.PASPORT | TBLTYPE.NSI | TBLTYPE.INTERN;
            DT[BD_PASPORT].Text = "�������";

            // ���������� �������������
            DT.Add(NS_USER, new TableDef(NS_USER, new DataColumn[]{
                new DataColumn("KP", typeof(string)),                   // ��� ������������
                new DataColumn("NMP", typeof(string)),                  // ��� ������������
                new DataColumn("TABN", typeof(string)),                 // ��������� �����
                new DataColumn("PP", typeof(string)),                   // ������
                new DataColumn("KSK", typeof(int)),                     // ��� ������
                new DataColumn("RIGHTS", typeof(int)) }));              // ����� �������

            DT[NS_USER].dt.PrimaryKey = new DataColumn[] { 
                DT[NS_USER].dt.Columns["KP"]
                //DT[NS_USER].dt.Columns["TABN"] 
            };
            DT[NS_USER].Text = "������������";

            // ������
            DT.Add(NS_SKLAD, new TableDef(NS_SKLAD, new DataColumn[]{
                new DataColumn("KSK", typeof(int)),                     // ��� ������
                new DataColumn("NAME", typeof(string)),                 // ������������ ������
                new DataColumn("ISFACTORY", typeof(int)) }));           // ���� ����������������� �������
            DT[NS_SKLAD].dt.PrimaryKey = new DataColumn[] { DT[NS_SKLAD].dt.Columns["KSK"] };
            DT[NS_SKLAD].Text = "������";

            // ������� �������
            DT.Add(NS_SUSK, new TableDef(NS_SUSK, new DataColumn[]{
                new DataColumn("KSK", typeof(int)),                     // ��� ������
                new DataColumn("NUCH", typeof(int)),                    // ��� �������
                new DataColumn("NAME", typeof(string)) }));             // ������������ �������
            DT[NS_SUSK].dt.PrimaryKey = new DataColumn[] { DT[NS_SUSK].dt.Columns["KSK"], DT[NS_SUSK].dt.Columns["NUCH"] };
            DT[NS_SUSK].Text = "������� �������";
            DT[NS_SUSK].nType = TBLTYPE.INTERN | TBLTYPE.NSI;

            // �����������
            DT.Add(NS_MC, new TableDef(NS_MC, new DataColumn[]{
                new DataColumn("KMC", typeof(string)),                  // GUID-��� (C(10))
                new DataColumn("KMT", typeof(string)),                  // ��� ��������� C(10)
                new DataColumn("KRKMC", typeof(int)),                   // ������� ��� 
                new DataColumn("SNM", typeof(string)),                  // ����������� (C(30))
                new DataColumn("EAN13", typeof(string)),                // EAN13 (C(13))
                //new DataColumn("ITF14", typeof(string)),                // ITF14 (C(14))
                new DataColumn("SRR", typeof(int)),                     // ���� ���������� (����) (N(6))
                new DataColumn("KSK", typeof(int)),                     // ���������������� ��������
                new DataColumn("SRP", typeof(int))  }));                // ������� �������� (1-�������) (N(3))
            DT[NS_MC].dt.PrimaryKey = new DataColumn[] { DT[NS_MC].dt.Columns["KMC"] };
            DT[NS_MC].dt.Columns["SRP"].AllowDBNull = false;
            DT[NS_MC].Text = "���. ��������";

            // ���������� ��������
            DT.Add(NS_SEMK, new TableDef(NS_SEMK, new DataColumn[]{
                new DataColumn("KMC", typeof(string)),                  // ��� ��������(C(10))
                new DataColumn("KRK", typeof(int)),                     // ���������� ���� (N(5))
                new DataColumn("KT", typeof(int)),                      // ��� ����(N(2))
                new DataColumn("EMK", typeof(FRACT)),                   // �������/���   (N(?))
                new DataColumn("EMKPOD", typeof(int)),                  // ������� ������� � ������ ������
                new DataColumn("ITF14", typeof(string)),                // ITF14 (C(14))
                new DataColumn("KRKMC", typeof(int)),                   // ������� ��� 
                new DataColumn("PR", typeof(int)) }));                  // ��������� (N(4))
            DT[NS_SEMK].Text = "�������";
            DT[NS_SEMK].dt.Columns["EMK"].DefaultValue = 0;
            DT[NS_SEMK].dt.Columns["EMKPOD"].DefaultValue = 0;


            // �������� ������� ��� � �����
            DT.Add(NS_ADR, new TableDef(NS_ADR, new DataColumn[]{
                new DataColumn("KADR", typeof(string)),             // ����� ������-����
                new DataColumn("NAME", typeof(string)),             // ������������
                new DataColumn("TYPE_R", typeof(int)) }));          // ���
            DT[NS_ADR].dt.PrimaryKey = new DataColumn[] { DT[NS_ADR].dt.Columns["KADR"] };
            //DT[NS_ADR].nState = DT_STATE_INIT;
            DT[NS_ADR].Text = "������";
            // ������� ��� ����������� ������ NameAdr(nSklad, sAdr);

            // ����������� / ����������
            DT.Add(NS_PP, new TableDef(NS_PP, new DataColumn[]{
                new DataColumn("KPL", typeof(string)),                  // ��� ����������� (C(8))
                new DataColumn("KPP", typeof(string)),                  // ������ ��� ����������
                new DataColumn("KRKPP", typeof(int)),                   // ��� (N(4))
                new DataColumn("NAME", typeof(string)) }));             // ������������ (C(50))
            DT[NS_PP].dt.PrimaryKey = new DataColumn[] { DT[NS_PP].dt.Columns["KRKPP"] };
            DT[NS_PP].Text = "����������-�����������";
            DT[NS_PP].nType = TBLTYPE.INTERN | TBLTYPE.NSI;

            // �����������
            DT.Add(NS_EKS, new TableDef(NS_EKS, new DataColumn[]{
                new DataColumn("KEKS", typeof(int)),                    // ��� ����������� (N(5))
                new DataColumn("FIO", typeof(string)) }));              // ��� ����������� (C(50))
            DT[NS_EKS].dt.PrimaryKey = new DataColumn[] { DT[NS_EKS].dt.Columns["KEKS"] };
            DT[NS_EKS].Text = "�����������";
            DT[NS_EKS].nType = TBLTYPE.INTERN | TBLTYPE.NSI;

            // ���������� ����
            DT.Add(NS_SMEN, new TableDef(NS_SMEN, new DataColumn[]{
                new DataColumn("KSMEN", typeof(string)),                // ��� �����
                new DataColumn("NAME", typeof(string)) }));             // ������������ �����
            DT[NS_SMEN].dt.PrimaryKey = new DataColumn[] { DT[NS_SMEN].dt.Columns["KSMEN"] };
            DT[NS_SMEN].Text = "�����";
            DT[NS_SMEN].nType = TBLTYPE.INTERN | TBLTYPE.NSI;

            // ���������� ������ �����
            DT.Add(NS_PRPR, new TableDef(NS_PRPR, new DataColumn[]{
                new DataColumn("KPR", typeof(string)),                  // ��� ������� ������
                new DataColumn("KRK", typeof(int)),                     // ��� ������� �������
                //new DataColumn("NAME", typeof(string)),                 // ������������ �������
                new DataColumn("SNM", typeof(string))}));               // ������� ������������ �������
            DT[NS_PRPR].dt.PrimaryKey = new DataColumn[] { DT[NS_PRPR].dt.Columns["KPR"] };
            DT[NS_PRPR].Text = "������� �����";
            DT[NS_PRPR].nType = TBLTYPE.INTERN | TBLTYPE.NSI;

            // ���������� ���� �����������
            DT.Add(NS_KRUS, new TableDef(NS_KRUS, new DataColumn[]{
                new DataColumn("KMC", typeof(string)),                  // ��� (C(10))
                new DataColumn("EAN13", typeof(string)),                // ��� (C(10))
                new DataColumn("KINT", typeof(string))  }));            // ���������� ���
            DT[NS_KRUS].dt.PrimaryKey = new DataColumn[] { DT[NS_KRUS].dt.Columns["KINT"] };
            DT[NS_KRUS].Text = "���� �����������";
            DT[NS_KRUS].nType = TBLTYPE.INTERN | TBLTYPE.NSI;

            // �������������� ����������
            DT.Add(NS_AI, new TableDef(NS_AI, new DataColumn[]{
                new DataColumn("KAI", typeof(string)),              // ��� ��������������
                new DataColumn("NAME", typeof(string)),             // ������������
                new DataColumn("TYPE", typeof(string)),             // ��� ������
                new DataColumn("MAXL", typeof(int)),                // ����� ������
                new DataColumn("VARLEN", typeof(int)),              // ������� ���������� �����
                new DataColumn("DECP", typeof(int)),                // ������� ���������� �����
                new DataColumn("PROP", typeof(string)),             // ����
                new DataColumn("KED", typeof(string)) }));          // ��� �������
            DT[NS_AI].dt.PrimaryKey = new DataColumn[] { DT[NS_AI].dt.Columns["KAI"] };
            DT[NS_AI].nType = TBLTYPE.INTERN | TBLTYPE.NSI;
            DT[NS_AI].nState = DT_STATE_INIT;
            DT[NS_AI].Text = "�������������� ����������";

            // ��������� ����������
            DT.Add(BD_DOCOUT, new TableDef(BD_DOCOUT, new DataColumn[]{
                new DataColumn("SYSN", typeof(int)),                    // ID ��� (N(9))
                new DataColumn("TD", typeof(int)),                      // ��� ��������� (N(2))
                new DataColumn("NOMD", typeof(string)),                 // ����� ��������� (C(10))
                new DataColumn("DT", typeof(string)),                   // ���� (C(8))
                new DataColumn("KSK", typeof(int)),                     // ��� ������ (N(3))
                new DataColumn("DOCBC", typeof(string)),                // �������� ��������� (Doc_Barcode)
                new DataColumn("MLBC", typeof(string)),                 // �������� ����.����� (ML_Barcode)
                new DataColumn("SOURCE", typeof(int)),                  // ������������� N(2))
                new DataColumn("TIMECR", typeof(DateTime)),             // ����-����� ��������

                new DataColumn("CHKSSCC", typeof(int)),                 // ��� �������� SSCC

                new DataColumn("KRKPP", typeof(int)),                   // ��� ���������� (N(4))
                new DataColumn("KEKS", typeof(int)),                    // ��� ����������� (N(5))
                new DataColumn("KSMEN", typeof(string)),                // ��� ����� (C(3))
                new DataColumn("NUCH", typeof(int)),                    // ����� ������� (N(3))
                new DataColumn("MEST", typeof(int)),                    // ���������� ����(N(3))
                new DataColumn("MESTZ", typeof(int)),                   // ���������� ���� �� ������(N(3))
                new DataColumn("TYPOP", typeof(int)),                   // ��� �������� (�������, ��������, ...)
                new DataColumn("LSTUCH", typeof(string)),               // ������ ��������
                new DataColumn("LSTNPD", typeof(string)),               // ������ ������� ��������
                new DataColumn("DIFF", typeof(int)),                    // ���������� �� ������
                new DataColumn("CONFSCAN", typeof(int)),                // ����� ������������� ������������(�����)

                new DataColumn("SSCCONLY", typeof(int)),                // 1 - ����� ����� - ������ SSCC

                new DataColumn("EXPR_DT", typeof(string)),              // ��������� ��� ����
                new DataColumn("EXPR_SRC", typeof(string)),             // ��������� ��� �������������
                new DataColumn("PP_NAME", typeof(string)),              // ������������ ����������
                new DataColumn("EKS_NAME", typeof(string)),             // ������������ �����������
                //new DataColumn("KOLE", typeof(FRACT)),                  // ���������� ������ (N(10,3))
                
                new DataColumn("KOBJ", typeof(string))  }));            // ��� ������� (C(10))

            DT[BD_DOCOUT].dt.Columns["EXPR_DT"].Expression = "substring(DT,7,2) + '.' + substring(DT,5,2) + '.' + substring(DT,3,2)";
            DT[BD_DOCOUT].dt.Columns["EXPR_SRC"].Expression = "iif(SOURCE=1,'����', iif(SOURCE=2,'����','����'))";

            DT[BD_DOCOUT].dt.Columns["DIFF"].DefaultValue = NSI.DOCCTRL.UNKNOWN;
            DT[BD_DOCOUT].dt.Columns["MEST"].DefaultValue = 0;
            DT[BD_DOCOUT].dt.Columns["MESTZ"].DefaultValue = 0;
            DT[BD_DOCOUT].dt.Columns["TIMECR"].DefaultValue = DateTime.Now; ;
            DT[BD_DOCOUT].dt.Columns["TYPOP"].DefaultValue = AppC.TYPOP_PRMK;
            DT[BD_DOCOUT].dt.Columns["CONFSCAN"].DefaultValue = 0;
            DT[BD_DOCOUT].dt.Columns["CHKSSCC"].DefaultValue = 0;

            DT[BD_DOCOUT].dt.PrimaryKey = new DataColumn[] { DT[BD_DOCOUT].dt.Columns["SYSN"] };
            DT[BD_DOCOUT].dt.Columns["SYSN"].AutoIncrement = true;
            DT[BD_DOCOUT].dt.Columns["SYSN"].AutoIncrementSeed = -1;
            DT[BD_DOCOUT].dt.Columns["SYSN"].AutoIncrementStep = -1;
            DT[BD_DOCOUT].nType = TBLTYPE.BD;

            // ��������� ������ (���������)
            DT.Add(BD_DOUTD, new TableDef(BD_DOUTD, new DataColumn[]{
                new DataColumn("SYSN", typeof(int)),                    // ���� ��������� (N(9))
                new DataColumn("ID", typeof(int)),                      // ID ������
                new DataColumn("KRKMC", typeof(int)),                   // ������� ��� (N(4))
                new DataColumn("KOLE", typeof(FRACT)),                  // ����� (������ ��� ���) (N(10,3))
                new DataColumn("BARCODE", typeof(string)),              // ��������� ��� (C(10))
                new DataColumn("TARTYPE", typeof(string)),              // ��� ����-���������

                new DataColumn("KMC", typeof(string)),                  // ��� (C(10))
                new DataColumn("SNM", typeof(string)),                  // ����������� (C(30))
                new DataColumn("KOLM", typeof(int)),                    // ���������� ���� (N(4))
                new DataColumn("KOLP", typeof(int)),                    // ���������� ��������
                new DataColumn("EMK", typeof(FRACT)),                   // �������   (N(?))
                //new DataColumn("NP", typeof(int)),                      // � ������ (N(4))
                new DataColumn("NP", typeof(string)),                      // � ������ (N(4))
                new DataColumn("DVR", typeof(string)),                  // ���� ��������� (D(8))
                new DataColumn("DTG", typeof(string)),                  // ���� �������� (D(8))

                new DataColumn("EAN13", typeof(string)),                // EAN13 (C(13))
                new DataColumn("ITF14", typeof(string)),                // GTIN (C(14))

                new DataColumn("SRP", typeof(int)),                     // ������� �������� (1-�������) (N(3))
                new DataColumn("KRKT", typeof(int)),                    // ������� ��� ����(N(2))
                new DataColumn("VES", typeof(FRACT)),                   // ����� (������ ��� ���) (N(10,3))
                new DataColumn("KOLSH", typeof(int)),                   // �� ����������� ��������-����/�������� (N(2))
                new DataColumn("DEST", typeof(int)),                    // ���������� ������
                new DataColumn("NPODDZ", typeof(int)),                  // � ������� �� ������

                new DataColumn("ADRFROM", typeof(string)),              // ����� �����������
                new DataColumn("ADRTO", typeof(string)),                // ����� ���������

                new DataColumn("NPODD", typeof(int)),                   // � ������� ������ ������
                new DataColumn("NMESTA", typeof(int)),                  // � �����
                new DataColumn("SSCC", typeof(string)),                 // ID �������
                new DataColumn("SSCCINT", typeof(string)),              // ���������� SSCC �������

                new DataColumn("NZAKL", typeof(int)),                   // � ����������

                new DataColumn("USER", typeof(string)),                 // ��� ������������
                new DataColumn("SRC", typeof(int)),                     // ������������� ������
                new DataColumn("TIMECR", typeof(DateTime)),             // ����-����� ��������
                new DataColumn("TIMEOV", typeof(DateTime)),             // ����-����� ��������

                new DataColumn("NPP_ZVK", typeof(int)),                 // ID ������-������

                new DataColumn("STATE", typeof(int)) }));               // ��������� ������
            DT[BD_DOUTD].dt.PrimaryKey = new DataColumn[] { DT[BD_DOUTD].dt.Columns["SYSN"], 
                DT[BD_DOUTD].dt.Columns["KRKMC"], 
                DT[BD_DOUTD].dt.Columns["EMK"], 
                DT[BD_DOUTD].dt.Columns["NP"],
                DT[BD_DOUTD].dt.Columns["ID"] };

            DT[BD_DOUTD].dt.Columns["ID"].AutoIncrement = true;
            DT[BD_DOUTD].dt.Columns["ID"].AutoIncrementSeed = -1;
            DT[BD_DOUTD].dt.Columns["ID"].AutoIncrementStep = -1;

            DT[BD_DOUTD].dt.Columns["DEST"].DefaultValue = DESTINPROD.USER;
            DT[BD_DOUTD].dt.Columns["SRC"].DefaultValue = SRCDET.HANDS;
            DT[BD_DOUTD].dt.Columns["TIMECR"].DefaultValue = DateTime.Now;
            DT[BD_DOUTD].dt.Columns["VES"].DefaultValue = 0;

            DT[BD_DOUTD].dt.Columns["ADRFROM"].DefaultValue = "";
            DT[BD_DOUTD].dt.Columns["ADRTO"].DefaultValue = "";
            DT[BD_DOUTD].dt.Columns["NPODD"].DefaultValue = 0;
            DT[BD_DOUTD].dt.Columns["NMESTA"].DefaultValue = 0;
            DT[BD_DOUTD].dt.Columns["NPODDZ"].DefaultValue = 0;
            DT[BD_DOUTD].dt.Columns["STATE"].DefaultValue = AppC.OPR_STATE.OPR_EMPTY;
            DT[BD_DOUTD].nType = TBLTYPE.BD;

            // ��������� ������ ������
            DT.Add(BD_DIND, new TableDef(BD_DIND, new DataColumn[]{
                new DataColumn("SYSN", typeof(int)),                    // ���� ��������� (N(9))
                new DataColumn("ID", typeof(int)),                      // ID ������
                new DataColumn("KRKMC", typeof(int)),                   // ������� ��� (N(4))
                new DataColumn("KOLE", typeof(FRACT)),                  // ����� (������ ��� ���) (N(10,3))
                new DataColumn("KOLM", typeof(int)),                    // ���������� ���� (N(4))
                new DataColumn("KOLP", typeof(int)),                    // ���������� ��������

                new DataColumn("DVR", typeof(string)),                  // ���� ��������� (D(8))
                new DataColumn("DTG", typeof(string)),                  // ���� �������� (D(8))
                new DataColumn("NP", typeof(string)),                      // � ������ (N(4))
                new DataColumn("EMK", typeof(FRACT)),                   // �������   (N(?))
                new DataColumn("EAN13", typeof(string)),                // EAN13 (C(13))
                new DataColumn("ITF14", typeof(string)),                // ITF14

                new DataColumn("KMC", typeof(string)),                  // ��� (C(10))
                new DataColumn("SNM", typeof(string)),                  // ����������� (C(30))
                new DataColumn("KRKT", typeof(int)),                    // ������� ��� ����(N(2))
                new DataColumn("SRP", typeof(int)),                     // ������� �������� (1-�������) (N(3))
                new DataColumn("KOLSH", typeof(int)),                   // �� ����������� ��������-����/�������� (N(2))

                new DataColumn("COND", typeof(int)),                    // ������� �� ������
                new DataColumn("READYZ", typeof(int)),                  // ���������� ������ �� ���������
                new DataColumn("NPODDZ", typeof(int)),                  // � �������

                new DataColumn("ADRFROM", typeof(string)),              // ����� �����������
                new DataColumn("ADRTO", typeof(string)),                // ����� ���������

                new DataColumn("NPP", typeof(int)),                     // � ������� �/� ��� ������� �������
                new DataColumn("SSCC", typeof(string)),                 // ID �������

                new DataColumn("IDX", typeof(int)),                      // ID ������

                new DataColumn("SSCCINT", typeof(string)) }));          // ���������� SSCC �������

            DT[BD_DIND].dt.Columns["NP"].DefaultValue = "";
            DT[BD_DIND].dt.Columns["NP"].AllowDBNull = false;
            DT[BD_DIND].dt.Columns["COND"].DefaultValue = SPECCOND.NO;
            DT[BD_DIND].dt.Columns["READYZ"].DefaultValue = READINESS.NO;
            DT[BD_DIND].dt.Columns["SSCC"].DefaultValue = "";
            DT[BD_DIND].dt.Columns["SSCCINT"].DefaultValue = "";

            DT[BD_DIND].nType = TBLTYPE.BD | TBLTYPE.LOAD;

            DT[BD_DIND].dt.Columns["ID"].AutoIncrement = true;
            DT[BD_DIND].dt.Columns["ID"].AutoIncrementSeed = -1;
            DT[BD_DIND].dt.Columns["ID"].AutoIncrementStep = -1;

            // ���������� KMC �� �������
            DT.Add(BD_ADRKMC, new TableDef(BD_ADRKMC, new DataColumn[]{
                new DataColumn("SYSN", typeof(int)),                    // ���� ��������� (N(9))
                new DataColumn("IDX", typeof(int)),                     // ID ������

                new DataColumn("KADR", typeof(string)),              // ����� �����������
                new DataColumn("NP", typeof(string)),                      // � ������ (N(4))
                new DataColumn("DVR", typeof(string)),                  // ���� ��������� (D(8))
                new DataColumn("DTG", typeof(string)),                  // ���� �������� (D(8))

                new DataColumn("EMK", typeof(FRACT)),                   // �������   (N(?))
                new DataColumn("KOLM", typeof(int)),                    // ���������� ���� (N(4))
                new DataColumn("KOLE", typeof(FRACT)),                  // ����� (������ ��� ���) (N(10,3))
                new DataColumn("KOLP", typeof(int)),                    // ���������� ��������
                new DataColumn("SSCC", typeof(string)),                 // ID �������

                new DataColumn("ID", typeof(int)) }));                  // ID ������
            DT[BD_ADRKMC].nType = TBLTYPE.BD;

            // ������ SSCC ��� ���������
            DT.Add(BD_SSCC, new TableDef(BD_SSCC, new DataColumn[]{
                new DataColumn("SYSN", typeof(int)),                    // ���� ��������� (N(9))
                new DataColumn("NPODDZ",    typeof(int)),               // � �������
                new DataColumn("SSCC", typeof(string)),                 // SSCC �������
                new DataColumn("IN_ZVK", typeof(int)),                  // 1 - �������� � ������� (��� ������)
                new DataColumn("IN_TTN", typeof(int)),                  // 1 - �����������������������
                new DataColumn("STATE", typeof(int)),                   // ���������
                new DataColumn("ID", typeof(int)) }));                  // ID ������
            DT[BD_SSCC].nType = TBLTYPE.BD;
            DT[BD_SSCC].dt.Columns["ID"].AutoIncrement = true;
            DT[BD_SSCC].dt.Columns["ID"].AutoIncrementSeed = -1;
            DT[BD_SSCC].dt.Columns["ID"].AutoIncrementStep = -1;
            DT[BD_SSCC].dt.Columns["IN_ZVK"].DefaultValue = 0;
            DT[BD_SSCC].dt.Columns["IN_TTN"].DefaultValue = 0;

            //// ������ ���������� SSCC ��� ���������
            //DT.Add(BD_SSCCI, new TableDef(BD_SSCCI, new DataColumn[]{
            //    new DataColumn("SYSN", typeof(int)),                    // ���� ��������� (N(9))
            //    new DataColumn("NOMPAL", typeof(int)),                  // � �������
            //    new DataColumn("SSCC", typeof(string)),                 // SSCC �������
            //    new DataColumn("STATE", typeof(int)),                   // ���������
            //    new DataColumn("ID", typeof(int)) }));                  // ID ������
            //DT[BD_SSCCI].nType = TBLTYPE.BD;
            //DT[BD_SSCCI].dt.Columns["ID"].AutoIncrement = true;
            //DT[BD_SSCCI].dt.Columns["ID"].AutoIncrementSeed = -1;
            //DT[BD_SSCCI].dt.Columns["ID"].AutoIncrementStep = -1;

            // ������ ����� � ���������
            DT.Add(BD_SPMC, new TableDef(BD_SPMC, new DataColumn[]{
                new DataColumn("SYSN", typeof(int)),                    // ���� ��������� (N(9))
                new DataColumn("ID", typeof(int)),                      // ID ������ ���������
                new DataColumn("IDB", typeof(int)),                     // ID ������ �����
                new DataColumn("SNM", typeof(string)),                  // ������������ �������
                new DataColumn("KOLM", typeof(int)),                    // ���������� ���� (N(4))
                new DataColumn("KOLE", typeof(FRACT)),                  // ����� (������ ��� ���) (N(10,3))
                new DataColumn("KRK", typeof(int)),                     // ��� ������� �������
                new DataColumn("KPR", typeof(string)),                  // ��� ������� ������
                new DataColumn("TIMECR", typeof(DateTime))}));          // ����-����� ��������
            //DT[BD_SPMC].dt.PrimaryKey = new DataColumn[] { DT[BD_SPMC].dt.Columns["SYSN"], 
            //    DT[BD_SPMC].dt.Columns["ID"], 
            //    DT[BD_SPMC].dt.Columns["IDB"]};
            DT[BD_SPMC].dt.PrimaryKey = new DataColumn[] { DT[BD_SPMC].dt.Columns["ID"], 
                DT[BD_SPMC].dt.Columns["IDB"]};

            DT[BD_SPMC].dt.Columns["IDB"].AutoIncrement = true;
            DT[BD_SPMC].dt.Columns["IDB"].AutoIncrementSeed = -1;
            DT[BD_SPMC].dt.Columns["IDB"].AutoIncrementStep = -1;

            DT[BD_SPMC].dt.Columns["KOLM"].DefaultValue = 0;
            DT[BD_SPMC].dt.Columns["KOLE"].DefaultValue = 0;
            DT[BD_SPMC].dt.Columns["SNM"].DefaultValue = "";

            DT[BD_SPMC].nType = TBLTYPE.BD;
            DT[BD_SPMC].Text = "������ �����";

            //// ������ ���� ��� ������
            //DT.Add(BD_SOTG, new TableDef(BD_SOTG, new DataColumn[]{
            //    new DataColumn("SYSN", typeof(int)),                    // ���� ��������� (N(9))
            //    new DataColumn("NPP", typeof(int)),                     // ���� ��������� (N(9))
            //    new DataColumn("ID", typeof(int)),                      // ID ������
            //    new DataColumn("KSMEN", typeof(string)),                // ��� ����� (C(3))
            //    //new DataColumn("DTP", typeof(DateTime)),                // ����/����� ��������
            //    //new DataColumn("DTU", typeof(DateTime)),                // ����/����� ������
            //    new DataColumn("DTP", typeof(string)),                // ����/����� ��������
            //    new DataColumn("DTU", typeof(string)),                // ����/����� ������
            //    new DataColumn("NSH", typeof(int)),                     // � �����
            //    new DataColumn("KEKS", typeof(int)),                    // ��� ����������� (N(5))
            //    new DataColumn("KAVT", typeof(string)),                 // � ����
            //    new DataColumn("NPL", typeof(int)),                     // � ��������
            //    new DataColumn("ND", typeof(int)),                      // � ���������
            //    new DataColumn("ROUTE", typeof(string)),                // �������� ��������
            //    new DataColumn("STATE", typeof(int))}));                // ���������
            //DT[BD_SOTG].dt.PrimaryKey = new DataColumn[] { DT[BD_SOTG].dt.Columns["ID"] };

            //DT[BD_SOTG].dt.Columns["ID"].AutoIncrement = true;
            //DT[BD_SOTG].dt.Columns["ID"].AutoIncrementSeed = -1;
            //DT[BD_SOTG].dt.Columns["ID"].AutoIncrementStep = -1;

            //DT[BD_SOTG].dt.Columns["STATE"].DefaultValue = 0;
            //DT[BD_SOTG].nType = TBLTYPE.BD;
            //DT[BD_SOTG].Text = "������ ����";


            // ��������� ������� �� ������������
            DT.Add(BD_KMPL, new TableDef(BD_KMPL, new DataColumn[]{
                new DataColumn("TD", typeof(int)),                      // ��� ��������� (N(2))
                new DataColumn("KRKPP", typeof(int)),                   // ��� ���������� (N(4))
                new DataColumn("KSMEN", typeof(string)),                // ��� ����� (C(3))
                new DataColumn("DT", typeof(string)),                   // ���� (C(8))
                new DataColumn("KSK", typeof(int)),                     // ��� ������ (N(3))
                new DataColumn("NUCH", typeof(string)),                 // ������ ��������
                new DataColumn("KEKS", typeof(int)),                    // ��� ����������� (N(5))
                new DataColumn("NOMD", typeof(string)),                 // ����� ��������� (C(10))
                new DataColumn("SYSN", typeof(long)),                    // ID ��� (N(9))
                new DataColumn("KOLPODD", typeof(int)),                 // �������� ��� ���������

                new DataColumn("EXPR_DT", typeof(string)),              // ��������� ��� ����
                
                new DataColumn("PP_NAME", typeof(string)),              // ������������ ����������
                new DataColumn("TYPOP", typeof(int)),                   // ��� �������� (�������, ��������, ...)
                
                new DataColumn("KOBJ", typeof(string))  }));            // ��� ������� (C(10))

            DT[BD_KMPL].dt.Columns["EXPR_DT"].Expression = "substring(DT,7,2) + '.' + substring(DT,5,2)";

            DT[BD_KMPL].dt.Columns["TYPOP"].DefaultValue = AppC.TYPOP_KMPL;

            DT[BD_KMPL].dt.PrimaryKey = new DataColumn[] { DT[BD_KMPL].dt.Columns["SYSN"] };
            DT[BD_KMPL].dt.Columns["SYSN"].AutoIncrement = true;
            DT[BD_KMPL].dt.Columns["SYSN"].AutoIncrementSeed = -1;
            DT[BD_KMPL].dt.Columns["SYSN"].AutoIncrementStep = -1;
            DT[BD_KMPL].nType = TBLTYPE.BD;

            // ������ �� ����� ����������
            DT.Add(NS_BLANK, new TableDef(NS_BLANK, new DataColumn[]{
                new DataColumn("TD",        typeof(int)),               // ��� ��������
                new DataColumn("KBL",       typeof(string)),            // ��� ������
                new DataColumn("NAME",      typeof(string)),            // ������������ ������
                new DataColumn("PS",        typeof(int)),               // �������� ��������� �����
                new DataColumn("NPARS",     typeof(int)) }));           // ���������� �������������� ����������
            DT[NS_BLANK].dt.PrimaryKey = new DataColumn[] { 
                DT[NS_BLANK].dt.Columns["TD"],
                DT[NS_BLANK].dt.Columns["KBL"]};
            DT[NS_BLANK].Text = "������ ����������";
            DT[NS_BLANK].nType = TBLTYPE.INTERN | TBLTYPE.NSI;
        }

        // �������� ������ ��������� ������
        public void ConnDTGrid(DataGrid dgDoc, DataGrid dgDet)
        {
            dgDoc.SuspendLayout();
            DT[BD_DOCOUT].dg = dgDoc;
            dgDoc.DataSource = DT[BD_DOCOUT].dt;
            CreateTableStyles(DT[BD_DOCOUT].dg);
            ChgGridStyle(BD_DOCOUT, GDOC_INV);
            dgDoc.ResumeLayout();

            // �������� ��������� �����
            dgDet.SuspendLayout();
            DT[BD_DOUTD].dg = dgDet;
            // � ������ - ��� �� Grid
            DT[BD_DIND].dg = dgDet;
            CreateTableStylesDet(dgDet);
            ChgGridStyle(BD_DIND, GDET_ZVK);
            // �� ��������� - �������� ���
            dgDet.DataSource = dsM.Relations[0].ChildTable;
            ChgGridStyle(BD_DOUTD, GDET_SCAN);
            dgDet.ResumeLayout();
        }

        // ����� ��������� ������� ���������� � �����
        private void CreateTableStyles(DataGrid dg)
        {
            // ����������� ����� ��� ����������� ��������
            System.Drawing.Color 
                colForFullAuto = System.Drawing.Color.LightGreen,
                colSpec = System.Drawing.Color.PaleGoldenrod;

            DataGridTextBoxColumn 
                nC;
            double
                nKoef = Screen.PrimaryScreen.Bounds.Width / 240.0;
            int
                nWMAdd= 0;
#if WMOBILE
            nWMAdd = 4;
#else
            nWMAdd = 0;
#endif

            dg.TableStyles.Clear();
            // ��� ��������������
            DataGridTableStyle tsi = new DataGridTableStyle();
            tsi.MappingName = GDOC_INV.ToString();

            DataGridTextBoxColumn t1 = new DataGridTextBoxColumn();
            t1.MappingName = "EXPR_DT";
            t1.HeaderText = "����";
            t1.Width = (int)(nKoef * 36 + nWMAdd);
            t1.NullText = "";
            tsi.GridColumnStyles.Add(t1);

            DataGridTextBoxColumn t = new DataGridTextBoxColumn();  
            t.MappingName = "TD";
            t.HeaderText = "���";
            t.Width = (int)(nKoef * 20 + nWMAdd);
            t.NullText = "";
            tsi.GridColumnStyles.Add(t);

            //DataGridTextBoxColumn t2 = new DataGridTextBoxColumn();
            //t2.MappingName = "KSMEN";
            //t2.HeaderText = "�����";
            //t2.Width = 37;
            //t2.NullText = "";
            //tsi.GridColumnStyles.Add(t2);

            //DataGridTextBoxColumn nUch = new DataGridTextBoxColumn();
            //nUch.MappingName = "NUCH";
            //nUch.HeaderText = "��";
            //nUch.Width = 20;
            //nUch.NullText = "";
            //tsi.GridColumnStyles.Add(nUch);

            //nC = new DataGridTextBoxColumn();
            //nC.MappingName = "KEKS";
            //nC.HeaderText = "����";
            //nC.Width = 33;
            //nC.NullText = "";
            //tsi.GridColumnStyles.Add(nC);

            DataGridTextBoxColumn cSt = new DataGridTextBoxColumn();
            cSt.MappingName = "EXPR_SRC";
            cSt.Width = (int)(nKoef * 42 + nWMAdd);
            cSt.HeaderText = "������";
            cSt.NullText = "";
            tsi.GridColumnStyles.Add(cSt);



            //DataGridTextBoxColumn nMi = new DataGridTextBoxColumn();
            //nMi.MappingName = "MEST";
            //nMi.HeaderText = "����";
            //nMi.Width = 42;
            //nMi.NullText = "";
            //tsi.GridColumnStyles.Add(nMi);


            ServClass.DGTBoxColorColumnDoc sColm = new ServClass.DGTBoxColorColumnDoc();
            sColm.Owner = dg;
            sColm.ReadOnly = true;
            sColm.AlternatingBackColor = colForFullAuto;
            sColm.AlternatingBackColorSpec = colSpec;
            sColm.MappingName = "MEST";
            sColm.HeaderText = "����";
            sColm.NullText = "";
            sColm.Width = (int)(nKoef * 34 + nWMAdd);
            tsi.GridColumnStyles.Add(sColm);


            //nC = new DataGridTextBoxColumn();
            //nC.MappingName = "MESTZ";
            //nC.HeaderText = "�����";
            //nC.Width = 44;
            //nC.NullText = "";
            //tsi.GridColumnStyles.Add(nC);

            //nC = new DataGridTextBoxColumn();
            //nC.MappingName = "DOCBC";
            //nC.HeaderText = "����";
            //nC.Width = (int)(nKoef * 82 + nWMAdd);
            //nC.NullText = "";
            //tsi.GridColumnStyles.Add(nC);

            nC = new DataGridTextBoxColumn();
            nC.MappingName = "NOMD";
            nC.HeaderText = "� ���";
            nC.Width = (int)(nKoef * 48 + nWMAdd);
            nC.NullText = "";
            tsi.GridColumnStyles.Add(nC);

            nC = new DataGridTextBoxColumn();
            nC.MappingName = "KSK";
            nC.HeaderText = "�����";
            nC.Width = (int)(nKoef * 41 + nWMAdd);
            nC.NullText = "";
            tsi.GridColumnStyles.Add(nC);

            //nC = new DataGridTextBoxColumn();
            //nC.MappingName = "DIFF";
            //nC.HeaderText = "��";
            //nC.Width = 18;
            //nC.NullText = "";
            //tsi.GridColumnStyles.Add(nC);

            //DataGridTextBoxColumn nVi = new DataGridTextBoxColumn();
            //nVi.MappingName = "KOLE";
            //nVi.HeaderText = "�����";
            //nVi.Width = 36;
            //nVi.NullText = "";
            //tsi.GridColumnStyles.Add(nVi);



            dg.TableStyles.Add(tsi);
        }

        private Color
            C_READY_ZVK = Color.LightGreen,                 // ��������� ������ ���������
            C_READY_TTN = Color.Lavender,                   // ��������� ��� ������ � ��������
            C_TNSFD_TTN = Color.LightGreen;                 // ��������� ��� �������� �� ������

        // ����� ������� ��������� ����� (��� � ������)
        private void CreateTableStylesDet(DataGrid dg)
        {
            DataGridTextBoxColumn 
                sColk, c;
            ServClass.DGTBoxColorColumn 
                sC;
            Color 
                colSpec = Color.PaleGoldenrod,
                colGreen = Color.LightGreen;
            double
                nKoef = Screen.PrimaryScreen.Bounds.Width / 240.0;
            int
                nWMDecMest = 0,
                nWMAdd = 0;
#if WMOBILE
            nWMAdd = 2;
            nWMDecMest = 4;
#endif

            dg.TableStyles.Clear();
            // ��� ����������� ������������
            DataGridTableStyle ts = new DataGridTableStyle();
            ts.MappingName = GDET_SCAN.ToString();

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "KRKMC";
            sC.HeaderText = "���";
            sC.Width = (int)(nKoef * 28 + nWMAdd + 4 - nWMDecMest); ;
            sC.AlternatingBackColor = C_READY_TTN;
            sC.AlternatingBackColorSpec = C_TNSFD_TTN;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "SNM";
            sC.HeaderText = "������������";
            sC.Width = (int)(134 * nKoef + nWMAdd - nWMDecMest);
            sC.AlternatingBackColor = C_READY_TTN;
            sC.AlternatingBackColorSpec = C_TNSFD_TTN;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "KOLM";
            sC.HeaderText = "���";
            sC.Width = (int)(32 * nKoef + nWMAdd - 6);
            sC.AlternatingBackColor = C_READY_TTN;
            sC.AlternatingBackColorSpec = C_TNSFD_TTN;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "KOLE";
            sC.HeaderText = "��.";
            sC.Width = (int)(43 * nKoef + nWMAdd);
            sC.AlternatingBackColor = C_READY_TTN;
            sC.AlternatingBackColorSpec = C_TNSFD_TTN;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "EMK";
            sC.HeaderText = "���";
            sC.Width = 35;
            sC.AlternatingBackColor = C_READY_TTN;
            sC.AlternatingBackColorSpec = C_TNSFD_TTN;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.AlternatingBackColor = colGreen;
            sC.AlternatingBackColorSpec = colSpec;
            sC.MappingName = "DVR";
            sC.HeaderText = "����";
            sC.Width = 36;
            sC.Alignment = HorizontalAlignment.Right;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "NP";
            sC.HeaderText = "���";
            sC.Width = (int)(35 * nKoef + nWMAdd); ;
            sC.NullText = "";
            sC.Alignment = HorizontalAlignment.Left;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.AlternatingBackColor = colGreen;
            sC.AlternatingBackColorSpec = colSpec;
            sC.MappingName = "DTG";
            sC.HeaderText = "�����";
            sC.Width = 36;
            sC.Alignment = HorizontalAlignment.Right;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "SSCC";
            sC.HeaderText = "SSCC";
            sC.Width = (int)(146 * nKoef + nWMAdd);
            sC.NullText = "";
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DOUTD);
            sC.MappingName = "SRC";
            sC.HeaderText = "���";
            sC.Width = (int)(16 * nKoef + nWMAdd);
            sC.NullText = "";
            ts.GridColumnStyles.Add(sC);

            dg.TableStyles.Add(ts);

            /// *************************** ��� ������ ************************
            ts = new DataGridTableStyle();
            ts.MappingName = GDET_ZVK.ToString();
            DataGridTableStyle tsK = new DataGridTableStyle();
            tsK.MappingName = GDET_ZVK_KMPL.ToString();

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.MappingName = "NPP";
            sC.HeaderText = "�";
            sC.Width = (int)(nKoef * 17 + nWMAdd - nWMDecMest);
            sC.Alignment = HorizontalAlignment.Right;
            sC.NullText = "";
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.MappingName = "KRKMC";
            sC.HeaderText = "���";
            sC.Width = (int)(nKoef * 27 + nWMAdd - nWMDecMest);
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.MappingName = "SNM";
            sC.HeaderText = "������������";
            sC.Width = (int)(132 * nKoef + nWMAdd - nWMDecMest);
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.AlternatingBackColor = colGreen;
            sC.AlternatingBackColorSpec = Color.Azure;
            sC.MappingName = "KOLM";
            sC.HeaderText = "����";
            sC.Width = (int)(29 * nKoef + nWMAdd - nWMDecMest);
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.AlternatingBackColor = colGreen;
            sC.AlternatingBackColorSpec = colSpec;
            sC.MappingName = "EMK";
            sC.HeaderText = "���";
            sC.Width = (int)(24 * nKoef + nWMAdd);
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.AlternatingBackColor = colGreen;
            sC.AlternatingBackColorSpec = colSpec;
            sC.MappingName = "KOLE";
            sC.HeaderText = "��.";
            sC.Width = (int)(45 * nKoef + nWMAdd);
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.MappingName = "NP";
            sC.HeaderText = "� ��";
            sC.Width = (int)(40 * nKoef + nWMAdd); ;
            sC.NullText = "";
            sC.Alignment = HorizontalAlignment.Right;
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.AlternatingBackColor = colGreen;
            sC.AlternatingBackColorSpec = colSpec;
            sC.MappingName = "DVR";
            sC.HeaderText = "����";
            sC.Width = 36;
            sC.Alignment = HorizontalAlignment.Right;
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.AlternatingBackColor = colGreen;
            sC.AlternatingBackColorSpec = colSpec;
            sC.MappingName = "DTG";
            sC.HeaderText = "�����";
            sC.Width = 36;
            sC.Alignment = HorizontalAlignment.Right;
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_DIND);
            sC.MappingName = "SSCC";
            sC.HeaderText = "SSCC";
            sC.Width = (int)(146 * nKoef + +nWMAdd);
            sC.NullText = "";
            ts.GridColumnStyles.Add(sC);
            tsK.GridColumnStyles.Add(sC);

            dg.TableStyles.Add(ts);
            dg.TableStyles.Add(tsK);
        }


        public string SortDet(bool bIsTTN, NSI x, ref string sNSort, bool bUseAdr)
        {
            int 
                nCurSort;
            string 
                nI, sRet = "";

            nI = (bIsTTN == true)?BD_DOUTD:BD_DIND;

            nCurSort = ((int)x.DT[nI].TSort) + 1;
            if ((!bUseAdr) && (nCurSort == (int)TABLESORT.ADRES))
                nCurSort++;
            if ((!bUseAdr) && (nCurSort == (int)TABLESORT.ADRESTO))
                nCurSort++;

            if (nCurSort == (int)TABLESORT.MAXDET)
                nCurSort = 0;
            x.DT[nI].TSort = nCurSort;
            sRet = SortName(bIsTTN, ref sNSort, true);

            x.DT[nI].sTSort = sRet;
            return (sRet);
        }

        public string SortName(bool bIsTTN, ref string sNSort, bool bShowMsg)
        {
            string
                sNameSort = "",
                sT = (bIsTTN)?BD_DOUTD:BD_DIND,
                sRet = "";
            switch ((TABLESORT)DT[sT].TSort)
            {
                case TABLESORT.NO:
                    sRet = "";
                    sNSort = "- - ";
                    sNameSort = "��� ����������";
                    break;
                case TABLESORT.ADRES:
                    sRet = "ADRFROM";
                    sNSort = "- �� -";
                    sNameSort = "�� ������(��������)";
                    break;
                case TABLESORT.ADRESTO:
                    sRet = "ADRTO";
                    sNSort = "- �� -";
                    sNameSort = "�� ������(��������)";
                    break;
                case TABLESORT.KRKMC:
                    sRet = "KRKMC";
                    sNSort = "- �� -";
                    sNameSort = "�� �������� ����";
                    break;
                case TABLESORT.NAMEMC:
                    sRet = "SNM";
                    sNSort = "- H - ";
                    sNameSort = "�� ������������";
                    break;
                case TABLESORT.RECSTATE:
                    sRet = (bIsTTN == true) ? "DEST" : "READYZ";
                    sNSort = "- B - ";
                    sNameSort = "�� ����������";
                    break;
            }
            if (bShowMsg)
                Srv.ErrorMsg("����������� ����������:\n" + sNameSort, "�������������", false);
            sNSort += "\xAF";
            if (DT[sT].sTFilt != "")
                sNSort += "F";
            return (sRet);
        }

        // ����� ����� �������
        // nSt - ��������� �����
        public void ChgGridStyle(string iT, int nSt)
        {
            if (DT[iT].nGrdStyle != -1)
            {                                                           // �� ��������� ���������
                int nOld = DT[iT].nGrdStyle;
                // ������� �������
                DT[iT].dg.TableStyles[nOld].MappingName = nOld.ToString();
                if (nSt == GDOC_NEXT)
                {                                                       // ����������� �����
                    nSt = ((nOld + 1) == DT[iT].dg.TableStyles.Count) ? 0 : nOld + 1;
                }
            }
            DT[iT].nGrdStyle = nSt;
            DT[iT].dg.TableStyles[nSt].MappingName = DT[iT].dt.TableName;
        }


        // �������� �� ��������� �������� ����������� � ������


        // �������������� ��� ����� �������
        public DataRow BD_TINF_RW(string sTName)
        {
            DataRow dr = DT[NSI.BD_TINF].dt.Rows.Find(new object[] { sTName });
            if (dr == null)
            {
                dr = DT[NSI.BD_TINF].dt.NewRow();
                dr["DT_NAME"] = sTName;
                dr["MD5"] = "";
                dr["LASTLOAD"] = DateTime.MinValue;
                dr["LOAD_HOST"] = "";
                dr["LOAD_PORT"] = 0;
                DT[NSI.BD_TINF].dt.Rows.Add(dr);
            }
            return (dr);
        }

      

        //internal bool ConnectByITF(string sEAN, int nKrKMC, int nKsk, ref PSC_Types.ScDat s)
        //{
        //    bool
        //        ret = false;
        //    return (ret);
        //}


        internal bool GetMCDataOnEAN(string sEAN, ref PSC_Types.ScDat s, bool bShowErr)
        {
            bool
                ret = false;
            int
                nPrzPl = -1;
            DataTable
                dt = DT[NS_MC].dt;
            DataRow
                dr = null;

            if (DT[NS_MC].nState > DT_STATE_INIT)  // ���������� ��������
            {
                string sss =
                    String.Format("EAN13 LIKE '{0}%'", sEAN);
                DataView
                    xRowDView = new DataView(DT[NSI.NS_MC].dt, String.Format("EAN13 LIKE '{0}%'", sEAN), "", DataViewRowState.CurrentRows);
                if (xRowDView.Count > 0)
                {
                    if (xRowDView.Count > 1)
                    {
                        if (bShowErr)
                            Srv.ErrorMsg(String.Format("EAN={0}\n���������� ITF({1})", sEAN, s.ci.ToString()), "���������������!", true);
                        return (false);
                    }
                    else
                        dr = xRowDView[0].Row;
                    ret = s.GetFromNSI(s.s, dr, ref nPrzPl);
                    if (!ret)
                    {
                        if (bShowErr)
                            Srv.ErrorMsg(String.Format("EAN={0}", sEAN), "�� ������� � ���!", true);
                    }
                }
            }
            return (ret);
        }


        internal bool Connect2MC(string sEAN, int nKrKMC, int nKsk, ref PSC_Types.ScDat s)
        {
            bool 
                bChoiceIsMade,
                ret = false;
            int
                i;
            string
                sFiltOnEAN = "";
            DataTable 
                dt = DT[NS_MC].dt;
            DataView 
                xRowDView = new DataView();
            DataRow 
                dr = null;

            try
            {

                if (nKrKMC > 0)              // ������� ���
                {// ������� �������
                    xRowDView = new DataView(DT[NSI.NS_MC].dt, String.Format("KRKMC={0}", nKrKMC),
                        "", DataViewRowState.CurrentRows);
                    if (xRowDView.Count >= 1)
                    {
                        dr = xRowDView[0].Row;
                        if (xRowDView.Count > 1)
                        {
                            if (xFF.xPars.UseList4ManyEAN)
                                dr = xFF.ManyEANChoice("", nKrKMC, false);
                        }
                    }
                    else
                        sFiltOnEAN = sEAN;
                }
                else
                {// ���� EAN13
                    if (sEAN.Length > 0)
                    {
                        sFiltOnEAN = sEAN;
                    }
                    else
                    {
                        return (ret);
                    }
                }

                if (sFiltOnEAN.Length > 0)
                {
                    xRowDView = new DataView(DT[NSI.NS_MC].dt, String.Format("EAN13 LIKE '{0}%'", sFiltOnEAN),
                        "", DataViewRowState.CurrentRows);
                        //if (xRowDView.Count == 1)
                        //{
                        //    nKsk = 0;
                        //    dr = xRowDView[0].Row;
                        //    s.bSetAccurCode = true;
                        //}
                    if (xRowDView.Count >= 1)
                    {
                        dr = xRowDView[0].Row;
                        if (xRowDView.Count == 1)
                        {
                            nKsk = 0;
                            s.bSetAccurCode = true;
                        }
                        else
                        {
                            //if (xFF.xPars.UseList4ManyEAN)
                            //    dr = xFF.ManyEANChoice(sFiltOnEAN, nKrKMC, true);
                        }
                    }
                }
                s.nEANs = xRowDView.Count;

                //if (nKrKMC <= 0)              // EAN ��� �� �����
                //{// ���� EAN13
                if (s.nEANs > 1)
                {
                    i = 0;
                    StrAndInt[] aN = new StrAndInt[s.nEANs];
                    foreach (DataRowView drv in xRowDView)
                    {
                        //aN[i] = new StrAndInt((string)drv.Row["SNM"], (int)drv.Row["KRKMC"]);
                        //aN[i].IntCodeAdd1 = (int)drv.Row["KSK"];
                        //aN[i].SNameAdd1 = (string)drv.Row["KMC"];
                        aN[i] = new StrAndInt((string)drv.Row["SNM"], (int)drv.Row["KRKMC"],
                            (string)drv.Row["KMC"], 
                            drv.Row,
                            (int)drv.Row["KSK"], 0
                            );

                        i++;
                        if (dr == null)
                        {
                            if ((nKsk < 0) || ((int)drv.Row["KSK"] == nKsk))
                                dr = drv.Row;
                        }
                    }
                    s.xEANs = new Srv.Collect4Show<StrAndInt>(aN);
                    s.xEANs.MoveEx(Srv.Collect4Show<StrAndInt>.DIR_MOVE.FORWARD);
                    if (nKsk < 0)
                    {
                        dr = xRowDView[0].Row;
                        nKsk = 0;
                    }
                }
                //}
                if (dr == null)
                {
                    //Srv.ErrorMsg(String.Format("EAN {0}\n�� ���������!", sEAN), true);
                    if (xFF.bEditMode)
                        Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                }
                if ((s.nEANs == 1) || (xFF.bEditMode))
                    ret = s.GetFromNSI(s.s, dr, ref nKsk);
                else
                    ret = true;
            }
            catch
            {
            }
            return (ret);
        }




        // ����� ����� ���������� ����� �����������
        internal bool IsAlien(string sEAN13, ref PSC_Types.ScDat s)
        {
            string sFind;
            bool ret = false;
            int nL = 0;
            DataRow dr = null;
            DataRow[] dra = null;

            if (DT[NS_KRUS].nState > DT_STATE_INIT)     // ���������� ��������
            {
                //sFind = sEAN13.Substring(0, 7);


                //dra = DT[NS_KRUS].dt.Select("[KINT] LIKE '" + sEAN13 + "'");
                dra = DT[NS_KRUS].dt.Select("[KINT] LIKE '" + sEAN13.Substring(0, 3) + "%'");
                foreach (DataRow drr in dra)
                {
                    sFind = (string)drr["KINT"];
                    nL = sFind.Length;
                    if ( (sFind == sEAN13.Substring(0, nL)))
                    {
                        dr = drr;
                        s.sIntKod = sFind;
                        break;
                    }
                }
                //DT[NS_KRUS].dt.DefaultView.RowFilter = String.Format("[KINT] LIKE '{0}%'", sV);

                //dr = DT[NS_KRUS].dt.Rows.Find(new object[] { sFind });
                if (dr != null)
                {
                    dr = DT[NS_MC].dt.Rows.Find(new object[] { (string)dr["EAN13"] });
                    if (dr != null)
                        ret = s.GetFromNSI(s.s, dr, ref nL);
                }
                DT[NS_KRUS].dt.DefaultView.RowFilter = "";
            }
            return (ret);
        }




        private void FillPoddonlLst(CurDoc xD)
        {
            DataTable 
                dt = DT[NSI.BD_DIND].dt,
                dtD = DT[NSI.BD_DOUTD].dt;

            // ������ ������� �������� �� ������
            DataView dv = new DataView(dt, xD.DefDetFilter(), "", DataViewRowState.CurrentRows);
            DataTable dtN = dv.ToTable(true, "NPODDZ");

            // ��� ��������� ������������?
            if ((dtN.Rows.Count == 1) && ( (int)(dtN.Rows[0]["NPODDZ"]) > 0 ))
            {
                xD.bFreeKMPL = true;
            }
            else
                xD.bFreeKMPL = false;

            // ������ ������� �������� �� ���������
            DataView dv1 = new DataView(dtD, xD.DefDetFilter(), "", DataViewRowState.CurrentRows);
            DataTable dtN1 = dv1.ToTable(true, "NPODDZ");

            //xCDoc.lstNomsFromZkz = new List<int>();
            //xCDoc.lstNomsFromZkz.Clear();

            //DataTable ddtt = (dtN1.Rows.Count > dtN.Rows.Count) ? dtN1 : dtN;
            //xCDoc.sLstNoms = "";
            xD.xNPs.Clear();
            foreach (DataRow dr in dtN.Rows)
            {
                //xCDoc.lstNomsFromZkz.Add((int)dr["NPODDZ"]);
                if (!xD.xNPs.ContainsKey((int)dr["NPODDZ"]))
                    xD.xNPs.Add((int)dr["NPODDZ"], new PoddonInfo());
            }
            foreach (DataRow dr in dtN1.Rows)
            {
                //xCDoc.lstNomsFromZkz.Add((int)dr["NPODDZ"]);
                if (!xD.xNPs.ContainsKey((int)dr["NPODDZ"]))
                    xD.xNPs.Add((int)dr["NPODDZ"], new PoddonInfo());
            }
        }

        //public string AdrName(string sA)
        //{
        //    string sR = sA;
        //    DataRow dr = DT[NS_ADR].dt.Rows.Find(new object[] { sA });
        //    if (dr != null)
        //        sR = (string)dr["NAME"];
        //    return (sR);
        //}




        // ������ ������� ������ � ������ ������ ����������
        public bool InitCurDoc(CurDoc xD, Smena xS)
        {
            bool 
                ret = false;
            int 
                i;
            DataRow 
                dr = xD.drCurRow;
            DocPars 
                x = xD.xDocP;

            if (dr != null)
            {
                try
                {
                    xD.nId = (int)((dr["SYSN"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["SYSN"]);
                    xD.nDocSrc = (int)((dr["SOURCE"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["SOURCE"]);

                    x.nNumTypD = (int)((dr["TD"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["TD"]);
                    x.sNomDoc = ((dr["NOMD"] == System.DBNull.Value) ? "" : dr["NOMD"].ToString());
                    x.nPol = (int)((dr["KRKPP"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["KRKPP"]);
                    x.sSmena = ((dr["KSMEN"] == System.DBNull.Value) ? "" : dr["KSMEN"].ToString());
                    x.nSklad = (int)((dr["KSK"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["KSK"]);
                    x.nUch = (int)((dr["NUCH"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["NUCH"]);
                    x.nEks = (int)((dr["KEKS"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["KEKS"]);

                    x.sBC_Doc = (string)((dr["DOCBC"] == System.DBNull.Value) ? "" : dr["DOCBC"]);
                    x.sBC_ML = (string)((dr["MLBC"] == System.DBNull.Value) ? "" : dr["MLBC"]);

                    try
                    {
                        x.dDatDoc = DateTime.ParseExact(dr["DT"].ToString(), "yyyyMMdd", null);
                    }
                    catch
                    {
                        x.dDatDoc = DateTime.MinValue;
                    }

                    xD.xDocP.TypOper = (int)((dr["TYPOP"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["TYPOP"]);

                    i = (int)((dr["CONFSCAN"] == System.DBNull.Value) ? 0 : dr["CONFSCAN"]);
                    xD.bConfScan = (i > 0) ? true : false;

                    xD.sLstUchNoms = (string)((dr["LSTUCH"] == System.DBNull.Value) ? "" : dr["LSTUCH"]);

                    xD.xNPs = new PoddonList();
                    if (xD.xDocP.TypOper == AppC.TYPOP_KMPL)
                    {
                        FillPoddonlLst(xD);
                        xS.FilterTTN = FILTRDET.UNFILTERED;
                        DT[NSI.BD_DOUTD].sTFilt = "";
                    }
                    xD.xDocP.lSysN = (int)((dr["DIFF"] == System.DBNull.Value) ? AppC.EMPTY_INT : dr["DIFF"]);

                    ret = true;
                }
                catch
                {

                }
            }
            return (ret);
        }



        // ���������� ������ ������� ����������
        public bool UpdateDocRec(DataRow dr, CurDoc xD)
        {
            bool ret = true;
            DocPars x = xD.xDocP;
            try
            {
                if (x.nSklad != AppC.EMPTY_INT)
                    dr["KSK"] = x.nSklad;
                else
                    dr["KSK"] = System.DBNull.Value;

                if (x.nUch != AppC.EMPTY_INT)
                    dr["NUCH"] = x.nUch;
                else
                    dr["NUCH"] = System.DBNull.Value;


                dr["DT"] = x.dDatDoc.ToString("yyyyMMdd");

                dr["KSMEN"] = x.sSmena;

                if (x.nNumTypD != AppC.EMPTY_INT)
                {
                    dr["TD"] = x.nNumTypD;
                    //if (x.nTypD)
                    //{
                    //}
                }
                else
                    dr["TD"] = System.DBNull.Value;


                
                dr["NOMD"] = x.sNomDoc;

                if (x.nPol != AppC.EMPTY_INT)
                {
                    dr["KRKPP"] = x.nPol;
                    dr["PP_NAME"] = x.sPol;
                }
                else
                {
                    dr["KRKPP"] = System.DBNull.Value;
                    dr["PP_NAME"] = "";
                }

                if (x.nEks != AppC.EMPTY_INT)
                {
                    dr["KEKS"] = x.nEks;
                    dr["EKS_NAME"] = x.sEks;
                }
                else
                {
                    dr["KEKS"] = System.DBNull.Value;
                    dr["EKS_NAME"] = "";
                }


                dr["TYPOP"] = xD.xDocP.TypOper;
                dr["CONFSCAN"] = xD.bConfScan;
                dr["DIFF"] = (int)(xD.xDocP.lSysN);

                dr["DOCBC"] = x.sBC_Doc;
                dr["MLBC"] = x.sBC_ML;
            }
            catch
            {
                ret = false;
            }
            return(ret);
        }

        // ���������� ����� ������ � ���������
        public bool AddDocRec(CurDoc xD)
        {
            bool ret = true;

            try
            {
                DataRow dr = DT[NSI.BD_DOCOUT].dt.NewRow();
                dr["SOURCE"] = DOCSRC_CRTD;
                dr["TIMECR"] = DateTime.Now;

                ret = UpdateDocRec(dr, xD);
                if (ret == true)
                {
                    DT[NSI.BD_DOCOUT].dt.Rows.Add(dr);
                    xD.nId = (int)dr["SYSN"];
                    xD.drCurRow = dr;
                }
            }
            catch {
                ret = false;
            }
            return (ret);
        }

        // ������ ������� ��������� ������
        public bool InitCurProd(ref PSC_Types.ScDat scD, DataRow drD)
        {
            bool ret = false;
            string 
                sD,
                s;
            MainF.AddrInfo
                xA;

            if (drD != null)
            {
                try
                {
                    scD.sKMC = (string)((drD["KMC"] == System.DBNull.Value) ? "" : drD["KMC"]);

                    scD.nKrKMC = (int)((drD["KRKMC"] == System.DBNull.Value) ? AppC.EMPTY_INT : drD["KRKMC"]);
                    scD.sN = (string)((drD["SNM"] == System.DBNull.Value) ? "" : drD["SNM"]);
                    scD.nMest = (int)((drD["KOLM"] == System.DBNull.Value) ? AppC.EMPTY_INT : drD["KOLM"]);
                    scD.fVsego = (FRACT)((drD["KOLE"] == System.DBNull.Value) ? 0 : drD["KOLE"]);
                    try
                    {
                        scD.nPalet = (int)drD["KOLP"];
                    }
                    catch { scD.nPalet = 0;}

                    scD.fEmk = (FRACT)((drD["EMK"] == System.DBNull.Value) ? 0 : drD["EMK"]);
                    scD.nParty = (string)((drD["NP"] == System.DBNull.Value) ? "" : drD["NP"]);
                    try
                    {
                        sD = drD["DVR"].ToString();
                        scD.dDataIzg = DateTime.ParseExact(sD, "yyyyMMdd", null);
                        scD.sDataIzg = scD.dDataIzg.ToString("dd.MM.yy");
                    }
                    catch
                    {
                        scD.dDataIzg = DateTime.MinValue;
                        scD.sDataIzg = "";
                    }

                    try
                    {
                        sD = drD["DTG"].ToString();
                        scD.dDataGodn = DateTime.ParseExact(sD, "yyyyMMdd", null);
                        scD.sDataGodn = scD.dDataGodn.ToString("dd.MM.yy");
                    }
                    catch
                    {
                        scD.dDataGodn = DateTime.MinValue;
                        scD.sDataGodn = "";
                    }

                    scD.sEAN = (string)((drD["EAN13"] == System.DBNull.Value) ? "" : drD["EAN13"]);
                    try
                    {
                        scD.sGTIN = (string)drD["ITF14"];
                    }
                    catch { scD.sGTIN = ""; }


                    int i = (int)((drD["SRP"] == System.DBNull.Value) ? AppC.EMPTY_INT : drD["SRP"]);
                    scD.bVes = (i == 1) ? true : false;


                    //scD.sGrK = (string)((drD["GKMC"] == System.DBNull.Value) ? "" : drD["GKMC"]);


                    //s = (string)((drD["ADRFROM"] == System.DBNull.Value) ? "" : drD["ADRFROM"]);
                    try
                    {
                        xA = new MainF.AddrInfo((string)drD["ADRFROM"], xFF.xSm.nSklad);
                    }
                    catch { xA = null; }
                    scD.xOp.SetOperSrc(xA, xFF.xCDoc.xDocP.DType);


                    //s = (string)((drD["ADRTO"] == System.DBNull.Value) ? "" : drD["ADRTO"]);
                    //scD.xOp.xAdrDst = new MainF.AddrInfo(s, AdrName(s), xFF);
                    try
                    {
                        xA = new MainF.AddrInfo((string)drD["ADRTO"], xFF.xSm.nSklad);
                    }
                    catch { xA = null; }
                    scD.xOp.SetOperDst(xA, xFF.xCDoc.xDocP.DType);

                    if (drD.Table == DT[NSI.BD_DOUTD].dt)
                    {// ������ ��� ���, ��� ������ �� ������
                        scD.nTara = (int)((drD["KRKT"] == System.DBNull.Value) ? AppC.EMPTY_INT : drD["KRKT"]);
                        scD.nKolSht = (int)((drD["KOLSH"] == System.DBNull.Value) ? AppC.EMPTY_INT : drD["KOLSH"]);

                        scD.nDest = (NSI.DESTINPROD)((drD["DEST"] == System.DBNull.Value) ? DESTINPROD.USER : drD["DEST"]);
                        scD.nRecSrc = (int)((drD["SRC"] == System.DBNull.Value) ? SRCDET.SCAN : drD["SRC"]);
                        scD.dtScan = (DateTime)((drD["TIMECR"] == System.DBNull.Value) ? DateTime.MinValue : drD["TIMECR"]);
                    }
                    scD.nZaklMT = (drD["NZAKL"] is int) ? ((int)drD["NZAKL"]) : 0;

                    ret = true;
                }
                catch{}
            }
            return (ret);
        }



        public DataRow AddDet(PSC_Types.ScDat s, CurDoc xCDoc, DataRow drOld)
        {
            return (AddDet(s, xCDoc, drOld, true));
        }



        /// ���������� ������� ��������� ������
        //public DataRow AddDet(PSC_Types.ScDat s, CurDoc xCDoc, DataRow drOld, bool bAddNew)
        //{
        //    DateTime
        //        dtCr;
        //    DataRow 
        //        ret = drOld;
        //    int 
        //        nPodz = 0,
        //        nKey = (int)xCDoc.drCurRow["SYSN"];

        //    if (drOld == null)
        //    {
        //        try
        //        {
        //            ret = DT[NSI.BD_DOUTD].dt.NewRow();

        //            ret["KRKMC"] = (s.nKrKMC == AppC.EMPTY_INT)?0:s.nKrKMC;
        //            ret["SNM"] = s.sN;
        //            ret["KOLM"] = s.nMest;
        //            ret["KOLE"] = s.fVsego;
        //            ret["KOLP"] = s.nPalet;

        //            ret["EMK"] = s.fEmk;
        //            ret["NP"] = s.nParty;

        //            ret["DVR"] = s.dDataIzg.ToString("yyyyMMdd");
        //            ret["DTG"] = s.dDataGodn.ToString("yyyyMMdd");

        //            ret["EAN13"] = s.sEAN;
        //            ret["ITF14"] = s.sGTIN;

        //            ret["SYSN"] = nKey;
        //            ret["SRP"] = (s.bVes == true) ? 1 : 0;
        //            //ret["GKMC"] = sSig.sGrK;

        //            if (s.nTara == AppC.EMPTY_INT)
        //                ret["KRKT"] = System.DBNull.Value;
        //            else
        //                ret["KRKT"] = s.nTara;

        //            if (s.nKolSht == AppC.EMPTY_INT)
        //                ret["KOLSH"] = System.DBNull.Value;
        //            else
        //                ret["KOLSH"] = s.nKolSht;

        //            ret["VES"] = s.fVes;
        //            ret["DEST"] = s.nDest;
        //            ret["KMC"] = s.sKMC;

        //            ret["SRC"] = s.nRecSrc;

        //            dtCr = s.dtScan;
        //            //if (xCDoc.nTypOp == AppC.TYPOP_MOVE)
        //            //{
        //            //    if (xCDoc.xOper.IsFillSrc())
        //            //        dtCr = xCDoc.xOper.xAdrSrc.dtScan;
        //            //}
        //            ret["TIMECR"] = dtCr;

        //            ret["NPODD"] = s.nNomPodd;
        //            ret["NMESTA"] = s.nNomMesta;

        //            s.xOp = xCDoc.xOper;
        //            ret["ADRFROM"] = s.xOp.GetSrc(false);
        //            ret["ADRTO"] = s.xOp.GetDst(false);

        //            // ������� ����� ������
        //            ret["BARCODE"] = s.s;
        //            ret["TARTYPE"] = 
        //                (s.tTyp == AppC.TYP_TARA.TARA_POTREB)?"�":
        //                (s.tTyp == AppC.TYP_TARA.TARA_PODDON)?"�":"�";
        //            ret["SSCC"] = xCDoc.sSSCC;

        //            try
        //            {
        //                nPodz = xCDoc.xNPs.Current;
        //            }
        //            catch
        //            {
        //                nPodz = 0;
        //            }
        //            ret["NPODDZ"] = nPodz;

        //            if (bAddNew)
        //            {
        //                DT[NSI.BD_DOUTD].dt.Rows.Add(ret);
        //            }
        //        }
        //        catch
        //        {
        //            Srv.ErrorMsg("������ ���������� ���������!");
        //        }
        //    }
        //    else
        //    {
        //        drOld["KOLP"] = (int)drOld["KOLP"] + s.nPalet;
        //        drOld["KOLM"] = (int)drOld["KOLM"] + s.nMest;
        //        drOld["KOLE"] = (FRACT)drOld["KOLE"] + s.fVsego;
        //        drOld["VES"] = (FRACT)drOld["VES"] + s.fVes;

        //        drOld["NPODD"] = s.nNomPodd;
        //        drOld["NMESTA"] = s.nNomMesta;
        //    }
        //    return(ret);
        //}


        /// ���������� ������� ��������� ������
        public DataRow AddDet(PSC_Types.ScDat s, CurDoc xCDoc, DataRow drOld, bool bAddNew)
        {
            DataRow
                ret = drOld;
            int
                nPodz = 0,
                nKey = (int)xCDoc.drCurRow["SYSN"];

            if ((drOld is DataRow) && (!s.bReWrite))
            {// ������ ��������������
                drOld["KOLP"] = (int)drOld["KOLP"] + s.nPalet;
                drOld["KOLM"] = (int)drOld["KOLM"] + s.nMest;
                drOld["KOLE"] = (FRACT)drOld["KOLE"] + s.fVsego;
                drOld["VES"] = (FRACT)drOld["VES"] + s.fVes;
                drOld["KOLSH"] = ((drOld["KOLSH"] is int) ? (int)drOld["KOLSH"] : 0) + s.nKolSht;

                drOld["NPODD"] = s.nNomPodd;
                drOld["NMESTA"] = s.nNomMesta;
            }
            else
            {// ������� ����� ��� ������������
                if (drOld == null)
                    ret = DT[NSI.BD_DOUTD].dt.NewRow();

                try
                {
                    ret["KRKMC"] = s.nKrKMC;
                    ret["SNM"] = s.sN;
                    ret["KOLM"] = s.nMest;
                    ret["KOLE"] = s.fVsego;
                    ret["KOLP"] = s.nPalet;

                    ret["EMK"] = s.fEmk;
                    ret["NP"] = s.nParty;

                    ret["DVR"] = s.dDataIzg.ToString("yyyyMMdd");
                    ret["DTG"] = s.dDataGodn.ToString("yyyyMMdd");

                    ret["EAN13"] = s.sEAN;
                    ret["ITF14"] = s.sGTIN;

                    ret["SYSN"] = nKey;
                    ret["SRP"] = (s.bVes == true) ? 1 : 0;

                    ret["VES"] = s.fVes;
                    ret["DEST"] = s.nDest;
                    ret["KMC"] = s.sKMC;
                    ret["KOLSH"] = s.nKolSht;
                    ret["NPODD"] = s.nNomPodd;
                    ret["NMESTA"] = s.nNomMesta;

                    ret["TIMECR"] = s.dtScan;
                    ret["SRC"] = s.nRecSrc;

                    if (s.nTara == AppC.EMPTY_INT)
                        ret["KRKT"] = System.DBNull.Value;
                    else
                        ret["KRKT"] = s.nTara;

                    s.xOp = xCDoc.xOper;
                    ret["ADRFROM"] = s.xOp.GetSrc(false);
                    ret["ADRTO"] = s.xOp.GetDst(false);

                    // ������� ����� ������
                    ret["BARCODE"] = s.s;
                    ret["TARTYPE"] =
                        (s.tTyp == AppC.TYP_TARA.TARA_POTREB) ? "�" :
                        (s.tTyp == AppC.TYP_TARA.TARA_PODDON) ? "�" : "�";
                    //ret["SSCC"] = xCDoc.sSSCC;
                    ret["SSCC"] = s.sSSCC;

                    try
                    {
                        nPodz = xCDoc.xNPs.Current;
                    }
                    catch
                    {
                        nPodz = 0;
                    }
                    ret["NPODDZ"] = nPodz;
                    ret["NZAKL"] = s.nZaklMT;

                    if ((bAddNew) && (!s.bReWrite))
                    {
                        DT[NSI.BD_DOUTD].dt.Rows.Add(ret);
                    }
                }
                catch
                {
                    Srv.ErrorMsg("������ ������ ���������!");
                }
            }
            return (ret);
        }



        // ���������� ������� ������� ��������� ������
        public int AddrUpdate(DataRow dr, MainF.AddrInfo xAdrSrc, MainF.AddrInfo xAdrDst)
        {
            int
                nUpd = 0;

                try
                {
                    if (xAdrSrc is MainF.AddrInfo)
                    {
                        nUpd++;
                        dr["ADRFROM"] = xAdrSrc.Addr;
                        dr["TIMEOV"] = DateTime.Now;
                    }
                    if (xAdrDst is MainF.AddrInfo)
                    {
                        nUpd++;
                        dr["ADRTO"] = xAdrDst.Addr;
                        dr["TIMEOV"] = DateTime.Now;
                    }
                }
                catch
                {
                }
            return (nUpd);
        }


        // ���������� DataSet ��� ��������
        public DataSet MakeWorkDataSet(DataTable dtM, DataTable dtD, DataRow[] drA, DataRow[] drDetReady, 
            Smena xSm, CurUpLoad xCU)
        {

            DataTable dtMastNew = dtM.Clone();
            DataTable dtDetNew = dtD.Clone();
            DataTable dtBNew = DT[BD_SPMC].dt.Clone();
            DataRow[] aDR, childRows;
            bool bNeedRow;
            string sS;

            DataRelation myRelation = dtM.ChildRelations[REL2TTN];


            foreach (DataRow dr in drA)
            {
                dtMastNew.LoadDataRow(dr.ItemArray, true);
                // ��� SSCC ��������� ������ ���������
                if ((xCU.sCurUplCommand == AppC.COM_ZSC2LST) ||
                     (xCU.sCurUplCommand == AppC.COM_ADR2CNT) )
                    break;

                if (drDetReady == null)
                {// ������ ��������� ����� ��� �� �����
                    if (xCU.bOnlyCurRow)
                        // �������������� �������� ����� ������ �� ��������� ��������
                        childRows = new DataRow[] { xCU.drForUpl };
                    else
                        childRows = dr.GetChildRows(myRelation);
                }
                else
                    childRows = drDetReady;

                dtDetNew.BeginLoadData();
                foreach (DataRow chRow in childRows)
                {
                    try
                    {
                        bNeedRow = true;
                        #region ������������� ��������� ��������� ������
                        do
                        {
                            if (drDetReady != null)
                                // ��� �������������� ������ ������ � ��������
                                break;

                            if ((int)dr["TYPOP"] != AppC.TYPOP_DOCUM)
                            {// ��� ������������� ������ ����� ���� ��������...
                                if ((AppC.OPR_STATE)chRow["STATE"] == AppC.OPR_STATE.OPR_TRANSFERED)
                                {// �������� ��� �����������
                                    bNeedRow = false;
                                }
                                else
                                {
                                    if ((int)dr["TYPOP"] == AppC.TYPOP_MOVE)
                                    {
                                        sS = (chRow["ADRFROM"] == System.DBNull.Value) ? "" : (string)chRow["ADRFROM"];
                                        if ((sS.Length > 0) && (xCU.sCurUplCommand != AppC.COM_CKCELL))
                                        {
                                            sS = (chRow["ADRTO"] == System.DBNull.Value) ? "" : (string)chRow["ADRTO"];
                                        }
                                        if (sS.Length == 0)
                                            bNeedRow = false;
                                    }
                                    else
                                    {
                                        sS = (chRow["SSCC"] == System.DBNull.Value) ? "" : "1";
                                        sS += (chRow["SSCCINT"] == System.DBNull.Value) ? "" : "2";
                                        if (sS.Length == 0)
                                        {// ����������������� ��������� 
                                            if (((int)dr["TYPOP"] == AppC.TYPOP_MARK) ||
                                                ((int)dr["TYPOP"] == AppC.TYPOP_KMPL))
                                                bNeedRow = false;
                                        }
                                    }
                                }
                            }


                        } while (false);
                        #endregion
                    }
                    catch
                    {
                        bNeedRow = false;
                    }

                    if (bNeedRow)
                    {
                        dtDetNew.LoadDataRow(chRow.ItemArray, true);

                        aDR = chRow.GetChildRows(REL2BRK);
                        foreach (DataRow bR in aDR)
                        {
                            //r = dtBNew.NewRow();
                            //r.ItemArray = bR.ItemArray;
                            //dtBNew.Rows.Add(r);

                            dtBNew.LoadDataRow(bR.ItemArray, true);
                        }
                    }
                }
                dtDetNew.EndLoadData();
            }

            DataSet ds1Rec = new DataSet("dsMOne");
            ds1Rec.Tables.Add(dtMastNew);
            ds1Rec.Tables.Add(dtDetNew);
            ds1Rec.Tables.Add(dtBNew);
            return (ds1Rec);
        }


        // ������� ��� ������
        public DataSet MakeDataSetForLoad(DataTable dtM, DataTable dtD)
        {

            DataTable
                dtMastNew = dtM.Clone(),
                dtDetNew = dtD.Clone(),
                dtAdrNew = DT[BD_ADRKMC].dt.Clone();

            dtMastNew.TableName = BD_ZDOC;
            dtDetNew.TableName = BD_ZDET;
            dtAdrNew.TableName = BD_ADRKMC;

            DataSet
                dsWZvk = new DataSet("dsZ");
            dsWZvk.Tables.Add(dtMastNew);
            dsWZvk.Tables.Add(dtDetNew);
            dsWZvk.Tables.Add(dtAdrNew);

            DataColumn
                dcDocHeader = dtMastNew.Columns["SYSN"],
                dcDet = dtDetNew.Columns["SYSN"];

            dsWZvk.Relations.Add(REL2ZVK, dcDocHeader, dcDet);
            dsWZvk.Relations.Add(REL2ADR,
                new DataColumn[]{
                dtDetNew.Columns["SYSN"],dtDetNew.Columns["IDX"]},
                new DataColumn[]{
                dtAdrNew.Columns["SYSN"], dtAdrNew.Columns["IDX"]}
                );

            // �������� ��������� ������ ���
            foreach (DataTable dt in dsNSI.Tables)
            {
                dsWZvk.Tables.Add(dt.Clone());
            }

            return (dsWZvk);
        }

        //
        private void ProceedOneRowMC(DataRow dr)
        {
            if ((dr["KMT"] is string) && !((dr["EAN13"] is string) && (((string)(dr["EAN13"])).Length > 0)))
                dr["EAN13"] = dr["KMT"];

            //if (!((dr["EAN13"] is string) && (((string)(dr["EAN13"])).Length == 13)))
            //{// ���� EAN13 �����-�� ������
            //    if ((dr["KMT"] is string) && (((string)(dr["KMT"])).Length > 0))
            //    {
            //        string s = (string)dr["KMT"];
            //        dr["EAN13"] = Srv.CheckSumModul10( "20" + s.PadLeft(10, '0') );
            //    }
            //}


            if (!(dr["SRP"] is int))
                dr["SRP"] = 0;
        }

        // ����� ������������� ����� �� ������
        public int AfterLoadNSI(string sTName, bool bFromSrv, string sFileFromSrv)
        {
            int
                nKr = 0,
                nRet = AppC.RC_OK;

            switch (sTName)
            {
                case NSI.NS_MC:
                    if (bFromSrv)
                    {
                        DT[NS_MC].dt.BeginLoadData();
                        try
                        {
                            foreach (DataRow dr in DT[NS_MC].dt.Rows)
                            {
                                ProceedOneRowMC(dr);
                            }
                        }
                        catch
                        {
                            nKr = 28;
                        }
                        finally
                        {
                            DT[NS_MC].dt.EndLoadData();
                        }
                    }
                    break;
                case NSI.NS_PRPR:
                    //TestPrBrak();
                    if (bFromSrv)
                    {
                        try
                        {
                            DataRow[] xMax = DT[NSI.NS_PRPR].dt.Select("KRK=MAX(KRK)");
                            if (xMax.Length > 0)
                                nKr = (int)xMax[0]["KRK"];
                        }
                        catch { }

                        foreach (DataRow drp in DT[NSI.NS_PRPR].dt.Rows)
                        {
                            if ((drp["KRK"] == DBNull.Value) || (((int)drp["KRK"] <= 0)))
                                drp["KRK"] = ++nKr;
                        }
                        if (sFileFromSrv.Length > 0)
                        {
                            DT[sTName].dt.WriteXml(sPathNSI + DT[sTName].sXML);
                            File.Delete(sFileFromSrv);
                            nRet = AppC.RC_BADTABLE;
                        }
                    }
                    break;
                case NSI.BD_PASPORT:
                    Srv.LoadInterCode(out xFF.xGExpr, xFF.xExpDic, DT[NSI.BD_PASPORT]);
                    //MainF.AddrInfo.NameFuncPresent = (xFF.xGExpr.run.FindFunc("NameAdr") is ExprDll.Action) ? true : false;
                    MainF.AddrInfo.xR = (xFF.xGExpr.run.FindFunc(AppC.FEXT_ADR_NAME) is ExprDll.Action) ? xFF.xGExpr.run : null;
                    break;
            }
            MainF.AddrInfo.dtA = DT[NSI.NS_ADR].dt;

            return (nRet);
        }


        // ���������� ��� �� ������
        public void AddNewNSI(DataSet ds)
        {
            int
                iAdd = 0;
            string
                i = "",
                sP;

            object[]
                aIt;

            DataRow drF;

            foreach (DataTable dt in ds.Tables)
            {
                if (dt.Rows.Count > 0)
                {
                    i = "";
                    switch (dt.TableName)
                    {
                        case NS_MC:
                            // ���������� ���������� ����������
                            i = NS_MC;

                            foreach (DataRow dr in dt.Rows)
                            {
                                ProceedOneRowMC(dr);
                                aIt = dr.ItemArray;
                                drF = DT[NS_MC].dt.Rows.Find(new object[] { dr["KMC"] });
                                if (null == drF)
                                {// � ����������� ������ �� ����
                                    iAdd++;
                                    drF = DT[NS_MC].dt.NewRow();
                                    DT[NS_MC].dt.Rows.Add(drF);
                                }
                                drF.ItemArray = aIt;
                                ProceedOneRowMC(drF);
                            }
                            break;
                        case NS_USER:
                            i = NS_USER;
                            break;
                        case NS_PP:
                            i = NS_PP;
                            break;
                    }
                    if (iAdd > 0)
                    {
                        DT[i].nAdded += iAdd;
                        //sP = sPathNSI + DT[i].sXML;
                        //DT[i].dt.WriteXml(sP);
                    }
                }
            }
        }



        private string sP_CSDat = "CSDat.xml";
        //public int DSRestore(string sDS, DateTime dCur, int nMaxD, bool bControlDate)
        //{
        //    int i = 0,
        //        nRet = AppC.RC_OK;
        //    DateTime dD;
        //    TimeSpan tsD;
        //    try
        //    {
        //        dsM.BeginInit();
        //        dsM.EnforceConstraints = false;
        //        dsM.Clear();
        //        try
        //        {
        //            dsM.ReadXml(sDS + sP_CSDat);
        //            if (bControlDate)
        //            {
        //                if (nMaxD > 0)
        //                {
        //                    while (i < DT[NSI.BD_DOCOUT].dt.Rows.Count)
        //                    {
        //                        dD = DateTime.ParseExact((string)(DT[NSI.BD_DOCOUT].dt.Rows[i]["DT"]), "yyyyMMdd", null);
        //                        tsD = dCur.Subtract(dD);
        //                        if (tsD.Days > nMaxD)
        //                            DT[NSI.BD_DOCOUT].dt.Rows.RemoveAt(i);
        //                        else
        //                            i++;
        //                    }
        //                }
        //            }
        //        }
        //        catch
        //        {// ��, ������, �� ���� 
        //            nRet = AppC.RC_CANCEL;
        //        }

        //        dsM.EnforceConstraints = true;
        //        dsM.EndInit();
        //    }
        //    catch
        //    {
        //        nRet = AppC.RC_NOFILE;
        //    }

        //    return (nRet);
        //}

        /// ������������ ����������� ������
        public int DSRestore(string sDS, DateTime dCur, int nMaxD, bool bControlDate)
        {
            int i = 0,
                nRet = AppC.RC_OK;
            DateTime
                dD;
            TimeSpan
                tsD;
            DataSet
                dsTMP;

            dsM.AcceptChanges();
            dsTMP = dsM.Copy();
            try
            {
                dsM.BeginInit();
                dsM.EnforceConstraints = false;
                dsM.Clear();
                try
                {
                    dsM.ReadXml(sDS + sP_CSDat);
                    if (bControlDate)
                    {
                        if (nMaxD > 0)
                        {
                            while (i < DT[NSI.BD_DOCOUT].dt.Rows.Count)
                            {
                                dD = DateTime.ParseExact((string)(DT[NSI.BD_DOCOUT].dt.Rows[i]["DT"]), "yyyyMMdd", null);
                                tsD = dCur.Subtract(dD);
                                if (tsD.Days > nMaxD)
                                    DT[NSI.BD_DOCOUT].dt.Rows.RemoveAt(i);
                                else
                                    i++;
                            }
                        }
                    }
                }
                catch
                {// ��, ������, �� ���� 
                    nRet = AppC.RC_CANCEL;
                }

                dsM.EnforceConstraints = true;
                dsM.EndInit();
            }
            catch
            {
                nRet = AppC.RC_NOFILE;
            }
            if (nRet != AppC.RC_OK)
            {
                dsM.BeginInit();
                dsM.EnforceConstraints = false;
                dsM.Clear();
                dsM.Merge(dsTMP);
                dsM.EnforceConstraints = true;
                dsM.EndInit();
            }
            return (nRet);
        }

        public int DSSave(string sF)
        {
            int ret = AppC.RC_OK;

            try
            {
                dsM.WriteXml(sF + sP_CSDat);
                foreach (DataTable dt in dsNSI.Tables)
                {
                    if (DT[dt.TableName].nAdded > 0)
                    {
                        dt.WriteXml( sPathNSI + DT[dt.TableName].sXML );
                    }
                }

            }
            catch
            {
                ret = AppC.RC_CANCEL;
            }

            return (ret);
        }


    }
}
