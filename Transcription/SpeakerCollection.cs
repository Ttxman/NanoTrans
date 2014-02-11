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
    public class SpeakerCollection
    {
        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
        }

        private List<Speaker> _Speakers = new List<Speaker>();    //vsichni mluvci ve streamu

        public List<Speaker> Speakers
        {
            get { return _Speakers; }
            set { _Speakers = value; }
        }

        private Dictionary<string, string> elements = new Dictionary<string, string>();
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
        /// <param name="overwrite">ovewrite speaker with same fullname (if exists)</param>
        /// <returns>false if, speaker with same fullname exists and ovewrite is false</returns>
        /// <exception cref="ArgumentException">when speaker have null or empty fullname</exception>
        public bool AddSpeaker(Speaker speaker, bool overwrite = true)
        {
            if (speaker.FullName != null && speaker.FullName != "")
            {
                for (int i = 0; i < _Speakers.Count; i++)
                {
                    if (((Speaker)_Speakers[i]).FullName == speaker.FullName)
                        if (overwrite)
                        {
                            _Speakers.RemoveAt(i);
                            break;
                        }
                        else
                            return false;
                }
                this._Speakers.Add(speaker);
                return true;
            }
            throw new ArgumentException("Spekear cannot have empty or null fullname");
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
            try
            {
                if (aSpeaker.FullName != null && aSpeaker.FullName != "")
                {
                    for (int i = 0; i < _Speakers.Count; i++)
                    {
                        if ((_Speakers[i]).FullName == aSpeaker.FullName)
                        {
                            this._Speakers.RemoveAt(i);
                            return true;
                        }

                    }
                    return false;


                }
                else return false;
            }
            catch// (Exception ex)
            {
                return false;
            }
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

        public XElement Serialize()
        {
            XElement elm = new XElement("sp",
                elements.Select(e => new XAttribute(e.Key, e.Value)),
                _Speakers.Select(s => s.Serialize())
            );

            return elm;
        }

        public void Serialize(string filename)
        {
            var xelm = Serialize();
            xelm.Save(filename);
        }

        //deserialize speaker database file...          
        public static SpeakerCollection Deserialize(String filename)
        {
            //pokud neexistuje soubor, vrati prazdnou databazi
            if (!new FileInfo(filename).Exists)
            {
                return new SpeakerCollection();
            }

            XDocument doc = XDocument.Load(filename);


            if (doc.Root.Name == "MySpeakers") //old format from XmlSerializer
            {
                var root = doc.Root;
                SpeakerCollection mysp = new SpeakerCollection();
                var speakers = root.Elements("Speaker");
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

                    if (fname.Value != null)
                        speaker.Attributes.Add(new SpeakerAttribute("comment", "comment", fname.Value));

                    speaker.DefaultLang = lang.Value ?? Speaker.Langs[0];

                    mysp.AddSpeaker(speaker);
                }

                return mysp;

            }
            else
            {

            }
            return null;
        }



    }
}
