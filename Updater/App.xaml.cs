using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string ExeName= null;
        static string ExeDir = null;
        static string NTExe = null;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ExeName = Environment.GetCommandLineArgs()[0];
            ExeDir = Path.GetDirectoryName(ExeName);
            NTExe = ExeDir + "\\NanoTrans.exe";

            if (File.Exists(NTExe))
            {
                FileVersionInfo fv = FileVersionInfo.GetVersionInfo(NTExe);
            }
            else
            {
                MessageBox.Show("Nanotrans nenalezen ukončuji updater", "Nanotrans nenalezen", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

        }
    }
}
