using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;
using System.Data;

using ScannerAll;
using SkladAll;
using PDA.OS;
using PDA.Service;

using FRACT = System.Decimal;

namespace SkladRM
{


    public partial class ServClass
    {
        // детальные строки
        public class DGTBoxColorColumn : DGCustomColumn
        {
            private MainF xF = null;
            public DGTBoxColorColumn() : this(null) { }

            public DGTBoxColorColumn(DataGrid dg):base()
            {
                if (dg != null)
                    base.Owner = dg;
                base.ReadOnly = true;
                xF = NSI.xFF;
            }
            public DGTBoxColorColumn(DataGrid dg, string sTable)
                : base()
            {
                if (dg != null)
                    base.Owner = dg;
                base.TableInd = sTable;
                base.ReadOnly = true;
                xF = NSI.xFF;
            }

            // Let'sTypDoc add this so user can access 
            public virtual TextBox TextBox
            {
                get { return this.HostedControl as TextBox; }
            }

            protected override string GetBoundPropertyName()
            {
                return "Text";                                                          // Need to bount to "Text" property on TextBox
            }

            protected override Control CreateHostedControl()
            {
                TextBox box = new TextBox();                                            // Our hosted control is a TextBox

                box.BorderStyle = BorderStyle.None;                                     // It has no border
                box.Multiline = true;                                                   // And it'sTypDoc multiline
                box.TextAlign = this.Alignment;                                         // Set up aligment.

                return box;
            }

            protected override bool DrawBackground(Graphics g, Rectangle bounds, int rowNum, 
                Brush backBrush, System.Data.DataRow dr)
            {
                Brush 
                    background = backBrush;
                bool 
                    bSelAll = false,
                    bSel = (((SolidBrush)backBrush).Color != Owner.SelectionBackColor) ? false : true;
                AppC.OPR_STATE
                    stDet;


                if (bSel == false)
                {// вообще-то не выделена, но иногда надо выделить
                    switch (this.TableInd)
                    {
                        case NSI.BD_DIND:
                            if (MainF.drEasyEdit == dr)
                            {// режим быстрого ввода
                                background = this.SelBackBrush;
                                bSelAll = true;
                            }
                            else
                            {// возможны и другие цвета
                                if ((null != this.AltSolidBrush) || (null != this.AltSolidBrushSpec))
                                {                                                                   // If have alternating brush, row is odd and not selected...
                                    if (System.DBNull.Value != dr["READYZ"])
                                    {
                                        NSI.READINESS nState = (NSI.READINESS)dr["READYZ"];
                                        if (nState == NSI.READINESS.FULL_READY)
                                            background = this.AltSolidBrush;                         // Then use alternating brush.
                                        else
                                        {
                                            if (this.AltSolidBrushSpec != null)
                                            {
                                                //if (((System.DBNull.Value != dr["NP"]) && (((string)dr["NP"]).Length > 0)))
                                                //    background = this.AltSolidBrushSpec;                         // Then use alternating brush.
                                            }
                                        }
                                    }
                                    if (this.MappingName == "KOLM")
                                    {
                                        background = this.AltSolidBrushSpec;                         // Then use alternating brush.
                                    }
                                }
                            }
                            break;
                        case NSI.BD_DOUTD:
                            stDet = (AppC.OPR_STATE)dr["STATE"];
                            if ((null != this.AltSolidBrush) || (null != this.AltSolidBrushSpec))
                            {// для ТТН
                                if ((stDet & AppC.OPR_STATE.OPR_READY) > 0 )
                                {// готова к передаче
                                    if ((this.MappingName == "KOLE") || (this.MappingName == "KOLM") || (this.MappingName == "SNM"))
                                    background = this.AltSolidBrush;
                                }
                                if ((stDet & AppC.OPR_STATE.OPR_TRANSFERED) > 0)
                                {// готова к передаче
                                    if ((this.MappingName == "SNM"))
                                        background = this.AltSolidBrushSpec;
                                }
                            }
                            break;
                    }



                    //if (this.TableInd == NSI.BD_DIND)
                    //{// колонка для заявок
                    //    if (MainF.drEasyEdit == dr)
                    //    {// режим быстрого ввода
                    //        background = this.SelBackBrush;
                    //        bSelAll = true;
                    //    }
                    //    else
                    //    {// возможны и другие цвета
                    //        if ((null != this.AltSolidBrush) || (null != this.AltSolidBrushSpec))
                    //        {                                                                   // If have alternating brush, row is odd and not selected...
                    //            if ((bSel == false) && (System.DBNull.Value != dr["READYZ"]))
                    //            {
                    //                NSI.READINESS nState = (NSI.READINESS)dr["READYZ"];
                    //                if (nState == NSI.READINESS.FULL_READY)
                    //                    background = this.AltSolidBrush;                         // Then use alternating brush.
                    //                else
                    //                {
                    //                    if (this.AltSolidBrushSpec != null)
                    //                    {
                    //                        if (((System.DBNull.Value != dr["NP"]) && ((int)dr["NP"] > 0)))
                    //                            background = this.AltSolidBrushSpec;                         // Then use alternating brush.
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                }

                g.FillRectangle(background, bounds);
                return (bSelAll);
            }

