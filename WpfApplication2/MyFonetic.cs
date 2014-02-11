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

    public class MyFoneticSlovnikPolozka
    {
        public string Slovo;
        public List<string> FonetickeVarianty;

        public MyFoneticSlovnikPolozka(string aSlovo, List<string> aFonetickeVarianty)
        {
            Slovo = aSlovo;
            FonetickeVarianty = aFonetickeVarianty;
        }

        public MyFoneticSlovnikPolozka(string aSlovo, string aFonetickaVarianta)
        {
            Slovo = aSlovo;
            FonetickeVarianty = new List<string>();
            FonetickeVarianty.Add(aFonetickaVarianta);
            
        }

        /// <summary>
        /// info zda je foneticka varianta v polozce
        /// </summary>
        /// <param name="aVarianta"></param>
        /// <returns></returns>
        public bool jeFonetickaVarianta(string aVarianta)
        {
            foreach (string i in FonetickeVarianty)
            {
                if (i == aVarianta) return true;
            }
            return false;
        }
        
        /// <summary>
        /// prida fonetickou variantu slova - pokud jiz exituje, vraci 1;
        /// </summary>
        /// <param name="aVarianta"></param>
        /// <returns></returns>
        public int PridejFonetickouVariantu(string aVarianta)
        {
            if (aVarianta == "" || aVarianta == null) return -2;
            if (jeFonetickaVarianta(aVarianta)) return 1;
            FonetickeVarianty.Add(aVarianta);
            return 0;
        }
    }

    public class MyFoneticSlovnik
    {
        /// <summary>
        /// seznam pridanych slov
        /// </summary>
        public List<MyFoneticSlovnikPolozka> PridanaSlova = new List<MyFoneticSlovnikPolozka>();
        public List<MyFoneticSlovnikPolozka> PridanaSlovaDocasna = new List<MyFoneticSlovnikPolozka>();
        public List<MyFoneticSlovnikPolozka> SlovnikZakladni = new List<MyFoneticSlovnikPolozka>();
        private string _CestaSlovnikUzivatelsky = null;


        public int PridejPolozkuDocasnehoSlovniku(MyFoneticSlovnikPolozka aPolozka)
        {
            try
            {
                if (aPolozka == null) return -2;
                bool pPridano = false;
                foreach (MyFoneticSlovnikPolozka i in PridanaSlovaDocasna)
                {
                    if (i.Slovo == aPolozka.Slovo)
                    {
                        foreach (string j in aPolozka.FonetickeVarianty)
                        {
                            i.PridejFonetickouVariantu(j);
                        }
                        pPridano = true;
                    }
                }
                if (!pPridano)
                {
                    PridanaSlovaDocasna.Add(aPolozka);
                }
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// vraci polozku pokud je v zxakladnim slovniku, jinak null
        /// </summary>
        /// <param name="aText"></param>
        /// <param name="aFonetickyPrepis"></param>
        /// <returns></returns>
        public MyFoneticSlovnikPolozka JeVZakladnimSlovniku(string aSlovo, string aFonetickyPrepis)
        {
            foreach (MyFoneticSlovnikPolozka i in SlovnikZakladni)
            {
                if (i.Slovo == aSlovo)
                {
                    foreach (string j in i.FonetickeVarianty)
                    {
                        if (j == aFonetickyPrepis)
                        {
                            return i;
                        }
                    }
                }
            }
            return null;
        }

        public MyFoneticSlovnikPolozka JeVUzivatelskemSlovniku(string aSlovo, string aFonetickyPrepis)
        {
            foreach (MyFoneticSlovnikPolozka i in PridanaSlova)
            {
                if (i.Slovo == aSlovo)
                {
                    foreach (string j in i.FonetickeVarianty)
                    {
                        if (j == aFonetickyPrepis)
                        {
                            return i;
                        }
                    }
                }
            }
            return null;
        }

        public int PridejPolozkuSlovniku(MyFoneticSlovnikPolozka aPolozka)
        {
            try
            {
                if (aPolozka == null) return -2;
                bool pPridano = false;
                foreach (MyFoneticSlovnikPolozka i in PridanaSlova)
                {
                    if (i.Slovo == aPolozka.Slovo)
                    {
                        foreach (string j in aPolozka.FonetickeVarianty)
                        {
                            i.PridejFonetickouVariantu(j);
                        }
                        pPridano = true;
                    }
                }
                if (!pPridano)
                {
                    PridanaSlova.Add(aPolozka);
                }
                return 0;
            }
            catch
            {
                return -1;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="aJmenoSouboru">pokud null, pokusi se ulozit do minuleho umisteni</param>
        /// <returns></returns>
        public bool UlozitSlovnik(string aJmenoSouboru)
        {
            StreamWriter sw = null;
            try
            {
                if (aJmenoSouboru == null) aJmenoSouboru = _CestaSlovnikUzivatelsky;
                if (aJmenoSouboru == null) return false;
                sw = new StreamWriter(aJmenoSouboru, false, Encoding.GetEncoding(1250));
                sw.WriteLine("User vocabulary size = " + PridanaSlova.Count.ToString());
                for (int i = 0; i < PridanaSlova.Count; i++)
                {
                    if (PridanaSlova[i].FonetickeVarianty.Count > 0)
                    {
                        string pRadek = PridanaSlova[i].FonetickeVarianty.Count.ToString() + "\t" + PridanaSlova[i].Slovo;
                        for (int j = 0; j < PridanaSlova[i].FonetickeVarianty.Count; j++)
                        {
                            pRadek += "\t" + PridanaSlova[i].FonetickeVarianty[j];
                        }
                        sw.WriteLine(pRadek);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Chyba pri serializaci souboru s ruchy: " + ex.Message, "Varování");
                Window1.logAplikace.LogujChybu(ex);
                return false;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

        }

        /// <summary>
        /// nacte docasny nebo trvaly slovnik ze souboru
        /// </summary>
        /// <param name="aJmenoSouboru"></param>
        /// <returns></returns>
        public bool NacistSlovnik(string aJmenoSouboru, List<MyFoneticSlovnikPolozka> aSlovnik)
        {
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(aJmenoSouboru, Encoding.GetEncoding(1250));
                aSlovnik.Clear();
                string pRadek = sr.ReadLine();
                while ((pRadek = sr.ReadLine()) != null)
                {
                    string[] pPolozky = pRadek.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (pPolozky != null && pPolozky.Length > 1)
                    {
                        string pSlovo = pPolozky[1];
                        List<string> pFonetickePrepisy = new List<string>();
                        for (int i = 2; i < pPolozky.Length; i++)
                        {
                            pFonetickePrepisy.Add(pPolozky[i]);
                        }
                        aSlovnik.Add(new MyFoneticSlovnikPolozka(pSlovo, pFonetickePrepisy));
                    }
                }
                if (aJmenoSouboru.Contains(MyKONST.CESTA_SLOVNIK_FONETIKA_UZIVATELSKY))
                    _CestaSlovnikUzivatelsky = aJmenoSouboru;
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Chyba pri serializaci souboru s ruchy: " + ex.Message, "Varování");
                Window1.logAplikace.LogujChybu(ex);
                return false;
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

        }
    }
    
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
        
        /// <summary>
        /// promenna s textem k prepsani pro pristup z vnejsku, kvuli threadum a pod.
        /// </summary>
        public string TextKPrepsani;
        public MyTag TagKPrepsani;

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

        public bool OdstraneniNefonetickychZnakuZPrepisu(MySubtitlesData aDokument, MyTag aTag)
        {
            try
            {
                if (aDokument == null) return false;
                if (aTag == null) return false;
                if (!aTag.JeOdstavec) return false;
                MyTag pTag = new MyTag(aTag);
                pTag.tTypElementu = MyEnumTypElementu.foneticky;
                MyParagraph pP = aDokument.VratOdstavec(pTag);
                if (pP == null) return false;
                bool pPredchoziMezera = true;
                for (int i = 0; i < pP.Phrases.Count; i++)
                {
                    MyPhrase pPhrase = pP.Phrases[i];
                    for (int j = 0; j < ABECEDA_NEFONETICKE_ZNAKY.Length; j++)
                    {
                        if (pPhrase.Text.Contains(ABECEDA_NEFONETICKE_ZNAKY[j].ToString()) && (pPhrase.Text.Length <= 2 || pPhrase.TextPrepisovany == null))
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
                    if (pPhrase.Text.Contains("_") && pPhrase.Text.Length == 1)
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
            try
            {
                
                

                if (aText == null || aText == "") return "";
                //zmenseni vsech pismen
                string ret = aText.ToLower();

                //pridani mezer pred a za konec vety (textu)
                if (ret[0] != ' ')
                {
                    ret = " " + ret;
                }
                if (ret[ret.Length - 1] != ' ')
                {
                    ret += " ";
                }

                for (int i = 0; i < this.prepisovaciPravidla.Count; i++)
                {
                    MyRule pRule = this.prepisovaciPravidla[i];

                    //for pro prepis vice znaku na vice - neznele parove na znele atd.
                    for (int pIndex = 0; pIndex < pRule.Znaky.Length; pIndex++)
                    {


                        if (pRule.ZnakyZa == null && pRule.ZnakyPred == null)
                        {
                            ret = ret.Replace(pRule.Znaky[pIndex], pRule.NaCO[pIndex]);
                        }
                        if (pRule.ZnakyPred == null && pRule.ZnakyZa != null)
                        {
                            for (int j = 0; j < prepisovaciPravidla[i].ZnakyZa.Length; j++)
                            {
                                string pSekvence = pRule.Znaky[pIndex] + pRule.ZnakyZa[j];
                                ret = ret.Replace(pSekvence, pRule.NaCO[pIndex] + pRule.ZnakyZa[j]);
                            }
                        }
                        if (pRule.ZnakyZa == null && pRule.ZnakyPred != null)
                        {
                            for (int j = 0; j < pRule.ZnakyPred.Length; j++)
                            {
                                string pSekvence = pRule.ZnakyPred[j] + pRule.Znaky[pIndex];
                                ret = ret.Replace(pSekvence, pRule.ZnakyPred[j] + pRule.NaCO[pIndex]);
                            }
                        }
                        if (pRule.ZnakyZa != null && pRule.ZnakyPred != null)
                        {
                            for (int j = 0; j < pRule.ZnakyPred.Length; j++)
                            {
                                for (int k = 0; k < pRule.ZnakyZa.Length; k++)
                                {
                                    string pSekvence = pRule.ZnakyPred[j] + pRule.Znaky[pIndex] + pRule.ZnakyZa[k];
                                    ret = ret.Replace(pSekvence, pRule.ZnakyPred[j] + pRule.NaCO[pIndex] + pRule.ZnakyZa[k]);
                                }
                            }
                        }
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                return null;
            }


        }

        private Thread _threadFonetickyPrepis = null;
        private MyBuffer16 _tBufferAudia;
        private string _tText;
        private MyFoneticSlovnik _tSlovnikNerozpoznanych;
        private MySetupFonetickyPrepis _tFonetickyPrepisNastaveni;
        private DelegatePhoneticOut _tDelegatPhonetic;

        

        public bool SpustFonetickyPrepisHTKAsynchronne(MyBuffer16 aBufferAudia, string aText, MyFoneticSlovnik aSlovnikNerozpoznanych, MySetupFonetickyPrepis aFonetickyPrepisNastaveni, DelegatePhoneticOut aDelegatFonetiky)
        {
            try
            {
                _tBufferAudia = aBufferAudia;
                _tText = aText;
                _tSlovnikNerozpoznanych = aSlovnikNerozpoznanych;
                _tFonetickyPrepisNastaveni = aFonetickyPrepisNastaveni;
                _tDelegatPhonetic = aDelegatFonetiky;

                if (_Prepisovani) return false;
                _threadFonetickyPrepis = new Thread(SpustFonetickyPrepisHTKAsynchronne);
                _threadFonetickyPrepis.Start();
                return true;
            }
            catch
            {
                return false;
            }

        }

        private void SpustFonetickyPrepisHTKAsynchronne()
        {
            string pTextOut = VratFonetickyPrepis(_tBufferAudia, _tText, _tSlovnikNerozpoznanych, _tFonetickyPrepisNastaveni);
            if (_tDelegatPhonetic != null)
                this._tDelegatPhonetic(pTextOut, _tBufferAudia, 0);
        }

        /// <summary>
        /// po predani audio dat a normalniho textu vraci vraci foneticky prepis - vola skript perlu (HTK)
        /// </summary>
        /// <param name="aBufferAudia"></param>
        /// <returns></returns>
        public string VratFonetickyPrepis(MyBuffer16 aBufferAudia, string aText, MyFoneticSlovnik aSlovnikNerozpoznanych, MySetupFonetickyPrepis aFonetickyPrepisNastaveni)
        {
            string output = "CHYBA";
            _Prepisovani = true;
            try
            {
                this._BufferAudia = aBufferAudia;
                string pCesta = this._CestaAdresareProgramu + MyKONST.CESTA_FONETIKA_DOCASNY_WAV;
                if (aBufferAudia.DelkaMS <= 0)
                    return "";
                if (aBufferAudia.UlozBufferDoWavSouboru(pCesta))
                {
                    string pJazyk = "";
                    string pPohlavi = "";
                    if (aFonetickyPrepisNastaveni != null)
                    {
                        if (aFonetickyPrepisNastaveni.Jazyk != null && aFonetickyPrepisNastaveni.Jazyk != "") pJazyk = " -l " + aFonetickyPrepisNastaveni.Jazyk + " ";
                        if (aFonetickyPrepisNastaveni.Pohlavi != null && aFonetickyPrepisNastaveni.Pohlavi != "") pPohlavi = " -g " + aFonetickyPrepisNastaveni.Pohlavi + " ";
                    }
                    
                    aText = aText.Replace("\"", "\\\"");
                    ProcessStartInfo ps = new ProcessStartInfo("perl.exe", "\"" + this._CestaAdresareProgramu + MyKONST.CESTA_FONETIKA_PERL_SCRIPT + "\" -w \"" + pCesta + "\" -t \"" + aText + "\"" + " -d " + "\"" + this._CestaAdresareProgramu + MyKONST.CESTA_FONETIKA + "\"" + pJazyk + pPohlavi);
                    ps.UseShellExecute = false;
                    ps.RedirectStandardOutput = true;
                    ps.RedirectStandardError = true;
                    ps.CreateNoWindow = true;
                    Process p = new Process();
                    p.StartInfo = ps;
                    p.Start();
                    output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    int pPozice = output.IndexOf("{}");
                    if (pPozice >= 0)
                    {
                        output = output.Remove(0, pPozice + 2);
                    }
                    /*
                    Regex re = new Regex("{.*?}");
                    MatchCollection mc =  re.Matches(output);
                    if (mc != null && mc.Count > 0)
                    {
                        for (int i = 0; i < mc.Count; i++)
                        {
                            string s = mc[i].Value.Substring(1, mc[i].Value.Length - 2);
                            Match mtc = mc[i];

                            string[] pSplitS = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (pSplitS != null && pSplitS.Length == 2)
                            {
                                if (aSlovnikNerozpoznanych != null)
                                {
                                    MyFoneticSlovnikPolozka pPol = new MyFoneticSlovnikPolozka(pSplitS[0], pSplitS[1]);
                                    aSlovnikNerozpoznanych.PridejPolozkuDocasnehoSlovniku(pPol);
                                }
                                output = output.Replace(s, pSplitS[1]);
                            }
                        }
                    }
                    */
                    Regex re = new Regex("{.*?}");
                    MatchCollection mc = re.Matches(output);
                    if (mc != null && mc.Count > 0)
                    {
                        for (int i = 0; i < mc.Count; i++)
                        {
                            string s = mc[i].Value.Substring(1, mc[i].Value.Length - 2);
                            Match mtc = mc[i];

                            string[] pSplitS = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (pSplitS != null && pSplitS.Length == 2)
                            {
                                if (aSlovnikNerozpoznanych != null)
                                {
                                    MyFoneticSlovnikPolozka pPol = new MyFoneticSlovnikPolozka(pSplitS[0], pSplitS[1]);
                                    aSlovnikNerozpoznanych.PridejPolozkuDocasnehoSlovniku(pPol);
                                }
                                //output = output.Replace(s, pSplitS[1]);
                            }
                        }
                    }


                }
                _Prepisovani = false;
                return output;
            }
            catch
            {
                _Prepisovani = false;
                return output;
            }

        }

    }
}
