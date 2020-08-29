using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;

using PDA.OS;
using PDA.Service;


namespace SkladRM
{
    public partial class MainF : Form
    {

        //private Dictionary<int, string> dicMTypes;

        // переход на вкладку Сервис
        private void EnterInPars()
        {
            if (xSm.urCur > Smena.USERRIGHTS.USER_KLAD)
            {
                tcPars.Enabled = true;
                if (cmbField.SelectedIndex < 0)
                    cmbField.SelectedIndex = 0;
                if (cmbMType.SelectedIndex < 0)
                    cmbMType.SelectedIndex = 0;
                if (cmbDocType.SelectedIndex < 0)
                    cmbDocType.SelectedIndex = 0;
                SetEditMode(true);
                tSrvParServer.Focus();
            }
        }

        // обработка функций и клавиш на панели
        private bool AppPars_KeyDown(int nFunc, KeyEventArgs e)
        {
            bool 
                ret = false;
            int 
                nR;
            Control 
                xC = Srv.GetPageControl(tpParPaths, 1);

            if (nFunc > 0)
            {
                switch (nFunc)
                {
                    case AppC.F_UPLD_DOC:               // сохранение параметров
                        nR = AppPars.SavePars(xPars);
                        if (AppC.RC_OK == nR)
                        {
                            Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
                            MessageBox.Show("Параметры сохранены", "Сохранение");
                        }
                        else
                        {
                            Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                            MessageBox.Show("Ошибка сохранения!", "Сохранение");
                        }
                        ret = true;
                        break;
                    case AppC.F_CHGSCR:
                        break;
                }
            }
            else
            {// это просто клавиша
                switch (e.KeyValue)
                {
                    case W32.VK_ESC:
                        // найти текущий и 
                        // перейти на предыдущий
                        xC.Parent.SelectNextControl(xC, false, true, false, true);
                        Back2Main();
                        ret = true;
                        break;
                    case W32.VK_TAB:
                        // следующая вкладка
                        int nN = (tcPars.SelectedIndex == tcPars.TabPages.Count - 1)? 0 : tcPars.SelectedIndex + 1;
                        tcPars.SelectedIndex = nN;
                        ret = true;
                        break;
                    case W32.VK_ENTER:
                        // найти текущий и 
                        // перейти на следующий
                        xC.Parent.SelectNextControl(xC, true, true, false, true);
                        ret = true;
                        break;
                    case W32.VK_F2:               // сохранение параметров с клавиатуры PC
                        nR = AppPars.SavePars(xPars);
                        if (AppC.RC_OK == nR)
                        {
                            Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
                            MessageBox.Show("Параметры сохранены", "Сохранение");
                        }
                        else
                        {
                            Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                            MessageBox.Show("Ошибка сохранения!", "Сохранение");
                        }
                        ret = true;
                        break;
                }
            }
            e.Handled |= ret;
            return (ret);
        }




        //private void SetParAppFields()
        //{
        //    //tSrvAppPath.Text = xPars.sAppStore;
        //    //tSrvParServer.Text = xPars.sHostSrv;
        //    //tSrvParServPort.Text = xPars.nSrvPort.ToString();
        //    //tSrvParServPortM.Text = xPars.nSrvPortM.ToString();
        //    cbShiftOnly.Checked = AppPars.bArrowsWithShift;
        //    //cbWaitSock.Checked = xPars.bWaitSock;
        //}

        private void cbConfVes_Validating(object sender, CancelEventArgs e)
        {
            AppPars.bVesNeedConfirm = chbConfMest.Checked;
        }

        // стрелки только с Shift
        //private void cbShiftOnly_Validating(object sender, CancelEventArgs e)
        //{
        //    AppPars.bArrowsWithShift = cbShiftOnly.Checked;
        //}

        // вызов контроля документов при перемещении в грид
        //private void cbDocCtrl_CheckStateChanged(object sender, EventArgs e)
        //{
        //    xPars.parDocControl = cbDocCtrl.Checked;
        //    if (xPars.parDocControl == true)
        //        tDocCtrlState.Text = "К";
        //    else
        //        tDocCtrlState.Text = "";
        //}



        private void cmbField_SelectedIndexChanged(object sender, EventArgs e)
        {
            //xPars.CurField = ((ComboBox)sender).SelectedIndex;
            int nI = ((ComboBox)sender).SelectedIndex;
            if (nI >= 0)
            {
                xPars.CurField = nI;
                cbAfterScan.DataBindings[0].ReadValue();
                cbAvEdit.DataBindings[0].ReadValue();
                cbAvVvod.DataBindings[0].ReadValue();
            }
        }

