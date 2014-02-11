using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Windows;
using System.Linq;
using System.Xml.Linq;

namespace NanoTrans
{
    [XmlInclude(typeof(MySpeaker))]
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

        private List<MySpeaker> m_Speakers = new List<MySpeaker>();    //vsichni mluvci ve streamu

        public List<MySpeaker> Speakers
        {
            get { return m_Speakers; }
            set { m_Speakers = value; }
        }
        public int speakersIndexCounter = 0;                  //ohlidani vzdy vetsiho ID vsech mluvcich

        private Dictionary<string, string> elements = new Dictionary<string, string>();
        public MySpeakers(XElement e, bool isStrict)
        {
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            m_Speakers = e.Elements(isStrict ? "speaker" : "s").Select(s => new MySpeaker(s, isStrict)).ToList();
        }

        //copy constructor
        public MySpeakers(MySpeakers aSpeakers)
        {
            if (aSpeakers != null)
            {
                this._JmenoSouboru = aSpeakers._JmenoSouboru;
                this._Ulozeno = aSpeakers._Ulozeno;
                this.speakersIndexCounter = aSpeakers.speakersIndexCounter;
                if (aSpeakers.m_Speakers != null)
                {
                    this.m_Speakers = new List<MySpeaker>();
                    for (int i = 0; i < aSpeakers.m_Speakers.Count; i++)
                    {
                        this.m_Speakers.Add(new MySpeaker(aSpeakers.m_Speakers[i]));
                    }
                }
            }
        }

        public MySpeakers()
        {
            
        }


        /// <summary>
        /// prida mluvciho do seznamu, vraci jeho ID ze seznamu
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public int NovySpeaker(MySpeaker aSpeaker)
        {
            try
            {
                if (aSpeaker.FullName != null && aSpeaker.FullName != "")
                {
                    for (int i = 0; i < m_Speakers.Count; i++)
                    {
                        if (((MySpeaker)m_Speakers[i]).FullName == aSpeaker.FullName)
                        {
                            MessageBox.Show("Mluvčí s tímto jménem již existuje!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return int.MinValue;
                        }

                    }
                    this.speakersIndexCounter = m_Speakers.Count > 0 ? m_Speakers.Max(s => s.ID) + 1:1;
                    aSpeaker.ID = speakersIndexCounter;
                    this.m_Speakers.Add(new MySpeaker(aSpeaker));
                    return speakersIndexCounter;


                }
                else return int.MinValue;
            }
            catch// (Exception ex)
            {
                return int.MinValue;
            }
        }

        /// <summary>
        /// vraci speakera podle ID pokud existuje, jinak prazdneho speakera
        /// </summary>
        /// <param name="aIDSpeakera"></param>
        /// <returns></returns>
        public MySpeaker VratSpeakera(int aIDSpeakera)
        {
            try
            {
                foreach (MySpeaker msp in this.m_Speakers)
                {
                    if (msp.ID == aIDSpeakera) return msp;
                }
                return new MySpeaker();

            }
            catch// (Exception ex)
            {
                return new MySpeaker();
            }

        }

        /// <summary>
        /// smazani speakera ze seznamu speakeru
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public bool OdstranSpeakera(MySpeaker aSpeaker)
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


        /// <summary>
        /// vraci ID speakera podle stringu jmena
        /// </summary>
        /// <param name="aJmeno"></param>
        /// <returns></returns>
        public int NajdiSpeakeraID(string aJmeno)
        {
            try
            {
                MySpeaker aSpeaker = new MySpeaker();
                for (int i = 0; i < this.m_Speakers.Count; i++)
                {
                    if (((MySpeaker)m_Speakers[i]).FullName == aJmeno)
                    {
                        aSpeaker = ((MySpeaker)m_Speakers[i]);
                        break;
                    }
                }
                return aSpeaker.ID;

            }
            catch// (Exception ex)
            {
                return new MySpeaker().ID;
            }
        }

        /// <summary>
        /// vraci speakera podle stringu jmena
        /// </summary>
        /// <param name="aJmeno"></param>
        /// <returns></returns>
        public MySpeaker NajdiSpeakeraSpeaker(string aJmeno)
        {
            try
            {
                MySpeaker aSpeaker = new MySpeaker();
                for (int i = 0; i < this.m_Speakers.Count; i++)
                {
                    if (((MySpeaker)m_Speakers[i]).FullName == aJmeno)
                    {
                        aSpeaker = ((MySpeaker)m_Speakers[i]);
                        break;
                    }
                }
                return aSpeaker;

            }
            catch// (Exception ex)
            {
                return new MySpeaker();
            }

        }

        public bool UpdatujSpeakera(string aJmeno, MySpeaker aSpeaker)
        {
            try
            {
                
                for (int i = 0; i < m_Speakers.Count; i++)
                {
                    if (((MySpeaker)m_Speakers[i]).FullName == aSpeaker.FullName)
                    {
                        //return false;
                    }

                }
                
                if (aSpeaker == null || aSpeaker.FullName == null || aSpeaker.FullName == "" || NajdiSpeakeraID(aJmeno) < 0) return false;
                if (NajdiSpeakeraID(aJmeno) != NajdiSpeakeraID(aSpeaker.FullName) && NajdiSpeakeraID(aSpeaker.FullName)>-1)
                {
                    MessageBox.Show("Mluvčí s tímto jménem již existuje!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false; //mluvci s timto jmenem jiz existuje
                }
                MySpeaker pSpeaker;
                for (int i = 0; i < this.m_Speakers.Count; i++)
                {
                    if (((MySpeaker)m_Speakers[i]).FullName == aJmeno)
                    {
                        pSpeaker = ((MySpeaker)m_Speakers[i]);
                        aSpeaker.ID = pSpeaker.ID;
                        m_Speakers[i] = new MySpeaker(aSpeaker);
                        
                        

                        return true;
                    }
                }
                return false;
            }
            catch// (Exception ex)
            {
                return false;
            }

        }



        public XElement Serialize(bool strict)
        {
            XElement elm = new XElement(strict ? "speakers" : "sp",
                elements.Select(e =>new XAttribute(e.Key, e.Value)),
                m_Speakers.Select(s=>s.Serialize(strict))
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
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri serializaci souboru s nastavením: " + ex.Message,"Varování!");
                return false;
            }
        }

        //Deserializuje soubor             
        public MySpeakers Deserializovat(String jmenoSouboru)
        {
            try
            {
                //pokud neexistuje soubor, vrati prazdnou databazi
                if (!new FileInfo(jmenoSouboru).Exists)
                {
                    return new MySpeakers();
                }
                                
                XmlSerializer serializer = new XmlSerializer(typeof(MySpeakers));
                //FileStream reader = new FileStream(jmenoSouboru, FileMode.Open);
                //TextReader reader = new StreamReader("e:\\MySubtitlesDataXml.txt");
                MySpeakers md;// = new MySubtitlesData();

                XmlTextReader xreader = new XmlTextReader(jmenoSouboru);
                md = (MySpeakers)serializer.Deserialize(xreader);
                xreader.Close();
                md._JmenoSouboru = jmenoSouboru;
                md._Ulozeno = true;
                return md;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba při načítání databáze mluvčích - nepodporovaný formát souboru: " + ex.Message);
                return null;
            }

        }

    }
}
