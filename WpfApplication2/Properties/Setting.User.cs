using TranscriptionCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPFLocalizeExtension.Engine;

namespace NanoTrans.Properties
{
    internal sealed partial class Settings
    {

        #region property changed - 

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string caller = null)
        {
            base.OnPropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(caller));
        }
        #endregion

        #region colors

        readonly ColorConverter cocon = new ColorConverter();

        SolidColorBrush _ParagraphBackground = null;
        public SolidColorBrush ParagraphBackground
        {
            get
            {
                _ParagraphBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.ParagraphBackgroundStore));

                return _ParagraphBackground;
            }
            set
            {
                _ParagraphBackground = value;
                ParagraphBackgroundStore = cocon.ConvertToString(_ParagraphBackground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _SectionBackground;
        public SolidColorBrush SectionBackground
        {
            get
            {
                _SectionBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.SectionBackgroundStore));
                return _SectionBackground;
            }
            set
            {
                _SectionBackground = value;
                SectionBackgroundStore = cocon.ConvertToString(_SectionBackground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _ChapterBackground;
        public SolidColorBrush ChapterBackground
        {
            get
            {
                _ChapterBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.ChapterBackgroundStore));

                return _ChapterBackground;
            }
            set
            {
                _ChapterBackground = value;
                ChapterBackgroundStore = cocon.ConvertToString(_ChapterBackground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _ActiveParagraphBackground;
        public SolidColorBrush ActiveParagraphBackground
        {
            get
            {
                _ActiveParagraphBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.ActiveParagraphBackgroundStore));

                return _ActiveParagraphBackground;
            }
            set
            {
                _ActiveParagraphBackground = value;
                ActiveParagraphBackgroundStore = cocon.ConvertToString(_ActiveParagraphBackground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _PhoneticParagraphBackground;
        public SolidColorBrush PhoneticParagraphBackground
        {
            get
            {
                _PhoneticParagraphBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.PhoneticParagraphBackgroundStore));

                return _PhoneticParagraphBackground;
            }
            set
            {
                _PhoneticParagraphBackground = value;
                PhoneticParagraphBackgroundStore = cocon.ConvertToString(_PhoneticParagraphBackground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _PhoneticParagraphDisabledBackground;
        public SolidColorBrush PhoneticParagraphDisabledBackground
        {
            get
            {
                _PhoneticParagraphDisabledBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.PhoneticParagraphDisabledBackgroundStore));

                return _PhoneticParagraphDisabledBackground;
            }
            set
            {
                _PhoneticParagraphDisabledBackground = value;
                PhoneticParagraphDisabledBackgroundStore = cocon.ConvertToString(_PhoneticParagraphDisabledBackground.Color);
                OnPropertyChanged();
            }
        }

        #endregion

        public double SlowedPlaybackSpeed
        {
            get
            {
                return this.SlowedPlaybackSpeedStore;
            }
            set
            {
                SlowedPlaybackSpeedStore = value;
                OnPropertyChanged();
            }
        }
        public TimeSpan WaveformSmallJump
        {
            get
            {
                return this.WaveformSmallJumpStore;
            }
            set
            {
                WaveformSmallJumpStore = value;
                OnPropertyChanged();
            }
        }

        string[] _NonSpeechEvents = null;
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

            private set
            {
                _NonSpeechEvents = value;
                OnPropertyChanged();
            }
        }

        string[] _SpeakerAttributteCategories = null;
        public string[] SpeakerAttributteCategories
        {
            get
            {
                if (_SpeakerAttributteCategories == null)
                {
                    _SpeakerAttributteCategories = NanoTrans.Properties.Strings.GlobalDefaultSpeakerAttributes.Split(',').Concat(NanoTrans.Properties.Settings.Default.AdditionalSpeakerAttributeCategories.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
                }

                return _SpeakerAttributteCategories;
            }

            private set
            {
                _SpeakerAttributteCategories = value;
                OnPropertyChanged();
            }
        }


        public double SetupTextFontSize
        {
            get { return SetupTextFontSizeStore; }
            set
            {
                SetupTextFontSizeStore = value;
                OnPropertyChanged();
                OnPropertyChanged("SetupOthersFontSize");
            }
        }

        public double SetupOthersFontSize
        {
            get { return SetupTextFontSize * 0.87; }
        }


        public string SpeakersDatabasePath
        {
            get
            {
                if (!Path.IsPathRooted(SpeakersDatabasePathStore))
                    return Path.Combine(FilePaths.ProgramDirectory, SpeakersDatabasePathStore);
                else
                    return SpeakersDatabasePathStore;
            }
            set
            {
                SpeakersDatabasePathStore = value;
            }
        }






        public bool SaveWholeSpeaker
        {
            get
            {
                return SaveWholeSpeakerStore;
            }

            set
            {
                SaveWholeSpeakerStore = value;
                OnPropertyChanged();
            }
        }

        public bool VideoPanelVisible
        {
            get
            {
                return VideoPanelVisibleStore && FeatureEnabler.VideoFrame;
            }

            set
            {
                VideoPanelVisibleStore = value;
                OnPropertyChanged();
                OnPropertyChanged("VideoPanelWidth");
            }
        }

        public double VideoPanelWidth
        {
            get
            {
                return VideoPanelVisible ? VideoPanelWidthStore : 0;
            }

            set
            {
                VideoPanelWidthStore = value;
                OnPropertyChanged();
            }
        }

        public bool PhoneticsPanelVisible
        {
            get
            {
                return PhoneticsPanelVisibleStore && FeatureEnabler.PhoneticEditation;
            }

            set
            {
                PhoneticsPanelVisibleStore = value;
                OnPropertyChanged();
            }
        }


        public System.Windows.Point WindowsPosition
        {
            get
            {
                return WindowsPositionStore;
            }

            set
            {
                WindowsPositionStore = value;
                OnPropertyChanged();
            }
        }


        public System.Windows.Size WindowSize
        {
            get
            {
                return WindowSizeStore;
            }

            set
            {
                WindowSizeStore = value;
                OnPropertyChanged();
            }
        }

        public System.Windows.WindowState WindowState
        {
            get
            {
                return WindowStateStore;
            }

            set
            {
                WindowStateStore = value;
                OnPropertyChanged();
            }
        }


        public bool ShowSpeakerImage
        {
            get
            {
                return ShowSpeakerImageStore;
            }

            set
            {
                ShowSpeakerImageStore = value;
                OnPropertyChanged();
            }
        }

        public double MaxSpeakerImageWidth
        {
            get
            {
                return MaxSpeakerImageWidthStore;
            }

            set
            {
                MaxSpeakerImageWidthStore = value;
                OnPropertyChanged();
            }
        }

        public bool ShowCustomParams
        {
            get
            {
                return ShowCustomParamsStore;
            }

            set
            {
                ShowCustomParamsStore = value;
                OnPropertyChanged();
            }
        }


        public int OutputDeviceIndex
        {
            get
            {
                return OutputDeviceIndexStore;
            }

            set
            {
                OutputDeviceIndexStore = value;
                OnPropertyChanged();
            }
        }





        public Settings()
        {

            if (string.IsNullOrEmpty(SpeakersDatabasePathStore))
                SpeakersDatabasePathStore = FilePaths.GetDefaultSpeakersPath();
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
                OnPropertyChanged();
            }
        }

        void LocalizationInstance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SpeakerAttributteCategories = null;
            NonSpeechEvents = null;
        }

        public Brush GetPAttributeBgColor(ParagraphAttributes param)
        {
            return Brushes.White;
        }

        public Brush GetPAttributeColor(ParagraphAttributes param)
        {
            return param switch
            {
                ParagraphAttributes.Background_noise => Brushes.DodgerBlue,
                ParagraphAttributes.Background_speech => Brushes.Chocolate,
                ParagraphAttributes.Junk => Brushes.Crimson,
                ParagraphAttributes.Narrowband => Brushes.Olive,
                _ => Brushes.White,
            };
        }


        SolidColorBrush _WaveformBackground;
        public SolidColorBrush WaveformBackground
        {
            get
            {
                _WaveformBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.WaveformBackgroundStore));
                return _WaveformBackground;
            }
            set
            {
                _WaveformBackground = value;
                WaveformBackgroundStore = cocon.ConvertToString(_WaveformBackground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _WaveformForeground;
        public SolidColorBrush WaveformForeground
        {
            get
            {
                _WaveformForeground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.WaveformForegroundStore));
                return _WaveformForeground;
            }
            set
            {
                _WaveformForeground = value;
                WaveformForegroundStore = cocon.ConvertToString(_WaveformForeground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _WaveformTimelineBackground;
        public SolidColorBrush WaveformTimelineBackground
        {
            get
            {
                _WaveformTimelineBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.WaveformTimelineBackgroundStore));
                return _WaveformTimelineBackground;
            }
            set
            {
                _WaveformTimelineBackground = value;
                WaveformTimelineBackgroundStore = cocon.ConvertToString(_WaveformTimelineBackground.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _WaveformSpeakerBackground;
        public SolidColorBrush WaveformSpeakerBackground
        {
            get
            {
                _WaveformSpeakerBackground ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.WaveformSpeakerBackgroundStore));
                return _WaveformSpeakerBackground;
            }
            set
            {
                _WaveformSpeakerBackground = value;
                WaveformSpeakerBackgroundStore = cocon.ConvertToString(_WaveformSpeakerBackground.Color);
                OnPropertyChanged();
            }
        }


        SolidColorBrush _ParagraphBegin;
        public SolidColorBrush ParagraphBeginColor
        {
            get
            {
                _ParagraphBegin ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.ParagraphBeginColorStore));
                return _ParagraphBegin;
            }
            set
            {
                _ParagraphBegin = value;
                ParagraphBeginColorStore = cocon.ConvertToString(_ParagraphBegin.Color);
                OnPropertyChanged();
            }
        }



        SolidColorBrush _ParagraphEnd;
        public SolidColorBrush ParagraphEndColor
        {
            get
            {
                _ParagraphEnd ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.ParagraphEndColorStore));
                return _ParagraphEnd;
            }
            set
            {
                _ParagraphEnd = value;
                ParagraphEndColorStore = cocon.ConvertToString(_ParagraphEnd.Color);
                OnPropertyChanged();
            }
        }

        SolidColorBrush _WaveformBlockMarkColorStore;
        public SolidColorBrush WaveformBlockMarkColor
        {
            get
            {
                _WaveformBlockMarkColorStore ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(this.WaveformBlockMarkColorStore));
                return _WaveformBlockMarkColorStore;
            }
            set
            {
                _WaveformBlockMarkColorStore = value;
                WaveformBlockMarkColorStore = cocon.ConvertToString(_WaveformBlockMarkColorStore.Color);
                OnPropertyChanged();
            }
        }
    }
}
