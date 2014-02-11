﻿using System;
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


namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window
    {
        #region WPF commandy...
        public static RoutedCommand CommandFindDialog = new RoutedCommand();
        public static RoutedCommand CommandPlayPause = new RoutedCommand();
        public static RoutedCommand CommandScrollUp = new RoutedCommand();
        public static RoutedCommand CommandScrollDown = new RoutedCommand();
        public static RoutedCommand CommandSmallJumpRight = new RoutedCommand();
        public static RoutedCommand CommandSmallJumpLeft = new RoutedCommand();
        public static RoutedCommand CommandMaximizeMinimize = new RoutedCommand();
        public static RoutedCommand CommandAutomaticFoneticTranscription = new RoutedCommand();
        public static RoutedCommand CommandShowPanelFoneticTranscription = new RoutedCommand();
        public static RoutedCommand CommandGeneratePhoneticTranscription = new RoutedCommand();
        public static RoutedCommand CommandStartStopDictate = new RoutedCommand();
        public static RoutedCommand CommandStartStopVoiceControl = new RoutedCommand(); 
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
        public static RoutedCommand CommandNewChapter= new RoutedCommand();
        public static RoutedCommand CommandDeleteElement = new RoutedCommand();
        public static RoutedCommand CommandAssignSpeaker = new RoutedCommand();
        public static RoutedCommand CommandExportElement = new RoutedCommand();

        public static RoutedCommand CommandAssignElementStart = new RoutedCommand();
        public static RoutedCommand CommandAssignElementEnd = new RoutedCommand();
        public static RoutedCommand CommandAssignElementTimeSelection = new RoutedCommand();


        public static RoutedCommand CommandJumpToBegin = new RoutedCommand();
        public static RoutedCommand CommandJumpToEnd = new RoutedCommand();

        public static RoutedCommand CommandImportFile = new RoutedCommand();
        public static RoutedCommand CommandExportFile = new RoutedCommand();


        public void InitCommands()
        {
            this.CommandBindings.Add(new CommandBinding(CommandFindDialog, CFindDialogExecute));
            this.CommandBindings.Add(new CommandBinding(CommandPlayPause, CPlayPauseExecute));
            this.CommandBindings.Add(new CommandBinding(CommandScrollUp, CScrollUP));
            this.CommandBindings.Add(new CommandBinding(CommandScrollDown, CScrollDown));
            this.CommandBindings.Add(new CommandBinding(CommandSmallJumpRight, CSmallJumpRight));
            this.CommandBindings.Add(new CommandBinding(CommandSmallJumpLeft, CSmallJumpLeft));
            this.CommandBindings.Add(new CommandBinding(CommandMaximizeMinimize, CMaximizeMinimize));
            this.CommandBindings.Add(new CommandBinding(CommandAutomaticFoneticTranscription, CAutomaticFoneticTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandShowPanelFoneticTranscription, CShowPanelFoneticTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandGeneratePhoneticTranscription, CGeneratePhoneticTranscription));
            this.CommandBindings.Add(new CommandBinding(CommandStartStopDictate, CStartStopDictate));
            this.CommandBindings.Add(new CommandBinding(CommandStartStopVoiceControl, CStartStopVoiceControl));
            this.CommandBindings.Add(new CommandBinding(CommandNormalizeParagraph, CNormalizeParagraph));
            this.CommandBindings.Add(new CommandBinding(CommandRemoveNonphonemes, CRemoveNonphonemes));
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
            this.CommandBindings.Add(new CommandBinding(CommandAssignElementStart, CAssignElementStart));
            this.CommandBindings.Add(new CommandBinding(CommandAssignElementEnd,CAssignElementEnd));
            this.CommandBindings.Add(new CommandBinding(CommandAssignElementTimeSelection,CAssignElementTimeSelection));


            this.CommandBindings.Add(new CommandBinding(CommandJumpToBegin,CJumpToBegin));
            this.CommandBindings.Add(new CommandBinding(CommandJumpToEnd, CJumpToEnd));

            this.CommandBindings.Add(new CommandBinding(CommandImportFile, CImportFile));
            this.CommandBindings.Add(new CommandBinding(CommandExportFile, CExportFile));

            CommandJumpToBegin.InputGestures.Add(new KeyGesture(Key.Home, ModifierKeys.Control));
            CommandJumpToEnd.InputGestures.Add(new KeyGesture(Key.End, ModifierKeys.Control));

            CommandImportFile.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift));
            CommandExportFile.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift));


            CommandAssignElementStart.InputGestures.Add(new KeyGesture(Key.Home,ModifierKeys.Alt));
            CommandAssignElementEnd.InputGestures.Add(new KeyGesture(Key.End, ModifierKeys.Alt));
           // CommandAssignElementTimeSelection.InputGestures.Add(new KeyGesture());

            CommandNewSection.InputGestures.Add(new KeyGesture(Key.F5));
            CommandInsertNewSection.InputGestures.Add(new KeyGesture(Key.F5,ModifierKeys.Shift));
            CommandNewChapter.InputGestures.Add(new KeyGesture(Key.F4));
            CommandDeleteElement.InputGestures.Add(new KeyGesture(Key.Delete,ModifierKeys.Shift));
            CommandAssignSpeaker.InputGestures.Add(new KeyGesture(Key.M,ModifierKeys.Control));
            CommandExportElement.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Shift));

            CommandFindDialog.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            CommandFindDialog.InputGestures.Add(new KeyGesture(Key.F3));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Shift));
            CommandPlayPause.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift));
            CommandScrollDown.InputGestures.Add(new KeyGesture(Key.PageDown));
            CommandScrollUp.InputGestures.Add(new KeyGesture(Key.PageUp));
            CommandSmallJumpRight.InputGestures.Add(new KeyGesture(Key.Right,ModifierKeys.Alt));
            CommandSmallJumpLeft.InputGestures.Add(new KeyGesture(Key.Left, ModifierKeys.Alt));
            CommandMaximizeMinimize.InputGestures.Add(new KeyGesture(Key.Return, ModifierKeys.Alt));
            CommandAutomaticFoneticTranscription.InputGestures.Add(new KeyGesture(Key.F10, ModifierKeys.Alt));
            CommandGeneratePhoneticTranscription.InputGestures.Add(new KeyGesture(Key.F5));
            CommandStartStopDictate.InputGestures.Add(new KeyGesture(Key.F6));
            CommandStartStopVoiceControl.InputGestures.Add(new KeyGesture(Key.F7));
            CommandNormalizeParagraph.InputGestures.Add(new KeyGesture(Key.F9));
            CommandRemoveNonphonemes.InputGestures.Add(new KeyGesture(Key.F11));
            CommandTakeSpeakerSnapshotFromVideo.InputGestures.Add(new KeyGesture(Key.F12));
            CommandCreateNewTranscription.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            CommandOpenTranscription.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandSaveTranscription.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandAbout.InputGestures.Add(new KeyGesture(Key.F1, ModifierKeys.Control));
            CommandHelp.InputGestures.Add(new KeyGesture(Key.F1));

        }

        private void CImportFile(object sender, ExecutedRoutedEventArgs e)
        {
            
            string[] masks = m_ImportPlugins.Select(p => p.Mask).ToArray();
            string[] filetypes = masks.SelectMany(m=>m.Split('|').Where((p,i)=>i%2==1).SelectMany(ex=>ex.Split(';'))).Distinct().ToArray();

            string allfilesMask = string.Format("Soubory všech importovatelných typů({0})|{0}",string.Join(";",filetypes));
            OpenFileDialog opf = new OpenFileDialog();
            opf.CheckFileExists = true;
            opf.CheckPathExists = true;
            opf.Filter = string.Join("|", new[] { allfilesMask }.Concat(masks));

            bool filedialogopened = false;
            if (e.Parameter is string)
            {
                filedialogopened = true;
                opf.FilterIndex = 1;
                opf.FileName = (string)e.Parameter;
            }else
                filedialogopened = opf.ShowDialog() == true;

            if (filedialogopened)
            {
                if (opf.FilterIndex == 1) //vsechny soubory
                {
                    var plugins = m_ImportPlugins.Where(p => p.Mask.Split('|').Where((s, i) => i % 2 == 1).Any(s => s.Contains(System.IO.Path.GetExtension(opf.FileName)))).ToArray();


                    
                    if (plugins.Length != 1)
                    {
                        PickOneDialog pd = new PickOneDialog(plugins.Select(p=>p.Name).ToList(),"Soubor s danou příponou je možné importovat více pluginy, vyberte ten správný");
                        if (pd.ShowDialog() == true)
                        {
                             LoadSubtitlesData(plugins[pd.SelectedIndex].ExecuteImport(opf.FileName));
                        }
                    }
                    else
                    {
                        LoadSubtitlesData(plugins[0].ExecuteImport(opf.FileName));
                    }
                    
                }
                else
                {
                    LoadSubtitlesData( m_ImportPlugins[opf.FilterIndex - 2].ExecuteImport(opf.FileName));
                }
            }
        }

        private void CExportFile(object sender, ExecutedRoutedEventArgs e)
        {
            string[] masks = m_ExportPlugins.Select(p => p.Mask).ToArray();

            SaveFileDialog sf = new SaveFileDialog();

            sf.CheckPathExists = true;
            sf.Filter = string.Join("|",masks);

            if (sf.ShowDialog() == true)
            {
                m_ExportPlugins[sf.FilterIndex - 1].ExecuteExport(myDataSource,sf.FileName);
            }
        }


        private void CJumpToBegin(object sender, ExecutedRoutedEventArgs e)
        {
           VirtualizingListBox.ActiveTransctiption =  VirtualizingListBox.Subtitles.First();
        }

        private void CJumpToEnd(object sender, ExecutedRoutedEventArgs e)
        {
            VirtualizingListBox.ActiveTransctiption = VirtualizingListBox.Subtitles.Last();
        }


        private bool WarnIfElementIsShort(TimeSpan T1, TimeSpan T2)
        {
            if ((T1 - T2).Duration() <= TimeSpan.FromMilliseconds(100))
            {
                MessageBox.Show("Odstavec nemůže být kratší než 100ms", "Chyba", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
            MySection s = new MySection("Sekce");
            MyParagraph p = new MyParagraph();
            p.Add(new MyPhrase());
            s.Add(p);
            myDataSource.Add(s);
            VirtualizingListBox.ActiveTransctiption = s;
        }
        private void CInsertNewSection(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption != null)
            {
                if (VirtualizingListBox.ActiveTransctiption.IsParagraph)
                {
                    MyParagraph p = (MyParagraph)VirtualizingListBox.ActiveTransctiption;
                    int idx = p.ParentIndex;
                    MySection sec = (MySection)p.Parent;
                    MySection s = new MySection("Sekce");
                    for (int i = idx; i < sec.Children.Count; i++)
                        s.Add(sec[i]);

                    sec.Children.RemoveRange(idx, sec.Children.Count - idx);

                    sec.Parent.Insert(sec.ParentIndex + 1, s);
                    VirtualizingListBox.ActiveTransctiption = s;
                }
                else if (VirtualizingListBox.ActiveTransctiption.IsSection)
                {
                    var s = new MySection("Sekce");
                    s.Children.AddRange(VirtualizingListBox.ActiveTransctiption.Children);
                    VirtualizingListBox.ActiveTransctiption.Children.Clear();
                    VirtualizingListBox.ActiveTransctiption.Parent.Insert(VirtualizingListBox.ActiveTransctiption.ParentIndex, s);
                    VirtualizingListBox.ActiveTransctiption = s;
                }
                else if (VirtualizingListBox.ActiveTransctiption.IsChapter)
                {
                    var s = new MySection("Sekce");
                    VirtualizingListBox.ActiveTransctiption.Insert(0, s);

                    VirtualizingListBox.ActiveTransctiption = s;
                }
            }
        }
        private void CNewChapter(object sender, ExecutedRoutedEventArgs e)
        {
            MyChapter c = new MyChapter("Kapitola");
            MySection s = new MySection("Sekce");
            MyParagraph p = new MyParagraph();
            p.Add(new MyPhrase());
            s.Add(p);
            c.Add(s);
            myDataSource.Add(c);
            VirtualizingListBox.ActiveTransctiption = c;
        }
        private void CDeleteElement(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption != null && VirtualizingListBox.ActiveTransctiption.Parent!=null)
            {
                VirtualizingListBox.ActiveTransctiption.Parent.Remove(VirtualizingListBox.ActiveTransctiption);
            }
        }
        private void CAssignSpeaker(object sender, ExecutedRoutedEventArgs e)
        {
            if (VirtualizingListBox.ActiveTransctiption == null || !VirtualizingListBox.ActiveTransctiption.IsParagraph)
                return;
            new WinSpeakers(VirtualizingListBox.ActiveTransctiption as MyParagraph, MySetup.Setup, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();
            VirtualizingListBox.SubtitlesContentChanged();
        }

        private void CExportElement(object sender, ExecutedRoutedEventArgs e)
        {
            MyParagraph par = VirtualizingListBox.ActiveTransctiption as MyParagraph;
            oWav.RamecSynchronne = true;
            bool nacteno = oWav.NactiRamecBufferu((long)par.Begin.TotalMilliseconds, (long)par.Delka.TotalMilliseconds, MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS);//)this.bPozadovanyPocatekRamce, this.bPozadovanaDelkaRamceMS, this.bIDBufferu);        
            oWav.RamecSynchronne = false;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "wav soubory (.wav)|*.wav";
            if (dlg.ShowDialog() == true)
            {
                string filename = dlg.FileName;
                //BinaryWriter bw = new BinaryWriter(new FileStream(filename, FileMode.Create));
                MyBuffer16 bf = new MyBuffer16(oWav.NacitanyBufferSynchronne.data.Length);

                bf.data = oWav.NacitanyBufferSynchronne.data;
                MyWav.VytvorWavSoubor(bf, filename);


                string ext = System.IO.Path.GetExtension(filename);
                filename = filename.Substring(0, filename.Length - ext.Length);
                string textf = filename + ".txt";

                File.WriteAllBytes(textf, win1250.GetBytes(par.Text));


                if (!string.IsNullOrEmpty(par.Phonetics))
                {
                    textf = filename + ".phn";
                    File.WriteAllBytes(textf, win1250.GetBytes(par.Phonetics));
                }
            }
        }

        private void CFindDialogExecute(object sender, ExecutedRoutedEventArgs e)
        {
            if (m_findDialog == null || !m_findDialog.IsLoaded || !m_findDialog.IsVisible)
            {
                m_findDialog = new FindDialog(this);
                m_findDialog.Owner = this;
                searchtextoffset = 0;
                m_findDialog.Show();
            }
            else
            {
                m_findDialog.SearchNext();
            }
        }

        private void CPlayPauseExecute(object sender, ExecutedRoutedEventArgs e)
        {
            if (_playing)
            {
                if (jeVideo) meVideo.Pause();
                prehratVyber = false;
                Playing = false;
                if (MWP != null)
                {
                    waveform1.CaretPosition = MWP.PausedAt;
                }
            }
            else
            {
                waveform1.Invalidate();
                bool adjustspeed = false;
                if ((Keyboard.Modifiers & ModifierKeys.Shift)== ModifierKeys.Shift || ToolBar2BtnSlow.IsChecked == true)
                {
                    adjustspeed = true;
                    meVideo.SpeedRatio = MySetup.Setup.ZpomalenePrehravaniRychlost;
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


                    prehratVyber = true;
                    oldms = TimeSpan.Zero;
                    

                    if ((waveform1.SelectionBegin - waveform1.SelectionEnd).Duration() > TimeSpan.FromMilliseconds(100))
                    {
                        NastavPoziciKurzoru(waveform1.SelectionBegin, true, false);
                    }
                    else
                    {
                        TranscriptionElement par = VirtualizingListBox.ActiveTransctiption;
                        TimeSpan konec = par.End + TimeSpan.FromMilliseconds(5);
                        NastavPoziciKurzoru(konec, true, true);
                        waveform1.SelectionBegin = konec;
                        waveform1.SelectionEnd = konec + TimeSpan.FromMilliseconds(120000);
                    }

                }

                meVideo.Position = waveform1.CaretPosition;
                if (jeVideo) meVideo.Play();
                //spusteni prehravani pomoci tlacitka-kvuli nacteni primeho prehravani

                Playing = true;

                if (adjustspeed)
                    MWP.Play(MySetup.Setup.ZpomalenePrehravaniRychlost);
                else
                    MWP.Play();

            }
        }

        private void CScrollDown(object sender, ExecutedRoutedEventArgs e)
        {
            VirtualizingListBox.gridscrollbar.Value += 0.7 * VirtualizingListBox.ActualHeight;
            SetCursor();
        }

        private void SetCursor()
        {
            UpdateLayout();
            HitTestResult res = VisualTreeHelper.HitTest(VirtualizingListBox, new Point(0,5));
            if (res.VisualHit != null)
            {
                Element e = res.VisualHit.FindVisualParent<Element>();
                if (e != null)
                {
                    e.editor.Focus();
                    e.editor.CaretOffset = 0;
                }
            }
        }

        private void CScrollUP(object sender, ExecutedRoutedEventArgs e)
        {
            VirtualizingListBox.gridscrollbar.Value -= 0.7 * VirtualizingListBox.ActualHeight;

        }

        private void CSmallJumpRight(object sender, ExecutedRoutedEventArgs e)
        {
           NastavPoziciKurzoru(waveform1.CaretPosition + waveform1.SmallJump, true, true);
           VyberTextMeziCasovymiZnackami(waveform1.CaretPosition);
        }

        private void CSmallJumpLeft(object sender, ExecutedRoutedEventArgs e)
        {
            NastavPoziciKurzoru(waveform1.CaretPosition - waveform1.SmallJump, true, true);
            VyberTextMeziCasovymiZnackami(waveform1.CaretPosition);
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

        private void CAutomaticFoneticTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            menuItemNastrojeFonetickyPrepis_Click(null, new RoutedEventArgs());
            menuItemFonetickyPrepis_Click(null, new RoutedEventArgs());
        }

        private void CShowPanelFoneticTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            ZobrazitOknoFonetickehoPrepisu(true);
        }

        private void CGeneratePhoneticTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            //TODO:
          /*  if (MyKONST.VERZE == MyEnumVerze.Externi) return;
            if (oPrepisovac != null && oPrepisovac.Rozpoznavani)
            {
                if (MessageBox.Show("Opravdu chcete přerušit právě probíhající přepis?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (oPrepisovac.StopHned() == 0)
                    {
                      if (pSeznamOdstavcuKRozpoznani != null) pSeznamOdstavcuKRozpoznani.Clear();
                    }
                }
            }
            else
            {
                SpustRozpoznavaniVybranehoElementu(VirtualizingListBox.ActiveTransctiption, new TimeSpan(-1), new TimeSpan(-1), false);
            }
           * */
        }

        private void CStartStopDictate(object sender, ExecutedRoutedEventArgs e)
        {
            if (MyKONST.VERZE == MyEnumVerze.Externi) return;
            //diktat docasne zrusen
        
        }

        private void CStartStopVoiceControl(object sender, ExecutedRoutedEventArgs e)
        {
            if (MyKONST.VERZE == MyEnumVerze.Externi) return;

        
        }

        private void CNormalizeParagraph(object sender, ExecutedRoutedEventArgs e)
        {
            Normalizovat(VirtualizingListBox.ActiveTransctiption, -1);
        }

        private void CRemoveNonphonemes(object sender, ExecutedRoutedEventArgs e)
        {
            //TODO:
        }

        private void CTakeSpeakerSnapshotFromVideo(object sender, ExecutedRoutedEventArgs e)
        {
            if (jeVideo)
            {

                Size dpi = new Size(96, 96);

                RenderTargetBitmap bmp = new RenderTargetBitmap((int)gVideoPouze.ActualWidth, (int)gVideoPouze.ActualHeight + (int)gVideoPouze.Margin.Top, dpi.Width, dpi.Height, PixelFormats.Pbgra32);
                bmp.Render(gVideoPouze);


                BitmapFrame pFrame = BitmapFrame.Create(bmp);

                string pBase = MyKONST.PrevedJPGnaBase64String(pFrame);


                WinSpeakers.ZiskejMluvciho(this.myDatabazeMluvcich, null, pBase);
            }
            else
            {
                MessageBox.Show("Nelze vytvořit obrázek mluvčího, protože není načteno video", "Upozornění!");
            }
        
        }
        private void CCreateNewTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            NoveTitulky();
        }

        private void COpenTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            OtevritTitulky(true, "", false);
        }
       
        private void CSaveTranscription(object sender, ExecutedRoutedEventArgs e)
        {
            if (myDataSource != null)
            {
                if (myDataSource.JmenoSouboru != null)
                {
                    UlozitTitulky(false, myDataSource.JmenoSouboru);
                }
                else
                {
                    UlozitTitulky(true, myDataSource.JmenoSouboru);
                }
            }
        }
        
        private void CSaveTranscriptionAs(object sender, ExecutedRoutedEventArgs e)
        {
            UlozitTitulky(true, "");
        }

        private void CAbout(object sender, ExecutedRoutedEventArgs e)
        {
            new WinOProgramu(MyKONST.NAZEV_PROGRAMU).ShowDialog();
        }
        
        private void CHelp(object sender, ExecutedRoutedEventArgs e)
        {
            if (oknoNapovedy ==null || !oknoNapovedy.IsLoaded)
            {
                oknoNapovedy = new WinHelp();
                oknoNapovedy.Show();
        }
        }

        int searchtextoffset = 0;
        public void FindNext(string pattern, bool isregex, bool CaseSensitive, bool searchinspeakers)
        {
            foreach (Element e in VirtualizingListBox.gridstack.Children)
            {
                e.editor.SelectionLength = 0;
            }

            if (VirtualizingListBox.ActiveTransctiption == null)
                if (m_mydatasource.Chapters.Count > 0)
                {
                    VirtualizingListBox.ActiveTransctiption = m_mydatasource.Chapters[0];
                }
                else
                    return;
            TranscriptionElement tag = VirtualizingListBox.ActiveTransctiption ;

            TranscriptionElement pr = tag;
            if (pr == null)
                tag = myDataSource.Chapters[0];

            int len;
            if (myDataSource.FindNext(ref tag, ref searchtextoffset, out len, pattern, isregex, CaseSensitive, searchinspeakers))
            {
                TranscriptionElement p = tag;
                waveform1.CaretPosition = p.Begin;
                VirtualizingListBox.ActiveTransctiption = p;

                if (VirtualizingListBox.ActiveElement != null)
                {
                    VirtualizingListBox.ActiveElement.SetSelection(searchtextoffset,len,0);
                    searchtextoffset += len;
                }
            }
            else
            {
                if (MessageBox.Show("Vyhledávaný výraz nebyl nalezen, začít znovu od začátku dokumentu?", "Vyhledávání", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    var first = m_mydatasource.First();
                    VirtualizingListBox.ActiveTransctiption = first;
                    FindNext(pattern, isregex, CaseSensitive, searchinspeakers);
                }
                   
            }
        }

        public FindDialog m_findDialog;
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