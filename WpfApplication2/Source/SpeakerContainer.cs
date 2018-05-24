using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranscriptionCore;

namespace NanoTrans
{
    [System.Diagnostics.DebuggerDisplay("Speaker container ({FullName})")]
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
        string _middleName = null;
        Speaker.Sexes? _sex;
        Speaker _speaker;
        string _surName = null;
        bool _updating = false;
        bool? _pinned = null;

        public SpeakerContainer(Speaker s)
            : this(null, s)
        {
        }

        public SpeakerContainer(SpeakerCollection speakers, Speaker s)
        {
            _speaker = s;
            this.SpeakerColletion = speakers;

            _OriginalAttributes = s.Attributes.Select(a => new SpeakerAttribute(a)).ToList(); ;
        }




        public event PropertyChangedEventHandler PropertyChanged;


        public void ApplyChanges()
        {
            DegreeAfter = DegreeAfter.Trim();
            DegreeBefore = DegreeBefore.Trim();
            FirstName = FirstName.Trim();
            MiddleName = MiddleName.Trim();
            SurName = SurName.Trim();

            if (_degreeAfter != null)
                _speaker.DegreeAfter = _degreeAfter;
            if (_degreeBefore != null)
                _speaker.DegreeBefore = _degreeBefore;
            if (_firstName != null)
                _speaker.FirstName = _firstName;
            if (_imgBase64 != null)
                _speaker.ImgBase64 = _imgBase64;
            if (_language != null)
                _speaker.DefaultLang = _language;
            if (_pinned != null)
                _speaker.PinnedToDocument = _pinned.Value;
            if (_middleName != null)
                _speaker.MiddleName = _middleName;
            if (_sex != null)
                _speaker.Sex = _sex.Value;
            if (_surName != null)
                _speaker.Surname = _surName;
            if (_pinned != null)
                _speaker.PinnedToDocument = _pinned.Value;


            foreach (var att in _RemovedAttributes)
                _speaker.Attributes.Remove(att);

            _speaker.Attributes.AddRange(_AddedAttributes);

            _OriginalAttributes = _speaker.Attributes.Select(a => new SpeakerAttribute(a)).ToList(); ;
            //changes should be discarded after applying 
            DiscardChanges();

            Changed = false;
            New = false;
        }


        public void DiscardChanges()
        {
            _degreeAfter = null;
            _degreeBefore = null;
            _firstName = null;
            _changed = false;
            _imgBase64 = null;
            _isLoading = false;
            _language = null;
            _middleName = null;
            _sex = null;
            _surName = null;
            _pinned = null;

            _speaker.Attributes = _OriginalAttributes.ToList();

            _AddedAttributes.Clear();
            _RemovedAttributes.Clear();

        }

        public string DegreeAfter
        {
            get
            {
                return _degreeAfter ?? _speaker.DegreeAfter ?? "";
            }

            set
            {
                _degreeAfter = (value ?? "");
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
                _degreeBefore = (value ?? "");
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DegreeBefore"));
            }
        }

        public string FirstName
        {
            get
            {
                return _firstName ?? _speaker.FirstName ?? "";
            }

            set
            {
                _firstName = (value ?? "");
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FirstName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        public string FullName
        {
            get { return Speaker.GetFullName(FirstName, MiddleName, SurName); }
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
                _imgBase64 = value;
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
                _language = value;
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
                return _pinned ?? _speaker.PinnedToDocument;
            }

            set
            {
                _pinned = value;
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("PinnedToDocument"));
            }
        }

        //Attributes
        public string MiddleName
        {
            get
            {
                return _middleName ?? _speaker.MiddleName ?? "";
            }

            set
            {
                _middleName = (value ?? "");
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("MiddleName"));
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
                _sex = value;
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
                return _surName ?? _speaker.Surname ?? "";
            }

            set
            {
                _surName = (value ?? "");
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

        public void ReloadSpeaker()
        {
            DiscardChanges();
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(null));

        }


        public bool New { get; set; }

        public ReadOnlyCollection<SpeakerAttributeContainer> Attributes
        {
            get { return Speaker.Attributes.Except(_RemovedAttributes).Concat(_AddedAttributes).Select(a => new SpeakerAttributeContainer(a)).ToList().AsReadOnly(); }
        }

        List<SpeakerAttribute> _AddedAttributes = new List<SpeakerAttribute>();
        internal void AttributesAdd(SpeakerAttribute sa)
        {
            _AddedAttributes.Add(sa);
            RefreshAttributes();
            Changed = true;
        }

        List<SpeakerAttribute> _RemovedAttributes = new List<SpeakerAttribute>();
        internal void AttributesRemove(SpeakerAttribute speakerAttribute)
        {
            _RemovedAttributes.Add(speakerAttribute);
            RefreshAttributes();
            Changed = true;
        }

        List<SpeakerAttribute> _OriginalAttributes = new List<SpeakerAttribute>();

    }

}