            protected override void Paint(Graphics g, Rectangle bounds, CurrencyManager source, int rowNum, Brush backBrush, Brush foreBrush, bool alignToRight)
            {
                int
                    nWMIncFont = 2;
#if WMOBILE
                nWMIncFont = 1;
#endif
                if ((this.TableInd == NSI.BD_DIND) || (this.TableInd == NSI.BD_DOUTD))
                {
                    if ((this.MappingName == "DVR") || (this.MappingName == "DTG") || (this.MappingName == "KOLM"))
                    {
                        RectangleF textBounds;                                              // Bounds of text 
                        Object cellData;                                                    // Object to show in the cell 
                        Font
                            xSpecFont;
                        string s;

                        bool bSell = DrawBackground(g, bounds, rowNum, backBrush,
                            ((System.Data.DataRowView)source.List[rowNum]).Row);            // Draw cell background

                        bounds.Inflate(-2, -2);                                             // Shrink cell by couple pixels for text.

                        textBounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                        // Set text bounds.
                        cellData = this.PropertyDescriptor.GetValue(source.List[rowNum]);   // Get data for this cell from data source.
                        if ((this.MappingName == "DVR") || (this.MappingName == "DTG"))
                        {
                            try
                            {
                                s = ((string)cellData);
                                s = s.Substring(6, 2) + "." + s.Substring(4, 2);
                                cellData = s;
                            }
                            catch
                            {
                                cellData = "";
                            }
                            xSpecFont = this.Owner.Font;
                        }
                        else
                        {
                            xSpecFont = new Font(this.Owner.Font.Name, this.Owner.Font.Size + nWMIncFont, FontStyle.Bold);

                        }
                        if (bSell == true)
                            foreBrush = this.SelForeBrush;

                        g.DrawString(FormatText(cellData), xSpecFont, foreBrush, textBounds, this.StringFormat);
                        // Render contents 
                        this.updateHostedControl();                                         // Update floating hosted control.
                        return;
                    }
                }
                base.Paint(g, bounds, source, rowNum, backBrush, foreBrush, alignToRight);
            }


        }


        // Для подсветки строк (документы)
        public class DGTBoxColorColumnDoc : DGCustomColumn
        {
            // Let'sTypDoc add this so user can access 
            public virtual TextBox TextBox
            {
                get { return this.HostedControl as TextBox; }
            }

            protected override string GetBoundPropertyName()
            {
                return "Text";                                                          // Need to bount to "Text" property on TextBox
            }

            protected override Control CreateHostedControl()
            {
                TextBox box = new TextBox();                                            // Our hosted control is a TextBox

                box.BorderStyle = BorderStyle.None;                                     // It has no border
                box.Multiline = true;                                                   // And it'sTypDoc multiline
                box.TextAlign = this.Alignment;                                         // Set up aligment.

                return box;
            }

            protected override bool DrawBackground(Graphics g, Rectangle bounds, int rowNum, Brush backBrush, System.Data.DataRow dr)
            {
                Brush background = backBrush;                                       // Use default brush by... hmm... default.

                if ((null != this.AltSolidBrush) || (null != this.AltSolidBrushSpec))
                {                                                                   // If have alternating brush, row is odd and not selected...
                    bool bSel = (((SolidBrush)backBrush).Color != Owner.SelectionBackColor) ? false : true;
                    if ((bSel == false) && (System.DBNull.Value != dr["DIFF"]))
                    {
                        NSI.DOCCTRL nState = (NSI.DOCCTRL)dr["DIFF"];
                        if (nState == NSI.DOCCTRL.OK)
                            background = this.AltSolidBrush;                         // Then use alternating brush.
                        else
                        {
                            if (this.AltSolidBrushSpec != null)
                            {
                                if (((System.DBNull.Value != dr["DIFF"]) && 
                                    ((NSI.DOCCTRL)dr["DIFF"] == NSI.DOCCTRL.WARNS)))
                                    background = this.AltSolidBrushSpec;                         // Then use alternating brush.
                            }
                        }
                    }
                }

                g.FillRectangle(background, bounds);                                // Draw cell background
                return (false);
            }



        }





        // Поиск активного Control на вкладке
        //public static Control GetPageControl(TabPage page, int nWhatFind)
        //{
        //    foreach (Control ctl in page.Controls)
        //    {
        //        if (nWhatFind == 0)
        //        {
        //            if (ctl.TabIndex == 0)
        //                return ctl;
        //        }
        //        else
        //        {
        //            if (ctl.Focused)
        //                return ctl;
        //        }
        //    }
        //    return (null);
        //}




        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUS
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public uint dwTotalPhys;
            public uint dwAvailPhys;
            public uint dwTotalPageFile;
            public uint dwAvailPageFile;
            public uint dwTotalVirtual;
            public uint dwAvailVirtual;
        }


        [DllImport("coredll")]
        static extern void GlobalMemoryStatus(ref MEMORYSTATUS buf);

