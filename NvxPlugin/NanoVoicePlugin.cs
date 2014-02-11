using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using NanoTrans.Core;

namespace NvxPlugin
{
    public class NanoVoicePlugin
    {
        public static Transcription Import(Stream input)
        {
            XDocument doc = XDocument.Load(input);
            var root = doc.Element("NanovoidSegmentation");

            var mediums = root.Elements("Medium").ToArray();

            XElement medium = null;
            if (mediums.Length > 1)
            {
                SelectFile sf = new SelectFile(mediums.Select(m => m.Attribute("url") != null ? Path.GetFileName(m.Attribute("url").Value) : "name not specified").ToList());
                sf.Title = "Select Medium";
                if (sf.ShowDialog() == true)
                {
                    medium = mediums[sf.SelectedIndex];
                }
                else return null;
            }
            else
                medium = mediums.First();

            var segmentations = medium.Elements("Segmentation").ToArray();

            XElement segmentation = null;
            if (segmentations.Length > 1)
            {
                var lst = segmentations.Select(s => string.Join(" ", s.Attributes().Select(a => a.Name + ":" + a.Value))).ToList();
                SelectFile sf = new SelectFile(lst);
                sf.Title = "Select segmentation";
                if (sf.ShowDialog() == true)
                {
                    segmentation = segmentations[sf.SelectedIndex];
                }
                else return null;
            }
            else
                segmentation = segmentations.First();

            TimeSpan unitlen = new TimeSpan((long)(TimeSpan.FromSeconds(1).Ticks * XmlConvert.ToDouble(segmentation.Attribute("tres").Value)));
            segmentations.Elements("Segment");

            Transcription tr = new Transcription();
            TranscriptionChapter ch = new TranscriptionChapter();
            ch.Text = segmentation.Attribute("type").Value;

            TranscriptionSection sec = new TranscriptionSection();
            sec.Text = segmentation.Attribute("label").Value;

            tr.mediaURI = medium.Attribute("url")!=null?medium.Attribute("url").Value:"";

            var idss = segmentation.Element("Identities");
            if (idss != null)
            {
                var ids = idss.Elements("Id");
                if (ids != null && ids.Count() > 0)
                {
                    var speakers = ids.Select(i => new Speaker() { ID = XmlConvert.ToInt32(i.Attribute("id").Value), Surname = i.Attribute("label").Value });
                    tr.Speakers = new MySpeakers(speakers);
                }
            }

            var segments = segmentation.Element("Segments").Elements("Segment");

            var paragraphs = MakeParagraphs(segments, unitlen);

            foreach (var p in paragraphs)
                sec.Add(p);

            ch.Add(sec);
            tr.Add(ch);



            return tr;
        }


        private static IEnumerable<TranscriptionParagraph> MakeParagraphs(IEnumerable<XElement> elements, TimeSpan timeunit)
        {
            foreach (var e in elements)
            {
                var p = new TranscriptionParagraph()
                {
                    Begin = new TimeSpan( timeunit.Ticks * XmlConvert.ToInt64(e.Attribute("bt").Value)),
                    End = new TimeSpan( timeunit.Ticks * XmlConvert.ToInt64(e.Attribute("et").Value)),
                };

                var ph = new TranscriptionPhrase()
                {
                    Begin = p.Begin,
                    End = p.End,
                    Text = "",
                };


                var idel = e.Element("Id");
                if (idel != null)
                {
                    p.speakerID = XmlConvert.ToInt32(idel.Attribute("id").Value);
                    ph.Text = "score: "+idel.Attribute("score").Value; 
                }
                p.Phrases.Add(ph);
                yield return p;
            }
        }
    }
}
