using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace NanoTrans
{
    /// <summary>
    /// delegate pro predani fonetickeho prepisu
    /// </summary>
    /// <param name="aText"></param>
    /// <param name="pStav"></param>
    public delegate void DelegatePhoneticOut(string aText, MyBuffer16 aBufferProPrepsani, int pStav);

    /// <summary>
    /// prepisovaci pravidlo
    /// </summary>
    public class MyRule
    {
        /// <summary>
        /// co prepisujeme
        /// </summary>
        public string[] Znaky;
        /// <summary>
        /// na co znak/y prepisujeme
        /// </summary>
        public string[] NaCO;
        /// <summary>
        /// znaky pred prepisovanym znakem
        /// </summary>
        public string[] ZnakyPred;
        /// <summary>
        /// znaky za prepisovanym znakem
        /// </summary>
        public string[] ZnakyZa;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aZnaky">co prepisujeme</param>
        /// <param name="aNaCo">na co znak/y prepisujeme</param>
        /// <param name="aZnakyPred">znaky pred prepisovanym znakem</param>
        /// <param name="aZnakyZa">znaky za prepisovanym znakem</param>
        public MyRule(string[] aZnaky, string[] aNaCo, string[] aZnakyPred, string[] aZnakyZa)
        {
            this.Znaky = aZnaky;
            this.NaCO = aNaCo;
            this.ZnakyPred = aZnakyPred;
            this.ZnakyZa = aZnakyZa;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="aZnaky">co prepisujeme</param>
        /// <param name="aNaCo">na co znak/y prepisujeme</param>
        /// <param name="aZnakyPred">znaky pred prepisovanym znakem</param>
        /// <param name="aZnakyZa">znaky za prepisovanym znakem</param>
        public MyRule(string[] aZnaky, string[] aNaCo, string[] aZnakyPred, string[] aZnakyZa, string aStringZa)
        {
            this.Znaky = aZnaky;
            this.NaCO = aNaCo;
            this.ZnakyPred = aZnakyPred;
            this.ZnakyZa = aZnakyZa;
            if (aStringZa != null)
            {
                ZnakyZa = new string[aZnakyZa.Length];
                for (int i = 0; i < ZnakyZa.Length; i++)
                {
                    ZnakyZa[i] = aZnakyZa[i] + aStringZa;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="aZnaky">co prepisujeme</param>
        /// <param name="aNaCo">na co znak/y prepisujeme</param>
        /// <param name="aZnakyPred">znaky pred prepisovanym znakem</param>
        /// <param name="aZnakyZa">znaky za prepisovanym znakem</param>
        public MyRule(string aZnak, string aNaCo, string aZnakPred, string aZnakZa)
        {
            this.Znaky = new string[] { aZnak };
            this.NaCO = new string[] { aNaCo };
            if (aZnakPred != null)
            {
                this.ZnakyPred = new string[] { aZnakPred };
            }
            if (aZnakZa != null)
            {
                this.ZnakyZa = new string[] { aZnakZa };
            }
        }

        /// <summary>
        /// pridat mezeru, nakonec znakuza prida mezeru
        /// </summary>
        /// <param name="aZnaky">co prepisujeme</param>
        /// <param name="aNaCo">na co znak/y prepisujeme</param>
        /// <param name="aZnakyPred">znaky pred prepisovanym znakem</param>
        /// <param name="aZnakyZa">znaky za prepisovanym znakem</param>
        /// 
        public MyRule(string aZnak, string aNaCo, string[] aZnakyPred, string[] aZnakyZa, bool aPom)
        {
            this.Znaky = new string[] { aZnak };
            this.NaCO = new string[] { aNaCo };

            this.ZnakyPred = aZnakyPred;
                        
            this.ZnakyZa = aZnakyZa;
            
        }

    }
    
    /// <summary>
    /// trida umoznujici foneticky prepis
    /// </summary>
    class MyFonetic
    {
        /// <summary>
        /// samohlásky
        /// </summary>
        public static string[] SA = { "a", "á", "e", "é", "i", "í", "o", "ó", "u", "ú" };
        /// <summary>
        /// znele parove souhlasky
        /// </summary>
        public static string[] ZPS = { "b", "d", "ď", "g", "z", "ž", "v", "h", "C", "Č" };//C=dz, Č=dž
        /// <summary>
        /// neznele parove souhlasky
        /// </summary>
        public static string[] NPS = { "p", "t", "ť", "k", "s", "š", "f", "X", "c", "č" };
        /// <summary>
        /// jedinecne souhlasky (znele)
        /// </summary>
        public static string[] JS = { "m", "n", "ň", "l", "j", "r", "ř" };

        public static string[] ABECEDA_mala = {"a", "á", "b", "c", "č", "d", "ď", "e", "é", "ě", "f", "g", "h", "i", "í", "j", "k", "l", "m", "n", "ň", "o", "ó", "p", "q", "r", "ř", "s", "š", "t", "ť", "u", "ú", "ů", "v", "w", "x", "y", "ý", "z","ž"};

        public static string[] ABECEDA_velka
        {
            get
            {
                string[] pAbeceda = new string[ABECEDA_mala.Length];
                for (int i = 0; i < pAbeceda.Length; i++)
                {
                    pAbeceda[i] = ABECEDA_mala[i].ToUpper();
                }
                return pAbeceda;
            }
        }

        public static string[] ABECEDA
        {
            get
            {
                string[] pAbeceda = new string[ABECEDA_velka.Length + ABECEDA_mala.Length];
                for (int i = 0; i < ABECEDA_mala.Length; i++)
                {
                    pAbeceda[i] = ABECEDA_mala[i];
                    pAbeceda[i + ABECEDA_mala.Length] = ABECEDA_velka[i];
                }
                return pAbeceda;
            }
        }

        /// <summary>
        /// znaky pripustne pro fonetickou abecedu
        /// </summary>
        public static char[] ABECEDA_FONETICKA = { 'a', 'á', 'b', 'c', 'C', 'č', 'Č', 'd', 'ď', 'e', 'é', 'f', 'g', 'h', 'X', 'i', 'í', 'j', 'k', 'l', 'm', 'M', 'n', 'N', 'ň', 'o', 'ó', 'p', 'r', 'ř', 'Ř', 's', 'š', 't', 'ť', 'u', 'ú', 'v', 'z', 'ž', '-', 'E', '1', '2', '3', '4', '5', '0' };
        public static string[] ABECEDA_FONETICKA_HTK = { "a", "aa", "b", "ts", "dz", "ch", "dg", "d", "dj", "e", "ee", "f", "g", "h", "x", "i", "ii", "y", "k", "l", "m", "mg", "n", "ng", "nj", "o", "oo", "p", "r", "rz", "rs", "s", "sh", "t", "tj", "u", "uu", "v", "z", "zh", "si", "swa", "n1", "n2", "n3", "n4", "n5", "n0" };

        /// <summary>
        /// prevede foneticky string na htk format - odstrani mezery
        /// </summary>
        /// <param name="aFonetickyText"></param>
        /// <returns></returns>
        public static string PrevedFonetickyTextNaHTKFormat(string aFonetickyText)
        {
            try
            {
                aFonetickyText = aFonetickyText.Replace("_", "");
                string pNovyFormat = "";
                for (int i = 0; i < aFonetickyText.Length; i++)
                {
                    for (int j = 0; j < ABECEDA_FONETICKA.Length; j++)
                    {
                        if (ABECEDA_FONETICKA[j] == aFonetickyText[i])
                        {
                            pNovyFormat += "0 0 " + ABECEDA_FONETICKA_HTK[j];
                            if (i < aFonetickyText.Length - 1)
                            {
                                pNovyFormat += "\n";
                            }
                        }
                    }
                }
                return pNovyFormat;
            }
            catch
            {
                return null;
            }

        }


        /// <summary>
        /// znaky z foneticke abecedy, ktere nemaji orto prepis
        /// </summary>
        public static char[] ABECEDA_NEFONETICKE_ZNAKY = { '-', 'E', '1', '2', '3', '4', '5', '0' };
        






        /// <summary>
        /// seznam prepisovacich pravidel v danem poradi jak je pouzit
        /// </summary>
        public List<MyRule> prepisovaciPravidla;

        /// <summary>
        /// prepisovaci pravidla pro prepis interpunkce - nejprve dojde k oddeleni interpunkce mezerou
        /// </summary>
        public List<MyRule> prepisovaciPravidlaNormalizace;

        /// <summary>
        /// buffer pro audio, ktere ze ktereho se vytvori wav a dojde k jeho rozpoznani na foneticky prepis
        /// </summary>
        private MyBuffer16 _BufferAudia;
        private string _CestaAdresareProgramu;

        
        private bool _Prepisovani;
        /// <summary>
        /// info zda uz dochazi k prepisovani jineho elementu
        /// </summary>
        public bool Prepisovani { get { return _Prepisovani; } }

        public MyFonetic(string aAbsolutniCestaAdresareProgramu)
        {
            this._CestaAdresareProgramu = aAbsolutniCestaAdresareProgramu;
            prepisovaciPravidla = new List<MyRule>();
            prepisovaciPravidla.Add(new MyRule(" ", "_", null, null));
            //prepisovaciPravidla.Add(new MyRule(new string[] { "ch" }, new string[] { "X" }, null, null));
            prepisovaciPravidla.Add(new MyRule("ch", "X", null, null));
            prepisovaciPravidla.Add(new MyRule("ů", "ú", null, null));
            prepisovaciPravidla.Add(new MyRule("w", "v", null, null));
            prepisovaciPravidla.Add(new MyRule("q", "kv", null, null));

            

            //ě → je / <b, p, f, v> _
            prepisovaciPravidla.Add(new MyRule(new string[] { "ě" }, new string[] { "je" }, new string[] { "b", "p", "f", "v" }, null));
            /*
                dě → ďe / _
                tě → ťe / _ 
                ně → ňe / _
                ě → ňe / m_ 
             */
            prepisovaciPravidla.Add(new MyRule("dě", "ďe", null, null));
            prepisovaciPravidla.Add(new MyRule("tě", "ťe", null, null));
            prepisovaciPravidla.Add(new MyRule("ně", "ňe", null, null));
            prepisovaciPravidla.Add(new MyRule("ě", "ňe", "m", null));
            /*
            d → ď / _<i, í>
            t → ť / _<i, í>
            n → ň / _<i, í>             
            */
            prepisovaciPravidla.Add(new MyRule(new string[] { "d" }, new string[] { "ď" }, null, new string[] { "i", "í" }));
            prepisovaciPravidla.Add(new MyRule(new string[] { "t" }, new string[] { "ť" }, null, new string[] { "i", "í" }));
            prepisovaciPravidla.Add(new MyRule(new string[] { "n" }, new string[] { "ň" }, null, new string[] { "i", "í" }));

            prepisovaciPravidla.Add(new MyRule("y", "i", null, null));
            prepisovaciPravidla.Add(new MyRule("ý", "í", null, null));

            /*
            x → gz / _ <ZPS, JS>
            x → ks / _ <NPS, - >
            */
            prepisovaciPravidla.Add(new MyRule("x", "gz", null, ZPS, false));
            prepisovaciPravidla.Add(new MyRule("x", "gz", null, JS, false));
            prepisovaciPravidla.Add(new MyRule("x", "ks", null, NPS, false));
            prepisovaciPravidla.Add(new MyRule("x", "ks", null, "_"));


            /*
            ex → egz / - _ SA
            */
            prepisovaciPravidla.Add(new MyRule("ex", "egz", new string[] { "_" }, SA, false));
            /*
            x → ks / - _ SA
            x → ks / SA1 _ SA2
            */
            prepisovaciPravidla.Add(new MyRule("x", "ks", new string[] { "_" }, SA, false));
            prepisovaciPravidla.Add(new MyRule("x", "ks", SA, SA, true));

            //pravidla spodoby znelosti
            /*
            ZPS1 → ¬ ZPS1 / _ < - , NPS, ZPS2 - >
            NPS1 → ¬ NPS1 / _ ZPS  
            */
            prepisovaciPravidla.Add(new MyRule(ZPS, NPS, null, new string[] { "_" }));
            prepisovaciPravidla.Add(new MyRule(ZPS, NPS, null, NPS));
            prepisovaciPravidla.Add(new MyRule(ZPS, NPS, null, ZPS,"_"));


            //pravidla spodoby artikulacni
            /*
            n → N / _ < k, g >
            m → M / _ < v, f >
            n → ň / _ < ť, ď >
            d → ď / _ ň
            t → ť / _ ň
            */
            prepisovaciPravidla.Add(new MyRule("n", "N", null, new string[] { "k", "g" }, false));
            prepisovaciPravidla.Add(new MyRule("m", "M", null, new string[] { "v", "f" }, false));
            prepisovaciPravidla.Add(new MyRule("n", "ň", null, new string[] { "ť", "ď" }, false));
            prepisovaciPravidla.Add(new MyRule("d", "ď", null, new string[] { "ň" }, false));
            prepisovaciPravidla.Add(new MyRule("t", "ť", null, new string[] { "ň" }, false));

            /*
            ts → c / _
            tš → č / _
            ds → c / _
            dš → č / _
            dz → C / _
            dž → Č / _
            */
            prepisovaciPravidla.Add(new MyRule("ts", "c", null, null));
            prepisovaciPravidla.Add(new MyRule("tš", "č", null, null));
            prepisovaciPravidla.Add(new MyRule("ds", "c", null, null));
            prepisovaciPravidla.Add(new MyRule("dš", "č", null, null));
            prepisovaciPravidla.Add(new MyRule("dz", "C", null, null));
            prepisovaciPravidla.Add(new MyRule("dž", "Č", null, null));

            prepisovaciPravidlaNormalizace = new List<MyRule>();
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "." }, new string[] { " ." }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "," }, new string[] { " ," }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { ";" }, new string[] { " ;" }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "!" }, new string[] { " !" }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "?" }, new string[] { " ?" }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { ":" }, new string[] { " :" }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "\"" }, new string[] { " \"" }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "\"" }, new string[] { "\" " }, new string[] { "" }, ABECEDA));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "\"" }, new string[] { " \"" }, new string[] { ",", ".", ";", "!", "?", ":" }, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "\"" }, new string[] { "\" " }, new string[] { "" }, new string[] { ",", ".", ";", "!", "?", ":" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "„" }, new string[] { "„ " }, new string[] { "" }, ABECEDA));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "”" }, new string[] { " ”" }, ABECEDA, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "”" }, new string[] { " ”" }, new string[] { ",", ".", ";", "!", "?", ":" }, new string[] { "" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { " ”" }, new string[] { " ” " }, new string[] { "" }, new string[] { ",", ".", ";", "!", "?", ":" }));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "”" }, new string[] { "” " }, new string[] { "" }, ABECEDA));
            prepisovaciPravidlaNormalizace.Add(new MyRule(new string[] { "-" }, new string[] { " -" }, ABECEDA, new string[] { "" }));
        }


        public int NormalizaceTextu(int aIndexNormalizace, string aTextKNormalizaci, ref string aNormalizovanyText)
        {
            try
            {
                aNormalizovanyText = aTextKNormalizaci;
                if (aIndexNormalizace == 2) return 0;
                foreach (MyRule r in prepisovaciPravidlaNormalizace)
                {
                    
                    string co = "";
                    for (int i = 0; i < r.ZnakyPred.Length; i++)
                    {
                        string pPred = r.ZnakyPred[i];
                        string pCo = r.Znaky[0];
                        for (int j = 0; j < r.ZnakyZa.Length; j++)
                        {
                            //if (aIndexNormalizace == 0)
                            {
                                co = pPred + pCo + r.ZnakyZa[j];
                                string NaCo = pPred + r.NaCO[0] + r.ZnakyZa[j];
                                aNormalizovanyText = aNormalizovanyText.Replace(co, NaCo);
                            }
                            
                        }
                        if (aIndexNormalizace == 1)
                        {
                            aNormalizovanyText = aNormalizovanyText.Replace(r.NaCO[0], " ");
                            aNormalizovanyText = aNormalizovanyText.Replace(r.Znaky[0], " ");
                        }
                    }
                    
                }
                if (aIndexNormalizace == 1)
                {
                    aNormalizovanyText = aNormalizovanyText.Replace("  ", " ");
                    aNormalizovanyText = aNormalizovanyText.Replace("  ", " ");
                }

                return 0;
            }
            catch
            {
                return -1;
            }

        }

        public bool OdstraneniNefonetickychZnakuZPrepisu(MySubtitlesData aDokument, TranscriptionElement aTag)
        {
            try
            {
                if (aDokument == null) return false;
                if (aTag == null) return false;
                if (!aTag.IsParagraph) return false;
                MyParagraph pP =(MyParagraph)aTag;
                if (pP == null) return false;
                bool pPredchoziMezera = true;
                for (int i = 0; i < pP.Phrases.Count; i++)
                {
                    MyPhrase pPhrase = pP.Phrases[i];
                    for (int j = 0; j < ABECEDA_NEFONETICKE_ZNAKY.Length; j++)
                    {
                        if (pPhrase.Phonetics.Contains(ABECEDA_NEFONETICKE_ZNAKY[j].ToString()) && (pPhrase.Phonetics.Length <= 2))
                        {
                            pP.Phrases.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }

                for (int i = 0; i < pP.Phrases.Count; i++)
                {
                    MyPhrase pPhrase = pP.Phrases[i];
                    if (pPhrase.Phonetics.Contains("_") && pPhrase.Phonetics.Length == 1)
                    {
                        if (pPredchoziMezera)
                        {
                            pP.Phrases.RemoveAt(i);
                            i--;
                        }
                        pPredchoziMezera = true;
                    }
                    else
                    {
                        pPredchoziMezera = false;
                    }


                }

                /*
                    40  -  -  si
                    41  @  E  swa
                    42  1  1  n1
                    43  2  2  n2
                    44  3  3  n3
                    45  4  4  n4
                    46  5  5  n5
                    47  0  0  n0
                 */
                return true;

            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// vytvori foneticky prepis pomoci vnitrnich prepisovacich pravidel
        /// </summary>
        /// <param name="aText"></param>
        /// <returns></returns>
        public string VratFonetickyPrepis(string aText)
        {
            //TODO:(x) foneticky prepis
            return "";
            throw new NotImplementedException();

        }

    }
}
