﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TranscriptionCore;

namespace TrsxV1Plugin
{
    public class ResContainer
    {

        public ResContainer(FileInfo input)
            : this(input.FullName)
        {
        }

        public ResContainer(string input)
        {
            string[] lines;
            using (var reader = new StreamReader(input))
            {
                string s = reader.ReadToEnd();
                lines = s.Split('\n').Select(l => l.Trim().Trim(new char[] { '\uFEFF' })).ToArray();
            }

            ParseLines(lines);
        }


        public ResContainer(Stream input)
        {
            string[] lines;
            using (var reader = new StreamReader(input))
            {
                string s = reader.ReadToEnd();
                lines = s.Split('\n').Select(l => l.Trim().Trim(new char[] { '\uFEFF' })).ToArray();
            }

            ParseLines(lines);

        }

        private void ParseLines(string[] lines)
        {
            var buckets = lines.SplitLines(l => l.StartsWith(@"FILE:")).ToArray();
            Files = buckets.Select(b => new ResFileSegment(b)).ToArray();
        }

        public ResFileSegment[] Files { get; set; }
    }

    public class ResFileSegment
    {
        public string FileName;
        public TimeSpan TRES;
        public string[] ORTOT;
        public TimeSpan[] START;
        public TimeSpan[] STOP;
        public string[] PRON;
        public string[] ORTOTP;
        public string ORTO;

        public ResFileSegment(string[] segment)
        {
            FileName = segment.First()[6..].Trim();
            TRES = TimeSpan.FromSeconds(double.Parse(segment.First(l => l.StartsWith("TRES:"))[6..].Trim(), CultureInfo.InvariantCulture));
            ORTOT = segment.First(l => l.StartsWith("ORTOT:"))[7..].Split('|').ToArray();

            START = segment.First(l => l.StartsWith("START:"))[7..].Split('|').Select(t => TimeSpan.FromSeconds(double.Parse(t, CultureInfo.InvariantCulture) * TRES.TotalSeconds)).ToArray();
            STOP = segment.First(l => l.StartsWith("STOP:"))[6..].Split('|').Select(t => TimeSpan.FromSeconds(double.Parse(t, CultureInfo.InvariantCulture) * TRES.TotalSeconds)).ToArray();
            PRON = segment.First(l => l.StartsWith("PRON:"))[6..].Split('|').ToArray();

            ORTO = segment.First(l => l.StartsWith("ORTO:"))[6..];

            var ortop = segment.FirstOrDefault(l => l.StartsWith("ORTOTP:"));
            if (ortop is { })
                ORTOTP = ortop[8..].Split('|').ToArray();
        }

        public Transcription GetTranscription(bool useOrtoTP, bool removeNonPhonemes)
        {
            Transcription tr = new Transcription();
            GetTranscription(useOrtoTP, removeNonPhonemes, tr);
            return tr;
        }

