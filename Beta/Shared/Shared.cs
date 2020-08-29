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
        // список Control для редактирования
        public class EditListC : List<Control>
        {
            private int
                m_CurI;

            private AppC.VerifyEditFields
                dgVer;

            private Control
                m_CtrlkBtwn = null,
                m_Cur = null;


            public VerRet VV()
            {
                VerRet v;
                v.nRet = AppC.RC_OK;
                v.cWhereFocus = null;
                return (v);
            }


            public EditListC()
                : base()
            {
                dgVer = new VerifyEditFields(VV);
            }


            public EditListC(AppC.VerifyEditFields dgx)
                : base()
            {
                dgVer = dgx;
            }

            private void CreateFict(Control xC)
            {
                Fict4Next = new TextBox();
                Fict4Next.SuspendLayout();
                Fict4Next.Name = String.Format("TMP_Ed{0}", DateTime.Now.Ticks / 100000);
                Fict4Next.Visible = false;
                Fict4Next.Enabled = true;
                Fict4Next.Parent = xC.Parent;
                Fict4Next.ResumeLayout();
            }

            // добавить с список доступных контроловв ввода/редактирования
            public void AddC(Control xC)
            {
                AddC(xC, true);
            }

            public void AddC(Control xC, bool bEn)
            {
                xC.Enabled = bEn;
                base.Add(xC);
                if (Fict4Next == null)
                {
                    CreateFict(xC);
                }
            }

            // сделать указанный контрол текущим
            public int SetCur(Control xC)
            {
                m_Cur = xC;
                m_CurI = base.FindIndex(IsSame);
                xC.Focus();
                return (m_CurI);
            }

            // сделать указанный по индексу контрол текущим
            public Control SetCur(int i)
            {
                m_Cur = base[i];
                m_CurI = i;
                m_Cur.Focus();
                return (m_Cur);
            }

            // текущий 
            public Control Current
            {
                get { return m_Cur; }
                set { m_Cur = value; }
            }

            // Фиктивный для переходов
            public Control Fict4Next
            {
                get { return m_CtrlkBtwn; }
                set { m_CtrlkBtwn = value; }
            }


            // определить текущий контрол среди списка
            public Control WhichCur()
            {
                Control xC = null;
                for (int i = 0; i < base.Count; i++)
                {
                    //if (base[i].Focused)
                    if (this[i].Focused)
                    {
                        //m_Cur = xC = base[i];
                        m_Cur = xC = this[i];
                        m_CurI = i;
                        break;
                    }
                }
                //if ((xC == null) && (m_CurI >= 0))
                //{
                //    xC = SetCur(m_CurI);
                //}
                return (xC);
            }


            // определить и установить текущий контрол
            public Control WhichSetCur()
            {
                Control xC = WhichCur();
                if (xC == null)
                {
                    //SetCur(base.FindIndex(IsNextOrPrev));
                    SetCur(this.FindIndex(IsNextOrPrev));
                    xC = Current;
                }
                return (xC);
            }


            private bool IsSame(Control x)
            {
                return ((x == m_Cur) ? true : false);
            }

            private bool IsNextOrPrev(Control x)
            {
                if (x == null)
                    x = m_Cur;
                return (x.Enabled);
            }

            private int TryMove(int i, bool bBack)
            {
                int
                    nRet = 0;
                return (nRet);
            }


            // попытка перехода на следующее поле при редактировании
            public bool TryNext(int nCommand)
            {
                int i = -1;
                bool bRet = AppC.RC_OKB;

                // можно попробовать сначала определить текущий, т.к.
                // он мог измениться нестандартным способом
                while ((!Current.Focused) && (i < base.Count))
                {
                    if (WhichCur() == null)
                    {// определить текущий не удалось
                        if (i == base.Count - 1)
                            return (AppC.RC_CANCELB);
                        else
                        {
                            if (base[++i].Enabled)
                            {
                                SetCur(i);
                                return (bRet);
                            }
                        }
                    }
                }

                // отработают все Valid
                //Current.Parent.Focus();
                Fict4Next.Focus();

                if (nCommand == AppC.CC_PREV)
                {// переход на предыдующий
                    i = (m_CurI > 0) ? base.FindLastIndex(m_CurI - 1, m_CurI, IsNextOrPrev) : -1;
                    if (i == -1)
                        i = base.FindLastIndex(base.Count - 1, base.Count, IsNextOrPrev);
                }
                else if ((nCommand == AppC.CC_NEXT) ||
                         (nCommand == AppC.CC_NEXTOVER))
                {
                    i = base.FindIndex(m_CurI + 1, IsNextOrPrev);
                    if (i == -1)
                    {
                        if (nCommand == AppC.CC_NEXTOVER)
                        {// следующего нет, это последнее поле
                            AppC.VerRet vRet = dgVer();
                            if (vRet.nRet == AppC.RC_OK)
                                return (AppC.RC_CANCELB);
                            if (vRet.cWhereFocus != null)
                            {
                                SetCur(vRet.cWhereFocus);
                                return (AppC.RC_OKB);
                            }
                        }
                        i = base.FindIndex(0, m_CurI, IsNextOrPrev);
                        if (i < 0)
                            //i = 0;
                            i = m_CurI;
                    }
                }

                if (i >= 0)
                    SetCur(i);
                else
                    bRet = AppC.RC_CANCELB;

                return (bRet);
            }



            public void EditIsOver()
            {
                for (int i = 0; i < base.Count; i++)
                    base[i].Enabled = false;
            }

            public void EditIsOver(Control x4Focus)
            {
                x4Focus.Focus();
                for (int i = 0; i < base.Count; i++)
                    base[i].Enabled = false;
            }

            public void EditIsOverEx(Control x4Focus)
            {
                for (int i = 0; i < base.Count; i++)
                {
                    if (base[i] != x4Focus)
                    {
                        base[i].Enabled = false;
                    }
                }
            }

            public void SetAvail(Control xC, bool bAvail)
            {
                int i = base.IndexOf(xC);
                if (i >= 0)
                {
                    bool bMayChange = true;
                    if ((xC == m_Cur) && (bAvail == false))
                        // пытаемся запретить текущий
                        bMayChange = TryNext(AppC.CC_NEXT);
                    if (bMayChange)
                        base[i].Enabled = bAvail;
                }
            }

        }

    }

}

namespace SkladRM
{
    public delegate void PrepareFields(int nReg, AppC.VerifyEditFields dgVerify);

    interface IMeth4Edit
    {
        void SetFields4EE(int nReg, AppC.VerifyEditFields dgVerify);
    }

    public class NewOrEdit
    {
        int
            m_nReg;
        AppC.VerifyEditFields
            m_dgVerify;
        PrepareFields
            m_dgPrep;

        public NewOrEdit(int nReg, AppC.VerifyEditFields dgVerify, PrepareFields dgPrep)
        {
            m_nReg = nReg;
            m_dgVerify = dgVerify;
            m_dgPrep = dgPrep;
        }

        /// Вход в режим создания/корректировки детальной строки **********************
        /// - установка флага редактирования
        /// - доступных полей
        public void BeginNOE()
        {
            m_dgPrep(m_nReg, m_dgVerify);
        }


    }

    public partial class MainF : Form
    {
    }

}
