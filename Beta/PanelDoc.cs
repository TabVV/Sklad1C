using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;

using SavuSocket;
using ScannerAll;
using PDA.OS;
using PDA.Service;
using PDA.BarCode;

using FRACT = System.Decimal;


namespace SkladRM
{
    public partial class MainF : Form
    {
        // флаг первого входа в документы
        private bool b1stEner = true;

        // текущая функция работы с документами
        private int nCurDocFunc = AppC.DT_SHOW;

        // при активации панели документов
        private void EnterInDoc(){
            FRACT f;

            if (b1stEner == true)
                Only1st();
            if (nCurDocFunc == AppC.DT_SHOW)            // в режиме просмотра
            {
                //StatAllDoc();
                if (xCDoc.drCurRow != null)
                {// есть запись для отображения
                    xCDoc.drCurRow["MEST"] = TotMest(NSI.REL2TTN, null);
                }
                dgDoc.Focus();
            }
        }


        // только 1-й раз призапуске программы
        private void Only1st()
        {
            string s;

            b1stEner = false;
            s = (xSm.sUser == AppC.SUSER) ? "Admin":
                (xSm.sUser == AppC.GUEST) ? "Кладовщик" : xSm.sUser;
            lInfDocWidLeft.Text = s;

            xCDoc = new CurDoc(xSm, AppC.DT_ADDNEW);
            //DataView dvMaster = ((DataTable)dgDoc.DataSource).DefaultView;
            //if (dvMaster.Count > 0)
                
            if (xSm.nDocs > 0)
            {// есть записи для просмотра
                RestShowDoc(false);
            }
        }


        private DateTime UnPackDate(string sDate4)
        {
            int
                nMonth,
                nDay;
            DateTime
                dDoc = xCDoc.xDocP.dDatDoc;
            try
            {
                char[] aCh = sDate4.ToCharArray();
                nDay = Convert.ToInt32(aCh[3]);
                nDay = (nDay >= 65) ? nDay - 55 : nDay - 48;
                nMonth = Convert.ToInt32(aCh[2]);
                nMonth = (nMonth >= 65) ? nMonth - 55 : nMonth - 48;
                dDoc = DateTime.ParseExact(sDate4.Substring(0, 2) + "0101", "yyMMdd", null);
                dDoc = dDoc.AddMonths(nMonth - 1);
                dDoc = dDoc.AddDays(nDay - 1);
            }
            catch
            {
                dDoc = DateTime.MinValue;
            }
            return(dDoc);
        }