        public static void MemInfo()
        {
            string s = "";
            MEMORYSTATUS memSt = new MEMORYSTATUS();
            GlobalMemoryStatus(ref memSt);

            uint i = (memSt.dwAvailPageFile / 1024);
            s += "Available Page File (kb):" + i.ToString() + "\r\n";

            i = (memSt.dwAvailPhys / 1024);
            s += "Available Virtual Memory (kb):" + i.ToString() + "\r\n";

            i = memSt.dwMemoryLoad;
            s += "Memory In Use :" + i.ToString() + "\r\n";

            i = (memSt.dwTotalPageFile / 1024);
            s += "Total Page Size (kb):" + i.ToString() + "\r\n";

            i = (memSt.dwTotalPhys / 1024);
            s += "Total Physical Memory (kb):" + i.ToString() + "\r\n";


            i = (memSt.dwTotalVirtual / 1024);
            s += "Total Virtual Memory (kb):" + i.ToString();

            MessageBox.Show(s, "Memory_Stat");

        }


        // Это что ? Подсветка ошибочного ввода ?
        public static void TBColor(TextBox tb, bool bBadVal)
        {
            if (bBadVal == true)
            {
                Color c = tb.BackColor;
            }
            else
            {
                Color c = tb.BackColor;
            }

        }


        //public class HelpShowList
        //{

                
        //    internal const int
        //        HELPLINES = 19,                     // строк в окне Help
        //        HELPLWIDTH = 33,
        //        SHOW_MANY = 1,
        //        SHOW_ONE = 2;

        //    private System.Windows.Forms.Panel 
        //        m_Panel;
        //    private System.Windows.Forms.Label 
        //        m_HelpStr;
        //    private System.Windows.Forms.TextBox 
        //        m_MainHelpTBox;
        //    private System.Windows.Forms.ListBox
        //        m_MainHelpLBox;

        //    private Control
        //        m_MainHelp,
        //        xParent = null,
        //        xBeforeFocused = null;
        //    private bool
        //        IsTBox = true;

        //    // текущая инфо
        //    private object 
        //        xInf;
        //    // тип представлеия: массив или список
        //    private bool m_IsArray;

        //    private int
        //        m_WinMode = SHOW_MANY,
        //        nMaxLines = HELPLINES,          // текущий размер окна вывода в строках
        //        nHelpInd = 0;                   // текущий индекс вывода

        //    private System.Single
        //        m_ssFSize = 9F;

        //    // обработчик клавиш для текущей функции
        //    private Srv.CurrFuncKeyHandler 
        //        ehThis = null;

        //    public HelpShowList() : this(null, true) { }

        //    public HelpShowList(Control xP, bool bUseTBox)
        //    {
        //        CreateHelpPanel(true);
        //        ehThis = new Srv.CurrFuncKeyHandler(HelpKeyDown);
        //        xParent = xP;
        //        m_WinMode = SHOW_MANY;
        //        IsTBox = bUseTBox;
        //    }

        //    public HelpShowList(Control xP, Rectangle screen, int nMaxLinesInf, System.Single FontSize, int HelpStringHeight, bool bUseTBox)
        //    {
        //        m_ssFSize = FontSize;
        //        nMaxLines = nMaxLinesInf;          // текущий размер окна вывода в строках

        //        CreateHelpPanel(screen, nMaxLines, HelpStringHeight);
        //        ehThis = new Srv.CurrFuncKeyHandler(HelpKeyDown);
        //        xParent = xP;
        //        m_WinMode = SHOW_ONE;
        //        IsTBox = bUseTBox;
        //    }


        //    private Panel CreateHelpPanel(bool bUseTBox)
        //    {
        //        Rectangle screen = Screen.PrimaryScreen.Bounds;
        //        if ((screen.Height == 240) && (screen.Width == 320))
        //            nMaxLines = 14;
        //        else
        //            nMaxLines = HELPLINES;


        //        m_Panel = new Panel();
        //        m_HelpStr = new Label();
        //        if (bUseTBox)
        //            m_MainHelp = new TextBox();
        //        else
        //            m_MainHelp = new ListBox();

        //        m_Panel.SuspendLayout();

        //        // 
        //        // pnHelp
        //        // 
        //        m_Panel.Name = "pnHelp";
        //        m_Panel.Controls.Add(m_HelpStr);
        //        m_Panel.Controls.Add(m_MainHelp);
        //        m_Panel.BackColor = System.Drawing.Color.MediumBlue;
        //        //m_Panel.Size = new System.Drawing.Size(240, 320);

        //        m_Panel.Size = new System.Drawing.Size(screen.Width, screen.Height);
        //        //m_Panel.Location = new Point((PanelRect.Width - this.Width) / 2,
        //        //    (PanelRect.Height - this.Height) / 2);
        //        m_Panel.Location = new Point(0, 0);

        //        m_Panel.Visible = false;

        //        // 
        //        // Подсказка
        //        // 
        //        const int nHStrHeight = 15;
        //        m_HelpStr.Name = "m_HelpStr";
        //        m_HelpStr.ForeColor = System.Drawing.Color.WhiteSmoke;

        //        m_HelpStr.Size = new System.Drawing.Size((int)(m_Panel.Width * 0.8), nHStrHeight);
        //        m_HelpStr.Location = new System.Drawing.Point((screen.Width - m_HelpStr.Width) / 2,
        //            screen.Height - nHStrHeight - (nHStrHeight / 2));

        //        m_HelpStr.Text = "Enter-дальше    Esc-закрыть";
        //        m_HelpStr.TextAlign = System.Drawing.ContentAlignment.TopCenter;

