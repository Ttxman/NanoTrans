using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Security.Cryptography;
using Ionic.Zip;

namespace UpdateBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            args = new string[]{@"C:\Users\Ttxman\Documents\NanoTrans\Work\WpfApplication2\bin\x86\Debug","Filelist.lst","300",@"http://admin.notebook.ttxman.operaunite.com/file_sharing_1/content/"};
            Console.WriteLine("Creating definitions");
            string basedir = args[0];
            string list = args[1];
            string build = args[2];
            string datastore = args[3];
            string packageDir = Path.Combine("packages",build);
            if (Directory.Exists(packageDir))
            {
                Directory.GetFiles(packageDir, "*.*", SearchOption.AllDirectories).ToList().ForEach(f => File.Delete(f));
                Directory.Delete(packageDir);
            }

            Directory.CreateDirectory(packageDir);

            string[] files = File.ReadAllLines(list).Select(l => Path.Combine(basedir, l)).ToArray();
            SHA1Cng sha = new SHA1Cng();

            var document =  new XDocument(
                                new XDeclaration("1.0","utf-8","true"),
                                new XElement("UpdateDefinitions",
                                    new XElement("Definition", new XAttribute("Application","NanoTrans"),new XAttribute("Build",build),new XAttribute("DataStoreURL",datastore),
                                        files.Select(f=>new XElement("File",new XAttribute("FileName",f.Substring(basedir.Length+1)),new XAttribute("SHA1",Convert.ToBase64String(sha.ComputeHash(File.OpenRead(f))))))
                                    )
                                )
                            );

            Console.WriteLine("saving definitions");
            document.Save(Path.Combine(packageDir,"Definitions.xml"));

            Console.WriteLine("Zipping files");
            foreach(string f in files)
            {
                Console.WriteLine("Zipping "+f);
                ZipFile zf = new ZipFile();
                zf.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zf.AddFile(f);
                string fullpath = Path.Combine(packageDir, f.Substring(basedir.Length + 1)) + ".zip";
                Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
                zf.Save(Path.Combine(packageDir, f.Substring(basedir.Length + 1)) + ".zip");
                zf.Dispose();
            }
            Console.WriteLine("all done");
        }
    }
}
