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
using SkladAll;


namespace SkladRM
{
    public sealed partial class NSI : NSIAll
    {

        // инициализация для таблиц SSCC
        public void InitTableSSCC(DataGrid dg)
        {
            dsM.Tables.Add(DT[BD_SSCC].dt);

            dsM.Relations.Add(REL2SSCC,
                DT[BD_DOCOUT].dt.Columns["SYSN"],
                DT[BD_SSCC].dt.Columns["SYSN"]);

            dg.SuspendLayout();
            DT[BD_SSCC].dg = dg;
            CreateTableStylesSSCC(dg);
            dg.DataSource = DT[BD_SSCC].dt;
            ChgGridStyle(BD_SSCC, GDET_SCAN);
            dg.ResumeLayout();
        }

        // стили таблицы SSCC
        private void CreateTableStylesSSCC(DataGrid dg)
        {
            //DataGridTextBoxColumn
            //    sColk;
            ServClass.DGTBoxColorColumn
                sC;
            Color
                colSpec = Color.PaleGoldenrod,
                colGreen = Color.LightGreen;
            double
                nKoef = Screen.PrimaryScreen.Bounds.Width / 240.0;
            int
                nWMAdd = 0;
#if WMOBILE
            nWMAdd = 4;
#else
            nWMAdd = 0;
#endif


            dg.TableStyles.Clear();
            // Для результатов сканирования
            DataGridTableStyle ts = new DataGridTableStyle();
            ts.MappingName = GDET_SCAN.ToString();

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_SSCC);
            sC.MappingName = "NPODDZ";
            sC.HeaderText = "№ ";
            sC.Width = (int)(25 * nKoef + nWMAdd); ;
            sC.NullText = "";
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_SSCC);
            sC.MappingName = "SSCC";
            sC.HeaderText = "Номер SSCC";
            sC.Width = (int)(146 * nKoef + nWMAdd);
            sC.AlternatingBackColor = C_READY_TTN;
            sC.AlternatingBackColorSpec = C_TNSFD_TTN;
            ts.GridColumnStyles.Add(sC);

            sC = new ServClass.DGTBoxColorColumn(dg, NSI.BD_SSCC);
            sC.MappingName = "STATE";
            sC.HeaderText = "Сост-е";
            sC.Width = (int)(58 * nKoef + nWMAdd - 6);
            sC.AlternatingBackColor = C_READY_TTN;
            sC.AlternatingBackColorSpec = C_TNSFD_TTN;
            ts.GridColumnStyles.Add(sC);

