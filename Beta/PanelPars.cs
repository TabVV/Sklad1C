using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;

using ScannerAll;
using PDA.OS;
using PDA.Service;


namespace SkladRM
{
    public partial class MainF : Form
    {
        //public event EventParsHandler ParsEvents;

        public delegate void EditParsOver(int nRetCode, int nFunc);
        EditParsOver dgOver;

        
        private DocPars
            xDP;                                            // текущие параметры (при редактировании)

        private int 
            //nTypDOld,                                       // значения типа документа до редактирования
            nCurFunc;                                       // текущая функция 


        // флаг работы с параметрами
        //private bool 
        //    bWorkWithDocPars = false;

        // флаг завершения ввода
        //private bool bQuitEdPars = false;
                
        private BarcodeScanner.BarcodeScanEventHandler 
            ehParScan =  null;

        // предыдущий обработчик клавиатуры
        Srv.CurrFuncKeyHandler oldKeyH;



        // с какого поля начать: первого доступного или первого пустого
        public enum CTRL1ST : int
        {
            START_AVAIL = 1,
            START_EMPTY = 2,
            START_LAST  = 3,
        }


        private void OnScanPar(object sender, BarcodeScannerEventArgs e)
        {
            if ((e.nID != BCId.NoData) && (bEditMode == true))
            {
                if ((e.nID == BCId.Code128) && (e.Data.Length == 14))
                {// Путевой лист или ТТН
                    //if (tNom_p.Enabled)
                    //    tNom_p.Text = e.Data.Substring(7);
                }
            }
        }


        // вход в режим ввода/корректировки параметров
        public void EditPars(int nReg, DocPars x, CTRL1ST FirstEd, AppC.VerifyEditFields dgVer, EditParsOver dgEnd)
        {
            bool
                bEnField = true;

            xDP = x;
            if (x != null)
            {
                //bQuitEdPars = false;
                nCurFunc = nReg;
                //bWorkWithDocPars = true;
                //ServClass.dgVerEd = new AppC.VerifyEditFields(dgVer);
                dgOver = new EditParsOver(dgEnd);
                SetParFields(xDP);

                oldKeyH = ehCurrFunc;
                ehCurrFunc = new Srv.CurrFuncKeyHandler(PPars_KeyDown);

                //xBCScanner.BarcodeScan -= ehScan;
                ehParScan = new BarcodeScanner.BarcodeScanEventHandler(OnScanPar);
                xBCScanner.BarcodeScan += ehParScan;

                //BeginEditPars(FirstEd, dgVer);

                aEdVvod = new AppC.EditListC(dgVer);

                if (nReg == AppC.F_LOAD_DOC)
                {
                    if (xDP.sBC_Doc.Length > 0)
                    {
                        bEnField = false;
                    }
                }
                aEdVvod.AddC(tKT_p, bEnField);
                aEdVvod.AddC(tNom_p, bEnField);
                aEdVvod.AddC(tBCDoc);

                if (FirstEd == CTRL1ST.START_EMPTY)
                    aEdVvod.SetCur(0);
                else
                    aEdVvod.SetCur(tBCDoc);
                SetEditMode(true);


            }
        }