        private void ProceedScanDoc(ScanVarRM xSc, ref PSC_Types.ScDat s)
        {
            bool
                bPrefixPresent;
            int
                nNumCode = 0,
                nNomDoc,
                nLen;
            string
                sSymCode,
                sPackedDate,
                sDBC = "", 
                sMLBC = "",
                sNomDoc = "",
                sDocBarCode = s.s;
            DateTime
                dDoc = xCDoc.xDocP.dDatDoc;

            if ((s.ci == ScannerAll.BCId.Code128) || (s.ci == ScannerAll.BCId.Interleaved25))
            {
                nLen = sDocBarCode.Length;
                do
                {
                    if ((xScan.bcFlags & ScanVarRM.BCTyp.SP_SSCC) > 0)
                    {// загрузка содержимого SSCC
                        break;
                    }

                    if ((nLen >= 8) && (nLen <= 28))
                    {// возможен штрих-код документа
                        sSymCode = sDocBarCode.Substring(0, 2);        // код типа
                        bPrefixPresent = AppC.xDocTDef.ContainsKey(sSymCode);
                        if (bPrefixPresent)
                        {// это точно штрих-код документа!

                            sPackedDate = sDocBarCode.Substring(3, 4);
                            dDoc = UnPackDate(sPackedDate);
                            nNumCode = AppC.xDocTDef[sSymCode].NumCode;
                            if (nNumCode == AppC.TYPD_SVOD)
                            {
                                sMLBC = sDocBarCode;
                            }
                            else
                            {
                                sDBC = sDocBarCode;
                            }

                            try
                            {
                                sNomDoc = sDocBarCode.Substring(sDocBarCode.Length - 6, 6);
                                nNomDoc = int.Parse(sNomDoc);
                            }
                            catch
                            {
                                nNomDoc = 0;
                            }

                            sNomDoc = (nNomDoc > 0) ? nNomDoc.ToString() : "";

                        }
                        else
                        {
                            try
                            {
                                sDBC = sDocBarCode;
                                int nDefis = sDocBarCode.IndexOf("-");
                                dDoc = DateTime.ParseExact(sDocBarCode.Substring(nDefis + 1, 10), "dd.MM.yyyy", null);
                                sSymCode = "RN";
                                nNumCode = AppC.xDocTDef[sSymCode].NumCode;
                                sDBC = sDocBarCode;
                                try
                                {
                                    sNomDoc = sDocBarCode.Substring(0, nDefis);
                                    nNomDoc = int.Parse(sNomDoc);
                                    bPrefixPresent = true;
                                }
                                catch
                                {
                                    nNomDoc = 0;
                                }
                                sNomDoc = (nNomDoc > 0) ? nNomDoc.ToString() : "";
                            }
                            catch
                            {
                            }
                        }

                        if (bEditMode)
                        {// документ уже вводится
                            xCDoc.xDocP.sBC_ML = tBCML.Text = sMLBC;
                            xCDoc.xDocP.sBC_Doc = tBCDoc.Text = sDBC;
                            if (aEdVvod.Current == tKT_p)
                            {
                                xCDoc.xDocP.nNumTypD = nNumCode;
                                //xCDoc.xDocP.sTypD = AppC.xDocTDef[sK].Name;
                                tKT_p.Text = xCDoc.xDocP.nNumTypD.ToString();
                                tNT_p.Text = AppC.xDocTDef[sSymCode].Name;
                                if (xCDoc.xDocP.sNomDoc.Length == 0)
                                {
                                    xCDoc.xDocP.sNomDoc = sNomDoc;
                                    tNom_p.Text = xCDoc.xDocP.sNomDoc;
                                }
                                xCDoc.xDocP.dDatDoc = dDoc;
                                aEdVvod.SetCur(aEdVvod[aEdVvod.Count - 1]);
                            }
                        }
                        else
                        {// пробуем загрузку документа

                            xCLoad = new CurLoad(AppC.UPL_FLT);
                            if (bPrefixPresent)
                            {
                                xCLoad.xLP.nNumTypD = nNumCode;
                            }
                            xCLoad.xLP.sNomDoc = sNomDoc;
                            xCLoad.xLP.dDatDoc = dDoc;
                            xCLoad.xLP.sBC_Doc = sDBC;
                            xCLoad.xLP.sBC_ML = sMLBC;
                            LoadDocFromServer(AppC.F_INITRUN, new KeyEventArgs(Keys.Enter), ref ehCurrFunc);
                        }

                    }
                } while (false);
            }
        }


