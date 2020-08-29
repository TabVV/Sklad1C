using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.Xml;
using System.IO;
using System.Threading;

using SavuSocket;
using PDA.Service;
using ScannerAll;

using FRACT = System.Decimal;

namespace SkladRM
{
    //public delegate int UpLoad2Srv();
    //public delegate void LoadFromSrv(Stream sTypDoc, Dictionary<string,string> aC, ref string sE);

    public delegate void LoadFromSrv(SocketStream s, Dictionary<string, string> aC, DataSet ds,
                                     ref string sE, int nRetSrv);

    public partial class MainF : Form
    {
        //private SocketStream ssWrite;

        // отображение уровня сигнала
        private void pnLoadDocG_EnabledChanged(object sender, EventArgs e)
        {
            //bool 
            //    bShowNow = ((Control)sender).Enabled && xBCScanner.WiFi.IsEnabled;
            bool bShowNow = ((Control)sender).Enabled && xBCScanner.WiFi.IsShownState;
            xBCScanner.WiFi.ShowWiFi(pnLoadDocG, bShowNow);
        }


        // формирование массива строк для выгрузки
        private DataRow[] PrepDataArrForUL(int nReg)
        {
            int 
                nRet = AppC.RC_OK;
            string 
                sRf = "";
            DataRow[] 
                ret = null;

            if (nReg == AppC.UPL_CUR)
            {
                if ((!xPars.UseAdr4DocMode) && (!xPars.OpAutoUpl))
                {
                    if (xCDoc.xDocP.TypOper == AppC.TYPOP_DOCUM)
                    {
                        if (((int)xCDoc.drCurRow["SOURCE"] == NSI.DOCSRC_UPLD) && (!xPars.bUseSrvG))
                        {
                            string sErr = "Уже выгружен!";
                            DialogResult dr;
                            dr = MessageBox.Show("Отменить выгрузку (Enter)?\r\n (ESC) - выгрузить повторно",
                            sErr,
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                            if (dr == DialogResult.OK)
                            {
                                nRet = AppC.RC_CANCEL;
                            }
                            else
                                nRet = AppC.RC_OK;
                        }
                    }
                }
                if (nRet == AppC.RC_OK)
                {
                    ret = new DataRow[] { xCDoc.drCurRow };
                    xCUpLoad.naComms = new List<int>();
                    xCUpLoad.naComms.Add( (int)xCDoc.drCurRow["TD"] );
                }
            }
            else if (nReg == AppC.UPL_ALL)
            {
                // фильтр - текущий для Grid документов + невыгруженные
                sRf = xNSI.DT[NSI.BD_DOCOUT].dt.DefaultView.RowFilter;
                if (sRf != "")
                {
                    sRf = "(" + sRf + ")AND";
                }
                sRf += String.Format("(SOURCE<>{0})", NSI.DOCSRC_UPLD);
                ret = PrepForAll(sRf);
            }
            else if (nReg == AppC.UPL_FLT)
            {
                //sRf = FiltForDoc(xCUpLoad.SetFiltInRow(xNSI));
                sRf = xCUpLoad.SetFiltInRow();
                ret = PrepForAll(sRf);
            }
            return (ret);
        }


        private DataRow[] PrepForAll(string sRf)
        {
            // все неотгруженные документы
            DataView dv = new DataView(xNSI.DT[NSI.BD_DOCOUT].dt, sRf, "", DataViewRowState.CurrentRows);
            DataRow[] drA = new DataRow[dv.Count];
            xCUpLoad.naComms = new List<int>();
            for (int i = 0; i < dv.Count; i++)
            {
                drA.SetValue(dv[i].Row, i);
                xCUpLoad.naComms.Add( (int)drA[i]["TD"]);

            }
            return (drA);

        }


        private string UpLoadDoc(ServerExchange xSExch, ref int nR)
        {
            int i,
                nRet = AppC.RC_OK;
            string nComm, 
                sErr = "",
                sAllErr = "";
            DataSet dsTrans;
            DataRow[] drAUpL = null;
            LoadFromSrv 
                dgL = null;

            try
            {
                drAUpL = PrepDataArrForUL(xCUpLoad.ilUpLoad.CurReg);
                if (drAUpL != null)
                {
                    if (xCUpLoad.sCurUplCommand != AppC.COM_CKCELL)
                        dgL = new LoadFromSrv(SetUpLoadState);
                    for (i = 0; i < drAUpL.Length; i++)
                    {

                        dsTrans = xNSI.MakeWorkDataSet(xNSI.DT[NSI.BD_DOCOUT].dt,
                                  xNSI.DT[NSI.BD_DOUTD].dt, new DataRow[] { drAUpL[i] }, null, xSm, xCUpLoad);
                        if (dsTrans.Tables[NSI.BD_DOUTD].Rows.Count > 0)
                        {
                            nRet = AppC.RC_OK;
                            nComm = AppC.COM_VDOC;
                            if (xCUpLoad.sCurUplCommand.Length == 0)
                                xCUpLoad.sCurUplCommand = nComm;

                            sErr = xSExch.ExchgSrv(xCUpLoad.sCurUplCommand, "", "", dgL, dsTrans, ref nRet, 600);

                            if ((xSExch.ServerRet == AppC.RC_OK) && (sErr != "OK"))
                                nRet = AppC.RC_HALFOK;
                            if (nRet != AppC.RC_OK)
                            {
                                nR = nRet;
                                sAllErr += sErr + "\n";
                            }
                        }
                        else
                            Srv.ErrorMsg(String.Format("Тип {0} №{1} от {2} -\nнет данных!", drAUpL[i]["TD"], drAUpL[i]["NOMD"], drAUpL[i]["DT"]));
                    }
                }
                else
                {
                    nRet = AppC.RC_NODATA;
                    sErr = "Нет данных для передачи";
                }
            }
            catch (Exception)
            {
                nRet = AppC.RC_NODATA;
                sErr = "Ошибка подготовки";
            }
            if (sAllErr.Length == 0)
            {
                nR = nRet;
                sAllErr = sErr;
            }
            return (sAllErr);
        }

        private void SetUpLoadState(SocketStream stmX, Dictionary<string, string> aC,
            DataSet dsU, ref string sErr, int nRetSrv)
        {
            DataView dv;
            DataRow dr;

            foreach (DataRow drT in dsU.Tables[0].Rows)
            {
                dr = xNSI.DT[NSI.BD_DOCOUT].dt.Rows.Find(new object[] { (int)drT["SYSN"] });
                if (null != dr)
                    dr["SOURCE"] = NSI.DOCSRC_UPLD;
                foreach (DataRow drD in dsU.Tables[1].Rows)
                {
                    dv = new DataView(xNSI.DT[NSI.BD_DOUTD].dt,
                        String.Format("(ID={0})", (int)drD["ID"]), "", DataViewRowState.CurrentRows);
                    dv[0].Row["STATE"] = AppC.OPR_STATE.OPR_TRANSFERED;
                }

            }

            sErr = "OK - Передача завершена";
        }

        private int AddZ(CurLoad xCL, ref string sErr)
        {
            return(AddZ(xCL, ref sErr, false));
        }


        // добавление полученных заявок в рабочие таблицы
        private int AddZ(CurLoad xCL, ref string sErr, bool bIsTask)
        {
            int 
                nRet = AppC.RC_OK,
                nNPP,
                nM = 0;
            string 
                s;
            PSC_Types.ScDat 
                sD = new PSC_Types.ScDat();
            //object xNewKey;

            DataSet 
                ds = xCL.dsZ;
            DataRow 
                drMDoc;
            DataRow[] 
                //drAKMC,
                drDetZ, 
                drMDetZ;
            DataTable 
                dtDocs = xNSI.DT[NSI.BD_DOCOUT].dt,
                dtZVK = xNSI.DT[NSI.BD_DIND].dt;

            // пока удалим связи и таблицу ТТН из DataSet
            //DataRelation dRel = xNSI.dsM.Relations[NSI.REL2TTN];

            //xNSI.dsM.Relations.Remove(NSI.REL2TTN);
            //xNSI.dsM.Tables.Remove(xNSI.dsM.Tables[NSI.BD_DOUTD]);

            if ((xCL.ilLoad.CurReg == AppC.UPL_CUR) && (!bIsTask))
            {// заявка только для текущего документа

                drMDoc = xCDoc.drCurRow;
                //object[] xCur = drMDoc.ItemArray;

                if (ds.Tables[NSI.BD_ZDET].Rows.Count > 0)
                {// имеются детальные строки для загрузки
                    // а это загруженные ранее
                    if (!bIsTask)
                    {// предыдущие задания не удаляем
                        drDetZ = drMDoc.GetChildRows(xNSI.dsM.Relations[NSI.REL2ZVK]);
                        if (drDetZ.Length > 0)
                        {// их все и удаляем
                            foreach (DataRow drDel in drDetZ)
                            {
                                xNSI.dsM.Tables[NSI.BD_DIND].Rows.Remove(drDel);
                            }
                        }
                    }
                    // установка существующего ключа (SYSN)
                    // каскадно должен измениться и в детальных
                    ds.Tables[NSI.BD_ZDOC].Rows[0]["SYSN"] = drMDoc["SYSN"];
                    nM = 0;
                    nNPP = 1;
                    foreach (DataRow drA in ds.Tables[NSI.BD_ZDET].Rows)
                    {
                        nM += SetOneDetZ(ref sD, dtZVK, ds, drA, drMDoc, ref nNPP);
                        nNPP++;
                    }
                    drMDoc["MESTZ"] = nM;
                    drMDoc["SOURCE"] = NSI.DOCSRC_LOAD;
                    drMDoc["CHKSSCC"] = 0;

                }
                else
                {
                    sErr = "Детальные строки не найдены!";
                    nRet = AppC.RC_CANCEL;
                }
                //if (drMDoc["SOURCE"] == NSI.DOCSRC_LOAD)
                //{// этот документ загружался
                //    nPKey = drMDoc["SYSN"];
                //    dr = ds.Tables[0].Rows.Find(new object[] { nPKey });
                //}


                //if (dr != null)
                //{// присутствует такой документ в заявках
                //    drDetZ = dr.GetChildRows(ds.Relations[0]);
                //    if ((drDetZ != null) && (drDetZ.Length > 0))
                //    {// имеются детальные строки заявки




                //        nRet = AddOneZ(dt, xCDoc.drCurRow, xCur, dtD, drDetZ);
                //    }
                //    else
                //    {
                //        Srv.ErrorMsg("Детальные строки не найдены!");
                //        return (AppC.RC_CANCEL);
                //    }
                //}
                //else
                //{
                //    Srv.ErrorMsg("Заявка не найдена!");
                //    return (AppC.RC_CANCEL);
                //}
            }
            else
            {// загрузка всего, что пришло (ALL или по фильтру)

                // пока ничего не загрузили
                xCL.dr1st = null;

                for (int i = 0; i < ds.Tables[NSI.BD_ZDOC].Rows.Count; i++)
                {
                    DataRow dr = ds.Tables[NSI.BD_ZDOC].Rows[i];
                    drDetZ = dr.GetChildRows(ds.Relations[NSI.REL2ZVK]);
                    if ((drDetZ != null) && (drDetZ.Length > 0))
                    {// имеются детальные строки для загрузки

                        // по-хорошему, надо искать по нормальному ключу, а не по SYSN
                        //nPKey = (int)dr["SYSN"];
                        s = FiltForDocExchg(dr, xCL, bIsTask);

                        DataRow[] aDr = dtDocs.Select(s);
                        if (aDr.Length > 0)
                            drMDoc = aDr[0];
                        else
                            drMDoc = null;

                        //drMDoc = dt.Rows.Find(new object[] { nPKey });

                        if (null != drMDoc)
                        {// ранее уже грузили
                            if (!bIsTask)
                            {// предыдущие задания не удаляем

                                drMDetZ = drMDoc.GetChildRows(xNSI.dsM.Relations[NSI.REL2ZVK]);
                                if (drDetZ.Length > 0)
                                {// их все и удаляем
                                    foreach (DataRow drDel in drMDetZ)
                                    {
                                        xNSI.dsM.Tables[NSI.BD_DIND].Rows.Remove(drDel);
                                    }
                                }
                            }
                        }
                        else
                        {// новая заявка
                            drMDoc = dtDocs.NewRow();
                            object x = drMDoc["SYSN"];
                            drMDoc.ItemArray = dr.ItemArray;
                            //for(int ii = 0; ii < dr.ItemArray.Length; ii++)
                            //{
                            //    if (!(dr.ItemArray[ii].GetType() == typeof(System.DBNull)))
                            //        drMDoc.ItemArray[ii] = dr.ItemArray[ii];
                            //}

                            drMDoc["SYSN"] = x;
                            drMDoc["SOURCE"] = NSI.DOCSRC_LOAD;
                            drMDoc["TIMECR"] = DateTime.Now;

                            if (xCL.nCommand == AppC.F_LOAD_DOC)
                                drMDoc["TYPOP"] = AppC.TYPOP_DOCUM;
                            else
                                drMDoc["TYPOP"] = AppC.TYPOP_PRMK;

                            if ((drMDoc["NOMD"] is string) && (((string)drMDoc["NOMD"]).Length > 0))
                            {
                            }
                            else
                            {
                                drMDoc["NOMD"] = xCLoad.xLP.sNomDoc;
                            }
                            if ((drMDoc["DOCBC"] is string) && (((string)drMDoc["DOCBC"]).Length > 0))
                            {
                            }
                            else
                            {
                                drMDoc["DOCBC"] = xCLoad.xLP.sBC_Doc;
                            }
                            if ((drMDoc["MLBC"] is string) && (((string)drMDoc["MLBC"]).Length > 0))
                            {
                            }
                            else
                            {
                                drMDoc["MLBC"] = xCLoad.xLP.sBC_ML;
                            }
                            drMDoc["CHKSSCC"] = 0;
                            dtDocs.Rows.Add(drMDoc);
                        }
                        // установка существующего ключа (SYSN)
                        // каскадно должен измениться и в детальных
                        //dr["DIFF"] = NSI.DOCCTRL.UNKNOWN;
                        dr["SYSN"] = drMDoc["SYSN"];
                        nM = 0;
                        nNPP = 1;
                        foreach (DataRow drZ in drDetZ)
                        {
                            nM += SetOneDetZ(ref sD, dtZVK, ds, drZ, drMDoc, ref nNPP);
                            nNPP++;
                        }
                        drMDoc["MESTZ"] = nM;
                        drMDoc["SOURCE"] = NSI.DOCSRC_LOAD;

                        if (xCL.dr1st == null)
                            xCL.dr1st = drMDoc;
                    }
                    else
                    {
                        sErr = String.Format("{0}-Детальные строки не найдены!", dr["SYSN"]);
                        nRet = AppC.RC_CANCEL;
                    }
                }
            }

            // возвращаем таблицу обратно и связи в DataSet
            //xNSI.dsM.Tables.Add(xNSI.dsM.Tables[NSI.BD_DOUTD]);
            //xNSI.dsM.Relations.Add(dsRel);


            return (nRet);
        }

        //private string FiltForDocExchg_(DataRow drZ, CurLoad xCL)
        //{
        //    int 
        //        n;
        //    string 
        //        s;

        //    string sF = String.Format("(TD={0}) AND (DT={1}) AND (KSK={2})", drZ["TD"], drZ["DT"], drZ["KSK"]);

        //    //******************
        //    s = "AND(ISNULL(NOMD,'')='')";
        //    try
        //    {
        //        s = (string)drZ["NOMD"];
        //        if (s.Length > 0)
        //        {
        //            s = "AND(NOMD='" + s + "')";
        //        }
        //        else
        //            drZ["NOMD"] = System.DBNull.Value;
        //    }
        //    catch { }
        //    finally
        //    {
        //        sF += s;
        //    }
        //    //******************






        //    s = "AND(ISNULL(NUCH,-1)=-1)";
        //    try
        //    {
        //        n = (int)drZ["NUCH"];
        //        if (n > 0)
        //        {
        //            s = "AND(NUCH=" + n.ToString() + ")";
        //        }
        //        else
        //            drZ["NUCH"] = System.DBNull.Value;
        //    }
        //    catch { s = ""; }
        //    finally
        //    {
        //        sF += s;
        //    }

        //    //s = "AND(ISNULL(KSMEN,'')='')";
        //    //try
        //    //{
        //    //    s = (string)drZ["KSMEN"];
        //    //    if (s.Length > 0)
        //    //    {
        //    //        s = "AND(KSMEN='" + s + "')";
        //    //    }
        //    //    else
        //    //        drZ["KSMEN"] = System.DBNull.Value;
        //    //}
        //    //catch { }
        //    //finally
        //    //{
        //    //    sF += s;
        //    //}



        //    s = "AND(ISNULL(KEKS,-1)=-1)";
        //    try
        //    {
        //        n = (int)drZ["KEKS"];
        //        if (n > 0)
        //        {
        //            s = "AND(KEKS=" + n.ToString() + ")";
        //        }
        //        else
        //            drZ["KEKS"] = System.DBNull.Value;
        //    }
        //    catch { s = ""; }
        //    finally
        //    {
        //        sF += s;
        //    }


        //    s = "AND(ISNULL(KRKPP,-1)=-1)";
        //    try
        //    {
        //        n = (int)drZ["KRKPP"];
        //        if (n > 0)
        //        {
        //            s = "AND(KRKPP=" + n.ToString() + ")";
        //        }
        //        else
        //            drZ["KRKPP"] = System.DBNull.Value;
        //    }
        //    catch { s = ""; }
        //    finally
        //    {
        //        sF += s;
        //    }

        //    //------
        //    if (xCL.nCommand == AppC.F_LOADKPL)
        //    {
        //        //sF += "AND(TYPOP=" + AppC.TYPOP_KMPL.ToString() + ")";
        //        sF = String.Format(sF + "AND(TYPOP={0})AND(NOMD={1})", AppC.TYPOP_KMPL, xCLoad.drPars4Load["NOMD"]);
        //    }
        //    else if (xCL.nCommand == AppC.F_LOADOTG)
        //        sF += "AND(TYPOP=" + AppC.TYPOP_OTGR.ToString() + ")";
        //    //------

        //    return ("(" + sF + ")");
        //}









        private string FiltForDocExchg(DataRow drZ, CurLoad xCL, bool bIsTask)
        {
            int
                n;
            string
                s;

            string sF = String.Format("(TD={0})", drZ["TD"]);
            s = (drZ["DOCBC"] is string) && (drZ["DOCBC"].ToString().Length > 0)?String.Format("AND(DOCBC='{0}')",drZ["DOCBC"]):"";
            if ((s.Length > 0) && (!bIsTask))
                // задания от сервера - по нулевому номеру (000)
                sF += s;
            else
            {
                s = (drZ["DT"] is string) && (drZ["DT"].ToString().Length > 0) ? String.Format("AND(DT='{0}')", drZ["DT"]) : "";
                sF += s;

                s = (drZ["NOMD"] is string) && (drZ["NOMD"].ToString().Length > 0) ? String.Format("AND(NOMD='{0}')", drZ["NOMD"]) : "";
                sF += s;

                s = (drZ["KSK"] is int) && ((int)drZ["KSK"] > 0) ? String.Format("AND(KSK={0})", drZ["KSK"]) : "";
                sF += s;

                s = (drZ["TYPKPP"] is string) && (drZ["TYPKPP"].ToString().Length > 0) ? String.Format("AND(TYPKPP='{0}')", drZ["TYPKPP"]) : "";
                sF += s;

            }
            return ("(" + sF + ")");
        }



        /// 
        private int SetOneDetZ(ref PSC_Types.ScDat sD, DataTable dtZVK, DataSet dsZ, DataRow dZ, DataRow dMDoc, ref int nNPP)
        {
            bool
                bFNsi,
                bSomeDateIsSet = false;
            int
                nCondition = 0,
                nIDOriginal,
                nPrzPl = 0,
                //nM = 0,
                nMest = 0;
            string
                sE,
                sKMC = "",
                sEAN = "";
            object[] 
                aIt = dZ.ItemArray;
            DataRow
                dr,
                drNewRow = dtZVK.NewRow();
            FRACT
                fEmkZ;

            nIDOriginal = (int)drNewRow["ID"];
            drNewRow.ItemArray = aIt;
            try
            {
                bFNsi = false;
                sD.fEmk = 0;
                nMest = (int)drNewRow["KOLM"];
                sKMC = (drNewRow["KMC"] is string) ? drNewRow["KMC"].ToString() : "";
                if (sKMC.Length > 0)
                {
                    dr = xNSI.DT[NSI.NS_MC].dt.Rows.Find(new object[] { sKMC });
                    bFNsi = sD.GetFromNSI(sD.s, dr, ref nPrzPl);
                    if (bFNsi)
                    {
                        drNewRow["KRKMC"] = sD.nKrKMC;
                        drNewRow["SNM"] = sD.sN;
                    }
                    else
                    {
                        drNewRow["KRKMC"] = 0;
                        drNewRow["SNM"] = "Неизвестно";
                    }
                }
                if (drNewRow["EAN13"] == System.DBNull.Value)
                {
                    if (bFNsi)
                        drNewRow["EAN13"] = sEAN = sD.sEAN;
                }
                else
                    sEAN = (string)drNewRow["EAN13"];
                if ((!bFNsi) && (sEAN.Length > 0))
                {
                    if (xNSI.GetMCDataOnEAN(sEAN, ref sD, false) == true)
                    {
                        drNewRow["KMC"] = sD.sKMC;
                        drNewRow["KRKMC"] = sD.nKrKMC;
                        drNewRow["SNM"] = sD.sN;
                    }
                    else
                    {
                        drNewRow["KRKMC"] = 0;
                        drNewRow["SNM"] = "Неизвестно";
                    }
                }
            }
            catch
            {
                drNewRow["KRKMC"] = 0;
                drNewRow["SNM"] = "Неизвестно";
                nMest = 0;

                    sE = String.Format("{0} - Загрузка BC={1} EAN={2}",
                    DateTime.Now.ToString("dd.MM.yy HH:mm:ss - "),
                    dMDoc["DOCBC"],
                    sEAN);
                    WriteProt(sE);
            }

            //nM = nMest;
            //if (drNewRow["EMK"] == System.DBNull.Value)
            //{
            //    if (nMest > 0)
            //    {
            //        if (sD.fEmk == 0)
            //            sD.fEmk = (FRACT)((int)(((FRACT)drNewRow["KOLE"]) / nMest));
            //        drNewRow["EMK"] = sD.fEmk;
            //    }
            //    else
            //        drNewRow["EMK"] = 0;
            //}

            try
            {
                fEmkZ = (FRACT)drNewRow["EMK"];
            }
            catch
            {
                fEmkZ = 0;
            }

            //if (drMZ["EMK"] == System.DBNull.Value)
            if (fEmkZ <= 0)
            {
                fEmkZ = 0;
                if (nMest > 0)
                {
                    if (sD.fEmk == 0)
                        sD.fEmk = (FRACT)((int)(((FRACT)drNewRow["KOLE"]) / nMest));
                    fEmkZ = sD.fEmk;
                }
                drNewRow["EMK"] = fEmkZ;
            }

            drNewRow["READYZ"] = NSI.READINESS.NO;

            if ((drNewRow["SSCC"] is string) && (drNewRow["SSCC"].ToString().Length > 0))
            {// код паллетты внешней задан
                nCondition |= (int)NSI.SPECCOND.SSCC;
            }
            else
                drNewRow["SSCC"] = "";

            if ((drNewRow["SSCCINT"] is string) && (drNewRow["SSCCINT"].ToString().Length > 0))
            {// код паллетты внутренней задан
                nCondition |= (int)NSI.SPECCOND.SSCC_INT;
            }
            else
                drNewRow["SSCCINT"] = "";

            if (drNewRow["NP"] is string)
            {
                if (((string)drNewRow["NP"] == "*")
                    || ((string)drNewRow["NP"] == "-1"))
                {
                    drNewRow["NP"] = "";
                    nCondition |= (int)NSI.SPECCOND.DATE_SET_EXACT;
                }
                else
                {
                    if (((string)drNewRow["NP"]).Length > 0)
                    {// партия задана
                        nCondition |= (int)NSI.SPECCOND.PARTY_SET;
                    }
                }
            }
            else
                drNewRow["NP"] = "";
            
            if (xPars.UseDTGodn)
            {
                try
                {
                    bSomeDateIsSet = DateCond(drNewRow, "DTG", ref nCondition);
                }
                catch
                {
                    drNewRow["DTG"] = null;
                    bSomeDateIsSet = false;
                }
            }

            if (xPars.UseDTProizv)
            {
                try
                {
                    bSomeDateIsSet |= DateCond(drNewRow, "DVR", ref nCondition);
                }
                catch
                {
                    drNewRow["DVR"] = null;
                }
            }

            /// если партия - *, а даты вообще нет
            if ((!bSomeDateIsSet) && ((nCondition & (int)NSI.SPECCOND.DATE_SET_EXACT) > 0))
                nCondition -= (int)NSI.SPECCOND.DATE_SET_EXACT;

            drNewRow["COND"] = nCondition;

            if (drNewRow["NPP"] is int)
                nNPP = (int)drNewRow["NPP"];
            else
                drNewRow["NPP"] = nNPP;

            try
            {
                nNPP = (int)drNewRow["NPP"];
            }
            catch { }
            drNewRow["NPP"] = nNPP;

            drNewRow["ID"] = nIDOriginal;
            drNewRow["SYSN"] = dMDoc["SYSN"];

            dtZVK.Rows.Add(drNewRow);
            AddAKMC(dsZ, dZ, drNewRow);
            return (nMest);
        }



        private void AddAKMC(DataSet dsZ, DataRow dZ, DataRow drD)
        {
            int
                nY;
            string
                sKAdr;
            DataRow
                drTMP;
            DataRow[]
                drA = dZ.GetChildRows(NSI.REL2ADR);
            DataTable
                dtAKMC = xNSI.DT[NSI.BD_ADRKMC].dt;
            foreach (DataRow dr in drA)
            {
                drTMP = dtAKMC.NewRow();
                drTMP.ItemArray = dr.ItemArray;
                drTMP["ID"] = drD["ID"];
                sKAdr = (string)dr["KADR"];
                nY = sKAdr.Length;
                if (nY >= 9)
                {
                    nY = int.Parse(sKAdr.Substring(nY - 1, 1));
                    drTMP["IDX"] = nY;
                }
                dtAKMC.Rows.Add(drTMP);
            }
        }

        private bool DateCond(DataRow drMZ, string sDateField, ref int nCondition)
        {
            bool
                bIsDateSet = false;

            if (drMZ[sDateField] is string)
            {
                if (DateTime.ParseExact((string)drMZ[sDateField], "yyyyMMdd", null) > DateTime.MinValue)
                {// дата выработки/годности задана
                    bIsDateSet = true;
                    nCondition |= (int)NSI.SPECCOND.DATE_SET;

                    nCondition |= (int)((sDateField == "DVR") ? NSI.SPECCOND.DATE_V_SET : NSI.SPECCOND.DATE_G_SET);

                    if ((nCondition & (int)NSI.SPECCOND.PARTY_SET) > 0)
                        nCondition |= (int)NSI.SPECCOND.DATE_SET_EXACT;

                }
            }
            return (bIsDateSet);
        }

        // обмен данными с сервером в формате XML
        public class ServerExchange
        {
            private int 
                m_RetAppSrv;

            private string
                m_FullCom = "",
                m_Task = "",
                m_ParString;

            private SocketStream 
                m_ssExchg;
            private MainF 
                xMF;
            private byte[] 
                m_aParsInXML;

            private Dictionary<string, string> 
                dicServAns,
                dicParsAnswer;


            private byte[] SetCommand2Srv(string nComCode, string sP, string sMD5)
            {
                string
                    sCom = "COM=" + nComCode,
                    sUserCode = ";KP=" + xMF.xSm.sUserTabNom,
                    sCurDoc = "",
                    sR = ",",
                    sRet = "",
                    sPar = ";";
                DocPars 
                    xP = xMF.xCDoc.xDocP;

                if (FullCOM2Srv.Length > 0)
                    return (Encoding.UTF8.GetBytes(FullCOM2Srv + Encoding.UTF8.GetString(AppC.baTermCom, 0, AppC.baTermCom.Length)));

                switch (nComCode)
                {
                    case AppC.COM_VTTN:
                    case AppC.COM_VVPER:
                        nComCode = AppC.COM_VDOC;
                        if (xMF.xCUpLoad.ilUpLoad.CurReg == AppC.UPL_CUR)
                        {
                            sCurDoc = ",CD=1";
                            xP = xMF.xCDoc.xDocP;
                        }
                        else
                            xP = xMF.xCUpLoad.xLP;
                        break;
                }

                switch (nComCode)
                {
                    case AppC.COM_GETPRN:
                    case AppC.COM_CHKSCAN:
                    case AppC.COM_VOPR:
                        break;
                    case AppC.COM_ZSPR:
                        sPar = ";PAR=" + sP + ";" + sMD5;
                        break;
                    case AppC.COM_VDOC:
                    case AppC.COM_ZDOC:

                        if (nComCode == AppC.COM_ZDOC)
                        {
                            if (xMF.xCLoad.ilLoad.CurReg == AppC.UPL_CUR)
                            {
                                sCurDoc = ",CD=1";
                                xP = xMF.xCDoc.xDocP;
                            }
                            else
                                xP = xMF.xCLoad.xLP;
                        }

                        sRet = "TD=" + xP.nNumTypD.ToString();
                        sRet += sR + "KSK=" + xP.nSklad.ToString();

                        if (xP.dDatDoc != DateTime.MinValue)
                            sRet += sR + "DT=" + xP.dDatDoc.ToString("yyyyMMdd");

                        if ((xP.nUch != AppC.EMPTY_INT) && (xP.nUch > 0))
                            sRet += sR + "NUCH=" + xP.nUch.ToString();
                        if (xP.sSmena != "")
                            sRet += sR + "KSMEN=" + xP.sSmena;
                        if ((xP.nEks != AppC.EMPTY_INT) && (xP.nEks > 0))
                            sRet += sR + "KEKS=" + xP.nEks.ToString();
                        if ((xP.nPol != AppC.EMPTY_INT) && (xP.nPol > 0))
                            sRet += sR + "KPP=" + xP.nPol.ToString();
                        if (xP.sNomDoc != "")
                            sRet += sR + "ND=" + xP.sNomDoc;
                        if (xP.sBC_Doc != "")
                            sRet += sR + "IDLOAD=" + xP.sBC_Doc;

                        if (nComCode == AppC.COM_ZDOC)
                        {
                            if (xP.nNumTypD != AppC.TYPD_MOVINT)
                                sRet += sR + "ADRKMC=FULL";
                            //sRet += sR + "ADRKMC=LIMIT";
                        }

                        sRet += sCurDoc;
                        //xMF.xCLoad.sFilt = sRet;
                        sRet = "PAR=(" + sRet + ")";

                        sPar = ";" + sRet + ";";
                        sCom = "COM=" + nComCode;
                        break;

                    case AppC.COM_CELLI:
                    case AppC.COM_ADR2CNT:
                        sPar = ";PAR=" + sP + ";";
                        break;

                    case AppC.COM_LST2SSCC:
                        sPar = String.Format(";{0}", sMD5);
                        break;
                    case AppC.COM_GENFUNC:
                    case AppC.COM_PRNBLK:
                        sPar = String.Format(";{0};", sP);
                        break;
                    case AppC.COM_UNKBC:
                        sPar = String.Format(";{0};", sP);
                        break;
                    default:
                        sPar = (sP.Length > 0) ? String.Format(";PAR={0};", sP) : ";";
                        break;
                }

                sCom += ";MAC=" + xMF.xPars.MACAdr +
                    sUserCode +
                    ";NUMT=" + xMF.xPars.NomTerm.ToString() +
                    sPar +
                    Encoding.UTF8.GetString(AppC.baTermCom, 0, AppC.baTermCom.Length);

                byte[] baCom = Encoding.UTF8.GetBytes(sCom);
                return (baCom);
            }


            public ServerExchange(MainF x)
            {
                xMF = x;
                XMLPars = null;
            }


            private void SelSrvPort(string sCom, string sPar1, out string sH, out int nP)
            {
                int i;

                sH = xMF.xPars.sHostSrv;
                nP = xMF.xPars.nSrvPort;
                if (xMF.xPars.bUseSrvG)
                {
                    switch (sCom)
                    {
                        case AppC.COM_ZSPR:
                            string sLH = (string)xMF.xNSI.BD_TINF_RW(sPar1)["LOAD_HOST"];
                            if (sLH.Length > 0)
                            {
                                try
                                {
                                    int nLP = (int)xMF.xNSI.BD_TINF_RW(sPar1)["LOAD_PORT"];
                                    if (nLP > 0)
                                    {
                                        sH = sLH;
                                        nP = nLP;
                                    }
                                }
                                catch { }
                            }
                            break;
                        case AppC.COM_VVPER:
                        case AppC.COM_VTTN:
                            if (xMF.xCUpLoad.nSrvGind >= 0)
                            {
                                i = xMF.xCUpLoad.nSrvGind - 1;
                                if (xMF.xCUpLoad.nSrvGind == 0)
                                {
                                    i = 0;
                                }
                                else
                                {
                                }
                                sH = xMF.xPars.aSrvG[i].sSrvHost;
                                nP = xMF.xPars.aSrvG[i].nPort;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            // строка команды серверу
            public string FullCOM2Srv
            {
                get { return m_FullCom; }
                set { m_FullCom = value; }
            }

            // строка c заданиями от сервера
            public string TasksFromSrv
            {
                get { return m_Task; }
                set { m_Task = value; }
            }


            // поток
            public SocketStream CurSocket
            {
                get { return m_ssExchg; }
                set { m_ssExchg = value; }
            }

            // значение параметра RET в ответе сервера
            public int ServerRet
            {
                get { return m_RetAppSrv; }
                set { m_RetAppSrv = value; }
            }

            // строка параметров ответа
            public string StringAnsPars
            {
                get { return m_ParString; }
                set { m_ParString = value; }
            }

            // список параметров ответа
            public Dictionary<string, string> ServerAnswer
            {
                get { return dicServAns; }
                set { dicServAns = value; }
            }

            // список параметров ответа
            public Dictionary<string, string> AnswerPars
            {
                get { return dicParsAnswer; }
                set { dicParsAnswer = value; }
            }

            // XML-представление параметров общей формы
            public byte[] XMLPars
            {
                get { return m_aParsInXML; }
                set { m_aParsInXML = value; }
            }


            public bool TestConn(bool bForcibly, BarcodeScanner xBCS, FuncPanel xFP)
            {
                bool ret = true;
                //string sOldInf = xFPan.RegInf;
                WiFiStat.CONN_TYPE cT = xBCS.WiFi.ConnectionType();

                if ((cT == WiFiStat.CONN_TYPE.NOCONNECTIONS) || (bForcibly))
                {
                    bool bHidePan = false;

                    if (!xFP.IsShown)
                    {
                        //xBCS.WiFi.IsEnabled = true;
                        xFP.ShowP(6, 50, "Переподключение к сети", "Wi-Fi");
                        bHidePan = true;
                    }

                    Cursor crsOld = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;

                    xFP.RegInf = "Переподключение Wi-Fi...";
                    ret = xBCS.WiFi.ResetWiFi(2);
                    if (ret)
                    {
                        Thread.Sleep(4000);
                        xBCS.WiFi.GetIPList();
                        xFP.RegInf = "IP: " + xBCS.WiFi.IPCurrent;
                    }
                    else
                        xFP.RegInf = "Сброс не выполнен...";
                    if (bHidePan)
                        xFP.HideP();

                    Cursor.Current = crsOld;
                }
                return (ret);
            }


            public string ExchgSrv(string nCom, string sPar1, string sDop,
                LoadFromSrv dgRead, DataSet dsTrans, ref int ret)
            {
                return (ExchgSrv(nCom, sPar1, sDop, dgRead, dsTrans, ref ret, 60, -2));
            }

            public string ExchgSrv(string nCom, string sPar1, string sDop,
                LoadFromSrv dgRead, DataSet dsTrans, ref int ret, int nTimeOutR)
            {
                return (ExchgSrv(nCom, sPar1, sDop, dgRead, dsTrans, ref ret, nTimeOutR, -2));
            }


            // обмен данными с сервером в формате XML
            // nCom - номер команды
            // sPar1
            // nTOutRead - таймаут на ожидание ответа от сервера
            public string ExchgSrv(string nCom, string sPar1, string sDop,
                LoadFromSrv dgRead, DataSet dsTrans, ref int ret, int nTOutRead, int nBufSize)
            {
                string
                    sOutFileXML = "",
                    sC,
                    sHost,
                    sAdr,
                    sErr;
                int nPort;

                SocketStream.ASRWERROR nRErr;

                System.IO.Stream stm = null;

                ret = 0;
                ServerRet = AppC.EMPTY_INT;

                SelSrvPort(nCom, sPar1, out sHost, out nPort);
                sAdr = sHost + ":" + nPort.ToString();
                sErr = sAdr + "-нет соединения!";

                Cursor.Current = Cursors.WaitCursor;

                try
                {
                    CurSocket = new SocketStream(sHost, nPort);
                    if (!TestConn(false, xMF.xBCScanner, xMF.xFPan))
                    {
                        //MessageBox.Show("Excep!");
                        //throw new System.Net.Sockets.SocketException(11053);
                    }
                    else
                    {
                        //MessageBox.Show("Good reset!");
                    }

                    stm = CurSocket.Connect();

                    // поток создан, отправка команды
                    sErr = sAdr + "-команды не отправлена";
                    byte[] baCom = SetCommand2Srv(nCom, sPar1, sDop);
                    //stm.Write(baCom, 0, baCom.Length);
                    //stm.Write(AppC.baTermCom, 0, AppC.baTermCom.Length);

                    // 20 секунд на запись команды
                    CurSocket.ASWriteS.TimeOutWrite = 1000 * 10;
                    CurSocket.ASWriteS.BeginAWrite(baCom, baCom.Length);

                    if ((dsTrans != null) || (XMLPars != null))
                    {// передача данных при выгрузке
                        //sErr = sAdr + "-ошибка выгрузки";
                        //dsTrans.WriteXml(stm, XmlWriteMode.IgnoreSchema);
                        //sErr = sAdr + "-ошибка завершения";

                        sErr = sAdr + "-ошибка выгрузки";
                        MemoryStream mst = new MemoryStream();
                        if (dsTrans != null)
                            dsTrans.WriteXml(mst, XmlWriteMode.IgnoreSchema);

                        if (XMLPars != null)
                        {
                            mst.Write(XMLPars, 0, XMLPars.Length);
                        }

                        // терминатор сообщения
                        mst.Write(AppC.baTermMsg, 0, AppC.baTermMsg.Length);

                        byte[] bm1 = mst.ToArray();
                        mst.Close();
                        // x секунд на запись данных
                        CurSocket.ASWriteS.TimeOutWrite = 1000 * 180;
                        //CurSocket.ASWriteS.TimeOutWrite = 1000 * 60;
                        CurSocket.ASWriteS.BeginAWrite(bm1, bm1.Length);
                    }
                    else
                    {
                        sErr = sAdr + "-ошибка завершения";
                        // 10 секунд на запись терминатора сообщения
                        CurSocket.ASWriteS.TimeOutWrite = 1000 * 10;
                        // терминатор сообщения
                        CurSocket.ASWriteS.BeginAWrite(AppC.baTermMsg, AppC.baTermMsg.Length);
                    }


                    //int nCommLen = 0;
                    //byte[] bAns = ReadAnswerCommand(stm, ref nCommLen);
                    //sC = Encoding.UTF8.GetString(bAns, 0, nCommLen - AppC.baTermCom.Length);

                    sErr = sAdr + "-нет ответа сервера!";
                    // 120 секунд на чтение ответа
                    //m_ssExchg.ASReadS.TimeOutRead = 1000 * 120;

                    //m_ssExchg.ASReadS.BufSize = 256;
                    //nRErr = m_ssExchg.ASReadS.BeginARead(bUseFileAsBuf, 1000 * nTOutRead);

                    if (nBufSize > 0)
                        CurSocket.ASReadS.BufSize = nBufSize;
                    nRErr = CurSocket.ASReadS.BeginARead(1000 * nTOutRead);

                    switch (nRErr)
                    {
                        case SocketStream.ASRWERROR.RET_FULLBUF:   // переполнение буфера
                            sErr = " длинная команда";
                            throw new System.Net.Sockets.SocketException(10061);
                        case SocketStream.ASRWERROR.RET_FULLMSG:   // сообщение полностью получено
                            sC = CurSocket.ASReadS.GetMsg();
                            break;
                        default:
                            throw new System.Net.Sockets.SocketException(10061);
                    }

                    sErr = sAdr + "-ошибка чтения";
                    //Dictionary<string, string> aComm = SrvCommandParse(sC);
                    ServerAnswer = Srv.SrvAnswerParParse(sC);
                    if (ServerAnswer.ContainsKey("PAR"))
                    {
                        StringAnsPars = ServerAnswer["PAR"];
                        StringAnsPars = StringAnsPars.Substring(1, StringAnsPars.Length - 2);
                        AnswerPars = Srv.SrvAnswerParParse(StringAnsPars, new char[] { ',' });
                    }


                    ServerRet = int.Parse(ServerAnswer["RET"]);

                    if ((ServerAnswer["COM"] == nCom) &&
                        ((ServerRet == AppC.RC_OK) ||
                        (ServerRet == AppC.RC_NEEDPARS) ||
                        (ServerRet == AppC.RC_HALFOK)))
                    {
                        CurSocket.ASReadS.OutFile = "";
                        if (ServerRet == AppC.RC_TASKS)
                        {
                            CurSocket.ASReadS.TermDat = AppC.baTermCom;
                            if (CurSocket.ASReadS.BeginARead(true, 1000 * nTOutRead) == SocketStream.ASRWERROR.RET_FULLMSG)
                            {
                                TasksFromSrv = CurSocket.ASReadS.GetMsg();
                            }
                            else
                                throw new System.Net.Sockets.SocketException(10061);
                        }

                        if (dgRead != null)
                            dgRead(CurSocket, ServerAnswer, dsTrans, ref sErr, ServerRet);
                        try
                        {
                            sErr = ServerAnswer["MSG"];
                        }
                        catch { sErr = "OK"; }
                        //dgRead(m_ssExchg, aComm, dsTrans, ref sErr, nRetSrv);
                        //else
                        //{
                        //    sErr = "OK";
                        //}
                    }
                    else
                    {
                        if (ServerAnswer["MSG"] != "")
                            sErr = ServerAnswer["MSG"];
                        else
                            sErr = sAdr + "\n Отложено выполнение";
                    }
                    ret = ServerRet;

                }
                catch (Exception e)
                {
                    //sC = e.Message;
                    sErr = e.Message;
                    ret = 3;
                }
                finally
                {
                    CurSocket.Disconnect();
                    Cursor.Current = Cursors.Default;
                    if (ServerRet == AppC.RC_TASKS)
                    {
                        
                        xMF.ProceedSrvTasks(TasksFromSrv);
                    }
                }
                return (sErr);


            }


        }
















    }
}
