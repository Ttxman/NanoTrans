using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TranscriptionCore
{
    /// <summary>
    /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
    /// </summary>
    /// <returns></returns>
    public class SpeakerCollection:IList<Speaker>
    {
        protected string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { _fileName  = value; }
        }

        protected List<Speaker> _Speakers = new List<Speaker>(); 


        protected Dictionary<string, string> elements = new Dictionary<string, string>();
        public SpeakerCollection(XElement e)
        {
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            _Speakers = e.Elements("s").Select(s => new Speaker(s)).ToList();
        }

        public SpeakerCollection(IEnumerable<Speaker> speakers)
        {
            _Speakers = speakers.ToList();
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="aSpeakers"></param>
        public SpeakerCollection(SpeakerCollection aSpeakers)
        {
            if (aSpeakers != null)
            {
                this._fileName = aSpeakers._fileName;
                if (aSpeakers._Speakers != null)
                {
                    this._Speakers = new List<Speaker>();
                    for (int i = 0; i < aSpeakers._Speakers.Count; i++)
                    {
                        this._Speakers.Add(aSpeakers._Speakers[i].Copy());
                    }
                }
            }
        }

        public SpeakerCollection()
        {

        }
        /// <summary>
        /// remove speaker from list - NOT FROM TRANSCRIPTION !!!!
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public bool RemoveSpeaker(Speaker aSpeaker)
        {
            return _Speakers.Remove(aSpeaker);
        }


        public Speaker GetSpeakerByDBID(string dbid)
        {
            return _Speakers.FirstOrDefault(s => s.DBID == dbid || s.Merges.Any(m=>m.DBID ==dbid));
        }

        public Speaker GetSpeakerByName(string fullname)
        {
            return _Speakers.FirstOrDefault(s => s.FullName == fullname);
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public virtual XElement Serialize(bool saveAll = true)
        {
            XElement elm = new XElement("sp",
                elements.Select(e => new XAttribute(e.Key, e.Value)),
                _Speakers.Select(s => s.Serialize(saveAll))
            );

            return elm;
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public void Serialize(string filename, bool saveAll = true)
        {
            var xelm = Serialize(saveAll);
            xelm.Save(filename);
        }

        /// <summary>
        /// called after deserialization
        /// </summary>
        protected virtual void Initialize(XDocument doc)
        {

        }


        /// <summary>
        /// //deserialize speaker database file. 
        /// Old file format support should not concern anyone outside ite.tul.cz, public release never containded old format
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="store"></param>
        public static void Deserialize(string filename, SpeakerCollection store)
        {
            //if file do not exists, do not modify store
            if (!File.Exists(filename))
            {
                return;
            }
            store._fileName = filename;
            XDocument doc = XDocument.Load(filename);


            if (doc.Root.Name == "MySpeakers") //old format from XmlSerializer
            {
                #region old format
                var root = doc.Root;
                var speakers = root.Elements("Speakers").Elements("Speaker");
                foreach (var sp in speakers)
                {
                    Speaker speaker = new Speaker();

                    var id = sp.Element("ID");
                    var fname = sp.Element("FirstName");
                    var sname = sp.Element("Surname");
                    var sex = sp.Element("Sex");
                    var comment = sp.Element("Comment");
                    var lang = sp.Element("DefaultLang");

                    if (id != null)
                        speaker.ID = XmlConvert.ToInt32(id.Value);
                    else
                        continue;

                    speaker.DBID = Guid.NewGuid().ToString();
                    speaker.FirstName = fname.Value ?? "";
                    speaker.Surname = sname.Value ?? "";

                    switch (sex.Value.ToLower())
                    {
                        case "m":
                        case "muž":
                        case "male":
                            speaker.Sex = Speaker.Sexes.Male;
                            break;

                        case "f":
                        case "žena":
                        case "female":
                            speaker.Sex = Speaker.Sexes.Female;
                            break;
                        default:
                            speaker.Sex = Speaker.Sexes.X;
                            break;
                    }

                    if (comment != null && !string.IsNullOrWhiteSpace(comment.Value))
                        speaker.Attributes.Add(new SpeakerAttribute("comment", "comment", comment.Value));


                    int vvvv;
                    if (int.TryParse(lang.Value, out vvvv) && vvvv < Speaker.Langs.Count)
                    {
                        speaker.DefaultLang = Speaker.Langs[vvvv];
                    }else
                    {
                        speaker.DefaultLang = lang.Value ?? Speaker.Langs[0];
                    }
                    store.Add(speaker);
                }
                #endregion
            }
            else
            {
                store._Speakers = doc.Root.Elements("s").Select(x => new Speaker(x)).ToList();
                store.Initialize(doc);
            }
        }

        //deserialize speaker database file...          
        public static SpeakerCollection Deserialize(String filename)
        {
            var mysp = new SpeakerCollection();
            Deserialize(filename, mysp);

            return mysp;
        }

        public SpeakerCollection(string filename)
        {
            SpeakerCollection.Deserialize(filename, this);
        }


        public int IndexOf(Speaker item)
        {
            return _Speakers.IndexOf(item);
        }

        public virtual void Insert(int index, Speaker item)
        {
            _Speakers.Insert(index, item);
        }

        public virtual void RemoveAt(int index)
        {
            _Speakers.RemoveAt(index);
        }

        public Speaker this[int index]
        {
            get
            {
                return _Speakers[index];
            }
            set
            {
                _Speakers[index] = value;
            }
        }

        public virtual void Add(Speaker item)
        {
            _Speakers.Add(item);
        }

        public virtual void Clear()
        {
            _Speakers.Clear();
        }

        public bool Contains(Speaker item)
        {
            return _Speakers.Contains(item);
        }

        public void CopyTo(Speaker[] array, int arrayIndex)
        {
            _Speakers.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _Speakers.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public virtual bool Remove(Speaker item)
        {
            return _Speakers.Remove(item);
        }

        public IEnumerator<Speaker> GetEnumerator()
        {
            return _Speakers.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _Speakers.GetEnumerator();
        }

        public virtual void AddRange(IEnumerable<Speaker> enumerable)
        {
            _Speakers.AddRange(enumerable);
        }
    }
}
