using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans.Core
{
    public class VirtualTypeList<T> : IList<T> where T : TranscriptionElement
    {
        List<TranscriptionElement> m_elementlist;
        TranscriptionElement m_parent;
        public VirtualTypeList(TranscriptionElement parent)
        {
            if (parent == null)
                throw new ArgumentNullException();

            m_elementlist = parent.Children;
            m_parent = parent;
        }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return m_elementlist.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_parent.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            m_parent.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return (T)m_elementlist[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            m_parent.Add(item);
        }

        public void Clear()
        {

            m_elementlist.Clear();
        }

        public bool Contains(T item)
        {
            return m_elementlist.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_elementlist.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return m_elementlist.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IList<TranscriptionElement>)m_elementlist).IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return m_parent.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return new VirtualEnumerator<T>(m_elementlist);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VirtualEnumerator<T>(m_elementlist);
        }

        class VirtualEnumerator<R> : IEnumerator<R> where R : TranscriptionElement
        {
            IEnumerator<TranscriptionElement> tre;
            public VirtualEnumerator(List<TranscriptionElement> list)
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
    }

}
