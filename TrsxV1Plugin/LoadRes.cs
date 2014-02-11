using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanoTrans;
using System.IO;
using System.Reflection;
using System.Globalization;


namespace TrsxV1Plugin
{
    public static class LoadRes
    {

        public static MySubtitlesData Import(Stream input)
        {
            
            MySubtitlesData data = new MySubtitlesData();
            StreamReader reader = new StreamReader(input);
            string s = reader.ReadToEnd();
            string[] lines = s.Split('\n').Select(l => l.Trim().Trim(new char[]{'\uFEFF'})).ToArray();
            var buckets = lines.SplitLines(l => l.StartsWith(@"FILE:")).ToArray();// || l.StartsWith("﻿FILE:")).ToArray();

            List<string> files = buckets.Select(a => a[0]).ToList();
            for (int i = 0; i < files.Count; i++)
                files[i] = "" + i + " - " + files[i];

            SelectFile sf = new SelectFile(files);
            if (sf.ShowDialog()==true)
            {
                var bucket = buckets[sf.line];

                var filename = bucket.First().Substring(6).Trim();
                var time = double.Parse(bucket.First(l => l.StartsWith("TRES:")).Substring(6).Trim(), CultureInfo.InvariantCulture);
                var orto = bucket.First(l => l.StartsWith("ORTOT:")).Substring(7).Split('|').ToArray();
                var starts = bucket.First(l => l.StartsWith("START:")).Substring(7).Split('|').Select(t => TimeSpan.FromSeconds(double.Parse(t, CultureInfo.InvariantCulture) * time)).ToArray();
                var ends = bucket.First(l => l.StartsWith("STOP:")).Substring(6).Split('|').Select(t => TimeSpan.FromSeconds(double.Parse(t, CultureInfo.InvariantCulture) * time)).ToArray();
                var pron = bucket.First(l => l.StartsWith("PRON:")).Substring(6).Split('|').ToArray();
                
                List<MyPhrase> phrazes = new List<MyPhrase>();

                for (int i = 0; i < orto.Length; i++)
                {
                    MyPhrase ph = new MyPhrase();
                    ph.Text = orto[i]+" ";
                    ph.Phonetics = pron[i];
                    ph.Begin = starts[i];
                    ph.End = ends[i];
                    phrazes.Add(ph);
                }

                MyChapter c = new MyChapter();
                MySection sec = new MySection();

                var second  = TimeSpan.FromSeconds(0.5);
                MyParagraph pah = new MyParagraph();
                List<MyPhrase> silence = new List<MyPhrase>();
                TimeSpan sec20 = TimeSpan.FromSeconds(20);
                while (phrazes.Count > 0)
                {
                    string tt = phrazes[0].Text.Trim();
                    if(tt.First() == '[' && tt.Last() == ']')//nerecova udalost
                    {
                        silence.Add(phrazes[0]);
                        TimeSpan begin;
                        TimeSpan end;

                        if(pah.Phrases.Count > 0)//pokud uz jsou sestavene enkae fraze spocitam jestli presahnou spolu s tichy 20s
                        {
                            begin = pah.Phrases.First().Begin;
                            end = silence.Last().End;
                            if(end-begin >= sec20)//pokud ano reknu, ze pred tichy mel odstavec zkoncit
                            {
                                 pah.Begin = pah.Phrases.First().Begin;
                                 pah.End = pah.Phrases.Last().End;
                                 sec.Paragraphs.Add(pah);
                                 pah  = new MyParagraph();
                            }
                        }

                        
                    }else if(silence.Count > 0)//mam nejaky nerecovy udalosti a prislo neco jinyho
                    {
                        if(silence.Last().End-silence.First().Begin >= TimeSpan.FromSeconds(2)) //mam vic nereci nez 2 sekundy udelat z ni samostatnej segment
                        {
                            if(pah.Phrases.Count > 0)
                            {
                                pah.Begin = pah.Phrases.First().Begin;
                                pah.End = pah.Phrases.Last().End;
                                sec.Paragraphs.Add(pah);
                                pah  = new MyParagraph();
                            }

                            foreach (var ss in silence)
                                pah.Phrases.Add(ss);
                            silence.Clear();

                            pah.Begin = pah.Phrases.First().Begin;
                            pah.End = pah.Phrases.Last().End;
                            sec.Paragraphs.Add(pah);
                            pah = new MyParagraph();


                            pah.Phrases.Add(phrazes[0]);
                        }else//mam ji malo -  prilepit nerecovy udalosti k odstavci
                        {
                            foreach(var ss in silence)
                                pah.Phrases.Add(ss);
                            silence.Clear();
                            pah.Phrases.Add(phrazes[0]);
                        }
                    }else//prisla recova fraze a nemam nic v tichu pridat do paragrafu
                    {
                        pah.Phrases.Add(phrazes[0]);
                    }

                    phrazes.RemoveAt(0);
                }

                //dosyp zbytek do prepisu
                pah.Begin = pah.Phrases.First().Begin;
                pah.End = pah.Phrases.Last().End;
                sec.Paragraphs.Add(pah);
                c.Sections.Add(sec);
                data.Chapters.Add(c);

                data.mediaURI = filename;
                return data;
            }
            return null;
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
