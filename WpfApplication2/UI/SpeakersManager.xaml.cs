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

    /// <summary>
    /// Interaction logic for SpeakersManager.xaml
    /// </summary>
    public partial class SpeakersManager : Window, INotifyPropertyChanged
    {

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName]string caller = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        public Speaker SelectedSpeaker { get; set; }
        public SpeakerContainer SelectedSpeakerContainer { get; set; }
        SpeakerCollection _documentSpeakers;
        bool _editable = true;
        bool _changed = false;
        SpeakerCollection _localSpeakers;
        string _message = "";
        string _messageLabel = "";
        Speaker _originalSpeaker = null;
        bool _selectmany = false;
        bool _showMiniatures = true;
        SpeakerManagerViewModel _speakerProvider;
        WPFTranscription _transcription;

        public SpeakersManager(Speaker originalSpeaker, WPFTranscription transcription, SpeakerCollection documentSpeakers, SpeakerCollection localSpeakers = null)
        {
            DataContext = this;//not good :)
            _originalSpeaker = originalSpeaker;
            _localSpeakers = localSpeakers;
            _documentSpeakers = documentSpeakers;
            _transcription = transcription;

            InitializeComponent();
            SpeakerProvider = new SpeakerManagerViewModel(documentSpeakers, localSpeakers, transcription.Api,this);
            var ss = SpeakerProvider.GetContainerForSpeaker(originalSpeaker);
            if (ss != null)
                ss.Marked = true;
            SpeakersBox.SelectedValue = ss;
            SpeakersBox.ScrollIntoView(SpeakersBox.SelectedItem);

            if (_transcription.Api != null)
            {
                SpeakerProvider.ShowLocal = false;
                SpeakerProvider.ShowOnline = true;
            }
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
        public SpeakerManagerViewModel SpeakerProvider
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
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {

            if (SelectedSpeakerContainer != null)
            {
                if (!CheckChanges(SelectedSpeakerContainer))
                    return;
            }


            preventDoublecheck = false;
            this.DialogResult = true;
            this.Close();
        }

        private void manager_Loaded(object sender, RoutedEventArgs e)
        {
            FilterTBox.Focus();
        }


        private void MenuItem_DeleteSpeaker(object sender, RoutedEventArgs e)
        {
            var selectedSpeaker = ((SpeakerContainer)SpeakersBox.SelectedValue).Speaker;

            if (MessageBox.Show(string.Format(Properties.Strings.SpeakersManagerDeleteSpeakerDialogFormat, selectedSpeaker), Properties.Strings.SpeakersManagerDeleteSpeakerDialogQuestion, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (SpeakerProvider.DeferRefresh())
                {
                    SpeakerProvider.DeleteSpeaker(selectedSpeaker);
                    _transcription.BeginUpdate();
                    foreach (TranscriptionParagraph tp in _transcription.EnumerateParagraphs())
                    {
                        if (tp.Speaker == selectedSpeaker)
                            tp.Speaker = Speaker.DefaultSpeaker;
                    }
                    _transcription.EndUpdate();
                }
                SpeakersBox.UnselectAll();
            }

        }

        private void MenuItem_MergeSpeakers(object sender, RoutedEventArgs e)
        {
            var selectedSpeaker = ((SpeakerContainer)SpeakersBox.SelectedValue).Speaker;
            SpeakersManager sm2 = new SpeakersManager(selectedSpeaker, _transcription, _documentSpeakers, _localSpeakers)
            {
                MessageLabel = Properties.Strings.SpeakersManagerSpeakerMergeLabel,
                Message = selectedSpeaker.FullName,
                Editable = false,
                SelectMany = true
            };

            if (sm2.ShowDialog() == true)
            {
                var speakers = sm2.SpeakersBox.SelectedItems.Cast<SpeakerContainer>().Select(x => x.Speaker).ToList();
                speakers.Remove(selectedSpeaker);

                //merge
                selectedSpeaker.Merges.AddRange(speakers.Select(s => new DBMerge(s.DBID, s.DataBaseType)));
                selectedSpeaker.Merges.AddRange(speakers.SelectMany(s => s.Merges));

                if (speakers.Count == 0)
                    return;

                if (MessageBox.Show(string.Format(Properties.Strings.SpeakersManagerSpeakerMergeDialogQuestionFormat, string.Join("\", \"", speakers.Select(s => s.FullName)), selectedSpeaker.FullName), Properties.Strings.SpeakersManagerSpeakerMergeDialogCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (SpeakerProvider.DeferRefresh())
                    {
                        foreach (var s in speakers)
                        {
                            if (_documentSpeakers != null)
                                _documentSpeakers.RemoveSpeaker(s);
                            if (_localSpeakers != null)
                                _localSpeakers.RemoveSpeaker(s);


                            SpeakerProvider.DeleteSpeaker(s);
                        }
                    }

                    _transcription.BeginUpdate();
                    foreach (TranscriptionParagraph tp in _transcription.EnumerateParagraphs())
                    {
                        if (speakers.Contains(tp.Speaker))
                            tp.Speaker = selectedSpeaker;
                    }
                    _transcription.EndUpdate();

                    SpeakerProvider.Refresh();
                    SpeakersBox.UnselectAll();
                }
            }

        }


        Speaker _newSpeaker = null;


        public bool SpeakersCreated
        {
            get
            {
                return NewSpeaker == null;
            }

        }

        public Speaker NewSpeaker
        {
            get { return _newSpeaker; }
            set
            {
                _newSpeaker = value;
                OnPropertyChanged();
                OnPropertyChanged("SpeakersCreated");
            }
        }

        private void MenuItem_NewSpeaker(object sender, RoutedEventArgs e)
        {

            Speaker sp;

            if (_speakerProvider.IsOnline)
                sp = new ApiSynchronizedSpeaker("-----", "-----", Speaker.Sexes.X)
                {
                    IsSaved = false,
                    DataBaseType = DBType.Api,
                };
            else
                sp = new Speaker("-----", "-----", Speaker.Sexes.X, null) { DataBaseType = DBType.User }; ;

            NewSpeaker = sp;

            SpeakerProvider.AddTempSpeaker(sp);

            var ss = SpeakerProvider.GetContainerForSpeaker(sp);

            SpeakersBox.SelectedValue = ss;
            ss.New = true;
            ss.Changed = true;
            SpeakersBox.ScrollIntoView(SpeakersBox.SelectedItem);
        }


        private async void SpeakerDetails_RevertSpeakerRequest(SpeakerContainer spk)
        {
            if (spk.IsOnline)
            {
                using (var wc = new WaitCursor())
                {
                    var s = await _transcription.Api.GetSpeaker(spk.Speaker.DBID);
                    Speaker.MergeFrom(spk.Speaker, s);
                    spk.ReloadSpeaker();
                }
            }
            else
            {
                spk.DiscardChanges();
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

            if (e.RemovedItems.Count > 0)
            {
                var changedSpeakers = e.RemovedItems.Cast<SpeakerContainer>().Where(c => c.Changed).ToArray();

                if (changedSpeakers.Length > 0)
                {
                    using (new WaitCursor())
                    {
                        foreach (var sc in changedSpeakers)
                        {
                            if (SpeakersBox.Items.Contains(sc) && CheckChanges(sc))
                            { //revert changes
                                if (SpeakersBox.SelectionMode == SelectionMode.Multiple)
                                {

                                    foreach (var r in e.AddedItems.Cast<SpeakerContainer>())
                                        SpeakersBox.SelectedItems.Remove(r);

                                    foreach (var a in e.AddedItems.Cast<SpeakerContainer>())
                                        SpeakersBox.SelectedItems.Add(a);
                                }
                                else
                                {
                                    SpeakersBox.SelectedItem = changedSpeakers.First();
                                }
                            }

                        }
                    }
                }
            }

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

        private bool CheckChanges(SpeakerContainer sc)
        {
            if (!sc.Changed)
                return true;

            var result = MessageBox.Show(string.Format(Properties.Strings.SpeakersManagerSpeakerApplyChangesDialogFormat, sc.FullName), Properties.Strings.SpeakersManagerSpeakerApplyChangesDialogQuestion, MessageBoxButton.YesNo, MessageBoxImage.Question);

            ApiSynchronizedSpeaker ss = sc.Speaker as ApiSynchronizedSpeaker;

            if (result == MessageBoxResult.Yes)
            {
                bool saved = AsyncHelpers.RunSync(() => TrySaveSpeaker(sc));
                if (!saved)
                {
                    return false;
                }
            }
            else
            {

                if (NewSpeaker == sc.Speaker)
                {
                    NewSpeaker = null;
                    SpeakerProvider.DeleteSpeaker(ss);
                }
                sc.ReloadSpeaker();
                return false;
            }

            return true;
        }


        private void SpeakerSmall_speakermodified()
        {
            SpeakerChanged = true;
        }

        bool preventDoublecheck = false;
        private void manager_Closing(object sender, CancelEventArgs e)
        {
            if (SelectedSpeakerContainer != null)
            {
                if (!CheckChanges(SelectedSpeakerContainer))
                {
                    e.Cancel = true;
                    return;
                }
            }

            //   SpeakersBox.SelectedItem = null;

            if (preventDoublecheck)
                return;
            preventDoublecheck = true;

            if (!AsyncHelpers.RunSync(() => this.SpeakerProvider.CloseConnection()))
            {
                e.Cancel = true;
            }

        }




        private async void SpeakerDetails_SaveSpeakerClick(SpeakerContainer spk)
        {
            using (new WaitCursor())
                await TrySaveSpeaker(spk);

        }

        private async Task<bool> TrySaveSpeaker(SpeakerContainer spk)
        {
            bool retval = false;
            if (NewSpeaker == spk.Speaker)
            {
                if (await TestName(spk))
                {
                    using (SpeakerProvider.DeferRefresh())
                    {
                        spk.ApplyChanges();
                        SpeakerProvider.DeleteSpeaker(spk.Speaker);
                        if (spk.IsOnline)
                            SpeakerProvider.AddOnlineSpeaker((ApiSynchronizedSpeaker)spk.Speaker);
                        else
                            SpeakerProvider.AddLocalSpeaker(spk.Speaker);

                        NewSpeaker = null;
                        retval = true;
                    }
                }
                else
                {
                    return false; //do not apply or save - name is invalid
                }
            }

            spk.ApplyChanges();
            if (spk.IsOnline)
            {

                using (var wc = new WaitCursor())
                {
                    ApiSynchronizedSpeaker ss = spk.Speaker as ApiSynchronizedSpeaker;
                    if (ss == null)
                        return false;
                    if (ss.IsSaved)
                        return await _transcription.Api.UpdateSpeaker(ss);
                    else
                        return await _transcription.Api.AddSpeaker(ss);
                }
            }

            return retval;
        }

        private async Task<bool> TestName(SpeakerContainer spk)
        {
            if (string.IsNullOrWhiteSpace(spk.FullName.Replace("-", "")))
            {
                MessageBox.Show(string.Format(Properties.Strings.SpeakersManagerSpeakerNameConflictForbiddenQuestionFormat, spk.FullName), Properties.Strings.SpeakersManagerSpeakerNameConflictCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else
            {
                var similar = await SpeakerProvider.FindSimilar(spk);
                if (similar.Length > 0)
                {
                    if (MessageBox.Show(string.Format(Properties.Strings.SpeakersManagerSpeakerNameConflictQuestionFormat, string.Join(",", similar.Take((similar.Length > 3) ? 3 : similar.Length).Select(s => s.FullName))),
                        Properties.Strings.SpeakersManagerSpeakerNameConflictCaption, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
                    {
                        return false;
                    }
                }

            }

            return true;
        }

        private void ReplaceSpeakerInTranscription(Speaker toReplace, Speaker replacement)
        {
            if (MessageBox.Show(string.Format(Properties.Strings.SpeakersManagerSpeakerReplaceDialogQuestionFormat, toReplace.FullName, replacement.FullName), Properties.Strings.SpeakersManagerSpeakerReplaceDialogCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _transcription.BeginUpdate();
                foreach (TranscriptionParagraph tp in _transcription.EnumerateParagraphs())
                {
                    if (tp.Speaker == toReplace)
                        tp.Speaker = replacement;
                }
                _transcription.EndUpdate();
            }
        }

        private void ButtonOKAll_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSpeakerContainer != null)
            {
                if (!CheckChanges(SelectedSpeakerContainer))
                    return;
            }

            ReplaceSpeakerInTranscription(_originalSpeaker, ((SpeakerContainer)SpeakersBox.SelectedValue).Speaker);
            preventDoublecheck = false;
            this.DialogResult = false; //Changes already applied
            this.Close();
        }
    }


}
