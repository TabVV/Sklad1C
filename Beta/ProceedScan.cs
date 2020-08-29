using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Data;

using ScannerAll;
using PDA.OS;
using PDA.BarCode;
using PDA.Service;

using FRACT = System.Decimal;

namespace SkladRM
{
    public class ScanVarRM : ScanVar
    {
        // флаги результата анализа штрихкода
        [Flags]
        public enum BCTyp : int
        {
            UNKNOWN = 0,                // отсканировали
            SP_OLD_ETIK = 1,                // скопировано из заявки
            SP_NEW_ETIK = 2,                // скопировано из заявки
            SP_SSCC = 4,                // внешнний SSCC
            SP_SSCC_INT = 8,                // внутренний SSCC

            //SP_ADR_OBJ      = 16,               // адрес (объект)
            //SP_ADR_STLG     = 32,               // адрес (стеллажи для хранения)
            //SP_ADR_ZONE     = 64,               // адрес (зона)

            RM_ADR_OBJ = 32,               // адрес (объект)
            //RM_ADR_STLG     = 32,               // адрес (стеллажи для хранения)
            VS_ADR_OBJ = 64,               // Высокое - адрес

            SNT_GTIN_OLD = 128,              // Санта-старая
            SNT_GTIN_NEW = 256,              // Санта-новая
            CSDR_GTIN = 512,              // Касандра-товар
            CSDR_DOC = 1024,             // Касандра-документ
            NG_BOX = 2048,             // Ногинск новый штрихкод (С128 - длина 39)
            SP_MT_PRDVN = 4096,             // предъявление для материалов (новое)

            SP_SSCC_PRT = 8192             // внешнний SSCC партии
        }
        private string
            //SNT_GLN = "4810168",
            SP_GLN = "4810268";
        private BarcodeScannerEventArgs
            m_SavedArgs;

        //private DataTable dtAI;

        //public bool TFullBC(ScanVar x)
        //{
        //    bool ret = false;
        //    if (x.dicSc.ContainsKey("02") &&
        //        x.dicSc.ContainsKey("11"))
        //        ret = true;
        //    x.FullData = ret;
        //    return (ret);
        //}


        public ScanVarRM(BarcodeScannerEventArgs e) : this(e, null) { }

        public ScanVarRM(BarcodeScannerEventArgs e, DataTable t)
            : base(t)
        {
            Id = e.nID;
            Dat = e.Data;
            m_SavedArgs = e;
            //dtAI = t;
            WhatBC();
        }

        public BCId Id;
        public string Dat;

        public BCTyp bcFlags = BCTyp.UNKNOWN;

        public bool bGoodParse = false;
        public bool bSPOldEtik = false;

        public DateTime ScanDTime
        {
            get { return m_SavedArgs.ScanDTime; }
        }

        public void WhatBC()
        {
            int
                nBCLen = Dat.Length,
                nAI = 0;
            string
                sPart,
                s;

            if ((Id == BCId.Code128) || true)
            {
                try
                {
                    try
                    {
                        base.ScanParse(Dat);
                    }
                    catch (Exception e)
                    {
                        if (e.Message.IndexOf("ИП") == 0)
                            throw new Exception(e.Message, e);
                        if (Dat.StartsWith("99") && (Dat.Length == 12))
                        {
                            base.dicSc.Clear();
                            base.dicSc.Add("99", new OneFieldBC("ADR", Dat.Substring(2), Dat.Substring(2), "C", "ADR"));
                        }
                    }
                    if ((nBCLen == 16) &&
                        ((Dat[3] >= '0') && (Dat[3] <= '9'))
                        )
                    {// Casandra GTIN ???
                        int d1 = int.Parse(Dat.Substring(4, 3));
                        int d2 = int.Parse(Dat.Substring(7, 3));
                        if ((d1 <= 999) && (d2 <= 999))
                        {
                            bcFlags |= BCTyp.CSDR_GTIN;
                            return;
                        }
                    }
                    nAI = base.dicSc.Count;
                    if (nAI > 0)
                    {
                        switch (nAI)
                        {
                            case 1:
                            case 2:

                                if ((nAI == 1) && Dat.StartsWith("9"))
                                {// адресная этикетка ?
                                    if (base.dicSc.ContainsKey("92"))
                                    {// Русское море - адрес
                                        bcFlags = BCTyp.RM_ADR_OBJ;
                                    }
                                    else if (base.dicSc.ContainsKey("93"))
                                    {// Высокое - адрес
                                        bcFlags = BCTyp.VS_ADR_OBJ;
                                    }
                                    else if (base.dicSc.ContainsKey("959"))
                                    {// SSCC_PARTY
                                        bcFlags = BCTyp.SP_SSCC_PRT;
                                    }
                                    //bcFlags = BCTyp.SP_ADR_OBJ;
                                    break;
                                }

                                if (Dat.Length == 38)
                                {// очень похоже на старый код
                                    if (Dat.StartsWith("02"))
                                    {
                                        s = Dat.Substring(16);
                                        if ((s.Length == 22) &&
                                            (s.Substring(6, 2) == "11") &&
                                            (s.Substring(14, 2) == "37"))
                                            bcFlags |= BCTyp.SP_OLD_ETIK;
                                    }
                                }
                                else
                                {
                                    if ((Dat.Length == 20) && (nAI == 1) && (base.dicSc.ContainsKey("00")))
                                    {
                                        if (Dat == Srv.CheckSumModul10(Dat.Substring(0, 19)))
                                        {
                                            bcFlags |= BCTyp.SP_SSCC;
                                            if (!Dat.Substring(2).StartsWith("1"))
                                                // внутренний SSCC
                                                bcFlags |= BCTyp.SP_SSCC_INT;
                                        }
                                    }
                                }

                                break;
                            case 3:
                                if ((base.dicSc.ContainsKey("17")) &&
                                    (base.dicSc.ContainsKey("10")))
                                {
                                    if (nBCLen == 44)
                                    {
                                        bcFlags |= BCTyp.SNT_GTIN_OLD;
                                    }
                                    if (nBCLen == 36)
                                    {
                                        bcFlags |= BCTyp.SNT_GTIN_NEW;
                                    }
                                    if ((nBCLen >= 38) && (nBCLen <= 40))
                                    {
                                        bcFlags |= BCTyp.NG_BOX;
                                    }
                                }
                                break;
                            case 5:
                                if ((base.dicSc.ContainsKey("02")) || (base.dicSc.ContainsKey("01")))
                                {
                                    if (Dat.Length > 38)
                                    {
                                        if ((base.dicSc.ContainsKey("11")) &&
                                            (base.dicSc.ContainsKey("23")) &&
                                            (base.dicSc.ContainsKey("37")) &&
                                            (base.dicSc.ContainsKey("10")))
                                        {
                                            bcFlags |= BCTyp.SP_NEW_ETIK;
                                        }
                                    }
                                }
                                break;
                        }

                    }
                    else
                    {
                        sPart = Dat.Substring(0, 2);
                        switch (sPart)
                        {
                            //case "11":
                            //    if (Dat.Length == 38)
                            //    {
                            //        bcFlags |= BCTyp.SP_MT_PRDV;
                            //    }
                            //    break;
                            case "52":
                                if (Dat.Length == 40)
                                {
                                    bcFlags |= BCTyp.SP_MT_PRDVN;
                                }
                                break;
                            case "53":
                                if (Dat.Length == 34)
                                {
                                    bcFlags |= BCTyp.SP_MT_PRDVN;
                                }
                                break;
                        }
                    }

                }
                catch// (Exception e)
                {
                    bGoodParse = false;
                }
            }
        }


