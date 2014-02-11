using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NanoTrans.Core
{
    public class TranscriptionParagraph : TranscriptionElement
    {


        public override bool IsParagraph
        {
            get
            {
                return true;
            }
        }
        public VirtualTypeList<TranscriptionPhrase> Phrases; //nejmensi textovy usek
        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - jedna se o text ze vsech podrazenych textovych jednotek (Phrases)
        /// </summary>
        public override string Text
        {
            get
            {
                string ret = "";
                if (this.Phrases != null)
                {
                    for (int i = 0; i < this.Phrases.Count; i++)
                    {
                        ret += this.Phrases[i].Text;
                    }
                }

                return ret;
            }
            set { throw new NotImplementedException("pokus o vlození textu primo do odstavce"); }
        }


        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - fonetika
        /// </summary>
        public override string Phonetics
        {
            get
            {
                string ret = "";
                if (this.Phrases != null)
                {
                    for (int i = 0; i < this.Phrases.Count; i++)
                    {
                        ret += this.Phrases[i].Phonetics;
                    }
                }

                return ret;
            }
            set { }
        }

        public ParagraphAttributes DataAttributes = ParagraphAttributes.None;


        public string Attributes
        {
            get
            {
                ParagraphAttributes[] attrs = (ParagraphAttributes[])Enum.GetValues(typeof(ParagraphAttributes));
                string s = "";
                foreach (var attr in attrs)
                {
                    if (attr != ParagraphAttributes.None)
                    {
                        if ((DataAttributes & attr) != 0)
                        {
                            string val = Enum.GetName(typeof(ParagraphAttributes), attr);
                            if (s.Length > 0)
                            {
                                s += "|";
                            }

                            s += val;
                        }
                    }
                }

                if (s.Length == 0)
                {
                    return Enum.GetName(typeof(ParagraphAttributes), ParagraphAttributes.None);
                }
                else
                {
                    return s;
                }
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    DataAttributes = ParagraphAttributes.None;
                    return;
                }
                string[] vals = value.Split('|');
                ParagraphAttributes attrs = ParagraphAttributes.None;
                foreach (string val in vals)
                {
                    attrs |= (ParagraphAttributes)Enum.Parse(typeof(ParagraphAttributes), val);
                }
                this.DataAttributes = attrs;
            }
        }


        public void SilentEndUpdate()
        {
            _updated = false;
            EndUpdate();
        }

        private int _speakerID = Speaker.DefaultID;
        public int SpeakerID
        {
            get
            {
                if (_speaker == null)
                    return _speakerID;
                else
                    return _speaker.ID;
            }
            set
            {
                if (_speaker != null && _speakerID != Speaker.DefaultID)
                    throw new ArgumentException("cannot set speaker ID while Speaker is set");
                _speakerID = value;
            }
        }

        Speaker _speaker = null;
        public Speaker Speaker
        {
            get
            {
                return _speaker??Speaker.DefaultSpeaker;
            }
            set
            {
                _speaker = value;
                _speakerID = value.ID;
            }
        }

        /// <summary>
        /// informace zda je dany element zahrnut pro trenovani dat 
        /// </summary>
        public bool trainingElement;

        /// <summary>
        /// vraci delku odstavce v MS mezi begin a end, pokud neni nektera hodnota nezadana -1
        /// </summary>
        public TimeSpan Delka
        {
            get
            {
                if (Begin == new TimeSpan(-1) || End == new TimeSpan(-1))
                    return TimeSpan.Zero;

                return End - Begin;
            }
        }



        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");

        /// <summary>
        /// V2 deserialization beware of local variable names
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isStrict"></param>
        /// <returns></returns>
        public static TranscriptionParagraph DeserializeV2(XElement e, bool isStrict)
        {
            TranscriptionParagraph par = new TranscriptionParagraph();
            par._speakerID = int.Parse(e.Attribute(isStrict ? "speakerid" : "s").Value);
            par.Attributes = (e.Attribute(isStrict ? "attributes" : "a") ?? EmptyAttribute).Value;

            par.elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            par.elements.Remove(isStrict ? "begin" : "b");
            par.elements.Remove(isStrict ? "end" : "e");
            par.elements.Remove(isStrict ? "attributes" : "a");
            par.elements.Remove(isStrict ? "speakerid" : "s");


            e.Elements(isStrict ? "phrase" : "p").Select(p => (TranscriptionElement)TranscriptionPhrase.DeserializeV2(p, isStrict)).ToList().ForEach(p => par.Add(p)); ;

            if (e.Attribute(isStrict ? "attributes" : "a") != null)
                par.Attributes = e.Attribute(isStrict ? "attributes" : "a").Value;

            if (e.Attribute(isStrict ? "begin" : "b") != null)
            {
                string val = e.Attribute(isStrict ? "begin" : "b").Value;
                int ms;
                if (int.TryParse(val, out ms))
                    par.Begin = TimeSpan.FromMilliseconds(ms);
                else
                    par.Begin = XmlConvert.ToTimeSpan(val);
            }
            else
            {
                var ch = par._children.FirstOrDefault();
                par.Begin = ch == null ? TimeSpan.Zero : ch.Begin;
            }

            if (e.Attribute(isStrict ? "end" : "e") != null)
            {
                string val = e.Attribute(isStrict ? "end" : "e").Value;
                int ms;
                if (int.TryParse(val, out ms))
                    par.End = TimeSpan.FromMilliseconds(ms);
                else
                    par.End = XmlConvert.ToTimeSpan(val);
            }
            else
            {
                var ch = par._children.LastOrDefault();
                par.End = ch == null ? TimeSpan.Zero : ch.Begin;
            }

            return par;
        }

        public TranscriptionParagraph(XElement e)
        {

            if (!e.CheckRequiredAtributes("b", "e", "s"))
                throw new ArgumentException("required attribute missing on paragraph (b,e,s)");

            Phrases = new VirtualTypeList<TranscriptionPhrase>(this);
            _speakerID = int.Parse(e.Attribute( "s").Value);
            Attributes = (e.Attribute( "a") ?? EmptyAttribute).Value;

            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);



            foreach (var p in e.Elements("p").Select(p => (TranscriptionElement)new TranscriptionPhrase(p)))
                Add(p);

            string bfr;
            if (elements.TryGetValue("a", out bfr))
                this.Attributes = bfr;

            if (elements.TryGetValue("b", out bfr))
            {
                int ms;
                if (int.TryParse(bfr, out ms))
                    Begin = TimeSpan.FromMilliseconds(ms);
                else
                    Begin = XmlConvert.ToTimeSpan(bfr);
            }
            else
            {
                var ch = _children.FirstOrDefault();
                Begin = ch == null ? TimeSpan.Zero : ch.Begin;
            }

            if (elements.TryGetValue("e", out bfr))
            {
                int ms;
                if (int.TryParse(bfr, out ms))
                    End = TimeSpan.FromMilliseconds(ms);
                else
                    End = XmlConvert.ToTimeSpan(bfr);
            }
            else
            {
                var ch = _children.LastOrDefault();
                End = ch == null ? TimeSpan.Zero : ch.Begin;
            }



            
            if (elements.TryGetValue("l", out bfr))
            {
                Language = bfr;
            }

            elements.Remove("b");
            elements.Remove("e");
            elements.Remove("s");
            elements.Remove("a");
            elements.Remove("l");

        }

        public XElement Serialize()
        {
            XElement elm = new XElement("pa",
                elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { 
                    new XAttribute("b", Begin), 
                    new XAttribute("e", End), 
                    new XAttribute("a", Attributes), 
                    new XAttribute("s", _speakerID),
                    new XAttribute("l", Language),
                }),
                Phrases.Select(p => p.Serialize())
            );

            return elm;
        }
        #endregion


        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public TranscriptionParagraph(TranscriptionParagraph aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.trainingElement = aKopie.trainingElement;
            this.DataAttributes = aKopie.DataAttributes;
            if (aKopie.Phrases != null)
            {
                this.Phrases = new VirtualTypeList<TranscriptionPhrase>(this);
                for (int i = 0; i < aKopie.Phrases.Count; i++)
                {
                    this.Phrases.Add(new TranscriptionPhrase(aKopie.Phrases[i]));
                }
            }
            this._speakerID = aKopie._speakerID;
        }

        public TranscriptionParagraph(List<TranscriptionPhrase> phrases)
            : this()
        {
            foreach (var p in phrases)
                Add(p);
            if (Phrases.Count > 0)
            {
                this.Begin = Phrases[0].Begin;
                this.End = Phrases[Phrases.Count - 1].End;
            }
        }

        public TranscriptionParagraph()
            : base()
        {
            Phrases = new VirtualTypeList<TranscriptionPhrase>(this);
            this.Begin = new TimeSpan(-1);
            this.End = new TimeSpan(-1);
            this.trainingElement = false;
        }

        //when phraze is removed...
        public override void ElementRemoved(TranscriptionElement element, int absoluteindex)
        {
            base.ElementChanged(this);
        }
        //when phraze is inserted/added
        public override void ElementInserted(TranscriptionElement element, int absoluteindex)
        {
            base.ElementChanged(this);
        }

        //when phraze is replaced
        public override void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            base.ElementChanged(this);
        }

        public override int AbsoluteIndex
        {
            get
            {
                if (_Parent != null)
                {
                    int sum = _Parent.AbsoluteIndex + _ParentIndex + 1;
                    //this.Phrases.Clear();
                  //  this.Add(new TranscriptionPhrase(){Text = sum.ToString()});
                    return sum;
                }

                return 0;
            }
        }

        string _lang = null;
        public string Language 
        { 
            get
            {
               
                return _lang ?? Speaker.DefaultLang;
            }
            set
            {
                _lang = value;
            }
        }
    }

}
