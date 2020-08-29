using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Data;

using ScannerAll;
using PDA.OS;
using PDA.Service;

using FRACT = System.Decimal;


namespace SkladRM
{
    public partial class MainF : Form
    {

        // текущее состояние панели
        private int nCurVvodState = AppC.DT_SHOW;

        // текущий документ (DataRow), отображаемый на панели
        private DataRow drShownDoc = null;

        
        private bool
            bShowSSCC = true,
            bShowTTN = true;                        // текущая таблица - заявки или введенные

        // заявки имеются для документа ?
        public bool 
            bZVKPresent = false;


        // текущая строка в таблице детальных строк
        private DataRow drDet = null;

        // текущие скан-данные
        private PSC_Types.ScDat scCur;

        // текущая таблица переходов для редактирования
        private AppC.EditListC aEdVvod;

        // флаг завершения ввода
        //private bool bQuitEdVvod = false;

        // текущая команда перехода в таблице переходов
        //private int nCurEditCommand = -1;

        // признак корректировки последних результатов сканирования
        private bool bLastScan = false;

        // старые введенные значения (результаты сканирования перед сохранением)
        private int nOldMest = 0;
        private FRACT fOldVsego, fOldVes;

        // старые введенные значения (результаты сканирования перед редактированием)
        private int nDefMest = 0;
        private FRACT fDefEmk, fDefVsego, fDefVes = 0;

        // старые введенные значения (результаты сканирования перед сохранением)
        private bool bAskEmk = false, 
            bAskKrk = false;

        private string 
            sOldKMC = "";

        private FRACT 
            fOldEmk = 0M;

        private int 
            nOldKrkEmkNoSuch = 0;

        // переход на вкладку Ввод
        private void EnterInScan()
        {
            DataTable 
                dtD = ((DataTable)this.dgDet.DataSource);

            // что показываем (ТТН или заявки)?
            bShowTTN = (dtD == xNSI.DT[NSI.BD_DOUTD].dt) ? true : false;
            //ShowRegVvod();
            //if (drShownDoc != xCDoc.drCurRow)
            //{// сменился документ
            //    NewDoc(dtD);
            //}
            //lDocInf.Text = CurDocInf(xCDoc.xDocP);
                
            NewDoc(dtD);
            lDocInf.Text = CurDocInf(xCDoc.xDocP);

            if (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.AVAIL)
            {// инвентаризации - всегда в ТТН
                ChgDetTable(null, NSI.BD_DOUTD);
            }

            if (bShowTTN == false)
                xNSI.ChgGridStyle(NSI.BD_DIND, NSI.GDET_ZVK);

            ShowRegVvod();
            dgDet.Focus();
        }

        // Формирование строки с инфой по текущему документу (заголовок панели)
        private string CurDocInf(DocPars xP)
        {
            string
                sNDoc = (xP.sNomDoc == "") ? "" : " № " + xP.sNomDoc,
                sBCDoc = (xP.sBC_Doc == "") ? "" : " ШК:" + xP.sBC_Doc,
                sTypDoc = TName(xP.nNumTypD) + ": ",
                sData = xP.dDatDoc.ToString("dd.MM");

            sTypDoc += String.Format("{0}{1}{2}", sData, sNDoc, sBCDoc);
            if (xCDoc.drCurRow["CHKSSCC"] is int)
            {
                if ((int)xCDoc.drCurRow["CHKSSCC"] > 0)
                    sTypDoc += " - контроль";
            }
            return (sTypDoc);
        }

        // действия при смене документа
        // dtD - таблица в гриде детальных строк
        private void NewDoc(DataTable dtD)
        {
            string 
                sF = "";
                
            bZVKPresent = false;
            if (xCDoc.drCurRow != null)
            {
                //sF = xCDoc.drCurRow["SYSN"].ToString();
                //DataRow[] childRows = xCDoc.drCurRow.GetChildRows(NSI.REL2ZVK);
                //if (childRows.Length > 0)
                //    bZVKPresent = true;

                sF = xCDoc.DefDetFilter();
                DataView dv = new DataView(xNSI.DT[NSI.BD_DIND].dt, sF, "", DataViewRowState.CurrentRows);
                if (dv.Count > 0)
                    bZVKPresent = true;

                xNSI.DT[NSI.BD_DIND].dt.DefaultView.RowFilter = sF;
                xNSI.DT[NSI.BD_DOUTD].dt.DefaultView.RowFilter = sF;
                xNSI.DT[NSI.BD_SSCC].dt.DefaultView.RowFilter = sF;

                drShownDoc = xCDoc.drCurRow;

                //if (ChangeDetRow(true) <= 0)        // документ пока пустой, 
                //    ShowOperState(xCDoc.xOper);     // отображаем состояние новой операции

                ChangeDetRow(true);
                ShowOperState(xCDoc.xOper);     // отображаем состояние новой операции
                ShowStatDoc();
                ShowRegVvod();

                if (xCDoc.xNPs.Current <= 0)
                    xCDoc.xNPs.TryNext(true, false);

                tCurrPoddon.Text = xCDoc.xNPs.Current.ToString();
            }
        }

        // количество детальных строк в ТТН/заявке
        private void ShowStatDoc()
        {
            string 
                sS = @"0(0М)";
            if (xCDoc.drCurRow != null)
            {
                int nM = 0;
                DataRow[] childRows = xCDoc.drCurRow.GetChildRows((bShowTTN) ? NSI.REL2TTN : NSI.REL2ZVK);
                foreach(DataRow dr in childRows)
                    nM += (int)dr["KOLM"];
                sS = String.Format("{0}({1}М)", childRows.Length, nM);
            }
            lSSCCState.Text = sS;
        }

        private void SetDetFields(bool bClearInf)
        {
            SetDetFields(bClearInf, ref scCur);
        }

        // заполнение полей ввода
        //private void SetDetFields(bool bClearInf, ref PSC_Types.ScDat scD)
        //{
        //    this.tKMC.Text = (scCur.nKrKMC == AppC.EMPTY_INT) ? "" : scCur.nKrKMC.ToString();

        //    this.tNameSc.Text = scCur.sN;

        //    this.tEAN.Text = scCur.sEAN;

        //    //this.tDatMC.Text = (xPars.IsDateOfProd == true) ? scCur.sDataIzg : scCur.sDataGodn;
        //    this.tDatMC.Text = (scCur.dDataGodn == DateTime.MinValue)?"":scCur.sDataGodn;
        //    this.tDatIzg.Text = (scCur.dDataIzg == DateTime.MinValue)?"":scCur.sDataIzg;

        //    this.tParty.Text = (scCur.nParty == "") ? "" : scCur.nParty;
        //    this.tMest.Text = (scCur.nMest == AppC.EMPTY_INT) ? "" : scCur.nMest.ToString();
        //    this.tEmk.Text = (scCur.fEmk == 0) ? "" : scCur.fEmk.ToString();
        //    this.tVsego.Text = (scCur.fVsego == 0) ? "" : scCur.fVsego.ToString();

        //    this.tPrzvFil.Text = scCur.nPrzvFil.ToString();
        //    this.tKolPal.Text = (scCur.nPalet == 0)? "" : scCur.nPalet.ToString();

        //    if (xCDoc.xOper.nOperState == AppC.OPR_STATE.OPR_EMPTY)
        //    {
        //        tAdrFrom.Text = scCur.xOp.GetSrc(true);
        //        tAdrTo.Text = scCur.xOp.GetDst(true);
        //    }
        //    else
        //    {
        //        tAdrFrom.Text = xCDoc.xOper.GetSrc(true);
        //        tAdrTo.Text = xCDoc.xOper.GetDst(true);
        //    }

        //    if (bClearInf == true)
        //    {
        //        lMst_alr.Text = "";
        //        lEdn_alr.Text = "";
        //        lOst_vv.Text = "";
        //        lOstVsego_vv.Text = "";
        //        lSpecCond_vv.Visible = false;
        //    }
        //}



        private void SetDetFields(bool bClearInf, ref PSC_Types.ScDat scDat)
        {
            bool
                bSetSpec = false;

            this.tKMC.Text = (scDat.nKrKMC == AppC.EMPTY_INT) ? "" : scDat.nKrKMC.ToString();

            this.tNameSc.Text = scDat.sN;

            this.tEAN.Text = scDat.sEAN;

            //this.tDatMC.Text = (xPars.IsDateOfProd == true) ? scCur.sDataIzg : scCur.sDataGodn;
            this.tDatMC.Text = (scDat.dDataGodn == DateTime.MinValue) ? "" : scDat.sDataGodn;
            this.tDatIzg.Text = (scDat.dDataIzg == DateTime.MinValue) ? "" : scDat.sDataIzg;

            if (!bShowTTN)
            {
                try
                {
                    if ((((int)drDet["COND"]) & (int)NSI.SPECCOND.DATE_SET_EXACT) > 0)
                    {
                        //lDateIzg.BackColor = Color.Coral;
                        lDateIzg.BackColor = Color.LimeGreen;
                        bSetSpec = true;
                    }
                }
                catch
                {
                }
            }
            if (!bSetSpec)
                lDateIzg.BackColor = Color.Lavender;


            this.tParty.Text = (scDat.nParty == "") ? "" : scDat.nParty;
            this.tMest.Text = (scDat.nMest == AppC.EMPTY_INT) ? "" : scDat.nMest.ToString();
            this.tEmk.Text = (scDat.fEmk == 0) ? "" : scDat.fEmk.ToString();
            this.tVsego.Text = (scDat.fVsego == 0) ? "" : scDat.fVsego.ToString();

            this.tPrzvFil.Text = scDat.nPrzvFil.ToString();
            this.tKolPal.Text = (scDat.nPalet == 0) ? "" : scDat.nPalet.ToString();

            if (xCDoc.xOper.nOperState == AppC.OPR_STATE.OPR_EMPTY)
            {
                //tAdrFrom.Text = scDat.xOp.GetSrc(true);
                //tAdrTo.Text = scDat.xOp.GetDst(true);
            }
            else
            {
                //tAdrFrom.Text = xCDoc.xOper.GetSrc(true);
                //tAdrTo.Text = xCDoc.xOper.GetDst(true);
            }

            if (bClearInf == true)
            {
                lMst_alr.Text = "";
                lEdn_alr.Text = "";
                lOst_vv.Text = "";
                lOstVsego_vv.Text = "";
                lSpecCond_vv.Visible = false;
            }
        }

        // обработчик смены ячейки
        private void dgDet_CurrentCellChanged(object sender, EventArgs e)
        {
            ChangeDetRow(false);
        }

