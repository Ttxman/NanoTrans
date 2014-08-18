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
using System.IO;
using System.Media;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using dbg = System.Diagnostics.Debug;
using System.Security.Permissions;
using System.Security;
using System.Xml.Linq;
using System.Reflection;
using Microsoft.Win32;
using NanoTrans.Audio;
using NanoTrans.Core;
using System.Windows.Controls.Primitives;
using WPFLocalizeExtension.Engine;
using System.Threading.Tasks;
using NanoTrans.OnlineAPI;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using NanoTrans.Properties;
using System.Collections.ObjectModel;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window, System.ComponentModel.INotifyPropertyChanged
    {

        private void OnPropertyChanged([CallerMemberName]string caller = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #region transcription walking
        ObservableCollection<FileInfo> _TranscriptionList = new ObservableCollection<FileInfo>();

        public ObservableCollection<FileInfo> TranscriptionList
        {
            get { return _TranscriptionList; }
            private set
            {
                _TranscriptionList = value;
                OnPropertyChanged();
            }
        }

        int _transcriptionIndex = 0;

        public int TranscriptionIndex
        {
            get { return _transcriptionIndex; }
            set
            {
                _transcriptionIndex = value;
                OnPropertyChanged();
                OnPropertyChanged("IsPreviousTranscriptionAvailable");
                OnPropertyChanged("IsNextTranscriptionAvailable");
                OnPropertyChanged("TranscriptionName");
                OnPropertyChanged("PreviousTranscriptionName");
                OnPropertyChanged("NextTranscriptionName");
            }
        }

        public bool IsPreviousTranscriptionAvailable
        {
            get { return _transcriptionIndex > 0; }
        }

        public bool IsNextTranscriptionAvailable
        {
            get { return _transcriptionIndex < _TranscriptionList.Count - 1; }
        }

        public string TranscriptionName
        {
            get { return _TranscriptionList[_transcriptionIndex].Name; }
        }

        public string PreviousTranscriptionName
        {
            get { return (IsPreviousTranscriptionAvailable) ? _TranscriptionList[_transcriptionIndex - 1].Name : ""; }
        }
        public string NextTranscriptionName
        {
            get { return (IsNextTranscriptionAvailable) ? _TranscriptionList[_transcriptionIndex + 1].Name : ""; }
        }

        #endregion
        //timer pro posuvnik videa....
        private DispatcherTimer caretRefreshTimer = new DispatcherTimer();

        private WPFTranscription _transcription;
        public WPFTranscription Transcription
        {
            get { return _transcription; }
            set
            {
                if (_transcription != null)
                    _transcription.ContentChanged -= _transcription_ContentChanged;
                _transcription = value;
                if (_transcription != null)
                    _transcription.ContentChanged += _transcription_ContentChanged;
                OnPropertyChanged();

            }
        }

        void _transcription_ContentChanged(object sender, TranscriptionElement.TranscriptionElementChangedEventArgs e)
        {
            if(e.ActionsTaken.Any(a=>!(a.ChangedElement is TranscriptionPhrase)))
                waveform1.InvalidateSpeakers();
        }


        private AdvancedSpeakerCollection SpeakersDatabase;

        WinHelp helpWindow;                                   //okno s napovedou

        private WavReader _WavReader = null;



        bool videoAvailable = false;

        /// <summary>
        /// true - play only selected audio, then stop,  dont stop at selection end
        /// </summary>
        bool PlayingSelection = false;  //prehrava jen vybranou prepsanou sekci, pokud je specifikovan zacatek a konec

        short caretRefreshTimerCounter = 0;

        private DXWavePlayer MWP = null;

        private int _playbackBufferIndex = 0;
        private int PlaybackBufferIndex
        {
            get { return _playbackBufferIndex; }
            set
            {
                _playbackBufferIndex = value;

            }
        }

        private bool _playing = false;
        private bool Playing
        {
            get { return _playing; }
            set
            {

                _playing = value;
                waveform1.Playing = value;
                PlaybackBufferIndex = (int)waveform1.CaretPosition.TotalMilliseconds;
                oldms = TimeSpan.Zero;

                if (value)
                {
                    if (MWP == null)
                    {
                        InitializeAudioPlayer();
                    }
                }
                else
                {
                    if (MWP != null)
                        MWP.Pause();
                }


                if (videoAvailable)
                {
                    if (value)
                        meVideo.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => meVideo.Play()));
                    else
                        meVideo.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => meVideo.Pause()));
                }
            }
        }

        public Window1()
        {
            this.DataContext = this;
            InitializeComponent();

            LoadGlobalSetup();

            Settings.Default.FeatureEnabler.FeaturesChanged += FeatureEnabler_FeaturesChanged;
#if MINIMAL
            var fe = NanoTrans.Properties.Settings.Default.FeatureEnabler;
            fe.AudioManipulation = false;
            fe.DbMerging = false;
            fe.ChaptersAndSections = false;
            fe.LocalEdit = false;
            fe.LocalSpeakers = false;
            fe.PhoneticEditation = false;
            fe.QuickExport = false;
            fe.QuickNavigation = false;
            fe.VideoFrame = false;
            fe.NonSpeechEvents = false;
            fe.Spellchecking = false;
            fe.Export = false;
            fe.SpeakerAttributes = false;

#endif

            SpeakersDatabase = new AdvancedSpeakerCollection();

            _WavReader = new WavReader();
            _WavReader.HaveData += oWav_HaveData;
            _WavReader.HaveFileNumber += oWav_ReportConversionProgress;
            _WavReader.TemporaryWavesDone += new EventHandler(oWav_TemporaryWavesDone);
        }

        void FeatureEnabler_FeaturesChanged(object sender, EventArgs e)
        {
            if (toolbarAdditional.Items.Cast<Control>().All(c => c.Visibility == System.Windows.Visibility.Collapsed))
                toolbarAdditional.Visibility = System.Windows.Visibility.Collapsed;
            else
                toolbarAdditional.Visibility = System.Windows.Visibility.Visible;
        }

        private void LoadSpeakersDatabase()
        {
            if (Settings.Default.FeatureEnabler.LocalSpeakers)
            {
                if (!File.Exists(Settings.Default.SpeakersDatabasePath))
                    MessageBox.Show(Properties.Strings.MessageBoxLocalSpeakersDatabaseUnreachableLoad, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);

                if (Settings.Default.SpeakersDatabasePath.Contains(FilePaths.ProgramDirectory))
                {
                    if (!FilePaths.WriteToAppData)
                    {
                        SpeakerCollection.Deserialize(Settings.Default.SpeakersDatabasePath, SpeakersDatabase);
                    }
                    else
                    {
                        string fname2 = System.IO.Path.Combine(FilePaths.AppDataDirectory, Settings.Default.SpeakersDatabasePath.Substring(FilePaths.ProgramDirectory.Length));
                        if (File.Exists(fname2))
                        {
                            SpeakerCollection.Deserialize(fname2, SpeakersDatabase);
                        }
                        else if (File.Exists(Settings.Default.SpeakersDatabasePath))
                        {
                            SpeakerCollection.Deserialize(Settings.Default.SpeakersDatabasePath, SpeakersDatabase);
                        }
                    }
                }
                else
                {
                    SpeakerCollection.Deserialize(FilePaths.EnsureDirectoryExists(Settings.Default.SpeakersDatabasePath), SpeakersDatabase);
                }
            }
            else
            {
                SpeakersDatabase = new AdvancedSpeakerCollection();
            }
        }

        private void LoadGlobalSetup()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.Locale))
            {
                LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo(Settings.Default.Locale);
            }

            //set window position, size and state
            if (Settings.Default.WindowsPosition != null)
            {
                if (Settings.Default.WindowsPosition.X >= 0 && Settings.Default.WindowsPosition.Y >= 0)
                {
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                    this.Left = Settings.Default.WindowsPosition.X;
                    this.Top = Settings.Default.WindowsPosition.Y;
                }
            }
            if (Settings.Default.WindowSize != null)
            {
                if (Settings.Default.WindowSize.Width >= 50 && Settings.Default.WindowSize.Height >= 50)
                {
                    this.Width = Settings.Default.WindowSize.Width;
                    this.Height = Settings.Default.WindowSize.Height;
                }
            }

            this.WindowState = Settings.Default.WindowState;

        }

        private void menuItemWave1_SetStartToCursor_Click(object sender, RoutedEventArgs e)
        {

            TranscriptionElement te = VirtualizingListBox.ActiveElement.ValueElement;

            TranscriptionElement pre = te.PreviousSibling();



            TimeSpan delta = waveform1.CaretPosition - te.Begin;


            while (te != null)
            {
                te.Begin += delta;
                te.End += delta;

                te = te.Next();
            }
            te = VirtualizingListBox.ActiveElement.ValueElement;
            if (pre != null && pre.End == te.Begin - delta && pre.IsParagraph && te.IsParagraph)
                pre.End += delta;

            waveform1.InvalidateSpeakers();
        }

        private void menuItemX9_substract50msClick(object sender, RoutedEventArgs e)
        {
            TranscriptionElement te = VirtualizingListBox.ActiveElement.ValueElement;
            TimeSpan ms50 = TimeSpan.Zero - TimeSpan.FromMilliseconds(50);
            while (te != null)
            {
                te.Begin += ms50;
                te.End += ms50;

                te = te.Next();
            }

            waveform1.InvalidateSpeakers();

        }

        private void menuItemX8_add50msClick(object sender, RoutedEventArgs e)
        {
            TranscriptionElement te = VirtualizingListBox.ActiveElement.ValueElement;
            TimeSpan ms50 = TimeSpan.FromMilliseconds(50);
            while (te != null)
            {
                te.Begin += ms50;
                te.End += ms50;

                te = te.Next();
            }
            waveform1.InvalidateSpeakers();
        }

        short[] ReadAudioDataFromWaveform(out int beginMS)
        {
            beginMS = -1;
            if (MWP != null)
            {
                if (_playing && _WavReader.Loaded)
                {
                    TimeSpan limitEndMS = new TimeSpan(-1);
                    if (PlayingSelection)
                    {
                        limitEndMS = waveform1.SelectionEnd;
                    }


                    if (MWP.PlayPosition >= _WavReader.FileLength)
                    {
                        if (!PlayingSelection)
                        {
                            Playing = false;
                            SetCaretPosition(_WavReader.FileLength);
                            PlaybackBufferIndex = 0;
                        }
                        else
                        {
                            PlaybackBufferIndex = (int)waveform1.SelectionBegin.TotalMilliseconds;
                        }
                    }

                    short[] bfr = waveform1.GetAudioData(TimeSpan.FromMilliseconds(PlaybackBufferIndex), TimeSpan.FromMilliseconds(150), limitEndMS);
                    beginMS = PlaybackBufferIndex;
                    PlaybackBufferIndex += 150;

                    return bfr;
                }
            }
            return new short[0];
        }

        private void oWav_ReportConversionProgress(object sender, EventArgs e)
        {
            AudioBufferEventArgs2 e2 = (AudioBufferEventArgs2)e;
            this.Dispatcher.InvokeAsync(() => ShowConversionProgress(e2));
        }

        private void oWav_TemporaryWavesDone(object sender, EventArgs e)
        {
            waveform1.Dispatcher.Invoke(new Action(delegate()
            {
                waveform1.AutomaticProgressHighlight = true;
            }));

        }

        private void ShowConversionProgress(AudioBufferEventArgs2 e)
        {
            pbStatusbarBrogress.Value = e.FileNumber;
            waveform1.ProgressHighlightBegin = TimeSpan.Zero;
            waveform1.ProgressHighlightEnd = TimeSpan.FromMilliseconds(e.ProcessedMS);

            var databegin = TimeSpan.FromMilliseconds(e.FileNumber * Const.TEMPORARY_AUDIO_FILE_LENGTH_MS);
            var dataend = TimeSpan.FromMilliseconds(e.ProcessedMS);

            var half = TimeSpan.FromMilliseconds(Const.DISPLAY_BUFFER_LENGTH_MS / 2);

            if (databegin <= waveform1.CaretPosition + half && dataend >= waveform1.CaretPosition - half) //load occured in displayed chunk -> redraw
                waveform1.AudioBufferCheck(waveform1.CaretPosition, true);



            if (e.ProcessedMS >= _WavReader.FileLengthMS)//done
            {
                ShowMessageInStatusbar(Properties.Strings.mainWindowStatusbarStatusTextConversionDone);
                pbStatusbarBrogress.Visibility = Visibility.Hidden;
                mainWindowStatusbarAudioConversionHeader.Visibility = Visibility.Hidden;
            }

            CommandManager.InvalidateRequerySuggested();
        }


        /// <summary>
        /// Callback from the audio-data processing thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void oWav_HaveData(object sender, EventArgs e)
        {

            AudioBufferEventArgs me = (AudioBufferEventArgs)e;
            if (me.BufferID == Const.ID_BUFFER_WAVEFORMVISIBLE)
            {
                waveform1.SetAudioData(me.data, TimeSpan.FromMilliseconds(me.StartMS), TimeSpan.FromMilliseconds(me.EndMS));
                if (me.StartMS == 0)
                {
                    if (!caretRefreshTimer.IsEnabled) InitializeTimer();
                }
            }
            else if (me.BufferID == Const.ID_BUFFER_TRANSCRIBED_ELEMENT_PHONETIC)
            {
                throw new NotImplementedException();
            }

            waveform1.Invalidate();

        }


        void gridX_MouseUp(object sender, MouseButtonEventArgs e)
        {

            bool cc = ((TextBox)((Grid)sender).Children[0]).Focus();
        }

        #region RclickMEnu

        //--------------------------------menu videa---------------------------------------------
        void menuItemVideoTakePicture_Click(object sender, RoutedEventArgs e)
        {
            CommandTakeSpeakerSnapshotFromVideo.Execute(null, this);
        }


        //--------------------------------obsluha context menu pro gridy v listboxu--------------------------------------------------
        void menuItemX_SetSpeaker_Click(object sender, RoutedEventArgs e)
        {
            CommandAssignSpeaker.Execute(null, null);
        }

        void menuItemX2_NewSection_Click(object sender, RoutedEventArgs e)
        {
            CommandNewSection.Execute(null, null);
        }

        void menuItemX2b_newSectionAtPosition_Click(object sender, RoutedEventArgs e)
        {
            CommandInsertNewSection.Execute(null, null);
        }

        void menuItemX3_NewChapter_Click(object sender, RoutedEventArgs e)
        {
            CommandNewChapter.Execute(null, null);
        }

        void menuItemX4_DeleteElement_Click(object sender, RoutedEventArgs e)
        {
            CommandDeleteElement.Execute(null, null);
        }

        static Encoding win1250 = Encoding.GetEncoding("windows-1250");

        void menuItemX7_ExportElement_Click(object sender, RoutedEventArgs e)
        {
            CommandExportElement.Execute(null, null);

        }
        #endregion

        public async Task<bool> NewTranscription()
        {
            if (!await TrySaveUnsavedChanges())
                return false;

            var source = new WPFTranscription();
            source.BeginUpdate(false);
            var c = new TranscriptionChapter(Properties.Strings.DefaultChapterText);
            var s = new TranscriptionSection(Properties.Strings.DefaultSectionText);
            var p = new TranscriptionParagraph();
            p.Add(new TranscriptionPhrase());
            c.Add(s);
            s.Add(p);
            source.Add(c);
            Transcription = source;
            SynchronizeSpeakers();
            VirtualizingListBox.ActiveTransctiption = p;

            TranscriptionList.Clear();
            source.ClearUndo();
            source.EndUpdate();

            Transcription.Saved = true;
            return true;
        }




        public async Task OpenTranscription(bool useOpenDialog, string fileName, bool listing = false)
        {
            try
            {
                if (!await TrySaveUnsavedChanges())
                    return;//cancel


                if (Transcription == null)
                    Transcription = new WPFTranscription();

                bool loadedsucessfuly = false;

                if (listing)
                {
                    loadedsucessfuly = TryLoadTranscription(fileName, listing);
                }
                else if (useOpenDialog)
                {
                    Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

                    fileDialog.Title = Properties.Strings.FileDialogLoadTranscriptionTitle;
                    fileDialog.Filter = Properties.Strings.FileDialogLoadTranscriptionFilter;
                    fileDialog.RestoreDirectory = true;

                    if (fileDialog.ShowDialog() == true)
                    {
                        loadedsucessfuly = TryLoadTranscription(fileDialog.FileName);
                    }
                    else
                    {
                        return; //cancel loading
                    }
                }
                else
                {
                    loadedsucessfuly = TryLoadTranscription(fileName);
                }

                waveform1.CaretPosition = TimeSpan.Zero;

                if (!loadedsucessfuly)
                {
                    MessageBox.Show(Properties.Strings.MessageBoxCannotLoadTranscription, Properties.Strings.MessageBoxCannotLoadTranscription, MessageBoxButton.OK, MessageBoxImage.Information);
                    await NewTranscription();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Strings.MessageBoxOpenTranscriptionError + ex.Message + ":" + fileName, Properties.Strings.MessageBoxErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TryLoadTranscription(string fileName, bool listing = false)
        {
            fileName = System.IO.Path.GetFullPath(fileName);
            TranscriptionIsLoading = true;
            var ext = System.IO.Path.GetExtension(fileName);
            if (ext == ".tlst") //list of transcriptions
            {
                TranscriptionList = new ObservableCollection<FileInfo>(File.ReadAllLines(fileName).Select(l => new FileInfo(l)));
                TranscriptionIndex = 0;
                fileName = TranscriptionList[TranscriptionIndex].FullName;
            }
            else if (!listing)
            {
                var dir = new DirectoryInfo(System.IO.Path.GetDirectoryName(fileName));
                TranscriptionList = new ObservableCollection<FileInfo>(dir.GetFiles("*" + ext));
                TranscriptionIndex = TranscriptionList.ToList().FindIndex(f => f.FullName == fileName);
            }
            else if (listing) //moving through list
            {
                TranscriptionIndex = TranscriptionList.ToList().FindIndex(f => f.FullName == fileName);
            }
            else
                return false;

            var trans = WPFTranscription.Deserialize(fileName);

            if (trans != null)
            {

                if (trans.IsOnline)
                {
                    _api = new SpeakersApi(trans.OnlineInfo.OriginalURL.ToString(), this);
                    _api.Trans = trans;
                    _api.Info = trans.OnlineInfo;
                    LoadOnlineSetting();//contains LoadTranscription(....);
                }
                else
                {
                    LoadTranscription(trans);
                }

                TranscriptionIsLoading = false;

                return true;
            }

            TranscriptionIsLoading = false;
            return false;
        }
        SpeakersApi _api = null;

        private void LoadOnlineSource(string path)
        {
            Settings.Default.FeatureEnabler.DbMerging = false;
            Settings.Default.FeatureEnabler.LocalEdit = false;
            Settings.Default.FeatureEnabler.PhoneticEditation = false;
            Settings.Default.FeatureEnabler.QuickExport = false;
            Settings.Default.FeatureEnabler.QuickNavigation = false;
            Settings.Default.FeatureEnabler.VideoFrame = false;
            Settings.Default.FeatureEnabler.LocalSpeakers = false;

            _api = new SpeakersApi(path, this);
            if (_api.TryLogin(this) == true)
            {
                LoadOnlineSetting();
            }
            else
                Close();
        }

        private void LoadOnlineSetting()
        {
            LoadTranscription(_api.Trans);
            Transcription.FileName = _api.Info.TrsxUploadURL.ToString();
            Transcription.OnlineInfo = _api.Info;

        }

        private void LoadTranscription(WPFTranscription trans)
        {
            Transcription = trans;
            Transcription.Saved = true;

            TryLoadAudioFile();
            TryLoadVideoFile();
            InitWindowAfterTranscriptionLoad();
        }

        private void InitWindowAfterTranscriptionLoad()
        {
            this.Title = Const.APP_NAME + " [" + Transcription.FileName + "]";
            VirtualizingListBox.ActiveTransctiption = Transcription.First(e => e.IsParagraph) ?? Transcription.First();
            SynchronizeSpeakers();
        }

        private void TryLoadVideoFile()
        {
            if (Transcription.VideoFileName != null && Transcription.FileName != null)
            {
                FileInfo fi = new FileInfo(Transcription.FileName);
                string pVideoFile = fi.Directory.FullName + "\\" + Transcription.VideoFileName;
                FileInfo fi2 = new FileInfo(pVideoFile);
                if (fi2.Exists && (meVideo.Source == null || meVideo.Source.AbsolutePath.ToUpper() != pVideoFile.ToUpper()))
                {
                    LoadVideo(pVideoFile);
                }
            }
        }

        private async void TryLoadAudioFile()
        {
            if (Transcription.IsOnline)
            {
                await LoadAudioOnline();
            }
            else if (!string.IsNullOrEmpty(Transcription.MediaURI) && Transcription.FileName != null)
            {
                FileInfo fiA = new FileInfo(Transcription.MediaURI);
                string pAudioFile = fiA.FullName;
                if (!fiA.Exists && string.IsNullOrEmpty(System.IO.Path.GetPathRoot(Transcription.MediaURI))) //not exists and relative path
                {

                    FileInfo fi = new FileInfo(Transcription.FileName);
                    pAudioFile = fi.Directory.FullName + "\\" + Transcription.MediaURI;
                    fiA = new FileInfo(pAudioFile);
                }

                if (fiA.Exists)
                    LoadAudio(pAudioFile);
            }
        }

        /// <summary>
        /// attempt to save changes with dialog
        /// </summary>
        /// <returns>true when save was sucessful; false when user cancels or on error when saving</returns>
        private async Task<bool> TrySaveUnsavedChanges()
        {
            if (Transcription == null || Transcription.Saved || (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                return true;

            MessageBoxResult mbr = MessageBox.Show(Properties.Strings.MessageBoxSaveBeforeClosing, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (mbr == MessageBoxResult.Cancel || mbr == MessageBoxResult.None) //cancel or dialog close .. saving not occured
                return false;

            if (mbr == MessageBoxResult.Yes)
                if (!await SaveTranscription(string.IsNullOrWhiteSpace(Transcription.FileName), Transcription.FileName))
                    return false;//error during save

            return true;
        }

        private void SynchronizeSpeakers()
        {

            //unpin data from last document
            foreach (var item in SpeakersDatabase)
                item.PinnedToDocument = false;

            var toremove = new List<Speaker>();
            var toadd = new List<Speaker>();

            foreach (Speaker i in Transcription.Speakers)
            {
                Speaker ss = SpeakersDatabase.SynchronizeSpeaker(i);
                if (ss != null)
                {
                    if (i.PinnedToDocument)//pin to this document
                        ss.PinnedToDocument = true;


                    foreach (var p in Transcription.EnumerateParagraphs().Where(p => p.Speaker == i))
                        p.Speaker = ss;

                    toremove.Add(i);
                    if (!Transcription.Speakers.Contains(ss))
                        toadd.Add(ss);
                }
            }

            foreach (var s in toremove)
                Transcription.Speakers.Remove(s);

            foreach (var item in toadd)
                Transcription.Speakers.Add(item);
        }

        public async Task<bool> SaveTranscription(bool useSaveDialog, string jmenoSouboru)
        {
            try
            {

                string savePath = Transcription.FileName;
                if (useSaveDialog)
                {
                    Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();

                    fileDialog.Title = Properties.Strings.FileDialogSaveTranscriptionTitle;
                    fileDialog.Filter = Properties.Strings.FileDialogSaveTranscriptionFilter;
                    fileDialog.FilterIndex = 1;
                    fileDialog.OverwritePrompt = true;
                    fileDialog.RestoreDirectory = true;
                    if (fileDialog.ShowDialog() == true)
                    {
                        savePath = fileDialog.FileName;
                        Transcription.Saved = true;
                    }
                    else
                        return false;
                }

                if (Transcription.IsOnline && !useSaveDialog)
                {
                    using (var wc = new WaitCursor())
                    {
                        var onl = await _api.UploadTranscription(Transcription);
                        Transcription.Saved = onl;
                        return onl;

                    }
                }
                else if (Transcription.Serialize(savePath, Settings.Default.SaveWholeSpeaker))
                {
                    this.Title = Const.APP_NAME + " [" + Transcription.FileName + "]";
                    Transcription.Saved = true;
                    return true;
                }
                return false;

            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Strings.MessageBoxSaveTranscriptionError + ex.Message, Properties.Strings.MessageBoxErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }




        TimeSpan oldms = TimeSpan.Zero;
        bool _setCaret = false;
        public void SetCaretPosition(TimeSpan position)
        {
            if (!_setCaret)
            {
                _setCaret = true;
                if (position < TimeSpan.Zero) return;

                if (waveform1.CaretPosition != position)
                    waveform1.CaretPosition = position;

                if (!Playing)
                    oldms = TimeSpan.Zero;

                if (!Playing && videoAvailable && Math.Abs(meVideo.Position.TotalMilliseconds) > 200)
                {
                    meVideo.Position = waveform1.CaretPosition;
                }
                _setCaret = false;
            }
        }

        private void SelectTextBetweenTimeOffsets(TimeSpan cursorPosition)
        {
            VirtualizingListBox.HiglightedPostion = cursorPosition;
            phoneticTranscription.HiglightedPostion = cursorPosition;
        }

        public void InitializeTimer()
        {
            caretRefreshTimer.Interval = new TimeSpan(0, 0, 0, 0, Const.WAVEFORM_CARET_REFRESH_MS);
            caretRefreshTimer.IsEnabled = true;
            caretRefreshTimer.Tick += new EventHandler(OnTimer);
        }

        void OnTimer(Object source, EventArgs e)
        {
            long delta = (long)waveform1.WaveLength.TotalMilliseconds; //waveform length in ms

            TimeSpan playpos = waveform1.CaretPosition;
            if (_playing && MWP!=null)
            {
                playpos = MWP.PlayPosition;
                if (PlayingSelection && playpos < waveform1.SelectionBegin)
                {
                    playpos = waveform1.SelectionBegin;
                }

                waveform1.CaretPosition = playpos;
            }

            caretRefreshTimerCounter++;
            if (caretRefreshTimerCounter > 0) //kazdy n ty tik dojde ke zmene pozice ctverce
            {
                if (caretRefreshTimerCounter > 2)
                {
                    caretRefreshTimerCounter = 0;
                    if (!_playing && videoAvailable)
                    {
                        meVideo.Pause();
                    }
                }



                if (PlayingSelection && playpos >= waveform1.SelectionEnd && waveform1.SelectionEnd >= TimeSpan.Zero)
                {

                    Playing = false;

                    oldms = TimeSpan.Zero;
                    SetCaretPosition(waveform1.SelectionBegin);

                    playpos = waveform1.CaretPosition;

                    Playing = true;
                    if (MWP != null)
                        MWP.Play(MWP.PlaySpeed);

                }
                else
                {
                    SetCaretPosition(waveform1.CaretPosition);
                }

                if (Playing)
                    SelectTextBetweenTimeOffsets(playpos);
            }
        }


        /// <summary>
        /// spusti proceduru pro nacitani videa, pokud je zadana cesta, pokusi se nacist dany soubor
        /// </summary>
        private async Task LoadAudioOnline()
        {
            try
            {
                ShowMessageInStatusbar(Properties.Strings.mainWindowStatusbarStatusMediaDownloading);
                pbStatusbarBrogress.Visibility = System.Windows.Visibility.Visible;
                pbStatusbarBrogress.IsIndeterminate = true;
                var filename = System.IO.Path.Combine(FilePaths.TempDirectory, System.IO.Path.GetFileName(Transcription.MediaURI));
                await _api.DownloadFile(Transcription.MediaURI, filename);
                LoadAudio(filename);
            }
            catch
            {
            }

        }

        /// <summary>
        /// Try to load audio file, if not found open openfile dialog
        /// </summary>
        /// <param name="aFileName"></param>
        /// <returns></returns>
        private bool LoadAudio(string aFileName, bool skipCheck = false)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
                openDialog.Title = Properties.Strings.LoadAudioTitle;
                openDialog.Filter = Properties.Strings.LoadAudioFilter;
                openDialog.FilterIndex = 1;

                bool pOtevrit = File.Exists(aFileName) || skipCheck;

                if (pOtevrit || openDialog.ShowDialog() == true)
                {
                    waveform1.AutomaticProgressHighlight = false;
                    if (!pOtevrit)
                    {
                        aFileName = openDialog.FileName;
                    }

                    FileInfo fi = new FileInfo(aFileName);
                    if (!Transcription.IsOnline)
                        Transcription.MediaURI = fi.FullName;
                    ////////////
                    TimeSpan fileLength = WavReader.ReturnAudioLength(aFileName);
                    if (fileLength > TimeSpan.Zero)
                    {
                        _WavReader.Stop();


                        Playing = false;

                        waveform1.CaretPosition = TimeSpan.Zero;

                        waveform1.DataRequestCallBack = _WavReader.LoadaudioDataBuffer;

                        pbStatusbarBrogress.Value = 0;
                        waveform1.AudioLength = fileLength;
                        pbStatusbarBrogress.Maximum = fileLength.TotalMinutes - 1;
                        pbStatusbarBrogress.Value = 0;

                        _WavReader.ConvertAudioFileToWave(aFileName); //conversion to wav chunks in separate thread 
                        ShowMessageInStatusbar(Properties.Strings.mainWindowStatusbarStatusTextConversionRunning);
                        pbStatusbarBrogress.Visibility = Visibility.Visible;
                        mainWindowStatusbarAudioConversionHeader.Visibility = Visibility.Visible;
                        /////////////
                    }
                    else
                    {
                        MessageBox.Show(Properties.Strings.MessageBoxAudioFormatError, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }


                    try
                    {

                        tbAudioFile.Text = new FileInfo(aFileName).Name;
                    }
                    catch
                    {
                        tbAudioFile.Text = openDialog.FileName;
                    }
                    finally
                    {
                        tbAudioFile.ToolTip = openDialog.FileName;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            LoadAudio(null);
        }

        /// <summary>
        /// spusti proceduru pro nacitani videa, pokud je zadana cesta, pokusi se nacist dany soubor
        /// </summary>
        /// <param name="aFileName"></param>
        /// <returns></returns>
        private bool LoadVideo(string aFileName)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
                openDialog.Title = Properties.Strings.LoadVideoTitle;
                openDialog.Filter = Properties.Strings.LoadVideoFilter;

                bool openExisting = File.Exists(aFileName);

                if (openExisting)
                    openDialog.FileName = aFileName;

                if (openExisting || openDialog.ShowDialog() == true)
                {
                    meVideo.Source = new Uri(openDialog.FileName);

                    meVideo.Play();

                    meVideo.Position = new TimeSpan(0, 0, 0, 0, PlaybackBufferIndex);
                    meVideo.IsMuted = true;
                    videoAvailable = true;


                    tbVideoFile.Text = new FileInfo(openDialog.FileName).Name;
                    Transcription.VideoFileName = tbVideoFile.Text;

                    infoPanels.SelectedIndex = 0;

                    if (!string.IsNullOrWhiteSpace(_WavReader.FilePath))
                    {
                        if (!openExisting && MessageBox.Show(Properties.Strings.MessageBoxUseVideoFileAsAudioSource, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            LoadAudio(openDialog.FileName);
                        }
                    }
                    else
                    {
                        LoadAudio(openDialog.FileName);
                    }

                }

                return true;

            }
            catch// (Exception ex)
            {
                return false;
            }

        }


        private void button6_Click(object sender, RoutedEventArgs e)
        {
            Mfile_open_video_Click(null, new RoutedEventArgs());
        }

        /// <summary>
        /// inicializuje MWP - prehravac audia, pokud neni null, zavola dispose a opet ho vytvori
        /// </summary>
        /// <returns></returns>
        private bool InitializeAudioPlayer()
        {
            try
            {
                if (MWP != null)
                {
                    MWP.Dispose();
                    MWP = null;
                }
                MWP = new DXWavePlayer(Settings.Default.OutputDeviceIndex, 4800, ReadAudioDataFromWaveform);
                return true;
            }
            catch
            {
                return false;
            }

        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            // mediaElement1.Stop();

            Playing = false;
            waveform1.CaretPosition = TimeSpan.Zero;
        }
        //menu----------------------------------------------------------------------

        #region menu file
        private void MFile_new_Click(object sender, RoutedEventArgs e)
        {
            CommandCreateNewTranscription.Execute(null, this);
        }

        private void MFile_open_Click(object sender, RoutedEventArgs e)
        {
            CommandOpenTranscription.Execute(null, this);
        }

        private void Mfile_open_video_Click(object sender, RoutedEventArgs e)
        {
            LoadVideo(null);
        }

        private void MFile_save_Click(object sender, RoutedEventArgs e)
        {
            CommandSaveTranscription.Execute(null, this);
        }

        private void MFile_save_as_Click(object sender, RoutedEventArgs e)
        {
            CommandSaveTranscriptionAs.Execute(null, this);
        }


        private void MFile_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MFile_open_audio_Click(object sender, RoutedEventArgs e)
        {
            button1_Click(null, new RoutedEventArgs());
        }
        #endregion

        #region menu Tools
        private void MTools_Set_Speaker_Click(object sender, RoutedEventArgs e)
        {
            CommandAssignSpeaker.Execute(null, null);
        }

        private void MTools_Settings_Click(object sender, RoutedEventArgs e)
        {
            var win = new WinSetup(SpeakersDatabase);
            win.Show();
            SaveGlobalSetup();
            InitializeAudioPlayer();
        }

        private void MHelp_Details_Click(object sender, RoutedEventArgs e)
        {
            CommandHelp.Execute(null, this);
        }

        private void MHelp_about_Click(object sender, RoutedEventArgs e)
        {
            CommandAbout.Execute(null, this);
        }

        #endregion

        //toolbar buttons
        //TODO: integrate into waveform
        private void Toolbar1Btn5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TB5.IsChecked = TB10.IsChecked = TB20.IsChecked = TB30.IsChecked = TB60.IsChecked = TB120.IsChecked = TB180.IsChecked = false;
                ToggleButton pButton = sender as ToggleButton;
                pButton.IsChecked = true;
                TimeSpan length = TimeSpan.FromMilliseconds(long.Parse(pButton.Tag.ToString()));
                TimeSpan begin = waveform1.WaveBegin;
                TimeSpan end = waveform1.WaveBegin + length;
                if (end < waveform1.WaveBegin)
                    begin = waveform1.CaretPosition - TimeSpan.FromTicks(length.Ticks / 2);

                if (begin < TimeSpan.Zero) begin = TimeSpan.Zero;
                end = begin + length;
                caretRefreshTimer.IsEnabled = false;
                waveform1.WaveBegin = begin;
                waveform1.WaveEnd = end;
                waveform1.WaveLength = length;
                caretRefreshTimer.IsEnabled = true;
            }
            catch
            {

            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Pedalthread != null)
                Pedalthread.Abort();

            var res = AsyncHelpers.RunSync(() => TrySaveUnsavedChanges());

            if (!res || !TrySaveSpeakersDatabase())
            {
                e.Cancel = true;
                return;
            }


            SaveGlobalSetup();

            foreach (Window win in App.Current.Windows)
            {
                if (win != this)
                    try { win.Close(); }
                    catch { };
            }

            if (MWP != null)
            {
                MWP.Dispose();
                MWP = null;
            }


            _WavReader.Stop();

            if (_api != null)
                _api.Cancel();

            FilePaths.DeleteTemp();
            Environment.Exit(0); //Force close application
        }

        private void SaveGlobalSetup()
        {
            Settings.Default.Save();
        }

        private bool TrySaveSpeakersDatabase()
        {
            if (SpeakersDatabase != null && Settings.Default.FeatureEnabler.LocalSpeakers)
            {

                try
                {
                    string fname = FilePaths.EnsureDirectoryExists(System.IO.Path.GetFullPath(Settings.Default.SpeakersDatabasePath));
                    if (fname.StartsWith(FilePaths.ProgramDirectory))//check access to program files.
                    {
                        if (!FilePaths.WriteToAppData)
                            SpeakersDatabase.Serialize(fname, true);
                        else
                            SpeakersDatabase.Serialize(FilePaths.AppDataDirectory + fname.Substring(FilePaths.ProgramDirectory.Length), true);
                    }
                    else// not in ProgramFiles - NanoTrans not installed, don't do anything
                    {
                        SpeakersDatabase.Serialize(Settings.Default.SpeakersDatabasePath, true);
                    }
                }
                catch
                {
                    if (MessageBox.Show(Properties.Strings.MessageBoxLocalSpeakersDatabaseUnreachableSave, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        private void btCloseVideo_Click(object sender, RoutedEventArgs e)
        {
            meVideo.Close();
            meVideo.Source = null;
            videoAvailable = false;
            // gListVideo.ColumnDefinitions[1].Width = new GridLength(0);

            Properties.Settings.Default.VideoPanelVisible = false;
        }



        #region menu uprava
        private void MEdit_new_chapter_Click(object sender, RoutedEventArgs e)
        {
            CommandNewChapter.Execute(null, null);
        }

        private void MEdit_New_Section_Click(object sender, RoutedEventArgs e)
        {
            CommandNewSection.Execute(null, null);
        }

        private void MEdit_Delete_Element_Click(object sender, RoutedEventArgs e)
        {
            CommandDeleteElement.Execute(null, null);
        }

        #endregion




        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSpeakersDatabase();

            foreach (var item in SpeakersDatabase)
                item.PinnedToDocument = false;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitCommands();
            LoadPlugins();
            if (Settings.Default.FeatureEnabler.Spellchecking)
            {
                //asynchronous spellcecking vocabluary load
                Thread t = new Thread(
                    delegate()
                    {
                        SpellChecker.LoadVocabulary();
                    }
                    ) { Name = "Spellchecking_Load" };
                t.Start();

            }

            //TODO: move to xaml?
            phoneticTranscription.Text = "";
            phoneticTranscription.buttonSpeaker.Visibility = Visibility.Collapsed;
            phoneticTranscription.checkBox1.Visibility = Visibility.Collapsed;
            phoneticTranscription.stackPanelAttributes.Visibility = Visibility.Collapsed;
            phoneticTranscription.textbegin.Visibility = Visibility.Collapsed;
            phoneticTranscription.textend.Visibility = Visibility.Collapsed;
            phoneticTranscription.DisableAutomaticElementVisibilityChanges = true;
            phoneticTranscription.EditPhonetics = true;
            phoneticTranscription.editor.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            phoneticTranscription.editor.Style = null;
            phoneticTranscription.editor.OverridesDefaultStyle = false;
            phoneticTranscription.editor.TextArea.TextView.LineTransformers.Remove(Element.DefaultSpellchecker);

            PedalsInit();


            string path = null;
            bool import = false;
            bool online = false;
            if (App.Startup_ARGS != null && App.Startup_ARGS.Length > 0)
            {
                if (App.Startup_ARGS[0] == "-i")
                {
                    import = true;
                    path = App.Startup_ARGS[1];
                }
                else if (App.Startup_ARGS[0] == "-urlapi")
                {
                    online = true;
                    path = App.Startup_ARGS[1];
                }
                else
                    path = App.Startup_ARGS[0];
            }

            if (path == null)
            {
                await NewTranscription();
            }
            else
            {
                if (online)
                {
                    if (path.StartsWith("trsx://"))
                        path = path.Substring(7);

                    if (path.StartsWith("trsx:"))
                        path = path.Substring(5);



                    LoadOnlineSource(path);
                }
                else if (import)
                {
                    await NewTranscription();
                    CommandImportFile.Execute(path, this);
                }
                else
                {
                    await OpenTranscription(false, path);
#if MINIMAL
                    Transcription.MediaURI = @"http://demo.ite.tul.cz/audio_cro/51b5752ca802563ad80020d1.mp4";
                    _api = new SpeakersApi2("https://demo.ite.tul.cz/speechlab/cro_1968_1988_trsx07/api/speakers?", this);
                    _api.Info = new OnlineTranscriptionInfo()
                    {
                        TrsxUploadURL = new Uri("https://demo.ite.tul.cz/speechlab/cro_1968_1988_trsx07/api/speakers"),
                        SpeakerAPI_URL = new Uri("https://demo.ite.tul.cz/speechlab/cro_1968_1988_trsx07/api/speakers"),

                    };                    

                    _api.LoadTranscription(Transcription);

                    await _api.UpdateTranscriptionSpeakers();
                    LoadOnlineSetting();


#endif
                }
            }

            VirtualizingListBox.RequestTimePosition += delegate(out TimeSpan value) { value = waveform1.CaretPosition; };
            VirtualizingListBox.RequestPlaying += delegate(out bool value) { value = Playing; };
            VirtualizingListBox.RequestPlayPause += delegate() { CommandPlayPause.Execute(null, null); };
        }




        private void menuFileExportClick(object sender, RoutedEventArgs e)
        {
            Plugin p = (Plugin)((MenuItem)sender).Tag;
            p.ExecuteExport(Transcription);
        }

        private void LoadSubtitlesData(WPFTranscription data)
        {
            if (data == null)
                return;
            Transcription = data;
            //load audio if possible
            if (!string.IsNullOrEmpty(Transcription.MediaURI))
            {
                FileInfo fiA = new FileInfo(Transcription.MediaURI);
                string pAudioFile = null;
                if (fiA.Exists)
                {
                    pAudioFile = fiA.FullName;
                }
                else if (System.IO.Path.IsPathRooted(Transcription.MediaURI))
                {
                    tbAudioFile.Text = Transcription.MediaURI;
                }
                else
                {
                    FileInfo fi = new FileInfo(Transcription.FileName);
                    pAudioFile = fi.Directory.FullName + "\\" + Transcription.MediaURI;
                }

                if (pAudioFile != null && pAudioFile.Split(new string[] { ":\\" }, StringSplitOptions.None).Length == 2)
                {
                    FileInfo fi2 = new FileInfo(pAudioFile);
                    if (fi2.Exists)
                    {
                        LoadAudio(pAudioFile);
                    }
                    else
                    {
                        tbAudioFile.Text = Transcription.MediaURI;
                    }
                }
            }

            this.Title = Const.APP_NAME + " [" + data.FileName + "]";


        }

        private void menuSouborImportovatClick(object sender, RoutedEventArgs e)
        {

            Plugin p = (Plugin)((MenuItem)sender).Tag;
            WPFTranscription data = p.ExecuteImport();
            LoadSubtitlesData(data);
        }

        Thread Pedalthread = null;
        Process PedalProcess = null;

        //init tool to handle pedals (foot control)
        private void PedalsInit()
        {
            string pedalsexe = FilePaths.PedalPath;

            if (File.Exists(pedalsexe))
            {

                var StartInfo = new ProcessStartInfo(pedalsexe)
                {
                    Arguments = "-nokeypress",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true

                };

                Process p = new Process();
                p.StartInfo = StartInfo;
                PedalProcess = p;

                Pedalthread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        PedalProcess.Start();
                        while (true)
                        {
                            if (PedalProcess.HasExited)
                                throw new Exception();
                            string s = PedalProcess.StandardOutput.ReadLine();
                            if (s == "+L")
                                this.Dispatcher.Invoke(new Action(() => CommandSmallJumpLeft.Execute(null, this)));
                            if (s == "+M")
                                this.Dispatcher.Invoke(new Action(() => CommandPlayPause.Execute(null, this)));
                            if (s == "+R")
                                this.Dispatcher.Invoke(new Action(() => CommandSmallJumpRight.Execute(null, this)));
                        }
                    }
                    finally
                    {
                        PedalProcess.StandardInput.WriteLine("exit");
                        PedalProcess.WaitForExit();
                        PedalProcess = null;
                        Pedalthread = null;
                    }

                })) { Name = "Pedals" };

                p.Exited += (sender, e) =>
                {
                    if (Pedalthread.IsAlive)
                    {
                        Pedalthread.Abort();
                        Pedalthread.Join(100);
                    }
                    Pedalthread = null;
                    PedalProcess = null;
                };

                Pedalthread.Start();
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

            ExceptionCatchWindow w = new ExceptionCatchWindow(this, e.ExceptionObject as Exception);
            w.ShowDialog();
        }


        /// <summary>
        /// threadsafe - show message in statusbar
        /// </summary>
        /// <param name="message"></param>
        private void ShowMessageInStatusbar(string message)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<string>(ShowMessageInStatusbar), message);
                return;
            }
            tbProgramStatus.Text = message;
        }


        private void button10_Click(object sender, RoutedEventArgs e)
        {
            CommandGeneratePhoneticTranscription.Execute(null, this);
        }


        private void btTakeSpeakerSnapshotFromVideo_Click(object sender, RoutedEventArgs e)
        {
            CommandTakeSpeakerSnapshotFromVideo.Execute(null, this);
        }


        private void menuItemShowPanelFoneticTranscription_Click(object sender, RoutedEventArgs e)
        {
            CommandShowPanelFoneticTranscription.Execute(null, this);
        }


        private delegate void ShowPanelFoneticTranscriptionDelegate(TranscriptionElement aTag);


        private void btClosePhoneticsPanel_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.PhoneticsPanelVisible = false;
        }

        private void gridSplitter2_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
        }



        public int NormalizePhonetics(TranscriptionElement aElement, int aIndexNormalizace)
        {
            throw new NotImplementedException();
        }

        private void btNormalize_Click(object sender, RoutedEventArgs e)
        {
            menuItemToolsNormalizePhonetics_Click(null, new RoutedEventArgs());

        }

        private void menuItemToolsNormalizePhonetics_Click(object sender, RoutedEventArgs e)
        {
            CommandNormalizeParagraph.Execute(null, this);
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (!Settings.Default.FeatureEnabler.LocalEdit)
                return;

            string[] p = e.Data.GetFormats();
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].ToLower() == "filenamew")
                {
                    string[] o = (string[])e.Data.GetData(p[i]);
                    if (System.IO.Path.GetExtension(o[0]) == ".trsx")
                        await OpenTranscription(false, o[0].ToString());
                    else
                        LoadAudio(o[0].ToString());
                }
            }

        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveElement != null)
            {
                Element focusedel = VirtualizingListBox.ActiveElement;
                if (focusedel != null)
                {
                    ICSharpCode.AvalonEdit.TextEditor focused = focusedel.editor;
                    string insert = "[" + ((Button)sender).Content + "]";
                    focused.Document.Insert(focused.CaretOffset, insert);
                }
            }
        }

        #region nonspeech events tags

        private void toolBar1_Loaded(object sender, RoutedEventArgs e)
        {
            Settings.Default.PropertyChanged += Setup_PropertyChanged;
            Setup_PropertyChanged(Settings.Default, new System.ComponentModel.PropertyChangedEventArgs("NonSpeechEvents"));
        }

        void Setup_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "NonSpeechEvents")
                return;
            toolBarNSE.Items.Clear();
            int index = 1;
            foreach (string s in Settings.Default.NonSpeechEvents)
            {
                Button b = new Button();
                b.Content = s;
                toolBarNSE.Items.Add(b);
                b.BorderBrush = Brushes.Black;
                b.Click += new RoutedEventHandler(Button_Click);
                ToolBar.SetOverflowMode(b, OverflowMode.Never);
                if (index <= 9)
                    this.InputBindings.Add(new KeyBinding(new ButtonClickCommand(b), (Key)Enum.Parse(typeof(Key), "D" + index), ModifierKeys.Alt));
                index++;
            }
        }

        #endregion

        #region undo not working .)
        List<byte[]> Back_data = new List<byte[]>();
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            while (Back_data.Count > 20)
            {
                Back_data.RemoveAt(0);
            }

            if (Transcription != null)
            {
                MemoryStream ms = new MemoryStream();
                Transcription.Serialize(ms, true);
                ms.Close();
                Back_data.Add(ms.ToArray());
            }
        }

        #endregion

        private void waveform1_SliderPositionChanged(object sender, WaveForm.TimeSpanEventArgs e)
        {
            if (!Playing)
            {
                TimeSpan ts = e.Value;
                PlaybackBufferIndex = (int)ts.TotalMilliseconds;
                if (videoAvailable) meVideo.Position = ts;

                if (!(waveform1.SelectionBegin <= e.Value && waveform1.SelectionEnd >= e.Value))
                {
                    waveform1.SelectionBegin = new TimeSpan(-1);
                    waveform1.SelectionEnd = new TimeSpan(-1);
                }

            }
        }
        private void waveform1_CaretPostionChanged(object sender, WaveForm.TimeSpanEventArgs e)
        {
            if (!Playing)
            {
                PlaybackBufferIndex = (int)e.Value.TotalMilliseconds;
                oldms = TimeSpan.Zero;

                if (!(waveform1.SelectionBegin <= e.Value && waveform1.SelectionEnd >= e.Value))
                {
                    waveform1.SelectionBegin = new TimeSpan(-1);
                    waveform1.SelectionEnd = new TimeSpan(-1);
                }
            }


            var list = _transcription.ReturnElementsAtTime(e.Value);
            if (list != null && list.Count > 0)
            {
                if (VirtualizingListBox.ActiveTransctiption != list[0])
                {
                    VirtualizingListBox.ActiveTransctiption = list[0];
                    phoneticTranscription.ValueElement = list[0];

                }
            }
        }

        private void waveform1_CaretPostionChangedByUser(object sender, WaveForm.TimeSpanEventArgs e)
        {
            if (Playing)
            {
                Playing = false;

            }

            if (!(waveform1.SelectionBegin <= e.Value && waveform1.SelectionEnd >= e.Value))
            {
                waveform1.SelectionBegin = new TimeSpan(-1);
                waveform1.SelectionEnd = new TimeSpan(-1);
            }

            PlaybackBufferIndex = (int)waveform1.CaretPosition.TotalMilliseconds;
            List<TranscriptionParagraph> pl = Transcription.ReturnElementsAtTime(waveform1.CaretPosition);

            _setCaret = true;
            var list = _transcription.ReturnElementsAtTime(e.Value);
            if (list != null && list.Count > 0)
            {
                if (VirtualizingListBox.ActiveTransctiption != list[0])
                    VirtualizingListBox.ActiveTransctiption = list[0];

            }
            _setCaret = false;
            SelectTextBetweenTimeOffsets(e.Value);
        }

        private void waveform1_ParagraphClick(object sender, WaveForm.MyTranscriptionElementEventArgs e)
        {
            VirtualizingListBox.ActiveTransctiption = e.Value;
            waveform1.SelectionBegin = e.Value.Begin;
            waveform1.SelectionEnd = e.Value.End;
        }

        private void waveform1_ParagraphDoubleClick(object sender, WaveForm.MyTranscriptionElementEventArgs e)
        {
            VirtualizingListBox.ActiveTransctiption = e.Value;
            CommandAssignSpeaker.Execute((TranscriptionParagraph)e.Value, null);
            VirtualizingListBox.SpeakerChanged();
        }


        private void waveform1_PlayPauseClick(object sender, RoutedEventArgs e)
        {
            CommandPlayPause.Execute(null, this);
        }


        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;


        private void VirtualizingListBox_ChangeSpeaker(object sender, EventArgs e)
        {
            CommandAssignSpeaker.Execute(null, null);
        }

        private void VirtualizingListBox_SelectedElementChanged(object sender, EventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption == null)
                return;

            phoneticTranscription.ValueElement = VirtualizingListBox.ActiveTransctiption;
            phoneticTranscription.IsEnabled = true;
            SetCaretPosition(VirtualizingListBox.ActiveTransctiption.Begin);
        }

        private void VirtualizingListBox_SetTimeRequest(TimeSpan obj)
        {
            if (Playing)
                CommandPlayPause.Execute(null, null);

            SetCaretPosition(obj);
            SelectTextBetweenTimeOffsets(obj);
        }

        private void menuToolsShowVideoFrame_Click(object sender, RoutedEventArgs e)
        {
            //gListVideo.ColumnDefinitions[1].Width = GridLength.Auto;
            Properties.Settings.Default.VideoPanelVisible = true;
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            VirtualizingListBox.Reset();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2 || e.ChangedButton == MouseButton.Middle)
                CommandPlayPause.Execute(null, null);
        }


        private void menuFileImport_Click(object sender, RoutedEventArgs e)
        {
            CommandImportFile.Execute(null, null);
        }

        private void menuFileExport_Click(object sender, RoutedEventArgs e)
        {
            CommandExportFile.Execute(null, null);
        }

        #region plugins

        List<Plugin> _ImportPlugins = new List<Plugin>();
        List<Plugin> _ExportPlugins = new List<Plugin>();
        private void LoadPlugins()
        {
            string file = FilePaths.GetReadPath(FilePaths.PluginsFile);
            string path = System.IO.Path.GetDirectoryName(file);

            using (var f = File.OpenRead(file))
            {
                var doc = XDocument.Load(f).Element("Plugins");
                var imports = doc.Element("Import").Elements("Plugin");
                var exports = doc.Element("Export").Elements("Plugin");

                foreach (var imp in imports.Where(i => i.Attribute("IsAssembly").Value.ToLower() == "true"))
                {
                    var asspath = System.IO.Path.Combine(path, imp.Attribute("File").Value);
                    if (!File.Exists(asspath))
                        continue;
                    Assembly a = Assembly.LoadFrom(asspath);
                    Type ptype = a.GetType(imp.Attribute("Class").Value);
                    MethodInfo mi = ptype.GetMethod("Import", BindingFlags.Static | BindingFlags.Public);
                    Func<Stream, Transcription, bool> act = (Func<Stream, Transcription, bool>)Delegate.CreateDelegate(typeof(Func<Stream, Transcription, bool>), mi);

                    _ImportPlugins.Add(new Plugin(true, true, imp.Attribute("Mask").Value, null, imp.Attribute("Name").Value, act, null, null));
                }

                foreach (var imp in imports.Where(i => i.Attribute("IsAssembly").Value.ToLower() == "false"))
                {
                    _ImportPlugins.Add(new Plugin(true, false, imp.Attribute("Mask").Value, imp.Attribute("Parameters").Value, imp.Attribute("Name").Value, null, null, imp.Attribute("File").Value));
                }


                foreach (var exp in exports.Where(e => e.Attribute("IsAssembly").Value.ToLower() == "true"))
                {
                    var asspath = System.IO.Path.Combine(path, exp.Attribute("File").Value);
                    if (!File.Exists(asspath))
                        continue;

                    Assembly a = Assembly.LoadFrom(asspath);
                    Type ptype = a.GetType(exp.Attribute("Class").Value);
                    MethodInfo mi = ptype.GetMethod("Export", BindingFlags.Static | BindingFlags.Public);
                    Func<Transcription, Stream, bool> act = (Func<Transcription, Stream, bool>)Delegate.CreateDelegate(typeof(Func<Transcription, Stream, bool>), mi);

                    _ExportPlugins.Add(new Plugin(true, true, exp.Attribute("Mask").Value, null, exp.Attribute("Name").Value, null, act, null));
                }

                foreach (var exp in exports.Where(i => i.Attribute("IsAssembly") == null || i.Attribute("IsAssembly").Value == "false"))
                {
                    _ExportPlugins.Add(new Plugin(true, false, exp.Attribute("Mask").Value, exp.Attribute("Parameters").Value, exp.Attribute("Name").Value, null, null, exp.Attribute("File").Value));
                }

            }

        }

        #endregion

        private void menuToolsDeleteNonSpeechEvents_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                try
                {
                    TranscriptionParagraph p = Transcription.Chapters[0].Sections[0].Paragraphs[0];
                    while (p != null)
                    {
                        for (int i = p.Phrases.Count - 1; i >= 0; i--)
                        {
                            if (Element.ignoredGroup.IsMatch(p.Phrases[i].Text.Trim()))
                            {
                                p.RemoveAt(i);
                            }
                            else
                            {
                                var ms = Element.ignoredGroup.Matches(p.Phrases[i].Text).Cast<Match>().ToArray();
                                if (ms.Length > 0)
                                {
                                    int from = 0;
                                    string s = "";
                                    foreach (var m in ms)
                                    {
                                        int copy = m.Index - from;
                                        if (copy > 0)
                                            s += p.Phrases[i].Text.Substring(from, copy);
                                        from = m.Index + m.Length;
                                    }

                                    if (from < p.Phrases[i].Text.Length)
                                        s += p.Phrases[i].Text.Substring(from);

                                    p.Phrases[i].Text = s;
                                }
                            }
                        }
                        p = (TranscriptionParagraph)p.NextSibling();
                    }
                    VirtualizingListBox.Reset();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

            }
        }

        private void MenuItemLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption == null || !VirtualizingListBox.ActiveTransctiption.IsParagraph)
                return;
            var par = VirtualizingListBox.ActiveTransctiption as TranscriptionParagraph;
            var l = (string)(sender as MenuItem).Header;

            VirtualizingListBox.ActiveElement.ElementLanguage = l;
            var prev = VirtualizingListBox.ActiveElement;
            if (VirtualizingListBox.ActiveElement.RefreshSpeakerInfos())
            {
                prev = VirtualizingListBox.GetVisualForTransctiption(VirtualizingListBox.ActiveTransctiption.Previous());//refresh previous elements
                while (prev != null && prev.RefreshSpeakerInfos())
                    prev = VirtualizingListBox.GetVisualForTransctiption(prev.ValueElement.Previous());

                prev = VirtualizingListBox.GetVisualForTransctiption(VirtualizingListBox.ActiveTransctiption.Next());//refresh next elements
                while (prev != null && prev.RefreshSpeakerInfos())
                    prev = VirtualizingListBox.GetVisualForTransctiption(prev.ValueElement.Next());

            }

        }



        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            new SpeakerSynchronizer(Transcription, SpeakersDatabase).ShowDialog();
            VirtualizingListBox.SpeakerChanged();

        }
        private void Button_HideNSE_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_HideNSE_Click(object sender, RoutedEventArgs e)
        {
            Button_HideNSE.IsChecked = NonEditableBlockGenerator.HideNSE = !NonEditableBlockGenerator.HideNSE;
            VirtualizingListBox.Reset();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await OpenTranscription(false, _TranscriptionList[_transcriptionIndex - 1].FullName, true);

        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            await OpenTranscription(false, _TranscriptionList[_transcriptionIndex + 1].FullName, true);
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ComboBox;
            if (box.SelectedIndex < 0 || TranscriptionIsLoading)
                return;

            await OpenTranscription(false, _TranscriptionList[box.SelectedIndex].FullName, true);
        }


        public bool TranscriptionIsLoading { get; set; }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            FilePaths.SelectFolderDialog(false);
            tbQuickSavePath.Text = System.IO.Path.GetDirectoryName(FilePaths.QuickSaveDirectory);
            tbQuickSaveName.Text = "\\" + System.IO.Path.GetFileName(FilePaths.QuickSaveDirectory);
        }

        private void button_back_Click(object sender, RoutedEventArgs e)
        {
            CommandUndo.Execute(null, null);
        }

        private void button_forward_Click(object sender, RoutedEventArgs e)
        {
            CommandRedo.Execute(null, null);
        }

        private void menuEditUndo_Click(object sender, RoutedEventArgs e)
        {
            CommandUndo.Execute(null, null);
        }

        private void menuEditRedo_Click(object sender, RoutedEventArgs e)
        {
            CommandRedo.Execute(null, null);
        }

    }

    public class DoubleGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new GridLength((double)value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            GridLength gridLength = (GridLength)value;
            return gridLength.Value;
        }
    }
}