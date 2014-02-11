using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reflection;
using System.Globalization;
using System.Resources;
using System.Collections;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinOProgramu.xaml
    /// </summary>
    public partial class WinOProgramu : Window
    {
        public WinOProgramu(string aNazevProgramu)
        {
            InitializeComponent();
            this.label1.Content = aNazevProgramu;
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            string About = string.Format(CultureInfo.InvariantCulture, @"Nanotrans Version {0}.{1}.{2} (r{3})", v.Major, v.Minor, v.Build, v.Revision);
            versiontext.Text = About;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
