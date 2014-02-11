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
using System.Security.Permissions;
using System.Security;
using System.Xml.Linq;
using System.Reflection;
using Microsoft.Win32;


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

        private MySubtitlesData m_mydatasource;
        public MySubtitlesData myDataSource
        {
            get { return m_mydatasource; }
            set
            {
                if (m_mydatasource != null)
                    m_mydatasource.SubtitlesChanged -= m_mydatasource_SubtitlesChanged;
                m_mydatasource = value;
                if (m_mydatasource != null)
                    m_mydatasource.SubtitlesChanged += m_mydatasource_SubtitlesChanged;

                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("myDataSource"));
            }
        }

        void m_mydatasource_SubtitlesChanged()
        {
            waveform1.InvalidateSpeakers();
        }

        /// <summary>
        /// databaze mluvcich konkretniho programu
        /// </summary>
        public MySpeakers myDatabazeMluvcich;
        //public static MySetup MySetup = null;          //trida pro nastaveni vlastnosti aplikace

        //public static bool spustenoOknoNapovedy = false;        //informace o spustenem oknu napovedy
        WinHelp oknoNapovedy;                                   //okno s napovedou

        /// <summary>
        /// trida starajici se o prevod multimedialnich souboru a nacitani bufferu pro zobrazeni a rozpoznani
        /// </summary>
        private MyWav oWav = null;

        /// <summary>
        /// trida starajici se o beh automatickeho prepisovace
        /// </summary>
        private MyPrepisovac oPrepisovac = null;


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

        private int m_pIndexBufferuVlnyProPrehrani = 0;
        private int pIndexBufferuVlnyProPrehrani
        {
            get { return m_pIndexBufferuVlnyProPrehrani; }
            set
            {
                m_pIndexBufferuVlnyProPrehrani = value;

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
        ContextMenu ContextMenuGridX;
        ContextMenu ContextMenuVlnaImage;
        ContextMenu ContextMenuVideo;


        /// <summary>
        /// nastavi formulari jazyk
        /// </summary>
        public void NastavJazyk(MyEnumJazyk aJazyk)
        {
            MySetup.Setup.jazykRozhranni = aJazyk;
            if (aJazyk == MyEnumJazyk.cestina) return;
            if (aJazyk == MyEnumJazyk.anglictina)
            {

                btNormalizovat.Content = "Normalization (F9)";
                btOdstranitNefonemy.Content = "Delete Nonphonemes (F11)";


                lPocatek.Content = "Beg.";
                lKonec.Content = "End:";
                btPriraditVyber.Content = "Assign selection to element";


                tiInfo.Header = "Document info";


                lDatum.Content = "Date and time:";
                lZdroj.Content = "Source:";
                lTyp.Content = "Type:";


                //menu
                menuSoubor.Header = "File";
                menuSouborExportovat.Header = "Export...";
                menuSouborKonec.Header = "Close";
                menuSouborNovyPrepis.Header = "New transcription";
                menuSouborOtevritAudio.Header = "Open audio file";
                menuSouborOtevritPrepis.Header = "Open transcription";
                menuSouborOtevritVideo.Header = "Open video file";
                menuSouborUlozit.Header = "Save";
                menuSouborUlozitJako.Header = "Save as...";

                menuUpravy.Header = "Edit";
                menuUpravyKapitola.Header = "New chapter";
                menuUpravySekce.Header = "New section at...";
                menuUpravySmazat.Header = "Delete element";

                menuNastroje.Header = "Tools";
                menuNastrojeFonetika.Header = "Phonetic transcription";
                menuNastrojeFonetikaFonetika.Header = "Automatic phonetic transcription";
                menuNastrojeFonetikaZobrazit.Header = "Show phonetic transcription";
                menuNastrojeNastaveni.Header = "Option";
                menuNastrojeNastavMluvciho.Header = "Speaker selection";
                menuNastrojeNormalizace.Header = "Normalization";
                menuNastrojeObrazekMluvciho.Header = "Speaker picture from video";

                menuPrepisovac.Header = "Voice recognition";
                menuPrepisovacDiktat.Header = "Dictate";
                menuPrepisovacHlasoveOvladani.Header = "Voice control";
                menuPrepisovacPrepis.Header = "Automatic audio recognition";

                menuNapoveda.Header = "Help";
                menuNapovedaOProgramu.Header = "About";
                menuNapovedaPopis.Header = "Shortcuts";




            }
        }


        /// <summary>
        /// zobrazi informace o vyberu vlny na formular
        /// </summary>
        public void ZobrazInformaceVyberu()
        {
            if (waveform1.SelectionEnd >= waveform1.SelectionBegin)
            {
                TimeSpan ts = waveform1.SelectionBegin;
                lAudioIndex1.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
                ts = waveform1.SelectionEnd;
                lAudioIndex2.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
            }
            else
            {
                TimeSpan ts = waveform1.SelectionEnd;
                lAudioIndex1.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
                ts = waveform1.SelectionBegin;
                lAudioIndex2.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
            }
        }

        public Window1()
        {
            InitializeComponent();

            try
            {

                if (MyKONST.VERZE == MyEnumVerze.Externi)
                {
                    //odstraneni automatickeho rozpoznavani - zalozka
                    tabControl1.Items.RemoveAt(1);
                    //odstraneni automatickeho rozpoznavani - menu
                    menu1.Items.RemoveAt(3);

                }


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
                myDatabazeMluvcich = new MySpeakers();

                string fname = System.IO.Path.GetFullPath(MySetup.Setup.CestaDatabazeMluvcich);
                if (fname.Contains(FilePaths.ProgramDirectory))
                {
                    if (!FilePaths.WriteToAppData)
                    {
                        myDatabazeMluvcich = myDatabazeMluvcich.Deserializovat(MySetup.Setup.CestaDatabazeMluvcich);
                    }
                    else
                    {
                        string fname2 = System.IO.Path.Combine(FilePaths.AppDataDirectory, fname.Substring(FilePaths.ProgramDirectory.Length));
                        if (File.Exists(fname2))
                        {
                            myDatabazeMluvcich = myDatabazeMluvcich.Deserializovat(fname2);
                        }
                        else if (File.Exists(MySetup.Setup.CestaDatabazeMluvcich))
                        {
                            myDatabazeMluvcich = myDatabazeMluvcich.Deserializovat(MySetup.Setup.CestaDatabazeMluvcich);
                        }
                    }
                }
                else
                {
                    myDatabazeMluvcich = myDatabazeMluvcich.Deserializovat(MySetup.Setup.CestaDatabazeMluvcich);
                }


                //vytvoreni kontext. menu pro oblast s textem
                ContextMenuGridX = new ContextMenu();
                MenuItem menuItemX = new MenuItem();
                menuItemX.Header = "Nastav mluvčího";
                menuItemX.InputGestureText = "Ctrl+M";
                menuItemX.Click += new RoutedEventHandler(menuItemX_Nastav_Mluvciho_Click);

                MenuItem menuItemX2 = new MenuItem();
                menuItemX2.Header = "Nová sekce";
                menuItemX2.InputGestureText = "F5";
                menuItemX2.Click += new RoutedEventHandler(menuItemX2_Nova_Sekce_Click);

                MenuItem menuItemX2b = new MenuItem();
                menuItemX2b.Header = "Nová sekce na pozici";
                menuItemX2b.InputGestureText = "Shift+F5";
                menuItemX2b.Click += new RoutedEventHandler(menuItemX2b_Nova_Sekce_Click);

                MenuItem menuItemX3 = new MenuItem();
                menuItemX3.Header = "Nová kapitola";
                menuItemX3.InputGestureText = "F4";
                menuItemX3.Click += new RoutedEventHandler(menuItemX3_Nova_Kapitola_Click);

                MenuItem menuItemX4 = new MenuItem();
                menuItemX4.Header = "Smazat...";
                menuItemX4.InputGestureText = "Shift+Del";
                menuItemX4.Click += new RoutedEventHandler(menuItemX4_Smaz_Click);

                MenuItem menuItemX7 = new MenuItem();
                menuItemX7.Header = "Exportovat záznam";
                menuItemX7.InputGestureText = "Ctrl+Shift+X";
                menuItemX7.Click += new RoutedEventHandler(menuItemX7_Click);

                ContextMenuGridX.Items.Add(menuItemX);
                ContextMenuGridX.Items.Add(new Separator());
                ContextMenuGridX.Items.Add(menuItemX3);
                ContextMenuGridX.Items.Add(menuItemX2);
                ContextMenuGridX.Items.Add(menuItemX2b);
                ContextMenuGridX.Items.Add(new Separator());
                ContextMenuGridX.Items.Add(menuItemX4);
                ContextMenuGridX.Items.Add(new Separator());
                ContextMenuGridX.Items.Add(menuItemX7);

                //menu pro image
                ContextMenuVlnaImage = new ContextMenu();
                MenuItem menuItemVlna1 = new MenuItem();
                menuItemVlna1.Header = "Přiřadit čas začátku elementu";
                menuItemVlna1.InputGestureText = "Ctrl+Home";
                menuItemVlna1.Click += new RoutedEventHandler(menuItemVlna1_prirad_zacatek_Click);
                MenuItem menuItemVlna2 = new MenuItem();
                menuItemVlna2.Header = "Přiřadit čas konce elementu";
                menuItemVlna2.InputGestureText = "Ctrl+End";
                menuItemVlna2.Click += new RoutedEventHandler(menuItemVlna1_prirad_konec_Click);
                MenuItem menuItemVlna3 = new MenuItem();
                menuItemVlna3.Header = "Přiřadit čas výběru elementu";
                menuItemVlna3.Click += new RoutedEventHandler(menuItemVlna1_prirad_vyber_Click);
                MenuItem menuItemVlna4 = new MenuItem();
                menuItemVlna4.Header = "Přidej čas. značku odstavce";
                menuItemVlna4.InputGestureText = "Ctrl+Mezerník";
                menuItemVlna4.Click += new RoutedEventHandler(menuItemVlna1_prirad_casovou_znacku_Click);
                MenuItem menuItemVlna5 = new MenuItem();
                menuItemVlna5.Header = "Automatické rozpoznání výběru";
                menuItemVlna5.Click += new RoutedEventHandler(menuItemVlna1_automaticke_rozpoznavani_useku_Click);
                ContextMenuVlnaImage.Items.Add(menuItemVlna1);
                ContextMenuVlnaImage.Items.Add(menuItemVlna2);
                ContextMenuVlnaImage.Items.Add(new Separator());
                ContextMenuVlnaImage.Items.Add(menuItemVlna3);

                waveform1.ContextMenu = ContextMenuVlnaImage;

                //menu pro video
                ContextMenuVideo = new ContextMenu();
                MenuItem menuItemVideoPoriditFotku = new MenuItem();
                menuItemVideoPoriditFotku.Header = "Sejmout obrázek mlvčího";
                menuItemVideoPoriditFotku.Click += new RoutedEventHandler(menuItemVideoPoriditFotku_Click);
                ContextMenuVideo.Items.Add(menuItemVideoPoriditFotku);
                gVideoPouze.ContextMenu = ContextMenuVideo;


                VirtualizingListBox.ContextMenu = ContextMenuGridX;
            }
            catch (Exception ex)
            {
                MessageBox.Show("chyba" + ex.Message);
            }


            try
            {
                //oWav = new MyWav(new ExampleCallback(ResultCallback), new BufferCallback(ResultCallbackBuffer), 1000000);
                oWav = new MyWav();
                oWav.HaveData += oWav_HaveData;
                oWav.HaveFileNumber += oWav_HaveFileNumber;
                oWav.TemporaryWavesDone += new EventHandler(oWav_TemporaryWavesDone);
            }
            catch (Exception ex)
            {
                MessageBox.Show("chyba2" + ex.Message);
            }
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
                if (_playing && oWav != null && oWav.Nacteno)
                {
                    TimeSpan pOmezeniMS = new TimeSpan(-1);
                    if (prehratVyber)
                    {
                        pOmezeniMS = waveform1.SelectionEnd;
                    }

                    short[] bfr = waveform1.GetAudioData(TimeSpan.FromMilliseconds(pIndexBufferuVlnyProPrehrani), TimeSpan.FromMilliseconds(150), pOmezeniMS);
                    zacatekbufferums = pIndexBufferuVlnyProPrehrani;
                    pIndexBufferuVlnyProPrehrani += 150;

                    if (pIndexBufferuVlnyProPrehrani > oWav.DelkaSouboruMS)
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
            MyEventArgs2 e2 = (MyEventArgs2)e;
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<MyEventArgs2>(ZobrazProgressPrevoduSouboru), e2);
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
        private void ZobrazProgressPrevoduSouboru(MyEventArgs2 e)
        {
            pbPrevodAudio.Value = e.souborCislo;
            waveform1.ProgressHighlightBegin = TimeSpan.Zero;
            waveform1.ProgressHighlightEnd = TimeSpan.FromMilliseconds(e.msCelkovyCas);


            if (e.msCelkovyCas >= oWav.DelkaSouboruMS)
            {
                if (MySetup.Setup.jazykRozhranni == MyEnumJazyk.anglictina)
                {
                    ZobrazStavProgramu("Conversion complete!");
                }
                else
                    ZobrazStavProgramu("Převod souboru dokončen!");
                pbPrevodAudio.Visibility = Visibility.Hidden;
                lPrevodAudia.Visibility = Visibility.Hidden;
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

            MyEventArgs me = (MyEventArgs)e;
            if (me.IDBufferu == MyKONST.ID_ZOBRAZOVACIHO_BUFFERU_VLNY)
            {
                waveform1.SetAudioData(me.data, TimeSpan.FromMilliseconds(me.pocatecniCasMS), TimeSpan.FromMilliseconds(me.koncovyCasMS));
                if (me.pocatecniCasMS == 0)
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
            else if (me.IDBufferu == MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU)
            {
                oPrepisovac.bufferProPrepsani = new MyBuffer16(me.koncovyCasMS - me.pocatecniCasMS);
                oPrepisovac.bufferProPrepsani.UlozDataDoBufferu(me.data, me.pocatecniCasMS, me.koncovyCasMS);
            }
            else if (me.IDBufferu == MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS)
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

        Brush GetRectangleBgColor(MyEnumParagraphAttributes param)
        {
            return Brushes.White;
        }

        Brush GetRectangleInnenrColor(MyEnumParagraphAttributes param)
        {
            switch (param)
            {
                default:
                case MyEnumParagraphAttributes.None:
                    return Brushes.White;
                case MyEnumParagraphAttributes.Background_noise:
                    return Brushes.DodgerBlue;
                case MyEnumParagraphAttributes.Background_speech:
                    return Brushes.Chocolate;
                case MyEnumParagraphAttributes.Junk:
                    return Brushes.Crimson;
                case MyEnumParagraphAttributes.Narrowband:
                    return Brushes.Olive;
            }
        }


        void gridX_MouseUp(object sender, MouseButtonEventArgs e)
        {

            bool cc = ((TextBox)((Grid)sender).Children[0]).Focus();
        }

        #region RclickMEnu

        //--------------------------------menu videa---------------------------------------------
        void menuItemVideoPoriditFotku_Click(object sender, RoutedEventArgs e)
        {
            CommandTakeSpeakerSnapshotFromVideo.Execute(null, this);
        }


        //--------------------------------obsluha context menu pro gridy v listboxu--------------------------------------------------
        void menuItemX_Nastav_Mluvciho_Click(object sender, RoutedEventArgs e)
        {
            CommandAssignSpeaker.Execute(null, null);
        }

        void menuItemX2_Nova_Sekce_Click(object sender, RoutedEventArgs e)
        {
            CommandNewSection.Execute(null, null);
        }

        void menuItemX2b_Nova_Sekce_Click(object sender, RoutedEventArgs e)
        {
            CommandInsertNewSection.Execute(null, null);
        }

        void menuItemX3_Nova_Kapitola_Click(object sender, RoutedEventArgs e)
        {
            CommandNewChapter.Execute(null, null);
        }

        void menuItemX4_Smaz_Click(object sender, RoutedEventArgs e)
        {
            CommandDeleteElement.Execute(null, null);
        }

        static Encoding win1250 = Encoding.GetEncoding("windows-1250");

        void menuItemX7_Click(object sender, RoutedEventArgs e)
        {
            CommandExportElement.Execute(null, null);

        }
        #endregion

        public bool NoveTitulky()
        {
            try
            {
                if (myDataSource != null && !myDataSource.Ulozeno)
                {
                    MessageBoxResult mbr = MessageBox.Show("Přepis není uložený. Chcete ho nyní uložit? ", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (mbr == MessageBoxResult.Cancel || mbr == MessageBoxResult.None)
                    {
                        return false;
                    }
                    else if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                    {
                        if (mbr == MessageBoxResult.Yes)
                        {
                            if (myDataSource.JmenoSouboru != null)
                            {
                                if (!UlozitTitulky(false, myDataSource.JmenoSouboru)) return false;
                            }
                            else
                            {
                                if (!UlozitTitulky(true, myDataSource.JmenoSouboru)) return false;
                            }
                        }
                        myDataSource = new MySubtitlesData();

                        this.Title = MyKONST.NAZEV_PROGRAMU + " [novy]";

                        var c = new MyChapter();
                        var s = new MySection();
                        var p = new MyParagraph();
                        c.Add(s);
                        s.Add(p);
                        p.Phrases.Add(new MyPhrase());

                        myDataSource.Add(c);

                        VirtualizingListBox.ActiveTransctiption = p;
                        return true;
                    }


                }
                else
                {
                    myDataSource = new MySubtitlesData();
                    this.Title = MyKONST.NAZEV_PROGRAMU + " [novy]";
                    var c = new MyChapter("Kapitola 0");
                    var s = new MySection("Sekce 0");
                    var p = new MyParagraph();
                    p.Add(new MyPhrase());
                    c.Add(s);
                    s.Add(p);
                    myDataSource.Add(c);
                    myDataSource.Ulozeno = true;
                    VirtualizingListBox.ActiveTransctiption = p;
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri vytvareni novych titulku " + ex.Message, "Chyba");
                return false;
            }
        }




        public bool OtevritTitulky(bool pouzitOpenDialog, string jmenoSouboru, bool aDavkovySoubor)
        {
            try
            {
                if (myDataSource != null && !myDataSource.Ulozeno)
                {
                    MessageBoxResult mbr = MessageBox.Show("Přepis není uložený. Chcete ho nyní uložit? ", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (mbr == MessageBoxResult.Cancel || mbr == MessageBoxResult.None)
                    {
                        return false;
                    }
                    else if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                    {
                        if (mbr == MessageBoxResult.Yes)
                        {
                            if (myDataSource.JmenoSouboru != null)
                            {
                                if (!UlozitTitulky(false, myDataSource.JmenoSouboru)) return false;
                            }
                            else
                            {
                                if (!UlozitTitulky(true, myDataSource.JmenoSouboru)) return false;
                            }
                        }
                    }
                }


                if (myDataSource == null) myDataSource = new MySubtitlesData();
                if (pouzitOpenDialog)
                {
                    Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

                    fileDialog.Title = "Otevřít soubor s titulky...";
                    //fdlg.InitialDirectory = @"c:\" ;
                    fileDialog.Filter = "Soubory titulků (*" + MySetup.Setup.PriponaTitulku + ")|*" + MySetup.Setup.PriponaTitulku;
                    if (MyKONST.VERZE == MyEnumVerze.Interni)
                    {
                        fileDialog.Filter += "|Soubory (*.trsx) |*.trsx";
                        fileDialog.Filter = "Podporované typy (*.trsx, *.xml)|*.trsx;*.xml;|" + fileDialog.Filter;
                    }
                    fileDialog.Filter += "|Všechny soubory (*.*)|*.*";
                    fileDialog.FilterIndex = 1;
                    fileDialog.RestoreDirectory = true;

                    blockfocus = true;
                    if (fileDialog.ShowDialog() == true)
                    {
                        if (myDataSource != null && !myDataSource.Ulozeno)
                        {
                            MessageBoxResult mbr = MessageBox.Show("Přepis není uložený. Chcete ho nyní uložit? ", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            if (mbr == MessageBoxResult.Cancel || mbr == MessageBoxResult.None)
                            {
                                return false;
                            }
                            else if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                            {
                                if (mbr == MessageBoxResult.Yes)
                                {
                                    if (myDataSource.JmenoSouboru != null)
                                    {
                                        if (!UlozitTitulky(false, myDataSource.JmenoSouboru)) return false;
                                    }
                                    else
                                    {
                                        if (!UlozitTitulky(true, myDataSource.JmenoSouboru)) return false;
                                    }
                                }
                            }
                        }

                        MySubtitlesData pDataSource = null;
                        pDataSource = MySubtitlesData.Deserialize(fileDialog.FileName);


                        if (pDataSource == null)
                        {
                            NoveTitulky();
                            //pDataSource = new MySubtitlesData();                            
                        }
                        else
                        {
                            myDataSource = pDataSource;


                            myDataSource.Ulozeno = true;

                            //nacteni audio souboru pokud je k dispozici
                            if (!string.IsNullOrEmpty(myDataSource.mediaURI) && myDataSource.JmenoSouboru != null)
                            {
                                FileInfo fiA = new FileInfo(myDataSource.mediaURI);
                                string pAudioFile = null;
                                if (fiA.Exists)
                                {
                                    pAudioFile = fiA.FullName;
                                }
                                else
                                {
                                    FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                                    pAudioFile = fi.Directory.FullName + "\\" + myDataSource.mediaURI;
                                }
                                if (pAudioFile.Split(new string[] { ":\\" }, StringSplitOptions.None).Length == 2)
                                {
                                    FileInfo fi2 = new FileInfo(pAudioFile);
                                    if (fi2.Exists)
                                    {
                                        NactiAudio(pAudioFile);
                                    }
                                }
                            }

                            if (myDataSource.videoFileName != null && myDataSource.JmenoSouboru != null)
                            {
                                FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                                string pVideoFile = fi.Directory.FullName + "\\" + myDataSource.videoFileName;
                                FileInfo fi2 = new FileInfo(pVideoFile);
                                if (fi2.Exists && (meVideo.Source == null || meVideo.Source.AbsolutePath.ToUpper() != pVideoFile.ToUpper()))
                                {
                                    NactiVideo(pVideoFile);
                                }
                            }


                            //synchronizace mluvcich podle vnitrni databaze
                            try
                            {
                                foreach (MySpeaker i in myDataSource.Speakers.Speakers)
                                {
                                    MySpeaker pSp = myDatabazeMluvcich.NajdiSpeakeraSpeaker(i.FullName);
                                    if (i.FullName == pSp.FullName && i.FotoJPGBase64 == null)
                                    {
                                        i.FotoJPGBase64 = pSp.FotoJPGBase64;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                        VirtualizingListBox.ActiveTransctiption = myDataSource.First(e => e.IsParagraph) ?? myDataSource.First();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (aDavkovySoubor)
                    {
                        try
                        {
                            MySubtitlesData pDataSource = null;
                            FileInfo fi = new FileInfo(jmenoSouboru);
                            if (fi != null && fi.Exists)
                            {
                                FileInfo[] files = fi.Directory.GetFiles("*.XML");
                                for (int i = 0; i < files.Length; i++)
                                {
                                    if (files[i].Name.ToUpper() == fi.Name.ToUpper().Replace(".TXT", "_PHONETIC.XML"))
                                    {
                                        pDataSource = MySubtitlesData.Deserialize(files[i].FullName);
                                        break;
                                    }
                                }
                                if (pDataSource == null)
                                {
                                    pDataSource = new MySubtitlesData();
                                    FileStream fs = new FileStream(fi.FullName, FileMode.Open);
                                    StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("windows-1250"));
                                    string pText = sr.ReadToEnd();
                                    sr.Close();
                                    fs.Close();

                                    var c = new MyChapter("");
                                    var s = new MySection("");
                                    var p = new MyParagraph() { Begin = TimeSpan.Zero };
                                    c.Add(s);
                                    s.Add(p);
                                    pDataSource.Add(c);



                                    pDataSource.JmenoSouboru = fi.FullName.ToUpper().Replace(".TXT", "_PHONETIC.XML");

                                }
                                myDataSource = pDataSource;
                                string pWav = fi.FullName.ToUpper().Replace(".TXT", ".WAV");
                                NactiAudio(pWav);

                            }
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        myDataSource = MySubtitlesData.Deserialize(jmenoSouboru);
                    }
                    if (myDataSource != null)
                    {
                        this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                        //nacteni audio souboru pokud je k dispozici
                        if (myDataSource.mediaURI != null && myDataSource.JmenoSouboru != null)
                        {
                            FileInfo fiA = new FileInfo(myDataSource.mediaURI);
                            string pAudioFile = null;
                            if (fiA.Exists)
                            {
                                pAudioFile = fiA.FullName;
                            }
                            else
                            {
                                FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                                pAudioFile = fi.Directory.FullName + "\\" + myDataSource.mediaURI;
                            }
                            if (pAudioFile.Split(new string[] { ":\\" }, StringSplitOptions.None).Length == 2)
                            {

                                FileInfo fi2 = new FileInfo(pAudioFile);
                                if (fi2.Exists && (!oWav.Nacteno || oWav.CestaSouboru.ToUpper() != pAudioFile.ToUpper()))
                                {
                                    NactiAudio(pAudioFile);
                                }
                            }
                        }

                        if (myDataSource.videoFileName != null && myDataSource.JmenoSouboru != null)
                        {
                            FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                            string pVideoFile = fi.Directory.FullName + "\\" + myDataSource.videoFileName;
                            FileInfo fi2 = new FileInfo(pVideoFile);
                            if (fi2.Exists && (meVideo.Source == null || meVideo.Source.AbsolutePath.ToUpper() != pVideoFile.ToUpper()))
                            {
                                NactiVideo(pVideoFile);
                            }
                        }
                        return true;
                    }
                    else
                    {
                        NoveTitulky();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri nacitani titulku: " + ex.Message, "Chyba");
                return false;
            }
        }


        public bool UlozitTitulky(bool useSaveDialog, string jmenoSouboru)
        {
            try
            {
                string savePath = myDataSource.JmenoSouboru;
                if (useSaveDialog)
                {
                    Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();

                    fileDialog.Title = "Uložit soubor s titulky...";
                    fileDialog.Filter = "Soubory titulků (*" + MySetup.Setup.PriponaTitulku + ")|*" + MySetup.Setup.PriponaTitulku + "|Všechny soubory (*.*)|*.*";
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

                if (myDataSource.Serialize(savePath, MySetup.Setup.UkladatKompletnihoMluvciho))
                {
                    this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri ukladani titulku: " + ex.Message, "Chyba");
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

        public void InitializeTimerRozpoznavace(long aIntervalMS)
        {

            timerRozpoznavace.Interval = new TimeSpan(0, 0, 0, 0, (int)aIntervalMS);
            timerRozpoznavace.Tick += new EventHandler(timerRozpoznavace_Tick);
            timerRozpoznavace.IsEnabled = true;

        }

        void timerRozpoznavace_Tick(object sender, EventArgs e)
        {
            try
            {


                if (oPrepisovac != null && oPrepisovac.Inicializovano)
                {
                    oPrepisovac.GetText();
                    oPrepisovac.GetDelay();
                    oPrepisovac.AsynchronniRead();
                }

            }
            catch
            {

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
        private bool NactiAudio(string aFileName)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
                openDialog.Title = "Otevřít zvukový soubor...";
                openDialog.InitialDirectory.Contains("d:\\");
                openDialog.Filter = "Multimediální soubory (*.wav, *.mp3, *.wma, *.avi, *.mpg, *.wmv)|*.wav;*.mp3;*.wma;*.avi;*.mpg;*.mpeg;*.wmv|Zvukové soubory (*.wav, *.mp3, *.wma)|*.wav;*.mp3;*.wma|Všechny soubory (*.*)|*.*";
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
                        myDataSource.mediaURI = fi.Name;
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
                        ZobrazStavProgramu("Probíhá převod vybraného audio souboru na podporovaný formát...");
                        pbPrevodAudio.Visibility = Visibility.Visible;
                        lPrevodAudia.Visibility = Visibility.Visible;
                        /////////////
                    }
                    else
                    {
                        MessageBox.Show("Soubor, který se pokoušíte přehrát není podporován nebo je poškozený", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            NactiAudio(null);
        }

        /// <summary>
        /// spusti proceduru pro nacitani videa, pokud je zadana cesta, pokusi se nacist dany soubor
        /// </summary>
        /// <param name="aFileName"></param>
        /// <returns></returns>
        private bool NactiVideo(string aFileName)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
                openDialog.Title = "Otevřít video soubor...";
                openDialog.Filter = "Video soubor (*.avi, *.mpg, *.wmv)|*.avi;*.mpg;*.mpeg;*.wmv|Všechny soubory (*.*)|*.*";
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
                        myDataSource.videoFileName = tbVideoSoubor.Text;
                    }
                    catch
                    {

                    }
                    tabControl1.SelectedIndex = 0;

                    if (oWav != null && oWav.CestaSouboru != null && oWav.CestaSouboru != "")
                    {
                        if (!pOtevrit && MessageBox.Show("Chcete použít jako zdroj audia načítaný video soubor?", "Otázka:", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            NactiAudio(openDialog.FileName);
                        }
                    }
                    else
                    {
                        //automaticke nacteni audia
                        NactiAudio(openDialog.FileName);
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

        delegate bool DelegatVyberElementu(TranscriptionElement aTag, bool aNezastavovatPrehravani);

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
                MWP = new MyWavePlayer(MySetup.Setup.audio.VystupniZarizeniIndex, 4800, WOP_ChciData);
                return true;
            }
            catch
            {
                return false;
            }

        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            mediaElement1.Stop();

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
            NactiVideo(null);
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
            new WinSpeakers((MyParagraph)VirtualizingListBox.ActiveTransctiption, MySetup.Setup, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();
        }

        private void MNastroje_Nastaveni_Click(object sender, RoutedEventArgs e)
        {
            MySetup.Setup = WinSetup.WinSetupNastavit(MySetup.Setup, myDatabazeMluvcich);
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
            if (myDataSource != null && !myDataSource.Ulozeno)
            {
                MessageBoxResult mbr = MessageBox.Show("Přepis není uložený. Chcete ho nyní uložit? ", "Varování", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (mbr == MessageBoxResult.Yes || mbr == MessageBoxResult.No)
                {
                    if (mbr == MessageBoxResult.Yes)
                    {
                        if (myDataSource.JmenoSouboru != null)
                        {
                            if (!UlozitTitulky(false, myDataSource.JmenoSouboru)) e.Cancel = true;
                        }
                        else
                        {
                            if (!UlozitTitulky(true, myDataSource.JmenoSouboru)) e.Cancel = true;
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
                if (m_findDialog != null && m_findDialog.IsLoaded)
                    m_findDialog.Close();


                if (MWP != null)
                {
                    MWP.Dispose();
                    MWP = null;
                }

                if (oPrepisovac != null)
                {
                    oPrepisovac.Dispose();
                    oPrepisovac = null;
                }

                if (oWav != null)
                {
                    oWav.Dispose();
                }


                //ulozeni databaze mluvcich - i externi databaze
                if (myDatabazeMluvcich != null)
                {
                    string fname = System.IO.Path.GetFullPath(MySetup.Setup.CestaDatabazeMluvcich);
                    if (fname.StartsWith(FilePaths.ProgramDirectory))//kdyz je to v adresari (program files..)
                    {
                        if (!FilePaths.WriteToAppData) //checkni jestli muzes zapisovat
                            myDatabazeMluvcich.Serialize_V1(fname, myDatabazeMluvcich);
                        else
                            myDatabazeMluvcich.Serialize_V1(FilePaths.AppDataDirectory + fname.Substring(FilePaths.ProgramDirectory.Length), myDatabazeMluvcich);
                    }
                    else //neni to u me neresit prava
                    {
                        myDatabazeMluvcich.Serialize_V1(MySetup.Setup.CestaDatabazeMluvcich, myDatabazeMluvcich);
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

            InitCommands();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            //inicializuje (asynchronni) nacitani slovniku

            Thread t = new Thread(
                delegate()
                {
                    if (SpellChecker.LoadVocabulary())
                    {
                        this.Dispatcher.Invoke(new Action(
                    delegate()
                    {
                        VirtualizingListBox.SubtitlesContentChanged();

                    }
                    ));
                    }
                }
                ) { Name = "Spellchecking_Load" };
            t.Start();

            phoneticTranscription.Text = "";
            phoneticTranscription.button1.Visibility = Visibility.Collapsed;
            phoneticTranscription.checkBox1.Visibility = Visibility.Collapsed;
            phoneticTranscription.stackPanel1.Visibility = Visibility.Collapsed;
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
            if (App.Startup_ARGS != null && App.Startup_ARGS.Length > 0)
            {
                pCesta = App.Startup_ARGS[0];
            }
            if (pCesta == null)
            {
                NoveTitulky();
            }
            else
            {
                OtevritTitulky(false, pCesta, false);
            }

            VirtualizingListBox.RequestTimePosition += delegate(out TimeSpan value) { value = waveform1.CaretPosition; };
            VirtualizingListBox.RequestPlaying += delegate(out bool value) { value = Playing; };
            VirtualizingListBox.RequestPlayPause += delegate() { CommandPlayPause.Execute(null, null); };


            LoadPlugins();

            foreach(var p in m_ExportPlugins)
            {
                var mi = new MenuItem() { Header = p.ToString(), Tag = p};
                mi.Click += menuSouborExportovatClick;
                menuSouborExportovat.Items.Add(mi);
            }

            foreach (var p in m_ImportPlugins)
            {
                var mi = new MenuItem() { Header = p.ToString(), Tag = p };
                mi.Click += menuSouborImportovatClick;
                menuSouborImportovat.Items.Add(mi);
            }

        }


        private void menuSouborExportovatClick(object sender, RoutedEventArgs e)
        {
            Plugin p = (Plugin)((MenuItem)sender).Tag;
            p.ExecuteExport(myDataSource);
        }

        private void menuSouborImportovatClick(object sender, RoutedEventArgs e)
        {
            Plugin p = (Plugin)((MenuItem)sender).Tag;
            MySubtitlesData data = p.ExecuteImport();
            if(data!=null)
                myDataSource = data;
            

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

        /// <summary>
        /// spusti rozpoznavani vybraneho elementu... pokud je element sekce a kapitola, zkusi rozpoznat jednotlive odstavce - DODELAT!!!!!!!!
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="aPocatekMS"></param>
        /// <param name="aKonecMS"></param>
        /// <returns></returns>
        private bool SpustRozpoznavaniVybranehoElementu(TranscriptionElement aTag, TimeSpan aPocatekMS, TimeSpan aKonecMS, bool aIgnorovatTextOdstavce)
        {


            //TODO:(x) rozpoznani elementu
            MessageBox.Show("Automatické fonetické rozpoznáni není v této verzi podporováno", "Oznámení", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
            #region sbaleno
            /*
            if (oPrepisovac != null && (oPrepisovac.Rozpoznavani || oPrepisovac.Ukoncovani))
            {
                if (MessageBox.Show("Dochází k automatickému rozpoznávání jiného úkolu. Nejprve musíte zastavit předchozí rozpoznávání.\nChcete ho nyní přerušit?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (oPrepisovac.StopHned() == 0)
                    {

                    }
                }
                return false;
            }



            if (aTag == null)
            {
                MessageBox.Show("Není vybrán ani nastaven žádný element k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }

            TimeSpan pPocatekMS;
            TimeSpan pKonceMS;
            TimeSpan pDelkaMS;



            if (aPocatekMS >= TimeSpan.Zero && aKonecMS >= TimeSpan.Zero)
            {
                pPocatekMS = aPocatekMS;
                pKonceMS = aKonecMS;
            }
            else
            {
                //kontrola audio dat jestli jsou vybrana
                pPocatekMS = myDataSource.VratCasElementuPocatek(aTag);
                pKonceMS = myDataSource.VratCasElementuKonec(aTag);
            }
            pDelkaMS = pKonceMS - pPocatekMS;

            if (!oWav.Nacteno)
            {
                MessageBox.Show("Není načten žádný audio soubor pro přepis! Automatický přepis nebude spuštěn.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (pDelkaMS <= TimeSpan.Zero)
            {
                MessageBox.Show("Vybraný element nemá přiřazena žádná audio data k přepsání. Nejprve je vyberte.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (pPocatekMS < TimeSpan.Zero || pPocatekMS.TotalMilliseconds > oWav.DelkaSouboruMS)
            {
                MessageBox.Show("Počátek audio dat elementu je mimo audio soubor! Automatický přepis nebude spuštěn.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (pKonceMS < TimeSpan.Zero || pKonceMS.TotalMilliseconds > oWav.DelkaSouboruMS)
            {
                MessageBox.Show("Konec audio dat elementu je mimo audio soubor! Automatický přepis nebude spuštěn.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }

            pPozadovanyStavRozpoznavace = MyKONST.ROZPOZNAVAC_0_OFFLINE_ROZPOZNAVANI;

            //pridani kapitoly pokud neexistuje
            if (aTag.tKapitola < 0)
            {
                aTag = PridejKapitolu(-1, "Kapitola automatického přepisu");
            }
            //testovani zda je element kapitola a dotazy na jeho plnost
            if (aTag.JeKapitola)
            {
                MyChapter pChapter = myDataSource.VratKapitolu(aTag);
                if (pChapter == null)
                {
                    MessageBox.Show("Není vybrán ani nastaven žádný element k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }
                if (pChapter.hasSection)
                {
                    if (MessageBox.Show("Vybraná kapitola již obsahuje sekce. Chcete je všechny smazat a začít s přepisem?", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                    {
                        //odstrani predesle sekce
                        for (int i = 0; i < pChapter.Sections.Count; i++)
                        {
                            OdstranSekci(aTag.tKapitola, i);
                        }


                    }
                    else return false;
                }
                aTag = PridejSekci(aTag.tKapitola, "Sekce automatického přepisu", -1, -1, pChapter.Begin, pChapter.End);
            }

            //testovani sekce
            if (aTag.JeSekce)
            {
                pSeznamOdstavcuKRozpoznani = new List<MyTag>();

                MySection pSection = myDataSource.VratSekci(aTag);
                if (pSection == null)
                {
                    MessageBox.Show("Není vybrán ani nastaven žádný element k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }

                if (pSection.hasParagraph)
                {
                    if (MessageBox.Show("Vybraná sekce již obsahuje odstavce. Chcete je všechny automaticky přepsat? Text uvnitř bude smazán.", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                    {

                        //odstavce k prepsani
                        for (int i = 0; i < pSection.Paragraphs.Count; i++)
                        {

                            //uprava tagu, aby obsahoval i sender na textbox, pokud byly vytvareny
                            MyTag ppTag = new MyTag(aTag.tKapitola, aTag.tSekce, i);
                            ppTag.tSender = VratSenderTextboxu(ppTag);
                            if (ppTag.tSender != null) ((TextBox)ppTag.tSender).Clear();
                            pSeznamOdstavcuKRozpoznani.Add(ppTag);
                        }
                        aTag = pSeznamOdstavcuKRozpoznani[0];
                        pSeznamOdstavcuKRozpoznani.RemoveAt(0);
                        aIgnorovatTextOdstavce = true;
                    }
                    else return false;
                }
                else
                {
                    aTag = PridejOdstavec(aTag.tKapitola, aTag.tSekce, "", null, -1, pSection.Begin, pSection.End, myDataSource.SeznamMluvcich.VratSpeakera(pSection.speaker));
                }
            }



            if (aTag.JeOdstavec)
            {
                MyParagraph pParagraph = myDataSource[aTag];
                if (pParagraph == null) return false;
                if (pParagraph.Delka <= TimeSpan.Zero)
                {
                    MessageBox.Show("Vybraný element nemá přiřazena žádná audio data k přepsání. Nejprve je vyberte.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }
                if (pParagraph.Phrases.Count > 0 && pParagraph.Text.Length > 0)
                {
                    if (aIgnorovatTextOdstavce)
                    {
                        pParagraph.UlozTextOdstavce("", null);
                    }
                    else
                    {
                        if (MessageBox.Show("Vybraný odstavec obsahuje text. Chcete přesto začít s přepisem? Data budou přepsána", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                        {
                            pParagraph.UlozTextOdstavce("", null);
                        }
                        else return false;
                    }
                }

            }
            else
            {
                MessageBox.Show("Není vybrán ani nastaven žádný element k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }





            aTag.tSender = VratSenderTextboxu(aTag);




            if (oPrepisovac == null) oPrepisovac = new MyPrepisovac(MySetup.Setup.AbsolutniAdresarRozpoznavace, MySetup.Setup.rozpoznavac.Mluvci, MySetup.Setup.rozpoznavac.JazykovyModel, MySetup.Setup.rozpoznavac.PrepisovaciPravidla, MySetup.Setup.rozpoznavac.LicencniServer, MySetup.Setup.rozpoznavac.LicencniSoubor, MySetup.Setup.rozpoznavac.DelkaInternihoBufferuPrepisovace, MySetup.Setup.rozpoznavac.KvalitaRozpoznavaniDiktat, new EventHandler(oPrepisovac_HaveDataPrectena));

            string pMluvci = MySetup.Setup.rozpoznavac.Mluvci;
            string pJazykovyModel = MySetup.Setup.rozpoznavac.JazykovyModel;
            MySpeaker pSpeaker = myDataSource.VratSpeakera(aTag);
            if (pSpeaker != null)
            {
                if (pSpeaker.RozpoznavacMluvci != null) pMluvci = MySetup.Setup.rozpoznavac.MluvciRelativniAdresar + "/" + pSpeaker.RozpoznavacMluvci;
                if (pSpeaker.RozpoznavacJazykovyModel != null) pJazykovyModel = MySetup.Setup.rozpoznavac.JazykovyModelRelativniAdresar + "/" + pSpeaker.RozpoznavacJazykovyModel;
            }
            oPrepisovac.InicializaceRozpoznavace(MySetup.Setup.AbsolutniAdresarRozpoznavace, MySetup.Setup.rozpoznavac.LicencniSoubor, pMluvci, pJazykovyModel, MySetup.Setup.rozpoznavac.PrepisovaciPravidla, MySetup.Setup.rozpoznavac.DelkaInternihoBufferuPrepisovace.ToString(), null);

            //oPrepisovac.InicializaceRozpoznavace();
            //oPrepisovac.HaveDataPrectena += new DataReadyEventHandler(oPrepisovac_HaveDataPrectena);
            if (timerRozpoznavace != null && timerRozpoznavace.IsEnabled == false)
            {
                InitializeTimerRozpoznavace(MyKONST.PERIODA_TIMERU_ROZPOZNAVACE_MS);
            }

            //inicializace prepisovace a hlidaciho timeru
            //vrati vybrany odstavec
            MyParagraph pOdstavec = myDataSource[aTag];
            //ulozi tag vybraneho odstavce do promenne prepisovace a vymaze pripadna drivejsi data z pomocnych promennych
            oPrepisovac.PrepisovanyElementTag = aTag;

            //pokus o uzamceni textboxu proti upravam psani
            try
            {
                ((TextBox)oPrepisovac.PrepisovanyElementTag.tSender).IsReadOnly = true;
            }
            catch
            {

            }
            oPrepisovac.PrepsanyText = "";
            oPrepisovac.PrepsanyTextCasoveZnacky = new List<MyCasovaZnacka>();

            //spusti asynchronni nacteni bufferu
            if (pOdstavec.Delka> TimeSpan.Zero)
            {
                oWav.AsynchronniNacteniRamce2((long)pOdstavec.Begin.TotalMilliseconds, (long)pOdstavec.Delka.TotalMilliseconds, MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU);
            }
            ZmenStavTlacitekRozpoznavace(true, false, false, true);
            //NASLEDNE SE ceka na INITIALIZED a pak je spusteno rozpoznavani
            return true;
             */
            #endregion

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

        private void btFonetickyPrepis_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Automatické fonetické rozpoznáni není v této verzi podporováno", "Oznámení", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
            //TODO:(x) foneticky prepis
            /*
            try
            {
                MyFonetic mf = new MyFonetic(MySetup.Setup.absolutniCestaEXEprogramu);
                tbphoneticTranscription.Text = mf.VratFonetickyPrepis(VirtualizingListBox.ActiveTransctiption.Text);
            }
            catch
            {

            }*/
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


        private void MSoubor_Exportovat_Click(object sender, RoutedEventArgs e)
        {
            ExportovatDokument(myDataSource, null, ExportType.TxtTimes);
        }

        private void menuSouborExportovatText_Click(object sender, RoutedEventArgs e)
        {
            ExportovatDokument(myDataSource, null, ExportType.Txt);
        }

        private enum ExportType
        {
            Txt,
            TxtTimes,
        }

        /// <summary>
        /// exportuje dokument do vybraneho formatu - pokud je cesta null, zavola savedialog
        /// </summary>
        /// <param name="aDokument"></param>
        /// <param name="aCesta"></param>
        /// <param name="aFormat"></param>
        /// <returns></returns>
        private int ExportovatDokument(MySubtitlesData aDokument, string aCesta, ExportType aFormat)
        {
            try
            {
                if (aCesta == null)
                {
                    Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();

                    fileDialog.Title = "Exportovat dokument...";
                    //fileDialog.Filter = "Soubory titulků (*" + nastaveniAplikace.PriponaTitulku + ")|*" + nastaveniAplikace.PriponaTitulku + "|Všechny soubory (*.*)|*.*";
                    fileDialog.Filter = "Dokument korpusu (*" + ".txt" + ")|*" + ".txt" + "|Všechny soubory (*.*)|*.*";
                    fileDialog.FilterIndex = 1;
                    fileDialog.OverwritePrompt = true;
                    fileDialog.RestoreDirectory = true;
                    if (fileDialog.ShowDialog() == true)
                    {
                        aCesta = fileDialog.FileName;

                        if (aFormat == ExportType.TxtTimes)
                        {
                            FileStream fs = new FileStream(aCesta, FileMode.Create);
                            StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("windows-1250"));
                            FileInfo fi = new FileInfo(aCesta);
                            string pNazev = fi.Name.ToUpper().Remove(fi.Name.Length - fi.Extension.Length);
                            string pHlavicka = "<" + pNazev + ";";
                            for (int i = 0; i < aDokument.Speakers.Speakers.Count; i++)
                            {
                                if (i > 0) pHlavicka += ",";
                                pHlavicka += " " + aDokument.Speakers.Speakers[i].FirstName;
                            }
                            pHlavicka += ">";
                            sw.WriteLine(pHlavicka);
                            sw.WriteLine();
                            for (int i = 0; i < aDokument.Chapters.Count; i++)
                            {
                                for (int j = 0; j < aDokument.Chapters[i].Sections.Count; j++)
                                {
                                    for (int k = 0; k < aDokument.Chapters[i].Sections[j].Paragraphs.Count; k++)
                                    {
                                        MyParagraph pP = aDokument.Chapters[i].Sections[j].Paragraphs[k];
                                        //zapsani jednotlivych odstavcu
                                        string pRadek = "<" + (pP.speakerID - 1).ToString() + "> ";
                                        for (int l = 0; l < pP.Phrases.Count; l++)
                                        {
                                            MyPhrase pFraze = pP.Phrases[l];
                                            pRadek += "[" + (pFraze.Begin).ToString() + "]" + pFraze.Text;
                                        }
                                        sw.WriteLine(pRadek);
                                    }
                                }
                            }

                            sw.Close();
                        }
                        else if (aFormat == ExportType.Txt)
                        {
                            StringBuilder sb = new StringBuilder();
                            if (myDataSource.Count > 0)
                            {
                                TranscriptionElement e = myDataSource[0];
                                while (e != null)
                                {
                                    if (e.IsParagraph)
                                    {
                                        sb.AppendLine(e.Text);
                                    }
                                    e = e.Next();
                                }
                            }

                            File.WriteAllText(aCesta, sb.ToString());

                        }
                    }
                    else return -3;
                }
                return -2;
            }
            catch
            {
                return -1;
            }

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
                    OtevritTitulky(false, o[0].ToString(), false);
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

            if (myDataSource != null)
            {
                MemoryStream ms = new MemoryStream();
                myDataSource.Serialize(ms, true);
                ms.Close();
                Back_data.Add(ms.ToArray());
            }

        }

        private void button3_Click_1(object sender, RoutedEventArgs e)
        {
            if (Back_data.Count == 0)
                return;


            byte[] state = Back_data[Back_data.Count - 1];
            Back_data.RemoveAt(Back_data.Count - 1);
            //myDataSource
            try
            {
                MemoryStream ms = new MemoryStream(state);
                myDataSource = MySubtitlesData.Deserialize(ms);
                if (myDataSource != null)
                {
                    this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                    //nacteni audio souboru pokud je k dispozici
                    if (myDataSource.mediaURI != null && myDataSource.JmenoSouboru != null)
                    {
                        FileInfo fiA = new FileInfo(myDataSource.mediaURI);
                        string pAudioFile = null;
                        if (fiA.Exists)
                        {
                            pAudioFile = fiA.FullName;
                        }
                        else
                        {
                            FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                            pAudioFile = fi.Directory.FullName + "\\" + myDataSource.mediaURI;
                        }
                        FileInfo fi2 = new FileInfo(pAudioFile);
                        if (fi2.Exists && (!oWav.Nacteno || oWav.CestaSouboru.ToUpper() != pAudioFile.ToUpper()))
                        {
                            NactiAudio(pAudioFile);
                        }
                    }

                    if (myDataSource.videoFileName != null && myDataSource.JmenoSouboru != null)
                    {
                        FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                        string pVideoFile = fi.Directory.FullName + "\\" + myDataSource.videoFileName;
                        FileInfo fi2 = new FileInfo(pVideoFile);
                        if (fi2.Exists && (meVideo.Source == null || meVideo.Source.AbsolutePath.ToUpper() != pVideoFile.ToUpper()))
                        {
                            NactiVideo(pVideoFile);
                        }
                    }
                }
                else
                {
                    NoveTitulky();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri obnoveni stavu: " + ex.Message, "Chyba");
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


            var list = m_mydatasource.VratElementDanehoCasu(e.Value);
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
            List<MyParagraph> pl = myDataSource.VratElementDanehoCasu(waveform1.CaretPosition);

            _pozicenastav = true;
            var list = m_mydatasource.VratElementDanehoCasu(e.Value);
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
            new WinSpeakers((MyParagraph)e.Value, MySetup.Setup, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();
        }



        private void waveform1_ElementChanged(object sender, Waveform.MyTranscriptionElementEventArgs e)
        {
            VirtualizingListBox.RecreateElements(VirtualizingListBox.gridscrollbar.Value);
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
            VirtualizingListBox.SubtitlesContentChanged();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2 || e.ChangedButton == MouseButton.Middle)
                CommandPlayPause.Execute(null, null);
        }
        #region IO plugins


        private class Plugin
        {

            private string m_fileName;

            public string FileName
            {
                get { return m_fileName; }
                set { m_fileName = value; }
            } 
            bool m_input;

            public bool Input
            {
                get { return m_input; }
                set { m_input = value; }
            }
            bool m_isassembly;

            public bool Isassembly
            {
                get { return m_isassembly; }
                set { m_isassembly = value; }
            }
            string m_mask;

            public string Mask
            {
                get { return m_mask; }
                set { m_mask = value; }
            }
            string m_parameters;

            public string Parameters
            {
                get { return m_parameters; }
                set { m_parameters = value; }
            }

            Func<Stream, MySubtitlesData> m_importDelegate;

            public Func<Stream, MySubtitlesData> ImportDelegate
            {
                get { return m_importDelegate; }
                set { m_importDelegate = value; }
            }
            Func<MySubtitlesData, Stream, bool> m_exportDelegate;

            public Func<MySubtitlesData, Stream, bool> ExportDelegate
            {
                get { return m_exportDelegate; }
                set { m_exportDelegate = value; }
            }


            string m_name;

            public string Name
            {
                get { return m_name; }
                set { m_name = value; }
            }

            public Plugin(bool input, bool isassembly, string mask, string parameters, string name, Func<Stream, MySubtitlesData> importDelegate, Func<MySubtitlesData, Stream, bool> exportDelegate, string filename)
            {
                m_input = input;
                m_isassembly = isassembly;
                m_mask = mask;
                m_parameters = parameters;
                m_importDelegate = importDelegate;
                m_exportDelegate = exportDelegate;
                m_fileName = filename;
                m_name = name;
            }


            public MySubtitlesData ExecuteImport()
            {
                OpenFileDialog opf = new OpenFileDialog();
                opf.CheckFileExists = true;
                opf.CheckPathExists = true;
                opf.Filter = m_mask;

                if (opf.ShowDialog() == true)
                {
                    if (Isassembly)
                    {
                        return m_importDelegate.Invoke(File.OpenRead(opf.FileName));
                    }
                    else
                    { 
                        string inputfile = opf.FileName;
                        string tempFolder = FilePaths.TempDirectory;
                        string tempFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName())+".trsx";


                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = m_fileName;
                        psi.Arguments = string.Format(inputfile,tempFolder,tempFile);

                        Process p = new Process();
                        p.StartInfo = psi;

                        p.Start();
                        p.WaitForExit();

                        return MySubtitlesData.Deserialize(tempFile);
                        
                    }
                
                }
                return null;
            }
            public override string ToString()
            {
                return m_name;
            }

            public void ExecuteExport(MySubtitlesData data)
            {
                SaveFileDialog sf = new SaveFileDialog();

                sf.CheckPathExists = true;
                sf.Filter = m_mask;

                if (sf.ShowDialog() == true)
                {
                    if (Isassembly)
                    {
                        m_exportDelegate.Invoke(data, File.Create(sf.FileName));
                    }
                    else
                    {
                        string tempFolder = FilePaths.TempDirectory;
                        string inputfile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName()) + ".trsx";
                        string tempFile = sf.FileName;

                        data.Serialize(inputfile, true);

                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = m_fileName;
                        psi.Arguments = string.Format(inputfile, tempFolder, tempFile);

                        Process p = new Process();
                        p.StartInfo = psi;

                        p.Start();
                        p.WaitForExit();
                    }
                }
            }
        
        }


        List<Plugin> m_ImportPlugins = new List<Plugin>();
        List<Plugin> m_ExportPlugins = new List<Plugin>();
        private void LoadPlugins()
        {
            string file = FilePaths.GetReadPath(FilePaths.PluginsFile);
            string path = System.IO.Path.GetDirectoryName(file);

            using(var f = File.OpenRead(file))
            {
                var doc = XDocument.Load(f).Element("Plugins");
                var imports = doc.Element("Import").Elements("Plugin");
                var exports = doc.Element("Export").Elements("Plugin");

                foreach (var imp in imports.Where(i => i.Attribute("IsAssembly") != null))
                {
                    Assembly a = Assembly.LoadFrom(System.IO.Path.Combine(path,imp.Attribute("File").Value));
                    Type ptype = a.GetType(imp.Attribute("Class").Value);
                    MethodInfo mi = ptype.GetMethod("Import",BindingFlags.Static | BindingFlags.Public);
                    Func<Stream, MySubtitlesData> act = (Func<Stream, MySubtitlesData>)Delegate.CreateDelegate(typeof(Func<Stream, MySubtitlesData>), mi);

                    m_ImportPlugins.Add(new Plugin(true, true, imp.Attribute("Mask").Value, null, imp.Attribute("Name").Value, act, null, null));
                }

                foreach (var imp in imports.Where(i => i.Attribute("IsAssembly") == null))
                {
                    m_ImportPlugins.Add(new Plugin(true, false, imp.Attribute("Mask").Value, imp.Attribute("Parameters").Value, imp.Attribute("Name").Value, null, null, imp.Attribute("File").Value));
                }


                foreach (var exp in exports.Where(e => e.Attribute("IsAssembly") != null))
                {
                    Assembly a = Assembly.LoadFrom(System.IO.Path.Combine(path, exp.Attribute("File").Value));
                    Type ptype = a.GetType(exp.Attribute("Class").Value);
                    MethodInfo mi = ptype.GetMethod("Export", BindingFlags.Static | BindingFlags.Public);
                    Func<MySubtitlesData, Stream, bool> act = (Func<MySubtitlesData, Stream, bool>)Delegate.CreateDelegate(typeof(Func<MySubtitlesData, Stream, bool>), mi);

                    m_ExportPlugins.Add(new Plugin(true, true, exp.Attribute("Mask").Value, null, exp.Attribute("Name").Value, null, act, null));
                }

                foreach (var exp in exports.Where(i => i.Attribute("IsAssembly") == null))
                {
                    m_ExportPlugins.Add(new Plugin(true, false, exp.Attribute("Mask").Value, exp.Attribute("Parameters").Value, exp.Attribute("Name").Value, null, null, exp.Attribute("File").Value));
                }

            }
        
        }
        #endregion

    }
}