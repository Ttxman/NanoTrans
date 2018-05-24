using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TranscriptionCore
{
    /// <summary>
    /// custom text based value for speaker V3+
    /// </summary>
    public class SpeakerAttribute
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }

        public SpeakerAttribute(string id, string name, string value)
        {
            ID = id;
            Name = name;
            Value = value;
        }

        public SpeakerAttribute(XElement elm)
        {
            this.Name = elm.Attribute("name").Value;

            DateTime date;
            try
            {
                date = XmlConvert.ToDateTime(elm.Attribute("date").Value, XmlDateTimeSerializationMode.Local); //stored in UTC convert to local
            }
            catch
            {
                if (DateTime.TryParse(elm.Attribute("date").Value, CultureInfo.CreateSpecificCulture("cs"), DateTimeStyles.None, out date))
                    date = TimeZoneInfo.ConvertTimeFromUtc(date, TimeZoneInfo.Local);
                else
                    date = DateTime.Now;
            }
            
            this.Date = date;
            this.Value = elm.Value;
        }

        //copy constructor
        public SpeakerAttribute(SpeakerAttribute a)
        {
            this.ID = a.ID;
            this.Name = a.Name;
            this.Value = a.Value;
            this.Date = a.Date;
        }

        public XElement Serialize()
        {
            return new XElement("a",
                new XAttribute("name",Name),
                new XAttribute("date",XmlConvert.ToString(Date, XmlDateTimeSerializationMode.Utc)),
                Value
                );
        }
    }
}
