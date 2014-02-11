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

            ID = -1;
            FirstName = null;
            Surname = null;
            Sex = null;
            RozpoznavacMluvci = null;
            RozpoznavacJazykovyModel = null;
            RozpoznavacPrepisovaciPravidla = null;
            FotoJPGBase64 = null;
            Comment = null;
        }
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

    }

    public abstract class TranscriptionElement
    {
        public double heigth;
        protected TimeSpan m_begin = new TimeSpan(-1);
        public TimeSpan Begin
        {
            get { return m_begin; }
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
            get { return m_end; }
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
            m_children = new List<TranscriptionElement>() ;
        }

        public virtual TranscriptionElement this[int Index]
        { 
            get
            {
                foreach (TranscriptionElement e in Children)
                {
                    if (--Index == 0)
                        return e;   
                }

                return null;
            }

            set
            {
                throw new NotSupportedException();
            }
        
        }

        private TranscriptionElement m_Parent;
        private int m_ParentIndex;
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
            get{return m_children;}
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
            ChildrenCountChanged();
        }

        public virtual void Insert(int index, TranscriptionElement data)
        {
            if(index <0 || index>Children.Count)
                throw new IndexOutOfRangeException();

            m_children.Insert(index, data);
            data.m_Parent = this;
            for (int i = index; i < m_children.Count; i++)
            {
                m_children[i].m_ParentIndex = i;
            }

            ChildrenCountChanged();
        
        }

        public virtual void RemoveAt(int index)
        {
             
            if(index < 0 || index >= Children.Count)
                throw new IndexOutOfRangeException();


            var c = m_children[index];
            c.m_Parent = null;
            c.m_ParentIndex = -1;
            m_children.RemoveAt(index);

            for (int i = index; i < m_children.Count; i++)
            {
                m_children[i].m_ParentIndex = i;
            }

            ChildrenCountChanged();
        }

        public virtual bool Remove(TranscriptionElement value)
        { 
             RemoveAt(m_children.IndexOf(value));

             ChildrenCountChanged();
             return true;
        }


        public TranscriptionElement Next()
        {

                if (m_Parent == null)
                    return null;
                if (m_ParentIndex == m_Parent.m_children.Count - 1)
                {
                    TranscriptionElement te = m_Parent.Next();
                    if (te != null && te.Children.Count >0)
                        return te.m_children[0];
                    else
                        return null;
                }
                else
                {
                    return m_Parent.m_children[m_ParentIndex + 1];
                }

        }

        public TranscriptionElement Previous()
        {

            if (m_Parent == null)
                return null;
            if (m_ParentIndex == 0)
            {
                TranscriptionElement te = m_Parent.Previous();
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
    
        public virtual int GetTotalChildrenCount()
        {
            int c = m_children.Count;
            foreach (var ch in m_children)
                c += ch.GetTotalChildrenCount();

            return c;
        }

        public virtual void ChildrenCountChanged()
        {
            if (Parent != null)
                Parent.ChildrenCountChanged();
        }
    }

    //nejmensi textovy usek - muze byt veta, vice slov nebo samotne slovo
    public sealed class MyPhrase : TranscriptionElement
    {
        private string m_text;//slovo/a ktere obsahuji i mezery na konci
        
        public override string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        private string m_phonetics;

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

        public MyPhrase(MyPhrase kopie)
        {
            this.m_begin = kopie.m_begin;
            this.m_end = kopie.m_end;
            this.m_text = kopie.m_text;
            this.m_phonetics = kopie.m_phonetics;
            this.heigth = kopie.heigth;
        }

        public MyPhrase():base()
        {

        }

        public MyPhrase(TimeSpan begin, TimeSpan end, string aWords):this()
        {
            this.Begin = begin;
            this.End = end;
            this.Text = aWords;
        }

        public MyPhrase(TimeSpan begin, TimeSpan end, string aWords, MyEnumTypElementu aElementType):this(begin,end,aWords)
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


        public override void ChildrenCountChanged()
        {
        }

    }

    public class VirtualTypeList<T> : IList<T> where T : TranscriptionElement
    {
        List<TranscriptionElement> m_elementlist;
        TranscriptionElement m_parent;
        public VirtualTypeList(TranscriptionElement parent)
        {
            if(parent == null)
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
                m_elementlist[index] = value;
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

        class VirtualEnumerator<R> : IEnumerator<R> where R:TranscriptionElement
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
                    return (R) tre.Current;
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
            set { }
        }


        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - fonetika
        /// </summary>
        public override string  Phonetics
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
                if (value == null)
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

        public int speakerID = -1;

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


        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MyParagraph(MyParagraph aKopie) : this()
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
                    this.Phrases.Add(new MyPhrase( aKopie.Phrases[i]));
                }
            }
            this.speakerID = aKopie.speakerID;
        }

        public MyParagraph(List<MyPhrase> phrases):this()
        {
            foreach(var p in phrases)
                Add(p);
            if (Phrases.Count > 0)
            {
                this.Begin = Phrases[0].Begin;
                this.End = Phrases[Phrases.Count - 1].End;
            }
        }

        public MyParagraph():base()
        {
            Phrases = new VirtualTypeList<MyPhrase>(this);
            this.Begin = new TimeSpan(-1);
            this.End = new TimeSpan(-1);
            this.trainingElement = false;
            this.speakerID = -1;
            
        }

        public MyParagraph(String aText, List<MyCasovaZnacka> aCasoveZnacky, TimeSpan aBegin, TimeSpan aEnd) : this()
        {
            //this.text = aText;
            this.Begin = aBegin;
            this.End = aEnd;
            this.trainingElement = false;
            this.speakerID = -1;
        }
    }



    //sekce textu nadrazena odstavci
    public class MySection:TranscriptionElement
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

        public String name;

        public VirtualTypeList<MyParagraph> Paragraphs;

        public int speaker;


        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MySection(MySection aKopie):this()
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

        public MySection(String aName) : this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {
        }
        public MySection(String aName, TimeSpan aBegin, TimeSpan aEnd):this()
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
    public class MyChapter:TranscriptionElement
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

        public String name;

        public VirtualTypeList<MySection> Sections;


        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MyChapter(MyChapter aKopie):this()
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

        public MyChapter():base()
        {
            Sections = new VirtualTypeList<MySection>(this);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
            Sections = new VirtualTypeList<MySection>(this);
        }

        public MyChapter(String aName): this(aName, new TimeSpan(-1), new TimeSpan(-1))
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

    public class MySubtitlesData : TranscriptionElement, IList<TranscriptionElement>
    {
        public double TotalHeigth;
        public bool FindNext(ref TranscriptionElement paragraph,ref int TextOffset,string pattern, bool isregex, bool CaseSensitive)
        {
            TranscriptionElement par = paragraph;

            if (par == null)
                return false;

            Regex r;
            if (isregex)
            {
                r = new Regex(pattern);
            }
            else
            {
                r = new Regex(".*?"+pattern);
            }

            TranscriptionElement tag = paragraph;
            while (par != null)
            {
                string s = par.Text;
                if (!CaseSensitive && !isregex)
                    s = s.ToLower();

                Match m = r.Match(s, TextOffset);

                if (m.Success)
                {
                    TextOffset += m.Length;
                    paragraph = tag;
                    return true;
                }

                tag = tag.Next() as MyParagraph; ;
                if (tag == null)
                    return false;
                par = tag;
                TextOffset = 0;
            }

            return false;
        }

        [XmlIgnore()]
        public string JmenoSouboru { get; set; }
        [XmlIgnore()]
        public bool Ulozeno { get; set; }

        /// <summary>
        /// datum a cas poradu, ktery je v transkripci zpracovan - napr. pocatecni cas audio souboru 
        /// </summary>
        [XmlAttribute]
        public DateTime dateTime { get; set; }
        /// <summary>
        /// zdroj odkud je transkripce - radio - nazev kanalu, televize, mikrofon, atd...
        /// </summary>
        [XmlAttribute]
        public string source { get; set; }
        /// <summary>
        /// typ poradu - cele transkripce 
        /// </summary>
        [XmlAttribute]
        public string type { get; set; }
        /// <summary>
        /// zdroj audio dat - muze byt stejny jako video a naopak
        /// </summary>
        [XmlAttribute]
        public string audioFileName { get; set; }
        /// <summary>
        /// zdroj video dat - muze byt stejny jako audio a naopak
        /// </summary>
        [XmlAttribute]
        public string videoFileName { get; set; }

        /// <summary>
        /// vsechny kapitoly streamu
        /// </summary>
        public VirtualTypeList<MyChapter> Chapters;    //vsechny kapitoly streamu

        [XmlElement("SpeakersDatabase")]
        public MySpeakers SeznamMluvcich = new MySpeakers();



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
            this.audioFileName = aKopie.audioFileName;
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
            this.SeznamMluvcich = new MySpeakers(aKopie.SeznamMluvcich);
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
                if (el.IsParagraph && el.Begin <= cas && el.End >= cas)
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
                if(el.End <cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (MyParagraph)el;
                    }
                }else
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
                if(el.Begin <cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (MyParagraph)el;
                    }
                }else
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
                    if (this.SeznamMluvcich.OdstranSpeakera(aSpeaker))
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
            int i = this.SeznamMluvcich.NovySpeaker(aSpeaker);
            if (i >= 0) this.Ulozeno = false;
            return i;
        }


        public MySpeaker VratSpeakera(int aIDSpeakera)
        {
            return this.SeznamMluvcich.VratSpeakera(aIDSpeakera);
        }

        /// <summary>
        /// vraci ID speakera podle stringu jmena
        /// </summary>
        /// <param name="aJmeno"></param>
        /// <returns></returns>
        public int NajdiSpeakera(string aJmeno)
        {
            return this.SeznamMluvcich.NajdiSpeakeraID(aJmeno);
        }


        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru - muze ulozit mluvci bez fotky
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serializovat(string jmenoSouboru, MySubtitlesData co, bool aUkladatKompletMluvci)
        {
            Stream s = File.Open(jmenoSouboru, FileMode.Create);
            bool output = Serializovat(s, co, aUkladatKompletMluvci);

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

        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru - muze ulozit mluvci bez fotky
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serializovat(Stream datastream, MySubtitlesData co, bool aUkladatKompletMluvci)
        {
            try
            {
                MySpeakers pKopieMluvcich = new MySpeakers(co.SeznamMluvcich);
                if (!aUkladatKompletMluvci)
                {
                    if (co != null && co.SeznamMluvcich != null)
                    {
                        for (int i = 0; i < co.SeznamMluvcich.Speakers.Count; i++)
                        {
                            co.SeznamMluvcich.Speakers[i].FotoJPGBase64 = null;
                        }
                    }
                }

                MySubtitlesData pCopy = new MySubtitlesData(co);

                System.Xml.XmlTextWriter writer = new XmlTextWriter(datastream, Encoding.UTF8);

                writer.WriteStartDocument(); //<?xml version ...

                
                writer.WriteStartElement("Transcription");
                writer.WriteAttributeString("dateTime", XmlConvert.ToString(pCopy.dateTime,XmlDateTimeSerializationMode.Local));
                writer.WriteAttributeString("audioFileName", pCopy.audioFileName);

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
                                writer.WriteAttributeString("begin", XmlConvert.ToString(p.Begin));
                                writer.WriteAttributeString("end", XmlConvert.ToString(p.End));
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
                                writer.WriteAttributeString("begin", XmlConvert.ToString(p.Begin));
                                writer.WriteAttributeString("end", XmlConvert.ToString(p.End));
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



                foreach (MySpeaker sp in pCopy.SeznamMluvcich.Speakers)
                {
                    writer.WriteStartElement("Speaker");
                    writer.WriteElementString("ID",XmlConvert.ToString(sp.ID));
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
                        co.SeznamMluvcich = pKopieMluvcich;
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


        //Deserializuje soubor             
        public MySubtitlesData Deserializovat(String jmenoSouboru)
        {
            Stream s = File.Open(jmenoSouboru, FileMode.Open);
            MySubtitlesData dta = Deserializovat(s);

            try
            {
                if (dta != null)
                {
                    dta.JmenoSouboru = jmenoSouboru;
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
        public MySubtitlesData Deserializovat(Stream datastream)
        {
            try
            {
                MySubtitlesData data = new MySubtitlesData();
                System.Xml.XmlTextReader reader = new XmlTextReader(datastream);
                reader.WhitespaceHandling = WhitespaceHandling.Significant;

                reader.Read(); //<?xml version ...


                reader.Read();
                
               // reader.ReadStartElement("Transcription");
                data.dateTime = XmlConvert.ToDateTime(reader.GetAttribute("dateTime"),XmlDateTimeSerializationMode.Local);
                data.audioFileName = reader.GetAttribute("audioFileName");
                
                int result;
                string val;

                reader.Read();
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
                    //reader.ReadStartElement("Section");

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

                            p.trainingElement = XmlConvert.ToBoolean(reader.GetAttribute("trainingElement"));
                            p.Attributes = reader.GetAttribute("Attributes");

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                MyPhrase ph = new MyPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    ph.Begin = new TimeSpan(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    ph.Begin = new TimeSpan(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;
                                reader.ReadStartElement("Text");//posun na content
                                ph.Text = reader.Value;
                                p.Phrases.Add(ph);

                                if (reader.Name != "Phrase") //text nebyl prazdny
                                {
                                    reader.Read();//text;
                                    reader.ReadEndElement();//Text;
                                }
                                reader.ReadEndElement();//Phrase;
                  
                            }
                            
                            if(reader.Name!="speakerID") 
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.speakerID = XmlConvert.ToInt32(reader.ReadElementString());

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

                            p.trainingElement = XmlConvert.ToBoolean(reader.GetAttribute("trainingElement"));
                            p.Attributes = reader.GetAttribute("Attributes");

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                MyPhrase ph = new MyPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    ph.Begin = new TimeSpan(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    ph.Begin = new TimeSpan(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;
                                reader.ReadStartElement("Text");//posun na content
                                ph.Text = reader.Value;
                                
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
                                //reader.Read();//Phrase | phhrases end element
                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.speakerID = XmlConvert.ToInt32(reader.ReadElementString());

                            reader.ReadEndElement();//paragraph
                            
                            //zarovnani fonetiky k textu


                            MyParagraph bestpar = null;
                            TimeSpan timeinboth = TimeSpan.Zero;
                            foreach (MyParagraph v in s.Paragraphs)
                            {
                                if (bestpar == null)
                                    bestpar = v;
                                else
                                {
                                    TimeSpan beg = v.Begin > p.Begin ? v.Begin : p.Begin;
                                    TimeSpan end = v.End < p.End ? v.End : p.End;

                                    TimeSpan duration = end - beg;

                                    if (duration > timeinboth)
                                    {
                                        timeinboth = duration;
                                        bestpar = v;
                                    }
                                }
                            }

                            foreach (MyPhrase hn in p.Phrases)
                            {
                                MyPhrase bestphrase = null;
                                foreach (MyPhrase h in bestpar.Phrases)
                                {
                                    if (bestphrase == null)
                                        bestphrase = h;
                                    else
                                    {
                                        TimeSpan beg = h.Begin > hn.Begin ? h.Begin : hn.Begin;
                                        TimeSpan end = h.End < hn.End ? h.End : hn.End;

                                        TimeSpan duration = end - beg;

                                        if (duration > timeinboth)
                                        {
                                            timeinboth = duration;
                                            bestphrase = h;
                                        }
                                    }
                                }

                                bestphrase.Phonetics = hn.Text;
                            }


                        }


                        if (!(reader.Name == "Section" && reader.NodeType == XmlNodeType.EndElement))
                        {
                            if (reader.Name != "speaker")
                                reader.Read();

                            s.speaker = XmlConvert.ToInt32(reader.ReadElementString("speaker"));
                        }
                        c.Sections.Add(s);
                        reader.ReadEndElement();//section
                    }

                    reader.ReadEndElement();//sections
                    reader.ReadEndElement();//chapter
                    data.Chapters.Add(c);


                }

                reader.ReadEndElement();//chapters
                reader.ReadStartElement("SpeakersDatabase");
                reader.ReadStartElement("Speakers");

                while (reader.Name == "Speaker")
                {
                    reader.ReadStartElement("Speaker");
                    MySpeaker sp = new MySpeaker();
                    sp.ID = XmlConvert.ToInt32(reader.ReadElementString("ID"));
                    sp.FirstName = reader.ReadElementString("FirstName");
                    sp.Surname = reader.ReadElementString("Surname");
                    sp.Sex = reader.ReadElementString("Sex");
                    sp.Comment = reader.ReadElementString("Comment");
                    reader.ReadEndElement();//speaker
                    data.SeznamMluvcich.Speakers.Add(sp);
                }

                return data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri derializaci souboru: " + ex.Message);
                return null;
            }

                

        }


        #region IList<TranscriptionElement> Members

        public int IndexOf(TranscriptionElement item)
        {
            int i = 0;
            if(Chapters.Count == 0)
                return -1;

            TranscriptionElement cur = Chapters[0];
            while (cur != null && cur != item)
            {
                i++;
                cur = cur.Next();
            }
            

            return i;
        }

        public override void Insert(int index, TranscriptionElement item)
        {
            throw new NotSupportedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public override TranscriptionElement this[int index]
        {
            get
            {
                int i=0;
                foreach (MyChapter c in Chapters)
                {
                    if(i==index)
                        return c;
                    i++;
                    if (index < i + c.GetTotalChildrenCount())
                    {
                        foreach (MySection s in c.Sections)
                        {
                            if(i==index)
                                return s;
                            i++;
                            if (index < i + s.GetTotalChildrenCount())
                            {
                                return s.Paragraphs[i + s.GetTotalChildrenCount() - index];

                            }
                            i+=s.GetTotalChildrenCount();
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
                m_children.Add((MyChapter)item);
                this.ChildrenCountChanged();
            }else if(item is MySection)
            {
                m_children[m_children.Count - 1].Add(item as MySection);
            }else if(item is MyParagraph)
            {
                m_children[m_children.Count - 1].Children[m_children[m_children.Count - 1].Children.Count - 1].Add(item as MySection);
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
            get { return Chapters.Sum(x=>x.GetTotalChildrenCount())+Chapters.Count; }
        }

        public bool IsReadOnly
        {
            get { return true ; }
        }

        public override bool Remove(TranscriptionElement item)
        {
            if (item is MyChapter)
            {
                return Chapters.Remove(item as MyChapter);
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


        public override void ChildrenCountChanged()
        {
            if (SubtitlesChanged != null)
                SubtitlesChanged();
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


    }

    //trida ktera obsahuje informace o casove znacce jednotlicych useku - pocatek a konec a index pozice v textu
    public class MyCasovaZnacka
    {
        public TimeSpan Time { get; set; }   //cas znacky v ms
        public int Index1 { get; set; }  //index v textu pred kurzorem
        public int Index2 { get; set; }  //index v textu za kurzorem - casova znacka lezi uprostred

        /// <summary>
        /// vytvori casovou znacku, kurzor lezi mezi indexem1 a indexem2
        /// </summary>
        /// <param name="aTime"></param>
        /// <param name="aIndexTextu1"></param>
        /// <param name="aIndexTextu2"></param>
        public MyCasovaZnacka(TimeSpan aTime, int aIndexTextu1, int aIndexTextu2)
        {
            this.Time = aTime;
            this.Index1 = aIndexTextu1;
            this.Index2 = aIndexTextu2;
        }
    }

}
