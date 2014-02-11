using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NanoTrans.Core
{
    /// <summary>
    /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
    /// </summary>
    /// <returns></returns>
    public class SpeakerCollection
    {
        protected string _fileName;
        public string FileName
        {
            get { return _fileName; }
        }

        protected List<Speaker> _Speakers = new List<Speaker>();    //vsichni mluvci ve streamu

        public List<Speaker> Speakers
        {
            get { return _Speakers; }
            set { _Speakers = value; }
        }

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
        /// add speaker to collection
        /// </summary>
        /// <param name="speaker"></param>
        public void AddSpeaker(Speaker speaker)
        {
            _Speakers.Add(speaker);
        }


        public Speaker GetSpeakerByID(int ID)
        {
            foreach (Speaker msp in this._Speakers)
            {
                if (msp.ID == ID) return msp;
            }

            return null;
        }

        /// <summary>
        /// smazani speakera ze seznamu speakeru
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public bool RemoveSpeaker(Speaker aSpeaker)
        {
            return _Speakers.Remove(aSpeaker);
        }


        public Speaker GetSpeakerByName(string fullname)
        {
            Speaker aSpeaker = new Speaker();
            for (int i = 0; i < this._Speakers.Count; i++)
            {
                if (((Speaker)_Speakers[i]).FullName == fullname)
                {
                    aSpeaker = ((Speaker)_Speakers[i]);
                    return aSpeaker;
                }
            }
            return null;
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <returns></returns>
        public virtual XElement Serialize()
        {
            XElement elm = new XElement("sp",
                elements.Select(e => new XAttribute(e.Key, e.Value)),
                _Speakers.Select(s => s.Serialize())
            );

            return elm;
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <returns></returns>
        public void Serialize(string filename)
        {
            var xelm = Serialize();
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
            //pokud neexistuje soubor, vrati prazdnou databazi
            if (!new FileInfo(filename).Exists)
            {
                throw new FileNotFoundException();
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
                    store.AddSpeaker(speaker);
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

    }
}
