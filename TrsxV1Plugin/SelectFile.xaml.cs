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

namespace TrsxV1Plugin
{
    /// <summary>
    /// Interaction logic for SelectFile.xaml
    /// </summary>
    public partial class SelectFile : Window
    {
        public SelectFile(List<string> data)
        {
            InitializeComponent();
            box.ItemsSource = data;

            
        }

        public int line = 0;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            line = box.SelectedIndex;
            
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
