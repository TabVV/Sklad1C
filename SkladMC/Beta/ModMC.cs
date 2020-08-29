using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;

using PDA;
using PDA.Service;
using PDA.BarCode;
using ScannerAll;

using FRACT = System.Decimal;

namespace PDA.Service
{
    public partial class AppC
    {
    }

}

namespace SkladRM
{
    public partial class MainF : Form
    {



        //***==================== ProceedScan
        private bool EANGTIN(ref PSC_Types.ScDat s, string sBC)
        {
            bool
                ret = true;
            string
                sEANP = "";

            try
            {
                if (sBC.Length == 14)
                {// это ITF
                    s.tTyp = AppC.TYP_TARA.TARA_TRANSP;
                    s.sGTIN = sBC;
                    if (SetKMCOnGTIN(ref s))
                    {
                        return (AppC.RC_OKB);
                    }
                    sEANP = sBC.Substring(1, 12);
                    s.sEAN = Srv.CheckSumModul10(sEANP);
                }
                else
                {
                    s.tTyp = AppC.TYP_TARA.TARA_POTREB;
                    if (sBC.Length == 13)
                    {
                        sEANP = sBC.Substring(0, 12);
                        s.sEAN = Srv.CheckSumModul10(sEANP);
                    }
                    else
                        s.sEAN = sBC;
                }
                ret = xNSI.Connect2MC(s.sEAN, 0, -1, ref s);
            }
            catch
            {
                ret = false;
            }
            return (ret);
        }



        /// Вход в режим создания/корректировки детальной строки **********************
        /// - установка флага редактирования
        /// - доступных полей
        private void EditBeginDet(int nReg, AppC.VerifyEditFields dgV)
        {
            bool
                bFlag;
            int
                nProdType = (scCur.bVes == true) ? AppC.PRODTYPE_VES : AppC.PRODTYPE_SHT;
            Control
                xBeCur = null;

            nCurVvodState = nReg;
            SetEditMode(true);
            aEdVvod = new AppC.EditListC(dgV);
            aEdVvod.AddC(tKMC);
            aEdVvod.AddC(tEAN);
            aEdVvod.AddC(tDatIzg);
            aEdVvod.AddC(tDatMC);
            aEdVvod.AddC(tParty);
            aEdVvod.AddC(tMest);
            aEdVvod.AddC(tEmk);
            aEdVvod.AddC(tVsego);
            aEdVvod.AddC(tKolPal, (nReg == AppC.F_ADD_REC) ? true : false);
            //aEdVvod.AddC(tPrzvFil, (nReg == AppC.F_ADD_REC) ? true : false);

            foreach (AppPars.FieldDef fd in xPars.aFields)
            {
                // Общий случай
                bFlag =
                    (nReg == AppC.F_CHG_REC) ? fd.aVes[nProdType].bEdit :
                    (nReg == AppC.F_ADD_SCAN) ? fd.aVes[nProdType].bScan :
                    fd.aVes[nProdType].bVvod;

                // В чужих кодах сведений о партии и дате выработки может не быть
                if ((scCur.bAlienMC) && (fd.sFieldName == "tParty"))
                {
                    //bFlag = (scCur.bNewAlienPInf || (scCur.nTypVes == AppC.TYP_VES_PAL)) ? true : false;
                    bFlag = (scCur.bNewAlienPInf || (scCur.tTyp == AppC.TYP_TARA.TARA_PODDON)) ? true : false;
                }

                if (fd.sFieldName == "tDatMC")
                {
                    // для своих ЕАН-13 дату вводить не надо, а в своих номер партии имеется
                    if (scCur.dDataGodn == DateTime.MinValue)
                    {
                        bFlag = true;
                    }
                }

                if (scCur.bFindNSI && (scCur.tTyp != AppC.TYP_TARA.TARA_PODDON))
                {
                    if ((fd.sFieldName == "tParty") && (scCur.nParty.Length == 0))
                    {
                        bFlag = true;
                    }

                }

                if (fd.sFieldName == "tEmk")
                {
                    if (scCur.xEmks.Count > 1)
                        bFlag = false;

                    if ((scCur.tTyp != AppC.TYP_TARA.TARA_POTREB) &&
                        (scCur.fEmk == 0))
                    {
                        bFlag = true;
                    }

                }
                aEdVvod.SetAvail(FieldByName(fd.sFieldName), bFlag);
            }

            if (nReg == AppC.F_ADD_REC)
                xBeCur = tKMC;

            // дата изготовления
            if (scCur.dDataIzg == DateTime.MinValue)
            {// не определена
                if (scCur.dDataGodn == DateTime.MinValue)
                {
                    aEdVvod.SetAvail(tDatIzg, true);
                    if ((tKMC.Enabled == false) && (tEAN.Enabled == false))
                        xBeCur = tDatIzg;
                }
            }
            else
                aEdVvod.SetAvail(tDatIzg, false);

            // дата годности
            if (scCur.dDataGodn == DateTime.MinValue)
            {
                if (scCur.dDataIzg == DateTime.MinValue)
                    aEdVvod.SetAvail(tDatMC, true);
                else
                    aEdVvod.SetAvail(tDatMC, false);
            }
            else
                aEdVvod.SetAvail(tDatMC, false);

            //if ((scCur.s.Length == 16) &&
            //    ((scCur.tTyp == AppC.TYP_TARA.TARA_TRANSP) ||
            //     (scCur.tTyp == AppC.TYP_TARA.TARA_PODDON)))
            //{// этикетка Ногинска 
            //    if (aEdVvod.Contains(tDatMC) && (!tDatMC.Enabled))
            //        tDatMC.Enabled = true;
            //}

            switch (scCur.tTyp)
            {
                case AppC.TYP_TARA.TARA_POTREB:
                    scCur.fEmk = 0;
                    tEmk.Text = "";
                    if (!scCur.bVes)
                    {
                        tMest.Enabled = false;
                        tVsego.Enabled = true;
                    }
                    else
                    {
                        tMest.Enabled = true;
                        if (scCur.fVsego > 0)
                            tVsego.Enabled = false;
                    }
                    break;
                case AppC.TYP_TARA.TARA_TRANSP:
                    if (xBeCur == null)
                        xBeCur = tMest;
                    break;
                case AppC.TYP_TARA.TARA_PODDON:
                    tMest.Enabled = true;
                    tKolPal.Text = "1";
                    scCur.nPalet = 1;
                    if (!scCur.bVes)
                    {
                        aEdVvod.SetAvail(tKolPal, true);
                        xBeCur = tKolPal;
                    }
                    break;
            }

            if (scCur.bVes)
            {
                if (scCur.fVsego > 0)
                    tVsego.Enabled = false;
            }


            //if (scCur.bFindNSI)
            //{
            //    if (!scCur.bSetAccurCode)
            //    {// производственная площадка неизвестна
            //        aEdVvod.SetAvail(tPrzvFil, true);
            //    }
            //}

            // партия пока не вводится/редактируется
            ///--- 21.06.17 - определим параметрами
            //aEdVvod.SetAvail(tParty, false);

            if ((xBeCur != null) && (xBeCur.Enabled == true))
                aEdVvod.SetCur(xBeCur);
            else
                aEdVvod.WhichSetCur();

            nDefMest = scCur.nMest;
            fDefEmk = scCur.fEmk;
            fDefVsego = scCur.fVsego;
            bMestChanged = false;

        }





    }

}
