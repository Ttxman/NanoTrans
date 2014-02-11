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

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window, System.ComponentModel.INotifyPropertyChanged
    {
        //timer pro posuvnik videa....
        private DispatcherTimer timer1 = new DispatcherTimer();
        private DispatcherTimer timerRozpoznavace = new DispatcherTimer();

        private WPFTranscription _mydatasource;
        public WPFTranscription Transcription
        {
            get { return _mydatasource; }
            set
            {
                if (_mydatasource != null)
                    _mydatasource.SubtitlesChanged -= _mydatasource_SubtitlesChanged;
                _mydatasource = value;
                if (_mydatasource != null)
                    _mydatasource.SubtitlesChanged += _mydatasource_SubtitlesChanged;

                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("Transcription"));

            }
        }

        void _mydatasource_SubtitlesChanged()
        {
            waveform1.InvalidateSpeakers();
        }

        /// <summary>
        /// databaze mluvcich konkretniho programu
        /// </summary>
        public SpeakerCollection SpeakersDatabase;
        //public static MySetup MySetup = null;          //trida pro nastaveni vlastnosti aplikace

        //public static bool spustenoOknoNapovedy = false;        //informace o spustenem oknu napovedy
        WinHelp oknoNapovedy;                                   //okno s napovedou

        /// <summary>
        /// trida starajici se o prevod multimedialnich souboru a nacitani bufferu pro zobrazeni a rozpoznani
        /// </summary>
        private WavReader oWav = null;



        bool jeVideo = false;

        /// <summary>
        /// po vybrani elementu pokracuje v prehravani
        /// </summary>
        bool prehratVyber = false;  //prehrava jen vybranou prepsanou sekci, pokud je specifikovan zacatek a konec




        short pocetTikuTimeru = 0;    //pomocna pro deleni tiku timeru

        /// <summary>
        /// obdelniky mluvcich pro zobrazeni v audio signalu
        /// </summary>
        private List<Button> bObelnikyMluvcich = new List<Button>();

        /// <summary>
        /// trida pro prehravani audio dat
        /// </summary>
        private MyWavePlayer MWP = null;

        private int _pIndexBufferuVlnyProPrehrani = 0;
        private int pIndexBufferuVlnyProPrehrani
        {
            get { return _pIndexBufferuVlnyProPrehrani; }
            set
            {
                _pIndexBufferuVlnyProPrehrani = value;

            }


        }
        private bool pZacloPrehravani = false;
        private bool _playing = false;
        private bool Playing
        {
            get { return _playing; }
            set
            {

                _playing = value;
                waveform1.Playing = value;
                pIndexBufferuVlnyProPrehrani = (int)waveform1.CaretPosition.TotalMilliseconds;
                oldms = TimeSpan.Zero;

                if (value)
                {
                    if (MWP == null)
                    {
                        InicializaceAudioPrehravace();
                    }
                }
                else
                {
                    if (MWP != null)
                        MWP.Pause();
                }


                if (jeVideo)
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

            //nastaveni aplikace
            Stream s = FilePaths.GetConfigFileReadStream();
            if (s != null)
                MySetup.Setup = MySetup.Setup.Deserializovat(s);
            else
                MySetup.Setup.Serializovat(s, MySetup.Setup);





            //nastaveni posledni pozice okna
            if (MySetup.Setup.OknoPozice != null)
            {
                if (MySetup.Setup.OknoPozice.X >= 0 && MySetup.Setup.OknoPozice.Y >= 0)
                {
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                    this.Left = MySetup.Setup.OknoPozice.X;
                    this.Top = MySetup.Setup.OknoPozice.Y;
                }
            }
            //nastaveni posledni velikosti okna
            if (MySetup.Setup.OknoVelikost != null)
            {
                if (MySetup.Setup.OknoVelikost.Width >= 50 && MySetup.Setup.OknoVelikost.Height >= 50)
                {
                    this.Width = MySetup.Setup.OknoVelikost.Width;
                    this.Height = MySetup.Setup.OknoVelikost.Height;
                }
            }

            this.WindowState = MySetup.Setup.OknoStav;


            ZobrazitOknoFonetickehoPrepisu(MySetup.Setup.ZobrazitFonetickyPrepis - 1 > 0);

            //databaze mluvcich
            SpeakersDatabase = new SpeakerCollection();

            string fname = System.IO.Path.GetFullPath(MySetup.Setup.CestaDatabazeMluvcich);
            if (fname.Contains(FilePaths.ProgramDirectory))
            {
                if (!FilePaths.WriteToAppData)
                {
                    SpeakersDatabase = SpeakerCollection.Deserialize(MySetup.Setup.CestaDatabazeMluvcich);
                }
                else
                {
                    string fname2 = System.IO.Path.Combine(FilePaths.AppDataDirectory, fname.Substring(FilePaths.ProgramDirectory.Length));
                    if (File.Exists(fname2))
                    {
                        SpeakersDatabase = SpeakerCollection.Deserialize(fname2);
                    }
                    else if (File.Exists(MySetup.Setup.CestaDatabazeMluvcich))
                    {
                        SpeakersDatabase = SpeakerCollection.Deserialize(MySetup.Setup.CestaDatabazeMluvcich);
                    }
                }
            }
            else
            {
                if (File.Exists(MySetup.Setup.CestaDatabazeMluvcich))
                {
                    SpeakersDatabase = SpeakerCollection.Deserialize(MySetup.Setup.CestaDatabazeMluvcich);
                }
                else
                {
                    MessageBox.Show(Properties.Strings.MessageBoxLocalSpeakersDatabaseUnreachableLoad, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }


            oWav = new WavReader();
            oWav.HaveData += oWav_HaveData;
            oWav.HaveFileNumber += oWav_HaveFileNumber;
            oWav.TemporaryWavesDone += new EventHandler(oWav_TemporaryWavesDone);

        }

        private void menuItemVlna1_SetStartToCursor_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// plni buffer pro prehravani
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        short[] WOP_ChciData(out int zacatekbufferums)
        {
            zacatekbufferums = -1;
            if (MWP != null)
            {
                if (_playing && oWav != null && oWav.Loaded)
                {
                    TimeSpan pOmezeniMS = new TimeSpan(-1);
                    if (prehratVyber)
                    {
                        pOmezeniMS = waveform1.SelectionEnd;
                    }

                    short[] bfr = waveform1.GetAudioData(TimeSpan.FromMilliseconds(pIndexBufferuVlnyProPrehrani), TimeSpan.FromMilliseconds(150), pOmezeniMS);
                    zacatekbufferums = pIndexBufferuVlnyProPrehrani;
                    pIndexBufferuVlnyProPrehrani += 150;

                    if (pIndexBufferuVlnyProPrehrani > oWav.FileLengthMS)
                    {
                        if (!prehratVyber)
                        {
                            Playing = false;
                            pIndexBufferuVlnyProPrehrani = 0;
                        }
                        else
                        {
                            pIndexBufferuVlnyProPrehrani = (int)waveform1.SelectionBegin.TotalMilliseconds;
                        }
                    }

                    if (!pZacloPrehravani)
                    {
                        pZacloPrehravani = true;

                    }

                    return bfr;
                }
                else //pause
                {
                }
            }
            return new short[0];
        }

        /// <summary>
        /// vraci cislo prevedeneho souboru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void oWav_HaveFileNumber(object sender, EventArgs e)
        {
            AudioBufferEventArgs2 e2 = (AudioBufferEventArgs2)e;
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<AudioBufferEventArgs2>(ZobrazProgressPrevoduSouboru), e2);
        }

        private void oWav_TemporaryWavesDone(object sender, EventArgs e)
        {
            waveform1.Dispatcher.Invoke(new Action(delegate()
            {
                waveform1.AutomaticProgressHighlight = true;
            }));

        }

        /// <summary>
        /// zobrazi progress nacitani
        /// </summary>
        /// <param name="e"></param>
        private void ZobrazProgressPrevoduSouboru(AudioBufferEventArgs2 e)
        {
            pbPrevodAudio.Value = e.FileNumber;
            waveform1.ProgressHighlightBegin = TimeSpan.Zero;
            waveform1.ProgressHighlightEnd = TimeSpan.FromMilliseconds(e.LengthMS);


            if (e.LengthMS >= oWav.FileLengthMS)
            {
                ZobrazStavProgramu(Properties.Strings.mainWindowStatusbarStatusTextConversionDone);
                pbPrevodAudio.Visibility = Visibility.Hidden;
                mainWindowStatusbarAudioConversionHeader.Visibility = Visibility.Hidden;
            }

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// automaticke nacteni 1 polozky
        /// </summary>
        bool pAutomaticky = false;


        /// <summary>
        /// metoda vracejici data z threadu, v argumentu e
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void oWav_HaveData(object sender, EventArgs e)
        {

            AudioBufferEventArgs me = (AudioBufferEventArgs)e;
            if (me.BufferID == MyKONST.ID_ZOBRAZOVACIHO_BUFFERU_VLNY)
            {
                waveform1.SetAudioData(me.data, TimeSpan.FromMilliseconds(me.StartMS), TimeSpan.FromMilliseconds(me.EndMS));
                if (me.StartMS == 0)
                {
                    if (!timer1.IsEnabled) InitializeTimer();
                    if (waveform1.WaveLength < TimeSpan.FromSeconds(30))
                    {
                        waveform1.WaveLength = TimeSpan.FromSeconds(30);
                    }
                }
                if (pAutomaticky)
                {
                    pAutomaticky = false;
                    menuItemNastrojeFonetickyPrepis_Click(null, new RoutedEventArgs());

                }
            }
            else if (me.BufferID == MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS)
            {
                throw new NotImplementedException();
            }

            waveform1.Invalidate();

        }


        bool blockfocus = false;
        void richX_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!blockfocus)
            {
                return;
            }
            blockfocus = false;
            e.Handled = true;
        }

        Brush GetRectangleBgColor(ParagraphAttributes param)
        {
            return Brushes.White;
        }

        Brush GetRectangleInnenrColor(ParagraphAttributes param)
        {
            switch (param)
            {
                default:
                case ParagraphAttributes.None:
                    return Brushes.White;
                case ParagraphAttributes.Background_noise:
                    return Brushes.DodgerBlue;
                case ParagraphAttributes.Background_speech:
                    return Brushes.Chocolate;
                case ParagraphAttributes.Junk:
                    return Brushes.Crimson;
                case ParagraphAttributes.Narrowband:
                    return Brushes.Olive;
            }
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
                            if (!UlozitTitulky(false, Transcription.FileName)) return false;
                        }
                        else
                        {
                            if (!UlozitTitulky(true, Transcription.FileName)) return false;
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
            VirtualizingListBox.ActiveTransctiption = p;
            return true;
        }




        public bool OpenTranscription(bool pouzitOpenDialog, string jmenoSouboru, bool aDavkovySoubor)
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
                                if (!UlozitTitulky(false, Transcription.FileName)) return false;
                            }
                            else
                            {
                                if (!UlozitTitulky(true, Transcription.FileName)) return false;
                            }
                        }
                    }
                }


                if (Transcription == null) Transcription = new WPFTranscription();
                if (pouzitOpenDialog)
                {
                    Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

                    fileDialog.Title = Properties.Strings.FileDialogLoadTranscriptionTitle;
                    fileDialog.Filter = Properties.Strings.FileDialogLoadTranscriptionFilter;
                    //fileDialog.FilterIndex = 1;
                    fileDialog.RestoreDirectory = true;

                    blockfocus = true;
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
                                        if (!UlozitTitulky(false, Transcription.FileName)) return false;
                                    }
                                    else
                                    {
                                        if (!UlozitTitulky(true, Transcription.FileName)) return false;
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

                            //nacteni audio souboru pokud je k dispozici
                            if (!string.IsNullOrEmpty(Transcription.mediaURI) && Transcription.FileName != null)
                            {
                                FileInfo fiA = new FileInfo(Transcription.mediaURI);
                                string pAudioFile = null;
                                if (fiA.Exists)
                                {
                                    pAudioFile = fiA.FullName;
                                }
                                else
                                {
                                    FileInfo fi = new FileInfo(Transcription.FileName);
                                    pAudioFile = fi.Directory.FullName + "\\" + Transcription.mediaURI;
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

                            if (Transcription.videoFileName != null && Transcription.FileName != null)
                            {
                                FileInfo fi = new FileInfo(Transcription.FileName);
                                string pVideoFile = fi.Directory.FullName + "\\" + Transcription.videoFileName;
                                FileInfo fi2 = new FileInfo(pVideoFile);
                                if (fi2.Exists && (meVideo.Source == null || meVideo.Source.AbsolutePath.ToUpper() != pVideoFile.ToUpper()))
                                {
                                    LoadVideo(pVideoFile);
                                }
                            }


                            //synchronizace mluvcich podle vnitrni databaze
                            try
                            {
                                foreach (Speaker i in Transcription.Speakers.Speakers)
                                {
                                    Speaker pSp = SpeakersDatabase.GetSpeakerByName(i.FullName);
                                    if (i.FullName == pSp.FullName && i.ImgBase64 == null)
                                    {
                                        i.ImgBase64 = pSp.ImgBase64;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        this.Title = MyKONST.NAZEV_PROGRAMU + " [" + Transcription.FileName + "]";
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

                    Transcription = WPFTranscription.Deserialize(jmenoSouboru);

                    if (Transcription != null)
                    {
                        this.Title = MyKONST.NAZEV_PROGRAMU + " [" + Transcription.FileName + "]";
                        //nacteni audio souboru pokud je k dispozici
                        if (Transcription.mediaURI != null && Transcription.FileName != null)
                        {
                            FileInfo fiA = new FileInfo(Transcription.mediaURI);
                            string pAudioFile = null;
                            if (fiA.Exists)
                            {
                                pAudioFile = fiA.FullName;
                            }
                            else
                            {
                                FileInfo fi = new FileInfo(Transcription.FileName);
                                pAudioFile = fi.Directory.FullName + "\\" + Transcription.mediaURI;
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

                        if (Transcription.videoFileName != null && Transcription.FileName != null)
                        {
                            FileInfo fi = new FileInfo(Transcription.FileName);
                            string pVideoFile = fi.Directory.FullName + "\\" + Transcription.videoFileName;
                            FileInfo fi2 = new FileInfo(pVideoFile);
                            if (fi2.Exists && (meVideo.Source == null || meVideo.Source.AbsolutePath.ToUpper() != pVideoFile.ToUpper()))
                            {
                                LoadVideo(pVideoFile);
                            }
                        }
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


        public bool UlozitTitulky(bool useSaveDialog, string jmenoSouboru)
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

                if (Transcription.Serialize(savePath, MySetup.Setup.UkladatKompletnihoMluvciho, !MySetup.Setup.SaveInShortFormat))
                {
                    this.Title = MyKONST.NAZEV_PROGRAMU + " [" + Transcription.FileName + "]";
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
        bool _pozicenastav = false;
        public void NastavPoziciKurzoru(TimeSpan position, bool nastavitMedia, bool aNeskakatNaZacatekElementu)
        {
            if (!_pozicenastav)
            {
                _pozicenastav = true;
                if (position < TimeSpan.Zero) return;

                if (waveform1.CaretPosition != position)
                    waveform1.CaretPosition = position;

                if (!Playing)
                    oldms = TimeSpan.Zero;

                if (!Playing && jeVideo && Math.Abs(meVideo.Position.TotalMilliseconds) > 200)
                {
                    meVideo.Position = waveform1.CaretPosition;
                }
                _pozicenastav = false;
            }
        }

        private void VyberFonetikuMeziCasovymiZnackami(TimeSpan aPoziceKurzoru)
        {
            phoneticTranscription.HiglightedPostion = aPoziceKurzoru;
        }


        private void VyberTextMeziCasovymiZnackami(TimeSpan aPoziceKurzoru)
        {
            VirtualizingListBox.HiglightedPostion = aPoziceKurzoru;

            VyberFonetikuMeziCasovymiZnackami(aPoziceKurzoru);
        }

        public void InitializeTimer()
        {
            timer1.Interval = new TimeSpan(0, 0, 0, 0, MyKONST.PERIODA_TIMERU_VLNY_MS);
            timer1.IsEnabled = true;
            timer1.Tick += new EventHandler(OnTimer);
        }

        void OnTimer(Object source, EventArgs e)
        {
            long rozdil = (long)waveform1.WaveLength.TotalMilliseconds; //delka zobrazeni v msekundach

            TimeSpan playpos = waveform1.CaretPosition;
            if (_playing)
            {
                playpos = MWP.PlayPosition;
                if (prehratVyber && playpos < waveform1.SelectionBegin)
                {
                    playpos = waveform1.SelectionBegin;
                }

                waveform1.CaretPosition = playpos;
            }

            pocetTikuTimeru++;
            if (pocetTikuTimeru > 0) //kazdy n ty tik dojde ke zmene pozice ctverce
            {
                if (pocetTikuTimeru > 2)
                {
                    pocetTikuTimeru = 0;
                    if (!_playing && jeVideo)
                    {
                        meVideo.Pause();
                    }
                }



                if (prehratVyber && playpos >= waveform1.SelectionEnd && waveform1.SelectionEnd >= TimeSpan.Zero)
                {

                    Playing = false;

                    oldms = TimeSpan.Zero;
                    NastavPoziciKurzoru(waveform1.SelectionBegin, true, true);

                    playpos = waveform1.CaretPosition;

                    Playing = true;
                    if (MWP != null)
                        MWP.Play(MWP.PlaySpeed);

                }
                else
                {
                    NastavPoziciKurzoru(waveform1.CaretPosition, false, true);
                }

                if (Playing) VyberTextMeziCasovymiZnackami(playpos);
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
                        Transcription.mediaURI = fi.Name;
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
                        ZobrazStavProgramu(Properties.Strings.mainWindowStatusbarStatusTextConversionRunning);
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

                        tbAudioSoubor.Text = new FileInfo(aFileName).Name;
                    }
                    catch
                    {
                        tbAudioSoubor.Text = openDialog.FileName;
                    }
                    finally
                    {
                        tbAudioSoubor.ToolTip = openDialog.FileName;

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

                    meVideo.Position = new TimeSpan(0, 0, 0, 0, pIndexBufferuVlnyProPrehrani);
                    //meVideo.Position = mediaElement1.Position;
                    //if (!playing) meVideo.Pause();
                    meVideo.IsMuted = true;
                    jeVideo = true;

                    try
                    {
                        tbVideoSoubor.Text = new FileInfo(openDialog.FileName).Name;
                        Transcription.videoFileName = tbVideoSoubor.Text;
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
            MSoubor_Otevrit_Video_Click(null, new RoutedEventArgs());
        }

        /// <summary>
        /// inicializuje MWP - prehravac audia, pokud neni null, zavola dispose a opet ho vytvori
        /// </summary>
        /// <returns></returns>
        private bool InicializaceAudioPrehravace()
        {
            try
            {
                if (MWP != null)
                {
                    MWP.Dispose();
                    MWP = null;
                }
                MWP = new MyWavePlayer(MySetup.Setup.audio.OutputDeviceIndex, 4800, WOP_ChciData);
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

        #region menu Soubor
        private void MSoubor_Novy_Click(object sender, RoutedEventArgs e)
        {
            CommandCreateNewTranscription.Execute(null, this);
        }

        private void MSoubor_Otevrit_Titulky_Click(object sender, RoutedEventArgs e)
        {
            CommandOpenTranscription.Execute(null, this);
        }

        private void MSoubor_Otevrit_Video_Click(object sender, RoutedEventArgs e)
        {
            LoadVideo(null);
        }

        private void MSoubor_Ulozit_Click(object sender, RoutedEventArgs e)
        {
            CommandSaveTranscription.Execute(null, this);
        }

        private void MSoubor_Ulozit_Titulky_Jako_Click(object sender, RoutedEventArgs e)
        {
            CommandSaveTranscriptionAs.Execute(null, this);
        }

        //Ukonceni aplikace z menu
        private void MSoubor_Konec_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //nacteni zvukoveho souboru - pro wav je vykreslena i vlna
        private void MSoubor_Otevrit_Zvukovy_Soubor_Click(object sender, RoutedEventArgs e)
        {
            button1_Click(null, new RoutedEventArgs());
        }
        #endregion

        #region menu Nastroje
        private void MNastroje_Nastav_Mluvciho_Click(object sender, RoutedEventArgs e)
        {
            CommandAssignSpeaker.Execute(null, null);
        }

        private void MNastroje_Nastaveni_Click(object sender, RoutedEventArgs e)
        {
            MySetup.Setup = WinSetup.WinSetupNastavit(MySetup.Setup, SpeakersDatabase);
            //pokus o ulozeni konfigurace
            MySetup.Setup.Serializovat(FilePaths.GetConfigFileWriteStream(), MySetup.Setup);
            waveform1.SmallJump = TimeSpan.FromSeconds(MySetup.Setup.VlnaMalySkok);
            InicializaceAudioPrehravace();  //nove nastaveni prehravaciho zarizeni 
        }

        private void MNapoveda_Popis_Programu_Click(object sender, RoutedEventArgs e)
        {
            CommandHelp.Execute(null, this);
        }

        private void MNapoveda_O_Programu_Click(object sender, RoutedEventArgs e)
        {
            CommandAbout.Execute(null, this);
        }

        #endregion

        //obsluha tlacitek v toolbaru u vlny....----------------------------------------------------------------------------
        private void Toolbar1Btn5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button pButton = sender as Button;
                TimeSpan pDelka = TimeSpan.FromMilliseconds(long.Parse(pButton.Tag.ToString()));
                TimeSpan pPocatek = waveform1.WaveBegin;
                TimeSpan pKonec = waveform1.WaveBegin + pDelka;
                if (pKonec < waveform1.WaveBegin)
                    pPocatek = waveform1.CaretPosition - TimeSpan.FromTicks(pDelka.Ticks / 2);

                if (pPocatek < TimeSpan.Zero) pPocatek = TimeSpan.Zero;
                pKonec = pPocatek + pDelka;
                timer1.IsEnabled = false;
                waveform1.WaveBegin = pPocatek;
                waveform1.WaveEnd = pKonec;
                waveform1.WaveLength = pDelka;
                timer1.IsEnabled = true;
            }
            catch
            {

            }
        }


        //osetreni ulozeni pri ukonceni aplikace
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
                            if (!UlozitTitulky(false, Transcription.FileName)) e.Cancel = true;
                        }
                        else
                        {
                            if (!UlozitTitulky(true, Transcription.FileName)) e.Cancel = true;
                        }
                    }

                    if (oknoNapovedy != null && !oknoNapovedy.IsLoaded)
                    {
                        oknoNapovedy.Close();
                    }


                }
                else if (mbr == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }


            }
            else if (oknoNapovedy != null && oknoNapovedy.IsLoaded)
            {
                oknoNapovedy.Close();
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


                //ulozeni databaze mluvcich - i externi databaze
                if (SpeakersDatabase != null)
                {
                    string fname = System.IO.Path.GetFullPath(MySetup.Setup.CestaDatabazeMluvcich);
                    if (fname.StartsWith(FilePaths.ProgramDirectory))//kdyz je to v adresari (program files..)
                    {
                        if (!FilePaths.WriteToAppData) //checkni jestli muzes zapisovat
                            SpeakersDatabase.Serialize(fname);
                        else
                            SpeakersDatabase.Serialize(FilePaths.AppDataDirectory + fname.Substring(FilePaths.ProgramDirectory.Length));
                    }
                    else //neni to u me neresit prava
                    {
                        try
                        {
                            SpeakersDatabase.Serialize(MySetup.Setup.CestaDatabazeMluvcich);
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

                //pokus o ulozeni nastaveni
                if (MySetup.Setup != null)
                {
                    if (this.WindowState == WindowState.Normal)
                    {
                        //nastaveni posledni zname souradnice okna a velikosti okna
                        MySetup.Setup.OknoPozice = new Point(this.Left, this.Top);
                        MySetup.Setup.OknoVelikost = new Size(this.Width, this.Height);
                    }
                    if (this.WindowState != WindowState.Minimized)
                    {
                        MySetup.Setup.OknoStav = this.WindowState;
                    }

                    MySetup.Setup.Serializovat(FilePaths.GetConfigFileWriteStream(), MySetup.Setup);

                }
            }

            //smazani souboru...
            FilePaths.DeleteTemp();

            Environment.Exit(0); // Vynuti ukonceni vsech vlaken a uvolneni prostredku


        }


        //zavre okno s videem a video
        private void btCloseVideo_Click(object sender, RoutedEventArgs e)
        {
            meVideo.Close();
            meVideo.Source = null;
            jeVideo = false;
            gListVideo.ColumnDefinitions[1].Width = new GridLength(1);
        }



        #region menu uprava
        private void MUpravy_Nova_Kapitola_Click(object sender, RoutedEventArgs e)
        {
            CommandNewChapter.Execute(null, null);
        }

        private void MUpravy_Nova_Sekce_Click(object sender, RoutedEventArgs e)
        {
            CommandNewSection.Execute(null, null);
        }

        private void MUpravy_Smazat_Polozku_Click(object sender, RoutedEventArgs e)
        {
            CommandDeleteElement.Execute(null, null);
        }

        #endregion



        //pokusi se zamerit textbox pri spousteni aplikace
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitCommands();
            LoadPlugins();
            //inicializuje (asynchronni) nacitani slovniku
            Thread t = new Thread(
                delegate()
                {
                    if (SpellChecker.LoadVocabulary())
                    {
                        this.Dispatcher.Invoke(new Action(
                    delegate()
                    {
                        //foreach (Element ee in VirtualizingListBox.listbox.VisualFindChildren<Element>())
                        //{
                        //    ee.editor.TextArea.TextView.LineTransformers.Remove();
                        //    ee.editor.TextArea.TextView.LineTransformers.Add();

                        //}
                    }
                    ));
                    }
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


            string pCesta = null;
            bool import = false;
            if (App.Startup_ARGS != null && App.Startup_ARGS.Length > 0)
            {
                if (App.Startup_ARGS[0] == "-i")
                {
                    import = true;
                    pCesta = App.Startup_ARGS[1];
                }
                else
                    pCesta = App.Startup_ARGS[0];
            }

            if (pCesta == null)
            {
                NewTranscription();
            }
            else
            {
                if (import)
                {
                    NewTranscription();
                    CommandImportFile.Execute(pCesta, this);
                }
                else
                    OpenTranscription(false, pCesta, false);
            }

            VirtualizingListBox.RequestTimePosition += delegate(out TimeSpan value) { value = waveform1.CaretPosition; };
            VirtualizingListBox.RequestPlaying += delegate(out bool value) { value = Playing; };
            VirtualizingListBox.RequestPlayPause += delegate() { CommandPlayPause.Execute(null, null); };



        }


        private void menuSouborExportovatClick(object sender, RoutedEventArgs e)
        {
            Plugin p = (Plugin)((MenuItem)sender).Tag;
            p.ExecuteExport(Transcription);
        }

        private void LoadSubtitlesData(WPFTranscription data)
        {
            if (data == null)
                return;
            Transcription = data;
            //nacteni audio souboru pokud je k dispozici
            if (!string.IsNullOrEmpty(Transcription.mediaURI))
            {
                FileInfo fiA = new FileInfo(Transcription.mediaURI);
                string pAudioFile = null;
                if (fiA.Exists)
                {
                    pAudioFile = fiA.FullName;
                }
                else if (System.IO.Path.IsPathRooted(Transcription.mediaURI))
                {
                    tbAudioSoubor.Text = Transcription.mediaURI;
                }
                else
                {
                    FileInfo fi = new FileInfo(Transcription.FileName);
                    pAudioFile = fi.Directory.FullName + "\\" + Transcription.mediaURI;
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
                        tbAudioSoubor.Text = Transcription.mediaURI;
                    }
                }
            }

            this.Title = MyKONST.NAZEV_PROGRAMU + " [" + data.FileName + "]";


        }

        private void menuSouborImportovatClick(object sender, RoutedEventArgs e)
        {

            Plugin p = (Plugin)((MenuItem)sender).Tag;
            WPFTranscription data = p.ExecuteImport();
            LoadSubtitlesData(data);
        }

        Thread Pedalthread = null;
        Process PedalProcess = null;
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





        private void btDiktat_Click(object sender, RoutedEventArgs e)
        {
            CommandStartStopDictate.Execute(null, this);
        }


        /// <summary>
        /// zobrazi zpravu o stavu programu ve spodni liste programu, je thread SAFE
        /// </summary>
        /// <param name="aZprava"></param>
        private void ZobrazStavProgramu(string aZprava)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                //do invoke stuff here
                this.Dispatcher.Invoke(new Action<string>(ZobrazStavProgramu), aZprava);
                return;
            }
            tbStavProgramu.Text = aZprava;
        }


        private void button10_Click(object sender, RoutedEventArgs e)
        {
            CommandGeneratePhoneticTranscription.Execute(null, this);
        }

        private void btHlasoveOvladani_Click(object sender, RoutedEventArgs e)
        {
            CommandStartStopVoiceControl.Execute(null, this);

        }


        /// <summary>
        /// porizeni obrazku z videa a poslani fotky do spravce mluvcich
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btPoriditObrazekZVidea_Click(object sender, RoutedEventArgs e)
        {
            CommandTakeSpeakerSnapshotFromVideo.Execute(null, this);
        }


        private void menuItemFonetickyPrepis_Click(object sender, RoutedEventArgs e)
        {
            CommandShowPanelFoneticTranscription.Execute(null, this);
        }


        /// <summary>
        /// delegat pro zobrazeni fonetickeho prepisu z jineho threadu
        /// </summary>
        /// <param name="aTag"></param>
        private delegate void ZobrazeniFonetickehoPrepisu(TranscriptionElement aTag);


        /// <summary>
        /// zobrazi nebo skryje foneticky prepis - respektive okno pro upravu
        /// </summary>
        /// <param name="aZobrazit"></param>
        /// <returns></returns>
        private bool ZobrazitOknoFonetickehoPrepisu(bool aZobrazit)
        {

            if (aZobrazit)
            {
                MySetup.Setup.ZobrazitFonetickyPrepis = Math.Abs(MySetup.Setup.ZobrazitFonetickyPrepis);

                d.RowDefinitions[1].Height = new GridLength(MySetup.Setup.ZobrazitFonetickyPrepis);

                return true;
            }
            else
            {
                MySetup.Setup.ZobrazitFonetickyPrepis = -Math.Abs(MySetup.Setup.ZobrazitFonetickyPrepis);
                d.RowDefinitions[1].Height = new GridLength(0);
                return false;
            }
        }

        private void btZavritFonPrepis_Click(object sender, RoutedEventArgs e)
        {
            ZobrazitOknoFonetickehoPrepisu(false);
        }

        private void gridSplitter2_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //      if (gPrepis.RowDefinitions[1].Height.Value > 40) MySetup.Setup.ZobrazitFonetickyPrepis = (float)gPrepis.RowDefinitions[1].Height.Value;
        }


        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            menuItemNastrojeFonetickyPrepis_Click(null, new RoutedEventArgs());

        }


        /// <summary>
        /// Normalizuje text
        /// </summary>
        /// <param name="aDokument">dokument s titulky, ktery bude normalizovan</param>
        /// <param name="aElement"></param>
        /// <param name="aIndexNormalizace">mensi nez 0, zavola okno pro vyber typu normalizace</param>
        /// <returns></returns>
        public int Normalizovat(TranscriptionElement aElement, int aIndexNormalizace)
        {
            throw new NotImplementedException();
        }

        private void btNormalizovat_Click(object sender, RoutedEventArgs e)
        {
            menuItemNastrojeNormalizovat_Click(null, new RoutedEventArgs());

        }

        private void menuItemNastrojeNormalizovat_Click(object sender, RoutedEventArgs e)
        {
            CommandNormalizeParagraph.Execute(null, this);
        }

        /// <summary>
        /// spustu automaticky foneticky prepis - THREAD SAFE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItemNastrojeFonetickyPrepis_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuItemNastrojeAllignment_Click(object sender, RoutedEventArgs e)
        {


        }

        private void btOdstranitNefonemy_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();

        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] p = e.Data.GetFormats();
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].ToLower() == "filenamew")
                {
                    string[] o = (string[])e.Data.GetData(p[i]);
                    OpenTranscription(false, o[0].ToString(), false);
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

        #region nerecove znacky

        private void toolBar1_Loaded(object sender, RoutedEventArgs e)
        {
            int index = 1;
            foreach (string s in MySetup.Setup.NerecoveUdalosti)
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

        #region undo
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
                Transcription.Serialize(ms, true, !MySetup.Setup.SaveInShortFormat);
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
                pIndexBufferuVlnyProPrehrani = (int)ts.TotalMilliseconds;
                if (jeVideo) meVideo.Position = ts;

                if (!(waveform1.SelectionBegin <= e.Value && waveform1.SelectionEnd >= e.Value))
                {
                    waveform1.SelectionBegin = new TimeSpan(-1);
                    waveform1.SelectionEnd = new TimeSpan(-1);
                }

            }
        }
        private void waveform1_CarretPostionChanged(object sender, Waveform.TimeSpanEventArgs e)
        {
            if (!Playing)
            {
                pIndexBufferuVlnyProPrehrani = (int)e.Value.TotalMilliseconds;
                oldms = TimeSpan.Zero;

                if (!(waveform1.SelectionBegin <= e.Value && waveform1.SelectionEnd >= e.Value))
                {
                    waveform1.SelectionBegin = new TimeSpan(-1);
                    waveform1.SelectionEnd = new TimeSpan(-1);
                }
            }


            var list = _mydatasource.VratElementDanehoCasu(e.Value);
            if (list != null && list.Count > 0)
            {
                if (VirtualizingListBox.ActiveTransctiption != list[0])
                {
                    VirtualizingListBox.ActiveTransctiption = list[0];
                    phoneticTranscription.ValueElement = list[0];

                }
            }
        }

        private void waveform1_CarretPostionChangedByUser(object sender, Waveform.TimeSpanEventArgs e)
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

            pIndexBufferuVlnyProPrehrani = (int)waveform1.CaretPosition.TotalMilliseconds;
            List<TranscriptionParagraph> pl = Transcription.VratElementDanehoCasu(waveform1.CaretPosition);

            _pozicenastav = true;
            var list = _mydatasource.VratElementDanehoCasu(e.Value);
            if (list != null && list.Count > 0)
            {
                if (VirtualizingListBox.ActiveTransctiption != list[0])
                    VirtualizingListBox.ActiveTransctiption = list[0];

            }
            _pozicenastav = false;
            VyberTextMeziCasovymiZnackami(e.Value);
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



        private void waveform1_ElementChanged(object sender, Waveform.MyTranscriptionElementEventArgs e)
        {
            //VirtualizingListBox.RecreateElements(VirtualizingListBox.gridscrollbar.Value);
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
            NastavPoziciKurzoru(VirtualizingListBox.ActiveTransctiption.Begin, true, true);
        }

        private void VirtualizingListBox_SetTimeRequest(TimeSpan obj)
        {
            if (Playing)
                CommandPlayPause.Execute(null, null);

            NastavPoziciKurzoru(obj, true, true);
            VyberTextMeziCasovymiZnackami(obj);
        }

        private void menuNastrojeVideoZobrazit_Click(object sender, RoutedEventArgs e)
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


        private void menuSouborImportovat_Click(object sender, RoutedEventArgs e)
        {
            CommandImportFile.Execute(null, null);
        }

        private void menuSouborExportovat_Click(object sender, RoutedEventArgs e)
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

        private void menuUpravySmazatNerecoveUdalosti_Click(object sender, RoutedEventArgs e)
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
            if (VirtualizingListBox.ActiveElement.RefreshSpeakerButton())
            {
                prev = VirtualizingListBox.GetVisualForTransctiption(VirtualizingListBox.ActiveTransctiption.Previous());//refresh previous elements
                while (prev != null && prev.RefreshSpeakerButton())
                    prev = VirtualizingListBox.GetVisualForTransctiption(prev.ValueElement.Previous());

                prev = VirtualizingListBox.GetVisualForTransctiption(VirtualizingListBox.ActiveTransctiption.Next());//refresh next elements
                while (prev != null && prev.RefreshSpeakerButton())
                    prev = VirtualizingListBox.GetVisualForTransctiption(prev.ValueElement.Next());

            }

        }

    }
}