            dg.TableStyles.Add(ts);

        }

    }

    public partial class MainF : Form
    {
        // переход на вкладку SSCC
        private void EnterInSSCC()
        {
            string
                sRf;
            DataView
                dv;
            //DataTable
            //  dtD = ((DataTable)this.dgSSCC.DataSource);

            //ShowRegVvod();
            //if (drShownDoc != xCDoc.drCurRow)
            //{// сменился документ
            //    NewDoc(dtD);
            //}

            sRf = String.Format("SYSN={0}", xCDoc.nId);
            dv = new DataView(xNSI.DT[NSI.BD_SSCC].dt, sRf, "", DataViewRowState.CurrentRows);

            lDocInfSSCC.Text = CurDocInf(xCDoc.xDocP);
            lSSCCState.Text = String.Format("Всего SSCC = {0}", dv.Count);

            //if (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.AVAIL)
            //{// инвентаризации - всегда в ТТН
            //    ChgDetTable(null, NSI.BD_DOUTD);
            //}

            //tCurrPoddon.Text = xCDoc.xNPs.Current.ToString();
            dgSSCC.Focus();
        }

        private bool IsDupSSCC(string sSSCC)
        {
            DataView dvT = new DataView(xNSI.DT[NSI.BD_DOUTD].dt,
                String.Format("(SYSN={0}) AND (SSCC='{1}')", xCDoc.drCurRow["SYSN"], sSSCC),
                "KMC,EMK DESC", DataViewRowState.CurrentRows);
            return ((dvT.Count > 0) ? true : false);
        }


        private void TryLoadSSCC(string sSSCC, int nRet)
        {
            int
                nM,
                nRec,
                nNPP;

            DataRow
                dr = null;
            DataTable
                dtD = xNSI.DT[NSI.BD_DIND].dt;
            DataSet
                ds;

            ServerExchange
                xSE = new ServerExchange(this);
            PSC_Types.ScDat
                scD = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));

            //nRet = ConvertSSCC2Lst(xSE, sSSCC, ref scD, true, xNSI.DT[NSI.BD_DIND].dt);
            if ((nRet == AppC.RC_OK) || (nRet == AppC.RC_MANYEAN))
            {
                xCDoc.drCurRow["CHKSSCC"] = 1;

                // для контроля удаляем все предыдущие
                DataRow[] drMDet = xCDoc.drCurRow.GetChildRows(xNSI.dsM.Relations[NSI.REL2TTN]);
                foreach (DataRow drDel in drMDet)
                {
                    xNSI.dsM.Tables[NSI.BD_DOUTD].Rows.Remove(drDel);
                }
                DataRow[] drMDetZ = xCDoc.drCurRow.GetChildRows(xNSI.dsM.Relations[NSI.REL2ZVK]);
                foreach (DataRow drDel in drMDetZ)
                {
                    xNSI.dsM.Tables[NSI.BD_DIND].Rows.Remove(drDel);
                }

                ds = xCLoad.dsZ;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    nRec = xCLoad.dtZ.Rows.Count;
                    nM = 0;
                    nNPP = 1;
                    foreach (DataRow drA in dtL.Rows)
                    {
                        scD = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));
                        nM += SetOneDetZ(ref scD, dtD, ds, drA, xCDoc.drCurRow, ref nNPP);
                        nNPP++;
                    }
                    scCur = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));
                    SetDetFields(true);
                    Srv.ErrorMsg(String.Format("{0} строк загружено", nRec), String.Format("SSCC...{0}", sSSCC.Substring(15, 5)), false);
                }
                catch (Exception exx)
                {
                    Srv.ErrorMsg(exx.Message, "Ошибка загрузки!", true);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }


        public int ConvertSSCC2Lst(ServerExchange xSE, string sSSCC, ref PSC_Types.ScDat scD, bool bInfoOnEmk)
        {
            return (ConvertSSCC2Lst(xSE, sSSCC, ref scD, bInfoOnEmk, xNSI.DT[NSI.BD_DOUTD].dt));
        }

        public int ConvertSSCC2Lst(ServerExchange xSE, string sSSCC, ref PSC_Types.ScDat scD, bool bInfoOnEmk, DataTable dtResult)
        {
            int nRec,
                nRet = AppC.RC_OK;
            string
                sP;

            DataSet
                dsTrans = null;

            // вместе с командой отдаем заголовок документа
            xCUpLoad = new CurUpLoad(xPars);
            xCUpLoad.sCurUplCommand = AppC.COM_ZSC2LST;

            if (xCDoc.drCurRow is DataRow)
                dsTrans = xNSI.MakeWorkDataSet(xNSI.DT[NSI.BD_DOCOUT].dt, dtResult, new DataRow[] { xCDoc.drCurRow }, null, xSm, xCUpLoad);

            MakeTempDOUTD(dtResult);

            LoadFromSrv dgL = new LoadFromSrv(LstFromSSCC);

            xCLoad = new CurLoad();
            xCLoad.sSSCC = sSSCC;
            xCLoad.xLP.lSysN = xCDoc.nId;
            xCLoad.dtZ = dtL;

            sP = String.Format("(SSCC={0},TYPE={1})", sSSCC, "ROW");
            string sL = xSE.ExchgSrv(AppC.COM_ZSC2LST, sP, "", dgL, dsTrans, ref nRet, 20);


            if (dtL.Rows.Count > 0)
            {
                nRet = TestProdBySrv(xSE, nRet);

                if (nRet == AppC.RC_OK)
                {
                    nRec = xCLoad.dtZ.Rows.Count;
                    if (nRec == 1)
                    {// будем изображивать сканирование
                        SetVirtScan(xCLoad.dtZ.Rows[0], ref scD, true, bInfoOnEmk);
                        scD.sSSCC = sSSCC;
                    }
                    else
                    {// добавление группы ???
                        nRet = AppC.RC_MANYEAN;
                    }
                }
            }
            else
            {// просто сохраним запись ??? -  если была сетевая ошибка! при ошибке сервера ничего сохранять не надо!
                if (xSE.ServerRet != AppC.RC_OK)
                    Srv.ErrorMsg(sL);
            }

            return (nRet);
        }


        /// подготовить и отобразить содержимое SSCC
        private void ShowSSCCContent(DataTable dtZ, string sSSCC, ServerExchange xSE, AddrInfo xA, ref Srv.CurrFuncKeyHandler ehKeybHdl)
        {
            const int NP_LEN = 5;
            int
                nTotMest,
                nM;
            string
                sNP,
                sUser = "",
                sFIO = "";
            char
                cExCh = '=';
            DataRow
                xd;
            DateTime
                dVyr;
            List<string>
                lKMC = new List<string>(),
                lCur = new List<string>();
            FRACT
                fTotEd,
                fE;

            nTotMest = 0;
            fTotEd = 0;
            try
            {
                string sA = "";
                try
                {
                    sA = xA.AddrShow;
                }
                catch { }

                try
                {
                    sUser = xSE.AnswerPars["USER"];
                    if ((sUser == AppC.SUSER) || (sUser == AppC.GUEST))
                        sFIO = (sUser == AppC.SUSER) ? "Admin" : "Работник склада";
                    else
                    {
                        NSI.RezSrch zS = xNSI.GetNameSPR(NSI.NS_USER, new object[] { sUser }, "NMP");
                        if (zS.bFind)
                            sFIO = sUser + '-' + zS.sName;
                        else
                            sFIO = sUser;
                    }
                }
                catch
                {
                    sFIO = "";
                }

                xInf = aKMCName(String.Format("{0} ({1}) {2}", sSSCC.Substring(2), sA, sFIO), false);
                xInf.Add(aKMCName("", true, cExCh)[0]);

                if (dtZ.Rows.Count > 0)
                {
                    DataView
                        dv = new DataView(dtZ, "", "KMC", DataViewRowState.CurrentRows);

                    foreach (DataRowView dva in dv)
                    {
                        xd = dva.Row;
                        try
                        {
                            nM = (int)xd["KOLM"];
                        }
                        catch { nM = 0; }
                        try
                        {
                            fE = (FRACT)xd["KOLE"];
                        }
                        catch { fE = 0; }

                        try
                        {
                            dVyr = DateTime.ParseExact((string)xd["DVR"], "yyyyMMdd", null);
                        }
                        catch { dVyr = DateTime.MinValue; }
                        nTotMest += nM;
                        fTotEd += fE;

                        if (!lKMC.Contains((string)xd["KMC"]))
                            lKMC.Add((string)xd["KMC"]);

                        lCur.Add(String.Format("{0,4} {1}", xd["KRKMC"], xd["SNM"]));
                        //lCur.Add(String.Format("{0} {1,6} {2,5:F1} {3,6} {4,7}", dVyr.ToString("dd.MM"), xd["NP"], xd["EMK"], nM, fE));
                        sNP = xd["NP"].ToString();
                        if (sNP.Length > NP_LEN)
                            sNP = sNP.Substring(sNP.Length - NP_LEN, NP_LEN);
                        else
                            sNP = sNP.PadLeft(NP_LEN);
                        lCur.Add(String.Format("{0} {1,5} {2,5:F1} {3,6} {4,7}", dVyr.ToString("dd.MM"), sNP, xd["EMK"], nM, fE));
                        lCur.Add(aKMCName("", true)[0]);
                    }
                    xInf.Add(String.Format("Всего SKU: {0}  Мест:{1}  Ед.:{2}", lKMC.Count, nTotMest, fTotEd));
                    xInf.Add(aKMCName("", true, cExCh)[0]);
                    xInf.Add(" Двыр   №пт  Емк    Мест     Ед.");
                    xInf.Add(aKMCName("", true, cExCh)[0]);
                    xInf.AddRange(lCur);
                }
                else
                {
                    xInf.Add("Нет сведений!");
                    Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                }
                //xHelpS.ShowInfo(xInf, ref ehKeybHdl);
                Srv.HelpShow
                    xSSCCCont = new Srv.HelpShow(this);
                //xSSCCCont.ShowInfo(xInf, ref ehKeybHdl);
                xSSCCCont.ShowInfo(null,
                                (tcMain.SelectedIndex == PG_DOC) ? dgDoc :
                                (tcMain.SelectedIndex == PG_SCAN) ? dgDet : null,
                                xInf, ref ehKeybHdl);

            }
            catch (Exception ex)
            {
                int ggg = 999;
            }


        }

        //private void WhatSSCCContent()
        //{
        //    int
        //        nRet;
        //    string
        //        sSSCC;
        //    ServerExchange
        //        xSE = new ServerExchange(this);

        //    try
        //    {
        //        if (tcMain.SelectedIndex == PG_SCAN)
        //        {
        //            sSSCC = (string)drDet["SSCC"];
        //            try
        //            {
        //                if ((xCLoad.sSSCC == sSSCC) && (xCLoad.sComLoad == AppC.COM_ZSC2LST) && (xCLoad.dtZ.Rows.Count > 0))
        //                {
        //                    ShowSSCCContent(xCLoad.dtZ, sSSCC, xCDoc.xOper.xAdrSrc);
        //                    return;
        //                }
        //            }
        //            catch { }
        //            if (scCur.nKrKMC == AppC.KRKMC_MIX)
        //            {
        //                PSC_Types.ScDat scD = scCur;
        //                //nRet = ConvertSSCC2Lst(xSE, sSSCC, true, false, ref scD);

        //                nRet = ConvertSSCC2Lst(xSE, scCur.sSSCC, ref scD, true, xNSI.DT[NSI.BD_DOUTD].dt);

        //                if ((nRet == AppC.RC_OK) || (nRet == AppC.RC_MANYEAN))
        //                {
        //                    ShowSSCCContent(xCLoad.dtZ, sSSCC, xCDoc.xOper.xAdrSrc);
        //                    return;
        //                }
        //            }
        //        }
        //    }
        //    catch { }

        //    xCDoc.sSSCC = "";
        //    WaitScan4Func(AppC.F_CNTSSCC, "Содержимое SSCC", "Отсканируйте SSCC");
        //}


        private void WhatSSCCContent()
        {
            int
                nRet;
            string
                sSSCC;
            ServerExchange
                xSE = new ServerExchange(this);

            try
            {
                if (tcMain.SelectedIndex == PG_SCAN)
                {
                    sSSCC = (string)drDet["SSCC"];
                    try
                    {
                        if ((xCLoad.sSSCC == sSSCC) && (xCLoad.sComLoad == AppC.COM_ZSC2LST) && (xCLoad.dtZ.Rows.Count > 0))
                        {
                            ShowSSCCContent(xCLoad.dtZ, sSSCC, null, xCDoc.xOper.xAdrSrc, ref ehCurrFunc);
                            return;
                        }
                    }
                    catch { }
                    if (scCur.nKrKMC == AppC.KRKMC_MIX)
                    {
                        PSC_Types.ScDat scD = scCur;
                        nRet = ConvertSSCC2Lst(xSE, sSSCC, ref scD, false);
                        if ((nRet == AppC.RC_OK) || (nRet == AppC.RC_MANYEAN))
                        {
                            ShowSSCCContent(xCLoad.dtZ, sSSCC, xSE, xCDoc.xOper.xAdrSrc, ref ehCurrFunc);
                            return;
                        }
                    }
                }
            }
            catch { }

            xCDoc.sSSCC = "";
            WaitScan4Func(AppC.F_CNTSSCC, "Содержимое SSCC", "Отсканируйте SSCC");
        }

        public int ProceedSSCC(ScanVarRM xSc, ref PSC_Types.ScDat scD)
        {
            ServerExchange
                xSE = null;
            return (ProceedSSCC(xSc, ref scD, xSE));
        }

        ///
        // обработка SSCC при вводе
        public int ProceedSSCC(ScanVarRM xSc, ref PSC_Types.ScDat scD, ServerExchange xSE)
        {
            int
                ret = AppC.RC_OK;
            bool
                bTryServer = false;
            string
                sSSCC = xSc.Dat;

            DataRow
                dr = null;
            AppC.MOVTYPE
                MoveType = xCDoc.xDocP.DType.MoveType;

            if (xSE == null)
            {
                xSE = new ServerExchange(this);
                bTryServer = true;
            }

            if (tcMain.SelectedIndex == PG_SCAN)
            {
                switch (MoveType)
                {
                    case AppC.MOVTYPE.AVAIL:        // инвентаризации
                        bTryServer = true;
                        break;
                    case AppC.MOVTYPE.RASHOD:       // расходные документы
                        bTryServer = true;
                        break;
                    case AppC.MOVTYPE.PRIHOD:       // документы поступления
                        bTryServer = false;
                        break;
                    case AppC.MOVTYPE.MOVEMENT:     // документы перемещения
                        bTryServer = false;
                        break;
                    default:
                        bTryServer = false;
                        break;
                }
            }
            else if (tcMain.SelectedIndex == PG_SSCC)
                bTryServer = false;

            xCDoc.xOper.SSCC = xSc.Dat;

            if (bTryServer)
            {
                    //ret = ConvertSSCC2Lst(xSE, xSc, ref scD, xNSI.DT[NSI.BD_DOUTD].dt, false);
                ret = ConvertSSCC2Lst(xSE, xCDoc.sSSCC, ref scD, true, xNSI.DT[NSI.BD_DOUTD].dt);

                    if (ret == AppC.RC_OK)
                    {
                        if (xCLoad.dtZ.Rows.Count == 1)
                        {// однородный поддон
                            scD.sSSCC = sSSCC;
                            dr = AddVirtProd(ref scD);
                        }
                    }
            }

            //if (dr == null)
            //    dr = AddDetSSCC(xSc);


            switch (MoveType)
            {
                case AppC.MOVTYPE.AVAIL:        // инвентаризации
                    ret = AddGroupDet(ret, (int)NSI.SRCDET.SSCCT, xCDoc.sSSCC);
                    break;
                case AppC.MOVTYPE.RASHOD:       // расходные документы
                    dr = AddSSCC2ProdList(xSc);
                    break;
                case AppC.MOVTYPE.PRIHOD:       // документы поступления
                    dr = AddSSCC2ProdList(xSc);
                    break;
                case AppC.MOVTYPE.MOVEMENT:     // документы перемещения
                    dr = AddSSCC2ProdList(xSc);
                    break;
                default:
                    dr = AddSSCC2ProdList(xSc);
                    break;
            }
            AddSSCC2SSCCTable(xSc.Dat, -1, xCDoc, true);

            if (dr != null)
                drDet = dr;
            IsOperReady();

            return (ret);
        }

        // добавление в список ТТН отмаркированного поддона
        private DataRow AddSSCC2ProdList(ScanVarRM xSc)
        {
            DataRow
                ret = null;
            try
            {

                PSC_Types.ScDat sc = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));
                sc.sSSCC = xSc.Dat;
                sc.nNomPodd = int.Parse(xSc.Dat.Substring(13, 7));
                //sc.sN = String.Format("Поддон №{0}", sc.nNomPodd);
                sc.sN = String.Format("SSCC № {0}...{1}", sc.sSSCC.Substring(2, 4), sc.sSSCC.Substring(15, 5));
                sc.nKrKMC = AppC.KRKMC_MIX;
                ret = AddVirtProd(ref sc);

            }
            catch //(Exception e)
            {
                ret = null;
            }
            return (ret);
        }



        // добавление в список SSCC отмаркированного поддона
        public DataRow AddSSCC2SSCCTable(string sSSCC, int nPP, CurDoc xCDoc, bool bAddNew)
        {
            DataRow
                ret = null;
            int
                nKey = (int)xCDoc.drCurRow["SYSN"];

            try
            {
                bAddNew = false;
                ret = FindSCCTInSSCCList(sSSCC, nKey, ref nPP);
                if (ret == null)
                {
                    bAddNew = true;
                    ret = xNSI.DT[NSI.BD_SSCC].dt.NewRow();
                    ret["SYSN"] = nKey;
                    ret["NPODDZ"] = nPP;
                    ret["SSCC"] = sSSCC;
                    ret["STATE"] = 1;
                }
                ret["IN_TTN"] = 1;
                if (bAddNew)
                {
                    xNSI.DT[NSI.BD_SSCC].dt.Rows.Add(ret);
                }
            }
            catch
            {
                Srv.ErrorMsg("Ошибка добавления SSCC!");
            }
            return (ret);
        }


        // поиск в списке SSCC отмаркированного поддона
        public DataRow FindSCCTInSSCCList(string sSSCC, int nID, ref int nPP)
        {
            DataRow
                ret = null;
            string
                sRf = ((DataTable)dgSSCC.DataSource).DefaultView.RowFilter;
            DataView
                dv;

            sRf = String.Format("SYSN={0} AND SSCC='{1}'", nID, sSSCC);
            dv = new DataView(xNSI.DT[NSI.BD_SSCC].dt, sRf, "", DataViewRowState.CurrentRows);
            if (dv.Count >= 1)
            {
                ret = dv[0].Row;
            }
            else
            {
                if (nPP < 0)
                {
                    sRf = String.Format("SYSN={0}", nID);
                    dv = new DataView(xNSI.DT[NSI.BD_SSCC].dt, sRf, "", DataViewRowState.CurrentRows);
                    if (nPP <= 0)
                    {
                        if (dv.Count >= 1)
                            nPP = (int)(dv[dv.Count - 1].Row["NPODDZ"]) + 1;
                        else
                            nPP = 1;
                    }
                }
            }
            return (ret);
        }

        // поиск в списке SSCC отмаркированного поддона
        public DataRow FindSCCTInProdList(string sSSCC, CurDoc xCDoc)
        {
            DataRow
                ret = null;
            string
                sRf = ((DataTable)dgDet.DataSource).DefaultView.RowFilter;

            sRf = String.Format("SYSN={0} AND SSCC='{1}' AND KRKMC={2}", xCDoc.nId, sSSCC, AppC.KRKMC_MIX);
            DataView dv = new DataView(xNSI.DT[NSI.BD_SSCC].dt, sRf, "", DataViewRowState.CurrentRows);
            if (dv.Count == 1)
                ret = dv[0].Row;

            return (ret);
        }


        public void SetSSCCForPoddon(string sSSCC, DataView dv, int nP)
        {
            string
                sF;
            sF = (sSSCC.Substring(2, 1) == "1") ? "SSCC" : "SSCCINT";
            foreach (DataRowView drv in dv)
            {
                (drv.Row[sF]) = sSSCC;
                (drv.Row["SSCC"]) = sSSCC;
            }
            //MessageBox.Show(String.Format("Поддон {0} подготовлен ({1}) позиций", nP, dv.Count));

            //xCDoc.xNPs.TryNextPoddon(true);
            tCurrPoddon.Text = xCDoc.xNPs.Current.ToString();
        }

        public bool StoreSSCC(ScanVarRM xSc, int nPoddonN, bool bNeedWrite, out DataView dv)
        {
            int n = 0;
            string
                s, sRf,
                sF,
                sD = xSc.Dat;
            bool
                //bIsExt,
                bRet = AppC.RC_CANCELB;
            DataView dvZ;
            DialogResult dRez;

            if (sD.Substring(2, 1) == "1")
            {
                //bIsExt = true;
                sF = "SSCC";
            }
            else
            {
                //bIsExt = false;
                sF = "SSCCINT";
            }

            /// 14.02.18
            sF = "SSCC";

            dv = null;
            if (xCDoc.drCurRow == null)
            {
                return (bRet);
            }
            try
            {
                if ((int)xCDoc.drCurRow["TYPOP"] == AppC.TYPOP_KMPL)
                {
                    if (nPoddonN > 0)
                    {
                        //string sRf = xCDoc.DefDetFilter() + String.Format(" AND (SSCC='{0}')", sSSCC);
                        //dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
                        //n = dv.Count;
                        if (IsUsedSSCC(sD))
                        {
                            dRez = MessageBox.Show(
                                String.Format("SSCC={0}\nОтменить (Enter)?\n(ESC)-проставить SSCC", sD),
                                "Уже использовался!", MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                            n = (dRez == DialogResult.OK) ? 1 : 0;
                        }
                        if (n == 0)
                        {// такой SSCC еще не использовался
                            sRf = xCDoc.DefDetFilter() + String.Format(" AND (NPODDZ={0})", nPoddonN);
                            dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
                            if (dv.Count > 0)
                            {

                                foreach (DataRowView drv in dv)
                                {
                                    if ((drv.Row[sF]) != System.DBNull.Value)
                                    {
                                        s = (drv.Row[sF]).ToString();
                                        if ((s.Length > 0) && (s != sD))
                                        {
                                            dRez = MessageBox.Show(
                                            String.Format("SSCC={0} уже установлен\nОтменить (Enter)?\n(ESC)-проставить SSCC", drv.Row["SSCC"]),
                                            String.Format("Поддон {0}", nPoddonN),
                                            MessageBoxButtons.OKCancel,
                                            MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                                            n = (dRez == DialogResult.OK) ? 1 : 0;
                                            break;
                                        }
                                    }
                                }
                                if (n == 0)
                                {// поддон еще не отмечался
                                    // добавим фильтр на выполненные
                                    sRf += String.Format("AND(READYZ<>{0})", (int)NSI.READINESS.FULL_READY);
                                    dvZ = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "", DataViewRowState.CurrentRows);
                                    n = dvZ.Count;
                                    if (n > 0)
                                    {// не вся заявка закрыта
                                        if (!xCDoc.bFreeKMPL)
                                        {
                                            dRez = MessageBox.Show(
                                                "Заявка не выполнена!\nОтменить (Enter)?\r\n(ESC)-проставить SSCC",
                                                String.Format("Поддон {0}", nPoddonN),
                                            MessageBoxButtons.OKCancel,
                                            MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                                            n = (dRez == DialogResult.OK) ? 1 : 0;
                                        }
                                        else
                                            n = 0;
                                    }
                                    if (n == 0)
                                    {
                                        bRet = AppC.RC_OKB;
                                        if (bNeedWrite)
                                            SetSSCCForPoddon(xSc.Dat, dv, nPoddonN);

                                        //foreach (DataRowView drv in dv)
                                        //{
                                        //    (drv.Row[sF]) = sSSCC;
                                        //}
                                        //MessageBox.Show(String.Format("Поддон {0} подготовлен ({1}) позиций",
                                        //    xCDoc.xNPs.Current, dv.Count));

                                        //xCDoc.xNPs.TryNextPoddon(true);
                                        //tCurrPoddon.Text = xCDoc.xNPs.Current.ToString();
                                    }
                                }
                            }
                            else
                                Srv.ErrorMsg("Нет отсканированных!");
                        }
                        else
                            Srv.ErrorMsg("SSCC поддона уже использовался!");
                    }
                    else
                        Srv.ErrorMsg("№ поддона не установлен!");
                }
                else
                    Srv.ErrorMsg("Только для комплектации!");





            }
            catch (Exception e)
            {
                Srv.ErrorMsg("Только для комплектации!");
            }




            return (bRet);
        }


    }
}
