using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
//using System.Linq;
using System.Windows;

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
        }
        
    }
}
