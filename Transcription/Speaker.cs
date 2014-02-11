using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace NanoTrans.Core
{
    public class Speaker
    {
        public List<SpeakerAttribute> Attributes = new List<SpeakerAttribute>();
        public static int speakersIndexCounter = 0;

        #region equality overriding
        //TODO: what about other fields?
        public override int GetHashCode()
        {
            return this.FullName.GetHashCode() ^ this.m_ID.GetHashCode();
        }


        public bool Equals(Speaker s)
        { 
           if (s == null)
                return false;

           if (s.m_ID == this.m_ID && s.FullName == this.FullName)
               return true;

           return false;
        }

        public override bool Equals(object obj)
        {
            Speaker s = obj as Speaker;
            return Equals(s);
        }

        public static bool operator ==(Speaker a, Speaker b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(Speaker a, Speaker b)
        {
            return !(a == b);
        }
        #endregion

        private int m_ID;
        private bool m_IDFixed = false;
        public bool IDFixed
        {
            get { return m_IDFixed; }
            set 
            {
                if (value)
                { 
                    m_IDFixed = true;
                }
                else if (m_IDFixed)
                {
                    
                }
            }
        }
        public void FixID()
        {
            m_IDFixed = true;
        }
        public int ID
        {
            get { return m_ID; }
            set
            {
                if (m_IDFixed)
                {

                    throw new ArgumentException("cannot chabge fixed speaker ID");
                }

                if (value >= speakersIndexCounter)
                {
                    speakersIndexCounter = value + 1;
                }
                m_ID = value;
            }
        }
        
        [XmlIgnore]
        public string FullName
        {
            get
            {
                string pJmeno = "";
                if (FirstName != null && FirstName.Length > 0)
                {
                    pJmeno += FirstName;
                }
                if (Surname != null && Surname.Length > 0)
                {
                    if (pJmeno.Length > 0) pJmeno += " ";
                    pJmeno += Surname;
                }
                if (string.IsNullOrEmpty(pJmeno))
                    pJmeno = "Mluvčí";
                return pJmeno;
            }
        }

        public enum Sexes : byte
        {
            X = 0,
            Male = 1,
            Female = 2

        }

        public string FirstName;
        public string Surname;
        public Sexes Sex;

        public string ImgBase64;
        public string Comment;
        public int DefaultLang = 0;


        public string DegreeBefore;
        public string SecondName;
        public string DegreeAfter;


        public Speaker()
        {

            m_ID = speakersIndexCounter++;
            FirstName = null;
            Surname = null;
            Sex = Sexes.X;
            ImgBase64 = null;
            Comment = null;
            DefaultLang = 0;
        }
        #region serializace nova


        public static readonly List<string> Langs = new List<string>{"CZ","SK","RU","HR","PL","EN","DE","ES","--"};
        [XmlIgnore]
        public Dictionary<string, string> Elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public Speaker(XElement s, bool isStrict)
        {
            m_ID = int.Parse(s.Attribute("id").Value);
            Surname = s.Attribute("surname").Value;
            FirstName = (s.Attribute("firstname") ?? EmptyAttribute).Value;

            switch ((s.Attribute("sex") ?? EmptyAttribute).Value)
            {
                case "M":
                    Sex = Sexes.Male;
                    break;
                case "F":
                    Sex = Sexes.Female;
                    break;
                default:
                    Sex = Sexes.X;
                    break;
            }

            Elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            string rem;
            if (Elements.TryGetValue("comment", out rem))
            {
                this.Comment = rem;
            }

            if (Elements.TryGetValue("lang", out rem))
            {
                int idx = Langs.IndexOf(rem);
                DefaultLang = (idx < 0) ? 0 : idx;
            }

            Elements.Remove("id");
            Elements.Remove("firstname");
            Elements.Remove("surname");
            Elements.Remove("sex");
            Elements.Remove("comment");
            Elements.Remove("lang");

        }

        public XElement Serialize(bool strict)
        {
            XElement elm = new XElement(strict ? "speaker" : "s",
                Elements.Select(e =>
                    new XAttribute(e.Key, e.Value))
                    .Union(new[]{ 
                    new XAttribute("id", m_ID.ToString()),
                    new XAttribute("surname",Surname),
                    new XAttribute("firstname",FirstName),
                    new XAttribute("sex",(Sex==Sexes.Male)?"M":(Sex==Sexes.Female)?"F":"X")
                    })
            );


            if (!string.IsNullOrWhiteSpace(Comment))
            { 
                elm.Add( new XAttribute("comment",Comment));
            }

            elm.Add(new XAttribute("lang", Langs[DefaultLang]));

            return elm;
        }
        #endregion

        /// <summary>
        /// kopie
        /// </summary>
        /// <param name="aSpeaker"></param>
        private Speaker(Speaker aSpeaker)
        {

            if (aSpeaker == null) aSpeaker = new Speaker();
            m_ID = speakersIndexCounter++;
            FirstName = aSpeaker.FirstName;
            Surname = aSpeaker.Surname;
            Sex = aSpeaker.Sex;
            ImgBase64 = aSpeaker.ImgBase64;
            Comment = aSpeaker.Comment;
            DefaultLang = aSpeaker.DefaultLang;
        }

        public Speaker(string aSpeakerFirstname, string aSpeakerSurname, Sexes aPohlavi, string aSpeakerFotoBase64, string aPoznamka) //constructor ktery vytvori speakera
        {
            m_ID = speakersIndexCounter++;
            FirstName = aSpeakerFirstname;
            Surname = aSpeakerSurname;
            Sex = aPohlavi;
            ImgBase64 = aSpeakerFotoBase64;
            Comment = aPoznamka;
        }

        public override string ToString()
        {
            return FullName + " (" +Langs[DefaultLang]+ ")";
        }

        public static readonly int DefaultID = int.MinValue;
        public static readonly Speaker DefaultSpeaker = new Speaker() { m_ID = DefaultID};

        public Speaker Copy()
        {
            return new Speaker(this);
        }
    }
}