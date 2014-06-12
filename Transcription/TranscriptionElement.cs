using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace NanoTrans.Core
{
    public abstract class TranscriptionElement
    {
        public double height;
        protected TimeSpan _begin = new TimeSpan(-1);
        public TimeSpan Begin
        {
            get
            {
                if (_begin == new TimeSpan(-1))
                {
                    if (_Parent != null && _ParentIndex == 0)
                    {
                        if (_Parent.Begin != new TimeSpan(-1))
                            return _Parent.Begin;
                    }

                    TranscriptionElement elm = this.Previous();
                    while (elm != null && elm._end == new TimeSpan(-1))
                    {
                        elm = elm.Previous();
                    }
                    if (elm != null)
                        return elm.End;
                    else
                        return TimeSpan.Zero;
                }

                return _begin;
            }
            set
            {
                _begin = value;
                if (BeginChanged != null)
                    BeginChanged(this, new EventArgs());
            }
        }
        protected TimeSpan _end = new TimeSpan(-1);
        public TimeSpan End
        {
            get
            {
                if (_end == new TimeSpan(-1))
                {

                    if (_Parent != null && _ParentIndex == _Parent.Children.Count - 1)
                    {
                        if (_Parent.End != new TimeSpan(-1))
                            return _Parent.End;
                    }


                    TranscriptionElement elm = this.Next();
                    while (elm != null && elm._begin == new TimeSpan(-1))
                    {
                        elm = elm.Next();
                    }
                    if (elm != null)
                        return elm.Begin;
                }

                return _end;
            }
            set
            {
                _end = value;
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

        /// <summary>
        /// Text of element including text of children
        /// </summary>
        public abstract string InnerText
        {
            get;
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
            _children = new List<TranscriptionElement>();
        }

        public virtual TranscriptionElement this[int Index]
        {
            get
            {
                return _children[Index];
            }

            set
            {
                _children[Index] = value;
            }

        }

        protected TranscriptionElement _Parent;
        protected int _ParentIndex;
        public int ParentIndex
        {
            get { return _ParentIndex; }
        }

        public TranscriptionElement Parent
        {
            get { return _Parent; }
        }


        protected List<TranscriptionElement> _children;
        public List<TranscriptionElement> Children
        {
            get { return _children; }
        }

        public bool HaveChildren
        {
            get { return Children.Count > 0; }
        }


        public abstract int AbsoluteIndex
        {
            get;
        }

        public virtual void ElementInserted(TranscriptionElement element, int absoluteindex)
        {
            if (_Parent != null)
                _Parent.ElementInserted(element, absoluteindex);//+1 for this
        }

        public virtual void Add(TranscriptionElement data)
        {
            _children.Add(data);
            data._Parent = this;
            data._ParentIndex = _children.Count - 1;

            ElementInserted(data, data.AbsoluteIndex);
            ChildrenCountChanged(ChangedAction.Add);
        }

        public virtual void Insert(int index, TranscriptionElement data)
        {
            if (index < 0 || index > Children.Count)
                throw new IndexOutOfRangeException();

            _children.Insert(index, data);
            data._Parent = this;
            for (int i = index; i < _children.Count; i++)
            {
                _children[i]._ParentIndex = i;
            }
            ElementInserted(data, data.AbsoluteIndex);
            ChildrenCountChanged(ChangedAction.Add);
            ChildrenCountChanged(ChangedAction.Replace);

        }
        public virtual void RemoveAt(int index)
        {

            if (index < 0 || index >= Children.Count)
                throw new IndexOutOfRangeException();

            TranscriptionElement element = _children[index];
            int indexabs = element.AbsoluteIndex;
            var c = _children[index];
            c._Parent = null;
            c._ParentIndex = -1;
            _children.RemoveAt(index);

            for (int i = index; i < _children.Count; i++)
            {
                _children[i]._ParentIndex = i;
            }
            ElementRemoved(element, indexabs);
            ChildrenCountChanged(ChangedAction.Remove);

        }

        public virtual void ElementRemoved(TranscriptionElement element, int absoluteindex)
        {
            if (_Parent != null)
                _Parent.ElementRemoved(element, absoluteindex);//+1 for this
        }

        public virtual bool Remove(TranscriptionElement value)
        {
            RemoveAt(_children.IndexOf(value));
            return true;
        }

        public virtual bool Replace(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            int index = _children.IndexOf(oldelement);
            if (index >= 0)
            {
                _children[index] = newelement;

                ChildrenCountChanged(ChangedAction.Replace);
                return true;
            }
            return false;
        }


        public virtual void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            if (_Parent != null)
                _Parent.ElementReplaced(oldelement, newelement);
        }

        public virtual void ElementChanged(TranscriptionElement element)
        {
            if (_Parent != null)
                _Parent.ElementChanged(element);
        }

        public TranscriptionElement Next()
        {

            if (_children.Count > 0 && !IsParagraph)
                return _children[0];

            if (_Parent == null)
                return null;
            if (_ParentIndex == _Parent._children.Count - 1)
            {
                TranscriptionElement te = _Parent.NextSibling();
                if (te != null)
                    return te.Next();

                return null;
            }
            else
            {
                return _Parent._children[_ParentIndex + 1];
            }

        }

        public IEnumerable<TranscriptionElement> EnumerateNext()
        {
            var n = Next();
            if (n == null)
                yield break;
            yield return n;
            while (n != null)
            {
                n = n.Next();
                yield return n;
            }

        }


        public TranscriptionElement NextSibling()
        {

            if (_Parent == null)
                return null;
            if (_ParentIndex == _Parent._children.Count - 1)
            {
                TranscriptionElement te = _Parent.NextSibling();
                if (te != null && te.Children.Count > 0)
                    return te._children[0];
                else
                    return null;
            }
            else
            {
                return _Parent._children[_ParentIndex + 1];
            }

        }


        public IEnumerable<TranscriptionElement> EnumerateNextSiblings()
        {
            var n = NextSibling();
            if (n == null)
                yield break;
            yield return n;
            while (n != null)
            {
                n = n.NextSibling();
                yield return n;
            }

        }

        public TranscriptionElement PreviousSibling()
        {

            if (_Parent == null)
                return null;
            if (_ParentIndex == 0)
            {
                TranscriptionElement te = _Parent.PreviousSibling();
                if (te != null && te.Children.Count > 0)
                    return te._children[te._children.Count - 1];
                else
                    return null;
            }
            else
            {
                return _Parent._children[_ParentIndex - 1];
            }

        }


        public IEnumerable<TranscriptionElement> EnumeratePreviousSibling()
        {
            var n = PreviousSibling();
            if (n == null)
                yield break;
            yield return n;
            while (n != null)
            {
                n = n.PreviousSibling();
                yield return n;
            }

        }

        public TranscriptionElement Previous()
        {

            if (_Parent == null)
                return null;
            if (_ParentIndex == 0)
            {
                if (IsChapter)
                    return null;
                return _Parent;
            }
            else
            {
                return _Parent._children[_ParentIndex - 1];
            }

        }

        public IEnumerable<TranscriptionElement> EnumeratePrevious()
        {
            var n = Previous();
            if (n == null)
                yield break;
            yield return n;
            while (n != null)
            {
                n = n.Previous();
                yield return n;
            }

        }


        public virtual int GetTotalChildrenCount()
        {
            int c = _children.Count;
            foreach (var ch in _children)
                c += ch.GetTotalChildrenCount();

            return c;
        }

        public virtual void ChildrenCountChanged(ChangedAction action)
        {
            if (!_Updating)
            {
                if (Parent != null)
                    Parent.ChildrenCountChanged(action);
            }
            else
                _updated = true;
        }

        private bool _Updating = false;
        protected bool _updated = false;
        public void BeginUpdate()
        {
            _Updating = true;
        }

        public void EndUpdate()
        {
            _Updating = false;
            if (_updated)
                ChildrenCountChanged(ChangedAction.Reset);
            _updated = false;
        }
    }

}
