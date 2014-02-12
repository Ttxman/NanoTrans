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
        List<string> _data;
        public PickOneDialog(List<string> data, string title)
        {
            InitializeComponent();
            box.ItemsSource = _data = data;
            this.Title = title;
        }

        private int _line = 0;

        public int SelectedIndex
        {
            get { return _line; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _line = _data.IndexOf(box.SelectedItem.ToString());

            this.DialogResult = true;
            Close();
        }

        private void box_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Button_Click(null, null);
        }

        private void box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Button_Click(null, null);
        }


        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                box.ItemsSource = _data;
            }
            else
            {
                box.ItemsSource = _data.Where(s => s.Contains(textBox1.Text));
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Button_Click(null, null);
            else if (e.Key == Key.Up)
            {
                int val = box.SelectedIndex - 1;
                box.SelectedIndex = (val >= 0) ? val : 0;
            }
            else if (e.Key == Key.Down)
            {
                int val = box.SelectedIndex + 1;
                box.SelectedIndex = (val < box.Items.Count) ? val : box.Items.Count - 1;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (box.Items.Count > 0)
                box.SelectedIndex = 0;

            textBox1.Focus();
        }

        private void textBox1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                int val = box.SelectedIndex - 1;
                box.SelectedIndex = (val >= 0) ? val : 0;
                box.ScrollIntoView(box.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                int val = box.SelectedIndex + 1;
                box.SelectedIndex = (val < box.Items.Count) ? val : box.Items.Count - 1;
                box.ScrollIntoView(box.SelectedItem);
                e.Handled = true;
            }
        }
    }
}
