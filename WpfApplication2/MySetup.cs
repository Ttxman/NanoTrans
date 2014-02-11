using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Windows.Media;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Windows;

namespace NanoTrans
{
    [XmlInclude(typeof(string))]
    public class MySetupFonetickyPrepis
    {
        /// <summary>
        /// cz / sk
        /// </summary>
        public string Jazyk;
        /// <summary>
        /// male / female
        /// </summary>
        public string Pohlavi;
        public bool PrehratAutomatickyRozpoznanyOdstavec;
    }


    //record nastaveni rozpoznavace
    [XmlInclude(typeof(string))]
    public class MySetupRozpoznavac
    {
        
        //public string AbsolutniCestaRozpoznavace;
        public string RelativniCestaRozpoznavace;

        //v relativnich adresarich je ulozen nazev adresare od nanocore
        public string Mluvci;
        /// <summary>vychozi relativni adresar pro mluvci </summary>
        public string MluvciRelativniAdresar;
        public string JazykovyModel;
        public string JazykovyModelRelativniAdresar;
        public string PrepisovaciPravidla;
        public string PrepisovaciPravidlaRelativniAdresar;
        public string LicencniServer;
        /// <summary> parametr spousteni rozpoznavace </summary>
        public long DelkaInternihoBufferuPrepisovace;
        /// <summary>parametr spousteni rozpoznavace pro online prepis hlasoveho ovladani</summary>
        public long KvalitaRozpoznavaniOvladani;
        /// <summary>parametr spousteni rozpoznavace pro online prepis diktat</summary>
        public long KvalitaRozpoznavaniDiktat;

        public string LicencniSoubor;
               

        public MySetupRozpoznavac()
        {
            
        }
        
        /// <summary>
        /// vrati seznam vsech jazykovych modelu v danem adresari - seznam souboru
        /// </summary>
        public string[] JazykovyModelSeznamDostupnych(string aAbslutniCestaRozpoznavace)
        {
            
                List<string> ret = new List<string>();
                try
                {
                    FileInfo[] fiPole;
                    DirectoryInfo di = new DirectoryInfo(aAbslutniCestaRozpoznavace + "/" + JazykovyModelRelativniAdresar);
                    if (di != null && di.Exists)
                    {
                        fiPole = di.GetFiles();
                        if (fiPole != null)
                        {
                            foreach (FileInfo fi in fiPole)
                            {
                                ret.Add(fi.Name);
                            }
                        }
                    }
                }
                finally
                {
                    
                }
                return ret.ToArray();
            
        }

    

        /// <summary>
        /// vrati seznam vsech mluvcich v danem adresari - seznam souboru
        /// </summary>
        public string[] MluvciSeznamDostupnych(string aAbslutniCestaRozpoznavace)
        {
            
                List<string> ret = new List<string>();
                try
                {
                    FileInfo[] fiPole;
                    DirectoryInfo di = new DirectoryInfo(aAbslutniCestaRozpoznavace + "/" + MluvciRelativniAdresar);
                    if (di != null && di.Exists)
                    {
                        fiPole = di.GetFiles();
                        if (fiPole != null)
                        {
                            foreach (FileInfo fi in fiPole)
                            {
                                ret.Add(fi.Name);
                            }
                        }
                    }
                }
                finally
                {
                    
                }
                return ret.ToArray();
            
        }
        


        /// <summary>
        /// vrati string aktualniho souboru mluvciho pro prepisovac v nastaveni
        /// </summary>
        public string MluvciVybrany
        {
            get
            {
                string s = "";
                FileInfo fi2 = new FileInfo(Mluvci);
                if (fi2 != null)
                {
                    s = fi2.Name;
                }
                return s;
            }
        }

