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
    /// Interaction logic for FindDialog.xaml
    /// </summary>
    public partial class FindDialog : Window
    {

        public string TextToFind
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        
        }

        private Window1 m_parent;
        public FindDialog(Window1 parent)
        {
            m_parent = parent;
            InitializeComponent();
        }

        bool searching = false;
        public void SearchNext()
        { 
            m_parent.FindNext(textBox1.Text,checkBox2.IsChecked == true,checkBox1.IsChecked == true, checkBox3.IsChecked == true);
            searching = true;
            m_parent.VirtualizingListBox.UpdateLayout();
            textBox1.Focus();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            SearchNext();
        }

        private void textBox1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SearchNext();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox1.Focus();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            if (searching)
            {
                Dispatcher.BeginInvoke((Action)(() => { textBox1.Focus(); searching = false; }));
            }
           
        }

        private void Window_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Window_LostFocus(null, null);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            m_parent.Focus();
        }


    }
}
