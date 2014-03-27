using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SRTPlugin
{
    public class SRTPlugin
    {
        //according to wikipedia srt is french format
        static CultureInfo FRculture = CultureInfo.CreateSpecificCulture("fr-FR");
        static string SRTdateformat = @"hh\:mm\:ss\,fff";

        public static bool Import(Stream input, Transcription storage)
        {
            var groups = ReadLines(input).SplitLines(x => x == "");

            //group[0] .. line index
            //group[1] .. time --> time SomeCustomStuff...
            //group[2...] .. string 

            //group[lenght-1] .. empty line ignored


            var paragraphs = groups.Where(g=>g.Length > 0).Select(g =>
                {
                    TranscriptionPhrase p = new TranscriptionPhrase();
                    var time= g[1].Split(' ');
                    p.Begin = TimeSpan.Parse(time[0],FRculture);
                    // -->
                    p.End = TimeSpan.Parse(time[2], FRculture);

                    if (time.Length > 3) //some position data
                        p.Phonetics = string.Join(" ", time.Skip(3));

                    p.Text = string.Join("\r\n",g.Skip(2));

                    return new TranscriptionParagraph(p);
                });

            foreach (var p in paragraphs)
                storage.Add(p);

            return true;
        }

        public static bool Export(Transcription transcription, Stream output)
        {
            using (StreamWriter sw = new StreamWriter(output))
            {
                int cntr = 1;
                foreach (var p in transcription.EnumerateParagraphs())
                {
                    sw.WriteLine(cntr.ToString());
                    string b = p.Begin.ToString(SRTdateformat);
                    string e = p.End.ToString(SRTdateformat);
                    if(string.IsNullOrWhiteSpace(p.Phonetics))
                        sw.WriteLine(string.Format("{0} --> {1}",b,e));
                    else
                        sw.WriteLine(string.Format("{0} --> {1} {2}", b, e, string.Join(" ",p.Phonetics.Split('\n').Select(ph=>ph.Trim())))); //position data.. remove new lines

                    foreach (var l in p.Text.Split('\n').Select(l=>l.Trim()))
                    {
                        if (!string.IsNullOrEmpty(l))
                            sw.WriteLine(l);
                    }

                    sw.WriteLine();
                    cntr++;
                }
            }

            return true;
        }

        static IEnumerable<string> ReadLines(Stream input)
        {
            using (StreamReader sr = new StreamReader(input))
            {
                while (!sr.EndOfStream)
                    yield return sr.ReadLine();
            }
        }





    }
    static class xtensions
    {

        public static IEnumerable<T[]> SplitLines<T>(this IEnumerable<T> source, Func<T, bool> IsEnd)
        {
            Queue<T> list = new Queue<T>();
            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (IsEnd(enumerator.Current) && list.Count > 0)
                {
                    yield return list.ToArray();
                    list.Clear();
                    continue;
                }
                T cur = enumerator.Current;
                list.Enqueue(cur);
            }
            yield return list.ToArray();
            list.Clear();
        }
    }
}
