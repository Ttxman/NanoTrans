using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace TranscriptionCore
{
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
                    if (pr != null && pr.Speaker.FullName.ToLower().Contains(pattern.ToLower()))
                    {
                        paragraph = pr;
                        TextOffset = 0;
                        return true;
                    }
                    prs = prs.Next();
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

        private bool _saved;
        public bool Saved
        {
            get
            {
                return _saved;
            }
            set
            {
                _saved = value;
            }
        }

        public string DocumentID { get; set; }
        public DateTime Created { get; set; }

        /// <summary>
        /// transcription source, not mandatory
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// transcription type, not mandatory
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// file containing audio data for transcription
        /// </summary>
        public string MediaURI { get; set; }
        /// <summary>
        /// file containing video data - can be same as audio 
        /// </summary>
        public string VideoFileName { get; set; }

        


        private VirtualTypeList<TranscriptionChapter> _Chapters;

        /// <summary>
        /// all chapters in transcription
        /// </summary>
        public VirtualTypeList<TranscriptionChapter> Chapters
        {
            get { return _Chapters; }
            private set { _Chapters = value; }
        }

        [XmlElement("SpeakersDatabase")]
        private SpeakerCollection _speakers = new SpeakerCollection();

        [XmlIgnore]
        public SpeakerCollection Speakers
        {
            get { return _speakers; }
            set { _speakers = value; }
        }



        public Transcription()
        {
            FileName = null;
            Saved = false;
            DocumentID = Guid.NewGuid().ToString();
            Chapters = new VirtualTypeList<TranscriptionChapter>(this,this._children);
            Created = DateTime.UtcNow;
            //constructor  
        }



        /// <summary>
        /// copy contructor
        /// </summary>
        /// <param name="toCopy"></param>
        public Transcription(Transcription toCopy)
            : this()
        {
            this.Source = toCopy.Source;
            this.MediaURI = toCopy.MediaURI;
            this.VideoFileName = toCopy.VideoFileName;
            this.Type = toCopy.Type;
            this.Created = toCopy.Created;
            if (toCopy.Chapters != null)
            {
                this.Chapters = new VirtualTypeList<TranscriptionChapter>(this, this._children);
                for (int i = 0; i < toCopy.Chapters.Count; i++)
                {
                    this.Chapters.Add(new TranscriptionChapter(toCopy.Chapters[i]));
                }
            }
            this.FileName = toCopy.FileName;
            this._speakers = new SpeakerCollection(toCopy._speakers);
            this.Saved = toCopy.Saved;
        }

        /// <summary>
        /// automaticly deserialize from file
        /// </summary>
        /// <param name="path"></param>
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
        public List<TranscriptionParagraph> ReturnElementsAtTime(TimeSpan time)
        {
            List<TranscriptionParagraph> toret = new List<TranscriptionParagraph>();
            foreach (var el in this)
            {
                if (el.IsParagraph && el.Begin <= time && el.End > time)
                {
                    toret.Add((TranscriptionParagraph)el);
                }
            }
            return toret;
        }

        public TranscriptionParagraph ReturnLastElemenWithEndBeforeTime(TimeSpan cas)
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


        public TranscriptionParagraph ReturnLastElemenWithBeginBeforeTime(TimeSpan cas)
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

        /// <summary>
        /// Remove speaker from all paragraphs (Paragraphs will return default Speaker.DefaultSpeaker) and from internal database (if not pinned)
        /// </summary>
        /// <param name="speaker"></param>
        /// <returns></returns>
        public bool RemoveSpeaker(Speaker speaker)
        {
            try
            {
                if (speaker.FullName != null && speaker.FullName != "")
                {
                    if (this._speakers.Contains(speaker))
                    {
                        if (!speaker.PinnedToDocument)
                            _speakers.Remove(speaker);

                        Saved = false;
                        foreach (var p in EnumerateParagraphs())
                            if (p.Speaker == speaker)
                                p.Speaker = Speaker.DefaultSpeaker;

                        Saved = false;
                        return true;
                    }
                    return false;

                }
                else return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Replace speaker in all paragraphs and in databse
        /// !!!also can modify Transcription.Speakers order
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public void ReplaceSpeaker(Speaker toReplace, Speaker replacement)
        {
            if (_speakers.Contains(toReplace))
            {
                _speakers.Remove(toReplace);
                if (!_speakers.Contains(replacement))
                    _speakers.Add(replacement);
            }

            foreach (var p in EnumerateParagraphs())
                if (p.Speaker == toReplace)
                    p.Speaker = replacement;

        }

        public bool Serialize(string filename, bool savecompleteSpeakers = false)
        {
            using (FileStream s = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
            {

                bool output = Serialize(s, savecompleteSpeakers);

                if (output)
                {
                    this.FileName = filename;
                    this.Saved = true;

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public XDocument Serialize(bool SaveSpeakersDetailed = false)
        {
            ReindexSpeakers();
            XElement pars = new XElement("transcription", Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { 
                        new XAttribute("version", "3.0"), 
                        new XAttribute("mediauri", MediaURI ?? ""),
                        new XAttribute("created",Created)
                        }),
                        this.Meta,
                        Chapters.Select(c => c.Serialize()),
                        SerializeSpeakers(SaveSpeakersDetailed)
                    );

            if (!string.IsNullOrWhiteSpace(this.DocumentID))
                pars.Add(new XAttribute("documentid", this.DocumentID));

            XDocument xdoc =
                new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    pars
                );

            return xdoc;
        }


        public bool Serialize(Stream datastream, bool SaveSpeakersDetailed = false)
        {

            XDocument xdoc = Serialize(SaveSpeakersDetailed);
            xdoc.Save(datastream);
            return true;
        }

        private void ReindexSpeakers()
        {
            var speakers = this.EnumerateParagraphs().Select(p => p.Speaker).Where(s => s != Speaker.DefaultSpeaker && s.ID != Speaker.DefaultID).Distinct().ToList();
            for (int i = 0; i < speakers.Count; i++)
            {
                speakers[i].ID = i;
            }
        }

        private XElement SerializeSpeakers(bool SaveSpeakersDetailed)
        {
            var speakers = this.EnumerateParagraphs().Select(p => p.Speaker).Where(s => s != Speaker.DefaultSpeaker && s.ID != Speaker.DefaultID)
                            .Concat(_speakers.Where(s => s.PinnedToDocument))
                            .Distinct()
                .ToList();
            return new SpeakerCollection(speakers).Serialize(SaveSpeakersDetailed);
        }


        public static Transcription Deserialize(string filename)
        {
            Transcription tr = new Transcription();
            Deserialize(filename, tr);
            return tr;
        }


        public static void Deserialize(string filename, Transcription storage)
        {
            using (Stream s = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
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

           
        public static void Deserialize(Stream datastream, Transcription storage)
        {
            storage.BeginUpdate(false);
            XmlTextReader reader = new XmlTextReader(datastream);
            if (reader.Read())
            {
                reader.Read();
                reader.Read();
                string version = reader.GetAttribute("version");

                if (version == "3.0")
                {
                    DeserializeV3(reader, storage);
                }
                else if (version == "2.0")
                {
                    DeserializeV2_0(reader, storage);
                }
                else
                {
                    datastream.Position = 0;
                    DeserializeV1(new XmlTextReader(datastream), storage);
                }
            }
            storage.EndUpdate();
        }

        public XElement Meta = EmptyMeta();
        private static XElement EmptyMeta()
        {
            return new XElement("meta");
        }

        public Dictionary<string, string> Elements = new Dictionary<string, string>();

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
            data.MediaURI = mediaURI;
            data.Meta = transcription.Element("meta");
            if(data.Meta == null)
                data.Meta = EmptyMeta();
            var chapters = transcription.Elements(isStrict ? "chapter" : "ch");

            data.Elements = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            data.Elements.Remove("style");
            data.Elements.Remove("mediaURI");
            data.Elements.Remove("version");

            chapters.Select(c => (TranscriptionElement)TranscriptionChapter.DeserializeV2(c, isStrict)).ToList().ForEach(c => data.Add(c));

            var speakers = transcription.Element(isStrict ? "speakers" : "sp");
            data.Speakers.Clear();
            data.Speakers.AddRange(speakers.Elements(isStrict ? "speaker" : "s").Select(s => Speaker.DeserializeV2(s, isStrict)));
            storage.AssingSpeakersByID();
            data.EndUpdate();
        }

        /// <summary>
        /// deserialize transcription in v3 format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">data will be deserialized into this object, useful for overrides </param>
        private static void DeserializeV3(XmlTextReader reader, Transcription storage)
        {

            Transcription data = storage;
            data.BeginUpdate();
            var document = XDocument.Load(reader);
            var transcription = document.Elements().First();

            string version = transcription.Attribute("version").Value;
            string mediaURI = transcription.Attribute("mediauri").Value;
            data.MediaURI = mediaURI;
            data.Meta = transcription.Element("meta");
            if(data.Meta == null)
                data.Meta = EmptyMeta();
            var chapters = transcription.Elements("ch");


            data.Elements = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            string did;
            if (data.Elements.TryGetValue("documentid", out did))
                data.DocumentID = did;
            if (data.Elements.TryGetValue("created", out did))
                data.Created = XmlConvert.ToDateTime(did, XmlDateTimeSerializationMode.Unspecified);


            data.Elements.Remove("style");
            data.Elements.Remove("mediauri");
            data.Elements.Remove("version");
            data.Elements.Remove("documentid");
            data.Elements.Remove("created");

            foreach (var c in chapters.Select(c => new TranscriptionChapter(c)))
                data.Add(c);

            data.Speakers = new SpeakerCollection(transcription.Element("sp"));
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
                data.MediaURI = reader.GetAttribute("audioFileName");
                string val = reader.GetAttribute("dateTime");
                if (val != null)
                    data.Created = XmlConvert.ToDateTime(val, XmlDateTimeSerializationMode.Local);

                reader.ReadStartElement("Transcription");

                int result;

                //reader.Read();
                reader.ReadStartElement("Chapters");
                //reader.ReadStartElement("Chapter");
                while (reader.Name == "Chapter")
                {
                    TranscriptionChapter c = new TranscriptionChapter();
                    c.Name = reader.GetAttribute("name");

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
                        s.Name = reader.GetAttribute("name");

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
                            p.AttributeString = reader.GetAttribute("Attributes");

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

                            p.InternalID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.InternalID == -1)
                                p.InternalID = Speaker.DefaultID;

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
                            p.AttributeString = reader.GetAttribute("Attributes");

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

                            p.InternalID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.InternalID == -1)
                                p.InternalID = Speaker.DefaultID;

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
                    sp.DBType = DBType.File;
                    sp.DBID = null;
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
                                var str = reader.ReadElementString("Comment");
                                if (string.IsNullOrWhiteSpace(str))
                                    break;
                                sp.Attributes.Add(new SpeakerAttribute("comment", "comment", str));
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
                    data._speakers.Add(sp);
                }

                storage.AssingSpeakersByID();
            }
            catch (Exception ex)
            {
                if (reader != null)
                    throw new TranscriptionSerializationException(string.Format("Deserialization error:(line:{0}, offset:{1}) {2}", reader.LineNumber, reader.LinePosition, ex.Message), ex);
                else
                    throw new TranscriptionSerializationException("Deserialization error: " + ex.Message, ex);
            }

        }

        /// <summary>
        /// Assigns Speaker (from internal Speaker pool Transcription.Speakers) to all paragraphs in Transcription by ID. Default speaker (Speaker.DefaultSpeaker) is assgned when no speaker si found
        /// </summary>
        public void AssingSpeakersByID()
        {
            foreach (var par in this.Where(e => e.IsParagraph).Cast<TranscriptionParagraph>())
            {
                var sp = _speakers.FirstOrDefault(s => s.ID == par.InternalID);
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


        public override TranscriptionElement this[TranscriptionIndex index]
        {
            get
            {
                ValidateIndexOrThrow(index);

                if (index.IsChapterIndex)
                {
                    if (index.IsSectionIndex)
                        return Chapters[index.Chapterindex][index];

                    return Chapters[index.Chapterindex];
                }

                throw new IndexOutOfRangeException("index");
            }
            set
            {
                ValidateIndexOrThrow(index);

                if (index.IsChapterIndex)
                {
                    if (index.IsSectionIndex)
                        Chapters[index.Chapterindex][index] = value;
                    else
                        Chapters[index.Chapterindex] = (TranscriptionChapter)value;
                }
                else
                    throw new IndexOutOfRangeException("index");

            }

        }


        public override void RemoveAt(TranscriptionIndex index)
        {
            ValidateIndexOrThrow(index);
            if (index.IsChapterIndex)
            {
                if (index.IsSectionIndex)
                    Chapters[index.Chapterindex].RemoveAt(index);
                else
                    Chapters.RemoveAt(index.Chapterindex);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }

        public override void Insert(TranscriptionIndex index, TranscriptionElement value)
        {
            ValidateIndexOrThrow(index);
            if (index.IsChapterIndex)
            {
                if (index.IsSectionIndex)
                    Chapters[index.Chapterindex].Insert(index, value);
                else
                    Chapters.Insert(index.Chapterindex,(TranscriptionChapter)value);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
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
            }
            else if (item is TranscriptionSection)
            {
                if (_children.Count == 0)
                    Add(new TranscriptionChapter());

                _children[_children.Count - 1].Add(item);
            }
            else if (item is TranscriptionParagraph)
            {
                if (_children.Count == 0)
                    Add(new TranscriptionChapter());

                if (_children[_children.Count - 1].Children.Count == 0)
                    Add(new TranscriptionSection());

                _children[_children.Count - 1].Children[_children[_children.Count - 1].Children.Count - 1].Add(item);
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

        public override string Text
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string Phonetics
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        public override int AbsoluteIndex
        {
            get { throw new NotSupportedException(); }
        }


        public IEnumerable<TranscriptionParagraph> EnumerateParagraphs()
        {
            return this.Where(p => p.IsParagraph).Cast<TranscriptionParagraph>();
        }

        public override string InnerText
        {
            get { return string.Join("\r\n", Children.Select(c => c.Text)); }
        }
    }
}
