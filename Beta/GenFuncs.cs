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
using PDA.BarCode;
using SavuSocket;

using FRACT = System.Decimal;


namespace SkladRM
{
    public partial class MainF : Form
    {
        // текущий объект загрузки документов
        public CurLoad xCLoad = null;

        // объект текущей выгрузки
        public CurUpLoad xCUpLoad = null;

        private bool bInUpload = false;

        // текущий режим (все, по фильтру,...)
        private IntRegsAvail irFunc = null;



        //#region OldHelpCode
        //// текущий индекс вывода
        //private int nHelpInd = 0;
        //// текущая инфо
        //private object xInf;

        //// вывод панели c окном помощи
        //public void ShowInf(object xInfP)
        //{
        //    ShowInf(xInfP, pnHelp);
        //}

        //public void ShowInf(object xInfP, Control pnH)
        //{
        //    if (xInfP != null)
        //        xInf = xInfP;

        //    if (xInf == null)
        //        return;

        //    // центровка панели
        //    Rectangle screen = Screen.PrimaryScreen.Bounds;
        //    if (screen.Width > 240)
        //    {
        //        pnH.Width = screen.Width;
        //        tMainHelp.Width = screen.Width - 4;
        //        lMainHelp.Left = (screen.Width - lMainHelp.Width) / 2;
        //    }
        //    pnH.Location = new Point((screen.Width - pnH.Width) / 2,
        //        (screen.Height - pnH.Height) / 2);

        //    nHelpInd = 0;

        //    if (xInf.GetType().IsArray == true)
        //        tMainHelp.Text = ((string[])xInf)[0];
        //    else
        //        tMainHelp.Text = NextInfPart();

        //    pnH.Visible = true;
        //    //pnH.Invalidate();
        //    pnH.BringToFront();
        //    ehCurrFunc += new CurrFuncKeyHandler(HelpKeyDown);
        //    tFiction.Focus();
        //}

        //private string NextInfPart()
        //{
        //    string sRet = "";
        //    int nStart = (nHelpInd * AppC.HELPLINES),
        //        nEnd;

        //    if (nStart >= ((List<string>)xInf).Count)
        //    {
        //        nHelpInd = 0;
        //        nStart = 0;
        //    }
        //    nEnd = Math.Min(((List<string>)xInf).Count, nStart + AppC.HELPLINES);

        //    for (int i = nStart; i < nEnd; i++)
        //        sRet += ((List<string>)xInf)[i] + "\r\n";
        //    if (sRet.Length > 0)
        //        sRet = sRet.Remove(sRet.Length - 2, 2);
        //    return (sRet);
        //}


        //private bool HelpKeyDown(int nFunc, KeyEventArgs e)
        //{
        //    bool bKeyHandled = true,
        //        bCloseHelp = false;
        //    string sH = "";

        //    if (nFunc > 0)
        //    {
        //        bCloseHelp = true;
        //        if (nFunc != AppC.F_HELP)
        //            bKeyHandled = false;
        //    }
        //    else
        //    {
        //        switch (e.KeyValue)
        //        {
        //            case W32.VK_ESC:
        //                bCloseHelp = true;
        //                break;
        //            case W32.VK_ENTER:
        //                nHelpInd++;
        //                if (xInf.GetType().IsArray == true)
        //                {
        //                    if (nHelpInd == ((string[])xInf).Length)
        //                        nHelpInd = 0;
        //                    sH = ((string[])xInf)[nHelpInd];
        //                }
        //                else
        //                    sH = NextInfPart();

        //                tMainHelp.Text = sH;
        //                break;
        //        }
        //    }
        //    if (bCloseHelp == true)
        //    {
        //        Control cOldCtrl = null;
        //        ehCurrFunc -= HelpKeyDown;
        //        pnHelp.Visible = false;
        //        pnHelp.Location = new Point(400, 50);
        //        switch (tcMain.SelectedIndex)
        //        {
        //            case PG_SCAN:
        //                cOldCtrl = dgDet;
        //                break;
        //            default:
        //                cOldCtrl = dgDoc;
        //                break;
        //        }
        //        cOldCtrl.Focus();
        //    }
        //    //else
        //    //    tFiction.Focus();
        //    return (bKeyHandled);
        //}

        //#endregion