        // обработка функций и клавиш на панели
        private bool Doc_KeyDown(int nFunc, KeyEventArgs e)
        {
            bool 
                ret = false;

            if (nFunc > 0)
            {
                if (bEditMode == false)
                {
                    switch (nFunc)
                    {
                        case AppC.F_ADD_REC:            // добавление новой
                        case AppC.F_CHG_REC:            // корректировка
                            AddOrChangeDoc(nFunc);
                            ret = true;
                            break;
                        case AppC.F_DEL_ALLREC:         // удаление всех
                        case AppC.F_DEL_REC:            // или одного
                            DelDoc(nFunc);
                            //StatAllDoc();
                            ret = true;
                            break;
                        case AppC.F_TOT_MEST:
                            // всего мест по накладная/заявка
                            ShowTotMest();
                            ret = true;
                            break;
                        case AppC.F_CTRLDOC:
                            // контроль текущего документа
                            ControlDocs(AppC.F_INITREG, null, ref ehCurrFunc);
                            ret = true;
                            break;
                        case AppC.F_GOFIRST:
                        case AppC.F_GOLAST:
                            //CurrencyManager cmDoc = (CurrencyManager)BindingContext[dgDoc.DataSource];
                            //if (cmDoc.Count > 0)
                            //    cmDoc.Position = (nFunc == AppC.F_GOFIRST) ? 0 : cmDoc.Count - 1;
                            Go1stLast(dgDoc, nFunc);
                            ret = true;
                            break;
                        //case AppC.F_CHGSCR:
                        //    // смена экрана
                        //    xScrDoc.NextReg(AppC.REG_SWITCH.SW_NEXT);
                        //    ret = true;
                        //    break;
                        case AppC.F_FLTVYP:
                            // установка фильтра по выгруженным
                            xPars.bHideUploaded = !xPars.bHideUploaded;
                            FiltForDocs(xPars.bHideUploaded, xNSI.DT[NSI.BD_DOCOUT]);
                            ret = true;
                            break;
                        case AppC.F_CHG_GSTYLE:
                        case AppC.F_LOADKPL:
                            xCLoad = new CurLoad(AppC.UPL_FLT);
                            if (LoadKomplLst(null, AppC.F_LOADKPL))
                            {
                                xCLoad.drPars4Load = null;
                                xDLLPars = AppC.F_LOADKPL;
                                DialogResult xDRslt = CallDllForm(sExeDir + "SGPF-Kompl.dll", false);
                                if (xCLoad.drPars4Load != null)
                                {
                                    xCLoad.sSSCC = "";
                                    LoadKomplLst(xCLoad.drPars4Load, AppC.F_LOADKPL);
                                }
                            }
                            ret = true;
                            break;
                        case AppC.F_LOADOTG:
                            xCLoad = new CurLoad(AppC.UPL_FLT);
                            if (LoadKomplLst(null, AppC.F_LOADOTG))
                            {
                                xCLoad.drPars4Load = null;
                                xDLLPars = AppC.F_LOADOTG;
                                DialogResult xDRslt = CallDllForm(sExeDir + "SGPF-Kompl.dll", false);

                                if (xCLoad.drPars4Load != null)
                                {
                                    LoadKomplLst(xCLoad.drPars4Load, AppC.F_LOADOTG);
                                }
                            }
                            ret = true;
                            break;
                        //case AppC.F_SAMEKMC:
                        //    // смена режима Документальный/Операционный
                        //    if (xSm.RegApp == AppC.REG_DOC)
                        //    {
                        //        xSm.RegApp = AppC.REG_OPR;
                        //        sMsg = "Операционный установлен";
                        //        lPoluch.Text = "Операция";
                        //    }
                        //    else
                        //    {
                        //        xSm.RegApp = AppC.REG_DOC;
                        //        sMsg = "Документальный установлен";
                        //        lPoluch.Text = "Получ-ль";
                        //    }
                        //    //StatAllDoc();
                        //    Srv.PlayMelody(W32.MB_4HIGH_FLY);
                        //    MessageBox.Show(sMsg, "Смена режима");
                        //    ret = true;
                        //    break;
                    }
                }
            }
            else
            {
                switch (e.KeyValue)
                {
                    case W32.VK_ENTER:
                        if (nCurDocFunc == AppC.DT_SHOW)
                        {
                            if (xCDoc.drCurRow != null)
                            {
                                tcMain.SelectedIndex = PG_SCAN;
                                ret = true;
                            }
                        }
                            break;
                }
            }
            e.Handled |= ret;
            return (ret);

        }


        // проверка параметров перед записью
        private bool VerifyPars(DocPars xP, int nF, ref object xErr)
        {
            bool 
                ret = false;
            string 
                sE = "";

            // для всех типов документов

            do
            {
                if (nF == AppC.F_LOAD_DOC)
                {
                    if (xP.sBC_Doc.Length > 0)
                    {
                        ret = true;
                        break;
                    }
                }
                if (xP.nNumTypD == AppC.EMPTY_INT)
                {
                    sE = "Ошибочный тип!";
                    xErr = tKT_p;
                }
                if (xP.dDatDoc == DateTime.MinValue)
                {
                    sE = "Ошибочная дата!";
                    break;
                }
                if (xP.nSklad == AppC.EMPTY_INT)
                {
                    sE = "Склад не указан!";
                    xErr = tKSkl_p;
                }
                ret = true;
            } while (false);
            
            //if ((xP.nTypD != AppC.EMPTY_INT) && (xP.dDatDoc != DateTime.MinValue))
            //{// смотрим дальше
            //    if (xP.nSklad != AppC.EMPTY_INT)
            //    {// склад имеется
            //        ret = true;
            //    }
            //    else
            //    {
            //        sE = "Склад не указан!";
            //        xErr = tKSkl_p;
            //    }
            //}
            //else
            //{
            //    if (nF == AppC.F_LOAD_DOC)
            //    {
            //        if (xP.sBC_Doc.Length == 0)
            //        {
            //        }
            //        else
            //        {
            //            ret = true;
            //        }
            //    }
            //    else
            //    {
            //    }
            //}

            if ((ret == false) || (sE.Length > 0))
            {
                Srv.ErrorMsg(sE);
            }

            xErr = null;
            return (ret);
        }