        // сменился тип продукции
        private void cmbMType_SelectedIndexChanged(object sender, EventArgs e)
        {
            int nI = ((ComboBox)sender).SelectedIndex;
            if (nI >= 0)
            {
                xPars.CurVesType = nI;
                chbConfMest.DataBindings[0].ReadValue();
                chbChkMaxPoddon.DataBindings[0].ReadValue();
                tVesVar.DataBindings[0].ReadValue();
                chbStartQ.DataBindings[0].ReadValue();

                cbAfterScan.DataBindings[0].ReadValue();
                cbAvEdit.DataBindings[0].ReadValue();
                cbAvVvod.DataBindings[0].ReadValue();
            }
        }

        // сменился тип документа
        private void cmbDocType_SelectedIndexChanged(object sender, EventArgs e)
        {
            //xPars.CurDocType = ((ComboBox)sender).SelectedIndex;
            int nI = ((ComboBox)sender).SelectedIndex;
            if (nI >= 0)
            {
                xPars.CurDocType = nI;
                try
                {
                    cbKolFromZ.DataBindings[0].ReadValue();
                    cbSumVes.DataBindings[0].ReadValue();
                    //cbDocCtrl.DataBindings[0].ReadValue();
                }
                catch
                {
                }
            }
        }

        // сменился флаг фильтра на выгруженные документы
        private void cbHidUpl_CheckStateChanged(object sender, EventArgs e)
        {
            if (xNSI != null)
                FiltForDocs(((CheckBox)sender).Checked, xNSI.DT[NSI.BD_DOCOUT]);
        }

        // сменился флаг использования даты изготовления в качестве даты контроля
        private void chDateProd_CheckStateChanged(object sender, EventArgs e)
        {
            xPars.UseDTGodn = !((CheckBox)sender).Checked;
        }


        private void SetBindAppPars()
        {
            Binding bi;

            bi = new Binding("Text", xPars, "sAppStore");
            tSrvAppPath.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "sNSIPath");
            tNsiPath.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "sDataPath");
            tDataPath.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "sHostSrv");
            tSrvParServer.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "nSrvPort");
            tSrvParServPort.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "nSrvPortM");
            tSrvParServPortM.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "NTPSrv");
            tNTPSrv.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "NomTerm");
            tNomTSD.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bWaitSock");
            cbWaitSock.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bAutoSave");
            cbAutoSave.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bUseSrvG");
            chUseSrvG.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bArrowsWithShift");
            cbShiftOnly.DataBindings.Add(bi);
             

            bi = new Binding("Checked", xPars, "PKeyIsGUID");
            chPKey.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "UseDTProizv");
            chDateProd.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "UseList4ManyEAN");
            chUseLst.DataBindings.Add(bi);

            // параметры ввода для типов продукции
            bi = new Binding("Checked", xPars, "bConfMest");
            chbConfMest.DataBindings.Add(bi);
            bi = new Binding("Checked", xPars, "bMaxKolEQPodd");
            chbChkMaxPoddon.DataBindings.Add(bi);
            bi = new Binding("Text", xPars, "MaxVesVar");
            tVesVar.DataBindings.Add(bi);
            bi = new Binding("Checked", xPars, "bStart1stPoddon");
            chbStartQ.DataBindings.Add(bi);


            // по полям
            bi = new Binding("Checked", xPars, "bAfterScan");
            cbAfterScan.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bEdit");
            cbAvEdit.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bManual");
            cbAvVvod.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "WarnNewScan");
            cbWarnNewScan.DataBindings.Add(bi);

            // для типов документа
            // предлагать количество из заявки
            bi = new Binding("Checked", xPars, "bKolFromZvk");
            cbKolFromZ.DataBindings.Add(bi);

            // контроль перед выгрузкой
            //bi = new Binding("Checked", xPars, "bTestBeforeUpload");
            //cbDocCtrl.DataBindings.Add(bi);

            // суммировать весовую продукцию
            bi = new Binding("Checked", xPars, "bSumVesProd");
            cbSumVes.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "Days2Save");
            tDays2Save.DataBindings.Add(bi);

            bi = new Binding("Text", xPars, "ReLogon");
            tReLogon.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bHideUploaded");
            cbHidUpl.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "OpAutoUpl");
            cbAutoUpLoadOper.DataBindings.Add(bi);

            // отправка детальной строки на сервер для документального
            bi = new Binding("Checked", xPars, "UseAdr4DocMode");
            cbUseAdr4Doc.DataBindings.Add(bi);

            // запрашивать сервер после сканирования
            bi = new Binding("Checked", xPars, "ConfScan");
            cbConfScan.DataBindings.Add(bi);


            //bi = new Binding("Checked", xPars, "OpAutoUpl");
            //cbAutoUpLoadOper.DataBindings.Add(bi);

            //bi = new Binding("Checked", xPars, "OpChkAdr");
            //cbChkOpr.DataBindings.Add(bi);

            // Группы серверов
            //bi = new Binding("DataSource", xPars, "bHideUploaded");
            //cmbHostG.DataBindings.Add(bi);


        }


    }
}
