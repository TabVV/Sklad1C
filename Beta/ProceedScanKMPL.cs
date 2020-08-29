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

    public partial class MainF : Form
    {

        // ����������� ���������� �� ��������� ���������� ������ (����������� �� ������� - �����)
        // � ��������������� �� ���������� �� ���
        public class OneEANStat
        {
            public class Sum4Cond
            {
                public NSI.SPECCOND tCond;
                public FRACT fEmk;
            }

            public Dictionary<Sum4Cond, FRACT> dicZVK;
            public Dictionary<Sum4Cond, FRACT> dicTTN;
        }

        // Event delegate and handler
        public delegate void ScanProceededEventHandler(object sender, EventArgs e);
        public event ScanProceededEventHandler AfterAddScan;

        private void OnPoddonReady(object sender, EventArgs e)
        {
            //if (IsEasyEdit())
            //{
            //    int nPos = dgDet.CurrentRowIndex;
            //    if (nPos <= 0)
            //    {
            //        string sTypDoc = "aass";
            //    }
            //    if (dgDet.VisibleRowCount <= 0)
            //        TryNextPoddon();
            //}
            if (xCDoc != null)
            {
                //if (xCDoc.nTypOp == AppC.TYPOP_MOVE)
                //{
                //    if ((xCDoc.xOper != null) && (xCDoc.xOper.bObjOperScanned == false))
                //    {// ������ �������� ����������
                //        xCDoc.xOper.bObjOperScanned = true;
                //    }
                //}
                //if (xCDoc.xDocP.nTypD == AppC.TYPD_BRAK)
                //{
                //    if (bShowTTN && (drDet != null))
                //    {
                //        xDLLAPars = new object[2] { xCDoc.xDocP.nTypD, drDet };
                //        DialogResult xDRslt = CallDllForm(sExeDir + "SGPF-Brak.dll", true);
                //        dgDet.Focus();
                //    }
                //}
            }
        }


        private bool CanProd(ref PSC_Types.ScDat sc)
        {
            bool
                ret = true;
            if (xPars.UseAdr4DocMode)
            {
                if (xCDoc.xDocP.DType.AdrFromNeed)
                {// �����-�������� ����� ?
                    if ((xCDoc.xOper.GetSrc(false).Length > 0) || ((xCDoc.drCurRow["CHKSSCC"] is int) && ((int)xCDoc.drCurRow["CHKSSCC"] > 0)))
                    { }
                    else
                    {
                        ret = false;
                        Srv.ErrorMsg("�� ������ �����!");
                    }
                }

                if (xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.PRIHOD)
                {
                    if (xCDoc.xOper.bObjOperScanned && !xCDoc.xOper.IsFillDst())
                    {
                        DialogResult drQ = MessageBox.Show("�������� ���������(ENT)?\n(ESC) - �������� ����",
                            "��������� �����!",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                        if (drQ != DialogResult.OK)
                            ret = false;
                        else
                            sc.bReWrite = true;
                    }
                }
            }

            return (ret);
        }

        private int ProceedProd(ScanVarRM xSc, ref PSC_Types.ScDat sc, bool bDupScan)
        {
            bool 
                bDopValuesNeed = true;
            int 
                nRet = AppC.RC_CANCEL;

            #region ��������� ����� ���������
            do
            {
                if (!CanProd(ref sc))
                    break;

                if (!AvailKMC4PartInvent(sc.sKMC, true))
                    break;

                xCDoc.bConfScan = (ConfScanOrNot(xCDoc.drCurRow, xPars.ConfScan) > 0) ? true : false;
                if (xCDoc.bConfScan)
                {// �������� ������� ������������� ����� ��� ������ ���������
                    //if ((sc.nRecSrc != (int)NSI.SRCDET.FROMADR) &&
                    //    (sc.nRecSrc != (int)NSI.SRCDET.SSCCT))
                    //    {// ���������� � ������� �� ���������
                    //    if (TestProdBySrv(ref sc) != AppC.RC_OK)
                    //        break;
                    //}

                    // ���������� � ������� ����! ���������
                    if (TestProdBySrv(ref sc) != AppC.RC_OK)
                        break;
                }


                if (xCDoc.drCurRow != null)
                {
                    scCur = sc;
                    //if (!sc.bFindNSI)
                    //    Srv.ErrorMsg(String.Format("��� {0} �� ������!\n�������� ���!", sc.sEAN), true);

                    bDopValuesNeed = true;
                    nRet = AppC.RC_OK;

                    if (scCur.bVes == true)
                    {
                        scCur.fVsego = scCur.fVes;

                        // ������ 05.07.18
                        //if (scCur.nRecSrc != (int)NSI.SRCDET.SSCCT)
                        //{
                        //    //if (!xScan.dicSc.ContainsKey("37") && (scCur.tTyp != AppC.TYP_TARA.TARA_POTREB))
                        //    //{
                        //    //    scCur.tTyp = AppC.TYP_TARA.TARA_POTREB;
                        //    //    scCur.fEmk = scCur.fEmk_s = 0;
                        //    //}
                        //}

                        if ((xCDoc.xDocP.DType.MoveType == AppC.MOVTYPE.MOVEMENT) && (scCur.tTyp == AppC.TYP_TARA.TARA_PODDON))
                        {
                            bDopValuesNeed = false;
                            scCur.nMest = scCur.nMestPal;
                        }
                        if (AppPars.bVesNeedConfirm == false)
                        {// ������������� ����� ���������
                            if ((scCur.tTyp != AppC.TYP_TARA.UNKNOWN) ||
                                ((scCur.tTyp == AppC.TYP_TARA.TARA_POTREB) && (scCur.nParty.Length > 0)))
                            {
                                bDopValuesNeed = false;
                            }
                        }
                        if ((scCur.fVes == fDefVes) && (scCur.fVes > 0))
                        {
                            DialogResult dr = MessageBox.Show("�������� ���� (Enter)?\r\n(ESC) - ���������� ����",
                                String.Format("��� �� ���-{0}!", scCur.fVes), MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                            if (dr == DialogResult.OK)
                                nRet = AppC.RC_CANCEL;
                        }
                        fDefVes = scCur.fVes;
                    }
                    else
                    {
                        if ((scCur.tTyp == AppC.TYP_TARA.TARA_PODDON))
                        {
                            if (AppPars.bVesNeedConfirm == false)
                                bDopValuesNeed = false;
                        }
                        else
                        {
                            nRet = AppC.RC_OK;
                        }
                    }

                    if ((AppC.RC_OK == nRet) &&
                        (AppC.RC_OK == EvalZVKMest(ref scCur, null, 0, 0)))
                    {
                            nRet = Prep4Ed(ref scCur, ref bDopValuesNeed, 0);

                        SetDopFieldsForEnter(false);
                        if (nRet == AppC.RC_OK)
                        {
                            PInfReady();
                            SetDetFields(false);
                            //ShowDopInfKMPL(scCur.nKolM_alr + scCur.nKolM_alrT);

                            //if (bDopValuesNeed == false)
                            if ((bDopValuesNeed == false) && (bEditMode))
                                {// �������������� ���� ����������, �� ���� �� ��� ������?
                                bDopValuesNeed = (VerifyVvod().nRet == AppC.RC_CANCEL) ? true : false;
                            }

                            if ((bDopValuesNeed == true))
                            {// ����� ��������������
                                int nR = IsGeneralEdit(ref scCur);
                                if (nR == AppC.RC_OK)
                                    AddOrChangeDet(AppC.F_ADD_SCAN);
                                else if (nR == AppC.RC_CANCEL)
                                {
                                    //Srv.ErrorMsg("���������������!\r\n��������� � ������� �����!");
                                    ZVKeyDown(AppC.F_PODD, null, ref ehCurrFunc);
                                }
                                else if (nR == AppC.RC_BADTABLE)
                                {
                                    Srv.ErrorMsg("������������ � ������...\r\n��������� ������������");
                                    ChgDetTable(null, "");
                                }
                                else if (nR == AppC.RC_ZVKONLY)
                                {
                                    Srv.ErrorMsg("������ � �������!");
                                }
                            }
                            else
                            {
                                AddDet1(ref scCur);
                                SetDopFieldsForEnter(true);
                            }
                        }
                    }
                }
            } while (false);
            #endregion

            return (nRet);
        }


        
        // ���������� ���� �� ������
        //private int Old_EvalZVKMest(ref PSC_Types.ScDat sc, DataView dvZ, int nCurR, int nMaxR)
        //{
        //    bool bNeedAsk = (IsEasyEdit()) ? false : true;
        //    int nRet = AppC.RC_OK;
        //    string sErr = "";

        //    //if ((bZVKPresent == true) || !bInScanProceed)
        //    //if ((bZVKPresent == true) || (!bInScanProceed && !bEditMode))

        //    sc.nDest = NSI.DESTINPROD.GENCASE;
        //    if (bZVKPresent)
        //        {
        //        nRet = LookAtZVK(ref sc, dvZ, nCurR, nMaxR);
        //        if (nRet != AppC.RC_OK)
        //        {
        //            switch (nRet)
        //            {
        //                case AppC.RC_SAMEVES:
        //                    sErr = String.Format("��� �� ���-{0}!", sc.fVes);
        //                    if (sc.nKrKMC == nOldKrk)
        //                        bNeedAsk = true;
        //                    break;
        //                case AppC.RC_NOEAN:
        //                    sErr = sc.nKrKMC.ToString() + "-��� � ������!";
        //                    if (sc.nKrKMC == nOldKrk)
        //                    {// ���������� ��� �� ���� ���������� ?
        //                        if (bAskKrk == true)
        //                            bNeedAsk = false;
        //                    }
        //                    //if (IsEasyEdit())
        //                    //{
        //                    //}
        //                    break;
        //                case AppC.RC_BADPARTY:
        //                    sErr = String.Format("������ {0} ��� � ������!", sc.nParty);
        //                    bNeedAsk = true;
        //                    break;
        //                case AppC.RC_NOEANEMK:
        //                    sErr = sc.sN + "/" + sc.fEmk.ToString() + "-��� � ������!";
        //                    if ((sc.sKMC == nOldKrk) && (sc.fEmk == nOldKrkEmkNoSuch))
        //                        if (bAskEmk == true)
        //                            bNeedAsk = true;
        //                    break;
        //                case AppC.RC_BADPODD:
        //                    if (!xCDoc.bFreeKMPL)
        //                    {
        //                        sErr = sc.sErr;
        //                        bNeedAsk = true;
        //                    }
        //                    else { bNeedAsk = false; }
        //                    break;
        //                case AppC.RC_UNVISPOD:
        //                    sErr = "������� ������ �� �������!";
        //                    break;
        //                case AppC.RC_NOAUTO:
        //                    sErr = "������ �� ������������!";
        //                    bNeedAsk = true;
        //                    break;
        //                case AppC.RC_ALLREADY:
        //                    sErr = "������ ��� ���������!";
        //                    bNeedAsk = true;
        //                    break;
        //                default:
        //                    sErr = "������������� ������!";
        //                    break;
        //            }

        //            bNeedAsk = (bInScanProceed || bEditMode) ? bNeedAsk : false;
        //            if (bNeedAsk && sc.nRecSrc == (int)NSI.SRCDET.SSCCT)
        //                bNeedAsk = false;

        //            if (bNeedAsk == true)
        //            {
        //                bAskEmk = false;
        //                bAskKrk = false;

        //                DialogResult dr = MessageBox.Show("�������� ���� (Enter)?\n(ESC) - ���������� ����", sErr,
        //                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
        //                if (dr == DialogResult.OK)
        //                    nRet = AppC.RC_CANCEL;
        //                else
        //                {
        //                    sc.nDest = NSI.DESTINPROD.USER;
        //                    if (nRet == AppC.RC_NOEANEMK)
        //                    {
        //                        bAskEmk = true;
        //                        nOldKrk = sc.nKrKMC;
        //                        fOldEmk = sc.fEmk;
        //                    }
        //                    if (nRet == AppC.RC_NOEAN)
        //                    {
        //                        bAskKrk = true;
        //                        nOldKrk = sc.nKrKMC;
        //                    }
        //                    if (nRet == AppC.RC_BADPODD)
        //                        sc.nDest = NSI.DESTINPROD.GENCASE;
        //                    nRet = AppC.RC_OK;

        //                    //if (bInEasyEditWait == true)
        //                    //{
        //                    //    SetFltVyp(false);
        //                    //    ZVKeyDown(0, new KeyEventArgs(Keys.Cancel));
        //                    //}
        //                }
        //            }
        //            else
        //            {// ������� �� �����, ������ ��������� 
        //                if (bInScanProceed || bEditMode)
        //                {// ��������� ������������
        //                    if (xScrDet.CurReg != ScrMode.SCRMODES.FULLMAX)
        //                        nRet = AppC.RC_OK;
        //                    if (sErr != "")
        //                        Srv.ErrorMsg(sErr, true);
        //                    if ((sc.drTotKey != null) || (sc.drTotKeyE != null))
        //                        sc.nDest = NSI.DESTINPROD.TOTALZ;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            bAskEmk = false;
        //            bAskKrk = false;
        //            if ((sc.drTotKey != null) || (sc.drTotKeyE != null))
        //                sc.nDest = NSI.DESTINPROD.TOTALZ;
        //        }
        //    }


        //    return (nRet);
        //}


        private bool IsEasyEdit()
        {
            return (
            ((xScrDet.CurReg == ScrMode.SCRMODES.FULLMAX) &&
                (dgDet.DataSource == xNSI.DT[NSI.BD_DIND].dt)) ? true : false
                );
        }



        private bool WhatTotKeyF_Emk(DataRow dr, ref PSC_Types.ScDat sc, FRACT fCurEmk)
        {
            bool bSet = false;
            if (fCurEmk == 0)
            {
                if (sc.drTotKeyE == null)
                {
                    sc.drTotKeyE = dr;
                    bSet = true;
                }
            }
            else
            {
                if (sc.drTotKey == null)
                {
                    sc.drTotKey = dr;
                    bSet = true;
                }
            }
            return (bSet);
        }

        private bool WhatPrtKeyF_Emk(DataRow dr, ref PSC_Types.ScDat sc, FRACT fCurEmk)
        {
            bool bSet = false;
            if (fCurEmk == 0)
            {
                if (sc.drPartKeyE == null)
                {
                    sc.drPartKeyE = dr;
                    bSet = true;
                }
            }
            else
            {
                if (sc.drPartKey == null)
                {
                    sc.drPartKey = dr;
                    bSet = true;
                }
            }
            return (bSet);
        }


        private int FindSSCCInZVK(ScanVarRM xSc, ref PSC_Types.ScDat sc)
        {
            int nRet = AppC.RC_NOSSCC;

            sc.ZeroZEvals();
            if (bZVKPresent)
            {
            }
            return (nRet);
        }




    }
}
