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

    [XmlInclude(typeof(MySpeaker))]
    public class MySetup : System.ComponentModel.INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion 

        static MySetup()
        {
            m_setup = new MySetup();
            m_refresher = new SingletonRefresher() { Setup = m_setup };
        }
        private static MySetup m_setup;
        public static MySetup Setup
        {
            set
            {
                m_setup = value;
                m_refresher.Setup = value;
            }
            get 
            {
                if (m_setup == null)
                    m_setup = new MySetup();
                return m_setup; 
            }
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
        public Brush BarvaTextBoxuSekce { get; set; }

        [XmlIgnore]
        public Brush BarvaTextBoxuKapitoly { get; set; }

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
            get { return m_SetupTextFontSize; }
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

        private string m_CestaDatabazeMluvcich;
        public string CestaDatabazeMluvcich
        {
            get
            {
                if (!Path.IsPathRooted(m_CestaDatabazeMluvcich))
                    return Path.Combine(FilePaths.ProgramDirectory,m_CestaDatabazeMluvcich);
                else
                    return m_CestaDatabazeMluvcich;
            }
            set
            {
                m_CestaDatabazeMluvcich = value;
            }
        }


        public bool SaveInShortFormat { get; set; }
        /// <summary>
        /// info zda je k prepisu ukladan komplet mluvci vcetne obrazku
        /// </summary>
        public bool UkladatKompletnihoMluvciho { get; set; }

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
        /// jazyk programu
        /// </summary>
        public MyEnumJazyk jazykRozhranni;



        public void NastavDefaultHodnoty()
        {
            jazykRozhranni = MyEnumJazyk.anglictina;

            audio.VystupniZarizeniIndex = 0;
            audio.VstupniZarizeniIndex = 0;

            CestaDatabazeMluvcich = "Data\\DatabazeMluvcich.xml";

            SetupTextFontSize = 13;

            ZobrazitFotografieMluvcich = true;
            Fotografie_VyskaMax = 50;

            ZobrazitFonetickyPrepis = 100;
            OknoPozice = new Point(-1, -1);
            OknoVelikost = new Size(800, 600);
            OknoStav = WindowState.Normal;
            ZpomalenePrehravaniRychlost = 0.8;
            VlnaMalySkok  = 5;
            NerecoveUdalosti = new[] { "kasel", "ehm", "smich", "ticho", "nadech", "hluk", "hudba", "mlask" };
            
        }


        public MySetup()
        {
            
            NastavDefaultHodnoty();
            PriponaTitulku = ".trsx";
            PriponaDatabazeMluvcich = ".xml";
            UkladatKompletnihoMluvciho = false;
            SaveInShortFormat = false;

            BarvaTextBoxuOdstavce = Brushes.AliceBlue;
            BarvaTextBoxuOdstavceAktualni = Brushes.AntiqueWhite;
            BarvaTextBoxuFoneticky = Brushes.AliceBlue;
            BarvaTextBoxuFonetickyZakazany = Brushes.LightGray;
            BarvaTextBoxuSekce = Brushes.LightGreen;
            BarvaTextBoxuKapitoly = Brushes.LightPink;
        }


        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serializovat(string filename, MySetup co)
        {
            if (!File.Exists(filename))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

            using (var s = File.Open(filename, FileMode.Create, FileAccess.ReadWrite))
                return Serializovat(s, co);
            
        }

        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serializovat(Stream filestream, MySetup co)
        {
            try
            {
                

                XmlSerializer serializer = new XmlSerializer(typeof(MySetup));
                TextWriter writer = new StreamWriter(filestream);
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


        public MySetup Deserializovat(String jmenoSouboru)
        { 
            if (!File.Exists(jmenoSouboru))
                return this;
            using (var s = File.Open(jmenoSouboru,FileMode.Open,FileAccess.Read))
                return Deserializovat(s);
        }

        //Deserializuje soubor             
        public MySetup Deserializovat(Stream filestream)
        {
            try
            {
                
                XmlSerializer serializer = new XmlSerializer(typeof(MySetup));
                MySetup md;

                XmlTextReader xreader = new XmlTextReader(filestream);
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