        // контроль документов
        private bool ControlDocs(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        {
            int nFunc = (int)nF;
            bool bKeyHandled = true;

            if (nFunc == AppC.F_INITREG)
            {
                // дальше клавиши обработаю сам
                ehCurrFunc += new Srv.CurrFuncKeyHandler(ControlDocs);
                if (irFunc == null)
                {
                    irFunc = new IntRegsAvail();
                    irFunc.SetAllAvail(false);
                    irFunc.SetAvail(AppC.UPL_CUR, true);
                    irFunc.SetAvail(AppC.UPL_ALL, true);
                }

                //pnLoadDoc.Left = dgDoc.Left + 5;
                //pnLoadDoc.Top = dgDoc.Top + 25;

                //tbPanP1.Text = irFunc.CurRegName;

                // заполнение полей для загрузки
                //lFuncNamePan.Text = "Контроль документов";
                //lpnLoadInf.Text = "<Enter>-выполнить контроль";

                //pnLoadDoc.Visible = true;

                xFPan.ShowP("Контроль документов", irFunc.CurRegName);
                //tFiction.Focus();

            }
            else
            {
                switch (e.KeyValue)
                {
                    case W32.VK_LEFT:
                        //tbPanP1.Text = irFunc.NextReg(false);
                        xFPan.UpdateReg(irFunc.NextReg(false));
                        break;
                    case W32.VK_RIGHT:
                        //tbPanP1.Text = irFunc.NextReg(false);
                        xFPan.UpdateReg(irFunc.NextReg(false));
                        break;
                    case W32.VK_ESC:
                    case W32.VK_ENTER:
                        if (e.KeyValue == W32.VK_ENTER)
                        {
                            if (xCDoc.drCurRow != null)
                            {
                                Cursor crsOld = Cursor.Current;
                                Cursor.Current = Cursors.WaitCursor;

                                xInf = new List<string>();
                                if (irFunc.CurReg == AppC.UPL_ALL)
                                    ControlAllDoc(xInf);
                                else
                                    ControlDocZVK(xCDoc.drCurRow, xInf);
                                Cursor.Current = crsOld;

                                xHelpS.ShowInfo(xInf, ref kh);
                            }
                        }
                        //pnLoadDoc.Visible = false;
                        //pnLoadDoc.Left = 350;
                        xFPan.HideP();
                        bInUpload = false;
                        // дальше клавиши не обрабатываю
                        ehCurrFunc -= ControlDocs;
                        dgDoc.Focus();
                        break;
                }
            }
            return (bKeyHandled);
        }




        // выгрузка документов
        // nRegUpl - что выгружать (текущий, все, фильтр)
        private bool UploadDocs2Server(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        {
            int nFunc = (int)nF;
            int nY = 28;                // панель для PG_DOC
            bool bKeyHandled = false;

            if (nFunc > 0)
            {
                switch (nFunc)
                {
                    case AppC.F_INITREG:
                        if (bInUpload == false)
                        {
                            if ((tcMain.SelectedIndex == PG_DOC) || (tcMain.SelectedIndex == PG_SCAN))
                            {
                                bInUpload = true;

                                // дальше клавиши обработаю сам
                                ehCurrFunc += new Srv.CurrFuncKeyHandler(UploadDocs2Server);

                                //if (xCUpLoad == null)
                                //{
                                //    xCUpLoad = new CurUpLoad(xPars);
                                //    if (xCLoad != null)
                                //    {// возьмем фильтр оттуда
                                //        xCUpLoad.xLP = xCLoad.xLP;
                                //    }
                                //    xDP = xCUpLoad.xLP;
                                //}

                                xCUpLoad = new CurUpLoad(xPars);
                                if (xCLoad != null)
                                {// возьмем фильтр оттуда
                                    xCUpLoad.xLP = xCLoad.xLP;
                                }
                                xDP = xCUpLoad.xLP;


                                xFPan = new FuncPanel(this, this.pnLoadDocG);

                                if (tcMain.SelectedIndex == PG_SCAN)
                                {// только текущий
                                    nY = 38;
                                    xCUpLoad.ilUpLoad.CurReg = AppC.UPL_CUR;
                                    xCUpLoad.ilUpLoad.SetAllAvail(false);
                                }
                                else
                                {
                                    xCUpLoad.ilUpLoad.SetAllAvail(true);
                                    //xCUpLoad.ilUpLoad.SetAvail(AppC.UPL_ALL, false);
                                }


                                //xBCScanner.WiFi.IsEnabled = true;
                                xBCScanner.WiFi.ShowWiFi(pnLoadDocG, true);
                                xFPan.UpdateSrv(xPars.sHostSrv);
                                xFPan.ShowP(6, nY, "Выгрузка документов", xCUpLoad.ilUpLoad.CurRegName);

                                //tFiction.Focus();
                            }
                        }
                        break;
                }
            }
            else
            {
                switch (e.KeyValue)
                {
                    case W32.VK_LEFT:
                        xFPan.UpdateReg(xCUpLoad.ilUpLoad.NextReg(false));
                        bKeyHandled = true;
                        break;
                    case W32.VK_RIGHT:
                        xFPan.UpdateReg(xCUpLoad.ilUpLoad.NextReg(true));
                        bKeyHandled = true;
                        break;
                    case W32.VK_ESC:
                        EndOfUpLoad(AppC.RC_CANCEL);
                        bKeyHandled = true;
                        break;
                    case W32.VK_DOWN:
                    case W32.VK_UP:
                        xCUpLoad.NextSrv();
                        xFPan.UpdateSrv(xCUpLoad.CurSrv);
                        bKeyHandled = true;
                        break;
                    case W32.VK_ENTER:
                        if (xCUpLoad.ilUpLoad.CurReg == AppC.UPL_FLT)
                            EditPars(AppC.F_UPLD_DOC, xCUpLoad.xLP, CTRL1ST.START_EMPTY, VerifyBeforeUpLoad, EditOverBeforeUpLoad);
                        else
                        {
                            if (xCUpLoad.ilUpLoad.CurReg == AppC.UPL_CUR)
                                xCUpLoad.xLP = xCDoc.xDocP;
                            EditOverBeforeUpLoad(AppC.RC_OK, AppC.F_UPLD_DOC);
                        }
                        bKeyHandled = true;
                        break;
                }
            }

            return (bKeyHandled);
        }

        // обработка окончания ввода параметров для выгрузки
        private AppC.VerRet VerifyBeforeUpLoad()
        {
            AppC.VerRet v;
            v.nRet = AppC.RC_OK;
            object xErr = null;
            bool bRet = VerifyPars(xCUpLoad.xLP, AppC.F_UPLD_DOC, ref xErr);
            if (bRet != true)
                v.nRet = AppC.RC_CANCEL;
            //else
            //    bQuitEdPars = true;
            v.cWhereFocus = (Control)xErr;
            return (v);
        }

        // автосохранение перед обменом данными
        private void AutoSaveDat()
        {
            if (xPars.bAutoSave == true)
            {
                xFPan.UpdateReg("Автосохранение...");

                Cursor.Current = Cursors.WaitCursor;
                // сохранение рабочих данных (если есть)
                    xSm.SaveCS(xPars.sDataPath, xNSI.DT[NSI.BD_DOCOUT].dt.Rows.Count);
                    xNSI.DSSave(xPars.sDataPath);
                Cursor.Current = Cursors.Default;
            }
        }

        // обработка окончания ввода параметров для выгрузки
        private void EditOverBeforeUpLoad(int nRetEdit, int nUICall)
        {
            int nRet = AppC.RC_OK;
            ServerExchange xSE = new ServerExchange(this);

            if (nRetEdit == AppC.RC_OK)
            {// закончили по Enter, начало выгрузки
                if (ControlBeforeUpload() == AppC.RC_OK)
                {

                    AutoSaveDat();
                    xFPan.UpdateHelp("Идет выгрузка данных...");

                    string sL = UpLoadDoc(xSE, ref nRet);

                    if ((xSE.ServerRet != AppC.EMPTY_INT) && (xSE.ServerRet != AppC.RC_OK))
                    {// операция выгрузки не прошла на сервере (содержательная ошибка)

                        Srv.ErrorMsg(sL, true);
                        //if (nUICall == 0)
                        //{
                        //    xCDoc.xOper.xAdrDst = null;
                        //}
                    }

                    if ((nUICall != 0) || (nRet != AppC.RC_OK))
                    {
                        Srv.ErrorMsg(sL, String.Format("Код завершения-{0}", nRet), false);
                        EndOfUpLoad(AppC.RC_OK);
                    }
                    CheckNSIState(false);
                }
                else
                {
                    W32.SimulKey(W32.VK_ESC, W32.VK_ESC);

                }
            }
        }


        // завершение выгрузки
        private void EndOfUpLoad(int nRet)
        {
            xFPan.HideP();
            bInUpload = false;
            // дальше клавиши не обрабатываю
            ehCurrFunc -= UploadDocs2Server;

            //if (nRet == AppC.RC_OK)
            //{// что-то выгрузилось
            //    StatAllDoc();
            //}
            xFPan = new FuncPanel(this, this.pnLoadDocG);
            //xCDoc.xOper = new CurOper();
            Back2Main();
        }




        private bool bInLoad = false;
        // загрузка документов
        private bool LoadDocFromServer(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        {
            int nFunc = (int)nF;
            int nY = 28;                // панель для PG_DOC
            bool bKeyHandled = false;

            if (nFunc > 0)
            {
                switch (nFunc)
                {
                    case AppC.F_INITRUN:
                    case AppC.F_INITREG:
                        if (bInLoad == false)
                        {
                            if ((tcMain.SelectedIndex == PG_DOC) || (tcMain.SelectedIndex == PG_SCAN))
                            {
                                bInLoad = true;

                                // дальше клавиши обработаю сам
                                ehCurrFunc += new Srv.CurrFuncKeyHandler(LoadDocFromServer);

                                if (xCLoad == null)
                                {
                                    xCLoad = new CurLoad(AppC.UPL_FLT);
                                    xDP = xCLoad.xLP;
                                }

                                if (tcMain.SelectedIndex == PG_SCAN)
                                {// на вкладке Ввод
                                    //pnLoadDoc.Left = dgDet.Left + 5;
                                    //pnLoadDoc.Top = dgDet.Top + 26;
                                    nY = 38;
                                    xCLoad.ilLoad.CurReg = AppC.UPL_CUR;
                                    xCLoad.ilLoad.SetAllAvail(false);
                                    //tbPanP1.Enabled = false;
                                }
                                else
                                {// на вкладке Документы
                                    //pnLoadDoc.Left = dgDoc.Left + 5;
                                    //pnLoadDoc.Top = dgDoc.Top + 25;
                                    if (xCDoc.drCurRow == null)
                                    {// документов еще нет
                                        xCLoad.ilLoad.CurReg = AppC.UPL_FLT;
                                        xCLoad.ilLoad.SetAllAvail(false);
                                        xCLoad.ilLoad.SetAvail(AppC.UPL_FLT, true);
                                    }
                                    else
                                    {
                                        xCLoad.ilLoad.SetAllAvail(true);
                                        xCLoad.ilLoad.SetAvail(AppC.UPL_ALL, false);
                                        //tbPanP1.Enabled = true;
                                    }

                                }
                                    //tbPanP1.Text = xCLoad.ilLoad.CurRegName;

                                    // заполнение полей для загрузки
                                    //lFuncNamePan.Text = "Загрузка документов";
                                    //lpnLoadInf.Text = "<Enter>-начать загрузку";

                                //xBCScanner.WiFi.IsEnabled = true;
                                xBCScanner.WiFi.ShowWiFi(pnLoadDocG, true);
                                xFPan.UpdateSrv(xPars.sHostSrv);
                                xFPan.ShowP(6, nY, "Загрузка документов", xCLoad.ilLoad.CurRegName);
                                if (nFunc != AppC.F_INITRUN)
                                    // для ручного ввода участок обнуляется
                                    xCLoad.xLP.nUch = AppC.EMPTY_INT;
                                //tFiction.Focus();
                                    //pnLoadDoc.Visible = true;

                                    if (nFunc == AppC.F_INITREG)
                                    {
                                        if (xCDoc.drCurRow == null)
                                        {// документов еще нет
                                            EditPars(AppC.F_LOAD_DOC, xCLoad.xLP, CTRL1ST.START_EMPTY, VerifyBeforeLoad, EditOverBeforeLoad);
                                        }

                                    }
                                    else
                                    {
                                        if (xCLoad.ilLoad.CurReg == AppC.UPL_FLT)
                                            EditPars(AppC.F_LOAD_DOC, xCLoad.xLP, CTRL1ST.START_LAST, VerifyBeforeLoad, EditOverBeforeLoad);
                                        else
                                            EditOverBeforeLoad(AppC.RC_OK, AppC.F_LOAD_DOC);
                                    }
                                }
                        }
                        break;
                    case AppC.F_OVERREG:
                        break;
                }
            }
            else
            {
                switch (e.KeyValue)
                {
                    case W32.VK_LEFT:
                        //tbPanP1.Text = xCLoad.ilLoad.NextReg(false);
                        xFPan.UpdateReg(xCLoad.ilLoad.NextReg(false));
                        bKeyHandled = true;
                        break;
                    case W32.VK_RIGHT:
                        //tbPanP1.Text = xCLoad.ilLoad.NextReg(true);
                        xFPan.UpdateReg(xCLoad.ilLoad.NextReg(true));
                        bKeyHandled = true;
                        break;
                    case W32.VK_ESC:
                        EndOfLoad(AppC.RC_CANCEL);
                        bKeyHandled = true;
                        break;
                    case W32.VK_ENTER:
                        if (xCLoad.ilLoad.CurReg == AppC.UPL_FLT)
                            EditPars(AppC.F_LOAD_DOC, xCLoad.xLP, CTRL1ST.START_EMPTY, VerifyBeforeLoad, EditOverBeforeLoad);
                        else
                            EditOverBeforeLoad(AppC.RC_OK, AppC.F_LOAD_DOC);
                        bKeyHandled = true;
                        break;
                }
            }

            return (bKeyHandled);
        }

        // позиционирование на указанную строку
        private bool SetCurRow(DataGrid dg, string sF, int nSys)
        {
            bool bRet = AppC.RC_CANCELB;
            CurrencyManager cmDoc = (CurrencyManager)BindingContext[dg.DataSource];
            for (int i = cmDoc.Count - 1; i >= 0; i--)
            {
                if ((int)(((DataRowView)cmDoc.List[i]).Row[sF]) == nSys)
                {
                    cmDoc.Position = i;
                    bRet = AppC.RC_OKB;
                    break;
                }
            }
            return (bRet);
        }

        // завершение загрузки
        private void PosOnLoaded(int nRet)
        {
            if (nRet == AppC.RC_OK)
            {// есть что показывать после загрузки
                if ((tcMain.SelectedIndex != PG_DOC) && (tcMain.SelectedIndex != PG_SCAN))
                    tcMain.SelectedIndex = PG_DOC;
                if ((xCLoad.dr1st != null) && (xCLoad.dr1st.Table == xNSI.DT[NSI.BD_DOCOUT].dt))
                {
                    SetCurRow(dgDoc, "SYSN", (int)xCLoad.dr1st["SYSN"]);
                }
                RestShowDoc(false);
                //StatAllDoc();
            }
            CheckNSIState(false);
        }


        // завершение загрузки
        private void EndOfLoad(int nRet)
        {
            //pnLoadDoc.Visible = false;
            //pnLoadDoc.Left = 350;
            xFPan.HideP();
            bInLoad = false;

            // дальше клавиши не обрабатываю
            ehCurrFunc -= LoadDocFromServer;

            //if (nRet == AppC.RC_OK)
            //{// есть что показывать после загрузки
            //    if ((tcMain.SelectedIndex != PG_DOC)&&(tcMain.SelectedIndex != PG_SCAN))
            //        tcMain.SelectedIndex = PG_DOC;
            //    if (xCLoad.dr1st != null)
            //    {
            //        CurrencyManager cmDoc = (CurrencyManager)BindingContext[dgDoc.DataSource];
            //        for (int i = 0; i < cmDoc.Count; i++)
            //        {
            //            if ( (int)(((DataRowView)cmDoc.List[i]).Row["SYSN"]) == (int)xCLoad.dr1st["SYSN"] )
            //            {
            //                cmDoc.Position = i;
            //                break;
            //            }
            //        }
            //    }
            //    RestShowDoc(false);
            //    StatAllDoc();
            //}
            //CheckNSIState();
            PosOnLoaded(nRet);
            Back2Main();
        }

        //private void CheckNSIState()
        //{
        //    //if (xPars.dtLoadNS.Date < DateTime.Now.Date)
        //    //{
        //    //    LoadNsiMenu(true);
        //    //}

        //    //if (xSm.dtLoadNS.Date < DateTime.Now.Date)

        //    try
        //    {
        //        DataRow[] xMax = xNSI.DT[NSI.BD_TINF].dt.Select("LASTLOAD=MIN(LASTLOAD)");
        //        if (xMax.Length > 0)
        //        {
        //            if (((DateTime)xMax[0]["LASTLOAD"]).Date < DateTime.Now.Date)
        //                LoadNsiMenu(true, new string[] { });
        //        }
        //        else
        //            LoadNsiMenu(false, new string[] { });
        //    }
        //    catch { }



        //    if (tcMain.SelectedIndex == PG_SCAN)
        //        dgDet.Focus();
        //    else if (tcMain.SelectedIndex == PG_DOC)
        //    {
        //        dgDoc.Focus();
        //    }

        //}

        // bUnCond - безусловная загрузка справочников
        //public void CheckNSIState(bool bUnCond)
        //{
        //    CheckNSIState(bUnCond, true);
        //}

        /// bUnCond - безусловная загрузка справочников
        public void CheckNSIState(bool bUnCond)
        {
            bool
                bNeedLoad;
            DataRow
                drTI;
            List<string>
                lTNames = new List<string>();
            try
            {
                //DataView
                //    // реально загружаемые таблицы
                //    dv = new DataView(xNSI.DT[NSI.BD_TINF].dt, "(ISNULL(FLAG_LOAD,'NEEDLOAD')<>'NOTLOAD')AND(LEN(MD5)>0)", "LASTLOAD", DataViewRowState.CurrentRows);

                //if (dv.Count > 0)
                //{// таблица существует
                //    foreach (DataRowView rv in dv)
                //    {
                //        if (xNSI.DT.ContainsKey((string)rv.Row["DT_NAME"]) &&
                //            (((DateTime)rv["LASTLOAD"]).Date < DateTime.Now.Date || bUnCond))
                //        {
                //            lTNames.Add((string)rv.Row["DT_NAME"]);
                //        }
                //    }
                //    if (lTNames.Count > 0)
                //        LoadNsiMenu(!bUnCond, lTNames.ToArray(), bWiFi);
                //}
                //else
                //{// начальное создание таблицы
                //    LoadNsiMenu(false, new string[] { }, bWiFi);
                //}
                if (bUnCond)
                    xNSI.DT[NSI.BD_TINF].dt.Rows.Clear();

                foreach (KeyValuePair<string, NSI.TableDef> td in xNSI.DT)
                {
                    bNeedLoad = false;
                    if (((td.Value.nType & NSI.TBLTYPE.NSI) == NSI.TBLTYPE.NSI) &&
                        ((td.Value.nType & NSI.TBLTYPE.LOAD) == NSI.TBLTYPE.LOAD))   // НСИ загружаемое
                    {
                        bNeedLoad = true;
                        try
                        {
                            if ((drTI = xNSI.DT[NSI.BD_TINF].dt.Rows.Find(td.Key)) is DataRow)
                            {
                                if ((string)drTI["FLAG_LOAD"] == NSI.NSI_NOT_LOAD)
                                    bNeedLoad = false;
                                else
                                {
                                    if ((((DateTime)drTI["LASTLOAD"]).Date >= DateTime.Now.Date && !bUnCond))
                                        bNeedLoad = false;
                                }
                            }
                        }
                        catch { }
                    }
                    if (bNeedLoad)
                        lTNames.Add(td.Key);
                }
                if (lTNames.Count > 0)
                    LoadNsiMenu(!bUnCond, lTNames.ToArray());
            }
            catch { }

            if (tcMain.SelectedIndex == PG_SCAN)
                dgDet.Focus();
            else if (tcMain.SelectedIndex == PG_DOC)
                dgDoc.Focus();
        }

        // обработка окончания ввода параметров
        private AppC.VerRet VerifyBeforeLoad()
        {
            AppC.VerRet v;
            v.nRet = AppC.RC_OK;
            object xErr = null;
            bool bRet = VerifyPars(xCLoad.xLP, AppC.F_LOAD_DOC, ref xErr);
            if (bRet != true)
                v.nRet = AppC.RC_CANCEL;
            //else
            //    bQuitEdPars = true;
            v.cWhereFocus = (Control)xErr;
            return (v);
        }

        // обработка окончания ввода параметров
        private void EditOverBeforeLoad(int nRetEdit, int nF)
        {
            int 
                nRet = AppC.RC_OK;
            ServerExchange 
                xSE = new ServerExchange(this);

            if (nRetEdit == AppC.RC_OK)
            {// закончили по Enter, начало загрузки
                AutoSaveDat();

                if (xSm.RegApp == AppC.REG_DOC)
                {

                    LoadFromSrv dgL = new LoadFromSrv(DocFromSrv);
                    xCLoad.nCommand = AppC.F_LOAD_DOC;
                    xCLoad.sComLoad = AppC.COM_ZDOC;

                    string sL = xSE.ExchgSrv(AppC.COM_ZDOC, "", "", dgL, null, ref nRet);


                    MessageBox.Show("Загрузка окончена - " + sL, "Код - " + nRet.ToString());
                }
                else
                {
                    if (xCDoc.xDocP.TypOper == AppC.TYPOP_KMPL)
                    {
                        if (xCLoad.ilLoad.CurReg == AppC.UPL_CUR)
                        {// повторная загрузка комплектации
                            xCLoad.sSSCC = "";
                            xCLoad.drPars4Load  = xNSI.DT[NSI.BD_KMPL].dt.NewRow();
                            xCLoad.drPars4Load ["TD"] = xCDoc.xDocP.nNumTypD;
                            xCLoad.drPars4Load ["KRKPP"] = xCDoc.xDocP.nPol;
                            xCLoad.drPars4Load ["KSMEN"] = xCDoc.xDocP.sSmena;
                            xCLoad.drPars4Load ["DT"] = xCDoc.xDocP.dDatDoc.ToString("yyyyMMdd");
                            xCLoad.drPars4Load ["KSK"] = xCDoc.xDocP.nSklad;
                            xCLoad.drPars4Load ["NUCH"] = xCDoc.sLstUchNoms;
                            xCLoad.drPars4Load ["KEKS"] = xCDoc.xDocP.nEks;
                            xCLoad.drPars4Load ["NOMD"] = xCDoc.xDocP.sNomDoc;
                            xCLoad.drPars4Load ["SYSN"] = (int)(xCDoc.xDocP.lSysN);
                            xCLoad.drPars4Load["TYPOP"] = xCDoc.xDocP.TypOper;
                            LoadKomplLst(xCLoad.drPars4Load , AppC.F_LOADKPL);
                        }
                    }
                }
                EndOfLoad(AppC.RC_OK);
            }
        }


        private void DocFromSrv(SocketStream stmX, Dictionary<string, string> aC,
            DataSet ds, ref string sErr, int nRetSrv)
        {
            bool bMyRead = false;
            sErr = "Ошибка чтения XML";
            string sXMLFile = "";
            //int nFileSize = ServClass.ReadXMLWrite2File(stmX.SStream, ref sXMLFile);

            if (stmX.ASReadS.OutFile.Length == 0)
            {
                bMyRead = true;
                stmX.ASReadS.TermDat = AppC.baTermMsg;
                if (stmX.ASReadS.BeginARead(true, 1000 * 60) != SocketStream.ASRWERROR.RET_FULLMSG)
                    throw new System.Net.Sockets.SocketException(10061);
            }
            sXMLFile = stmX.ASReadS.OutFile;

            //sXMLFile = stmX.ASReadS..OutFile;

            xCLoad.dsZ = xNSI.MakeDataSetForLoad(xNSI.DT[NSI.BD_DOCOUT].dt, xNSI.DT[NSI.BD_DIND].dt);

            sErr = "Ошибка загрузки XML";
            xCLoad.dsZ.BeginInit();
            xCLoad.dsZ.EnforceConstraints = false;

            //byte[] baCom = Encoding.UTF8.GetBytes(sCom);

            System.Xml.XmlReader xmlRd = System.Xml.XmlReader.Create(sXMLFile);
            xCLoad.dsZ.ReadXml(xmlRd);
            xmlRd.Close();
            xNSI.AddNewNSI(xCLoad.dsZ);
            if (bMyRead)
                System.IO.File.Delete(sXMLFile);
            xCLoad.dsZ.EndInit();
            xCLoad.dr1st = null;
            nRetSrv = AddZ(xCLoad, ref sErr);
            if (nRetSrv == AppC.RC_OK)
                sErr = "OK";
            else
                throw new Exception(sErr);
        }






        // запрос на список комплектаций/заявку на комплектацию
        private bool LoadKomplLst(DataRow drPars, int nFunc)
        {
            bool bRet = AppC.RC_OKB;
            int nRet = AppC.RC_OK;
            string nCom = AppC.COM_ZKMPLST;
            LoadFromSrv dgL = null;
            ServerExchange xSE = new ServerExchange(this);

            xDP = xCLoad.xLP;
            xCLoad.nCommand = nFunc;

            if (drPars == null)
            {// загрузка списка комплектаций
                if (nFunc == AppC.F_LOADKPL)
                {
                    dgL = new LoadFromSrv(LstKomplFromSrv);
                }
                else
                {
                    dgL = new LoadFromSrv(LstKomplFromSrv);
                }
            }
            else
            {// загрузка заявки на комплектацию
                nCom = AppC.COM_ZKMPD;
                if (nFunc == AppC.F_LOADKPL)
                {
                    if (xCLoad.sSSCC == "")
                    {//
                        xDP.dDatDoc = DateTime.ParseExact((string)drPars["DT"], "yyyyMMdd", null);
                        xDP.nEks = (int)drPars["KEKS"];
                        xDP.lSysN = (long)drPars["SYSN"];
                    }
                    dgL = new LoadFromSrv(DocFromSrv);
                }
                else
                {
                    if (xCLoad.sSSCC == "")
                    {
                        xDP.dDatDoc = DateTime.ParseExact((string)drPars["DT"], "yyyyMMdd", null);
                        xDP.nEks = (int)drPars["KEKS"];
                        xDP.lSysN = (long)drPars["SYSN"];
                    }
                    dgL = new LoadFromSrv(DocFromSrv);
                }
            }

            string sL = xSE.ExchgSrv(nCom, "", "", dgL, null, ref nRet);
            if (nRet == AppC.RC_OK)
            {
                bRet = AppC.RC_OKB;
                if (drPars != null)
                {
                    if ((xCLoad.dr1st != null) && (xCLoad.dr1st.Table.TableName == NSI.BD_DOCOUT))
                    {
                        xCLoad.dr1st["TYPOP"] = (xCLoad.nCommand == AppC.F_LOADKPL) ? AppC.TYPOP_KMPL : AppC.TYPOP_OTGR;
                        xCLoad.dr1st["LSTUCH"] = xSm.LstUchKompl;
                        xCLoad.dr1st["DIFF"] = (int)(xCLoad.xLP.lSysN);
                        if (xCLoad.ilLoad.CurReg == AppC.UPL_FLT)
                            PosOnLoaded(nRet);
                    }
                }
            }
            else
            {
                bRet = AppC.RC_CANCELB;
                Srv.ErrorMsg(sL);
            }
            return (bRet);
        }

        private void LstKomplFromSrv(SocketStream stmX, Dictionary<string, string> aC,
            DataSet ds, ref string sErr, int nRetSrv)
        {
            bool bMyRead = false;
            DataTable dt = xNSI.DT[NSI.BD_KMPL].dt;
            string sOldName = dt.TableName;

            sErr = "Ошибка чтения XML";
            string sXMLFile = "";

            try
            {
                if (stmX.ASReadS.OutFile.Length == 0)
                {
                    bMyRead = true;
                    stmX.ASReadS.TermDat = AppC.baTermMsg;
                    if (stmX.ASReadS.BeginARead(true, 1000 * 60) != SocketStream.ASRWERROR.RET_FULLMSG)
                        throw new System.Net.Sockets.SocketException(10061);
                }
                sXMLFile = stmX.ASReadS.OutFile;

                dt.TableName = NSI.BD_ZDOC;

                sErr = "Ошибка загрузки XML";

                dt.BeginInit();
                dt.BeginLoadData();
                dt.Clear();

                System.Xml.XmlReader xmlRd = System.Xml.XmlReader.Create(sXMLFile);
                dt.ReadXml(xmlRd);
                xmlRd.Close();
                if (bMyRead)
                    System.IO.File.Delete(sXMLFile);
                dt.EndLoadData();
                dt.EndInit();


                sErr = "OK";
            }
            finally
            {
                dt.TableName = sOldName;
            }
        }

        private bool IsUsedSSCC(string sSSCC)
        {
            bool bRet = AppC.RC_CANCELB;
            string sRf = xCDoc.DefDetFilter() + String.Format("AND(SSCC='{0}')", sSSCC);
            DataView dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
            if (dv.Count > 0)
                bRet = AppC.RC_OKB; 
            return (bRet);
        }


        /// буферная таблица для преобразовпания SSCC->список продукции
        private DataTable 
            dtL = null;



        private void MakeTempDOUTD()
        {
            MakeTempDOUTD(xNSI.DT[NSI.BD_DOUTD].dt);
        }

        private void MakeTempDOUTD(DataTable dtDestin)
        {
            int 
                i = 0,
                nC = dtDestin.Columns.Count;
            DataColumn[] dcl = new DataColumn[nC];
            foreach (DataColumn dc in dtDestin.Columns)
                dcl[i++] = new DataColumn(dc.ColumnName, dc.DataType);
            dtL = new DataTable(NSI.BD_DOUTD);
            dtL.Columns.AddRange(dcl);
        }

        public void Back2Main()
        {
            if ((tcMain.SelectedIndex != PG_DOC) && (tcMain.SelectedIndex != PG_SCAN))
            {
                if (xScrDet.CurReg != 0)
                {// когда выйти из полноэкранного
                    Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                    xScrDet.NextReg(AppC.REG_SWITCH.SW_CLEAR, tNameSc);
                }
                tcMain.SelectedIndex = PG_DOC;
            }
            ((tcMain.SelectedIndex == PG_DOC) ? dgDoc : dgDet).Focus();
        }


        private void TryNextPoddon(DataRow[] drP)
        {
            int
                nOldP = xCDoc.xNPs.Current,
                nOperType = (int)xCDoc.drCurRow["TYPOP"];
            string
                sSSCC = "";

            DialogResult
                drz = CallDllForm(sExeDir + "SGPF-PdSSCC.dll", true);

            if (drz == DialogResult.OK)
            {
                xDLLAPars = (object[])xDLLPars;
                sSSCC = (string)xDLLAPars[1];
                xCDoc.xOper.SSCC = sSSCC;
                PrintEtikPoddon(String.Format("PAR=(SSCC={0});", sSSCC), sSSCC, drP);
            }
        }


        public bool PrintEtikPoddon(string sDop, string sSSCC, DataRow[] drPodd)
        {
            bool
                bRePrint = false,
                bRet = AppC.RC_CANCELB;
            int
                nPodd = 0,
                nNewPodd = 0,
                nRet = AppC.RC_OK;
            string
                sTmp,
                sRf,
                sOldSSCC = "",
                sH = "",
                sErr = "";
            DataView
                dv;
            DataSet
                dsTrans;
            DataRow[]
                drD = null;
            ServerExchange
                xSE = new ServerExchange(this);

            if ((sDop.Length == 0) && (sSSCC.Length == 0))
            {// запрос на печать и формирование SSCC
                if ((xSm.CurPrinterMOBName.Length > 0) || (xSm.CurPrinterSTCName.Length > 0))
                {
                    sH = (xSm.CurPrinterMOBName.Length > 0) ? xSm.CurPrinterMOBName : xSm.CurPrinterSTCName;
                }
                else
                {
                    Srv.ErrorMsg("Выберите принтер", true);
                    return (bRet);
                }
            }


            // какой поддон будет проставлен/сформирован
            sRf = xCDoc.DefDetFilter() + "AND(NPODDZ>0)";
            dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "NPODDZ DESC", DataViewRowState.CurrentRows);
            nPodd = (dv.Count > 0) ? (int)dv[0].Row["NPODDZ"] + 1 : nPodd = 1;

            // состав нового поддона
            sRf = xCDoc.DefDetFilter() + "AND( (ISNULL(NPODDZ, -1)=-1)OR(NPODDZ=0) )";

            // для документов проверки SSCC - устанавливаем по всему документу
            if ((int)xCDoc.drCurRow["CHKSSCC"] > 0)
            {
                try
                {
                    sRf = xCDoc.DefDetFilter();
                    dv = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "", DataViewRowState.CurrentRows);
                    sOldSSCC = (string)dv[0].Row["SSCC"];
                }
                catch 
                {
                    sOldSSCC = "";
                }
            }

            dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
            if (dv.Count > 0)
            {
                nNewPodd = nPodd;
                drD = new DataRow[dv.Count];
                for (int i = 0; i < dv.Count; i++)
                {
                    if ((dv[i].Row["SSCCINT"] is string) && (((string)dv[i].Row["SSCCINT"]).Length > 0))
                        {}
                    else
                        dv[i].Row["SSCCINT"] = sOldSSCC;
                    drD[i] = dv[i].Row;
                }
                //for (int i = 0; i < drD.Length; i++)
                //    drD[i]["NPODDZ"] = nPodd;
                //tCurrPoddon.Text = nPodd.ToString();
            }
            else
            {// вновь отсканированных нет, возможно, попытка распечатать существующий
                try
                {
                    nPodd = (int)drDet["NPODDZ"];
                    sTmp = drDet["SSCC"].ToString();
                    if (sTmp.Length == 0)
                    {
                        if (sSSCC.Length > 0)
                            sTmp = sSSCC;
                    }
                    if ((nPodd > 0) && (sTmp.Length > 0))
                    {
                        sRf = String.Format("Поддон № {0}", nPodd);
                        DialogResult drPr = MessageBox.Show("Распечатать повторно (Enter)?\n(ESC) - отменить", sRf,
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                        if (drPr == DialogResult.OK)
                        {
                            dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, xCDoc.DefDetFilter() + String.Format(" AND (NPODDZ={0})", nPodd), "", DataViewRowState.CurrentRows);
                            drD = new DataRow[dv.Count];
                            for (int i = 0; i < dv.Count; i++)
                                drD[i] = dv[i].Row;
                            bRePrint = true;
                            sSSCC = sTmp;
                            sDop = String.Format("PAR=(SSCC={0},TYPOP=REPRINT);", sSSCC);
                        }
                        else
                        {
                            return (bRet);
                        }
                    }
                    else
                        nPodd = -1;
                }
                catch
                {
                    nPodd = -1;
                }
                if (nPodd < 0)
                {
                    Srv.ErrorMsg("Отсутствуют данные", true);
                    return (bRet);
                }
            }

            // по умолчанию - все непривязанные к поддонам
            if (drPodd == null)
                drPodd = drD;

            xCUpLoad = new CurUpLoad(xPars);
            xCUpLoad.sCurUplCommand = AppC.COM_LST2SSCC;

            dsTrans = xNSI.MakeWorkDataSet(xNSI.DT[NSI.BD_DOCOUT].dt,
                      xNSI.DT[NSI.BD_DOUTD].dt, new DataRow[] { xCDoc.drCurRow }, drPodd, xSm, xCUpLoad);


            sErr = xSE.ExchgSrv(AppC.COM_LST2SSCC, sH, sDop, null, dsTrans, ref nRet, 20);
            if ((nRet != AppC.RC_OK) || (xSE.ServerRet != AppC.RC_OK))
            {
                Srv.ErrorMsg(sErr, true);
            }
            else
            {
                sH = "";
                if (xSE.ServerAnswer.ContainsKey("SSCC"))
                    sH = xSE.ServerAnswer["SSCC"];
                else if (xSE.AnswerPars.ContainsKey("SSCC"))
                    sH = xSE.AnswerPars["SSCC"];
                else if (sSSCC.Length > 0)
                    sH = sSSCC;

                if (!bRePrint)
                {
                    for (int i = 0; i < drPodd.Length; i++)
                    {
                        if (sH.Length > 0)
                            drPodd[i]["SSCC"] = sH;
                        if (nNewPodd > 0)
                            drPodd[i]["NPODDZ"] = nNewPodd;
                    }
                    if (nNewPodd > 0)
                        tCurrPoddon.Text = nNewPodd.ToString();
                }
                else
                {
                    nNewPodd = nPodd;
                }

                bRet = AppC.RC_OKB;
                sErr = "Данные отправлены";
                if (sH.Length > 0)
                    sErr = "SSCC=" + sH + "\n" + sErr;
                sH = String.Format("Поддон № {0}", nNewPodd);
                AddSSCC2SSCCTable(sH, nNewPodd, xCDoc, true);
                MessageBox.Show(sErr, sH);
            }
            Back2Main();
            return (bRet);
        }

        public void SetSSCCForPoddon(string sSSCC, DataView dv, int nP, string sF)
        {
            foreach (DataRowView drv in dv)
            {
                (drv.Row[sF]) = sSSCC;
            }
            MessageBox.Show(String.Format("Поддон {0} подготовлен ({1}) позиций", nP, dv.Count));

            //xCDoc.xNPs.TryNextPoddon(true);
            tCurrPoddon.Text = xCDoc.xNPs.Current.ToString();
        }

        public int ConvertSSCC2Lst_Old(ServerExchange xSE, ScanVarRM xSc, ref PSC_Types.ScDat scD, DataTable dtResult, bool bInfoOnEmk)
        {
            int nRec,
                nRet = AppC.RC_OK;
            string
                sP,
                sSSCC = xSc.Dat;

            DataSet
                dsTrans;

            // вместе с командой отдаем заголовок документа
            xCUpLoad = new CurUpLoad(xPars);
            xCUpLoad.sCurUplCommand = AppC.COM_ZSC2LST;
            dsTrans = xNSI.MakeWorkDataSet(xNSI.DT[NSI.BD_DOCOUT].dt, dtResult, new DataRow[] { xCDoc.drCurRow }, null, xSm, xCUpLoad);

            MakeTempDOUTD(dtResult);

            LoadFromSrv dgL = new LoadFromSrv(LstFromSSCC);

            xCLoad = new CurLoad();
            xCLoad.sSSCC = sSSCC;
            xCLoad.xLP.lSysN = xCDoc.nId;
            xCLoad.dtZ = dtL;


            //Cursor crsOld = Cursor.Current;
            //Cursor.Current = Cursors.WaitCursor;

            sP = String.Format("(SSCC={0},TYPE={1})", sSSCC, "ROW");
            string sL = xSE.ExchgSrv(AppC.COM_ZSC2LST, sP, "", dgL, dsTrans, ref nRet, 20);

            //nRet = TestProdBySrv(xSE, nRet);

            if (nRet == AppC.RC_OK)
            {
                nRec = xCLoad.dtZ.Rows.Count;
                if (nRec == 1)
                {// будем изображивать сканирование
                    SetVirtScan(xCLoad.dtZ.Rows[0], ref scD, true, bInfoOnEmk);
                }
                else
                {// добавление группы ???
                    //if (xCDoc.nTypOp != AppC.TYPOP_OTGR)
                    //{
                    //    for (int i = 0; i < nRec; i++)
                    //    {
                    //        PSC_Types.ScDat scMD = new PSC_Types.ScDat();
                    //        SetVirtScan(xCLoad.dtZ.Rows[i], ref scMD);
                    //        xNSI.AddDet(scMD, xCDoc, null);
                    //    }
                    //}
                    //else
                    //{
                    //    nRet = AppC.RC_MANYEAN;
                    //}
                    nRet = AppC.RC_MANYEAN;
                }
            }
            else
            {// просто сохраним запись ??? -  если была сетевая ошибка! при ошибке сервера ничего сохранять не надо!
                if (xSE.ServerRet == AppC.RC_OK)
                {// сервер против этой записи ничего не имеет
                }
                //if (bInfoOnEmk)
                Srv.ErrorMsg(sL);
            }

            return (nRet);
        }


        // --- найти или создать временный документ
        private DataRow IsDocMovePresent(int nFunc, bool bCreateNew)
        {
            int
                nTypDoc = -1;
            string
                sDocBC,
                sNomDoc,
                sFilt4Doc;
            DataRow
                ret = null;
            DataView
                dv;

            sNomDoc = "000";
            switch (nFunc)
            {
                case AppC.F_TMPMARK:
                    sDocBC = "SSCC";
                    nTypDoc = AppC.TYPD_MARK;
                    break;
                case AppC.F_TMPMOV:
                    sDocBC = "ПЕРЕМЕЩЕНИЕ";
                    nTypDoc = AppC.TYPD_MOVINT;
                    break;
                default:
                    sDocBC = "TMP";
                    break;
            }

            if (nTypDoc >= 0)
            {
                sFilt4Doc = String.Format("(TD={0})AND(NOMD='{1}')", nTypDoc, sNomDoc);
                dv = new DataView(xNSI.DT[NSI.BD_DOCOUT].dt, sFilt4Doc, "DT", DataViewRowState.CurrentRows);

                if (dv.Count == 0)
                {
                    if (bCreateNew)
                    {
                        CurDoc xD = new CurDoc(xSm);
                        xD.xDocP.nNumTypD = nTypDoc;
                        xD.xDocP.sNomDoc = sNomDoc;
                        xD.xDocP.sBC_Doc = sDocBC;
                        xD.xDocP.nSklad = xSm.nSklad;
                        xD.xDocP.dDatDoc = xCDoc.xDocP.dDatDoc;
                        xD.xDocP.sSmena = "TMP";
                        if (xNSI.AddDocRec(xD))
                        {
                            ret = xD.drCurRow;
                        }
                        else
                            Srv.ErrorMsg("Ошибка добавления документа!");
                    }
                }
                else
                {
                    ret = dv[0].Row;
                }
            }

            return (ret);
        }


        // --- временная операция начинается
        // nFunc -  F_TMPMARK - // временная операция маркировки
        //          F_TMPMOV  - // временная операция перемещения
        private void TempOperStartEnd(int nFunc)
        {
            int 
                nPg = tcMain.SelectedIndex;
            DataRow
                drCurr = xCDoc.drCurRow,
                drTMP = null;
            CurrencyManager
                cmDoc = (CurrencyManager)BindingContext[dgDoc.DataSource];


                    drTMP = IsDocMovePresent(nFunc, true);
                    if (drTMP is DataRow)
                    {
                        if (drTMP != drCurr)
                        {// будет переход во временный документ
                            for (int i = 0; i < cmDoc.List.Count; i++)
                                //if (((DataRowView)cmDoc.List[i]).Row["SYSN"] == dr["SYSN"])
                                if (((DataRowView)cmDoc.List[i]).Row == drTMP)
                                {
                                    xSm.DocBeforeTmpMove(drCurr, ref nPg);
                                    cmDoc.Position = i;

                                    xCDoc.drCurRow = drTMP;
                                    xNSI.InitCurDoc(xCDoc, xSm);
                                    if (nFunc == AppC.F_TMPMARK)
                                    {// для маркировки удаляем все предыдущие
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
                                    }
                                    SetParFields(xCDoc.xDocP);
                                    if (tcMain.SelectedIndex == PG_DOC)
                                        tcMain.SelectedIndex = PG_SCAN;

                                    NewDoc((DataTable)this.dgDet.DataSource);
                                    lDocInf.Text = CurDocInf(xCDoc.xDocP);
                                    break;
                                }
                        }
                        else
                        {// будет переход из временного документа
                            RetAfterTempMove();
                        }
                    }
        }

        // --- временная операция перемещения успешно завершена
        private void RetAfterTempMove()
        {
            int
                nPg = tcMain.SelectedIndex;
            CurrencyManager
                cmDoc = (CurrencyManager)BindingContext[dgDoc.DataSource];
            DataRow
                dBef = xSm.DocBeforeTmpMove(1, ref nPg);

            if (dBef is DataRow)
            {
                for (int i = 0; i < cmDoc.List.Count; i++)
                    //if (((DataRowView)cmDoc.List[i]).Row["SYSN"] == dBef["SYSN"])
                    if (((DataRowView)cmDoc.List[i]).Row == dBef)
                    {
                        cmDoc.Position = i;
                        xCDoc.drCurRow = dBef;
                        xNSI.InitCurDoc(xCDoc, xSm);
                        SetParFields(xCDoc.xDocP);
                        NewDoc((DataTable)this.dgDet.DataSource);
                        lDocInf.Text = CurDocInf(xCDoc.xDocP);
                        // сбросить адрес возврата
                        tcMain.SelectedIndex = nPg;
                        xSm.DocBeforeTmpMove(null, ref nPg);
                        break;
                    }
            }
        }


        // загрузка заданий от сервера
        public void ProceedSrvTasks(string sXMLData)
        {
            int
                nRec,
                nRet = AppC.RC_OK;
            string
                sErr;

            DataRow
                dr = null;
            DataTable
                dtD = xNSI.DT[NSI.BD_DIND].dt;
            DataSet
                ds;


            try
            {

                DataRow drVPer = IsDocMovePresent(AppC.F_TMPMOV, true);

                xCLoad.dsZ = xNSI.MakeDataSetForLoad(xNSI.DT[NSI.BD_DOCOUT].dt, xNSI.DT[NSI.BD_DIND].dt);

                sErr = "Ошибка загрузки XML";
                xCLoad.dsZ.BeginInit();
                xCLoad.dsZ.EnforceConstraints = false;

                //string f = @"\Temp\ZVK.xml";
                //System.IO.TextReader streamReader = new System.IO.StreamReader(f);
                //sXMLData = streamReader.ReadToEnd();
                //streamReader.Close();

                System.Xml.XmlReader xmlRd = System.Xml.XmlReader.Create(new System.IO.StringReader(sXMLData));

                //System.Xml.XmlReader xmlRd = System.Xml.XmlReader.Create(f);

                xCLoad.dsZ.ReadXml(xmlRd);
                xmlRd.Close();
                xCLoad.dsZ.EndInit();
                nRet = AddZ(xCLoad, ref sErr, true);
                if (nRet == AppC.RC_OK)
                    sErr = "Получены новые задания!";
                Srv.ErrorMsg(sErr);
            }
            catch(Exception exx)
            {
                int i = 99;
            }
        }










        private int 
            nSpecAdrWait = 0;

        /// обработка клавиш для панели временного ввода
        //private bool Keys4FixAddr(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        //{
        //    int
        //        nRet = AppC.RC_OK,
        //        nFunc = (int)nF;
        //    string
        //        sSSCC,
        //        sErr,
        //        sPar;
        //    bool
        //        bKeyHandled = false;
        //    ServerExchange
        //        xSE;


        //    if ((e.KeyValue == W32.VK_ENTER) ||
        //        //((e.KeyValue == W32.VK_F3) && ((nSpecAdrWait == AppC.F_CELLINF) || (nSpecAdrWait == AppC.F_CNTSSCC))))
        //        ((nFunc == AppC.F_LOAD_DOC) && ((nSpecAdrWait == AppC.F_CELLINF) || (nSpecAdrWait == AppC.F_CNTSSCC))))
        //    {
        //        bKeyHandled = true;
        //        switch (nSpecAdrWait)
        //        {
        //            case AppC.F_CNTSSCC:
        //                // содержимое SSCC
        //                PSC_Types.ScDat scD = scCur;
        //                sSSCC = 
        //                    scD.sSSCC = xCDoc.sSSCC;
        //                if (sSSCC.Length == 20)
        //                {
        //                    xSE = new ServerExchange(this);
        //                    nRet = ConvertSSCC2Lst(xSE, sSSCC, ref scD, xNSI.DT[NSI.BD_DOUTD].dt, true);
        //                    if ((nRet == AppC.RC_OK) || (nRet == AppC.RC_MANYEAN))
        //                    {
        //                        if (e.KeyValue == W32.VK_ENTER)
        //                        {// показать содержимое SSCC
        //                            ShowSSCCContent(xCLoad.dtZ, sSSCC, xCDoc.xOper.xAdrSrc);
        //                        }
        //                        else
        //                        {// добавить содержимое SSCC
        //                            nRet = AddGroupDet(nRet, (int)NSI.SRCDET.SSCCT, sSSCC);
        //                        }
        //                    }
        //                }
        //                break;
        //            case AppC.F_SETADRZONE:
        //                // функция фиксации адреса
        //                if (xSm.xAdrForSpec != null)
        //                {
        //                    xSm.xAdrFix1 = xSm.xAdrForSpec;
        //                    lDocInf.Text = CurDocInf(xCDoc.xDocP);
        //                }
        //                break;
        //            case AppC.F_CELLINF:
        //                if (xSm.xAdrForSpec != null)
        //                {
        //                    xCDoc.xOper.SetOperSrc(xSm.xAdrForSpec, xCDoc.xDocP.DType);
        //                    if ((xCDoc.xDocP.TypOper == AppC.TYPOP_MOVE) ||
        //                        (e.KeyValue == W32.VK_ENTER))
        //                        sPar = "TXT";
        //                    else
        //                        sPar = "ROW";
        //                    nRet = ConvertAdr2Lst(xSm.xAdrForSpec, AppC.COM_CELLI, sPar, true);
        //                }
        //                break;
        //            case AppC.F_CLRCELL:
        //                if (xSm.xAdrForSpec != null)
        //                {
        //                    xSE = new ServerExchange(this);
        //                    xSE.FullCOM2Srv = String.Format("COM={0};KSK={1};MAC={2};KP={3};PAR=(KSK={1},ADRCELL={4});",
        //                        AppC.COM_CCELL,
        //                        xSm.nSklad,
        //                        xPars.MACAdr,
        //                        xSm.sUser,
        //                        xSm.xAdrForSpec.Addr
        //                        );
        //                    sErr = xSE.ExchgSrv(AppC.COM_CCELL, "", "", null, null, ref nRet);
        //                    if (xSE.ServerRet == AppC.RC_OK)
        //                    {
        //                        Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
        //                        Srv.ErrorMsg(xSm.xAdrForSpec.AddrShow, "Очищено...", false);
        //                    }
        //                    else
        //                        Srv.ErrorMsg(sErr, "Ошибка!", true);
        //                }
        //                break;
        //        }
        //    }

        //    if ((bKeyHandled == true) 
        //        || ( (e.KeyValue == W32.VK_LEFT) || (e.KeyValue == W32.VK_RIGHT) || (e.KeyValue == W32.VK_ESC)))
        //    {
        //            bKeyHandled = true;
        //            xFPan.InfoHeightUp(false, 2);
        //            xFPan.IFaceReset(false);
        //            xFPan.HideP();
        //            // дальше клавиши не обрабатываю
        //            ehCurrFunc -= Keys4FixAddr;
        //            nSpecAdrWait = 0;
        //            xSm.xAdrForSpec = null;
        //    }
        //    return (bKeyHandled);
        //}




        static BarcodeScannerEventArgs
            xSimScan;
        static string
            sSimulScan = "";

        Srv.CurrFuncKeyHandler
            ehCurrFuncW4 = null;

        private bool Keys4FixAddr(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        {
            int
                nRet = AppC.RC_OK,
                nFunc = (int)nF;
            string
                sSSCC,
                sErr,
                sPar;
            bool
                bSimulScan = false,
                bHandledByOther = false,
                bCloseWait = false,
                bKeyHandled = false;
            ServerExchange
                xSE;

            try
            {
                if (ehCurrFuncW4 != null)
                {// клавиши ловит одна из функций
                    bHandledByOther = ehCurrFuncW4(nFunc, e, ref ehCurrFuncW4);
                }
            }
            catch
            {
                Srv.ErrorMsg("Ошибка обработки", true);
                bHandledByOther = true;
            }

            bKeyHandled = bHandledByOther;

            if (!bHandledByOther)
            {
                if ((nFunc > 0) && !(e.KeyValue == W32.VK_PERIOD))
                {
                    //if (nFunc != PDA.Service.AppC.F_HELP)
                    bKeyHandled = true;
                }
                else
                {
                    if ((e.KeyValue == W32.VK_ENTER) ||
                        ((e.KeyValue == W32.VK_PERIOD) && ((nSpecAdrWait == AppC.F_CELLINF)
                        || (nSpecAdrWait == AppC.F_SIMSCAN)
                        || (nSpecAdrWait == AppC.F_CHKSSCC)
                        || (nSpecAdrWait == AppC.F_CNTSSCC))))
                    {
                        bCloseWait = true;
                        switch (nSpecAdrWait)
                        {
                            case AppC.F_CHKSSCC:
                            // Загрузка SSCC в заявку
                            case AppC.F_CNTSSCC:
                                // содержимое SSCC
                                if (xCDoc.sSSCC.Length == 20)
                                {
                                    PSC_Types.ScDat scD = scCur;
                                    sSSCC =
                                        scD.sSSCC = xCDoc.sSSCC;
                                    try
                                    {
                                        xSE = xCLoad.xLastSE;
                                        nRet = (xCLoad.dtZ.Rows.Count > 1) ? AppC.RC_MANYEAN : AppC.RC_OK;
                                    }
                                    catch
                                    {
                                        xSE = new ServerExchange(this);
                                        if (nSpecAdrWait== AppC.F_CNTSSCC)
                                            nRet = ConvertSSCC2Lst(xSE, sSSCC, ref scD, true);
                                        else
                                            nRet = ConvertSSCC2Lst(xSE, sSSCC, ref scD, true, xNSI.DT[NSI.BD_DIND].dt);
                                    }

                                    if ((nRet == AppC.RC_OK) || (nRet == AppC.RC_MANYEAN))
                                    {
                                        if (e.KeyValue == W32.VK_ENTER)
                                        {// показать содержимое SSCC
                                            ShowSSCCContent(xCLoad.dtZ, sSSCC, xSE, xCDoc.xOper.xAdrSrc, ref ehCurrFuncW4);
                                            bCloseWait = false;
                                        }
                                        else if (e.KeyValue == W32.VK_PERIOD)
                                        {
                                            if (nSpecAdrWait == AppC.F_CNTSSCC)
                                            {// добавить содержимое SSCC
                                                if (!IsDupSSCC(sSSCC))
                                                    AddGroupDet(AppC.RC_MANYEAN, (int)NSI.SRCDET.SSCCT, sSSCC);
                                                else
                                                    Srv.ErrorMsg("Уже добавлялся!", sSSCC, false);
                                            }
                                            else
                                            {// контроль
                                                TryLoadSSCC(xCDoc.sSSCC, nRet);
                                            }
                                        }
                                    }
                                }
                                else
                                    bCloseWait = false;
                                break;
                            case AppC.F_SETADRZONE:
                                // функция фиксации адреса
                                if (xSm.xAdrForSpec != null)
                                {
                                    xSm.xAdrFix1 = xSm.xAdrForSpec;
                                    lDocInf.Text = CurDocInf(xCDoc.xDocP);
                                }
                                break;
                            case AppC.F_CELLINF:
                                if (xSm.xAdrForSpec != null)
                                {
                                    xCDoc.xOper.SetOperSrc(xSm.xAdrForSpec, xCDoc.xDocP.DType);
                                    if ((xCDoc.xDocP.TypOper == AppC.TYPOP_MOVE) ||
                                        (e.KeyValue == W32.VK_ENTER))
                                    {
                                        sPar = "TXT";
                                        bCloseWait = false;
                                    }
                                    else
                                        sPar = "ROW";
                                    nRet = ConvertAdr2Lst(xSm.xAdrForSpec, AppC.COM_CELLI, sPar, true, NSI.SRCDET.FROMADR_BUTTON, ref ehCurrFuncW4);
                                }
                                break;
                            case AppC.F_CLRCELL:
                                if (xSm.xAdrForSpec != null)
                                {
                                    xSE = new ServerExchange(this);
                                    xSE.FullCOM2Srv = String.Format("COM={0};KSK={1};MAC={2};KP={3};PAR=(KSK={1},ADRCELL={4});",
                                        AppC.COM_CCELL,
                                        xSm.nSklad,
                                        xPars.MACAdr,
                                        xSm.sUser,
                                        xSm.xAdrForSpec.Addr
                                        );
                                    sErr = xSE.ExchgSrv(AppC.COM_CCELL, "", "", null, null, ref nRet);
                                    if (xSE.ServerRet == AppC.RC_OK)
                                    {
                                        Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
                                        Srv.ErrorMsg(xSm.xAdrForSpec.AddrShow, "Очищено...", false);
                                    }
                                    else
                                    {
                                        Srv.ErrorMsg(sErr, "Ошибка!", true);
                                        bCloseWait = false;
                                    }
                                }
                                break;

                            case AppC.F_SIMSCAN:
                                if (tbPanP1G.Text.Length > 0)
                                {
                                    bSimulScan = true;
                                    sSimulScan = tbPanP1G.Text;
                                    xSimScan = new BarcodeScannerEventArgs(BCId.Code128, sSimulScan);
                                    //if (this.InvokeRequired)
                                    //    ;
                                    //ThreadPool.QueueUserWorkItem(CallScanWithPause, this);
                                    //OnScan(this, eScan);
                                }
                                break;

                        }
                    }
                    else
                    {
                        switch (e.KeyValue)
                        {
                            case W32.VK_ESC:
                                bCloseWait = true;
                                break;
                            default:
                                //bKeyHandled = true;

                                if (nSpecAdrWait == AppC.F_SIMSCAN)
                                    bKeyHandled = false;
                                else
                                    bKeyHandled = true;

                                break;
                        }
                    }

                    if (bCloseWait)
                    {
                        bKeyHandled = true;
                        xFPan.InfoHeightUp(false, 2);
                        xFPan.IFaceReset(false);
                        xFPan.HideP((tcMain.SelectedIndex == PG_DOC) ? dgDoc : dgDet);
                        // дальше клавиши не обрабатываю
                        ehCurrFunc -= Keys4FixAddr;
                        nSpecAdrWait = 0;
                        xSm.xAdrForSpec = null;
                        ShowOperState(xCDoc.xOper);
                        //Application.DoEvents();
                        if (xCLoad != null)
                        {
                            xCLoad.dtZ = null;
                            xCLoad.xLastSE = null;
                        }
                        xCDoc.sSSCC = "";
                        //Back2Main();
                        if (bSimulScan)
                        {
                            OnScan(this, xSimScan);
                        }
                    }

                }
            }
            else
            {
            }

            return (bKeyHandled);
        }




        /// окно для сканирования
        //private void WaitScan4Func(int nWaitMode, string sMsg, string sHeader)
        //{
        //    nSpecAdrWait = nWaitMode;
        //    tbPanP2G.Visible = false;
        //    xFPan.IFaceReset(true);
        //    if (nWaitMode == AppC.F_GENSCAN)
        //        xFPan.InfoHeightUp(true, 2);

        //    xFPan.ShowP(6, 28, sMsg, sHeader);
        //    // дальше клавиши обработаю сам
        //    ehCurrFunc += new Srv.CurrFuncKeyHandler(Keys4FixAddr);
        //}



        /// ожидание сканирования для спецрежимов
        private void WaitScan4Func(int nWaitMode, string sMsg, string sHeader)
        {
            WaitScan4Func(nWaitMode, sMsg, sHeader, null);
        }

        /// ожидание сканирования для спецрежимов
        private void WaitScan4Func(int nWaitMode, string sMsg, string sHeader, ScanVarRM xSc)
        {
            nSpecAdrWait = nWaitMode;
            tbPanP2G.Visible = false;
            xFPan.IFaceReset(true);
            if ((nWaitMode == AppC.F_GENSCAN)
                || (nWaitMode == AppC.F_SIMSCAN))
                xFPan.InfoHeightUp(true, 2);
            xFPan.ShowP(6, 28, sMsg, sHeader);

            if (nWaitMode == AppC.F_SIMSCAN)
            {
                tbPanP1G.Focus();
            }

            ehCurrFunc += new Srv.CurrFuncKeyHandler(Keys4FixAddr);
            if (xSc != null)
            {
                SpecScan(xSc);
                W32.keybd_event(W32.VK_ENTER, W32.VK_ENTER, 0, 0);
                W32.keybd_event(W32.VK_ENTER, W32.VK_ENTER, W32.KEYEVENTF_KEYUP, 0);
            }
        }





    }
}
