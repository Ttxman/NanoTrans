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


            _pairs = _transcription.Speakers
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.FirstName)
                .ThenBy(s => s.MiddleName)
                .Select(s => new SpeakerPair { Speaker1 = new SpeakerContainer(s) })
                .ToList();

            //Join speakers from document with speakers from localDB by fullname and order pairs
            var first = _pairs.Join(_speakersDatabase, p => p.Speaker1.Speaker.FullName.ToLower(), s => s.FullName.ToLower(), (sp, s) => new { document = sp, local = s })
                .Distinct()
                .OrderBy(s => s.local.Surname)
                .ThenBy(s => s.local.FirstName)
                .ThenBy(s => s.local.MiddleName);

            //get all speakers from local DB that was not joined
            var other = _speakersDatabase.Except(first.Select(s => s.local).ToArray())
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.FirstName)
                .ThenBy(s => s.MiddleName);

            //concat all speakers from local DB but ordered to match remaining speakers in document.
            listlocal.ItemsSource = first.Select(s => s.local).Concat(other).ToList();


            //add pairs with order matching to listlocal and concat speakers without matching name in local DB
            listdocument.ItemsSource = _pairs = first.Select(s => s.document)
                                                        .OrderBy(s => s.Speaker1.SurName)
                                                        .ThenBy(s => s.Speaker1.FirstName)
                                                        .ThenBy(s => s.Speaker1.MiddleName)
                                                        .Concat(_pairs.Except(first.Select(s => s.document))).ToList();





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
            var cleardicts = _pairs.Where(p => p.Speaker2 == null).Count();
            if (cleardicts > 0)
            {
                if (MessageBox.Show("Chcete v lokální databázi vytvořit nové položky pro všechny nepřiřazené mluvčí?", "automatické vytváření mluvčích", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach (var p in _pairs.Where(pa => pa.Speaker2 == null))
                    {
                        var n = new SpeakerContainer(p.Speaker1.Speaker.Copy());
                        n.Speaker.DataBaseType = DBType.User;
                        _speakersDatabase.Add(n.Speaker);
                        p.Speaker2 = n;
                    }
                }
            }

            var pairdict = _pairs.Where(p => p.Speaker2 != null).ToDictionary(p => p.Speaker1.Speaker, p => (p.Speaker2 == null) ? null : p.Speaker2.Speaker);
            _transcription.BeginUpdate();
            foreach (var par in _transcription.EnumerateParagraphs())
            {
                Speaker os;
                if (pairdict.TryGetValue(par.Speaker, out os))
                    par.Speaker = os;
            }
            _transcription.EndUpdate();
            foreach (var spe in pairdict.Keys)
            {
                _transcription.Speakers.RemoveSpeaker(spe);
            }

            this.DialogResult = true;
            Close();

           
        }

        private void MenuItemClearPairing_Click(object sender, RoutedEventArgs e)
        {
            if (listdocument.SelectedValue != null)
            {
                ((SpeakerPair)listdocument.SelectedValue).Speaker2 = null;
            }
        }

        private void listdocument_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            MenuItemClearPairing_Click(null, null);
        }

        private void documentFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(documentFilterBox.Text))
            {
                listdocument.Items.Filter = x => true;
            }
            else
            {
                listdocument.Items.Filter = x => (((SpeakerPair)x).Speaker1.FullName.ToLower().Contains(documentFilterBox.Text.ToLower()));
            }
        }

        private void userFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(documentFilterBox.Text))
            {
                listlocal.Items.Filter = x => true;
            }
            else
            {
                listlocal.Items.Filter = x => (((Speaker)x).FullName.ToLower().Contains(userFilterBox.Text.ToLower()));
            }
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
                return new SpeakerContainer(value as Speaker);


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
