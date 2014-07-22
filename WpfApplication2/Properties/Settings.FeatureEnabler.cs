using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans.Properties
{
    public sealed partial class Settings
    {

        private Features _FeatureEnabler = null;

        public Features FeatureEnabler
        {
            get 
            {
                if (_FeatureEnabler == null)
                    _FeatureEnabler = new Features(this);
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


            public bool VideoFrame
            {
                get { return _VideoFrame; }
                set
                {
                    _VideoFrame = value;
                    OnPropertyChanged();
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

            Settings _parent;
            internal Features( Settings parent)
            {
                _parent = parent;
            }


            private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName]string caller = null)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }



            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