        //        // 
        //        // Окно вывода
        //        // 
        //        m_MainHelp.BackColor = System.Drawing.Color.Lavender;
        //        m_MainHelp.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular);
        //        m_MainHelp.Location = new System.Drawing.Point(2, 3);
        //        if (bUseTBox)
        //        {
        //            ((TextBox)m_MainHelp).Multiline = true;
        //            ((TextBox)m_MainHelp).WordWrap = false;
        //        }
        //        m_MainHelp.Name = "m_MainHelp";
        //        //m_MainHelp.Size = new System.Drawing.Size(236, 296);
        //        m_MainHelp.Size = new System.Drawing.Size(screen.Width - 4, screen.Height - nHStrHeight - 9);
        //        m_MainHelp.TabIndex = 0;

        //        m_Panel.ResumeLayout();

        //        return (m_Panel);
        //    }

        //    private void AjustMainHelp(Rectangle screen, int HelpStringHeight, bool bUseTBox)
        //    {
        //        // 
        //        // Окно вывода
        //        // 
        //        m_MainHelp = new TextBox();

        //        m_MainHelp.BackColor = System.Drawing.Color.Lavender;
        //        m_MainHelp.Font = new System.Drawing.Font("Courier New", m_ssFSize, System.Drawing.FontStyle.Bold);
        //        m_MainHelp.Location = new System.Drawing.Point(2, 3);
        //        if (bUseTBox)
        //        {
        //            ((TextBox)m_MainHelp).Multiline = true;
        //            ((TextBox)m_MainHelp).WordWrap = false;
        //            ((TextBox)m_MainHelp).TextAlign = HorizontalAlignment.Center;
        //        }
        //        m_MainHelp.Name = "m_MainHelp";
        //        //m_MainHelp.Size = new System.Drawing.Size(236, 296);
        //        m_MainHelp.Size = new System.Drawing.Size(screen.Width - 4, screen.Height - HelpStringHeight - 6);
        //        m_MainHelp.TabIndex = 0;

        //    }

        //    private void AjustHelpString(Rectangle screen, int nHStrHeight)
        //    {
        //        if (nHStrHeight > 0)
        //        {
        //            m_HelpStr = new Label();

        //            // 
        //            // Подсказка
        //            // 
        //            m_HelpStr.Name = "m_HelpStr";
        //            m_HelpStr.ForeColor = System.Drawing.Color.WhiteSmoke;

        //            m_HelpStr.Size = new System.Drawing.Size((int)(m_Panel.Width * 0.8), nHStrHeight);
        //            m_HelpStr.Location = new System.Drawing.Point((screen.Width - m_HelpStr.Width) / 2,
        //                screen.Height - nHStrHeight - (nHStrHeight / 2));

        //            m_HelpStr.Text = "Enter-дальше Tab Esc-закрыть";
        //            m_HelpStr.TextAlign = System.Drawing.ContentAlignment.TopCenter;
        //        }
        //    }


        //    private Panel CreateHelpPanel(Rectangle PanelRect, int nMaxLines, int HelpStringHeight)
        //    {
        //        m_Panel = new Panel();

        //        m_Panel.SuspendLayout();

        //        // 
        //        // pnHelp
        //        // 
        //        m_Panel.Name = "pnHelp";
        //        m_Panel.BackColor = System.Drawing.Color.MediumBlue;
        //        //m_Panel.Size = new System.Drawing.Size(240, 320);

        //        m_Panel.Size = PanelRect.Size;
        //        m_Panel.Location = PanelRect.Location;
        //        m_Panel.Visible = false;


        //        // 
        //        // Подсказка
        //        // 
        //        if (HelpStringHeight > 0)
        //        {
        //            AjustHelpString(PanelRect, HelpStringHeight);
        //            m_Panel.Controls.Add(m_HelpStr);
        //        }

        //        // 
        //        // Окно вывода
        //        // 
        //        AjustMainHelp(PanelRect, HelpStringHeight, true);
        //        m_Panel.Controls.Add(m_MainHelp);
        //        m_Panel.ResumeLayout();

        //        return (m_Panel);
        //    }



        //    public void ShowInfo(object xI, ref Srv.CurrFuncKeyHandler khCurr)
        //    {
        //        ShowInfo(null, null, xI, ref khCurr);
        //    }


        //    public void ShowInfo(Control xPrnt, Control xBF, object xI, ref Srv.CurrFuncKeyHandler khCurr)
        //    {
        //        if (xPrnt != null)
        //            xParent = xPrnt;
        //        if (xBF != null)
        //            xBeforeFocused = xBF;
        //        xInf = xI;

        //        xParent.Controls.Add(m_Panel);
        //        m_Panel.SuspendLayout();
        //        nHelpInd = 0;

        //        if (xInf.GetType().IsArray == true)
        //        {
        //            m_IsArray = true;
        //            m_MainHelp.Text = ((string[])xInf)[0];
        //        }
        //        else
        //        {
        //            m_IsArray = false;
        //            m_MainHelp.Text = NextInfPart();
        //        }

        //        m_Panel.Visible = true;
        //        m_Panel.BringToFront();

        //        m_Panel.ResumeLayout();

        //        khCurr += ehThis;
        //    }

        //    private string NextInfPart()
        //    {
        //        string
        //            sRet = "";
        //        int
        //            nStart = (nHelpInd * nMaxLines),
        //            nEnd;

