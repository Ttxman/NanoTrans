using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace SVNVersioning
{
    class Program
    {
        static int Main(string[] args)
        {
            
            //args = new string[]
            //{
            //@"C:\Users\Ttxman\Documents\NanoTrans\Work\",
            //@"C:\Users\Ttxman\Documents\NanoTrans\Work\SVNBinaries\SVNVersion.exe",
            //@"C:\Users\Ttxman\Documents\NanoTrans\Work\WpfApplication2\Properties\AssemblyInfo.cs",
            //@"C:\Users\Ttxman\Documents\NanoTrans\Work\Setup\ReleaseSetupScript.iss"
            //};
            
            int retval = 0;
            try
            {
                string dir = args[0];
                string versioner = "\"" + args[1] + "\"";
                Console.WriteLine("\nsvn versioning");
                Console.WriteLine(args[0]);
                Console.WriteLine(args[1]);
                Console.WriteLine(args[2]);
                Console.WriteLine(args[3]);
                Process p;
                ProcessStartInfo psi = new ProcessStartInfo(versioner, "\"" + dir + "\\\"");
                psi.RedirectStandardOutput = true;
                p = new Process();
                p.StartInfo = psi;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                string SVNversion = p.StandardOutput.ReadToEnd().Trim();
                
                string file = args[2];
                string sfile = args[3];

                string versionfile = File.ReadAllText(file);
                string scriptfile = File.ReadAllText(sfile,Encoding.GetEncoding(1250));
                if (SVNversion == "exported")
                    retval = 1;
                else
                {
                    if (SVNversion.Contains(':'))
                        SVNversion = SVNversion.Substring(SVNversion.IndexOf(':')+1);

                    if(SVNversion.Contains('M'))
                         SVNversion = (int.Parse(SVNversion.Substring(0,SVNversion.IndexOf('M')))+1).ToString();
                    else if(SVNversion.Contains('S'))
                        SVNversion = SVNversion.Substring(0, SVNversion.IndexOf('M'));

                    Regex x = new Regex("(.*\\[assembly: AssemblyVersion\\(\"\\w*?\\.\\w*?\\.\\w*?\\.)(\\w*?)(\"\\)\\].*)", RegexOptions.Singleline);

                    Regex x2 = new Regex("(.*#define MyAppVersion \")(.*?)(\".*)", RegexOptions.Singleline);
                    
                    Match m = x.Match(versionfile);
                    Match m2 = x2.Match(scriptfile);
                    
                    
                    versionfile = m.Groups[1]+SVNversion+m.Groups[3];
                    x = new Regex("(.*\\[assembly: AssemblyFileVersion\\(\"\\w*?\\.\\w*?\\.\\w*?\\.)(\\w*?)(\"\\)\\].*)", RegexOptions.Singleline);
                    m = x.Match(versionfile);
                    versionfile = m.Groups[1] + SVNversion + m.Groups[3];


                    scriptfile = m2.Groups[1] + "0."+SVNversion+"b" + m2.Groups[3];

                    File.WriteAllText(file,versionfile);
                    File.WriteAllText(sfile, scriptfile, Encoding.GetEncoding(1250));
                    

                    File.WriteAllText(Path.Combine(dir, "CurrentSVNVersion"),SVNversion);
                }
            }
            catch { }
            Console.WriteLine("svn versioning end with code {0} \n",retval);
            return retval;
        }
    }
}
