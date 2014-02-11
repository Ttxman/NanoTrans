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

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for ExceptionCatchWindow.xaml
    /// </summary>
    public partial class ExceptionCatchWindow : Window
    {
        private Window1 m_parent;
        private Exception m_e;
        public ExceptionCatchWindow(Window1 parent, Exception e)
        {
            InitializeComponent();
            m_e = e;
            m_parent = parent;
            textBox1.Text = e.Message;
        }

        private void buttonSaveAndRestart_Click(object sender, RoutedEventArgs e)
        {
            m_parent.UlozitTitulky(true, m_parent.myDataSource.JmenoSouboru);
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void buttonIgnore_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
