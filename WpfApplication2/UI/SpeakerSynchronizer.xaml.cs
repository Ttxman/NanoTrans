using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for SpeakerSynchronizer.xaml
    /// </summary>
    public partial class SpeakerSynchronizer : Window
    {
        public SpeakerSynchronizer()
        {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class SpeakerPair : INotifyPropertyChanged
    {
        SpeakerContainer _speaker1;
        public SpeakerContainer Speaker1
        {
            get
            {
                return _speaker1;
            }
            set
            {
                _speaker1 = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Speaker1"));
            }
        }

        SpeakerContainer _speaker2;
        public SpeakerContainer Speaker2
        {
            get
            {
                return _speaker2;
            }
            set
            {
                _speaker2 = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Speaker2"));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }


    [ValueConversion(typeof(Speaker), typeof(SpeakerContainer))]
    public class SpeakerWrapperConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is Speaker)
                return new SpeakerContainer(value as Speaker);

            throw new ArgumentException();
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is SpeakerContainer)
                return ((SpeakerContainer)value).Speaker;

            throw new ArgumentException();
        }
    }

}
