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
using USBHIDDRIVER;
using dbg = System.Diagnostics.Debug;


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


        public static RoutedCommand CommandNewParagraphAtPosition = new RoutedCommand();

        public void InitCommands()
        {
            #region mainform bindings
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
            #endregion

            this.CommandBindings.Add(new CommandBinding(CommandNewParagraphAtPosition, CNewParagraphAtPosition));



            CommandNewParagraphAtPosition.InputGestures.Add(new KeyGesture(Key.Return));
        }

        #region mainform actions
        private void CFindDialogExecute(object sender, ExecutedRoutedEventArgs e)
        {
            if (!m_findDialog.IsLoaded || !m_findDialog.IsVisible)
            {
                m_findDialog = new FindDialog(this);

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
                    prehratVyber = true;
                    oldms = TimeSpan.Zero;
                    //if (waveform1.CaretPosition >= waveform1.SelectionBegin && waveform1.CaretPosition <= waveform1.SelectionEnd)

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
            VirtualizingListBox.gridscrollbar.Value += 0.7 * VirtualizingListBox.Height;

        }

        private void CScrollUP(object sender, ExecutedRoutedEventArgs e)
        {
            VirtualizingListBox.gridscrollbar.Value -= 0.7 * VirtualizingListBox.Height;
        }

        private void CSmallJumpRight(object sender, ExecutedRoutedEventArgs e)
        {
           NastavPoziciKurzoru(waveform1.CaretPosition + waveform1.SmallJump, true, true);
        }

        private void CSmallJumpLeft(object sender, ExecutedRoutedEventArgs e)
        {
            NastavPoziciKurzoru(waveform1.CaretPosition - waveform1.SmallJump, true, true);
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
            //refresh uz vykreslenych textboxu

            VirtualizingListBox.SubtitlesContentChanged();
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
        public void FindNext(string pattern, bool isregex, bool CaseSensitive)
        {
            if (VirtualizingListBox.ActiveTransctiption == null)
                return;
            TranscriptionElement tag = VirtualizingListBox.ActiveTransctiption ;

            TranscriptionElement pr = tag;
            if (pr == null)
                tag = myDataSource.Chapters[0];




            if (myDataSource.FindNext(ref tag, ref searchtextoffset, pattern, isregex, CaseSensitive))
            {
                TranscriptionElement p = tag;
                waveform1.CaretPosition = p.Begin;
                VirtualizingListBox.ActiveTransctiption = p;
            }
        }

        public FindDialog m_findDialog;
        #endregion


        #region textboxActions
        private void CNewParagraphAtPosition(object sender, ExecutedRoutedEventArgs e)
        {
            TranscriptionElement te = VirtualizingListBox.ActiveTransctiption;
                if (te!= null)
                {
                    //rozdeleni

                    MyParagraph before = new MyParagraph();
                    MyParagraph after = new MyParagraph();
                    MyParagraph par = te as MyParagraph;
                    int i;

                    int pos = VirtualizingListBox.ActiveElement.myTextBox1.CaretIndex;

                    if(pos == 0)
                        return;

                    int sum = 0;
                    for (i = 0; i < par.Phrases.Count;i++)
                    {
                        if (sum + par.Phrases[i].Text.Length == pos) //deleni v mezere
                        {
                            int ix = te.ParentIndex;
                            te.Parent.RemoveAt(ix);
                            var b = new List<MyPhrase>();
                            var a = new List<MyPhrase>();
                            for (int j = 0; j <= i; j++)
                                b.Add(par.Phrases[i]);

                            for (int j = i+1; j <= par.Phrases.Count; j++)
                                a.Add(par.Phrases[i]);

                            par.Phrases.Clear();

                            te.Parent.Insert(ix, new MyParagraph(a));
                            te.Parent.Insert(ix, new MyParagraph(b)); 
                            break;
                        }
                        else if (sum > pos && sum < pos + par.Phrases[i].Text.Length)
                        { 
                        
                        }
                    }
                }
                else if(myDataSource.VratElementDanehoCasu(waveform1.CaretPosition).Count == 0)
                {

                    //novy v prazdnu
                    int indx = -1;
                    TranscriptionElement lastbefore = null;
                    bool openelement = false;
                    foreach (var elm in myDataSource)
                    {
                        indx++;
                        if (lastbefore == null)
                            lastbefore = elm;
                        else if (elm.End >= lastbefore.End && elm.End <= waveform1.CaretPosition)
                        {
                            lastbefore = elm;
                        }
                        else if(elm.End < TimeSpan.Zero) //neukonceny element
                        {
                            elm.End = waveform1.CaretPosition;
                            lastbefore = elm;
                            openelement = true;
                            break;
                        }else
                            break;
                    }

                    var p = new MyParagraph() { Begin = lastbefore.End, End = waveform1.CaretPosition };
                    if(openelement)
                        p.End = new TimeSpan(-1);

                    myDataSource.Insert(indx, p);

                }
        }
        #endregion




        #endregion
        #region usb HID pedals
        static USBHIDDRIVER.USBInterface usbI = new USBInterface("vid_05f3", "pid_00ff");
        EventHandler savehandle;
        public void HidInit()
        {
            savehandle = new EventHandler(HIDhandler);
            bool conn = usbI.Connect();
            if (conn)
            {
                usbI.enableUsbBufferEvent(savehandle);
                Thread.Sleep(5);
                usbI.startRead();
            }

        }
        FCPedal FCstatus = FCPedal.None;
        [Flags]
        public enum FCPedal : byte
        {
            None = 0x0,
            Left = 0x1,
            Middle = 0x2,
            Right = 0x4,
            Invalid = 0xFF
        }
        public void HIDhandler(object sender, System.EventArgs e)
        {
            USBHIDDRIVER.List.ListWithEvent ev = (USBHIDDRIVER.List.ListWithEvent)sender;
            foreach (object o in ev)
            {
                if (o is byte[])
                {
                    byte[] data = (byte[])o;
                    byte stat = data[1];
                    if (FCstatus != FCPedal.Invalid)
                    {
                        if ((((byte)FCPedal.Left) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Left & FCstatus) == 0) //down event
                            {
                                this.Dispatcher.Invoke(new Action(()=>CommandSmallJumpLeft.Execute(null, this)));
                            }
                        }
                        else if ((((byte)FCPedal.Middle) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Middle & FCstatus) == 0) //down event
                            {
                                this.Dispatcher.Invoke(new Action(() => CommandPlayPause.Execute(null, this)));

                            }

                        }
                        else if ((((byte)FCPedal.Right) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Right & FCstatus) == 0) //down event
                            {
                                this.Dispatcher.Invoke(new Action(() => CommandSmallJumpRight.Execute(null, this)));
                            }

                        }
                    }

                    FCstatus = (FCPedal)stat;
                }
            }
            ev.Clear();
        }

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

        private void tbFonetickyPrepis_PreviewKeyDown(object sender, KeyEventArgs e)
        {
        }
    }


}