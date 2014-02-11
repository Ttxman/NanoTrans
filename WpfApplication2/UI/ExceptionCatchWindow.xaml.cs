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
        private Window1 _parent;
        private Exception _e;
        public ExceptionCatchWindow(Window1 parent, Exception e)
        {
            InitializeComponent();
            _e = e;
            _parent = parent;
            textBox1.Text = e.ToString()+"\n\n"+ e.Message;
        }

        private void buttonSaveAndRestart_Click(object sender, RoutedEventArgs e)
        {
            _parent.SaveTranscription(true, _parent.Transcription.FileName);
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void buttonIgnore_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
