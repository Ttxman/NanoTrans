using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Windows;
using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Specialized;


namespace NanoTrans
{
    //mluvci
    public class MySpeaker
    {
        //Dovymyslet co se bude ukladat o mluvcim -- jmeno, pohlavi, obrazek, popis, atd...
        public int ID;
        /// <summary>
        /// GET vraci cele jmeno slozene z krestniho+prijmeni
        /// </summary>
        [XmlIgnore]
        public string FullName
        {
            get
            {
                string pJmeno = "";
                if (FirstName != null && FirstName.Length > 0)
                {
                    pJmeno += FirstName;
                }
                if (Surname != null && Surname.Length > 0)
                {
                    if (pJmeno.Length > 0) pJmeno += " ";
                    pJmeno += Surname;
                }
                if (string.IsNullOrEmpty(pJmeno))
                    pJmeno = "Mluvčí";
                return pJmeno;
            }
        }
        public string FirstName;
        public string Surname;
        public string Sex;
        public string RozpoznavacMluvci;
        public string RozpoznavacJazykovyModel;
        public string RozpoznavacPrepisovaciPravidla;
        public string FotoJPGBase64;
        public string Comment;



        public MySpeaker()
        {

            ID = MySpeaker.DefaultID;
            FirstName = null;
            Surname = null;
            Sex = null;
            RozpoznavacMluvci = null;
            RozpoznavacJazykovyModel = null;
            RozpoznavacPrepisovaciPravidla = null;
            FotoJPGBase64 = null;
            Comment = null;
        }
        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public MySpeaker(XElement s, bool isStrict)
        {
            ID = int.Parse(s.Attribute("id").Value);
            Surname = s.Attribute("surname").Value;
            FirstName = (s.Attribute("firstname") ?? EmptyAttribute).Value;

            switch ((s.Attribute("sex") ?? EmptyAttribute).Value)
            {
                case "M":
                    Sex = "male";
                    break;
                case "F":
                    Sex = "female";
                    break;
                default:
                    Sex = "-";
                    break;
            }


            elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove("id");
            elements.Remove("firstname");
            elements.Remove("surname");
            elements.Remove("sex");

        }

        public XElement Serialize(bool strict)
        { 
            XElement elm = new XElement(strict?"speaker":"s",
                elements.Select(e=>
                    new XAttribute(e.Key,e.Value))
                    .Union(new []{ 
                    new XAttribute("id", ID.ToString()),
                    new XAttribute("surname",Surname),
                    new XAttribute("firstname",FirstName),
                    new XAttribute("sex",(Sex=="male")?"M":(Sex=="female")?"F":"X")
                    })
            );

            return elm;
        }
        #endregion

        /// <summary>
        /// kopie
        /// </summary>
        /// <param name="aSpeaker"></param>
        public MySpeaker(MySpeaker aSpeaker)
        {

            if (aSpeaker == null) aSpeaker = new MySpeaker();
            ID = aSpeaker.ID;
            FirstName = aSpeaker.FirstName;
            Surname = aSpeaker.Surname;
            Sex = aSpeaker.Sex;
            RozpoznavacMluvci = aSpeaker.RozpoznavacMluvci;
            RozpoznavacJazykovyModel = aSpeaker.RozpoznavacJazykovyModel;
            RozpoznavacPrepisovaciPravidla = aSpeaker.RozpoznavacPrepisovaciPravidla;
            FotoJPGBase64 = aSpeaker.FotoJPGBase64;
            Comment = aSpeaker.Comment;
        }

        public MySpeaker(string aSpeakerFirstname, string aSpeakerSurname, string aPohlavi, string aRozpoznavacMluvci, string aRozpoznavacJazykovyModel, string aRozpoznavacPrepisovaciPravidla, string aSpeakerFotoBase64, string aPoznamka) //constructor ktery vytvori speakera
        {
            ID = -1;
            FirstName = aSpeakerFirstname;
            Surname = aSpeakerSurname;
            Sex = aPohlavi;
            RozpoznavacMluvci = aRozpoznavacMluvci;
            RozpoznavacJazykovyModel = aRozpoznavacJazykovyModel;
            RozpoznavacPrepisovaciPravidla = aRozpoznavacPrepisovaciPravidla;
            FotoJPGBase64 = aSpeakerFotoBase64;
            Comment = aPoznamka;
        }

        public override string ToString()
        {
            return FullName;
        }

