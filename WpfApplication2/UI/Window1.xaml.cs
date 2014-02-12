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

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window, System.ComponentModel.INotifyPropertyChanged
    {
        //timer pro posuvnik videa....
        private DispatcherTimer caretRefreshTimer = new DispatcherTimer();

        private WPFTranscription _transcription;
        public WPFTranscription Transcription
        {
            get { return _transcription; }
            set
            {
                if (_transcription != null)
                    _transcription.SubtitlesChanged -= _SubtitlesChanged;
                _transcription = value;
                if (_transcription != null)
                    _transcription.SubtitlesChanged += _SubtitlesChanged;

                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("Transcription"));

            }
        }

        void _SubtitlesChanged()
        {
            waveform1.InvalidateSpeakers();
        }


        private AdvancedSpeakerCollection SpeakersDatabase;

        WinHelp helpWindow;                                   //okno s napovedou

        private WavReader oWav = null;



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
        private bool playbackStarted = false;
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
            InitializeComponent();

            //load settings
            Stream s = FilePaths.GetConfigFileReadStream();
            if (s != null)
                GlobalSetup.Setup = GlobalSetup.Setup.Deserialize(s);
            else
                GlobalSetup.Setup.Serialize(s, GlobalSetup.Setup);

            if (GlobalSetup.Setup.Locale != null)
            {
                LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo(GlobalSetup.Setup.Locale);
            }

            //set window position, size and state
            if (GlobalSetup.Setup.WindowsPosition != null)
            {
                if (GlobalSetup.Setup.WindowsPosition.X >= 0 && GlobalSetup.Setup.WindowsPosition.Y >= 0)
                {
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                    this.Left = GlobalSetup.Setup.WindowsPosition.X;
                    this.Top = GlobalSetup.Setup.WindowsPosition.Y;
                }
            }
            if (GlobalSetup.Setup.WindowSize != null)
            {
                if (GlobalSetup.Setup.WindowSize.Width >= 50 && GlobalSetup.Setup.WindowSize.Height >= 50)
                {
                    this.Width = GlobalSetup.Setup.WindowSize.Width;
                    this.Height = GlobalSetup.Setup.WindowSize.Height;
                }
            }

            this.WindowState = GlobalSetup.Setup.WindowState;


            ShowPhoneticTranscription(GlobalSetup.Setup.PhoneticsPanelHeight - 1 > 0);


            SpeakersDatabase = new AdvancedSpeakerCollection();

            string fname = System.IO.Path.GetFullPath(GlobalSetup.Setup.SpeakersDatabasePath);
            if (fname.Contains(FilePaths.ProgramDirectory))
            {
                if (!FilePaths.WriteToAppData)
                {
                    SpeakerCollection.Deserialize(GlobalSetup.Setup.SpeakersDatabasePath, SpeakersDatabase);
                }
                else
                {
                    string fname2 = System.IO.Path.Combine(FilePaths.AppDataDirectory, fname.Substring(FilePaths.ProgramDirectory.Length));
                    if (File.Exists(fname2))
                    {
                        SpeakerCollection.Deserialize(fname2, SpeakersDatabase);
                    }
                    else if (File.Exists(GlobalSetup.Setup.SpeakersDatabasePath))
                    {
                        SpeakerCollection.Deserialize(GlobalSetup.Setup.SpeakersDatabasePath, SpeakersDatabase);
                    }
                }
            }
            else
            {
                if (File.Exists(GlobalSetup.Setup.SpeakersDatabasePath))
                {
                    SpeakerCollection.Deserialize(GlobalSetup.Setup.SpeakersDatabasePath, SpeakersDatabase);
                }
                else
                {
                    MessageBox.Show(Properties.Strings.MessageBoxLocalSpeakersDatabaseUnreachableLoad, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            foreach (var item in SpeakersDatabase)
                item.PinnedToDocument = false;

            oWav = new WavReader();
            oWav.HaveData += oWav_HaveData;
            oWav.HaveFileNumber += oWav_ReportConversionProgress;
            oWav.TemporaryWavesDone += new EventHandler(oWav_TemporaryWavesDone);

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
                if (_playing && oWav != null && oWav.Loaded)
                {
                    TimeSpan limitEndMS = new TimeSpan(-1);
                    if (PlayingSelection)
                    {
                        limitEndMS = waveform1.SelectionEnd;
                    }

                    short[] bfr = waveform1.GetAudioData(TimeSpan.FromMilliseconds(PlaybackBufferIndex), TimeSpan.FromMilliseconds(150), limitEndMS);
                    beginMS = PlaybackBufferIndex;
                    PlaybackBufferIndex += 150;

                    if (PlaybackBufferIndex > oWav.FileLengthMS)
                    {
                        if (!PlayingSelection)
                        {
                            Playing = false;
                            PlaybackBufferIndex = 0;
                        }
                        else
                        {
                            PlaybackBufferIndex = (int)waveform1.SelectionBegin.TotalMilliseconds;
                        }
                    }

                    if (!playbackStarted)
                    {
                        playbackStarted = true;

                    }

                    return bfr;
                }
            }
            return new short[0];
        }

        private void oWav_ReportConversionProgress(object sender, EventArgs e)
        {
            AudioBufferEventArgs2 e2 = (AudioBufferEventArgs2)e;
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<AudioBufferEventArgs2>(ShowConversionProgress), e2);
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
            pbPrevodAudio.Value = e.FileNumber;
            waveform1.ProgressHighlightBegin = TimeSpan.Zero;
            waveform1.ProgressHighlightEnd = TimeSpan.FromMilliseconds(e.LengthMS);


            if (e.LengthMS >= oWav.FileLengthMS)
            {
                ShowMessageInStatusbar(Properties.Strings.mainWindowStatusbarStatusTextConversionDone);
                pbPrevodAudio.Visibility = Visibility.Hidden;
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
                    if (waveform1.WaveLength < TimeSpan.FromSeconds(30))
                    {
                        waveform1.WaveLength = TimeSpan.FromSeconds(30);
                    }
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

        public bool NewTranscription()
        {
            if (Transcription != null && !Transcription.Saved)
            {
                MessageBoxResult mbr = MessageBox.Show(Properties.Strings.MessageBoxSaveBeforeClosing, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (mbr == MessageBoxResult.Cancel || mbr == MessageBoxResult.None)
                {
                    return false;
                }
                else if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                {
                    if (mbr == MessageBoxResult.Yes)
                    {
                        if (Transcription.FileName != null)
                        {
                            if (!SaveTranscription(false, Transcription.FileName)) return false;
                        }
                        else
                        {
                            if (!SaveTranscription(true, Transcription.FileName)) return false;
                        }
                    }
                }
            }

            var source = new WPFTranscription();
            var c = new TranscriptionChapter(Properties.Strings.DefaultChapterText);
            var s = new TranscriptionSection(Properties.Strings.DefaultSectionText);
            var p = new TranscriptionParagraph();
            p.Add(new TranscriptionPhrase());
            c.Add(s);
            s.Add(p);
            source.Add(c);
            source.Saved = true;
            Transcription = source;
            SynchronizeSpeakers();
            VirtualizingListBox.ActiveTransctiption = p;
            return true;
        }




        public bool OpenTranscription(bool useOpenDialog, string fileName)
        {
            try
            {
                if (Transcription != null && !Transcription.Saved)
                {
                    MessageBoxResult mbr = MessageBox.Show(Properties.Strings.MessageBoxSaveBeforeClosing, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (mbr == MessageBoxResult.Cancel || mbr == MessageBoxResult.None)
                    {
                        return false;
                    }
                    else if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                    {
                        if (mbr == MessageBoxResult.Yes)
                        {
                            if (Transcription.FileName != null)
                            {
                                if (!SaveTranscription(false, Transcription.FileName)) return false;
                            }
                            else
                            {
                                if (!SaveTranscription(true, Transcription.FileName)) return false;
                            }
                        }
                    }
                }


                if (Transcription == null) Transcription = new WPFTranscription();
                if (useOpenDialog)
                {
                    Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

                    fileDialog.Title = Properties.Strings.FileDialogLoadTranscriptionTitle;
                    fileDialog.Filter = Properties.Strings.FileDialogLoadTranscriptionFilter;
                    //fileDialog.FilterIndex = 1;
                    fileDialog.RestoreDirectory = true;

                    if (fileDialog.ShowDialog() == true)
                    {
                        if (Transcription != null && !Transcription.Saved)
                        {
                            MessageBoxResult mbr = MessageBox.Show(Properties.Strings.MessageBoxSaveBeforeClosing, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            if (mbr == MessageBoxResult.Cancel || mbr == MessageBoxResult.None)
                            {
                                return false;
                            }
                            else if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                            {
                                if (mbr == MessageBoxResult.Yes)
                                {
                                    if (Transcription.FileName != null)
                                    {
                                        if (!SaveTranscription(false, Transcription.FileName)) return false;
                                    }
                                    else
                                    {
                                        if (!SaveTranscription(true, Transcription.FileName)) return false;
                                    }
                                }
                            }
                        }

                        WPFTranscription pDataSource = new WPFTranscription(fileDialog.FileName);

                        if (pDataSource == null)
                        {
                            NewTranscription();
                        }
                        else
                        {
                            Transcription = pDataSource;


                            Transcription.Saved = true;

                            //try to load the audio file
                            if (!string.IsNullOrEmpty(Transcription.MediaURI) && Transcription.FileName != null)
                            {
                                FileInfo fiA = new FileInfo(Transcription.MediaURI);
                                string pAudioFile = null;
                                if (fiA.Exists)
                                {
                                    pAudioFile = fiA.FullName;
                                }
                                else
                                {
                                    FileInfo fi = new FileInfo(Transcription.FileName);
                                    pAudioFile = fi.Directory.FullName + "\\" + Transcription.MediaURI;
                                }
                                if (pAudioFile.Split(new string[] { ":\\" }, StringSplitOptions.None).Length == 2)
                                {
                                    FileInfo fi2 = new FileInfo(pAudioFile);
                                    if (fi2.Exists)
                                    {
                                        LoadAudio(pAudioFile);
                                    }
                                }
                            }

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

                        SynchronizeSpeakers();


                        this.Title = Const.APP_NAME + " [" + Transcription.FileName + "]";
                        VirtualizingListBox.ActiveTransctiption = Transcription.First(e => e.IsParagraph) ?? Transcription.First();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {

                    Transcription = WPFTranscription.Deserialize(fileName);

                    if (Transcription != null)
                    {
                        this.Title = Const.APP_NAME + " [" + Transcription.FileName + "]";
                        //try to load the audio file
                        if (Transcription.MediaURI != null && Transcription.FileName != null)
                        {
                            FileInfo fiA = new FileInfo(Transcription.MediaURI);
                            string pAudioFile = null;
                            if (fiA.Exists)
                            {
                                pAudioFile = fiA.FullName;
                            }
                            else
                            {
                                FileInfo fi = new FileInfo(Transcription.FileName);
                                pAudioFile = fi.Directory.FullName + "\\" + Transcription.MediaURI;
                            }
                            if (pAudioFile.Split(new string[] { ":\\" }, StringSplitOptions.None).Length == 2)
                            {

                                FileInfo fi2 = new FileInfo(pAudioFile);
                                if (fi2.Exists && (!oWav.Loaded || oWav.FilePath.ToUpper() != pAudioFile.ToUpper()))
                                {
                                    LoadAudio(pAudioFile);
                                }
                            }
                        }

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
                        SynchronizeSpeakers();
                        return true;
                    }
                    else
                    {
                        NewTranscription();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Strings.MessageBoxOpenTranscriptionError + ex.Message, Properties.Strings.MessageBoxErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SynchronizeSpeakers()
        {

            //unpin data from last document
            foreach (var item in SpeakersDatabase)
                item.PinnedToDocument = false;

            var toremove = new List<Speaker>();
            foreach (Speaker i in Transcription.Speakers)
            {
                Speaker ss = SpeakersDatabase.SynchronizeSpeaker(i);
                if (ss != null)
                {
                    if (i.PinnedToDocument)//pin to this document
                        ss.PinnedToDocument = true;


                    foreach (var p in Transcription.EnumerateParagraphs().Where(p => p.Speaker == i))
                        p.Speaker = ss;
                    if(!i.PinnedToDocument)
                        toremove.Add(i);
                }
            }

            foreach (var s in toremove)
                Transcription.Speakers.Remove(s);
        }


        public bool SaveTranscription(bool useSaveDialog, string jmenoSouboru)
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
                    }
                    else
                        return false;
                }

                if (Transcription.Serialize(savePath, GlobalSetup.Setup.SaveWholeSpeaker))
                {
                    this.Title = Const.APP_NAME + " [" + Transcription.FileName + "]";
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Strings.MessageBoxSaveTranscriptionError + ex.Message, Properties.Strings.MessageBoxErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }




        TimeSpan oldms = TimeSpan.Zero;
        bool _setCaret= false;
        public void SetCaretPosition(TimeSpan position, bool nastavitMedia, bool aNeskakatNaZacatekElementu)
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

        private void VyberFonetikuMeziCasovymiZnackami(TimeSpan aPoziceKurzoru)
        {
            phoneticTranscription.HiglightedPostion = aPoziceKurzoru;
        }


        private void SelectTextBetweenTimeOffsets(TimeSpan aPoziceKurzoru)
        {
            VirtualizingListBox.HiglightedPostion = aPoziceKurzoru;

            VyberFonetikuMeziCasovymiZnackami(aPoziceKurzoru);
        }

        public void InitializeTimer()
        {
            caretRefreshTimer.Interval = new TimeSpan(0, 0, 0, 0, Const.WAVEFORM_CARET_REFRESH_MS);
            caretRefreshTimer.IsEnabled = true;
            caretRefreshTimer.Tick += new EventHandler(OnTimer);
        }

        void OnTimer(Object source, EventArgs e)
        {
            long rozdil = (long)waveform1.WaveLength.TotalMilliseconds; //delka zobrazeni v msekundach

            TimeSpan playpos = waveform1.CaretPosition;
            if (_playing)
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
                    SetCaretPosition(waveform1.SelectionBegin, true, true);

                    playpos = waveform1.CaretPosition;

                    Playing = true;
                    if (MWP != null)
                        MWP.Play(MWP.PlaySpeed);

                }
                else
                {
                    SetCaretPosition(waveform1.CaretPosition, false, true);
                }

                if (Playing) SelectTextBetweenTimeOffsets(playpos);
            }
        }

        /// <summary>
        /// spusti proceduru pro nacitani videa, pokud je zadana cesta, pokusi se nacist dany soubor
        /// </summary>
        /// <param name="aFileName"></param>
        /// <returns></returns>
        private bool LoadAudio(string aFileName)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
                openDialog.Title = Properties.Strings.LoadAudioTitle;
                openDialog.Filter = Properties.Strings.LoadAudioFilter;
                openDialog.FilterIndex = 1;

                bool pOtevrit = false;
                if (aFileName != null && aFileName != "")
                {
                    FileInfo fi = new FileInfo(aFileName);
                    if (fi.Exists)
                    {
                        pOtevrit = true;
                    }
                }

                if (pOtevrit || openDialog.ShowDialog() == true)
                {
                    waveform1.AutomaticProgressHighlight = false;
                    if (!pOtevrit)
                    {
                        aFileName = openDialog.FileName;
                    }



                    FileInfo fi = new FileInfo(aFileName);
                    if (fi != null)
                    {
                        Transcription.MediaURI = fi.Name;
                    }

                    ////////////
                    long pDelkaSouboruMS = oWav.VratDelkuSouboruMS(aFileName);

                    if (pDelkaSouboruMS > -1)
                    {
                        if (oWav != null)
                        {
                            oWav.Dispose();
                        }

                        //nastaveni pocatku prehravani
                        Playing = false;

                        waveform1.CaretPosition = TimeSpan.Zero;
                        //  pIndexBufferuVlnyProPrehrani = 0;
                        waveform1.DataRequestCallBack = oWav.NactiRamecBufferu;

                        pbPrevodAudio.Value = 0;
                        waveform1.AudioLength = TimeSpan.FromMilliseconds(pDelkaSouboruMS);
                        TimeSpan ts = new TimeSpan(pDelkaSouboruMS * 10000);

                        pbPrevodAudio.Maximum = ts.TotalMinutes - 1;
                        pbPrevodAudio.Value = 0;

                        //start prevodu docasnych souboru
                        oWav.AsynchronniPrevodMultimedialnihoSouboruNaDocasne2(aFileName); //spusti se thread ktery prevede soubor na temp wavy
                        ShowMessageInStatusbar(Properties.Strings.mainWindowStatusbarStatusTextConversionRunning);
                        pbPrevodAudio.Visibility = Visibility.Visible;
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
            catch// (Exception ex)
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
                FileInfo fi = null;
                if (aFileName != null) fi = new FileInfo(aFileName);
                bool pOtevrit = fi != null && fi.Exists;
                if (pOtevrit) openDialog.FileName = aFileName;
                if (pOtevrit || openDialog.ShowDialog() == true)
                {
                    meVideo.Source = new Uri(openDialog.FileName);

                    meVideo.Play();

                    meVideo.Position = new TimeSpan(0, 0, 0, 0, PlaybackBufferIndex);
                    //meVideo.Position = mediaElement1.Position;
                    //if (!playing) meVideo.Pause();
                    meVideo.IsMuted = true;
                    videoAvailable = true;

                    try
                    {
                        tbVideoFile.Text = new FileInfo(openDialog.FileName).Name;
                        Transcription.VideoFileName = tbVideoFile.Text;
                    }
                    catch
                    {

                    }
                    infoPanels.SelectedIndex = 0;

                    if (oWav != null && oWav.FilePath != null && oWav.FilePath != "")
                    {
                        if (!pOtevrit && MessageBox.Show(Properties.Strings.MessageBoxUseVideoFileAsAudioSource, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            LoadAudio(openDialog.FileName);
                        }
                    }
                    else
                    {
                        //automaticke nacteni audia
                        LoadAudio(openDialog.FileName);
                    }

                    //gListVideo.ColumnDefinitions[1].Width = new GridLength(150);

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
                MWP = new DXWavePlayer(GlobalSetup.Setup.audio.OutputDeviceIndex, 4800, ReadAudioDataFromWaveform);
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
            GlobalSetup.Setup = WinSetup.WinSetupShowDialog(GlobalSetup.Setup, SpeakersDatabase);
            GlobalSetup.Setup.Serialize(FilePaths.GetConfigFileWriteStream(), GlobalSetup.Setup);
            waveform1.SmallJump = TimeSpan.FromSeconds(GlobalSetup.Setup.WaveformSmallJump);
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
            if (Transcription != null && !Transcription.Saved)
            {
                MessageBoxResult mbr = MessageBox.Show(Properties.Strings.MessageBoxSaveBeforeClosing, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                {
                    if (mbr == MessageBoxResult.Yes)
                    {
                        if (Transcription.FileName != null)
                        {
                            if (!SaveTranscription(false, Transcription.FileName)) e.Cancel = true;
                        }
                        else
                        {
                            if (!SaveTranscription(true, Transcription.FileName)) e.Cancel = true;
                        }
                    }

                    if (helpWindow != null && !helpWindow.IsLoaded)
                    {
                        helpWindow.Close();
                    }


                }
                else if (mbr == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }


            }
            else if (helpWindow != null && helpWindow.IsLoaded)
            {
                helpWindow.Close();
            }


            if (!e.Cancel)
            {
                if (_findDialog != null && _findDialog.IsLoaded)
                    _findDialog.Close();


                if (MWP != null)
                {
                    MWP.Dispose();
                    MWP = null;
                }

                if (oWav != null)
                {
                    oWav.Dispose();
                }



                if (SpeakersDatabase != null)
                {
                    string fname = System.IO.Path.GetFullPath(GlobalSetup.Setup.SpeakersDatabasePath);
                    if (fname.StartsWith(FilePaths.ProgramDirectory))//check access to program files.
                    {
                        if (!FilePaths.WriteToAppData)
                            SpeakersDatabase.Serialize(fname,true);
                        else
                            SpeakersDatabase.Serialize(FilePaths.AppDataDirectory + fname.Substring(FilePaths.ProgramDirectory.Length),true);
                    }
                    else// not in ProgramFiles - NanoTrans not installed, don't do anything
                    {
                        try
                        {
                            SpeakersDatabase.Serialize(GlobalSetup.Setup.SpeakersDatabasePath,true);
                        }
                        catch
                        {
                            if (MessageBox.Show(Properties.Strings.MessageBoxLocalSpeakersDatabaseUnreachableSave, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                    }
                }

                if (GlobalSetup.Setup != null)
                {
                    if (this.WindowState == WindowState.Normal)
                    {
                        GlobalSetup.Setup.WindowsPosition = new Point(this.Left, this.Top);
                        GlobalSetup.Setup.WindowSize = new Size(this.Width, this.Height);
                    }
                    if (this.WindowState != WindowState.Minimized)
                    {
                        GlobalSetup.Setup.WindowState = this.WindowState;
                    }

                    GlobalSetup.Setup.Serialize(FilePaths.GetConfigFileWriteStream(), GlobalSetup.Setup);

                }
            }

            FilePaths.DeleteTemp();

            Environment.Exit(0); //Force close application


        }


        private void btCloseVideo_Click(object sender, RoutedEventArgs e)
        {
            meVideo.Close();
            meVideo.Source = null;
            videoAvailable = false;
            gListVideo.ColumnDefinitions[1].Width = new GridLength(1);
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



        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitCommands();
            LoadPlugins();
            //asyncronous spellcecking vocabluary load
            Thread t = new Thread(
                delegate()
                {
                    SpellChecker.LoadVocabulary();
                }
                ) { Name = "Spellchecking_Load" };
            t.Start();

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
            if (App.Startup_ARGS != null && App.Startup_ARGS.Length > 0)
            {
                if (App.Startup_ARGS[0] == "-i")
                {
                    import = true;
                    path = App.Startup_ARGS[1];
                }
                else
                    path = App.Startup_ARGS[0];
            }

            if (path == null)
            {
                NewTranscription();
            }
            else
            {
                if (import)
                {
                    NewTranscription();
                    CommandImportFile.Execute(path, this);
                }
                else
                    OpenTranscription(false, path);
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

                }));

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


        private bool ShowPhoneticTranscription(bool show)
        {

            if (show)
            {
                GlobalSetup.Setup.PhoneticsPanelHeight = Math.Abs(GlobalSetup.Setup.PhoneticsPanelHeight);

                d.RowDefinitions[1].Height = new GridLength(GlobalSetup.Setup.PhoneticsPanelHeight);

                return true;
            }
            else
            {
                GlobalSetup.Setup.PhoneticsPanelHeight = -Math.Abs(GlobalSetup.Setup.PhoneticsPanelHeight);
                d.RowDefinitions[1].Height = new GridLength(0);
                return false;
            }
        }

        private void btClosePhoneticsPanel_Click(object sender, RoutedEventArgs e)
        {
            ShowPhoneticTranscription(false);
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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] p = e.Data.GetFormats();
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].ToLower() == "filenamew")
                {
                    string[] o = (string[])e.Data.GetData(p[i]);
                    OpenTranscription(false, o[0].ToString());
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
            GlobalSetup.Setup.PropertyChanged += Setup_PropertyChanged;
            Setup_PropertyChanged(GlobalSetup.Setup, new System.ComponentModel.PropertyChangedEventArgs("NonSpeechEvents"));
        }

        void Setup_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "NonSpeechEvents")
                return;
            toolBar1.Items.Clear();
            int index = 1;
            foreach (string s in GlobalSetup.Setup.NonSpeechEvents)
            {
                Button b = new Button();
                b.Content = s;
                toolBar1.Items.Add(b);
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

        private void waveform1_SliderPositionChanged(object sender, Waveform.TimeSpanEventArgs e)
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
        private void waveform1_CaretPostionChanged(object sender, Waveform.TimeSpanEventArgs e)
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

        private void waveform1_CaretPostionChangedByUser(object sender, Waveform.TimeSpanEventArgs e)
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

        private void waveform1_ParagraphClick(object sender, Waveform.MyTranscriptionElementEventArgs e)
        {
            VirtualizingListBox.ActiveTransctiption = e.Value;
            waveform1.SelectionBegin = e.Value.Begin;
            waveform1.SelectionEnd = e.Value.End;
        }

        private void waveform1_ParagraphDoubleClick(object sender, Waveform.MyTranscriptionElementEventArgs e)
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
            SetCaretPosition(VirtualizingListBox.ActiveTransctiption.Begin, true, true);
        }

        private void VirtualizingListBox_SetTimeRequest(TimeSpan obj)
        {
            if (Playing)
                CommandPlayPause.Execute(null, null);

            SetCaretPosition(obj, true, true);
            SelectTextBetweenTimeOffsets(obj);
        }

        private void menuToolsShowVideoFrame_Click(object sender, RoutedEventArgs e)
        {
            gListVideo.ColumnDefinitions[1].Width = GridLength.Auto;
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
                    Assembly a = Assembly.LoadFrom(System.IO.Path.Combine(path, imp.Attribute("File").Value));
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
                    Assembly a = Assembly.LoadFrom(System.IO.Path.Combine(path, exp.Attribute("File").Value));
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

    }
}