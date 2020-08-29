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


        // ���������� ���� �� ������
        private int EvalZVKMest(ref PSC_Types.ScDat sc, DataView dvZ, int nCurR, int nMaxR)
        {
            bool
                bNeedAsk = (IsEasyEdit()) ? false : true;
            int
                nRet = AppC.RC_OK;
            string
                sDopMsg = "",
                sErr = "";

            sc.nDest = NSI.DESTINPROD.GENCASE;
            if (bZVKPresent)
            {
                nRet = LookAtZVK(ref sc, dvZ, nCurR, nMaxR);
                if (nRet != AppC.RC_OK)
                {
                    switch (nRet)
                    {
                        case AppC.RC_SAMEVES:
                            sErr = String.Format("��� �� ���-{0}!", sc.fVes);
                            if (sc.sKMC == sOldKMC)
                                bNeedAsk = true;
                            break;
                        case AppC.RC_NOEAN:
                            //sErr = sc.nKrKMC.ToString() + "-��� � ������!";
                            sErr = sc.sN + "-��� � ������!";
                            if (sc.sKMC == sOldKMC)
                            {// ���������� ��� �� ���� ���������� ?
                                if (bAskKrk == true)
                                    bNeedAsk = false;
                            }
                            //if (IsEasyEdit())
                            //{
                            //}
                            break;
                        case AppC.RC_BADDVR:
                        case AppC.RC_BADDTG:
                        case AppC.RC_BADDATE:
                            sErr = "������� ����!";
                            sDopMsg = sc.sErr;
                            bNeedAsk = true;
                            break;
                        case AppC.RC_BADPARTY:
                            sErr = String.Format("������ {0} ��� � ������!", sc.nParty);
                            bNeedAsk = true;
                            break;
                        case AppC.RC_NOEANEMK:
                            sErr = sc.sN + "/" + sc.fEmk.ToString() + "-��� � ������!";
                            if ((sc.sKMC == sOldKMC) && (sc.fEmk == nOldKrkEmkNoSuch))
                                if (bAskEmk == true)
                                    bNeedAsk = true;
                            break;
                        case AppC.RC_BADPODD:
                            if (!xCDoc.bFreeKMPL)
                            {
                                sErr = sc.sErr;
                                bNeedAsk = true;
                            }
                            else { bNeedAsk = false; }
                            break;
                        case AppC.RC_UNVISPOD:
                            sErr = "������� ������ �� �������!";
                            break;
                        case AppC.RC_NOAUTO:
                            sErr = "������ �� ������������!";
                            bNeedAsk = true;
                            break;
                        case AppC.RC_ALREADY:
                            sErr = "������ ��� ���������!";
                            bNeedAsk = true;
                            break;
                        default:
                            sErr = "������������� ������!";
                            break;
                    }

                    bNeedAsk = (bInScanProceed || bEditMode) ? bNeedAsk : false;
                    if (bNeedAsk && ((sc.nRecSrc == (int)NSI.SRCDET.SSCCT) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR)))
                        bNeedAsk = false;

                    if (bNeedAsk == true)
                    {
                        bAskEmk = false;
                        bAskKrk = false;

                        DialogResult
                            dr = MessageBox.Show(sErr + ((sDopMsg.Length > 0) ? "\n" + sDopMsg : "") + "\n\n�������� ���� (Enter)?\n(ESC) - ���������� ����", 
                            sErr,
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                        if (dr == DialogResult.OK)
                            nRet = AppC.RC_CANCEL;
                        else
                        {
                            sc.nDest = NSI.DESTINPROD.USER;
                            if (nRet == AppC.RC_NOEANEMK)
                            {
                                bAskEmk = true;
                                sOldKMC = sc.sKMC;
                                fOldEmk = sc.fEmk;
                            }
                            if (nRet == AppC.RC_NOEAN)
                            {
                                bAskKrk = true;
                                sOldKMC = sc.sKMC;
                            }
                            if (nRet == AppC.RC_BADPODD)
                                sc.nDest = NSI.DESTINPROD.GENCASE;
                            nRet = AppC.RC_OK;

                            //if (bInEasyEditWait == true)
                            //{
                            //    SetFltVyp(false);
                            //    ZVKeyDown(0, new KeyEventArgs(Keys.Cancel));
                            //}
                        }
                    }
                    else
                    {// ������� �� �����, ������ ��������� 
                        if (bInScanProceed || bEditMode)
                        {// ��������� ������������
                            if (xScrDet.CurReg != ScrMode.SCRMODES.FULLMAX)
                                nRet = AppC.RC_OK;
                            if (sErr != "")
                                Srv.ErrorMsg(sErr, true);
                            if ((sc.drTotKey != null) || (sc.drTotKeyE != null))
                                sc.nDest = NSI.DESTINPROD.TOTALZ;
                        }
                    }
                }
                else
                {
                    bAskEmk = false;
                    bAskKrk = false;
                    if ((sc.drTotKey != null) || (sc.drTotKeyE != null))
                        sc.nDest = NSI.DESTINPROD.TOTALZ;
                }
            }

            return (nRet);
        }

        // ������� � ������
        // ��� ��������:
        // RC_NOEAN - ���� ��� � ������, ������� ������ �� �����������
        // RC_NOEANEMK - ��������� ������ ���� ��� � ������, ���� � ����� �������� ����
        //               ��������� nKolM_zvk - ����� �����-�� ����
        // RC_OK - ���� ��� �������� (fKolE_zvk != 0, ����� �������� drPartKeyE != null drTotKeyE != null) 
        //         ��� ����� (nKolM_zvk != 0, ����� �������� drPartKey != null drTotKey != null)

        private int LookAtZVK(ref PSC_Types.ScDat sc, DataView dv, int i, int nMaxR)
        {
            bool
                bEvalInPall;
            DataRow
                dr = null;
            int
                nRFind = AppC.RC_ALREADY,
                nRet = AppC.RC_OK,
                nM = 0,
                nMestEmk = 0,
                nMest = 0,
                nState;
            string
                nParty = "";

            FRACT fCurEmk;

            sc.ZeroZEvals();
            if ((xCDoc.xNPs.Current > 0) && ((xCDoc.xDocP.TypOper == AppC.TYPOP_KMPL) || (xCDoc.xDocP.TypOper == AppC.TYPOP_OTGR)))
                bEvalInPall = true;
            else
                bEvalInPall = false;

            // ������ - SYSN + KMC
            if (bInScanProceed || bEditMode)
            {// ����� ��� ����� ������ ����� ������������
                nMaxR = 0;
                if (bEvalInPall)
                {// �� �������� �������
                    sc.sFilt4View = FilterKompl(xCDoc.nId, sc.sKMC, true);
                    dv = new DataView(xNSI.DT[NSI.BD_DIND].dt,
                        sc.sFilt4View, "EMK, DTG, NP DESC", DataViewRowState.CurrentRows);
                    nMaxR = dv.Count;
                    if (nMaxR > 0)
                    {// ���� ���� ���������� - �������� � ��������, ����� - ���� ��������
                        i = 0;
                        for (int j = 0; j < nMaxR; j++)
                        {
                            if ((int)dv[j].Row["READYZ"] != (int)NSI.READINESS.FULL_READY)
                            {
                                i = nMaxR;
                                break;
                            }
                        }
                        nMaxR = i;
                    }
                }
                if (nMaxR == 0)
                {
                    sc.sFilt4View = FilterKompl(xCDoc.nId, sc.sKMC, false);
                    dv = new DataView(xNSI.DT[NSI.BD_DIND].dt,
                        sc.sFilt4View, "EMK, DTG, NP DESC", DataViewRowState.CurrentRows);
                    if (bEvalInPall)
                    {// �� �������� ������� ������ �� �������
                        if (dv.Count > 0)
                        {// � ��� ��� ������ �������
                            if (xSm.FilterTTN == NSI.FILTRDET.NPODD)
                            {// ������ ������� �� �������� �� ������
                                return (AppC.RC_UNVISPOD);
                            }
                            else
                            {// ������ ������� �� ������, �� �������� ����
                                nRet = AppC.RC_BADPODD;
                            }
                        }
                        else
                        {// �� ������ �������� ����� ������ ���
                            return (AppC.RC_NOEAN);
                        }
                    }
                    nMaxR = dv.Count;
                }
                i = 0;
            }
            else
                sc.sFilt4View = FilterKompl(xCDoc.nId, sc.sKMC, false);

            while ((i < nMaxR) && ((string)dv[i].Row["KMC"] == sc.sKMC))
            {
                dr = dv[i].Row;
                fCurEmk = (FRACT)dr["EMK"];
                nParty = (string)dr["NP"];
                nState = (int)dr["READYZ"];

                if (fCurEmk == 0)
                {// ������� �� ������� (�������� ��������� ������� ���������)
                    sc.fKolE_zvk += (FRACT)dr["KOLE"];
                }
                else
                {// ������� ������������ � ������, ��������� �����
                    nM = (int)dr["KOLM"];
                    nMest += nM;
                    if (fCurEmk == sc.fEmk)
                    {// � ����-������ ������� ����� �� � ���������
                        nMestEmk += nM;
                    }
                }

                // ���-�� �� ������ ����������� ���� �������������?
                nRFind = FindRowsInZVK(dr, ref sc, nParty, fCurEmk, nRet, bEvalInPall);
                if (nRFind == AppC.RC_OK)
                    sc.lstAvailInZVK.Add(dr);
                else
                {
                    //if ((nRFind == AppC.RC_ALLREADY) && (fCurEmk == sc.fEmk) && (nParty == sc.nParty) )
                    //    break;
                }

                i++;
            } // �������� ����


            if (nMaxR > 0)
            {// �����-�� ������ ���-���� ����
                if (sc.fEmk == 0)
                {// ������� �� ��������� ����-������ ���������� �� �������
                    sc.nKolM_zvk = nMest;
                }
                else
                {
                    sc.nKolM_zvk = nMestEmk;
                }


                if (sc.lstAvailInZVK.Count > 0)
                {
                    sc.nCurAvail = 0;
                    sc.nKolM_zvk = 0;
                    foreach (DataRow drl in sc.lstAvailInZVK)
                        sc.nKolM_zvk += (int)drl["KOLM"];
                    nRet = AppC.RC_OK;
                }
                else
                {// ������ ���������� �������� - ������ �� �����
                    nRet = nRFind;
                }



                if (nRet == AppC.RC_OK)
                {// ���� ��� ��� � �������
                    if ((nMest != 0) && (nMestEmk == 0) && (sc.fEmk != 0))
                    {// 
                        nRet = AppC.RC_NOEANEMK;
                    }

                }
            }
            else
                nRet = AppC.RC_NOEAN;

            return (nRet);
        }

        // ������ � �������� ��� ��������� �� ������ � ���
        // ��� ����������� ������� � ��� �������������
        private string FilterKompl(int nSys, string sKMC, bool bUsePoddon)
        {
            int 
                nCurPoddon = 0;
            string 
                ret = "(SYSN={0})AND(KMC='{1}')";

            if (xCDoc.xDocP.TypOper == AppC.TYPOP_KMPL)
            {// ��� ������������ ����� ������������ ����������� �� �������
                if (bUsePoddon)
                {// ������ �� ������� ����������
                    try
                    {
                        nCurPoddon = xCDoc.xNPs.Current;
                    }
                    catch { nCurPoddon = 0; }
                    if (nCurPoddon > 0)
                        ret += "AND(NPODDZ={2})";
                }
            }
            ret = String.Format(ret, nSys, sKMC, nCurPoddon);
            return (ret);
        }


        private int FindRowsInZVK_(DataRow dr, ref PSC_Types.ScDat sc, string nParty, FRACT fCurEmk, int nRetAtMy, bool bEvalInPall)
        {
            int
                //nState = (int)dr["READYZ"],
                //nCond = (int)dr["COND"],
                nRet = AppC.RC_ALREADY;
            string
                sDat;

            #region ������ ���������� ����� ������
            do
            {// ��������� ��������� ������� ���������������� � ������
                if ((int)dr["READYZ"] != (int)NSI.READINESS.FULL_READY)
                {// ������ ������ ���� �� ���������

                    if (bEvalInPall)
                    {// ��� ������������ ��������� ������ (���� ��� ����������)
                        if (xCDoc.xNPs.Current != (int)dr["NPODDZ"])
                        {// ��� �����
                            if (nRetAtMy == AppC.RC_OK)
                            {// � ����� ����������, �� ����� ����
                                nRet = AppC.RC_BADPODD;
                                break;
                            }
                            nRet = AppC.RC_OK;
                            if (sc.sErr == "")
                                sc.sErr = String.Format("������ ({0}) ������!", dr["NPODDZ"]);
                        }
                    }
                    //sDat = (dr["DVR"] == System.DBNull.Value) ? "" : (string)dr["DVR"];
                    sDat = (dr["DTG"] == System.DBNull.Value) ? "" : (string)dr["DTG"];

                    if (sDat.Length > 0)
                    {// ���� ����������� �� ���� ��� ������
                        DateTime dDat = DateTime.ParseExact(sDat, "yyyyMMdd", null);
                        if (nParty.Length > 0)
                        {// ������� ���������� ������ �� xx/yy/zz
                            if (nParty == "*")
                            {// ������ ����� ���� �����, �� ���� ����� ���������
                                if (dDat == sc.dDataGodn)
                                {// ������ ���������� �����
                                    nRet = (WhatPrtKeyF_Emk(dr, ref sc, fCurEmk)) ? AppC.RC_OK : AppC.RC_NOEANEMK;
                                }
                                else
                                {
                                    nRet = AppC.RC_BADDTG;
                                    sc.sErr = String.Format("�������� ������ {0}!", dDat.ToString("dd.MM.yy"));
                                }
                            }
                            else
                            {
                                if ((nParty == sc.nParty) && (dDat == sc.dDataGodn))
                                {// ������ ���������� �����
                                    nRet = (WhatTotKeyF_Emk(dr, ref sc, fCurEmk)) ? AppC.RC_OK : AppC.RC_NOEANEMK;

                                }
                                else
                                    nRet = AppC.RC_BADPARTY;
                            }
                            break;
                        }
                        if (sc.dDataGodn >= dDat)
                        {// �� ����� �������� ��������
                            nRet = AppC.RC_OK;
                            if (WhatPrtKeyF_Emk(dr, ref sc, fCurEmk))
                                break;
                            nRet = AppC.RC_NOEANEMK;
                        }
                        else
                        {// �� ����� �������� �� ��������
                            nRet = AppC.RC_BADDTG;
                            sc.sErr = String.Format("�������� �� ������ {0}!", dDat.ToString("dd.MM.yy"));
                        }

                    }
                    else
                    {// ����� ������, ����������� ����� ������� ��� �����
                        nRet = AppC.RC_OK;
                        if (WhatPrtKeyF_Emk(dr, ref sc, fCurEmk))
                            break;
                        nRet = AppC.RC_NOEANEMK;
                    }
                }
            } while (false);
            #endregion

            return (nRet);
        }

        /// ����� ���������� ����� � ������
        private int FindRowsInZVK(DataRow dr, ref PSC_Types.ScDat sc, string nParty, FRACT fCurEmk, int nRetAtMy, bool bEvalInPall)
        {
            bool
                bGoodDate = false,
                bUseDTG = false;
            int
                //nState = (int)dr["READYZ"],
                nCond = (int)dr["COND"],
                nRet = AppC.RC_ALREADY;
            string
                sKindDat = "",
                sDat = "";
            DateTime 
                dCurDat,
                dDat;

            #region ������ ���������� ����� ������
            do
            {// ��������� ��������� ������� ���������������� � ������
                if ((int)dr["READYZ"] != (int)NSI.READINESS.FULL_READY)
                {// ������ ������ ���� �� ���������

                    if (bEvalInPall)
                    {// ��� ������������ ��������� ������ (���� ��� ����������)
                        if (xCDoc.xNPs.Current != (int)dr["NPODDZ"])
                        {// ��� �����
                            if (nRetAtMy == AppC.RC_OK)
                            {// � ����� ����������, �� ����� ����
                                nRet = AppC.RC_BADPODD;
                                break;
                            }
                            nRet = AppC.RC_OK;
                            if (sc.sErr == "")
                                sc.sErr = String.Format("������ ({0}) ������!", dr["NPODDZ"]);
                        }
                    }
                    //sDat = (dr["DVR"] == System.DBNull.Value) ? "" : (string)dr["DVR"];
                    //sDat = (dr["DTG"] == System.DBNull.Value) ? "" : (string)dr["DTG"];

                    if ((nCond & (int)NSI.SPECCOND.DATE_G_SET) > 0)
                    {
                        sDat = (string)dr["DTG"];
                        sKindDat = "��������";
                        bUseDTG = true;
                    }
                    else if ((nCond & (int)NSI.SPECCOND.DATE_V_SET) > 0)
                    {
                        sDat = (string)dr["DVR"];
                        sKindDat = "���������";
                        bUseDTG = false;
                    }
                    else
                        sDat = "";

                    if (sDat.Length > 0)
                    {// ���� ����������� �� ���� ��� ������
                        dDat = DateTime.ParseExact(sDat, "yyyyMMdd", null);
                        if (bUseDTG)
                        {// �������� ��������
                            if (sc.dDataGodn >= dDat)
                            {
                                if (((nCond & (int)NSI.SPECCOND.DATE_SET_EXACT) == 0) &&
                                    ((nCond & (int)NSI.SPECCOND.PARTY_SET) == 0) || 
                                    (sc.dDataGodn == dDat))
                                    bGoodDate = true;
                            }
                        }
                        else
                        {// �������� ���������
                            if (sc.dDataIzg >= dDat)
                            {
                                if (((nCond & (int)NSI.SPECCOND.DATE_SET_EXACT) == 0) &&
                                    ((nCond & (int)NSI.SPECCOND.PARTY_SET) == 0) || 
                                    (sc.dDataIzg == dDat))
                                bGoodDate = true;
                            }
                        }

                        if (bGoodDate)
                        {
                            if ((nCond & (int)NSI.SPECCOND.PARTY_SET) > 0)
                            {// ������ � ���� ������ ����� ���������
                                if (nParty == sc.nParty)
                                {// ������ ���������� �����
                                    nRet = (WhatTotKeyF_Emk(dr, ref sc, fCurEmk)) ? AppC.RC_OK : AppC.RC_NOEANEMK;
                                }
                                else
                                    nRet = AppC.RC_BADPARTY;
                            }
                            else
                            {// ������ �� ����������
                                if (WhatPrtKeyF_Emk(dr, ref sc, fCurEmk))
                                    nRet = AppC.RC_OK;
                                else
                                    nRet = AppC.RC_NOEANEMK;
                            }
                        }
                        else
                        {// �� ���� �� ��������
                                nRet = AppC.RC_BADDATE;
                            sc.sErr = String.Format("���� {0} ({1})\n������ ��������:\n{2}!", sKindDat, 
                                ((bUseDTG)?sc.dDataGodn:sc.dDataIzg).ToString("dd.MM.yy"), 
                                dDat.ToString("dd.MM.yy"));
                        }
                        break;
                    }

                    if (nParty.Length > 0)
                    {// ������� ���������� ������
                        if (nParty == sc.nParty)
                        {// ������ ���������� �����
                            nRet = (WhatTotKeyF_Emk(dr, ref sc, fCurEmk)) ? AppC.RC_OK : AppC.RC_NOEANEMK;
                        }
                        else
                            nRet = AppC.RC_BADPARTY;
                        break;
                    }

                    // ����� ������, ����������� ����� ������� ��� �����
                    if (WhatPrtKeyF_Emk(dr, ref sc, fCurEmk))
                        nRet = AppC.RC_OK;
                    else
                        nRet = AppC.RC_NOEANEMK;
                    break;
                }
            } while (false);
            #endregion

            return (nRet);
        }




        // ������ ��� ���������� ����������
        private DataRow PrevKol(ref PSC_Types.ScDat sc, ref int nAlrM, ref FRACT fAlrE)
        {
            int
                nIDZvk;
            string
                sF;
            DataRow
                drZ = null;
            DataView
                dv;

            nAlrM = 0;
            fAlrE = 0;

            try
            {
                drZ = sc.lstAvailInZVK[sc.nCurAvail];
                nIDZvk = (int)(drZ["NPP"]);

                sF = String.Format("{0} AND (NPP_ZVK={1})", sc.sFilt4View, nIDZvk);
                dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt,
                    sF, "EMK, DVR, NP DESC", DataViewRowState.CurrentRows);

                foreach (DataRowView drv in dv)
                {
                    nAlrM += (int)(drv.Row["KOLM"]);
                    fAlrE += (FRACT)(drv.Row["KOLE"]);
                }
            }
            catch
            {
                drZ = null;
                nIDZvk = -1;
            }

            sc.nMAlr_NPP = nAlrM;
            sc.fVAlr_NPP = fAlrE;

            return (drZ);
        }

        // ������ ����������� ��� ����� ����������
        // nNonCondUsing = -1 - �� ������������ ������, ���� ���� ����
        // nNonCondUsing =  1 - ������������ ������, ���� ���� �� ���������
        private int Prep4Ed(ref PSC_Types.ScDat sc, ref bool bWillBeEdit, int nUnCondUsing)
        {
            int
                nM = 0,                 // ��������� ���������� ����, ��� ��������������� � ���
                nMEd = 0,               // ���������� ���������� ���� ��� ��������������/�������������
                nRet = AppC.RC_OK;
            bool
                bUseZVK = false;        // � ������� ���-�� �� ��������, ��� �� ���������

            string
                sErr;

            FRACT
                fVEd = 0,               // ���������� ���������� ������ ��� ��������������/�������������
                fV = 0;                 // ��������� ���������� ������ , ��� ��������������� � ���


            DataRow
                drZ;
            DataView
                dv4Sum = null;

            if (xCDoc.xDocP.nNumTypD == AppC.TYPD_MOVINT)
            {// �������� �������������� ��������

                if ((xCDoc.xDocP.TypOper == AppC.TYPOP_PRMK) ||
                    (xCDoc.xDocP.TypOper == AppC.TYPOP_MOVE) ||
                    (xCDoc.xDocP.TypOper == AppC.TYPOP_MARK))
                {
                    if (sc.tTyp == AppC.TYP_TARA.TARA_PODDON)
                    {
                        bWillBeEdit = false;
                        if ((sc.nRecSrc == (int)NSI.SRCDET.FROMADR) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR_BUTTON) ||
                            (sc.nRecSrc == (int)NSI.SRCDET.SSCCT)) { }
                        else
                            sc.nMest = sc.nMestPal;
                        if (sc.bVes == true)
                        {
                            sc.fVsego = sc.fVes;
                        }
                        else
                        {
                            sc.fVsego = sc.nMest * sc.fEmk;
                        }
                    }
                    else
                    {
                        sc.nMest = sc.nMestPal;
                        if (sc.bVes == true)
                        {
                            //sc.nMest = (sc.nTypVes == AppC.TYP_VES_PAL) ? -1 :
                            //            (sc.nTypVes == AppC.TYP_VES_TUP) ? 1 : 0;
                            sc.nMest = (sc.tTyp == AppC.TYP_TARA.TARA_PODDON) ? -1 :
                                        (sc.tTyp == AppC.TYP_TARA.TARA_TRANSP) ? 1 : 0;
                            sc.fVsego = sc.fVes;
                        }
                        else
                            sc.fVsego = ((sc.nMest == 0) ? 1 : sc.nMest) * sc.fEmk;
                    }
                    if (sc.fVsego == 0)
                        bWillBeEdit = true;

                }
                return (nRet);
            }

            if (sc.sFilt4View.Length == 0)
                sc.sFilt4View = FilterKompl(xCDoc.nId, sc.sKMC, false);

            dv4Sum = new DataView(xNSI.DT[NSI.BD_DOUTD].dt,
                sc.sFilt4View, "EMK, DTG, NP DESC", DataViewRowState.CurrentRows);

            EvalEnteredKol(dv4Sum, 0, ref sc, sc.sKMC, sc.fEmk, out nM, out fV, false);

            if (bZVKPresent)
            {// ������ �������
                if (sc.nCurAvail >= 0)
                {// � ������ ������� ����������

                    drZ = sc.lstAvailInZVK[sc.nCurAvail];

                    if ((xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM) && (xCDoc.xDocP.nNumTypD != AppC.EMPTY_INT))
                    {// ��� ��������� ��������� ����� ���������� �� ������?
                        //if (xPars.aDocPars[xCDoc.xDocP.nNumTypD].bShowFromZ)
                        if (xCDoc.xDocP.DType.MoveType != AppC.MOVTYPE.AVAIL)
                        {// ��, ����� �������� ������� ������������ - ����, ������ - �� ����
                            if (nUnCondUsing != -1)
                                bUseZVK = true;
                        }
                        else
                        {
                            if (nUnCondUsing == 1)
                                bUseZVK = true;
                        }
                    }
                    if (bUseZVK)
                    {// ������� ������������
                        if (IsEasyEdit()
                            || (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM))
                        {// � ���������� �������� � ����� ������� ������
                            //nMEd = (int)drZ["KOLM"] - nM;
                            //fVEd = (FRACT)drZ["KOLE"] - fV;

                            nMEd = (int)drZ["KOLM"];
                            fVEd = (FRACT)drZ["KOLE"];
                            PrevKol(ref sc, ref nM, ref fV);
                            if (((nMEd > 0) && (nM > 0))
                                || ((nMEd == 0) && ((fVEd > 0) && (fV > 0))))
                            {// ���� ��������� �������� �� ������
                                bWillBeEdit = true;
                            }
                            nMEd -= nM;
                            fVEd -= fV;
                        }
                        else
                        {// � ������� �������� �� ���� �������
                            nMEd = sc.nKolM_zvk - nM;
                            fVEd = sc.fKolE_zvk - fV;
                        }

                        if ((nMEd <= 0) && (fVEd <= 0))
                        {// �����-�� ������������ ��������� ���������� ��� ����� ����������
                            sc.nDocCtrlResult = AppC.RC_ALREADY;
                            sErr = "������ ��� ���������!";

                            DialogResult
                                dr = MessageBox.Show("�������� ���� (Enter)?\n(ESC) - ���������� ����", sErr,
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                            if (dr == DialogResult.OK)
                                return (AppC.RC_CANCEL);

                            // ��� ����� ������ �������
                            sc.nDest = NSI.DESTINPROD.USER;
                            bUseZVK = false;
                        }
                        if (bUseZVK)
                        {// ����� ����������� ��������������?
                            if (sc.bVes == true)
                            {// ��� ��������

                                //if ((nMEd > 0) &&
                                //    (sc.tTyp == AppC.TYP_TARA.TARA_TRANSP) &&
                                //    (xCDoc.xDocP.nTypD == AppC.TYPD_INV))
                                //{// ������ �� ������� - ������ ��� ��������������
                                //}
                                //else
                                //    bUseZVK = false;

                                // ������ ��������������� ���������� ���� � ����������� �� ����
                                bUseZVK = false;

                            }
                            else
                            {// ��� ��������

                                // ���-�� ���������� ��� ��������������� ����������?
                                if (!((nMEd > 0) && (fVEd == 0)))
                                    bWillBeEdit = true;

                                // 
                                if (sc.tTyp == AppC.TYP_TARA.TARA_PODDON)
                                {
                                }
                                else
                                {
                                    if (sc.nRecSrc == (int)NSI.SRCDET.SSCCT)
                                    {// ������ �� ��������� �� �������� �������, ������ �� ������������
                                        bUseZVK = false;
                                    }
                                }

                                bWillBeEdit |= VerifyMestByPoddon(ref sc, ref nMEd, ref fVEd);
                                nMEd = Math.Max(nMEd, 0);
                            }

                            if ((sc.nDest != NSI.DESTINPROD.TOTALZ) && (sc.nDest != NSI.DESTINPROD.USER))
                                sc.nDest = NSI.DESTINPROD.PARTZ;
                        }
                    }
                }
            }

            if (bUseZVK == false)
            {// ������ ����������� ��� �� ����������� �� ��������
                do
                {
                    if ((sc.nRecSrc == (int)NSI.SRCDET.SSCCT) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR_BUTTON))
                    {// �� ������� ���������� �������������� ��� �������� � ��������
                        nMEd = sc.nMest;
                        fVEd = sc.fVsego;
                        break;
                    }

                    if (sc.bVes == true)
                    {//  ��� �������� ������ ��� ����� ������
                        switch (sc.tTyp)
                        {
                            case AppC.TYP_TARA.TARA_POTREB:
                                nMEd = 0;
                                break;
                            case AppC.TYP_TARA.TARA_TRANSP:
                                nMEd = 1;
                                break;
                            case AppC.TYP_TARA.TARA_PODDON:
                                nMEd = (sc.nMestPal > 0) ? sc.nMestPal : -1;
                                break;
                        }
                    }
                    else
                    {// ��� �������� ������ ��� ����� ������
                        if (sc.tTyp == AppC.TYP_TARA.TARA_PODDON)
                        {// ����������� ���������� ��� ����-����������

                            // ���������� ������ � �������, ��� ����� ����� ������ ���������� (��������)
                            if ((sc.nRecSrc == (int)NSI.SRCDET.SCAN) || (sc.nRecSrc == (int)NSI.SRCDET.HANDS))
                            {
                                nMEd = sc.nMestPal;
                                fVEd = nMEd * sc.fEmk;
                            }
                        }
                        else { }// �� ��������� nMest = 0, fVsego=0
                    }
                } while (false);
            }


            if (sc.bVes)
                fVEd = sc.fVes;
            //else
            //{
            //    if (sc.tTyp == AppC.TYP_TARA.TARA_POTREB)
            //    {
            //        if ((nMEd > 0) && (fVEd > 0))
            //        {
            //            fV = TrySetEmkByZVK(AppC.TYP_TARA.TARA_POTREB, ref sc, 0);
            //            if (fV > 0)
            //            {
            //                sc.tTyp = AppC.TYP_TARA.TARA_TRANSP;
            //            }
            //            else
            //            {
            //                fV   = 0;
            //                nMEd = 0;
            //            }
            //            sc.fEmk = fV;
            //        }
            //    }
            //}

            sc.nMest = nMEd;
            sc.fVsego = fVEd;

            return (nRet);
        }

        // ��������� ������ ���
        // ������� ��� �������������
        private int EvalEnteredKol(DataView dvEn, int i, ref PSC_Types.ScDat sc, string sKMCCode, FRACT fE,
            out int nM_A, out FRACT fE_A, bool bDocControl)
        {
            //int ret = AppC.RC_OK;
            bool
                bInEasy,
                bTry2FindSimilar,
                bUsingZVK = false,
                bSame = false;
            string
                //nParty_ZVK = "",
                nParty = "";

            int
                ret = 0,
                nM = 0,
                nMaxR = 0,
                //nCond = (int)NSI.SPECCOND.NO,

                // ������������� ���� � ������ ����������� �����
                nKolM_alrT = 0,
                // ������������� ���� � ���������� ����������� �����
                nKolM_alr = 0;

            DateTime
                //dDVyr,
                dDGodn;

            FRACT
                fEm = 0,
                //fEmk_ZVK = 0,

                fV = 0,

                // ������������� ������ � ������ ����������� �����
                fKolE_alrT = 0,
                // ������������� ������ � ���������� ����������� �����
                fKolE_alr = 0;

            //NSI.DESTINPROD 
            //    desProd;
            DataRow
                dr;
            //drZ = null;

            // ����� ���� �� ���� �������������
            nM_A = 0;
            // ����� ������ �� ���� �������������
            fE_A = 0;

            // ���� ������ ������ �� ���������
            sc.drEd = sc.drMest = null;

            bInEasy = IsEasyEdit();

            if ((sc.lstAvailInZVK.Count > 0) && (sc.nCurAvail >= 0))
            {// ���������� ������ ��� ������� ����� (�������� � ����� �� ��������� �����)
                bUsingZVK = true;
                //drZ = sc.lstAvailInZVK[sc.nCurAvail];
                //nCond = (int)drZ["COND"];
                //fEmk_ZVK = (FRACT)drZ["EMK"];
                //if (nCond != (int)NSI.SPECCOND.NO)
                //   dDVyr_ZVK = DateTime.ParseExact((string)drZ["DVR"], "yyyyMMdd", null);
                //nParty_ZVK = (string)drZ["NP"];
            }

            // ������ - SYSN + KMC [+ � �������]
            nMaxR = dvEn.Count;
            while ((i < nMaxR) && ((string)dvEn[i].Row["KMC"] == sKMCCode))
            {
                dr = dvEn[i].Row;
                nM = (int)dr["KOLM"];
                fV = ((int)dr["SRP"] > 0) ? 1 : (FRACT)dr["KOLE"];
                fEm = (FRACT)dr["EMK"];
                nParty = (string)dr["NP"];
                //dDVyr = DateTime.ParseExact((string)dr["DVR"], "yyyyMMdd", null);
                dDGodn = DateTime.ParseExact((string)dr["DTG"], "yyyyMMdd", null);

                // ��� ���� ����� ����� �� � ������ ���?
                bSame = ((nParty == sc.nParty) && (dDGodn.Date == sc.dDataGodn.Date)) ? true : false;

                if (bUsingZVK)
                {// �������� ������� ������ �� ��� � ������ ����������?
                    //bTry2FindSimilar = CmpFromTTN2ZVK(nCond, fEmk_ZVK, dDVyr_ZVK, nParty_ZVK, fEm, dDVyr, nParty);
                    bTry2FindSimilar = CmpFromTTN2ZVK(ref sc, fEm, dDGodn, nParty);
                }
                else
                    bTry2FindSimilar = true;

                if (bTry2FindSimilar)
                {
                    if (fEm == 0)
                    {// ��������������� �������

                        fE_A += fV;
                        if (bSame)
                        {
                            fKolE_alrT += fV;
                            if (bDocControl)
                                dr["DEST"] = NSI.DESTINPROD.TOTALZ;
                            if (sc.drEd == null)
                            {
                                if ((xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM) &&
                                    ((int)dr["NPODDZ"] == 0))
                                {// � �������������� ������� �� ���������
                                    sc.drEd = dr;
                                }
                            }
                        }
                        else
                        {// ������� �� ����� ������
                            fKolE_alr += fV;
                            if (bDocControl)
                                dr["DEST"] = NSI.DESTINPROD.PARTZ;
                        }
                    }
                    else
                    {// ��������������� �����
                        if (fEm == fE)
                        {
                            nM_A += nM;
                            if (bSame)
                            {
                                nKolM_alrT += nM;
                                if (bDocControl)
                                    dr["DEST"] = NSI.DESTINPROD.TOTALZ;

                                if (sc.drMest == null)
                                {
                                    if ((xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM) &&
                                        ((int)dr["NPODDZ"] == 0))
                                    {// � �������������� ������� �� ���������
                                        sc.drMest = dr;
                                    }
                                }
                            }
                            else
                            {// ��������� ����� �����
                                nKolM_alr += nM;
                                if (bDocControl)
                                    dr["DEST"] = NSI.DESTINPROD.PARTZ;
                            }
                        }
                    }
                }
                i++;
            }

            sc.nKolM_alrT = nKolM_alrT;
            sc.fKolE_alrT = fKolE_alrT;

            sc.nKolM_alr = nKolM_alr;
            sc.fKolE_alr = fKolE_alr;

            return (ret);

        }

        /// ���� �� ������� ����, ������ ��� �� �������
        //public bool Old_VerifyMestByPoddon(ref PSC_Types.ScDat sc, ref int nMZ, ref FRACT fVZ)
        //{
        //    bool
        //        bWillBeEdit = false;
        //    int
        //        nEmkPal,
        //        nM = nMZ;
        //    FRACT
        //        fV = fVZ;
        //    do
        //    {
        //        if (xCDoc.xDocP.nNumTypD== AppC.TYPD_INV)
        //            // ��� �������������� �������� ���
        //            break;

        //        try
        //        {
        //            nEmkPal = ((StrAndInt)sc.xEmks.Current).IntCode;
        //        }
        //        catch
        //        {
        //            nEmkPal = 0;
        //        }

        //        if ((nEmkPal > 0) && (nMZ > 0))
        //        {// ���� �� ������� ��������
        //            if ((nMZ > nEmkPal) && (!sc.bVes))
        //            {// ������ ������ �������
        //                if (!xPars.aParsTypes[AppC.PRODTYPE_SHT].b1stPoddon)
        //                {// ������ ������������ �������
        //                    nM = nMZ % nEmkPal;
        //                    if (nM == 0)
        //                        nM = nEmkPal;
        //                }
        //                else
        //                {// ������ ������������ ������ �������
        //                    nM = nEmkPal;
        //                }
        //            }
        //        }

        //        // ���������� ������ � �������, ��� ����� ����� ������ ���������� (��������)
        //        if ((sc.nRecSrc == (int)NSI.SRCDET.SSCCT) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR_BUTTON))
        //        {// �� ������� ���������� ��������������
        //            if (sc.nMest <= nM)
        //            {
        //                nM = sc.nMest;
        //                fV = sc.fVsego;
        //                break;
        //            }
        //        }

        //        if ((nM != 0) && (sc.fEmk != 0))
        //            fV = nM * sc.fEmk;
        //    } while (false);

        //    if (nM != nMZ)
        //        bWillBeEdit = true;

        //    nMZ = nM;
        //    fVZ = fV;

        //    return (bWillBeEdit);
        //}

        /// �������� ������������� ���������� ��� �������� ������
        public bool VerifyMestByPoddon(ref PSC_Types.ScDat sc, ref int nMZ, ref FRACT fVZ)
        {
            bool
                bWillBeEdit = false;
            int
                nEmkPal,
                nM = nMZ;
            FRACT
                fV = fVZ;
            do
            {
                if (xCDoc.xDocP.nNumTypD == AppC.TYPD_INV)
                    // ��� �������������� �������� ���
                    break;

                try
                {
                    nEmkPal = ((StrAndInt)sc.xEmks.Current).IntCode;
                }
                catch
                {
                    nEmkPal = 0;
                }

                if ((nEmkPal > 0) && (nMZ > 0))
                {// ���� �� ������� ��������
                    if ((nMZ > nEmkPal) && (!sc.bVes))
                    {// ������ ������ �������
                        if (!xPars.aParsTypes[AppC.PRODTYPE_SHT].b1stPoddon)
                        {// ������ ������������ �������
                            nM = nMZ % nEmkPal;
                            if (nM == 0)
                                nM = nEmkPal;
                        }
                        else
                        {// ������ ������������ ������ �������
                            nM = nEmkPal;
                        }
                    }
                }

                // ���������� ������ � �������, ��� ����� ����� ������ ���������� (��������)
                if ((sc.nRecSrc == (int)NSI.SRCDET.SSCCT) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR) || (sc.nRecSrc == (int)NSI.SRCDET.FROMADR_BUTTON))
                {// �� ������� ���������� ��������������
                    if (sc.nMest <= nM)
                    {
                        nM = sc.nMest;
                        fV = sc.fVsego;
                        break;
                    }
                }

                if ((nM != 0) && (sc.fEmk != 0))
                    fV = nM * sc.fEmk;

            } while (false);

            if (nM != nMZ)
                bWillBeEdit = true;

            nMZ = nM;
            fVZ = fV;

            return (bWillBeEdit);
        }



        // �������� �� ��� ��������������� ��������� � ������?
        // dr - ������ ������
        private bool CmpFromTTN2ZVK(ref PSC_Types.ScDat sc, FRACT fEmk_TTN, DateTime dDat_TTN, string sParty_TTN)
        {
            bool
                bTryNext = false,
                bMayUsed = false;
            string
                sParty_ZVK;
            DateTime
                dDat_ZVK = DateTime.MinValue; ;
            FRACT
                fEmk_ZVK;
            DataRow
                drZ = null;
            int
                iZ = 0,
                nCond;

            #region ������������� ������ ������ � ������� ������ � ������ ���
            do
            {
                drZ = sc.lstAvailInZVK[iZ];
                nCond = (int)drZ["COND"];
                fEmk_ZVK = (FRACT)drZ["EMK"];
                sParty_ZVK = (string)drZ["NP"];
                if ((nCond & (int)NSI.SPECCOND.DATE_SET) > 0)
                {
                    if (xPars.UseDTProizv)
                        dDat_ZVK = (drZ["DVR"] is string) ? DateTime.ParseExact((string)drZ["DVR"], "yyyyMMdd", null) : DateTime.MinValue;
                    else
                        dDat_ZVK = (drZ["DTG"] is string) ? DateTime.ParseExact((string)drZ["DTG"], "yyyyMMdd", null) : DateTime.MinValue;
                }

                if ((fEmk_ZVK == fEmk_TTN) || (fEmk_ZVK == 0))
                {
                    if (nCond != (int)NSI.SPECCOND.NO)
                    {// ���� ����������� ��� ���� ��� ������
                        if (nCond == (int)NSI.SPECCOND.PARTY_SET)
                        {// ������� ���������� ������ �� xx/yy/zz
                            if ((sParty_ZVK == sParty_TTN) && (dDat_ZVK == dDat_TTN))
                            {// ������ ���������� �����
                                bMayUsed = true;
                            }
                            break;
                        }
                        if (dDat_TTN >= dDat_ZVK)
                        {// �� ����� �������� ��������
                            bMayUsed = true;
                            break;
                        }
                    }
                    else
                    {// ����� ������, ����������� ����� ������� ��� �����
                        bMayUsed = true;
                        break;
                    }
                }
                if (++iZ >= sc.lstAvailInZVK.Count)
                    bTryNext = false;
            } while (!bMayUsed && bTryNext);

            #endregion

            if (bMayUsed)
                // ����� �� break - ������!!!
                sc.nCurAvail = iZ;
            return (bMayUsed);
        }

        // ���������� ��������� "��������"-"�������" ��� ������� �������� scCur (���������-�������-����-������)
        private bool TryEvalNewZVKTTN(ref PSC_Types.ScDat scD, bool bUpdateGUI)
        {
            bool
                bRet = AppC.RC_CANCELB,
                bDopValuesNeed = false;
            int
                nRegZVKUsing = 0;

            if (AppC.RC_OK == EvalZVKMest(ref scD, null, 0, 0))
            {
                if (!bUpdateGUI)
                    nRegZVKUsing = -1;

                if (AppC.RC_OK == Prep4Ed(ref scD, ref bDopValuesNeed, nRegZVKUsing))
                {
                    //if (bUpdateGUI)
                    //    VerifyMestByPoddon(ref scD);

                    PInfReady();
                    if (bUpdateGUI)
                    {
                        ShowDopInfKMPL(scD.nKolM_alr + scD.nKolM_alrT);
                        SetDopFieldsForEnter(false, true);
                    }
                    bRet = AppC.RC_OKB;
                }
            }
            return (bRet);
        }


        // ����� ���.���������� ����� ������������ ��� ������������
        private void ShowDopInfKMPL(object x)
        {
            string sN = "";
            if (((int)xCDoc.drCurRow["TYPOP"] == AppC.TYPOP_KMPL) &&
                (xScrDet.CurReg == ScrMode.SCRMODES.FULLMAX))
            {
                try
                {
                    sN = scCur.dDataIzg.ToString("dd.MM");
                    sN = String.Format("� {0} {1}", scCur.nParty, sN);
                }
                catch { }
                //tVvod_VESReg.Text = sN;
                lSSCCState.Text = x.ToString();
            }
        }

        private FRACT TrySetEmkByZVK(AppC.TYP_TARA tTara, ref PSC_Types.ScDat sc, FRACT fCurE)
        {
            FRACT
                fEZ,
                fE = fCurE;
            if (bZVKPresent)
            {
                string sF = FilterKompl(xCDoc.nId, sc.sKMC, (xCDoc.xNPs.Current > 0) ? true : false);
                sF += " AND(EMK>0)";
                DataView dv = new DataView(xNSI.DT[NSI.BD_DIND].dt,
                        sF, "EMK, DVR, NP DESC", DataViewRowState.CurrentRows);
                if (dv.Count == 1)
                {
                    string sMsg = "";
                    fEZ = (FRACT)dv[0].Row["EMK"];
                    if (tTara == AppC.TYP_TARA.TARA_POTREB)
                        // ��������������� ��������
                        sMsg = String.Format("(ENT) - ��� ���� \n �������� {0:N1}\n(ESC) - ��������������� ����\n", fEZ);
                    else if (fEZ != fCurE)
                        sMsg = String.Format("(ENT) - ����� {0:N1} �� ������ ?\n(ESC) - ������������ {1:N1}", fEZ, fCurE);
                    if (sMsg.Length > 0)
                    {
                        DialogResult dr = MessageBox.Show(sMsg, "������� ����������!",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                        if (dr == DialogResult.OK)
                            fE = fEZ;
                    }
                }
            }
            return (fE);
        }









        // ����������� ������� � ������ ��� �������� ������ �� ����������� ��������
        // ��������� �������� - 1 ������������ �������� (���� �����)
        //                      2 ��������������� ���� (���� �����)
        //                      3 ������ (��������� ����)
        // ����� ���� �����������:
        // - �������
        // - ����
        // - ���������� ����
        // - ��� ��������
        private bool TrySetEmk(DataTable dtM, DataTable dtD, ref PSC_Types.ScDat sc, FRACT fVesU)
        {
            bool ret = false;

            bool bTryComp = false,       // ���� ���-�� ��������� ��� ����������� ���� ��������?
                bNot1Sht = true,         // ��� �� ��������� �������
                bNot1Pal = true;         // ��� �� �������

            int
                nPrPl = 0,               // � ������. ����.
                nVesVar = xPars.aParsTypes[AppC.PRODTYPE_VES].nDefEmkVar,
                nT = 0,
                nEP = 0,
                nSht = 0;
            FRACT fEmk = 0;
            FRACT
                fCE,
                fSignDiff,
                fDiff,
                fDiffPercent = 0,
                fDiff1Ed = 1000,
                fDiff_Start = 1000000;

            if ((sc.bFindNSI == true) && (sc.drMC != null))
            {// ����� ��� ������ ������� �� ���� ��������� � �������� ���������� ����

                DataRelation myRelation = dtM.ChildRelations[NSI.REL2EMK];
                DataRow[] childRows = sc.drMC.GetChildRows(myRelation);

                if (sc.nParty.Length > 0)
                    //nPrPl = int.Parse(sc.nParty.ToString().Substring(0, 1));
                    nPrPl = int.Parse(sc.nParty.Substring(0, 1));

                foreach (DataRow chRow in childRows)
                {
                    fCE = (FRACT)chRow["EMK"];
                    if (fCE != 0)
                    {// ������� �������
                        if (fVesU > 0)
                        {// � ��� �������
                            bTryComp = true;
                            fSignDiff = fVesU - fCE;
                            fDiff = Math.Abs(fSignDiff);
                            fDiffPercent = fDiff * 100 / fCE;

                            //bNot1Sht |= (fDiffPercent < 40) || (fDiffPercent > 100);

                            //bNot1Pal |= (fDiffPercent < 200);

                            if (fDiffPercent <= nVesVar)
                            {// ������ �� 1 ������ �����, ������������ ������ ���������� � �������� 40%
                                if (fDiff < fDiff_Start)
                                {
                                    bNot1Sht = true;
                                    bNot1Pal = true;
                                    fDiff_Start = fDiff;
                                    fEmk = fCE;
                                    nT = (int)chRow["KT"];
                                    nSht = (int)chRow["KRK"];
                                    nEP = (int)chRow["EMKPOD"];
                                }
                            }
                            else
                            {// ��� ����� ��� ������
                                if (fVesU < fCE)
                                {// ���� �� ������
                                    bNot1Pal = true;
                                    nSht = (int)chRow["KRK"];
                                    nEP = (int)chRow["EMKPOD"];
                                    if (nSht > 0)
                                    {
                                        fDiff1Ed = Math.Abs(fVesU - (fCE / nSht));
                                        fDiff1Ed = fDiff1Ed * 100 / fVesU;
                                        if (fDiff1Ed <= 20)
                                        {
                                            bNot1Sht = false;
                                            fEmk = 0;
                                            break;
                                        }
                                    }

                                }
                                else
                                {// ��� �������� ������� ������������ �� ����� ����
                                    // ��� ������� ������� (���������� ��)
                                    bNot1Sht = true;
                                    bNot1Pal = false;
                                }
                            }


                        }
                        else
                        {// ����������� ���� � ����/����
                            if (sc.fEmk > 0)
                            {// ��� �������
                                if (sc.fEmk == (FRACT)chRow["EMK"])
                                {// ��������� �������
                                    if (((int)chRow["PR"] > 0) || (fEmk == 0))
                                    {// ���� �� ������������ ��� ����� �� ��������
                                        fEmk = (FRACT)chRow["EMK"];
                                        nT = (int)chRow["KT"];
                                        nSht = (int)chRow["KRK"];
                                        nEP = (int)chRow["EMKPOD"];
                                        if ((int)chRow["PR"] == nPrPl)
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    if (childRows.Length == 1)
                    {// ��������� ������, ������ ���� �������
                        fEmk = (FRACT)chRow["EMK"];
                        nT = (int)chRow["KT"];
                        nSht = (int)chRow["KRK"];
                        nEP = (int)chRow["EMKPOD"];
                    }
                }

                sc.fEmk = fEmk;
                sc.fEmk_s = sc.fEmk;
                sc.nTara = nT;
                sc.nKolSht = nSht;
                sc.nMestPal = nEP;
                if (bTryComp == true)
                {
                    if (bNot1Sht == false)
                    {// 1 �����
                        if (sc.nTypVes != AppC.TYP_VES_PAL)
                        {
                            sc.fEmk = TrySetEmkByZVK(AppC.TYP_TARA.TARA_POTREB, ref sc, 0);
                            if (sc.fEmk != 0)
                            {
                                sc.nTypVes = AppC.TYP_VES_TUP;
                                sc.nMest = 1;
                                fEmk = sc.fEmk;
                            }
                            else
                            {
                                sc.nTypVes = AppC.TYP_VES_1ED;
                                sc.nMest = 0;
                            }
                        }
                    }
                    else if ((fEmk != 0) && (fDiff_Start < 40))
                    {
                        if (sc.nTypVes != AppC.TYP_PALET)
                        {
                            sc.nTypVes = AppC.TYP_VES_TUP;
                            sc.nMest = 1;
                            sc.fEmk = TrySetEmkByZVK(AppC.TYP_TARA.TARA_TRANSP, ref sc, fEmk);
                            if (sc.fEmk != fEmk)
                            {// ���-�� ����������
                                if ((fVesU / sc.fEmk) > 1.3M)
                                {
                                    sc.nTypVes = AppC.TYP_VES_PAL;
                                    sc.nMest = -1;
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                    else if (bNot1Pal == false)
                    {
                        sc.nTypVes = AppC.TYP_VES_PAL;
                        sc.nMest = -1;
                    }

                }
                ret = (fEmk > 0) ? true : false;
            }
            else
            {// ���������� �����������, �� ��������� - ???
            }

            return (ret);
        }




        private int EvalEnteredVals(ref PSC_Types.ScDat sc, string sKMCCode, FRACT fE, string nP,
            DataView dvEn, int i, int nMaxR)
        {
            int
                ret = 0,
                nM = 0,
                nMest = 0;
            string
                nParty = "",
                sDVyr;

            FRACT fEm = 0,
                fVsego = 0,
                fV = 0;

            NSI.DESTINPROD desProd;
            DataRow dr;

            sc.fKolE_alr = 0;
            sc.nKolM_alr = 0;
            sc.fMKol_alr = 0;

            sc.fKolE_alrT = 0;          // ��� ������� ������ ������� ���� (���� = 0)
            sc.nKolM_alrT = 0;          // ��� ������� ���� ������� ����
            sc.fMKol_alrT = 0;

            sc.drEd = null;
            sc.drMest = null;


            // ������ - SYSN + EAN13
            if (dvEn == null)
            {
                if (sc.sFilt4View.Length == 0)
                    sc.sFilt4View = FilterKompl(xCDoc.nId, sKMCCode, false);

                dvEn = new DataView(xNSI.DT[NSI.BD_DOUTD].dt,
                    sc.sFilt4View, "EMK, DVR, NP DESC", DataViewRowState.CurrentRows);
                nMaxR = dvEn.Count;
                i = 0;
            }

            while ((i < nMaxR) && ((string)dvEn[i].Row["KMC"] == sKMCCode))
            {
                dr = dvEn[i].Row;
                nM = (int)dr["KOLM"];
                fV = ((int)dr["SRP"] > 0) ? 1 : (FRACT)dr["KOLE"];
                fEm = (FRACT)dr["EMK"];
                nParty = (string)dr["NP"];
                sDVyr = (string)dr["DVR"];

                desProd = (NSI.DESTINPROD)(dr["DEST"]);

                if (fEm == 0)
                {// ��� �������
                    if (desProd == NSI.DESTINPROD.TOTALZ)
                    {// ������� ��� ���������� ������ �� ������
                        sc.fKolE_alrT += fV;
                    }
                    else
                    {// ������� �� ����� ������
                        sc.fKolE_alr += fV;
                    }
                    if (nParty == nP)
                    {// ���� ������� ������ � ���� ���������
                        if (sDVyr == sc.dDataIzg.ToString("yyyyMMdd"))
                        {// ���� ����������� - ������ ����
                            sc.drEd = dr;
                        }
                    }
                }
                else
                {// �����
                    nMest += nM;
                    fVsego += fV;
                    if (fEm == fE)
                    {
                        if (desProd == NSI.DESTINPROD.TOTALZ)
                        {
                            sc.nKolM_alrT += nM;
                            sc.fMKol_alrT += fV;
                        }
                        else
                        {// ��������� ����� �����
                            sc.nKolM_alr += nM;
                            sc.fMKol_alr += fV;
                        }
                        if (nParty == nP)
                        {
                            if (sDVyr == sc.dDataIzg.ToString("yyyyMMdd"))
                            {// ���� ����������� - ������ ����
                                sc.drMest = dr;
                            }
                        }
                    }
                }

                i++;

                // ����� � ������ �����
                ret++;
            }

            if (fE == 0)
            {// ������� ����������
                sc.nKolM_alr = nMest;
                sc.fMKol_alr = fVsego;
            }
            return (ret);
        }


        // �������� ����� ��������� �� ������������ ������
        private void ControlDocZVK_Old(DataRow drD, List<string> lstProt)
        {
            int
                i = 0,
                nM,
                iStart,
                iCur,
                iTMax, iZMax,
                nDokState = AppC.RC_OK,
                nRet;
            string
                s1,
                s2,
                sFlt;
            FRACT
                fE,
                fV = 0;
            object
                xProt;

            DataRow
                drC;
            RowObj
                xR;

            //TimeSpan tsDiff;
            //int t1 = Environment.TickCount, t2, t3, td1, td2, tc = 0, tc1 = 0, tc2 = 0;
            //t2 = t1;

            if (drD == null)
                drD = xCDoc.drCurRow;

            bZVKPresent = (xCDoc.drCurRow.GetChildRows(NSI.REL2ZVK).Length > 0) ? true : false;

            string sRf = String.Format("(SYSN={0})", drD["SYSN"]);

            PSC_Types.ScDat sc = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));
            lstProt.Add(HeadLineCtrl(drD));

            // ���� ��� ��������� �������� ����������
            drD["DIFF"] = NSI.DOCCTRL.UNKNOWN;

            // ��� ��������� �� ������ �� ���������
            DataView
                //dvZ = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "KRKMC", DataViewRowState.CurrentRows);
                dvZ = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, (xPars.PKeyIsGUID)?"KMC":"KRKMC", DataViewRowState.CurrentRows);

            iZMax = dvZ.Count;
            if (iZMax <= 0)
            {
                nDokState = AppC.RC_CANCEL;
                lstProt.Add("*** ������ �����������! ***");
            }

            // ��� ��������� �� ��� �� ���������
            DataView
            //dvT = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "KRKMC,EMK DESC", DataViewRowState.CurrentRows);
            dvT = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, (xPars.PKeyIsGUID) ? "KMC,EMK DESC" : "KRKMC,EMK DESC", DataViewRowState.CurrentRows);
            iTMax = dvT.Count;
            if (iTMax <= 0)
            {
                nDokState = AppC.RC_CANCEL;
                lstProt.Add("*** ��� �����������! ***");
            }
            dvZ.BeginInit();
            dvT.BeginInit();

            if (nDokState == AppC.RC_OK)
            {
                foreach (DataRowView dr in dvZ)
                {// ����� ���� ��������
                    dr["READYZ"] = NSI.READINESS.NO;
                }
                foreach (DataRowView dr in dvT)
                {// ����� ���� ���������� �����
                    dr["DEST"] = NSI.DESTINPROD.UNKNOWN;
                }
                lstProt.Add("<->----- ��� ------<->");
                while (i < iTMax)
                {

                    if ((int)dvT[i]["DEST"] != (int)NSI.DESTINPROD.UNKNOWN)
                    {// ������ ��������� ��� ����������
                        i++;
                        continue;
                    }

                    drC = dvT[i].Row;
                    // ��� �� ������ � ������?
                    xR = new RowObj(drC);

                    if (xR.AllFlags == (int)AppC.OBJ_IN_DROW.OBJ_NONE)
                    {
                        lstProt.Add("��� ���������/SSCC");
                        i++;
                        continue;
                    }
                    if (!xR.IsEAN)
                    {// ���� �� SSCC
                        sFlt = "";
                        if (xR.IsSSCCINT)
                            sFlt += String.Format("AND(SSCCINT='{0}')", xR.sSSCCINT);
                        if (xR.IsSSCC)
                            sFlt += String.Format("AND(SSCC='{0}')", xR.sSSCC);

                        DataView dvZSC = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf + sFlt, "SSCC,SSCCINT",
                            DataViewRowState.CurrentRows);
                        if (dvZSC.Count > 0)
                            dvZSC[0].Row["READYZ"] = NSI.READINESS.FULL_READY;
                        else
                        {// SSCC �� ������
                            lstProt.Add(String.Format("����.{0} {1}:��� ������", xR.sSSCCINT, xR.sSSCC));
                        }
                        i++;
                        continue;
                    }

                    sc.sEAN = (string)drC["EAN13"];
                    sc.sKMC = (string)drC["KMC"];
                    sc.nKrKMC = (int)drC["KRKMC"];

                    sc.bVes = ((int)(drC["SRP"]) > 0) ? true : false;

                    sc.fEmk = (FRACT)drC["EMK"];
                    sc.nParty = (string)drC["NP"];
                    sc.dDataIzg = DateTime.ParseExact((string)drC["DVR"], "yyyyMMdd", null);
                    sc.dDataGodn = DateTime.ParseExact((string)drC["DTG"], "yyyyMMdd", null);
                    sc.nTara = (int)drC["KRKT"];

                    sc.nRecSrc = (int)NSI.SRCDET.CR4CTRL;

                    //td1 = Environment.TickCount;

                    //iStart = dvZ.Find(sc.nKrKMC);
                    iStart = (xPars.PKeyIsGUID)?dvZ.Find(sc.sKMC):dvZ.Find(sc.nKrKMC);
                    if (iStart != -1)
                        nRet = EvalZVKMest(ref sc, dvZ, iStart, iZMax);
                    //nRet = LookAtZVK(ref sc, dvZ, iStart, iZMax);
                    else
                        nRet = AppC.RC_NOEAN;

                    //tc += (Environment.TickCount - td1);

                    iCur = -1;
                    if (nRet == AppC.RC_OK)
                    {// ���� ��� �������� ��� ����� �������
                        //td1 = Environment.TickCount;

                        //EvalEnteredVals(ref sc, sc.sKMC, sc.fEmk, sc.nParty, dvT, i, iTMax);

                        EvalEnteredKol(dvT, i, ref sc, sc.sKMC, sc.fEmk, out nM, out fV, true);

                        //td2 = Environment.TickCount;
                        //tc1 += (td2 - td1);

                        iCur = i;
                        nRet = EvalDiffZVK(ref sc, dvZ, dvT, lstProt, iStart, iZMax, ref iCur, iTMax, nM, fV);

                        //tc2 += (Environment.TickCount - td2);

                        if (nDokState != AppC.RC_CANCEL)
                        {
                            if (nRet != AppC.RC_OK)
                                nDokState = nRet;
                        }
                    }
                    else
                    {
                        switch (nRet)
                        {

                            case AppC.RC_NOEAN:
                                // ��� �����������
                                s1 = "";
                                xProt = "";
                                fE = -100;
                                break;
                            case AppC.RC_NOEANEMK:
                                // ������� �����������
                                s1 = "���.";
                                xProt = sc.fEmk;
                                fE = sc.fEmk;
                                break;
                            case AppC.RC_BADPARTY:
                                // ��� ������
                                s1 = "����.";
                                xProt = sc.nParty;
                                fE = sc.fEmk;
                                break;
                            default:
                                s1 = String.Format("���={0}", sc.fEmk);
                                xProt = String.Format("����-{0}", sc.nParty);
                                fE = sc.fEmk;
                                break;
                        }
                        nDokState = AppC.RC_CANCEL;

                        lstProt.Add(String.Format("_{0} {3} {1} {2}:��� ������", sc.nKrKMC, s1, xProt, sc.sEAN));
                        iCur = SetTTNState(dvT, ref sc, fE, NSI.DESTINPROD.USER, i, iTMax);
                    }
                    if (iCur != -1)
                        i = iCur;

                    i++;
                }

                //t2 = Environment.TickCount;

                lstProt.Add("<->---- ������ ----<->");
                for (i = 0; i < dvZ.Count; i++)
                {
                    if ((NSI.READINESS)dvZ[i]["READYZ"] != NSI.READINESS.FULL_READY)
                    {
                        nDokState = AppC.RC_CANCEL;
                        drC = dvZ[i].Row;
                        xR = new RowObj(drC);
                        try
                        {
                            if (xR.IsEAN)
                            {
                                s1 = ((NSI.READINESS)dvZ[i]["READYZ"] == NSI.READINESS.NO) ? "��� �����" : "��������";
                                if ((FRACT)drC["EMK"] > 0)
                                    lstProt.Add(String.Format("_{0} {3}:{2}-{1} �", (int)drC["KRKMC"], (int)drC["KOLM"], s1, drC["EAN13"]));
                                else
                                    lstProt.Add(String.Format("_{0} {}3:{2}-{1} ��", (int)drC["KRKMC"], (FRACT)drC["KOLE"], s1, drC["EAN13"]));
                            }
                            else
                            {
                                if (xR.IsSSCCINT || xR.IsSSCC)
                                {
                                    lstProt.Add(String.Format("����.{0} {1}:��� �����", xR.sSSCCINT, xR.sSSCC));
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (nDokState == AppC.RC_CANCEL)
            {
                drD["DIFF"] = NSI.DOCCTRL.ERRS;
                lstProt.Add("!!!===! ������ �������� !===!!!");
            }
            else if (nDokState == AppC.RC_WARN)
            {
                drD["DIFF"] = NSI.DOCCTRL.WARNS;
                lstProt.Add("== ��������� - �������������� ==");
            }
            else if (nDokState == AppC.RC_OK)
            {
                drD["DIFF"] = NSI.DOCCTRL.OK;
                lstProt.Add("=== ��������� - ��� ������ ===");
            }

            dvT.EndInit();
            dvZ.EndInit();

            //t3 = Environment.TickCount;
            //tsDiff = new TimeSpan(0, 0, 0, 0, t3 - t1);

            //lstProt.Add(String.Format("����� - {0}, ������ - {1}, ZVK-{2}, TTN-{3}, Diff-{4}",
            //    tsDiff.TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, t3 - t2).TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, tc).TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, tc1).TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, tc2).TotalSeconds));
            //MessageBox.Show(new TimeSpan(0, 0, 0, 0, tss).TotalSeconds.ToString());
        }

        // ��������� ��������
        // - �� ����� ���� (��� RC_NOEAN), fE = -100
        // - �� ����� ���� � ������ ������� (��� RC_NOEAN)
        private int SetTTNState(DataView dv, ref PSC_Types.ScDat sc, FRACT fE, NSI.DESTINPROD dSt, int i, int iMax)
        {
            //int tss1 = Environment.TickCount;
            int
                nLastI = i;

            if ((fE >= 0) && (dSt != NSI.DESTINPROD.USER))
            {
                dv[i]["DEST"] = dSt;
                nLastI = i;
            }
            else
            {
                while ((i < iMax) && ((string)dv[i]["KMC"] == sc.sKMC))
                {
                    if ((fE < 0) || (fE == (FRACT)dv[i]["EMK"]))
                    {
                        dv[i]["DEST"] = dSt;
                        nLastI = i;
                    }
                    i++;
                }
            }
            //tss += (Environment.TickCount - tss1);
            return (nLastI);
        }
        //int tss = 0;

        // ������� ����� ������� � ���������
        private int EvalDiffZVK(ref PSC_Types.ScDat sc, DataView dvZ, DataView dvT, List<string> lstProt,
            int iZ, int iZMax, ref int iT, int iTMax, int nMAll, FRACT fEAll)
        {
            bool
                bNeedSetZVK = false;
            int
                nRet = AppC.RC_OK,
                nM = 0;
            FRACT
                fV = 0;
            NSI.READINESS
                rpEmk = NSI.READINESS.NO;

            if (sc.fEmk > 0)
            {
                if (sc.nKolM_zvk > 0)
                {
                    bNeedSetZVK = true;


                    //nM = sc.nKolM_zvk - sc.nKolM_alrT;
                    //if ( (sc.nKolM_alr > 0) && (sc.nKolM_alrT == 0))
                    //    nM = sc.nKolM_zvk - sc.nKolM_alr;

                    if ((int)sc.lstAvailInZVK[0]["COND"] == (int)NSI.SPECCOND.NO)
                        nM = sc.nKolM_zvk - nMAll;
                    else
                    {
                        nM = sc.nKolM_zvk - sc.nKolM_alrT;
                        if ((sc.nKolM_alr > 0) && (sc.nKolM_alrT == 0))
                            nM = sc.nKolM_zvk - sc.nKolM_alr;
                    }

                    if (nM > 0)
                    {// ����-�� ��� ��������, �� ������ ������ �� ���������
                        nRet = AppC.RC_CANCEL;
                        rpEmk = NSI.READINESS.PART_READY;
                        lstProt.Add(String.Format("_{0}:���������-{1} �",
                            sc.nKrKMC, nM));
                    }
                    else
                    {
                        if (nM < 0)
                        {// ������� �� ������, ���������
                            nRet = AppC.RC_WARN;
                            lstProt.Add(String.Format(" {0}:������ {1} �",
                                sc.nKrKMC, Math.Abs(nM)));
                        }
                        rpEmk = NSI.READINESS.FULL_READY;
                    }
                }
                else
                {
                    nRet = AppC.RC_CANCEL;
                    rpEmk = NSI.READINESS.PART_READY;
                    lstProt.Add(String.Format("_{0}:��� � ������-{1} �",
                        sc.nKrKMC, (sc.nKolM_alr + sc.nKolM_alrT)));
                }

                // ��������� ��������� � ��������
                try
                {
                    iT = SetTTNState(dvT, ref sc, sc.fEmk, NSI.DESTINPROD.PARTZ, iT, iTMax);
                    if (bNeedSetZVK == true)
                    {
                        SetZVKState(dvZ, ref sc, rpEmk, iZ, iZMax);
                    }
                }
                catch { }
            }
            else
            {
                if ((sc.fKolE_zvk > 0) || ((sc.fKolE_alr + sc.fKolE_alrT) > 0))
                {// ���� ��� ������ ��� �����
                    if (sc.fKolE_zvk > 0)
                    {
                        bNeedSetZVK = true;
                        //fV = sc.fKolE_zvk - sc.fKolE_alrT;
                        //if ((sc.fKolE_alr > 0) && (sc.fKolE_alrT == 0))
                        //    fV = sc.fKolE_zvk - sc.fKolE_alr;

                        if ((int)sc.lstAvailInZVK[0]["COND"] == (int)NSI.SPECCOND.NO)
                            fV = sc.fKolE_zvk - fEAll;
                        else
                        {
                            fV = sc.fKolE_zvk - sc.fKolE_alrT;
                            if ((sc.fKolE_alr > 0) && (sc.fKolE_alrT == 0))
                                fV = sc.fKolE_zvk - sc.fKolE_alr;
                        }
                        if (fV > 0)
                        {// ����-�� ��� ��������
                            nRet = AppC.RC_CANCEL;
                            rpEmk = NSI.READINESS.PART_READY;
                            lstProt.Add(String.Format("_{0}:���������-{1} ��",
                                sc.nKrKMC, fV));
                        }
                        else
                        {
                            if (fV < 0)
                            {// ������� �� ���������, ���������
                                nRet = AppC.RC_WARN;
                                lstProt.Add(String.Format(" {0}:������ {1} ��",
                                    sc.nKrKMC, Math.Abs(fV)));
                            }
                            rpEmk = NSI.READINESS.FULL_READY;
                        }
                    }
                    else
                    {
                        nRet = AppC.RC_CANCEL;
                        rpEmk = NSI.READINESS.PART_READY;
                        lstProt.Add(String.Format("_{0}:��� � ������-{1} ��",
                            sc.nKrKMC, (sc.fKolE_alr + sc.fKolE_alrT)));
                    }

                    try
                    {
                        iT = SetTTNState(dvT, ref sc, sc.fEmk, NSI.DESTINPROD.PARTZ, iT, iTMax);
                        if (bNeedSetZVK == true)
                        {
                            SetZVKState(dvZ, ref sc, rpEmk, iZ, iZMax);
                        }
                    }
                    catch { }
                }
            }

            return (nRet);
        }

        private void SetZVKState(DataView dv, ref PSC_Types.ScDat sc, NSI.READINESS rpE, int i, int nZMax)
        {
            if (sc.lstAvailInZVK.Count > 0)
            {
                foreach (DataRow drl in sc.lstAvailInZVK)
                {
                    drl["READYZ"] = rpE;
                }
            }
            else
            {
                while ((i < nZMax) && ((string)dv[i]["KMC"] == sc.sKMC))
                {
                    if (sc.fEmk == (FRACT)dv[i]["EMK"])
                        dv[i]["READYZ"] = rpE;
                    i++;
                }
            }
        }








        /// �������� ����� ��������� �� ������������ ������
        private int ControlDocZVK(DataRow drD, List<string> lstProt)
        {
            return (ControlDocZVK(drD, lstProt, ""));
        }



        /// �������� ����� ��������� �� ������������ ������
        private int ControlDocZVK(DataRow drD, List<string> lstProt, string s1Pallet)
        {
            bool
                bGood_KMC,
                bIsKMPL;
            int
                i = 0,
                nM,
                iStart,
                iCur,
                iCurSaved,
                iTMax, iZMax,
                nDokState = AppC.RC_OK,
                nM_KMC,
                nBad_NPP,
                nRet;
            string
                s1,
                s2,
                sOldKMC,
                sKMC,
                sFlt;
            FRACT
                fE,
                fE_KMC,
                fV = 0;
            object
                xProt;

            DataRow
                drC;
            DataView
                dv,
                dvZ, dvT;
            RowObj
                xR;

            //TimeSpan tsDiff;
            //int t1 = Environment.TickCount, t2, t3, td1, td2, tc = 0, tc1 = 0, tc2 = 0;
            //t2 = t1;

            if (drD == null)
                drD = xCDoc.drCurRow;

            bIsKMPL = (xCDoc.xDocP.TypOper == AppC.TYPOP_KMPL) ? true : false;

            string sRf = String.Format("(SYSN={0})", drD["SYSN"]);

            PSC_Types.ScDat sc = new PSC_Types.ScDat(new BarcodeScannerEventArgs(BCId.Code128, ""));
            lstProt.Add(HeadLineCtrl(drD));

            // ���� ��� ��������� �������� ����������
            drD["DIFF"] = NSI.DOCCTRL.UNKNOWN;

            // ��� ��������� �� ������ �� ���������

            //dvZ = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "KMC", DataViewRowState.CurrentRows);
            dvZ = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf, "KMC", DataViewRowState.CurrentRows);

            iZMax = dvZ.Count;
            if (iZMax <= 0)
            {
                nDokState = AppC.RC_CANCEL;
                lstProt.Add("*** ������ �����������! ***");
            }
            else
                bZVKPresent = true;

            /// ��� ��������� �� ��� �� ���������
            //dvT = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "KMC,EMK DESC", DataViewRowState.CurrentRows);

            if (s1Pallet.Length > 0)
                sRf = String.Format("{0} AND (SSCC='{1}')", sRf, s1Pallet);

            dvT = new DataView(xNSI.DT[NSI.BD_DOUTD].dt, sRf, "KMC,EMK DESC", DataViewRowState.CurrentRows);
            iTMax = dvT.Count;
            if (iTMax <= 0)
            {
                nDokState = AppC.RC_CANCEL;
                lstProt.Add("*** ��� �����������! ***");
            }
            dvZ.BeginInit();
            dvT.BeginInit();

            if (nDokState == AppC.RC_OK)
            {
                foreach (DataRowView dr in dvZ)
                {// ����� ���� ��������
                    dr["READYZ"] = NSI.READINESS.NO;
                }
                foreach (DataRowView dr in dvT)
                {// ����� ���� ���������� �����
                    dr["DEST"] = NSI.DESTINPROD.UNKNOWN;
                    dr["NPP_ZVK"] = -1;
                }


                lstProt.Add("<->----- ��� ------<->");

                sOldKMC = "";
                fE_KMC = 0;
                nM_KMC = 0;
                while (i < iTMax)
                {

                    if ((int)dvT[i]["DEST"] != (int)NSI.DESTINPROD.UNKNOWN)
                    {// ������ ��������� ��� ����������
                        i++;
                        continue;
                    }

                    drC = dvT[i].Row;
                    // ��� �� ������ � ������?
                    xR = new RowObj(drC);

                    if (xR.AllFlags == (int)AppC.OBJ_IN_DROW.OBJ_NONE)
                    {
                        lstProt.Add("��� ���������/SSCC");
                        i++;
                        continue;
                    }
                    if (!xR.IsEAN)
                    {// ���� �� SSCC
                        sFlt = "";
                        if (xR.IsSSCCINT)
                            sFlt += String.Format("AND(SSCCINT='{0}')", xR.sSSCCINT);
                        if (xR.IsSSCC)
                            sFlt += String.Format("AND(SSCC='{0}')", xR.sSSCC);

                        DataView dvZSC = new DataView(xNSI.DT[NSI.BD_DIND].dt, sRf + sFlt, "SSCC,SSCCINT",
                            DataViewRowState.CurrentRows);
                        if (dvZSC.Count > 0)
                            dvZSC[0].Row["READYZ"] = NSI.READINESS.FULL_READY;
                        else
                        {// SSCC �� ������
                            lstProt.Add(String.Format("����.{0} {1}:��� ������", xR.sSSCCINT, xR.sSSCC));
                        }
                        i++;
                        continue;
                    }

                    sc.sEAN = (string)drC["EAN13"];
                    sc.sKMC = (string)drC["KMC"];
                    sc.nKrKMC = (int)drC["KRKMC"];

                    sc.bVes = ((int)(drC["SRP"]) > 0) ? true : false;

                    sc.fEmk = (FRACT)drC["EMK"];
                    sc.nParty = (string)drC["NP"];
                    sc.dDataIzg = DateTime.ParseExact((string)drC["DVR"], "yyyyMMdd", null);
                    sc.dDataGodn = DateTime.ParseExact((string)drC["DTG"], "yyyyMMdd", null);
                    //sc.nTara = (string)drC["KRKT"];
                    //sc.nTara = (string)drC["KTARA"];

                    sc.nRecSrc = (int)NSI.SRCDET.CR4CTRL;

                    //td1 = Environment.TickCount;

                    iStart = dvZ.Find(sc.sKMC);
                    //iStart = dvZ.Find(sc.sKMC);
                    if (iStart != -1)
                        nRet = EvalZVKMest(ref sc, dvZ, iStart, iZMax);
                    //nRet = LookAtZVK(ref sc, dvZ, iStart, iZMax);
                    else
                        nRet = AppC.RC_NOEAN;

                    //tc += (Environment.TickCount - td1);

                    iCur = -1;
                    if (nRet == AppC.RC_OK)
                    {// ���� ��� �������� ��� ����� �������
                        //td1 = Environment.TickCount;

                        if ((bIsKMPL) || true)
                        {
                            sc.nMest = (int)drC["KOLM"];
                            sc.fVsego = (FRACT)drC["KOLE"];
                            EvalZVKStateNew(ref sc, drC);

                            if (sOldKMC != sc.sKMC)
                            {// ����� ����
                                iCurSaved = i;

                                //EvalEnteredKol(dvT, i, ref sc, sc.sKMC, sc.fEmk, out nM, out fV, true);

                                iCur = i;

                                //nRet = EvalDiffZVK(ref sc, dvZ, dvT, lstProt, iStart, iZMax, ref iCur, iTMax, nM, fV);

                                //if (nDokState != AppC.RC_CANCEL)
                                //{
                                //    if (nRet != AppC.RC_OK)
                                //        nDokState = nRet;
                                //}
                                sOldKMC = sc.sKMC;
                            }
                        }
                        else
                        {
                            EvalEnteredKol(dvT, i, ref sc, sc.sKMC, sc.fEmk, out nM, out fV, true);

                            //td2 = Environment.TickCount;
                            //tc1 += (td2 - td1);

                            iCur = i;
                            nRet = EvalDiffZVK(ref sc, dvZ, dvT, lstProt, iStart, iZMax, ref iCur, iTMax, nM, fV);

                            //tc2 += (Environment.TickCount - td2);

                            if (nDokState != AppC.RC_CANCEL)
                            {
                                if (nRet != AppC.RC_OK)
                                    nDokState = nRet;
                            }
                        }
                    }
                    else
                    {
                        switch (nRet)
                        {

                            case AppC.RC_NOEAN:
                                // ��� �����������
                                s1 = "";
                                xProt = "";
                                fE = -100;
                                break;
                            case AppC.RC_NOEANEMK:
                                // ������� �����������
                                s1 = "���.";
                                xProt = sc.fEmk;
                                fE = sc.fEmk;
                                break;
                            case AppC.RC_BADPARTY:
                                // ��� ������
                                s1 = "����.";
                                xProt = sc.nParty;
                                fE = sc.fEmk;
                                break;
                            default:
                                s1 = String.Format("���={0}", sc.fEmk);
                                xProt = String.Format("����-{0}", sc.nParty);
                                fE = sc.fEmk;
                                break;
                        }
                        nDokState = AppC.RC_CANCEL;

                        lstProt.Add(String.Format("_{0} {1} {2}:��� ������", sc.nKrKMC, s1, xProt));
                        iCur = SetTTNState(dvT, ref sc, fE, NSI.DESTINPROD.USER, i, iTMax);
                    }
                    if (iCur != -1)
                        i = iCur;

                    i++;
                }


                if (s1Pallet.Length > 0)
                    return (nDokState);


                //t2 = Environment.TickCount;

                lstProt.Add("<->---- ������ ----<->");
                sOldKMC = "";
                fE_KMC = 0;
                nM_KMC = 0;
                bGood_KMC = true;
                nBad_NPP = 0;
                for (i = 0; i < dvZ.Count; i++)
                {
                    drC = dvZ[i].Row;

                    if (((int)drC["KOLM"] == 0) && ((FRACT)drC["KOLE"] == 0))
                        continue;

                    xR = new RowObj(drC);
                    sKMC = (string)drC["KMC"];

                    if (sOldKMC != sKMC)
                    {// ����� ����
                        if (nBad_NPP > 1)
                        {
                            Total4KMC(dvZ[i - 1].Row, sOldKMC, true, true, lstProt, nM_KMC, fE_KMC);
                        }
                        sOldKMC = sKMC;
                        fE_KMC = 0;
                        nM_KMC = 0;
                        bGood_KMC = true;
                        nBad_NPP = 0;
                    }

                    try
                    {
                        if (xR.IsEAN)
                        {
                            if ((FRACT)drC["EMK"] > 0)
                                nM_KMC += (int)(drC["KOLM"]);
                            else
                                fE_KMC += (FRACT)(drC["KOLE"]);

                            if ((NSI.READINESS)drC["READYZ"] != NSI.READINESS.FULL_READY)
                            {
                                nDokState = AppC.RC_CANCEL;
                                bGood_KMC = false;
                                nBad_NPP++;
                                Total4KMC(drC, sKMC, xR.IsEAN, false, lstProt, (int)(drC["KOLM"]), (FRACT)(drC["KOLE"]));
                            }
                        }
                        else
                        {
                            if (xR.IsSSCCINT || xR.IsSSCC)
                            {
                                if ((NSI.READINESS)drC["READYZ"] != NSI.READINESS.FULL_READY)
                                    lstProt.Add(String.Format("����.{0} {1}:��� �����", xR.sSSCCINT, xR.sSSCC));
                            }
                        }
                    }
                    catch
                    {
                    }


                }
                if (i > 0)
                {
                    if (nBad_NPP > 1)
                    {
                        i--;
                        drC = dvZ[i].Row;
                        xR = new RowObj(drC);
                        //sKMC = (string)drC["KMC"];
                        Total4KMC(drC, sOldKMC, xR.IsEAN, true, lstProt, nM_KMC, fE_KMC);
                    }
                }

            }

            if (nDokState == AppC.RC_CANCEL)
            {
                drD["DIFF"] = NSI.DOCCTRL.ERRS;
                lstProt.Add("!!!===! ������ �������� !===!!!");
            }
            else if (nDokState == AppC.RC_WARN)
            {
                drD["DIFF"] = NSI.DOCCTRL.WARNS;
                lstProt.Add("== ��������� - �������������� ==");
            }
            else if (nDokState == AppC.RC_OK)
            {
                drD["DIFF"] = NSI.DOCCTRL.OK;
                lstProt.Add("=== ��������� - ��� ������ ===");
            }

            dvT.EndInit();
            dvZ.EndInit();

            //t3 = Environment.TickCount;
            //tsDiff = new TimeSpan(0, 0, 0, 0, t3 - t1);

            //lstProt.Add(String.Format("����� - {0}, ������ - {1}, ZVK-{2}, TTN-{3}, Diff-{4}",
            //    tsDiff.TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, t3 - t2).TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, tc).TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, tc1).TotalSeconds,
            //    new TimeSpan(0, 0, 0, 0, tc2).TotalSeconds));
            //MessageBox.Show(new TimeSpan(0, 0, 0, 0, tss).TotalSeconds.ToString());
            return (nDokState);
        }

        private void Total4KMC(DataRow drC, string sKMC, bool bIsProd, bool bFullKMC, List<string> lstProt, int nM_KMC, FRACT fE_KMC)
        {
            int
                nKrKMC = 0,
                nDiff,
                nM;
            string
                sFlt,
                s2,
                s1;
            FRACT
                fDiff,
                fV;
            DataView
                dv;


            try
            {
                if (bIsProd)
                {
                    nKrKMC = (int)drC["KRKMC"];
                    sFlt = String.Format("{0} AND (KMC='{1}')", xCDoc.DefDetFilter(), sKMC);
                    if (!bFullKMC)
                        sFlt += String.Format(" AND (NPP_ZVK={0})", drC["NPP"]);

                    dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt,
                        sFlt, "EMK, DVR, NP DESC", DataViewRowState.CurrentRows);

                    nM = 0;
                    fV = 0;
                    foreach (DataRowView drv in dv)
                    {
                        if ((FRACT)(drv.Row["EMK"]) > 0)
                            nM += (int)(drv.Row["KOLM"]);
                        else
                            fV += (FRACT)(drv.Row["KOLE"]);
                    }

                    nDiff = nM_KMC - nM;
                    fDiff = fE_KMC - fV;

                    s1 = ((NSI.READINESS)drC["READYZ"] == NSI.READINESS.NO) ? "��� �����" : "��������";
                    s2 = (bFullKMC) ? "\x3A3" : drC["NPP"].ToString();
                    if ((FRACT)drC["EMK"] > 0)
                        lstProt.Add(String.Format("({5})_{0}:{1}:{3}-{2}={4}�", nKrKMC, s1, nM, nM_KMC, nDiff, s2));
                    else
                        lstProt.Add(String.Format("({5})_{0}:{1}:{3}-{2}={4}��", nKrKMC, s1, fV, fE_KMC, fDiff, s2));
                }
            }
            catch
            {
            }

        }

        private object ID_Det(DataRow dr, ref PSC_Types.ScDat sc)
        {
            object
                ret;
            switch (xPars.ID4Protocol)
            {
                case AppC.IDDET4CTRL.KRKMC:
                    ret = (dr == null)?sc.nKrKMC:dr["KRKMC"];
                    break;
                case AppC.IDDET4CTRL.NPP:
                    ret = (dr == null) ? "" : dr["NPP"];
                    break;
                default:
                    ret = (dr == null) ? "" : "";
                    break;
            }
            return (ret);
        }


        private int ControlBeforeUpload()
        {
            int
                nRezCtrl,
                nRet = AppC.RC_OK;

            if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
            {
                xInf = new List<string>();
                nRezCtrl = AppC.RC_OK;
                if (bZVKPresent)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        nRezCtrl = ControlDocZVK(null, xInf, "");
                    }
                    finally
                    {
                        Cursor.Current = Cursors.Default;
                    }
                }

                if (nRezCtrl != AppC.RC_OK)
                {
                    DialogResult dr = MessageBox.Show("�������� �������� (Enter)?\n(ESC) - ���������\n(SHIFT-F1 - ��������)",
                        "�������������� ������!",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    //if (dr == DialogResult.OK)
                    //    bGoodData = false;

                    if (dr != DialogResult.OK)
                    {
                        nRezCtrl = AppC.RC_OK;
                    }
                }
                nRet = nRezCtrl;
            }
            return (nRet);
        }




    }
}
