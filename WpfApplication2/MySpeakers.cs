using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Windows;


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

        public List<MySpeaker> Speakers = new List<MySpeaker>();    //vsichni mluvci ve streamu
        public int speakersIndexCounter = 0;                  //ohlidani vzdy vetsiho ID vsech mluvcich


        public MySpeakers()
        {

        }

        //copy constructor
        public MySpeakers(MySpeakers aSpeakers)
        {
            if (aSpeakers != null)
            {
                this._JmenoSouboru = aSpeakers._JmenoSouboru;
                this._Ulozeno = aSpeakers._Ulozeno;
                this.speakersIndexCounter = aSpeakers.speakersIndexCounter;
                if (aSpeakers.Speakers != null)
                {
                    this.Speakers = new List<MySpeaker>();
                    for (int i = 0; i < aSpeakers.Speakers.Count; i++)
                    {
                        this.Speakers.Add(new MySpeaker(aSpeakers.Speakers[i]));
                    }
                }
            }
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
                    for (int i = 0; i < Speakers.Count; i++)
                    {
                        if (((MySpeaker)Speakers[i]).FullName == aSpeaker.FullName)
                        {
                            MessageBox.Show("Mluvčí s tímto jménem již existuje!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return -1;
                        }

                    }
                    this.speakersIndexCounter++;
                    aSpeaker.ID = speakersIndexCounter;
                    this.Speakers.Add(new MySpeaker(aSpeaker));
                    return speakersIndexCounter;


                }
                else return -1;
            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
                return -1;
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
                foreach (MySpeaker msp in this.Speakers)
                {
                    if (msp.ID == aIDSpeakera) return msp;
                }
                return new MySpeaker();

            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
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
                    for (int i = 0; i < Speakers.Count; i++)
                    {
                        if ((Speakers[i]).FullName == aSpeaker.FullName)
                        {
                            this.Speakers.RemoveAt(i);
                            return true;
                        }

                    }
                    return false;


                }
                else return false;
            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
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
                for (int i = 0; i < this.Speakers.Count; i++)
                {
                    if (((MySpeaker)Speakers[i]).FullName == aJmeno)
                    {
                        aSpeaker = ((MySpeaker)Speakers[i]);
                        break;
                    }
                }
                return aSpeaker.ID;

            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
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
                for (int i = 0; i < this.Speakers.Count; i++)
                {
                    if (((MySpeaker)Speakers[i]).FullName == aJmeno)
                    {
                        aSpeaker = ((MySpeaker)Speakers[i]);
                        break;
                    }
                }
                return aSpeaker;

            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
                return new MySpeaker();
            }

        }

        public bool UpdatujSpeakera(string aJmeno, MySpeaker aSpeaker)
        {
            try
            {
                
                for (int i = 0; i < Speakers.Count; i++)
                {
                    if (((MySpeaker)Speakers[i]).FullName == aSpeaker.FullName)
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
                for (int i = 0; i < this.Speakers.Count; i++)
                {
                    if (((MySpeaker)Speakers[i]).FullName == aJmeno)
                    {
                        pSpeaker = ((MySpeaker)Speakers[i]);
                        aSpeaker.ID = pSpeaker.ID;
                        Speakers[i] = new MySpeaker(aSpeaker);
                        
                        

                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
                return false;
            }

        }


        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serializovat(String jmenoSouboru, MySpeakers co)
        {
            try
            {
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
                Window1.logAplikace.LogujChybu(ex);
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
                Window1.logAplikace.LogujChybu(ex);
                return null;
            }

        }

    }
}
