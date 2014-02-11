using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ExePlugin
{
    class Program
    {
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

            int i = arg.IndexOf("-i")+1;
            int o = arg.IndexOf("-o")+1;


            //musi to byt ve vlastni classe kvuli resolveru, jinak to padne pred resolve, ve chvili kdy se zacne vytvaret typ z nenacteny ddlky
            var e = new Exporter();
            e.ExportovatDokument(arg[i].Trim('"'), arg[o].Trim('"'), times);
            
        }
    }

    class Exporter
    {

        /// <summary>
        /// exportuje dokument do vybraneho formatu - pokud je cesta null, zavola savedialog
        /// </summary>
        /// <param name="aDokument"></param>
        /// <param name="vystup"></param>
        /// <param name="aFormat"></param>
        /// <returns></returns>
        public void ExportovatDokument(string vstup, string vystup, bool times)
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
                            string pRadek = "<" + (pP.SpeakerID - 1).ToString() + "> ";
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
                if (data.Count > 0)
                {
                    TranscriptionElement e = data[0];
                    while (e != null)
                    {
                        if (e.IsParagraph)
                        {
                            sb.AppendLine(e.Text);
                        }
                        e = e.Next();
                    }
                }

                File.WriteAllText(vystup, sb.ToString());
            }
        }
    }
}