        // заполнение таблицы с идентификаторами применения
        protected override DataTable DefaultAI(string sTName)
        {
            DataRow r;
            if (sTName.Length <= 0)
                sTName = "TNS_AI";
            DataTable dt = new DataTable(sTName);

            dt.Columns.AddRange(new DataColumn[]{
                new DataColumn("KAI", typeof(string)),          // Код идентификатора
                new DataColumn("NAME", typeof(string)),         // Наименование
                new DataColumn("TYPE", typeof(string)),         // Тип данных
                new DataColumn("MAXL", typeof(int)),            // Максимальная длина данных
                new DataColumn("VARLEN", typeof(int)),          // Признак переменной длины
                new DataColumn("DECP", typeof(int)),            // Позиция десятичной точки
                new DataColumn("PROP", typeof(string)),         // Поле
                new DataColumn("KED", typeof(string)) });       // Код единицы

            dt.PrimaryKey = new DataColumn[] { dt.Columns["KAI"] };
            dt.Columns["TYPE"].DefaultValue = "N";
            dt.Columns["DECP"].DefaultValue = 0;
            dt.Columns["VARLEN"].DefaultValue = 0;

            r = dt.NewRow();
            r["KAI"] = "00";
            r["NAME"] = "Серийный грузовой контейнерный код";
            r["TYPE"] = "C";
            r["MAXL"] = 18;
            r["PROP"] = "SSCC";
            dt.Rows.Add(r);

            r = dt.NewRow();
            r["KAI"] = "01";
            r["NAME"] = "Идентификационный номер единицы товара";
            r["TYPE"] = "C";
            r["MAXL"] = 14;
            r["PROP"] = "GTIN";
            dt.Rows.Add(r);

            r = dt.NewRow();
            r["KAI"] = "02";
            r["NAME"] = "GTIN торговых единиц, содержащихся в грузе";
            r["TYPE"] = "C";
            r["MAXL"] = 14;
            r["PROP"] = "CONTENT";
            dt.Rows.Add(r);

            dt.LoadDataRow(new object[] { "10", "Номер лота (партии, группы, пакета)", "C", 20, 1, 0, "LOT", "" }, true);
            dt.LoadDataRow(new object[] { "11", "Дата выработки (ГГММДД)", "D", 6, 0, 0, "PRODDATE", "" }, true);
            dt.LoadDataRow(new object[] { "15", "Минимальный срок годности (ГГММДД)", "D", 6, 0, 0, "BESTBEF", "" }, true);
            dt.LoadDataRow(new object[] { "17", "Максимальный срок годности (ГГММДД)", "D", 6, 0, 0, "USEBEF", "" }, true);
            dt.LoadDataRow(new object[] { "20", "Разновидность продукта", "N", 2, 0, 0, "VARIANT", "" }, true);
            dt.LoadDataRow(new object[] { "21", "Серийный номер", "C", 20, 1, 0, "SERIAL", "" }, true);
            dt.LoadDataRow(new object[] { "23", "Номер лота  (переходный)", "N", 19, 1, 0, "LOTOLD", "" }, true);
            dt.LoadDataRow(new object[] { "30", "Переменное количество", "N", 8, 1, 0, "VARCOUNT", "" }, true);
            dt.LoadDataRow(new object[] { "37", "Количество торговых единиц  в грузе", "N", 8, 1, 0, "COUNT", "" }, true);
            dt.LoadDataRow(new object[] { "310", "Вес нетто, кг", "N", 6, 0, 1, "NETKG", "кг" }, true);
            dt.LoadDataRow(new object[] { "330", "Вес брутто, кг", "N", 6, 0, 1, "GROSSKG", "кг" }, true);
            dt.LoadDataRow(new object[] { "92", "Адрес ячейки/зоны на складе", "C", 9, 0, 0, "ADDRRM", "" }, true);
            dt.LoadDataRow(new object[] { "93", "Адрес ячейки/зоны на складе", "C", 6, 0, 0, "ADDRVS", "" }, true);
            dt.LoadDataRow(new object[] { "959", "SSCC партии продукта (на ящик)", "N", 7, 0, 1, "SSCC_PARTY", "" }, true);
            return (dt);
        }

    }

    public partial class MainF : Form
    {
        private ScanVarRM 
            xScan, 
            xScanPrev = null;

        private bool 
            bInScanProceed = false;


