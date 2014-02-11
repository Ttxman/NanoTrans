using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace NanoTrans.Core
{
    //mluvci
    public class Speaker
    {
        //Dovymyslet co se bude ukladat o mluvcim -- jmeno, pohlavi, obrazek, popis, atd...
        public int ID;
        /// <summary>
        /// GET vraci cele jmeno slozene z krestniho+prijmeni
        /// </summary>
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
        public string RozpoznavacMluvci;
        public string RozpoznavacJazykovyModel;
        public string RozpoznavacPrepisovaciPravidla;
        public string FotoJPGBase64;
        public string Comment;
        public int DefaultLang = 0;



        public Speaker()
        {

            ID = Speaker.DefaultID;
            FirstName = null;
            Surname = null;
            Sex = Sexes.X;
            RozpoznavacMluvci = null;
            RozpoznavacJazykovyModel = null;
            RozpoznavacPrepisovaciPravidla = null;
            FotoJPGBase64 = null;
            Comment = null;
            DefaultLang = 0;
        }
        #region serializace nova


        public static readonly List<string> Langs = new List<string>{"CZ","SK","RU","HR","PL","EN","DE"};
        [XmlIgnore]
        public Dictionary<string, string> Elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public Speaker(XElement s, bool isStrict)
        {
            ID = int.Parse(s.Attribute("id").Value);
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
                    new XAttribute("id", ID.ToString()),
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
        public Speaker(Speaker aSpeaker)
        {

            if (aSpeaker == null) aSpeaker = new Speaker();
            ID = aSpeaker.ID;
            FirstName = aSpeaker.FirstName;
            Surname = aSpeaker.Surname;
            Sex = aSpeaker.Sex;
            RozpoznavacMluvci = aSpeaker.RozpoznavacMluvci;
            RozpoznavacJazykovyModel = aSpeaker.RozpoznavacJazykovyModel;
            RozpoznavacPrepisovaciPravidla = aSpeaker.RozpoznavacPrepisovaciPravidla;
            FotoJPGBase64 = aSpeaker.FotoJPGBase64;
            Comment = aSpeaker.Comment;
            DefaultLang = aSpeaker.DefaultLang;
        }

        public Speaker(string aSpeakerFirstname, string aSpeakerSurname, Sexes aPohlavi, string aRozpoznavacMluvci, string aRozpoznavacJazykovyModel, string aRozpoznavacPrepisovaciPravidla, string aSpeakerFotoBase64, string aPoznamka) //constructor ktery vytvori speakera
        {
            ID = -1;
            FirstName = aSpeakerFirstname;
            Surname = aSpeakerSurname;
            Sex = aPohlavi;
            RozpoznavacMluvci = aRozpoznavacMluvci;
            RozpoznavacJazykovyModel = aRozpoznavacJazykovyModel;
            RozpoznavacPrepisovaciPravidla = aRozpoznavacPrepisovaciPravidla;
            FotoJPGBase64 = aSpeakerFotoBase64;
            Comment = aPoznamka;
        }

        public override string ToString()
        {
            return FullName + " (" +Langs[DefaultLang]+ ")";
        }

        public static readonly int DefaultID = int.MinValue;

    }
}