        // сброс/установка полей ввода/вывода
        private void SetParFields(DocPars xDP)
        {
            int 
                n = xDP.nNumTypD;

            tNT_p.Text = TName(xDP.nNumTypD);
            if (tNT_p.Text.Length == 0)
            {
                tNT_p.Text = "<Неизвестный>";
                tKT_p.Text = "";
                xDP.nNumTypD = AppC.EMPTY_INT;
            }
            else
            {
                tKT_p.Text = xDP.nNumTypD.ToString();
            }
            //xDP.sTypD = tNT_p.Text;

            tNom_p.Text = xDP.sNomDoc;
            tBCDoc.Text = xDP.sBC_Doc;
            tBCML.Text = xDP.sBC_ML;

            tDateD_p.Text = DateTime.Now.ToString("dd.MM.yy");
            if (xDP.dDatDoc != DateTime.MinValue)
            {
                tDateD_p.Text = xDP.dDatDoc.ToString("dd.MM.yy");
            }
            tKSkl_p.Text = "";
            tNSkl_p.Text = "";
            if (xDP.nSklad != AppC.EMPTY_INT)
            {
                tKSkl_p.Text = xDP.nSklad.ToString();
                NSI.RezSrch zS = xNSI.GetNameSPR(NSI.NS_SKLAD, new object[] { xDP.nSklad }, "NAME");
                if (zS.bFind == true)
                {
                    tNSkl_p.Text = zS.sName;
                    xDP.sSklad = zS.sName;
                }

            }

            //tKUch_p.Text = "";
            //if ((xDP.nUch != AppC.EMPTY_INT) && (xDP.nUch != 0))
            //{
            //    tKUch_p.Text = xDP.nUch.ToString();
            //}


            //tSm_p.Text = xDP.sSmena;

            //tKEks_p.Text = "";
            //tNEks_p.Text = "";
            //if (xDP.nEks != AppC.EMPTY_INT)
            //{
            //    tKEks_p.Text = xDP.nEks.ToString();
            //    NSI.RezSrch zS = xNSI.GetNameSPR(NSI.NS_EKS, new object[] { xDP.nEks }, "FIO");
            //    if (zS.bFind == true)
            //    {
            //        tNEks_p.Text = zS.sName;
            //        xDP.sEks = zS.sName;
            //    }
            //}
            //else
            //    xDP.sEks = "";


            //tKPol_p.Text = "";
            //tNPol_p.Text = "";
            //if (xDP.nPol != AppC.EMPTY_INT)
            //{
            //    tKPol_p.Text = xDP.nPol.ToString();
            //    if (xDP.nTypD == AppC.TYPD_RASHNKLD)
            //    {
            //        sIS = DocPars.OPRName(ref xDP.nPol);
            //    }
            //    else
            //    {
            //        NSI.RezSrch zS = xNSI.GetNameSPR((xDP.nTypD == AppC.TYPD_PERMGP ) ?
            //            NSI.NS_SKLAD : NSI.NS_PP, new object[] { xDP.nPol }, "NAME");
            //        sIS = zS.sName;
            //    }
            //    tNPol_p.Text = sIS;
            //    xDP.sPol = sIS;

            //}
            //else
            //    xDP.sPol = "";
        }

        private void SetTypSensitive(int nT, ref bool bE, ref bool bP, ref bool bN)
        {
            switch (nT)
            {
                case AppC.TYPD_VOZV:
                    bE = false;
                    break;
                case AppC.TYPD_SVOD:
                    bP = false;
                    break;
                case AppC.TYPD_INV:
                    bE = false;
                    bP = false;
                    break;
                case AppC.TYPD_PERMGP:
                    if (xDP.nEks == AppC.EMPTY_INT)
                        bE = false;
                    break;
                case AppC.TYPD_RASHNKLD:
                    bE = false;
                    bP = true;
                    bN = false;
                    break;
                case AppC.TYPD_BRAK:
                    bE = false;
                    bP = false;
                    bN = true;
                    break;
                default:
                    break;
            }
        }

        // создание массива управления редактированием полей
        //private void BeginEditPars(CTRL1ST FirstEd, AppC.VerifyEditFields dgV)
        //{
        //    bool
        //        bEnField = true;

        //    aEdVvod = new AppC.EditListC(dgV);

        //    if (nCurFunc == AppC.F_LOAD_DOC)
        //    {
        //        if (xDP.sBC_Doc.Length > 0)
        //        {
        //            bEnField = false;
        //        }
        //    }
        //    aEdVvod.AddC(tKT_p, bEnField);
        //    aEdVvod.AddC(tNom_p, bEnField);
        //    aEdVvod.AddC(tBCDoc);


        //    aEdVvod.SetCur(aEdVvod[0]);
        //    SetEditMode(true);
        //}

        // завершение режима ввода/корректировки параметров
        public void EndEditPars(int nKey)
        {
            int nRet = (nKey == W32.VK_ENTER) ? AppC.RC_OK : AppC.RC_CANCEL;
            ehCurrFunc -= PPars_KeyDown;
            ehCurrFunc = oldKeyH;

            if (ehParScan != null)
                xBCScanner.BarcodeScan -= ehParScan;
            //xBCScanner.BarcodeScan += ehScan;

            SetEditMode(false);
            aEdVvod.EditIsOver();

            //bWorkWithDocPars = false;
            dgOver(nRet, nCurFunc);
        }



        // сохранение предыдущего значения типа документа
        //private void SaveOldTyp(object sender, EventArgs e)
        //{
        //    ((TextBox)sender).SelectAll();
        //    //nTypDOld = xDP.nTypD;
        //}

