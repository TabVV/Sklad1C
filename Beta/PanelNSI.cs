using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.IO;

using PDA.OS;
using PDA.Service;
using SavuSocket;


namespace SkladRM
{
    public partial class MainF : Form
    {
        private Srv.Collect4Show<NSI.TableDef>
            xCollNsi = null;

        //private BindingList<ServerInf> 
        //    blSrvG;


        // при активации панели НСИ
        private void EnterInNSI()
        {
            //if (nCurNsi == "")
            //{
            //    enuNSI = xNSI.DT.GetEnumerator();
            //    ChangeCurNSI();

            //    blSrvG = new BindingList<ServerInf>();
            //    blSrvG.Clear();

            //    biL = new BindingList<SkladAll.NSIAll.TableDef>();
            //    foreach(KeyValuePair<string, NSI.TableDef> kvP in xNSI.DT)
            //    {
            //        biL.Add(kvP.Value);
            //    }
            //}

            if (xCollNsi == null)
            {// выполняется один раз
                NSI.TableDef[] aTD = new NSI.TableDef[xNSI.DT.Values.Count];
                xNSI.DT.Values.CopyTo(aTD, 0);
                xCollNsi = new Srv.Collect4Show<NSI.TableDef>(aTD);
                ChangeCurNSI(false);

                //blSrvG = new BindingList<ServerInf>();
                //blSrvG.Clear();
            }
            dgMC.Focus();
        }

        private void ShowNSIStat()
        {
            DataRow r;
            //if (nCurNsi != "")
            //{
            //    lNsiInf.Text = xNSI.DT[nCurNsi].Text;
            //    tNsiInf.Text = "Записей - " + xNSI.DT[nCurNsi].dt.Rows.Count.ToString();
            //    r = xNSI.BD_TINF_RW(nCurNsi);
            //    tNsiLoadHost.Text = (r["LOAD_HOST"] == System.DBNull.Value)?"":(string)r["LOAD_HOST"];
            //    tNsiLoadPort.Text = (r["LOAD_PORT"] == System.DBNull.Value) ? "" : r["LOAD_PORT"].ToString();
            //}
            //else
            //{
            //    lNsiInf.Text = "";
            //    tNsiInf.Text = "Записей";
            //    tNsiLoadHost.Text = "";
            //    tNsiLoadPort.Text = "";
            //}

            string
                sCurNsiKey = (xCollNsi.Current is NSI.TableDef) ? ((NSI.TableDef)xCollNsi.Current).dt.TableName : "";

            if (sCurNsiKey != "")
            {
                lNsiInf.Text = xNSI.DT[sCurNsiKey].Text;
                tNsiInf.Text = "Записей - " + xNSI.DT[sCurNsiKey].dt.Rows.Count.ToString();
                r = xNSI.BD_TINF_RW(sCurNsiKey);
                tNsiLoadHost.Text = (r["LOAD_HOST"] == System.DBNull.Value) ? "" : (string)r["LOAD_HOST"];
                tNsiLoadPort.Text = (r["LOAD_PORT"] == System.DBNull.Value) ? "" : r["LOAD_PORT"].ToString();
            }
            else
            {
                lNsiInf.Text = "";
                tNsiInf.Text = "Записей";
                tNsiLoadHost.Text = "";
                tNsiLoadPort.Text = "";
            }








        }

        // смена текущего справочника
        //private void ChangeCurNSI()
        //{
        //    bool bFound = false;

        //    while (enuNSI.MoveNext())
        //    {
        //        if (((enuNSI.Current.Value.nType & NSI.TBLTYPE.NSI) == NSI.TBLTYPE.NSI) &&
        //            ((enuNSI.Current.Value.nType & NSI.TBLTYPE.INTERN) != NSI.TBLTYPE.INTERN))
        //            {
        //            nCurNsi = enuNSI.Current.Key;
        //            bFound = true;
        //            break;
        //        }
        //    }
        //    if (bFound)
        //    {
        //        dgMC.DataSource = xNSI.DT[nCurNsi].dt;
        //        dgMC.Refresh();
        //        ShowNSIStat();
        //    }
        //    else
        //    {
        //        enuNSI = xNSI.DT.GetEnumerator();
        //        ChangeCurNSI();
        //    }

        //}


