using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans.Properties
{
    internal sealed partial class Settings
    {

        private Features _FeatureEnabler = null;

        public Features FeatureEnabler
        {
            get
            {
                _FeatureEnabler ??= new Features(this);
                return _FeatureEnabler;
            }
        }

        public class Features : INotifyPropertyChanged
        {
            bool _VideoFrame = true;
            bool _PhoneticEditation = true;


            bool _localSpeakers = true;


            bool _localEdit = true;

            bool _quickExport = true;
            bool _quickNavigation = true;

            bool _dbMerging = true;

            bool _audioManipulation = true;

            bool _ChaptersAndSections = true;

            bool _nonSpeechEvents = true;


            bool _spellchecking = true;

            bool _export = true;

            bool _SpeakerAttributes = true;

            public bool SpeakerAttributes
            {
                get { return _SpeakerAttributes; }
                set { _SpeakerAttributes = value; }
            }


            public bool Export
            {
                get { return _export; }
                set
                {
                    _export = value;
                    OnPropertyChanged();
                }
            }

            public bool Spellchecking
            {
                get { return _spellchecking; }
                set
                {
                    _spellchecking = value;
                    OnPropertyChanged();
                }
            }

            public bool NonSpeechEvents
            {
                get { return _nonSpeechEvents; }
                set
                {
                    _nonSpeechEvents = value;
                    OnPropertyChanged();
                }
            }



            public bool DbMerging
            {
                get { return _dbMerging; }
                set
                {
                    _dbMerging = value;
                    OnPropertyChanged();
                }
            }


            public bool QuickNavigation
            {
                get { return _quickNavigation; }
                set
                {
                    _quickNavigation = value;
                    OnPropertyChanged();
                }
            }

            public bool QuickExport
            {
                get { return _quickExport; }
                set
                {
                    _quickExport = value;
                    OnPropertyChanged();
                }
            }

            public bool ChaptersAndSections
            {
                get { return _ChaptersAndSections; }
                set
                {
                    _ChaptersAndSections = value;
                    OnPropertyChanged();
                }
            }

            public bool AudioManipulation
            {
                get { return _audioManipulation; }
                set
                {
                    _audioManipulation = value;
                    OnPropertyChanged();
                }
            }

            public bool VideoFrame
            {
                get { return _VideoFrame; }
                set
                {
                    _VideoFrame = value;
                    OnPropertyChanged();
                    _parent.OnPropertyChanged("VideoPanelVisible");
                    _parent.OnPropertyChanged("VideoPanelWidth");
                }
            }

            public bool PhoneticEditation
            {
                get { return _PhoneticEditation; }
                set
                {
                    _PhoneticEditation = value;
                    OnPropertyChanged();
                    _parent.OnPropertyChanged("PhoneticsPanelVisible");
                }
            }




            public bool LocalSpeakers
            {
                get { return _localSpeakers; }
                set
                {
                    _localSpeakers = value;
                    OnPropertyChanged();
                }
            }

            public bool LocalEdit
            {
                get { return _localEdit; }
                set
                {
                    _localEdit = value;
                    OnPropertyChanged();
                }
            }

            readonly Settings _parent;
            internal Features(Settings parent)
            {
                _parent = parent;
            }



            public event EventHandler FeaturesChanged;

            private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string caller = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));

                FeaturesChanged?.Invoke(this, null);

            }



            public event PropertyChangedEventHandler PropertyChanged;

        }
    }
}