        // возврат в режим просмотра
        private void RestShowDoc(bool bGoodBefore)
        {
            //tStat_Reg.Text = "Просмотр";
            nCurDocFunc = AppC.DT_SHOW;
            if (bGoodBefore == false)
            {// предыдущая операция неудачная, перечитать запись (если есть)
                DataView dvMaster = ((DataTable)dgDoc.DataSource).DefaultView;

                if (dvMaster.Count > 0)
                {// есть записи для просмотра
                    xCDoc.drCurRow = dvMaster[dgDoc.CurrentRowIndex].Row;
                    xNSI.InitCurDoc(xCDoc, xSm);
                }
                else
                    xCDoc.xDocP = new DocPars(AppC.DT_ADDNEW);
                SetParFields(xCDoc.xDocP);
            }
            dgDoc.Focus();
        }



        /// *** Функции работы с документами
        /// 

        // добавление новой или изменение старой
        // nReg - требуемый режим
        private void AddOrChangeDoc(int nFunc)
        {
                CTRL1ST 
                    FirstC = CTRL1ST.START_AVAIL;

                if (nFunc == AppC.F_ADD_REC)
                {// вход в режим добавления новой записи
                    xCDoc = new CurDoc(xSm, AppC.DT_ADDNEW);
                    //tStat_Reg.Text = "Новый";
                    if (xSm.RegApp == AppC.REG_DOC)
                        FirstC = CTRL1ST.START_EMPTY;
                }
                else
                {// вход в режим корректировки записи
                    if (xCDoc.drCurRow == null)
                        return;
                    //tStat_Reg.Text = "Корр-ка";
                }
                EditPars(nFunc, xCDoc.xDocP, FirstC, VerifyDoc, EditFieldsIsOver);
        }

        // проверка введенных значений
        private AppC.VerRet VerifyDoc()
        {
            AppC.VerRet v;
            v.nRet = AppC.RC_OK;
            object xErr = null;
            bool bRet = VerifyPars(xCDoc.xDocP, nCurFunc, ref xErr);
            if (bRet != true)
                v.nRet = AppC.RC_CANCEL;
            else
            {
                //bQuitEdPars = true;
                //if (xCDoc.xDocP.nTypD == AppC.TYPD_RASHNKLD)
                //{
                //    xCDoc.nTypOp = xCDoc.xDocP.nPol;
                //}

            }
            v.cWhereFocus = (Control)xErr;
            return (v);
        }

        private void EditFieldsIsOver(int RC, int nF)
        {
            bool bRet = false;          // перечитать запись
            if (RC == AppC.RC_OK)
            {
                switch (nF)
                {
                    case AppC.F_ADD_REC:
                        bRet = xNSI.AddDocRec(xCDoc);
                        // если добавленная не видна, надо как-то встать на нее
                        //CurrencyManager cmDoc = (CurrencyManager)BindingContext[dgDoc.DataSource];
                        //cmDoc.Position = cmDoc.Count - 1;
                        SetCurRow(dgDoc, "SYSN", xCDoc.nId);
                        break;
                    case AppC.F_CHG_REC:
                        bRet = xNSI.UpdateDocRec(xCDoc.drCurRow, xCDoc);
                        break;
                }
            }
            RestShowDoc(bRet);
        }


