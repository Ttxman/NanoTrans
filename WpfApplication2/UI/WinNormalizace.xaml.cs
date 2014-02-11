using System;
using System.Collections.Generic;
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
    /// Interaction logic for WinNormalizace.xaml
    /// </summary>
    public partial class WinNormalizace : Window
    {
        /// <summary>
        /// 0 - rozepsat interpunkci; 1 - odstranit interpunkci
        /// </summary>
        public static int bIndexNormalizace = -1;
        
        public WinNormalizace()
        {
            InitializeComponent();
            if (WinNormalizace.bIndexNormalizace >= 0 && WinNormalizace.bIndexNormalizace < listBox1.Items.Count)
            {
                                
                

            }
            else
            {
                WinNormalizace.bIndexNormalizace = 0;
            }
            (listBox1.Items[WinNormalizace.bIndexNormalizace] as ListBoxItem).Focus();
            (listBox1.Items[WinNormalizace.bIndexNormalizace] as ListBoxItem).IsSelected = true;
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            WinNormalizace.bIndexNormalizace = listBox1.SelectedIndex;
            DialogResult = true;
            this.Close();
        }

        public static int ZobrazitVyberNormalizace(int aIndexNormalizace, Window aRodic)
        {
            if (aIndexNormalizace < 0) aIndexNormalizace = 0;
            WinNormalizace wn = new WinNormalizace();
            wn.Owner = aRodic;
            wn.ShowDialog();
            bool aStav = (bool)wn.DialogResult;
            if (aStav) return WinNormalizace.bIndexNormalizace;
            return -1;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btOK_Click(null, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                this.Close();
            }
        }
    }
}
