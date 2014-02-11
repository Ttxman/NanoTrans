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

        public MySpeaker(string aSpeakerFirstname, string aSpeakerSurname) //constructor ktery vytvori speakera
        {
            ID = -1;
            FirstName = aSpeakerFirstname;
            Surname = aSpeakerSurname;
            Sex = null;
            RozpoznavacMluvci = null;
            RozpoznavacJazykovyModel = null;
            RozpoznavacPrepisovaciPravidla = null;
            FotoJPGBase64 = null;
            Comment = null;
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
        protected TimeSpan m_begin;
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
        protected TimeSpan m_end;
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

        public TranscriptionElement this[int Index]
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

        public void Add(TranscriptionElement data)
        {
            m_children.Add(data);
            data.m_Parent = this;
            data.m_ParentIndex = m_children.Count - 1;
        }

        public void Insert(TranscriptionElement data, int index)
        {
            if(index <0 || index>Children.Count)
                throw new IndexOutOfRangeException();

            m_children.Insert(index, data);
            data.m_Parent = this;
            for (int i = index; i < m_children.Count; i++)
            {
                m_children[i].m_ParentIndex = i;
            }
            

        
        }

        public void RemoveAt(int index)
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

        }

        public bool Remove(TranscriptionElement value)
        {
           
             RemoveAt(m_children.IndexOf(value));
             return true;
        }


        public TranscriptionElement Next()
        {

                if (m_Parent == null)
                    return null;
                if (m_ParentIndex == m_Parent.m_children.Count - 1)
                {
                    return m_Parent.Next();
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
                    return m_Parent.Previous();
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

        /// <summary>
        /// text ze ktereho vznikl foneticky prepis
        /// </summary>
        public string TextPrepisovany;

        public int speakerID;  //index mluvciho v seznamu dole

        public MyPhrase():base()
        {

        }

        public MyPhrase(TimeSpan begin, TimeSpan end, string aWords):this()
        {
            this.Begin = begin;
            this.End = end;
            this.Text = aWords;
            this.TextPrepisovany = null;
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
                    this.TextPrepisovany = pSplitS[0];
                    if (pPouzeNavrh)
                        this.Text = "{" + this.Text + "}";
                }
            }
        }

        public override int GetTotalChildrenCount()
        {
            return 0;
        }
    }



    public class VirtualTypeList<T> : IList<T> where T : TranscriptionElement
    {
        List<TranscriptionElement> m_elementlist;
        TranscriptionElement m_parent;
        public VirtualTypeList(List<TranscriptionElement> elementlist, TranscriptionElement parent)
        {
            if(elementlist == null || parent == null)
                throw new ArgumentNullException();

            m_elementlist = elementlist;
            m_parent = parent;
        }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return m_elementlist.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_parent.Insert(item, index);
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

        public bool IsPhonetic = false;




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
                this.Phrases = new VirtualTypeList<MyPhrase>(m_children, this);
                for (int i = 0; i < aKopie.Phrases.Count; i++)
                {
                    this.Phrases.Add(aKopie.Phrases[i]);
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
            Phrases = new VirtualTypeList<MyPhrase>(m_children, this);
            this.Begin = new TimeSpan(-1);
            this.End = new TimeSpan(-1);
            this.trainingElement = false;
            this.speakerID = -1;
            
        }
        public MyParagraph(String aText, List<MyCasovaZnacka> aCasoveZnacky):this()
        {
            UlozTextOdstavce(aText, aCasoveZnacky);
        }

        public MyParagraph(String aText, List<MyCasovaZnacka> aCasoveZnacky, TimeSpan aBegin, TimeSpan aEnd) : this()
        {
            //this.text = aText;
            this.Begin = aBegin;
            this.End = aEnd;
            this.trainingElement = false;
            this.speakerID = -1;
            UlozTextOdstavce(aText, aCasoveZnacky);
        }

        /// <summary>
        /// ulozi text odstavce a provede jeho rozdeleni podle casovych znacek - casove znacky jsou mimo pocatek a konec odstavce?
        /// </summary>
        /// <param name="aText"></param>
        /// <param name="pZnacky"></param>
        /// <returns></returns>
        public bool UlozTextOdstavce(string aText, List<MyCasovaZnacka> pZnacky)
        {
            return UlozTextOdstavce(aText, pZnacky, MyEnumTypElementu.normalni);
        }

        /// <summary>
        /// ulozi text odstavce a provede jeho rozdeleni podle casovych znacek - casove znacky jsou mimo pocatek a konec odstavce?
        /// </summary>
        /// <param name="aText"></param>
        /// <param name="pZnacky"></param>
        /// <returns></returns>
        public bool UlozTextOdstavce(string aText, List<MyCasovaZnacka> pZnacky, MyEnumTypElementu aTypOdstavce)
        {
            try
            {
                //this.Phrases.Clear();
                List<MyPhrase> pPhrases = new List<MyPhrase>();
                if (pZnacky != null)
                {
                    if (pZnacky.Count == 0 && aText != null && aText != "")
                    {
                        pPhrases.Add(new MyPhrase(new TimeSpan(-1), new TimeSpan(-1), aText, aTypOdstavce));
                    }

                    for (int i = 0; i < pZnacky.Count; i++)
                    {
                        string pText1 = "";
                        if (pZnacky[i].Index1 >= aText.Length) break; //pokud jsou casove znacky mimo text, jsou ignorovany a vymazany

                        if (pZnacky[i].Index2 >= 0)
                        {
                            if (i + 1 < pZnacky.Count)
                            {
                                if (pZnacky[i + 1].Index1 >= aText.Length)
                                {
                                    pText1 = aText.Substring(pZnacky[i].Index2);
                                    pPhrases.Add(new MyPhrase(pZnacky[i].Time, new TimeSpan(-1), pText1, aTypOdstavce));
                                }
                                else
                                {
                                    //jestli existuje jen 1 znacka,tak musi byt ulozen pocatek odstavce,ktery muze byt bez znacky
                                    if (i == 0 && pZnacky[i].Index2 > 0)
                                    {
                                        pText1 = aText.Substring(0, pZnacky[i].Index2);
                                        if (pText1 != null && pText1 != "")
                                        {
                                            pPhrases.Add(new MyPhrase(new TimeSpan(-1),new TimeSpan( -1), pText1, aTypOdstavce));
                                        }
                                    }

                                    if (pZnacky[i + 1].Index2 - pZnacky[i].Index2 >= 0) //ohlidani zda nejsou znacky posunuty
                                    {
                                        pText1 = aText.Substring(pZnacky[i].Index2, pZnacky[i + 1].Index2 - pZnacky[i].Index2);
                                        pPhrases.Add(new MyPhrase(pZnacky[i].Time, pZnacky[i + 1].Time, pText1, aTypOdstavce));
                                    }
                                }


                            }
                            else
                            {
                                //jestli existuje jen 1 znacka,tak musi byt ulozen pocatek odstavce,ktery muze byt bez znacky
                                if (i == 0 && pZnacky[i].Index2 > 0)
                                {
                                    pText1 = aText.Substring(0, pZnacky[i].Index2);
                                    if (pText1 != null && pText1 != "")
                                    {
                                        Phrases.Add(new MyPhrase(new TimeSpan(-1),new TimeSpan( -1), pText1, aTypOdstavce));
                                    }
                                }
                                pText1 = aText.Substring(pZnacky[i].Index2, aText.Length - pZnacky[i].Index2);
                                pPhrases.Add(new MyPhrase(pZnacky[i].Time,new TimeSpan( -1), pText1, aTypOdstavce));

                            }
                        }


                    }

                    if (pPhrases.Count <= Phrases.Count)
                    {
                        int pKamDosloKopirovani = -1;
                        for (int i = 0; i < pPhrases.Count; i++)
                        {
                            if (pPhrases[i].Text == Phrases[i].Text)
                            {
                                pPhrases[i].TextPrepisovany = Phrases[i].TextPrepisovany;
                                pKamDosloKopirovani = i;
                            }
                            else
                            {
                                break;
                                //pPhrases[i].TextPrepisovany = Phrases[i].TextPrepisovany;
                            }
                        }
                        int pKamSesloKopirovaniOdKonce = pPhrases.Count;
                        int pI = Phrases.Count - 1;
                        for (int i = pPhrases.Count - 1; i > pKamDosloKopirovani; i--)
                        {
                            if (pPhrases[i].Text == Phrases[pI].Text && pI >= 0)
                            {
                                pPhrases[i].TextPrepisovany = Phrases[pI].TextPrepisovany;
                                pKamSesloKopirovaniOdKonce = i;
                                pI--;
                            }
                            else
                            {
                                break;
                                //pPhrases[i].TextPrepisovany = Phrases[i].TextPrepisovany;
                            }
                        }
                        if (pKamDosloKopirovani + 1 == pKamSesloKopirovaniOdKonce)
                        {

                        }
                        else
                        {
                            if (pKamDosloKopirovani + 2 == pKamSesloKopirovaniOdKonce && Phrases.Count == pPhrases.Count)
                            {
                                pPhrases[pKamDosloKopirovani + 1].TextPrepisovany = Phrases[pKamDosloKopirovani + 1].TextPrepisovany;
                            }
                        }
                    }
                    else
                    {

                    }

                }
                Phrases.Clear();
                foreach(var p in pPhrases)
                    Phrases.Add(p);
                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        public int PridejCasovouZnacku(MyCasovaZnacka aCasovaZnacka)
        {
            try
            {
                if (this.Text == null) return 1; //odstavec nema zadny text, nelze pridat znacku
                List<MyCasovaZnacka> pCasoveZnacky = this.VratCasoveZnackyTextu;
                if (pCasoveZnacky.Count == 0)
                {
                    pCasoveZnacky.Add(aCasovaZnacka);
                }
                else
                {
                    bool pVlozeno = false;
                    for (int i = 0; i < pCasoveZnacky.Count; i++)
                    {
                        if (pCasoveZnacky[i].Index2 > aCasovaZnacka.Index2)
                        {
                            pCasoveZnacky.Insert(i, aCasovaZnacka);
                            pVlozeno = true;
                            break;
                        }
                    }
                    if (!pVlozeno)
                    {
                        pCasoveZnacky.Add(aCasovaZnacka);
                    }
                }


                this.UlozTextOdstavce(this.Text, pCasoveZnacky);

                return 0;
            }
            catch
            {
                return -1;
            }

        }
        /// <summary>
        /// vraci list casovych znacek v textu bez casu pocatku a konce
        /// </summary>
        /// <returns></returns>
        public List<MyCasovaZnacka> VratCasoveZnackyTextu
        {
            get
            {
                List<MyCasovaZnacka> pZnacky = new List<MyCasovaZnacka>();
                try
                {
                    //pZnacky.Add(new MyCasovaZnacka(this.begin, 0));//casova znacka pocatku se shoduje s casem odstavce
                    if (Phrases != null && Phrases.Count > 0)
                    {
                        int pIndexTextu1 = -1;
                        int pIndexTextu2 = 0;
                        for (int i = 0; i < Phrases.Count; i++)
                        {
                            if (((MyPhrase)Phrases[i]).Begin >= TimeSpan.Zero)
                            {
                                pZnacky.Add(new MyCasovaZnacka(((MyPhrase)Phrases[i]).Begin, pIndexTextu1, pIndexTextu2));
                            }
                            pIndexTextu1 += ((MyPhrase)Phrases[i]).Text.Length;
                            pIndexTextu2 += ((MyPhrase)Phrases[i]).Text.Length;
                        }
                    }
                    //if (pZnacky.Count == 0) pZnacky.Add(new MyCasovaZnacka(this.begin, -1, 0));//casova znacka pocatku se shoduje s casem odstavce pokud neexistuji napocatku
                }
                catch (Exception ex)
                {
                    MyLog.LogujChybu(ex);
                }
                return pZnacky;
            }
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

        public String name;

        public VirtualTypeList<MyParagraph> Paragraphs;
        /// <summary>
        /// foneticka verze obycejneho odstavce
        /// </summary>
        public List<MyParagraph> PhoneticParagraphs = new List<MyParagraph>();


        public int speaker;

        /// <summary>
        /// Informace, jestli ma tato sekce odstavec nebo vice
        /// </summary>
        public bool hasParagraph
        {
            get { if (this.Paragraphs != null && this.Paragraphs.Count > 0) return true; else return false; }
        }

        /// <summary>
        /// Informace, jestli ma tato sekce foneticky neprazdny odstavec nebo vice
        /// </summary>
        public bool hasPhoneticParagraph
        {
            get
            {
                if (this.PhoneticParagraphs == null || this.PhoneticParagraphs.Count == 0) return false;
                bool pMa = false;
                foreach (MyParagraph ppp in PhoneticParagraphs)
                {
                    if (ppp.Text != null && ppp.Text != "")
                    {
                        pMa = true;
                        break;
                    }
                }
                return pMa;
            }
        }

        /// <summary>
        /// Informace, jestli ma tato sekce trenovaci element - finalni uzamceny
        /// </summary>
        public bool hasTrainingParagraph
        {
            get
            {
                if (this.Paragraphs == null || this.Paragraphs.Count == 0) return false;
                bool pMa = false;
                foreach (MyParagraph ppp in Paragraphs)
                {
                    if (ppp.trainingElement)
                    {
                        pMa = true;
                        break;
                    }
                }
                return pMa;
            }
        }



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
                this.Paragraphs = new VirtualTypeList<MyParagraph>(m_children,this);
                for (int i = 0; i < aKopie.Paragraphs.Count; i++)
                {
                    this.Paragraphs.Add(new MyParagraph(aKopie.Paragraphs[i]));
                }
            }
            if (aKopie.PhoneticParagraphs != null)
            {
                this.PhoneticParagraphs = new List<MyParagraph>();
                for (int i = 0; i < aKopie.PhoneticParagraphs.Count; i++)
                {
                    this.PhoneticParagraphs.Add(new MyParagraph(aKopie.PhoneticParagraphs[i]));
                }
            }
        }

        public MySection()
        {
            Paragraphs = new VirtualTypeList<MyParagraph>(m_children, this);
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
        public String name;

        public VirtualTypeList<MySection> Sections;

        /// <summary>
        /// Informace, jestli ma tato kapitola nejakou sekci
        /// </summary>
        public bool hasSection
        {
            get { if (this.Sections != null && this.Sections.Count > 0) return true; else return false; }
        }


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
                this.Sections = new VirtualTypeList<MySection>(m_children, this);
                for (int i = 0; i < aKopie.Sections.Count; i++)
                {
                    this.Sections.Add(new MySection(aKopie.Sections[i]));
                }
            }
        }

        public MyChapter():base()
        {
            Sections = new VirtualTypeList<MySection>(m_children, this);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
            Sections = new VirtualTypeList<MySection>(m_children, this);
        }

        public MyChapter(String aName): this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {

        }
        public MyChapter(String aName, TimeSpan aBegin, TimeSpan aEnd)
        {
            Sections = new VirtualTypeList<MySection>(m_children, this);
            this.name = aName;
            this.Begin = aBegin;
            this.End = aEnd;
        }
    }


    //hlavni trida s titulky a se vsemi potrebnymi metodami pro serializaci

    public class MySubtitlesData : IList<TranscriptionElement>
    {
        public double TotalHeigth;
        public bool FindNext(ref MyTag paragraph,ref int TextOffset,string pattern, bool isregex, bool CaseSensitive)
        {
            MyParagraph par = this[paragraph];

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

            MyTag tag = paragraph;
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

                tag = this.VratOdstavecNasledujiciTag(tag);
                if (tag == null)
                    return false;
                par = this[tag];
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

        /*        [XmlAttribute]
                [DefaultValueAttribute(0)]
                public int SecondsSpendWriting { get; set; }

                [XmlAttribute]
                [DefaultValueAttribute(0)]
                public TimeSpan SecondsSpendPlaying { get; set; }
                */
        /// <summary>
        /// vsechny kapitoly streamu
        /// </summary>
        public List<MyChapter> Chapters = new List<MyChapter>();    //vsechny kapitoly streamu

        [XmlElement("SpeakersDatabase")]
        public MySpeakers SeznamMluvcich = new MySpeakers();



        public MySubtitlesData()
        {
            JmenoSouboru = null;
            Ulozeno = false;

            //constructor  
        }


        public MyParagraph this[MyTag tag]
        {
            get 
            {
                return this.VratOdstavec(tag);
            }
        
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
                this.Chapters = new List<MyChapter>();
                for (int i = 0; i < aKopie.Chapters.Count; i++)
                {
                    this.Chapters.Add(new MyChapter(aKopie.Chapters[i]));
                }
            }
            this.JmenoSouboru = aKopie.JmenoSouboru;
            this.SeznamMluvcich = new MySpeakers(aKopie.SeznamMluvcich);
            this.Ulozeno = aKopie.Ulozeno;
        }

        //funkce pridavajici jesnotlive kapitoly, vraci index pridane kapitoly
        //tvori 'rozhranni pro komunikaci'
        [Obsolete]
        public int NovaKapitola()
        {
            try
            {
                Ulozeno = false;
                Chapters.Add(new MyChapter());
                return Chapters.Count - 1;
            }
            catch
            {
                return -1;
            }
        }
        [Obsolete]
        public int NovaKapitola(int aIndexNoveKapitoly, string nazev)
        {
            if (aIndexNoveKapitoly > Chapters.Count) return -1;
            Ulozeno = false;
            if (aIndexNoveKapitoly < 0)
            {
                Chapters.Add(new MyChapter(nazev));
                return Chapters.Count - 1;
            }
            else
            {
                Chapters.Insert(aIndexNoveKapitoly, new MyChapter(nazev));
                return aIndexNoveKapitoly;
            }
        }
        [Obsolete]
        public int NovaKapitola(int aIndexNoveKapitoly, string nazev, TimeSpan begin, TimeSpan end)
        {
            if (aIndexNoveKapitoly > Chapters.Count) return -1;
            Ulozeno = false;
            if (aIndexNoveKapitoly < 0)
            {
                Chapters.Add(new MyChapter(nazev, begin, end));
                return Chapters.Count - 1;
            }
            else
            {
                Chapters.Insert(aIndexNoveKapitoly, new MyChapter(nazev, begin, end));
                return aIndexNoveKapitoly;
            }
        }

        [Obsolete]
        public int NovaSekce(int kapitola, string nazev, int index, TimeSpan begin, TimeSpan end)
        {
            try
            {
                if (index < -1)
                {
                    Ulozeno = false;
                    //return ((MyChapter)chapters[kapitola]).sections.Add(new MySection(nazev, begin, end));
                    Chapters[kapitola].Sections.Add(new MySection(nazev, begin, end));
                    return Chapters[kapitola].Sections.Count - 1;
                }
                else
                {
                    index++;
                    //((MyChapter)chapters[kapitola]).sections.Insert(index, new MySection(nazev, begin, end));
                    Chapters[kapitola].Sections.Insert(index, new MySection(nazev, begin, end));
                    Ulozeno = false;
                    return index;

                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return -1;
            }
        }
        [Obsolete]
        public int NovyOdstavec(int kapitola, int sekce, int index)
        {
            try
            {
                if (index < 0)
                {
                    Ulozeno = false;
                    //return ((MySection)((MyChapter)chapters[kapitola]).sections[sekce]).paragraphs.Add(new MyParagraph());
                    Chapters[kapitola].Sections[sekce].Paragraphs.Add(new MyParagraph());
                    Chapters[kapitola].Sections[sekce].PhoneticParagraphs.Add(new MyParagraph()); //prida odpovidajici foneticky odstavec jako null, ptz je prazdny
                    return Chapters[kapitola].Sections[sekce].Paragraphs.Count - 1;
                }
                else
                {
                    index++;
                    Chapters[kapitola].Sections[sekce].Paragraphs.Insert(index, new MyParagraph());
                    Chapters[kapitola].Sections[sekce].PhoneticParagraphs.Insert(index, new MyParagraph());//prida odpovidajici foneticky odstavec jako null, ptz je prazdny
                    Ulozeno = false;
                    return index;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Chyba pri vkladani odstavce..." + ex.Message);
                MyLog.LogujChybu(ex);
                return -1;
            }
        }

        /// <summary>
        /// smaze kapitolu z datove struktury
        /// </summary>
        /// <param name="kapitola"></param>
        /// <returns></returns>
        [Obsolete]
        public bool SmazKapitolu(int aKapitola)
        {
            try
            {
                Chapters.RemoveAt(aKapitola);
                Ulozeno = false;
                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }
        }
        [Obsolete]
        public bool SmazSekci(int kapitola, int sekce)
        {
            try
            {
                ((MyChapter)Chapters[kapitola]).Sections.RemoveAt(sekce);
                Ulozeno = false;
                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }
        }
        [Obsolete]
        public bool SmazOdstavec(int kapitola, int sekce, int index)
        {
            try
            {
                Chapters[kapitola].Sections[sekce].Paragraphs.RemoveAt(index);
                Chapters[kapitola].Sections[sekce].PhoneticParagraphs.RemoveAt(index);//smazani foneticke varianty
                Ulozeno = false;
                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }
        }
        [Obsolete]
        public int NovyOdstavec(int kapitola, int sekce, String text, List<MyCasovaZnacka> aCasoveZnacky, int index)
        {
            try
            {
                if (index < 0)
                {
                    Ulozeno = false;
                    //return ((MySection)((MyChapter)chapters[kapitola]).sections[sekce]).paragraphs.Add(new MyParagraph(text,aCasoveZnacky));
                    Chapters[kapitola].Sections[sekce].Paragraphs.Add(new MyParagraph(text, aCasoveZnacky));
                    Chapters[kapitola].Sections[sekce].PhoneticParagraphs.Add(new MyParagraph());
                    return Chapters[kapitola].Sections[sekce].Paragraphs.Count - 1;

                }
                else
                {
                    index++;
                    Chapters[kapitola].Sections[sekce].Paragraphs.Insert(index, new MyParagraph(text, aCasoveZnacky));
                    Chapters[kapitola].Sections[sekce].PhoneticParagraphs.Insert(index, new MyParagraph());
                    Ulozeno = false;
                    return index;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Chyba pri vkladani odstavce..." + ex.Message);
                MyLog.LogujChybu(ex);
                return -1;
            }
        }
        [Obsolete]
        public int NovyOdstavec(Int32 kapitola, int sekce, String text, List<MyCasovaZnacka> aCasoveZnacky, TimeSpan begin, TimeSpan end, int index)
        {
            try
            {
                if (index < 0)
                {
                    int i = 0;
                    if (index < -1)
                    {
                        Chapters[kapitola].Sections[sekce].Paragraphs.Insert(0, new MyParagraph(text, aCasoveZnacky, begin, end));
                        Chapters[kapitola].Sections[sekce].PhoneticParagraphs.Insert(0, new MyParagraph("", null, begin, end));
                    }
                    else
                    {
                        Chapters[kapitola].Sections[sekce].Paragraphs.Add(new MyParagraph(text, aCasoveZnacky, begin, end));
                        Chapters[kapitola].Sections[sekce].PhoneticParagraphs.Add(new MyParagraph("", null, begin, end));
                        i = Chapters[kapitola].Sections[sekce].Paragraphs.Count - 1;
                    }
                    Ulozeno = false;
                    return i;
                }
                else
                {
                    index++;
                    //((MySection)((MyChapter)chapters[kapitola]).sections[sekce]).paragraphs.Insert(index, new MyParagraph(text,aCasoveZnacky, begin, end));
                    Chapters[kapitola].Sections[sekce].Paragraphs.Insert(index, new MyParagraph(text, aCasoveZnacky, begin, end));
                    Chapters[kapitola].Sections[sekce].PhoneticParagraphs.Insert(index, new MyParagraph("", null, begin, end));
                    Ulozeno = false;
                    return index;
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return -1;
            }
        }

        /// <summary>
        /// upravi nazev kapitoly nebo sekce
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        [Obsolete]
        public bool UpravElement(MyTag aTag, string text)
        {
            return UpravElement(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec, text);
        }

        /// <summary>
        /// upravi nazev kapitoly nebo sekce
        /// </summary>
        /// <param name="kapitola"></param>
        /// <param name="sekce"></param>
        /// <param name="odstavec"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        [Obsolete]
        public bool UpravElement(int kapitola, int sekce, int odstavec, string text)
        {
            try
            {
                if (odstavec > -1)
                {
                    //sem by nemel prijit, ptz je toto nahrazeno upravenim elementu odstavce
                    Chapters[kapitola].Sections[sekce].Paragraphs[odstavec].UlozTextOdstavce(text, null);
                    Ulozeno = false;
                    return true;
                }
                else if (sekce > -1)
                {
                    Chapters[kapitola].Sections[sekce].name = text;
                    Ulozeno = false;
                    return true;
                }
                else if (kapitola > -1)
                {
                    Chapters[kapitola].name = text;
                    Ulozeno = false;
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        /// <summary>
        /// upravi odstavec - text a casove znacky, zvlast pro normalni odstavec a jeho fonetickou variantu
        /// </summary>
        /// <param name="kapitola"></param>
        /// <param name="sekce"></param>
        /// <param name="odstavec"></param>
        /// <param name="text"></param>
        /// <param name="aCasoveZnacky"></param>
        /// <param name="aTypOdstavce">Cislo udavajici typ odstavce: 0 - normalni, 1 - foneticky</param>
        /// <returns></returns>
        public bool UpravElementOdstavce(MyTag aTag, string text, List<MyCasovaZnacka> aCasoveZnacky)
        {
            try
            {
                if (aTag.JeOdstavec)
                {
                    if (aTag.tTypElementu == MyEnumTypElementu.normalni)
                    {
                        Chapters[aTag.tKapitola].Sections[aTag.tSekce].Paragraphs[aTag.tOdstavec].UlozTextOdstavce(text, aCasoveZnacky, aTag.tTypElementu);
                    }
                    else if (aTag.tTypElementu == MyEnumTypElementu.foneticky)
                    {
                        Chapters[aTag.tKapitola].Sections[aTag.tSekce].PhoneticParagraphs[aTag.tOdstavec].UlozTextOdstavce(text, aCasoveZnacky, aTag.tTypElementu);
                    }
                    Ulozeno = false;
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        /// <summary>
        /// edituje cas po ktery element trva, pro vsechny verze odstavcu jsou tyto casy shodne
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="timeStart"></param>
        /// <param name="timeStop"></param>
        /// <returns></returns>
        public bool UpravCasElementu(MyTag aTag, TimeSpan timeStart, TimeSpan timeStop)
        {
            try
            {
                int kapitola = aTag.tKapitola;
                int sekce = aTag.tSekce;
                int odstavec = aTag.tOdstavec;
                if (odstavec > -1)
                {
                    if (timeStart > new TimeSpan(-2))
                    {
                        Chapters[kapitola].Sections[sekce].Paragraphs[odstavec].Begin = timeStart;
                        Chapters[kapitola].Sections[sekce].PhoneticParagraphs[odstavec].Begin = timeStart;
                    }
                    if (timeStop > new TimeSpan(-2))
                    {
                        Chapters[kapitola].Sections[sekce].Paragraphs[odstavec].End = timeStop;
                        Chapters[kapitola].Sections[sekce].PhoneticParagraphs[odstavec].End = timeStop;
                    }
                    Ulozeno = false;
                    return true;
                }
                else if (sekce > -1)
                {
                    if (timeStart > new TimeSpan(-2)) ((MySection)((MyChapter)Chapters[kapitola]).Sections[sekce]).Begin = timeStart;
                    if (timeStop > new TimeSpan(-2)) ((MySection)((MyChapter)Chapters[kapitola]).Sections[sekce]).End = timeStop;
                    Ulozeno = false;
                    return true;
                }
                else if (kapitola > -1)
                {
                    if (timeStart >new TimeSpan( -2)) ((MyChapter)Chapters[kapitola]).Begin = timeStart;
                    if (timeStop > new TimeSpan(-2)) ((MyChapter)Chapters[kapitola]).End = timeStop;
                    Ulozeno = false;
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        /// <summary>
        /// podle tagu vraci cas elementu...begin
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public TimeSpan VratCasElementuPocatek(MyTag aTag)
        {
            if (aTag.tKapitola >= 0)
            {
                if (aTag.tKapitola >= Chapters.Count)
                    return new TimeSpan(-1);

                MyChapter chapter = Chapters[aTag.tKapitola];
                if (aTag.tSekce >= 0)
                {
                    if (aTag.tSekce >= chapter.Sections.Count)
                        return new TimeSpan(-1);

                    MySection section = chapter.Sections[aTag.tSekce];
                    if (aTag.tOdstavec >= 0)
                    {
                        if (aTag.tOdstavec >= section.Paragraphs.Count)
                            return new TimeSpan(-1);

                        return section.Paragraphs[aTag.tOdstavec].Begin;
                    }
                    return section.Begin;
                }
                return chapter.Begin;
            }

            return new TimeSpan(-1);
        }


        //podle tagu vraci cas elementu...end
        public TimeSpan VratCasElementuKonec(MyTag aTag)
        {
            if (aTag.tKapitola >= 0)
            {
                if (aTag.tKapitola >= Chapters.Count)
                    return new TimeSpan(-1);

                MyChapter chapter = Chapters[aTag.tKapitola];
                if (aTag.tSekce >= 0)
                {
                    if (aTag.tSekce >= chapter.Sections.Count)
                        return new TimeSpan(-1);

                    MySection section = chapter.Sections[aTag.tSekce];
                    if (aTag.tOdstavec >= 0)
                    {
                        if (aTag.tOdstavec >= section.Paragraphs.Count)
                            return new TimeSpan(-1);

                        return section.Paragraphs[aTag.tOdstavec].End;
                    }
                    return section.End;
                }
                return chapter.End;
            }

            return new TimeSpan(-1);

        }

        /// <summary>
        /// oznaci podle tagu data pro trenovani - umi kapitoly, sekce a odstavec - upravi i foneticky
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="aStav"></param>
        /// <returns></returns>
        public bool OznacTrenovaciData(MyTag aTag, bool aStav)
        {
            try
            {
                if (aTag == null) return false;
                if (aTag.JeOdstavec)
                {
                    MyParagraph pP = VratOdstavec(aTag);
                    MyParagraph pP2 = VratOdstavec(new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec, MyEnumTypElementu.foneticky, null));
                    if (pP == null) return false;
                    pP.trainingElement = aStav;
                    if (pP2 != null) pP2.trainingElement = aStav;

                }
                else if (aTag.JeSekce) //oznaceni cele sekce
                {
                    MySection pS = VratSekci(aTag);
                    if (pS == null) return false;
                    for (int i = 0; i < pS.Paragraphs.Count; i++)
                    {
                        pS.Paragraphs[i].trainingElement = aStav;
                        pS.PhoneticParagraphs[i].trainingElement = aStav;
                    }
                }
                else if (aTag.JeKapitola)
                {
                    MyChapter pCh = VratKapitolu(aTag);
                    if (pCh == null) return false;
                    for (int j = 0; j < pCh.Sections.Count; j++)
                    {
                        MySection pS = pCh.Sections[j];
                        for (int i = 0; i < pS.Paragraphs.Count; i++)
                        {
                            pS.Paragraphs[i].trainingElement = aStav;
                            pS.PhoneticParagraphs[i].trainingElement = aStav;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

        /// <summary>
        /// vrati vsechny vyhovujici elementy casu
        /// </summary>
        /// <param name="aPoziceKurzoru"></param>
        /// <returns></returns>
        public List<TranscriptionElement> VratElementDanehoCasu(TimeSpan PoziceKurzoru)
        {
            List<TranscriptionElement> pRet = new List<TranscriptionElement>();

            foreach (var tr in this)
            {
                if (tr.Begin <= PoziceKurzoru && tr.End >= PoziceKurzoru && tr.IsParagraph)
                    pRet.Add(tr);
            }

            return pRet;
        }


        /// <summary>
        /// vrati vsechny vyhovujici elementy casu
        /// </summary>
        /// <param name="aPoziceKurzoruMS"></param>
        /// <returns></returns>
        [Obsolete]
        public List<MyTag> VratElementDanehoCasu(TimeSpan aPoziceKurzoru, MyTag aVychoziTag)
        {
            List<MyTag> pRet = new List<MyTag>();
            try
            {
                if (aVychoziTag == null) aVychoziTag = new MyTag(0, 0, 0);
                if (!aVychoziTag.JeSekce && !aVychoziTag.JeOdstavec) aVychoziTag = new MyTag(aVychoziTag.tKapitola, 0, 0);
                if (!aVychoziTag.JeOdstavec) aVychoziTag = new MyTag(aVychoziTag.tKapitola, aVychoziTag.tSekce, 0);

                int pi = aVychoziTag.tKapitola;
                int pj = aVychoziTag.tSekce;
                int pk = aVychoziTag.tOdstavec;
                MyParagraph pP1 = VratOdstavec(aVychoziTag);
                if (pP1 != null && pP1.Delka >= TimeSpan.Zero)
                {
                    if (aPoziceKurzoru >= pP1.Begin && aPoziceKurzoru < pP1.End)
                    {
                        pRet.Add(aVychoziTag);
                        return pRet;
                    }
                }

                for (int i = pi; i < this.Chapters.Count; i++)
                {
                    for (int j = pj; j < this.Chapters[i].Sections.Count; j++)
                    {
                        for (int k = pk; k < this.Chapters[i].Sections[j].Paragraphs.Count; k++)
                        {
                            MyParagraph pP = this.Chapters[i].Sections[j].Paragraphs[k];
                            if (pP.Delka >= TimeSpan.Zero)
                            {
                                if (aPoziceKurzoru > pP.Begin && aPoziceKurzoru < pP.End)
                                {
                                    pRet.Add(new MyTag(i, j, k));

                                }
                                else if (aPoziceKurzoru == pP.Begin || aPoziceKurzoru == pP.End)
                                {
                                    pRet.Add(new MyTag(i, j, k));

                                }
                                else if (aPoziceKurzoru < pP.Begin)
                                {
                                    return pRet;
                                }
                            }
                        }
                        pk = 0;

                    }

                }
                return pRet;
            }
            catch
            {
                return pRet;
            }

        }




        public MyTag VratElementKonciciPred(TimeSpan aPoziceKurzoru)
        {

                 MyTag aVychoziTag = new MyTag(0, 0, 0);


                int pi = aVychoziTag.tKapitola;
                int pj = aVychoziTag.tSekce;
                int pk = aVychoziTag.tOdstavec;
                MyParagraph nejlepsi = this[aVychoziTag];
                MyTag nejlepsitag = new MyTag();

                for (int i = pi; i < this.Chapters.Count; i++)
                {
                    for (int j = pj; j < this.Chapters[i].Sections.Count; j++)
                    {
                        for (int k = pk; k < this.Chapters[i].Sections[j].Paragraphs.Count; k++)
                        {
                            MyParagraph pP = this.Chapters[i].Sections[j].Paragraphs[k];
                            if (pP.Delka >= TimeSpan.Zero)
                            {
                                if (aPoziceKurzoru > pP.End && pP.End > nejlepsi.End )
                                {
                                    nejlepsi = pP;
                                    nejlepsitag = new MyTag(i, j, k);

                                }

                            }
                            else if (pP.Begin > aPoziceKurzoru)
                            {
                                return nejlepsitag;
                            }
                        }
                        
                        pk = 0;

                    }

                }
                return nejlepsitag;

        }

        public MyTag VratElementZacinajiciPred(TimeSpan aPoziceKurzoruMS)
        {

            MyTag aVychoziTag = new MyTag(0, 0, 0);


            int pi = aVychoziTag.tKapitola;
            int pj = aVychoziTag.tSekce;
            int pk = aVychoziTag.tOdstavec;
            MyParagraph nejlepsi = this[aVychoziTag];
            MyTag nejlepsitag = new MyTag(0,0,0);

            for (int i = pi; i < this.Chapters.Count; i++)
            {
                for (int j = pj; j < this.Chapters[i].Sections.Count; j++)
                {
                    for (int k = pk; k < this.Chapters[i].Sections[j].Paragraphs.Count; k++)
                    {
                        MyParagraph pP = this.Chapters[i].Sections[j].Paragraphs[k];
                        if (pP.Delka >= TimeSpan.Zero)
                        {
                            if (aPoziceKurzoruMS > pP.Begin && pP.Begin > nejlepsi.Begin)
                            {
                                nejlepsi = pP;
                                nejlepsitag = new MyTag(i, j, k);

                            }

                        }
                        else if (pP.Begin > aPoziceKurzoruMS)
                        {
                            return nejlepsitag;
                        }
                    }

                    pk = 0;

                }

            }
            return nejlepsitag;

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
                                        Chapters[k].Sections[l].PhoneticParagraphs[m].speakerID = new MySpeaker().ID;

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
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
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

        /// <summary>
        /// zadani speakera do datove struktury
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="indexSpeakera"></param>
        /// <returns></returns>
        public bool ZadejSpeakera(MyTag aTag, int aIDSpeakera)
        {


            try
            {
                MySpeaker aSpeaker = new MySpeaker();

                aSpeaker = this.SeznamMluvcich.VratSpeakera(aIDSpeakera);


                if (aTag.tOdstavec > -1)
                {
                    //nastavi mluvciho pro odstavec podle tagu
                    Chapters[aTag.tKapitola].Sections[aTag.tSekce].Paragraphs[aTag.tOdstavec].speakerID = aSpeaker.ID;
                    Chapters[aTag.tKapitola].Sections[aTag.tSekce].PhoneticParagraphs[aTag.tOdstavec].speakerID = aSpeaker.ID;
                    Ulozeno = false;
                    return true;
                }
                else if (aTag.tSekce > -1)
                {
                    //nastavi mluvciho pro celou sekci podle tagu
                    ((MySection)((MyChapter)Chapters[aTag.tKapitola]).Sections[aTag.tSekce]).speaker = aSpeaker.ID;

                    if (((MySection)((MyChapter)Chapters[aTag.tKapitola]).Sections[aTag.tSekce]).Paragraphs.Count > 0)
                    {
                        for (int i = 0; i < ((MySection)((MyChapter)Chapters[aTag.tKapitola]).Sections[aTag.tSekce]).Paragraphs.Count; i++)
                        {
                            ((MyParagraph)((MySection)((MyChapter)Chapters[aTag.tKapitola]).Sections[aTag.tSekce]).Paragraphs[i]).speakerID = aSpeaker.ID;
                        }
                        Ulozeno = false;
                        return true;
                    }
                    else return false;


                }
                else if (aTag.tKapitola > -1)
                {
                    //u kapitoly se speaktrer nenastavuje
                    return false;
                }
                else
                {

                    return false;
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }



        /// <summary>
        /// vraci speakera z datove struktury podle tagu
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public MySpeaker VratSpeakera(MyTag aTag)
        {
            try
            {

                if (aTag.tOdstavec > -1)
                {

                    int ms = Chapters[aTag.tKapitola].Sections[aTag.tSekce].Paragraphs[aTag.tOdstavec].speakerID;
                    return this.SeznamMluvcich.VratSpeakera(ms);

                }
                else if (aTag.tSekce > -1)
                {
                    return new MySpeaker();
                }
                else if (aTag.tKapitola > -1)
                {
                    return new MySpeaker();
                }
                else
                {
                    return new MySpeaker();
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return new MySpeaker();
            }

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
        /// vrati odstavec podle typu
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="aTypOdstavce"></param>
        /// <returns></returns>
        private MyParagraph VratOdstavec(MyTag aTag)
        {
            if (aTag == null || aTag.tKapitola < 0 || aTag.tSekce < 0 || aTag.tOdstavec < 0) return null;

            if (aTag.tKapitola < Chapters.Count)
            {
                MyChapter chapter = Chapters[aTag.tKapitola];
                if (aTag.tSekce < chapter.Sections.Count)
                {
                    MySection section = chapter.Sections[aTag.tSekce];

                    if (aTag.tOdstavec < section.Paragraphs.Count)
                    {
                        if (aTag.tTypElementu == MyEnumTypElementu.normalni)
                        {
                            return section.Paragraphs[aTag.tOdstavec];
                        }
                        else if (aTag.tTypElementu == MyEnumTypElementu.foneticky)
                        {
                            return section.PhoneticParagraphs[aTag.tOdstavec];
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// vrati nasledujici odstavec, pokud existuje - i z dalsi sekce, nikoliv kapitoly!! jinak null
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public MyTag VratOdstavecNasledujiciTag(MyTag aTag)
        {
            try
            {
                if (aTag.tKapitola < 0 || aTag.tSekce < 0 || aTag.tOdstavec < 0) return null;
                int pPocetSekci = Chapters[aTag.tKapitola].Sections.Count;
                int pPocetOdstavcu = Chapters[aTag.tKapitola].Sections[aTag.tSekce].Paragraphs.Count;
                if (pPocetOdstavcu > aTag.tOdstavec + 1)
                {
                    //return Chapters[aTag.tKapitola].Sections[aTag.tSekce].Paragraphs[aTag.tOdstavec + 1];
                    return new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec + 1);
                }
                else
                {
                    if (pPocetSekci > aTag.tSekce + 1)
                    {
                        if (Chapters[aTag.tKapitola].Sections[aTag.tSekce + 1].hasParagraph)
                        {
                            //return Chapters[aTag.tKapitola].Sections[aTag.tSekce + 1].Paragraphs[0];
                            return new MyTag(aTag.tKapitola, aTag.tSekce + 1, 0);
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// vrati predchozi odstavec, pokud existuje - i z predchozi sekce, nikoliv kapitoly!! jinak null
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public MyTag VratOdstavecPredchoziTag(MyTag aTag)
        {
            try
            {
                if (aTag.tKapitola < 0 || aTag.tSekce < 0 || aTag.tOdstavec < 0) return null;
                int pPocetSekci = Chapters[aTag.tKapitola].Sections.Count;
                int pPocetOdstavcu = Chapters[aTag.tKapitola].Sections[aTag.tSekce].Paragraphs.Count;
                if (aTag.tOdstavec > 0)
                {
                    //return Chapters[aTag.tKapitola].Sections[aTag.tSekce].Paragraphs[aTag.tOdstavec - 1];
                    return new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec - 1);
                }
                else
                {
                    if (aTag.tSekce > 0)
                    {
                        if (Chapters[aTag.tKapitola].Sections[aTag.tSekce - 1].hasParagraph)
                        {
                            //return Chapters[aTag.tKapitola].Sections[aTag.tSekce - 1].Paragraphs[Chapters[aTag.tKapitola].Sections[aTag.tSekce - 1].Paragraphs.Count - 1];
                            return new MyTag(aTag.tKapitola, aTag.tSekce - 1, Chapters[aTag.tKapitola].Sections[aTag.tSekce - 1].Paragraphs.Count - 1);
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Vraci sekci podle tagu, pokud neexistuje, vraci null
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public MySection VratSekci(MyTag aTag)
        {
            try
            {
                if (aTag.tKapitola < 0 || aTag.tSekce < 0 || aTag.tOdstavec >= 0) return null;
                return ((MySection)((MyChapter)Chapters[aTag.tKapitola]).Sections[aTag.tSekce]);

            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Vraci kapitolu podle tagu, pokud neexistuje, vraci null
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public MyChapter VratKapitolu(MyTag aTag)
        {
            try
            {
                if (aTag.tKapitola < 0 || aTag.tSekce > 0 || aTag.tOdstavec >= 0) return null;
                return ((MyChapter)Chapters[aTag.tKapitola]);

            }
            catch
            {
                return null;
            }
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
                //odstraneni prazdnych fonetickych prepisu, aby se zbytecne nezapisovaly
                for (int i = 0; i < pCopy.Chapters.Count; i++)
                {
                    for (int j = 0; j < pCopy.Chapters[i].Sections.Count; j++)
                    {
                        bool pPrazdne = true;
                        for (int k = 0; k < pCopy.Chapters[i].Sections[j].Paragraphs.Count; k++)
                        {
                            MyParagraph pP = pCopy.VratOdstavec(new MyTag(i, j, k, MyEnumTypElementu.foneticky, null));
                            if (pP.Text != null && pP.Text != "")
                            {
                                pPrazdne = false;
                                break;
                            }
                        }
                        if (pPrazdne) pCopy.Chapters[i].Sections[j].PhoneticParagraphs.Clear();
                    }
                }


               // TextWriter writer = new StreamWriter(datastream);
               // XmlSerializer serializer = new XmlSerializer(typeof(MySubtitlesData));

                //XmlTextWriter writer = new XmlTextWriter(jmenoSouboru, Encoding.UTF8);

               // serializer.Serialize(writer, pCopy);
               // writer.Close();

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
                        foreach (MyParagraph p in s.PhoneticParagraphs)
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
                MyLog.LogujChybu(ex);
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
                            MyParagraph p = new MyParagraph() { IsPhonetic = true};
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
                                    ph.TextPrepisovany = reader.ReadElementString();
                                }

                                p.Phrases.Add(ph);
                                reader.ReadEndElement();//Phrase;
                                //reader.Read();//Phrase | phhrases end element
                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.speakerID = XmlConvert.ToInt32(reader.ReadElementString());

                            reader.ReadEndElement();//paragraph
                            s.PhoneticParagraphs.Add(p);
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

                //pokud nebyly v souboru ulozeny foneticke prepisy odstavcu, jsou automaticky vytvoreny podle struktury
                for (int i = 0; i < data.Chapters.Count; i++)
                {
                    for (int j = 0; j < data.Chapters[i].Sections.Count; j++)
                    {
                        if (data.Chapters[i].Sections[j].Paragraphs.Count != data.Chapters[i].Sections[j].PhoneticParagraphs.Count)
                        {
                            data.Chapters[i].Sections[j].PhoneticParagraphs.Clear();
                            for (int k = 0; k < data.Chapters[i].Sections[j].Paragraphs.Count; k++)
                            {
                                MyParagraph pP = data.VratOdstavec(new MyTag(i, j, k));
                                data.Chapters[i].Sections[j].PhoneticParagraphs.Add(new MyParagraph("", null, pP.Begin, pP.End));
                                data.ZadejSpeakera(new MyTag(i, j, k), pP.speakerID);
                            }
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri derializaci souboru: " + ex.Message);
                MyLog.LogujChybu(ex);
                return null;
            }

                

        }

        public MyTag UpravCasZobraz(MyTag aTag, TimeSpan aBegin, TimeSpan aEnd)
        {
            return UpravCasZobraz(aTag, aBegin, aEnd,false);
        }

        /// <summary>
        /// upravi a zobrazi cas u textboxu....begin a end.. pokud -2 tak se nemeni hodnoty casu..., -1 znamena vynulovani hodnot casu pocatku nebo konce
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="aBegin"></param>
        /// <param name="aEnd"></param>
        /// <returns></returns>
        public MyTag UpravCasZobraz(MyTag aTag, TimeSpan aBegin, TimeSpan aEnd, bool aIgnorovatPrekryv)
        {

            MyTag pTagPredchozi = new MyTag(-1, -1, -1);     //predchozi element
            MyTag newTag2 = new MyTag(-1, -1, -1);  //nasledujici element
            MyParagraph pParPredchozi = null;
            MyParagraph pParAktualni = VratOdstavec(aTag);
            MyParagraph pParNasledujici = null;


            if (aTag.tOdstavec > 0)
            {
                pTagPredchozi = new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec - 1);
                pParPredchozi = VratOdstavec(pTagPredchozi);

            }
            if (aTag.tOdstavec > -1)
            {
                newTag2 = new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec + 1);
                pParNasledujici = VratOdstavec(newTag2);
            }

            TimeSpan pKonecPredchoziho = VratCasElementuKonec(pTagPredchozi);
            TimeSpan pZacatekSoucasneho = VratCasElementuPocatek(aTag);
            TimeSpan pKonecSoucasneho = VratCasElementuKonec(aTag);
            TimeSpan pZacatekNasledujiciho = VratCasElementuPocatek(newTag2);


            if (VratCasElementuPocatek(pTagPredchozi) > new TimeSpan(-1) && aBegin > new TimeSpan(-1))
            {
                TimeSpan pCasElementuKonec = VratCasElementuKonec(aTag);

                if (pCasElementuKonec >= TimeSpan.Zero && pCasElementuKonec < aBegin && aEnd < aBegin)
                {
                    MessageBox.Show("Nelze nastavit počáteční čas bloku větší než jeho konec. ", "Varování", MessageBoxButton.OK);
                    return null;
                }

                if (VratCasElementuPocatek(pTagPredchozi) <= aBegin) //dopsano ==
                {
                    if (VratCasElementuKonec(pTagPredchozi) > aBegin)
                    {
                        if (VratSpeakera(aTag).FullName == VratSpeakera(pTagPredchozi).FullName && !aIgnorovatPrekryv)
                        {
                            MessageBox.Show("Nelze nastavit počáteční čas bloku nižší než konec předchozího pro stejného mluvčího ", "Varování", MessageBoxButton.OK);
                            return null;
                        }
                        else
                        {
                            MessageBoxResult mbr = MessageBoxResult.Yes;
                            bool pZobrazitHlasku = pKonecPredchoziho <= pZacatekSoucasneho;
                         
                            //TODO: proc jsou z tychle funkce volany messageboxy
                            if (!aIgnorovatPrekryv && pZobrazitHlasku) mbr = MessageBox.Show("Mluvčí se bude překrývat s předchozím, chcete toto povolit?", "Varování", MessageBoxButton.YesNoCancel);
                            if (mbr != MessageBoxResult.Yes)
                            {
                                aBegin = pKonecPredchoziho;
                            }

                        }
                    }
                }
                else
                {
                    MessageBox.Show("Nelze nastavit počáteční čas bloku nižší než začátek předchozího. ", "Varování", MessageBoxButton.OK);
                    return aTag;
                }

            }


            if (VratCasElementuKonec(newTag2) > new TimeSpan(-1) && aEnd > new TimeSpan(-1))
            {
                if (VratCasElementuPocatek(aTag) > aEnd)
                {
                    MessageBox.Show("Nelze nastavit koncový čas bloku menší než jeho počátek. ", "Varování", MessageBoxButton.OK);
                    return null;
                }
                else
                {
                    if (VratCasElementuPocatek(newTag2) < aEnd)
                    {
                        if (VratSpeakera(aTag).FullName == VratSpeakera(newTag2).FullName && !aIgnorovatPrekryv)
                        {
                            MessageBox.Show("Nelze nastavit koncový čas bloku vyšší než počátek následujícího pro stejného mluvčího ", "Varování", MessageBoxButton.OK);
                            return aTag;
                        }
                        else
                        {
                            MessageBoxResult mbr = MessageBoxResult.Yes;
                            bool pZobrazitHlasku = pKonecSoucasneho <= pZacatekNasledujiciho;
                            if (!aIgnorovatPrekryv && pZobrazitHlasku) mbr = MessageBox.Show("Mluvčí se bude překrývat s následujícím, chcete toto povolit?", "Varování", MessageBoxButton.YesNoCancel);
                            if (mbr != MessageBoxResult.Yes)
                            {
                                aEnd = pZacatekNasledujiciho;
                            }
                        }
                    }
                }
            }


            UpravCasElementu(aTag, aBegin, aEnd);
            return aTag;
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

        public void Insert(int index, TranscriptionElement item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public TranscriptionElement this[int index]
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

        public void Add(TranscriptionElement item)
        {
            throw new NotSupportedException();
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

        public bool Remove(TranscriptionElement item)
        {
            throw new NotSupportedException();
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