        // удаление документа (ов)
        private void DelDoc(int nReg)
        {
            if (xCDoc.drCurRow != null)
            {
                if (nReg == AppC.F_DEL_REC)
                {// удаление одиночной
                    xNSI.DT[NSI.BD_DOCOUT].dt.Rows.Remove( xCDoc.drCurRow );

                    DataView dvMaster = ((DataTable)dgDoc.DataSource).DefaultView;
                    if (dvMaster.Count > 0)
                        xCDoc.drCurRow = dvMaster[dgDoc.CurrentRowIndex].Row;
                    else
                        xCDoc.drCurRow = null;
                }
                else
                {
                    DialogResult dr = MessageBox.Show("Отменить удаление всех (Enter)?\n(ESC) - все удалить без сомнений",
                        "Удаляются все строки!",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    if (dr != DialogResult.OK)
                    {
                        xNSI.DT[NSI.BD_SSCC].dt.Rows.Clear();
                        xNSI.DT[NSI.BD_SPMC].dt.Rows.Clear();
                        xNSI.DT[NSI.BD_DIND].dt.Rows.Clear();
                        xNSI.DT[NSI.BD_DOUTD].dt.Rows.Clear();
                        xNSI.DT[NSI.BD_DOCOUT].dt.Rows.Clear();
                        xCDoc.drCurRow = null;
                    }
                }
                RestShowDoc(false);

                //if (xCDoc.drCurRow != null)
                //{
                //    RestShowDoc(false);
                //}
                //else
                //{// по-хорошему, сначала проверить, есть ли фильтр
                //    //AddOrChangeDoc(AppC.DT_ADDNEW);
                //}
            }
        }

        private void ControlAllDoc(List<string> lstProt)
        {
            DataView dvD = ((DataTable)dgDoc.DataSource).DefaultView;
            for (int i = 0; i < dvD.Count; i++)
            {
                ControlDocZVK(dvD[i].Row, lstProt);
            }
        }


        // смена стиля Grid-документы
        //private void ChgDocGridStyle(int nReg)
        //{
        //    //MessageBox.Show("Changing...");
        //    xNSI.ChgGridStyle(NSI.NS_DOCOUT, NSI.GDOC_NEXT);
        //}


        // обработчик смены ячейки


        private void dgDoc_CurrentCellChanged(object sender, EventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            DataView dvMaster = ((DataTable)dg.DataSource).DefaultView;
            DataRow dr = dvMaster[dg.CurrentRowIndex].Row;
            if (xCDoc.drCurRow != dr)
            {// сменилась строка
                xCDoc = new CurDoc(xSm);
                xCDoc.drCurRow = dr;
                xNSI.InitCurDoc(xCDoc, xSm);
                SetParFields(xCDoc.xDocP);

                DataRow[] childRows = xCDoc.drCurRow.GetChildRows(NSI.REL2ZVK);
                if (childRows.Length > 0)
                    bZVKPresent = true;
            }
        }

        //public void SetSSCCForPoddon(ScanVarRM xSc, DataView dv, int nP)
        //{
        //    string sF,
        //        sD = xSc.Dat;
        //    sF = (sD.Substring(2, 1) == "1") ? "SSCC" : "SSCCINT";
        //    foreach (DataRowView drv in dv)
        //    {
        //        (drv.Row[sF]) = sD;
        //    }
        //    MessageBox.Show(String.Format("Поддон {0} подготовлен ({1}) позиций", nP, dv.Count));

        //    //xCDoc.xNPs.TryNext(true);
        //    tCurrPoddon.Text = xCDoc.xNPs.Current.ToString();
        //}

        //public bool StoreSSCC(ScanVarRM xSc, int nPoddonN, bool bNeedWrite, out DataView dv)
        //{
        //    int n = 0;
        //    string 
        //        s, sRf,
        //        sF,
        //        sD = xSc.Dat;
        //    bool 
        //        //bIsExt,
        //        bRet = AppC.RC_CANCELB;
        //    DataView dvZ;
        //    DialogResult dRez;

        //    if (sD.Substring(2, 1) == "1")
        //    {
        //        //bIsExt = true;
        //        sF = "SSCC";
        //    }
        //    else
        //    {
        //        //bIsExt = false;
        //        sF = "SSCCINT";
        //    }

        //    dv = null;
        //    if (xCDoc.drCurRow == null)
        //    {
        //        return (bRet);
        //    }
        //    try
        //    {
        //        if ((int)xCDoc.drCurRow["TYPOP"] == AppC.TYPOP_KMPL)
        //        {
        //            if (nPoddonN > 0)
        //            {
        //                //string sRf = xCDoc.DefDetFilter() + String.Format(" AND (SSCC='{0}')", sD);
        //                //dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
        //                //n = dv.Count;
        //                if ( IsUsedSSCC(sD) )
        //                {
        //                    dRez = MessageBox.Show(
        //                        String.Format("SSCC={0}\nОтменить (Enter)?\n(ESC)-проставить SSCC", sD),
        //                        "Уже использовался!", MessageBoxButtons.OKCancel,
        //                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
        //                    n = (dRez == DialogResult.OK) ? 1 : 0;
        //                }
        //                if (n == 0)
        //                {// такой SSCC еще не использовался
        //                    sRf = xCDoc.DefDetFilter() + String.Format(" AND (NPODDZ={0})", nPoddonN);
        //                    dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
        //                    if (dv.Count > 0)
        //                    {

        //                        foreach (DataRowView drv in dv)
        //                        {
        //                            if ((drv.Row[sF]) != System.DBNull.Value)
        //                            {
        //                                s = (drv.Row[sF]).ToString();
        //                                if ((s.Length > 0) && (s != sD))
        //                                {
        //                                    dRez = MessageBox.Show(
        //                                    String.Format("SSCC={0} уже установлен\nОтменить (Enter)?\n(ESC)-проставить SSCC", drv.Row["SSCC"]),
        //                                    String.Format("Поддон {0}", nPoddonN),
        //                                    MessageBoxButtons.OKCancel,
        //                                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
        //                                    n = (dRez == DialogResult.OK) ? 1 : 0;
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                        if (n == 0)
        //                        {// поддон еще не отмечался
        //                            // добавим фильтр на выполненные
        //                            sRf += String.Format("AND(READYZ<>{0})", (int)NSI.READINESS.FULL_READY);
        //                            dvZ = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "", DataViewRowState.CurrentRows);
        //                            n = dvZ.Count;
        //                            if (n > 0)
        //                            {// не вся заявка закрыта
        //                                if (!xCDoc.bFreeKMPL)
        //                                {
        //                                    dRez = MessageBox.Show(
        //                                        "Заявка не выполнена!\nОтменить (Enter)?\r\n(ESC)-проставить SSCC",
        //                                        String.Format("Поддон {0}", nPoddonN),
        //                                    MessageBoxButtons.OKCancel,
        //                                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
        //                                    n = (dRez == DialogResult.OK) ? 1 : 0;
        //                                }
        //                                else
        //                                    n = 0;
        //                            }
        //                            if (n == 0)
        //                            {
        //                                bRet = AppC.RC_OKB;
        //                                if (bNeedWrite)
        //                                    SetSSCCForPoddon(xSc, dv, nPoddonN);

        //                                //foreach (DataRowView drv in dv)
        //                                //{
        //                                //    (drv.Row[sF]) = sD;
        //                                //}
        //                                //MessageBox.Show(String.Format("Поддон {0} подготовлен ({1}) позиций",
        //                                //    xCDoc.xNPs.Current, dv.Count));

        //                                //xCDoc.xNPs.TryNext(true);
        //                                //tCurrPoddon.Text = xCDoc.xNPs.Current.ToString();
        //                            }
        //                        }
        //                    }
        //                    else
        //                        Srv.ErrorMsg("Нет отсканированных!");
        //                }
        //                else
        //                    Srv.ErrorMsg("SSCC поддона уже использовался!");
        //            }
        //            else
        //                Srv.ErrorMsg("№ поддона не установлен!");
        //        }
        //        else
        //            Srv.ErrorMsg("Только для комплектации!");





        //    }
        //    catch (Exception e)
        //    {
        //        Srv.ErrorMsg("Только для комплектации!");
        //    }




        //    return (bRet);
        //}

        //// нажали цифру или нет
        //public static bool IsDigKey(KeyEventArgs e, ref int nNum)
        //{
        //    bool bRet = AppC.RC_CANCELB;
        //    if ((e.KeyValue >= W32.VK_D1) && (e.KeyValue <= W32.VK_D9))
        //    {
        //        nNum = (e.KeyValue == W32.VK_D1) ? 1 : (e.KeyValue == W32.VK_D2) ? 2 : (e.KeyValue == W32.VK_D3) ? 3 :
        //                (e.KeyValue == W32.VK_D4) ? 4 : (e.KeyValue == W32.VK_D5) ? 5 : (e.KeyValue == W32.VK_D6) ? 6 :
        //                (e.KeyValue == W32.VK_D7) ? 7 : (e.KeyValue == W32.VK_D8) ? 8 : 9;
        //        bRet = AppC.RC_OKB;
        //    }
        //    return (bRet);
        //}

    }
}
