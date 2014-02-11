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


namespace NanoTrans
{
    //mluvci

    [XmlType("Speaker")]
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


    //nejmensi textovy usek - muze byt veta, vice slov nebo samotne slovo
    [XmlType("Phrase")]
    public class MyPhrase
    {
        //public MyWord words2;  //slova tvorici vetu
        public string Text;    //slovo/a ktere obsahuji i mezery na konci
        /// <summary>
        /// text ze ktereho vznikl foneticky prepis
        /// </summary>
        public string TextPrepisovany;

        //[XmlAttribute]
        [XmlIgnore]
        public int speakerID;  //index mluvciho v seznamu dole
        [XmlAttribute]
        public long begin;     //zacatek vety v ms

        //[XmlIgnore]
        [XmlAttribute]
        public long end;       //konec vety v ms - zatim je ignorovan

        public MyPhrase()
        {
        }
        public MyPhrase(long aBegin, long aEnd, string aWords, int aSpeakerIndex)
        {
            this.begin = aBegin;
            this.end = aEnd;
            this.Text = aWords;
            this.TextPrepisovany = null;
            this.speakerID = aSpeakerIndex;
        }

        public MyPhrase(long aBegin, long aEnd, string aWords, int aSpeakerIndex, MyEnumTypElementu aElementType)
        {
            this.begin = aBegin;
            this.end = aEnd;
            this.Text = aWords;
            this.TextPrepisovany = null;
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
            this.speakerID = aSpeakerIndex;
        }

    }


    /// <summary>
    /// odstavec slozeny z mensich textovych useku (vet, nebo i jednotlivych slov zvlast)
    /// </summary>
    [XmlType("Paragraph")]
    [XmlInclude(typeof(MyPhrase))]
    public class MyParagraph
    {
        [XmlAttribute]
        public String name;

        //public ArrayList phrases = new ArrayList(); //nejmensi textovy usek
        public List<MyPhrase> Phrases = new List<MyPhrase>(); //nejmensi textovy usek
        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - jedna se o text ze vsech podrazenych textovych jednotek (Phrases)
        /// </summary>
        [XmlIgnore]
        public string Text
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
        }

        [XmlIgnore]
        public MyEnumParagraphAttributes DataAttributes = MyEnumParagraphAttributes.None;


        [XmlAttribute]
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
        [XmlAttribute]
        public long begin;     //zacatek v ms
        [XmlAttribute]
        public long end;       //konec v ms
        /// <summary>
        /// informace zda je dany element zahrnut pro trenovani dat 
        /// </summary>
        [XmlAttribute]
        public bool trainingElement;

        /// <summary>
        /// vraci delku odstavce v MS mezi begin a end, pokud neni nektera hodnota nezadana -1
        /// </summary>
        [XmlIgnore]
        public long DelkaMS
        {
            get
            {
                if (begin == -1 || end == -1) return 0;
                return end - begin;
            }
        }



        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MyParagraph(MyParagraph aKopie)
        {
            this.begin = aKopie.begin;
            this.end = aKopie.end;
            this.trainingElement = aKopie.trainingElement;
            this.name = aKopie.name;
            this.DataAttributes = aKopie.DataAttributes;
            if (aKopie.Phrases != null)
            {
                this.Phrases = new List<MyPhrase>();
                for (int i = 0; i < aKopie.Phrases.Count; i++)
                {
                    this.Phrases.Add(aKopie.Phrases[i]);
                }
            }
            this.speakerID = aKopie.speakerID;
        }

