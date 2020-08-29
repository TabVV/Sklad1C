using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Data;
using System.Drawing;

using ScannerAll;
using PDA.OS;
using PDA.BarCode;
using PDA.Service;

using FRACT = System.Decimal;

namespace SkladRM
{

    public partial class MainF : Form
    {

        public enum ADR_TYPE : int
        {
            UNKNOWN     = 0,
            OBJECT      = 1,            // объект
            CHANNEL     = 2,            // канал
            LEVEL       = 4,            // ярус
            STELLAGE    = 8,            // стеллаж
            ZONE        = 32,            // зона
            HIGHBAY     = 128,
            VIRTUAL     = 512,          // код пользователя/паллеты
            SSCC        = 1024          // SSCC
        }

        // информация по адресу для операции
        public class AddrInfo
        {
            public static DataTable
                dtA = null;
            // функция создания строкового представления адреса
            public static ExprDll.RUN
                xR = null;

            // структура: ХХ-склад, ХХ-стеллаж, ХХ-линия, ХХ-ряд, Х-ярус
            private string
                m_FullAddr = "",           // адрес ячейки-зоны
                m_AddrName = "";
            private int
                m_Sklad;
            private DateTime
                m_dtScan = DateTime.Now;

            private ScanVarRM
                m_Scan;
            //private ExprDll.RUN
            //    xR = null;

            //public string
            //    sName = "",           // наименование ячейки-зоны
            //    sSklad = "",            // категория ячейки-зоны
            //    sStellag = "",            // стеллаж
            //    sLine = "",           // адрес ячейки-зоны
            //    sMesto = "",           // адрес ячейки-зоны
            //    sYarus = "";           // адрес ячейки-зоны

            public bool 
                bFixed = false;         // адрес зафиксирован

            public ADR_TYPE
                nType = ADR_TYPE.UNKNOWN;         // тип адреса


            public AddrInfo() { }

            // Происхождение адреса - сканирование
            public AddrInfo(ScanVarRM xSc, int nSklad)
            {
                m_Scan = xSc;
                m_Sklad = nSklad;
                ScanDT = xSc.ScanDTime;
                if ((xSc.bcFlags & ScanVarRM.BCTyp.SP_SSCC) > 0)
                {
                    nType = ADR_TYPE.SSCC;
                    Addr = xSc.Dat;
                }
                else
                {
                    nType = ADR_TYPE.OBJECT;
                    Addr = xSc.Dat.Substring(2);
                }
            }

            // Происхождение - из таблицы
            public AddrInfo(string sA, int nSklad)
            {
                m_Scan = null;
                m_Sklad = nSklad;
                ScanDT = DateTime.MinValue;
                Addr = sA;
            }

            // строка адреса
            public string Addr
            {
                get { return m_FullAddr; }
                set
                {
                    try
                    {
                        m_FullAddr = value.Trim();
                        if (nType == ADR_TYPE.UNKNOWN)
                        {
                            if (m_FullAddr.Length == 9)
                                nType = ADR_TYPE.OBJECT;
                            else
                            {
                                if ((m_FullAddr.Length == 20) && (m_FullAddr.Substring(0, 2) == "00"))
                                {
                                    nType = ADR_TYPE.SSCC;
                                }
                                else
                                    if (m_FullAddr.IndexOf("USID") >= 0)
                                        nType = ADR_TYPE.VIRTUAL;
                            }
                        }

                        if (m_FullAddr.Length > 0)
                            m_AddrName = AdrName(m_FullAddr, dtA, xR);
                        else
                            m_AddrName = "";

                    }
                    catch
                    {
                        nType = ADR_TYPE.UNKNOWN;
                    }
                }
            }

            //private string x;
            // символьное отображение адреса
            public string AddrShow
            {
                get { return m_AddrName; }
                set { m_AddrName = value; }
            }

            // визуальное представление адреса
            private string AdrName(string sA, DataTable NS_Adr, ExprDll.RUN xFun4Name)
            {
                string
                    sN = "";
                DataRow
                    dr;

                try
                {
                    try
                    {
                        if (nType == ADR_TYPE.SSCC)
                        {
                            sN = String.Format("SSCC-{0}...{1}", m_FullAddr.Substring(2,1), m_FullAddr.Substring(m_FullAddr.Length - 1 - 4, 4));
                        }
                        else
                        {
                            dr = NS_Adr.Rows.Find(new object[] { sA });
                            sN = ((string)dr["NAME"]).Trim();
                        }
                    }
                    catch { sN = ""; }

                    if (sN.Length == 0)
                    {
                        if (nType < ADR_TYPE.VIRTUAL)
                        {
                            if (xR != null)
                            {
                                sN = (string)xFun4Name.ExecFunc(AppC.FEXT_ADR_NAME, new object[] { m_Sklad, sA });
                            }
                            else
                                sN = String.Format("{1}-{2}.{3}.{4}",
                                sA.Substring(0, 2),
                                sA.Substring(2, 2),
                                sA.Substring(4, 2),
                                sA.Substring(6, 2),
                                sA.Substring(8, 1)
                                );
                        }
                        else if (nType == ADR_TYPE.VIRTUAL)
                            sN = String.Format("V-<поддон>", Addr.Substring(4));
                    }
                }
                catch
                {
                    sN = "";
                }
                if (sN.Length == 0)
                    sN = sA;
                return (sN);
            }

            // время сканирования адреса
            public DateTime ScanDT
            {
                get { return m_dtScan; }
                set { m_dtScan = value; }
            }

        }

        // вставка записи с виртульной продукцией (пусто, SSCC)
        private DataRow AddVirtProd(ref PSC_Types.ScDat sc)
        {
            DataRow
                drFictProd = null;

            drFictProd = xNSI.AddDet(sc, xCDoc, null);
            if (drFictProd != null)
            {
                xCDoc.xOper.SetOperObj(drFictProd, xCDoc.xDocP.DType);
                if (bShowTTN)
                {
                    drDet = drFictProd;
                    scCur = sc;
                    SetCurRow(dgDet, "ID", (int)drFictProd["ID"]);
                }
                SetDetFields(false);
                AfterAddScan(this, new EventArgs());
            }
            return (drFictProd);
        }

