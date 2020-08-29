using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

using SavuSocket;

using ScannerAll;
using PDA.Service;
using PDA.OS;
using SkladAll;
using SkladRM;

using FRACT = System.Decimal;

namespace SGPF_LstCh
{
    public partial class Shlyuz : Form
    {

        private string
            sEAN;

        private MainF xMainF = null;
        private NSI xNSI;

        private BindingSource bsSh;
        private DataView dvAvto;

        // не обрабатывать введенный символ
        private bool bSkipKey = false;

        // цвета для операций
        private Color 
            colCome = Color.PaleGreen,
            colGOut = Color.Gold;
        private BarcodeScanner.BarcodeScanEventHandler ehOldScan = null;



        private MainF.ServerExchange xSE;

        public Shlyuz()
        {
            InitializeComponent();
        }

        public void AfterConstruct(MainF xM, string e, int nKrKMC, bool bSelectEAN)
        {
            xMainF = xM;
            xNSI = xM.xNSI;
            sEAN = e;

            dvAvto = xNSI.DT[NSI.NS_MC].dt.DefaultView;

            bsSh = new BindingSource();
            bsSh.DataSource = dvAvto;
            if (bSelectEAN)
                lEAN.Text = String.Format("EAN13 = <{0}>", sEAN);
            else
                lEAN.Text = String.Format("Краткий код = <{0}>", nKrKMC);


            // Настройки грида
            dgShlyuz.SuspendLayout();
            dgShlyuz.DataSource = bsSh;
            ShlzStyle(dgShlyuz);
            dgShlyuz.ResumeLayout();

            if (bSelectEAN)
                bsSh.Filter = string.Format("EAN13='{0}'", sEAN);
            else
                bsSh.Filter = string.Format("KRKMC={0}", nKrKMC);
            bsSh.Sort = "KSK";
            bsSh.ResetBindings(false);
            dgShlyuz.Focus();

            ehOldScan = new BarcodeScanner.BarcodeScanEventHandler(OnScanPL);
            xMainF.xBCScanner.BarcodeScan += ehOldScan;

        }

        private void Shlyuz_Closing(object sender, CancelEventArgs e)
        {
            //CurrencyManager cmDet = (CurrencyManager)BindingContext[dgShlyuz.DataSource];
            DataRow
                dr;
            try
            {
                dr = ((DataRowView)bsSh.Current).Row;
            }
            catch
            {
                dr = null;
            }
            xMainF.xDLLAPars = new object[] { dr };
            bsSh.RemoveFilter();
            xMainF.xBCScanner.BarcodeScan -= ehOldScan;
        }


        private void OnScanPL(object sender, BarcodeScannerEventArgs e)
        {
            if (e.nID != BCId.NoData)
            {
            }
        }


        // выделение всего поля при входе (текстовые поля)
        private void SelAllTextF(object sender, EventArgs e)
        {
            TextBox xT = (TextBox)sender;
            xT.SelectAll();
        }

        public class DGTBox4Ch : DGCustomColumn
        {
            private MainF xF = null;
            public DGTBox4Ch() : this(null) { }

            public DGTBox4Ch(DataGrid dg)
                : base()
            {
                if (dg != null)
                    base.Owner = dg;
                base.ReadOnly = true;
                xF = NSI.xFF;
            }
            public DGTBox4Ch(DataGrid dg, string sTable)
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
                box.WordWrap = true;

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

                g.FillRectangle(background, bounds);
                return (bSelAll);
            }

            protected override void Paint(Graphics g, Rectangle bounds, CurrencyManager source, int rowNum, Brush backBrush, Brush foreBrush, bool alignToRight)
            {
                RectangleF
                    textBounds;                                              // Bounds of text 
                Object
                    cellData;                                                    // Object to show in the cell 
                //System.Data.DataRowView drv = (System.Data.DataRowView)source.List[rowNum]; 

                //source.List[rowNum].
                bool bSell = DrawBackground(g, bounds, rowNum, backBrush,
                    ((System.Data.DataRowView)source.List[rowNum]).Row);                       // Draw cell background

                bounds.Inflate(-2, -2);                                             // Shrink cell by couple pixels for text.

                textBounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                // Set text bounds.
                cellData = this.PropertyDescriptor.GetValue(source.List[rowNum]);   // Get sBarCode for this cell from sBarCode source.

                //if (bSell == true)
                //    foreBrush = this._selectedFore;

                Font xFnt = new System.Drawing.Font("Tahoma", 8F, FontStyle.Regular);
                g.DrawString(FormatText(cellData), xFnt, foreBrush, textBounds, this.StringFormat);
                // Render contents 
                this.updateHostedControl();                                         // Update floating hosted control.
            }

        }


        // стили таблицы авто
        private void ShlzStyle(DataGrid dg)
        {
            DGTBox4Ch sC;
            System.Drawing.Color
                colForFullAuto = System.Drawing.Color.LightGreen,
                colSpec = System.Drawing.Color.PaleGoldenrod;

            DataGridTableStyle ts = new DataGridTableStyle();
            ts.MappingName = NSI.NS_MC;

            sC = new DGTBox4Ch(dg, NSI.NS_MC);
            sC.MappingName = "KSK";
            sC.HeaderText = "Пп";
            sC.Width = 19;
            ts.GridColumnStyles.Add(sC);

            sC = new DGTBox4Ch(dg, NSI.NS_MC);
            sC.MappingName = "SNM";
            sC.HeaderText = "Наименование";
            sC.Width = 214;
            sC.StringFormat = new StringFormat();
            ts.GridColumnStyles.Add(sC);

            dg.TableStyles.Add(ts);
        }

        // Обработка клавиш
        private void Shlyuz_KeyDown(object sender, KeyEventArgs e)
        {
            int nFunc = 0;
            bool 
                ret = false;

            bSkipKey = false;
            nFunc = xMainF.xFuncs.TryGetFunc(e);
            if (nFunc > 0)
            {
                switch (nFunc)
                {
                    case AppC.F_LOAD_DOC:
                        // загрузка нового списка
                        break;
                    case AppC.F_UPLD_DOC:
                        break;
                }
            }
            else
            {

                switch (e.KeyValue)
                {
                    case W32.VK_ESC:
                        this.DialogResult = DialogResult.Cancel;
                        ret = true;
                        break;
                    case W32.VK_ENTER:
                        this.DialogResult = DialogResult.OK;
                        ret = true;
                        break;
                    case W32.VK_RIGHT:
                    case W32.VK_LEFT:
                        ret = false;
                        break;
                    default:
                        break;
                }
            }
            e.Handled = ret;
            bSkipKey = ret;

        }

        private void Shlyuz_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (bSkipKey == true)
            {
                bSkipKey = false;
                e.Handled = true;
            }
        }



        private void dgShlyuz_GotFocus(object sender, EventArgs e)
        {
        }


        private void dgShlyuz_LostFocus(object sender, EventArgs e)
        {
        }


        // для передачи параметров в форму
        private void Shlyuz_Activated(object sender, EventArgs e)
        {
            if (this.Tag != null)
            {
                object[] aP = (object[])this.Tag;
                AfterConstruct((MainF)aP[0], (string)aP[1], (int)aP[2], (bool)aP[3]);
                this.Tag = null;
            }
        }

    }
}