        // смена текущего справочника
        private void ChangeCurNSI(bool PrevNsi)
        {
            bool
                bFound = false;
            NSI.TableDef
                xTD = null;
            NSI.TBLTYPE
                nTType;
            do
            {
                xTD = (PrevNsi) ? xCollNsi.MoveEx(Srv.Collect4Show<SkladAll.NSIAll.TableDef>.DIR_MOVE.BACK) :
                    xCollNsi.MoveEx(Srv.Collect4Show<SkladAll.NSIAll.TableDef>.DIR_MOVE.FORWARD);
                nTType = xTD.nType;
                if (((nTType & NSI.TBLTYPE.NSI) == NSI.TBLTYPE.NSI) &&
                    ((nTType & NSI.TBLTYPE.INTERN) != NSI.TBLTYPE.INTERN))
                {
                    bFound = true;
                    break;
                }
            } while (xCollNsi.Current != null);

            if (bFound)
            {
                dgMC.DataSource = xTD.dt;
                dgMC.Refresh();
                ShowNSIStat();
            }
        }



        // обработка клавиш на панели НСИ
        private bool NSI_KeyDown(int nFunc, KeyEventArgs e)
        {
            bool ret = false;

            if (nFunc > 0)
            {
                switch (nFunc)
                {
                    case AppC.F_LOAD_DOC:                       // загрузка справочников
                        //NSI.TBLTYPE tT = xNSI.DT[nCurNsi].nType;
                        NSI.TBLTYPE tT = ((NSI.TableDef)xCollNsi.Current).nType;

                        if (((tT & NSI.TBLTYPE.NSI) == NSI.TBLTYPE.NSI) &&
                            ((tT & NSI.TBLTYPE.LOAD) == NSI.TBLTYPE.LOAD))   // НСИ загружаемое
                        {
                            //tNsiInf.Text = "Идет загрузка...";
                            object xDSrc = dgMC.DataSource;
                            dgMC.DataSource = null;

                            LoadNsiMenu(false, new string[] { ((NSI.TableDef)xCollNsi.Current).dt.TableName });
                            dgMC.DataSource = xDSrc;
                            ShowNSIStat();
                        }
                        ret = true;
                        break;
                    case AppC.F_CHG_REC:                       // смена сервера
                        if ( xPars.bUseSrvG ) 
                        {
                        }
                        ret = true;
                        break;
                }
            }

            else
            {
                switch (e.KeyValue)
                {
                    case W32.VK_ESC:
                        tcMain.SelectedIndex = 0;
                        ret = true;
                        break;
                    case W32.VK_ENTER:                      // следующий справочник
                        ChangeCurNSI(e.Control);
                        ret = true;
                        break;
                }
            }
            e.Handled = ret;

            return (ret);
        }

        class LoadNSISrv
        {
            private NSI xNSI;
            private string sTName;
            private bool bMD_5;

            public string sDop = "";
            public LoadFromSrv dgL;

            public LoadNSISrv(NSI x_NSI, string sTName_Ind, bool bMD5)
            {
                xNSI = x_NSI;
                sTName = sTName_Ind;
                bMD_5 = bMD5;

                //sDop = (bMD5 == true) ? "MD5=" + xNSI.DT[sTName].MD5 + ";" : "";

                sDop = (bMD5 == true) ? "MD5=" + (string)xNSI.BD_TINF_RW(sTName)["MD5"] + ";" : "";
                dgL = new LoadFromSrv(NsiFromSrv);

            }

            


            private void NsiFromSrv(SocketStream stmX, Dictionary<string, string> aC, DataSet ds,
                ref string sErr, int nRetSrv)
            {
                SocketStream.ASRWERROR nRErr;
                string sMD5New = aC["MD5"];
                string sP = xNSI.sPathNSI + xNSI.DT[sTName].sXML;

                if ((bMD_5 == true) && (sMD5New == (string)xNSI.BD_TINF_RW(sTName)["MD5"]))
                {
                    sErr = "OK-No Load";
                    xNSI.BD_TINF_RW(sTName)["LASTLOAD"] = DateTime.Now;
                }
                else
                {
                    sErr = "Ошибка чтения XML";
                    string sXMLFile = "";

                    if (stmX.ASReadS.OutFile.Length == 0)
                    {
                        stmX.ASReadS.TermDat = AppC.baTermMsg;
                        nRErr = stmX.ASReadS.BeginARead(true, 1000 * 60);
                        switch (nRErr)
                        {
                            case SocketStream.ASRWERROR.RET_FULLMSG:   // сообщение полностью получено
                                break;
                            default:
                                throw new System.Net.Sockets.SocketException(10061);
                        }
                    }
                    sXMLFile = stmX.ASReadS.OutFile;
                    stmX.Disconnect();

                    try
                    {
                        sErr = "Ошибка загрузки XML";
                        xNSI.DT[sTName].dt.BeginLoadData();
                        xNSI.DT[sTName].dt.Clear();
                        System.Xml.XmlReader xmlRd = System.Xml.XmlReader.Create(sXMLFile);
                        xNSI.DT[sTName].dt.ReadXml(xmlRd);
                        xmlRd.Close();

                        xNSI.DT[sTName].dt.EndLoadData();

                        sErr = "Ошибка сохранения XML";

                        if (File.Exists(sP))
                            File.Delete(sP);

                        if (xNSI.AfterLoadNSI(sTName, true, sXMLFile) == AppC.RC_OK)
                            File.Move(sXMLFile, sP);

                        xNSI.BD_TINF_RW(sTName)["MD5"] = sMD5New;
                        xNSI.BD_TINF_RW(sTName)["LASTLOAD"] = DateTime.Now;
                    }
                    finally
                    {
                        if (File.Exists(sXMLFile))
                        {// возникла ошибка при загрузке, иначе отработал бы Move
                            sErr = "Ошибка загрузки XML";
                            File.Delete(sXMLFile);
                        }
                        else
                        {// ошибок не было
                            sErr = "OK";
                        }
                    }
                }
            }

        }