        // изменение типа, вывод нименования
        private void tKT_p_TextChanged(object sender, EventArgs e)
        {
            if (bEditMode == true)
            {// при просмотре не проверяется
                int nTD = AppC.EMPTY_INT;
                string s = "";

                try
                {
                    nTD = int.Parse(tKT_p.Text);
                    s = TName(nTD);
                }
                catch { s = ""; }

                if (s.Length == 0)
                {
                    xDP.nNumTypD = AppC.EMPTY_INT;
                    s = "<Неизвестный>";
                }
                else
                {
                    xDP.nNumTypD = nTD;
                }

                tNT_p.Text = s;
            }
        }



        // проверка типа
        private void tKT_p_Validating(object sender, CancelEventArgs e)
        {
            string
                sT = tKT_p.Text.Trim();

            if (bEditMode)
            {
                if (xDP.nNumTypD == AppC.EMPTY_INT)
                {
                    if (!aEdVvod.Fict4Next.Focused)
                        Srv.ErrorMsg("Укажите тип!", true);
                    e.Cancel = true;
                }
            }
        }

        // тип документа все-таки сменился
        //private void tKT_p_Validated(object sender, EventArgs e)
        //{
        //    int i;
        //    if (bEditMode)
        //    {
        //        if (xDP.nTypD == AppC.EMPTY_INT)
        //        {
        //            tKT_p.Text = "";
        //            tNT_p.Text = "";
        //            //e.Cancel = true;
        //            //ServClass.TBColor((TextBox)sender, true);
        //            //for (i = 0; i < aEdVvod.Count; i++)
        //            //{
        //            //    if (aEdVvod[i] != tKT_p)
        //            //        aEdVvod[i].Enabled = true;
        //            //}
        //        }
        //    }
        //}