        /// обработка сканирования в спецокне
        private void SpecScan(ScanVarRM xSc)
        {
            string
                s;

            switch (nSpecAdrWait)
            {

                case AppC.F_CHKSSCC:
                    // Загрузка SSCC в заявку
                case AppC.F_CNTSSCC:
                    if ((xSc.bcFlags & ScanVarRM.BCTyp.SP_SSCC) > 0)
                    {
                        xCDoc.sSSCC = xSc.Dat;
                        xFPan.UpdateReg(xCDoc.sSSCC);
                        //s = (nSpecAdrWait == AppC.F_CNTSSCC) ? "Enter-на экран, .-добавить строки" : "Enter-загрузить";
                        s = "Enter-на экран, .-добавить строки";
                        xFPan.UpdateHelp(s);
                    }
                    break;
                case AppC.F_GENSCAN:
                    xFPan.UpdateReg(xSc.Dat);
                    xFPan.UpdateHelp(String.Format("Тип-{0} длина={1} AI={2}", xSc.Id.ToString(), xSc.Dat.Length, xSc.dicSc.Count));
                    break;
                case AppC.F_SETADRZONE:
                    // функция фиксации адреса
                    if ((xSc.bcFlags & ScanVarRM.BCTyp.RM_ADR_OBJ) > 0)
                    {// Адрес зоны или объекта
                        xSm.xAdrForSpec = new AddrInfo(xSc, xSm.nSklad);
                        xFPan.UpdateReg(String.Format("{0:20}...", xSm.xAdrForSpec.AddrShow));
                        xFPan.UpdateHelp("Enter - зафиксировать адрес");
                    }
                    break;
                case AppC.F_CELLINF:
                    // функция получения содержимого адреса
                    if ((xSc.bcFlags & ScanVarRM.BCTyp.RM_ADR_OBJ) > 0)
                    {// Адрес зоны или объекта
                        xSm.xAdrForSpec = new AddrInfo(xSc, xSm.nSklad);
                        xFPan.UpdateReg(xSm.xAdrForSpec.AddrShow);
                        s = (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM) ?
                            "Enter-на экран, F3-добавить строки" :
                            "Enter-на экран";
                        xFPan.UpdateHelp(s);
                    }
                    break;
                case AppC.F_CLRCELL:
                    // очистка содержимого ячейки
                    if ((xSc.bcFlags & ScanVarRM.BCTyp.RM_ADR_OBJ) > 0)
                    {// Адрес зоны или объекта
                        xSm.xAdrForSpec = new AddrInfo(xSc, xSm.nSklad);
                        xFPan.UpdateReg(xSm.xAdrForSpec.AddrShow);
                        xFPan.UpdateHelp("Enter - очистить адрес   ESC - выход");
                    }
                    break;
            }
        }



