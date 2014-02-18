using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Windows;
using NanoTrans.Core;
using WPFLocalizeExtension.Engine;

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


        string[] _NonSpeechEvents = null;
        
        [XmlIgnore]
        public string[] NonSpeechEvents 
        {
            get
            {
                if (_NonSpeechEvents == null)
                {
                    _NonSpeechEvents = NanoTrans.Properties.Strings.GlobalNonSpeechEvents.Split(',');
                }

                return _NonSpeechEvents;
            }

            set 
            {
                _NonSpeechEvents = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("NonSpeechEvents"));
            }
        }

        string[] _SpeakerAtributteCategories = null;
        [XmlIgnore]
        public string[] SpeakerAtributteCategories 
        {
            get
            {
                if (_SpeakerAtributteCategories == null)
                {
                    _SpeakerAtributteCategories = NanoTrans.Properties.Strings.GlobalDefaultSpeakerAttributes.Split(',');
                }

                return _SpeakerAtributteCategories;
            }

            set
            {
                _SpeakerAtributteCategories = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("SpeakerAtributteCategories"));
            }
        }



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
        public string SpeakersDatabasePath
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

        /// <summary>
        /// Save whole speaker (with image) into trsxes
        /// </summary>
        public bool SaveWholeSpeaker { get; set; }

        /// <summary>
        /// height of phonetic panel.  (negative numbers hides the panel)
        /// </summary>
        public float PhoneticsPanelHeight;

        /// <summary>
        /// last window position
        /// </summary>
        public System.Windows.Point WindowsPosition;

        /// <summary>
        /// last window size
        /// </summary>
        public System.Windows.Size WindowSize;

        /// <summary>
        /// window state (maximized, minimized..)
        /// </summary>
        public WindowState WindowState;


        public AudioDevices audio;


        public bool ShowSpeakerImage { get; set; }

        public double MaxSpeakerImageWidth { get; set; }




        public void Initialize()
        {

            audio.OutputDeviceIndex = 0;
            audio.InputDeviceIndex = 0;

            SpeakersDatabasePath = FilePaths.GetDefaultSpeakersPath();

            SetupTextFontSize = 13;

            ShowSpeakerImage = true;
            MaxSpeakerImageWidth = 50;

            PhoneticsPanelHeight = 100;
            WindowsPosition = new Point(-1, -1);
            WindowSize = new Size(800, 600);
            WindowState = WindowState.Normal;
            SlowedPlaybackSpeed = 0.8;
            WaveformSmallJump  = 5;
            //NonSpeechEvents = new[] { "kasel", "ehm", "smich", "ticho", "nadech", "hluk", "hudba", "mlask" };
            //SpeakerAtributteCategories = new[] {"Komentář","Rodinný stav", "Národnost", "Zaměstnání"  };
            
        }


        public GlobalSetup()
        {
            
            Initialize();
            SaveWholeSpeaker = false;

            ParagraphBackground = Brushes.AliceBlue;
            ActiveParagraphBackground = Brushes.AntiqueWhite;
            PhoneticParagraphBackground = Brushes.AliceBlue;
            PhoneticParagraphDisabledBackground = Brushes.LightGray;
            SectionBackground = Brushes.LightGreen;
            ChapterBackground = Brushes.LightPink;

            LocalizeDictionary.Instance.PropertyChanged += LocalizationInstance_PropertyChanged;
        }

        string _Locale = null;
        public string Locale
        {
            get
            {
                return _Locale;
            }

            set
            {
                _Locale = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("Locale"));
            }
        }

        void LocalizationInstance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SpeakerAtributteCategories = null;
            NonSpeechEvents = null;
        }

        public bool Serialize(string filename, GlobalSetup co)
        {
            if (!File.Exists(filename))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

            using (var s = File.Open(filename, FileMode.Create, FileAccess.ReadWrite))
                return Serialize(s, co);
            
        }

        public bool Serialize(Stream filestream, GlobalSetup co)
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


        public GlobalSetup Deserialize(String jmenoSouboru)
        { 
            if (!File.Exists(jmenoSouboru))
                return this;
            using (var s = File.Open(jmenoSouboru,FileMode.Open,FileAccess.Read))
                return Deserialize(s);
        }

          
        public GlobalSetup Deserialize(Stream filestream)
        {
            try
            {
                
                XmlSerializer serializer = new XmlSerializer(typeof(GlobalSetup));
                GlobalSetup md;

                XmlTextReader xreader = new XmlTextReader(filestream);
                md = (GlobalSetup)serializer.Deserialize(xreader);
                xreader.Close();
                if (md == null) 
                    return this;
                return md;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Strings.MessageBoxConfigFileDeserializationError + ex.Message);
                return this;
            }

        }



        public bool ShowCustomParams { get; set; }


        public Brush GetPAttributeBgColor(ParagraphAttributes param)
        {
            return Brushes.White;
        }

        public Brush GetPAttributeColor(ParagraphAttributes param)
        {
            switch (param)
            {
                default:
                case ParagraphAttributes.None:
                    return Brushes.White;
                case ParagraphAttributes.Background_noise:
                    return Brushes.DodgerBlue;
                case ParagraphAttributes.Background_speech:
                    return Brushes.Chocolate;
                case ParagraphAttributes.Junk:
                    return Brushes.Crimson;
                case ParagraphAttributes.Narrowband:
                    return Brushes.Olive;
            }
        }

    }

}
