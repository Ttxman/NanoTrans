using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{
    /// <summary>
    /// base class for each node in Transcription tree structure
    /// </summary>
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
                var ov = Begin;
                _begin = value;
                OnContentChanged(new BeginAction(this, this.TranscriptionIndex, this.AbsoluteIndex, ov));
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
                var ov = End;
                _end = value;
                OnContentChanged(new EndAction(this, this.TranscriptionIndex, this.AbsoluteIndex, ov));
            }
        }



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
            _vChildren = new VirtualTypeList<TranscriptionElement>(this, _children);
        }

        public virtual TranscriptionElement this[int index]
        {
            get
            {
                return _children[index];
            }

            set
            {
                var c = _children[index];
                var ci = c.TranscriptionIndex;
                var ca = c.AbsoluteIndex;


                value._Parent = this;
                value._ParentIndex = index;

                c._Parent = null;
                c._ParentIndex = -1;

                _children[index] = value;

                OnContentChanged(new ReplaceAction(c, ci, ca));
            }

        }

        public abstract TranscriptionElement this[TranscriptionIndex index]
        {
            get;
            set;
        }

        public abstract void RemoveAt(TranscriptionIndex index);

        public abstract void Insert(TranscriptionIndex index, TranscriptionElement value);


        public void ValidateIndexOrThrow(TranscriptionIndex index)
        {
            if (!index.IsValid)
                throw new ArgumentOutOfRangeException("index", "invalid index value");
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


        protected readonly List<TranscriptionElement> _children = new List<TranscriptionElement>();


        private VirtualTypeList<TranscriptionElement> _vChildren;
        public VirtualTypeList<TranscriptionElement> Children
        {
            get { return _vChildren; }
        }


        public bool HaveChildren
        {
            get { return Children.Count > 0; }
        }

        /// <summary>
        /// Absolute index = index Depth-first tree traversal order of TranscriptionChapter, Section and Paragraph structure (Phrases does not count)
        /// </summary>
        public abstract int AbsoluteIndex
        {
            get;
        }

        /// <summary>
        /// This value will be wrong if element is not part of Transcription
        /// </summary>
        public TranscriptionIndex TranscriptionIndex
        {
            get
            {
                if (this.Parent == null && !(this is Transcription))
                    return TranscriptionIndex.Invalid;
                var acc = this;
                var parents = new Stack<TranscriptionElement>();
                while (acc != null && !(acc is Transcription))
                {
                    parents.Push(acc);
                    acc = acc.Parent;
                }

                var indexa = new int[4] { -1, -1, -1, -1 };

                int index = 0;
                while (parents.Count > 0)
                    indexa[index++] = parents.Pop().ParentIndex;

                var indx = new TranscriptionIndex(indexa);

                if (!indx.IsValid)
                    return TranscriptionIndex.Invalid;

                return indx;
            }
        }

        public virtual void Add(TranscriptionElement data)
        {
            _children.Add(data);
            data._Parent = this;
            data._ParentIndex = _children.Count - 1;
            OnContentChanged(new InsertAction(data, data.TranscriptionIndex, data.AbsoluteIndex));
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

            OnContentChanged(new InsertAction(data, data.TranscriptionIndex, data.AbsoluteIndex));
        }


        public virtual void RemoveAt(int index)
        {

            if (index < 0 || index >= Children.Count)
                throw new IndexOutOfRangeException();

            TranscriptionElement element = _children[index];
            int indexabs = element.AbsoluteIndex;
            var c = _children[index];
            var ci = c.TranscriptionIndex;
            var ca = c.AbsoluteIndex;
            c._Parent = null;
            c._ParentIndex = -1;
            _children.RemoveAt(index);

            for (int i = index; i < _children.Count; i++)
            {
                _children[i]._ParentIndex = i;
            }

            OnContentChanged(new RemoveAction(c, ci, ca));

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
                var oi = oldelement.TranscriptionIndex;
                var oa = oldelement.AbsoluteIndex;

                newelement._Parent = this;
                newelement._ParentIndex = oldelement._ParentIndex;

                oldelement._Parent = null;
                oldelement._ParentIndex = -1;

                _children[index] = newelement;

                OnContentChanged(new ReplaceAction(oldelement, oi, oa));
                return true;
            }
            return false;
        }


        /// <summary>
        /// next element in index Depth-first tree traversal. (Phrases are ignored)
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// enumerable of elements next to this in Depth-first tree traversal. (Phrases are ignored)
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// get next sibling == get next element in Depth-first tree traversal of same type as this
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        ///  get enumerable of next elements with same type as this in Depth-first tree traversal 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// get next sibling == get previous element in Depth-first tree traversal of same type as this
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// get enumerable of previous elements with same type as this in Depth-first tree traversal 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// previous element in index Depth-first tree traversal. (Phrases are ignored)
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// enumerable of elements previuos to this in Depth-first tree traversal. (Phrases are ignored)
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// total count of subelements (phrazes are as alway ignored)
        /// </summary>
        /// <returns></returns>
        public virtual int GetTotalChildrenCount()
        {
            int c = _children.Count;
            foreach (var ch in _children)
                c += ch.GetTotalChildrenCount();

            return c;
        }



        List<ChangeAction> _changes = null;

        /// <summary>
        /// When something in the transcription tree structure changes, change action (with undo) bubbles up to the Transcription element
        /// base.OnContentChanged() should be called, or there is chance of breaking undo functionality
        /// </summary>
        /// <param name="actions"></param>
        /// <returns>false, when change should not be processed (for example after BeginUpdate)</returns>
        public virtual bool OnContentChanged(params ChangeAction[] actions)
        {
            if (_Updating <= 0)
            {
                if (_ContentChanged != null)
                    _ContentChanged(this, new TranscriptionElementChangedEventArgs(actions));

                if (Parent != null)
                    Parent.OnContentChanged(actions);
            }
            else
            {
                if (_logUpdates)
                    _changes.AddRange(actions);
                _updated = true;
                return false;
            }
            return true;
        }


        private EventHandler<TranscriptionElementChangedEventArgs> _ContentChanged;
        public event EventHandler<TranscriptionElementChangedEventArgs> ContentChanged
        {
            add
            {
                _ContentChanged += value;
            }
            remove
            {
                _ContentChanged -= value;
            }
        }

        public class TranscriptionElementChangedEventArgs : EventArgs
        {
            public ChangeAction[] ActionsTaken { get; private set; }

            public TranscriptionElementChangedEventArgs(ChangeAction[] actions)
            {
                ActionsTaken = actions;
            }
        }



        private int _Updating = 0;

        public bool Updating
        {
            get { return _Updating > 0; }
        }
        private bool _updated = false;
        private bool _logUpdates = true;
        /// <summary>
        /// Stop Bubbling changes through OnContentChanged() anc ContentChanged event and acumulate changes until EndUpdate is called
        /// </summary>
        public void BeginUpdate(bool logupdates = true)
        {
            if (_Updating <= 0)
            {
                _logUpdates = logupdates;

                _changes = new List<ChangeAction>();
            }
            _Updating++;

        }

        /// <summary>
        /// Bubble all accumulated changes (since BeginUpdate) as one big change, and resume immediate Bubbling of changes
        /// </summary>
        public void EndUpdate()
        {
            _Updating--;
            if (_Updating > 0)
                return;

            if (_updated)
                OnContentChanged(_changes.ToArray());
            _changes = null;
            _updated = false;
        }
    }

}
