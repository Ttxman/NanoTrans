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

           LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
           LocalizeDictionary.Instance.Culture = new CultureInfo("en");

        }

        void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
#if DEBUG
            if(!File.Exists("nanotrans.log"))
                File.Create("nanotrans.log").Dispose();
            File.AppendAllText("nanotrans.log", string.Format("{6}: FirstChanceException event raised in {0}: {1}\r\nsource: {2}\r\n trace = {3}\r\nex:{4}\r\ninner:{5}\r\r\n\r\r\n\r\n\r\n", AppDomain.CurrentDomain.FriendlyName, e.Exception.Message, e.Exception.Source, e.Exception.StackTrace,e.Exception,e.Exception.InnerException, DateTime.Now));
#endif
        }

    }
}
