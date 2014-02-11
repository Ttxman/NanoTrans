using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.IO;
using Microsoft.Win32;

namespace NanoTrans
{
    internal class Plugin
    {

        private string m_fileName;

        public string FileName
        {
            get { return m_fileName; }
            set { m_fileName = value; }
        }
        bool m_input;

        public bool Input
        {
            get { return m_input; }
            set { m_input = value; }
        }
        bool m_isassembly;

        public bool Isassembly
        {
            get { return m_isassembly; }
            set { m_isassembly = value; }
        }
        string m_mask;

        public string Mask
        {
            get { return m_mask; }
            set { m_mask = value; }
        }
        string m_parameters;

        public string Parameters
        {
            get { return m_parameters; }
            set { m_parameters = value; }
        }

        Func<Stream, MySubtitlesData> m_importDelegate;

        public Func<Stream, MySubtitlesData> ImportDelegate
        {
            get { return m_importDelegate; }
            set { m_importDelegate = value; }
        }
        Func<MySubtitlesData, Stream, bool> m_exportDelegate;

        public Func<MySubtitlesData, Stream, bool> ExportDelegate
        {
            get { return m_exportDelegate; }
            set { m_exportDelegate = value; }
        }


        string m_name;

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public Plugin(bool input, bool isassembly, string mask, string parameters, string name, Func<Stream, MySubtitlesData> importDelegate, Func<MySubtitlesData, Stream, bool> exportDelegate, string filename)
        {
            m_input = input;
            m_isassembly = isassembly;
            m_mask = mask;
            m_parameters = parameters;
            m_importDelegate = importDelegate;
            m_exportDelegate = exportDelegate;
            m_fileName = filename;
            m_name = name;
        }


        public MySubtitlesData ExecuteImport(string sourcefile = null)
        {
            if (sourcefile == null)
            {
                OpenFileDialog opf = new OpenFileDialog();
                opf.CheckFileExists = true;
                opf.CheckPathExists = true;
                opf.Filter = m_mask;

                if (opf.ShowDialog() == true)
                    sourcefile = opf.FileName;
            }

            if (File.Exists(sourcefile))
            {
                try
                {
                    if (Isassembly)
                    {
                        MySubtitlesData imp = m_importDelegate.Invoke(File.OpenRead(sourcefile));
                        imp.JmenoSouboru = sourcefile;
                        return imp;
                    }
                    else
                    {
                        string inputfile = sourcefile;
                        string tempFolder = FilePaths.TempDirectory;
                        string tempFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName()) + ".trsx";


                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = Path.Combine(FilePaths.GetReadPath(FilePaths.PluginsPath), m_fileName);
                        psi.Arguments = string.Format(m_parameters, "\"" + inputfile + "\"", "\"" + tempFile + "\"", "\"" + tempFolder + "\"");

                        Process p = new Process();
                        p.StartInfo = psi;

                        p.Start();
                        p.WaitForExit();

                        var data =  MySubtitlesData.Deserialize(tempFile);

                        return data;

                    }
                }
                catch
                {
                    MessageBox.Show("Import souboru skončil chybou", "chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return null;
        }
        public override string ToString()
        {
            return m_name;
        }

        public void ExecuteExport(MySubtitlesData data, string destfile = null)
        {
            if (destfile == null)
            {
                SaveFileDialog sf = new SaveFileDialog();

                sf.CheckPathExists = true;
                sf.Filter = m_mask;

                if (sf.ShowDialog() == true)
                {
                    destfile = sf.FileName;
                }

            }

            try
            {
                if (Isassembly)
                {
                    m_exportDelegate.Invoke(data, File.Create(destfile));
                }
                else
                {
                    string tempFolder = FilePaths.TempDirectory;
                    string inputfile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName()) + ".trsx";
                    string tempFile = destfile;

                    data.Serialize(inputfile, true);

                    ProcessStartInfo psi = new ProcessStartInfo();

                    psi.FileName = Path.Combine(FilePaths.GetReadPath(FilePaths.PluginsPath), m_fileName);
                    psi.Arguments = string.Format(m_parameters, "\"" + inputfile+"\"", "\"" + tempFile+"\"", "\"" + tempFolder+"\"");

                    Process p = new Process();
                    p.StartInfo = psi;

                    p.Start();
                    p.WaitForExit();
                    File.Delete(tempFile);
                }
            }
            catch
            {
                MessageBox.Show("Export souboru skončil chybou", "chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

    }
}
