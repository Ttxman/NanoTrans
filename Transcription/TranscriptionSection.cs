﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NanoTrans.Core
{
    //sekce textu nadrazena odstavci
    public class TranscriptionSection : TranscriptionElement
    {
        public override bool IsSection
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

        public VirtualTypeList<TranscriptionParagraph> Paragraphs;

        public int Speaker;




        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public TranscriptionSection(XElement e, bool isStrict)
        {
            this.Paragraphs = new VirtualTypeList<TranscriptionParagraph>(this);
            name = e.Attribute("name").Value;
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove("name");
            e.Elements(isStrict ? "paragraph" : "pa").Select(p => (TranscriptionElement)new TranscriptionParagraph(p, isStrict)).ToList().ForEach(p => Add(p));

        }

        public XElement Serialize(bool strict)
        {

            XElement elm = new XElement(strict ? "section" : "se", elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { new XAttribute("name", name), }),
                Paragraphs.Select(p => p.Serialize(strict))
            );

            return elm;
        }
        #endregion


        /// <summary>
        /// kopie objektu
        /// </summary>
        /// <param name="aKopie"></param>
        public TranscriptionSection(TranscriptionSection aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.name = aKopie.name;
            if (aKopie.Paragraphs != null)
            {
                this.Paragraphs = new VirtualTypeList<TranscriptionParagraph>(this);
                for (int i = 0; i < aKopie.Paragraphs.Count; i++)
                {
                    this.Paragraphs.Add(new TranscriptionParagraph(aKopie.Paragraphs[i]));
                }
            }
        }

        public TranscriptionSection()
        {
            Paragraphs = new VirtualTypeList<TranscriptionParagraph>(this);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
        }

        public TranscriptionSection(String aName)
            : this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {
        }
        public TranscriptionSection(String aName, TimeSpan aBegin, TimeSpan aEnd)
            : this()
        {
            this.name = aName;
            this.Begin = aBegin;
            this.End = aEnd;
        }

        public override int GetTotalChildrenCount()
        {
            return m_children.Count;
        }
    }

}