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
        private WPFTranscription _transcription;
        private AdvancedSpeakerCollection _speakersDatabase;
        List<SpeakerPair> _pairs;
        public SpeakerSynchronizer()
        {
            InitializeComponent();
        }

        public SpeakerSynchronizer(WPFTranscription Transcription, AdvancedSpeakerCollection SpeakersDatabase)
            : this()
        {
            this._transcription = Transcription;
            this._speakersDatabase = SpeakersDatabase;

            listlocal.ItemsSource = _speakersDatabase.Speakers;

            _pairs = _transcription._speakers.Speakers.Select(s => new SpeakerPair { Speaker1 = new SpeakerContainer(s) { IsDocument = true } }).ToList();
            listdocument.ItemsSource = _pairs;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {

        }


        private void SpeakerSmall_MouseMove(object sender, MouseEventArgs e)
        {
            var ss = sender as SpeakerSmall;

            if (ss != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(ss, ss.SpeakerContainer, DragDropEffects.Copy);
            }
        }

        private void SpeakerSmall_Drop(object sender, DragEventArgs e)
        {
            if (sender != null && e.AllowedEffects.HasFlag(DragDropEffects.Copy) && e.Data.GetData(typeof(SpeakerContainer)) != null)
            {
                SpeakerSmall ss = (sender as UIElement).VisualFindChild<SpeakerSmall>();
                e.Effects = DragDropEffects.Copy;
                SpeakerContainer cont = (SpeakerContainer)e.Data.GetData(typeof(SpeakerContainer));
                (ss.DataContext as SpeakerPair).Speaker2 = cont;
                e.Handled = true;
            }
        }

        private void SpeakerSmall_DragOver(object sender, DragEventArgs e)
        {
            if (e.AllowedEffects.HasFlag(DragDropEffects.Copy) && e.Data.GetData(typeof(SpeakerContainer)) != null)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

        }

        private void SpeakerSmall_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var ss = sender as SpeakerSmall;
            if (sender != null && e.ChangedButton == MouseButton.Left)
            {
                speakerControl.SpeakerContainer = ss.SpeakerContainer;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var pairdict = _pairs.Where(p => p.Speaker2 != null).ToDictionary(p => p.Speaker1.Speaker, p => (p.Speaker2 == null) ? null : p.Speaker2.Speaker);
            foreach (var par in _transcription.EnumerateParagraphs())
            {
                Speaker os;
                if (pairdict.TryGetValue(par.Speaker, out os))
                    par.Speaker = os;
            }

            foreach (var spe in pairdict.Keys)
            {
                _transcription.Speakers.RemoveSpeaker(spe);
            }

            this.DialogResult = true;
            Close();
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
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            if (value is Speaker)
                return new SpeakerContainer(value as Speaker) { IsLocal = true };


            throw new ArgumentException();
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is SpeakerContainer)
                return ((SpeakerContainer)value).Speaker;

            throw new ArgumentException();
        }
    }

    public sealed class NullToVisibiltyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