        // обработка выполненного сканирования
        private void OnScan(object sender, BarcodeScannerEventArgs e)
        {
            bool 
                bRet = AppC.RC_CANCELB,
                bDupScan;
            int 
                nRet = AppC.RC_CANCEL;
            string 
                sErr = "";

            // началась обработка сканирования
            bInScanProceed = true;
            if (e.nID != BCId.NoData)
            {
                try
                {
                    xScan = new ScanVarRM(e, xNSI.DT["NS_AI"].dt);
                    PSC_Types.ScDat sc = new PSC_Types.ScDat(e, xScan);
                    bDupScan = ((xScanPrev != null) && (xScanPrev.Dat == xScan.Dat)) ? true : false;

                    sc.sN = e.Data + "-???";

                    #region Обработка скана
                    do
                    {
                        if (nSpecAdrWait > 0)
                        {
                            SpecScan(xScan);
                            break;
                        }

                        switch (tcMain.SelectedIndex)
                        {
                            case PG_DOC:
                                ProceedScanDoc(xScan, ref sc);
                                nRet = AppC.RC_OK;
                                break;
                            case PG_SCAN:
                                if (bDupScan)
                                {// подтверждение операции иногда допустимо
                                    //if (xCDoc.nTypOp == AppC.TYPOP_PRMK)
                                    //{
                                    //    SetOverOPR(true);
                                    //    xScan = null;
                                    //    break;
                                    //}
                                }

                                if (
                                    ((xScan.bcFlags & ScanVarRM.BCTyp.RM_ADR_OBJ) > 0) ||
                                    ((xScan.bcFlags & ScanVarRM.BCTyp.VS_ADR_OBJ) > 0) ||
                                    ((xScan.bcFlags & ScanVarRM.BCTyp.SP_SSCC) > 0)
                                   )
                                {// обработка адреса
                                    nRet = ProceedAdrNew(xScan, ref sc);

                                    if (nRet != AppC.RC_CONTINUE)
                                        break;
                                    bRet = true;
                                }
                                if ((xScan.bcFlags & ScanVarRM.BCTyp.SP_SSCC) > 0)
                                {
                                    int nRetSSCC = ProceedSSCC(xScan, ref sc);
                                    if (nRetSSCC == AppC.RC_WARN)
                                    {
                                        bRet = true;
                                    }
                                    else
                                    {
                                        //ChkOPR(true);
                                        xScan = null;
                                        break;
                                    }
                                }
                                else
                                {// обработка НЕ-Адреса и НЕ-SSCC
                                        if (((xScan.bcFlags & ScanVarRM.BCTyp.CSDR_GTIN) == ScanVarRM.BCTyp.CSDR_GTIN))
                                            bRet = CasandraGTIN(ref sc);
                                        else if (
                                            (xScan.Dat.Length >= 13) &&
                                            (xScan.Dat.Length <= 14))
                                            bRet = EANGTIN(ref sc, xScan.Dat);
                                        else
                                        {
                                            if ((xScan.bcFlags & ScanVarRM.BCTyp.SP_OLD_ETIK) == ScanVarRM.BCTyp.SP_OLD_ETIK)
                                            {// старая этикетка Савушкин
                                                bRet = TranslSCode(ref sc, ref sErr);
                                            }
                                            else if ((xScan.bcFlags & ScanVarRM.BCTyp.SP_MT_PRDVN) > 0)
                                                bRet = TranslMTNew(ref sc);

                                            else
                                            {// новая этикетка
                                                // попытка разбора по стандартным AI
                                                bRet = NewTranslSCode(ref sc);
                                            }
                                        }
                                }

                                sc.nPrzvFil = GetKSKFromBC("FKsk", e.Data, e.nID.ToString());

                                if (!bRet)
                                {
                                    if (sErr.Length == 0)
                                        sErr = (!sc.bFindNSI) ? "Код не найден!" : "Неизвестный штрихкод";
                                    sErr += String.Format("\nGTIN14={0}\nGTIN13={1}", sc.sGTIN, sc.sEAN);
                                    throw new Exception(sErr);
                                }

                                if (xPars.WarnNewScan == true)
                                {// завершение ввода с помощью сканирования
                                    if (bEditMode == true)
                                    {
                                        Control xCc = aEdVvod.Current;
                                        aEdVvod.Fict4Next.Focus();
                                        aEdVvod.SetCur(xCc);

                                        AppC.VerRet vRet = VerifyVvod();
                                        if (vRet.nRet == AppC.RC_OK)
                                        {
                                            EditEndDet(AppC.CC_NEXTOVER);
                                        }
                                        else
                                        {
                                            Srv.ErrorMsg("Не все данные!", true);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                        if (bEditMode == true)
                                    {
                                        Srv.ErrorMsg("Закончите ввод!", true);
                                        break;
                                    }
                                }

                                if ( IsEasyEdit() )
                                {// для режима упрощенного ввода
                                    if ((bDupScan) && (bInEasyEditWait == true))
                                    {
                                        ZVKeyDown(AppC.F_ZVK2TTN, null, ref ehCurrFunc);
                                        break;
                                    }
                                    ZVKeyDown(AppC.F_OVERREG, null, ref ehCurrFunc);
                                }
                                sc.fEmk_s = sc.fEmk;
                                SetDTG(ref sc);
                                nRet = ProceedProd(xScan, ref sc, bDupScan);

                                if ((sc.nRecSrc == (int)NSI.SRCDET.FROMADR)
                                    && (!IsEasyEdit()))
                                    ShowOperState(xCDoc.xOper, sc.nMest);

                                //if (ChkOPR(true) != AppC.RC_OK)
                                //{
                                //    break;
                                //}
                                break;
                        }
                        xScanPrev = xScan;
                    } while (false);
                    #endregion
                }
                catch (Exception ex)
                {
                    string sE = String.Format("{0}({1}){2}", xScan.Id.ToString(), xScan.Dat.Length, xScan.Dat);
                    if (tcMain.SelectedIndex == PG_SCAN)
                        tNameSc.Text = sE;

                    WriteProt(ex.Message + "\n" + sE);

                    Srv.ErrorMsg(sE + "\n" + ex.Message, "Ошибка сканирования", true);
                }
            }
            // обработка сканирования окончена
            bInScanProceed = false;
            //ResetTimerReLogon(true);
        }

        private void WriteProt(string s)
        {
            if (swProt != null)
            {
                swProt.WriteLine(s);
            }
        }


        // старый формат или весовой
        public bool TranslSCode(ref PSC_Types.ScDat s, ref string sErr)
        {
            bool
                bFind = false,     // связь со справочником MC не установлена (пока)
                ret = true;
            int
                n;
            string
                sIdPrim,
                sP,
                sVsego = "",
                sS = s.s;

            try
            {
                //if (s.ci != ScannerAll.BCId.EAN13)
                if (sS.Length > 14)
                {
                    while (sS.Length > 0)
                    {
                        sIdPrim = sS.Substring(0, 2);
                        sS = sS.Substring(2);
                        switch (sIdPrim)
                        {
                            case "01":                          // глобальный номер товара
                            case "02":
                                s.sEAN = Srv.CheckSumModul10(sS.Substring(1, 12));
                                s.sGTIN = sS.Substring(0, 14);
                                sS = sS.Substring(14);
                                break;
                            case "10":                          // номер партии
                                s.nParty = int.Parse(sS.Substring(0, 4)).ToString();
                                sS = sS.Substring(4);
                                break;
                            case "11":                          // дата изготовления (ГГММДД)
                                sP = sS.Substring(0, 6);
                                s.dDataIzg = DateTime.ParseExact(sP, "yyMMdd", null);
                                s.sDataIzg = s.dDataIzg.ToString("dd.MM.yy");
                                sS = sS.Substring(6);
                                break;
                            case "30":                          // количество мест на поддоне
                                s.nMestPal = int.Parse(sS.Substring(0, 4));
                                //s.nTypVes = AppC.TYP_PALET;
                                s.tTyp = AppC.TYP_TARA.TARA_PODDON;
                                sS = sS.Substring(4);
                                break;
                            case "37":                          // количество изделий
                                sVsego = sS.Substring(0, 6);
                                sS = sS.Substring(6);
                                break;
                            default:
                                sS = "";
                                ret = false;
                                break;
                        }
                    }
                    if (ret)
                    {
                        //bFind = false;
                        //if (!s.sGTIN.StartsWith("0"))
                        //{// GTIN точно задан
                        //    bFind = SetKMCOnGTIN_N(ref s, s.sGTIN);
                        //}
                        //if (!bFind)
                        //{
                        //    bFind = xNSI.GetMCDataOnEAN(s.sEAN, ref s, true);
                        //}

                        bFind = SetKMCOnGTIN(ref s);
                        if (!bFind)
                            bFind = xNSI.Connect2MC(s.sEAN, 0, s.nPrzvFil, ref s);

                        if (s.bVes == true)
                            s.fVes = FRACT.Parse(sVsego) / 1000;
                        else
                        {// штучный товар, - емкость из штрихкода
                            n = int.Parse(sVsego.Substring(2, 4));
                            if (s.xEmks.Count == 0)
                            {
                                //siTmp[i] = new StrAndInt(i.ToString(),
                                //    draE[i]["EMKPOD"],
                                //    draE[i]["KT"],
                                //    draE[i]["ITF14"],
                                //    draE[i]["KRK"],
                                //    draE[i]["PR"]);
                                //siTmp[i].DecDat = (draE[i]["EMK"] is FRACT) ? (FRACT)draE[i]["EMK"] : 0;
                                sErr = "Нет емкостей!";
                                throw new Exception();
                            }
                            if ((int)((StrAndInt)s.xEmks.Current).DecDat != n)
                            {
                                bFind = false;
                                for (int j = 0; j < s.xEmks.Count; j++)
                                {
                                    s.xEmks.CurrIndex = j;
                                    if (((StrAndInt)s.xEmks.Current).DecDat == n)
                                    {
                                        bFind = true;
                                        break;
                                    }
                                }
                                if (!bFind)
                                {
                                    s.tTyp = AppC.TYP_TARA.TARA_TRANSP;
                                    CompareEmk(ref s, n);
                                }
                                else
                                {
                                    s.fEmk_s = s.fEmk = n;
                                    s.nKolSht = ((StrAndInt)s.xEmks.Current).IntCodeAdd1;
                                    s.nMestPal = ((StrAndInt)s.xEmks.Current).IntCode;
                                }
                            }
                            else
                            {
                                s.fEmk_s = s.fEmk = n;
                                s.nKolSht = ((StrAndInt)s.xEmks.Current).IntCodeAdd1;
                                s.nMestPal = ((StrAndInt)s.xEmks.Current).IntCode;
                            }
                        }
                    }
                }
                else
                {// tckb <= 14
                    if (sS.Length == 14)
                    {
                        s.sGTIN = sS;
                        s.sEAN = Srv.CheckSumModul10(sS.Substring(1, 12));
                        bFind = SetKMCOnGTIN(ref s);
                        if (!bFind)
                            sS = s.sEAN;
                        else
                            sS = "";
                    }
                    else
                    {
                        sIdPrim = sS.Substring(0, 1);
                        if (sIdPrim == "2")     // весовая продукция или внутренний код
                        {
                            bFind = xNSI.IsAlien(sS, ref s);
                            if (!bFind)
                            {
                                sS = sS.Substring(1);
                                s.fVes = FRACT.Parse(sS.Substring(5, 6)) / 1000;
                                if (sS.Substring(0, 1) != "9")
                                {// на транспортной единице (ящик или поддон)
                                    s.nParty = int.Parse(sS.Substring(0, 3)).ToString();
                                    s.nKrKMC = int.Parse(sS.Substring(3, 2));
                                }
                                else
                                {// на отдельной единице весовой продукции
                                    if (sS.Substring(4, 1) == "6")
                                        s.nKrKMC = 52;
                                    else
                                        s.nKrKMC = 23;
                                    s.tTyp = AppC.TYP_TARA.TARA_POTREB;
                                }
                                //bFind = xNSI.GetMCData("", ref s, s.nKrKMC, false);
                                bFind = xNSI.Connect2MC("", s.nKrKMC, s.nPrzvFil, ref s);
                                s.bVes = true;
                            }
                            else
                            {
                                s.bAlienMC = true;
                                s.fVes = FRACT.Parse(sS.Substring(7, 5)) / 1000;
                            }
                            // поиск по EAN не нужен
                            sS = "";
                        }
                        else
                        {
                            s.sEAN = sS;
                            s.sGTIN = "0" + sS;
                        }
                    }
                    if (sS.Length > 0)     // НЕ весовая продукция или внутренний код
                        ret = xNSI.GetMCDataOnEAN(sS, ref s, true);
                }
            }
            catch
            {
                ret = false;
                sS = "";
            }
            return (ret);
        }

        // новый формат ШК для материалов
        public bool TranslMTNew(ref PSC_Types.ScDat s)
        {
            bool
                bPoddon = false,
                bFind = false,          // связь со справочником MC не установлена (пока)
                ret = false;
            string
                sTaraType,
                sP,
                sS = s.s;

            sTaraType = sS.Substring(0, 2);
            if (sTaraType == "52")
            {// для поддонов
                bPoddon = true;
                s.tTyp = AppC.TYP_TARA.TARA_PODDON;
            }
            else if (sTaraType == "53")
            {// для тарных мест
                bPoddon = false;
                s.tTyp = AppC.TYP_TARA.TARA_TRANSP;
            }
            sS = sS.Substring(2);

            // код материала
            s.sKMC = sS.Substring(0, 10);
            //s.sEAN = Srv.CheckSumModul10("20" + sS.Substring(0, 10));
            s.sEAN = sS.Substring(0, 10);
            bFind = xNSI.GetMCDataOnEAN(s.sEAN, ref s, true);
            sS = sS.Substring(10);

            // SysN документа (заключение) - оно же и номер партии
            s.nParty = sS.Substring(0, 9);

            // SysN документа (заключение)
            //s.nNPredMT = int.Parse(s.nParty) * (-1);
            s.nZaklMT = int.Parse(s.nParty);
            s.nParty = s.nParty.Substring(4, 5);

            sS = sS.Substring(9);

            // дата годности(изготовления) (ГГММДД)
            sP = sS.Substring(0, 6);

            //s.dDataGodn =
            //s.dDataIzg = DateTime.ParseExact(sP, "yyMMdd", null);
            //s.sDataIzg = sP.Substring(4, 2) + "." + sP.Substring(2, 2) + "." +
            //    sP.Substring(0, 2);

            s.dDataGodn = DateTime.ParseExact(sP, "yyMMdd", null);
            s.sDataGodn = sP.Substring(4, 2) + "." + sP.Substring(2, 2) + "." +
                sP.Substring(0, 2);

            sS = sS.Substring(6);

            // емкость/количество единиц
            s.fVsego = Srv.Str2VarDec(sS.Substring(0, 7));
            s.fEmk = s.fEmk_s = s.fVsego;

            if (s.bVes)
                s.fVes = s.fVsego;

            sS = sS.Substring(7);

            if (bPoddon)
            {
                // № поддона
                s.nNomPodd = int.Parse(sS.Substring(0, 3));
                s.nMestPal = int.Parse(sS.Substring(3, 3));
                s.nMest = s.nMestPal;
                if (!s.bVes)
                {
                    s.fVsego = s.fEmk * s.nMestPal;
                }
            }
            else
            {
            }

            sS = "";
            ret = true;

            return (ret);
        }




        // проверка (установка) KMC по GTIN
        private bool SetKMCOnGTIN(ref PSC_Types.ScDat sc)
        {
            bool
                ret = AppC.RC_CANCELB;
            string
                sF;
            int
                nPrzPl = 0;
            DataView
                dvMC,
                dv;
            DataRow
                //drMC,
                drE;

            if (sc.sGTIN.Length > 0)
            {
                sF = String.Format("(ITF14='{0}')AND(EMK>0)", sc.sGTIN);
                dv = new DataView(xNSI.DT[NSI.NS_SEMK].dt, sF, "EMK", DataViewRowState.CurrentRows);
                if (dv.Count > 0)
                {
                    drE = dv[0].Row;
                    sc.bSetAccurCode = true;
                    sc.fEmk = (FRACT)drE["EMK"];
                    //sc.nMestPal = (int)drE["EMKPOD"];
                    if ((sc.tTyp == AppC.TYP_TARA.TARA_PODDON) && (sc.nMestPal > 0))
                    { }
                    else
                        sc.nMestPal = (int)drE["EMKPOD"];

                    dvMC = new DataView(xNSI.DT[NSI.NS_MC].dt, String.Format("(KMC='{0}')", drE["KMC"]), "", DataViewRowState.CurrentRows);
                    ret = sc.GetFromNSI(sc.s, dvMC[0].Row, ref nPrzPl);
                }
            }

            return (ret);
        }



        // старые коды Ногинска (16-символов)
        private bool CasandraGTIN(ref PSC_Types.ScDat s)
        {
            //string
            //    sP;
            int 
                n;
            bool 
                ret = false;
            DateTime
                dStart = new DateTime(2005, 1, 1),
                dBase;

            try
            {
                if (xScan.Id == ScannerAll.BCId.Code128)
                {

                    s.nKrKMC = int.Parse(xScan.Dat.Substring(0, 3));
                    s.bSetAccurCode = true;
                    n = int.Parse(xScan.Dat.Substring(3, 1));
                    dBase = dStart.AddYears(n * 2);

                    //s.dDataIzg = dBase.AddDays( int.Parse(xScan.Dat.Substring(4, 3)) );
                    //s.dDataMrk = dBase.AddDays( int.Parse(xScan.Dat.Substring(7, 3)) );

                    s.dDataMrk = dBase.AddDays(int.Parse(xScan.Dat.Substring(4, 3)));
                    //s.dDataIzg = dBase.AddDays(int.Parse(xScan.Dat.Substring(7, 3)));
                    s.dDataIzg = s.dDataMrk;
                    s.sDataIzg = s.dDataIzg.ToString("dd.MM.yy");



                    // коробка или поддон
                    n = int.Parse(xScan.Dat.Substring(10, 3));
                    if (n == 0)
                        s.tTyp = AppC.TYP_TARA.TARA_TRANSP;
                    else
                    {
                        s.tTyp = AppC.TYP_TARA.TARA_PODDON;
                        s.nMestPal = n;
                    }
                    //s.nParty = int.Parse(xScan.Dat.Substring(13, 3));
                    s.nParty = xScan.Dat.Substring(13, 3);

                    //xNSI.GetMCData(sSig.sEAN, ref sSig, 0);
                    xNSI.Connect2MC("", s.nKrKMC, 0, ref s);
                    if (s.bFindNSI)
                    {
                        //s.dDataGodn = s.dDataMrk.AddDays(s.nSrok);
                        //s.sDataGodn = s.dDataGodn.ToString("dd.MM.yy");
                    }

                    ret = true;
                }
            }
            catch
            {
                ret = false;
            }
            return (ret);
        }

        // заполнение структуры ScDat на основе прочитанного штрих-кода
        // (находится там же)
        private bool NewTranslSCode(ref PSC_Types.ScDat s)
        {
            string 
                sP;
            int 
                n;
            bool 
                ret = true;

            try
            {
                if ( (xScan.Id == ScannerAll.BCId.Code128) ||
                     (xScan.Id == ScannerAll.BCId.GS1DataBar) )
                {
                    s.nPrzvFil = AppC.OAOSP_CODE;
                    if (xScan.dicSc.ContainsKey("01"))
                    {
                        //s.sEAN = xScan.dicSc["01"].Dat.Substring(1);
                        s.sEAN = xScan.dicSc["01"].Dat.Substring(1, 12);
                        s.sGTIN = xScan.dicSc["01"].Dat;
                        s.tTyp = AppC.TYP_TARA.TARA_TRANSP;
                    }
                    if (xScan.dicSc.ContainsKey("02"))
                    {
                        //s.sEAN = xScan.dicSc["02"].Dat.Substring(1);

                        s.sEAN = xScan.dicSc["02"].Dat.Substring(1, 12);
                        s.sGTIN = xScan.dicSc["02"].Dat;
                        s.tTyp = AppC.TYP_TARA.TARA_PODDON;
                    }
                    s.sEAN = Srv.CheckSumModul10(s.sEAN);

                    if (((xScan.bcFlags & ScanVarRM.BCTyp.SNT_GTIN_OLD) == ScanVarRM.BCTyp.SNT_GTIN_OLD) ||
                        ((xScan.bcFlags & ScanVarRM.BCTyp.SNT_GTIN_NEW) == ScanVarRM.BCTyp.SNT_GTIN_NEW))
                    {// этикетка Санты (36 | 44) для ящиков тоже AI=02
                        s.tTyp = AppC.TYP_TARA.TARA_TRANSP;
                    }

                    // срок годности
                    if (xScan.dicSc.ContainsKey("17"))
                    {
                        if (xScan.dicSc["17"].xV is DateTime)
                        {
                            s.dDataGodn = (DateTime)xScan.dicSc["17"].xV;
                            s.sDataGodn = s.dDataGodn.ToString("dd.MM.yy");
                        }
                    }

                    if (xScan.dicSc.ContainsKey("11"))
                    {// дата изготовления
                        sP = xScan.dicSc["11"].Dat;
                        s.dDataIzg = (DateTime)(xScan.dicSc["11"].xV);
                        s.sDataIzg = s.dDataIzg.ToString("dd.MM.yy");
                    }

                    // партия
                    if (xScan.dicSc.ContainsKey("10"))
                    {
                        sP = xScan.dicSc["10"].Dat;
                        if (((xScan.bcFlags & ScanVarRM.BCTyp.SNT_GTIN_OLD) == ScanVarRM.BCTyp.SNT_GTIN_OLD) ||
                            ((xScan.bcFlags & ScanVarRM.BCTyp.SNT_GTIN_NEW) == ScanVarRM.BCTyp.SNT_GTIN_NEW))
                        {// в партии несколько полей
                            if ((xScan.bcFlags & ScanVarRM.BCTyp.SNT_GTIN_OLD) == ScanVarRM.BCTyp.SNT_GTIN_OLD)
                            {// в старой этикетке партия содержит несколько полей

                                //s.dDataIzg = DateTime.ParseExact(sP.Substring(3, 6), "yyMMdd", null);

                                //sP = sP.Substring(9, 5);
                                sP = sP.Substring(9);
                            }
                            else
                            {
                                if ((xScan.bcFlags & ScanVarRM.BCTyp.SNT_GTIN_NEW) == ScanVarRM.BCTyp.SNT_GTIN_NEW)
                                {
                                    //int
                                    //    nNed = int.Parse(sP.Substring(5, 2)),
                                    //    nDay = int.Parse(sP.Substring(7, 1));
                                    //DateTime
                                    //    dt1st = DateTime.ParseExact("201" + sP.Substring(4, 1) + "0101", "yyyyMMdd", null);
                                    //System.Globalization.Calendar xC = new System.Globalization.GregorianCalendar();

                                    //s.dDataIzg = dt1st.AddDays(7 * (nNed - 1) + nDay - 2); ;
                                    //s.sDataIzg = s.dDataIzg.ToString("dd.MM.yy");

                                    //sP = sP.Substring(8);

                                    sP = sP.Substring(4);
                                }
                            }
                        }
                        else
                        {
                            if ((xScan.bcFlags & ScanVarRM.BCTyp.NG_BOX) == ScanVarRM.BCTyp.NG_BOX)
                            {// Ногинск новая (EAN128)
                                s.dDataIzg = DateTime.ParseExact(sP.Substring(0, 6), "yyMMdd", null);
                                s.sDataIzg = s.dDataIzg.ToString("dd.MM.yy");

                                sP = sP.Substring(6);
                                // потом убрать!!!
                                //s.tTyp = AppC.TYP_TARA.TARA_TRANSP;
                            }
                        }
                        try
                        {
                            while (sP.Length > 0)
                            {
                                if (sP.StartsWith("0"))
                                    sP = sP.Substring(1);
                                else
                                    break;
                            }
                            s.nParty = sP;
                        }
                        catch
                        {
                        }


                    }

                    //xNSI.GetMCData(sSig.sEAN, ref sSig, 0);
                    //xNSI.Connect2MC(s.sEAN, 0, s.nPrzvFil, ref s);
                    ret = SetKMCOnGTIN(ref s);
                    if (!ret)
                        ret = xNSI.Connect2MC(s.sEAN, 0, s.nPrzvFil, ref s);

                    if (xScan.dicSc.ContainsKey("37"))
                    {
                        n = (int)(long)(xScan.dicSc["37"].xV);
                        if (s.tTyp == AppC.TYP_TARA.TARA_PODDON)
                        {
                            s.nMestPal = n;
                        }
                        else
                        {
                            //if (s.bVes)
                            //    s.nKolSht = n;
                            //else
                            //    s.fEmk = n;
                                
                            if (s.bVes)
                                s.nKolSht = n;
                            else
                            {
                                if (s.fEmk == 0)
                                    s.fEmk = n;
                            }
                        }
                    }




                    if (xScan.dicSc.ContainsKey("30"))
                    {
                        n = (int)(long)(xScan.dicSc["30"].xV);
                        if (s.tTyp == AppC.TYP_TARA.TARA_PODDON)
                        {
                            s.nKolSht = n;
                            if (s.nMestPal == 0)
                            {
                                if ((int)s.fEmk > 0)
                                    s.nMestPal = n / (int)s.fEmk;
                            }
                        }
                        else
                        {
                            if (s.bVes)
                                s.nKolSht = n;
                            else
                                s.fEmk = n;
                        }
                    }


                    if (xScan.dicSc.ContainsKey("310"))
                    {// весовой товар
                        s.nTara = 0;
                        s.fVes = (FRACT)(xScan.dicSc["310"].xV);
                    }

                    if (xScan.dicSc.ContainsKey("23"))
                    {// номер поддона или места
                        if (s.tTyp == AppC.TYP_TARA.TARA_PODDON)
                            s.nNomPodd = (int)(long)(xScan.dicSc["23"].xV);
                        else
                            s.nNomMesta = (int)(long)(xScan.dicSc["23"].xV);
                    }
                    // серийный номер
                    if (xScan.dicSc.ContainsKey("21"))
                    {
                        if (s.tTyp == AppC.TYP_TARA.TARA_PODDON)
                            //s.nNomPodd = (int)(long)(xScan.dicSc["21"].xV);
                        //else
                            //s.nNomMesta = (int)(long)(xScan.dicSc["21"].xV);
                        {
                            try
                            {
                                s.nNomPodd = int.Parse((string)(xScan.dicSc["21"].xV));
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                s.nNomMesta = int.Parse((string)(xScan.dicSc["21"].xV));
                            }
                            catch { }
                        }
                    }

                }
            }
            catch (Exception e)
            {
                ret = false;
                //sTypDoc.sN = sS + "-???";
            }
            return (ret);
        }


        private bool CompareEmk(ref PSC_Types.ScDat s, int n)
        {
            bool
                ret = true;
            int
                nNSI = 0;
            if (s.tTyp == AppC.TYP_TARA.TARA_TRANSP)
            {// на ящиках кривые этикетки могут неправильно давать емкость
                //if ((s.drSEMK != null) && (n > 0))
                if ((s.xEmks.Count > 0) && (n > 0))
                {// удалось установить емкость по справочнику
                    if (s.bVes)
                    {
                        nNSI = (int)((StrAndInt)s.xEmks.Current).IntCodeAdd1;
                        if (s.nKolSht != n)
                        {
                            string
                                sP = String.Format("Несовпадение емкостей!\nВ штрихкоде - {0}\nВ справочнике - {1}\nПодтвердить {0}(Enter)?\n(ESC)-принять {1}", n, nNSI);
                            DialogResult dr = MessageBox.Show(sP, String.Format("Несовпадение:{0} <> {1}", n, s.nKolSht),
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                            if (dr == DialogResult.OK)
                                s.nKolSht = n;
                            else
                                s.nKolSht = nNSI;
                        }
                    }
                    else
                    {
                        nNSI = (int)((StrAndInt)s.xEmks.Current).DecDat;
                        if (nNSI != n)
                        {
                            string
                                sP = String.Format("Несовпадение емкостей!\nВ штрихкоде - {0}\nВ справочнике - {1}\nПодтвердить {0}(Enter)?\n(ESC)-принять {1}", n, nNSI);
                            DialogResult dr = MessageBox.Show(sP, String.Format("Несовпадение:{0} <> {1}", n, nNSI),
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                            if (dr == DialogResult.OK)
                                s.fEmk = s.fEmk_s = n;
                            else
                                s.fEmk = s.fEmk_s = nNSI;
                        }
                    }
                }
            }
            return (ret);
        }

        // проверка емкости в штуках по справочнику емкостей
        private int CheckEmk(DataTable dtM, DataTable dtD, ref PSC_Types.ScDat sc)
        {
            int nRet = AppC.RC_OK,
                nEmkPod_Def = 0,
                nEmkPod = 0;
            FRACT fCE, 
                fEmk_Def = 0,
                fEmk = 0;

            if ((sc.bFindNSI == true) && (sc.drMC != null))
            {// поиск емкости по коду продукции и возможно считанному весу
                DataRelation myRelation = dtM.ChildRelations["KMC2Emk"];
                DataRow[] childRows = sc.drMC.GetChildRows(myRelation);
                if (childRows.Length == 1)
                {// подбирать нечего, только одна емкость
                    fEmk = (FRACT)childRows[0]["EMK"];
                    nEmkPod = (int)childRows[0]["EMKPOD"];
                }
                else
                {
                    foreach (DataRow chRow in childRows)
                    {
                        fCE = (FRACT)chRow["EMK"];
                        nEmkPod = (int)chRow["EMKPOD"];
                        if (fCE != 0)
                        {// емкость указана
                            if (fCE == sc.fEmk)
                            {// емкость совпала
                                fEmk = fCE;
                                break;
                            }
                            if ((int)chRow["PR"] > 0)
                            {// емкость по умолчанию
                                fEmk_Def = fCE;
                                nEmkPod_Def = (int)chRow["EMKPOD"];
                            }
                        }
                    }
                    if ((fEmk == 0) && (fEmk_Def != 0))
                    {
                        fEmk = fEmk_Def;
                        nEmkPod = nEmkPod_Def;
                    }
                }
                if (fEmk != 0)
                {
                    if ((sc.fEmk == 0) || (bEditMode == true))
                    {
                        sc.fEmk = fEmk;
                        sc.fEmk_s = sc.fEmk;
                    }
                    else
                    {
                        if (fEmk != sc.fEmk)
                        {
                            DialogResult dr = MessageBox.Show("Отменить сканирование(Enter)?\n(ESC)-подвердить емкость",
                                "Несовпадение емкостей",
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                            if (dr == DialogResult.OK)
                                nRet = AppC.RC_CANCEL;
                        }
                    }
                }
            }
            if (sc.nMestPal == 0)
                sc.nMestPal = nEmkPod;
            return (nRet);
        }

        // проверка на совпадение продукции
        private bool IsSameKMC(DataRow dr, int nKr, string sK)
        {
            bool
                bRet = false;
            try
            {
                if (xPars.PKeyIsGUID)
                {
                    bRet = ((string)dr["KMC"] == sK) ? true : false;
                }
                else
                {
                    bRet = ((int)dr["KRKMC"] == nKr) ? true : false;
                }
            }
            catch{}
            return (bRet);
        }


        private int GetKSKFromBC(string sEvent, string sBC, string sBCT)
        {
            int
                nRet = 0;
            object
                x;
            object[]
                aP;
            ExprDll.Action
                xFind;

            if (xExpDic.Count > 0)
            {// блок кода присутствует 
                try
                {
                    xFind = xGExpr.run.FindFunc(sEvent);
                    if (xFind != null)
                    {
                        aP = new object[] { sBC, sBCT };
                        x = xGExpr.run.ExecFunc(sEvent, aP);
                        if (x != null)
                            nRet = (int)x;
                    }
                }
                catch (Exception ex)
                {
                    string
                        s = ex.Message;
                    Srv.ErrorMsg(s);
                }
                finally
                {

                }
            }
            return (nRet);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //bool r = ManyEANChoice();
            tcMain.SelectedIndex = PG_SCAN;
            OnScan(null, new BarcodeScannerEventArgs(BCId.EAN13, "4605561000702"));
        }


        public DataRow ManyEANChoice(string sEAN, int nKrKMC, bool bSelectEAN)
        {
            int
                nPrzPl = 0;
            DialogResult 
                xDRslt;
            DataRow 
                dr = null;

            //Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
            xDRslt = CallDllForm(sExeDir + "SGPF-LstCh.dll", true, new object[]{this, sEAN, nKrKMC, bSelectEAN});
            if (xDRslt == DialogResult.OK)
            {
                dr = (DataRow)xDLLAPars[0];
                if (bSelectEAN)
                    scCur.GetFromNSI(scCur.sEAN, dr, ref nPrzPl);
                scCur.bSetAccurCode = true;
            }
            return (dr);
        }


        // nFunc - 1 - источник
        private int TestOperByZVK(int nFunc)
        {
            int
                nRet = AppC.RC_OK;
            if (bZVKPresent)
            {
                if (xCDoc.xDocP.nNumTypD == AppC.TYPD_MOVINT)
                {
                }
            }

            return(nRet);
        }



    }
}