        // проверка склада
        private void tKSkl_p_Validating(object sender, CancelEventArgs e)
        {
            string sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                try
                {
                    int nS = int.Parse(sT);
                    NSI.RezSrch zS = xNSI.GetNameSPR(NSI.NS_SKLAD, new object[] { nS }, "NAME");
                    tNSkl_p.Text = zS.sName;
                    if (zS.bFind == false)
                        e.Cancel = true;
                    else
                    {
                        xDP.nSklad = nS;
                        xDP.sSklad = zS.sName;
                    }
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
            {
                xDP.nSklad = AppC.EMPTY_INT;
            }
            //if (e.Cancel != true)
                //e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
                //aEdVvod.TryNext(AppC.CC_NEXT);

            if ((true == e.Cancel) || (xDP.nSklad == AppC.EMPTY_INT))
            {
                Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                xDP.sSklad = "";
                xDP.nUch = AppC.EMPTY_INT;
                tKUch_p.Text = "";
            }
        }

        // проверка участка
        private void tKUch_p_Validating(object sender, CancelEventArgs e)
        {
            string sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                try
                {
                    int nS = int.Parse(tKSkl_p.Text),
                        nU = int.Parse(tKUch_p.Text);
                    NSI.RezSrch zS = xNSI.GetNameSPR(NSI.NS_SUSK, new object[] { nS, nU }, "NAME");
                    if (zS.bFind == false)
                        e.Cancel = true;
                    else
                        xDP.nUch = nU;
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
                xDP.nUch = AppC.EMPTY_INT;
            //if (e.Cancel != true)
            //    e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
        }

        // проверка даты
        private void tDateD_p_Validating(object sender, CancelEventArgs e)
        {
            string sD = ((TextBox)sender).Text.Trim();
            if (sD.Length > 0)
            {
                try
                {
                    sD = Srv.SimpleDateTime(sD);
                    DateTime d = DateTime.ParseExact(sD, "dd.MM.yy", null);
                    xDP.dDatDoc = d;
                    ((TextBox)sender).Text = sD;
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
                xDP.dDatDoc = DateTime.MinValue;
            //if (e.Cancel != true)
            //    e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
        }

        // проверка смены
        private void tSm_p_Validating(object sender, CancelEventArgs e)
        {
            string sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                    NSI.RezSrch zS = xNSI.GetNameSPR(NSI.NS_SMEN, new object[] { sT }, "NAME");
                    //02.05.11 !!! e.Cancel = !zS.bFind;
            }
            xDP.sSmena = sT;
            //if (e.Cancel != true)
            //    e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
        }

        // проверка номера документа
        private void tNom_p_Validating(object sender, CancelEventArgs e)
        {
            string sT = ((TextBox)sender).Text.Trim();
            if (sT.Length == 0)
            {
                if (xDP.nNumTypD == AppC.TYPD_PERMGP)
                {
                    //ServClass.ChangeEdArrDet(new Control[] { tKEks_p }, new Control[] { tNom_p }, aEdVvod);
                    tKEks_p.Enabled = false;
                    tNom_p.Enabled = true;
                }
            }
            xDP.sNomDoc = sT;
            //if (e.Cancel != true)
            //    e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
        }

        // проверка штрихкода документа
        private void tBCDoc_Validating(object sender, CancelEventArgs e)
        {
            string 
                sT = ((TextBox)sender).Text.Trim();
            if (sT.Length == 0)
            {
            }
            xDP.sBC_Doc = sT;
        }


        // проверка экспедитора
        private void tKEks_p_Validating(object sender, CancelEventArgs e)
        {
            string sT = ((TextBox)sender).Text.Trim();
            if (sT.Length > 0)
            {
                try
                {
                    int nE = int.Parse(sT);
                    NSI.RezSrch zS = xNSI.GetNameSPR(NSI.NS_EKS, new object[] { nE }, "FIO");
                    tNEks_p.Text = zS.sName;
                    if (zS.bFind == false)
                        e.Cancel = true;
                    else
                    {
                        xDP.nEks = nE;
                        xDP.sEks = zS.sName;
                    }
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
            {
                xDP.nEks = AppC.EMPTY_INT;
                xDP.sEks = "";
                if (xDP.nNumTypD == AppC.TYPD_PERMGP)
                {
                    //ServClass.ChangeEdArrDet(new Control[] { tNom_p }, new Control[] { tKEks_p }, aEdVvod);
                    tNom_p.Enabled = true;
                    tKEks_p.Enabled = false;
                }

            }
            //if (e.Cancel != true)
            //    e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);

        }

        // проверка получателя
        private void tKPol_p_Validating(object sender, CancelEventArgs e)
        {
            NSI.RezSrch zS;
            string sT = ((TextBox)sender).Text.Trim(),
                sN = "";
            if (sT.Length > 0)
            {
                try
                {
                    int nK = int.Parse(sT);

                    if (xDP.nNumTypD == AppC.TYPD_RASHNKLD)
                    {
                        sN = DocPars.OPRName(ref nK);
                    }
                    else
                    {
                        zS = xNSI.GetNameSPR((xDP.nNumTypD == AppC.TYPD_PERMGP) ?
                            NSI.NS_SKLAD : NSI.NS_PP, new object[] { nK }, "NAME");
                        sN = zS.sName;
                    }

                    tNPol_p.Text = sN;
                    tKPol_p.Text = sT;
                    xDP.nPol = nK;
                    xDP.sPol = sN;
                }
                catch
                {
                    e.Cancel = true;
                }
            }
            else
            {
                xDP.nPol = AppC.EMPTY_INT;
                xDP.sPol = "";
            }
            //if (e.Cancel != true)
            //    e.Cancel = !ServClass.TryEditNextFiled((Control)sender, nCurEditCommand, aEdVvod);
        }

        // обработка функций и клавиш на панели
        private bool PPars_KeyDown(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        {
            int nFunc = (int)nF;
            bool ret = true;

            if (nFunc > 0)
            {
                ret = false;
            }
            else
            {
                switch (e.KeyValue)
                {
                    case W32.VK_ESC:
                        EndEditPars(e.KeyValue);
                        break;
                    case W32.VK_UP:
                    case W32.VK_DOWN:
                        aEdVvod.TryNext((e.KeyValue == W32.VK_UP) ? AppC.CC_PREV : AppC.CC_NEXT);
                        break;
                    case W32.VK_ENTER:
                        bSkipChar = true;
                        if (aEdVvod.TryNext(AppC.CC_NEXTOVER) == AppC.RC_CANCELB)
                            //if (bQuitEdPars == true)
                            EndEditPars(e.KeyValue);
                        break;
                    case W32.VK_TAB:
                        aEdVvod.TryNext((e.Shift) ? AppC.CC_PREV : AppC.CC_NEXT);
                        ret = false;
                        break;
                    default:
                        ret = false;
                        break;
                }
            }
            e.Handled |= ret;
            return (ret);
        }











    }
}
