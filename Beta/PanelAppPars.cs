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

        // ������� �� ������� ������
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

        // ��������� ������� � ������ �� ������
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
                    case AppC.F_UPLD_DOC:               // ���������� ����������
                        nR = AppPars.SavePars(xPars);
                        if (AppC.RC_OK == nR)
                        {
                            Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
                            MessageBox.Show("��������� ���������", "����������");
                        }
                        else
                        {
                            Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                            MessageBox.Show("������ ����������!", "����������");
                        }
                        ret = true;
                        break;
                    case AppC.F_CHGSCR:
                        break;
                }
            }
            else
            {// ��� ������ �������
                switch (e.KeyValue)
                {
                    case W32.VK_ESC:
                        // ����� ������� � 
                        // ������� �� ����������
                        xC.Parent.SelectNextControl(xC, false, true, false, true);
                        Back2Main();
                        ret = true;
                        break;
                    case W32.VK_TAB:
                        // ��������� �������
                        int nN = (tcPars.SelectedIndex == tcPars.TabPages.Count - 1)? 0 : tcPars.SelectedIndex + 1;
                        tcPars.SelectedIndex = nN;
                        ret = true;
                        break;
                    case W32.VK_ENTER:
                        // ����� ������� � 
                        // ������� �� ���������
                        xC.Parent.SelectNextControl(xC, true, true, false, true);
                        ret = true;
                        break;
                    case W32.VK_F2:               // ���������� ���������� � ���������� PC
                        nR = AppPars.SavePars(xPars);
                        if (AppC.RC_OK == nR)
                        {
                            Srv.PlayMelody(W32.MB_2PROBK_QUESTION);
                            MessageBox.Show("��������� ���������", "����������");
                        }
                        else
                        {
                            Srv.PlayMelody(W32.MB_3GONG_EXCLAM);
                            MessageBox.Show("������ ����������!", "����������");
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

        // ������� ������ � Shift
        //private void cbShiftOnly_Validating(object sender, CancelEventArgs e)
        //{
        //    AppPars.bArrowsWithShift = cbShiftOnly.Checked;
        //}

        // ����� �������� ���������� ��� ����������� � ����
        //private void cbDocCtrl_CheckStateChanged(object sender, EventArgs e)
        //{
        //    xPars.parDocControl = cbDocCtrl.Checked;
        //    if (xPars.parDocControl == true)
        //        tDocCtrlState.Text = "�";
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

        // �������� ��� ���������
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

        // �������� ��� ���������
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

        // �������� ���� ������� �� ����������� ���������
        private void cbHidUpl_CheckStateChanged(object sender, EventArgs e)
        {
            if (xNSI != null)
                FiltForDocs(((CheckBox)sender).Checked, xNSI.DT[NSI.BD_DOCOUT]);
        }

        // �������� ���� ������������� ���� ������������ � �������� ���� ��������
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

            // ��������� ����� ��� ����� ���������
            bi = new Binding("Checked", xPars, "bConfMest");
            chbConfMest.DataBindings.Add(bi);
            bi = new Binding("Checked", xPars, "bMaxKolEQPodd");
            chbChkMaxPoddon.DataBindings.Add(bi);
            bi = new Binding("Text", xPars, "MaxVesVar");
            tVesVar.DataBindings.Add(bi);
            bi = new Binding("Checked", xPars, "bStart1stPoddon");
            chbStartQ.DataBindings.Add(bi);


            // �� �����
            bi = new Binding("Checked", xPars, "bAfterScan");
            cbAfterScan.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bEdit");
            cbAvEdit.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "bManual");
            cbAvVvod.DataBindings.Add(bi);

            bi = new Binding("Checked", xPars, "WarnNewScan");
            cbWarnNewScan.DataBindings.Add(bi);

            // ��� ����� ���������
            // ���������� ���������� �� ������
            bi = new Binding("Checked", xPars, "bKolFromZvk");
            cbKolFromZ.DataBindings.Add(bi);

            // �������� ����� ���������
            //bi = new Binding("Checked", xPars, "bTestBeforeUpload");
            //cbDocCtrl.DataBindings.Add(bi);

            // ����������� ������� ���������
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

            // �������� ��������� ������ �� ������ ��� ���������������
            bi = new Binding("Checked", xPars, "UseAdr4DocMode");
            cbUseAdr4Doc.DataBindings.Add(bi);

            // ����������� ������ ����� ������������
            bi = new Binding("Checked", xPars, "ConfScan");
            cbConfScan.DataBindings.Add(bi);


            //bi = new Binding("Checked", xPars, "OpAutoUpl");
            //cbAutoUpLoadOper.DataBindings.Add(bi);

            //bi = new Binding("Checked", xPars, "OpChkAdr");
            //cbChkOpr.DataBindings.Add(bi);

            // ������ ��������
            //bi = new Binding("DataSource", xPars, "bHideUploaded");
            //cmbHostG.DataBindings.Add(bi);


        }


    }
}
