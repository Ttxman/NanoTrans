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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for SelectFile.xaml
    /// </summary>
    public partial class PickOneDialog : Window
    {
        public PickOneDialog(List<string> data, string title)
        {
            InitializeComponent();
            box.ItemsSource = data;
            this.Title = title;
        }

        private int m_line = 0;

        public int SelectedIndex
        {
            get { return m_line; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            m_line = box.SelectedIndex;
            
            this.DialogResult = true;
            Close();
        }

        private void box_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Button_Click(null, null);
        }

        private void box_KeyDown(object sender, KeyEventArgs e)
        {
           if( e.Key == Key.Return)
               Button_Click(null, null);
        }
    }
}
