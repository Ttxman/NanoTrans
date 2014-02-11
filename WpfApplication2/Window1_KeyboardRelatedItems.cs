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
                        MyParagraph par = myDataSource[MySetup.Setup.RichTag];
                        TimeSpan konec = myDataSource.VratCasElementuPocatek(MySetup.Setup.RichTag) + TimeSpan.FromMilliseconds(5);
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
            double pOffset = svDokument.VerticalOffset;
            svDokument.ScrollToVerticalOffset(svDokument.VerticalOffset + svDokument.ViewportHeight * 0.8);
            svDokument.UpdateLayout();
            if (pOffset == svDokument.VerticalOffset)
            {
                (spSeznam.Children[spSeznam.Children.Count - 1] as Grid).Children[0].Focus();
                return;
            }
            // position of your visual inside the scrollviewer    
            Grid pGNalezen = null;
            for (int i = 0; i < spSeznam.Children.Count; i++)
            {
                Grid pG = (spSeznam.Children[i] as Grid);
                GeneralTransform childTransform = pG.TransformToAncestor(svDokument);
                Rect rectangle = childTransform.TransformBounds(new Rect(new Point(0, 0), pG.RenderSize));
                //Check if the elements Rect intersects with that of the scrollviewer's
                Rect result = Rect.Intersect(new Rect(new Point(0, 0), svDokument.RenderSize), rectangle);
                //if result is Empty then the element is not in view
                if (result == Rect.Empty)
                {
                    if (pGNalezen != null)
                    {
                        pGNalezen.Children[0].Focus();
                    }
                }
                else
                {
                    if (pGNalezen == null)
                    {
                        pGNalezen = pG;
                    }
                    else
                    {
                        pG.Children[0].Focus();
                        break;
                    }
                    //obj is partially Or completely visible
                }
            }


        }

        private void CScrollUP(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            double pOffset = svDokument.VerticalOffset;
            svDokument.ScrollToVerticalOffset(svDokument.VerticalOffset - svDokument.ViewportHeight * 0.8);
            svDokument.UpdateLayout();
            if (pOffset == svDokument.VerticalOffset)
            {
                (spSeznam.Children[0] as Grid).Children[0].Focus();
                return;
            }
            // position of your visual inside the scrollviewer    
            Grid pGNalezen = null;
            for (int i = 0; i < spSeznam.Children.Count; i++)
            {
                Grid pG = (spSeznam.Children[i] as Grid);
                GeneralTransform childTransform = pG.TransformToAncestor(svDokument);
                Rect rectangle = childTransform.TransformBounds(new Rect(new Point(0, 0), pG.RenderSize));
                //Check if the elements Rect intersects with that of the scrollviewer's
                Rect result = Rect.Intersect(new Rect(new Point(0, 0), svDokument.RenderSize), rectangle);
                //if result is Empty then the element is not in view
                if (result == Rect.Empty)
                {
                    if (pGNalezen != null)
                    {
                        pGNalezen.Children[0].Focus();
                    }
                }
                else
                {
                    if (pGNalezen == null)
                    {
                        pGNalezen = pG;
                    }
                    else
                    {
                        pG.Children[0].Focus();
                        break;
                    }
                    //obj is partially Or completely visible
                }
            }


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
            if (MyKONST.VERZE == MyEnumVerze.Externi) return;
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
                SpustRozpoznavaniVybranehoElementu(MySetup.Setup.RichTag, new TimeSpan(-1), new TimeSpan(-1), false);
            }
        }

        private void CStartStopDictate(object sender, ExecutedRoutedEventArgs e)
        {
            if (MyKONST.VERZE == MyEnumVerze.Externi) return;
            if (!btDiktat.IsEnabled) return;
            if (recording)
            {
                if (MWR != null) MWR.Dispose();
                MWR = null;
                recording = false;
                ZmenStavTlacitekRozpoznavace(false, true, false, false);
            }
            else
            {
                if (SpustRozpoznavaniHlasu())
                {
                    ZmenStavTlacitekRozpoznavace(false, true, false, true);
                }
            }
        
        }

        private void CStartStopVoiceControl(object sender, ExecutedRoutedEventArgs e)
        {
            if (MyKONST.VERZE == MyEnumVerze.Externi) return;
            if (recording)
            {
                if (MWR != null) MWR.Dispose();
                MWR = null;
                recording = false;
                ZmenStavTlacitekRozpoznavace(false, false, true, false);
            }
            else
            {
                if (SpustHlasoveOvladani())
                {
                    ZmenStavTlacitekRozpoznavace(false, false, true, true);
                }
            }
        
        }

        private void CNormalizeParagraph(object sender, ExecutedRoutedEventArgs e)
        {
            MyTag pTag = MySetup.Setup.RichTag;
            Normalizovat(myDataSource, pTag, -1);
        }

        private void CRemoveNonphonemes(object sender, ExecutedRoutedEventArgs e)
        {
            if (bFonetika == null) bFonetika = new MyFonetic(MySetup.Setup.absolutniCestaEXEprogramu);

            bool pStav = bFonetika.OdstraneniNefonetickychZnakuZPrepisu(myDataSource, MySetup.Setup.RichTag);
            if (pStav)
            {
                MyTag pTag = new MyTag(MySetup.Setup.RichTag);
                pTag.tTypElementu = MyEnumTypElementu.foneticky;
                MyParagraph pP = myDataSource[pTag];
                MySetup.Setup.CasoveZnackyText = pP.Text;
                MySetup.Setup.CasoveZnacky = pP.VratCasoveZnackyTextu;
            }
            ZobrazitFonetickyPrepisOdstavce(MySetup.Setup.RichTag);
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

            VirtualizingListBox.Refresh();
            this.Dispatcher.Invoke(new Action(
                delegate()
                {
                    foreach (UIElement element in spSeznam.Children)
                    {
                        if (element is Grid)
                        {
                            foreach (UIElement subelement in ((Grid)element).Children)
                            {
                                if (subelement is MyTextBox)
                                {
                                    ((MyTextBox)subelement).RefreshTextMarking();
                                }
                            }
                        }
                    }


                }
                ));
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
            if (MySetup.Setup.RichTag == null)
                return;
            MyTag tag = MySetup.Setup.RichTag;

            MyParagraph pr = myDataSource[tag];
            if (pr == null)
                tag = new MyTag(0, 0, 0);




            if (myDataSource.FindNext(ref tag, ref searchtextoffset, pattern, isregex, CaseSensitive))
            {
                MyParagraph p = myDataSource[tag];
                waveform1.CaretPosition = p.Begin;

                //TODO: omg neni potreba, zadna virtualizace neexistuje .... ZobrazXMLData();

                foreach (UIElement c in spSeznam.Children)
                {
                    Grid g = c as Grid;

                    if (g.Children[0] is MyTextBox)
                    {
                        MyTextBox tb = g.Children[0] as MyTextBox;

                        MyTag mt = tb.Tag as MyTag;
                        if (mt != null)
                        {
                            if (mt.tKapitola == tag.tKapitola && mt.tOdstavec == tag.tOdstavec && mt.tSekce == tag.tSekce)
                            {
                                tb.Focus();
                                tb.Select(searchtextoffset - pattern.Length, pattern.Length);
                                return;
                            }

                        }

                    }



                }





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

                            te.Parent.Insert(new MyParagraph(a), ix);
                            te.Parent.Insert(new MyParagraph(b), ix); 
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

                    MySection s = p.Parent as MySection;
                    var pp = new MyParagraph() { Begin = lastbefore.End, End = waveform1.CaretPosition, IsPhonetic = true };

                    int ix = s.Paragraphs.IndexOf(p);
                    s.PhoneticParagraphs.Insert(ix, pp);


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



        void richX_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Delete || e.Key == Key.Back)
            {
                popup.IsOpen = false;
            }

            if (e.Key == Key.PageUp || e.Key == Key.PageDown) // klavesy, ktere textbox krade, posleme rucne parentu...
            {
                e.Handled = true;
                KeyEventArgs kea = new KeyEventArgs((KeyboardDevice)e.Device, PresentationSource.FromVisual(this), e.Timestamp, e.Key) { RoutedEvent = Window1.PreviewKeyDownEvent };
                RaiseEvent(kea);
                if (!kea.Handled)
                {
                    kea.RoutedEvent = Window1.KeyDownEvent;
                    RaiseEvent(kea);
                }


                return;
            }

            try
            {
                int index = spSeznam.Children.IndexOf((Grid)((TextBox)(sender)).Parent);
                MyTag mT = (MyTag)((TextBox)(sender)).Tag;
                TextBox pTB = ((TextBox)sender);

                KeyConverter kc = new KeyConverter();


                if (e.Key == Key.Escape && sender != tbFonetickyPrepis)
                {
                    if (popup.IsOpen)
                    {
                        popup.IsOpen = false;
                        e.Handled = true;
                        listboxpopupPopulate(MySetup.Setup.NerecoveUdalosti);

                        popup_filter = "";
                    }
                    else if (MySetup.Setup.ZobrazitFonetickyPrepis > 10)
                    {
                        tbFonetickyPrepis.Focus();
                        e.Handled = true;
                    }
                }


                else if (e.Key == Key.F2 && !e.IsRepeat)
                {
                    e.Handled = true;
                    MyTag pomTag = PridejKapitolu(mT.tKapitola + 1, "");
                    if (pomTag != null)
                    {
                        pomTag.tSender = VratSenderTextboxu(pomTag);
                        if (pomTag.tSender != null)
                        {
                            spSeznam.UpdateLayout();
                            ((TextBox)pomTag.tSender).Focus();
                        }
                    }
                }
                else if (e.Key == Key.F4 && !e.IsRepeat)
                {

                    //TODO: nevim jestli (mluvci sekce) to enkdo nekdy pouzival, ale F3 potrebuju
                    e.Handled = true;
                    MyTag pomTag = null;
                    pomTag = PridejSekci(mT.tKapitola, "", mT.tSekce, mT.tOdstavec, new TimeSpan(-1), new TimeSpan(-1));

                    if (pomTag != null)
                    {
                        pomTag.tSender = VratSenderTextboxu(pomTag);
                        if (pomTag.tSender != null)
                        {
                            spSeznam.UpdateLayout();
                            ((TextBox)pomTag.tSender).Focus();
                        }
                    }

                }

                else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Shift && !e.IsRepeat)
                {
                    e.Handled = true;

                    if (mT.tOdstavec > -1)
                    {
                        if (OdstranOdstavec(mT.tKapitola, mT.tSekce, mT.tOdstavec))
                        {
                            if (index >= spSeznam.Children.Count) index--;
                            ((Grid)spSeznam.Children[index]).Children[0].Focus();
                        }
                    }
                    else if (mT.tSekce > -1)
                    {
                        if (OdstranSekci(mT.tKapitola, mT.tSekce))
                        {
                            if (index >= spSeznam.Children.Count) index--;
                            ((Grid)spSeznam.Children[index]).Children[0].Focus();
                        }
                    }
                    else
                    {
                        OdstranKapitolu(mT.tKapitola);
                    }
                }
                else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Control && !e.IsRepeat)        //smazani pocatecniho casoveho indexu
                {
                    e.Handled = true;
                    menuItemX5_Smaz_Click(null, new RoutedEventArgs());
                }
                else if (e.Key == Key.Home && Keyboard.Modifiers == ModifierKeys.Control && !e.IsRepeat) //nastavi index pocatku elementu podle pozice kurzoru
                {
                    e.Handled = true;
                    menuItemVlna1_prirad_zacatek_Click(null, new RoutedEventArgs());

                }
                else if (e.Key == Key.End && Keyboard.Modifiers == ModifierKeys.Control && !e.IsRepeat) //nastavi index konce elementu podle pozice kurzoru
                {
                    e.Handled = true;
                    menuItemVlna1_prirad_konec_Click(null, new RoutedEventArgs());
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Space)    //ctrl+mezernik = prida casovou znacku do textu
                {
                    menuItemVlna1_prirad_casovou_znacku_Click(null, new RoutedEventArgs());
                    e.Handled = true;
                }
                /*    else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control && !e.IsRepeat)
                    {
                        e.Handled = true;
                        MenuItemFonetickeVarianty_Click(null, new RoutedEventArgs());
                        Keyboard.Modifiers == ModifierKeys.Control = false;

                    }*/
                else if (e.Key == Key.M)
                {
                    if (Keyboard.Modifiers == ModifierKeys.Control && !e.IsRepeat)  //prehrani nebo pausnuti prehravani
                    {
                        e.Handled = true;
                        buttonX_Click(((Button)(((Grid)((TextBox)sender).Parent).Children[1] as StackPanel).Children[0]), new RoutedEventArgs());
                    }
                }
                else if ((e.Key == Key.Up))
                {
                    if (mT.tTypElementu == MyEnumTypElementu.foneticky)
                        return;
                    if (Keyboard.Modifiers != ModifierKeys.Shift)
                    {
                        this.SmazatVyberyTextboxu(pPocatecniIndexVyberu, pKoncovyIndexVyberu, -1);
                        pZiskatNovyIndex = true;
                    }
                    //int TP = ((TextBox)sender).GetLineIndexFromCharacterIndex(((TextBox)sender).CaretIndex);
                    int TP = pTB.GetLineIndexFromCharacterIndex(pTB.SelectionStart + pTB.SelectionLength);
                    if (pPocatecniIndexVyberu <= index) TP = pTB.GetLineIndexFromCharacterIndex(pTB.SelectionStart);



                    if (TP == 0 && index > 0)
                    {
                        //int pPoziceCursoruNaRadku = pTB.CaretIndex - pTB.GetCharacterIndexFromLineIndex(pTB.GetLineIndexFromCharacterIndex(pTB.CaretIndex));
                        int pPoziceCursoruNaRadku = (pTB.SelectionStart) - pTB.GetCharacterIndexFromLineIndex(pTB.GetLineIndexFromCharacterIndex(pTB.SelectionStart));
                        //pPoziceCursoruNaRadku = pTB.GetCharacterIndexFromPoint(new Point(pTB.GetRectFromCharacterIndex(pTB.CaretIndex).X, pTB.GetRectFromCharacterIndex(pTB.CaretIndex).Y), true);
                        if (Keyboard.Modifiers == ModifierKeys.Shift)
                        {
                            int pPozice = pTB.SelectionStart;
                            int pDelka = pTB.SelectionLength;
                        }
                        else
                        {
                            TextBox pTBPredchozi = (TextBox)((Grid)spSeznam.Children[index - 1]).Children[0];
                            pTB.Select(0, 0);
                            pTBPredchozi.Focus();
                            pTBPredchozi.CaretIndex = pTBPredchozi.GetCharacterIndexFromLineIndex(pTBPredchozi.LineCount - 1) + pPoziceCursoruNaRadku;
                            if (Keyboard.Modifiers == ModifierKeys.Shift)
                            {
                                //pTBPredchozi.CaretIndex = pTBPredchozi.Text.Length;
                            }
                            e.Handled = true;
                        }
                    }

                }
                else if ((e.Key == Key.Down))
                {
                    if (mT.tTypElementu == MyEnumTypElementu.foneticky)
                        return;

                    if ((index < (spSeznam.Children.Count - 1)))
                    {


                        int TP = ((TextBox)sender).GetLineIndexFromCharacterIndex(pTB.SelectionStart + pTB.SelectionLength);
                        if (TP == ((TextBox)sender).LineCount - 1)
                        ///if (TP.GetOffsetToPosition(TP.DocumentEnd) == ((RichTextBox)sender).Selection.Start.GetOffsetToPosition(((RichTextBox)sender).Selection.Start.DocumentEnd))
                        {
                            if (Keyboard.Modifiers != ModifierKeys.Shift)
                            {
                                this.SmazatVyberyTextboxu(pPocatecniIndexVyberu, pKoncovyIndexVyberu, -1);
                                pZiskatNovyIndex = true;
                            }

                            //int pPoziceCursoruNaRadku = pTB.CaretIndex - pTB.GetCharacterIndexFromLineIndex(pTB.GetLineIndexFromCharacterIndex(pTB.CaretIndex));
                            int pPoziceCursoruNaRadku = (pTB.SelectionStart + pTB.SelectionLength) - pTB.GetCharacterIndexFromLineIndex(pTB.GetLineIndexFromCharacterIndex(pTB.SelectionStart + pTB.SelectionLength));
                            TextBox pTBDalsi = (TextBox)((Grid)spSeznam.Children[index + 1]).Children[0];

                                pTB.CaretIndex = 0;
                                pTBDalsi.Focus();
                                pTBDalsi.CaretIndex = pPoziceCursoruNaRadku;
                                e.Handled = true;
                        }

                    }
                }
                else if (e.Key == Key.Right)
                {
                    if (mT.tTypElementu == MyEnumTypElementu.foneticky)
                        return;
                    if ((index < (spSeznam.Children.Count - 1)))
                    {

                        TextBox pTBDalsi = (TextBox)((Grid)spSeznam.Children[index + 1]).Children[0];
                        if (Keyboard.Modifiers != ModifierKeys.Shift && pTB.CaretIndex == pTB.Text.Length)
                        {
                            pTB.SelectionLength = 0;
                            pTBDalsi.Focus();
                            pTBDalsi.CaretIndex = 0;
                            e.Handled = true;
                        }
                    }

                }
                else if (e.Key == Key.Left)
                {
                    if (mT.tTypElementu == MyEnumTypElementu.foneticky)
                        return;
                    if ((index > 0))
                    {

                        TextBox pTBPredchozi = (TextBox)((Grid)spSeznam.Children[index - 1]).Children[0];
                        if (Keyboard.Modifiers != ModifierKeys.Shift && pTB.CaretIndex == 0)
                        {
                            pTB.SelectionLength = 0;
                            pTBPredchozi.Focus();
                            pTBPredchozi.CaretIndex = pTBPredchozi.Text.Length;
                            e.Handled = true;
                        }
                    }

                }
                else if (e.Key == Key.Back)
                {




                    string tr = ((TextBox)(sender)).Text;
                    int j = ((TextBox)(sender)).CaretIndex;





                    if ((j == 0) && mT.JeOdstavec)
                    {
                        this.pUpravitOdstavec = false; //odstavec je upraven jiz zde, a nebude dale upravovan v udalosti text change

                        ///flowDoc = ((RichTextBox)(sender)).Document;
                        ///zac = ((RichTextBox)(sender)).Selection.Start;
                        ///kon = flowDoc.ContentEnd.GetPositionAtOffset(-1);
                        ///tr = new TextRange(zac, kon);
                        string s = tr;
                        s = s.Replace("\n\r", "\r");       //nahrazeni \n za \r kvuli odradkovani
                        s = s.Replace("\r\n", "\r");       //nahrazeni \n za \r kvuli odradkovani
                        s = s.Replace("\n", "\r");       //nahrazeni \n za \r kvuli odradkovani

                        List<MyCasovaZnacka> pCasoveZnackyMazaneho = myDataSource[mT].VratCasoveZnackyTextu;


                        if (mT.tOdstavec > 0 || s.Length == 0)
                        {

                            if (index > 0)
                            {


                                if (((MyTag)((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Tag).tOdstavec > -1)
                                {


                                    tr = ((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Text;
                                    int pDelka = tr.Length;
                                    string t = tr;
                                    t = t.Replace("\n\r", "\r");       //nahrazeni \n za \r kvuli odradkovani
                                    t = t.Replace("\r\n", "\r");       //nahrazeni \n za \r kvuli odradkovani
                                    t = t.Replace("\n", "\r");       //nahrazeni \n za \r kvuli odradkovani

                                    //casove znacky predchoziho odstavce, ke kteremu se budou pridavat nasledujici
                                    MyTag pTagPredchoziho = new MyTag(mT.tKapitola, mT.tSekce, mT.tOdstavec - 1);
                                    List<MyCasovaZnacka> pCasoveZnackyPredchoziho = myDataSource[pTagPredchoziho].VratCasoveZnackyTextu;

                                    myDataSource.UpravCasElementu(pTagPredchoziho, new TimeSpan(-2), myDataSource.VratCasElementuKonec(mT));    //koncovy cas elementu je nastaven podle aktualniho

                                    OdstranOdstavec(mT.tKapitola, mT.tSekce, mT.tOdstavec); //odstrani odstavec z datove struktury

                                    //upraveni casovych indexu znacek podle predchozich
                                    for (int i = 0; i < pCasoveZnackyMazaneho.Count; i++)
                                    {
                                        pCasoveZnackyMazaneho[i].Index1 += t.Length;
                                        pCasoveZnackyMazaneho[i].Index2 += t.Length;
                                    }
                                    pCasoveZnackyPredchoziho.AddRange(pCasoveZnackyMazaneho);
                                    myDataSource[pTagPredchoziho].UlozTextOdstavce(t + s, pCasoveZnackyPredchoziho);
                                    //kvuli pozdejsi editaci
                                    if (s == null || s == "")
                                    {
                                        pUpravitOdstavec = true;
                                    }



                                    //((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Document = new FlowDocument(new Paragraph(new Run(t + s)));
                                    MySetup.Setup.CasoveZnackyText = t + s;
                                    MySetup.Setup.CasoveZnacky = pCasoveZnackyPredchoziho;
                                    ///((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Document = VytvorFlowDocumentOdstavce(myDataSource[pTagPredchoziho));
                                    ((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Text = myDataSource[pTagPredchoziho].Text;

                                    ///TextPointer sel ;

                                    ///sel = ((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Document.ContentStart.GetPositionAtOffset(pDelka-2);


                                    ///((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Selection.Select(sel, sel);
                                    ((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).SelectionStart = pDelka;
                                    ((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).SelectionLength = 0;


                                    ((Grid)spSeznam.Children[index - 1]).Children[0].Focus();
                                    UpdateXMLData();
                                }
                                e.Handled = true;

                            }
                        }

                    }

                }
                else if (e.Key == Key.Delete)
                {
                    string tr2 = ((TextBox)(sender)).Text.Substring(((TextBox)(sender)).SelectionStart);
                    int j2 = tr2.Length;



                    if ((j2 == 0) && mT.JeOdstavec)
                    {
                        this.pUpravitOdstavec = false; //odstavec je upraven jiz zde, a nebude dale upravovan v udalosti text change

                        tr2 = ((TextBox)(sender)).Text;
                        string s2 = tr2;
                        s2 = s2.Replace("\n\r", "\r");       //nahrazeni \n za \r kvuli odradkovani
                        s2 = s2.Replace("\r\n", "\r");       //nahrazeni \n za \r kvuli odradkovani
                        s2 = s2.Replace("\n", "\r");       //nahrazeni \n za \r kvuli odradkovani

                        List<MyCasovaZnacka> pCasoveZnackyAktualniho = myDataSource[mT].VratCasoveZnackyTextu;

                        ///int pDelka2 = flowDoc2.ContentStart.GetOffsetToPosition(flowDoc2.ContentEnd);  //delka ve "znacich" kvuli nastaveni kurzoru
                        int pDelka2 = ((TextBox)(sender)).Text.Length;  //delka ve "znacich" kvuli nastaveni kurzoru

                        if (mT.tOdstavec > -1 || s2.Length == 0)
                        {


                            if (index > 0 && mT.tOdstavec < ((MySection)((MyChapter)myDataSource.Chapters[mT.tKapitola]).Sections[mT.tSekce]).Paragraphs.Count - 1)
                            {
                                MyTag pTagNasledujicihoOdstavce = new MyTag(mT.tKapitola, mT.tSekce, mT.tOdstavec + 1);

                                ///flowDoc2 = ((RichTextBox)((Grid)spSeznam.Children[index + 1]).Children[0]).Document;
                                ///zac2 = flowDoc2.ContentStart;
                                ///kon2 = flowDoc2.ContentEnd.GetPositionAtOffset(-1);
                                ///tr2 = new TextRange(zac2, kon2);
                                tr2 = ((TextBox)((Grid)spSeznam.Children[index + 1]).Children[0]).Text;
                                string t2 = tr2;
                                t2 = t2.Replace("\n\r", "\r");       //nahrazeni \n za \r kvuli odradkovani
                                t2 = t2.Replace("\r\n", "\r");       //nahrazeni \n za \r kvuli odradkovani
                                t2 = t2.Replace("\n", "\r");       //nahrazeni \n za \r kvuli odradkovani
                                List<MyCasovaZnacka> pCasoveZnackyNasledujiciho = myDataSource[pTagNasledujicihoOdstavce].VratCasoveZnackyTextu;

                                myDataSource.UpravCasElementu(mT, new TimeSpan(-2), myDataSource.VratCasElementuKonec(pTagNasledujicihoOdstavce));    //koncovy cas elementu je nastaven podle nasledujiciho

                                waveform1.SelectionBegin = myDataSource.VratCasElementuPocatek(mT);
                                waveform1.SelectionEnd =   myDataSource.VratCasElementuKonec(mT);

                                OdstranOdstavec(mT.tKapitola, mT.tSekce, mT.tOdstavec + 1);
                                //prepocitani novych casovych znacek
                                if (pCasoveZnackyNasledujiciho != null)
                                {
                                    for (int i = 0; i < pCasoveZnackyNasledujiciho.Count; i++)
                                    {
                                        pCasoveZnackyNasledujiciho[i].Index1 += s2.Length;
                                        pCasoveZnackyNasledujiciho[i].Index2 += s2.Length;
                                    }
                                    pCasoveZnackyAktualniho.AddRange(pCasoveZnackyNasledujiciho);
                                }
                                myDataSource[mT].UlozTextOdstavce(s2 + t2, pCasoveZnackyAktualniho);   //ulozeni zmen do akktualniho odstavce
                                if (t2 == "")
                                {
                                    pUpravitOdstavec = true;
                                }


                                MySetup.Setup.CasoveZnackyText = s2 + t2;
                                MySetup.Setup.CasoveZnacky = pCasoveZnackyAktualniho;
                                ((TextBox)((Grid)spSeznam.Children[index]).Children[0]).Text = myDataSource[mT].Text;
                                ((TextBox)((Grid)spSeznam.Children[index]).Children[0]).SelectionStart = pDelka2;
                                ((TextBox)((Grid)spSeznam.Children[index]).Children[0]).SelectionLength = 0;
                                UpdateXMLData();
                                e.Handled = true;

                            }
                        }

                    }

                }


            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

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
            if (e.Key == Key.Escape)
            {
                TextBox pTb = (TextBox)VratSenderTextboxu(((MyTag)((TextBox)tbFonetickyPrepis).Tag));
                pTb.Focus();
                return;
            }
        }



        private bool DekodujZavolejPrikaz(int aIndexPrikazu)
        {
            try
            {
                if (pSeznamNahranychMaker != null)
                {
                    foreach (MyMakro i in pSeznamNahranychMaker)
                    {
                        if (aIndexPrikazu == i.indexMakra)
                        {
                            ZobrazRozpoznanyPrikaz(i.hodnotaVraceni);
                            break;
                        }
                    }
                }

                switch (aIndexPrikazu)
                {
                    case 10:
                        MSoubor_Novy_Click(null, new RoutedEventArgs());
                        break;
                    case 20:
                        MSoubor_Otevrit_Titulky_Click(null, new RoutedEventArgs());
                        break;
                    case 30:
                        MSoubor_Otevrit_Zvukovy_Soubor_Click(null, new RoutedEventArgs());
                        break;
                    case 40:
                        MSoubor_Otevrit_Video_Click(null, new RoutedEventArgs());
                        break;
                    case 50:
                        MSoubor_Ulozit_Click(null, new RoutedEventArgs());
                        break;
                    case 60:
                        MSoubor_Ulozit_Titulky_Jako_Click(null, new RoutedEventArgs());
                        break;
                    case 200:
                        MNastroje_Nastaveni_Click(null, new RoutedEventArgs());
                        break;
                    case 220:
                        MNastroje_Nastav_Mluvciho_Click(null, new RoutedEventArgs());
                        break;
                    case 301:
                        MUpravy_Nova_Kapitola_Click(null, new RoutedEventArgs());
                        break;
                    case 302:
                        MUpravy_Nova_Sekce_Click(null, new RoutedEventArgs());
                        break;

                    case 400:
                        MNapoveda_Popis_Programu_Click(null, new RoutedEventArgs());
                        break;
                    case 401:
                        MNapoveda_O_Programu_Click(null, new RoutedEventArgs());
                        break;

                    case 500:
                        if (myDataSource != null) myDataSource.Ulozeno = false;
                        this.Close();
                        break;
                    case 505:   //maximalizovat
                        if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal; else this.WindowState = WindowState.Maximized;
                        break;
                    case 506:   //minimalizovat
                        if (this.WindowState == WindowState.Minimized) this.WindowState = WindowState.Normal; else this.WindowState = WindowState.Minimized;
                        break;
                    case 550:   //konec hlasoveho ovladani
                        btHlasoveOvladani_Click(null, new RoutedEventArgs());
                        break;

                    //case 1000:
                    //    Audio_PlayPause();
                    //    break;
                    //case 1001:
                    //    Audio_PlayPause();
                    //    break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ZpracujPovelHlasovehoRozpoznavace(string s)
        {
            int pVystupniPrikaz = -1;
            try
            {

                if (s.Contains("MAKRO:"))
                {
                    s = s.Replace("SHOW_TEXTBLOCK:", "SHOW_TEXTBLOCK");
                    string[] pS = s.Split(':');
                    if (pS.Length > 1)
                    {
                        int pPocetPrikazu = pS.Length / 2;
                        int[] pIndexPrikazu = new int[pPocetPrikazu];
                        try
                        {
                            for (int i = 0; i < pPocetPrikazu; i++)
                            {
                                pIndexPrikazu[i] = int.Parse(pS[i * 2 + 1]);
                            }
                        }
                        catch
                        {

                        }


                        if (s.Contains("SHOW_TEXTBLOCK"))
                        {
                            if (pSeznamZpracovanychPrikazuRozpoznavace.Count < pIndexPrikazu.Length)
                            {
                                pSeznamZpracovanychPrikazuRozpoznavace.Clear();
                                pSeznamZpracovanychPrikazuRozpoznavace = new List<int>(pIndexPrikazu);
                                if (pSeznamZpracovanychPrikazuRozpoznavace.Count > 0) pVystupniPrikaz = pSeznamZpracovanychPrikazuRozpoznavace[pSeznamZpracovanychPrikazuRozpoznavace.Count - 1];
                            }
                        }
                        else if (s.Contains("FIX_TEXTBLOCK"))
                        {
                            if (pSeznamZpracovanychPrikazuRozpoznavace.Count < pIndexPrikazu.Length)
                            {
                                pVystupniPrikaz = pIndexPrikazu[pIndexPrikazu.Length - 1];
                                pSeznamZpracovanychPrikazuRozpoznavace.Clear();
                            }
                            else if (pSeznamZpracovanychPrikazuRozpoznavace.Count == pIndexPrikazu.Length)
                            {
                                pSeznamZpracovanychPrikazuRozpoznavace.Clear();
                            }
                            else
                            {
                                pSeznamZpracovanychPrikazuRozpoznavace.Clear();  //sem by to nemelo dojit
                            }

                        }


                        if (pIndexPrikazu.Length >= 0)
                        {
                            for (int k = 0; k < menu1.Items.Count; k++)
                            {
                                if (menu1.Items[k].GetType() == new MenuItem().GetType())
                                {
                                    MenuItem pMI = ((MenuItem)menu1.Items[k]);

                                    if (pMI.Tag != null && pMI.Tag.ToString() == pIndexPrikazu.ToString())
                                    {
                                        pMI.Focus();

                                    }
                                }
                            }

                        }


                    }

                }
                if (pVystupniPrikaz > -1)
                {
                    string sss = pVystupniPrikaz.ToString();
                    DekodujZavolejPrikaz(pVystupniPrikaz);
                    spSeznam.Visibility = Visibility.Hidden;
                    spSeznam.Visibility = Visibility.Visible;

                }

                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }


        }


    }


}