        // смена отображаемой продукции
        // bReRead = true - принудительное чтение строки
        private int ChangeDetRow(bool bReRead)
        {
            int 
                ret;
            DataView 
                dvDetail = ((DataTable)this.dgDet.DataSource).DefaultView;

            ret = dvDetail.Count;

            if (ret >= 1)
            {
                DataRowView drv = dvDetail[this.dgDet.CurrentRowIndex];
                if ((drDet != drv.Row) || (bReRead == true))
                {// смена строки
                    if (bInEasyEditWait == true)
                    {
                        ZVKeyDown(AppC.F_OVERREG, null, ref ehCurrFunc);
                    }
                    drDet = drv.Row;
                    if ((!bEditMode) || (bReRead))
                    {
                        scCur = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.NoData, ""));
                        xNSI.InitCurProd(ref scCur, drDet);
                        SetDetFields(true);
                    }
                    else
                        SetDetFields(false);
                }
            }
            else
            {
                drDet = null;
                scCur = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.NoData, ""));
                SetDetFields(true);
            }

            bLastScan = false;

            return (ret);
        }


        string GetGridCurrentStyle(DataGrid dataGrid)
        {
            CurrencyManager currencyManager = (CurrencyManager)BindingContext[dataGrid.DataSource];
            IList iList = currencyManager.List;
            if (iList is ITypedList)
            {
                ITypedList iTypedList = (ITypedList)currencyManager.List;
                return iTypedList.GetListName(null);
            }
            else
                return iList.GetType().Name;
        }
        private void SetDopFieldsForEnter(bool bAfterAdd)
        { SetDopFieldsForEnter(bAfterAdd, false); }

        // установка полей вывода заявка - уже введено
        // bAfterAdd = true - вывод после добавления отсканированной продукции
        private void SetDopFieldsForEnter(bool bAfterAdd, bool bMainRefresh)
        {
            int 
                iZ, iA,
                nMa = scCur.nKolM_alr + scCur.nKolM_alrT;
            FRACT 
                fVa = scCur.fKolE_alr + scCur.fKolE_alrT;
            bool 
                bShowZvk = false;

            if (bAfterAdd == true)
            {// после редактирования и добавления меняется только "уже введено"
                nMa += scCur.nMest;

                if (scCur.fEmk == 0)
                {// сейчас вводились единицы
                    if (fVa == 0)
                        // отдельная продукция не вводилась, выводится всего (штук или кг)
                        fVa = scCur.fVsego;
                    else
                        // отдельная продукция вводилась, выводится ее количество (штук или кг)
                        fVa += scCur.fVsego;
                }
                else
                {// вводились места
                    if (fVa == 0)
                        // отдельная продукция не вводилась, выводится всего (штук или кг)
                        fVa = scCur.fVsego + scCur.fMKol_alr;
                    else
                    {// отдельная продукция вводилась, суммировать нельзя
                    }
                }
            }
            else
            {// ввод еще будет, пока выведем всего
                if (fVa == 0)
                {// отдельная продукция не вводилась, выводится всего (штук или кг)
                    fVa = scCur.fMKol_alr + scCur.fMKol_alrT;
                }
            }

            if (bZVKPresent == true)
            {
                //if (xCDoc.xDocP.nNumTypD != AppC.EMPTY_INT)
                //{
                //    bShowZvk = xPars.aDocPars[xCDoc.xDocP.nNumTypD].bShowFromZ;
                //}
                if (xCDoc.xDocP.DType.MoveType != AppC.MOVTYPE.AVAIL)
                {
                    bShowZvk = true;
                }
            }


            if (bShowZvk == true)
            {
                iZ = (scCur.nKolM_zvk > 0) ? scCur.nKolM_zvk : (int)scCur.fKolE_zvk;
                //lOst_vv.Text = iZ.ToString();
                lOstVsego_vv.Text = iZ.ToString();
                //lOstVsego_vv.Text = (scCur.nKolM_zvk * scCur.fEmk).ToString();
            }
            else
            {
                lOst_vv.Text = "";
                lOstVsego_vv.Text = "";
            }

            //lMst_alr.Text = nMa.ToString();
            //lEdn_alr.Text = fVa.ToString();

            // 28.06.18 iA = (scCur.nKolM_zvk > 0) ? nMa : (int)(nMa * scCur.fEmk);

            if ((xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM) && (bZVKPresent))
            {// для комплектации
                nMa = scCur.nMAlr_NPP;
                fVa = scCur.fVAlr_NPP;
            }
            if ((xCDoc.xDocP.TypOper == AppC.TYPOP_MOVE) && (scCur.nKrKMC == AppC.KRKMC_MIX))
            {// для перемещения сборных поддонов
                nMa = scCur.nKolM_alr;
                fVa = scCur.fKolE_alr;
            }

            iA = nMa;
            lEdn_alr.Text = iA.ToString();

            if (bMainRefresh)
            {
                tMest.Text = (scCur.nMest == AppC.EMPTY_INT) ? "" : scCur.nMest.ToString();
                tEmk.Text = (scCur.fEmk == 0) ? "" : scCur.fEmk.ToString();
                tVsego.Text = (scCur.fVsego == 0) ? "" : scCur.fVsego.ToString();
            }
        }


        // режим ввода детальных строк для данного документа
        private void ShowRegVvod()
        {
            if (bShowTTN)
            {// для операций перемещения - другие названия
                lDocInf.BackColor = Color.LightSkyBlue;
            }
            else
            {
                lDocInf.BackColor = Color.LightCoral;
                    //PaleGoldenrod;
            }
        }



        // обработка клавиш на панели сканирования/ввода
        private bool Vvod_KeyDown(int nFunc, KeyEventArgs e)
        {
            bool 
                ret = false;
            int
                i;
            CurrencyManager cmDet;

            if ((nFunc <= 0) && (bEditMode == false) &&
                (e.KeyValue == W32.VK_ESC) && (e.Modifiers == Keys.None))
                nFunc = AppC.F_MAINPAGE;

            if (nFunc > 0)
            {
                if (xScrDet.CurReg != 0)
                {// когда выйти из полноэкранного
                    if ((nFunc != AppC.F_CHG_SORT) && (nFunc != AppC.F_CHGSCR)
                     && (nFunc != AppC.F_HELP)
                     && (nFunc != AppC.F_KMCINF)
                     && (nFunc != AppC.F_CELLINF)
                     && (nFunc != AppC.F_SETPODD)
                     && (nFunc != AppC.F_GOFIRST)
                     && (nFunc != AppC.F_GOLAST)
                     && (nFunc != AppC.F_NEXTDOC) && (nFunc != AppC.F_PREVDOC)
                     && (nFunc != AppC.F_QUIT) && (nFunc != AppC.F_SAMEKMC)
                     && (nFunc != AppC.F_EASYEDIT) && (nFunc != AppC.F_CHG_GSTYLE)
                     && (nFunc != AppC.F_DEL_REC) && (nFunc != AppC.F_FLTVYP))
                    {
                        Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                        xScrDet.NextReg(AppC.REG_SWITCH.SW_CLEAR, tNameSc);
                    }
                }

                switch (nFunc)
                {
                    case AppC.F_CELLINF:
                        AddrInfo xA = WhatAdr4Inf();
                        if ((xA is AddrInfo) && (xA.Addr.Length > 0))
                        {
                            ConvertAdr2Lst(xA, "TXT");
                        }
                        else
                            Srv.ErrorMsg("Адрес не заполнен!");
                        ret = true;
                        break;
                    case AppC.F_KMCINF:
                        // для заявок ищем с ограничениями
                        GetKMCInf((bShowTTN)?0:1);
                        ret = true;
                        break;
                }



                if (bEditMode == false)
                {// функции только для режима просмотра
                    switch (nFunc)
                    {
                        case AppC.F_MAINPAGE:
                            tcMain.SelectedIndex = PG_DOC;
                            ret = true;
                            break;
                        case AppC.F_ADD_REC:
                            PSC_Types.ScDat scTmp = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.NoData, ""));
                            if (CanProd(ref scTmp))
                            {
                                scCur = scTmp;
                                SetDetFields(false);
                                AddOrChangeDet(nFunc);
                            }
                            ret = true;
                            break;
                        case AppC.F_CHG_REC:
                            AddOrChangeDet(nFunc);
                            ret = true;
                            break;
                        case AppC.F_DEL_ALLREC:
                        case AppC.F_DEL_REC:
                            DelDetDoc(nFunc);
                            ShowStatDoc();
                            //ShowOperState(xCDoc.xOper);
                            ret = true;
                            break;
                        case AppC.F_CHG_SORT:
                            string sNS = "";
                            bool bUA = ((!xPars.UseAdr4DocMode) && 
                                (xCDoc.xDocP.DType.MoveType != AppC.MOVTYPE.MOVEMENT))?false:true;

                            string sS = xNSI.SortDet(bShowTTN, xNSI, ref sNS, bUA);
                            lSortInf.Text = sNS;
                            DataView dv = ((DataTable)this.dgDet.DataSource).DefaultView;
                            dv.Sort = sS;
                            ChangeDetRow(true);
                            ret = true;
                            break;
                        case AppC.F_NEXTDOC:
                        case AppC.F_PREVDOC:
                            SetNextPrevDoc(nFunc);
                            ret = true;
                            break;
                        case AppC.F_GOFIRST:
                        case AppC.F_GOLAST:
                            //cmDet = (CurrencyManager)BindingContext[dgDet.DataSource];
                            //if (cmDet.Count > 0)
                            //{
                            //    cmDet.Position = (nFunc == AppC.F_GOFIRST) ? 0 : cmDet.Count - 1;
                            //    ChangeDetRow(true);
                            //}
                            Go1stLast(dgDoc, nFunc);
                            ret = true;
                            break;
                        case AppC.F_CHG_GSTYLE:
                            // переключение накладная/заявка
                            ChgDetTable(null, "");
                            ret = true;
                            break;
                        case AppC.F_TOT_MEST:
                            // всего мест по накладная/заявка
                            //ShowTotMest();
                            if (drDet != null)
                                //ShowTotMestAll((string)drDet["KMC"], (string)drDet["NP"], (string)drDet["SNM"]);
                                ShowTotMestProd();
                            else
                                ShowTotMest();
                                ret = true;
                            break;
                        case AppC.F_CTRLDOC:
                            // контроль текущего документа
                            if (drShownDoc != null)
                            {
                                Cursor.Current = Cursors.WaitCursor;
                                xInf = new List<string>();
                                ControlDocZVK(null, xInf);
                                Cursor.Current = Cursors.Default;
                                xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                            }
                            ret = true;
                            break;
                        case AppC.F_SAMEKMC:
                            // переход на такой же код в смежном документе
                            GoSameKMC();
                            ret = true;
                            break;
                        case AppC.F_CHGSCR:
                            // смена экрана
                            xScrDet.NextReg(AppC.REG_SWITCH.SW_NEXT, tNameSc);
                            ret = true;
                            break;
                        case AppC.F_FLTVYP:
                            // установка фильтра - только невыполненные
                            SetDetFlt(AppC.REG_SWITCH.SW_NEXT);
                            ret = true;
                            break;
                        case AppC.F_EASYEDIT:
                            // режим упрощенного ввода
                            SetEasyEdit(AppC.REG_SWITCH.SW_NEXT);
                            ret = true;
                            break;
                        case AppC.F_ZVK2TTN:
                            // перенос в ТТН
                            ZVK2TTN();
                            ret = true;
                            break;
                        case AppC.F_BRAKED:
                            if (bShowTTN && (drDet != null))
                            {
                                xDLLAPars = new object[2]{xCDoc.xDocP.nNumTypD, drDet};
                                DialogResult xDRslt = CallDllForm(sExeDir + "SGPF-Brak.dll", true);
                                dgDet.Focus();
                            }
                            ret = true;
                            break;
                        //case AppC.F_OPROVER:
                        //    SetOverOPR(false);
                        //    ret = true;
                        //    break;
                        case AppC.F_SETPODD:
                            TryNextPoddon(null);
                            ret = true;
                            break;
                        //case AppC.F_PRNDOC:
                        //    PrintEtikPoddon();
                        //    ret = true;
                        //    break;
                        //case AppC.F_SETPRN:
                        //    SetCurPrinter(true);
                        //    ret = true;
                        //    break;
                        case AppC.F_PODD:
                            CallFrmPars();
                            ret = true;
                            break;
                        case AppC.F_NEWOPER:
                            NewOper();
                            ret = true;
                            break;
                    }
                }
                else
                {// допустимы и при редактировании
                    switch (nFunc)
                    {
                        case AppC.F_PODD:
                            if (tMest.Focused == true)
                            {
                                int nM = int.Parse(tMest.Text);
                                nM = scCur.nMestPal * nM;
                                tMest.Text = nM.ToString();
                            }
                            ret = true;
                            break;
                    }
                }

            }
            else
            {
                if (bEditMode == true)
                {
                    switch (e.KeyValue)
                    {
                        case W32.VK_ESC:
                            //nCurEditCommand = AppC.CC_CANCEL;
                            ret = true;
                            EditEndDet(AppC.CC_CANCEL);
                            break;
                        case W32.VK_UP:
                        case W32.VK_DOWN:
                            aEdVvod.TryNext((e.KeyValue == W32.VK_UP) ? AppC.CC_PREV : AppC.CC_NEXT);
                            ret = true;
                            break;
                        case W32.VK_ENTER:
                            ret = true;
                            if (aEdVvod.TryNext(AppC.CC_NEXTOVER) == AppC.RC_CANCELB)
                                //if (bQuitEdVvod == true)
                                EditEndDet(AppC.CC_NEXTOVER);
                            break;
                        case W32.VK_TAB:
                            aEdVvod.TryNext((e.Shift == true) ? AppC.CC_PREV : AppC.CC_NEXT);
                            ret = true;
                            break;
                        case W32.VK_LEFT:
                        case W32.VK_RIGHT:
                            if ((aEdVvod.Current == tDatIzg) || (aEdVvod.Current == tPrzvFil))
                            {// попытка смены KMC
                                if (scCur.xEANs.Count > 1)
                                {
                                    StrAndInt xS = null;
                                    if (e.KeyValue == W32.VK_LEFT)
                                        xS = scCur.xEANs.MoveEx(Srv.Collect4Show<StrAndInt>.DIR_MOVE.BACK);
                                    else
                                        xS = scCur.xEANs.MoveEx(Srv.Collect4Show<StrAndInt>.DIR_MOVE.FORWARD);
                                    if (scCur.xEANs.Current != null)
                                    {
                                        scCur.nPrzvFil = xS.IntCodeAdd1;
                                        scCur.GetFromNSI("", xS.NSIRow, ref scCur.nPrzvFil, true);
                                        SetDetFields(false);
                                        //tNameSc.Text = xS.SName;
                                        //tKMC.Text = xS.IntCode.ToString();
                                        //tPrzvFil.Text = xS.IntCodeAdd1.ToString();
                                    }
                                }
                            }
                            if (((aEdVvod.Current == tMest) || (aEdVvod.Current == tParty))
                                && (scCur.xEmks.Count > 0))
                            {
                                if (scCur.xEmks.Count > 1)
                                {
                                    bool bMayChange = true;

                                    if (scCur.nRecSrc != (int)NSI.SRCDET.HANDS)
                                    {
                                        if (tEmk.Enabled == false)
                                            bMayChange = false;
                                    }
                                    if (bMayChange)
                                    {
                                        StrAndInt xS = null;
                                        if (scCur.xEmks.Current == null)
                                            xS = scCur.xEmks.MoveEx(Srv.Collect4Show<StrAndInt>.DIR_MOVE.FORWARD);
                                        else
                                        {
                                            if (e.KeyValue == W32.VK_LEFT)
                                                xS = scCur.xEmks.MoveEx(Srv.Collect4Show<StrAndInt>.DIR_MOVE.BACK);
                                            else
                                                xS = scCur.xEmks.MoveEx(Srv.Collect4Show<StrAndInt>.DIR_MOVE.FORWARD);
                                        }
                                        //if (scCur.xEmks.Current != null)
                                        //{
                                        scCur.fEmk = xS.DecDat;
                                        scCur.nTara = xS.IntCodeAdd3;
                                        scCur.nKolSht = xS.IntCodeAdd1;
                                        if (!scCur.bVes)
                                            scCur.fVsego = scCur.nMest * scCur.fEmk;
                                        //scCur.nKolG = scCur.nMest * scCur.nKolSht;

                                        tEmk.Text = scCur.fEmk.ToString();
                                        tVsego.Text = scCur.fVsego.ToString();
                                        //}
                                    }
                                }
                            }
                            ret = true;
                            break;
                    }
                }
                else
                {// для режима просмотра
                    switch (e.KeyValue)
                    {
                        case W32.VK_ENTER:
                            ShowAdrKmc();
                            ret = true;
                            break;
                        case W32.VK_TAB:
                            CallFrmPars();
                            ret = true;
                            break;
                    }
                }
            }
            e.Handled = bSkipChar = ret;

            return (ret);
        }

        private void ShowAdrKmc_()
        {
            int
                nCondition;
            string
                sPartyZ = "";
            char
                cExCh = '*';

            if (bZVKPresent && !bShowTTN)
            {
                try
                {
                    // все адреса с данной продукцией
                    string sRf = String.Format("(ID={0})", drDet["ID"]);
                    DataRow
                        xd;
                    DateTime
                        dGodnCurr,
                        dGodnZ;
                    bool
                        WasExact = false,
                        WasGoodGodn = false,
                        WasBadGodn = false;
                    AddrInfo
                        xa;
                    DataView
                        dv = new DataView(xNSI.DT[NSI.BD_ADRKMC].dt, sRf, "DTG ASC,IDX ASC,KOLE DESC", DataViewRowState.CurrentRows);


                    if (dv.Count > 0)
                    {
                        nCondition = (int)drDet["COND"];
                        sPartyZ = (drDet["NP"] is string)?(string)drDet["NP"]:"";
                        xInf = aKMCName(scCur.sN, true);
                        //xInf.Insert(xInf.Count, " ".PadRight(32, '-'));
                        try
                        {
                            dGodnZ = DateTime.ParseExact((string)drDet["DTG"], "yyyyMMdd", null);
                        }
                        catch
                        {
                            dGodnZ = DateTime.MinValue;
                        }
                        foreach (DataRowView dva in dv)
                        {
                            xd = dva.Row;
                            xa = new AddrInfo((string)xd["KADR"], xSm.nSklad);
                            try
                            {
                                dGodnCurr = DateTime.ParseExact((string)xd["DTG"], "yyyyMMdd", null);
                            }
                            catch
                            {
                                dGodnCurr = DateTime.MinValue;
                            }
                            if (dGodnCurr < dGodnZ)
                                WasBadGodn = true;
                            else
                            {
                                if (((nCondition & (int)NSI.SPECCOND.DATE_SET_EXACT) > 0) ||
                                 ((nCondition & (int)NSI.SPECCOND.PARTY_SET) > 0))
                                {// требуется точное соответствие
                                    if (dGodnCurr == dGodnZ)
                                    {
                                        if ((nCondition & (int)NSI.SPECCOND.DATE_SET_EXACT) > 0)
                                        {
                                            WasGoodGodn = true;
                                            WasExact = true;
                                        }
                                        else
                                        {
                                            if (sPartyZ == xd["NP"].ToString())
                                            {
                                                WasGoodGodn = true;
                                                WasExact = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (WasExact)
                                        {
                                            WasExact = false;
                                            xInf.Add(aKMCName("", true, cExCh)[0]);
                                        }
                                    }
                                }
                                else
                                    if ((nCondition & (int)NSI.SPECCOND.DATE_G_SET) > 0)
                                        WasGoodGodn = true;

                                if (WasGoodGodn)
                                {
                                    if (WasBadGodn)
                                    {
                                        WasBadGodn = false;
                                        xInf.Add("!= Подходят только следующие: =!");

                                        if (WasExact)
                                            xInf.Add(aKMCName("", true, cExCh)[0]);
                                        else
                                            xInf.Add(aKMCName("", true)[0]);
                                    }
                                }
                            }
                            xInf.Add(String.Format("{0} {1} {2} {3}",
                                xa.AddrShow.PadRight(10),
                                dGodnCurr.ToString("dd.MM.yy"),
                                xd["KOLM"].ToString().PadLeft(4),
                                xd["KOLE"].ToString().PadLeft(7)));
                        }
                        if (WasGoodGodn == false)
                            xInf.Add("!  Нет подходящих дат годности !");
                        xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                    }
                    else
                    {
                        //Srv.ErrorMsg("Адреса отсутствуют!");
                        Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                    }
                }
                catch (Exception ex)
                {
                    int ggg = 999;
                }
            }


            //if (xScrDet.CurReg == ScrMode.SCRMODES.FULLMAX)
            //{
            //    if ((xCDoc.drCurRow != null) &&
            //        ((int)xCDoc.drCurRow["TYPOP"] == AppC.TYPOP_KMPL))
            //    {
            //        SetDetFlt(NSI.BD_DOUTD, AppC.REG_SWITCH.SW_NEXT);
            //    }
            //}
        }

        private void ShowAdrKmc()
        {
            const string
                sNoGoodMsg  = "<=!=!  Нет подходящих дат  !=!=>",
                sNoExactMsg = "<=!=!  Нет точных сроков   !=!=>",
                sUpExMsg    = "********  Точные сроки  *********",
                sDownExMsg  = "*********************************";
            int
                nCondition;
            string
                sPartyZ = "";
            char
                cExCh = '=';

            if (bZVKPresent && !bShowTTN)
            {
                try
                {
                    // все адреса с данной продукцией
                    string sRf = String.Format("(ID={0})", drDet["ID"]);
                    DataRow
                        xd;
                    DateTime
                        dGodnCurr,
                        dGodnZ;
                    bool
                        NoExact = false,
                        Up4Exact = false,
                        Bottom4Exact = false,
                        bNeedExact = false,
                        WasExact = false;
                        //WasGoodGodn = false;
                    AddrInfo
                        xa;
                    List<string>
                        lGoodTerm = new List<string>(),
                        lOldTerm = new List<string>(),
                        lCur = null;

                    DataView
                        dv = new DataView(xNSI.DT[NSI.BD_ADRKMC].dt, sRf, "DTG ASC,IDX ASC,KOLE DESC", DataViewRowState.CurrentRows);


                    if (dv.Count > 0)
                    {
                        nCondition = (int)drDet["COND"];
                        sPartyZ = (drDet["NP"] is string) ? (string)drDet["NP"] : "";

                        bNeedExact = (((nCondition & (int)NSI.SPECCOND.DATE_SET_EXACT) > 0) ||
                                       ((nCondition & (int)NSI.SPECCOND.PARTY_SET) > 0));

                        xInf = aKMCName(scCur.sN, true);
                        //xInf.Insert(xInf.Count, " ".PadRight(32, '-'));
                        try
                        {
                            dGodnZ = DateTime.ParseExact((string)drDet["DTG"], "yyyyMMdd", null);
                        }
                        catch
                        {
                            dGodnZ = DateTime.MinValue;
                        }
                        foreach (DataRowView dva in dv)
                        {
                            xd = dva.Row;
                            xa = new AddrInfo((string)xd["KADR"], xSm.nSklad);
                            try
                            {
                                dGodnCurr = DateTime.ParseExact((string)xd["DTG"], "yyyyMMdd", null);
                            }
                            catch
                            {
                                dGodnCurr = DateTime.MinValue;
                            }
                            if (dGodnCurr < dGodnZ)
                            {
                                lCur = lOldTerm;
                            }
                            else
                            {
                                lCur = lGoodTerm;

                                if (bNeedExact)
                                {// требуется точное соответствие
                                    if (dGodnCurr == dGodnZ)
                                    {
                                        if ((nCondition & (int)NSI.SPECCOND.DATE_SET_EXACT) > 0)
                                        {// только дата
                                            //WasGoodGodn = true;
                                            WasExact = true;
                                        }
                                        else
                                        {// дата и партия
                                            if (sPartyZ == xd["NP"].ToString())
                                            {
                                                //WasGoodGodn = true;
                                                WasExact = true;
                                            }
                                        }
                                        if (WasExact && !Up4Exact)
                                        {
                                            Up4Exact = true;
                                            lCur.Add(sUpExMsg);
                                        }

                                    }
                                    else
                                    {// превышение даты
                                        if (WasExact)
                                        {
                                            if (!Bottom4Exact)
                                            {
                                                Bottom4Exact = true;
                                                lCur.Add(sDownExMsg);
                                            }
                                        }
                                        else
                                        {
                                            if (!NoExact)
                                            {
                                                NoExact = true;
                                                lCur.Add(sNoExactMsg);
                                            }
                                        }
                                    }
                                }
                                else
                                {// возможен срок "не хуже"
                                    //if ((nCondition & (int)NSI.SPECCOND.DATE_G_SET) > 0)
                                    //    WasGoodGodn = true;
                                }

                            }
                            lCur.Add(String.Format("{0} {1} {2} {3}",
                                xa.AddrShow.PadRight(10),
                                dGodnCurr.ToString("dd.MM.yy"),
                                xd["KOLM"].ToString().PadLeft(4),
                                xd["KOLE"].ToString().PadLeft(7)));
                        }

                        if (lGoodTerm.Count > 0)
                        {
                            //xInf.Add("!= Подходят только следующие: =!");
                            //if (WasGoodGodn)
                            //    lCur.Add(aKMCName("", true, cExCh)[0]);
                            //else
                            //    lCur.Add(aKMCName("", true)[0]);
                            //if (bNeedExact && !WasExact)
                            //    xInf.Add(sNoGoodMsg);
                            xInf.AddRange(lGoodTerm);
                            if (Up4Exact && !Bottom4Exact)
                            {
                                Bottom4Exact = true;
                                xInf.Add(sDownExMsg);
                            }
                            xInf.Add(aKMCName("", true)[0]);
                        }
                        else
                        {
                            xInf.Add(sNoGoodMsg);
                        }

                        if (lOldTerm.Count > 0)
                        {
                            //xInf.Add(aKMCName("", true, cExCh)[0]);
                            xInf.Add("!_!_!_!   Старые сроки   !_!_!_!");
                            xInf.AddRange(lOldTerm);
                        }
                        xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                    }
                    else
                    {
                        //Srv.ErrorMsg("Адреса отсутствуют!");
                        Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                    }
                }
                catch (Exception ex)
                {
                    int ggg = 999;
                }
            }


            //if (xScrDet.CurReg == ScrMode.SCRMODES.FULLMAX)
            //{
            //    if ((xCDoc.drCurRow != null) &&
            //        ((int)xCDoc.drCurRow["TYPOP"] == AppC.TYPOP_KMPL))
            //    {
            //        SetDetFlt(NSI.BD_DOUTD, AppC.REG_SWITCH.SW_NEXT);
            //    }
            //}
        }


        // проверка кода
        private void tKMC_Validating(object sender, CancelEventArgs e)
        {
            string 
                sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                try
                {
                    int nM = int.Parse(sT);
                    if ((nM == 0) && (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.AVAIL ))
                    {
                        if (xPars.UseAdr4DocMode)
                        {
                            aEdVvod.EditIsOverEx(tKMC);
                            scCur.nKrKMC = 0;
                            xCDoc.xOper.bObjOperScanned = true;
                            return;
                        }
                    }
                    else
                    {
                        PSC_Types.ScDat 
                            sTmp = scCur;

                        if (true == xNSI.Connect2MC("", nM, 0, ref sTmp))
                        {
                            if (!AvailKMC4PartInvent(sTmp.sKMC, false))
                                e.Cancel = true;
                            else
                            {
                                scCur = sTmp;
                                //scCur.nKrKMC = nM;
                                scCur.bSetAccurCode = true;
                                scCur.nRecSrc = (int)NSI.SRCDET.HANDS;

                                //if (scCur.bVes == true)
                                //    TrySetEmk(xNSI.DT[NSI.NS_MC].dt, xNSI.DT[NSI.NS_SEMK].dt, ref scCur, 0);
                                //else
                                //    CheckEmk(xNSI.DT[NSI.NS_MC].dt, xNSI.DT[NSI.NS_SEMK].dt, ref scCur);

                                if (scCur.bVes)
                                    TrySetEmk(xNSI.DT[NSI.NS_MC].dt, xNSI.DT[NSI.NS_SEMK].dt, ref scCur, 0);

                                SetDetFields(false);
                                aEdVvod.SetAvail(tEAN, false);
                                aEdVvod.SetAvail(tPrzvFil, false);
                            }
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
                scCur.nKrKMC = 0;

            if (scCur.bFindNSI && (scCur.nKrKMC > 0))
            {
                aEdVvod.SetAvail(tEAN, false);
            }
            else
                aEdVvod.SetAvail(tEAN, true);
        }

        // проверка EAN
        private void tEAN_Validating(object sender, CancelEventArgs e)
        {
            bool
                bFind;
            string 
                sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                try
                {
                    //e.Cancel = !xNSI.Connect2MC(sT, 0, 0, ref scCur);
                    bFind = EANGTIN(ref scCur, sT);
                    if (bFind)
                    {

                        if (scCur.xEANs.Count > 1)
                        {
                            if (xPars.UseList4ManyEAN)
                            {
                                if (ManyEANChoice(scCur.sEAN, -1, true) != null)
                                {
                                    SetDetFields(false);
                                    aEdVvod.SetAvail(tKMC, false);
                                    aEdVvod.SetAvail(tPrzvFil, false);
                                }
                                else
                                    EditEndDet(AppC.CC_CANCEL);
                            }
                        }
                        if (!AvailKMC4PartInvent(scCur.sKMC, false))
                            e.Cancel = true;
                        else
                        {
                            if (scCur.bVes == true)
                                TrySetEmk(xNSI.DT[NSI.NS_MC].dt, xNSI.DT[NSI.NS_SEMK].dt, ref scCur, 0);
                            else
                                CheckEmk(xNSI.DT[NSI.NS_MC].dt, xNSI.DT[NSI.NS_SEMK].dt, ref scCur);
                            SetDetFields(false);
                            IsKeyFieldChanged(tKMC, scCur.sEAN, scCur.fEmk, scCur.nParty);
                        }
                    }
                    e.Cancel = !bFind;
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            scCur.sEAN = sT;
            //if (e.Cancel != true)
            //    e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);

        }

        // проверка даты изготовления
        private void tDatIzg_Validating(object sender, CancelEventArgs e)
        {
            string 
                sD = ((TextBox)sender).Text.Trim();
            if (sD.Length > 0)
            {
                try
                {
                    sD = Srv.SimpleDateTime(sD);
                    DateTime d = DateTime.ParseExact(sD, "dd.MM.yy", null);
                    scCur.dDataIzg = d;
                    scCur.sDataIzg = sD;
                    ((TextBox)sender).Text = sD;
                    //TryEvalNewZVKTTN(ref scCur, true);
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
            {
                scCur.dDataIzg = DateTime.MinValue;
                scCur.sDataIzg = "";
            }
            if (scCur.dDataIzg != DateTime.MinValue)
            {
                aEdVvod.SetAvail(tDatMC, false);
                SetDTG(ref scCur);
                tDatMC.Text = scCur.sDataGodn;
            }
            else
            {
                if (scCur.dDataGodn == DateTime.MinValue)
                    aEdVvod.SetAvail(tDatMC, true);
            }

        }

        // проверка даты годности
        private void tDatMC_Validating(object sender, CancelEventArgs e)
        {
            string sD = ((TextBox)sender).Text.Trim();
            if (sD.Length > 0)
            {
                try
                {
                    sD = Srv.SimpleDateTime(sD);
                    DateTime d = DateTime.ParseExact(sD, "dd.MM.yy", null);
                    scCur.dDataGodn = d;
                    scCur.sDataGodn = sD;
                    ((TextBox)sender).Text = sD;
                    //TryEvalNewZVKTTN(ref scCur, true);
                    if (TryEvalNewZVKTTN(ref scCur, true) == AppC.RC_CANCELB)
                    {
                        EditEndDet(AppC.CC_CANCEL);
                    }
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
            {
                scCur.dDataGodn = DateTime.MinValue;
                scCur.sDataGodn = "";
            }
            if (scCur.dDataGodn != DateTime.MinValue)
            {
                aEdVvod.SetAvail(tDatIzg, false);
            }
            else
            {
                if (scCur.dDataIzg == DateTime.MinValue)
                    aEdVvod.SetAvail(tDatIzg, true);
            }
        }



        //private void SelAllTextForParty(object sender, EventArgs e)
        //{
        //    aEdVvod.TryNext(AppC.CC_NEXT);
        //}

        // проверка введенной партии
        private void tParty_Validating(object sender, CancelEventArgs e)
        {
            if (tParty.Text.Trim().Length > 0)
            {
                try
                {
                    //int nP = int.Parse(tParty.Text);
                    //if (nP >= 0)
                    //{
                    //    if (nP != scCur.nParty)
                    //    {
                    //        IsKeyFieldChanged(tParty, scCur.sEAN, scCur.fEmk, nP);
                    //        scCur.nParty = nP;
                    //    }
                    string nP = tParty.Text.Trim();
                    if (nP.Length > 0)
                    {
                        if (nP != scCur.nParty)
                        {
                            IsKeyFieldChanged(tParty, scCur.sEAN, scCur.fEmk, nP);
                            scCur.nParty = nP;
                        }
                    }
                    else
                        e.Cancel = true;
                }
                catch { e.Cancel = true; }
            }
            else
                scCur.nParty = "";
        }


        private bool bMestChanged = false;
        // изменение введенных мест
        private void tMest_TextChanged(object sender, EventArgs e)
        {
            bMestChanged = true;
        }

        // проверка введенных мест
        private void tMest_Validating(object sender, CancelEventArgs e)
        {
            string s, 
                sErr = "";
            int i, nDif,
                nM = 0;
            bool bGoodData = true;

            // по завершении редактирования фокус теряется
            if (!bEditMode)
                return;

            s = tMest.Text.Trim();
            if (s.Length > 0)
            {
                if (scCur.bAlienMC && !scCur.bNewAlienPInf && !tParty.Enabled && (s.Length >= 2))
                {// попытка изменить имеющиеся данные о партии
                    if (s.Substring(0,2) == "00")
                    {
                        tParty.Enabled=false;
                        aEdVvod.TryNext(AppC.CC_PREV);
                        scCur.bNewAlienPInf = true;
                        tMest.Text = scCur.nMest.ToString();
                        bMestChanged = false;
                        return;
                    }
                }
                try
                {
                    nM = int.Parse(s);
                    if (nM < 0)
                        e.Cancel = true;
                }
                catch
                {
                    e.Cancel = true;
                }
            }

            if (e.Cancel != true)
            {
                if (scCur.bVes == true)
                {
                    bGoodData = (MestValid(ref nM) == AppC.RC_OK) ? true : false;
                    // единичная продукция, всего можно корректировать, если
                    // это не отсканированная весовая единичка
                }
                else
                {// штучная продукция
                    if (nM == 0)
                    {// емкость обнуляется и недоступна, во всего - количество единичек
                        scCur.fEmk = 0;
                        tEmk.Text = "0";
                        tVsego.Enabled = true;
                        tEmk.Enabled = false;
                    }
                    else
                    {// транспортные упаковки

                        // больше емкости поддона вводить не разрешаем (после сканирования и при ручном вводе)
                        if ((xPars.aParsTypes[AppC.PRODTYPE_SHT].bMAX_Kol_EQ_Poddon) &&
                            ((nCurVvodState == AppC.F_ADD_SCAN) || (nCurVvodState == AppC.F_ADD_REC)))
                        {
                            if (xCDoc.xDocP.nNumTypD != AppC.TYPD_INV)
                            {
                                if (scCur.nMestPal > 0)
                                {// укладка на поддон имеется
                                    i = scCur.nMestPal;
                                }
                                else
                                {// укладка на поддон отсутствует, но больше 200 не бывает (пока что)
                                    i = 200;
                                }
                                nDif = nM - i;
                                bGoodData = (nDif <= 0) ? true : false;
                                sErr = String.Format("Превышение({0}) поддона({1})!", nDif, i);
                            }
                        }
                        if (bGoodData)
                        {// проверка на соответствие заявке
                            if (bZVKPresent)
                            {
                                if (nM > scCur.nMest)
                                {
                                    DialogResult dr = MessageBox.Show("Отменить ввод (Enter)?\n(ESC) - продолжить ввод",
                                        "Превышение заявки!",
                                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                                    if (dr == DialogResult.OK)
                                        bGoodData = false;
                                }
                            }
                        }
                        else
                        {
                            Srv.ErrorMsg(sErr, true);
                        }
                        if (bGoodData)
                        {
                            // попытка возврата от единичек опять к местам ?
                            if (scCur.fEmk == 0)
                            {
                                // пробуем взять емкость из справочника
                                FRACT dEmk = Math.Max(scCur.fEmk, scCur.fEmk_s);
                                if (dEmk > 0)
                                {// если есть емкость - посчитаем всего
                                    scCur.fEmk = dEmk;
                                    tEmk.Text = scCur.fEmk.ToString();
                                    scCur.fVsego = nM * scCur.fEmk;
                                    tVsego.Text = scCur.fVsego.ToString();
                                    tEmk.Enabled = false;
                                    tVsego.Enabled = false;
                                }
                                else
                                {// если нет - пускай вводят
                                    tEmk.Enabled = true;
                                }
                            }
                            else
                            {
                                scCur.fVsego = nM * scCur.fEmk;
                                tVsego.Text = scCur.fVsego.ToString();
                                tEmk.Enabled = false;
                                tVsego.Enabled = false;
                            }
                        }
                    }
                }
                ((TextBox)sender).Text = s;
                if (bGoodData)
                {
                    scCur.nMest = nM;
                    //e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
                }
                else
                {
                    ((TextBox)sender).SelectAll();
                    e.Cancel = true;
                }
            }
        }


        // проверка Мест для весового
        // nM - введенное количество
        // выход:
        // - RC_OK - переход к следующему
        // - RC_CANCEL - остаться в редактировании
        // - RC_EDITEND - редактирование окончено, сохранение результатов
        private int MestValid(ref int nM)
        {
            int nRet = AppC.RC_OK;
            bool bDopInf = false;

            switch (nCurVvodState)
            {
                case AppC.F_ADD_SCAN:
                    switch (scCur.nTypVes)
                    {
                        case AppC.TYP_VES_PAL:
                            // ввели мест на палетте
                            if (nM != 0)
                            {
                                if (scCur.fEmk == 0)
                                {
                                    bDopInf = TrySetEmk(xNSI.DT[NSI.NS_MC].dt, xNSI.DT[NSI.NS_SEMK].dt,
                                        ref scCur, scCur.fVes / nM);
                                    if ((bDopInf == true) && (scCur.nTypVes == AppC.TYP_VES_TUP))
                                    {// определилась емкость
                                        IsKeyFieldChanged(tEmk, scCur.sEAN, scCur.fEmk, scCur.nParty);
                                        tEmk.Text = scCur.fEmk.ToString();
                                    }
                                    else
                                    {
                                        Srv.ErrorMsg("Емкость не определена!");
                                        nRet = AppC.RC_CANCEL;
                                    }
                                }

                            }
                            else
                                nRet = AppC.RC_CANCEL;
                            break;
                        case AppC.TYP_VES_1ED:
                            // единичная продукция, или списываем тару (1) или нет (0)
                            nM = (nM > 0) ? 1 : 0;
                            break;
                        case AppC.TYP_VES_TUP:
                        case AppC.TYP_PALET:
                            if ((nM != scCur.nMest) || (bMestChanged == true))
                            {// вес - расчитывается по емкости
                                scCur.fVsego = nM * scCur.fEmk;
                                tVsego.Text = scCur.fVsego.ToString();
                            }
                            break;
                    }
                    break;
                case AppC.F_CHG_REC:
                    //if ((bLastScan == true) && (nM == 0) && (nOldMest != nM))
                    //{// отмена последнего сканирования
                    //    //scCur.nMest = nM;
                    //    return (AppC.RC_OK);
                    //}
                    if (scCur.fEmk == 0)
                    {// для единичных можем только прицепить/отцепить тару
                        nM = (nM > 0) ? 1 : 0;
                    }
                    else
                    {
                        switch (scCur.nTypVes)
                        {
                            case AppC.TYP_VES_PAL:
                                break;
                            case AppC.TYP_VES_TUP:
                                if (((nM != scCur.nMest) || (bMestChanged == true)))
                                {// для инвентаризации вес - расчитывается по емкости
                                    if ((xCDoc != null) && (xCDoc.xDocP.nNumTypD == AppC.TYPD_INV))
                                    {
                                        scCur.fVsego = nM * scCur.fEmk;
                                        tVsego.Text = scCur.fVsego.ToString();
                                    }
                                }
                                else
                                    nRet = AppC.RC_CANCEL;
                                break;
                        }
                    }
                    break;
            }
            return (nRet);
        }

        // проверка емкости
        private void tEmk_Validating(object sender, CancelEventArgs e)
        {
            string sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                try
                {
                    FRACT nM = FRACT.Parse(sT);
                    if (nM != scCur.fEmk)
                    {
                        scCur.fEmk = nM;
                        // 
                        if (scCur.bVes == true)
                            TrySetEmk(xNSI.DT[NSI.NS_MC].dt, xNSI.DT[NSI.NS_SEMK].dt, ref scCur, 0);
                        IsKeyFieldChanged(tEmk, scCur.sEAN, scCur.fEmk, scCur.nParty);
                        return;
                    }
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
                scCur.fEmk = 0;
            if (e.Cancel != true)
            {
                if (!scCur.bVes)
                {
                    scCur.fVsego = scCur.nMest * scCur.fEmk;
                    tVsego.Text = scCur.fVsego.ToString();
                }
                if ((scCur.bVes == true) || (scCur.nMest == 0))
                    tVsego.Enabled = true;

                //e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
            }

        }

        // проверка количества единиц (всего)
        private void tVsego_Validating(object sender, CancelEventArgs e)
        {
            string
                sT = ((TextBox)sender).Text.Trim();
            FRACT
                fTot = 0;

            if (sT.Length > 0)
            {
                try
                {
                    fTot = FRACT.Parse(sT);
                    if (fTot <= 0)
                    {
                        if ((xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM) && (!xPars.UseAdr4DocMode))
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    else
                    {
                        if (!scCur.bVes)
                            fTot = Math.Round(fTot, 0);
                    }
                    scCur.fVsego = fTot;
                    tVsego.Text = fTot.ToString();
                }
                catch
                {
                    //scCur.fVsego = 0;
                    e.Cancel = true;
                }
            }
            else
            //    scCur.fVsego = 0;
            {
                Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                e.Cancel = true;
            }
        }

        // проверка количества паллет
        private void tKolPal_Validating(object sender, CancelEventArgs e)
        {
            int
                nPal;
            string
                sT = ((TextBox)sender).Text.Trim();

            if (sT.Length > 0)
            {
                try
                {
                    nPal = int.Parse(sT);
                    if (nPal == 1)
                    {
                        scCur.nPalet = nPal;
                        //if (!scCur.bVes)
                        //{
                        //    if (nPal > 1)
                        //        scCur.nMest = nPal * scCur.nMest;
                        //    scCur.fVsego =  scCur.nMest * scCur.fEmk;
                        //    scCur.nKolSht = (int)scCur.fVsego;
                        //}
                    }
                    else
                        e.Cancel = true;
                }
                catch
                {
                    e.Cancel = true;
                }
            }

        }

        // № площадки
        private void tPrzvFil_Validating(object sender, CancelEventArgs e)
        {
            int
                nParKMC = 0,
                nOldFil,
                nF;
            string
                sParEAN = "",
                sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                try
                {
                    nF = int.Parse(sT);
                    nOldFil = scCur.nPrzvFil;

                    if (!scCur.bSetAccurCode)
                        sParEAN = scCur.sEAN;
                    else
                        nParKMC = scCur.nKrKMC;

                    PSC_Types.ScDat
                        scT = scCur;

                    if (scT.nPrzvFil != nOldFil)
                    {// изменилась
                        if (xNSI.Connect2MC(sParEAN, nParKMC, nF, ref scT))
                        {// такая площадка существует
                            scCur = scT;
                            //EvalEnteredVals(ref scCur, scCur.nKrKMC, scCur.fEmk, scCur.nParty, null, 0, 0, scCur.sKMC);
                            EvalEnteredVals(ref scCur, scCur.sKMC, scCur.fEmk, scCur.nParty, null, 0, 0);
                            SetDopFieldsForEnter(false);
                            tKMC.Text = scCur.nKrKMC.ToString();
                        }
                        else
                        {
                            scCur.nPrzvFil = nOldFil;
                            e.Cancel = true;
                        }
                    }
                }
                catch
                {
                    e.Cancel = true;
                }
            }
        }




        private void IsKeyFieldChanged(TextBox tB, string sEANCode, FRACT fVal, string nVal)
        {
            bool 
                bDopValuesNeed = true,
                bNeedEval = false;     // пересчет нужен ?
            PSC_Types.ScDat 
                sc = scCur;

            if ((nCurVvodState == AppC.F_CHG_REC) || (bShowTTN == false))
            {
                return;
            }

            if (tB == tEmk)
            {// емкость ненулевая и другая
                bNeedEval = true;
                //EvalEnteredVals(ref scCur, scCur.sKMC, fVal, scCur.nParty, null, 0, 0);

            }
            else if (tB == tKMC)
            {// введена новая партия
                bNeedEval = true;
                //EvalEnteredVals(ref scCur, scCur.sKMC, scCur.fEmk, nVal, null, 0, 0);
            }
            else if (tB == tParty)
            {// введена новая партия
                bNeedEval = true;
                //EvalEnteredVals(ref scCur, scCur.sKMC, scCur.fEmk, nVal, null, 0, 0);
            }

            if (bNeedEval == true)
            {
                EvalZVKMest(ref scCur, null, 0, 0);
                Prep4Ed(ref scCur, ref bDopValuesNeed, 0);

                SetDopFieldsForEnter(true, true);
            }
        }

        /// --- Функции работы с детальными строками --- 
        ///

        // --- добавление новой или изменение старой
        // nReg - требуемый режим
        private void AddOrChangeDet(int nReg)
        {
            bool 
                bMayEdit = false;

            DataTable dtChg = ((DataTable)this.dgDet.DataSource);

            switch (nReg)
            {
                case AppC.F_ADD_REC:
                    bMayEdit = true;
                    scCur.nRecSrc = (int)NSI.SRCDET.HANDS;
                    break;
                case AppC.F_ADD_SCAN:
                    if (scCur.xEANs.Count > 1)
                    {
                        if (xPars.UseList4ManyEAN)
                        {
                            if (ManyEANChoice(scCur.sEAN, -1, true) != null)
                            {
                                bMayEdit = true;
                                SetDetFields(false);
                            }
                        }
                    }
                    else
                        bMayEdit = true;
                    break;
                case AppC.F_CHG_REC:
                    if ((dtChg.DefaultView.Count > 0))
                    {// только для ТТН, заявки нельзя
                        if ((xSm.sUser == AppC.SUSER) || (dtChg.TableName == NSI.BD_DOUTD))
                        {
                            bMayEdit = true;
                            if (scCur.bVes == true)
                            {
                                if ((scCur.fEmk != 0) && (scCur.nMest == 1))        // транспортная не корректируется
                                    bMayEdit = false;
                            }
                        }
                    }
                    break;
            }

            if (bMayEdit == true)
                EditBeginDet(nReg, new AppC.VerifyEditFields(VerifyVvod));
        }

        class PartyInf
        {
            public string nParty;
            //public int nParty;
            public DateTime dV;
            public PartyInf(string nP, DateTime d)
            {
                nParty = nP;
                dV = d;
            }
        }

        Dictionary<string, PartyInf> dicAlienP = new Dictionary<string,PartyInf>();

        private bool PInfReady()
        {
            bool ret = false,
                bFind = true;
            string sK = scCur.sKMC + scCur.sIntKod;
            try
            {
                PartyInf xPi = dicAlienP[sK];
                scCur.nParty = xPi.nParty;
                scCur.dDataIzg = xPi.dV;
                scCur.sDataIzg = xPi.dV.ToString("dd.MM.yy");
            }
            catch
            {
                bFind = false;
            }
            scCur.bNewAlienPInf = !bFind;
            if ((!bFind) || (scCur.nTypVes == AppC.TYP_VES_PAL))
            {
                ret = true;
            }
            return (ret);
        }

        private void SetDTG(ref PSC_Types.ScDat sc)
        {
            DateTime 
                dtBase;
            if (sc.dDataGodn == DateTime.MinValue)
            {
                //dtBase = (sc.dDataIzg > DateTime.MinValue)?sc.dDataIzg : sc.dDataMrk;
                dtBase = (sc.dDataMrk > DateTime.MinValue) ? sc.dDataMrk : sc.dDataIzg;
                if ((dtBase > DateTime.MinValue) && (sc.drMC is DataRow))
                {
                    sc.dDataGodn = dtBase.AddDays((int)sc.drMC["SRR"]);
                    sc.sDataGodn = sc.dDataGodn.ToString("dd.MM.yy");
                }
            }
        }


        // вернуть контрол по его имени
        private Control FieldByName(string s)
        {
            return (
                (s == "tKMC") ? this.tKMC :
                (s == "tParty") ? this.tParty :
                (s == "tEAN") ? this.tEAN :
                (s == "tDatMC") ? this.tDatMC :
                (s == "tDatIzg") ? this.tDatIzg :
                (s == "tMest") ? this.tMest :
                (s == "tEmk") ? this.tEmk :
                (s == "tVsego") ? this.tVsego :
                (s == "tKolPal") ? this.tKolPal : this.tPrzvFil);
        }

        // проверка введенных данных на корректность
        private AppC.VerRet VerifyVvod()
        {
            int 
                nRet = AppC.RC_OK;
            string
                sSaved = (bEditMode == true) ? aEdVvod.Current.Text : "";
            Control
                xWFocus = null;
            AppC.VerRet 
                v;

            #region Проверка корректности введенных полей
            do
            {
                if (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.AVAIL)
                {
                    if (!AvailKMC4PartInvent(scCur.sKMC, true))
                    {
                        nRet = AppC.RC_CANCEL;
                        break;
                    }
                    if (xCDoc.xOper.bObjOperScanned)
                    {
                        break;
                    }
                }
                switch (nCurVvodState)
                {
                    case AppC.F_ADD_REC:
                    case AppC.F_ADD_SCAN:
                        if (scCur.fVsego <= 0)
                        {
                            if ((xCDoc.xDocP.nNumTypD == AppC.TYPD_MOVINT)
                                || ((xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM) && (!xPars.UseAdr4DocMode)))
                            {
                                Srv.ErrorMsg("Ошибочное количество!");
                                nRet = AppC.RC_CANCEL;
                                xWFocus = (tVsego.Enabled)?tVsego:tMest;
                            }
                        }
                        break;
                    case AppC.F_CHG_REC:
                        // редактирование окончено - попытка сохранения изменений
                        if (bShowTTN == true)
                        {
                        }
                        break;
                }
                //if ((scCur.nParty == 0)||(scCur.nParty == AppC.EMPTY_INT))
                //{
                //    if (scCur.tTyp == AppC.TYP_TARA.TARA_TRANSP)
                //    {
                //        Srv.ErrorMsg("Партия не указана!");
                //        nRet = AppC.RC_CANCEL;
                //    }
                //    break;
                //}


                //if ((scCur.dDataIzg == DateTime.MinValue)&&(scCur.dDataGodn == DateTime.MinValue))
                //{// нет ни одной даты
                //    if ((scCur.tTyp != AppC.TYP_TARA.TARA_POTREB) || (bEditMode == false))
                //    {
                //        Srv.ErrorMsg("Нет даты!");
                //        //nRet = AppC.RC_CANCEL;
                //        break;
                //    }
                //}

                if ((scCur.dDataIzg == DateTime.MinValue) && (scCur.dDataGodn == DateTime.MinValue))
                {// нет ни одной даты
                    if (tDatIzg.Enabled)
                    {
                        xWFocus = tDatIzg;
                    }
                    if (tDatMC.Enabled)
                    {
                        xWFocus = tDatMC;
                    }
                    Srv.ErrorMsg("Нет даты!");
                    nRet = AppC.RC_CANCEL;
                    break;
                }

                if ((scCur.nPrzvFil < 0 ) && tPrzvFil.Enabled )
                {
                    nRet = AppC.RC_CANCEL;
                    v.cWhereFocus = tPrzvFil;
                    break;
                }

            } while (false);
            #endregion

            v.nRet = nRet;
            v.cWhereFocus = null;
            if (bEditMode)
                aEdVvod.Current.Text = sSaved;
            
            return (v);

        }

        // окончание ввода/редактирования
        private void EditEndDet(int nReg)
        {
            bool 
                bReRead = false;

            if (nReg == AppC.CC_NEXTOVER)
            {// успешное окончание ввода
                bReRead = true;
                switch (nCurVvodState)
                {
                    case AppC.F_ADD_REC:
                    case AppC.F_ADD_SCAN:
                        // перечитываем только существующую запись ТТН
                        bReRead= !AddDet1(ref scCur);

                        if (bShowTTN == true)
                            SetDopFieldsForEnter(true);

                        break;
                    case AppC.F_CHG_REC:
                        SaveDetChange(1);
                        if ((bShowTTN == true) && (bLastScan == true))
                            SetDopFieldsForEnter(false);
                        break;
                }
                if (scCur.bAlienMC && scCur.bNewAlienPInf)
                {
                    string sK = scCur.sKMC + scCur.sIntKod;
                    if (dicAlienP.ContainsKey(sK))
                    {
                        dicAlienP[sK].nParty = scCur.nParty;
                        dicAlienP[sK].dV = scCur.dDataIzg;
                    }
                    else
                        dicAlienP.Add(sK, new PartyInf(scCur.nParty, scCur.dDataIzg));
                }
            }

            SetEditMode(false);
            //for (int i = 0; i < aEdVvod.Count; i++)
            //{
            //    aEdVvod[i].Enabled = false;
            //}

            if (bReRead == true)
                ChangeDetRow(true);

            ShowStatDoc();

            if ((nCurVvodState == AppC.F_ADD_SCAN) && (nReg == AppC.CC_NEXTOVER))
                bLastScan = true;

            nCurVvodState = AppC.DT_SHOW;
            if (xCDoc.bTmpEdit)
            {
                SetEasyEdit(AppC.REG_SWITCH.SW_SET);
                xCDoc.bTmpEdit = false;
            }
            aEdVvod.EditIsOver(dgDet);
            //dgDet.Focus();
        }



        private bool MayAddDefaultAdr()
        {
            bool
                ret = true;
            string
                sDefAdr;
            if (xPars.UseAdr4DocMode)
            {
                sDefAdr = String.Format("USID{0}{1}", xPars.MACAdr, xSm.sUser);
                AddrInfo xA = new AddrInfo(sDefAdr, xSm.nSklad);
                xA.ScanDT = DateTime.Now;
                if (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.PRIHOD)
                {
                    if (!xCDoc.xDocP.DType.AdrFromNeed)
                    {// адрес-источник можно установить по умолчанию
                        if (xCDoc.xOper.GetSrc(false).Length == 0)
                        {
                            xCDoc.xOper.SetOperSrc(xA, xCDoc.xDocP.DType);
                        }
                    }
                }
                if (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.RASHOD)
                {
                    if (!xCDoc.xDocP.DType.AdrToNeed)
                    {// адрес-источник можно установить по умолчанию
                        if (xCDoc.xOper.GetDst(false).Length == 0)
                        {
                            xCDoc.xOper.SetOperDst(xA, xCDoc.xDocP.DType);
                        }
                    }
                }

            }

            return (ret);
        }

        // добавление записи в детальные строки ТТН
        private bool AddDet1(ref PSC_Types.ScDat scForAdd)
        { 
            DataRow d = null;
            return( AddDet1(ref scForAdd, out d) );
        }



        private bool AddDet1(ref PSC_Types.ScDat scForAdd, out DataRow dr)
        {
            bool
                bNewRec = false,
                bOperReady;
            int
                nRet;

            // добавляем новую или суммируем в существующей
            //dr = null;
            //if (xCDoc.xDocP.nTypD != AppC.TYPD_RASHNKLD)
            dr = WhatRegAdd(ref scForAdd);

            if (dr == null)
                bNewRec = true;

            nOldMest = scForAdd.nMest;
            fOldVsego = scForAdd.fVsego;
            fOldVes = scForAdd.fVes;


            if (scForAdd.nRecSrc == (int)NSI.SRCDET.HANDS)
                scForAdd.dtScan = DateTime.Now;
            dr = xNSI.AddDet(scForAdd, xCDoc, dr);
            if (dr != null)
            {

                xCDoc.xOper.SetOperObj(dr, xCDoc.xDocP.DType);
                MayAddDefaultAdr();

                if (bShowTTN == true)
                {// встать на добавленную/скорректированную
                    if (drDet == null)
                    {
                        drDet = dr;
                        SetDetFields(false);
                    }
                    int nOldRec = GetRecNoInGrid(dr);
                    if (nOldRec != -1)
                        dgDet.CurrentRowIndex = nOldRec;
                }

                ShowStatDoc();

                bOperReady = (xCDoc.xOper.nOperState & AppC.OPR_STATE.OPR_READY) > 0;

                if (bOperReady)
                {// операция готова к отправке, все введено
                    if (IsOperReady() != AppC.RC_OK)
                    {
                        //if (bNewRec)
                        //{
                        //    xNSI.DT[NSI.BD_DOUTD].dt.Rows.Remove(dr);
                        //    bNewRec = true;
                        //}
                    }
                    //xCDoc.xOper = new CurOper(xCDoc.xDocP.DType);
                }
                else
                    ShowOperState(xCDoc.xOper);

                if (dr != null)
                {
                    EvalZVKStateNew(ref scForAdd, dr, bNewRec);
                    //MayAddSSCC(xScan.Dat, ref scForAdd);
                    AfterAddScan(this, new EventArgs());
                }

            }
            return (bNewRec);
        }

















        private int GetRecNoInGrid(DataRow dr)
        {
            int nPos = -1;
            DataView dv = ((DataTable)dgDet.DataSource).DefaultView;
            for (int i = 0; i < dv.Count; i++)
            {
                if (dv[i].Row == dr)
                {
                    nPos = i;
                    break;
                }
            }

            return(nPos);
        }

        // определение режима добавления отсканированных сведений: новая запись или накопление
        private DataRow WhatRegAdd(ref PSC_Types.ScDat sc)
        {
            int 
                nDocType = xCDoc.xDocP.nNumTypD;
            DataRow 
                ret = null;

            //if (!xPars.UseAdr4DocMode)
            //{//фиксируются все операции
            //    if (// это весовой и он не суммируется
            //        //((xPars.aDocPars[nDocType].bSumVes == false) && (sc.bVes == true)) ||
            //        ((xCDoc.xDocP.DType.MoveType != AppC.MOVTYPE.AVAIL) && (sc.bVes == true)) ||
            //        (nDocType == AppC.TYPD_RASHNKLD) ||
            //        (nDocType == AppC.TYPD_BRAK) ||
            //        (xCDoc.xDocP.nTypOp != AppC.TYPOP_DOCUM))
            //        ret = null;
            //    else
            //    {
            //        if ((sc.fEmk == 0) ||
            //            (sc.tTyp == AppC.TYP_TARA.TARA_POTREB))
            //            ret = sc.drEd;
            //        else
            //            ret = sc.drMest;
            //    }
            //}

            if (!xPars.UseAdr4DocMode)
            {//для адресного - ничего не суммируется
                if (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.AVAIL)
                {
                    if ((sc.fEmk == 0) ||
                        (sc.tTyp == AppC.TYP_TARA.TARA_POTREB))
                        ret = sc.drEd;
                    else
                        ret = sc.drMest;
                }
            }
            else
            {
                if (sc.bReWrite)
                {
                    ret = drDet;
                }
            }

            return (ret);
        }


        /// установка статуса заявки после ввода/корректировки
        private void EvalZVKStateNew(ref PSC_Types.ScDat sc, DataRow drTTN)
        {
            EvalZVKStateNew(ref sc, drTTN, true);
        }

        /// установка статуса заявки после ввода/корректировки
        private void EvalZVKStateNew(ref PSC_Types.ScDat sc, DataRow drTTN, bool bNewRow)
        {
            int 
                nAddMest = 0,
                nMz = 0;
            FRACT 
                fAddEd = 0,
                fVz = 0;

            if (bZVKPresent == true)
            {// заявка имеется

                #region Что из статусов заявки может поменяться
                do
                {
                    if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
                    {//27.02

                        //int nM = 0;
                        //FRACT fV = 0;
                        //DataRow drZ = PrevKol(ref sc, ref nM, ref fV);

                        //if (drZ is DataRow)
                        //{
                        //    drTTN["NPP_ZVK"] = drZ["NPP"];

                        //    if (!sc.bVes)
                        //    {
                        //        if (((int)drZ["KOLM"] <= (sc.nMest + nM)) &&
                        //            (FRACT)drZ["KOLE"] <= (sc.fVsego + fV))
                        //            drZ["READYZ"] = NSI.READINESS.FULL_READY;
                        //        else
                        //            drZ["READYZ"] = NSI.READINESS.PART_READY;
                        //    }
                        //    else
                        //    {
                        //        if ((int)drZ["KOLM"] <= (sc.nMest + nM))
                        //            drZ["READYZ"] = NSI.READINESS.FULL_READY;
                        //        else
                        //            drZ["READYZ"] = NSI.READINESS.PART_READY;
                        //    }
                        //}
                        //else
                        //    drTTN["NPP_ZVK"] = -1;

                        int
                            nM = 0,
                            nMFull = 0;
                        FRACT
                            fV = 0,
                            fVFull = 0;
                        DataRow drZ = PrevKol(ref sc, ref nM, ref fV);
                        nMFull = nM;
                        fVFull = fV;

                        if (bNewRow)
                        {// коррктировки еще не вошли
                            nMFull += sc.nMest;
                            fVFull += sc.fVsego;
                        }

                        if (drZ is DataRow)
                        {
                            drTTN["NPP_ZVK"] = drZ["NPP"];

                            if (!sc.bVes)
                            {
                                if (((int)drZ["KOLM"] <= nMFull) &&
                                    (FRACT)drZ["KOLE"] <= fVFull)
                                    drZ["READYZ"] = NSI.READINESS.FULL_READY;
                                else
                                    drZ["READYZ"] = NSI.READINESS.PART_READY;
                            }
                            else
                            {
                                if ((int)drZ["KOLM"] <= nMFull)
                                    drZ["READYZ"] = NSI.READINESS.FULL_READY;
                                else
                                    drZ["READYZ"] = NSI.READINESS.PART_READY;
                            }
                        }
                        else
                            drTTN["NPP_ZVK"] = -1;

                        break;
                    }

                    if (sc.fEmk > 0)
                    {// что закрывают введенные места
                        nMz = sc.nMest;
                        if (sc.drTotKey != null)
                        {// при вводе мест могли закрыть конкретную партию
                            nMz = (int)sc.drTotKey["KOLM"] - (sc.nKolM_alrT + nMz);
                            if (nMz <= 0)
                                // заявка закрывается
                                sc.drTotKey["READYZ"] = NSI.READINESS.FULL_READY;
                            if (nMz >= 0)
                                // больше распределять нечего
                                break;
                            nMz = Math.Abs(nMz);
                        }
                        else
                            nAddMest = sc.nKolM_alrT;

                        if (sc.drPartKey != null)
                        {// емкость имелась, могли ввести места
                            nMz = (int)sc.drPartKey["KOLM"] - (sc.nKolM_alr + nAddMest + nMz);
                            if (nMz <= 0)
                                // заявка закрывается
                                sc.drPartKey["READYZ"] = NSI.READINESS.FULL_READY;
                        }
                    }
                    else
                    {// что закрывают введенные единицы
                        fVz = sc.fVsego;
                        if (sc.drTotKeyE != null)
                        {// при вводе единиц могли закрыть конкретную партию
                            fVz = (FRACT)sc.drTotKeyE["KOLE"] - (sc.fKolE_alrT + fVz);
                            if (fVz <= 0)
                                // заявка закрывается
                                sc.drTotKeyE["READYZ"] = NSI.READINESS.FULL_READY;
                            if (fVz >= 0)
                                // больше распределять нечего
                                break;
                            fVz = Math.Abs(fVz);
                        }
                        else
                            fAddEd = sc.fKolE_alrT;
                        if (sc.drPartKeyE != null)
                        {// при вводе единиц могли их и закрыть
                            fVz = (FRACT)sc.drPartKeyE["KOLE"] - (sc.fKolE_alr + fAddEd + fVz);
                            if (fVz <= 0)
                                // заявка закрывается
                                sc.drPartKeyE["READYZ"] = NSI.READINESS.FULL_READY;
                        }
                    }
                } while (false);

                #endregion

            }
        }









        // сохранение корректировки
        private int SaveDetChange(int nReg)
        {
            int nRet = AppC.RC_OK;
            int nM;
            FRACT fV, fVess;

            try
            {
                if (scCur.bFindNSI == true)
                {
                    drDet["KRKMC"] = (int)scCur.nKrKMC;
                    drDet["SNM"] = scCur.sN;
                    drDet["EAN13"] = (string)scCur.sEAN;
                }

                if ((nOldMest != scCur.nMest) || (fOldVsego != scCur.fVsego))
                    ClearZVKState(scCur.sKMC);

                if (bLastScan == true)
                {// корректировка последних введенных данных
                    if (((scCur.nMest == 0) && (nOldMest != scCur.nMest)) ||
                        ((scCur.nMest == 0) && (scCur.fVsego == 0.0M)))
                    {// отмена последнего сканирования
                        scCur.fVsego = 0;
                        scCur.fVes = 0;
                    }
                    nM = ((int)drDet["KOLM"] - nOldMest) + scCur.nMest;
                    nOldMest = scCur.nMest;
                    fV = ((FRACT)drDet["KOLE"] - fOldVsego) + scCur.fVsego;
                    fOldVsego = scCur.fVsego;
                    fVess = ((FRACT)drDet["VES"] - fOldVes) + scCur.fVes;
                    fOldVes = scCur.fVes;
                }
                else
                {
                    nM = scCur.nMest;
                    fV = scCur.fVsego;
                    fVess = scCur.fVes;
                }

                drDet["NP"] = scCur.nParty;
                drDet["EMK"] = scCur.fEmk;

                drDet["KOLM"] = nM;
                drDet["KOLE"] = fV;
            }
            catch
            {
                MessageBox.Show("Ошибка корректировки!");
            }

            return (nRet);

        }

        private void ZVKStyle()
        {
        }


        // --- смена таблицы
        private void ChgDetTable(DataRow drNew, string sNeededTable)
        {
            string 
                sRf = xCDoc.DefDetFilter();
            int
                ts = (int)NSI.TABLESORT.NO;
            DataGridCell dgCur = dgDet.CurrentCell;

            dgDet.SuspendLayout();
            if (((bShowTTN == false) && (sNeededTable == "")) ||
                (sNeededTable == NSI.BD_DOUTD) )
            {// текущая - заявки, устанавливается ТТН
                dgDet.DataSource = xNSI.DT[NSI.BD_DOUTD].dt;
                //tVvodReg.Text = "ТТН";
                bShowTTN = true;
                ts = xNSI.DT[NSI.BD_DOUTD].TSort;
                if ((int)xCDoc.drCurRow["TYPOP"] == AppC.TYPOP_KMPL)
                {
                    if (xCDoc.xNPs.Current > 0)
                    {
                        if (xNSI.DT[NSI.BD_DOUTD].sTFilt != "")
                            sRf += xNSI.DT[NSI.BD_DOUTD].sTFilt;
                    }
                }
            }
            else
            {// сейчас текущая - ТТН, устанавливается зявки
                dgDet.DataSource = xNSI.DT[NSI.BD_DIND].dt;
                //tVvodReg.Text = "Заявка";
                bShowTTN = false;
                ts = xNSI.DT[NSI.BD_DIND].TSort;
                sRf += xNSI.DT[NSI.BD_DIND].sTFilt;
                if ((xCDoc.xDocP.TypOper == AppC.TYPOP_KMPL) ||
                    (xCDoc.xDocP.TypOper == AppC.TYPOP_OTGR))
                {
                    xNSI.ChgGridStyle(NSI.BD_DIND, NSI.GDET_ZVK_KMPL);
                    if (xCDoc.xNPs.Current > 0)
                    {
                        if (xNSI.DT[NSI.BD_DOUTD].sTFilt != "")
                            sRf += xNSI.DT[NSI.BD_DOUTD].sTFilt;
                    }
                }
                else
                {
                    xNSI.ChgGridStyle(NSI.BD_DIND, NSI.GDET_ZVK);
                }
            }
            ShowRegVvod();

            ((DataTable)dgDet.DataSource).DefaultView.RowFilter = sRf;
            AdjustCurRow(dgCur, drNew);

            xNSI.SortName(bShowTTN, ref sRf, false);
            lSortInf.Text = sRf;
            dgDet.ResumeLayout();
            ShowStatDoc();
        }

        // позиционирование на нужну DataRow в гриде
        private void AdjustCurRow(DataGridCell dgOldCell, DataRow drNew)
        {
            bool bRowChanged = false;
            int nI = dgDet.CurrentRowIndex;
            DataGridCell dgCur = dgDet.CurrentCell;

            CurrencyManager cmDet = (CurrencyManager)BindingContext[dgDet.DataSource];
            int nOldPos = cmDet.Position;

            if (drNew != null)
            {
                nOldPos = GetRecNoInGrid(drNew);
            }

            if (((dgDet.VisibleRowCount > 0)&& ((nOldPos) >= dgDet.VisibleRowCount)) || (drNew != null))
            {
                //dgDet.CurrentRowIndex = 0;
                if (nOldPos > -1)
                {
                    dgDet.CurrentRowIndex = nOldPos;
                    bRowChanged = true;
                }
                else
                    Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
            }
            if (bRowChanged == false)
            {
                //if (dgOldCell.Equals(dgCur) == true)
                    ChangeDetRow(true);
            }
        }

        // --- смена текущего документа
        private void SetNextPrevDoc(int nFunc)
        {
            bool 
                bChanged = false;
            CurrencyManager 
                cmDoc = (CurrencyManager)BindingContext[dgDoc.DataSource];

            if (cmDoc.Count > 0)
            {
                if (nFunc == AppC.F_PREVDOC)
                {
                    if (cmDoc.Position > 0)
                    {
                        cmDoc.Position--;
                        bChanged = true;
                    }
                }
                else
                {
                    if (cmDoc.Position < cmDoc.Count - 1)
                    {
                        cmDoc.Position++;
                        bChanged = true;
                    }
                }
                if (bChanged == true)
                {
                    xCDoc.drCurRow = ((DataRowView)cmDoc.Current).Row;
                    xNSI.InitCurDoc(xCDoc, xSm);
                    SetParFields(xCDoc.xDocP);

                    NewDoc((DataTable)this.dgDet.DataSource);
                    lDocInf.Text = CurDocInf(xCDoc.xDocP);
                }
            }
        }

        // --- удаление детальной строки
        private void DelDetDoc(int nFunc)
        {
            DataTable 
                dtDel = ((DataTable)this.dgDet.DataSource);
            //!!! if (dtDel == xNSI.DT[NSI.BD_DOUTD].dt)
                
            if (dtDel == xNSI.DT[NSI.BD_DOUTD].dt)
                {
                DataView 
                    dvDetail = dtDel.DefaultView;
                int 
                    ret = dvDetail.Count;
                if (ret >= 1)
                {
                    if (nFunc == AppC.F_DEL_REC)
                    {
                        ClearZVKState( (string)dvDetail[this.dgDet.CurrentRowIndex].Row["KMC"] );
                        dtDel.Rows.Remove(dvDetail[this.dgDet.CurrentRowIndex].Row);
                        xCDoc.xOper.SetOperObj(null, xCDoc.xDocP.DType);
                    }
                    else
                    {
                        DialogResult dr = MessageBox.Show("Отменить удаление всех (Enter)?\r\n(ESC) - все удалить без сомнений",
                            "Удаляются все строки!",
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                        if (dr != DialogResult.OK)
                        {
                            DataRow[] drMDetZ = xCDoc.drCurRow.GetChildRows(xNSI.dsM.Relations[NSI.REL2TTN]);
                            foreach (DataRow drDel in drMDetZ)
                            {
                                xNSI.dsM.Tables[NSI.BD_DOUTD].Rows.Remove(drDel);
                            }
                            ClearZVKState("");
                        }
                    }
                    ChangeDetRow(false);
                    xCDoc.drCurRow["DIFF"] = NSI.DOCCTRL.UNKNOWN;
                }
            }
        }

        //// сброс статуса заявки
        //private void ClearZVKState(string sEAN)
        //{
        //    // фильтр - SYSN + EAN13
        //    string sRf = ((DataTable)dgDet.DataSource).DefaultView.RowFilter;
        //    if (sEAN != "")
        //        sRf += String.Format("AND(EAN13='{0}')", sEAN);

        //    // вся продукция с данным кодом по заявке
        //    DataView dv = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "", DataViewRowState.CurrentRows);
        //    for (int i = 0; i < dv.Count; i++)
        //    {
        //        dv[i].Row["READYZ"] = NSI.READINESS.NO;
        //    }
        //}

        // сброс статуса заявки
        private void ClearZVKState(string sK)
        {// фильтр - SYSN + KRKMC
            string 
                sRf = ((DataTable)dgDet.DataSource).DefaultView.RowFilter;
            if (sK.Length > 0)
            {
                sRf += String.Format("AND(KMC='{0}')", sK);
                // вся продукция с данным кодом по заявке
            }
            DataView dv = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "", DataViewRowState.CurrentRows);
            for (int i = 0; i < dv.Count; i++)
                dv[i].Row["READYZ"] = NSI.READINESS.NO;
        }


        // показать статистику по документу
        private void ShowTotMest()
        {
            try
            {
                xInf = aKMCName(CurDocInf(xCDoc.xDocP), true);
                TotMest(NSI.REL2TTN, xInf);
                TotMest(NSI.REL2ZVK, xInf);
                xHelpS.ShowInfo(xInf, ref ehCurrFunc);
            }
            catch { }
        }

        // всего мест по списку продукции (заявка или ТТН)
        private int TotMest(string sRel, List<String> xI)
        {
            int
                nState,
                nTTNTrans = 0,
                nTTNReady = 0,
                nMTTN = 0;
            FRACT
                fTotKolE = 0,
                fTotVes = 0;
            DataRow[] 
                chR = xCDoc.drCurRow.GetChildRows(sRel);
            try
            {
                foreach (DataRow dr in chR)
                {
                    nMTTN += (int)dr["KOLM"];
                    if (sRel == NSI.REL2TTN) 
                    {
                        if ((int)dr["SRP"] > 0)
                        {
                            fTotVes += (FRACT)dr["KOLE"];
                        }
                        else
                            fTotKolE += (FRACT)dr["KOLE"];
                        nState = (int)dr["STATE"] & (int)AppC.OPR_STATE.OPR_READY;
                        if (nState > 0)
                            nTTNReady++;
                     
                        nState = (int)dr["STATE"] & (int)AppC.OPR_STATE.OPR_TRANSFERED;
                        if (nState > 0)
                            nTTNTrans++;
                    }
                    else
                    {
                        fTotKolE += (FRACT)dr["KOLE"];
                        nState = (int)dr["READYZ"] & (int)NSI.READINESS.FULL_READY;
                        if (nState > 0)
                            nTTNReady++;
                    }
                    
                }
            }
            catch { }
            if (xI != null)
            {
                xI.Add(String.Format("  Строк в {0} - {1}", (sRel == NSI.REL2TTN) ? "ТТН" : "Заявке", chR.Length));
                if (sRel == NSI.REL2TTN) 
                {
                    xI.Add(String.Format("    из них выгружено - {0}", nTTNTrans));
                    xI.Add(String.Format("    из них готово к выгрузке - {0}", nTTNReady));
                }
                else
                    xI.Add(String.Format("    из них выполнено - {0}", nTTNReady));


                xI.Add(String.Format("    мест - {0}, вес - {1}", nMTTN, fTotVes));
                xI.Add(String.Format("    штук - {0}", fTotKolE));
            }
            return (nMTTN);
        }

        // показать статистику по продукции
        private void ShowTotMestProd()
        {
            try
            {
                if (drDet != null)
                {
                    xInf = aKMCName((string)drDet["SNM"], true);
                    TotMestProd(xNSI.DT[NSI.BD_DOUTD].dt, true, xInf);
                    TotMestProd(xNSI.DT[NSI.BD_DIND].dt, false, xInf);
                    xHelpS.ShowInfo(xInf, ref ehCurrFunc);
                }
            }
            catch { }
        }




        private int TotMestProd(DataTable dtD, bool bIsTTN, List<String> xI)
        {
            int
                nState,
                nTTNTrans = 0,
                nTTNReady = 0,
                nMTTN = 0;
            FRACT
                fTotKolE = 0,
                fTotVes = 0;
            DataRow
                dr;
            DateTime
                dGodnCurr;
            AddrInfo
                xa;
            string
                sA;

            string sRf = String.Format("(SYSN={0})AND(KMC='{1}')", drDet["SYSN"], drDet["KMC"]);

            DataView 
                dv = new DataView(dtD, sRf, "DTG", DataViewRowState.CurrentRows);

            try
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    dr = dv[i].Row;
                    nMTTN += (int)dr["KOLM"];
                    if (bIsTTN)
                    {
                        if ((int)dr["SRP"] > 0)
                        {
                            fTotVes += (FRACT)dr["KOLE"];
                        }
                        else
                            fTotKolE += (FRACT)dr["KOLE"];
                        nState = (int)dr["STATE"] & (int)AppC.OPR_STATE.OPR_READY;
                        if (nState > 0)
                            nTTNReady++;

                        nState = (int)dr["STATE"] & (int)AppC.OPR_STATE.OPR_TRANSFERED;
                        if (nState > 0)
                            nTTNTrans++;
                    }
                    else
                    {
                        fTotKolE += (FRACT)dr["KOLE"];
                        nState = (int)dr["READYZ"] & (int)NSI.READINESS.FULL_READY;
                        if (nState > 0)
                            nTTNReady++;
                    }
                    xa = new AddrInfo((string)dr["ADRFROM"], xSm.nSklad);
                    try
                    {
                        dGodnCurr = DateTime.ParseExact((string)dr["DTG"], "yyyyMMdd", null);
                    }
                    catch
                    {
                        dGodnCurr = DateTime.MinValue;
                    }
                    sA = (xa.AddrShow.Length > 0)? xa.AddrShow:" ".PadRight(10);
                    xI.Add(String.Format("{0} {1} {2} {3}",
                        sA,
                        dGodnCurr.ToString("dd.MM.yy"),
                        dr["KOLM"].ToString().PadLeft(4),
                        dr["KOLE"].ToString().PadLeft(7)));
                }
            }
            catch { }
            if (xI != null)
            {
                xI.Add(String.Format("  Строк в {0} - {1}", (bIsTTN) ? "ТТН" : "Заявке", dv.Count));
                if (bIsTTN)
                {
                    xI.Add(String.Format("    из них выгружено - {0}", nTTNTrans));
                    xI.Add(String.Format("    из них готово к выгрузке - {0}", nTTNReady));
                }
                else
                    xI.Add(String.Format("    из них выполнено - {0}", nTTNReady));


                xI.Add(String.Format("    мест - {0}, вес - {1}", nMTTN, fTotVes));
                xI.Add(String.Format("    штук - {0}", fTotKolE));
            }
            return (nMTTN);
        }


        private void ShowTotMestAll(string nK, string nP, string sNamenK)
        {
            int nM,
                nMK, nMKP;
            FRACT fV = 0,
                fVK = 0, fVKP;
            try
            {

                nM = TotMestAll(NSI.REL2TTN, nK, nP, out fV, 
                    out nMK, out fVK,
                    out nMKP, out fVKP);

                string sKP = String.Format("{0} П.№ {1} мест {2}\n               ед. {3}", sNamenK, nP, nMKP, fVKP);
                //string sK = String.Format("{0}        мест {1}\n               ед. {2}", sNamenK, nMK, fVK);
                string sK = String.Format("{0}        мест {1}\n               ед. {2}", "", nMK, fVK);
                string sM = String.Format("Всего мест {0} \nВсего вес {1}", nM, fV);

                string s = sKP + "\n" + sK + "\n" + sM + "\n" + "===== Заявка =====" + "\n";


                nM = TotMestAll(NSI.REL2ZVK, nK, nP, out fV,
                    out nMK, out fVK,
                    out nMKP, out fVKP);

                //sKP = String.Format("{0} П.№ {1} мест {2}\n               ед. {3}", sNamenK, nP, nMKP, fVKP);
                //sK = String.Format("{0}        мест {1}\n               ед. {2}", sNamenK, nMK, fVK);
                sKP = String.Format("{0} П.№ {1} мест {2}\n               ед. {3}", "", nP, nMKP, fVKP);
                sK = String.Format("{0}        мест {1}\n               ед. {2}", "", nMK, fVK);
                sM = String.Format("Всего мест {0} \nВсего вес {1}", nM, fV);

                s += sKP + "\n" + sK + "\n" + sM;

                MessageBox.Show(s);
            }
            catch { }
        }

        private int TotMestAll(string sRel, string nKrKMC, string nParty, out FRACT fTotVes,
                        out int nMKrKMC, out FRACT fTotVesKrKMC,
                        out int nMKrKMCP, out FRACT fTotVesKrKMCP)
        {
            int nMTTN = 0;
            fTotVes = 0;

                        
            nMKrKMC = nMKrKMCP = 0;
            fTotVesKrKMC = fTotVesKrKMCP = 0;

            try
            {
                DataRow[] chR = xCDoc.drCurRow.GetChildRows(sRel);
                foreach (DataRow dr in chR)
                {
                    nMTTN += (int)dr["KOLM"];
                    if ((int)dr["SRP"] > 0)
                    {
                        fTotVes += (FRACT)dr["KOLE"];
                    }
                    if (nKrKMC == (string)dr["KMC"])
                    {
                        nMKrKMC += (int)dr["KOLM"];
                        fTotVesKrKMC += (FRACT)dr["KOLE"];
                        if (nParty == (string)dr["NP"])
                        {
                            nMKrKMCP += (int)dr["KOLM"];
                            fTotVesKrKMCP += (FRACT)dr["KOLE"];
                        }
                    }
                }
            }
            catch { }
            return (nMTTN);
        }




        private string HeadLineCtrl(DataRow dr)
        {
            int
                nT = AppC.TYPD_SVOD;
            string s = "",
                sData = "",
                sSmena = " См:",
                sEks = " Экс: ",
                sPol = " Пол: ";

            try
            {
                nT = (int)dr["TD"];
                s = TName(nT) + ":";
                sData = (string)dr["EXPR_DT"];
                sSmena += (string)dr["KSMEN"];
                sEks += dr["KEKS"].ToString();
                sPol += dr["KRKPP"].ToString();
            }
            catch
            {
            }

            s += sData;

            switch(nT){
                case AppC.TYPD_SVOD:
                    s += sEks + sSmena;
                    break;
                case AppC.TYPD_PRIHGP:
                    s += sEks + sSmena + sPol ;
                    break;
                case AppC.TYPD_VOZV:
                    s += sSmena + sPol;
                    break;
                case AppC.TYPD_PERMGP:
                    s += sSmena + sPol;
                    break;
            }
            return (s);
        }

        //// разница между заявкой и отгрузкой
        //private int EvalDiffZVK(ref PSC_Types.ScDat sc, DataView dvZ, DataView dvT, List<string> lstProt,
        //    int iZ, int iZMax, ref int iT, int iTMax)
        //{
        //    bool bNeedSetZVK = false;
        //    int nRet = AppC.RC_OK;
        //    int nM = 0;
        //    FRACT fV = 0;
        //    NSI.READINESS rpEmk = NSI.READINESS.NO;

        //    if (sc.fEmk > 0)
        //    {
        //        if (sc.nKolM_zvk > 0)
        //        {
        //            bNeedSetZVK = true;
        //            nM = sc.nKolM_zvk - (sc.nKolM_alr + sc.nKolM_alrT);
        //            if (nM > 0)
        //            {// чего-то там осталось, по местам заявка не выполнена
        //                nRet = AppC.RC_CANCEL;
        //                rpEmk = NSI.READINESS.PART_READY;
        //                lstProt.Add(String.Format("_{0}:недостача-{1} М", sc.nKrKMC, nM));
        //            }
        //            else
        //            {
        //                if (nM < 0)
        //                {// перебор по местам, сообщение
        //                    nRet = AppC.RC_WARN;
        //                    lstProt.Add(String.Format(" {0}:лишние {1} М", sc.nKrKMC, Math.Abs(nM)));
        //                }
        //                rpEmk = NSI.READINESS.FULL_READY;
        //            }
        //        }
        //        else
        //        {
        //            nRet = AppC.RC_CANCEL;
        //            rpEmk = NSI.READINESS.PART_READY;
        //            lstProt.Add(String.Format("_{0}:нет в заявке-{1} М",
        //                sc.nKrKMC, (sc.nKolM_alr + sc.nKolM_alrT)));
        //        }

        //        // установка признаков в массивах
        //        try
        //        {
        //            iT = SetTTNState(dvT, sc.nKrKMC, sc.fEmk, NSI.DESTINPROD.PARTZ, iT, iTMax, sc.sKMC);
        //            if (bNeedSetZVK == true)
        //            {
        //                SetZVKState(dvZ, sc.nKrKMC, sc.fEmk, rpEmk, iZ, iZMax, sc.sKMC);
        //                //while ((iZ < iZMax) && ((int)dvZ[iZ]["KRKMC"] == sc.nKrKMC))
        //                //{
        //                //    if (sc.fEmk == (FRACT)dvZ[iZ]["EMK"])
        //                //        dvZ[iZ]["READYZ"] = rpEmk;
        //                //    iZ++;
        //                //}
        //            }
        //        }
        //        catch { }


        //    }
        //    else
        //    {
        //        if ((sc.fKolE_zvk > 0) || ((sc.fKolE_alr + sc.fKolE_alrT) > 0))
        //        {// есть или заявка или сканы
        //            if (sc.fKolE_zvk > 0)
        //            {
        //                bNeedSetZVK = true;
        //                fV = sc.fKolE_zvk - (sc.fKolE_alr + sc.fKolE_alrT);

        //                if (fV > 0)
        //                {// чего-то там осталось
        //                    nRet = AppC.RC_CANCEL;
        //                    rpEmk = NSI.READINESS.PART_READY;
        //                    lstProt.Add(String.Format("_{0}:недостача-{1} Ед", sc.nKrKMC, fV));
        //                }
        //                else
        //                {
        //                    if (fV < 0)
        //                    {// перебор по единичкам, сообщение
        //                        nRet = AppC.RC_WARN;
        //                        lstProt.Add(String.Format(" {0}:лишние{1} Ед", sc.nKrKMC, Math.Abs(fV)));
        //                    }
        //                    rpEmk = NSI.READINESS.FULL_READY;
        //                }
        //            }
        //            else
        //            {
        //                nRet = AppC.RC_CANCEL;
        //                rpEmk = NSI.READINESS.PART_READY;
        //                lstProt.Add(String.Format("_{0}:нет в заявке-{1} Ед",
        //                    sc.nKrKMC, (sc.fKolE_alr + sc.fKolE_alrT)));
        //            }

        //            try
        //            {
        //                iT = SetTTNState(dvT, sc.nKrKMC, sc.fEmk, NSI.DESTINPROD.PARTZ, iT, iTMax, sc.sKMC);
        //                if (bNeedSetZVK == true)
        //                {
        //                    SetZVKState(dvZ, sc.nKrKMC, sc.fEmk, rpEmk, iZ, iZMax, sc.sKMC);
        //                    //while ((iZ < iZMax) && ((int)dvZ[iZ]["KRKMC"] == sc.nKrKMC))
        //                    //{
        //                    //    if (sc.fEmk == (FRACT)dvZ[iZ]["EMK"])
        //                    //        dvZ[iZ]["READYZ"] = rpEmk;
        //                    //    iZ++;
        //                    //}
        //                }
        //            }
        //            catch { }
        //        }
        //    }

        //    return (nRet);
        //}

        //private void SetZVKState(DataView dv, int nK, FRACT fE, NSI.READINESS rpE, int i, int nZMax, string sK)
        //{
        //    while ((i < nZMax) && IsSameKMC( dv[i].Row, nK, sK))
        //    {
        //        if (fE == (FRACT)dv[i]["EMK"])
        //            dv[i]["READYZ"] = rpE;
        //        i++;
        //    }
        //}

        //// установка признака
        //// - по всему коду (для RC_NOEAN), fE = -100
        //// - по всему коду и данной емкости (для RC_NOEAN)
        //private int SetTTNState(DataView dv, int nK, FRACT fE, NSI.DESTINPROD dSt, int i, int iMax, string sK)
        //{
        //    //int tss1 = Environment.TickCount;
        //    int nLastI = -1;
        //    while ((i < iMax) && IsSameKMC(dv[i].Row, nK, sK)) 
        //    {
        //        if ((fE == -100) || (fE == (FRACT)dv[i]["EMK"]))
        //        {
        //            dv[i]["DEST"] = dSt;
        //            nLastI = i;
        //        }
        //        i++;
        //    }
        //    //tss += (Environment.TickCount - tss1);
        //    return (nLastI);
        //}
        ////int tss = 0;





        // переход на такой же код в смежном документе
        private void GoSameKMC()
        {
            DataRow drNew = null;
            if (drDet != null)
            {// есть что искать
                try
                {
                    object[] xF = new object[] { (int)drDet["SYSN"], (int)drDet["KRKMC"] };

                    DataView dvEn = (bShowTTN == true) ? new DataView(xNSI.DT[NSI.BD_DIND].dt) :
                        new DataView(xNSI.DT[NSI.BD_DOUTD].dt);
                    dvEn.Sort = "SYSN,KRKMC";
                    int nR = dvEn.Find(xF);
                    if (nR > -1)
                        drNew = dvEn[nR].Row;
                    else
                    {
                        Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                    }
                }
                catch { }
            }
            ChgDetTable(drNew, "");
        }

        // перенос из заяки в отгруженные
        private void ZVK2TTN()
        {
            DataRow drNew = null;
            if ((drDet != null) && (bZVKPresent == true) && (bShowTTN == false))
            {// есть что искать
                try
                {
                    // попытаемся такой же найти
                    object[] xF = new object[] { (int)drDet["SYSN"], (int)drDet["KRKMC"], (FRACT)drDet["EMK"], 
                    (int)drDet["NP"]};

                    DataView dvEn = new DataView(xNSI.DT[NSI.BD_DOUTD].dt);
                    dvEn.Sort = "SYSN,KRKMC,EMK,NP";
                    int nR = dvEn.Find(xF);

                    if (nR > -1)
                    {// уже есть такой код
                        drNew = dvEn[nR].Row;
                        Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                        ChgDetTable(drNew, "");
                    }
                    else
                    {
                        DataRow drN = xNSI.DT[NSI.BD_DOUTD].dt.NewRow();
                        drN["SYSN"] = drDet["SYSN"];
                        drN["KRKMC"] = drDet["KRKMC"];
                        drN["SNM"] = drDet["SNM"];
                        drN["KOLM"] = drDet["KOLM"];
                        drN["KOLE"] = drDet["KOLE"];
                        drN["EMK"] = drDet["EMK"];
                        drN["NP"] = drDet["NP"];
                        drN["DVR"] = drDet["DVR"];
                        drN["EAN13"] = drDet["EAN13"];
                        drN["SRP"] = drDet["SRP"];
                        drN["KMC"] = drDet["KMC"];
                        drN["DEST"] = (int)NSI.DESTINPROD.USER;
                        drN["SRC"] = (int)NSI.SRCDET.FROMZ;
                        drN["TIMECR"] = DateTime.Now;

                        xNSI.DT[NSI.BD_DOUTD].dt.Rows.Add(drN);
                        Srv.PlayMelody(W32.MB_4HIGH_FLY);
                        MessageBox.Show("Перенесено...");
                    }
                }
                catch { }
            }
        }





        // описание полноэкранного и обычного режима
        // отображения грида детальных строк
        class ScrMode
        {
            // режимы отображения
            public enum SCRMODES : int
            {
                NORMAL      = 0,                        // 4-хстрочное отображение
                FULLMAX     = 1                         // весь экран полностью
            }

            private Point[] 
                xNameLoc,
                xLoc;
            private Size[] 
                xSize;
            private Control[] 
                xParent;
            private int[] 
                nTabI;
            
            private SCRMODES 
                nCur = SCRMODES.NORMAL;
            private int 
                nMaxReg = 2;
            
            private Control
                //xName,                                  // для вывода наименования
                xCtrl;                                  // грид


            public ScrMode(Control xC, Control xNameC)
            {
            
                int
                    nWMAddLoc = 0,
                    nWMMinSize = 0;
#if WMOBILE
            nWMAddLoc = 12;
            nWMMinSize = 12;
#endif                
                xCtrl = xC;
                xLoc  = new Point[] { 
                    new Point(xC.Location.X, xC.Location.Y), 
                    new Point(0, 0) };

                xNameLoc = new Point[] { 
                    new Point(xNameC.Location.X, 
                        xNameC.Location.Y), 
                    new Point(xNameC.Location.X, 
                        Screen.PrimaryScreen.Bounds.Height - xNameC.Height - 33 + nWMAddLoc) };

                xSize = new Size[]{ 
                    new Size(xC.Size.Width, xC.Size.Height),
                    new Size(Screen.PrimaryScreen.Bounds.Width, 
                        Screen.PrimaryScreen.Bounds.Height - xNameC.Height - 8 - nWMMinSize) };

                xParent = new Control[] { 
                    xC.Parent, 
                    xC.TopLevelControl };

                nTabI = new int[] { xC.TabIndex, 0 };
                nCur = SCRMODES.NORMAL;
            }

            // Текущий режим
            public SCRMODES CurReg
            {
                get { return (nCur); }
            }

            // Переключение на следующий режим
            public void NextReg(AppC.REG_SWITCH rgSW, Control xN)
            {
                if (rgSW == AppC.REG_SWITCH.SW_NEXT)
                {
                    nCur++;
                    if ((int)nCur == nMaxReg)
                        nCur = 0;
                }
                else
                    nCur = (rgSW == AppC.REG_SWITCH.SW_SET) ? SCRMODES.FULLMAX : SCRMODES.NORMAL;

                xCtrl.SuspendLayout();
                xCtrl.Parent = xParent[(int)nCur];
                xCtrl.TabIndex = nTabI[(int)nCur];
                xCtrl.Location = xLoc[(int)nCur];
                xCtrl.Size = xSize[(int)nCur];
                if (nCur != SCRMODES.NORMAL)
                    xCtrl.BringToFront();
                
                xCtrl.ResumeLayout();
                xCtrl.Focus();

                xN.SuspendLayout();
                xN.Location = xNameLoc[(int)nCur];
                if (nCur != SCRMODES.NORMAL)
                    xN.BringToFront();
                xN.ResumeLayout();
            }
        }

        // определение режима ввода
        private int IsGeneralEdit(ref PSC_Types.ScDat sc)
        {
            int nRet = AppC.RC_OK;
            int nM = 0;
            FRACT fV = 0;

            if (xScrDet.CurReg == ScrMode.SCRMODES.FULLMAX)
            {// полноэкранный режим
                if (bZVKPresent == true)
                {// заявка имеется
                    if (bShowTTN == false)
                    {// текущая - заявка
                        nRet = AppC.RC_CANCEL;
                        if (((sc.nMest > 0) || (sc.fVsego > 0)) && (sc.nDest != NSI.DESTINPROD.USER))
                        {
                            DataRow dr = null;
                            if (sc.nMest > 0)
                            {
                                if (sc.drTotKey != null)
                                {// при вводе мест можем закрыть конкретную партию
                                    nM = (int)sc.drTotKey["KOLM"] - (sc.nKolM_alrT + sc.nMest);
                                    if (nM >= 0)
                                    {
                                        if ((nM == 0) || (sc.bVes == true))
                                            dr = sc.drTotKey;
                                    }
                                }
                                if ((sc.drPartKey != null) && (dr == null))
                                {// при вводе мест можем закрыть
                                    nM = (int)sc.drPartKey["KOLM"] - (sc.nKolM_alr + sc.nMest);
                                    if (nM >= 0)
                                    {
                                        if ((nM == 0) || (sc.bVes == true))
                                            dr = sc.drPartKey;
                                    }
                                }
                                if (dr != null)
                                {// будем закрывать места
                                    if (sc.bVes == false)
                                        sc.fVsego = sc.nMest * sc.fEmk;
                                }
                            }
                            else
                            {
                                if (sc.drTotKeyE != null)
                                {// при вводе единиц можем закрыть конкретную партию
                                    fV = (FRACT)sc.drTotKeyE["KOLE"] - (sc.fKolE_alrT + sc.fVsego);
                                    if (fV >= 0)
                                    {
                                        if ((fV == 0) || (sc.bVes == true))
                                            dr = sc.drTotKeyE;
                                    }
                                }

                                if ((sc.drPartKeyE != null) && (dr == null))
                                {// при вводе единиц можем закрыть
                                    fV = (FRACT)sc.drPartKeyE["KOLE"] - (sc.fKolE_alr + sc.fVsego);
                                    if (fV >= 0)
                                    {
                                        if ((fV == 0) || (sc.bVes == true))
                                            dr = sc.drPartKeyE;
                                    }
                                }
                                if (dr != null)
                                {// будем закрывать единички
                                    sc.fEmk = 0;
                                }
                            }



                            if (dr != null)
                            {
                                if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
                                {
                                    if ((sc.nMAlr_NPP > 0) || (sc.fVAlr_NPP > 0))
                                        dr = null;
                                    else if ((sc.nMest < sc.nKolM_zvk) || (sc.fVsego < sc.fKolE_zvk))
                                        dr = null;
                                }
                            }

                            if (dr != null)
                            {// если отсканирован ITF-14
                                if ((sc.nParty.Length == 0) && (sc.s.Length <= 14))
                                    nRet = AppC.RC_NOTALLDATA;
                            }




                            if ((dr != null) && (nRet == AppC.RC_CANCEL))
                            {
                                nRet = AppC.RC_ALREADY;
                                PSC_Types.ScDat scOld = sc;
                                drEasyEdit = dr;
                                dgDet.CurrentRowIndex = GetRecNoInGrid(dr);

                                scCur = scOld;

                                if (bInEasyEditWait == false)
                                {
                                    bInEasyEditWait = true;
                                    ehCurrFunc += new Srv.CurrFuncKeyHandler(ZVKeyDown);
                                }
                                dgDet.Invalidate();
                            }
                        }
                    }
                    else
                        nRet = AppC.RC_BADTABLE;
                }
                else
                    nRet = AppC.RC_ZVKONLY;
            }
            return (nRet);
        }

        public static DataRow drEasyEdit = null;
        public static bool bInEasyEditWait = false;

        //private bool ZVKeyDown(int nFunc, KeyEventArgs e, ref PSC_Types.ScDat scN)
        //{
        //    if (scN.sTypDoc == scCur.sTypDoc)
        //    {
        //        nFunc = AppC.F_ZVK2TTN;
        //        //scCur = scN;
        //    }
        //    return (ZVKeyDown(nFunc, null));
        //}


        private bool ZVKeyDown(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        {
            bool bKeyHandled = true,
                bWriteData = false,
                bCloseEdit = true;
            int 
                nFunc = (int)nF,
                nNum = 0;
            DataRow
                drZ = null,
                drD = null;

            if (nFunc > 0)
            {
                switch (nFunc)
                {
                    case AppC.F_ZVK2TTN:
                        // подтверждение ввода сканом
                        bWriteData = true;
                        break;
                    case AppC.F_HELP:
                        bCloseEdit = false;
                        bKeyHandled = false;
                        break;
                    case AppC.F_PODD:
                        xCDoc.bTmpEdit = true;
                        bKeyHandled = false;
                        break;
                }
            }
            else
            {
                if (Srv.IsDigKey(e, ref nNum))
                {
                    bKeyHandled = false;
                    xCDoc.bTmpEdit = true;
                }
                else
                {
                    switch (e.KeyValue)
                    {
                        case W32.VK_RIGHT:
                        case W32.VK_LEFT:
                            bCloseEdit = false;
                            break;
                        case W32.VK_ENTER:
                            // есть ли все данные?
                            if (VerifyVvod().nRet == AppC.RC_CANCEL)
                                xCDoc.bTmpEdit = true;
                            else
                                bWriteData = true;
                            break;
                    }
                }
            }
            if (bCloseEdit == true)
            {
                drEasyEdit = null;
                if (bWriteData == true)
                {
                    Srv.PlayMelody(W32.MB_1MIDDL_HAND);
                    int nOldPos = dgDet.CurrentRowIndex;
                    bInEasyEditWait = false;

                    if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
                    {//27.02
                        int nM = 0;
                        FRACT fV = 0;
                        drZ = PrevKol(ref scCur, ref nM, ref fV);
                        if (!scCur.bVes)
                        {
                            scCur.nMest = (int)drZ["KOLM"] - nM;
                            scCur.fVsego = (FRACT)drZ["KOLE"] - fV;
                        }
                    }

                    AddDet1(ref scCur);
                    int nNewPos = dgDet.CurrentRowIndex;
                    if (nOldPos != nNewPos)
                    {
                        if (dgDet.VisibleRowCount > 0)
                        {
                        }
                    }
                }
                else
                {
                    Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                    Srv.PlayMelody(W32.MB_4HIGH_FLY);
                }
                ehCurrFunc -= ZVKeyDown;
                bInEasyEditWait = false;
                dgDet.Invalidate();
                if (xCDoc.bTmpEdit)
                {// переход в обычный режим
                    PSC_Types.ScDat scS = scCur;
                    SetEasyEdit(AppC.REG_SWITCH.SW_CLEAR);
                    scCur = scS;
                    SetDetFields(false);
                    SetDopFieldsForEnter(false);
                    AddOrChangeDet(AppC.F_ADD_SCAN);
                    if (nFunc != AppC.F_PODD)
                        W32.SimulKey(e.KeyValue, e.KeyValue);
                }
            }
            return (bKeyHandled);
        }

        private void SetDetFlt(AppC.REG_SWITCH rg)
        {
            string s = ((bShowTTN == false)&&(bZVKPresent))?NSI.BD_DIND : NSI.BD_DOUTD;
            SetDetFlt(s, rg);
        }

        // фильтр для таблицы детальных по поддону
        private void SetDetFlt(string sT, AppC.REG_SWITCH bForceSet)
        {
            string
                sFP = "";
            DataTable dt = ((DataTable)dgDet.DataSource);

            // циклическое переключение на следующий режим (установка/сброс)
            dgDet.SuspendLayout();
            int nCurPoddon = 0;
            try
            {
                nCurPoddon = xCDoc.xNPs.Current;
            }
            catch { nCurPoddon = 0; }
            if (nCurPoddon > 0)
                sFP = String.Format("AND(NPODDZ={0})", nCurPoddon);

            //sF = DefDetFilter();
            if ( ((xNSI.DT[sT].sTFilt == "") && (bForceSet == AppC.REG_SWITCH.SW_NEXT)) ||
                (bForceSet == AppC.REG_SWITCH.SW_SET))

            //if ((((xNSI.DT[NSI.BD_DOUTD].sTFilt == "")||
            //     ((sT == NSI.BD_DIND) && (xCDoc.FilterZVK == NSI.FILTRDET.UNFILTERED))) && 
            //    (bForceSet == AppC.REG_SWITCH.SW_NEXT)) ||
            //    (bForceSet == AppC.REG_SWITCH.SW_SET))
            {// выполняется установка
                if (sT == NSI.BD_DOUTD)
                {// установка для ТТН ставит и для заявок
                    xNSI.DT[NSI.BD_DOUTD].sTFilt = xNSI.DT[NSI.BD_DIND].sTFilt = sFP;
                    xSm.FilterTTN = NSI.FILTRDET.NPODD;
                    if (xSm.FilterZVK == NSI.FILTRDET.READYZ)
                        xNSI.DT[NSI.BD_DIND].sTFilt += String.Format("AND(READYZ<>{0})", (int)NSI.READINESS.FULL_READY);
                }
                if (sT == NSI.BD_DIND)
                {
                    xNSI.DT[sT].sTFilt = xNSI.DT[NSI.BD_DOUTD].sTFilt + 
                        String.Format("AND(READYZ<>{0})", (int)NSI.READINESS.FULL_READY);
                    xSm.FilterZVK = NSI.FILTRDET.READYZ;
                }
                //if ((sT == NSI.BD_DOUTD) ||
                //    (((DataTable)dgDet.DataSource).TableName == sT))
                //    ((DataTable)dgDet.DataSource).DefaultView.RowFilter = sF;
            }
            else
            {// выполняется сброс

                if (sT == NSI.BD_DOUTD)
                {// сброс для ТТН и для заявок
                    xNSI.DT[NSI.BD_DOUTD].sTFilt = xNSI.DT[NSI.BD_DIND].sTFilt = "";
                    xSm.FilterTTN = NSI.FILTRDET.UNFILTERED;
                    if (xSm.FilterZVK == NSI.FILTRDET.READYZ)
                        xNSI.DT[NSI.BD_DIND].sTFilt =
                            String.Format("AND(READYZ<>{0})", (int)NSI.READINESS.FULL_READY);
                }
                if (sT == NSI.BD_DIND)
                {
                    xNSI.DT[sT].sTFilt = "";
                    if (xSm.FilterTTN == NSI.FILTRDET.NPODD)
                        xNSI.DT[sT].sTFilt += sFP;
                    xSm.FilterZVK = NSI.FILTRDET.UNFILTERED;
                }
            }
            if ((sT == NSI.BD_DOUTD) || (dt.TableName == sT))
                dt.DefaultView.RowFilter = xCDoc.DefDetFilter() + xNSI.DT[dt.TableName].sTFilt;
            xNSI.SortName(bShowTTN, ref sFP, false);
            lSortInf.Text = sFP;
            dgDet.ResumeLayout();
        }


        // сброс полей текущей операции
        private void NewOper()
        {
            DataRow
                drObj = xCDoc.xOper.OperObj;
            CurrencyManager
                cmDoc = (CurrencyManager)BindingContext[dgDoc.DataSource];

            if (drObj != null)
            {
                int
                    nPrevPos = cmDoc.Position;

                xNSI.dsM.Tables[NSI.BD_DOUTD].Rows.Remove(drObj);
            }
            xCDoc.xOper = new CurOper(xCDoc.xDocP.DType);
            xCDoc.xOper.SetOperObj(null, xCDoc.xDocP.DType);
            ChangeDetRow(true);
            ShowOperState(xCDoc.xOper);
        }








        // установка/сброс режима упрощенного ввода
        private void SetEasyEdit(AppC.REG_SWITCH rgSW)
        {
            if (((xScrDet.CurReg == 0) && (rgSW == AppC.REG_SWITCH.SW_NEXT)) ||
                (rgSW == AppC.REG_SWITCH.SW_SET))
                {// пока обычный режим, переключение в полноэкранный
                if (bZVKPresent == true)
                {// заявка имеется
                    if (bShowTTN == true)
                        ChgDetTable(null, "");
                    //SetFltVyp(true);
                    SetDetFlt(NSI.BD_DIND, AppC.REG_SWITCH.SW_SET);
                    if (xScrDet.CurReg == 0)
                        xScrDet.NextReg(AppC.REG_SWITCH.SW_SET, tNameSc);
                }
            }
            else
            {// полноэкранный режим, переключение в обычный
                if (bShowTTN == false)
                    ChgDetTable(null, "");
                //SetFltVyp(false);
                SetDetFlt(NSI.BD_DOUTD, AppC.REG_SWITCH.SW_CLEAR);
                xScrDet.NextReg(AppC.REG_SWITCH.SW_CLEAR, tNameSc);
                //ShowRegVvod();
            }
        }

        // допустимость продукции для частичной инвентаризации
        private bool AvailKMC4PartInvent(string sKMC, bool bShowErr)
        {
            bool
                ret = true;
            string
                sRf;
            DataView
                dv;

            if (xCDoc.xDocP.nNumTypD == 41)
            {
                sRf = String.Format("SYSN={0}", xCDoc.nId);
                dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
                if (dv.Count > 0)
                {
                    if (sKMC != (string)dv[0].Row["KMC"])
                    {
                        ret = false;
                        if (bShowErr)
                            Srv.ErrorMsg((string)dv[0].Row["SNM"], "Допустимо только:", true);
                    }
                }
            }

            return(ret);
        }



        #region NOT_USED
        /*
         * 
        // поиск текущего контрола в списке
        private int SearchActControl(out int iP, out int iN, out int iFst, out int iL)
        {
            bool bSearchNext = false;
            int iPrev = -1, iNext = -1, iTmp = -1;
            int iFirst = -1, iLast = -1;
            int ret = -1;
            for (int i = 0; i < aEdVvod.Count; i++)
            {
                if (aEdVvod[i].xControl.Enabled == true)
                {// только для доступных
                    if (iFirst == -1)
                        iFirst = i;
                    if (aEdVvod[i].xControl.Focused == true)  // ищем текущий активный контрол
                    {
                        ret = i;
                        iPrev = iTmp;
                        bSearchNext = true;
                    }
                    else
                    {
                        if (bSearchNext == true)
                        {
                            bSearchNext = false;
                            iNext = i;
                        }
                        iTmp = i;
                    }
                    iLast = i;
                }
            }
            iP = iPrev;
            iN = iNext;
            iFst = iFirst;
            iL = iLast;
            return (ret);
        }


        // перемещение между Controls клавишами управления
        private bool SetNextControl(bool bNext)
        {
            int nPrev, nNext, nFirst, nLast;
            bool ret = true;                                                            // типа куда-то успешно перейдем
            int i = SearchActControl(out nPrev, out nNext, out nFirst, out nLast);      // текущий индекс

            if (bNext == false)
            {// переход на предыдущий
                if (nPrev >= 0)
                    aEdVvod[nPrev].xControl.Focus();
                else
                {
                    if (nLast > i)
                        aEdVvod[nLast].xControl.Focus();
                    else
                        ret = false;        // а вот хрен там - некуда идти
                }
            }
            else
            {// переход на следующий
                if (nNext >= 0)
                    aEdVvod[nNext].xControl.Focus();
                else
                {// стоим на последнем
                    if (nFirst >= 0)
                        aEdVvod[nFirst].xControl.Focus();
                    else
                        ret = false;        // а вот хрен там - некуда идти
                }
            }
            return (ret);
        }

        private void EditEndDet()
        {
            RestShowVvod(false);
        }

         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */
        #endregion



    }
}
