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
    [XmlInclude(typeof(Speaker))]
    public class MySpeakers
    {
        private bool _Ulozeno = false;
        [XmlIgnore]
        public bool Ulozeno { get { return _Ulozeno; } }
        [XmlIgnore]
        private string _JmenoSouboru;
        public string JmenoSouboru
        {
            get { return _JmenoSouboru; }
        }

        private List<Speaker> m_Speakers = new List<Speaker>();    //vsichni mluvci ve streamu

        public List<Speaker> Speakers
        {
            get { return m_Speakers; }
            set { m_Speakers = value; }
        }

        private Dictionary<string, string> elements = new Dictionary<string, string>();
        public MySpeakers(XElement e, bool isStrict)
        {
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            m_Speakers = e.Elements(isStrict ? "speaker" : "s").Select(s => new Speaker(s, isStrict)).ToList();
        }

        public MySpeakers(IEnumerable<Speaker> speakers)
        {
            m_Speakers = speakers.ToList();
        }

        //copy constructor
        public MySpeakers(MySpeakers aSpeakers)
        {
            if (aSpeakers != null)
            {
                this._JmenoSouboru = aSpeakers._JmenoSouboru;
                this._Ulozeno = aSpeakers._Ulozeno;
                if (aSpeakers.m_Speakers != null)
                {
                    this.m_Speakers = new List<Speaker>();
                    for (int i = 0; i < aSpeakers.m_Speakers.Count; i++)
                    {
                        this.m_Speakers.Add(aSpeakers.m_Speakers[i].Copy());
                    }
                }
            }
        }

        public MySpeakers()
        {

        }


        /// <summary>
        /// add speaker to collection
        /// </summary>
        /// <param name="speaker"></param>
        /// <param name="overwrite">ovewrite speaker with same fullname (if exists)</param>
        /// <returns>false if, speaker with same fullname exists and ovewrite is false</returns>
        /// <exception cref="ArgumentException">when speaker have null or empty fullname</exception>
        public bool AddSpeaker(Speaker speaker, bool overwrite=true)
        {
            if (speaker.FullName != null && speaker.FullName != "")
            {
                for (int i = 0; i < m_Speakers.Count; i++)
                {
                    if (((Speaker)m_Speakers[i]).FullName == speaker.FullName)
                        if (overwrite)
                        {
                            m_Speakers.RemoveAt(i);
                            break;
                        }
                        else
                            return false;
                }
                this.m_Speakers.Add(speaker);
                return true;
            }
            throw new ArgumentException("Spekear cannot have empty or null fullname");
        }


        public Speaker GetSpeakerByID(int ID)
        {
            foreach (Speaker msp in this.m_Speakers)
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
                    for (int i = 0; i < m_Speakers.Count; i++)
                    {
                        if ((m_Speakers[i]).FullName == aSpeaker.FullName)
                        {
                            this.m_Speakers.RemoveAt(i);
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
            for (int i = 0; i < this.m_Speakers.Count; i++)
            {
                if (((Speaker)m_Speakers[i]).FullName == fullname)
                {
                    aSpeaker = ((Speaker)m_Speakers[i]);
                    return aSpeaker;
                }
            }
            return null;
        }

        public XElement Serialize(bool strict)
        {
            XElement elm = new XElement(strict ? "speakers" : "sp",
                elements.Select(e => new XAttribute(e.Key, e.Value)),
                m_Speakers.Select(s => s.Serialize(strict))
            );

            return elm;
        }

        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serialize_V1(String jmenoSouboru, MySpeakers co)
        {
            try
            {
                if (!File.Exists(jmenoSouboru))
                    Directory.CreateDirectory(Path.GetDirectoryName(jmenoSouboru));

                XmlSerializer serializer = new XmlSerializer(typeof(MySpeakers));
                TextWriter writer = new StreamWriter(jmenoSouboru);
                //XmlTextWriter writer = new XmlTextWriter(jmenoSouboru, Encoding.UTF8);

                serializer.Serialize(writer, co);
                writer.Close();
                this._JmenoSouboru = jmenoSouboru;
                this._Ulozeno = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Deserializuje soubor             
        public MySpeakers Deserialize(String filename)
        {
            //pokud neexistuje soubor, vrati prazdnou databazi
            if (!new FileInfo(filename).Exists)
            {
                return new MySpeakers();
            }

            XmlSerializer serializer = new XmlSerializer(typeof(MySpeakers));
            //FileStream reader = new FileStream(jmenoSouboru, FileMode.Open);
            //TextReader reader = new StreamReader("e:\\MySubtitlesDataXml.txt");
            MySpeakers md;// = new MySubtitlesData();

            XmlTextReader xreader = new XmlTextReader(filename);
            md = (MySpeakers)serializer.Deserialize(xreader);
            xreader.Close();
            md._JmenoSouboru = filename;
            md._Ulozeno = true;
            return md;

        }

    }
}
