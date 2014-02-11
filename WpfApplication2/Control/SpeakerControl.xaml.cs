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
using System.Globalization;
using NanoTrans.Core;
using System.IO;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for SpeakerControl.xaml
    /// </summary>
    public partial class SpeakerControl : UserControl
    {
        public static readonly DependencyProperty SpeakerProperty =
        DependencyProperty.Register("SpeakerContainer", typeof(SpeakerContainer), typeof(SpeakerControl), new FrameworkPropertyMetadata(OnSpeakerChanged));

        public static void OnSpeakerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        public SpeakerContainer SpeakerContainer
        {
            get
            {
                return (SpeakerContainer)GetValue(SpeakerProperty);
            }
            set
            {

                SetValue(SpeakerProperty, value);
            }
        }

        public SpeakerControl()
        {
            InitializeComponent();
        }
        public delegate void SpeakerDelegate(SpeakerContainer spk);
        public event SpeakerDelegate StoreSpeakerRequest;
        public event SpeakerDelegate RevertSpeakerRequest;





        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (RevertSpeakerRequest != null)
                RevertSpeakerRequest(this.SpeakerContainer);
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (StoreSpeakerRequest != null)
                StoreSpeakerRequest(this.SpeakerContainer);
        }
    }

    [ValueConversion(typeof(Speaker.Sexes),typeof(string))]
    public class SexConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
                if(((string)value) == Properties.Strings.SexConversionFemale)
                    return Speaker.Sexes.Female;
                else if (((string)value) == Properties.Strings.SexConversionMale)
                    return Speaker.Sexes.Male;
                else
                    return Speaker.Sexes.X;
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            switch((Speaker.Sexes)value)
            {
                case Speaker.Sexes.Female:
                    return Properties.Strings.SexConversionFemale;
                case Speaker.Sexes.Male:
                    return Properties.Strings.SexConversionMale;
                default:
                    return "--";
            }
        }
    }


    [ValueConversion(typeof(string), typeof(Image))]
    public class JPGB64Converter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            string s = value as string;

            if (string.IsNullOrEmpty(s))
                return null;


            BitmapImage bim = new BitmapImage();
            bim.StreamSource = new MemoryStream(System.Convert.FromBase64String(s));

            return new Image() { Source = bim };

        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
