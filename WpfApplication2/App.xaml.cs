using System;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
//using System.Linq;
using System.Windows;
using WPFLocalizeExtension.Engine;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static string[] Startup_ARGS;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
           Startup_ARGS = e.Args;
           AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {

        }

    }
}
