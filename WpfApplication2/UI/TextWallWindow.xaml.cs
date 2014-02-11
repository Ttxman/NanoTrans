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
    /// Interaction logic for TextWallWindow.xaml
    /// </summary>
    public partial class TextWallWindow : Window
    {
        private TextWallWindow()
        {
            InitializeComponent();
        }


        public static bool ShowWall(bool buttons, string caption,string text)
        {
            var w = new TextWallWindow();
            w.textbox.Text = text;
            w.Title = caption;
            if (!buttons)
            {
                w.ButtonStack.Visibility = Visibility.Collapsed;
            }

            return w.ShowDialog() == true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    }
}
