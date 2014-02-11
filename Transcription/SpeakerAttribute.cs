using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace NanoTrans.Core
{
    /// <summary>
    /// custom text based value for speaker V3+
    /// </summary>
    public class SpeakerAttribute
    {
        private SpeakerAttribute a;

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
            this.Name = elm.Element("name").Value;
            this.Date = XmlConvert.ToDateTime(elm.Element("date").Value,XmlDateTimeSerializationMode.Unspecified);
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
                new XAttribute("date",Date),
                Value
                );
        }
    }
}
