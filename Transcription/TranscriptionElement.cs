using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans.Core
{
    public abstract class TranscriptionElement
    {
        public double height;
        protected TimeSpan m_begin = new TimeSpan(-1);
        public TimeSpan Begin
        {
            get
            {
                if (m_begin == new TimeSpan(-1))
                {
                    if (m_Parent != null && m_ParentIndex == 0)
                    {
                        if (m_Parent.Begin != new TimeSpan(-1))
                            return m_Parent.Begin;
                    }

                    TranscriptionElement elm = this.Previous();
                    while (elm != null && elm.m_end == new TimeSpan(-1))
                    {
                        elm = elm.Previous();
                    }
                    if (elm != null)
                        return elm.End;
                    else
                        return TimeSpan.Zero;
                }

                return m_begin;
            }
            set
            {
                m_begin = value;
                if (BeginChanged != null)
                    BeginChanged(this, new EventArgs());
            }
        }
        protected TimeSpan m_end = new TimeSpan(-1);
        public TimeSpan End
        {
            get
            {
                if (m_end == new TimeSpan(-1))
                {

                    if (m_Parent != null && m_ParentIndex == m_Parent.Children.Count - 1)
                    {
                        if (m_Parent.End != new TimeSpan(-1))
                            return m_Parent.End;
                    }


                    TranscriptionElement elm = this.Next();
                    while (elm != null && elm.m_begin == new TimeSpan(-1))
                    {
                        elm = elm.Next();
                    }
                    if (elm != null)
                        return elm.Begin;
                }

                return m_end;
            }
            set
            {
                m_end = value;
                if (EndChanged != null)
                    EndChanged(this, new EventArgs());
            }
        }

        public event EventHandler BeginChanged;
        public event EventHandler EndChanged;

        public abstract string Text
        {
            get;
            set;
        }

        public abstract string Phonetics
        {
            get;
            set;
        }

        public virtual bool IsSection
        {
            get { return false; }
        }

        public virtual bool IsChapter
        {
            get { return false; }
        }

        public virtual bool IsParagraph
        {
            get { return false; }
        }


        public TranscriptionElement()
        {
            m_children = new List<TranscriptionElement>();
        }

        public virtual TranscriptionElement this[int Index]
        {
            get
            {
                return m_children[Index];
            }

            set
            {
                m_children[Index] = value;
            }

        }

        protected TranscriptionElement m_Parent;
        protected int m_ParentIndex;
        public int ParentIndex
        {
            get { return m_ParentIndex; }
        }

        public TranscriptionElement Parent
        {
            get { return m_Parent; }
        }


        protected List<TranscriptionElement> m_children;
        public List<TranscriptionElement> Children
        {
            get { return m_children; }
        }

        public bool HaveChildren
        {
            get { return Children.Count > 0; }
        }

        public virtual void Add(TranscriptionElement data)
        {
            m_children.Add(data);
            data.m_Parent = this;
            data.m_ParentIndex = m_children.Count - 1;
            ChildrenCountChanged(NotifyCollectionChangedAction.Add);
        }

        public virtual void Insert(int index, TranscriptionElement data)
        {
            if (index < 0 || index > Children.Count)
                throw new IndexOutOfRangeException();

            m_children.Insert(index, data);
            data.m_Parent = this;
            for (int i = index; i < m_children.Count; i++)
            {
                m_children[i].m_ParentIndex = i;
            }
            ElementInserted(data, data.m_ParentIndex);
            ChildrenCountChanged(NotifyCollectionChangedAction.Add);
            ChildrenCountChanged(NotifyCollectionChangedAction.Replace);

        }

        public virtual int AbsoluteIndex
        {
            get { return (m_Parent != null) ? m_Parent.AbsoluteIndex + m_ParentIndex : 0; }
        }

        public virtual void ElementInserted(TranscriptionElement element, int index)
        {
            if (m_Parent != null)
                m_Parent.ElementInserted(element, m_Parent.ParentIndex + index + 1);//+1 for this
        }

        public virtual void RemoveAt(int index)
        {

            if (index < 0 || index >= Children.Count)
                throw new IndexOutOfRangeException();

            TranscriptionElement element = m_children[index];
            var c = m_children[index];
            c.m_Parent = null;
            c.m_ParentIndex = -1;
            m_children.RemoveAt(index);

            for (int i = index; i < m_children.Count; i++)
            {
                m_children[i].m_ParentIndex = i;
            }
            ElementRemoved(element, index);
            ChildrenCountChanged(NotifyCollectionChangedAction.Remove);

        }

        public virtual void ElementRemoved(TranscriptionElement element, int index)
        {
            if (m_Parent != null)
                m_Parent.ElementRemoved(element, m_Parent.ParentIndex + index + 1);//+1 for this
        }

        public virtual bool Remove(TranscriptionElement value)
        {
            RemoveAt(m_children.IndexOf(value));
            return true;
        }

        public virtual bool Replace(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            int index = m_children.IndexOf(oldelement);
            if (index >= 0)
            {
                m_children[index] = newelement;

                ChildrenCountChanged(NotifyCollectionChangedAction.Replace);
                return true;
            }
            return false;
        }


        public virtual void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            if (m_Parent != null)
                m_Parent.ElementReplaced(oldelement, newelement);
        }

        public virtual void ElementChanged(TranscriptionElement element)
        {
            if (m_Parent != null)
                m_Parent.ElementChanged(element);
        }

        public TranscriptionElement Next()
        {

            if (m_children.Count > 0 && !IsParagraph)
                return m_children[0];

            if (m_Parent == null)
                return null;
            if (m_ParentIndex == m_Parent.m_children.Count - 1)
            {
                TranscriptionElement te = m_Parent.NextSibling();
                if (te != null)
                    return te.Next();

                return null;
            }
            else
            {
                return m_Parent.m_children[m_ParentIndex + 1];
            }

        }

        public TranscriptionElement NextSibling()
        {

            if (m_Parent == null)
                return null;
            if (m_ParentIndex == m_Parent.m_children.Count - 1)
            {
                TranscriptionElement te = m_Parent.NextSibling();
                if (te != null && te.Children.Count > 0)
                    return te.m_children[0];
                else
                    return null;
            }
            else
            {
                return m_Parent.m_children[m_ParentIndex + 1];
            }

        }

        public TranscriptionElement PreviousSibling()
        {

            if (m_Parent == null)
                return null;
            if (m_ParentIndex == 0)
            {
                TranscriptionElement te = m_Parent.PreviousSibling();
                if (te != null && te.Children.Count > 0)
                    return te.m_children[te.m_children.Count - 1];
                else
                    return null;
            }
            else
            {
                return m_Parent.m_children[m_ParentIndex - 1];
            }

        }

        public TranscriptionElement Previous()
        {

            if (m_Parent == null)
                return null;
            if (m_ParentIndex == 0)
            {
                if (IsChapter)
                    return null;
                return m_Parent;
            }
            else
            {
                return m_Parent.m_children[m_ParentIndex - 1];
            }

        }


        public virtual int GetTotalChildrenCount()
        {
            int c = m_children.Count;
            foreach (var ch in m_children)
                c += ch.GetTotalChildrenCount();

            return c;
        }

        public virtual void ChildrenCountChanged(NotifyCollectionChangedAction action)
        {
            if (!m_Updating)
            {
                if (Parent != null)
                    Parent.ChildrenCountChanged(action);
            }
            else
                m_updated = true;
        }

        private bool m_Updating = false;
        protected bool m_updated = false;
        public void BeginUpdate()
        {
            m_Updating = true;
        }

        public void EndUpdate()
        {
            m_Updating = false;
            if (m_updated)
                ChildrenCountChanged(NotifyCollectionChangedAction.Reset);
            m_updated = false;
        }
    }

}
