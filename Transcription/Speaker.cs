using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace NanoTrans.Core
{
    public class Speaker
    {
        public List<SpeakerAttribute> Attributes = new List<SpeakerAttribute>();
        public static int speakersIndexCounter = 0;

        private int _ID;
        private bool _IDFixed = false;
        public bool IDFixed
        {
            get { return _IDFixed; }
            set
            {
                if (value)
                {
                    _IDFixed = true;
                }
                else if (_IDFixed)
                {

                }
            }
        }


        private bool _PinnedToDocument = false;
        /// <summary>
        ///  == Do not delete from document, when not used in any paragraph
        /// </summary>
        public bool PinnedToDocument
        {
            get { return _PinnedToDocument; }
            set
            {
                _PinnedToDocument = value;
            }
        }

        public void FixID()
        {
            _IDFixed = true;
        }

        /// <summary>
        /// Serialization ID, Changed when Transcription is serialized. For user ID use DBID property
        /// </summary>
        public int ID
        {
            get { return _ID; }
            set
            {
                if (_IDFixed)
                {

                    throw new ArgumentException("cannot chabge fixed speaker ID");
                }

                if (value >= speakersIndexCounter)
                {
                    speakersIndexCounter = value + 1;
                }
                _ID = value;
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
                if (MiddleName != null && MiddleName.Length > 0)
                {
                    pJmeno += " " + MiddleName;
                }

                if (Surname != null && Surname.Length > 0)
                {
                    if (pJmeno.Length > 0) pJmeno += " ";
                    pJmeno += Surname;
                }

                if (string.IsNullOrEmpty(pJmeno))
                    pJmeno = "---";
                return pJmeno;
            }
        }

        public enum Sexes : byte
        {
            X = 0,
            Male = 1,
            Female = 2

        }

        private string _firstName = "";
        public string FirstName
        {
            get
            {
                return _firstName;
            }

            set
            {
                _firstName = value ?? "";
            }
        }
        private string _surName = "";
        public string Surname
        {
            get
            {
                return _surName;
            }

            set
            {
                _surName = value ?? "";
            }
        }
        public Sexes Sex;

        public string ImgBase64;

        string _defaultLang = null;
        public string DefaultLang
        {
            get
            {
                return _defaultLang ?? Langs[0];
            }

            set
            {
                _defaultLang = value;
            }
        }


        public string DegreeBefore;
        public string MiddleName;
        public string DegreeAfter;


        public Speaker()
        {

            _ID = speakersIndexCounter++;
            FirstName = null;
            Surname = null;
            Sex = Sexes.X;
            ImgBase64 = null;
            DefaultLang = Langs[0];
        }
        #region serializace nova


        public static readonly List<string> Langs = new List<string> { "CZ", "SK", "RU", "HR", "PL", "EN", "DE", "ES", "IT", "CU", "--" };
        [XmlIgnore]
        public Dictionary<string, string> Elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");

        internal static Speaker DeserializeV2(XElement s, bool isStrict)
        {
            Speaker sp = new Speaker();

            if (!s.CheckRequiredAtributes("id", "surname"))
                throw new ArgumentException("required attribute missing on v2format speaker  (id, surname)");

            sp._ID = int.Parse(s.Attribute("id").Value);
            sp.Surname = s.Attribute("surname").Value;
            sp.FirstName = (s.Attribute("firstname") ?? EmptyAttribute).Value;

            switch ((s.Attribute("sex") ?? EmptyAttribute).Value)
            {
                case "M":
                    sp.Sex = Sexes.Male;
                    break;
                case "F":
                    sp.Sex = Sexes.Female;
                    break;
                default:
                    sp.Sex = Sexes.X;
                    break;
            }

            sp.Elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            string rem;
            if (sp.Elements.TryGetValue("comment", out rem))
            {
                SpeakerAttribute sa = new SpeakerAttribute("comment", "comment", rem);
                sp.Attributes.Add(sa);
            }

            if (sp.Elements.TryGetValue("lang", out rem))
            {
                int idx = Langs.IndexOf(rem);
                sp.DefaultLang = rem;
            }

            sp.Elements.Remove("id");
            sp.Elements.Remove("firstname");
            sp.Elements.Remove("surname");
            sp.Elements.Remove("sex");
            sp.Elements.Remove("comment");
            sp.Elements.Remove("lang");

            return sp;
        }
        internal static CultureInfo csCulture = CultureInfo.CreateSpecificCulture("cs");
        public Speaker(XElement s)//V3 format
        {
            if (!s.CheckRequiredAtributes("id", "surname", "firstname", "sex", "lang"))
                throw new ArgumentException("required attribute missing on speaker (id, surname, firstname, sex, lang)");

            _ID = int.Parse(s.Attribute("id").Value);
            Surname = s.Attribute("surname").Value;
            FirstName = (s.Attribute("firstname") ?? EmptyAttribute).Value;

            switch ((s.Attribute("sex") ?? EmptyAttribute).Value)
            {
                case "m":
                    Sex = Sexes.Male;
                    break;
                case "f":
                    Sex = Sexes.Female;
                    break;
                default:
                    Sex = Sexes.X;
                    break;
            }

            DefaultLang = s.Attribute("lang").Value.ToUpper();

            //merges
            this.Merges.AddRange(s.Elements("m").Select(m => new DBMerge(m.Attribute("dbid").Value, stringToDBType(m.Attribute("dbtype").Value))));

            Attributes.AddRange(s.Elements("a").Select(e => new SpeakerAttribute(e)));

            Elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);


            string rem;
            if (Elements.TryGetValue("dbid", out rem))
            {
                DBID = rem;
                if (Elements.TryGetValue("dbtype", out rem))
                    this.DataBaseType = stringToDBType(rem);

            }

            if (Elements.TryGetValue("synchronized", out rem))
            {
                DateTime date;

                //problem with saving datetimes in local format
                try
                {
                    date = XmlConvert.ToDateTime(rem, XmlDateTimeSerializationMode.Local); //stored in UTC convert to local
                }
                catch
                {
                    if (DateTime.TryParse(rem, csCulture, DateTimeStyles.None, out date))
                        date = TimeZoneInfo.ConvertTimeFromUtc(date, TimeZoneInfo.Local);
                    else
                        date = DateTime.Now;
                }
                this.Synchronized = date;
            }


            if (Elements.TryGetValue("middlename", out rem))
                this.MiddleName = rem;

            if (Elements.TryGetValue("degreebefore", out rem))
                this.DegreeBefore = rem;

            if (Elements.TryGetValue("pinned", out rem))
                this.PinnedToDocument = XmlConvert.ToBoolean(rem);



            Elements.Remove("id");
            Elements.Remove("surname");
            Elements.Remove("firstname");
            Elements.Remove("sex");
            Elements.Remove("lang");

            Elements.Remove("dbid");
            Elements.Remove("dbtype");
            Elements.Remove("middlename");
            Elements.Remove("degreebefore");
            Elements.Remove("degreeafter");
            Elements.Remove("synchronized");

            Elements.Remove("pinned");


        }

        private DBType stringToDBType(string rem)
        {
            switch (rem)
            {
                case "user":
                    return DBType.User;
                case "api":
                    return DBType.Api;

                case "file":
                    return DBType.File;

                default:
                    goto case "file";
            }
        }

        /// <summary>
        /// serialize speaker
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public XElement Serialize(bool saveAll = false) //v3
        {
            XElement elm = new XElement("s",
                Elements.Select(e =>
                    new XAttribute(e.Key, e.Value))
                    .Union(new[]{ 
                    new XAttribute("id", _ID.ToString()),
                    new XAttribute("surname",Surname),
                    new XAttribute("firstname",FirstName),
                    new XAttribute("sex",(Sex==Sexes.Male)?"m":(Sex==Sexes.Female)?"f":"x"),
                    new XAttribute("lang",DefaultLang.ToLower())

                    })
            );

            string val = "file";
            if (DataBaseType != DBType.File)
            {
                elm.Add(new XAttribute("dbid", this.DBID));
                if (this.DataBaseType == DBType.Api)
                    val = "api";
                else if (DataBaseType == DBType.User)
                    val = "user";
                else
                    val = "file";
                elm.Add(new XAttribute("dbtype", val));
            }

            if (!string.IsNullOrWhiteSpace(MiddleName))
                elm.Add(new XAttribute("middlename", MiddleName));

            if (!string.IsNullOrWhiteSpace(DegreeBefore))
                elm.Add(new XAttribute("degreebefore", DegreeBefore));

            if (!string.IsNullOrWhiteSpace(DegreeAfter))
                elm.Add(new XAttribute("degreeafter", DegreeAfter));

            if (DataBaseType != DBType.File)
                elm.Add(new XAttribute("synchronized", XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc)));//stored in UTC convert from local

            if (PinnedToDocument)
                elm.Add(new XAttribute("pinned", true));

            if (saveAll)
                foreach (var m in Merges)
                    elm.Add(m.Serialize());

            foreach (var a in Attributes)
                elm.Add(a.Serialize());

            return elm;
        }
        #endregion

        /// <summary>
        /// copy constructor - copies all info, but with new DBID, ID ....
        /// </summary>
        /// <param name="s"></param>
        private Speaker(Speaker aSpeaker)
        {
            _ID = speakersIndexCounter++;
            DataBaseType = aSpeaker.DataBaseType;
            Surname = aSpeaker.Surname;
            FirstName = aSpeaker.FirstName;
            MiddleName = aSpeaker.MiddleName;
            DegreeBefore = aSpeaker.DegreeBefore;
            DegreeAfter = aSpeaker.DegreeAfter;
            DefaultLang = aSpeaker.DefaultLang;
            Sex = aSpeaker.Sex;
            ImgBase64 = aSpeaker.ImgBase64;
            Merges = new List<DBMerge>(aSpeaker.Merges);

            foreach (var a in aSpeaker.Attributes)
            {
                Attributes.Add(new SpeakerAttribute(a));
            }

        }

        public Speaker(string aSpeakerFirstname, string aSpeakerSurname, Sexes aPohlavi, string aSpeakerFotoBase64) //constructor ktery vytvori speakera
        {
            _ID = speakersIndexCounter++;
            FirstName = aSpeakerFirstname;
            Surname = aSpeakerSurname;
            Sex = aPohlavi;
            ImgBase64 = aSpeakerFotoBase64;
        }


        public override string ToString()
        {
            return FullName + " (" + DefaultLang + ")";
        }

        public static readonly int DefaultID = int.MinValue;
        public static readonly Speaker DefaultSpeaker = new Speaker() { _ID = DefaultID, DBID = new Guid().ToString() };

        /// <summary>
        /// copies all info, and generates new DBI and ID .... (deep copy)
        /// </summary>
        /// <param name="s"></param>
        public Speaker Copy()
        {
            return new Speaker(this);
        }

        string _dbid = null;

        /// <summary>
        /// if not set, GUID is automatically generated on first access (when database is not API based)
        /// if DataBaseType is DBType.API and not set - returns null
        /// if DataBaseType is DBType.User - modification is disabled
        /// </summary>
        public string DBID
        {
            get
            {
                if (_dbid == null)
                {
                    if (DataBaseType == DBType.Api)
                        return null;
                    else
                        _dbid = Guid.NewGuid().ToString();
                }

                return _dbid;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(_dbid))
                    _dbid = value;
                else if (DataBaseType == DBType.User)
                    throw new ArgumentException("cannot change DBID when Dabase is User");
                else
                    _dbid = value;

            }
        }

        public DBType DataBaseType { get; set; }

        public DateTime Synchronized { get; set; }

        public List<DBMerge> Merges = new List<DBMerge>();
    }
}