        // имитация пустой ячейки двойным сканом
        private DataRow ZeroCell(AddrInfo xSrc, AddrInfo xDst)
        {
            DataRow
                drFictProd = null;

            if (false && // 02.12.16 - пока убрал
                (xSrc is AddrInfo)&&(xDst is AddrInfo)&&(xSrc.Addr == xDst.Addr))
            {
                if (!xCDoc.xOper.bObjOperScanned && (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.AVAIL) && (xPars.UseAdr4DocMode))
                {
                    PSC_Types.ScDat sc = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));
                    sc.nKrKMC = 0;
                    AddVirtProd(ref sc);
                }
            }
            return(drFictProd);
        }



        // какой из адресов следует заполнить (по типу движения документа)
        private int SrcOrDest(ScanVarRM xSc, ref PSC_Types.ScDat scD)
        {
            int
                nNumDocType = xCDoc.xDocP.DType.NumCode,
                nSrcOrDst = 0;

            AppC.MOVTYPE
                MoveType = xCDoc.xDocP.DType.MoveType;

            if ((xSc.bcFlags & ScanVarRM.BCTyp.SP_SSCC) == 0)
            {// это обычный адрес
                switch (MoveType)
                {
                    case AppC.MOVTYPE.AVAIL:        // инвентаризации
                        nSrcOrDst = 1;
                        break;
                    case AppC.MOVTYPE.RASHOD:       // расходные документы
                        nSrcOrDst = 1;
                        break;
                    case AppC.MOVTYPE.PRIHOD:       // документы поступления
                        nSrcOrDst = 2;
                        break;
                    case AppC.MOVTYPE.MOVEMENT:     // документы перемещения
                        if (!xCDoc.xOper.IsFillSrc())
                            nSrcOrDst = 1;
                        else
                        {
                            if (!xCDoc.xOper.IsFillDst())
                                nSrcOrDst = 2;
                        }
                        break;
                    default:
                        if (xCDoc.xOper.IsFillSrc())
                        {// источник - задан

                            if (xCDoc.xDocP.DType.AdrToNeed)
                            {//... - true
                                if (!xCDoc.xOper.IsFillDst())
                                    nSrcOrDst = 2;
                            }
                            else
                            {//... - false
                                // надо посмотреть, чем заполнен
                                if (!xCDoc.xOper.IsFillDst())
                                    nSrcOrDst = 1;
                            }
                        }
                        else
                        {// источник - пусто
                            if (xCDoc.xDocP.DType.AdrFromNeed)
                                nSrcOrDst = 1;
                            else
                            {
                                if (xCDoc.xDocP.DType.AdrToNeed)
                                {// false - true
                                    if (!xCDoc.xOper.IsFillDst())
                                        nSrcOrDst = 2;
                                }
                                else
                                {// false - false
                                    if (!xCDoc.xOper.IsFillDst())
                                        nSrcOrDst = 1;
                                }
                            }
                        }
                        break;
                }
            }
            else
            {// для SSCC
                if ((!xPars.UseAdr4DocMode) &&
                    (xCDoc.xDocP.DType.MoveType != AppC.MOVTYPE.MOVEMENT))
                {// если нет адресного и не внутрискладское, то это просто поддон
                    nSrcOrDst = AppC.RC_NOTADR;
                }
                else
                {
                    #region SSCC depending types
                    switch (MoveType)
                    {
                        case AppC.MOVTYPE.AVAIL:        // инвентаризации
                            if ((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_SRC_SET) > 0)
                                nSrcOrDst = AppC.RC_NOTADR;                                 // использовать содержимое
                            //else
                            //    nSrcOrDst = AppC.RC_CONTINUE;                               // сначала - адрес
                            else
                            {
                                if (xPars.UseAdr4DocMode)
                                    nSrcOrDst = AppC.RC_CONTINUE;                               // сначала - адрес
                                else
                                    nSrcOrDst = AppC.RC_NOTADR;                                 // использовать содержимое
                            }
                            break;
                        case AppC.MOVTYPE.RASHOD:       // расходные документы
                            if ((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_SRC_SET) == 0)
                                nSrcOrDst = 1;
                            else
                            {
                                if ((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_OBJ_SET) > 0)
                                    nSrcOrDst = 2;
                                else
                                    nSrcOrDst = AppC.RC_NOTADR;
                            }
                            break;
                        case AppC.MOVTYPE.PRIHOD:       // документы поступления
                            if ((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_OBJ_SET) > 0)
                                nSrcOrDst = 2;                                              // использовать содержимое
                            else
                                nSrcOrDst = AppC.RC_NOTADR;                                 // сначала - адрес
                            break;
                        case AppC.MOVTYPE.MOVEMENT:     // документы перемещения

                            if ((xCDoc.xOper.nOperState == AppC.OPR_STATE.OPR_EMPTY))
                            {
                                nSrcOrDst = 1;                                              // адрес-источник
                            }
                            else
                            {
                                if ((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_OBJ_SET) > 0)
                                {// есть объект, но он должен быть коробка или штучка
                                    nSrcOrDst = 2;                                              // адрес-источник
                                }
                                else
                                {
                                    nSrcOrDst = AppC.RC_NOTADR;
                                }

                            }
                            break;
                        default:
                            if (xCDoc.xOper.IsFillSrc())
                            {// источник - задан

                                if (xCDoc.xDocP.DType.AdrToNeed)
                                {//... - true
                                    if (!xCDoc.xOper.IsFillDst())
                                        nSrcOrDst = 2;
                                }
                                else
                                {//... - false
                                    // надо посмотреть, чем заполнен
                                    if (!xCDoc.xOper.IsFillDst())
                                        nSrcOrDst = 1;
                                }
                            }
                            else
                            {// источник - пусто
                                if (xCDoc.xDocP.DType.AdrFromNeed)
                                    nSrcOrDst = 1;
                                else
                                {
                                    if (xCDoc.xDocP.DType.AdrToNeed)
                                    {// false - true
                                        if (!xCDoc.xOper.IsFillDst())
                                            nSrcOrDst = 2;
                                    }
                                    else
                                    {// false - false
                                        if (!xCDoc.xOper.IsFillDst())
                                            nSrcOrDst = 1;
                                    }
                                }
                            }
                            break;
                    }

                    #endregion
                }
            }

            if (nSrcOrDst == 0)
            {// выяснить не удалось
                DialogResult drQ = MessageBox.Show("\"Источник\" - Yes\n\"Приемник\" - No\nОтмена - Cancel",
                    "Какой адрес установить?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (drQ == DialogResult.Yes)
                    nSrcOrDst = 1;
                else if (drQ == DialogResult.No)
                    nSrcOrDst = 2;
            }

            return (nSrcOrDst);
        }


        Color

            С_ADR_EMP   = Color.Lavender,                       // адрес пуст
            C_OPR_READY = Color.Yellow,                         // стрелки движения
            С_ADR_SET   = Color.LightSkyBlue,                   // адрес установлен
            С_OBJ_EMP   = Color.Lavender,                       // объект пуст
            С_OBJ_SET   = Color.CornflowerBlue;                 // адрес установлен

        // отображение адресов и статуса операции
        public void ShowOperState(CurOper xOp)
        {
            ShowOperState(xOp, -1);
        }

        public void ShowOperState(CurOper xOp, int nM)
        {
            string
                A1 = xOp.GetSrc(true),
                A2 = xOp.GetDst(true),
                x = "";

            if (nM < 0)
                x = "";
            else
            {
                x = "> " + nM.ToString();
                lObjDirection.ForeColor = Color.Black;
            }

            if (xCDoc == null)
                return;

            lAdrFrom.SuspendLayout();
            lAdrTo.SuspendLayout();
            lObjDirection.SuspendLayout();

            if (xCDoc.xOper == xOp)
            {
                if ((xOp.nOperState & AppC.OPR_STATE.OPR_SRC_SET) > 0)
                {// источник установлен
                    lAdrFrom.BackColor = С_ADR_SET;
                }
                else
                    lAdrFrom.BackColor = С_ADR_EMP;

                if ((xOp.nOperState & AppC.OPR_STATE.OPR_DST_SET) > 0)
                {// приемник установлен
                    lAdrTo.BackColor = С_ADR_SET;
                }
                else
                    lAdrTo.BackColor = С_ADR_EMP;

                if ((xOp.nOperState & AppC.OPR_STATE.OPR_OBJ_SET) > 0)
                {// продукт установлен
                    lObjDirection.BackColor = С_OBJ_SET;
                }
                else
                    lObjDirection.BackColor = С_OBJ_EMP;

                if ((xOp.nOperState & AppC.OPR_STATE.OPR_READY) > 0)
                {// операция готова к передаче на сервер
                    x = ">=>=>";
                    lObjDirection.ForeColor = C_OPR_READY;
                }
            }
            else
            {
                lAdrFrom.BackColor = С_ADR_EMP;
                lAdrTo.BackColor = С_ADR_EMP;
                lObjDirection.BackColor = С_OBJ_EMP;
            }
            lAdrFrom.Text = A1;
            lAdrTo.Text = A2;
            lObjDirection.Text = x;

            lObjDirection.ResumeLayout();
            lAdrFrom.ResumeLayout();
            lAdrTo.ResumeLayout();
        }


        //public void ShowOperState(CurOper xOp)
        //{
        //    string
        //        A1 = xOp.GetSrc(true),
        //        A2 = xOp.GetDst(true),
        //        x  = ">=>=>";

        //    if (xCDoc == null)
        //        return;

        //    lAdrFrom.SuspendLayout();
        //    lAdrTo.SuspendLayout();
        //    lObjDirection.SuspendLayout();

        //    if (xCDoc.xOper == xOp)
        //    {
        //        if ((xOp.nOperState & AppC.OPR_STATE.OPR_SRC_SET) > 0)
        //        {// источник установлен
        //            lAdrFrom.BackColor = С_ADR_SET;
        //        }
        //        else
        //            lAdrFrom.BackColor = С_ADR_EMP;

        //        if ((xOp.nOperState & AppC.OPR_STATE.OPR_DST_SET) > 0)
        //        {// приемник установлен
        //            lAdrTo.BackColor = С_ADR_SET;
        //        }
        //        else
        //            lAdrTo.BackColor = С_ADR_EMP;

        //        if ((xOp.nOperState & AppC.OPR_STATE.OPR_OBJ_SET) > 0)
        //        {// продукт установлен
        //            lObjDirection.BackColor = С_OBJ_SET;
        //        }
        //        else
        //            lObjDirection.BackColor = С_OBJ_EMP;

        //        //if ((xOp.nOperState & AppC.OPR_STATE.OPR_READY) == 0)
        //        //{// операция готова к передаче на сервер
        //        //    x = "";
        //        //}

        //        if ((xOp.nOperState & AppC.OPR_STATE.OPR_READY) > 0)
        //        {// операция готова к передаче на сервер
        //            lObjDirection.ForeColor = C_OPR_READY;
        //        }
        //    }
        //    else
        //    {
        //        lAdrFrom.BackColor = С_ADR_EMP;
        //        lAdrTo.BackColor = С_ADR_EMP;
        //        lObjDirection.BackColor = С_OBJ_EMP;
        //        x = "";
        //    }
        //    lAdrFrom.Text = A1;
        //    lAdrTo.Text = A2;
        //    lObjDirection.Text = x;

        //    lObjDirection.ResumeLayout();
        //    lAdrFrom.ResumeLayout();
        //    lAdrTo.ResumeLayout();

        //}


        private int ProceedAdrNew(ScanVarRM xSc, ref PSC_Types.ScDat scD)
        {
            bool
                IsSSCC,
                bOperReady;
            int
                //nRec,
                nRet = AppC.RC_OK,
                nSrcOrDst = 0; // 1-From, 2-To
            string 
                sA1,
                sA2;

            
            if ((xSc.bcFlags & ScanVarRM.BCTyp.SP_SSCC) > 0)
            {
                IsSSCC = true;
                scD.sN = xSc.Dat;                               // SSCC полностью
            }
            else
            {
                IsSSCC = false;
                scD.sN = xSc.Dat.Substring(2);                  // значение адреса (без AI)
            }

            if (xSm.xAdrFix1 != null)
            {// зафиксирован адрес, пришел еще один
                if (!xCDoc.xOper.bObjOperScanned)
                {// поддон еще не сканировался, сейчас пришел адрес отправителя
                    nSrcOrDst = 1;
                    xCDoc.xOper.SetOperSrc(xSm.xAdrFix1, xCDoc.xDocP.DType);
                }
                else
                {// поддон уже сканировался, сейчас пришел адрес получателя
                    nSrcOrDst = 2;
                    xCDoc.xOper.SetOperDst(xSm.xAdrFix1, xCDoc.xDocP.DType);
                }
            }
            else
            {// фиксированных адресов пока не было
                nSrcOrDst = SrcOrDest(xSc, ref scD);
            }

            if ((nSrcOrDst == 1)||(nSrcOrDst == 2))
            {// отсканированным адресом следует воспользоваться  как адресом отправителя или получателя
                AddrInfo xA = new AddrInfo(xSc, xSm.nSklad);

                if (!xCDoc.xOper.bObjOperScanned && (xPars.UseAdr4DocMode))
                {// пока только для ... инвентаризации ...
                    if (xCDoc.xOper.nOperState == AppC.OPR_STATE.OPR_EMPTY)
                    {
                        PSC_Types.ScDat scEmp = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));
                        SetDetFields(true, ref scEmp);
                    }
                }

                if (nSrcOrDst == 1)
                {// это источник
                    xCDoc.xOper.SetOperSrc(xA, xCDoc.xDocP.DType);
                    sA1 = xA.AddrShow;
                    sA2 = xCDoc.xOper.GetDst(false);
                }
                else
                {// это приемник
                    xCDoc.xOper.SetOperDst(xA, xCDoc.xDocP.DType);
                    sA1 = xCDoc.xOper.GetSrc(false);
                    sA2 = xA.AddrShow;
                }
                //tAdrFrom.Text = sA1;
                //tAdrTo.Text = sA2;


                var d = ZeroCell(xCDoc.xOper.xAdrSrc, xCDoc.xOper.xAdrDst);
                if (d != null)
                {
                    xCDoc.xOper = new CurOper(xCDoc.xDocP.DType);
                }

                bOperReady = (xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_READY) > 0;

                if (bOperReady)
                {// операция готова к отправке, все введено
                    nRet = IsOperReady();
                }
                else
                {
                    if (!xCDoc.xOper.bObjOperScanned)
                    {
                        if (nSrcOrDst == 1)
                        {//для источника определяем содержимое
                            if (xCDoc.xDocP.DType.TryFromServer && xPars.UseAdr4DocMode)
                            {//если это требуется
                                if (!IsSSCC)
                                    nRet = AdrResult(xSc, ref scD, xA);
                            }
                        }
                        else
                        {
                            //tAdrTo.Text = xCDoc.xOper.GetDst(true);
                        }
                    }
                }
            }
            else
            {
                if (nSrcOrDst == AppC.RC_NOTADR)
                    nRet = AppC.RC_CONTINUE;
            }

            return (nRet);
        }

        private int IsOperReady()
        {
            int
                nRet = AppC.RC_OPNOTREADY;
            DataRow
                drOpr = xCDoc.xOper.OperObj;


            if ((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_READY) > 0)
            {// готовность операции имеется 
                nRet = AppC.RC_HALFOK;
                if (xPars.OpAutoUpl && !(xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.AVAIL))
                {// выгрузка по готовности каждой операции установлена и это не инвентаризация
                    nRet = AppC.RC_OK;
                    nRet = SetOverOPR(false, drOpr, AppC.COM_VOPR);


                }
                // или уже отправили или начинаем новую
                xCDoc.xOper = new CurOper(xCDoc.xDocP.DType);
            }


            //if (nRet == AppC.RC_OK)
            //{
            //    //if ((xCDoc.xOper.xAdrDst_Srv != null) && xCDoc.xOper.IsFillDst())
            //    //{// сервер задавал адрес назначения
            //    //    if (xCDoc.xOper.xAdrDst_Srv.Addr != xCDoc.xOper.xAdrDst.Addr)
            //    //    {
            //    //        DialogResult drQ = MessageBox.Show(String.Format(
            //    //            "(Yes) - {0}\n<<с сервера>>\n(No) - {1}\n<<отсканирован>>\n(ESC) - отмена", xCDoc.xOper.xAdrDst_Srv.Addr, xCDoc.xOper.xAdrDst.Addr),
            //    //            "Адрес назначения ?!",
            //    //            MessageBoxButtons.YesNoCancel, 
            //    //            MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            //    //        if (drQ == DialogResult.Yes)
            //    //            xCDoc.xOper.SetOperDst(xCDoc.xOper.xAdrDst_Srv, xCDoc.xDocP.DType);
            //    //        else if (drQ == DialogResult.No)
            //    //        { }
            //    //        else
            //    //            return (AppC.RC_CANCEL);
            //    //    }
            //    //}

            //    //drDet["TIMEOV"] = xCDoc.xOper.xAdrDst.ScanDT;
            //    //drDet["ADRFROM"] = xCDoc.xOper.xAdrSrc.Addr;
            //    //drDet["ADRTO"] = xCDoc.xOper.xAdrDst.Addr;

            //    //if (drOpr != null)
            //    //{
            //    //    drOpr["TIMEOV"] = xCDoc.xOper.xAdrDst.ScanDT;
            //    //    drOpr["ADRFROM"] = xCDoc.xOper.xAdrSrc.Addr;
            //    //    drOpr["ADRTO"] = xCDoc.xOper.xAdrDst.Addr;
            //    //}
            //    //else
            //    //    throw new Exception("Продукция не определена!");

            //}
            return (nRet);
        }

        private int SetOverOPR(bool bAfterScan, DataRow drOpr)
        {
            return (SetOverOPR(bAfterScan, drOpr, ""));
        }

        private int SetOverOPR(bool bAfterScan, DataRow drOpr, string sComm)
        {
            bool
                bNeedTrans;
            int
                nRet = AppC.RC_OK;
            ServerExchange
                xSE = new ServerExchange(this);

            if ((bEditMode == false) || true)
            {
                if (drOpr != null)
                {
                    bNeedTrans = (((int)drOpr["STATE"] != (int)AppC.OPR_STATE.OPR_TRANSFERED) || (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM));
                    if (bNeedTrans)
                    {
                        if (bAfterScan)
                        {
                            //if ((scCur.sKMC == (string)drDet["EAN13"]) &&
                            if ((scCur.sKMC == (string)drDet["KMC"]) &&
                                (scCur.nParty == (string)drDet["NP"]) &&
                                (scCur.dDataIzg.ToString("yyyyMMdd") == (string)drDet["DVR"]))
                                bAfterScan = false;
                        }
                        if (!bAfterScan)
                        {// выгрузка по кнопочке
                            drOpr["STATE"] = AppC.OPR_STATE.OPR_READY;
                            xCUpLoad = new CurUpLoad(xPars);
                            xDP = xCUpLoad.xLP;

                            xCUpLoad.bOnlyCurRow = true;
                            xCUpLoad.drForUpl = drOpr;
                            xCUpLoad.sCurUplCommand = sComm;

                            //xFPan = new FuncPanel(this, this.pnLoadDocG);
                            //EditOverBeforeUpLoad(AppC.RC_OK, 0);

                            if (xPars.OpAutoUpl)
                            {// авто-выгрузка операций
                                string sL = UpLoadDoc(xSE, ref nRet);
                                if (xSE.ServerRet == AppC.RC_OK)
                                    xCDoc.xOper = new CurOper(xCDoc.xDocP.DType);

                                if (nRet != AppC.RC_OK)
                                {
                                    if (nRet == AppC.RC_HALFOK)
                                    {
                                        Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
                                        MessageBox.Show(sL, "Предупреждение!");
                                    }
                                    else
                                        Srv.ErrorMsg(sL, true);
                                }

                                if ((xSE.ServerRet != AppC.EMPTY_INT) &&
                                    (xSE.ServerRet != AppC.RC_OK))
                                {// операция выгрузки не прошла на сервере (содержательная ошибка)
                                    xCDoc.xOper.SetOperDst(null, xCDoc.xDocP.DType);
                                    tDatMC.Text = "";
                                }

                            }
                            else
                                xCDoc.xOper = new CurOper(xCDoc.xDocP.DType);
                            xCUpLoad = null;
                        }
                    }
                }
                else
                    Srv.ErrorMsg("Продукция не определена!");
            }
            return (nRet);
        }

        // обработка адреса-источника определяем содержимое
        private int AdrResult(ScanVarRM xSc, ref PSC_Types.ScDat scD, AddrInfo xA)
        {
            bool
                bIsPoddon = false;
            int
                nRec,
                nRet = AppC.RC_OK;
            DataRow
                dr;
            DialogResult
                dRez;

            nRet = ConvertAdr2Lst(xA, AppC.COM_ADR2CNT, "ROW", false);
            if (nRet == AppC.RC_OK)
            {
                nRec = xCLoad.dtZ.Rows.Count;
                if (nRec == 1)
                {
                    scD = new PSC_Types.ScDat(new ScannerAll.BarcodeScannerEventArgs(ScannerAll.BCId.NoData, ""));
                    //SetVirtScan(xCLoad.dtZ.Rows[0], ref scD, true, false);
                    SetVirtScan(xCLoad.dtZ.Rows[0], ref scD, bIsPoddon, true);
                    scD.nRecSrc = (int)NSI.SRCDET.FROMADR;
                    xCDoc.xOper.SSCC = scD.sSSCC;

                    // Если пришел адрес назначения
                    if ((scD.xOp.xAdrDst != null) && (scD.xOp.xAdrDst.Addr.Length > 0))
                    {
                        xCDoc.xOper.xAdrDst_Srv = scD.xOp.xAdrDst;          // сохранить рекомендации сервера
                        //if (xDestInfo == null)
                        //{
                        //    int
                        //        FontS = 28,
                        //        INFWIN_WIDTH = 230,
                        //        INFWIN_HEIGHT = 90;
                        //    System.Drawing.Rectangle
                        //        recInf,
                        //        screen = Screen.PrimaryScreen.Bounds;

                        //    recInf = new System.Drawing.Rectangle((screen.Width - INFWIN_WIDTH) / 2, 200, INFWIN_WIDTH, INFWIN_HEIGHT);
                        //    xDestInfo = new Srv.HelpShow(this, recInf, 1, FontS, 0);
                        //}
                        //xDestInfo.ShowInfo(new string[] { scD.xOp.xAdrDst.AddrShow }, ref ehCurrFunc);
                    }
                    scD.xOp = (xCDoc.xOper == null) ? new CurOper(xCDoc.xDocP.DType) : xCDoc.xOper;


                    //if ((xCDoc.nTypOp == AppC.TYPOP_DOCUM) ||
                    //    (xCDoc.nTypOp == AppC.TYPOP_KMSN))
                    //{// будет редактирование количества "отсканированной" продукции
                    //    return (AppC.RC_CONTINUE);
                    //}


                    if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
                    {// будет редактирование количества "отсканированной" продукции
                        return (AppC.RC_CONTINUE);
                    }

                    // далее выполняется только для операций

                    if (xCDoc.xDocP.TypOper == AppC.TYPOP_MOVE)
                    {
                        if (scD.tTyp == AppC.TYP_TARA.TARA_PODDON)
                        {// для поддонов редактирования не будет
                            scCur = scD;
                            if (AddDet1(ref scD, out dr))
                            {
                                xCDoc.xOper.bObjOperScanned = true;
                                //SetDetFields(false);
                                if (dr != null)
                                {
                                    drDet = dr;
                                    //dr["SSCC"] = scD.sSSCC;
                                    //xCDoc.xOper.SSCC = scD.sSSCC;
                                }
                            }
                        }
                    }
                    //IsOperReady(false);

                }
                else if (nRec > 1)
                {// ROW - добавление группы
                    if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
                    {
                        dRez = MessageBox.Show(
                            String.Format("Новых строк {0}\nДобавить (Enter)?\n(ESC)- вывод на экран", xCLoad.dtZ.Rows.Count),
                            "Добавление продукции", MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    }
                    else
                    {
                        dRez = DialogResult.Cancel;
                    }
                    if (dRez == DialogResult.OK)
                    {
                        nRet = AddGroupDet(AppC.RC_MANYEAN, (int)NSI.SRCDET.FROMADR, xA.AddrShow);
                    }
                    else
                    {
                        if (AppC.RC_OK == ConvertAdr2Lst(xA, "TXT"))
                        {
                            // справочная информация, просто выводится
                            xInf.Insert(0, String.Format("   === Адрес {0} ===", xA.AddrShow));
                            xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                        }
                    }
                }






            }
            return (nRet);
        }

        private void InfAbout(SavuSocket.SocketStream stmX, Dictionary<string, string> aC,
            DataSet ds, ref string sErr, int nRetSrv)
        {
            bool bMyRead = false;
            List<string> lstI = new List<string>();
            sErr = "Ошибка чтения XML";
            string sXMLFile = "";
            System.IO.StreamReader sr;

            if (stmX.ASReadS.OutFile.Length == 0)
            {
                bMyRead = true;
                stmX.ASReadS.TermDat = AppC.baTermMsg;
                if (stmX.ASReadS.BeginARead(true, 1000 * 60) != SavuSocket.SocketStream.ASRWERROR.RET_FULLMSG)
                    throw new System.Net.Sockets.SocketException(10061);
            }
            sXMLFile = stmX.ASReadS.OutFile;

            sErr = "Ошибка загрузки XML";

            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (sr = new System.IO.StreamReader(sXMLFile))
                {
                    string line;
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        lstI.Add(line);
                    }
                    //string[] aI = new string[lstI.Count];
                    //lstI.CopyTo(aI);
                    //xInf = aI;
                    xInf = lstI;
                    sr.Close();
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (bMyRead)
                    System.IO.File.Delete(sXMLFile);
            }

            sErr = "OK";
        }


        private void LstFromSSCC(SavuSocket.SocketStream stmX, Dictionary<string, string> aC,
            DataSet ds, ref string sErr, int nRetSrv)
        {
            bool
                bMyRead = false;
            string
                sE,
                sXMLFile = "";
            DataTable
                dtL = xCLoad.dtZ;
            System.Xml.XmlReader
                xmlRd = null;

            sErr = "Ошибка чтения XML";

            if (stmX.ASReadS.OutFile.Length == 0)
            {
                bMyRead = true;
                stmX.ASReadS.TermDat = AppC.baTermMsg;
                if (stmX.ASReadS.BeginARead(true, 1000 * 60) != SavuSocket.SocketStream.ASRWERROR.RET_FULLMSG)
                    throw new System.Net.Sockets.SocketException(10061);
            }
            sXMLFile = stmX.ASReadS.OutFile;

            sErr = "Ошибка загрузки XML";
            dtL.BeginInit();
            dtL.BeginLoadData();
            dtL.Clear();
            try
            {
                xmlRd = System.Xml.XmlReader.Create(sXMLFile);
                dtL.ReadXml(xmlRd);
            }
            catch (Exception e)
            {
                int i = dtL.Rows.Count;
                sE = "";
                if (i-- > 0)
                {
                    sE = String.Format("\nПоследняя({0}) строка:\n{1} {2}", i + 1, dtL.Rows[i]["KMC"], dtL.Rows[i]["SNM"]);
                    WriteProt(sE);
                }
                Srv.ErrorMsg(e.Message + sE, "Ошибка в данных", true);
            }
            finally
            {
                if (xmlRd != null)
                    xmlRd.Close();
            }

            if (bMyRead)
                System.IO.File.Delete(sXMLFile);
            dtL.EndLoadData();
            dtL.EndInit();
            if (dtL.Rows.Count < 1)
            {
                if (xCLoad.sComLoad == AppC.COM_ADR2CNT)
                {
                }
                else
                    throw new Exception("Нет данных");
            }
            try
            {
                sErr = aC["MSG"];
            }
            catch
            {
                sErr = "OK";
            }
        }

        private AddrInfo WhatAdr4Inf()
        {
            bool
                bAsk = false;
            DialogResult
                drQ;
            AddrInfo 
                xA = null;

            if (xCDoc.xOper.nOperState == AppC.OPR_STATE.OPR_EMPTY)
            {// операция еще не начиналась, смотрим по таблице
                try
                {
                    if ((drDet["ADRFROM"] is string) && (scCur.xOp.xAdrSrc.nType < ADR_TYPE.VIRTUAL) && (scCur.xOp.xAdrSrc.Addr.Length > 0))
                    {

                        if ((drDet["ADRTO"] is string) && (scCur.xOp.xAdrDst.nType < ADR_TYPE.VIRTUAL) && (scCur.xOp.xAdrDst.Addr.Length > 0))
                            bAsk = true;
                        else
                            xA = new AddrInfo((string)drDet["ADRFROM"], xSm.nSklad);
                    }
                    else
                    {
                        if ((drDet["ADRTO"] is string) && (scCur.xOp.xAdrDst.nType < ADR_TYPE.VIRTUAL) && (scCur.xOp.xAdrDst.Addr.Length > 0))
                            xA = new AddrInfo((string)drDet["ADRTO"], xSm.nSklad);
                    }
                }
                catch
                {
                }
                if (bAsk)
                {
                    drQ = MessageBox.Show("\"Источник\" - Yes\n\"Приемник\" - No\nОтмена - Cancel",
                        "Какой адрес использовать?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    if (drQ == DialogResult.Yes)
                        xA = new AddrInfo((string)drDet["ADRFROM"], xSm.nSklad);
                    else if (drQ == DialogResult.No)
                        xA = new AddrInfo((string)drDet["ADRTO"], xSm.nSklad);
                }
            }
            else
            {
                if (((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_SRC_SET) > 0) && (xCDoc.xOper.xAdrSrc.nType < ADR_TYPE.VIRTUAL))
                {// источник задан
                    if (((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_DST_SET) > 0) && (xCDoc.xOper.xAdrDst.nType < ADR_TYPE.VIRTUAL))
                        bAsk = true;
                    else
                        xA = xCDoc.xOper.xAdrSrc;
                }
                else
                {
                    if (((xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_DST_SET) > 0) && (xCDoc.xOper.xAdrDst.nType < ADR_TYPE.VIRTUAL))
                        xA = xCDoc.xOper.xAdrDst;
                }
                if (bAsk)
                {
                    drQ = MessageBox.Show("\"Источник\" - Yes\n\"Приемник\" - No\nОтмена - Cancel",
                        "Какой адрес использовать?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    if (drQ == DialogResult.Yes)
                        xA = xCDoc.xOper.xAdrSrc;
                    else if (drQ == DialogResult.No)
                        xA = xCDoc.xOper.xAdrDst;
                }
            }
            return(xA);
        }



        private int ConvertAdr2Lst(AddrInfo xAdrSrc, string sTypeInf)
        {
            return (ConvertAdr2Lst(xAdrSrc, AppC.COM_CELLI, sTypeInf, true));
        }

        private int ConvertAdr2Lst(AddrInfo xAdrSrc, string sCOM, string sTypeInf, bool bProceedResult)
        {
            return (ConvertAdr2Lst(xAdrSrc, sCOM, sTypeInf, bProceedResult, NSI.SRCDET.FROMADR, ref ehCurrFunc));
        }


        private int ConvertAdr2Lst(AddrInfo xAdrSrc, string sCOM, string sTypeInf, bool bProceedResult, NSI.SRCDET srcAdd)
        {
            return (ConvertAdr2Lst(xAdrSrc, sCOM, sTypeInf, bProceedResult, srcAdd, ref ehCurrFunc));
        }

        //private int ConvertAdr2Lst(AddrInfo xAdrSrc, string sCOM, string sTypeInf, bool bProceedResult)
        private int ConvertAdr2Lst(AddrInfo xAdrSrc, string sCOM, string sTypeInf, bool bProceedResult, NSI.SRCDET srcAdd, ref Srv.CurrFuncKeyHandler kbh)
        {
            int
                nRet = AppC.RC_OK;
            string
                sNom = "",
                sQ;

            DataRow
                dr;
            DataSet
                dsTrans = null;
            PSC_Types.ScDat
                scD;

            LoadFromSrv
                dgL = null;
            ServerExchange
                xSE = new ServerExchange(this);

            // буфер для приема данных с сервера
            MakeTempDOUTD(xNSI.DT[NSI.BD_DOUTD].dt);

            xCLoad = new CurLoad();
            xCLoad.xLP.lSysN = xCDoc.nId;
            xCLoad.dtZ = dtL;

            sQ = String.Format("(KSK={0},ADRCELL={1},TYPE={2}", xSm.nSklad, xAdrSrc.Addr, sTypeInf);
            switch (sCOM)
            {
                case AppC.COM_CELLI:
                    dgL = (sTypeInf == "TXT") ? new LoadFromSrv(InfAbout) : new LoadFromSrv(LstFromSSCC);
                    break;
                //case AppC.COM_A4MOVE:
                //    //sQ += ",TIMECR=" + xAdrSrc.ScanDT.ToString("s");

                //    sNom = (xSm.Curr4Invent > 0) ?
                //        String.Format(",ND={0}", xSm.Curr4Invent) :
                //        "";
                //    sQ += String.Format(",TIMECR={0}{1}", xAdrSrc.ScanDT.ToString("s"), sNom);


                //    dgL = new LoadFromSrv(LstFromSSCC);
                //    break;
                case AppC.COM_ADR2CNT:

                    xCUpLoad = new CurUpLoad(xPars);
                    xCUpLoad.sCurUplCommand = AppC.COM_ADR2CNT;

                    dsTrans = xNSI.MakeWorkDataSet(xNSI.DT[NSI.BD_DOCOUT].dt,
                              xNSI.DT[NSI.BD_DOUTD].dt, new DataRow[] { xCDoc.drCurRow }, null, xSm, xCUpLoad);


                    dgL = new LoadFromSrv(LstFromSSCC);
                    break;
            }

            sQ += ")";

            xCLoad.sComLoad = sCOM;
            string sL = xSE.ExchgSrv(sCOM, sQ, "", dgL, dsTrans, ref nRet, 20);

            if (sCOM == AppC.COM_ADR2CNT)
            {
                if (dtL.Rows.Count > 0)
                    nRet = TestProdBySrv(xSE, nRet);

                if (nRet == AppC.RC_OK)
                {
                    sNom = "";
                    if (xSE.ServerRet != AppC.EMPTY_INT)
                    {
                        if (xSE.ServerAnswer.ContainsKey("ND"))
                            sNom = xSE.ServerAnswer["ND"];
                        else if (xSE.AnswerPars.ContainsKey("ND"))
                            sNom = xSE.AnswerPars["ND"];

                        try
                        {
                            xSm.Curr4Invent = int.Parse(sNom);
                        }
                        catch
                        {
                            xSm.Curr4Invent = 0;
                        }
                    }
                }
            }

            if (nRet == AppC.RC_OK)
            {
                if (!bProceedResult)
                    return (nRet);

                if (sTypeInf == "TXT")
                {// справочная информация, просто выводится
                    xInf.Insert(0, String.Format("   === Адрес {0} ===", xAdrSrc.AddrShow));

                    xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                }
                else
                {
                    if (sTypeInf == "MOV")
                    {
                        scD = new PSC_Types.ScDat(new ScannerAll.BarcodeScannerEventArgs(ScannerAll.BCId.NoData, ""));
                        SetVirtScan(xCLoad.dtZ.Rows[0], ref scD, true, false);
                        AddDet1(ref scD, out dr);
                        if (dr != null)
                        {
                            dr["SSCC"] = scD.sSSCC;
                            xCDoc.xOper.SSCC = scD.sSSCC;
                            scD.nRecSrc = (int)srcAdd;
                            xCDoc.xOper.bObjOperScanned = true;
                            drDet = dr;
                        }
                        //IsOperReady(false);

                        // Если пришел адрес назначения
                        if ((scD.xOp.xAdrDst != null) && (scD.xOp.xAdrDst.Addr.Length > 0))
                        {
                            xCDoc.xOper.xAdrDst_Srv = scD.xOp.xAdrDst;          // сохранить рекомендации сервера
                            //if (xDestInfo == null)
                            //{
                            //    int
                            //        FontS = 28,
                            //        INFWIN_WIDTH = 230,
                            //        INFWIN_HEIGHT = 90;
                            //    Rectangle
                            //        recInf,
                            //        screen = Screen.PrimaryScreen.Bounds;

                            //    recInf = new Rectangle((screen.Width - INFWIN_WIDTH) / 2, 200, INFWIN_WIDTH, INFWIN_HEIGHT);
                            //    xDestInfo = new Srv.HelpShow(this, recInf, 1, FontS, 0);
                            //}
                            //xDestInfo.ShowInfo(new string[] { scD.xOp.xAdrDst.AddrShow }, ref ehCurrFunc);
                        }
                    }
                    else
                    {// ROW - добавление группы
                        DialogResult dRez = MessageBox.Show(
                            String.Format("Новых строк {0}\nДобавить (Enter)?\n(ESC)- отменить", xCLoad.dtZ.Rows.Count),
                            "Добавление продукции", MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                        if (dRez == DialogResult.OK)
                        {
                            nRet = AppC.RC_MANYEAN;
                            AddGroupDet(nRet, (int)srcAdd, xAdrSrc.Addr);
                        }
                    }
                }
            }
            else
            {
                Srv.ErrorMsg(sL);
            }

            return (nRet);
        }

        private bool SetVirtScan(DataRow dr, ref PSC_Types.ScDat scD, bool bIsPallet, bool bEmkInf)
        {
            bool
                bRet = true;
            int
                nPrzPl = -1;
            //DataRow drNSI;

            try
            {
                scD.nRecSrc = (int)NSI.SRCDET.FROMADR;
                scD.tTyp = (bIsPallet) ? AppC.TYP_TARA.TARA_PODDON : AppC.TYP_TARA.TARA_TRANSP;

                scD.sKMC = (dr["KMC"] is string) ? (string)dr["KMC"] : "";
                scD.sEAN = dr["EAN13"].ToString();
                scD.nKrKMC = (dr["KRKMC"] is int) ? (int)dr["KRKMC"] : 0;

                if (scD.sKMC.Length > 0)
                {
                    bRet = scD.GetFromNSI(scD.s,
                        xNSI.DT[NSI.NS_MC].dt.Rows.Find(new object[] { scD.sKMC }),
                        ref nPrzPl);
                }
                else
                {
                    bRet = xNSI.GetMCDataOnEAN(scD.sEAN, ref scD, false);
                }

                try { scD.nMest = (int)dr["KOLM"]; }
                catch { scD.nMest = 0; }

                try { scD.fVsego = scD.fVes = (FRACT)dr["KOLE"]; }
                catch { scD.fVsego = scD.fVes = 0; }

                if (bRet)
                {
                    if (bIsPallet)
                        scD.nMestPal = scD.nMest;

                    if (dr["DVR"] is string)
                    {
                        scD.sDataIzg = dr["DVR"].ToString();
                        scD.dDataIzg = DateTime.ParseExact(scD.sDataIzg, "yyyyMMdd", null);
                        scD.sDataIzg = scD.dDataIzg.ToString("dd.MM.yy");
                    }
                    if (dr["DTG"] is string)
                    {
                        scD.sDataGodn = dr["DTG"].ToString();
                        scD.dDataGodn = DateTime.ParseExact(scD.sDataGodn, "yyyyMMdd", null);
                        scD.sDataGodn = scD.dDataIzg.ToString("dd.MM.yy");
                    }

                    //scD.fEmk = (FRACT)dr["EMK"];
                    try { scD.nParty = (string)dr["NP"]; }
                    catch { scD.nParty = "";}

                    try { scD.nNomPodd = (int)dr["NPODD"]; }
                    catch { scD.nNomPodd = 0; }

                    try { scD.nNomMesta = (int)dr["NMESTA"]; }
                    catch { scD.nNomMesta = 0; }

                    //scD.nNPredMT = (dr["SYSPRD"] is int) ? ((int)dr["SYSPRD"]) : 0;
                }
                if ((dr["ADRFROM"] is string) && (((string)dr["ADRFROM"]).Length > 0))
                    //scD.xOp.xAdrDst = new AddrInfo((string)dr["ADRTO"], xNSI.AdrName((string)dr["ADRTO"]));
                    scD.xOp.SetOperSrc(new AddrInfo((string)dr["ADRFROM"], xSm.nSklad), xCDoc.xDocP.DType);
                if ((dr["ADRTO"] is string) && (((string)dr["ADRTO"]).Length > 0))
                    //scD.xOp.xAdrDst = new AddrInfo((string)dr["ADRTO"], xNSI.AdrName((string)dr["ADRTO"]));
                    scD.xOp.SetOperDst(new AddrInfo((string)dr["ADRTO"], xSm.nSklad), xCDoc.xDocP.DType);

                //scD.fEmk = (FRACT)dr["EMK"];
                if (dr["SSCC"] is string)
                    scD.sSSCC = (string)dr["SSCC"];

                scD.nKolSht = (dr["KOLSH"] is int) ? (int)dr["KOLSH"] : 0;

                scD.nZaklMT = (dr["NZAKL"] is int) ? ((int)dr["NZAKL"]) : 0;

            }
            catch
            {
            }
            return (bRet);
        }


        private int AddGroupDet(int nRLoad, int nRowSource, string sAddInf)
        {
            bool
                bRet;
            int
                nRet = AppC.RC_OK,
                nRec;
            string
                sK = "",
                sE = "";
            DataRow
                d = null;

            if ((nRLoad == AppC.RC_MANYEAN) || (nRLoad == AppC.RC_OK))
            {// Добавление группы строк (скомплектованный поддон)
                if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        nRec = xCLoad.dtZ.Rows.Count;
                        for (int i = 0; i < nRec; i++)
                        {

                            sE = xCLoad.dtZ.Rows[i]["EAN13"].ToString();
                            PSC_Types.ScDat scMD = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.EAN13,
                                sE));
                            if (SetVirtScan(xCLoad.dtZ.Rows[i], ref scMD, false, false))
                            {
                                scMD.nRecSrc = nRowSource;
                                if (nRowSource == (int)NSI.SRCDET.SSCCT)
                                {
                                    scMD.sSSCC = sAddInf;
                                }
                                scCur = scMD;
                                //TryEvalNewZVKTTN(ref scMD, false);
                                //AddDet1(ref scMD, out d);
                                TryEvalNewZVKTTN(ref scCur, false);
                                AddDet1(ref scCur, out d);

                            }
                            else
                            {
                                nRet = AppC.RC_NOEAN;
                                sK = xCLoad.dtZ.Rows[i]["KMC"].ToString();
                                break;
                            }
                        }
                    }
                    catch { }
                    finally 
                    {
                        Cursor.Current = Cursors.Default;
                    }
                    if (nRet != AppC.RC_OK)
                    {
                        Srv.ErrorMsg(String.Format("Не найден KMC={0}\nEAN={1}", sK, sE), sAddInf, true);
                    }
                }
            }
            else
            {// дальнейшую обработку сканирования прекращаем
                nRet = AppC.RC_OK;
            }

            return (nRet);
        }


        private int TestProdBySrv(ref PSC_Types.ScDat sc)
        {
            ServerExchange
                xSE = new ServerExchange(this);
            int
                nRet = ServConfScan(ref sc, xSE);
            return (TestProdBySrv(xSE, nRet));
        }


        private int TestProdBySrv(ServerExchange xSE, int nRet)
        {
            string
                sH = "Отгрузка запрещена!",
                sMess = "";
            bool
                b4biddScan = true;

            if (nRet != AppC.RC_OK)
            {
                if (xSE.ServerRet != AppC.EMPTY_INT)
                {// Ответ от сервера получить удалось
                    try
                    {
                        sMess = xSE.ServerAnswer["MSG"];
                    }
                    catch
                    {
                        sMess = "Недопустимая продукция!";
                    }

                    if ((xSE.ServerRet == AppC.RC_HALFOK) || (true))
                    {// сервер желает пообщаться
                        if (sMess.Length == 0)
                            sMess = "Недопустимая продукция!";
                        sMess += "\n(OK-отказ)\n(ESC-отгрузить)";

                        Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                        DialogResult drQ = MessageBox.Show(sMess, sH,
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                        if (drQ != DialogResult.OK)
                        {
                            b4biddScan = false;
                            nRet = AppC.RC_OK;
                        }
                        else
                        {// Enter - отказ от сканирования, выводить ничего не надо
                            sMess = "";
                        }
                    }
                    if (b4biddScan)
                    {
                        //if (sMess.Length > 0)
                        //Srv.ErrorMsg(sMess, sH, true);
                        //break;
                    }
                }
                else
                {
                    sMess = "Сервер недоступен!";
                    sMess += "\n(OK-отказ)\n(ESC-продолжить ввод)";

                    DialogResult drQ = MessageBox.Show(sMess, "Ошибка!",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    if (drQ != DialogResult.OK)
                    {
                        b4biddScan = false;
                        nRet = AppC.RC_OK;
                    }
                    else
                    {// Enter - отказ от сканирования, выводить ничего не надо
                    }

                }
            }





            return (nRet);
        }





        // получить от сервера подтверждение на продукцию
        private int ServConfScan(ref PSC_Types.ScDat scD, ServerExchange xSE)
        {
            int nRet = AppC.RC_OK;
            string sErr = "";
            DataSet dsTrans;
            DataRow[] drD = null;

            MakeTempDOUTD(xNSI.DT[NSI.BD_DOUTD].dt);

            drD = new DataRow[1] { xNSI.AddDet(scD, xCDoc, null, false) };

            xCUpLoad = new CurUpLoad(xPars);
            xCUpLoad.sCurUplCommand = AppC.COM_CHKSCAN;

            dsTrans = xNSI.MakeWorkDataSet(xNSI.DT[NSI.BD_DOCOUT].dt,
                      xNSI.DT[NSI.BD_DOUTD].dt, new DataRow[] { xCDoc.drCurRow }, drD, xSm, xCUpLoad);

            sErr = xSE.ExchgSrv(AppC.COM_CHKSCAN, "", "", null, dsTrans, ref nRet);
            if (xSE.ServerRet != AppC.EMPTY_INT)
            {// Ответ от сервера получить удалось
                nRet = xSE.ServerRet;
                if (nRet != AppC.RC_OK)
                {// И он оказался не очень-то...
                    //bRet = AppC.RC_CANCELB;
                    //Srv.ErrorMsg(sErr, true);
                }
            }

            Back2Main();

            return (nRet);
        }

        // получить от сервера подтверждение на продукцию
        public int ConfScanOrNot(DataRow dr, bool AppPar4ConfScan)
        {
            int
                t,
                nRet = 0;

            object
                x;
            ExprDll.Action
                xFind;

            try
            {
                xFind = xGExpr.run.FindFunc(AppC.FEXT_CONF_SCAN);
                if (xFind != null)
                {
                    x = xGExpr.run.ExecFunc(AppC.FEXT_CONF_SCAN, new object[] { dr, AppPar4ConfScan });

                    if (x is int)
                        nRet = (int)x;
                    else
                        nRet = 0;
                }
                else
                {
                    if (AppPar4ConfScan)
                    {// установлен флаг запроса подтверждения
                        if ((int)dr["TYPOP"] == AppC.TYPOP_DOCUM)
                        {
                            t = (int)dr["TD"];
                            if ((t >= 0) && (t <= 3))
                            {
                                nRet = 1;
                            }
                        }
                    }
                    else
                    {
                        nRet = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Srv.ErrorMsg(ex.Message);
                nRet = 0;
            }
            return (nRet);
        }

        private void GetKMCInf(int nForZVK)
        {
            int 
                nRet = AppC.RC_CANCEL;
            string
                sQ,
                sCom = AppC.COM_KMCI,
                sErr;
            ServerExchange 
                xSE = new ServerExchange(this);

            try
            {
                sQ = String.Format("(KSK={0}", xSm.nSklad);
                //sQ = String.Format(sQ + ",KMC={0}", scCur.drMC["KMC"]);
                sQ = String.Format(sQ + ",KMC={0}", scCur.sKMC);
                if (nForZVK > 0)
                {
                    if (scCur.nParty.Length > 0)
                        sQ = String.Format(sQ + ",NP={0}", scCur.nParty);
                    if (scCur.dDataIzg != DateTime.MinValue)
                        sQ = String.Format(sQ + ",DVR={0}", scCur.dDataIzg.ToString("yyyyMMdd"));
                    if (scCur.dDataGodn != DateTime.MinValue)
                        sQ = String.Format(sQ + ",DTG={0}", scCur.dDataGodn.ToString("yyyyMMdd"));
                    if (scCur.fVsego > 0)
                        sQ = String.Format(sQ + ",KOLE={0}", scCur.fVsego);
                }

                sQ += ")";

                LoadFromSrv dgL = new LoadFromSrv(InfAbout);
                sErr = xSE.ExchgSrv(sCom, sQ, "", dgL, null, ref nRet, 60);
                if (xSE.ServerRet == AppC.RC_OK)
                {
                    int 
                        i = 0;
                    List<string> lN = aKMCName(scCur.sN, false);
                    while (i < lN.Count)
                    {
                        xInf.Insert(i, lN[i]);
                        i++;
                    }
                    xInf.Insert(i, " ".PadRight(32,'-'));
                    xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                }
                else
                    Srv.ErrorMsg(sErr, "Ошибка!", true);
            }
            catch(Exception exx)
            {
                int ggg = 8899;
            }
        }

        private List<string> aKMCName(string sN, bool bRazd)
        {
            return(aKMCName(sN, bRazd, '-'));
        }

        private List<string> aKMCName(string sN, bool bRazd, char cM)
        {
            int
                l;
            string
                ss = sN;
            List<string>
                aS = new List<string>();

            while (ss.Length > 0)
            {
                l = (ss.Length >= 33) ? 33 : ss.Length;
                aS.Add(ss.Substring(0, l));
                ss = ss.Substring(l);
            }
            if (bRazd)
                aS.Add(" ".PadRight(32, cM));
 
            return(aS);
        }

    }
}    