        //        if (nStart >= ((List<string>)xInf).Count)
        //        {
        //            nHelpInd = 0;
        //            nStart = 0;
        //        }
        //        nEnd = Math.Min(((List<string>)xInf).Count, nStart + nMaxLines);

        //        for (int i = nStart; i < nEnd; i++)
        //            sRet += ((List<string>)xInf)[i] + "\r\n";
        //        if (sRet.Length > 0)
        //            sRet = sRet.Remove(sRet.Length - 2, 2);
        //        return (sRet);
        //    }

        //    private bool HelpKeyDown(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
        //    {
        //        int
        //            nFunc = (int)nF;
        //        bool
        //            bKeyHandled = true,
        //            bCloseHelp = false;
        //        string
        //            sH = "";

        //        if (nFunc > 0)
        //        {
        //            bCloseHelp = true;
        //            if (nFunc != PDA.Service.AppC.F_HELP)
        //                bKeyHandled = false;
        //        }
        //        else
        //        {
        //            switch (e.KeyValue)
        //            {
        //                case W32.VK_TAB:
        //                    if (IsTBox)
        //                        ((TextBox)m_MainHelp).WordWrap = !((TextBox)m_MainHelp).WordWrap;
        //                    break;
        //                case W32.VK_ESC:
        //                    bCloseHelp = true;
        //                    break;
        //                case W32.VK_ENTER:
        //                    nHelpInd++;
        //                    if (m_IsArray)
        //                    {
        //                        if (nHelpInd == ((string[])xInf).Length)
        //                            nHelpInd = 0;
        //                        sH = ((string[])xInf)[nHelpInd];
        //                    }
        //                    else
        //                        sH = NextInfPart();

        //                    m_MainHelp.Text = sH;
        //                    break;
        //                default:
        //                    bKeyHandled = false;
        //                    break;
        //            }
        //        }
        //        if (m_WinMode == SHOW_ONE)
        //        {
        //            bCloseHelp = true;
        //            bKeyHandled = false;
        //        }

        //        if (bCloseHelp == true)
        //        {
        //            StopShow(ref kh);
        //        }

        //        e.Handled = bKeyHandled;
        //        return (bKeyHandled);
        //    }

        //    public void StopShow(ref Srv.CurrFuncKeyHandler kh)
        //    {

        //        if (m_Panel.Visible)
        //        {
        //            m_Panel.SuspendLayout();
        //            m_Panel.Visible = false;
        //            m_Panel.SendToBack();
        //            if (xParent != null)
        //                xParent.Controls.Remove(m_Panel);
        //            m_Panel.ResumeLayout();
        //            kh -= ehThis;
        //            if (xBeforeFocused != null)
        //                xBeforeFocused.Focus();
        //            xBeforeFocused = null;
        //        }
        //    }

        //    public Control PanelParent
        //    {
        //        get { return xParent; }
        //        set { xParent = value; }
        //    }

        //    // куда вернуть фокус
        //    public Control PreviosControl
        //    {
        //        get { return xBeforeFocused; }
        //        set { xBeforeFocused = value; }
        //    }

        //    public Control ThisPanel
        //    {
        //        get { return m_Panel; }
        //    }


        //}


    }


    #region Неизвестно что

        //public DateTime GetNTPTime()
        //{

        //    // 0x1B == 0b11011 == NTP version 3, client - see RFC 2030
        //    byte[] ntpPacket = new byte[] { 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        //    IPAddress[] addressList = Dns.GetHostEntry("pool.ntp.org").AddressList;

        //    if (addressList.Length == 0)
        //    {
        //        // error
        //        return DateTime.MinValue;
        //    }

        //    IPEndPoint ep = new IPEndPoint(addressList[0], 123);
        //    UdpClient client = new UdpClient();
        //    client.Connect(ep);
        //    client.Send(ntpPacket, ntpPacket.Length);
        //    byte[] sBarCode = client.Receive(ref ep);

        //    // receive date sBarCode is at offset 32
        //    // Data is 64 bits - first 32 is seconds - we'll toss the fraction of a second
        //    // it is not in an endian order, so we must rearrange
        //    byte[] endianSeconds = new byte[4];
        //    endianSeconds[0] = (byte)(sBarCode[32 + 3] & (byte)0x7F); // turn off MSB (some servers set it)
        //    endianSeconds[1] = sBarCode[32 + 2];
        //    endianSeconds[2] = sBarCode[32 + 1];
        //    endianSeconds[3] = sBarCode[32 + 0];
        //    uint seconds = BitConverter.ToUInt32(endianSeconds, 0);

        //    return (new DateTime(1900, 1, 1)).AddSeconds(seconds);
        //}






    #endregion



    #region Not_Used_But_Tested