        // загрузка справочников с сервера
        //private int LoadAllNSISrv(string[] aI, bool bMD5, bool xShow)
        //{
        //    return (nRet);
        //}

        // групповая операция (проверка/загрузка)
        private void LoadNsiMenu(bool bTestByMD5, string[] aTables)
        {
            bool 
                xShow = true,
                bOldNsi;
            string i,
                sFull = "",
                sT = "";

            ServerExchange 
                xSE = new ServerExchange(this);

            //xBCScanner.WiFi.IsEnabled = true;
            xBCScanner.WiFi.ShowWiFi(pnLoadDocG, true);
            xFPan.UpdateSrv(xPars.sHostSrv);
            xFPan.ShowP(6, 45, "Обновление НСИ", (bTestByMD5 == true) ? "Проверка" : "Загрузка");
            xFPan.UpdateHelp("Соединение с сервером");

            int j,
                nErr = 0,
                nGood = 0,
                nRet = 0,
                tc1 = Environment.TickCount;

            xNSI.dsNSI.EnforceConstraints = false;

            if (aTables.Length == 0)
            {
                //aTables = new string[xNSI.DT.Keys.Count];
                //xNSI.DT.Keys.CopyTo(aTables, 0);

                List<string> lT = new List<string>();

                foreach (KeyValuePair<string, NSI.TableDef> td in xNSI.DT)
                {
                    if (((td.Value.nType & NSI.TBLTYPE.NSI) == NSI.TBLTYPE.NSI) &&
                        ((td.Value.nType & NSI.TBLTYPE.LOAD) == NSI.TBLTYPE.LOAD))   // НСИ загружаемое
                        lT.Add(td.Key);
                }
                aTables = lT.ToArray();
            }

            for (j = 0; j < aTables.Length; j++)
            {
                nRet = 0;
                i = aTables[j];
                //if (((bTestByMD5) && (((DateTime)xNSI.BD_TINF_RW(i)["LASTLOAD"]).Date < DateTime.Now.Date)) || 
                //    (!bTestByMD5))
                try
                {
                    bOldNsi = (((DateTime)xNSI.BD_TINF_RW(i)["LASTLOAD"]).Date < DateTime.Now.Date);
                }
                catch
                {
                    bOldNsi = true;
                }


                    if ((bTestByMD5 && bOldNsi) ||
                        (!bTestByMD5))
                    {
                    LoadNSISrv lnsi = new LoadNSISrv(xNSI, i, bTestByMD5);
                    sT = xSE.ExchgSrv(AppC.COM_ZSPR, i, lnsi.sDop, lnsi.dgL, null, ref nRet);
                    if (nRet == 0)
                    {
                        nGood++;
                    }
                    else
                        nErr++;
                    sT = xNSI.DT[i].Text + "..." + sT + "\r\n";
                    sFull += sT;

                    if (xShow == true)
                        xFPan.UpdateReg(sT);
                }
            }

            try
            {
                xNSI.dsNSI.EnforceConstraints = true;
            }
            catch
            {
                nErr = 1;
            }
            if (nErr == 0)
            {
                //xSm.dtLoadNS = DateTime.Now;
            }

            sT = Srv.TimeDiff(tc1, Environment.TickCount);
            if (bTestByMD5 == false)
                MessageBox.Show(sFull, "Время-" + sT);
            xFPan.HideP();

        }





    }
}