        public MyParagraph()
        {
            this.begin = -1;
            this.end = -1;
            this.trainingElement = false;
            this.speakerID = -1;

        }
        public MyParagraph(String aText, List<MyCasovaZnacka> aCasoveZnacky)
        {
            //this.text = aText;
            this.begin = -1;
            this.end = -1;
            this.trainingElement = false;
            this.speakerID = -1;
            UlozTextOdstavce(aText, aCasoveZnacky);
        }
        public MyParagraph(String aText, List<MyCasovaZnacka> aCasoveZnacky, long aBegin, long aEnd)
        {
            //this.text = aText;
            this.begin = aBegin;
            this.end = aEnd;
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
                        pPhrases.Add(new MyPhrase(-1, -1, aText, this.speakerID, aTypOdstavce));
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
                                    pPhrases.Add(new MyPhrase(pZnacky[i].Time, -1, pText1, this.speakerID, aTypOdstavce));
                                }
                                else
                                {
                                    //jestli existuje jen 1 znacka,tak musi byt ulozen pocatek odstavce,ktery muze byt bez znacky
                                    if (i == 0 && pZnacky[i].Index2 > 0)
                                    {
                                        pText1 = aText.Substring(0, pZnacky[i].Index2);
                                        if (pText1 != null && pText1 != "")
                                        {
                                            pPhrases.Add(new MyPhrase(-1, -1, pText1, this.speakerID, aTypOdstavce));
                                        }
                                    }

                                    if (pZnacky[i + 1].Index2 - pZnacky[i].Index2 >= 0) //ohlidani zda nejsou znacky posunuty
                                    {
                                        pText1 = aText.Substring(pZnacky[i].Index2, pZnacky[i + 1].Index2 - pZnacky[i].Index2);
                                        pPhrases.Add(new MyPhrase(pZnacky[i].Time, pZnacky[i + 1].Time, pText1, this.speakerID, aTypOdstavce));
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
                                        Phrases.Add(new MyPhrase(-1, -1, pText1, this.speakerID, aTypOdstavce));
                                    }
                                }
                                pText1 = aText.Substring(pZnacky[i].Index2, aText.Length - pZnacky[i].Index2);
                                pPhrases.Add(new MyPhrase(pZnacky[i].Time, -1, pText1, this.speakerID, aTypOdstavce));

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
                Phrases = pPhrases;
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
        [XmlIgnore]
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
                            if (((MyPhrase)Phrases[i]).begin >= 0)
                            {
                                pZnacky.Add(new MyCasovaZnacka(((MyPhrase)Phrases[i]).begin, pIndexTextu1, pIndexTextu2));
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
    [XmlType("Section")]
    [XmlInclude(typeof(MyParagraph))]
    public class MySection
    {
        [XmlAttribute]
        public String name;
        //public MyParagraph paragraphs2;
        //public ArrayList paragraphs = new ArrayList();
        public List<MyParagraph> Paragraphs = new List<MyParagraph>();
        /// <summary>
        /// foneticka verze obycejneho odstavce
        /// </summary>
        public List<MyParagraph> PhoneticParagraphs = new List<MyParagraph>();
        [XmlAttribute]
        public long begin;     //zacatek v ms
        [XmlAttribute]
        public long end;       //konec v ms

        public int speaker;

        /// <summary>
        /// Informace, jestli ma tato sekce odstavec nebo vice
        /// </summary>
        [XmlIgnore]
        public bool hasParagraph
        {
            get { if (this.Paragraphs != null && this.Paragraphs.Count > 0) return true; else return false; }
        }

        /// <summary>
        /// Informace, jestli ma tato sekce foneticky neprazdny odstavec nebo vice
        /// </summary>
        [XmlIgnore]
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
        [XmlIgnore]
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
        public MySection(MySection aKopie)
        {
            this.begin = aKopie.begin;
            this.end = aKopie.end;
            this.name = aKopie.name;
            if (aKopie.Paragraphs != null)
            {
                this.Paragraphs = new List<MyParagraph>();
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
            this.begin = -1;
            this.end = -1;
        }
        public MySection(String aName)
        {
            this.name = aName;
            this.begin = -1;
            this.end = -1;
        }
        public MySection(String aName, long aBegin, long aEnd)
        {
            this.name = aName;
            this.begin = aBegin;
            this.end = aEnd;
        }
    }
    //kapitola

    [XmlType("Chapter")]
    [XmlInclude(typeof(MySection))]
    public class MyChapter
    {
        [XmlAttribute]
        public String name;
        /// <summary>
        /// typ poradu - kapitoly 
        /// </summary>
        [XmlAttribute]
        public String type;


        public List<MySection> Sections = new List<MySection>();
        [XmlAttribute]
        public long begin;     //zacatek v ms
        [XmlAttribute]
        public long end;       //konec v ms

        /// <summary>
        /// Informace, jestli ma tato kapitola nejakou sekci
        /// </summary>
        [XmlIgnore]
        public bool hasSection
        {
            get { if (this.Sections != null && this.Sections.Count > 0) return true; else return false; }
        }

        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public MyChapter(MyChapter aKopie)
        {
            this.begin = aKopie.begin;
            this.end = aKopie.end;
            this.name = aKopie.name;
            if (aKopie.Sections != null)
            {
                this.Sections = new List<MySection>();
                for (int i = 0; i < aKopie.Sections.Count; i++)
                {
                    this.Sections.Add(new MySection(aKopie.Sections[i]));
                }
            }
        }

        public MyChapter()
        {
            this.begin = -1;
            this.end = -1;
        }
        public MyChapter(String aName)
        {
            this.name = aName;
            this.begin = -1;
            this.end = -1;
        }
        public MyChapter(String aName, long aBegin, long aEnd)
        {
            this.name = aName;
            this.begin = aBegin;
            this.end = aEnd;
        }
    }


    //hlavni trida s titulky a se vsemi potrebnymi metodami pro serializaci

    [XmlType("Transcription")]
    [XmlInclude(typeof(MyChapter))]
    public class MySubtitlesData
    {
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
        public int NovaKapitola(int aIndexNoveKapitoly, string nazev, Int32 begin, Int32 end)
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


        public int NovaSekce(int kapitola, string nazev, int index, long begin, long end)
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
        public int NovyOdstavec(Int32 kapitola, int sekce, String text, List<MyCasovaZnacka> aCasoveZnacky, long begin, long end, int index)
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
        public bool UpravCasElementu(MyTag aTag, long timeStart, long timeStop)
        {
            try
            {
                int kapitola = aTag.tKapitola;
                int sekce = aTag.tSekce;
                int odstavec = aTag.tOdstavec;
                if (odstavec > -1)
                {
                    if (timeStart > -2)
                    {
                        Chapters[kapitola].Sections[sekce].Paragraphs[odstavec].begin = timeStart;
                        Chapters[kapitola].Sections[sekce].PhoneticParagraphs[odstavec].begin = timeStart;
                    }
                    if (timeStop > -2)
                    {
                        Chapters[kapitola].Sections[sekce].Paragraphs[odstavec].end = timeStop;
                        Chapters[kapitola].Sections[sekce].PhoneticParagraphs[odstavec].end = timeStop;
                    }
                    Ulozeno = false;
                    return true;
                }
                else if (sekce > -1)
                {
                    if (timeStart > -2) ((MySection)((MyChapter)Chapters[kapitola]).Sections[sekce]).begin = timeStart;
                    if (timeStop > -2) ((MySection)((MyChapter)Chapters[kapitola]).Sections[sekce]).end = timeStop;
                    Ulozeno = false;
                    return true;
                }
                else if (kapitola > -1)
                {
                    if (timeStart > -2) ((MyChapter)Chapters[kapitola]).begin = timeStart;
                    if (timeStop > -2) ((MyChapter)Chapters[kapitola]).end = timeStop;
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
        public long VratCasElementuPocatek(MyTag aTag)
        {
            if (aTag.tKapitola >= 0)
            {
                if (aTag.tKapitola >= Chapters.Count)
                    return -1;

                MyChapter chapter = Chapters[aTag.tKapitola];
                if (aTag.tSekce >= 0)
                {
                    if (aTag.tSekce >= chapter.Sections.Count)
                        return -1;

                    MySection section = chapter.Sections[aTag.tSekce];
                    if (aTag.tOdstavec >= 0)
                    {
                        if (aTag.tOdstavec >= section.Paragraphs.Count)
                            return -1;

                        return section.Paragraphs[aTag.tOdstavec].begin;
                    }
                    return section.begin;
                }
                return chapter.begin;
            }

            return -1;
        }


        //podle tagu vraci cas elementu...end
        public long VratCasElementuKonec(MyTag aTag)
        {
            if (aTag.tKapitola >= 0)
            {
                if (aTag.tKapitola >= Chapters.Count)
                    return -1;

                MyChapter chapter = Chapters[aTag.tKapitola];
                if (aTag.tSekce >= 0)
                {
                    if (aTag.tSekce >= chapter.Sections.Count)
                        return -1;

                    MySection section = chapter.Sections[aTag.tSekce];
                    if (aTag.tOdstavec >= 0)
                    {
                        if (aTag.tOdstavec >= section.Paragraphs.Count)
                            return -1;

                        return section.Paragraphs[aTag.tOdstavec].end;
                    }
                    return section.end;
                }
                return chapter.end;
            }

            return -1;

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
        /// <param name="aPoziceKurzoruMS"></param>
        /// <returns></returns>
        public List<MyTag> VratElementDanehoCasu(long aPoziceKurzoruMS, MyTag aVychoziTag)
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
                if (pP1 != null && pP1.DelkaMS >= 0)
                {
                    if (aPoziceKurzoruMS >= pP1.begin && aPoziceKurzoruMS < pP1.end)
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
                            if (pP.DelkaMS >= 0)
                            {
                                if (aPoziceKurzoruMS > pP.begin && aPoziceKurzoruMS < pP.end)
                                {
                                    //return new MyTag(i, j, k);
                                    pRet.Add(new MyTag(i, j, k));

                                }
                                else if (aPoziceKurzoruMS == pP.begin || aPoziceKurzoruMS == pP.end)
                                {
                                    pRet.Add(new MyTag(i, j, k));

                                }
                                else if (aPoziceKurzoruMS < pP.begin)
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


                TextWriter writer = new StreamWriter(datastream);
                XmlSerializer serializer = new XmlSerializer(typeof(MySubtitlesData));

                //XmlTextWriter writer = new XmlTextWriter(jmenoSouboru, Encoding.UTF8);

                serializer.Serialize(writer, pCopy);
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

            if (dta != null)
            {
                dta.JmenoSouboru = jmenoSouboru;
                dta.Ulozeno = true;
                return dta;
            }

            return null;
        }

        //Deserializuje soubor             
        public MySubtitlesData Deserializovat(Stream datastream)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MySubtitlesData));
                //FileStream reader = new FileStream(jmenoSouboru, FileMode.Open);
                //TextReader reader = new StreamReader("e:\\MySubtitlesDataXml.txt");
                MySubtitlesData md;// = new MySubtitlesData();

                XmlTextReader xreader = new XmlTextReader(datastream);
                md = (MySubtitlesData)serializer.Deserialize(xreader);
                xreader.Close();


                //pokud nebyly v souboru ulozeny foneticke prepisy odstavcu, jsou automaticky vytvoreny podle struktury
                for (int i = 0; i < md.Chapters.Count; i++)
                {
                    for (int j = 0; j < md.Chapters[i].Sections.Count; j++)
                    {
                        if (md.Chapters[i].Sections[j].Paragraphs.Count != md.Chapters[i].Sections[j].PhoneticParagraphs.Count)
                        {
                            md.Chapters[i].Sections[j].PhoneticParagraphs.Clear();
                            for (int k = 0; k < md.Chapters[i].Sections[j].Paragraphs.Count; k++)
                            {
                                MyParagraph pP = md.VratOdstavec(new MyTag(i, j, k));
                                md.Chapters[i].Sections[j].PhoneticParagraphs.Add(new MyParagraph("", null, pP.begin, pP.end));
                                md.ZadejSpeakera(new MyTag(i, j, k), pP.speakerID);


                            }
                        }
                    }
                }


                return md;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri derializaci souboru: " + ex.Message);
                MyLog.LogujChybu(ex);
                return null;
            }

        }

        public MyTag UpravCasZobraz(MyTag aTag, long aBegin, long aEnd)
        {
            return UpravCasZobraz(aTag, aBegin, aEnd, false);
        }

        /// <summary>
        /// upravi a zobrazi cas u textboxu....begin a end.. pokud -2 tak se nemeni hodnoty casu..., -1 znamena vynulovani hodnot casu pocatku nebo konce
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="aBegin"></param>
        /// <param name="aEnd"></param>
        /// <returns></returns>
        public MyTag UpravCasZobraz(MyTag aTag, long aBegin, long aEnd, bool aIgnorovatPrekryv)
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

            long pKonecPredchoziho = VratCasElementuKonec(pTagPredchozi);
            long pZacatekSoucasneho = VratCasElementuPocatek(aTag);
            long pKonecSoucasneho = VratCasElementuKonec(aTag);
            long pZacatekNasledujiciho = VratCasElementuPocatek(newTag2);


            if (VratCasElementuPocatek(pTagPredchozi) > -1 && aBegin > -1)
            {
                long pCasElementuKonec = VratCasElementuKonec(aTag);

                if (pCasElementuKonec >= 0 && pCasElementuKonec < aBegin && aEnd < aBegin)
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



           // UpravCasElementu(aTag, aBegin, -2);

            if (VratCasElementuKonec(newTag2) > -1 && aEnd > -1)
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
    }

    //trida ktera obsahuje informace o casove znacce jednotlicych useku - pocatek a konec a index pozice v textu
    public class MyCasovaZnacka
    {
        public long Time { get; set; }   //cas znacky v ms
        public int Index1 { get; set; }  //index v textu pred kurzorem
        public int Index2 { get; set; }  //index v textu za kurzorem - casova znacka lezi uprostred

        /// <summary>
        /// vytvori casovou znacku, kurzor lezi mezi indexem1 a indexem2
        /// </summary>
        /// <param name="aTime"></param>
        /// <param name="aIndexTextu1"></param>
        /// <param name="aIndexTextu2"></param>
        public MyCasovaZnacka(long aTime, int aIndexTextu1, int aIndexTextu2)
        {
            this.Time = aTime;
            this.Index1 = aIndexTextu1;
            this.Index2 = aIndexTextu2;
        }
    }

}
