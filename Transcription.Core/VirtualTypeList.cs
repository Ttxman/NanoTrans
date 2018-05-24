using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{
    /// <summary>
    /// Just IList wrapper around TranscriptionElement.Children. Automaticaly cast content to given type T. Used to provide conveniet list of sublements in derived classes (like Paragraph.Phrases)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VirtualTypeList<T> : IList<T> where T : TranscriptionElement
    {
        IList<TranscriptionElement> _elementlist;
        TranscriptionElement _parent;
        public VirtualTypeList(TranscriptionElement parent, List<TranscriptionElement> list)
        {
            if (parent == null)
                throw new ArgumentNullException();

            _elementlist = list;
           _parent = parent;
        }

        public void AddMany(IEnumerable<T> elements)
        {
            foreach (var elm in elements)
                this.Add(elm);
        }


        #region IList<T> Members

        public int IndexOf(T item)
        {
            return _elementlist.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
           _parent.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
           _parent.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return (T)_elementlist[index];
            }
            set
            {
                _parent[index] = value;
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
           _parent.Add(item);
        }

        public void Clear()
        {
            _parent.BeginUpdate();
            while(_elementlist.Count >0)
            {
                _parent.RemoveAt(_elementlist.Count -1);
            }

           _parent.EndUpdate();
        }

        public bool Contains(T item)
        {
            return _elementlist.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
           _elementlist.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _elementlist.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IList<TranscriptionElement>)_elementlist).IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return _parent.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return new VirtualEnumerator<T>(_elementlist);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VirtualEnumerator<T>(_elementlist);
        }

        class VirtualEnumerator<R> : IEnumerator<R> where R : TranscriptionElement
        {
            IEnumerator<TranscriptionElement> tre;
            public VirtualEnumerator(IList<TranscriptionElement> list)
            {
                tre = list.GetEnumerator();
            }

            #region IEnumerator<R> Members
            public R Current
            {
                get
                {
                    return (R)tre.Current;
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                tre.Dispose();
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get
                {
                    return (R)tre.Current;
                }
            }

            public bool MoveNext()
            {
                return tre.MoveNext();
            }

            public void Reset()
            {
                tre.Reset();
            }

            #endregion
        }

        #endregion


        public void ForEach(Action<T> action)
        {
            foreach (var item in _elementlist)
            {
                action((T)item);
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            _parent.BeginUpdate();
            foreach (var item in collection)
            {
                _parent.Add(item);
            }
            _parent.EndUpdate();
        }

        public void RemoveRange(int index,int count)
        {
            _parent.BeginUpdate();
            for(int i=0;i<count;i++)
            {
                _parent.RemoveAt(index + i);
            }
            _parent.EndUpdate();
        }
    }

}
