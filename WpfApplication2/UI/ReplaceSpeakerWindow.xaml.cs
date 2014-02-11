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
        public Speaker From
        {
            get { return (Speaker)comboboxReplaced.SelectedItem; }
        }

        public Speaker To
        {
            get { return (Speaker)comboBoxReplacer.SelectedItem; }
        }

        public ReplaceSpeakerWindow(MySpeakers speakers)
        {
            InitializeComponent();

            List<Speaker> from = new List<Speaker>(speakers.Speakers);
            from.Add(new Speaker() { ID = Speaker.DefaultID, Surname = "Neidentifikovaný mluvčí" });
            comboboxReplaced.ItemsSource = from;
            comboBoxReplacer.ItemsSource = new List<Speaker>(speakers.Speakers);
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