        public void GetTranscription(bool useOrtoTP, bool removeNonPhonemes, Transcription storage)
        {

            Transcription data = storage;
            List<TranscriptionPhrase> phrazes = new List<TranscriptionPhrase>();

            string[] orto = useOrtoTP ? ORTOTP : ORTOT;

            for (int i = 0; i < ORTOT.Length; i++)
            {
                TranscriptionPhrase ph = new TranscriptionPhrase();
                ph.Text = orto[i].Trim() + " ";
                if (orto[i].Length == 0) //empty words
                    ph.Text = "";
                ph.Phonetics = PRON[i];
                ph.Begin = START[i];
                ph.End = STOP[i];
                phrazes.Add(ph);
            }

            TranscriptionChapter c = new TranscriptionChapter();
            TranscriptionSection sec = new TranscriptionSection();

            TranscriptionParagraph pah = new TranscriptionParagraph();
            List<TranscriptionPhrase> silence = new List<TranscriptionPhrase>();
            TimeSpan sec20 = TimeSpan.FromSeconds(20);
            #region sileny splitovaci algoritmus
            while (phrazes.Count > 0)
            {
                string tt = phrazes[0].Text.Trim();
                if (string.IsNullOrWhiteSpace(tt) || (tt.First() == '[' && tt.Last() == ']'))//nerecova udalost
                {
                    silence.Add(phrazes[0]);
                    TimeSpan begin;
                    TimeSpan end;

                    if (pah.Phrases.Count > 0)//pokud uz jsou sestavene nejake fraze spocitam jestli presahnou spolu s tichy 20s
                    {
                        begin = pah.Phrases.First().Begin;
                        end = silence.Last().End;
                        if (end - begin >= sec20)//pokud ano reknu, ze pred tichy mel odstavec zkoncit
                        {
                            pah.Begin = pah.Phrases.First().Begin;
                            pah.End = pah.Phrases.Last().End;
                            sec.Paragraphs.Add(pah);
                            pah = new TranscriptionParagraph();
                        }
                    }


                }
                else if (silence.Count > 0)//mam nejaky nerecovy udalosti a prislo neco jinyho
                {
                    if (silence.Last().End - silence.First().Begin >= TimeSpan.FromSeconds(2)) //mam vic nereci nez 2 sekundy udelat z ni samostatnej segment
                    {
                        if (pah.Phrases.Count > 0)
                        {
                            pah.Begin = pah.Phrases.First().Begin;
                            pah.End = pah.Phrases.Last().End;
                            sec.Paragraphs.Add(pah);
                            pah = new TranscriptionParagraph();
                        }


                        foreach (var ss in silence)
                            pah.Phrases.Add(ss);

                        silence.Clear();

                        pah.Begin = pah.Phrases.First().Begin;
                        pah.End = pah.Phrases.Last().End;
                        sec.Paragraphs.Add(pah);
                        pah = new TranscriptionParagraph();


                        pah.Phrases.Add(phrazes[0]);
                    }
                    else//mam ji malo -  prilepit nerecovy udalosti k odstavci
                    {

                        foreach (var ss in silence)
                            pah.Phrases.Add(ss);

                        silence.Clear();
                        pah.Phrases.Add(phrazes[0]);
                    }
                }
                else//prisla recova fraze a nemam nic v tichu pridat do paragrafu
                {
                    pah.Phrases.Add(phrazes[0]);
                }

                phrazes.RemoveAt(0);
            }

            //dosyp zbytek do prepisu

            while (silence.Count > 0)
            {
                pah.Phrases.Add(silence[0]);
                silence.RemoveAt(0);
            }
            #endregion

            pah.Begin = pah.Phrases.First().Begin;
            pah.End = pah.Phrases.Last().End;
            sec.Paragraphs.Add(pah);
            c.Sections.Add(sec);
            data.Chapters.Add(c);

            if (removeNonPhonemes)
            {
                {
                    TranscriptionPhrase ph = (TranscriptionPhrase)sec[0][0];
                    while (ph is { })
                    {
                        var ph2 = (TranscriptionPhrase)ph.NextSibling();

                        string t = ph.Text.Trim();
                        if (string.IsNullOrWhiteSpace(t) || (t.StartsWith("[") && t.EndsWith("]")))
                            ph.Parent.Remove(ph);

                        ph = ph2;
                    }
                }

            }

            data.MediaURI = FileName;
        }


    }

    public static class LoadRes
    {

        public static bool Import(Stream input, Transcription storage)
        {
            ResContainer rc = new ResContainer(input);
            List<string> files = rc.Files.Select(f => f.FileName).ToList();
            for (int i = 0; i < files.Count; i++)
                files[i] = "" + i + " - " + files[i];

            SelectFile sf = new SelectFile(files);
            sf.line = 0;

            if (files.Count == 1 || sf.ShowDialog() == true)
            {
                var ResFile = rc.Files[sf.line];
                ResFile.GetTranscription(sf.RemoveNonPhonemes, sf.RemoveNonPhonemes, storage);
                return true;
            }
            return false;
        }

        public static IEnumerable<T[]> SplitLines<T>(this IEnumerable<T> source, Func<T, bool> IsFirst)
        {
            Queue<T> list = new Queue<T>();
            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (IsFirst(enumerator.Current) && list.Count > 0)
                {
                    yield return list.ToArray();
                    list.Clear();
                }
                list.Enqueue(enumerator.Current);
            }
            yield return list.ToArray();
            list.Clear();
        }
    }
}
