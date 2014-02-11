using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace NanoTrans
{
    /// <summary>
    /// polozka makra - skladajici se z toho co se vrati a fonetickeho prepisu a cisla ktere se vrati
    /// </summary>
    public class MyMakro
    {
        public string fonetickyPrepis;
        /// <summary>
        /// text, ktery se zobrazi
        /// </summary>
        public string hodnotaVraceni;
        /// <summary>
        /// Hodnota, ktera je zapsana do rozpoznavace, v prislusnem tvaru {MAKRO...}
        /// </summary>
        [XmlIgnore]
        public string HodnotaVraceniMakro
        {
            get
            {
                string ret = "";
                //ret = hodnotaVraceni.Replace("-X-", "-" + indexMakra.ToString() + "-");
                //ret = "{MAKRO:" + indexMakra.ToString() + ":" + hodnotaVraceni + "}";
                ret = "MAKRO:" + indexMakra.ToString() + ":";
                return ret;
            }
        }
        public int indexMakra;

        public MyMakro()
        {

        }
        
        public MyMakro(string aHodnotaVraceni, string aFonetickyPrepis, int aIndexMakra)
        {
            hodnotaVraceni = aHodnotaVraceni;
            fonetickyPrepis = aFonetickyPrepis;
            indexMakra = aIndexMakra;
        }

        /// <summary>
        /// serializuje seznam maker do souboru
        /// </summary>
        /// <param name="aSeznam"></param>
        /// <param name="aCesta"></param>
        /// <returns></returns>
        public static bool SerializovatSeznamMaker(List<MyMakro> aSeznam, string aCesta)
        {
            try
            {
                if (aSeznam == null || aCesta == null) return false;
                XmlSerializer serializer = new XmlSerializer(aSeznam.GetType());
                TextWriter writer = new StreamWriter(aCesta);

                serializer.Serialize(writer, aSeznam);
                writer.Close();
                return true;
            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
                return false;
            }
        }

        public static List<MyMakro> DeserializovatSeznamMaker(string aCesta)
        {
            try
            {
                if (aCesta != null)
                {
                    //pokus o serializaci pokud soubor neexistuje
                    if (!new FileInfo(aCesta).Exists)
                    {
                        SerializovatSeznamMaker(VychoziSeznamMaker(), aCesta);
                    }

                    XmlSerializer serializer = new XmlSerializer(typeof(List<MyMakro>));
                    List<MyMakro> mM;

                    XmlTextReader xreader = new XmlTextReader(aCesta);
                    mM = (List<MyMakro>)serializer.Deserialize(xreader);
                    xreader.Close();
                    if (mM != null) return mM;
                }
            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
            }
            return VychoziSeznamMaker();
            
        }
        
        private static List<MyMakro> VychoziSeznamMaker()
        {
            
            List<MyMakro> pMakra = new List<MyMakro>();

            pMakra.Add(new MyMakro("Nový přepis", "novípřepis", 10));
            pMakra.Add(new MyMakro("Otevřít přepis", "otevřítpřepis", 20));

            pMakra.Add(new MyMakro("Otevrit audio", "otevřítaudio", 30));
            pMakra.Add(new MyMakro("Otevřít video", "otevřítvideo", 40));

            pMakra.Add(new MyMakro("Uložit", "uložit", 50));
            pMakra.Add(new MyMakro("Uložit jako", "uložitjako", 60));

            pMakra.Add(new MyMakro("Mluvčí", "mlufčí", 220));
            pMakra.Add(new MyMakro("Vložit ruch", "vložitruX", 230));

            pMakra.Add(new MyMakro("Nastavení", "nastaveňí", 200));

            pMakra.Add(new MyMakro("Nová kapitola", "novákapitola", 301));
            pMakra.Add(new MyMakro("Nová sekce", "novásekce", 302));
            pMakra.Add(new MyMakro("Nový odstavec", "novíodstavec", 303));

            pMakra.Add(new MyMakro("Nápověda", "nápovjeda", 400));
            pMakra.Add(new MyMakro("O programu", "oprogramu", 401));

            pMakra.Add(new MyMakro("Ukončit", "ukončit", 500));
            pMakra.Add(new MyMakro("Maximalizovat", "maksimalizovat", 505));
            pMakra.Add(new MyMakro("Minimalizovat", "minimalizovat", 506));

            pMakra.Add(new MyMakro("Konec hlasového ovládání", "konechlasovéhoovládání", 550));
            pMakra.Add(new MyMakro("Konec hlasového ovládání", "konecovládání", 550));
            pMakra.Add(new MyMakro("Konec hlasového ovládání", "zastavithlasovéovládání", 550));


            pMakra.Add(new MyMakro("Přehrát", "přehrát", 1000));
            pMakra.Add(new MyMakro("Zastavit", "zastavit", 1001));
            return pMakra;

            pMakra.Add(new MyMakro("{MAKRO-X-Novýpřepis}", "novípřepis", 10));
            pMakra.Add(new MyMakro("{MAKRO-X-Otevřítpřepis}", "otevřítpřepis", 20));

            pMakra.Add(new MyMakro("{MAKRO-X-Otevritaudio}", "otevřítaudio", 30));
            pMakra.Add(new MyMakro("{MAKRO-X-OtevritvideoAAAB}", "otevřítvideo", 40));

            pMakra.Add(new MyMakro("{MAKRO-X-Ulozit}", "uložit", 50));
            pMakra.Add(new MyMakro("{MAKRO-X-Ulozitjako}", "uložitjako", 60));

            pMakra.Add(new MyMakro("{MAKRO-X-Mluvčí}", "mlufčí", 220));
            


            pMakra.Add(new MyMakro("{MAKRO-X-Nastavení}", "nastaveňí", 200));
            pMakra.Add(new MyMakro("{MAKRO-X-Ukončit}", "ukončit", 500));

            pMakra.Add(new MyMakro("{MAKRO-X-}", "přehrát", 1000));
            pMakra.Add(new MyMakro("{MAKRO-X-}", "zastavit", 1001));
            return pMakra;
            //return null;

            pMakra.Add(new MyMakro("{MAKRO-X-}", "novípřepis", 10));
            pMakra.Add(new MyMakro("{MAKRO-X-}", "otevřítpřepis", 20));

            pMakra.Add(new MyMakro("{MAKRO-X-}", "otevřítaudio", 30));
            pMakra.Add(new MyMakro("{MAKRO-X-}", "otevřítvideo", 40));
            pMakra.Add(new MyMakro("{MAKRO-X-}", "uložit", 50));
            pMakra.Add(new MyMakro("{MAKRO-X-}", "uložitjako", 60));

            pMakra.Add(new MyMakro("{MAKRO-X-}", "mlufčí", 220));

            pMakra.Add(new MyMakro("{MAKRO-X-}", "nastaveňí", 200));

            pMakra.Add(new MyMakro("{MAKRO-X-}", "ukončit", 500));


            pMakra.Add(new MyMakro("{MAKRO-X-}", "přehrát", 1000));
            pMakra.Add(new MyMakro("{MAKRO-X-}", "zastavit", 1001));

            return pMakra;


            return pMakra;
        }
    }
}
