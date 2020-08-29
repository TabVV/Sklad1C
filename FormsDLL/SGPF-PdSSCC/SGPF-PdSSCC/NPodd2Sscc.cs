using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using ScannerAll;
using PDA.Service;
using PDA.OS;
using PDA.BarCode;
using SkladRM;

using FRACT = System.Decimal;

namespace SGPF_PdSSCC
{
    public partial class NPodd2Sscc : Form
    {
        private MainF 
            xMF;
        private NSI 
            xNSI;

        private BarcodeScanner.BarcodeScanEventHandler ehOldScan;

        private bool 
            bDocIsKMPL,
            bMaySetSSCC = false,        // проставка SSCC подготовлена
            bSkipKey = false,           // не обрабатывать введенный символ
            bEditMode = false;

        private AppC.EditListC 
            aEd;
       
        ScanVarRM 
            xScan = null;
        
        private int nNomPodd = 0;
        DataView dv;

        public NPodd2Sscc()
        {
            InitializeComponent();
            // центровка формы
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            this.Location = new Point((screen.Width - this.Width) / 2,
                (screen.Height - this.Height) / 2);
        }

        private bool AfterConstruct(MainF x)
        {
            int
                nTotM_ZVK = 0,
                nTotM_TTN = 0;
            string
                sRf;
            DataView
                dvM;

            xMF = x;
            xNSI = xMF.xNSI;

            bDocIsKMPL = ((int)x.xCDoc.drCurRow["TYPOP"] == AppC.TYPOP_KMPL) ? true : false;
            //if (bDocIsKMPL)
            //{
            //    bWillMark = xMF.IsZkzReady(true);
            //    if (!bWillMark)
            //        return (false);
            //}

            ehOldScan = new BarcodeScanner.BarcodeScanEventHandler(OnScanPL);
            xMF.xBCScanner.BarcodeScan += ehOldScan;

            //xFuncs = x.xFuncs;
            nNomPodd = x.xCDoc.xNPs.Current;

            //tNomPoddon.Text = (xMF.xCDoc.nCurNomPodd + 1).ToString();
            tSSCC.Text = "";

            if (bDocIsKMPL)
            {
                this.Text = String.Format("Текущий поддон {0}", x.xCDoc.xNPs.Current);
                tNomPoddon.Text = xMF.xCDoc.xNPs.TryNext(false, true).ToString();
                lNomsAvail.Text = "(" + xMF.xCDoc.xNPs.RangeN() + ")";
                            
                sRf = xMF.xCDoc.DefDetFilter() + String.Format(" AND (NPODDZ={0})", x.xCDoc.xNPs.Current);
            }
            else
            {
                this.Text = String.Format("Текущий поддон {0}", x.xCDoc.xNPs.Current + 1);
                lNomPodd.Text = "";
                tNomPoddon.Text = "";
                lNomsAvail.Text = "";
            
                sRf = xMF.xCDoc.DefDetFilter();
                if ((int)xMF.xCDoc.drCurRow["CHKSSCC"] <= 0)
                    sRf += " AND (LEN(SSCC)<>20)";
            }

            dvM = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "", DataViewRowState.CurrentRows);
            foreach(DataRowView drv in dvM)
            {
                nTotM_TTN += (int)drv.Row["KOLM"];
            }
            if (x.bZVKPresent)
            {
                if (!bDocIsKMPL)
                    sRf = xMF.xCDoc.DefDetFilter();
                dvM = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "", DataViewRowState.CurrentRows);
                foreach (DataRowView drv in dvM)
                {
                    nTotM_ZVK += (int)drv.Row["KOLM"];
                }
            }

            lMestOnPodd.Text = String.Format("Мест = {0} (заказ = {1})", nTotM_TTN, nTotM_ZVK);

