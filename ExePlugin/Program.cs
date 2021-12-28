using TranscriptionCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ExePlugin
{
    class Program
    {
        /// <summary>
        /// resolve dependency on nanotrans core by searching parrent directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("NanoTransCore"))
            {
                string path = Assembly.GetExecutingAssembly().Location;
                string assembliesDir = Directory.GetParent(Path.GetDirectoryName(path)).FullName;
                Assembly asm = Assembly.LoadFrom(Path.Combine(assembliesDir, "NanoTransCore.dll"));
                return asm;
            }
            return null;
        }

        static void Main(string[] args)
        {
            //abych nemusel mit dllku od nanotransu ve stejny slozce musim rucne rict kde je
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            List<string> arg = args.ToList();
            bool times = false;
            if (arg.Contains("-times"))
                times = true;

            bool nonoises = false;
            if (arg.Contains("-nonoises"))
                nonoises = true;

            int i = arg.IndexOf("-i") + 1;
            int o = arg.IndexOf("-o") + 1;


            //funtion have to be in 
            var e = new Exporter();
            e.ExportovatDokument(arg[i].Trim('"'), arg[o].Trim('"'), times, nonoises);

        }
    }

    class Exporter
    {
        public static readonly Regex ignoredGroup = new Regex(@"\[.*?\]", RegexOptions.Singleline | RegexOptions.Compiled);
        public static readonly Regex whitespaceGroup = new Regex(@"\s\s+", RegexOptions.Singleline | RegexOptions.Compiled);
        /// <summary>
        /// exportuje dokument do vybraneho formatu - pokud je cesta null, zavola savedialog
        /// </summary>
        /// <param name="aDokument"></param>
        /// <param name="vystup"></param>
        /// <param name="aFormat"></param>
        /// <returns></returns>
        public void ExportovatDokument(string vstup, string vystup, bool times, bool nonoises)
        {

            Transcription data = null;
            using (var file = File.OpenRead(vstup))
                data = Transcription.Deserialize(file);

            if (times)
            {
                FileStream fs = new FileStream(vystup, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("windows-1250"));
                FileInfo fi = new FileInfo(vystup);
                string pNazev = fi.Name.ToUpper().Remove(fi.Name.Length - fi.Extension.Length);
                string pHlavicka = "<" + pNazev + ";";
                for (int i = 0; i < data.Speakers.Count; i++)
                {
                    if (i > 0) pHlavicka += ",";
                    pHlavicka += " " + data.Speakers[i].FirstName;
                }
                pHlavicka += ">";
                sw.WriteLine(pHlavicka);
                sw.WriteLine();
                for (int i = 0; i < data.Chapters.Count; i++)
                {
                    for (int j = 0; j < data.Chapters[i].Sections.Count; j++)
                    {
                        for (int k = 0; k < data.Chapters[i].Sections[j].Paragraphs.Count; k++)
                        {
                            TranscriptionParagraph pP = data.Chapters[i].Sections[j].Paragraphs[k];
                            //zapsani jednotlivych odstavcu

                            string pRadek = "<" + (data.Speakers.IndexOf(pP.Speaker) - 1).ToString() + "> ";
                            for (int l = 0; l < pP.Phrases.Count; l++)
                            {
                                TranscriptionPhrase pFraze = pP.Phrases[l];
                                pRadek += "[" + (pFraze.Begin).ToString() + "]" + pFraze.Text;
                            }
                            sw.WriteLine(pRadek);
                        }
                    }
                }

                sw.Close();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in data.EnumerateParagraphs())
                    if (nonoises)
                    {
                        var alltext = ignoredGroup.Replace(item.Text, "");
                        alltext = whitespaceGroup.Replace(alltext, " ");
                        if (!string.IsNullOrWhiteSpace(alltext))
                            sb.AppendLine(alltext);
                    }
                    else
                    {
                        sb.AppendLine(item.Text);
                    }

                File.WriteAllText(vystup, sb.ToString());
            }
        }
    }
}
