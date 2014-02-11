using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NanoTrans.Core
{
    //nejmensi textovy usek - muze byt veta, vice slov nebo samotne slovo
    public sealed class TranscriptionPhrase : TranscriptionElement
    {
        private string m_text = "";//slovo/a ktere obsahuji i mezery na konci

        public override string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        private string m_phonetics = "";

        public override string Phonetics
        {
            get
            {
                return m_phonetics;
            }
            set
            {
                m_phonetics = value;
            }
        }



        #region serializace nova
        private Dictionary<string, string> elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");
        public TranscriptionPhrase(XElement e, bool isStrict)
        {
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elements.Remove(isStrict ? "begin" : "b");
            elements.Remove(isStrict ? "end" : "e");
            elements.Remove(isStrict ? "fon" : "f");


            this.m_phonetics = (e.Attribute(isStrict ? "fon" : "f") ?? EmptyAttribute).Value;
            this.m_text = e.Value.Trim('\r', '\n');
            if (e.Attribute(isStrict ? "begin" : "b") != null)
            {
                string val = e.Attribute(isStrict ? "begin" : "b").Value;
                int ms;
                if (int.TryParse(val, out ms))
                {
                    Begin = TimeSpan.FromMilliseconds(ms);
                }
                else
                    Begin = XmlConvert.ToTimeSpan(val);

            }

            if (e.Attribute(isStrict ? "end" : "e") != null)
            {
                string val = e.Attribute(isStrict ? "end" : "e").Value;
                int ms;
                if (int.TryParse(val, out ms))
                {
                    End = TimeSpan.FromMilliseconds(ms);
                }
                else
                    End = XmlConvert.ToTimeSpan(val);
            }

        }

        public XElement Serialize(bool strict)
        {
            XElement elm = new XElement(strict ? "phrase" : "p",
                elements.Select(e =>
                    new XAttribute(e.Key, e.Value))
                    .Union(new[]{ 
                    new XAttribute(strict?"begin":"b", Begin),
                    new XAttribute(strict?"end":"e", End),
                    new XAttribute(strict?"fon":"f", m_phonetics),
                    }),
                    m_text.Trim('\r', '\n')
            );

            return elm;
        }
        #endregion


        public TranscriptionPhrase(TranscriptionPhrase kopie)
        {
            this.m_begin = kopie.m_begin;
            this.m_end = kopie.m_end;
            this.m_text = kopie.m_text;
            this.m_phonetics = kopie.m_phonetics;
            this.height = kopie.height;
        }

        public TranscriptionPhrase()
            : base()
        {

        }

        public TranscriptionPhrase(TimeSpan begin, TimeSpan end, string aWords)
            : this()
        {
            this.Begin = begin;
            this.End = end;
            this.Text = aWords;
        }

        public TranscriptionPhrase(TimeSpan begin, TimeSpan end, string aWords, ElementType aElementType)
            : this(begin, end, aWords)
        {
            if (aElementType == ElementType.Phonetic)
            {
                Regex re = new Regex("{.*?}");
                MatchCollection mc = re.Matches(aWords);
                string s = aWords;
                bool pPouzeNavrh = false;
                if (mc != null && mc.Count > 0)
                {
                    s = mc[0].Value.Substring(1, mc[0].Value.Length - 2);
                    pPouzeNavrh = true;
                }
                string[] pSplitS = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (pSplitS != null && pSplitS.Length > 1)
                {
                    this.Text = pSplitS[1];
                    if (pPouzeNavrh)
                        this.Text = "{" + this.Text + "}";
                }
            }
        }

        public override int GetTotalChildrenCount()
        {
            return 0;
        }

        public override void ChildrenCountChanged(ChangedAction action)
        {

        }

        public override void ElementChanged(TranscriptionElement element)
        {

        }

        public override void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {

        }

        public override void ElementInserted(TranscriptionElement element, int absoluteindex)
        {
        }

        public override void ElementRemoved(TranscriptionElement element, int absoluteindex)
        {
        }

        public override int AbsoluteIndex
        {
            get 
            {
                return 0;
            }
        }

        public override void Add(TranscriptionElement data)
        {
            throw new NotSupportedException();
        }

        public override bool Remove(TranscriptionElement value)
        {
            throw new NotSupportedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public override bool Replace(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            throw new NotSupportedException();
        }

        public override void Insert(int index, TranscriptionElement data)
        {
            throw new NotSupportedException();
        }

        public override TranscriptionElement this[int Index]
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }

}