                return (true);
        }

        // Обработка клавиш
        private void NPodd2Sscc_KeyDown(object sender, KeyEventArgs e)
        {
            int nFunc = 0;
            bool ret = true;

            bSkipKey = false;
            nFunc = xMF.xFuncs.TryGetFunc(e);

            if (bEditMode == false)
            {//в режиме просмотра
                switch (e.KeyValue)
                {
                    case W32.VK_ESC:
                        this.DialogResult = DialogResult.Cancel;
                        break;
                    default:
                        ret = false;
                        break;
                }
            }
            else
            {// для режима редактирования
                switch (e.KeyValue)
                {
                    case W32.VK_UP:
                        aEd.TryNext(AppC.CC_PREV);
                        break;
                    case W32.VK_DOWN:
                        aEd.TryNext(AppC.CC_NEXT);
                        break;
                    case W32.VK_ENTER:
                        if (aEd.TryNext(AppC.CC_NEXTOVER) == AppC.RC_CANCELB)
                            EndEditB(true);
                        break;
                    case W32.VK_ESC:
                        EndEditB(false);
                        break;
                    default:
                        ret = false;
                        break;
                }
            }
            e.Handled = ret;
            bSkipKey = ret;
        }

        private void NPodd2Sscc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (bSkipKey == true)
            {
                bSkipKey = false;
                e.Handled = true;
            }
        }

        // Начало редактирования
        public void BeginEditB()
        {
            aEd = new AppC.EditListC(new AppC.VerifyEditFields(VerifyB));
            //if (bDocIsKMPL)
            //    aEd.AddC(tNomPoddon);
            //else
            //    aEd.AddC(tSSCC);

            aEd.AddC(tSSCC);

            bEditMode = true;
            aEd.SetCur(aEd[0]);
        }

        // Корректность введенного
        private AppC.VerRet VerifyB()
        {
            string
                sSavedNom = tNomPoddon.Text;
            int
                n = 0;
            AppC.VerRet 
                v;
            DialogResult dRez;

            v.nRet = AppC.RC_CANCEL;
            try
            {
                //if (!bDocIsKMPL)
                //{
                //    if (tSSCC.Text.Length == 20)
                //        v.nRet = AppC.RC_OK;
                //    v.cWhereFocus = null;
                //    return (v);
                //}

                if (tSSCC.Text.Length == 20)
                {
                    if (!bDocIsKMPL)
                    {
                        v.nRet = AppC.RC_OK;
                        n = 1;
                    }
                    else
                    {


                        n = int.Parse(tNomPoddon.Text);
                        if (!xMF.xCDoc.xNPs.ContainsKey(n))
                        {
                            if (n > 0)
                            {
                                if (!xMF.xCDoc.bFreeKMPL)
                                {
                                    dRez = MessageBox.Show("Отменить (Enter)?\n(ESC)-добавить №",
                                    String.Format("Поддона №{0} нет!", n),
                                    MessageBoxButtons.OKCancel,
                                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                                    n = (dRez != DialogResult.OK) ? n : 0;
                                }
                                if (n > 0)
                                {
                                    xMF.xCDoc.xNPs.Add(n, new PoddonInfo());
                                    v.nRet = AppC.RC_OK;
                                }
                            }
                        }
                        else
                            v.nRet = AppC.RC_OK;
                    }
                }
                else
                {
                    n = 1;
                    Srv.ErrorMsg("Нет SSCC!");
                }
            }
            catch
            {
                n = 0;
            }

            if (n == 0)
            {
                Srv.ErrorMsg("Нет такого номера!");
            }

            v.cWhereFocus = null;
            tNomPoddon.Text = sSavedNom;
            return (v);
        }

        // Завершение редактирования
        public void EndEditB(bool bSave)
        {
            string
                sSSCC = "";

            bEditMode = false;
            this.Tag = new object[] { Srv.ExchangeContext.xEA, sSSCC };

            if (bSave)
            {// все нормально, данные корректны
                //xMF.xCDoc.sSSCC = tSSCC.Text;
                //xMF.xCDoc.xOper.SSCC = tSSCC.Text;
                sSSCC = tSSCC.Text.Trim();
                if (bDocIsKMPL)
                {
                    //xMF.xCDoc.xNPs.Current = int.Parse(tNomPoddon.Text);
                    //xMF.xCDoc.nCurNomPodd = xMF.xCDoc.xNPs.Current;
                    if (bMaySetSSCC == true)
                    {
                        //xMF.SetSSCCForPoddon(sSSCC, dv, nNomPodd);
                    }
                    //aEd.EditIsOver();
                }
                else
                {
                    // !!!
                    //xMF.xCDoc.sSSCC = "00248102680000014790";
                }
                ((object[])this.Tag)[1] = sSSCC;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                //xMF.xCDoc.sSSCC = "";
                //xMF.xCDoc.xOper.SSCC = "";
                this.DialogResult = DialogResult.Cancel;
            }
        }

        private void SelAllTextF(object sender, EventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void NPodd2Sscc_Closing(object sender, CancelEventArgs e)
        {
            if (ehOldScan != null)
                xMF.xBCScanner.BarcodeScan -= ehOldScan;
        }

        //private string SP_GLN = "4810268";
        private void OnScanPL(object sender, BarcodeScannerEventArgs e)
        {
            if (e.nID != BCId.NoData)
            {
                xScan = new ScanVarRM(e, xNSI.DT["NS_AI"].dt);
                //if ((xScan.bcFlags == ScanVarGP.BCTyp.SP_SSCC_EXT) ||
                //    (xScan.bcFlags == ScanVarGP.BCTyp.SP_SSCC_INT))
                if ((xScan.bcFlags & ScanVarRM.BCTyp.SP_SSCC) > 0)
                {
                    tSSCC.Text = xScan.Dat;
                    if (bDocIsKMPL)
                    {// для комплектации
                        if (xMF.StoreSSCC(xScan, nNomPodd, false, out dv) == AppC.RC_OKB)
                        {// предложенный SSCC может быть проставлен
                            bMaySetSSCC = true;
                        }
                        else
                            bMaySetSSCC = false;
                    }
                    else
                    {// для всего остального
                        tSSCC.Text = xScan.Dat;
                    }
                }

            }
        }

        private void NPodd2Sscc_Activated(object sender, EventArgs e)
        {
            if (this.Tag != null)
            {
                AfterConstruct((MainF)this.Tag);
                this.Tag = null;
                BeginEditB();
            }
        }







    }
}