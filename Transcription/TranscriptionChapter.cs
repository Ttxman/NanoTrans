﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NanoTrans.Core
{
    //kapitola
    public class TranscriptionChapter : TranscriptionElement
    {
        public override bool IsChapter
        {
            get
            {
                return true;
            }
        }
        public override string Text
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
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

        public string name = string.Empty;

        public VirtualTypeList<TranscriptionSection> Sections;


        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public TranscriptionChapter(XElement c, bool isStrict)
        {
            Sections = new VirtualTypeList<TranscriptionSection>(this);
            name = c.Attribute("name").Value;
            elements = c.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove("name");
            c.Elements(isStrict ? "section" : "se").Select(s => (TranscriptionElement)new TranscriptionSection(s, isStrict)).ToList().ForEach(s => Add(s));

        }

        public XElement Serialize(bool strict)
        {

            XElement elm = new XElement(strict ? "chapter" : "ch",
                elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { new XAttribute("name", name), }),
                Sections.Select(s => s.Serialize(strict))
            );

            return elm;
        }
        #endregion

        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public TranscriptionChapter(TranscriptionChapter aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.name = aKopie.name;
            if (aKopie.Sections != null)
            {
                this.Sections = new VirtualTypeList<TranscriptionSection>(this);
                for (int i = 0; i < aKopie.Sections.Count; i++)
                {
                    this.Sections.Add(new TranscriptionSection(aKopie.Sections[i]));
                }
            }
        }

        public TranscriptionChapter()
            : base()
        {
            Sections = new VirtualTypeList<TranscriptionSection>(this);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
        }

        public TranscriptionChapter(String aName)
            : this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {

        }
        public TranscriptionChapter(String aName, TimeSpan aBegin, TimeSpan aEnd)
        {
            Sections = new VirtualTypeList<TranscriptionSection>(this);
            this.name = aName;
            this.Begin = aBegin;
            this.End = aEnd;
        }

    }


}