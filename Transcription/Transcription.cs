using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace NanoTrans.Core
{
    //hlavni trida s titulky a se vsemi potrebnymi metodami pro serializaci
    public class Transcription : TranscriptionElement, IList<TranscriptionElement>
    {
        public double TotalHeigth;
        public bool FindNext(ref TranscriptionElement paragraph, ref int TextOffset, out int length, string pattern, bool isregex, bool CaseSensitive, bool searchinspeakers)
        {
            TranscriptionElement par = paragraph;
            length = 0;
            if (par == null)
                return false;

            if (searchinspeakers)
            {
                TranscriptionElement prs = paragraph.Next();

                while (prs != null)
                {
                    TranscriptionParagraph pr = prs as TranscriptionParagraph;
                    if (pr != null && GetSpeakerByID(pr.SpeakerID).FullName.ToLower().Contains(pattern.ToLower()))
                    {
                        paragraph = pr;
                        TextOffset = 0;
                        return true;
                    }
                    prs = pr.Next();
                }
                return false;
            }

            Regex r;
            if (isregex)
            {
                r = new Regex(pattern);
            }
            else
            {
                if (!CaseSensitive)
                    pattern = pattern.ToLower();
                r = new Regex(Regex.Escape(pattern));
            }

            TranscriptionElement tag = paragraph;
            while (par != null)
            {
                string s = par.Text;
                if (!CaseSensitive && !isregex)
                    s = s.ToLower();
                if (TextOffset >= s.Length)
                    TextOffset = 0;
                Match m = r.Match(s, TextOffset);

                if (m.Success)
                {
                    TextOffset = m.Index;
                    length = m.Length;
                    paragraph = tag;
                    return true;
                }

                tag = tag.Next();
                if (tag == null)
                    return false;
                par = tag;
                TextOffset = 0;
            }

            return false;
        }

        public string FileName { get; set; }

        private bool m_saved;
        public bool Saved
        {
            get
            {
                return m_saved;
            }
            set
            {
                m_saved = value;
            }
        }

        /// <summary>
        /// datum a cas poradu, ktery je v transkripci zpracovan - napr. pocatecni cas audio souboru 
        /// </summary>
        public DateTime dateTime { get; set; }
        /// <summary>
        /// zdroj odkud je transkripce - radio - nazev kanalu, televize, mikrofon, atd...
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// typ poradu - cele transkripce 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// zdroj audio dat - muze byt stejny jako video a naopak
        /// </summary>
        public string mediaURI { get; set; }
        /// <summary>
        /// zdroj video dat - muze byt stejny jako audio a naopak
        /// </summary>
        public string videoFileName { get; set; }

        /// <summary>
        /// vsechny kapitoly streamu
        /// </summary>
        public VirtualTypeList<TranscriptionChapter> Chapters;    //vsechny kapitoly streamu

        [XmlElement("SpeakersDatabase")]
        public MySpeakers m_speakers = new MySpeakers();

        [XmlIgnore]
        public MySpeakers Speakers
        {
            get { return m_speakers; }
            set { m_speakers = value; }
        }



        public Transcription()
        {
            FileName = null;
            Saved = false;

            Chapters = new VirtualTypeList<TranscriptionChapter>(this);
            //constructor  
        }



        /// <summary>
        /// vytvori kopii objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public Transcription(Transcription aKopie)
        {
            this.dateTime = aKopie.dateTime;
            this.source = aKopie.source;
            this.mediaURI = aKopie.mediaURI;
            this.videoFileName = aKopie.videoFileName;
            this.type = aKopie.type;
            if (aKopie.Chapters != null)
            {
                this.Chapters = new VirtualTypeList<TranscriptionChapter>(this);
                for (int i = 0; i < aKopie.Chapters.Count; i++)
                {
                    this.Chapters.Add(new TranscriptionChapter(aKopie.Chapters[i]));
                }
            }
            this.FileName = aKopie.FileName;
            this.m_speakers = new MySpeakers(aKopie.m_speakers);
            this.Saved = aKopie.Saved;
        }


        public Transcription(string path)
            : this()
        {
            Deserialize(path, this);
        }
        public Transcription(FileInfo f)
            : this(f.FullName)
        {

        }

        /// <summary>
        /// vrati vsechny vyhovujici elementy casu
        /// </summary>
        /// <param name="aPoziceKurzoru"></param>
        /// <returns></returns>
        public List<TranscriptionParagraph> VratElementDanehoCasu(TimeSpan cas)
        {
            List<TranscriptionParagraph> toret = new List<TranscriptionParagraph>();
            foreach (var el in this)
            {
                if (el.IsParagraph && el.Begin <= cas && el.End > cas)
                {
                    toret.Add((TranscriptionParagraph)el);
                }
            }
            return toret;
        }

        public TranscriptionParagraph VratElementKonciciPred(TimeSpan cas)
        {
            List<TranscriptionParagraph> toret = new List<TranscriptionParagraph>();
            TranscriptionParagraph par = null;
            foreach (var el in this)
            {
                if (el.End < cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (TranscriptionParagraph)el;
                    }
                }
                else
                    break;
            }
            return par;
        }


        public TranscriptionParagraph VratElementZacinajiciPred(TimeSpan cas)
        {
            List<TranscriptionParagraph> toret = new List<TranscriptionParagraph>();
            TranscriptionParagraph par = null;
            foreach (var el in this)
            {
                if (el.Begin < cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (TranscriptionParagraph)el;
                    }
                }
                else
                    break;
            }
            return par;

        }

        //smazani speakera ze seznamu speakeru a odstraneni speakera v pouzitych odstavcich
        public bool RemoveSpeaker(Speaker aSpeaker)
        {
            try
            {
                if (aSpeaker.FullName != null && aSpeaker.FullName != "")
                {
                    if (this.m_speakers.RemoveSpeaker(aSpeaker))
                    {
                        Saved = false;
                        for (int k = 0; k < Chapters.Count; k++)
                        {
                            for (int l = 0; l < ((TranscriptionChapter)Chapters[k]).Sections.Count; l++)
                            {
                                for (int m = 0; m < ((TranscriptionSection)((TranscriptionChapter)Chapters[k]).Sections[l]).Paragraphs.Count; m++)
                                {
                                    if (Chapters[k].Sections[l].Paragraphs[m].Speaker == aSpeaker)
                                    {
                                        Chapters[k].Sections[l].Paragraphs[m].Speaker = Speaker.DefaultSpeaker;
                                    }
                                }

                            }

                        }
                        Saved = false;
                        return true;
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
        /// novy mluvci do databaze titulku
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public bool AddSpeaker(Speaker aSpeaker)
        {
            if (m_speakers.AddSpeaker(aSpeaker, false))
            {
                this.Saved = false;
                return true;
            }
            return false;
        }


        public Speaker GetSpeakerByID(int ID)
        {
            return this.m_speakers.GetSpeakerByID(ID);
        }

        public Speaker GetSpeakerByName(string fullname)
        {
            return this.m_speakers.GetSpeakerByName(fullname);
        }

        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru - muze ulozit mluvci bez fotky
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serialize(string jmenoSouboru, bool aUkladatKompletMluvci = false, bool StrictFormat = false)
        {
            using (FileStream s = new FileStream(jmenoSouboru, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
            {

                bool output = Serialize(s, aUkladatKompletMluvci, StrictFormat);

                if (output)
                {
                    this.FileName = jmenoSouboru;
                    this.Saved = true;

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        /// <summary>
        /// serialize data to txsx v1 format
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="co"></param>
        /// <param name="SaveSpeakersInCompleteFormat"></param>
        /// <returns></returns>
        /// <exception cref="NanoTransSerializationException">when writing failed</exception>
        public static void SerializeV1(Stream stream, Transcription co, bool SaveSpeakersInCompleteFormat)
        {
            try
            {
                System.Xml.XmlTextWriter writer = new XmlTextWriter(stream, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument(); //<?xml version ...


                writer.WriteStartElement("Transcription");
                writer.WriteAttributeString("dateTime", XmlConvert.ToString(co.dateTime, XmlDateTimeSerializationMode.Local));
                writer.WriteAttributeString("audioFileName", co.mediaURI);

                writer.WriteStartElement("Chapters");


                foreach (TranscriptionChapter c in co.Chapters)
                {
                    writer.WriteStartElement("Chapter");
                    writer.WriteAttributeString("name", c.name);
                    writer.WriteAttributeString("begin", XmlConvert.ToString(c.Begin));
                    writer.WriteAttributeString("end", XmlConvert.ToString(c.End));

                    writer.WriteStartElement("Sections");

                    foreach (TranscriptionSection s in c.Sections)
                    {
                        writer.WriteStartElement("Section");
                        writer.WriteAttributeString("name", s.name);
                        writer.WriteAttributeString("begin", XmlConvert.ToString(s.Begin));
                        writer.WriteAttributeString("end", XmlConvert.ToString(s.End));

                        writer.WriteStartElement("Paragraphs");

                        foreach (TranscriptionParagraph p in s.Paragraphs)
                        {
                            writer.WriteStartElement("Paragraph");
                            writer.WriteAttributeString("begin", XmlConvert.ToString(p.Begin));
                            writer.WriteAttributeString("end", XmlConvert.ToString(p.End));
                            writer.WriteAttributeString("trainingElement", XmlConvert.ToString(p.trainingElement));
                            writer.WriteAttributeString("Attributes", p.Attributes);

                            writer.WriteStartElement("Phrases");

                            foreach (TranscriptionPhrase ph in p.Phrases)
                            {
                                writer.WriteStartElement("Phrase");
                                writer.WriteAttributeString("begin", XmlConvert.ToString(ph.Begin));
                                writer.WriteAttributeString("end", XmlConvert.ToString(ph.End));
                                writer.WriteElementString("Text", ph.Text);
                                writer.WriteEndElement();//Phrase
                            }


                            writer.WriteEndElement();//Phrases
                            writer.WriteElementString("speakerID", XmlConvert.ToString(p.SpeakerID));
                            writer.WriteEndElement();//Paragraph
                        }

                        writer.WriteEndElement();//Paragraphs




                        writer.WriteStartElement("PhoneticParagraphs");
                        foreach (TranscriptionParagraph p in s.Paragraphs)
                        {
                            writer.WriteStartElement("Paragraph");
                            writer.WriteAttributeString("begin", XmlConvert.ToString(p.Begin));
                            writer.WriteAttributeString("end", XmlConvert.ToString(p.End));
                            writer.WriteAttributeString("trainingElement", XmlConvert.ToString(p.trainingElement));
                            writer.WriteAttributeString("Attributes", p.Attributes);

                            writer.WriteStartElement("Phrases");

                            foreach (TranscriptionPhrase ph in p.Phrases)
                            {
                                writer.WriteStartElement("Phrase");
                                writer.WriteAttributeString("begin", XmlConvert.ToString(ph.Begin));
                                writer.WriteAttributeString("end", XmlConvert.ToString(ph.End));
                                writer.WriteElementString("Text", ph.Phonetics);
                                writer.WriteEndElement();//Phrase
                            }


                            writer.WriteEndElement();//Phrases
                            writer.WriteElementString("speakerID", XmlConvert.ToString(p.SpeakerID));
                            writer.WriteEndElement();//Paragraph
                        }



                        writer.WriteEndElement();//PhoneticParagraphs
                        writer.WriteEndElement();//section
                    }


                    writer.WriteEndElement();//sections
                    writer.WriteEndElement();//chapter
                }

                writer.WriteEndElement();//chapters


                writer.WriteStartElement("SpeakersDatabase");
                writer.WriteStartElement("Speakers");



                foreach (Speaker sp in co.m_speakers.Speakers)
                {
                    writer.WriteStartElement("Speaker");
                    writer.WriteElementString("ID", XmlConvert.ToString(sp.ID));
                    writer.WriteElementString("Firstname", sp.FirstName);
                    writer.WriteElementString("Surname", sp.Surname);
                    writer.WriteElementString("Sex", (sp.Sex == Speaker.Sexes.Female) ? "female" : (sp.Sex == Speaker.Sexes.Male) ? "male" : "-");
                    writer.WriteElementString("Comment", sp.Comment);

                    writer.WriteEndElement();//speaker
                }

                writer.WriteEndElement();//Speakers
                writer.WriteEndElement();//SpeakersDatabase

                writer.WriteEndElement();//Transcription

                writer.Close();

            }
            catch (Exception ex)
            {
                throw new NanoTransSerializationException(ex.Message, ex);
            }

        }
        /// <summary>
        /// Serializuje tuto tridu a ulozi data do xml souboru - muze ulozit mluvci bez fotky
        /// </summary>
        /// <param name="jmenoSouboru"></param>
        /// <param name="co"></param>
        /// <returns></returns>
        public bool Serialize(Stream datastream, bool aUkladatKompletMluvci = false, bool StrictFormat = false)
        {
            XDocument xdoc =
                new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("transcription", elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { new XAttribute("version", "3.0"), new XAttribute("mediaURI", mediaURI ?? "") }),
                        this.Meta,
                        Chapters.Select(c => c.Serialize(StrictFormat)),
                        Speakers.Serialize()
                    )
                );

            xdoc.Save(datastream);
            return true;
        }


        public static Transcription Deserialize(string filename)
        {
            Transcription tr = new Transcription();
            Deserialize(filename, tr);
            return tr;
        }
        //Deserializuje soubor             
        public static void Deserialize(string filename, Transcription storage)
        {
            using (Stream s = File.Open(filename, FileMode.Open))
            {
                Deserialize(s, storage);
                if (storage != null)
                {
                    storage.FileName = filename;
                    storage.Saved = true;
                }
            }
        }


        public static Transcription Deserialize(Stream datastream)
        {
            Transcription tr = new Transcription();
            Deserialize(datastream, tr);
            return tr;
        }


        //Deserializuje soubor             
        public static void Deserialize(Stream datastream, Transcription storage)
        {
            XmlTextReader reader = new XmlTextReader(datastream);
            if (reader.Read())
            {
                reader.Read();
                reader.Read();
                string version = reader.GetAttribute("version");

                if (version == "2.0")
                {
                    DeserializeV2_0(reader, storage);
                }
                else
                {
                    datastream.Position = 0;
                    DeserializeV1(new XmlTextReader(datastream), storage);
                }
            }
        }

        public XElement Meta = EmptyMeta;
        private static readonly XElement EmptyMeta = new XElement("Meta");
        private Dictionary<string, string> elements = new Dictionary<string, string>();

        /// <summary>
        /// deserialize transcription in v2 format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">data will be deserialized into this object, useful for overrides </param>
        private static void DeserializeV2_0(XmlTextReader reader, Transcription storage)
        {

            Transcription data = storage;
            data.BeginUpdate();
            var document = XDocument.Load(reader);
            var transcription = document.Elements().First();

            string style = transcription.Attribute("style").Value;

            bool isStrict = style == "strict";
            string version = transcription.Attribute("version").Value;
            string mediaURI = transcription.Attribute("mediaURI").Value;
            data.mediaURI = mediaURI;
            data.Meta = transcription.Element("meta") ?? EmptyMeta;
            var chapters = transcription.Elements(isStrict ? "chapter" : "ch");

            data.elements = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            data.elements.Remove("style");
            data.elements.Remove("mediaURI");
            data.elements.Remove("version");

            chapters.Select(c => (TranscriptionElement)new TranscriptionChapter(c, isStrict)).ToList().ForEach(c => data.Add(c));

            var speakers = transcription.Element(isStrict ? "speakers" : "sp");
            data.Speakers.Speakers = new List<Speaker>(speakers.Elements(isStrict ? "speaker" : "s").Select(s => new Speaker(s, isStrict)));
            storage.AssingSpeakersByID();
            data.EndUpdate();
        }

        /// <summary>
        /// read old transcription format (v1)
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">deserializes into this transcription (useful for overrides)</param>
        /// <returns>deserialized transcription</returns>
        /// <exception cref="NanotransSerializationException"></exception>
        public static void DeserializeV1(XmlTextReader reader, Transcription storage)
        {
            try
            {
                Transcription data = storage;
                reader.WhitespaceHandling = WhitespaceHandling.Significant;

                reader.Read(); //<?xml version ...
                reader.Read();
                data.mediaURI = reader.GetAttribute("audioFileName");
                string val = reader.GetAttribute("dateTime");
                if (val != null)
                    data.dateTime = XmlConvert.ToDateTime(val, XmlDateTimeSerializationMode.Local);

                reader.ReadStartElement("Transcription");

                int result;

                //reader.Read();
                reader.ReadStartElement("Chapters");
                //reader.ReadStartElement("Chapter");
                while (reader.Name == "Chapter")
                {
                    TranscriptionChapter c = new TranscriptionChapter();
                    c.name = reader.GetAttribute("name");

                    val = reader.GetAttribute("begin");
                    if (int.TryParse(val, out result))
                        if (result < 0)
                            c.Begin = new TimeSpan(result);
                        else
                            c.Begin = TimeSpan.FromMilliseconds(result);
                    else
                        c.Begin = XmlConvert.ToTimeSpan(val);

                    val = reader.GetAttribute("end");
                    if (int.TryParse(val, out result))
                        if (result < 0)
                            c.End = new TimeSpan(result);
                        else
                            c.End = TimeSpan.FromMilliseconds(result);
                    else
                        c.End = XmlConvert.ToTimeSpan(val);

                    reader.Read();

                    reader.ReadStartElement("Sections");


                    while (reader.Name == "Section")
                    {

                        TranscriptionSection s = new TranscriptionSection();
                        s.name = reader.GetAttribute("name");

                        val = reader.GetAttribute("begin");
                        if (int.TryParse(val, out result))
                            if (result < 0)
                                s.Begin = new TimeSpan(result);
                            else
                                s.Begin = TimeSpan.FromMilliseconds(result);
                        else
                            s.Begin = XmlConvert.ToTimeSpan(val);

                        val = reader.GetAttribute("end");
                        if (int.TryParse(val, out result))
                            if (result < 0)
                                s.End = new TimeSpan(result);
                            else
                                s.End = TimeSpan.FromMilliseconds(result);
                        else
                            s.End = XmlConvert.ToTimeSpan(val);

                        reader.Read();
                        reader.ReadStartElement("Paragraphs");

                        while (reader.Name == "Paragraph")
                        {
                            TranscriptionParagraph p = new TranscriptionParagraph();
                            val = reader.GetAttribute("begin");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1 && s.Paragraphs.Count > 0)
                                        p.Begin = s.Paragraphs[s.Paragraphs.Count - 1].End;
                                    else
                                        p.Begin = new TimeSpan(result);
                                else
                                    p.Begin = TimeSpan.FromMilliseconds(result);
                            else
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    p.End = new TimeSpan(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else
                                p.End = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("trainingElement");
                            p.trainingElement = val == null ? false : XmlConvert.ToBoolean(val);
                            p.Attributes = reader.GetAttribute("Attributes");

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                TranscriptionPhrase ph = new TranscriptionPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.Begin = new TimeSpan(result);
                                    else
                                        ph.Begin = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.End = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;

                                if (reader.IsEmptyElement)
                                    reader.Read();

                                while (reader.Name == "Text")
                                {
                                    reader.WhitespaceHandling = WhitespaceHandling.All;
                                    if (!reader.IsEmptyElement)
                                    {
                                        reader.Read();
                                        while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element)
                                        {
                                            ph.Text = reader.Value.Trim('\r', '\n');
                                            reader.Read();
                                        }
                                    }
                                    reader.WhitespaceHandling = WhitespaceHandling.Significant;
                                    reader.ReadEndElement();//text
                                }
                                p.Phrases.Add(ph);
                                if (reader.Name != "Phrase") //text nebyl prazdny
                                {
                                    reader.Read();//text;
                                    reader.ReadEndElement();//Text;
                                }
                                reader.ReadEndElement();//Phrase;

                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.SpeakerID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.SpeakerID == -1)
                                p.SpeakerID = Speaker.DefaultID;

                            reader.ReadEndElement();//paragraph
                            s.Paragraphs.Add(p);

                        }

                        if (reader.Name == "Paragraphs") //teoreticky mohl byt prazdny
                            reader.ReadEndElement();

                        if (reader.Name == "PhoneticParagraphs")
                            reader.ReadStartElement();

                        while (reader.Name == "Paragraph")
                        {
                            TranscriptionParagraph p = new TranscriptionParagraph();
                            val = reader.GetAttribute("begin");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    p.Begin = TimeSpan.Zero;
                                else
                                    p.Begin = TimeSpan.FromMilliseconds(result);
                            else
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1)
                                    {
                                        p.Begin = s.Paragraphs[s.Paragraphs.Count - 1].End;
                                    }
                                    else
                                        p.End = TimeSpan.FromMilliseconds(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else
                                p.End = XmlConvert.ToTimeSpan(val);

                            p.trainingElement = XmlConvert.ToBoolean(reader.GetAttribute("trainingElement"));
                            p.Attributes = reader.GetAttribute("Attributes");

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                TranscriptionPhrase ph = new TranscriptionPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.Begin = p.Begin;
                                    else
                                        ph.Begin = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else
                                    ph.End = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;
                                reader.ReadStartElement("Text");//posun na content
                                ph.Text = reader.Value.Trim('\r', '\n');

                                if (reader.Name != "Phrase") //text nebyl prazdny
                                {
                                    reader.Read();//text;
                                    reader.ReadEndElement();//Text;
                                }

                                if (reader.Name == "TextPrepisovany")
                                {
                                    reader.ReadElementString();
                                }

                                p.Phrases.Add(ph);
                                reader.ReadEndElement();//Phrase;
                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.SpeakerID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.SpeakerID == -1)
                                p.SpeakerID = Speaker.DefaultID;

                            reader.ReadEndElement();//paragraph

                            //zarovnani fonetiky k textu


                            TranscriptionParagraph bestpar = null;
                            TimeSpan timeinboth = TimeSpan.Zero;

                            if (p.Phrases.Count == 0)
                                continue;


                            TimeSpan minusone = new TimeSpan(-1);

                            foreach (TranscriptionParagraph v in s.Paragraphs)
                            {
                                if (v.End < p.Begin && v.End != minusone && p.Begin != minusone)
                                    continue;

                                if (v.Begin > p.End && v.End != minusone && v.Begin != minusone)
                                    continue;

                                TimeSpan beg = v.Begin > p.Begin ? v.Begin : p.Begin;
                                TimeSpan end;

                                if (v.End < p.End)
                                {
                                    end = v.End;
                                    if (v.End == minusone)
                                        end = p.End;
                                }
                                else
                                {
                                    end = p.End;
                                    if (p.End == minusone)
                                        end = v.End;
                                }

                                TimeSpan duration = end - beg;





                                if (bestpar == null)
                                {
                                    bestpar = v;
                                    timeinboth = duration;
                                }
                                else
                                {
                                    if (duration > timeinboth)
                                    {
                                        timeinboth = duration;
                                        bestpar = v;
                                    }
                                }
                            }

                            if (bestpar != null)
                            {
                                if (p.Phrases.Count == bestpar.Phrases.Count)
                                {
                                    for (int i = 0; i < p.Phrases.Count; i++)
                                    {
                                        bestpar.Phrases[i].Phonetics = p.Phrases[i].Text;
                                    }
                                }
                                else
                                {
                                    int i = 0;
                                    int j = 0;

                                    TimeSpan actual = p.Phrases[i].Begin;
                                    while (i < p.Phrases.Count && j < bestpar.Phrases.Count)
                                    {
                                        TranscriptionPhrase to = p.Phrases[i];
                                        TranscriptionPhrase from = bestpar.Phrases[j];
                                        if (true)
                                        {

                                        }
                                        i++;
                                    }
                                }

                            }
                        }
                        if (reader.Name == "PhoneticParagraphs" && reader.NodeType == XmlNodeType.EndElement)
                            reader.ReadEndElement();


                        if (!(reader.Name == "Section" && reader.NodeType == XmlNodeType.EndElement))
                        {

                            if (reader.Name != "speaker")
                                reader.Read();

                            int spkr = XmlConvert.ToInt32(reader.ReadElementString("speaker"));
                            s.Speaker = (spkr < 0) ? Speaker.DefaultID : spkr;

                        }
                        c.Sections.Add(s);
                        reader.ReadEndElement();//section
                    }

                    if (reader.Name == "Sections")
                        reader.ReadEndElement();//sections
                    reader.ReadEndElement();//chapter
                    data.Chapters.Add(c);
                }

                reader.ReadEndElement();//chapters
                reader.ReadStartElement("SpeakersDatabase");
                reader.ReadStartElement("Speakers");


                while (reader.Name == "Speaker")
                {
                    bool end = false;

                    Speaker sp = new Speaker();
                    reader.ReadStartElement("Speaker");
                    while (!end)
                    {
                        switch (reader.Name)
                        {
                            case "ID":

                                sp.ID = XmlConvert.ToInt32(reader.ReadElementString("ID"));
                                break;
                            case "Surname":
                                sp.Surname = reader.ReadElementString("Surname");
                                break;
                            case "Firstname":
                                sp.FirstName = reader.ReadElementString("Firstname");
                                break;
                            case "FirstName":
                                sp.FirstName = reader.ReadElementString("FirstName");
                                break;
                            case "Sex":
                                {
                                    string ss = reader.ReadElementString("Sex");

                                    if (new[] { "male", "m", "muž" }.Contains(ss.ToLower()))
                                    {
                                        sp.Sex = Speaker.Sexes.Male;
                                    }
                                    else if (new[] { "female", "f", "žena" }.Contains(ss.ToLower()))
                                    {
                                        sp.Sex = Speaker.Sexes.Female;
                                    }
                                    else
                                        sp.Sex = Speaker.Sexes.X;

                                }
                                break;
                            case "Comment":
                                sp.Comment = reader.ReadElementString("Comment");
                                break;
                            case "Speaker":
                                if (reader.NodeType == XmlNodeType.EndElement)
                                {
                                    reader.ReadEndElement();
                                    end = true;
                                }
                                else
                                    goto default;
                                break;

                            default:
                                if (reader.IsEmptyElement)
                                    reader.Read();
                                else
                                    reader.ReadElementString();
                                break;
                        }
                    }
                    data.m_speakers.Speakers.Add(sp);
                    storage.AssingSpeakersByID();
                }
            }
            catch (Exception ex)
            {
                if (reader != null)
                    throw new NanoTransSerializationException(string.Format("Chyba pri deserializaci souboru:(řádek:{0}, pozice:{1}) {2}", reader.LineNumber, reader.LinePosition, ex.Message), ex);
                else
                    throw new NanoTransSerializationException("Chyba pri deserializaci souboru: " + ex.Message, ex);
            }

        }

        /// <summary>
        /// Assigns Speaker (from internal Speaker pool Transcription.Speakers) to all paragraphs in Transcription by ID. Default speaker (Speaker.DefaultSpeaker) is assgned when no speaker si found
        /// </summary>
        public void AssingSpeakersByID()
        {
            foreach (var par in this.Where(e => e.IsParagraph).Cast<TranscriptionParagraph>())
            {
                var sp = GetSpeakerByID(par.SpeakerID);
                if (sp != null)
                {
                    par.Speaker = sp;
                }
                else
                {
                    par.Speaker = Speaker.DefaultSpeaker;
                }
            }
        }


        public TranscriptionChapter LastChapter
        {
            get { return Chapters.Last(); }
        }

        public TranscriptionSection LastSection
        {
            get { return Chapters.Last().Sections.Last(); }
        }

        public TranscriptionParagraph LastParagraph
        {
            get { return Chapters.Last().Sections.Last().Paragraphs.Last(); }
        }


        #region IList<TranscriptionElement> Members

        public int IndexOf(TranscriptionElement item)
        {
            int i = 0;
            if (Chapters.Count == 0)
                return -1;

            TranscriptionElement cur = Chapters[0];
            while (cur != null && cur != item)
            {
                i++;
                cur = cur.NextSibling();
            }


            return i;
        }

        public override void Insert(int index, TranscriptionElement item)
        {
            throw new NotSupportedException();
        }

        public override TranscriptionElement this[int index]
        {
            get
            {
                int i = 0;
                foreach (TranscriptionChapter c in Chapters)
                {
                    if (i == index)
                        return c;
                    i++;
                    if (index < i + c.GetTotalChildrenCount())
                    {
                        foreach (TranscriptionSection s in c.Sections)
                        {
                            if (i == index)
                                return s;
                            i++;
                            if (index < i + s.GetTotalChildrenCount())
                            {
                                return s.Paragraphs[index - i];

                            }
                            i += s.GetTotalChildrenCount();
                        }

                    }
                    i += c.GetTotalChildrenCount();
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection<TranscriptionElement> Members

        public override void Add(TranscriptionElement item)
        {
            if (item is TranscriptionChapter)
            {
                base.Add(item);
                this.ChildrenCountChanged(ChangedAction.Add);
            }
            else if (item is TranscriptionSection)
            {
                m_children[m_children.Count - 1].Add(item);
            }
            else if (item is TranscriptionParagraph)
            {
                m_children[m_children.Count - 1].Children[m_children[m_children.Count - 1].Children.Count - 1].Add(item);
            }
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TranscriptionElement item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(TranscriptionElement[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return Chapters.Sum(x => x.GetTotalChildrenCount()) + Chapters.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public override bool Remove(TranscriptionElement item)
        {
            if (item is TranscriptionChapter)
            {
                return base.Remove(item);
            }

            foreach (TranscriptionElement el in this)
            {
                if (el == item)
                {
                    return item.Parent.Remove(item);
                }
            }

            return false;
        }

        #endregion

        #region IEnumerable<TranscriptionElement> Members

        public IEnumerator<TranscriptionElement> GetEnumerator()
        {
            foreach (TranscriptionChapter c in this.Chapters)
            {
                yield return c;

                foreach (TranscriptionSection s in c.Sections)
                {
                    yield return s;

                    foreach (TranscriptionParagraph p in s.Paragraphs)
                    {
                        yield return p;
                    }
                }
            }
            yield break;
        }


        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        public event Action SubtitlesChanged;


        public override void ChildrenCountChanged(ChangedAction action)
        {
            if (SubtitlesChanged != null)
                SubtitlesChanged();
            //if (CollectionChanged != null)
            //{
            //    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            //}
        }

        public override string Text
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public override string Phonetics
        {
            get
            {
                return null;
            }
            set
            {
            }
        }


        public override void ElementChanged(TranscriptionElement element)
        {

        }

        public override void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {

        }

        public override void ElementInserted(TranscriptionElement element, int absoluteindex)
        {

        }

        public override void ElementRemoved(TranscriptionElement element, int absoluteindex)
        {

        }

        public override int AbsoluteIndex
        {
            get { throw new NotSupportedException(); }
        }
    }
}
