using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Windows;

namespace NanoTrans
{

    public struct AudioDevices
    {

        public int OutputDeviceIndex;

        public int InputDeviceIndex;

    }

    public class SingletonRefresher : System.ComponentModel.INotifyPropertyChanged
    {
        private GlobalSetup _setup;
        public GlobalSetup Setup
        {
            get { return _setup; }
            set
            {
                _setup = value;
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


    //TODO: modify properties to be bindable
    //TODO: ensure using of defined values from whole project
    public class GlobalSetup : System.ComponentModel.INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion 

        static GlobalSetup()
        {
            _setup = new GlobalSetup();
            _refresher = new SingletonRefresher() { Setup = _setup };
        }
        private static GlobalSetup _setup;
        public static GlobalSetup Setup
        {
            set
            {
                _setup = value;
                _refresher.Setup = value;
            }
            get 
            {
                if (_setup == null)
                    _setup = new GlobalSetup();
                return _setup; 
            }
        }


        static SingletonRefresher _refresher = null;
        //hack original source modified whole setup property, so i cannot bind to it
        public static SingletonRefresher Refresher
        {
            get
            {
                return _refresher;
            }
        }



        Brush _ParagraphBackground;
        [XmlIgnore]
        public Brush ParagraphBackground
        {
            get { return _ParagraphBackground; }
            set
            {
                _ParagraphBackground = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("ParagraphBackground"));
            }
        }



        [XmlIgnore]
        public Brush SectionBackground { get; set; }

        [XmlIgnore]
        public Brush ChapterBackground { get; set; }

        [XmlIgnore]
        public Brush ActiveParagraphBackground { get; set; }

        [XmlIgnore]
        public Brush PhoneticParagraphBackground { get; set; }

        [XmlIgnore]
        public Brush PhoneticParagraphDisabledBackground { get; set; }


        public double SlowedPlaybackSpeed { get; set; }
        public double WaveformSmallJump { get; set; } //delka maleho skoku na vlne
        [XmlIgnore]
        public string[] NonSpeechEvents { get; set; }

        double _SetupTextFontSize;
        public double SetupTextFontSize //udava velikost pisma v textboxech   
        {
            get { return _SetupTextFontSize; }
            set
            {
                _SetupTextFontSize = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("SetupTextFontSize"));
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("SetupOthersFontSize"));
                }
            }
        }

        public double SetupOthersFontSize
        {
            get { return _SetupTextFontSize * 0.87; }
        }


        private string _CestaDatabazeMluvcich;
        public string CestaDatabazeMluvcich
        {
            get
            {
                if (!Path.IsPathRooted(_CestaDatabazeMluvcich))
                    return Path.Combine(FilePaths.ProgramDirectory,_CestaDatabazeMluvcich);
                else
                    return _CestaDatabazeMluvcich;
            }
            set
            {
                _CestaDatabazeMluvcich = value;
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


        public AudioDevices audio;

        /// <summary>
        /// info zda jsou zobrazeny fotografie mluvcich v prepisu
        /// </summary>
        public bool ZobrazitFotografieMluvcich { get; set; }

        /// <summary>
        /// maximalni vyska u fotky pri zobrazeni v prepisu
        /// </summary>
        public double Fotografie_VyskaMax { get; set; }


        public string[] SpeakerAtributteCategories{ get; set; }

        public void NastavDefaultHodnoty()
        {

            audio.OutputDeviceIndex = 0;
            audio.InputDeviceIndex = 0;

            CestaDatabazeMluvcich = "Data\\SpeakersDatabase.xml";

            SetupTextFontSize = 13;

            ZobrazitFotografieMluvcich = true;
            Fotografie_VyskaMax = 50;

            ZobrazitFonetickyPrepis = 100;
            OknoPozice = new Point(-1, -1);
            OknoVelikost = new Size(800, 600);
            OknoStav = WindowState.Normal;
            SlowedPlaybackSpeed = 0.8;
            WaveformSmallJump  = 5;
            NonSpeechEvents = new[] { "kasel", "ehm", "smich", "ticho", "nadech", "hluk", "hudba", "mlask" };
            SpeakerAtributteCategories = new[] {"Komentář","Rodinný stav", "Národnost", "Zaměstnání"  };
            
        }


        public GlobalSetup()
        {
            
            NastavDefaultHodnoty();
            UkladatKompletnihoMluvciho = false;
            SaveInShortFormat = true;

            ParagraphBackground = Brushes.AliceBlue;
            ActiveParagraphBackground = Brushes.AntiqueWhite;
            PhoneticParagraphBackground = Brushes.AliceBlue;
            PhoneticParagraphDisabledBackground = Brushes.LightGray;
            SectionBackground = Brushes.LightGreen;
            ChapterBackground = Brushes.LightPink;
        }


        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serializovat(string filename, GlobalSetup co)
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
        public bool Serializovat(Stream filestream, GlobalSetup co)
        {
            try
            {
                

                XmlSerializer serializer = new XmlSerializer(typeof(GlobalSetup));
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


        public GlobalSetup Deserializovat(String jmenoSouboru)
        { 
            if (!File.Exists(jmenoSouboru))
                return this;
            using (var s = File.Open(jmenoSouboru,FileMode.Open,FileAccess.Read))
                return Deserializovat(s);
        }

        //Deserializuje soubor             
        public GlobalSetup Deserializovat(Stream filestream)
        {
            try
            {
                
                XmlSerializer serializer = new XmlSerializer(typeof(GlobalSetup));
                GlobalSetup md;

                XmlTextReader xreader = new XmlTextReader(filestream);
                md = (GlobalSetup)serializer.Deserialize(xreader);
                xreader.Close();
                if (md == null) return this;
                return md;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Strings.MessageBoxConfigFileDeserializationError + ex.Message);
                return this;
            }

        }


    }

}
