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
using Microsoft.Win32;
using NanoTrans.Audio;
using NanoTrans.Properties;
using TranscriptionCore;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window
    {
        #region WPF commands...
        public static RoutedCommand CommandFindDialog = new RoutedCommand();
        public static RoutedCommand CommandPlayPause = new RoutedCommand();
        public static RoutedCommand CommandScrollUp = new RoutedCommand();
        public static RoutedCommand CommandScrollDown = new RoutedCommand();
        public static RoutedCommand CommandSmallJumpRight = new RoutedCommand();
        public static RoutedCommand CommandSmallJumpLeft = new RoutedCommand();
        public static RoutedCommand CommandMaximizeMinimize = new RoutedCommand();
        public static RoutedCommand CommandShowPanelFoneticTranscription = new RoutedCommand();
        public static RoutedCommand CommandGeneratePhoneticTranscription = new RoutedCommand();
        public static RoutedCommand CommandNormalizeParagraph = new RoutedCommand();
        public static RoutedCommand CommandRemoveNonphonemes = new RoutedCommand();
        public static RoutedCommand CommandTakeSpeakerSnapshotFromVideo = new RoutedCommand();
        public static RoutedCommand CommandCreateNewTranscription = new RoutedCommand();
        public static RoutedCommand CommandOpenTranscription = new RoutedCommand();
        public static RoutedCommand CommandSaveTranscription = new RoutedCommand();
        public static RoutedCommand CommandSaveTranscriptionAs = new RoutedCommand();
        public static RoutedCommand CommandHelp = new RoutedCommand();
        public static RoutedCommand CommandAbout = new RoutedCommand();


        public static RoutedCommand CommandNewSection = new RoutedCommand();
        public static RoutedCommand CommandInsertNewSection = new RoutedCommand();
        public static RoutedCommand CommandNewChapter = new RoutedCommand();
        public static RoutedCommand CommandDeleteElement = new RoutedCommand();
        public static RoutedCommand CommandAssignSpeaker = new RoutedCommand();
        public static RoutedCommand CommandExportElement = new RoutedCommand();
        public static RoutedCommand CommandQuickExportElement = new RoutedCommand();

        public static RoutedCommand CommandAssignElementStart = new RoutedCommand();
        public static RoutedCommand CommandAssignElementEnd = new RoutedCommand();
        public static RoutedCommand CommandAssignElementTimeSelection = new RoutedCommand();


        public static RoutedCommand CommandJumpToBegin = new RoutedCommand();
        public static RoutedCommand CommandJumpToEnd = new RoutedCommand();

        public static RoutedCommand CommandImportFile = new RoutedCommand();
        public static RoutedCommand CommandExportFile = new RoutedCommand();

        public static RoutedCommand CommandUndo = new RoutedCommand();
        public static RoutedCommand CommandRedo = new RoutedCommand();


        public void InitCommands()
        {
            this.CommandBindings.Add(new CommandBinding(CommandFindDialog, CFindDialogExecute));
            this.CommandBindings.Add(new CommandBinding(CommandPlayPause, CPlayPauseExecute));
            this.CommandBindings.Add(new CommandBinding(CommandScrollUp, CScrollUP));
            this.CommandBindings.Add(new CommandBinding(CommandScrollDown, CScrollDown));
            this.CommandBindings.Add(new CommandBinding(CommandSmallJumpRight, CSmallJumpRight));
            this.CommandBindings.Add(new CommandBinding(CommandSmallJumpLeft, CSmallJumpLeft));
            this.CommandBindings.Add(new CommandBinding(CommandMaximizeMinimize, CMaximizeMinimize));
            this.CommandBindings.Add(new CommandBinding(CommandShowPanelFoneticTranscription, CShowPanelFoneticTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandGeneratePhoneticTranscription, CGeneratePhoneticTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandTakeSpeakerSnapshotFromVideo, CTakeSpeakerSnapshotFromVideo));
            this.CommandBindings.Add(new CommandBinding(CommandCreateNewTranscription, CCreateNewTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandOpenTranscription, COpenTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandSaveTranscription, CSaveTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandSaveTranscriptionAs, CSaveTranscriptionAs));
            this.CommandBindings.Add(new CommandBinding(CommandHelp, CHelp));
            this.CommandBindings.Add(new CommandBinding(CommandAbout, CAbout));


            this.CommandBindings.Add(new CommandBinding(CommandNewSection, CNewSection));
            this.CommandBindings.Add(new CommandBinding(CommandInsertNewSection, CInsertNewSection));
            this.CommandBindings.Add(new CommandBinding(CommandNewChapter, CNewChapter));
            this.CommandBindings.Add(new CommandBinding(CommandDeleteElement, CDeleteElement));
            this.CommandBindings.Add(new CommandBinding(CommandAssignSpeaker, CAssignSpeaker));
            this.CommandBindings.Add(new CommandBinding(CommandExportElement, CExportElement));
            this.CommandBindings.Add(new CommandBinding(CommandQuickExportElement, CQuickExportElement));
            this.CommandBindings.Add(new CommandBinding(CommandAssignElementStart, CAssignElementStart));
            this.CommandBindings.Add(new CommandBinding(CommandAssignElementEnd, CAssignElementEnd));
            this.CommandBindings.Add(new CommandBinding(CommandAssignElementTimeSelection, CAssignElementTimeSelection));


            this.CommandBindings.Add(new CommandBinding(CommandJumpToBegin, CJumpToBegin));
            this.CommandBindings.Add(new CommandBinding(CommandJumpToEnd, CJumpToEnd));

            this.CommandBindings.Add(new CommandBinding(CommandImportFile, CImportFile));
            this.CommandBindings.Add(new CommandBinding(CommandExportFile, CExportFile));

            this.CommandBindings.Add(new CommandBinding(CommandUndo, CUndo));
            this.CommandBindings.Add(new CommandBinding(CommandRedo, CRedo));



            CommandJumpToBegin.InputGestures.Add(new KeyGesture(Key.Home, ModifierKeys.Control));
            CommandJumpToEnd.InputGestures.Add(new KeyGesture(Key.End, ModifierKeys.Control));

            CommandImportFile.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift));
            CommandExportFile.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift));


            CommandAssignElementStart.InputGestures.Add(new KeyGesture(Key.Home, ModifierKeys.Alt));
            CommandAssignElementEnd.InputGestures.Add(new KeyGesture(Key.End, ModifierKeys.Alt));

            CommandNewSection.InputGestures.Add(new KeyGesture(Key.F5));
            CommandInsertNewSection.InputGestures.Add(new KeyGesture(Key.F5, ModifierKeys.Shift));
            CommandNewChapter.InputGestures.Add(new KeyGesture(Key.F4));
            CommandDeleteElement.InputGestures.Add(new KeyGesture(Key.Delete, ModifierKeys.Shift));
            CommandAssignSpeaker.InputGestures.Add(new KeyGesture(Key.M, ModifierKeys.Control));

            CommandExportElement.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Alt));
            CommandQuickExportElement.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift));

            CommandFindDialog.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            CommandFindDialog.InputGestures.Add(new KeyGesture(Key.F3));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Shift));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift));
            CommandScrollDown.InputGestures.Add(new KeyGesture(Key.PageDown));
            CommandScrollUp.InputGestures.Add(new KeyGesture(Key.PageUp));
            CommandSmallJumpRight.InputGestures.Add(new KeyGesture(Key.Right, ModifierKeys.Alt));
            CommandSmallJumpLeft.InputGestures.Add(new KeyGesture(Key.Left, ModifierKeys.Alt));
            CommandMaximizeMinimize.InputGestures.Add(new KeyGesture(Key.Return, ModifierKeys.Alt));
            CommandGeneratePhoneticTranscription.InputGestures.Add(new KeyGesture(Key.F5));
            CommandTakeSpeakerSnapshotFromVideo.InputGestures.Add(new KeyGesture(Key.F12));
            CommandCreateNewTranscription.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            CommandOpenTranscription.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandSaveTranscription.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandAbout.InputGestures.Add(new KeyGesture(Key.F1, ModifierKeys.Control));
            CommandHelp.InputGestures.Add(new KeyGesture(Key.F1));

            CommandUndo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control));
            CommandRedo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));

        }

        private void CRedo(object sender, ExecutedRoutedEventArgs e)
        {
            Transcription.Redo();
        }

        private void CUndo(object sender, ExecutedRoutedEventArgs e)
        {
            Transcription.Undo();
        }

        private void CImportFile(object sender, ExecutedRoutedEventArgs e)
        {

            if (!Settings.Default.FeatureEnabler.LocalEdit)
                return;

            string path = e.Parameter as string;

            var imp = ImportTranscription(path);

            if (imp != null)
                LoadTranscription(imp);
            else
                MessageBox.Show(Properties.Strings.MessageBoxImportError, Properties.Strings.MessageBoxErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);

        }

        private WPFTranscription ImportTranscription(string filepath = null)
        {
            string[] masks = _ImportPlugins.Select(p => p.Mask).ToArray();
            string[] filetypes = masks.SelectMany(m => m.Split('|').Where((p, i) => i % 2 == 1).SelectMany(ex => ex.Split(';'))).Distinct().ToArray();

            string allfilesMask = string.Format(Properties.Strings.FileDialogLoadImportFilter, string.Join(";", filetypes));
            OpenFileDialog opf = new OpenFileDialog();
            opf.CheckFileExists = true;
            opf.CheckPathExists = true;
            opf.Filter = string.Join("|", new[] { allfilesMask }.Concat(masks));
            opf.Title = Properties.Strings.FileDialogLoadImportTitle;
            bool filedialogopened = false;

            if (filepath != null)
            {
                opf.FilterIndex = 1;
                filedialogopened = true;
                opf.FileName = filepath;
            }
            else
                filedialogopened = opf.ShowDialog() == true;

            WPFTranscription trans = null;

            if (filedialogopened)
            {
                if (opf.FilterIndex == 1) //all files
                {
                    var plugins = _ImportPlugins.Where(
                        p => p.Mask.Split('|')
                            .Where((s, i) => i % 2 == 1)
                            .Any(s => Regex.IsMatch(System.IO.Path.GetExtension(opf.FileName), string.Join("|", s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Replace("*", "").Replace(".", "\\.").Replace('?', '.') + "$"))))).ToArray();

                    if (plugins.Length != 1)
                    {
                        PickOneDialog pd = new PickOneDialog(plugins.Select(p => p.Name).ToList(), Properties.Strings.ImportSelectImportPlugin);
                        if (pd.ShowDialog() == true)
                        {
                            trans = plugins[pd.SelectedIndex].ExecuteImport(opf.FileName);
                        }
                    }
                    else
                    {
                        trans = plugins[0].ExecuteImport(opf.FileName);
                    }

                }
                else
                {
                    trans = _ImportPlugins[opf.FilterIndex - 2].ExecuteImport(opf.FileName);
                }

                if (trans != null)
                {
                    trans.FileName += ".trsx";
                }
            }

            return trans;
        }

        private void CExportFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Settings.Default.FeatureEnabler.Export)
                return;

            string[] masks = _ExportPlugins.Select(p => p.Mask).ToArray();

            SaveFileDialog sf = new SaveFileDialog();
            if (Transcription.FileName != null)
                sf.InitialDirectory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Transcription.FileName));
            sf.FileName = System.IO.Path.GetFileNameWithoutExtension(Transcription.FileName);

            sf.AddExtension = true;
            sf.CheckPathExists = true;
            sf.Filter = string.Join("|", masks);

            if (sf.ShowDialog() == true)
            {
                _ExportPlugins[sf.FilterIndex - 1].ExecuteExport(Transcription, sf.FileName);
            }
        }


        private void CJumpToBegin(object sender, ExecutedRoutedEventArgs e)
        {
            VirtualizingListBox.ActiveTransctiption = VirtualizingListBox.Transcription.First();
        }

        private void CJumpToEnd(object sender, ExecutedRoutedEventArgs e)
        {
            VirtualizingListBox.ActiveTransctiption = VirtualizingListBox.Transcription.Last();
        }


        private bool WarnIfElementIsShort(TimeSpan T1, TimeSpan T2)
        {
            if ((T1 - T2).Duration() <= TimeSpan.FromMilliseconds(100))
            {
                MessageBox.Show(Properties.Strings.MessageBoxTooShortParagraph, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }

            return false;
        }
        private void CAssignElementStart(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption == null)
                return;

            if (WarnIfElementIsShort(VirtualizingListBox.ActiveTransctiption.End, waveform1.CaretPosition))
                return;

            VirtualizingListBox.ActiveTransctiption.Begin = waveform1.CaretPosition;
            waveform1.InvalidateSpeakers();
        }

        private void CAssignElementEnd(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption == null)
                return;

            if (WarnIfElementIsShort(VirtualizingListBox.ActiveTransctiption.Begin, waveform1.CaretPosition))
                return;

            VirtualizingListBox.ActiveTransctiption.End = waveform1.CaretPosition;
            waveform1.InvalidateSpeakers();
        }
        private void CAssignElementTimeSelection(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption == null)
                return;

            if (WarnIfElementIsShort(waveform1.SelectionBegin, waveform1.SelectionEnd))
                return;


            VirtualizingListBox.ActiveTransctiption.Begin = waveform1.SelectionBegin;
            VirtualizingListBox.ActiveTransctiption.End = waveform1.SelectionEnd;
            waveform1.InvalidateSpeakers();
        }

        #region mainform actions

        private void CNewSection(object sender, ExecutedRoutedEventArgs e)
        {
            TranscriptionSection s = new TranscriptionSection(Properties.Strings.DefaultSectionText);
            TranscriptionParagraph p = new TranscriptionParagraph();
            p.Add(new TranscriptionPhrase());
            Transcription.Add(s);
            s.Add(p);
            VirtualizingListBox.ActiveTransctiption = s;
        }

        private void CInsertNewSection(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption != null)
            {
                if (VirtualizingListBox.ActiveTransctiption.IsParagraph)
                {
                    TranscriptionParagraph p = (TranscriptionParagraph)VirtualizingListBox.ActiveTransctiption;
                    int idx = p.ParentIndex;
                    TranscriptionSection sec = (TranscriptionSection)p.Parent;
                    TranscriptionSection s = new TranscriptionSection(Properties.Strings.DefaultSectionText);
                    for (int i = idx; i < sec.Children.Count; i++)
                        s.Add(sec[i]);

                    sec.Children.RemoveRange(idx, sec.Children.Count - idx);

                    sec.Parent.Insert(sec.ParentIndex + 1, s);
                    VirtualizingListBox.ActiveTransctiption = s;

                    VirtualizingListBox.Reset();
                }
                else if (VirtualizingListBox.ActiveTransctiption.IsSection)
                {
                    var s = new TranscriptionSection(Properties.Strings.DefaultSectionText);
                    s.Children.AddRange(VirtualizingListBox.ActiveTransctiption.Children);
                    VirtualizingListBox.ActiveTransctiption.Children.Clear();
                    VirtualizingListBox.ActiveTransctiption.Parent.Insert(VirtualizingListBox.ActiveTransctiption.ParentIndex, s);
                    VirtualizingListBox.ActiveTransctiption = s;
                    VirtualizingListBox.Reset();
                }
                else if (VirtualizingListBox.ActiveTransctiption.IsChapter)
                {
                    var s = new TranscriptionSection(Properties.Strings.DefaultSectionText);
                    VirtualizingListBox.ActiveTransctiption.Insert(0, s);
                    VirtualizingListBox.ActiveTransctiption = s;
                    VirtualizingListBox.Reset();
                }
            }
        }
        private void CNewChapter(object sender, ExecutedRoutedEventArgs e)
        {
            TranscriptionChapter c = new TranscriptionChapter(Properties.Strings.DefaultChapterText);
            TranscriptionSection s = new TranscriptionSection(Properties.Strings.DefaultSectionText);
            TranscriptionParagraph p = new TranscriptionParagraph();
            p.Add(new TranscriptionPhrase());
            Transcription.Add(c);
            c.Add(s);
            s.Add(p);
            VirtualizingListBox.ActiveTransctiption = c;
        }
        private void CDeleteElement(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption != null && VirtualizingListBox.ActiveTransctiption.Parent != null)
            {
                VirtualizingListBox.ActiveTransctiption.Parent.Remove(VirtualizingListBox.ActiveTransctiption);
            }
        }

        double? smwidth = null;
        double? smheight = null;
        double? smleft = null;
        double? smtop = null;

        private void CAssignSpeaker(object sender, ExecutedRoutedEventArgs e)
        {
            TranscriptionParagraph tpr = null;
            if (sender is TranscriptionParagraph)
                tpr = (TranscriptionParagraph)sender;
            else
                tpr = VirtualizingListBox.ActiveTransctiption as TranscriptionParagraph;

            var mgr = new SpeakersManager(tpr.Speaker, Transcription, Transcription.Speakers, SpeakersDatabase) { MessageLabel = Properties.Strings.mainWindowSpeakersManagerSelectedParagraphMessage, Message = VirtualizingListBox.ActiveTransctiption.Text };
            if (smwidth != null && smheight != null && smleft != null && smtop != null)
            {
                mgr.Width = smwidth.Value;
                mgr.Height = smheight.Value;
                mgr.Left = smleft.Value;
                mgr.Top = smtop.Value;
            }

            if (mgr.ShowDialog() == true && mgr.SelectedSpeaker != null)
            {
                Transcription.BeginUpdate();
                var origspk = tpr.Speaker;
                tpr.Speaker = mgr.SelectedSpeaker;

                if (!Transcription.EnumerateParagraphs().Any(p => p.Speaker == origspk))
                    Transcription.Speakers.Remove(origspk);


                var replaced = AdvancedSpeakerCollection.SynchronizedAdd(Transcription.Speakers, mgr.SelectedSpeaker);

                if (replaced != null)
                {
                    foreach (var p in Transcription.EnumerateParagraphs().Where(p => p.Speaker == replaced))
                        p.Speaker = mgr.SelectedSpeaker;
                }

                Transcription.EndUpdate();
            }

            smwidth = mgr.Width;
            smheight = mgr.Height;
            smleft = mgr.Left;
            smtop = mgr.Top;

            if (mgr.SpeakerChanged)//refresh all sepakers
            {
                var pinned = SpeakersDatabase.Where(s => s.PinnedToDocument);
                Transcription.Speakers.AddRange(pinned.Except(Transcription.Speakers));
            }

            VirtualizingListBox.SpeakerChanged(VirtualizingListBox.ActiveElement);
        }


        private void CQuickExportElement(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Settings.Default.FeatureEnabler.QuickExport)
                return;
            using (new WaitCursor())
            {
                TranscriptionParagraph par = VirtualizingListBox.ActiveTransctiption as TranscriptionParagraph;
                var data = _WavReader.LoadaudioDataBuffer(par.Begin, par.End);

                if (FilePaths.QuickSaveDirectory == null || !Directory.Exists(FilePaths.QuickSaveDirectory))
                {
                    MessageBox.Show(Properties.Strings.QuickSavePathNotSpecifiedError, Properties.Strings.QuickSavePathNotSpecifiedError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DirectoryInfo nfo = new DirectoryInfo(FilePaths.QuickSaveDirectory);

                int index = 0;
                var pars = nfo.GetFiles("*.wav").Where(p => p.Name.StartsWith("paragraph_")).ToArray();

                if (pars.Count() > 0)
                    index = 1 + (int)pars.Max(p => { int res = 0; int.TryParse(System.IO.Path.GetFileNameWithoutExtension(p.Name.Substring(10)), out res); return res; });

                var basename = System.IO.Path.Combine(nfo.FullName, "paragraph_" + index);
                WavReader.SaveToWav(basename + ".wav", data);
                string textf = basename + ".txt";
                //File.WriteAllBytes(textf, win1250.GetBytes());

                File.WriteAllText(textf, par.Text);

                if (!string.IsNullOrEmpty(par.Phonetics))
                {
                    textf = basename + ".phn";
                    File.WriteAllText(textf, par.Phonetics);
                }

                SystemSounds.Asterisk.Play();

            }
        }

        private void CExportElement(object sender, ExecutedRoutedEventArgs e)
        {

            TranscriptionParagraph par = VirtualizingListBox.ActiveTransctiption as TranscriptionParagraph;
            var data = _WavReader.LoadaudioDataBuffer(par.Begin, par.End);


            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "wav soubory (.wav)|*.wav";
            if (dlg.ShowDialog() == true)
            {
                WavReader.SaveToWav(dlg.FileName, data);

                string ext = System.IO.Path.GetExtension(dlg.FileName);
                dlg.FileName = dlg.FileName.Substring(0, dlg.FileName.Length - ext.Length);
                string textf = dlg.FileName + ".txt";

                File.WriteAllText(textf, par.Text);


                if (!string.IsNullOrEmpty(par.Phonetics))
                {
                    textf = dlg.FileName + ".phn";
                    File.WriteAllText(textf, par.Phonetics);
                }
            }
        }

        private void CFindDialogExecute(object sender, ExecutedRoutedEventArgs e)
        {
            if (_findDialog == null || !_findDialog.IsLoaded || !_findDialog.IsVisible)
            {
                _findDialog = new FindDialog(this);
                _findDialog.Owner = this;
                searchtextoffset = 0;
                _findDialog.Show();
            }
            else
            {
                _findDialog.SearchNext();
            }
        }

        private void CPlayPauseExecute(object sender, ExecutedRoutedEventArgs e)
        {
            if (MWP == null)
                InitializeAudioPlayer();

            if (MWP == null)
                return;

            if (_playing)
            {
                if (videoAvailable) meVideo.Pause();
                PlayingSelection = false;
                Playing = false;

                waveform1.CaretPosition = MWP.PausedAt;

            }
            else
            {
                // waveform1.Invalidate();
                bool adjustspeed = false;
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift || ToolBar2BtnSlow.IsChecked == true)
                {
                    adjustspeed = true;
                    meVideo.SpeedRatio = Settings.Default.SlowedPlaybackSpeed;
                }
                else
                {
                    meVideo.SpeedRatio = 1.0;
                }

                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {

                    if (waveform1.WaveBegin > waveform1.SelectionEnd || waveform1.WaveEnd < waveform1.SelectionBegin)
                    {
                        waveform1.SelectionBegin = VirtualizingListBox.ActiveTransctiption.Begin;
                        waveform1.SelectionEnd = VirtualizingListBox.ActiveTransctiption.End;
                    }


                    PlayingSelection = true;
                    oldms = TimeSpan.Zero;


                    if ((waveform1.SelectionBegin - waveform1.SelectionEnd).Duration() > TimeSpan.FromMilliseconds(100))
                    {
                        SetCaretPosition(waveform1.SelectionBegin);
                    }
                    else
                    {
                        TranscriptionElement par = VirtualizingListBox.ActiveTransctiption;
                        TimeSpan konec = par.End + TimeSpan.FromMilliseconds(5);
                        SetCaretPosition(konec);
                        waveform1.SelectionBegin = konec;
                        waveform1.SelectionEnd = konec + TimeSpan.FromMilliseconds(120000);
                    }

                }

                meVideo.Position = waveform1.CaretPosition;
                if (videoAvailable) meVideo.Play();
                //spusteni prehravani pomoci tlacitka-kvuli nacteni primeho prehravani

                Playing = true; //TODO: MWP and Playing are not properly linked - you have to set Playing to true before calling play

                if (adjustspeed)
                    MWP.Play(Settings.Default.SlowedPlaybackSpeed);
                else
                    MWP.Play();

            }
        }

        private void CScrollDown(object sender, ExecutedRoutedEventArgs e)
        {
            // VirtualizingListBox.gridscrollbar.Value += 0.7 * VirtualizingListBox.ActualHeight;
            SetCursor();
        }

        private void SetCursor()
        {
            UpdateLayout();
            HitTestResult res = VisualTreeHelper.HitTest(VirtualizingListBox, new Point(0, 5));
            if (res.VisualHit != null)
            {
                Element e = res.VisualHit.VisualFindParent<Element>();
                if (e != null)
                {
                    e.editor.Focus();
                    e.editor.CaretOffset = 0;
                }
            }
        }

        private void CScrollUP(object sender, ExecutedRoutedEventArgs e)
        {
            // VirtualizingListBox.gridscrollbar.Value -= 0.7 * VirtualizingListBox.ActualHeight;

        }

        private void CSmallJumpRight(object sender, ExecutedRoutedEventArgs e)
        {
            var ply = Playing;
            if (ply)
                CommandPlayPause.Execute(null, null);

            var newtime = waveform1.CaretPosition + waveform1.SmallJump;
            SetCaretPosition(newtime);
            SelectTextBetweenTimeOffsets(waveform1.CaretPosition);

            if (ply)
                CommandPlayPause.Execute(null, null);
        }

        private void CSmallJumpLeft(object sender, ExecutedRoutedEventArgs e)
        {
            var ply = Playing;
            if (ply)
                CommandPlayPause.Execute(null, null);

            var newtime = waveform1.CaretPosition - waveform1.SmallJump;
            if (newtime < TimeSpan.Zero)
                newtime = TimeSpan.Zero;

            SetCaretPosition(newtime);
            SelectTextBetweenTimeOffsets(waveform1.CaretPosition);

            if (ply)
                CommandPlayPause.Execute(null, null);
        }

        private void CMaximizeMinimize(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void CShowPanelFoneticTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Settings.Default.FeatureEnabler.PhoneticEditation)
                return;

            Settings.Default.PhoneticsPanelVisible = true;
        }

        private void CGeneratePhoneticTranscription(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void CNormalizeParagraph(object sender, ExecutedRoutedEventArgs e)
        {
            NormalizePhonetics(VirtualizingListBox.ActiveTransctiption, -1);
        }


        private void CTakeSpeakerSnapshotFromVideo(object sender, ExecutedRoutedEventArgs e)
        {
            if (videoAvailable)
            {

                Size dpi = new Size(96, 96);

                RenderTargetBitmap bmp = new RenderTargetBitmap((int)gVideoPouze.ActualWidth, (int)gVideoPouze.ActualHeight + (int)gVideoPouze.Margin.Top, dpi.Width, dpi.Height, PixelFormats.Pbgra32);
                bmp.Render(gVideoPouze);


                BitmapFrame pFrame = BitmapFrame.Create(bmp);

                string pBase = Const.JpgToBase64(pFrame);


                throw new NotImplementedException();
            }
            else
            {
                MessageBox.Show(Properties.Strings.MessageBoxCannotTakeSnapshotVideoNotLoaded, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
        private async void CCreateNewTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Settings.Default.FeatureEnabler.LocalEdit)
                return;
            await NewTranscription();
        }

        private async void COpenTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Settings.Default.FeatureEnabler.LocalEdit)
                return;

            e.Handled = true;
            await OpenTranscription(true, "");

        }

        private async void CSaveTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            if (Transcription != null)
            {
                if (Transcription.FileName != null)
                {
                    await SaveTranscription(false, Transcription.FileName);
                }
                else
                {
                    await SaveTranscription(true, Transcription.FileName);
                }
            }
        }

        private async void CSaveTranscriptionAs(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Settings.Default.FeatureEnabler.LocalEdit)
                return;

            await SaveTranscription(true, "");
        }

        private void CAbout(object sender, ExecutedRoutedEventArgs e)
        {
            new WinOProgramu(Const.APP_NAME).ShowDialog();
        }

        private void CHelp(object sender, ExecutedRoutedEventArgs e)
        {
            if (helpWindow == null || !helpWindow.IsLoaded)
            {
                helpWindow = new WinHelp();
                helpWindow.Show();
            }
        }

        int searchtextoffset = 0;
        public void FindNext(string pattern, bool isregex, bool CaseSensitive, bool searchinspeakers)
        {
            //foreach (Element e in VirtualizingListBox.gridstack.Children)
            //{
            //    e.editor.SelectionLength = 0;
            //}

            if (VirtualizingListBox.ActiveTransctiption == null)
                if (_transcription.Chapters.Count > 0)
                {
                    VirtualizingListBox.ActiveTransctiption = _transcription.Chapters[0];
                }
                else
                    return;
            TranscriptionElement tag = VirtualizingListBox.ActiveTransctiption;

            TranscriptionElement pr = tag;
            if (pr == null)
                tag = Transcription.Chapters[0];

            int len;
            if (Transcription.FindNext(ref tag, ref searchtextoffset, out len, pattern, isregex, CaseSensitive, searchinspeakers))
            {
                TranscriptionElement p = tag;
                waveform1.CaretPosition = p.Begin;
                VirtualizingListBox.ActiveTransctiption = p;

                if (VirtualizingListBox.ActiveElement != null)
                {
                    VirtualizingListBox.ActiveElement.SetSelection(searchtextoffset, len, 0);
                    searchtextoffset += len;
                }
            }
            else
            {
                if (MessageBox.Show(Properties.Strings.MessageBoxSearchTryFromBegining, Properties.Strings.MessageBoxSearchCaption, MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                {
                    var first = _transcription.First();
                    VirtualizingListBox.ActiveTransctiption = first;
                    FindNext(pattern, isregex, CaseSensitive, searchinspeakers);
                }

            }
        }

        public FindDialog _findDialog;
        #endregion


        #endregion

        /// <summary>
        /// obsluha stisku klaves a zkratek k ovladani programu - klavesa pustena 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F10:
                    if (e != null)
                        e.Handled = true;
                    break;
                default:
                    break;
            }
        }
    }


}