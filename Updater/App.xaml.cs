using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static string ExeName = null;
        internal static string ExeDir = null;
        internal static string NTExe = null;
        internal static int version = 0;

        internal static CancellationTokenSource CancelWork = new CancellationTokenSource();
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                ExeName = Environment.GetCommandLineArgs()[0];
                ExeDir = Path.GetDirectoryName(ExeName);
                NTExe = ExeDir + "\\NanoTrans.exe";
                if (File.Exists(NTExe))
                {
                    FileVersionInfo fv = FileVersionInfo.GetVersionInfo(NTExe);
                    version = fv.FilePrivatePart;
                }
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            }
            catch
            {
                MessageBox.Show("Chyba při načítání definic updatu, ukončuji updater", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        bool terminating = false;
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            lock(this)
            {
                if(!terminating)
                {
                    terminating = true;
                    CancelWork.Cancel(true);
                    MessageBox.Show("Chyba při updatu \n Zkuste updatovat později nebo se obraťte na dodavatele softwaru", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
                    proc.Kill();
                    
                }
            }
        }

    }
}
