using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans
{
    public class SpeakerContainer : INotifyPropertyChanged
    {
        public SpeakerCollection SpeakerColletion;
        string _degreeAfter = null;
        string _degreeBefore = null;
        string _firstName = null;
        bool _changed = false;
        string _imgBase64 = null;
        bool _isLoading = false;
        string _language;
        bool _marked = false;
        string _secondName = null;
        Speaker.Sexes? _sex;
        Speaker _speaker;
        string _surName = null;
        bool _updating = false;

        public SpeakerContainer(Speaker s)
            : this(null, s)
        {
        }

        public SpeakerContainer(SpeakerCollection speakers, Speaker s)
        {
            _speaker = s;
            this.SpeakerColletion = speakers;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ReadOnlyCollection<SpeakerAttributeContainer> Attributes
        {
            get { return Speaker.Attributes.Select(a => new SpeakerAttributeContainer(a)).ToList().AsReadOnly(); }
        }

        public string DegreeAfter
        {
            get
            {
                return _degreeAfter ?? _speaker.DegreeAfter ?? "";
            }

            set
            {
                _speaker.DegreeAfter = _degreeAfter = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DegreeAfter"));
            }
        }

        public string DegreeBefore
        {
            get
            {
                return _degreeBefore ?? _speaker.DegreeBefore ?? "";
            }

            set
            {
                _speaker.DegreeBefore = _degreeBefore = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DegreeBefore"));
            }
        }

        public string FirstName
        {
            get
            {
                return _firstName ?? _speaker.FirstName;
            }

            set
            {
                _speaker.FirstName = _firstName = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FirstName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        public string FullName
        {
            get { return _speaker.FullName; }
        }

        public bool Changed
        {
            get { return _changed; }
            set
            {
                _changed = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Changed"));
            }
        }

        public string ImgBase64
        {
            get
            {
                return _imgBase64 ?? _speaker.ImgBase64;
            }

            set
            {
                _speaker.ImgBase64 = _imgBase64 = value;
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ImgBase64"));
            }
        }

        public bool IsDocument
        {
            get { return Speaker.DataBaseType == DBType.File; }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsLoading"));
            }
        }

        public bool IsLocal
        {
            get { return Speaker.DataBaseType == DBType.User; }
        }

        public bool IsOnline
        {
            get { return Speaker.DataBaseType == DBType.Api; }
        }

        public bool IsOffline
        {
            get { return !IsOnline; }
        }

        public string Language
        {
            get
            {
                return _language ?? _speaker.DefaultLang;
            }

            set
            {
                _speaker.DefaultLang = _language = value;
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Language"));
            }
        }

        public bool Marked
        {
            get
            {
                return _marked;
            }
            set
            {
                _marked = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Marked"));
            }
        }

        public bool PinnedToDocument
        {
            get
            {
                return _speaker.PinnedToDocument;
            }

            set
            {

                _speaker.PinnedToDocument = value;
                Changed = true;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("PinnedToDocument"));
            }
        }

        //Attributes
        public string SecondName
        {
            get
            {
                return _secondName ?? _speaker.MiddleName;
            }

            set
            {
                _speaker.MiddleName = _secondName = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SecondName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        public Speaker.Sexes Sex
        {
            get
            {
                return _sex ?? Speaker.Sex;
            }

            set
            {
                _sex = Speaker.Sex = value;
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Sex"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        public Speaker Speaker
        {
            get
            {
                return _speaker;
            }
        }

        public string SurName
        {
            get
            {
                return _surName ?? _speaker.Surname;
            }

            set
            {
                _speaker.Surname = _surName = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SurName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        public bool Updating
        {
            get { return _updating; }
            set
            {
                _updating = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Updating"));
            }
        }
        //Attributes
        public void RefreshAttributes()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Attributes"));
        }

        public void UpdateBindings()
        {

            _degreeAfter = null;
            _degreeBefore = null;
            _firstName = null;
            _changed = false;
            _imgBase64 = null;
            _isLoading = false;
            _language = null;
            _secondName = null;
            _sex = null;
            _surName = null;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(null));

        }
    }

}