        /// <summary>
        /// vrati seznam vsech prepisovacich pravidel v danem adresari - seznam souboru
        /// </summary>
        public string[] PrepisovaciPravidlaSeznamDostupnych(string aAbslutniCestaRozpoznavace)
        {

            List<string> ret = new List<string>();
            try
            {
                FileInfo[] fiPole;
                DirectoryInfo di = new DirectoryInfo(aAbslutniCestaRozpoznavace + "/" + PrepisovaciPravidlaRelativniAdresar);
                if (di != null && di.Exists)
                {
                    fiPole = di.GetFiles();
                    if (fiPole != null)
                    {
                        foreach (FileInfo fi in fiPole)
                        {
                            ret.Add(fi.Name);
                        }
                    }
                }
            }
            finally
            {

            }
            return ret.ToArray();
        }
    }

    public struct Audio
    {

        public int VystupniZarizeniIndex;

        public int VstupniZarizeniIndex;

    }


    public class SingletonRefresher : System.ComponentModel.INotifyPropertyChanged
        {
            private MySetup m_setup;
            public MySetup Setup
            {
                get { return m_setup; }
                set
                {
                    m_setup = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("Setup"));
                }
            }

            //je to na setupu lepsi refreshnout vsechny bindingy.. stejne se vklada najedou
            public void Refresher()
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("Setup"));
            }

            #region INotifyPropertyChanged Members

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            #endregion
        }


    [XmlInclude(typeof(MySetupFonetickyPrepis))]
    [XmlInclude(typeof(MySetupRozpoznavac))]
    [XmlInclude(typeof(MySpeaker))]
    public class MySetup : System.ComponentModel.INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        static MySetup()
        {
            m_setup = new MySetup();
            m_refresher = new SingletonRefresher() {Setup = m_setup };
        }
        private static MySetup m_setup;
        public static MySetup Setup
        {
            set 
            {
                m_setup = value;
                m_refresher.Setup = value;
            }
            get { return m_setup; }
        }


        static SingletonRefresher m_refresher = null;
        //tohle je hack protoze tahle trida nebyla vytvarena jako singleton a objekt v Setup se muze menit
        public static SingletonRefresher Refresher
        {
            get
            {
                return m_refresher;
            }
        }


        /// <summary>
        /// absolutni cesta k exe programu po spusteni
        /// </summary>
        [XmlIgnore]
        public string absolutniCestaEXEprogramu
        {
            get
            {
                string loc = System.Reflection.Assembly.GetEntryAssembly().Location;
                loc = Path.GetDirectoryName(loc);
                return loc;
            }
                
        }
        /// <summary>
        /// Vraci absolutni cestu adresare s rozpoznavacem - pouze GET property
        /// </summary>
        [XmlIgnore]
        public string AbsolutniAdresarRozpoznavace
        {
            get
            {
                //return "";
                if (absolutniCestaEXEprogramu == null || rozpoznavac.RelativniCestaRozpoznavace == null) return null;
                return absolutniCestaEXEprogramu + rozpoznavac.RelativniCestaRozpoznavace;
            }
        }


        Brush m_BarvaTextBoxuOdstavce;
        [XmlIgnore]
        public Brush BarvaTextBoxuOdstavce //udava barvu textboxu
        {
            get { return m_BarvaTextBoxuOdstavce; }
            set 
            { 
                m_BarvaTextBoxuOdstavce = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("BarvaTextBoxuOdstavce"));
            }
        }                    
        [XmlIgnore]
        public Brush BarvaTextBoxuOdstavceAktualni { get; set; }            //udava barvu vybraneho textboxu
        /// <summary>
        /// barva textboxu pro foneticky prepis
        /// </summary>
        [XmlIgnore]
        public Brush BarvaTextBoxuFoneticky { get; set; }

        /// <summary>
        /// pokud nelze foneticky text editovat nebo tvorit
        /// </summary>
        [XmlIgnore]
        public Brush BarvaTextBoxuFonetickyZakazany { get; set; }
        

        public double ZpomalenePrehravaniRychlost { get; set; } //rychlost zpomaleneho prehravani
        public double VlnaMalySkok { get; set; } //delka maleho skoku na vlne
        public string[] NerecoveUdalosti { get; set; }
        
        double m_SetupTextFontSize;
        public double SetupTextFontSize //udava velikost pisma v textboxech   
        { 
            get{return m_SetupTextFontSize;}
            set
            {
                m_SetupTextFontSize = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("SetupTextFontSize"));
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("SetupOthersFontSize"));
                }
            }
        }

        public double SetupOthersFontSize
        {
            get { return m_SetupTextFontSize * 0.87; }
        }



        public string PriponaTitulku { get; set; } //property pripona titulku
        public string PriponaDatabazeMluvcich { get; set; }
        public string CestaDatabazeMluvcich { get; set; }

        /// <summary>
        /// info zda je k prepisu ukladan komplet mluvci vcetne obrazku
        /// </summary>
        public bool UkladatKompletnihoMluvciho { get; set; }

        public MySetupFonetickyPrepis fonetickyPrepis;
        public MySetupRozpoznavac rozpoznavac;
        

        public MySpeaker diktatMluvci;
        public MySpeaker hlasoveOvladaniMluvci;

        /// <summary>
        /// vyska okna fonetickeho prepisu v pixelech, pokud je hodnota zaporna, okno neni zobrazeno
        /// </summary>
        public float ZobrazitFonetickyPrepis;

        /// <summary>
        /// posledni pozice okna
        /// </summary>
        public System.Windows.Point OknoPozice;

        /// <summary>
        /// posledni velikost okna
        /// </summary>
        public System.Windows.Size OknoVelikost;

        /// <summary>
        /// stav okna - maximalizovano, normalni...
        /// </summary>
        public WindowState OknoStav;
        
        
        public Audio audio;

        /// <summary>
        /// info zda jsou zobrazeny fotografie mluvcich v prepisu
        /// </summary>
        public bool ZobrazitFotografieMluvcich { get; set; }

        /// <summary>
        /// maximalni vyska u fotky pri zobrazeni v prepisu
        /// </summary>
        public double Fotografie_VyskaMax { get; set; }
        /// <summary>
        /// default pozice richtextboxu kapitoly a max sirka fotografie
        /// </summary>
        public int defaultLeftPositionRichX;

        /// <summary>
        /// udava, kolik je maximalni sirka textu mluvciho na tlacitku
        /// </summary>
        public int maximalniSirkaMluvciho;

        /// <summary>
        /// zda zobrazit casove informace o zacatku segmentu
        /// </summary>
        public bool zobrazitCasBegin;
        /// <summary>
        /// zda zobrazit informace o konci segmentu
        /// </summary>
        public bool zobrazitCasEnd;

        /// <summary>
        /// jazyk programu
        /// </summary>
        public MyEnumJazyk jazykRozhranni;



        public void NastavDefaultHodnoty()
        {
            jazykRozhranni = MyEnumJazyk.anglictina;

            audio.VystupniZarizeniIndex = 0;
            audio.VstupniZarizeniIndex = 0;

            fonetickyPrepis.Jazyk = "";
            fonetickyPrepis.Pohlavi = "";
            fonetickyPrepis.PrehratAutomatickyRozpoznanyOdstavec = false;

            rozpoznavac.RelativniCestaRozpoznavace = "/Nanocore";
            rozpoznavac.Mluvci = "amodels/male.amd";
            rozpoznavac.JazykovyModel = "lmodels/spoken_20081128-20081130_153158-shd3-ubm.bin";
            rozpoznavac.PrepisovaciPravidla = "drules/spoken_20081128.ppp";
            rozpoznavac.DelkaInternihoBufferuPrepisovace = 6000;
            rozpoznavac.LicencniServer = "quadira.ite.tul.cz";
            rozpoznavac.LicencniSoubor = "martin.cickan@tul.cz";
            rozpoznavac.KvalitaRozpoznavaniOvladani = 110;
            rozpoznavac.KvalitaRozpoznavaniDiktat = 110;
            
            rozpoznavac.MluvciRelativniAdresar = "amodels";
            rozpoznavac.JazykovyModelRelativniAdresar = "lmodels";
            rozpoznavac.PrepisovaciPravidlaRelativniAdresar = "drules";

            diktatMluvci = new MySpeaker();
            hlasoveOvladaniMluvci = new MySpeaker();

            //CestaDatabazeMluvcich = this.absolutniCestaEXEprogramu + "/Data/DatabazeMluvcich.xml";
            CestaDatabazeMluvcich = "Data/DatabazeMluvcich.xml";

            SetupTextFontSize = 13;

            ZobrazitFotografieMluvcich = true;
            Fotografie_VyskaMax = 50;
            defaultLeftPositionRichX = 70;
            maximalniSirkaMluvciho = defaultLeftPositionRichX + 40;

            zobrazitCasBegin = true;
            zobrazitCasEnd = true;

            ZobrazitFonetickyPrepis = 100;
            OknoPozice = new Point(-1, -1);
            OknoVelikost = new Size(800, 600);
            OknoStav = WindowState.Normal;

            NerecoveUdalosti =  new []{ "kasel", "ehm", "smich", "ticho", "nadech", "hluk", "hudba", "mlask" };
        }

        
        public MySetup()
        {
            fonetickyPrepis = new MySetupFonetickyPrepis();
            rozpoznavac = new MySetupRozpoznavac();
            NastavDefaultHodnoty();
            PriponaTitulku = ".trsx";
            PriponaDatabazeMluvcich = ".xml";
            UkladatKompletnihoMluvciho = false;

            BarvaTextBoxuOdstavce = Brushes.AliceBlue;
            BarvaTextBoxuOdstavceAktualni = Brushes.AntiqueWhite;
            BarvaTextBoxuFoneticky = Brushes.AliceBlue;
            BarvaTextBoxuFonetickyZakazany = Brushes.LightGray;
        }
        
        public MySetup(string aAbsolutniCestaEXEprogramu)
        {
            if (aAbsolutniCestaEXEprogramu==null) aAbsolutniCestaEXEprogramu="";
            fonetickyPrepis = new MySetupFonetickyPrepis();
            rozpoznavac = new MySetupRozpoznavac();
            
            NastavDefaultHodnoty();
            //rozpoznavac.AbsolutniCestaRozpoznavace = this.AbsolutniCestaEXEprogramu + rozpoznavac.RelativniCestaRozpoznavace;
            
            PriponaTitulku = ".trsx";
            PriponaDatabazeMluvcich = ".xml";


            BarvaTextBoxuOdstavce = Brushes.AliceBlue;
            BarvaTextBoxuOdstavceAktualni = Brushes.AntiqueWhite;
            BarvaTextBoxuFoneticky = Brushes.AliceBlue;
            BarvaTextBoxuFonetickyZakazany = Brushes.LightGray;
        }

        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serializovat(String jmenoSouboru, MySetup co)
        {
            try
            {
                if (!File.Exists(jmenoSouboru))
                    Directory.CreateDirectory(Path.GetDirectoryName(jmenoSouboru));

                XmlSerializer serializer = new XmlSerializer(typeof(MySetup));
                TextWriter writer = new StreamWriter(jmenoSouboru);
                //XmlTextWriter writer = new XmlTextWriter(jmenoSouboru, Encoding.UTF8);
                
                serializer.Serialize(writer, co);
                writer.Close();
                return true;
            }
            catch //(Exception ex)
            {
                //MessageBox.Show("Chyba pri serializaci konfiguračního souboru: " + ex.Message);
                return false;
            }

        }

        //Deserializuje soubor             
        public MySetup Deserializovat(String jmenoSouboru)
        {
            try
            {
                if (!new FileInfo(jmenoSouboru).Exists) return this;
                XmlSerializer serializer = new XmlSerializer(typeof(MySetup));
                MySetup md;

                XmlTextReader xreader = new XmlTextReader(jmenoSouboru);
                md = (MySetup)serializer.Deserialize(xreader);
                xreader.Close();
                if (md == null) return this;
                return md;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri derializaci konfiguračního souboru souboru: " + ex.Message);
                return this;
            }

        }


    }
        
}
