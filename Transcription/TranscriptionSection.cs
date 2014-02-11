using System;
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
        public static TranscriptionSection DeserializeV2(XElement e, bool isStrict)
        {
            TranscriptionSection tsec = new TranscriptionSection();
            tsec.name = e.Attribute("name").Value;
            tsec.elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            tsec.elements.Remove("name");
            foreach (var p in e.Elements(isStrict ? "paragraph" : "pa").Select(p => (TranscriptionElement)TranscriptionParagraph.DeserializeV2(p, isStrict)))
                tsec.Add(p);

            return tsec;
        }

        public TranscriptionSection(XElement e)
        {
            this.Paragraphs = new VirtualTypeList<TranscriptionParagraph>(this);
            name = e.Attribute("name").Value;
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove("name");
            foreach(var p in e.Elements("pa").Select(p => (TranscriptionElement)new TranscriptionParagraph(p)))
                Add(p);

        }

        public XElement Serialize(bool strict)
        {

            XElement elm = new XElement("se", elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { new XAttribute("name", name), }),
                Paragraphs.Select(p => p.Serialize())
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
            return _children.Count;
        }

        public override int AbsoluteIndex
        {
            get
            {

                if (_Parent != null)
                {
                    
                    int sum = _Parent.AbsoluteIndex;//parent absolute index index
                    sum += _Parent.Children.Take(this.ParentIndex) //take previous siblings
                        .Sum(s => s.GetTotalChildrenCount()); //+ all pre siblings counts (index on sublayers)

                    sum += ParentIndex; //+ parent index (index on sibling layer)
                    //... sum = all previous

                    sum++;//+1 - this
                   // this.Text = sum.ToString();
                    return sum;

                }

                return 0;
            }
        }
    }
}
