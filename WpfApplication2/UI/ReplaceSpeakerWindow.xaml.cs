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
    /// Interaction logic for ReplaceSpeakerWindow.xaml
    /// </summary>
    public partial class ReplaceSpeakerWindow : Window
    {
        public MySpeaker From
        {
            get { return (MySpeaker)comboboxReplaced.SelectedItem; }
        }

        public MySpeaker To
        {
            get { return (MySpeaker)comboBoxReplacer.SelectedItem; }
        }

        public ReplaceSpeakerWindow(MySpeakers speakers)
        {
            InitializeComponent();

            List<MySpeaker> from = new List<MySpeaker>(speakers.Speakers);
            from.Add(new MySpeaker() { ID = MySpeaker.DefaultID, Surname = "Neidentifikovaný mluvčí" });
            comboboxReplaced.ItemsSource = from;
            comboBoxReplacer.ItemsSource = new List<MySpeaker>(speakers.Speakers);
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