/*
/* Замена Enter на Tab

 tried SelectNextControl() and couldn't get it to work like the ordinary hardware tab-button. I ended up using P/Invoke and keybd_event() to simulate tab key presses. It works perfectly and you only need to set one form property to true and add one form event handler (not one event handler for every control). If you put the code below in a base form and derive all your forms from that form you will get tab-handling automatically - set it and forget it.

1. Add the following P/Invoke code. NOTE: You should wrap this code in a conditional define (for instance DESIGN) which is true when you need VS design support. The VS designer will not let you design your forms if you have P/Invoke code anywhere in your project...

#if !DESIGN
     [DllImport("coredll.dll")]
    internal extern static void keybd_event(byte bVk, byte bScan, Int32 dwFlags, Int32   dwExtraInfo);
    internal const int KEYEVENTF_KEYUP = 0x02;
    internal const int VK_TAB = 0x09;
    internal const int VK_SHIFT = 0x10;
#endif


2. Set property KeyPreview to true for the form. This gives the form a chance to see the key presses before the controls get them.


3. Add an event handler for the key down event of the form:

private void Form_KeyDown(object sender, KeyEventArgs e)
{
#if !DESIGN
  if (e.KeyCode == System.Windows.Forms.Keys.Up)
  {
    keybd_event(VK_SHIFT, VK_SHIFT, 0, 0);
    keybd_event(VK_TAB, VK_TAB, 0, 0);
    keybd_event(VK_TAB, VK_TAB, KEYEVENTF_KEYUP, 0);
    keybd_event(VK_SHIFT, VK_SHIFT, KEYEVENTF_KEYUP, 0);
    e.Handled = true;
  }
  else if(e.KeyCode == System.Windows.Forms.Keys.Down)
  {
    keybd_event(VK_TAB, VK_TAB, 0, 0);
    keybd_event(VK_TAB, VK_TAB, KEYEVENTF_KEYUP, 0);
    e.Handled = true;
  }
#endif
}

    --- сабклассинг ---
 * 
    using System.Drawing;
    using System.Runtime.InteropServices;

    public sealed class Win32
    {
        /// <summary>
        /// A callback to a Win32 window procedure (wndproc)
        /// </summary>
        /// <param m_Name="hwnd">The handle of the window receiving a message</param>
        /// <param m_Name="msg">The message</param>
        /// <param m_Name="wParam">The message'sTypDoc parameters (part 1)</param>
        /// <param m_Name="lParam">The message'sTypDoc parameters (part 2)</param>
        /// <returns>A integer as described for the given message in MSDN</returns>
        public delegate int WndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);

#if DESKTOP
    [DllImport("user32.dll")]
#else
        [DllImport("coredll.dll")]
#endif
        public extern static int DefWindowProc(
            IntPtr hwnd, uint msg, uint wParam, int lParam);

#if DESKTOP
    [DllImport("user32.dll")]
#else
        [DllImport("coredll.dll")]
#endif
        public extern static IntPtr SetWindowLong(
            IntPtr hwnd, int nIndex, IntPtr dwNewLong);

        public const int GWL_WNDPROC = -4;

#if DESKTOP
    [DllImport("user32.dll")]
#else
        [DllImport("coredll.dll")]
#endif
        public extern static int CallWindowProc(
            IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, uint wParam, int lParam);
    }

    class WndProcHooker
    {
        /// <summary>
        /// The callback used when a hooked window'sTypDoc message map contains the
        /// hooked message
        /// </summary>
        /// <param m_Name="hwnd">The handle to the window for which the message
        /// was received</param>
        /// <param m_Name="wParam">The message'sTypDoc parameters (part 1)</param>
        /// <param m_Name="lParam">The message'sTypDoc parameters (part 2)</param>
        /// <param m_Name="handled">The invoked function sets this to true if it
        /// handled the message. If the value is false when the callback
        /// returns, the next window procedure in the wndproc chain is
        /// called</param>
        /// <returns>A value specified for the given message in the MSDN
        /// documentation</returns>
        public delegate int WndProcCallback(
            IntPtr hwnd, uint msg, uint wParam, int lParam, ref bool handled);

        /// <summary>
        /// This is the global list of all the window procedures we have
        /// hooked. The key is an hwnd. The value is a HookedProcInformation
        /// object which contains a pointer to the old wndproc and a map of
        /// messages/callbacks for the window specified. Controls whose handles
        /// have been created go into this dictionary.
        /// </summary>
        private static Dictionary<IntPtr, HookedProcInformation> hwndDict =
            new Dictionary<IntPtr, HookedProcInformation>();

        /// <summary>
        /// See <see>hwndDict</see>. The key is a control and the value is a
        /// HookedProcInformation. Controls whose handles have not been created
        /// go into this dictionary. When the HandleCreated event for the
        /// control is fired the control is moved into <see>hwndDict</see>.
        /// </summary>
        private static Dictionary<Control, HookedProcInformation> ctlDict =
            new Dictionary<Control, HookedProcInformation>();

        /// <summary>
        /// Makes a connection between a message on a specified window handle
        /// and the callback to be called when that message is received. If the
        /// window was not previously hooked it is added to the global list of
        /// all the window procedures hooked.
        /// </summary>
        /// <param m_Name="ctl">The control whose wndproc we are hooking</param>
        /// <param m_Name="callback">The method to call when the specified
        /// message is received for the specified window</param>
        /// <param m_Name="msg">The message we are hooking.</param>
        public static void HookWndProc(
            Control ctl, WndProcCallback callback, uint msg)
        {
            HookedProcInformation hpi = null;
            if (ctlDict.ContainsKey(ctl))
                hpi = ctlDict[ctl];
            else if (hwndDict.ContainsKey(ctl.Handle))
                hpi = hwndDict[ctl.Handle];
            if (hpi == null)
            {
                // We havne't seen this control before. Create a new
                // HookedProcInformation for it
                hpi = new HookedProcInformation(ctl,
                    new Win32.WndProc(WndProcHooker.WindowProc));
                ctl.HandleCreated += new EventHandler(ctl_HandleCreated);
                ctl.HandleDestroyed += new EventHandler(ctl_HandleDestroyed);
                ctl.Disposed += new EventHandler(ctl_Disposed);

                // If the handle has already been created set the hook. If it
                // hasn't been created yet, the hook will get set in the
                // ctl_HandleCreated event handler
                if (ctl.Handle != IntPtr.Zero)
                    hpi.SetHook();
            }

            // stick hpi into the correct dictionary
            if (ctl.Handle == IntPtr.Zero)
                ctlDict[ctl] = hpi;
            else
                hwndDict[ctl.Handle] = hpi;

            // add the message/callback into the message map
            hpi.messageMap[msg] = callback;
        }

        /// <summary>
        /// The event handler called when a control is disposed.
        /// </summary>
        /// <param m_Name="sender">The object that raised this event</param>
        /// <param m_Name="e">The arguments for this event</param>
        static void ctl_Disposed(object sender, EventArgs e)
        {
            Control ctl = sender as Control;
            if (ctlDict.ContainsKey(ctl))
                ctlDict.Remove(ctl);
            else
                System.Diagnostics.Debug.Assert(false);
        }

        /// <summary>
        /// The event handler called when a control'sTypDoc handle is destroyed.
        /// We remove the HookedProcInformation from <see>hwndDict</see> and
        /// put it back into <see>ctlDict</see> in case the control get re-
        /// created and we still want to hook its messages.
        /// </summary>
        /// <param m_Name="sender">The object that raised this event</param>
        /// <param m_Name="e">The arguments for this event</param>
        static void ctl_HandleDestroyed(object sender, EventArgs e)
        {
            // When the handle for a control is destroyed, we want to
            // unhook its wndproc and update our lists
            Control ctl = sender as Control;
            if (hwndDict.ContainsKey(ctl.Handle))
            {
                HookedProcInformation hpi = hwndDict[ctl.Handle];
                UnhookWndProc(ctl, false);
            }
            else
                System.Diagnostics.Debug.Assert(false);
        }

        /// <summary>
        /// The event handler called when a control'sTypDoc handle is created. We
        /// call SetHook() on the associated HookedProcInformation object and
        /// move it from <see>ctlDict</see> to <see>hwndDict</see>.
        /// </summary>
        /// <param m_Name="sender"></param>
        /// <param m_Name="e"></param>
        static void ctl_HandleCreated(object sender, EventArgs e)
        {
            Control ctl = sender as Control;
            if (ctlDict.ContainsKey(ctl))
            {
                HookedProcInformation hpi = ctlDict[ctl];
                hwndDict[ctl.Handle] = hpi;
                ctlDict.Remove(ctl);
                hpi.SetHook();
            }
            else
                System.Diagnostics.Debug.Assert(false);
        }

        /// <summary>
        /// This is a generic wndproc. It is the callback for all hooked
        /// windows. If we get into this function, we look up the hwnd in the
        /// global list of all hooked windows to get its message map. If the
        /// message received is present in the message map, its callback is
        /// invoked with the parameters listed here.
        /// </summary>
        /// <param m_Name="hwnd">The handle to the window that received the
        /// message</param>
        /// <param m_Name="msg">The message</param>
        /// <param m_Name="wParam">The message'sTypDoc parameters (part 1)</param>
        /// <param m_Name="lParam">The messages'sTypDoc parameters (part 2)</param>
        /// <returns>If the callback handled the message, the callback'sTypDoc return
        /// value is returned form this function. If the callback didn't handle
        /// the message, the message is forwarded on to the previous wndproc.
        /// </returns>
        private static int WindowProc(
            IntPtr hwnd, uint msg, uint wParam, int lParam)
        {
            if (hwndDict.ContainsKey(hwnd))
            {
                HookedProcInformation hpi = hwndDict[hwnd];
                if (hpi.messageMap.ContainsKey(msg))
                {
                    WndProcCallback callback = hpi.messageMap[msg];
                    bool handled = false;
                    int retval = callback(hwnd, msg, wParam, lParam, ref handled);
                    if (handled)
                        return retval;
                }

                // if we didn't hook the message passed or we did, but the
                // callback didn't set the handled property to true, call
                // the original window procedure
                return hpi.CallOldWindowProc(hwnd, msg, wParam, lParam);
            }

            System.Diagnostics.Debug.Assert(
                false, "WindowProc called for hwnd we don't know about");
            return Win32.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        /// <summary>
        /// This method removes the specified message from the message map for
        /// the specified hwnd.
        /// </summary>
        /// <param m_Name="ctl">The control whose message we are unhooking
        /// </param>
        /// <param m_Name="msg">The message no longer want to hook</param>
        public static void UnhookWndProc(Control ctl, uint msg)
        {
            // look for the HookedProcInformation in the control and hwnd
            // dictionaries
            HookedProcInformation hpi = null;
            if (ctlDict.ContainsKey(ctl))
                hpi = ctlDict[ctl];
            else if (hwndDict.ContainsKey(ctl.Handle))
                hpi = hwndDict[ctl.Handle];

            // if we couldn't find a HookedProcInformation, throw
            if (hpi == null)
                throw new ArgumentException("No hook exists for this control");

            // look for the message we are removing in the messageMap
            if (hpi.messageMap.ContainsKey(msg))
                hpi.messageMap.Remove(msg);
            else
                // if we couldn't find the message, throw
                throw new ArgumentException(
                    string.Format(
                        "No hook exists for message ({0}) on this control",
                         msg));
        }

        /// <summary>
        /// Restores the previous wndproc for the specified window.
        /// </summary>
        /// <param m_Name="ctl">The control whose wndproc we no longer want to
        /// hook</param>
        /// <param m_Name="disposing">if true we remove don't readd the
        /// HookedProcInformation
        /// back into ctlDict</param>
        public static void UnhookWndProc(Control ctl, bool disposing)
        {
            HookedProcInformation hpi = null;
            if (ctlDict.ContainsKey(ctl))
                hpi = ctlDict[ctl];
            else if (hwndDict.ContainsKey(ctl.Handle))
                hpi = hwndDict[ctl.Handle];

            if (hpi == null)
                throw new ArgumentException("No hook exists for this control");

            // If we found our HookedProcInformation in ctlDict and we are
            // disposing remove it from ctlDict
            if (ctlDict.ContainsKey(ctl) && disposing)
                ctlDict.Remove(ctl);

            // If we found our HookedProcInformation in hwndDict, remove it
            // and if we are not disposing stick it in ctlDict
            if (hwndDict.ContainsKey(ctl.Handle))
            {
                hpi.Unhook();
                hwndDict.Remove(ctl.Handle);
                if (!disposing)
                    ctlDict[ctl] = hpi;
            }
        }

        /// <summary>
        /// This class remembers the old window procedure for the specified
        /// window handle and also provides the message map for the messages
        /// hooked on that window.
        /// </summary>
        class HookedProcInformation
        {
            /// <summary>
            /// The message map for the window
            /// </summary>
            public Dictionary<uint, WndProcCallback> messageMap;

            /// <summary>
            /// The old window procedure for the window
            /// </summary>
            private IntPtr oldWndProc;

            /// <summary>
            /// The delegate that gets called in place of this window'sTypDoc
            /// wndproc.
            /// </summary>
            private Win32.WndProc newWndProc;

            /// <summary>
            /// Control whose wndproc we are hooking
            /// </summary>
            private Control control;

            /// <summary>
            /// Constructs a new HookedProcInformation object
            /// </summary>
            /// <param m_Name="ctl">The handle to the window being hooked</param>
            /// <param m_Name="wndproc">The window procedure to replace the
            /// original one for the control</param>
            public HookedProcInformation(Control ctl, Win32.WndProc wndproc)
            {
                control = ctl;
                newWndProc = wndproc;
                messageMap = new Dictionary<uint, WndProcCallback>();
            }

            /// <summary>
            /// Replaces the windows procedure for <see>control</see> with the
            /// one specified in the constructor.
            /// </summary>
            public void SetHook()
            {
                IntPtr hwnd = control.Handle;
                if (hwnd == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Handle for control has not been created");

                oldWndProc = Win32.SetWindowLong(hwnd, Win32.GWL_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(newWndProc));
            }

            /// <summary>
            /// Restores the original window procedure for the control.
            /// </summary>
            public void Unhook()
            {
                IntPtr hwnd = control.Handle;
                if (hwnd == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Handle for control has not been created");

                Win32.SetWindowLong(hwnd, Win32.GWL_WNDPROC, oldWndProc);
            }

            /// <summary>
            /// Calls the original window procedure of the control with the
            /// arguments provided.
            /// </summary>
            /// <param m_Name="hwnd">The handle of the window that received the
            /// message</param>
            /// <param m_Name="msg">The message</param>
            /// <param m_Name="wParam">The message'sTypDoc arguments (part 1)</param>
            /// <param m_Name="lParam">The message'sTypDoc arguments (part 2)</param>
            /// <returns>The value returned by the control'sTypDoc original wndproc
            /// </returns>
            public int CallOldWindowProc(
                    IntPtr hwnd, uint msg, uint wParam, int lParam)
            {
                return Win32.CallWindowProc(
                    oldWndProc, hwnd, msg, wParam, lParam);
            }
        }
    }
 * 
 * Form1 - Load
             WndProcHooker wphF = new WndProcHooker();
            WndProcHooker.HookWndProc(this.tcMain, OnKeyDown, W32.WM_KEYDOWN);
* 
         private int OnKeyDown(IntPtr hwnd, uint msg, uint wParam, int lParam, ref bool handled)
        {
            int ret = 0;
            switch (msg)
            {
                case W32.WM_SCANNED:
                    break;
                case W32.WM_KEYDOWN:
                    switch (wParam)
                    {
                        case W32.VK_SCAN:
                            break;
                        case W32.VK_HOME:
//                            CreateMMenu();
                            //handled = true;
                            break;
                    }
                    break;
                case W32.WM_KEYUP:
                    break;
            }
            return (ret);
        }
* 
 * 
 * 
 * 
 * 
 * 
 */
    #endregion




}
