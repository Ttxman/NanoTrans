using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using NanoTrans.Core;

namespace NanoTrans
{
    internal class Plugin
    {

        private string _fileName;

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }
        bool _input;

        public bool Input
        {
            get { return _input; }
            set { _input = value; }
        }
        bool _isassembly;

        public bool Isassembly
        {
            get { return _isassembly; }
            set { _isassembly = value; }
        }
        string _mask;

        public string Mask
        {
            get { return _mask; }
            set { _mask = value; }
        }
        string _parameters;

        public string Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        Func<Stream, Transcription, bool> _importDelegate;

        public Func<Stream, Transcription, bool> ImportDelegate
        {
            get { return _importDelegate; }
            set { _importDelegate = value; }
        }
        Func<Transcription, Stream, bool> _exportDelegate;

        public Func<Transcription, Stream, bool> ExportDelegate
        {
            get { return _exportDelegate; }
            set { _exportDelegate = value; }
        }


        string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Plugin(bool input, bool isassembly, string mask, string parameters, string name, Func<Stream, Transcription, bool> importDelegate, Func<Transcription, Stream, bool> exportDelegate, string filename)
        {
            _input = input;
            _isassembly = isassembly;
            _mask = mask;
            _parameters = parameters;
            _importDelegate = importDelegate;
            _exportDelegate = exportDelegate;
            _fileName = filename;
            _name = name;
        }


        public WPFTranscription ExecuteImport(string sourcefile = null)
        {
            if (sourcefile == null)
            {
                OpenFileDialog opf = new OpenFileDialog();
                opf.CheckFileExists = true;
                opf.CheckPathExists = true;
                opf.Filter = _mask;

                if (opf.ShowDialog() == true)
                    sourcefile = opf.FileName;
            }

            if (File.Exists(sourcefile))
            {
                try
                {
                    if (Isassembly)
                    {
                        using (var f = File.OpenRead(sourcefile))
                        {
                            var imp = new WPFTranscription();
                            if (!_importDelegate.Invoke(f, imp))
                                throw new Exception();
                            
                            imp.FileName = sourcefile;
                            
                            return imp;
                        }
                    }
                    else
                    {
                        string inputfile = sourcefile;
                        string tempFolder = FilePaths.TempDirectory;
                        string tempFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName()) + ".trsx";


                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = Path.Combine(FilePaths.GetReadPath(FilePaths.PluginsPath), _fileName);
                        psi.Arguments = string.Format(_parameters, "\"" + inputfile + "\"", "\"" + tempFile + "\"", "\"" + tempFolder + "\"");

                        Process p = new Process();
                        p.StartInfo = psi;

                        p.Start();
                        p.WaitForExit();

                        var data = WPFTranscription.Deserialize(tempFile);
                        return data;

                    }
                }
                catch
                {
                    MessageBox.Show(Properties.Strings.MessageBoxImportError, Properties.Strings.MessageBoxErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return null;
        }
        public override string ToString()
        {
            return _name;
        }

        public void ExecuteExport(Transcription data, string destfile = null)
        {
            if (destfile == null)
            {
                SaveFileDialog sf = new SaveFileDialog();

                sf.CheckPathExists = true;
                sf.Filter = _mask;

                if (sf.ShowDialog() == true)
                {
                    destfile = sf.FileName;
                }

            }

            try
            {
                if (Isassembly)
                {
                    _exportDelegate.Invoke(data, File.Create(destfile));
                }
                else
                {
                    string tempFolder = FilePaths.TempDirectory;
                    string inputfile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName()) + ".trsx";
                    string tempFile = destfile;

                    data.Serialize(inputfile, true, !GlobalSetup.Setup.SaveInShortFormat);

                    ProcessStartInfo psi = new ProcessStartInfo();

                    psi.FileName = Path.Combine(FilePaths.GetReadPath(FilePaths.PluginsPath), _fileName);
                    psi.Arguments = string.Format(_parameters, "\"" + inputfile + "\"", "\"" + tempFile + "\"", "\"" + tempFolder + "\"");

                    Process p = new Process();
                    p.StartInfo = psi;

                    p.Start();
                    p.WaitForExit();
                    File.Delete(inputfile);
                }
            }
            catch
            {
                MessageBox.Show(Properties.Strings.MessageBoxExportError, Properties.Strings.MessageBoxErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

    }
}