        public static readonly int DefaultID = int.MinValue;

    }

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
            ElementInserted(data,data.m_ParentIndex);
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
                m_Parent.ElementInserted(element,m_Parent.ParentIndex+index+1);//+1 for this
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
            ElementRemoved(element,index);
            ChildrenCountChanged(NotifyCollectionChangedAction.Remove);
           
        }

        public virtual void ElementRemoved(TranscriptionElement element,int index)
        {
            if (m_Parent != null)
                m_Parent.ElementRemoved(element,m_Parent.ParentIndex+index+1);//+1 for this
        }

        public virtual bool Remove(TranscriptionElement value)
        {
            RemoveAt(m_children.IndexOf(value));
            return true;
        }

        public virtual bool Replace(TranscriptionElement oldelement,TranscriptionElement newelement)
        {
            int index = m_children.IndexOf(oldelement);
            if (index >= 0)
            {
                m_children[index] = newelement;
                
                ChildrenCountChanged(NotifyCollectionChangedAction.Replace);
                return true;
            }
            return false ;
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

    //nejmensi textovy usek - muze byt veta, vice slov nebo samotne slovo
    public sealed class MyPhrase : TranscriptionElement
    {
        private string m_text = "";//slovo/a ktere obsahuji i mezery na konci

        public override string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        private string m_phonetics = "";

        public override string Phonetics
        {
            get
            {
                return m_phonetics;
            }
            set
            {
                m_phonetics = value;
            }
        }


        
        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public MyPhrase(XElement e, bool isStrict)
        {
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove(isStrict?"begin":"b");
            elements.Remove(isStrict?"end":"e");
            elements.Remove(isStrict?"fon":"f");


            this.m_phonetics = (e.Attribute(isStrict ? "fon" : "f") ?? EmptyAttribute).Value;
            this.m_text = e.Value.Trim('\r','\n');
            if (e.Attribute(isStrict ? "begin" : "b") != null)
            {
                string val = e.Attribute(isStrict ? "begin" : "b").Value;
                int ms;
                if (int.TryParse(val, out ms))
                {
                    Begin = TimeSpan.FromMilliseconds(ms);
                }
                else
                    Begin = XmlConvert.ToTimeSpan(val);
                
            }

            if (e.Attribute(isStrict ? "end" : "e") != null)
            {
                string val = e.Attribute(isStrict ? "end" : "e").Value;
                int ms;
                if (int.TryParse(val, out ms))
                {
                    End = TimeSpan.FromMilliseconds(ms);
                }
                else
                    End = XmlConvert.ToTimeSpan(val);
            }

        }

        public XElement Serialize(bool strict)
        {
            XElement elm = new XElement(strict?"phrase":"p",
                elements.Select(e=>
                    new XAttribute(e.Key,e.Value))
                    .Union(new []{ 
                    new XAttribute(strict?"begin":"b", Begin),
                    new XAttribute(strict?"end":"e", End),
                    new XAttribute(strict?"fon":"f", m_phonetics),
                    }),
                    m_text.Trim('\r','\n')
            );

            return elm;
        }
        #endregion


        public MyPhrase(MyPhrase kopie)
        {
            this.m_begin = kopie.m_begin;
            this.m_end = kopie.m_end;
            this.m_text = kopie.m_text;
            this.m_phonetics = kopie.m_phonetics;
            this.height = kopie.height;
        }

        public MyPhrase()
            : base()
        {

        }

        public MyPhrase(TimeSpan begin, TimeSpan end, string aWords)
            : this()
        {
            this.Begin = begin;
            this.End = end;
            this.Text = aWords;
        }

        public MyPhrase(TimeSpan begin, TimeSpan end, string aWords, MyEnumTypElementu aElementType)
            : this(begin, end, aWords)
        {
            if (aElementType == MyEnumTypElementu.foneticky)
            {
                Regex re = new Regex("{.*?}");
                MatchCollection mc = re.Matches(aWords);
                string s = aWords;
                bool pPouzeNavrh = false;
                if (mc != null && mc.Count > 0)
                {
                    s = mc[0].Value.Substring(1, mc[0].Value.Length - 2);
                    pPouzeNavrh = true;
                }
                string[] pSplitS = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (pSplitS != null && pSplitS.Length > 1)
                {
                    this.Text = pSplitS[1];
                    if (pPouzeNavrh)
                        this.Text = "{" + this.Text + "}";
                }
            }
        }

        public override int GetTotalChildrenCount()
        {
            return 0;
        }

        public override void ChildrenCountChanged(NotifyCollectionChangedAction action)
        {

        }

        public override void ElementChanged(TranscriptionElement element)
        {
            
        }

        public override void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            
        }

        public override void ElementInserted(TranscriptionElement element, int index)
        {
        }

        public override void ElementRemoved(TranscriptionElement element, int index)
        {
        }

    }

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

    public class MyParagraph : TranscriptionElement
    {

        public override bool IsParagraph
        {
            get
            {
                return true;
            }
        }
        public VirtualTypeList<MyPhrase> Phrases; //nejmensi textovy usek
        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - jedna se o text ze vsech podrazenych textovych jednotek (Phrases)
        /// </summary>
        public override string Text
        {
            get
            {
                string ret = "";
                if (this.Phrases != null)
                {
                    for (int i = 0; i < this.Phrases.Count; i++)
                    {
                        ret += this.Phrases[i].Text;
                    }
                }

                return ret;
            }
            set { throw new NotImplementedException("pokus o vlození textu primo do odstavce"); }
        }


        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - fonetika
        /// </summary>
        public override string Phonetics
        {
            get
            {
                string ret = "";
                if (this.Phrases != null)
                {
                    for (int i = 0; i < this.Phrases.Count; i++)
                    {
                        ret += this.Phrases[i].Phonetics;
                    }
                }

                return ret;
            }
            set { }
        }

        public MyEnumParagraphAttributes DataAttributes = MyEnumParagraphAttributes.None;


        public string Attributes
        {
            get
            {
                MyEnumParagraphAttributes[] attrs = (MyEnumParagraphAttributes[])Enum.GetValues(typeof(MyEnumParagraphAttributes));
                string s = "";
                foreach (var attr in attrs)
                {
                    if (attr != MyEnumParagraphAttributes.None)
                    {
                        if ((DataAttributes & attr) != 0)
                        {
                            string val = Enum.GetName(typeof(MyEnumParagraphAttributes), attr);
                            if (s.Length > 0)
                            {
                                s += "|";
                            }

                            s += val;
                        }
                    }
                }

                if (s.Length == 0)
                {
                    return Enum.GetName(typeof(MyEnumParagraphAttributes), MyEnumParagraphAttributes.None);
                }
                else
                {
                    return s;
                }
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    DataAttributes = MyEnumParagraphAttributes.None;
                    return;
                }
                string[] vals = value.Split('|');
                MyEnumParagraphAttributes attrs = MyEnumParagraphAttributes.None;
                foreach (string val in vals)
                {
                    attrs |= (MyEnumParagraphAttributes)Enum.Parse(typeof(MyEnumParagraphAttributes), val);
                }
                this.DataAttributes = attrs;
            }
        }


        public void SilentEndUpdate()
        {
            m_updated = false;
            EndUpdate();
        }

        public int speakerID = MySpeaker.DefaultID;

        /// <summary>
        /// informace zda je dany element zahrnut pro trenovani dat 
        /// </summary>
        public bool trainingElement;

        /// <summary>
        /// vraci delku odstavce v MS mezi begin a end, pokud neni nektera hodnota nezadana -1
        /// </summary>
        public TimeSpan Delka
        {
            get
            {
                if (Begin == new TimeSpan(-1) || End == new TimeSpan(-1))
                    return TimeSpan.Zero;

                return End - Begin;
            }
        }



        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public MyParagraph(XElement e,bool isStrict)
        {
            Phrases = new VirtualTypeList<MyPhrase>(this);
            speakerID = int.Parse(e.Attribute(isStrict?"speakerid":"s").Value);
            Attributes = (e.Attribute(isStrict?"speakerid":"s") ?? EmptyAttribute).Value;

            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove(isStrict?"begin":"b");
            elements.Remove(isStrict?"end":"e");
            elements.Remove(isStrict?"attributes":"a");
            elements.Remove(isStrict?"speakerid":"s");


            e.Elements(isStrict ? "phrase" : "p").Select(p => (TranscriptionElement)new MyPhrase(p, isStrict)).ToList().ForEach(p => Add(p)); ;

            if (e.Attribute(isStrict ? "attributes" : "a") != null)
            {
                this.Attributes = e.Attribute(isStrict ? "attributes" : "a").Value;
            
            }

            if (e.Attribute(isStrict?"begin":"b") != null)
            {
                string val = e.Attribute(isStrict?"begin":"b").Value;
                int ms;
                if (int.TryParse(val, out ms))
                {
                    Begin = TimeSpan.FromMilliseconds(ms);
                }
                else
                    Begin = XmlConvert.ToTimeSpan(val);

            }
            else
            {
                var ch = m_children.FirstOrDefault();
                Begin = ch == null ? TimeSpan.Zero : ch.Begin;
            }

            if (e.Attribute(isStrict?"end":"e") != null)
            {
                string val = e.Attribute(isStrict?"end":"e").Value;
                int ms;
                if (int.TryParse(val, out ms))
                {
                    End = TimeSpan.FromMilliseconds(ms);
                }
                else
                    End = XmlConvert.ToTimeSpan(val);
            }
            else
            {
                var ch = m_children.LastOrDefault();
                End = ch == null ? TimeSpan.Zero : ch.Begin;
            }

        }

        public XElement Serialize(bool strict)
        {
            XElement elm = new XElement(strict?"paragraph":"pa",
                elements.Select(e=>new XAttribute(e.Key,e.Value)).Union(new []{ new XAttribute(strict?"begin":"b", Begin),new XAttribute(strict?"end":"e", End),new XAttribute(strict?"attributes":"a", Attributes),new XAttribute(strict?"speakerid":"s", speakerID),}),
                Phrases.Select(p=>p.Serialize(strict))
            );

            return elm;
        }
        #endregion


        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MyParagraph(MyParagraph aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.trainingElement = aKopie.trainingElement;
            this.DataAttributes = aKopie.DataAttributes;
            if (aKopie.Phrases != null)
            {
                this.Phrases = new VirtualTypeList<MyPhrase>(this);
                for (int i = 0; i < aKopie.Phrases.Count; i++)
                {
                    this.Phrases.Add(new MyPhrase(aKopie.Phrases[i]));
                }
            }
            this.speakerID = aKopie.speakerID;
        }

        public MyParagraph(List<MyPhrase> phrases)
            : this()
        {
            foreach (var p in phrases)
                Add(p);
            if (Phrases.Count > 0)
            {
                this.Begin = Phrases[0].Begin;
                this.End = Phrases[Phrases.Count - 1].End;
            }
        }

        public MyParagraph()
            : base()
        {
            Phrases = new VirtualTypeList<MyPhrase>(this);
            this.Begin = new TimeSpan(-1);
            this.End = new TimeSpan(-1);
            this.trainingElement = false;
            this.speakerID = MySpeaker.DefaultID;
        }
    }

    //sekce textu nadrazena odstavci
    public class MySection : TranscriptionElement
    {
        public override bool IsSection
        {
            get
            {
                return true;
            }
        }

        public override string Text
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        public override string Phonetics
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public string name = string.Empty;

        public VirtualTypeList<MyParagraph> Paragraphs;

        public int Speaker;




        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public MySection(XElement e, bool isStrict)
        {
            this.Paragraphs = new VirtualTypeList<MyParagraph>(this);
            name = e.Attribute("name").Value;
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove("name");
            e.Elements(isStrict ? "paragraph" : "pa").Select(p => (TranscriptionElement)new MyParagraph(p, isStrict)).ToList().ForEach(p => Add(p));

        }

        public XElement Serialize(bool strict)
        {
            
            XElement elm = new XElement(strict?"section":"se",elements.Select(e=>new XAttribute(e.Key,e.Value)).Union(new []{ new XAttribute("name", name),}),
                Paragraphs.Select(p=>p.Serialize(strict))
            );

            return elm;
        }
        #endregion


        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MySection(MySection aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.name = aKopie.name;
            if (aKopie.Paragraphs != null)
            {
                this.Paragraphs = new VirtualTypeList<MyParagraph>(this);
                for (int i = 0; i < aKopie.Paragraphs.Count; i++)
                {
                    this.Paragraphs.Add(new MyParagraph(aKopie.Paragraphs[i]));
                }
            }
        }

        public MySection()
        {
            Paragraphs = new VirtualTypeList<MyParagraph>(this);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
        }

        public MySection(String aName)
            : this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {
        }
        public MySection(String aName, TimeSpan aBegin, TimeSpan aEnd)
            : this()
        {
            this.name = aName;
            this.Begin = aBegin;
            this.End = aEnd;
        }

        public override int GetTotalChildrenCount()
        {
            return m_children.Count;
        }
    }

    //kapitola
    public class MyChapter : TranscriptionElement
    {
        public override bool IsChapter
        {
            get
            {
                return true;
            }
        }
        public override string Text
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        public override string Phonetics
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public string name = string.Empty;

        public VirtualTypeList<MySection> Sections;


        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public MyChapter(XElement c, bool isStrict)
        {
            Sections = new VirtualTypeList<MySection>(this);
            name = c.Attribute("name").Value;
            elements = c.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove("name");
            c.Elements(isStrict ? "section" : "se").Select(s => (TranscriptionElement)new MySection(s, isStrict)).ToList().ForEach(s => Add(s));

        }

        public XElement Serialize(bool strict)
        {
            
            XElement elm = new XElement(strict?"chapter":"ch",
                elements.Select(e=>new XAttribute(e.Key,e.Value)).Union(new []{ new XAttribute("name", name),}),
                Sections.Select(s=>s.Serialize(strict))
            );

            return elm;
        }
        #endregion

        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MyChapter(MyChapter aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.name = aKopie.name;
            if (aKopie.Sections != null)
            {
                this.Sections = new VirtualTypeList<MySection>(this);
                for (int i = 0; i < aKopie.Sections.Count; i++)
                {
                    this.Sections.Add(new MySection(aKopie.Sections[i]));
                }
            }
        }

        public MyChapter()
            : base()
        {
            Sections = new VirtualTypeList<MySection>(this);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
        }

        public MyChapter(String aName)
            : this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {

        }
        public MyChapter(String aName, TimeSpan aBegin, TimeSpan aEnd)
        {
            Sections = new VirtualTypeList<MySection>(this);
            this.name = aName;
            this.Begin = aBegin;
            this.End = aEnd;
        }

    }


    //hlavni trida s titulky a se vsemi potrebnymi metodami pro serializaci
    public class MySubtitlesData : TranscriptionElement, IList<TranscriptionElement>, INotifyCollectionChanged
    {
        public double TotalHeigth;
        public bool FindNext(ref TranscriptionElement paragraph, ref int TextOffset, out int length, string pattern, bool isregex, bool CaseSensitive, bool searchinspeakers)
        {
            TranscriptionElement par = paragraph;
            length = 0;
            if (par == null)
                return false;

            if (searchinspeakers)
            {
                TranscriptionElement prs = paragraph.Next();

                while (prs != null)
                {
                    MyParagraph pr = prs as MyParagraph;
                    if (pr != null && GetSpeaker(pr.speakerID).FullName.ToLower().Contains(pattern.ToLower()))
                    {
                        paragraph = pr;
                        TextOffset = 0;
                        return true;
                    }
                    prs = pr.Next();
                }
                return false;
            }

            Regex r;
            if (isregex)
            {
                r = new Regex(pattern);
            }
            else
            {
                if (!CaseSensitive)
                    pattern = pattern.ToLower();
                r = new Regex(Regex.Escape(pattern));
            }

            TranscriptionElement tag = paragraph;
            while (par != null)
            {
                string s = par.Text;
                if (!CaseSensitive && !isregex)
                    s = s.ToLower();
                if (TextOffset >= s.Length)
                    TextOffset = 0;
                Match m = r.Match(s, TextOffset);

                if (m.Success)
                {
                    TextOffset = m.Index;
                    length = m.Length;
                    paragraph = tag;
                    return true;
                }

                tag = tag.Next();
                if (tag == null)
                    return false;
                par = tag;
                TextOffset = 0;
            }

            return false;
        }

        public string JmenoSouboru { get; set; }

        private bool m_Ulozeno;
        public bool Ulozeno
        {
            get
            {
                return m_Ulozeno;
            }
            set
            {
                m_Ulozeno = value;
            }
        }

        /// <summary>
        /// datum a cas poradu, ktery je v transkripci zpracovan - napr. pocatecni cas audio souboru 
        /// </summary>
        public DateTime dateTime { get; set; }
        /// <summary>
        /// zdroj odkud je transkripce - radio - nazev kanalu, televize, mikrofon, atd...
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// typ poradu - cele transkripce 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// zdroj audio dat - muze byt stejny jako video a naopak
        /// </summary>
        public string mediaURI { get; set; }
        /// <summary>
        /// zdroj video dat - muze byt stejny jako audio a naopak
        /// </summary>
        public string videoFileName { get; set; }

        /// <summary>
        /// vsechny kapitoly streamu
        /// </summary>
        public VirtualTypeList<MyChapter> Chapters;    //vsechny kapitoly streamu

        [XmlElement("SpeakersDatabase")]
        public MySpeakers m_SeznamMluvcich = new MySpeakers();

        [XmlIgnore]
        public MySpeakers Speakers
        {
            get { return m_SeznamMluvcich; }
            set { m_SeznamMluvcich = value; }
        }



        public MySubtitlesData()
        {
            JmenoSouboru = null;
            Ulozeno = false;

            Chapters = new VirtualTypeList<MyChapter>(this);
            //constructor  
        }



        /// <summary>
        /// vytvori kopii objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MySubtitlesData(MySubtitlesData aKopie)
        {
            this.dateTime = aKopie.dateTime;
            this.source = aKopie.source;
            this.mediaURI = aKopie.mediaURI;
            this.videoFileName = aKopie.videoFileName;
            this.type = aKopie.type;
            if (aKopie.Chapters != null)
            {
                this.Chapters = new VirtualTypeList<MyChapter>(this);
                for (int i = 0; i < aKopie.Chapters.Count; i++)
                {
                    this.Chapters.Add(new MyChapter(aKopie.Chapters[i]));
                }
            }
            this.JmenoSouboru = aKopie.JmenoSouboru;
            this.m_SeznamMluvcich = new MySpeakers(aKopie.m_SeznamMluvcich);
            this.Ulozeno = aKopie.Ulozeno;
        }

        /// <summary>
        /// vrati vsechny vyhovujici elementy casu
        /// </summary>
        /// <param name="aPoziceKurzoru"></param>
        /// <returns></returns>
        public List<MyParagraph> VratElementDanehoCasu(TimeSpan cas)
        {
            List<MyParagraph> toret = new List<MyParagraph>();
            foreach (var el in this)
            {
                if (el.IsParagraph && el.Begin <= cas && el.End > cas)
                {
                    toret.Add((MyParagraph)el);
                }
            }
            return toret;
        }

        public MyParagraph VratElementKonciciPred(TimeSpan cas)
        {
            List<MyParagraph> toret = new List<MyParagraph>();
            MyParagraph par = null;
            foreach (var el in this)
            {
                if (el.End < cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (MyParagraph)el;
                    }
                }
                else
                    break;
            }
            return par;
        }


        public MyParagraph VratElementZacinajiciPred(TimeSpan cas)
        {
            List<MyParagraph> toret = new List<MyParagraph>();
            MyParagraph par = null;
            foreach (var el in this)
            {
                if (el.Begin < cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (MyParagraph)el;
                    }
                }
                else
                    break;
            }
            return par;

        }

        //smazani speakera ze seznamu speakeru a odstraneni speakera v pouzitych odstavcich
        public bool OdstranSpeakera(MySpeaker aSpeaker)
        {
            try
            {
                if (aSpeaker.FullName != null && aSpeaker.FullName != "")
                {
                    if (this.m_SeznamMluvcich.OdstranSpeakera(aSpeaker))
                    {
                        Ulozeno = false;
                        for (int k = 0; k < Chapters.Count; k++)
                        {
                            for (int l = 0; l < ((MyChapter)Chapters[k]).Sections.Count; l++)
                            {
                                for (int m = 0; m < ((MySection)((MyChapter)Chapters[k]).Sections[l]).Paragraphs.Count; m++)
                                {
                                    if (Chapters[k].Sections[l].Paragraphs[m].speakerID == aSpeaker.ID)
                                    {
                                        Chapters[k].Sections[l].Paragraphs[m].speakerID = new MySpeaker().ID;
                                    }
                                }

                            }

                        }
                        Ulozeno = false;
                        return true;
                    }

                    return false;

                }
                else return false;
            }
            catch// (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// novy mluvci do databaze titulku
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public int NovySpeaker(MySpeaker aSpeaker)
        {
            int i = this.m_SeznamMluvcich.NovySpeaker(aSpeaker);
            if (i != MySpeaker.DefaultID)
                this.Ulozeno = false;
            return i;
        }


        public MySpeaker VratSpeakera(int aIDSpeakera)
        {
            return this.m_SeznamMluvcich.VratSpeakera(aIDSpeakera);
        }

        /// <summary>
        /// vraci ID speakera podle stringu jmena
        /// </summary>
        /// <param name="aJmeno"></param>
        /// <returns></returns>
        public int GetSpeaker(string aJmeno)
        {
            return this.m_SeznamMluvcich.NajdiSpeakeraID(aJmeno);
        }

        /// <summary>
        /// vraci ID speakera podle stringu jmena
        /// </summary>
        /// <param name="aJmeno"></param>
        /// <returns></returns>
        public MySpeaker GetSpeaker(int id)
        {
            return this.m_SeznamMluvcich.VratSpeakera(id);
        }


        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru - muze ulozit mluvci bez fotky
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serialize(string jmenoSouboru, bool aUkladatKompletMluvci)
        {
            using (FileStream s = new FileStream(jmenoSouboru, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
            {

                bool output = Serialize(s, aUkladatKompletMluvci);

                if (output)
                {
                    this.JmenoSouboru = jmenoSouboru;
                    this.Ulozeno = true;

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public bool SerializeV1(Stream datastream, MySubtitlesData co, bool aUkladatKompletMluvci)
        {
            try
            {
                MySpeakers pKopieMluvcich = new MySpeakers(co.m_SeznamMluvcich);
                if (!aUkladatKompletMluvci)
                {
                    if (co != null && co.m_SeznamMluvcich != null)
                    {
                        for (int i = 0; i < co.m_SeznamMluvcich.Speakers.Count; i++)
                        {
                            co.m_SeznamMluvcich.Speakers[i].FotoJPGBase64 = null;
                        }
                    }
                }

                MySubtitlesData pCopy = new MySubtitlesData(co);

                System.Xml.XmlTextWriter writer = new XmlTextWriter(datastream, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument(); //<?xml version ...


                writer.WriteStartElement("Transcription");
                writer.WriteAttributeString("dateTime", XmlConvert.ToString(pCopy.dateTime, XmlDateTimeSerializationMode.Local));
                writer.WriteAttributeString("audioFileName", pCopy.mediaURI);

                writer.WriteStartElement("Chapters");


                foreach (MyChapter c in pCopy.Chapters)
                {
                    writer.WriteStartElement("Chapter");
                    writer.WriteAttributeString("name", c.name);
                    writer.WriteAttributeString("begin", XmlConvert.ToString(c.Begin));
                    writer.WriteAttributeString("end", XmlConvert.ToString(c.End));

                    writer.WriteStartElement("Sections");

                    foreach (MySection s in c.Sections)
                    {
                        writer.WriteStartElement("Section");
                        writer.WriteAttributeString("name", s.name);
                        writer.WriteAttributeString("begin", XmlConvert.ToString(s.Begin));
                        writer.WriteAttributeString("end", XmlConvert.ToString(s.End));

                        writer.WriteStartElement("Paragraphs");

                        foreach (MyParagraph p in s.Paragraphs)
                        {
                            writer.WriteStartElement("Paragraph");
                            writer.WriteAttributeString("begin", XmlConvert.ToString(p.Begin));
                            writer.WriteAttributeString("end", XmlConvert.ToString(p.End));
                            writer.WriteAttributeString("trainingElement", XmlConvert.ToString(p.trainingElement));
                            writer.WriteAttributeString("Attributes", p.Attributes);

                            writer.WriteStartElement("Phrases");

                            foreach (MyPhrase ph in p.Phrases)
                            {
                                writer.WriteStartElement("Phrase");
                                writer.WriteAttributeString("begin", XmlConvert.ToString(ph.Begin));
                                writer.WriteAttributeString("end", XmlConvert.ToString(ph.End));
                                writer.WriteElementString("Text", ph.Text);
                                writer.WriteEndElement();//Phrase
                            }


                            writer.WriteEndElement();//Phrases
                            writer.WriteElementString("speakerID", XmlConvert.ToString(p.speakerID));
                            writer.WriteEndElement();//Paragraph
                        }

                        writer.WriteEndElement();//Paragraphs




                        writer.WriteStartElement("PhoneticParagraphs");
                        foreach (MyParagraph p in s.Paragraphs)
                        {
                            writer.WriteStartElement("Paragraph");
                            writer.WriteAttributeString("begin", XmlConvert.ToString(p.Begin));
                            writer.WriteAttributeString("end", XmlConvert.ToString(p.End));
                            writer.WriteAttributeString("trainingElement", XmlConvert.ToString(p.trainingElement));
                            writer.WriteAttributeString("Attributes", p.Attributes);

                            writer.WriteStartElement("Phrases");

                            foreach (MyPhrase ph in p.Phrases)
                            {
                                writer.WriteStartElement("Phrase");
                                writer.WriteAttributeString("begin", XmlConvert.ToString(ph.Begin));
                                writer.WriteAttributeString("end", XmlConvert.ToString(ph.End));
                                writer.WriteElementString("Text", ph.Phonetics);
                                writer.WriteEndElement();//Phrase
                            }


                            writer.WriteEndElement();//Phrases
                            writer.WriteElementString("speakerID", XmlConvert.ToString(p.speakerID));
                            writer.WriteEndElement();//Paragraph
                        }



                        writer.WriteEndElement();//PhoneticParagraphs
                        writer.WriteEndElement();//section
                    }


                    writer.WriteEndElement();//sections
                    writer.WriteEndElement();//chapter
                }

                writer.WriteEndElement();//chapters


                writer.WriteStartElement("SpeakersDatabase");
                writer.WriteStartElement("Speakers");



                foreach (MySpeaker sp in pCopy.m_SeznamMluvcich.Speakers)
                {
                    writer.WriteStartElement("Speaker");
                    writer.WriteElementString("ID", XmlConvert.ToString(sp.ID));
                    writer.WriteElementString("Firstname", sp.FirstName);
                    writer.WriteElementString("Surname", sp.Surname);
                    writer.WriteElementString("Sex", sp.Sex);
                    writer.WriteElementString("Comment", sp.Comment);

                    writer.WriteEndElement();//speaker
                }

                writer.WriteEndElement();//Speakers
                writer.WriteEndElement();//SpeakersDatabase

                writer.WriteEndElement();//Transcription

                writer.Close();

                if (!aUkladatKompletMluvci)
                {
                    if (co != null)
                    {
                        co.m_SeznamMluvcich = pKopieMluvcich;
                    }

                }


                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri serializaci souboru: " + ex.Message);
                return false;
            }
        }
        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru - muze ulozit mluvci bez fotky
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serialize(Stream datastream, bool aUkladatKompletMluvci)
        {
            XDocument xdoc = 
                new XDocument(new XDeclaration("1.0","utf-8","yes"),
                    new XElement("transcription",elements.Select(e=>new XAttribute(e.Key,e.Value)).Union(new []{ new XAttribute("version","2.0"),new XAttribute("style",MySetup.Setup.SaveInShortFormat?"short":"strict"),new XAttribute("mediaURI",mediaURI??"")}),
                        this.Meta,
                        Chapters.Select(c => c.Serialize(!MySetup.Setup.SaveInShortFormat)),
                        Speakers.Serialize(!MySetup.Setup.SaveInShortFormat)
                    )
                );

            xdoc.Save(datastream);
            return true;
        }

        //Deserializuje soubor             
        public static MySubtitlesData Deserialize(string filename)
        {
            Stream s = File.Open(filename, FileMode.Open);
            MySubtitlesData dta = Deserialize(s);

            try
            {
                if (dta != null)
                {
                    dta.JmenoSouboru = filename;
                    dta.Ulozeno = true;
                    return dta;
                }
            }
            finally
            {
                s.Close();
            }
            return null;
        }

        //Deserializuje soubor             
        public static MySubtitlesData Deserialize(Stream datastream)
        {
            XmlTextReader reader = new XmlTextReader(datastream);
            if (reader.Read())
            {
                reader.Read();
                reader.Read();
                string version = reader.GetAttribute("version");
                
                if (version == "2.0")
                {
                    return DeserializeV2_0(reader);
                }
                else
                {
                    datastream.Position = 0;
                    return DeserializeV1(new XmlTextReader(datastream));
                }
            }

            return null;
        }

        private XElement Meta = EmptyMeta;
        private static readonly XElement EmptyMeta = new XElement("Meta");
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static MySubtitlesData DeserializeV2_0(XmlTextReader reader)
        {

            MySubtitlesData data = new MySubtitlesData();
            data.BeginUpdate();
            var document = XDocument.Load(reader);
            var transcription = document.Elements().First();

            string style = transcription.Attribute("style").Value;

            bool isStrict = style == "strict";
            string version = transcription.Attribute("version").Value;
            string mediaURI = transcription.Attribute("mediaURI").Value;
            data.mediaURI = mediaURI;
            data.Meta = transcription.Element("meta") ?? EmptyMeta;
            var chapters = transcription.Elements(isStrict?"chapter":"ch");

            data.elements = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            data.elements.Remove("style");
            data.elements.Remove("mediaURI");
            data.elements.Remove("version");

            chapters.Select(c => (TranscriptionElement)new MyChapter(c,isStrict)).ToList().ForEach(c=>data.Add(c));

            var speakers = transcription.Element(isStrict?"speakers":"sp");
            data.Speakers.Speakers = new List<MySpeaker>(speakers.Elements("speaker").Select(s => new MySpeaker(s,isStrict)));
            data.EndUpdate();

            return data;
        }


        public static MySubtitlesData DeserializeV1(XmlTextReader reader)
        {
            try
            {
                MySubtitlesData data = new MySubtitlesData();
                reader.WhitespaceHandling = WhitespaceHandling.Significant;

                reader.Read(); //<?xml version ...
                reader.Read();

                reader.ReadStartElement("Transcription");
                string val = reader.GetAttribute("dateTime");
                if (val != null)
                    data.dateTime = XmlConvert.ToDateTime(val, XmlDateTimeSerializationMode.Local);
                data.mediaURI = reader.GetAttribute("audioFileName");

                int result;

                //reader.Read();
                reader.ReadStartElement("Chapters");
                //reader.ReadStartElement("Chapter");
                while (reader.Name == "Chapter")
                {
                    MyChapter c = new MyChapter();
                    c.name = reader.GetAttribute("name");

                    val = reader.GetAttribute("begin");
                    if (int.TryParse(val, out result))
                        if (result < 0)
                            c.Begin = new TimeSpan(result);
                        else
                            c.Begin = TimeSpan.FromMilliseconds(result);
                    else
                        c.Begin = XmlConvert.ToTimeSpan(val);

                    val = reader.GetAttribute("end");
                    if (int.TryParse(val, out result))
                        if (result < 0)
                            c.End = new TimeSpan(result);
                        else
                            c.End = TimeSpan.FromMilliseconds(result);
                    else
                        c.End = XmlConvert.ToTimeSpan(val);

                    reader.Read();

                    reader.ReadStartElement("Sections");


                    while (reader.Name == "Section")
                    {

                        MySection s = new MySection();
                        s.name = reader.GetAttribute("name");

                        val = reader.GetAttribute("begin");
                        if (int.TryParse(val, out result))
                            if (result < 0)
                                s.Begin = new TimeSpan(result);
                            else
                                s.Begin = TimeSpan.FromMilliseconds(result);
                        else
                            s.Begin = XmlConvert.ToTimeSpan(val);

                        val = reader.GetAttribute("end");
                        if (int.TryParse(val, out result))
                            if (result < 0)
                                s.End = new TimeSpan(result);
                            else
                                s.End = TimeSpan.FromMilliseconds(result);
                        else
                            s.End = XmlConvert.ToTimeSpan(val);

                        reader.Read();
                        reader.ReadStartElement("Paragraphs");

                        while (reader.Name == "Paragraph")
                        {
                            MyParagraph p = new MyParagraph();
                            val = reader.GetAttribute("begin");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1 && s.Paragraphs.Count > 0)
                                        p.Begin = s.Paragraphs[s.Paragraphs.Count - 1].End;
                                    else
                                        p.Begin = new TimeSpan(result);
                                else
                                    p.Begin = TimeSpan.FromMilliseconds(result);
                            else
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    p.End = new TimeSpan(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else
                                p.End = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("trainingElement");
                            p.trainingElement = val == null ? false : XmlConvert.ToBoolean(val);
                            p.Attributes = reader.GetAttribute("Attributes");

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                MyPhrase ph = new MyPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.Begin = new TimeSpan(result);
                                    else
                                        ph.Begin = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.End = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;

                                if (reader.IsEmptyElement)
                                    reader.Read();

                                while (reader.Name == "Text")
                                {
                                    reader.WhitespaceHandling = WhitespaceHandling.All;
                                    if (!reader.IsEmptyElement)
                                    {
                                        reader.Read();
                                        while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element)
                                        {
                                            ph.Text = reader.Value.Trim('\r', '\n');
                                            reader.Read();
                                        }
                                    }
                                    reader.WhitespaceHandling = WhitespaceHandling.Significant;
                                    reader.ReadEndElement();//text
                                }
                                p.Phrases.Add(ph);
                                if (reader.Name != "Phrase") //text nebyl prazdny
                                {
                                    reader.Read();//text;
                                    reader.ReadEndElement();//Text;
                                }
                                reader.ReadEndElement();//Phrase;

                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.speakerID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.speakerID == -1)
                                p.speakerID = MySpeaker.DefaultID;

                            reader.ReadEndElement();//paragraph
                            s.Paragraphs.Add(p);

                        }

                        if (reader.Name == "Paragraphs") //teoreticky mohl byt prazdny
                            reader.ReadEndElement();

                        if (reader.Name == "PhoneticParagraphs")
                            reader.ReadStartElement();

                        while (reader.Name == "Paragraph")
                        {
                            MyParagraph p = new MyParagraph();
                            val = reader.GetAttribute("begin");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    p.Begin = TimeSpan.Zero;
                                else
                                    p.Begin = TimeSpan.FromMilliseconds(result);
                            else
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1)
                                    {
                                        p.Begin = s.Paragraphs[s.Paragraphs.Count - 1].End;
                                    }
                                    else
                                        p.End = TimeSpan.FromMilliseconds(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else
                                p.End = XmlConvert.ToTimeSpan(val);

                            p.trainingElement = XmlConvert.ToBoolean(reader.GetAttribute("trainingElement"));
                            p.Attributes = reader.GetAttribute("Attributes");

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                MyPhrase ph = new MyPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.Begin = p.Begin;
                                    else
                                        ph.Begin = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.End = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;
                                reader.ReadStartElement("Text");//posun na content
                                ph.Text = reader.Value.Trim('\r', '\n');

                                if (reader.Name != "Phrase") //text nebyl prazdny
                                {
                                    reader.Read();//text;
                                    reader.ReadEndElement();//Text;
                                }

                                if (reader.Name == "TextPrepisovany")
                                {
                                    reader.ReadElementString();
                                }

                                p.Phrases.Add(ph);
                                reader.ReadEndElement();//Phrase;
                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.speakerID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.speakerID == -1)
                                p.speakerID = MySpeaker.DefaultID;

                            reader.ReadEndElement();//paragraph

                            //zarovnani fonetiky k textu


                            MyParagraph bestpar = null;
                            TimeSpan timeinboth = TimeSpan.Zero;

                            if (p.Phrases.Count == 0)
                                continue;


                            TimeSpan minusone = new TimeSpan(-1);

                            foreach (MyParagraph v in s.Paragraphs)
                            {
                                if (v.End < p.Begin && v.End != minusone && p.Begin != minusone)
                                    continue;

                                if (v.Begin > p.End && v.End != minusone && v.Begin != minusone)
                                    continue;

                                TimeSpan beg = v.Begin > p.Begin ? v.Begin : p.Begin;
                                TimeSpan end;

                                if (v.End < p.End)
                                {
                                    end = v.End;
                                    if (v.End == minusone)
                                        end = p.End;
                                }
                                else
                                {
                                    end = p.End;
                                    if (p.End == minusone)
                                        end = v.End;
                                }

                                TimeSpan duration = end - beg;





                                if (bestpar == null)
                                {
                                    bestpar = v;
                                    timeinboth = duration;
                                }
                                else
                                {
                                    if (duration > timeinboth)
                                    {
                                        timeinboth = duration;
                                        bestpar = v;
                                    }
                                }
                            }

                            if (bestpar != null)
                            {
                                if (p.Phrases.Count == bestpar.Phrases.Count)
                                {
                                    for (int i = 0; i < p.Phrases.Count; i++)
                                    {
                                        bestpar.Phrases[i].Phonetics = p.Phrases[i].Text;
                                    }
                                }
                                else
                                {
                                    int i = 0;
                                    int j = 0;

                                    TimeSpan actual = p.Phrases[i].Begin;
                                    while (i < p.Phrases.Count && j < bestpar.Phrases.Count)
                                    {
                                        MyPhrase to = p.Phrases[i];
                                        MyPhrase from = bestpar.Phrases[j];
                                        if (true)
                                        {

                                        }
                                        i++;
                                    }
                                }

                            }
                        }
                        if (reader.Name == "PhoneticParagraphs" && reader.NodeType == XmlNodeType.EndElement)
                            reader.ReadEndElement();


                        if (!(reader.Name == "Section" && reader.NodeType == XmlNodeType.EndElement))
                        {

                            if (reader.Name != "speaker")
                                reader.Read();

                            int spkr = XmlConvert.ToInt32(reader.ReadElementString("speaker"));
                            s.Speaker = (spkr < 0) ? MySpeaker.DefaultID : spkr;

                        }
                        c.Sections.Add(s);
                        reader.ReadEndElement();//section
                    }

                    if (reader.Name == "Sections")
                        reader.ReadEndElement();//sections
                    reader.ReadEndElement();//chapter
                    data.Chapters.Add(c);
                }

                reader.ReadEndElement();//chapters
                reader.ReadStartElement("SpeakersDatabase");
                reader.ReadStartElement("Speakers");


                while (reader.Name == "Speaker")
                {
                    bool end = false;

                    MySpeaker sp = new MySpeaker();
                    reader.ReadStartElement("Speaker");
                    while (!end)
                    {
                        switch (reader.Name)
                        {
                            case "ID":

                                sp.ID = XmlConvert.ToInt32(reader.ReadElementString("ID"));
                                break;
                            case "Surname":
                                sp.Surname = reader.ReadElementString("Surname");
                                break;
                            case "Firstname":
                                sp.FirstName = reader.ReadElementString("Firstname");
                                break;
                            case "FirstName":
                                sp.FirstName = reader.ReadElementString("FirstName");
                                break;
                            case "Sex":
                                sp.Sex = reader.ReadElementString("Sex");
                                break;
                            case "Comment":
                                sp.Comment = reader.ReadElementString("Comment");
                                break;
                            case "Speaker":
                                if (reader.NodeType == XmlNodeType.EndElement)
                                {
                                    reader.ReadEndElement();
                                    end = true;
                                }
                                else
                                    goto default;
                                break;

                            default:
                                if (reader.IsEmptyElement)
                                    reader.Read();
                                else
                                    reader.ReadElementString();
                                break;
                        }
                    }
                    data.m_SeznamMluvcich.Speakers.Add(sp);
                }



                return data;
            }
            catch (Exception ex)
            {
                if (reader != null)
                    MessageBox.Show(string.Format("Chyba pri deserializaci souboru:(řádek:{0}, pozice:{1}) {2}", reader.LineNumber, reader.LinePosition, ex.Message));
                else
                    MessageBox.Show("Chyba pri deserializaci souboru: " + ex.Message);
                return null;
            }

        }


        #region IList<TranscriptionElement> Members

        public int IndexOf(TranscriptionElement item)
        {
            int i = 0;
            if (Chapters.Count == 0)
                return -1;

            TranscriptionElement cur = Chapters[0];
            while (cur != null && cur != item)
            {
                i++;
                cur = cur.NextSibling();
            }


            return i;
        }

        public override void Insert(int index, TranscriptionElement item)
        {
            throw new NotSupportedException();
        }

        public override TranscriptionElement this[int index]
        {
            get
            {
                int i = 0;
                foreach (MyChapter c in Chapters)
                {
                    if (i == index)
                        return c;
                    i++;
                    if (index < i + c.GetTotalChildrenCount())
                    {
                        foreach (MySection s in c.Sections)
                        {
                            if (i == index)
                                return s;
                            i++;
                            if (index < i + s.GetTotalChildrenCount())
                            {
                                return s.Paragraphs[index - i];

                            }
                            i += s.GetTotalChildrenCount();
                        }

                    }
                    i += c.GetTotalChildrenCount();
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection<TranscriptionElement> Members

        public override void Add(TranscriptionElement item)
        {
            if (item is MyChapter)
            {
                base.Add(item);
                this.ChildrenCountChanged(NotifyCollectionChangedAction.Add);
            }
            else if (item is MySection)
            {
                m_children[m_children.Count - 1].Add(item);
            }
            else if (item is MyParagraph)
            {
                m_children[m_children.Count - 1].Children[m_children[m_children.Count - 1].Children.Count - 1].Add(item);
            }
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TranscriptionElement item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(TranscriptionElement[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return Chapters.Sum(x => x.GetTotalChildrenCount()) + Chapters.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public override bool Remove(TranscriptionElement item)
        {
            if (item is MyChapter)
            {
                return base.Remove(item);
            }

            foreach (TranscriptionElement el in this)
            {
                if (el == item)
                {
                    return item.Parent.Remove(item);
                }
            }

            return false;
        }

        #endregion

        #region IEnumerable<TranscriptionElement> Members

        public IEnumerator<TranscriptionElement> GetEnumerator()
        {
            foreach (MyChapter c in this.Chapters)
            {
                yield return c;

                foreach (MySection s in c.Sections)
                {
                    yield return s;

                    foreach (MyParagraph p in s.Paragraphs)
                    {
                        yield return p;
                    }
                }
            }
            yield break;
        }


        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        public event Action SubtitlesChanged;


        public override void ChildrenCountChanged(NotifyCollectionChangedAction action)
        {
            if (SubtitlesChanged != null)
                SubtitlesChanged();
            //if (CollectionChanged != null)
            //{
            //    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            //}
        }

        public override string Text
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public override string Phonetics
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public override void ElementChanged(TranscriptionElement element)
        {
             
        }

        public override void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            if(CollectionChanged!=null)
                CollectionChanged(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,oldelement,newelement));
        }

        public override void ElementInserted(TranscriptionElement element, int index)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, element, index));
        }

        public override void ElementRemoved(TranscriptionElement element, int index)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, element, index));
        }
    }
}
