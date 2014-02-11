using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Net;
using System.Security.Cryptography;
using Ionic.Zip;
namespace Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string ExeName = null;
        static string ExeDir = null;
        static string NTExe = null;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                ExeName = Environment.GetCommandLineArgs()[0];
                ExeDir = Path.GetDirectoryName(ExeName);
                NTExe = ExeDir + "\\NanoTrans.exe";

                int version = 0;
                if (File.Exists(NTExe))
                {
                    FileVersionInfo fv = FileVersionInfo.GetVersionInfo(NTExe);
                    version = fv.FilePrivatePart;
                }
                string URL = ConfigurationManager.AppSettings["UpdateDefinitions"];

                WebClient client = new WebClient();

                var document = XDocument.Load(new MemoryStream(client.DownloadData(URL)));
                var definition = document.Descendants("Definition").OrderByDescending(d => int.Parse(d.Attribute("Build").Value)).First();

                if (int.Parse(definition.Attribute("Build").Value) > version)
                {
                    var filelist = definition.Descendants("File").ToList();

                    List<XElement> flist = new List<XElement>();
                    SHA1Cng sha = new SHA1Cng();
                    foreach (var f in filelist)
                    {
                        string file = ExeDir + "\\" + f.Attribute("FileName").Value;
                        if (File.Exists(file))
                        {
                            string h = Convert.ToBase64String(sha.ComputeHash(File.OpenRead(file)));
                            string h2 = f.Attribute("SHA1").Value;
                            if (h != h2)
                            {
                                flist.Add(f);
                            }
                        }
                        else
                        {
                            flist.Add(f);
                        }
                    }


                    foreach (var f in flist)
                    {
                        
                        string path = f.Attribute("FileName").Value;
                        string targetf = Path.Combine(ExeDir,path);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetf));
                        string sourcef = definition.Attribute("DataStoreURL").Value +"/"+ path.Replace('\\','/')+".zip";
                        var zf = ZipFile.Read(new MemoryStream(client.DownloadData(sourcef)));
                        zf.Entries.First().Extract(File.OpenWrite(targetf));
                        
                    }
                }

            }
            catch
            {
                MessageBox.Show("Chyba při načítání definic updatu, ukončuji updater", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
    }
}
