using NanoTrans.Core;
using NanoTrans.OnlineAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
    [ValueConversion(typeof(bool), typeof(SelectionMode))]
    public class MultipleSelectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return SelectionMode.Extended;
            else
                return SelectionMode.Single;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (((SelectionMode)value) == SelectionMode.Extended)
                return true;
            else
                return false;
        }
    }

    public class SpeakerContainer : INotifyPropertyChanged
    {
        public SpeakerCollection SpeakerColletion;
        string _degreeAfter = null;
        string _degreeBefore = null;
        string _firstName = null;
        bool _changed = false;
        string _imgBase64 = null;
        bool _isLoading = false;
        string _language;
        bool _marked = false;
        string _secondName = null;
        Speaker.Sexes? _sex;
        Speaker _speaker;
        string _surName = null;
        bool _updating = false;

        public SpeakerContainer(Speaker s)
            : this(null, s)
        {
        }

        public SpeakerContainer(SpeakerCollection speakers, Speaker s)
        {
            _speaker = s;
            this.SpeakerColletion = speakers;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ReadOnlyCollection<SpeakerAttributeContainer> Attributes
        {
            get { return Speaker.Attributes.Select(a => new SpeakerAttributeContainer(a)).ToList().AsReadOnly(); }
        }

        public string DegreeAfter
        {
            get
            {
                return _degreeAfter ?? _speaker.DegreeAfter ?? "";
            }

            set
            {
                _speaker.DegreeAfter = _degreeAfter = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DegreeAfter"));
            }
        }

        public string DegreeBefore
        {
            get
            {
                return _degreeBefore ?? _speaker.DegreeBefore ?? "";
            }

            set
            {
                _speaker.DegreeBefore = _degreeBefore = (value ?? "").Trim();
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
                _speaker.FirstName = _firstName = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FirstName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

        public string FullName
        {
            get { return _speaker.FullName; }
        }

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

        public bool IsDocument
        {
            get { return Speaker.DataBaseType == DBType.File; }
        }

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

        public bool IsLocal
        {
            get { return Speaker.DataBaseType == DBType.User; }
        }

        public bool IsOnline
        {
            get { return Speaker.DataBaseType == DBType.Api; }
        }

        public bool IsOffline
        {
            get { return !IsOnline; }
        }

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

        public bool PinnedToDocument
        {
            get
            {
                return _speaker.PinnedToDocument;
            }

            set
            {

                _speaker.PinnedToDocument = value;
                Changed = true;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("PinnedToDocument"));
            }
        }

        //Attributes
        public string SecondName
        {
            get
            {
                return _secondName ?? _speaker.MiddleName;
            }

            set
            {
                _speaker.MiddleName = _secondName = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SecondName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
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

        public Speaker Speaker
        {
            get
            {
                return _speaker;
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
                _speaker.Surname = _surName = (value ?? "").Trim();
                Changed = true;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SurName"));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FullName"));
            }
        }

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
        //Attributes
        public void RefreshAttributes()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Attributes"));
        }

        public void UpdateBindings()
        {

            _degreeAfter = null;
            _degreeBefore = null;
            _firstName = null;
            _changed = false;
            _imgBase64 = null;
            _isLoading = false;
            _language = null;
            _secondName = null;
            _sex = null;
            _surName = null;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(null));

        }
    }

    /// <summary>
    /// Interaction logic for SpeakersManager.xaml
    /// </summary>
    public partial class SpeakersManager : Window, INotifyPropertyChanged
    {
        public Speaker SelectedSpeaker{get; set;}
        public SpeakerContainer SelectedSpeakerContainer{get; set;}
        SpeakerCollection _documentSpeakers;
        bool _editable = true;
        bool _changed = false;
        SpeakerCollection _localSpeakers;
        string _message = "";
        string _messageLabel = "";
        Speaker _originalSpeaker = null;
        bool _selectmany = false;
        bool _showMiniatures = true;
        SpeakersViewModel _speakerProvider;
        WPFTranscription _transcription;

        public SpeakersManager(Speaker originalSpeaker, WPFTranscription transcription, SpeakerCollection documentSpeakers, SpeakerCollection localSpeakers = null)
        {
            DataContext = this;//not good way :)
            _originalSpeaker = originalSpeaker;
            _localSpeakers = localSpeakers;
            _documentSpeakers = documentSpeakers;
            _transcription = transcription;

            InitializeComponent();
            SpeakerProvider = new SpeakersViewModel(documentSpeakers, localSpeakers, transcription.Api);
            var ss = SpeakerProvider.GetContainerForSpeaker(originalSpeaker);
            if (ss != null)
                ss.Marked = true;
            SpeakersBox.SelectedValue = ss;
            SpeakersBox.ScrollIntoView(SpeakersBox.SelectedItem);
            //SpeakersBox.Items.SortDescriptions.Add( new SortDescription("",ListSortDirection.Ascending));
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public Speaker OriginalSpeaker
        {
            get { return _originalSpeaker; }
        }

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

        public bool SpeakerChanged
        {
            get { return _changed; }
            set { _changed = true; } //cannot unchange speaker, refresh is required
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
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            preventDoublecheck = false;
            this.Close();
        }

        private void ButtonNewSpeaker_Click(object sender, RoutedEventArgs e)
        {
            FilterTBox.Text = "";
            MenuItem_NewSpeaker(null, null);
            _transcription.Saved = false;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            preventDoublecheck = false;
            _transcription.Saved = false;
            this.DialogResult = true;
            this.Close();
        }

        private void manager_Loaded(object sender, RoutedEventArgs e)
        {
            FilterTBox.Focus();
        }

        //TODO: translate
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
                SpeakersBox.UnselectAll();
            }

        }

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

                //merge
                selectedSpeaker.Merges.AddRange(speakers.Select(s => new DBMerge(s.DBID, s.DataBaseType)));
                selectedSpeaker.Merges.AddRange(speakers.SelectMany(s => s.Merges));

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
                    SpeakersBox.UnselectAll();
                }
            }

        }
        private void MenuItem_NewSpeaker(object sender, RoutedEventArgs e)
        {

            Speaker sp;

            if (_speakerProvider.IsOnline)
                sp = new ApiSynchronizedSpeaker("-----", "-----", Speaker.Sexes.X) 
                { 
                    IsSaved = false,
                    DataBaseType= DBType.Api,
                };
            else
                sp = new Speaker("-----", "-----", Speaker.Sexes.X, null) { DataBaseType = DBType.User }; ;


            SpeakerProvider.AddLocalSpeaker(sp);


            SpeakerProvider.View.Refresh();

            var ss = SpeakerProvider.GetContainerForSpeaker(sp);
            ss.Marked = true;
            SpeakersBox.SelectedValue = ss;
            SpeakersBox.ScrollIntoView(SpeakersBox.SelectedItem);
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

        private async void SpeakerDetails_RevertSpeakerRequest(SpeakerContainer spk)
        {
            using (var wc = new WaitCursor())
            {
                var s = await _transcription.Api.GetSpeaker(spk.Speaker.DBID);
                Speaker.MergeFrom(spk.Speaker, s);
                spk.UpdateBindings();
            }
        }

        private void SpeakersBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!_editable)
                e.Handled = true;
        }
        private void SpeakersBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SpeakersBox.SelectedItem != null)
                ButtonOK_Click(null, null);
        }

        private void SpeakersBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var sp = (SpeakerContainer)SpeakersBox.GetObjectAtPoint<ListBoxItem>(e.GetPosition(SpeakersBox));
            if (SpeakersBox.SelectedItem != sp)
                SpeakersBox.SelectedItem = sp;
        }

        private void SpeakersBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpeakersBox.SelectedItem == null)
            {
                SelectedSpeaker = null;
                SelectedSpeakerContainer = null;
            }
            else
            {
                SelectedSpeakerContainer = (SpeakerContainer)SpeakersBox.SelectedItem;
                SelectedSpeaker = SelectedSpeakerContainer.Speaker;
            }
        }
        private void SpeakerSmall_speakermodified()
        {
            SpeakerChanged = true;
        }

        bool preventDoublecheck = false;
        private void manager_Closing(object sender, CancelEventArgs e)
        {
            if (preventDoublecheck)
                return;
            preventDoublecheck = true;
            Task<bool> t = new Task<bool>(() => this.SpeakerProvider.CloseConnection().Result);
            t.ConfigureAwait(false);
            t.Start();
            if (!t.Result)
            {
                e.Cancel = true;
            }

        }

        private async void SpeakerDetails_SaveSpeakerClick(SpeakerContainer spk)
        {
            using (var wc = new WaitCursor())
            {
                ApiSynchronizedSpeaker ss = spk.Speaker as ApiSynchronizedSpeaker;
                if (ss == null)
                    return;
                if (ss.IsSaved)
                {
                    if (await _transcription.Api.UpdateSpeaker(ss))
                        spk.Changed = false;
                }
                else
                {
                    if (await _transcription.Api.AddSpeaker(ss))
                        spk.Changed = false;
                }
            }
        }
    }

    public class SpeakersViewModel : INotifyPropertyChanged
    {
        List<SpeakerContainer> _allSpeakers;
        int _currentIndex = -1;
        SpeakerContainer _currentItem = null;
        private SpeakerCollection _documentSpeakers;
        private string _filterstring = "";
        private SpeakerCollection _localSpeakers;


        ICollectionView _view;
        private SpeakersApi _api;
        public SpeakersViewModel(SpeakerCollection documentSpeakers, SpeakerCollection localSpeakers, SpeakersApi api)
        {
            _api = api;
            if (_api != null)
            {
                _loadingTimer = new System.Timers.Timer(1000);
                _loadingTimer.AutoReset = false;
                _loadingTimer.Elapsed += _loadingTimer_Elapsed;

            }

            this._documentSpeakers = documentSpeakers;
            this._localSpeakers = localSpeakers;

            ReloadSpeakers();
        }


        private void ReloadSpeakers()
        {
            _allSpeakers = new List<SpeakerContainer>();

            if (_documentSpeakers != null)
                _allSpeakers.AddRange(_documentSpeakers.Select(s => new SpeakerContainer(_documentSpeakers, s)));

            if (_localSpeakers != null)
                _allSpeakers.AddRange(_localSpeakers.Select(s => new SpeakerContainer(_localSpeakers, s)));

            _allSpeakers.Sort((x, y) => { int cmp = string.Compare(x.SurName, y.SurName); return (cmp != 0) ? cmp : string.Compare(x.FirstName, y.FirstName); });

            View = CollectionViewSource.GetDefaultView(_allSpeakers);

            _view.Filter = FilterItems;
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public string FilterString
        {
            get { return _filterstring; }
            set
            {
                _filterstring = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FilterString"));
                if (_api != null)
                {
                    UpdateOnlineSpeakers();
                }
                UpdateFilters();
            }
        }


        Visibility _LoadingVisible = Visibility.Collapsed;
        public Visibility LoadingVisible
        {
            get { return _LoadingVisible; }
            set
            {
                _LoadingVisible = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("LoadingVisible"));
            }
        }

        System.Timers.Timer _loadingTimer;
        bool _loadingFilterChanged = false;


        void _loadingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {

            if (_loadingFilterChanged)
            {
                UpdateOnlineSpeakers();
            }
            _loadingFilterChanged = false;
        }

        string _lastFilterString = null;
        private void UpdateOnlineSpeakers()
        {
            if (_lastFilterString != _filterstring)
            {
                _loadingFilterChanged = true;
                if (_filterstring != null && _filterstring.Length > 1 && !_loadingTimer.Enabled)
                {
                    _loadingTimer.Start();
                    _api.Cancel();
                    LoadingVisible = Visibility.Visible;
                    Task.Run(async () =>
                        {
                            var speakerst = await _api.SimpleSearch(_filterstring);
                            UpdateOnlineSpeakers(speakerst);
                            LoadingVisible = Visibility.Collapsed;
                        });

                    _lastFilterString = _filterstring;
                }
            }

        }

        List<ApiSynchronizedSpeaker> _onlineSpeakers = new List<ApiSynchronizedSpeaker>();
        private async void UpdateOnlineSpeakers(IEnumerable<ApiSynchronizedSpeaker> speakerst)
        {
            using (var wc = new WaitCursor())
            {
                var ToRemove = new HashSet<string>(_onlineSpeakers.Except(speakerst, new SpeakerComparer()).Select(s => s.DBID));
                var ToRemoveCont = _allSpeakers.Where(c => ToRemove.Contains(c.Speaker.DBID));


                await SaveOnlineSpeakersUsavedChanges(ToRemoveCont.Where(c => c.Changed).Select(c => c.Speaker));


                _allSpeakers.RemoveAll(sc => ToRemove.Contains(sc.Speaker.DBID));//remove online items missing from this search

                var ToUpdate = _allSpeakers
                    .Where(sc => sc.Speaker.DataBaseType != DBType.File)
                    .Join(speakerst, sc => sc.Speaker.DBID, s => s.DBID, (sc, s) => Tuple.Create(sc, s))
                    .ToArray();

                foreach (var item in ToUpdate)
                {
                    Speaker.MergeFrom(item.Item1.Speaker, item.Item2);
                    item.Item1.UpdateBindings();
                }

                var ToAdd = speakerst.Except(ToUpdate.Select(t => t.Item2));
                _allSpeakers.AddRange(ToAdd.Select(s => new SpeakerContainer(s)));


                var DoNotRemove = new HashSet<string>(speakerst
                    .Except(_localSpeakers
                        .Concat(_documentSpeakers)
                        .Where(s => s.GetType() == typeof(ApiSynchronizedSpeaker))
                        .Cast<ApiSynchronizedSpeaker>())
                        .Select(s => s.DBID));

                _onlineSpeakers = speakerst.Where(s => !DoNotRemove.Contains(s.DBID)).ToList();
            }
        }

        /// <summary>
        /// returns false if user cancels
        /// </summary>
        /// <param name="speaker"></param>
        /// <returns></returns>
        private async Task<bool> SaveOnlineSpeakersUsavedChanges(IEnumerable<Speaker> unsavedSpeakers, bool useMessagebox = true)
        {
            if (unsavedSpeakers.Count() <= 0)
                return true;

            bool update = !useMessagebox;
            if (useMessagebox)
            {
                var result = MessageBox.Show("Nekterí mluvčí obsahují změny, které nebyly uloženy do centrálního úložiště. \n Uložit všechny?", "Uložit změny?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                if (result != MessageBoxResult.Cancel)
                {
                    if (result == MessageBoxResult.Yes)
                        update = true;
                }
            }



            if (update)
            {
                foreach (ApiSynchronizedSpeaker speaker in unsavedSpeakers)
                {
                    if (speaker.IsSaved)
                        await _api.UpdateSpeaker(speaker);
                    else
                        await _api.AddSpeaker(speaker);
                }

                return true;
            }
            return false;
        }


        private class SpeakerComparer : IEqualityComparer<ApiSynchronizedSpeaker>
        {

            public bool Equals(ApiSynchronizedSpeaker x, ApiSynchronizedSpeaker y)
            {
                return x.DBID == y.DBID;
            }

            public int GetHashCode(ApiSynchronizedSpeaker obj)
            {
                return obj.DBID.GetHashCode();
            }
        }


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
        #region filterproperties
        private bool _showDocument = true;
        private bool _showLocal = true;
        private bool _showOnline = true;

        /// <summary>
        /// Is connected to online storage?
        /// </summary>
        public bool IsOnline
        {
            get { return _api != null; }
        }

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


        public bool ContainsSpeaker(Speaker sp)
        {
            return _allSpeakers.Any(s => s.Speaker == sp);
        }

        public IDisposable DeferRefresh()
        {
            return _view.DeferRefresh();
        }

        public SpeakerContainer GetContainerForSpeaker(Speaker sp)
        {
            return _allSpeakers.FirstOrDefault(s => s.Speaker == sp);
        }

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
                View.Refresh();
            }
        }

        internal void AddOnlineSpeaker(ApiSynchronizedSpeaker sp)
        {
            _onlineSpeakers.Add(sp);
            _allSpeakers.Add(new SpeakerContainer(_documentSpeakers, sp));
            View.Refresh();
        }

        internal void AddLocalSpeaker(Speaker sp)
        {
            _localSpeakers.Add(sp);
            _allSpeakers.Add(new SpeakerContainer(_documentSpeakers, sp));
            View.Refresh();
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

        internal async Task<bool> CloseConnection()
        {
            if (_api != null)
            {
                return await SaveOnlineSpeakersUsavedChanges(_allSpeakers.Where(sc => sc.IsOnline && sc.Changed).Select(sc => sc.Speaker));
            }

            return true;
        }
    }
}
