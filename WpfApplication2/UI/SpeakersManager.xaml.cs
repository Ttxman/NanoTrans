using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    /// Interaction logic for SpeakersManager.xaml
    /// </summary>
    public partial class SpeakersManager : Window, INotifyPropertyChanged
    {
        SpeakersViewModel _speakerProvider;
        bool _editable = true;
        public bool Editable
        {
            get { return _editable; }
            set
            {
                _editable = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Editable"));
            }
        }

        bool _showMiniatures = true;
        public bool ShowMiniatures
        {
            get { return _showMiniatures; }
            set
            {
                _showMiniatures = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowMiniatures"));
            }
        }
        bool _selectmany = false;
        public bool SelectMany
        {
            get { return _selectmany; }
            set
            {
                _selectmany = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectMany"));
            }
        }

        string _messageLabel = "";
        public string MessageLabel
        {
            get { return _messageLabel; }
            set
            {
                _messageLabel = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("MessageLabel"));
            }
        }
        string _message = "";
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Message"));
            }
        }


        public SpeakersViewModel SpeakerProvider
        {
            get
            {
                return _speakerProvider;
            }
            set
            {
                _speakerProvider = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SpeakerProvider"));
            }
        }

        Speaker _originalSpeaker = null;
        public Speaker OriginalSpeaker
        {
            get { return _originalSpeaker; }
        }

        SpeakerCollection _documentSpeakers;
        SpeakerCollection _localSpeakers;
        Transcription _transcription;
        public SpeakersManager(Speaker originalSpeaker, Transcription transcription, SpeakerCollection documentSpeakers, SpeakerCollection localSpeakers = null)
        {
            DataContext = this;//not good way :)
            _originalSpeaker = originalSpeaker;
            _localSpeakers = localSpeakers;
            _documentSpeakers = documentSpeakers;
            _transcription = transcription;

            InitializeComponent();
            SpeakerProvider = new SpeakersViewModel(documentSpeakers, localSpeakers);
            SpeakerProvider.CurrentChanging += (s, e) => SpeakersBox.UnselectAll();
            var ss = SpeakerProvider.GetContainerForSpeaker(originalSpeaker);
            if(ss!=null)
                ss.Marked = true;
            SpeakersBox.SelectedValue = ss;
            SpeakersBox.ScrollIntoView(SpeakersBox.SelectedItem);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void MenuItem_MergeSpeakers(object sender, RoutedEventArgs e)
        {
            var selectedSpeaker = ((SpeakerContainer)SpeakersBox.SelectedValue).Speaker;
            SpeakersManager sm2 = new SpeakersManager(selectedSpeaker, _transcription, _documentSpeakers, _localSpeakers)
            {
                MessageLabel = "Vyberte další mluvčí pro sloučení s :",
                Message = selectedSpeaker.FullName,
                Editable = false,
                SelectMany = true
            };

            if (sm2.ShowDialog() == true)
            {
                _transcription.Saved = false;
                var speakers = sm2.SpeakersBox.SelectedItems.Cast<SpeakerContainer>().Select(x => x.Speaker).ToList();
                speakers.Remove(selectedSpeaker);
                if (speakers.Count == 0)
                    return;

                if (MessageBox.Show(string.Format("Opravdu chcete sloučit mluvčí \"{0}\" s mluvčím  \"{1}\"?", string.Join("\", \"", speakers.Select(s => s.FullName)), selectedSpeaker.FullName), "sloučení mluvčích", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (SpeakerProvider.DeferRefresh())
                    {
                        foreach (var s in speakers)
                        {
                            if (_documentSpeakers != null)
                                _documentSpeakers.RemoveSpeaker(s);
                            if (_localSpeakers != null)
                                _localSpeakers.RemoveSpeaker(s);


                            SpeakerProvider.RemoveSpeaker(s);
                        }
                    }

                    foreach (TranscriptionParagraph tp in _transcription.EnumerateParagraphs())
                    {
                        if (speakers.Contains(tp.Speaker))
                            tp.Speaker = selectedSpeaker;
                    }

                    SpeakerProvider.View.Refresh();

                }
            }

        }


        private void MenuItem_DeleteSpeaker(object sender, RoutedEventArgs e)
        {
            var selectedSpeaker = ((SpeakerContainer)SpeakersBox.SelectedValue).Speaker;

            if (MessageBox.Show(string.Format("Opravdu chcete odstranit mluvčího \"{0}\"?", selectedSpeaker), "Odstranění mluvčího", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (SpeakerProvider.DeferRefresh())
                {
                    if (_documentSpeakers != null)
                        _documentSpeakers.RemoveSpeaker(selectedSpeaker);
                    if (_localSpeakers != null)
                        _localSpeakers.RemoveSpeaker(selectedSpeaker);
                    SpeakerProvider.RemoveSpeaker(selectedSpeaker);
                }

                foreach (TranscriptionParagraph tp in _transcription.EnumerateParagraphs())
                {
                    if (tp.Speaker == selectedSpeaker)
                        tp.Speaker = Speaker.DefaultSpeaker;
                }

                SpeakerProvider.View.Refresh();

            }

        }

        private void MenuItem_NewSpeaker(object sender, RoutedEventArgs e)
        {
            Speaker sp = new Speaker("-----", "-----", Speaker.Sexes.X, null) { DataBaseType = DBType.User };
            SpeakerProvider.AddLocalSpeaker(sp);
            SpeakerProvider.View.Refresh();

            var ss = SpeakerProvider.GetContainerForSpeaker(sp);
            ss.Marked = true;
            SpeakersBox.SelectedValue = ss;
            SpeakersBox.ScrollIntoView(SpeakersBox.SelectedItem);
        }


        private void SpeakersBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!_editable)
                e.Handled = true;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            _transcription.Saved = false;
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonNewSpeaker_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_NewSpeaker(null, null);
            _transcription.Saved = false;
        }

        private void MenuItemReplaceSpeaker_Click(object sender, RoutedEventArgs e)
        {
            var selectedSpeaker = ((SpeakerContainer)SpeakersBox.SelectedValue).Speaker;
            SpeakersManager sm2 = new SpeakersManager(selectedSpeaker, _transcription, _documentSpeakers, _localSpeakers)
            {
                MessageLabel = "Vyberte mluvčího kterým v přepisu nahradíte:",
                Message = selectedSpeaker.FullName,
                Editable = false,
                SelectMany = false
            };



            if (sm2.ShowDialog() == true)
            {
                _transcription.Saved = false;
                var speaker = ((SpeakerContainer)sm2.SpeakersBox.SelectedValue).Speaker;
                if (MessageBox.Show(string.Format("Opravdu chcete v prepisu nahradit mluvčího \"{0}\" mluvčím  \"{1}\"?", selectedSpeaker.FullName, speaker.FullName), "Nahrazení mluvčího", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach (TranscriptionParagraph tp in _transcription.EnumerateParagraphs())
                    {
                        if (tp.Speaker == selectedSpeaker)
                            tp.Speaker = speaker;
                    }
                }
            }
        }

        public Speaker SelectedSpeaker;
        private void SpeakersBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpeakersBox.SelectedItem == null)
                return;
            SelectedSpeaker = ((SpeakerContainer)SpeakersBox.SelectedItem).Speaker;
        }

        private void SpeakersBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(SpeakersBox.SelectedItem!=null)
                ButtonOK_Click(null, null);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void manager_Loaded(object sender, RoutedEventArgs e)
        {
            FilterTBox.Focus();
        }

    }

    public class SpeakersViewModel : INotifyPropertyChanged
    {
        ICollectionView _view;

        public ICollectionView View
        {
            get
            {
                return _view;
            }
            set
            {
                _view = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("View"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region filterproperties
        private bool _showLocal = true;
        public bool ShowLocal
        {
            get { return _showLocal; }
            set
            {
                _showLocal = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowLocal"));

                UpdateFilters();
            }
        }

        private bool _showDocument = true;
        public bool ShowDocument
        {
            get { return _showDocument; }
            set
            {
                _showDocument = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowDocument"));
                UpdateFilters();
            }
        }

        private bool _onlineAccesible = false;
        public bool OnlineAccesible
        {
            get { return _onlineAccesible; }
            set
            {
                _onlineAccesible = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("OnlineAccesible"));
                UpdateFilters();
            }
        }

        private bool _showOnline = true;
        public bool ShowOnline
        {
            get { return _showOnline; }
            set
            {
                _showOnline = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowOnline"));
                UpdateFilters();
            }
        }
        #endregion

        private string _filterstring = "";
        public string FilterString
        {
            get { return _filterstring; }
            set
            {
                _filterstring = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FilterString"));

                UpdateFilters();
            }
        }

        public bool Contains(object item)
        {
            throw new NotImplementedException();
        }

        public System.Globalization.CultureInfo Culture
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler CurrentChanged;

        public event CurrentChangingEventHandler CurrentChanging;

        SpeakerContainer _currentItem = null;
        int _currentIndex = -1;
        public object CurrentItem
        {
            get
            {
                return _currentItem;
            }
        }

        public int CurrentPosition
        {
            get { return _currentIndex; }
        }

        public IDisposable DeferRefresh()
        {
            return _view.DeferRefresh();
        }

        private bool FilterItems(object item)
        {
            SpeakerContainer cont = item as SpeakerContainer;

            bool res = false;
            if (cont == null)
                return false;

            if (_showDocument && cont.IsDocument)
                res = true;

            if (_showLocal && cont.IsLocal)
                res = true;

            if (_showOnline && cont.IsOnline)
                res = true;

            if (!string.IsNullOrWhiteSpace(_filterstring))
            {


                string flover = _filterstring.ToLower();
                res &= (cont.FullName.ToLower().Contains(flover) |
                        cont.DegreeBefore.ToLower().Contains(flover) |
                        cont.DegreeAfter.ToLower().Contains(flover) |
                        cont.Language.ToLower().Contains(flover));
            }

            return res;
        }

        public class SpeakersGroupDescription : GroupDescription
        {
            SpeakersViewModel _model;
            public SpeakersGroupDescription(SpeakersViewModel model)
            {
                _model = model;
            }

            public override object GroupNameFromItem(object item, int level, System.Globalization.CultureInfo culture)
            {
                SpeakerContainer cont = item as SpeakerContainer;

                if (cont.SpeakerColletion == _model._documentSpeakers)
                    return "document";

                if (cont.SpeakerColletion == _model._onlineSpeakers)
                    return "online";

                return "local";

            }
        }

        public System.Collections.ObjectModel.ObservableCollection<GroupDescription> GroupDescriptions
        {
            get
            {
                return new System.Collections.ObjectModel.ObservableCollection<GroupDescription>(
                    new[] { new SpeakersGroupDescription(this) });
            }
        }

        public System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups
        {
            get
            {
                return new ReadOnlyObservableCollection<object>(new ObservableCollection<object>(_allSpeakers));
            }
        }

        public bool IsCurrentAfterLast
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsCurrentBeforeFirst
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsEmpty
        {
            get { return _allSpeakers.Count == 0; }
        }

        public SortDescriptionCollection SortDescriptions
        {
            get
            {
                return null;
            }
        }

        List<SpeakerContainer> _allSpeakers;

        public bool ContainsSpeaker(Speaker sp)
        {
            return _allSpeakers.FirstOrDefault(s => s.Speaker == sp) != null;
        }

        public SpeakerContainer GetContainerForSpeaker(Speaker sp)
        {
            return _allSpeakers.FirstOrDefault(s => s.Speaker == sp);
        }

        public SpeakersViewModel(SpeakerCollection documentSpeakers, SpeakerCollection localSpeakers)
        {


            this._documentSpeakers = documentSpeakers;
            this._localSpeakers = localSpeakers;

            _allSpeakers = new List<SpeakerContainer>();

            if (documentSpeakers != null)
                _allSpeakers.AddRange(documentSpeakers.Select(s => new SpeakerContainer(documentSpeakers, s) { IsDocument = true }));

            if (localSpeakers != null)
                _allSpeakers.AddRange(localSpeakers.Select(s => new SpeakerContainer(documentSpeakers, s) { IsLocal = true }));

            _view = CollectionViewSource.GetDefaultView(_allSpeakers);

            _view.Filter = FilterItems;
        }

        private void UpdateFilters()
        {
            using (_view.DeferRefresh())
            {
                _view.Filter = null;
                _view.Filter = FilterItems;
            }

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Filter"));
        }


        private SpeakerCollection _documentSpeakers;

        private SpeakerCollection _localSpeakers;

        private SpeakerCollection _onlineSpeakers;

        public void RemoveSpeaker(Speaker s)
        {
            var cont = _allSpeakers.FirstOrDefault(sc => sc.Speaker == s);
            if (s.DataBaseType == DBType.User)
                _localSpeakers.Remove(s);
            else if (s.DataBaseType == DBType.File)
                _documentSpeakers.Remove(s);

            if (cont != null)
            {
                _allSpeakers.Remove(cont);
            }
        }


        public void AddDocumentSpeaker(Speaker sp)
        {
            throw new NotImplementedException();
           // _allSpeakers.Add(new SpeakerContainer(_documentSpeakers, sp));
        }

        internal void AddLocalSpeaker(Speaker sp)
        {
            _localSpeakers.Add(sp);
            _allSpeakers.Add(new SpeakerContainer(_documentSpeakers, sp) { IsLocal = true});
        }
    }

    public class SpeakerContainer : INotifyPropertyChanged
    {
        bool _updating = false;

        public bool Updating
        {
            get { return _updating; }
            set
            {
                _updating = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Updating"));
            }
        }

        public ReadOnlyCollection<SpeakerAttributeContainer> Attributes
        {
            get { return Speaker.Attributes.Select(a=>new SpeakerAttributeContainer(a)).ToList().AsReadOnly(); }
        }

        bool _marked = false;
        public bool Marked
        {
            get
            {
                return _marked;
            }
            set
            {
                _marked = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Marked"));
            }
        }



        Speaker _speaker;
        public Speaker Speaker
        {
            get
            {
                return _speaker;
            }
        }
        public SpeakerContainer( Speaker s):this(null,s)
        {
        }
        public SpeakerCollection SpeakerColletion;
        public SpeakerContainer(SpeakerCollection speakers, Speaker s)
        {
            _speaker = s;
            this.SpeakerColletion = speakers;
        }

        bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsLoading"));
            }
        }

        bool _changed = false;
        public bool Changed
        {
            get { return _changed; }
            set
            {
                _changed = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Changed"));
            }
        }


        bool _online = false;
        public bool IsOnline
        {
            get { return _online; }
            set
            {
                _online = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsOnline"));
            }
        }

        bool _document = false;
        public bool IsDocument
        {
            get { return _document; }
            set
            {
                _document = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDocument"));
            }
        }

        bool _local = false;
        public bool IsLocal
        {
            get { return _local; }
            set
            {
                _local = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsLocal"));
            }
        }


        string _degreeBefore = null;
        string _firstName = null;
        string _secondName = null;
        string _surName = null;
        string _degreeAfter = null;
        string _imgBase64 = null;
        Speaker.Sexes? _sex;
        //Attributes

        public string DegreeBefore
        {
            get
            {
                return _degreeBefore ?? _speaker.DegreeBefore ?? "";
            }

            set
            {
                _speaker.DegreeBefore = _degreeBefore = value.Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DegreeBefore"));
            }
        }

        public string FirstName
        {
            get
            {
                return _firstName ?? _speaker.FirstName;
            }

            set
            {
                _speaker.FirstName = _firstName = value.Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FirstName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        string _language;
        public string Language
        {
            get
            {
                return _language ?? _speaker.DefaultLang;
            }

            set
            {
                _speaker.DefaultLang = _language = value;
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Language"));
            }
        }

        public string SecondName
        {
            get
            {
                return _secondName ?? _speaker.MiddleName;
            }

            set
            {
                _speaker.MiddleName = _secondName = value.Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SecondName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }
        public string SurName
        {
            get
            {
                return _surName ?? _speaker.Surname;
            }

            set
            {
                _speaker.Surname = _surName = value.Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SurName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }
        public string DegreeAfter
        {
            get
            {
                return _degreeAfter ?? _speaker.DegreeAfter ?? "";
            }

            set
            {
                _speaker.DegreeAfter = _degreeAfter = value.Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DegreeAfter"));
            }
        }
        public string ImgBase64
        {
            get
            {
                return _imgBase64 ?? _speaker.ImgBase64;
            }

            set
            {
                _speaker.ImgBase64 = _imgBase64 = value;
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ImgBase64"));
            }
        }
        public Speaker.Sexes Sex
        {
            get
            {
                return _sex ?? Speaker.Sex;
            }

            set
            {
                _sex = Speaker.Sex = value;
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Sex"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        public string FullName
        {
            get { return _speaker.FullName; }
        }
        //Attributes

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshAttributes()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Attributes"));
        }
    }


    [ValueConversion(typeof(bool), typeof(SelectionMode))]
    public class MultipleSelectionConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (((SelectionMode)value) == SelectionMode.Extended)
                return true;
            else
                return false;
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return SelectionMode.Extended;
            else
                return SelectionMode.Single;
        }
    }
}
