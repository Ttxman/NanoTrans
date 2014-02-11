using System;
using System.Collections.Generic;
//using System.Linq;
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


namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window
    {
        #region usb HID pedals
        static USBHIDDRIVER.USBInterface usbI = new USBInterface("vid_05f3", "pid_00ff");
        EventHandler savehandle;
		public void HidInit()
		{
            savehandle = new EventHandler(HIDhandler);
			bool conn = usbI.Connect();
			if(conn)
			{
				usbI.enableUsbBufferEvent(savehandle);
			    Thread.Sleep(5);
			  	usbI.startRead();          
			}

		} 
		FCPedal FCstatus = FCPedal.None;
		[Flags]
		public enum FCPedal: byte
		{
            None = 0x0,
			Left = 0x1,
			Middle = 0x2,
			Right = 0x4,
			Invalid = 0xFF
		}
        Key HIDsystemkey = Key.None;
        Key HIDkey = Key.None;
		public void HIDhandler(object sender, System.EventArgs e)
		{
			USBHIDDRIVER.List.ListWithEvent ev = (USBHIDDRIVER.List.ListWithEvent)sender;
			foreach(object o in ev)
			{
				if(o is byte[])
				{
					byte[] data = (byte[])o;
                    byte stat = data[1];
					if(FCstatus!= FCPedal.Invalid)
					{
						if((((byte)FCPedal.Left) & stat) !=0)
                        {
                            if ((byte)(FCPedal.Left & FCstatus) == 0) //down event
                            {
                                HIDkey = Key.System;
                                HIDsystemkey = Key.Left;
                                Window_PreviewKeyDown(null, null);
                                lAudioPozice.Dispatcher.Invoke(new KeyEventHandler(Window_PreviewKeyDown), null, null);
                            }
                        }
                        else if ((((byte)FCPedal.Middle) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Middle & FCstatus) == 0) //down event
                            {
                                HIDkey = Key.Tab;
                                HIDsystemkey = Key.None;
                                lAudioPozice.Dispatcher.Invoke(new KeyEventHandler(Window_PreviewKeyDown), null, null);
                                
                            }

                        }
                        else if ((((byte)FCPedal.Right) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Right & FCstatus) == 0) //down event
                            {
                                HIDkey = Key.System;
                                HIDsystemkey = Key.Right;
                                lAudioPozice.Dispatcher.Invoke(new KeyEventHandler(Window_PreviewKeyDown), null, null);
                            }

                        }
					}
					
					FCstatus =(FCPedal)stat;
				}
			}
			ev.Clear();
		}

    	#endregion
        WinLog mWL = null;

        /// <summary>
        /// udava v jakem stavu chceme rozpoznavac spustit
        /// </summary>
        private short pPozadovanyStavRozpoznavace;
        private List<MyTag> pSeznamOdstavcuKRozpoznani;

        private List<int> pSeznamZpracovanychPrikazuRozpoznavace = new List<int>();
        private List<MyMakro> pSeznamNahranychMaker = null;

        /// <summary>
        /// seznam ktere foneticke odstavce prepsat
        /// </summary>
        private List<MyTag> pSeznamOdstavcuKRozpoznaniFonetika;
        /// <summary>
        /// pokud je treba zanechat puvodni dokument - napr. allignmentm tak je tato promenna ruzna od mydatasource
        /// </summary>
        private MySubtitlesData pDokumentFonetickehoPrepisu;

        //timer pro posuvnik videa....
        private DispatcherTimer timer1 = new DispatcherTimer();
        private DispatcherTimer timerRozpoznavace = new DispatcherTimer();


        public MySubtitlesData myDataSource;
        /// <summary>
        /// databaze mluvcich konkretniho programu
        /// </summary>
        public MySpeakers myDatabazeMluvcich;
        public MySetup nastaveniAplikace = null;          //trida pro nastaveni vlastnosti aplikace

        //public static bool spustenoOknoNapovedy = false;        //informace o spustenem oknu napovedy
        WinHelp oknoNapovedy;                                   //okno s napovedou


        /// <summary>
        /// informace o vyberu a pozici kurzoru ve "vlne", obsahuje buffery pro zobrazeni a prehrani vlny
        /// </summary>
        private MyVlna oVlna = new MyVlna(MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS);

        /// <summary>
        /// trida starajici se o prevod multimedialnich souboru a nacitani bufferu pro zobrazeni a rozpoznani
        /// </summary>
        private MyWav oWav = null;

        /// <summary>
        /// trida starajici se o beh automatickeho prepisovace
        /// </summary>
        private MyPrepisovac oPrepisovac = null;
        /// <summary>
        /// trida starajici se o hlasove ovladani
        /// </summary>
        private MyPrepisovac oHlasoveOvladani = null;

        /// <summary>
        /// trida starajici se o foneticke prepisy
        /// </summary>
        private MyFonetic bFonetika = null;

        /// <summary>
        /// slovnik s vlastnimi slovy prepisu
        /// </summary>
        private MyFoneticSlovnik bSlovnikFonetickehoDoplneni = null;


        bool mouseDown = false; //promenna pro detekci stisknuti mysi na posuvniku videa
        /// <summary>
        /// info zda je nacitan audio soubor kvuli davce
        /// </summary>
        bool pNacitaniAudiaDavka = false;

        static bool nahled = false;

        bool jeVideo = false;

        bool prehratVyber = false;  //prehrava jen vybranou prepsanou sekci, pokud je specifikovan zacatek a konec
        /// <summary>
        /// po vybrani elementu pokracuje v prehravani
        /// </summary>
        bool pPokracovatVprehravaniPoVyberu = false;


        static long mSekundyKonec = 0;   //informace o pozici konce vykreslene vlny...
        short pocetTikuTimeru = 0;    //pomocna pro deleni tiku timeru
        bool leftCtrl = false;
        bool leftShift = false;
        bool skocitNaPoziciSlovaTextboxu = false;
        private bool pSkocitNahoru = false;
        private bool pSkocitDolu = false;
        private int pPocatecniIndexVyberu = -1;
        private int pKoncovyIndexVyberu = -1;
        private bool pZiskatNovyIndex = true;
        /// <summary>
        /// neskakat na zacatek ve vlne pri vyberu elemetu
        /// </summary>
        private bool pNeskakatNaZacatekElementu;

        /// <summary>
        /// obdelniky mluvcich pro zobrazeni v audio signalu
        /// </summary>
        private List<Button> bObelnikyMluvcich = new List<Button>();


        bool pUpravitOdstavec = true;


        /// <summary>
        /// trida pro prehravani audio dat
        /// </summary>
        private MyWavePlayer MWP = null;
        private int pIndexBufferuVlnyProPrehrani = 0;
        private DateTime pCasZacatkuPrehravani;
        private bool pZacloPrehravani = false;
        private bool _playing = false;
        private bool Playing
        {
            get { return _playing; }
            set
            {
                _playing = value;
                if (this.Dispatcher.Thread == System.Threading.Thread.CurrentThread)
                {
                    if (_playing)
                    {

                        if (oWav.Nacteno)
                        {
                            BitmapImage bi3 = new BitmapImage();
                            bi3.BeginInit();
                            bi3.UriSource = new Uri("icons/iPause.png", UriKind.Relative);
                            bi3.EndInit();

                            iPlayPause.Source = bi3;
                        }

                    }
                    else
                    {
                        BitmapImage bi3 = new BitmapImage();
                        bi3.BeginInit();
                        bi3.UriSource = new Uri("icons/iPlay.png", UriKind.Relative);
                        bi3.EndInit();
                        iPlayPause.Source = bi3;
                    }
                }
            }
        }


        /// <summary>
        /// trida pro nahravani audia
        /// </summary>
        private MyWaveRecorder MWR = null;
        /// <summary>
        /// informace zda je nahravan zvuk
        /// </summary>
   
        private bool recording = false;


        byte[] pVyrovnavaciPametNahravani = new byte[320000];    //vterina vyrovnavaci pameti pro nahravani-10s!!!!!
        int pVyrovnavaciPametIndexVrcholu = 0;

        /// <summary>
        /// pomocna promenna - pocet tiku timeru rozpoznavace - pro zobrazeni hlasoveho povelu
        /// </summary>
        private int pDelkaZobrazeniHlasovehoPovelu = 0;


        ContextMenu ContextMenuGridX;
        ContextMenu ContextMenuVlnaImage;
        ContextMenu ContextMenuVideo;


        /// <summary>
        /// nastavi formulari jazyk
        /// </summary>
        public void NastavJazyk(MyEnumJazyk aJazyk)
        {
            nastaveniAplikace.jazykRozhranni = aJazyk;
            if (aJazyk == MyEnumJazyk.cestina) return;
            if (aJazyk == MyEnumJazyk.anglictina)
            {

                btNormalizovat.Content = "Normalization (F9)";
                btOdstranitNefonemy.Content = "Delete Nonphonemes (F11)";
                chbPrehravatRozpoznane.Content = "Play after recognition";
                button1.Content = "Phonetic recognition (F10)";
                lPohlavi.Content = "Sex:";
                lJazyk.Content = "Language:";
                lPocatek.Content = "Beg.";
                lKonec.Content = "End:";
                btPriraditVyber.Content = "Assign selection to element";

                btAutomaticky.Content = "Automatic";
                btNacistAdresar.Content = "Select folder";
                chbAutomatickyRozpoznat.Content = "Recognize after loading";

                tiInfo.Header = "Document info";
                tiDavka.Header = "Batch processing";
                tiPrepis.Header = "Voice recognition";

                lDatum.Content = "Date and time:";
                lZdroj.Content = "Source:";
                lTyp.Content = "Type:";

                btAutomatickeRozpoznani.Content = "Aut. audio recognition";
                btDiktat.Content = "Dictate";
                btHlasoveOvladani.Content = "Voice control";
                lZbyvajiciData.Content = "Data to recognition (Delay)";


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
                menuUpravySmazatKonec.Header = "Remove ending time mark";
                menuUpravySmazatPocatek.Header = "Remove beginning time mark";

                menuNastroje.Header = "Tools";
                menuNastrojeFonetika.Header = "Phonetic transcription";
                menuNastrojeFonetikaFonetika.Header = "Automatic phonetic transcription";
                menuNastrojeFonetikaVarianty.Header = "Phonetic alternative";
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
        /// podle tagu zkusi vratit sendera richtextboxu
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public object VratSenderTextboxu(MyTag aTag)
        {
            try
            {
                for (int i = 0; i < spSeznam.Children.Count; i++)
                {
                    MyTag pTag = (MyTag)((TextBox)((Grid)spSeznam.Children[i]).Children[0]).Tag;
                    if (aTag.tKapitola == pTag.tKapitola && aTag.tSekce == pTag.tSekce && aTag.tOdstavec == pTag.tOdstavec)
                    {
                        return (pTag.tSender);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }

        }


        /// <summary>
        /// Zobrazi nactena XML data do listboxu
        /// </summary>
        /// <returns></returns>
        public bool ZobrazXMLData()
        {
            return ZobrazXMLData(null);
        }

        /// <summary>
        /// Zobrazi nactena XML data do listboxu
        /// </summary>
        /// <param name="aTagFocusu"></param>
        /// <returns></returns>
        public bool ZobrazXMLData(MyTag aTagFocusu)
        {
            try
            {
                if (myDataSource != null)
                {
                    //spSeznam.Children.Clear();
                    spSeznam.Children.Clear();
                    for (int i = 0; i < myDataSource.Chapters.Count; i++)
                    {
                        PridejTextBox(-1, MyKONST.PrevedTextNaFlowDocument(((MyChapter)myDataSource.Chapters[i]).name), new MyTag(i, -1, -1)); //prida textbox kapitoly
                        for (int j = 0; j < ((MyChapter)myDataSource.Chapters[i]).Sections.Count; j++)
                        {
                            PridejTextBox(-1, MyKONST.PrevedTextNaFlowDocument(((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).name), new MyTag(i, j, -1)); //prida textbox sekce
                            for (int k = 0; k < ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).Paragraphs.Count; k++)
                            {
                                //FlowDocument pFd = VytvorFlowDocumentOdstavce(((MyParagraph)((MySection)((MyChapter)myDataSource.chapters[i]).sections[j]).paragraphs[k]));
                                string pFd = ((MyParagraph)((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).Paragraphs[k]).Text;
                                PridejTextBox(-1, pFd, new MyTag(i, j, k));

                            }
                        }

                    }
                    UpdateXMLData();
                    UpdateXMLData();
                    //if (spSeznam.Children.Count > 0) ((TextBox)((Grid)spSeznam.Children[0]).Children[0]).Focus();
                    if (spSeznam.Children.Count > 0) ((TextBox)((Grid)spSeznam.Children[0]).Children[0]).Focus();
                    spSeznam.UpdateLayout();
                    if (aTagFocusu != null)
                    {
                        try
                        {
                            aTagFocusu.tSender = VratSenderTextboxu(aTagFocusu);
                            (aTagFocusu.tSender as TextBox).Focus();
                        }
                        catch
                        {

                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }



        /// <summary>
        /// THREAD SAFE, zobrazi a aktualizuje odstavce, casy, mluvci u odstavcu
        /// </summary>
        /// <returns></returns>
        public bool UpdateXMLData()
        {


            return UpdateXMLData(true, true, true, true, true);
        }

        /// <summary>
        /// thread SAFE, zobrazi a aktualizuje u listu s daty u odstavcu a ostatnich polozek jmena mluvcich, a velikkost fontu prepisu
        /// </summary>
        /// <returns></returns>
        public bool UpdateXMLData(bool aUpdateText, bool aUpdateCasy, bool aUpdateMluvci, bool aUpdateTrainingElement, bool aUpdateMluvciSignalu)
        {
            try
            {

                if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
                {
                    //do invoke stuff here
                    this.Dispatcher.Invoke(new Func<bool>(UpdateXMLData), new object[] { });
                    return false;
                }

                if (myDataSource != null)
                {
                    //info o dokumentu
                    cbZdroj.Text = this.myDataSource.source;
                    cbTypPoradu.Text = this.myDataSource.type;
                    DateTime pDatum = this.myDataSource.dateTime;
                    if (pDatum < new DateTime(1900, 1, 1)) pDatum = DateTime.Now;
                    tbDatumCasDokumentu.Text = pDatum.ToString();// this.myDataSource.dateTime.ToShortTimeString();

                    //foneticky prepis
                    if (nastaveniAplikace.fonetickyPrepis.Jazyk == null || nastaveniAplikace.fonetickyPrepis.Jazyk == "") cbJazyk.SelectedIndex = 0; else cbJazyk.Text = nastaveniAplikace.fonetickyPrepis.Jazyk;
                    if (nastaveniAplikace.fonetickyPrepis.Pohlavi == null || nastaveniAplikace.fonetickyPrepis.Pohlavi == "") cbPohlavi.SelectedIndex = 0; else cbPohlavi.Text = nastaveniAplikace.fonetickyPrepis.Pohlavi;
                    chbPrehravatRozpoznane.IsChecked = nastaveniAplikace.fonetickyPrepis.PrehratAutomatickyRozpoznanyOdstavec;




                    tbFonetickyPrepis.FontSize = nastaveniAplikace.SetupTextFontSize;

                    int defaultLeftPositionRichX = nastaveniAplikace.defaultLeftPositionRichX;

                    string pNameSpeakerPredchozi = null;
                    string pNameSpeakerPredchoziKratky = null;
                    spSeznam.UpdateLayout();
                    for (int i = 0; i < spSeznam.Children.Count; i++)
                    {
                        //update tlacitek
                        //MyTag aTag = ((MyTag)((RichTextBox)((Grid)spSeznam.Children[i]).Children[0]).Tag);
                        MyTag aTag = ((MyTag)((TextBox)((Grid)spSeznam.Children[i]).Children[0]).Tag);
                        TextBox pTB = (TextBox)((Grid)spSeznam.Children[i]).Children[0];
                        Button pBt = ((Button)(((Grid)spSeznam.Children[i]).Children[1] as StackPanel).Children[0]);
                        TextBlock pTextBlock = ((TextBlock)((StackPanel)pBt.Content).Children[0]);
                        Image pImage = ((Image)((StackPanel)pBt.Content).Children[1]);
                        FrameworkElement pEl = ((Grid)spSeznam.Children[i]).Children[2] as FrameworkElement;
                       // if (((Grid)spSeznam.Children[i]).Children[2] is Ellipse)
                       //     pEl = ((Grid)spSeznam.Children[i]).Children[2] as Ellipse;
                       // else
                       //     pEl = new Ellipse();

                        Label pLbBegin = ((Label)(((Grid)spSeznam.Children[i]).Children[1] as StackPanel).Children[1]);
                        Label pLbEnd = ((Label)(((Grid)spSeznam.Children[i]).Children[1] as StackPanel).Children[2]);
                        CheckBox pChbTrenovani = ((CheckBox)((Grid)spSeznam.Children[i]).Children[3]);

                        //zmena velikosti pisma prepisu
                        pBt.FontSize = this.nastaveniAplikace.SetupTextFontSize * 0.87;
                        pTB.FontSize = this.nastaveniAplikace.SetupTextFontSize;
                        pLbBegin.FontSize = this.nastaveniAplikace.SetupTextFontSize * 0.87;
                        pLbEnd.FontSize = this.nastaveniAplikace.SetupTextFontSize * 0.87;
                        //




                        double pDown = 0;

                        long pBegin = myDataSource.VratCasElementuPocatek(aTag);
                        long pEnd = myDataSource.VratCasElementuKonec(aTag);

                        if (aUpdateCasy)
                        {
                            if (pBegin > -1 && nastaveniAplikace.zobrazitCasBegin)
                            {
                                //pLbBegin.Background = Brushes.Red;
                                //pTop = pLbBegin.FontSize;
                                pDown += pLbBegin.FontSize;
                                pEl.Visibility = Visibility.Visible;
                                pLbBegin.Visibility = Visibility.Visible;
                                TimeSpan ts = new TimeSpan(pBegin * 10000);
                                pLbBegin.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2");
                            }
                            else
                            {
                                pEl.Visibility = Visibility.Hidden;
                                pLbBegin.Visibility = Visibility.Hidden;
                            }

                            if (pEnd > -1 && nastaveniAplikace.zobrazitCasEnd)
                            {
                                pDown += pLbEnd.FontSize;
                                pLbEnd.Visibility = Visibility.Visible;
                                TimeSpan ts = new TimeSpan(pEnd * 10000);
                                pLbEnd.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2");
                                //((Ellipse)((Grid)spSeznam.Children[i]).Children[2]).Visibility = Visibility.Visible;
                            }
                            else
                            {//((Ellipse)((Grid)spSeznam.Children[i]).Children[2]).Visibility = Visibility.Hidden;
                                pLbEnd.Visibility = Visibility.Hidden;
                            }
                        }

                        //pBt.Margin = new Thickness(0, pTop, 0, pDown);
                        //if (pDown - 0.1 > 0 && pDown < pLbBegin.FontSize+0.1) pDown = 0;
                        //pLbBegin.Margin = new Thickness(0, 0, 0, pDown - pLbBegin.FontSize);


                        if (aTag.JeOdstavec)
                        {
                            MyParagraph pP = myDataSource.VratOdstavec(aTag);
                            if (aUpdateTrainingElement)
                            {
                                if (pP != null)
                                {
                                    pChbTrenovani.IsChecked = pP.trainingElement;
                                }
                            }

                            if (aUpdateMluvci)
                            {

                                if (myDataSource.VratSpeakera(aTag).FullName != null && myDataSource.VratSpeakera(aTag).FullName != "")
                                {
                                    TextBox pRtb = ((TextBox)((Grid)spSeznam.Children[i]).Children[0]);

                                    //uprava zobrazovaneho jmena mluvciho

                                    MySpeaker pSpeaker = myDataSource.VratSpeakera(aTag);
                                    string pName = pSpeaker.FullName;
                                    if (pName != pNameSpeakerPredchozi)
                                    {
                                        pNameSpeakerPredchozi = pName;
                                        pNameSpeakerPredchoziKratky = pName;

                                        FormattedText fmtText = new FormattedText(pName, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(pTextBlock.FontFamily, pTextBlock.FontStyle, pTextBlock.FontWeight, pTextBlock.FontStretch), pTextBlock.FontSize, pTextBlock.Foreground);
                                        if (pName != null && fmtText.Width > nastaveniAplikace.maximalniSirkaMluvciho)
                                        {
                                            pBt.ToolTip = pName;
                                            while (pName.Length > 0 && fmtText.Width > nastaveniAplikace.maximalniSirkaMluvciho)
                                            {
                                                pName = pName.Remove(pName.Length - 1);
                                                fmtText = new FormattedText(pName + "...", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(pTextBlock.FontFamily, pTextBlock.FontStyle, pTextBlock.FontWeight, pTextBlock.FontStretch), pTextBlock.FontSize, pTextBlock.Foreground);
                                            }
                                            pName += "...";
                                            pNameSpeakerPredchoziKratky = pName;
                                        }
                                        else
                                        {
                                            pBt.ToolTip = null;
                                        }


                                    }
                                    else
                                    {
                                        pName = pNameSpeakerPredchoziKratky;
                                        pBt.ToolTip = null;
                                    }

                                    if (pSpeaker.Comment != null && pSpeaker.Comment != "")
                                    {
                                        if (pBt.ToolTip == null) pBt.ToolTip = ""; else pBt.ToolTip += "\n";
                                        pBt.ToolTip = pBt.ToolTip.ToString() + pSpeaker.Comment;
                                    }

                                    //pName = "0:00:00\n" + pName + "\n0:00:00";

                                    //pTextBlock.Text = myDataSource.VratSpeakera(aTag).Name;
                                    pTextBlock.Text = pName;


                                    if (nastaveniAplikace.ZobrazitFotografieMluvcich)
                                    {
                                        if (myDataSource.VratSpeakera(aTag).FullName == myDataSource.VratSpeakera(new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec - 1)).FullName && myDataSource.VratSpeakera(aTag).FullName != null && myDataSource.VratSpeakera(aTag).FullName != "")
                                        {
                                            pBt.Visibility = Visibility.Collapsed;
                                            pImage.Source = null;
                                        }
                                        else
                                        {
                                            pImage.Source = MyKONST.PrevedBase64StringNaJPG(myDataSource.VratSpeakera(aTag).FotoJPGBase64);
                                            pImage.MaxHeight = nastaveniAplikace.Fotografie_VyskaMax;
                                        }
                                    }
                                    else
                                    {
                                        pImage.Source = null;
                                    }
                                    pBt.UpdateLayout();
                                    if (pBt.Margin.Left + pBt.ActualWidth > pRtb.Margin.Left - 16)
                                    {

                                        pRtb.Margin = new Thickness(pBt.Margin.Left + pBt.ActualWidth + 15, 0, 0, 0);
                                        pEl.Margin = new Thickness(pBt.Margin.Left + pBt.ActualWidth + 5, 0, 0, 0);
                                    }
                                    else if (pBt.Margin.Left + pBt.ActualWidth <= defaultLeftPositionRichX + 35)
                                    {
                                        pRtb.Margin = new Thickness(defaultLeftPositionRichX + 35 + 8, 0, 0, 0);
                                        pEl.Margin = new Thickness(defaultLeftPositionRichX + 25, 0, 0, 0);
                                    }

                                }
                                else
                                {
                                    //((Button)((Grid)spSeznam.Children[i]).Children[1]).Content = "mluvci...";
                                    pTextBlock.Text = "mluvci...";
                                    pBt.ToolTip = null;
                                    pImage.Source = null;
                                    pTB.Margin = new Thickness(defaultLeftPositionRichX + 35 + 8, 0, 0, 0);
                                    pEl.Margin = new Thickness(defaultLeftPositionRichX + 25, 0, 0, 0);
                                }
                            }
                            if (aTag.tOdstavec == 0)
                            {
                                 ((Button)(((Grid)spSeznam.Children[i]).Children[1] as StackPanel).Children[0]).Visibility = Visibility.Visible;
                            }
                            else
                            {
                                if (myDataSource.VratSpeakera(aTag).FullName == myDataSource.VratSpeakera(new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec - 1)).FullName && myDataSource.VratSpeakera(aTag).FullName != null && myDataSource.VratSpeakera(aTag).FullName != "")
                                {
                                    pBt.Visibility = Visibility.Collapsed;
                                    pImage.Source = null;
                                }
                                else
                                {
                                    pBt.Visibility = Visibility.Visible;
                                    if (nastaveniAplikace.ZobrazitFotografieMluvcich)
                                    {
                                        pImage.Source = MyKONST.PrevedBase64StringNaJPG(myDataSource.VratSpeakera(aTag).FotoJPGBase64);
                                        pImage.MaxHeight = nastaveniAplikace.Fotografie_VyskaMax;
                                    }
                                    else
                                    {
                                        pImage.Source = null;
                                    }
                                }


                            }


                        }
                        else if (aTag.JeSekce)
                        {


                            pTB.Margin = new Thickness(pBt.Margin.Left + pBt.ActualWidth + 15, 0, 0, 0);
                            pEl.Margin = new Thickness(pBt.Margin.Left + pBt.ActualWidth + 2, 0, 0, 0);
                        }
                        if (!aTag.JeOdstavec)
                        {
                            pLbBegin.Visibility = System.Windows.Visibility.Collapsed;
                            pLbEnd.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        else
                        {
                            pLbBegin.Visibility = System.Windows.Visibility.Visible;
                            pLbEnd.Visibility = System.Windows.Visibility.Visible;
                        }

                    }
                    if (aUpdateMluvciSignalu)
                    {
                        KresliMluvciDoVlny(this.myDataSource, this.oVlna);
                    }

                    



                    return true;
                }
                else
                {

                    return false;
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        /// <summary>
        /// zobrazi informace o cas. indexech elementu na formular
        /// </summary>
        /// <param name="aTag"></param>
        public void ZobrazInformaceElementu(MyTag aTag)
        {
            try
            {
                if (myDataSource.VratCasElementuPocatek(aTag) > -1)
                {
                    TimeSpan ts = new TimeSpan(myDataSource.VratCasElementuPocatek(aTag) * 10000);
                    lAudioIndex1.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;

                }
                else
                {
                    //lAudioIndex1.Content = "-1";
                    lAudioIndex1.Content = "N/A";
                }
                if (myDataSource.VratCasElementuKonec(aTag) > -1)
                {
                    TimeSpan ts = new TimeSpan(myDataSource.VratCasElementuKonec(aTag) * 10000);
                    lAudioIndex2.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;

                }
                else
                {
                    //lAudioIndex2.Content = "-1";
                    lAudioIndex2.Content = "N/A";
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

        }

        /// <summary>
        /// zobrazi informace o vyberu vlny na formular
        /// </summary>
        public void ZobrazInformaceVyberu()
        {
            try
            {
                if (oVlna.KurzorVyberKonecMS >= oVlna.KurzorVyberPocatekMS)
                {
                    TimeSpan ts = new TimeSpan(oVlna.KurzorVyberPocatekMS * 10000);
                    lAudioIndex1.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
                    ts = new TimeSpan(oVlna.KurzorVyberKonecMS * 10000);
                    lAudioIndex2.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
                }
                else
                {
                    TimeSpan ts = new TimeSpan(oVlna.KurzorVyberKonecMS * 10000);
                    lAudioIndex1.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
                    ts = new TimeSpan(oVlna.KurzorVyberPocatekMS * 10000);
                    lAudioIndex2.Content = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2"); ;
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

        }



        /// <summary>
        /// prida kapitolu do datove struktury a vytvori textbox,vraci tag nove kapitoly...
        /// </summary>
        /// <param name="nazev_Kapitoly"></param>
        /// <returns></returns>
        public MyTag PridejKapitolu(int aIndexNoveKapitoly, string nazev_Kapitoly)
        {
            try
            {

                int index_Kapitoly = myDataSource.NovaKapitola(aIndexNoveKapitoly, nazev_Kapitoly);
                if (index_Kapitoly < 0) return null; //nezdarilo se priani kapitoly na dany index
                int index = -1;
                for (int i = 0; i <= index_Kapitoly; i++)
                {
                    index = index + 1; //zvyseni indexu o 1 kvuli radku s nazvem kapitoly
                    index = index + ((MyChapter)myDataSource.Chapters[i]).Sections.Count;
                    for (int j = 0; j <= ((MyChapter)myDataSource.Chapters[i]).Sections.Count - 1; j++)
                    {
                        index = index + ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).Paragraphs.Count;
                    }
                }
                int pomIndex = PridejTextBox(index, MyKONST.PrevedTextNaFlowDocument(nazev_Kapitoly), new MyTag(index_Kapitoly, -1, -1)); //textbox do seznamu
                for (int i = pomIndex + 1; i < spSeznam.Children.Count; i++)
                {
                    ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[i])).Children[0])).Tag)).tKapitola++;
                }
                //((RichTextBox)((Grid)spSeznam.Children[pomIndex-1]).Children[0]).Focus();
                return new MyTag(index_Kapitoly, -1, -1);

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return null;
            }
        }


        /// <summary>
        /// vlozi sekci nakonec soucasne sekce nebo na index odstavce
        /// </summary>
        /// <param name="aKapitola"></param>
        /// <param name="nazev_Sekce"></param>
        /// <param name="aIndex"></param>
        /// <param name="aIndexOdstavce"></param>
        /// <returns></returns>
        public MyTag PridejSekci(int aKapitola, string nazev_Sekce, int aIndex, int aIndexOdstavce, long aBeginMS, long aEndMS)
        {
            try
            {
                int index_Sekce = myDataSource.NovaSekce(aKapitola, nazev_Sekce, aIndex, aBeginMS, aEndMS);
                if (aIndexOdstavce > -1)
                {
                    int pom0 = ((MySection)((MyChapter)myDataSource.Chapters[aKapitola]).Sections[aIndex]).Paragraphs.Count;

                    for (int i = aIndexOdstavce; i < pom0; i++)
                    {
                        MyParagraph mp = (MyParagraph)((MySection)((MyChapter)myDataSource.Chapters[aKapitola]).Sections[aIndex]).Paragraphs[i];
                        int pomIndex = myDataSource.NovyOdstavec(aKapitola, index_Sekce, mp.Text, null, mp.begin, mp.end, -1);
                        myDataSource.ZadejSpeakera(new MyTag(aKapitola, index_Sekce, pomIndex), mp.speakerID);

                    }
                    for (int i = aIndexOdstavce; i < pom0; i++)
                    {
                        myDataSource.SmazOdstavec(aKapitola, aIndex, aIndexOdstavce);
                    }

                }
                int index = -1;
                for (int i = 0; i <= aKapitola; i++)
                {
                    index = index + 1; //zvyseni indexu o 1 kvuli radku s nazvem kapitoly
                    //index = index + ((MyChapter)myDataSource.chapters[i]).sections.Count;
                    //for (int j = 0; j <= index_Sekce; j++)
                    int pom1;
                    if (aKapitola == i) pom1 = aIndex; else pom1 = ((MyChapter)myDataSource.Chapters[i]).Sections.Count - 1;

                    for (int j = 0; j <= pom1; j++)
                    {
                        index++; //pridani radku se sekci
                        index = index + ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).Paragraphs.Count;
                    }
                }
                index++;


                PridejTextBox(index, MyKONST.PrevedTextNaFlowDocument(nazev_Sekce), new MyTag(aKapitola, index_Sekce, -1)); //textbox do seznamu

                //prepocitani nasledujicich indexu po vlozeni noveho odstavce mezi ostatni
                MyTag pom;

                index++;    //zvyseni na nasledujici index
                if ((index <= spSeznam.Children.Count - 1))
                {

                    //pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                    pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                    int pomRozdil = pom.tOdstavec;

                    while ((index <= spSeznam.Children.Count - 1) && (pom.tKapitola == aKapitola))
                    {
                        //((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tSekce++;
                        ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tSekce++;
                        if (aIndexOdstavce > -1)
                        {
                            //((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec - pomRozdil;
                            ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec - pomRozdil;
                        }
                        index++;
                        //if (index <= spSeznam.Children.Count - 1) pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                        if (index <= spSeznam.Children.Count - 1) pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));

                    }
                }

                if (aIndexOdstavce > -1)
                {
                    UpdateXMLData();
                }
                MyTag pMT = new MyTag(aKapitola, index_Sekce, -1);
                pMT.tSender = VratSenderTextboxu(pMT);
                return pMT;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return null;
            }
        }

        /// <summary>
        /// index -2, pridani textboxu hned za sekci
        /// </summary>
        /// <param name="aKapitola"></param>
        /// <param name="aSekce"></param>
        /// <param name="text_Odstavce"></param>
        /// <param name="aCasoveZnacky"></param>
        /// <param name="aIndex"></param>
        /// <param name="aBegin"></param>
        /// <param name="aEnd"></param>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public MyTag PridejOdstavec(int aKapitola, int aSekce, string text_Odstavce, List<MyCasovaZnacka> aCasoveZnacky, int aIndex, long aBegin, long aEnd, MySpeaker aSpeaker)
        {
            try
            {
                int index_Odstavce = myDataSource.NovyOdstavec(aKapitola, aSekce, text_Odstavce, aCasoveZnacky, aBegin, aEnd, aIndex);
                myDataSource.ZadejSpeakera(new MyTag(aKapitola, aSekce, index_Odstavce), aSpeaker.ID);
                int index = -1;
                for (int i = 0; i <= aKapitola; i++)
                {
                    index = index + 1; //zvyseni indexu o 1 kvuli radku s nazvem kapitoly
                    //index = index + ((MyChapter)myDataSource.chapters[i]).sections.Count;

                    int pom1;
                    if (aKapitola == i) pom1 = aSekce; else pom1 = ((MyChapter)myDataSource.Chapters[i]).Sections.Count - 1;

                    for (int j = 0; j <= pom1; j++)
                    {
                        index = index + 1;  //zvyseni indexu o 1 kvuli radku s nazvem sekce
                        index = index + ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).Paragraphs.Count;

                    }
                    if ((i == aKapitola))
                    {
                        index = index - ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[aSekce]).Paragraphs.Count;
                        index = index + index_Odstavce + 1;
                    }
                }
                MyTag mT = new MyTag(aKapitola, aSekce, index_Odstavce);
                //index = PridejTextBox(index, MyPRACE.PrevedTextNaFlowDocument(text_Odstavce), mT); //textbox do seznamu
                //index = PridejTextBox(index, VytvorFlowDocumentOdstavce(myDataSource.VratOdstavec(mT)), mT); //textbox do seznamu
                index = PridejTextBox(index, (myDataSource.VratOdstavec(mT)).Text, mT); //textbox do seznamu


                //prepocitani nasledujicich indexu po vlozeni noveho odstavce mezi ostatni
                MyTag pom;
                //index++;    //zvyseni na nasledujici index
                if ((index <= spSeznam.Children.Count - 1))
                {
                    //pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                    pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));

                    while ((index <= spSeznam.Children.Count - 1) && (pom.tKapitola == aKapitola) && (pom.tSekce == aSekce) && (pom.tOdstavec >= 1 - 1))//odectena 1... kdyztak vratit
                    {
                        //((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec++;
                        ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec++;
                        index++;
                        //if (index <= spSeznam.Children.Count-1) pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                        if (index <= spSeznam.Children.Count - 1) pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));

                    }
                }
                spSeznam.UpdateLayout();
                mT.tSender = VratSenderTextboxu(mT);
                return mT;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                MessageBox.Show("Chyba pri vkladani richtextboxu odstavce..." + ex.Message);
                return null;
            }
        }


        public bool OdstranKapitolu(int aKapitola)
        {
            try
            {
                if (oPrepisovac != null && oPrepisovac.Rozpoznavani && oPrepisovac.PrepisovanyElementTag.tKapitola == aKapitola)
                {
                    MessageBox.Show("Nelze odstranit kapitolu,která je automaticky přepisována", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                if (aKapitola < 0) return false;
                int pomPocetSekciKapitoly = ((MyChapter)myDataSource.Chapters[aKapitola]).Sections.Count;
                if (pomPocetSekciKapitoly > 0)
                {
                    MessageBoxResult mbr = MessageBox.Show("Opravdu chcete smazat vybranou kapitolu se vším co obsahuje?", "Varování", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (mbr != MessageBoxResult.Yes) return false;
                }
                if (myDataSource.SmazKapitolu(aKapitola))
                {
                    ZobrazXMLData();
                    try
                    {
                        int pKap = aKapitola;
                        if (pKap > myDataSource.Chapters.Count - 1) pKap--;
                        if (pKap < 0) pKap = 0;

                        ((TextBox)VratSenderTextboxu(new MyTag(pKap, -1, -1))).Focus();
                    }
                    catch
                    { }
                }

                return false;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        public bool OdstranSekci(int aKapitola, int aSekce)
        {
            try
            {
                if (oPrepisovac != null && oPrepisovac.Rozpoznavani && oPrepisovac.PrepisovanyElementTag.tKapitola == aKapitola && oPrepisovac.PrepisovanyElementTag.tSekce == aSekce)
                {
                    MessageBox.Show("Nelze odstranit sekci,která je automaticky přepisována", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                if (aSekce < 0) return false;
                int pomPocetOdstavcuSekce = ((MySection)((MyChapter)myDataSource.Chapters[aKapitola]).Sections[aSekce]).Paragraphs.Count;
                if (pomPocetOdstavcuSekce > 0)
                {
                    MessageBoxResult mbr = MessageBox.Show("Opravdu chcete smaazt vybranou sekci se všemi odstavci?", "Varování", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (mbr != MessageBoxResult.Yes) return false;
                }
                if (myDataSource.SmazSekci(aKapitola, aSekce))
                {
                    int index_Sekce = aSekce;
                    int index = -1;
                    for (int i = 0; i <= aKapitola; i++)
                    {
                        index = index + 1; //zvyseni indexu o 1 kvuli radku s nazvem kapitoly


                        int pom1;
                        if (aKapitola == i) pom1 = aSekce; else pom1 = ((MyChapter)myDataSource.Chapters[i]).Sections.Count;

                        for (int j = 0; j < pom1; j++)
                        {
                            index = index + 1;  //zvyseni indexu o 1 kvuli radku s nazvem sekce
                            index = index + ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).Paragraphs.Count;

                        }
                        if ((i == aKapitola))
                        {
                            index = index + 1;
                        }
                    }
                    bool pomOK = true;
                    for (int ii = 0; ii <= pomPocetOdstavcuSekce; ii++)
                    {
                        pomOK = SmazTextBox(index);
                        if (!pomOK) return false;

                    }
                    if (pomOK)
                    {
                        MyTag pom;

                        if ((index <= spSeznam.Children.Count - 1))
                        {
                            //pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                            pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));

                            while ((index <= spSeznam.Children.Count - 1) && (pom.tKapitola == aKapitola) && (pom.tSekce > aSekce))
                            {
                                //((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tSekce--;
                                ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tSekce--;
                                index++;
                                //if (index <= spSeznam.Children.Count - 1) pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                                if (index <= spSeznam.Children.Count - 1) pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));

                            }
                        }


                        return true;
                    }
                    else return false;
                }
                else return false;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                //MessageBox.Show("Chyba pri mazani richtextboxu odstavce..." + ex.Message);
                return false;
            }
        }

        public bool OdstranOdstavec(int aKapitola, int aSekce, int aIndex)
        {
            try
            {
                if (oPrepisovac != null && oPrepisovac.Rozpoznavani && oPrepisovac.PrepisovanyElementTag.tKapitola == aKapitola && oPrepisovac.PrepisovanyElementTag.tSekce == aSekce && oPrepisovac.PrepisovanyElementTag.tOdstavec == aIndex)
                {
                    MessageBox.Show("Nelze odstranit odstavec,který je automaticky přepisován", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                if (myDataSource.SmazOdstavec(aKapitola, aSekce, aIndex))
                {
                    int index_Odstavce = aIndex;
                    int index = -1;
                    for (int i = 0; i <= aKapitola; i++)
                    {
                        index = index + 1; //zvyseni indexu o 1 kvuli radku s nazvem kapitoly
                        //index = index + ((MyChapter)myDataSource.chapters[i]).sections.Count;


                        int pom1;
                        if (aKapitola == i) pom1 = aSekce; else pom1 = ((MyChapter)myDataSource.Chapters[i]).Sections.Count - 1;

                        for (int j = 0; j <= pom1; j++)
                        {
                            index = index + 1;  //zvyseni indexu o 1 kvuli radku s nazvem sekce
                            index = index + ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[j]).Paragraphs.Count;

                        }
                        if ((i == aKapitola))
                        {
                            index = index - ((MySection)((MyChapter)myDataSource.Chapters[i]).Sections[aSekce]).Paragraphs.Count;
                            index = index + index_Odstavce + 1;
                        }
                    }
                    if (SmazTextBox(index))
                    {


                        //prepocitani nasledujicich indexu po vlozeni noveho odstavce mezi ostatni
                        MyTag pom;
                        //index++;    //zvyseni na nasledujici index
                        if ((index <= spSeznam.Children.Count - 1))
                        {
                            //pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                            pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));

                            while ((index <= spSeznam.Children.Count - 1) && (pom.tKapitola == aKapitola) && (pom.tSekce == aSekce) && (pom.tOdstavec >= 1))
                            {
                                //((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec--;
                                ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag)).tOdstavec--;
                                index++;
                                //if (index <= spSeznam.Children.Count - 1) pom = ((MyTag)(((RichTextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));
                                if (index <= spSeznam.Children.Count - 1) pom = ((MyTag)(((TextBox)(((Grid)(spSeznam.Children[index])).Children[0])).Tag));

                            }
                        }


                        return true;
                    }
                    else return false;
                }
                else return false;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                MessageBox.Show("Chyba pri mazani richtextboxu odstavce..." + ex.Message);
                return false;
            }
        }



        public bool UpravCasZobraz(MyTag aTag, long aBegin, long aEnd)
        {
            return UpravCasZobraz(aTag, aBegin, aEnd, false);
        }

        /// <summary>
        /// upravi a zobrazi cas u textboxu....begin a end.. pokud -2 tak se nemeni hodnoty casu..., -1 znamena vynulovani hodnot casu pocatku nebo konce
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="aBegin"></param>
        /// <param name="aEnd"></param>
        /// <returns></returns>
        public bool UpravCasZobraz(MyTag aTag, long aBegin, long aEnd, bool aIgnorovatPrekryv)
        {
            try
            {
                MyTag pTagPredchozi = new MyTag(-1, -1, -1);     //predchozi element
                MyTag newTag2 = new MyTag(-1, -1, -1);  //nasledujici element
                MyParagraph pParPredchozi = null;
                MyParagraph pParAktualni = myDataSource.VratOdstavec(aTag);
                MyParagraph pParNasledujici = null;


                if (aTag.tOdstavec > 0)
                {
                    pTagPredchozi = new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec - 1);
                    pParPredchozi = myDataSource.VratOdstavec(pTagPredchozi);

                }
                if (aTag.tOdstavec > -1)
                {
                    newTag2 = new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec + 1);
                    pParNasledujici = myDataSource.VratOdstavec(newTag2);
                }

                long pKonecPredchoziho = myDataSource.VratCasElementuKonec(pTagPredchozi);
                long pZacatekSoucasneho = myDataSource.VratCasElementuPocatek(aTag);
                long pKonecSoucasneho = myDataSource.VratCasElementuKonec(aTag);
                long pZacatekNasledujiciho = myDataSource.VratCasElementuPocatek(newTag2);


                if (myDataSource.VratCasElementuPocatek(pTagPredchozi) > -1 && aBegin > -1)
                {
                    long pCasElementuKonec = myDataSource.VratCasElementuKonec(aTag);

                    if (pCasElementuKonec >= 0 && pCasElementuKonec < aBegin && aEnd < aBegin)
                    {
                        MessageBox.Show("Nelze nastavit počáteční čas bloku větší než jeho konec. ", "Varování", MessageBoxButton.OK);
                        //NastavPoziciKurzoru(myDataSource.VratCasElementuPocatek(aTag), true);
                        //KresliVyber(myDataSource.VratCasElementuPocatek(aTag), myDataSource.VratCasElementuKonec(aTag), myDataSource.VratCasElementuPocatek(aTag));
                        return false;
                    }

                    if (myDataSource.VratCasElementuPocatek(pTagPredchozi) <= aBegin) //dopsano ==
                    {
                        if (myDataSource.VratCasElementuKonec(pTagPredchozi) > aBegin)
                        {
                            if (myDataSource.VratSpeakera(aTag).FullName == myDataSource.VratSpeakera(pTagPredchozi).FullName && !aIgnorovatPrekryv)
                            {
                                MessageBox.Show("Nelze nastavit počáteční čas bloku nižší než konec předchozího pro stejného mluvčího ", "Varování", MessageBoxButton.OK);
                                //NastavPoziciKurzoru(myDataSource.VratCasElementuPocatek(aTag), true);
                                //KresliVyber(myDataSource.VratCasElementuPocatek(aTag), myDataSource.VratCasElementuKonec(aTag), myDataSource.VratCasElementuPocatek(aTag));
                                oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(aTag);
                                oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(aTag);
                                KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, -2);
                                return false;
                            }
                            else
                            {
                                MessageBoxResult mbr = MessageBoxResult.Yes;
                                bool pZobrazitHlasku = pKonecPredchoziho <= pZacatekSoucasneho;
                                if (!aIgnorovatPrekryv && pZobrazitHlasku) mbr = MessageBox.Show("Mluvčí se bude překrývat s předchozím, chcete toto povolit?", "Varování", MessageBoxButton.YesNoCancel);
                                if (mbr != MessageBoxResult.Yes)
                                {
                                    //NastavPoziciKurzoru(myDataSource.VratCasElementuPocatek(aTag), true, true);
                                    //KresliVyber(myDataSource.VratCasElementuPocatek(aTag), myDataSource.VratCasElementuKonec(aTag), myDataSource.VratCasElementuPocatek(aTag));
                                    oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(aTag);
                                    oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(aTag);
                                    KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, -2);
                                    return false;
                                }
                                if (!aIgnorovatPrekryv)
                                {
                                    //NastavPoziciKurzoru(aBegin, true, true);
                                    //KresliVyber(aBegin, myDataSource.VratCasElementuKonec(aTag), aBegin);
                                    KresliVyber(aBegin, myDataSource.VratCasElementuKonec(aTag), -2);
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nelze nastavit počáteční čas bloku nižší než začátek předchozího. ", "Varování", MessageBoxButton.OK);
                        //NastavPoziciKurzoru(myDataSource.VratCasElementuPocatek(aTag), true);
                        KresliVyber(myDataSource.VratCasElementuPocatek(aTag), myDataSource.VratCasElementuKonec(aTag), myDataSource.VratCasElementuPocatek(aTag));
                        return false;
                    }

                }



                myDataSource.UpravCasElementu(aTag, aBegin, -2);

                if (myDataSource.VratCasElementuKonec(newTag2) > -1 && aEnd > -1)
                {
                    if (myDataSource.VratCasElementuPocatek(aTag) > aEnd)
                    {
                        MessageBox.Show("Nelze nastavit koncový čas bloku menší než jeho počátek. ", "Varování", MessageBoxButton.OK);
                        //NastavPoziciKurzoru(myDataSource.VratCasElementuPocatek(aTag), true);
                        //KresliVyber(myDataSource.VratCasElementuPocatek(aTag), myDataSource.VratCasElementuKonec(aTag), myDataSource.VratCasElementuPocatek(aTag));
                        return false;
                    }
                    else
                        if (myDataSource.VratCasElementuPocatek(newTag2) < aEnd)
                        {
                            if (myDataSource.VratSpeakera(aTag).FullName == myDataSource.VratSpeakera(newTag2).FullName && !aIgnorovatPrekryv)
                            {
                                MessageBox.Show("Nelze nastavit koncový čas bloku vyšší než počátek následujícího pro stejného mluvčího ", "Varování", MessageBoxButton.OK);
                                //NastavPoziciKurzoru(myDataSource.VratCasElementuPocatek(aTag), true);
                                //KresliVyber(myDataSource.VratCasElementuPocatek(aTag), myDataSource.VratCasElementuKonec(aTag), myDataSource.VratCasElementuPocatek(aTag));
                                oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(aTag);
                                oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(aTag);
                                KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, -2);
                                return false;
                            }
                            else
                            {
                                MessageBoxResult mbr = MessageBoxResult.Yes;
                                bool pZobrazitHlasku = pKonecSoucasneho <= pZacatekNasledujiciho;
                                if (!aIgnorovatPrekryv && pZobrazitHlasku) mbr = MessageBox.Show("Mluvčí se bude překrývat s následujícím, chcete toto povolit?", "Varování", MessageBoxButton.YesNoCancel);
                                if (mbr != MessageBoxResult.Yes)
                                {
                                    //NastavPoziciKurzoru(myDataSource.VratCasElementuPocatek(aTag), true, false);
                                    //KresliVyber(myDataSource.VratCasElementuPocatek(aTag), myDataSource.VratCasElementuKonec(aTag), myDataSource.VratCasElementuPocatek(aTag));
                                    oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(aTag);
                                    oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(aTag);
                                    KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, -2);
                                    return false;
                                }
                                if (!aIgnorovatPrekryv)
                                {
                                    //NastavPoziciKurzoru(aBegin, true, false);
                                    KresliVyber(myDataSource.VratCasElementuPocatek(aTag), aEnd, -2);
                                }
                            }
                        }
                }




                myDataSource.UpravCasElementu(aTag, aBegin, aEnd);

                UpdateXMLData();


                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        //vlozi ruch do textboxu, sender ukazuje na vybrany textbox
        public static bool VlozRuch(object aSender, string aRuch)
        {
            try
            {
                if (aSender != null && aRuch != null)
                {
                    int aKapitola = ((MyTag)((TextBox)(aSender)).Tag).tKapitola;
                    int aSekce = ((MyTag)((TextBox)(aSender)).Tag).tSekce;
                    int aOdstavec = ((MyTag)((TextBox)(aSender)).Tag).tOdstavec;

                    /*
                    FlowDocument flowDoc = ((RichTextBox)(aSender)).Document;
                    
                    ((RichTextBox)(aSender)).Selection.Text=" "+aRuch+" ";
                    //((RichTextBox)(aSender)).Document.
                    ((RichTextBox)(aSender)).Selection.Select(((RichTextBox)(aSender)).Selection.End, ((RichTextBox)(aSender)).Selection.End);

                    Run r1 = new Run("debil");
                    r1.Background = Brushes.Red;
                    
                    Run r2 = new Run("je to tak");

                    Paragraph par = new Paragraph();
                    par.Inlines.Add(r1);
                    par.Inlines.Add(r2);
                    //flowDoc = new FlowDocument(par);

                    ((RichTextBox)(aSender)).Document = flowDoc;
                    TextRange tr = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd.GetPositionAtOffset(-1));
                    */

                    ((TextBox)aSender).SelectedText = " " + aRuch + " ";
                    ((TextBox)aSender).CaretIndex = ((TextBox)aSender).SelectionStart + ((TextBox)aSender).SelectionLength;

                    return true;
                }
                else return false;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
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


                slPoziceMedia.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(slPoziceMedia_MouseDown), true);

                //nastaveni aplikace
                this.nastaveniAplikace = new MySetup(new FileInfo(Application.ResourceAssembly.Location).DirectoryName);
                if (this.nastaveniAplikace != null)
                {
                    nastaveniAplikace = nastaveniAplikace.Deserializovat(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.KONFIGURACNI_SOUBOR);
                    nastaveniAplikace.Serializovat(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.KONFIGURACNI_SOUBOR, nastaveniAplikace);
                    NastavJazyk(nastaveniAplikace.jazykRozhranni);
                }

                if (nastaveniAplikace.SpustitLogovaciOkno)
                {
                    mWL = new WinLog();
                    mWL.Show();
                }
                //nastaveni posledni pozice okna
                if (nastaveniAplikace.OknoPozice != null)
                {
                    if (nastaveniAplikace.OknoPozice.X >= 0 && nastaveniAplikace.OknoPozice.Y >= 0)
                    {
                        this.WindowStartupLocation = WindowStartupLocation.Manual;
                        this.Left = nastaveniAplikace.OknoPozice.X;
                        this.Top = nastaveniAplikace.OknoPozice.Y;
                    }
                }
                //nastaveni posledni velikosti okna
                if (nastaveniAplikace.OknoVelikost != null)
                {
                    if (nastaveniAplikace.OknoVelikost.Width >= 50 && nastaveniAplikace.OknoVelikost.Height >= 50)
                    {
                        this.Width = nastaveniAplikace.OknoVelikost.Width;
                        this.Height = nastaveniAplikace.OknoVelikost.Height;
                    }
                }

                this.WindowState = nastaveniAplikace.OknoStav;

                bSlovnikFonetickehoDoplneni = new MyFoneticSlovnik();
                bSlovnikFonetickehoDoplneni.NacistSlovnik(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_SLOVNIK_FONETIKA_UZIVATELSKY, bSlovnikFonetickehoDoplneni.PridanaSlova);
                bSlovnikFonetickehoDoplneni.NacistSlovnik(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_SLOVNIK_FONETIKA_ZAKLADNI, bSlovnikFonetickehoDoplneni.SlovnikZakladni);

                ZobrazitOknoFonetickehoPrepisu(nastaveniAplikace.ZobrazitFonetickyPrepis - 1 > 0);

                //databaze mluvcich
                myDatabazeMluvcich = new MySpeakers();
                myDatabazeMluvcich = myDatabazeMluvcich.Deserializovat(nastaveniAplikace.CestaDatabazeMluvcich);


                //vytvoreni kontext. menu pro oblast s textem
                ContextMenuGridX = new ContextMenu();
                MenuItem menuItemX = new MenuItem();
                menuItemX.Header = "Nastav mluvčího";
                menuItemX.InputGestureText = "Ctrl+M";
                menuItemX.Click += new RoutedEventHandler(menuItemX_Nastav_Mluvciho_Click);

                MenuItem menuItemX2 = new MenuItem();
                menuItemX2.Header = "Nová sekce";
                menuItemX2.InputGestureText = "F3";
                menuItemX2.Click += new RoutedEventHandler(menuItemX2_Nova_Sekce_Click);

                MenuItem menuItemX2b = new MenuItem();
                menuItemX2b.Header = "Nová sekce na pozici";
                menuItemX2b.InputGestureText = "Shift+F3";
                menuItemX2b.Click += new RoutedEventHandler(menuItemX2b_Nova_Sekce_Click);

                MenuItem menuItemX3 = new MenuItem();
                menuItemX3.Header = "Nová kapitola";
                menuItemX3.InputGestureText = "F2";
                menuItemX3.Click += new RoutedEventHandler(menuItemX3_Nova_Kapitola_Click);

                MenuItem menuItemX4 = new MenuItem();
                menuItemX4.Header = "Smazat...";
                menuItemX4.InputGestureText = "Shift+Del";
                menuItemX4.Click += new RoutedEventHandler(menuItemX4_Smaz_Click);

                MenuItem menuItemX5 = new MenuItem();
                menuItemX5.Header = "Smazat poč. časový index";
                menuItemX5.InputGestureText = "Ctrl+Del";
                menuItemX5.Click += new RoutedEventHandler(menuItemX5_Smaz_Click);

                MenuItem menuItemX6 = new MenuItem();
                menuItemX6.Header = "Smazat kon. časový index";
                menuItemX6.Click += new RoutedEventHandler(menuItemX6_Smaz_Click);

                MenuItem menuItemX7 = new MenuItem();
                menuItemX7.Header = "Exportovat záznam";
                menuItemX7.Click += new RoutedEventHandler(menuItemX7_Click);

                ContextMenuGridX.Items.Add(menuItemX);
                ContextMenuGridX.Items.Add(new Separator());
                ContextMenuGridX.Items.Add(menuItemX3);
                ContextMenuGridX.Items.Add(menuItemX2);
                ContextMenuGridX.Items.Add(menuItemX2b);
                ContextMenuGridX.Items.Add(new Separator());
                ContextMenuGridX.Items.Add(menuItemX4);
                ContextMenuGridX.Items.Add(new Separator());
                ContextMenuGridX.Items.Add(menuItemX5);
                ContextMenuGridX.Items.Add(menuItemX6);
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
                ContextMenuVlnaImage.Items.Add(menuItemVlna5);
                ContextMenuVlnaImage.Items.Add(new Separator());
                ContextMenuVlnaImage.Items.Add(menuItemVlna4);
                myImage.ContextMenu = ContextMenuVlnaImage;
                grid1.ContextMenu = ContextMenuVlnaImage;

                //menu pro video
                ContextMenuVideo = new ContextMenu();
                MenuItem menuItemVideoPoriditFotku = new MenuItem();
                menuItemVideoPoriditFotku.Header = "Sejmout obrázek mlvčího";
                menuItemVideoPoriditFotku.Click += new RoutedEventHandler(menuItemVideoPoriditFotku_Click);
                ContextMenuVideo.Items.Add(menuItemVideoPoriditFotku);
                gVideoPouze.ContextMenu = ContextMenuVideo;

            }
            catch (Exception ex)
            {
                MessageBox.Show("chyba" + ex.Message);
            }


            try
            {
                //oWav = new MyWav(new ExampleCallback(ResultCallback), new BufferCallback(ResultCallbackBuffer), 1000000);
                oWav = new MyWav(nastaveniAplikace.absolutniCestaEXEprogramu);
                oWav.HaveData += new DataReadyEventHandler(oWav_HaveData);
                oWav.HaveFileNumber += new DataReadyEventHandler(oWav_HaveFileNumber);

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
        short[] WOP_ChciData()
        {
            try
            {
                if (MWP != null)
                {
                    if (_playing && oWav != null && oWav.Nacteno)
                    {
                        long pOmezeniMS = -1;
                        if (prehratVyber)
                        {
                            pOmezeniMS = oVlna.KurzorVyberKonecMS;
                        }

                        short[] bfr = oVlna.bufferPrehravaniZvuku.VratDataBufferuShort(pIndexBufferuVlnyProPrehrani, 150, pOmezeniMS);
  
                        pCasZacatkuPrehravani = DateTime.Now.AddMilliseconds(-150);
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
                                pIndexBufferuVlnyProPrehrani = (int)oVlna.KurzorVyberPocatekMS;
                            }
                        }

                        if (!pZacloPrehravani)
                        {
                            pZacloPrehravani = true;
                            pCasZacatkuPrehravani = DateTime.Now;
                        }

                        return bfr;
                    }
                    else //pause
                    {
                        pCasZacatkuPrehravani = DateTime.Now.AddMilliseconds(-150);
                    }
                }

                
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
               
            }
            return new short[0];
        }


        /// <summary>
        /// handle pro nahravana audio data z recorderu
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        void MWR_MamData(IntPtr data, int size)
        {
            try
            {
                if ((oPrepisovac != null && oPrepisovac.bufferProHlasoveOvladani != null) || (oHlasoveOvladani != null && oHlasoveOvladani.bufferProHlasoveOvladani != null))
                {
                    byte[] pData2 = new byte[size];

                    System.Runtime.InteropServices.Marshal.Copy(data, pData2, 0, size);

                    if (pVyrovnavaciPametIndexVrcholu > 300)
                    {
                        byte[] p = new byte[pVyrovnavaciPametIndexVrcholu + size];
                        for (int i = 0; i < pVyrovnavaciPametIndexVrcholu; i++)
                        {
                            p[i] = pVyrovnavaciPametNahravani[i];
                        }
                        for (int i = pVyrovnavaciPametIndexVrcholu; i < pVyrovnavaciPametIndexVrcholu + size; i++)
                        {
                            p[i] = pData2[i - pVyrovnavaciPametIndexVrcholu];
                        }

                        pVyrovnavaciPametIndexVrcholu = 0;

                        if (pPozadovanyStavRozpoznavace == MyKONST.ROZPOZNAVAC_1_DIKTAT)
                        {
                            oPrepisovac.AsynchronniZapsaniDat(p);
                        }
                        else
                        {
                            oHlasoveOvladani.AsynchronniZapsaniDat(p);
                        }
                    }
                    else
                    {
                        bool pDoBufferu = false;
                        if (oPrepisovac != null && oPrepisovac.TypRozpoznavani == MyKONST.ROZPOZNAVAC_1_DIKTAT && !oPrepisovac.AsynchronniZapsaniDat(pData2)) pDoBufferu = true;
                        if (oHlasoveOvladani != null && oHlasoveOvladani.TypRozpoznavani == MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI && !oHlasoveOvladani.AsynchronniZapsaniDat(pData2)) pDoBufferu = true;
                        //if ((pPozadovanyStavRozpoznavace != MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI && !oPrepisovac.AsynchronniZapsaniDat(pData2)) || (pPozadovanyStavRozpoznavace == MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI && !oHlasoveOvladani.AsynchronniZapsaniDat(pData2)))
                        if (pDoBufferu)
                        {
                            pData2.CopyTo(pVyrovnavaciPametNahravani, pVyrovnavaciPametIndexVrcholu);
                            pVyrovnavaciPametIndexVrcholu += pData2.Length;

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
        /// vraci cislo prevedeneho souboru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void oWav_HaveFileNumber(object sender, EventArgs e)
        {
            try
            {
                MyEventArgs2 e2 = (MyEventArgs2)e;
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<MyEventArgs2>(ZobrazProgressPrevoduSouboru), e2);
                if (oVlna != null && oVlna.bufferCeleVlny != null)
                {
                    ///oVlna.bufferCeleVlny.UlozDataDoBufferu(oWav.DataProVykresleniNahleduVlny, 0, oWav.PrevedenoDatMS);
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        /// <summary>
        /// zobrazi progress nacitani
        /// </summary>
        /// <param name="e"></param>
        private void ZobrazProgressPrevoduSouboru(MyEventArgs2 e)
        {
            pbPrevodAudio.Value = e.souborCislo;
            slPoziceMedia.SelectionStart = 0;
            slPoziceMedia.SelectionEnd = e.msCelkovyCas;


            if (e.msCelkovyCas >= oWav.DelkaSouboruMS)
            {
                if (nastaveniAplikace.jazykRozhranni == MyEnumJazyk.anglictina)
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
        /// automaticke nacteni vsech polozek
        /// </summary>
        bool pAutomaticky2 = false;
        /// <summary>
        /// index pri automatickem nacitani
        /// </summary>
        int pAutomatickyIndex = -1;


        /// <summary>
        /// metoda vracejici data z threadu, v argumentu e...zatim nepouzivana ale jo pouzivana
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void oWav_HaveData(object sender, EventArgs e)
        {
            try
            {
                MyEventArgs me = (MyEventArgs)e;
                if (me.IDBufferu == MyKONST.ID_ZOBRAZOVACIHO_BUFFERU_VLNY)
                {
                    oVlna.bufferPrehravaniZvuku.UlozDataDoBufferu(me.data, me.pocatecniCasMS, me.koncovyCasMS);
                    if (me.pocatecniCasMS == 0)
                    {
                        //mSekundyKonec = 0;


                        //mSekundyKonec = 0;
                        if (!timer1.IsEnabled) InitializeTimer();
                        if (oVlna.DelkaVlnyMS < 30000)
                        {
                            oVlna.NastavDelkuVlny(30000);
                        }
                        if (pNacitaniAudiaDavka && oWav.Nacteno)
                        {
                            //myDataSource.Chapters[0].Sections[0].Paragraphs[0].end = oWav.DelkaSouboruMS;
                            MyTag pTag0 = new MyTag(0, 0, 0);
                            if (myDataSource.VratCasElementuKonec(pTag0) < 0) myDataSource.UpravCasElementu(pTag0, -2, oWav.DelkaSouboruMS);
                            UpdateXMLData();
                            //spSeznam.UpdateLayout();
                            VyberElement(pTag0, true);
                            pNacitaniAudiaDavka = false;
                        }
                        if (oVlna.bufferPrehravaniZvuku.PocatekMS >= oVlna.mSekundyVlnyZac)
                        {
                            bool pStav = KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.DelkaVlnyMS, false);
                        }
                    }
                    if (pAutomaticky)
                    {
                        pAutomaticky = false;
                        menuItemNastrojeFonetickyPrepis_Click(null, new RoutedEventArgs());

                    }


                    /*
                    //davkovy foneticky prepis
                    if (pAutomaticky && pAutomaticky2)
                    {
                        pAutomaticky2 = false;
                        button1_Click_1(null, new RoutedEventArgs());
                    }
                     */

                }
                else if (me.IDBufferu == MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU)
                {
                    oPrepisovac.bufferProPrepsani = new MyBuffer16(me.koncovyCasMS - me.pocatecniCasMS);
                    oPrepisovac.bufferProPrepsani.UlozDataDoBufferu(me.data, me.pocatecniCasMS, me.koncovyCasMS);
                }
                else if (me.IDBufferu == MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS)
                {
                    if (bFonetika != null)
                    {
                        MyBuffer16 bufferProPrepsani = new MyBuffer16(me.koncovyCasMS - me.pocatecniCasMS);
                        bufferProPrepsani.UlozDataDoBufferu(me.data, me.pocatecniCasMS, me.koncovyCasMS);
                        bFonetika.SpustFonetickyPrepisHTKAsynchronne(bufferProPrepsani, bFonetika.TextKPrepsani, bSlovnikFonetickehoDoplneni, nastaveniAplikace.fonetickyPrepis, new DelegatePhoneticOut(FonetickyPrepisDokoncen));
                    }
                    else
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }



        }

        public bool SmazTextBox(int index)
        {
            try
            {
                spSeznam.Children.RemoveAt(index);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// prida textbox a dalsi ovladaci prvky 1 elementu a vraci index v listboxu,kam byl pridan
        /// </summary>
        /// <param name="index"></param>
        /// <param name="aText"></param>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public int PridejTextBox(int index, string aText, MyTag aTag)
        {

            int defaultLeftPositionRichX = nastaveniAplikace.defaultLeftPositionRichX;
            if (nastaveniAplikace != null && nastaveniAplikace.defaultLeftPositionRichX > 20) defaultLeftPositionRichX = nastaveniAplikace.defaultLeftPositionRichX;

            Grid gridX = new Grid();

            gridX.ContextMenu = ContextMenuGridX;
            gridX.ContextMenu.Tag = gridX;

            gridX.MouseUp += new MouseButtonEventHandler(gridX_MouseUp);



            gridX.VerticalAlignment = VerticalAlignment.Stretch;
            gridX.HorizontalAlignment = HorizontalAlignment.Stretch;
            gridX.Background = nastaveniAplikace.BarvaTextBoxuOdstavce;


            //RichTextBox richX = new RichTextBox(aText);
            MyTextBox richX = new MyTextBox();
            richX.TextWrapping = TextWrapping.Wrap;
            richX.Text = aText;
            richX.BorderThickness = new Thickness(0);
            //richX.Background = nastaveniAplikace.BarvaTextBoxuOdstavce;

            Canvas cx = new Canvas();
            richX.BGcanvas = cx;
            cx.Background = nastaveniAplikace.BarvaTextBoxuOdstavce;

            richX.AcceptsReturn = false;
            richX.AcceptsTab = true;
            richX.FontSize = nastaveniAplikace.SetupTextFontSize;
            //if (spSeznam.ActualHeight > 0.1) richX.MaxHeight = spSeznam.ActualHeight;
            //spSeznam.
            //richX.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;



            richX.HorizontalAlignment = HorizontalAlignment.Stretch;
            richX.Tag = new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec, richX);
            richX.TextChanged += new TextChangedEventHandler(RichText_TextChanged);
            richX.PreviewKeyDown += new KeyEventHandler(richX_PreviewKeyDown);
            richX.PreviewTextInput += new TextCompositionEventHandler(richX_PreviewTextInput);

            richX.LostFocus += new RoutedEventHandler(richX_LostFocus);
            richX.GotFocus += new RoutedEventHandler(richX_GotFocus);

            richX.SelectionChanged += new RoutedEventHandler(richX_SelectionChanged);
            richX.MouseDown += new MouseButtonEventHandler(richX_MouseDown);


            richX.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(richX_MouseLeftButtonDown);
            richX.PreviewMouseMove += new MouseEventHandler(richX_PreviewMouseMove);
            richX.MouseEnter += new MouseEventHandler(richX_MouseEnter);
            richX.ContextMenu = gridX.ContextMenu;

            //checkbox pro zobrazeni 
            CheckBox checkX = new CheckBox();
            checkX.HorizontalAlignment = HorizontalAlignment.Right;
            checkX.VerticalAlignment = VerticalAlignment.Center;
            checkX.Margin = new Thickness(0, 0, 4, 0);

            checkX.ToolTip = "Určuje, zda je vybraný element zahrnut do trénovacích dat.";
            checkX.IsTabStop = false;
            checkX.Focusable = false;
            checkX.Click += new RoutedEventHandler(checkX_Click);


            StackPanel stackX = new StackPanel();
            TextBlock textX = new TextBlock();
            Image imageX = new Image();
            imageX.Stretch = Stretch.Uniform;
            imageX.StretchDirection = StretchDirection.DownOnly;

            imageX.MaxWidth = nastaveniAplikace.defaultLeftPositionRichX;
            imageX.MaxHeight = nastaveniAplikace.Fotografie_VyskaMax;
            stackX.Children.Add(textX);
            stackX.Children.Add(imageX);

            Button buttonX = new Button();
            buttonX.HorizontalAlignment = HorizontalAlignment.Stretch;
            buttonX.VerticalAlignment = VerticalAlignment.Stretch;
            buttonX.Content = stackX;
            buttonX.Focusable = false;

            /*double pTop = 0;
            double pDown = 0;
            if (nastaveniAplikace.zobrazitCasBegin) pTop = nastaveniAplikace.SetupTextFontSize;
            if (nastaveniAplikace.zobrazitCasEnd) pDown = nastaveniAplikace.SetupTextFontSize;
            
            buttonX.Margin = new Thickness(0, pTop, 0, pDown);
            
            buttonX.Focusable = false;*/

            //buttonX.Width = 70;

            //labely casu startu a konce
            Label labelStartX = new Label();
            labelStartX.HorizontalAlignment = HorizontalAlignment.Left;
            labelStartX.VerticalAlignment = VerticalAlignment.Bottom;
            labelStartX.Margin = new Thickness(0, 0, 0, 0);
            labelStartX.Foreground = Brushes.Green;
            labelStartX.Padding = new Thickness(0);
            //labelStartX.Content = "0:00:00,00";

            Label labelEndX = new Label();
            labelEndX.HorizontalAlignment = HorizontalAlignment.Left;
            labelEndX.VerticalAlignment = VerticalAlignment.Bottom;
            labelEndX.Margin = new Thickness(0, 0, 0, 0);
            labelEndX.Foreground = Brushes.Red;
            labelEndX.Padding = new Thickness(0);
            //labelEndX.Content = "0:00:00,00";


            string s = myDataSource.VratSpeakera(aTag).FullName;
            if (s == null || s == "")
            {
                if (aTag.tOdstavec > -1)
                {
                    textX.Text = "mluvci...";
                }
                else if (aTag.tSekce > -1)
                {
                    //buttonX.Content = "mluvci sekce";
                    textX.Text = "mluvci sekce";
                }

            }
            else
            {
                //buttonX.Content = s;
                textX.Text = s;

            }
            buttonX.HorizontalAlignment = HorizontalAlignment.Left;
            buttonX.Click += new RoutedEventHandler(buttonX_Click);

            
            
            
            
            //richX.OnTextChanged(null);

            gridX.Children.Add(richX);
            StackPanel attrs = new StackPanel();
            attrs.HorizontalAlignment = HorizontalAlignment.Left;

            attrs.Margin = new Thickness(0, 0, 0, 0);
            //Ellipse circleStartX = new Ellipse();

            //circleStartX.Width = 10;
            //circleStartX.Height = 10;
            //circleStartX.Fill = nastaveniAplikace.BarvaStartTime;

            //circleStartX.HorizontalAlignment = HorizontalAlignment.Left;
         //   if (myDataSource.VratCasElementuPocatek(aTag) > 0)
         //   {
                //circleStartX.Visibility = Visibility.Visible;
                //attrs.Visibility = Visibility.Visible;
         //   }
         //   else if (myDataSource.VratCasElementuKonec(aTag) > 0)
         //   {
                //circleStartX.Visibility = Visibility.Visible;
                //attrs.Visibility = Visibility.Visible;
          //  }
          //  else
          //  {
                //circleStartX.Visibility = Visibility.Hidden;
                //attrs.Visibility = Visibility.Hidden;
          //  }

            richX.Padding = new Thickness(3, 0, 18, 0);

            if (aTag.tOdstavec < 0 && aTag.tSekce < 0)
            {
                richX.Background = Brushes.LightPink;
                richX.Margin = new Thickness(defaultLeftPositionRichX+8, 0, 0, 0);
                //attrs.Margin = new Thickness(defaultLeftPositionRichX - 15, 0, 0, 0);
                //circleStartX.Margin = new Thickness(defaultLeftPositionRichX - 15, 0, 0, 0);
                buttonX.Visibility = Visibility.Collapsed;
            }
            else if (aTag.tOdstavec < 0 && aTag.tSekce > -1)
            {
                richX.Background = Brushes.LightGreen;
                richX.Margin = new Thickness(defaultLeftPositionRichX + 15+8, 0, 0, 0);
               // circleStartX.Margin = new Thickness(defaultLeftPositionRichX + 2, 0, 0, 0);
                //attrs.Margin = new Thickness(defaultLeftPositionRichX + 2, 0, 0, 0);

                buttonX.Visibility = Visibility.Visible;
            }
            else
            {
                richX.Margin = new Thickness(defaultLeftPositionRichX + 35+8, 0, 0, 0);
               // circleStartX.Margin = new Thickness(defaultLeftPositionRichX + 25, 0, 0, 0);
               // attrs.Margin = new Thickness(defaultLeftPositionRichX + 25, 0, 0, 0);
                buttonX.Visibility = Visibility.Visible;

            }

            attrs.Margin = new Thickness(defaultLeftPositionRichX + 15, 5, 0, 0);

            if (aTag.JeOdstavec)
            {
                MyEnumParagraphAttributes[] all = (MyEnumParagraphAttributes[])Enum.GetValues(typeof(MyEnumParagraphAttributes));

                foreach (MyEnumParagraphAttributes at in all)
                {
                    if (at != MyEnumParagraphAttributes.None)
                    {
                        string nam = Enum.GetName(typeof(MyEnumParagraphAttributes), at);

                        Rectangle r = new Rectangle();
                        r.Stroke = Brushes.Green;
                        r.Width = 10;
                        r.Height = 8;
                        r.ToolTip = nam;
                        r.Margin = new Thickness(0, 0, 0, 1);
                        MyParagraph par = myDataSource.VratOdstavec(aTag);
                        if ((par.DataAttributes & at) != 0)
                        {
                            r.Fill = GetRectangleInnenrColor(at);
                        }
                        else
                        {
                            r.Fill = GetRectangleBgColor(at);
                        }

                        r.Tag = aTag;
                        r.MouseLeftButtonDown += new MouseButtonEventHandler(richX_attributes_MouseLeftButtonDown);
                        attrs.Children.Add(r);


                    }
                }
            }
            attrs.Width = 15;
            attrs.Background = Brushes.LightBlue;
            //attrs.MinHeight = 40;

            StackPanel sp3 = new StackPanel();
            //sp3.MaxWidth = 50;
            sp3.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            sp3.Children.Add(buttonX);

            if (!aTag.JeOdstavec)
            {
                labelEndX.Visibility = System.Windows.Visibility.Collapsed;
                labelStartX.Visibility = System.Windows.Visibility.Collapsed;
            }

            sp3.Children.Add(labelStartX);
            sp3.Children.Add(labelEndX);

            gridX.Children.Add(sp3);
            //attrs.Visibility = System.Windows.Visibility.Visible;
            if (aTag.JeOdstavec)
                gridX.Children.Add(attrs);
            else
                gridX.Children.Add(new Ellipse() { Width = 10, Height = 10, Visibility = System.Windows.Visibility.Hidden});

            
            gridX.Children.Add(checkX);
            gridX.Children.Add(cx);
            Grid.SetZIndex(cx, -1);
            Grid.SetZIndex(richX, 0);

            if (index < 0) //pokud je index mensi nez 0, je pridan textbox  nakonec listboxu
            {
                index = spSeznam.Children.Add(gridX);
            }
            else
            {
                spSeznam.Children.Insert(index, gridX);
            }

            return index + 1;


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

        void richX_attributes_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int i= ((sender as Rectangle).Parent as StackPanel).Children.IndexOf(sender as UIElement)+1;
            MyEnumParagraphAttributes[] attrs = (MyEnumParagraphAttributes[]) Enum.GetValues(typeof(MyEnumParagraphAttributes));
            
            MyTag tag = (MyTag)(sender as Rectangle).Tag;
            MyParagraph par =  myDataSource.VratOdstavec(tag);

            par.DataAttributes ^= attrs[i];
            if ((par.DataAttributes & attrs[i]) != 0)
            {
                (sender as Rectangle).Fill = GetRectangleInnenrColor(attrs[i]);
            }
            else
            {
                (sender as Rectangle).Fill = GetRectangleBgColor(attrs[i]);
            }
        }


        void checkX_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MyTag mt = ((MyTag)((TextBox)((Grid)((CheckBox)sender).Parent).Children[0]).Tag);
                myDataSource.OznacTrenovaciData(mt, (bool)(((CheckBox)sender).IsChecked));
                UpdateXMLData();
            }
            catch
            {

            }
        }



        /// <summary>
        /// smaze vybery textboxu krome specifikovaneho
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="krome"></param>
        private void SmazatVyberyTextboxu(int a1, int a2, int krome)
        {
            try
            {
                if (a1 >= 0 && a2 >= 0 && a1 < spSeznam.Children.Count && a2 < spSeznam.Children.Count)
                {
                    for (int i = a1; i <= a2; i++)
                    {
                        if (i != krome)
                        {
                            TextBox pTB = (TextBox)((Grid)spSeznam.Children[i]).Children[0];
                            pTB.CaretIndex = pTB.CaretIndex;
                        }

                    }
                }
            }
            catch
            {

            }

        }

        void richX_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                skocitNaPoziciSlovaTextboxu = true;
                TextBox pTB = ((TextBox)sender);

                pPocatecniIndexVyberu = spSeznam.Children.IndexOf((Grid)((TextBox)(sender)).Parent);
                ///int index = spSeznam.Children.IndexOf(((TextBox)(sender)).Parent);

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }


        void richX_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    TextBox pTB = ((TextBox)sender);
                    int pIndex = spSeznam.Children.IndexOf((Grid)((TextBox)(sender)).Parent);
                    if (pPocatecniIndexVyberu != pIndex)
                    {
                        //pTB.Background = Brushes.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }


        void richX_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }





        /// <summary>
        /// obsluha nastaveni pozice kurzoru pri zmene pozice - pouze pri stisknute klavese ctrl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void richX_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox pTB = ((TextBox)sender);
                MyTag pTag = (MyTag)pTB.Tag;


                if (pTB != null)
                {
                    Rect r = pTB.GetRectFromCharacterIndex(pTB.CaretIndex);

                    popup.PlacementTarget = pTB;
                    popup.HorizontalOffset = r.Left;
                    popup.VerticalOffset = r.Bottom;

                }


                #region slinkovani se zvukem
                if (leftCtrl && skocitNaPoziciSlovaTextboxu || (!Playing && !pTB.IsReadOnly && leftCtrl))
                {
                    skocitNaPoziciSlovaTextboxu = false;
                    TextBox pTB2 = ((TextBox)nastaveniAplikace.RichTag.tSender);
                    int pPoziceKurzoru = pTB2.CaretIndex;
                    int j = -1;
                    long pTime = -1;
                    //long pTime = myDataSource.VratCasElementuPocatek((MyTag)pTB.Tag);

                    if (pTag.JeOdstavec)
                    {
                        List<MyCasovaZnacka> pCZnacky = myDataSource.VratOdstavec(pTag).VratCasoveZnackyTextu;


                        for (int i = 0; i < pCZnacky.Count; i++)
                        {
                            if (pPoziceKurzoru >= pCZnacky[i].Index2) j = i;
                        }

                        if (pTB.CaretIndex == 0)
                        {
                            pTime = myDataSource.VratCasElementuPocatek((MyTag)pTB.Tag);
                        }
                        else if (pTB.CaretIndex == pTB.Text.Length)
                        {
                            pTime = myDataSource.VratCasElementuKonec((MyTag)pTB.Tag);
                            if (pTime < 0)
                            {
                                //pTime = myDataSource.VratCasElementuPocatek((MyTag)pTB.Tag);
                            }
                        }
                        else if (j >= 0) pTime = pCZnacky[j].Time;
                    }
                    if (pTime >= 0)
                    {
                        //if (oVlna.KurzorPoziceMS > pTime)
                        {
                            NastavPoziciKurzoru(pTime, true, true);
                        }
                    }
                }

                int index = spSeznam.Children.IndexOf((Grid)((TextBox)(sender)).Parent);

                if (pSkocitNahoru && index > 0)
                {
                    pSkocitNahoru = false;
                    e.Handled = true;
                    if ((pTB.SelectionStart == 0))
                    {
                        TextBox pTBPredchozi = (TextBox)((Grid)spSeznam.Children[index - 1]).Children[0];
                        pTBPredchozi.Focus();
                        if (leftShift)
                        {
                            if (pPocatecniIndexVyberu > index - 1) pPocatecniIndexVyberu = index - 1; else pKoncovyIndexVyberu = index - 1;
                        }
                        if (leftShift && pTBPredchozi.SelectionLength == 0)
                        {
                            pTBPredchozi.CaretIndex = pTBPredchozi.Text.Length;

                        }
                    }
                }
                else if (pSkocitDolu && index < spSeznam.Children.Count - 1)
                {
                    pSkocitDolu = false;
                    e.Handled = true;
                    TextBox pTBDalsi = (TextBox)((Grid)spSeznam.Children[index + 1]).Children[0];
                    pTBDalsi.Focus();
                    if (leftShift && pTBDalsi.SelectionLength == 0)
                    {
                        pTBDalsi.CaretIndex = 0;

                    }
                    if (leftShift)
                    {
                        if (pKoncovyIndexVyberu <= index + 1)
                        {
                            pKoncovyIndexVyberu = index + 1;
                        }
                        else
                        {
                            pPocatecniIndexVyberu = index + 1;
                        }
                    }


                }

                #endregion

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }


        void gridX_MouseUp(object sender, MouseButtonEventArgs e)
        {

            bool cc = ((TextBox)((Grid)sender).Children[0]).Focus();
        }


        //--------------------------------menu videa---------------------------------------------
        void menuItemVideoPoriditFotku_Click(object sender, RoutedEventArgs e)
        {
            btPoriditObrazekZVidea_Click(null, new RoutedEventArgs());
        }


        //--------------------------------obsluha context menu pro gridy v listboxu--------------------------------------------------
        void menuItemX_Nastav_Mluvciho_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                //MyTag mt = ((MyTag)((RichTextBox)((Grid)((ContextMenu)((MenuItem)sender).Parent).Parent).Children[0]).Tag);
                Grid ss = ((Grid)((ContextMenu)((MenuItem)sender).Parent).Tag);
                MyTag mT = ((MyTag)((TextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                //((Button)ss.Children[1]).Visibility = Visibility.Visible;
                //buttonX_Click(((Button)ss.Children[1]), new RoutedEventArgs());
                new WinSpeakers(mT, this.nastaveniAplikace, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();

                UpdateXMLData();
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        void menuItemX2_Nova_Sekce_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid ss = ((Grid)((ContextMenu)((MenuItem)sender).Parent).Tag);
                MyTag mT = ((MyTag)((RichTextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                PridejSekci(mT.tKapitola, "", mT.tSekce, -1, -1, -1);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        void menuItemX2b_Nova_Sekce_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid ss = ((Grid)((ContextMenu)((MenuItem)sender).Parent).Tag);
                MyTag mT = ((MyTag)((RichTextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                PridejSekci(mT.tKapitola, "", mT.tSekce, mT.tOdstavec, -1, -1);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        void menuItemX3_Nova_Kapitola_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid ss = ((Grid)((ContextMenu)((MenuItem)sender).Parent).Tag);
                MyTag mT = ((MyTag)((RichTextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;

                int pIndex = -1;
                if (mT != null) pIndex = mT.tKapitola;
                PridejKapitolu(pIndex + 1, "");
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        void menuItemX4_Smaz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid ss = ((Grid)((ContextMenu)((MenuItem)sender).Parent).Tag);
                MyTag mT = ((MyTag)((TextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                if (mT.JeKapitola) OdstranKapitolu(mT.tKapitola);
                else if (mT.JeSekce) OdstranSekci(mT.tKapitola, mT.tSekce);
                else if (mT.JeOdstavec) OdstranOdstavec(mT.tKapitola, mT.tSekce, mT.tOdstavec);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        void menuItemX5_Smaz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Grid ss = ((Grid)((ContextMenu)((MenuItem)sender).Parent).Tag);
                MyTag mT;// = ((MyTag)((RichTextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                UpravCasZobraz(mT, -1, -2);
                UpdateXMLData();
                ZobrazInformaceElementu(mT);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        void menuItemX6_Smaz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Grid ss = ((Grid)((ContextMenu)((MenuItem)sender).Parent).Tag);
                MyTag mT;// = ((MyTag)((RichTextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                UpravCasZobraz(mT, -2, -1);
                UpdateXMLData();
                ZobrazInformaceElementu(mT);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        void menuItemX7_Click(object sender, RoutedEventArgs e)
        {
            MyTag tag = new MyTag(nastaveniAplikace.RichTag);
            MyParagraph par = myDataSource.VratOdstavec(tag);
            tag.tTypElementu = MyEnumTypElementu.foneticky;
            MyParagraph parf = myDataSource.VratOdstavec(tag);
            oWav.RamecSynchronne = true;
            bool nacteno = oWav.NactiRamecBufferu(par.begin, par.DelkaMS, MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS);//)this.bPozadovanyPocatekRamce, this.bPozadovanaDelkaRamceMS, this.bIDBufferu);        
            oWav.RamecSynchronne = false;
            
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "wav soubory (.wav)|*.wav";
            if (dlg.ShowDialog() == true)
            {
                string filename = dlg.FileName;
                //BinaryWriter bw = new BinaryWriter(new FileStream(filename, FileMode.Create));
                MyBuffer16 bf = new MyBuffer16(oWav.NacitanyBufferSynchronne.data.Length);
                bf.data = new List<short>(oWav.NacitanyBufferSynchronne.data);
                MyWav.VytvorWavSoubor(bf, filename);


                string ext = System.IO.Path.GetExtension(filename);
                filename = filename.Substring(0, filename.Length - ext.Length);
                string textf = filename + ".txt";

                File.WriteAllText(textf, par.Text);


                if (parf != null)
                {
                    textf = filename + ".phn";
                    File.WriteAllText(textf, parf.Text);
                }
            }

        }

        void buttonX_Click(object sender, RoutedEventArgs e)
        {
            try
            {


                MyTag mt = ((MyTag)((TextBox)((Grid)((sender as Button).Parent as StackPanel).Parent).Children[0]).Tag);
                new WinSpeakers(mt, this.nastaveniAplikace, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();

                UpdateXMLData();

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        //pri zamereni...
        void richX_GotFocus(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
            try
            {
                if (pZiskatNovyIndex)
                {
                    int pIndex = spSeznam.Children.IndexOf((Grid)((TextBox)(sender)).Parent);
                    pPocatecniIndexVyberu = pIndex;
                    pKoncovyIndexVyberu = pIndex;
                    pZiskatNovyIndex = false;

                }
                nastaveniAplikace.RichFocus = true;
                MyTag pPuvodniTag = new MyTag(nastaveniAplikace.RichTag);
                nastaveniAplikace.RichTag = (MyTag)((TextBox)(sender)).Tag;
                nastaveniAplikace.RichTag.tSender = sender;

                if (nastaveniAplikace.RichTag.tOdstavec >= 0)
                {
                    MyParagraph pP = myDataSource.VratOdstavec(nastaveniAplikace.RichTag);
                    nastaveniAplikace.CasoveZnacky = pP.VratCasoveZnackyTextu;    //casove znacky, ktere jsou nastaveny v odstavci


                    //ulozeni delky textu - melo by stacit ulozit text v ulozenem odstavci - property text
                    ///FlowDocument flowDoc = ((RichTextBox)(sender)).Document;
                    ///TextRange tr = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd.GetPositionAtOffset(-1));

                    ///nastaveniAplikace.CasoveZnackyText = tr.Text;               //cely text bez koncu - pro pozdejsi porovnani pri zmene
                    nastaveniAplikace.CasoveZnackyText = ((TextBox)sender).Text;

                }

                //provede zvyrazneni vyberu ve vlne podle dat
                oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(nastaveniAplikace.RichTag);
                oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(nastaveniAplikace.RichTag);

                //nastaveni pozice kurzoru a obsluha prehravani podle nastaveni
                if (nastaveniAplikace.SetupSkocitNaPozici && !pNeskakatNaZacatekElementu)
                {
                    oVlna.KurzorPoziceMS = oVlna.KurzorVyberPocatekMS;
                    if (nastaveniAplikace.SetupSkocitZastavit)
                    {
                        if (jeVideo) meVideo.Play();
                        Playing = false;
                    }
                }
                if (pNeskakatNaZacatekElementu) pNeskakatNaZacatekElementu = false;
                if (nastaveniAplikace.RichTag.JeOdstavec)
                {
                    KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, oVlna.KurzorPoziceMS);
                    if (!Playing) NastavPoziciKurzoru(oVlna.KurzorPoziceMS, true, true);
                }
                ZobrazInformaceElementu(nastaveniAplikace.RichTag);
                if (nastaveniAplikace.RichTag.tOdstavec > -1)
                {
                    if (nastaveniAplikace.RichTag.tTypElementu == MyEnumTypElementu.normalni)
                    {
                        ((MyTextBox)sender).BGcanvas.Background = nastaveniAplikace.BarvaTextBoxuOdstavceAktualni;
                        ((Grid)((TextBox)sender).Parent).Background = nastaveniAplikace.BarvaTextBoxuOdstavceAktualni;
                    }
                    else if (nastaveniAplikace.RichTag.tTypElementu == MyEnumTypElementu.foneticky)
                    {
                        if (pPuvodniTag.tTypElementu != MyEnumTypElementu.foneticky)
                        {
                            ((MyTextBox)pPuvodniTag.tSender).BGcanvas.Background = nastaveniAplikace.BarvaTextBoxuOdstavceAktualni;
                            ((Grid)((MyTextBox)pPuvodniTag.tSender).Parent).Background = nastaveniAplikace.BarvaTextBoxuOdstavceAktualni;
                            tbFonetickyPrepis.Background = nastaveniAplikace.BarvaTextBoxuFonetickyAktualni;
                        }
                    }


                }
                ZobrazitFonetickyPrepisOdstavce(nastaveniAplikace.RichTag);

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        //pri ztrate zamereni textboxu...
        void richX_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                nastaveniAplikace.RichFocus = false;
                nastaveniAplikace.BylFocus = true;
                ///nastaveniAplikace.RichTag = (MyTag)((RichTextBox)(sender)).Tag;
                nastaveniAplikace.RichTag = (MyTag)((TextBox)(sender)).Tag;
                nastaveniAplikace.RichTag.tSender = sender;
                if (nastaveniAplikace.RichTag.tOdstavec > -1)
                {
                    if (nastaveniAplikace.RichTag.tTypElementu == MyEnumTypElementu.normalni)
                    {
                        ((MyTextBox)sender).BGcanvas.Background = nastaveniAplikace.BarvaTextBoxuOdstavce;
                        ((Grid)((TextBox)sender).Parent).Background = nastaveniAplikace.BarvaTextBoxuOdstavce;
                    }
                    else if (nastaveniAplikace.RichTag.tTypElementu == MyEnumTypElementu.foneticky)
                    {
                        object pTb = VratSenderTextboxu(((MyTag)((TextBox)tbFonetickyPrepis).Tag));
                        ((MyTextBox)pTb).BGcanvas.Background = nastaveniAplikace.BarvaTextBoxuOdstavce;
                        ((Grid)((TextBox)pTb).Parent).Background = nastaveniAplikace.BarvaTextBoxuOdstavce;
                        tbFonetickyPrepis.Background = nastaveniAplikace.BarvaTextBoxuFoneticky;
                    }


                }

                //if (((TextBox)sender).SelectionLength > 0) e.Handled = true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }


        void richX_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Delete || e.Key == Key.Back)
            {
                popup.IsOpen = false;
            }


            try
            {
                int index = spSeznam.Children.IndexOf((Grid)((TextBox)(sender)).Parent);
                MyTag mT = (MyTag)((TextBox)(sender)).Tag;
                TextBox pTB = ((TextBox)sender);

                if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift) leftShift = true; else leftShift = false;
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) leftCtrl = true; else leftCtrl = false;
                KeyConverter kc = new KeyConverter();


                if (e.Key == Key.Escape && sender != tbFonetickyPrepis)
                {
                    if (popup.IsOpen)
                    {
                        popup.IsOpen = false;
                        e.Handled = true;
                        listboxpopupPopulate(nastaveniAplikace.NerecoveUdalosti);

                        popup_filter = "";
                    }
                    else if (nastaveniAplikace.ZobrazitFonetickyPrepis > 10)
                    {
                        tbFonetickyPrepis.Focus();
                        e.Handled = true;
                    }
                }
                if (e.Key == Key.Return)
                {

                    MyTag x = (MyTag)((TextBox)(sender)).Tag;
                    MyParagraph para = myDataSource.VratOdstavec(x);
                    MyTag next = myDataSource.VratOdstavecNasledujiciTag(x);

                    if (next != null)
                        return ;


                    //prida do seznamu richtextboxu novou komponentu
                    
                    if (x.tTypElementu == MyEnumTypElementu.foneticky)
                    {
                        MenuItemFonetickeVarianty_Click(null, new RoutedEventArgs());
                        return;
                    }
                    if ((x.tOdstavec > -1))
                    {
                        if (!e.IsRepeat)
                        {
                            MyParagraph pPuvodniOdstavec = myDataSource.VratOdstavec(x);
                            if (string.IsNullOrEmpty(pTB.Text))
                                return;

                            long pocatek = myDataSource.VratCasElementuPocatek(x);
                            if (pocatek + 20 >= oVlna.KurzorPoziceMS) //minuly elment nema konec
                                return;

                            this.pUpravitOdstavec = false; //odstavec je upraven jiz zde, a nebude dale upravovan v udalosti text change



                            //text budouciho odstavce
                            ///FlowDocument flowDoc = ((RichTextBox)(sender)).Document;
                            ///TextPointer zac = ((RichTextBox)(sender)).Selection.Start;
                            ///TextPointer kon = flowDoc.ContentEnd.GetPositionAtOffset(-1);
                            ///TextRange trDalsi = new TextRange(zac, kon);    //textrange budouciho odstavce
                            string trDalsi = "";
                            if (((TextBox)sender).Text.Length > ((TextBox)sender).SelectionStart) trDalsi = ((TextBox)sender).Text.Substring(((TextBox)sender).SelectionStart);
                            //casove znacky budouciho odstavce
                            List<MyCasovaZnacka> pNoveZnacky = myDataSource.VratOdstavec(x).VratCasoveZnackyTextu;



                            //smazani casovych znacek,ktere patri puvodnimu textboxu
                            int pDelkaTextu = pPuvodniOdstavec.Text.Length;
                            while (pNoveZnacky.Count > 0 && pNoveZnacky[0].Index2 < pDelkaTextu - trDalsi.Length)
                            {
                                pNoveZnacky.RemoveAt(0);
                            }
                            for (int i = 0; i < pNoveZnacky.Count; i++) //odecteni indexu casovych znacek
                            {
                                pNoveZnacky[i].Index1 = pNoveZnacky[i].Index1 - (pDelkaTextu - trDalsi.Length);
                                pNoveZnacky[i].Index2 = pNoveZnacky[i].Index2 - (pDelkaTextu - trDalsi.Length);
                            }



                            //vytvoreni noveho odstavce, jeho textboxu a jeho zobrazeni
                            long pomKon = myDataSource.VratCasElementuKonec(x); //pokud mel puvodni element index konce,je prirazen novemu elementu
                            //PridejOdstavec(x.tKapitola, x.tSekce, "", null, x.tOdstavec, -1, -1, new MySpeaker());
                            //return;
                            if (UpravCasZobraz(x, -2, oVlna.KurzorPoziceMS))
                            {


                                long pomPoc = myDataSource.VratCasElementuKonec(x);
                                if (pomKon <= pomPoc) pomKon = -1;

                                //pokud je stisknut ctrl, je vytvoren novy mluvci
                                MySpeaker pSpeaker = new MySpeaker();
                                if (!leftCtrl) pSpeaker = myDataSource.VratSpeakera(x);
                                //

                                PridejOdstavec(x.tKapitola, x.tSekce, trDalsi, pNoveZnacky, x.tOdstavec, pomPoc, pomKon, pSpeaker);

                                //pokud nema dalsi odstavec text, musi se nastavit aby byl pozdeji odstavec upravovan
                                if (trDalsi == null || trDalsi == "")
                                {
                                    pUpravitOdstavec = true;
                                }


                            }



                            //upraveny text puvodniho odstavce
                            //zmena textu v aktualnim textboxu
                            ///zac = ((RichTextBox)(sender)).Document.ContentStart;
                            ///kon = ((RichTextBox)(sender)).Selection.Start;
                            ///TextRange trAktualni = new TextRange(zac, kon);
                            string trAktualni = ((TextBox)sender).Text;
                            if (trAktualni.Length > ((TextBox)sender).SelectionStart) trAktualni = ((TextBox)sender).Text.Remove(((TextBox)sender).SelectionStart);

                            //myDataSource.UpravElementOdstavce(x.tKapitola, x.tSekce, x.tOdstavec, trAktualni.Text, nastaveniAplikace.CasoveZnacky);
                            MyTag x2 = new MyTag(x);
                            x.tTypElementu = MyEnumTypElementu.normalni;
                            myDataSource.UpravElementOdstavce(x2, trAktualni, myDataSource.VratOdstavec(x).VratCasoveZnackyTextu);

                            ///flowDoc = VytvorFlowDocumentOdstavce(myDataSource.VratOdstavec(x));
                            //nastaveni aktualnich dat textboxu odstavce,aby nedochazelo ke zmenam
                            nastaveniAplikace.CasoveZnacky = myDataSource.VratOdstavec(x).VratCasoveZnackyTextu;
                            nastaveniAplikace.CasoveZnackyText = myDataSource.VratOdstavec(x).Text;





                            ///((RichTextBox)(sender)).Document = flowDoc;
                            ((TextBox)sender).Text = myDataSource.VratOdstavec(x).Text;

                            if ((x.tOdstavec > -1) || (x.tSekce > -1))
                            {
                                spSeznam.UpdateLayout();
                                e.Handled = true;
                                ((Grid)spSeznam.Children[index + 1]).Children[0].Focus();
                            }


                        }

                    }
                    else if (x.tSekce > -1)
                    {
                        if (oVlna.KurzorPoziceMS == -1)
                            return;

                        MyTag pMT = PridejOdstavec(x.tKapitola, x.tSekce, "", null, -2, oVlna.KurzorPoziceMS, -1, new MySpeaker());
                        try
                        {
                            ((TextBox)pMT.tSender).Focus();
                        }
                        catch
                        {

                        }
                    }
                    else if (x.tKapitola > -1)
                    {
                        MyTag pMT = PridejSekci(x.tKapitola, "", -1, -1, -1, -1);
                        try
                        {
                            ((TextBox)pMT.tSender).Focus();
                        }
                        catch
                        {

                        }
                    }
                    UpdateXMLData();    //update mluvcich...
                    return;
                }
                else if (e.Key == Key.PageDown)
                {
                    e.Handled = true;
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
                else if (e.Key == Key.PageUp)
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
                else if (e.Key == Key.F3 && !e.IsRepeat)
                {
                    e.Handled = true;
                    MyTag pomTag = null;
                    //  if (leftShift)
                    //  {
                    pomTag = PridejSekci(mT.tKapitola, "", mT.tSekce, mT.tOdstavec, -1, -1);
                    //  }
                    //  else
                    //  {
                    //     pomTag = PridejSekci(mT.tKapitola, "", mT.tSekce, -1, -1, -1);
                    //  }

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

                else if (e.Key == Key.Delete && leftShift && !e.IsRepeat)
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
                    //leftCtrl = false;
                }
                else if (e.Key == Key.Delete && leftCtrl && !e.IsRepeat)        //smazani pocatecniho casoveho indexu
                {
                    e.Handled = true;
                    menuItemX5_Smaz_Click(null, new RoutedEventArgs());
                }
                else if (e.Key == Key.Home && leftCtrl && !e.IsRepeat) //nastavi index pocatku elementu podle pozice kurzoru
                {
                    e.Handled = true;
                    menuItemVlna1_prirad_zacatek_Click(null, new RoutedEventArgs());
                    //leftCtrl = false;

                }
                else if (e.Key == Key.End && leftCtrl && !e.IsRepeat) //nastavi index konce elementu podle pozice kurzoru
                {
                    e.Handled = true;
                    menuItemVlna1_prirad_konec_Click(null, new RoutedEventArgs());
                    //leftCtrl = false;
                }
                else if (leftCtrl && e.Key == Key.Space)    //ctrl+mezernik = prida casovou znacku do textu
                {
                    menuItemVlna1_prirad_casovou_znacku_Click(null, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.F && leftCtrl && !e.IsRepeat)
                {
                    e.Handled = true;
                    MenuItemFonetickeVarianty_Click(null, new RoutedEventArgs());
                    leftCtrl = false;

                }
                else if (e.Key == Key.M)
                {
                    if (leftCtrl && !e.IsRepeat)  //prehrani nebo pausnuti prehravani
                    {
                        e.Handled = true;
                        buttonX_Click(((Button)(((Grid)((TextBox)sender).Parent).Children[1] as StackPanel).Children[0]), new RoutedEventArgs());
                        leftCtrl = false;
                    }
                }
                else if ((e.Key == Key.Up))
                {
                    if (mT.tTypElementu == MyEnumTypElementu.foneticky)
                        return;
                    if (!leftShift)
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
                        if (leftShift)
                        {
                            int pPozice = pTB.SelectionStart;
                            int pDelka = pTB.SelectionLength;
                            //pTB.SelectionStart = 0;
                            //pTB.SelectionLength = pPozice + pDelka;

                            //pSkocitNahoru = true;
                        }
                        else
                        {
                            TextBox pTBPredchozi = (TextBox)((Grid)spSeznam.Children[index - 1]).Children[0];
                            pTB.Select(0, 0);
                            pTBPredchozi.Focus();
                            pTBPredchozi.CaretIndex = pTBPredchozi.GetCharacterIndexFromLineIndex(pTBPredchozi.LineCount - 1) + pPoziceCursoruNaRadku;
                            if (leftShift)
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
                            if (!leftShift)
                            {
                                this.SmazatVyberyTextboxu(pPocatecniIndexVyberu, pKoncovyIndexVyberu, -1);
                                pZiskatNovyIndex = true;
                            }

                            //int pPoziceCursoruNaRadku = pTB.CaretIndex - pTB.GetCharacterIndexFromLineIndex(pTB.GetLineIndexFromCharacterIndex(pTB.CaretIndex));
                            int pPoziceCursoruNaRadku = (pTB.SelectionStart + pTB.SelectionLength) - pTB.GetCharacterIndexFromLineIndex(pTB.GetLineIndexFromCharacterIndex(pTB.SelectionStart + pTB.SelectionLength));
                            TextBox pTBDalsi = (TextBox)((Grid)spSeznam.Children[index + 1]).Children[0];
                            if (leftShift)
                            {
                                /*
                                pSkocitDolu = true;
                                
                                int pPozice = pTB.SelectionStart;
                                int pDelka = pTB.SelectionLength;
                                if (pTBDalsi.SelectionLength == 0)
                                {
                                    
                                    pTB.Select(pTB.SelectionStart, pTB.Text.Length - pTB.SelectionStart);
                                    pSkocitDolu = false;
                                    pTBDalsi.Focus();
                                    pTBDalsi.Select(0, pPoziceCursoruNaRadku);
                                    e.Handled = true;
                                    //pKoncovyIndexVyberu = index + 1;
                                }
                                 */
                            }
                            else
                            {

                                pTB.CaretIndex = 0;
                                pTBDalsi.Focus();
                                pTBDalsi.CaretIndex = pPoziceCursoruNaRadku;
                                e.Handled = true;

                            }

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
                        if (!leftShift && pTB.CaretIndex == pTB.Text.Length)
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
                        if (!leftShift && pTB.CaretIndex == 0)
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

                        List<MyCasovaZnacka> pCasoveZnackyMazaneho = myDataSource.VratOdstavec(mT).VratCasoveZnackyTextu;


                        if (mT.tOdstavec > 0 || s.Length == 0)
                        {

                            if (index > 0)
                            {


                                if (((MyTag)((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Tag).tOdstavec > -1)
                                {


                                    ///flowDoc = ((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Document;
                                    ///zac = ((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Document.ContentStart;
                                    ///kon = flowDoc.ContentEnd.GetPositionAtOffset(-1);
                                    ///tr = new TextRange(zac, kon);
                                    tr = ((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Text;
                                    ///int pDelka = flowDoc.ContentStart.GetOffsetToPosition(flowDoc.ContentEnd);  //delka ve "znacich" kvuli nastaveni kurzoru
                                    int pDelka = tr.Length;
                                    string t = tr;
                                    t = t.Replace("\n\r", "\r");       //nahrazeni \n za \r kvuli odradkovani
                                    t = t.Replace("\r\n", "\r");       //nahrazeni \n za \r kvuli odradkovani
                                    t = t.Replace("\n", "\r");       //nahrazeni \n za \r kvuli odradkovani

                                    //casove znacky predchoziho odstavce, ke kteremu se budou pridavat nasledujici
                                    MyTag pTagPredchoziho = new MyTag(mT.tKapitola, mT.tSekce, mT.tOdstavec - 1);
                                    List<MyCasovaZnacka> pCasoveZnackyPredchoziho = myDataSource.VratOdstavec(pTagPredchoziho).VratCasoveZnackyTextu;

                                    myDataSource.UpravCasElementu(pTagPredchoziho, -2, myDataSource.VratCasElementuKonec(mT));    //koncovy cas elementu je nastaven podle aktualniho

                                    OdstranOdstavec(mT.tKapitola, mT.tSekce, mT.tOdstavec); //odstrani odstavec z datove struktury

                                    //upraveni casovych indexu znacek podle predchozich
                                    for (int i = 0; i < pCasoveZnackyMazaneho.Count; i++)
                                    {
                                        pCasoveZnackyMazaneho[i].Index1 += t.Length;
                                        pCasoveZnackyMazaneho[i].Index2 += t.Length;
                                    }
                                    pCasoveZnackyPredchoziho.AddRange(pCasoveZnackyMazaneho);
                                    myDataSource.VratOdstavec(pTagPredchoziho).UlozTextOdstavce(t + s, pCasoveZnackyPredchoziho);
                                    //kvuli pozdejsi editaci
                                    if (s == null || s == "")
                                    {
                                        pUpravitOdstavec = true;
                                    }



                                    //((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Document = new FlowDocument(new Paragraph(new Run(t + s)));
                                    nastaveniAplikace.CasoveZnackyText = t + s;
                                    nastaveniAplikace.CasoveZnacky = pCasoveZnackyPredchoziho;
                                    ///((RichTextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Document = VytvorFlowDocumentOdstavce(myDataSource.VratOdstavec(pTagPredchoziho));
                                    ((TextBox)((Grid)spSeznam.Children[index - 1]).Children[0]).Text = myDataSource.VratOdstavec(pTagPredchoziho).Text;

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

                        List<MyCasovaZnacka> pCasoveZnackyAktualniho = myDataSource.VratOdstavec(mT).VratCasoveZnackyTextu;

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
                                List<MyCasovaZnacka> pCasoveZnackyNasledujiciho = myDataSource.VratOdstavec(pTagNasledujicihoOdstavce).VratCasoveZnackyTextu;

                                myDataSource.UpravCasElementu(mT, -2, myDataSource.VratCasElementuKonec(pTagNasledujicihoOdstavce));    //koncovy cas elementu je nastaven podle nasledujiciho
                                KresliVyber(myDataSource.VratCasElementuPocatek(mT), myDataSource.VratCasElementuKonec(mT), -1);

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
                                myDataSource.VratOdstavec(mT).UlozTextOdstavce(s2 + t2, pCasoveZnackyAktualniho);   //ulozeni zmen do akktualniho odstavce
                                if (t2 == "")
                                {
                                    pUpravitOdstavec = true;
                                }


                                nastaveniAplikace.CasoveZnackyText = s2 + t2;
                                nastaveniAplikace.CasoveZnacky = pCasoveZnackyAktualniho;
                                ((TextBox)((Grid)spSeznam.Children[index]).Children[0]).Text = myDataSource.VratOdstavec(mT).Text;
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


        //pri zmene textu v textboxech dojde k uprave v datove strukture----------------------------------------------DODELAT PRO POSUNY CASOVYCH ZNACEK
        private void RichText_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                #region popup window
                TextBox tb = sender as TextBox;
                if (tb != null)
                {
                    Rect r = tb.GetRectFromCharacterIndex(tb.CaretIndex);

                    popup.PlacementTarget = tb;
                    popup.HorizontalOffset = r.Left;
                    popup.VerticalOffset = r.Bottom;

                }

                #endregion



                if (this.pUpravitOdstavec)
                {
                    TextBox pTb = (TextBox)sender;
                    MyTag pTag = ((MyTag)pTb.Tag);

                    if (pTag.tTypElementu == MyEnumTypElementu.foneticky)
                    {
                        MyParagraph pP = myDataSource.VratOdstavec(pTag);
                        if (pP != null)
                        {
                            pUpravitOdstavec = false;
                            MyKONST.OverZmenyTextBoxu(pTb, pP.Text, ref e, MyEnumTypElementu.foneticky);
                            pUpravitOdstavec = true;
                        }
                    }

                    string tr = pTb.Text;
                    if (!pTag.JeOdstavec)
                    {
                        if (pTag.tTypElementu == MyEnumTypElementu.normalni)
                        {
                            myDataSource.UpravElement(pTag, tr);    //je upravena kapitola a sekce
                        }
                    }
                    else
                    {
                        //jinak je upraven odstavec a jeho casove znacky

                        if (nastaveniAplikace.RichTag.tOdstavec == pTag.tOdstavec)   //dojde k uprave jestli souhlasi vybrany odstavec a odstavec ze ktereho je volana tato funkce
                        {
                            MyParagraph pP = myDataSource.VratOdstavec(pTag);
                            int carretpos = (pTb.SelectionLength<=0)?pTb.CaretIndex:pTb.SelectionStart;
                            int pIndexZmeny = MyKONST.VratIndexZmenyStringu(nastaveniAplikace.CasoveZnackyText, tr, carretpos);// pTb.SelectionStart);
                            if (pIndexZmeny >= 0) //doslo ke zmene ve stringu
                            {
                                //jak se zmenila delka - novy je o kolik delsi - muze byt i zaporny - tzn je ktratsi nez puvodni
                                int pDelka = tr.Length - nastaveniAplikace.CasoveZnackyText.Length;

                                //nalezeni od ktereho indexu dojde k prepoctu
                                for (int i = 0; i < nastaveniAplikace.CasoveZnacky.Count; i++)
                                {
                                    if (pIndexZmeny <= nastaveniAplikace.CasoveZnacky[i].Index2) //&& i>0)
                                    {
                                        //smazani indexove casove znacky
                                        if (pIndexZmeny == nastaveniAplikace.CasoveZnacky[i].Index2 && pDelka < 0)
                                        {
                                            //mazalo se tesne za indexem -> nebude s timhle se nebude delat nic
                                            //nastaveniAplikace.CasoveZnacky.RemoveAt(i);
                                            //i--; //aby doslo k uprave nasledujici znacky pokud navazuje

                                        }
                                        else if (pIndexZmeny < nastaveniAplikace.CasoveZnacky[i].Index2)// uprava casove znacky-podminka,aby slo psat za prave vlozenou znacku
                                        {
                                            //mazalo se pred indexem, tento se musi posunout

                                            if (nastaveniAplikace.CasoveZnacky[i].Index2 + pDelka < pIndexZmeny)
                                            {
                                                nastaveniAplikace.CasoveZnacky.RemoveAt(i);
                                                i--;
                                            }
                                            else
                                            {
                                                nastaveniAplikace.CasoveZnacky[i].Index1 += pDelka;
                                                nastaveniAplikace.CasoveZnacky[i].Index2 += pDelka;
                                            }
                                        }

                                    }

                                }
                                myDataSource.UpravElementOdstavce(pTag, tr, nastaveniAplikace.CasoveZnacky);

                                nastaveniAplikace.CasoveZnackyText = tr;               //cely text bez koncu - pro pozdejsi porovnani pri zmene
                            }
                        }
                    }
                }
                else
                {
                    pUpravitOdstavec = true;
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }


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
                        PridejKapitolu(-1, "Kapitola 0");
                        PridejSekci(0, "Sekce 0", -1, -1, -1, -1);
                        PridejOdstavec(0, 0, "", null, -1, 0, -1, new MySpeaker());
                        myDataSource.Ulozeno = true;
                        ZobrazXMLData();

                        return true;
                    }


                }
                else
                {
                    myDataSource = new MySubtitlesData();
                    this.Title = MyKONST.NAZEV_PROGRAMU + " [novy]";
                    PridejKapitolu(-1, "Kapitola 0");
                    PridejSekci(0, "Sekce 0", -1, -1, -1, -1);
                    PridejOdstavec(0, 0, "", null, -1, 0, -1, new MySpeaker());
                    myDataSource.Ulozeno = true;
                    ZobrazXMLData();
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                MessageBox.Show("Chyba pri vytvareni novych titulku " + ex.Message, "Chyba");
                return false;
            }
        }

        /// <summary>
        /// pokusi se prevest korpus do xml
        /// </summary>
        /// <param name="aCestaKorpus"></param>
        /// <param name="aCestaXML"></param>
        /// <returns></returns>
        public bool PrevedKorpus(string aCestaKorpus, ref string aCestaXML)
        {
            try
            {
                FileInfo fi = new FileInfo(aCestaKorpus);
                if (fi == null || !fi.Exists || (fi.Extension != ".krp" && fi.Extension != ".txt")) return false;
                string pCestaXML = nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_KORPUS2XML_DIR + "temp.xml";

                //zavolani scriptu v perlu
                ProcessStartInfo ps = new ProcessStartInfo("perl.exe", "\"" + nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_KORPUS2XML + "\" -k \"" + aCestaKorpus + "\"" + " -x " + "\"" + pCestaXML + "\"");
                ps.UseShellExecute = false;
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardError = true;
                ps.CreateNoWindow = true;
                Process p = new Process();
                p.StartInfo = ps;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();

                aCestaXML = pCestaXML;
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// pokusi se prevest tta na xml, vraci cestu ke xml, v pripade uspechu
        /// </summary>
        /// <param name="aCestaTTA"></param>
        /// <returns></returns>
        public bool PrevedTTA(string aCestaTTA, ref string aCestaWav, ref string aCestaXML)
        {
            try
            {
                FileInfo fi = new FileInfo(aCestaTTA);
                if (fi == null || !fi.Exists || fi.Extension != ".tta") return false;
                string pCestaWav = nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_TTASPLIT_TEMP + "temp.wav";
                string pCestaXML = nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_TTASPLIT_TEMP + "temp.xml";
                string pCestaTtaSplit = nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_TTASPLIT_EXE;

                //zavolani scriptu v perlu
                ProcessStartInfo ps = new ProcessStartInfo("perl.exe", "\"" + nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_TTA2XML + "\"" + " -e \"" + pCestaTtaSplit + "\"" + " -t \"" + aCestaTTA + "\" -w \"" + pCestaWav + "\"" + " -d " + "\"" + nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_TTA2XML_DIR + "\"" + " -x " + "\"" + pCestaXML + "\"");
                ps.UseShellExecute = false;
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardError = true;
                ps.CreateNoWindow = true;
                Process p = new Process();
                p.StartInfo = ps;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();

                aCestaWav = pCestaWav;
                aCestaXML = pCestaXML;
                return true;
            }
            catch
            {
                return false;
            }
        }


        public bool OtevritTitulky(bool pouzitOpenDialog, string jmenoSouboru, bool aDavkovySoubor)
        {
            try
            {
                pNacitaniAudiaDavka = false;
                if (myDataSource == null) myDataSource = new MySubtitlesData();
                if (pouzitOpenDialog)
                {
                    Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

                    fileDialog.Title = "Otevřít soubor s titulky...";
                    //fdlg.InitialDirectory = @"c:\" ;
                    fileDialog.Filter = "Soubory titulků (*" + nastaveniAplikace.PriponaTitulku + ")|*" + nastaveniAplikace.PriponaTitulku;
                    if (MyKONST.VERZE == MyEnumVerze.Interni)
                    {
                        fileDialog.Filter += "|Soubory (*.tta) |*.tta";
                        fileDialog.Filter += "|Soubory (*.trsx) |*.trsx";
                        fileDialog.Filter = "Podporované typy (*.trsx, *.xml, *.tta)|*.trsx;*.xml;*.tta|" + fileDialog.Filter;
                    }
                    fileDialog.Filter += "|Všechny soubory (*.*)|*.*";
                    fileDialog.FilterIndex = 1;
                    fileDialog.RestoreDirectory = true;
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

                        bool pNactenoTTA = false;
                        if (MyKONST.VERZE == MyEnumVerze.Interni)
                        {
                            string pCestaXML = null;
                            string pCestaWAV = null;
                            if (this.PrevedTTA(fileDialog.FileName, ref pCestaWAV, ref pCestaXML))
                            {
                                if (pCestaXML != null) fileDialog.FileName = pCestaXML;
                                pNactenoTTA = true;
                                NactiAudio(pCestaWAV);
                            }
                        }

                        if (!pNactenoTTA && !aDavkovySoubor)
                        {
                            string pCestaXML = null;
                            if (this.PrevedKorpus(fileDialog.FileName, ref pCestaXML))
                            {
                                if (pCestaXML != null) 
                                    fileDialog.FileName = pCestaXML;

                            }
                        }

                        MySubtitlesData pDataSource = null;
                        pDataSource = myDataSource.Deserializovat(fileDialog.FileName);


                        if (pDataSource == null)
                        {
                            NoveTitulky();
                            //pDataSource = new MySubtitlesData();                            
                        }
                        else
                        {
                            if (pNactenoTTA) pDataSource.JmenoSouboru = null;
                            myDataSource = null;
                            myDataSource = pDataSource;
                            myDataSource.Ulozeno = true;

                            //nacteni audio souboru pokud je k dispozici
                            if (myDataSource.audioFileName != null && myDataSource.JmenoSouboru != null)
                            {
                                FileInfo fiA = new FileInfo(myDataSource.audioFileName);
                                string pAudioFile = null;
                                if (fiA.Exists)
                                {
                                    pAudioFile = fiA.FullName;
                                }
                                else
                                {
                                    FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                                    pAudioFile = fi.Directory.FullName + "\\" + myDataSource.audioFileName;
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


                            //synchronizace mluvcich podle vnitrni databaze
                            try
                            {
                                foreach (MySpeaker i in myDataSource.SeznamMluvcich.Speakers)
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

                        ZobrazXMLData();
                        this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                        return true;
                    }
                    else return false;

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
                                        pDataSource = myDataSource.Deserializovat(files[i].FullName);
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
                                    pDataSource.NovaKapitola();
                                    pDataSource.NovaSekce(0, "", -1, -1, -1);
                                    pDataSource.NovyOdstavec(0, 0, pText, new List<MyCasovaZnacka>(), -1);
                                    pDataSource.UpravCasElementu(new MyTag(0, 0, 0), 0, -1);
                                    //pDataSource.Chapters[0].Sections[0].Paragraphs[0].begin = 0;
                                    //pDataSource.Chapters[0].Sections[0].PhoneticParagraphs[0].begin = 0;
                                    pDataSource.JmenoSouboru = fi.FullName.ToUpper().Replace(".TXT", "_PHONETIC.XML");
                                    //pDataSource.Serializovat(, pDataSource, false);

                                }
                                myDataSource = pDataSource;
                                string pWav = fi.FullName.ToUpper().Replace(".TXT", ".WAV");
                                pNacitaniAudiaDavka = true;
                                NactiAudio(pWav);

                            }
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        myDataSource = myDataSource.Deserializovat(jmenoSouboru);
                    }
                    if (myDataSource != null)
                    {
                        this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                        ZobrazXMLData();
                        //nacteni audio souboru pokud je k dispozici
                        if (myDataSource.audioFileName != null && myDataSource.JmenoSouboru != null)
                        {
                            FileInfo fiA = new FileInfo(myDataSource.audioFileName);
                            string pAudioFile = null;
                            if (fiA.Exists)
                            {
                                pAudioFile = fiA.FullName;
                            }
                            else
                            {
                                FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                                pAudioFile = fi.Directory.FullName + "\\" + myDataSource.audioFileName;
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
                MyLog.LogujChybu(ex);
                MessageBox.Show("Chyba pri nacitani titulku: " + ex.Message, "Chyba");
                return false;
            }
        }


        public bool UlozitTitulky(bool pouzitSaveDialog, string jmenoSouboru)
        {
            try
            {
                if (pouzitSaveDialog)
                {
                    Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();

                    fileDialog.Title = "Uložit soubor s titulky...";
                    fileDialog.Filter = "Soubory titulků (*" + nastaveniAplikace.PriponaTitulku + ")|*" + nastaveniAplikace.PriponaTitulku + "|Všechny soubory (*.*)|*.*";
                    fileDialog.FilterIndex = 1;
                    fileDialog.OverwritePrompt = true;
                    fileDialog.RestoreDirectory = true;
                    if (fileDialog.ShowDialog() == true)
                    {
                        if (myDataSource.Serializovat(fileDialog.FileName, myDataSource, nastaveniAplikace.UkladatKompletnihoMluvciho))
                        {
                            this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                            return true;
                        }
                        else return false;
                    }
                    else return false;

                }
                else
                {
                    Console.WriteLine(jmenoSouboru);
                    bool pStav = myDataSource.Serializovat(jmenoSouboru, myDataSource, nastaveniAplikace.UkladatKompletnihoMluvciho);
                    if (pStav && lbDavkoveNacteni.Items.Count > 0)
                    {
                        if (myDataSource.JmenoSouboru.ToLower().Contains("_phonetic"))
                        {
                            for (int i = 0; i < lbDavkoveNacteni.Items.Count; i++)
                            {
                                string pPol = (lbDavkoveNacteni.Items[i] as CheckBox).Content.ToString().ToLower();
                                FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                                string pHledane = fi.Name.ToLower().Replace("_phonetic.xml", "");
                                if (pPol.Contains(pHledane))
                                {
                                    (lbDavkoveNacteni.Items[i] as CheckBox).IsChecked = true;
                                    break;
                                }
                            }
                        }
                    }
                    return pStav;
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                MessageBox.Show("Chyba pri ukladani titulku: " + ex.Message, "Chyba");
                Console.WriteLine(ex);
                return false;
            }
        }




        public void NastavPoziciKurzoru(long aMilisekundy, bool nastavitMedia, bool aNeskakatNaZacatekElementu)
        {
            try
            {
                if (aMilisekundy < 0) return;
                oVlna.KurzorPoziceMS = aMilisekundy;
                TimeSpan ts = new TimeSpan(aMilisekundy * 10000);
                string label = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2");

                lAudioPozice.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    lAudioPozice.Content = label;
                }), null);
                
                double aLeft = aMilisekundy - oVlna.mSekundyVlnyZac;
                aLeft = aLeft / oVlna.DelkaVlnyMS * myImage.ActualWidth;
                //(double)((aMilisekundy - nastaveniAplikace.mSekundyVlnyZac) / nastaveniAplikace.mSekundyVlny) * myImage.ActualWidth;


                rectangle1.Margin = new Thickness(aLeft - 2, rectangle1.Margin.Top, rectangle1.Margin.Right, rectangle1.Margin.Bottom);

                if (!Playing && jeVideo && Math.Abs(meVideo.Position.TotalMilliseconds - (aMilisekundy - 150)) > 200)
                {
                    meVideo.Position = new TimeSpan((aMilisekundy - 150) * 10000);
                }

                if (nastavitMedia)
                {
                    //mediaElement1.Position = new TimeSpan(aMilisekundy * 10000);
                    pIndexBufferuVlnyProPrehrani = (int)aMilisekundy;
                    //pCasZacatkuPrehravani = DateTime.Now.AddMilliseconds(-pIndexBufferuVlnyProPrehrani);
                    //pCasZacatkuPrehravani = DateTime.Now;
                    if (jeVideo) meVideo.Position = new TimeSpan(aMilisekundy * 10000);
                    List<MyTag> pTagy = myDataSource.VratElementDanehoCasu(aMilisekundy, null);
                    for (int i = 0; i < pTagy.Count; i++)
                    {
                        if (pTagy[i].tKapitola == nastaveniAplikace.RichTag.tKapitola && pTagy[i].tSekce == nastaveniAplikace.RichTag.tSekce && pTagy[i].tOdstavec == nastaveniAplikace.RichTag.tOdstavec)
                        {
                            return;
                        }
                    }
                    VyberElement(pTagy[0], aNeskakatNaZacatekElementu);
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
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


                //if (oPrepisovac != null && (oPrepisovac.Rozpoznavani || oPrepisovac.Ukoncovani))
                if (oHlasoveOvladani != null)//&& oHlasoveOvladani.TypRozpoznavani == MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI)
                {
                    if (oHlasoveOvladani != null && oHlasoveOvladani.Inicializovano)
                    {
                        oHlasoveOvladani.GetText();
                        oHlasoveOvladani.GetDelay();
                        oHlasoveOvladani.AsynchronniRead();
                    }

                    if (pDelkaZobrazeniHlasovehoPovelu == 4)
                    {
                        ZobrazRozpoznanyPrikaz("...");
                        pDelkaZobrazeniHlasovehoPovelu = 5;
                    }
                    else if (pDelkaZobrazeniHlasovehoPovelu < 4)
                    {
                        pDelkaZobrazeniHlasovehoPovelu++;
                    }
                }

                {
                    if (oPrepisovac != null && oPrepisovac.Inicializovano)
                    {
                        oPrepisovac.GetText();
                        oPrepisovac.GetDelay();
                        oPrepisovac.AsynchronniRead();
                    }
                }
            }
            catch
            {

            }
        }

        private void VyberTextMeziCasovymiZnackami(long aPoziceKurzoru)
        {
            try
            {
                MyTag pTag = new MyTag(nastaveniAplikace.RichTag);
                List<MyTag> pTagy = myDataSource.VratElementDanehoCasu(aPoziceKurzoru, pTag);
                if (pTagy != null && pTagy.Count > 0)
                {
                    pTag = pTagy[0];
                }
                if (pTag != null)
                {
                    VyberElement(pTag, true);
                }


                if (nastaveniAplikace.RichTag != null)
                {
                    MyParagraph pP = myDataSource.VratOdstavec(nastaveniAplikace.RichTag);
                    List<MyCasovaZnacka> pCasZnacky = null;
                    if (pP != null) pCasZnacky = pP.VratCasoveZnackyTextu;
                    if (pCasZnacky != null && pCasZnacky.Count > 1)
                    {
                        TextBox pRTB = (TextBox)nastaveniAplikace.RichTag.tSender;

                        if (aPoziceKurzoru >= pP.begin && aPoziceKurzoru <= pP.end)
                        {
                            int aIndex1 = -1;
                            int aIndex2 = -1;
                            int i1 = -1;
                            int i2 = -1;

                            for (int i = 0; i < pCasZnacky.Count; i++)
                            {
                                if (pCasZnacky[i].Time <= aPoziceKurzoru)
                                {
                                    aIndex1 = pCasZnacky[i].Index2;
                                    i1 = i;
                                }
                                if (pCasZnacky[i].Time <= aPoziceKurzoru)
                                {
                                    if (aIndex1 >= 0)
                                    {
                                        aIndex2 = pCasZnacky[i].Index2;
                                        i2 = i;
                                    }
                                }
                                else
                                {
                                    if (aIndex1 >= 0 && aIndex2 >= 0 && pCasZnacky[i2].Time - pCasZnacky[i1].Time == 0)
                                    {
                                        i2 += 1;
                                        if (i2 >= pCasZnacky.Count) i2 = pCasZnacky.Count - 1;
                                    }
                                    break;
                                }
                            }
                            if (aIndex1 >= 0 && aIndex2 >= 0)
                            {
                                ///aIndex1+=2;
                                aIndex2 = pCasZnacky[i2].Index2;// +2;




                                ///pRTB.Selection.Select(pRTB.Document.ContentStart.GetPositionAtOffset(aIndex1), pRTB.Document.ContentStart.GetPositionAtOffset(aIndex2));
                                pRTB.Select(aIndex1, aIndex2 - aIndex1);

                            }
                            else
                            {
                                aIndex1 = 0;
                                aIndex2 = pCasZnacky[0].Index2;
                                pRTB.Select(aIndex1, aIndex2 - aIndex1);
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

        public void InitializeTimer()
        {
            timer1.Interval = new TimeSpan(0, 0, 0, 0, MyKONST.PERIODA_TIMERU_VLNY_MS);
            timer1.IsEnabled = true;
            timer1.Tick += new EventHandler(OnTimer);
        }

        void OnTimer(Object source, EventArgs e)
        {
            try
            {
                //return;
                if (!mouseDown && !nahled)
                {
                    slPoziceMedia.Value = pIndexBufferuVlnyProPrehrani;
                }


                long rozdil = oVlna.DelkaVlnyMS; //delka zobrazeni v msekundach
                long celkMilisekundy = (long)slPoziceMedia.Value;
                if (_playing)
                {
                    celkMilisekundy = celkMilisekundy + (long)(DateTime.Now.Subtract(pCasZacatkuPrehravani).Duration().TotalMilliseconds) - 300;
                    //celkMilisekundy = celkMilisekundy + MWP.MSplayedThisBufer;
                    //System.Diagnostics.Debug.WriteLine("_" + (DateTime.Now.Subtract(pCasZacatkuPrehravani).Duration().TotalMilliseconds));
                    //System.Diagnostics.Debug.WriteLine(MWP.MSplayedThisBufer);

                    if (prehratVyber && celkMilisekundy < oVlna.KurzorVyberPocatekMS)
                    {
                        celkMilisekundy = oVlna.KurzorVyberPocatekMS;
                    }
                }

                oVlna.KurzorPoziceMS = celkMilisekundy;
               // oVlna.KurzorPoziceMS = MWP.MilisecondsPlayedThisSession + 
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

                    double left;

                    long rozdil2 = (celkMilisekundy - oVlna.mSekundyVlnyZac);
                    left = myImage.ActualWidth / (rozdil) * rozdil2;



                    if (prehratVyber && celkMilisekundy >= oVlna.KurzorVyberKonecMS && oVlna.KurzorVyberKonecMS > -1)
                    {
                        NastavPoziciKurzoru(oVlna.KurzorVyberPocatekMS, true, true);
                        celkMilisekundy = oVlna.KurzorPoziceMS;
                        /*
                        NastavPoziciKurzoru(oVlna.KurzorVyberKonecMS, true);
                        prehratVyber = false;
                        if (jeVideo) meVideo.Pause();
                        Playing = false;
                        */
                    }
                    else
                    {
                        NastavPoziciKurzoru(celkMilisekundy, false, true);
                    }
                    //NastavPoziciKurzoru(celkMilisekundy, false);

                    if (Playing) VyberTextMeziCasovymiZnackami(celkMilisekundy);
                }

                long pPozadovanyPocatekVlny = oVlna.mSekundyVlnyZac;

                if (celkMilisekundy >= mSekundyKonec - oVlna.MSekundyDelta && mSekundyKonec < oWav.DelkaSouboruMS && !nahled)
                {

                    if (celkMilisekundy < oWav.DelkaSouboruMS && celkMilisekundy < oVlna.bufferPrehravaniZvuku.KonecMS)
                    {
                        long pKonec = celkMilisekundy - oVlna.MSekundyDelta + rozdil;
                        long pPocatekMS = pKonec - rozdil;
                        if (pKonec > oVlna.mSekundyVlnyKon) pPocatekMS = oVlna.KurzorPoziceMS - rozdil / 2;
                        if (pPocatekMS < 0) pPocatekMS = 0;
                        pKonec = pPocatekMS + rozdil;
                        mSekundyKonec = pKonec;
                        KresliVlnu(pPocatekMS, mSekundyKonec, oVlna.PouzeCasovaOsa);
                    }
                }
                else if (celkMilisekundy < oVlna.mSekundyVlnyZac && oVlna.mSekundyVlnyZac > 0 && !nahled)
                {
                    if (celkMilisekundy < oWav.DelkaSouboruMS && celkMilisekundy < oVlna.bufferPrehravaniZvuku.KonecMS && celkMilisekundy >= oVlna.bufferPrehravaniZvuku.PocatekMS)
                    {
                        //long pKonec = celkMilisekundy + oVlna.MSekundyDelta * 2;
                        long pKonec = celkMilisekundy + (long)(rozdil * 0.3);
                        long pPocatekMS = pKonec - rozdil;
                        if (pKonec > oVlna.mSekundyVlnyKon) pPocatekMS = oVlna.KurzorPoziceMS - rozdil / 2;
                        if (pPocatekMS < 0)
                        {
                            pPocatekMS = 0;
                            pPozadovanyPocatekVlny = 0;
                        }
                        pKonec = pPocatekMS + rozdil;
                        mSekundyKonec = pKonec;
                        KresliVlnu(pPocatekMS, mSekundyKonec, oVlna.PouzeCasovaOsa);

                        /*
                        mSekundyKonec = celkMilisekundy + oVlna.MSekundyDelta;
                        if (mSekundyKonec - rozdil < 0)
                        {
                            mSekundyKonec = rozdil;
                            pPozadovanyPocatekVlny = 0;
                        }
                        KresliVlnu(mSekundyKonec - rozdil, mSekundyKonec, oVlna.PouzeCasovaOsa);
                        */
                    }

                }
                else if (mouseDown == false && nahled == true)
                {
                    nahled = false;
                    //KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, false);
                }

                if (oWav.Nacteno && celkMilisekundy > oVlna.bufferPrehravaniZvuku.KonecMS - (oVlna.bufferPrehravaniZvuku.KonecMS - oVlna.bufferPrehravaniZvuku.PocatekMS) * 0.2)
                {
                    if (!oWav.NacitaniBufferu && !nahled) //pokud jiz neni nacitano vlakno,dojde k inicializaci threadu
                    {
                        if (oVlna.bufferPrehravaniZvuku.KonecMS < oWav.DelkaSouboruMS && !oWav.NacitaniBufferu)
                        {
                            oWav.AsynchronniNacteniRamce2((long)(celkMilisekundy - (oVlna.bufferPrehravaniZvuku.KonecMS - oVlna.bufferPrehravaniZvuku.PocatekMS) * 0.3), MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS, 0);
                        }
                    }
                }
                else if (oWav.Nacteno && (oVlna.bufferPrehravaniZvuku.PocatekMS > 0 && celkMilisekundy < oVlna.bufferPrehravaniZvuku.PocatekMS + (oVlna.bufferPrehravaniZvuku.KonecMS - oVlna.bufferPrehravaniZvuku.PocatekMS) * 0.2) || pPozadovanyPocatekVlny < oVlna.bufferPrehravaniZvuku.PocatekMS)
                {
                    if (!oWav.NacitaniBufferu && !nahled) //pokud jiz neni nacitano vlakno,dojde k inicializaci threadu
                    {
                        oWav.AsynchronniNacteniRamce2((long)(celkMilisekundy - (oVlna.bufferPrehravaniZvuku.KonecMS - oVlna.bufferPrehravaniZvuku.PocatekMS) * 0.6), MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS, 0);
                    }
                }





            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                //MessageBox.Show("OnTimer(): " + ex.Message);
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
                    if (!pOtevrit)
                    {
                        aFileName = openDialog.FileName;
                    }



                    FileInfo fi = new FileInfo(aFileName);
                    if (fi != null)
                    {
                        myDataSource.audioFileName = fi.Name;
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
                        if (MWP != null)
                        {
                            MWP.Dispose();
                            MWP = null;
                        }
                        pIndexBufferuVlnyProPrehrani = 0;


                        pbPrevodAudio.Value = 0;
                        slPoziceMedia.Maximum = pDelkaSouboruMS;
                        TimeSpan ts = new TimeSpan(pDelkaSouboruMS * 10000);
                        lAudioDelka2.Content = ts.Hours.ToString("d1") + ":" + ts.Minutes.ToString("d2") + ":" + ts.Seconds.ToString("d2");

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
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
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
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }

        }

        public bool KresliCasovouOsu(long zacatek, long konec)
        {
            try
            {

                //casova osa
                gCasovaOsa.Children.Clear();
                double delka = (double)((double)(konec - zacatek) / 1000);

                double krokMaly = Math.Round(delka / 30);
                if (krokMaly < 1) krokMaly = 1;
                double krok = Math.Round(delka / 5);
                long pPocatekPrvniZnacky_S = zacatek / 1000;
                int pDelka_S = (int)Math.Round(delka);

                if (pDelka_S < 6)
                {
                    krok = 1;
                    krokMaly = 0.1;
                }
                else if (pDelka_S >= 6 && pDelka_S < 20)
                {
                    krok = 2;
                    krokMaly = 0.5;
                }
                else if (pDelka_S >= 20 && pDelka_S < 45)
                {
                    krok = 5;
                    krokMaly = 1;
                }
                else if (pDelka_S >= 45 && pDelka_S < 90)
                {
                    krok = 10;
                    krokMaly = 2;
                }
                else if (pDelka_S >= 90 && pDelka_S < 150)
                {
                    krok = 15;
                    krokMaly = 3;
                }
                else if (pDelka_S >= 150)
                {
                    krok = 30;
                    krokMaly = 5;

                }
                while (pPocatekPrvniZnacky_S % krok != 0 && pPocatekPrvniZnacky_S > 0)
                {
                    pPocatekPrvniZnacky_S--;
                }



                if (krok < 1) krok = 1;

                if (Math.Abs((int)krok - 5) == 1)
                {
                    krok = 5;
                }
                double pKonecKroku = ((double)konec / 1000) - pPocatekPrvniZnacky_S;
                //for (double i = krokMaly; i <= delka; i = i + krokMaly)
                for (double i = krokMaly; i <= pKonecKroku; i = i + krokMaly)
                {
                    double aLeft = (pPocatekPrvniZnacky_S + i) * 1000 - oVlna.mSekundyVlnyZac;
                    aLeft = aLeft / oVlna.DelkaVlnyMS * myImage.ActualWidth;
                    double pozice = aLeft;
                    Rectangle r1 = new Rectangle();
                    r1.Margin = new Thickness(pozice - 2, 0, 0, gCasovaOsa.ActualHeight / 3 * 2.5);
                    r1.Height = gCasovaOsa.ActualHeight;
                    r1.Width = 1;
                    r1.HorizontalAlignment = HorizontalAlignment.Left;
                    r1.Fill = Brushes.Black;

                    gCasovaOsa.Children.Add(r1);
                }

                for (double i = krok; i <= delka; i = i + krok)
                {
                    double aLeft = (pPocatekPrvniZnacky_S + i) * 1000 - oVlna.mSekundyVlnyZac;
                    aLeft = aLeft / oVlna.DelkaVlnyMS * myImage.ActualWidth;
                    double pozice = aLeft;

                    TimeSpan ts = new TimeSpan((long)(pPocatekPrvniZnacky_S * 1000 + i * 1000) * 10000);

                    Label lX = new Label();
                    lX.Content = ts.Minutes.ToString("D2") + "m : " + ts.Seconds.ToString("D2") + "s";
                    lX.Margin = new Thickness(pozice - 32, 0, 0, 0);
                    double d = lX.ActualWidth;
                    Rectangle r1 = new Rectangle();
                    r1.Margin = new Thickness(pozice - 2, 0, 0, gCasovaOsa.ActualHeight / 3 * 2);
                    r1.Height = gCasovaOsa.ActualHeight;
                    r1.Width = 2;
                    r1.HorizontalAlignment = HorizontalAlignment.Left;
                    r1.Fill = Brushes.Black;

                    gCasovaOsa.Children.Add(lX);
                    gCasovaOsa.Children.Add(r1);


                }

                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }
        }


        public bool KresliMluvciDoVlny(MySubtitlesData aDokument, MyVlna aZobrazenaVlna)
        {
            try
            {

                if (aDokument == null) return false;
                if (aZobrazenaVlna == null) return false;
                //smazani obdelniku mluvcich
                foreach (Button pL in bObelnikyMluvcich)
                {
                    grid1.Children.Remove(pL);
                }
                this.bObelnikyMluvcich.Clear();

                for (int i = 0; i < aDokument.Chapters.Count; i++)
                {
                    MyChapter pChapter = aDokument.Chapters[i];
                    for (int j = 0; j < pChapter.Sections.Count; j++)
                    {
                        MySection pSection = pChapter.Sections[j];
                        for (int k = 0; k < pSection.Paragraphs.Count; k++)
                        {
                            MyParagraph pParagraph = pSection.Paragraphs[k];
                            long pBegin = pParagraph.begin;
                            long pEnd = pParagraph.end;
                            if (pBegin >= 0 && pEnd != pBegin && pEnd >= 0)
                            {
                                if ((pBegin < aZobrazenaVlna.mSekundyVlnyZac && pEnd < aZobrazenaVlna.mSekundyVlnyZac) || (pBegin > aZobrazenaVlna.mSekundyVlnyKon))
                                {

                                }
                                else
                                {

                                    if (pBegin >= aZobrazenaVlna.mSekundyVlnyZac && pEnd <= aZobrazenaVlna.mSekundyVlnyKon)
                                    {

                                    }
                                    if (pBegin < aZobrazenaVlna.mSekundyVlnyZac) pBegin = aZobrazenaVlna.mSekundyVlnyZac;
                                    if (pEnd > aZobrazenaVlna.mSekundyVlnyKon) pEnd = aZobrazenaVlna.mSekundyVlnyKon;

                                    Button pMluvci = new Button();
                                    pMluvci.VerticalAlignment = VerticalAlignment.Top;
                                    pMluvci.HorizontalAlignment = HorizontalAlignment.Left;

                                    double aLeft = (double)(myImage.ActualWidth * (pBegin - aZobrazenaVlna.mSekundyVlnyZac) / aZobrazenaVlna.DelkaVlnyMS);
                                    //double aLeft = (double)(gCasovaOsa.ActualWidth * (pBegin - aZobrazenaVlna.mSekundyVlnyZac) / aZobrazenaVlna.DelkaVlnyMS);

                                    //double aRight = myImage.ActualWidth - (double)(myImage.ActualWidth * (pEnd - aZobrazenaVlna.mSekundyVlnyZac) / aZobrazenaVlna.DelkaVlnyMS);
                                    double aRight = gCasovaOsa.ActualWidth - (double)(myImage.ActualWidth * (pEnd - aZobrazenaVlna.mSekundyVlnyZac) / aZobrazenaVlna.DelkaVlnyMS);
                                    pMluvci.Margin = new Thickness(aLeft, pMluvci.Margin.Top, pMluvci.Margin.Right, rectangle2.Margin.Bottom);
                                    //pMluvci.Width = myImage.ActualWidth - aLeft - aRight;
                                    pMluvci.Width = gCasovaOsa.ActualWidth - aLeft - aRight;

                                    pMluvci.Background = Brushes.LightPink;
                                    MySpeaker pSpeaker = aDokument.SeznamMluvcich.VratSpeakera(pParagraph.speakerID);
                                    string pText = "";
                                    if (pSpeaker != null) pText = pSpeaker.FullName;
                                    //pMluvci.Content = pText;
                                    pMluvci.Visibility = Visibility.Visible;
                                    pMluvci.BringIntoView();
                                    pMluvci.Focusable = false;
                                    pMluvci.IsTabStop = false;
                                    pMluvci.Cursor = Cursors.Arrow;
                                    //pMluvci.IsHitTestVisible = false;
                                    if (pText != null && pText != "") pMluvci.ToolTip = pMluvci.Content;
                                    pMluvci.Tag = new MyTag(i, j, k);
                                    pMluvci.Click += new RoutedEventHandler(pMluvci_Click);
                                    pMluvci.MouseDoubleClick += new MouseButtonEventHandler(pMluvci_MouseDoubleClick);


                                    DockPanel dp = new DockPanel() { LastChildFill = true, Margin = new Thickness(0, 0, 0, 0) };
                                    dp.Height = 15;
                                    DockPanel dp2 = new DockPanel() { LastChildFill = true, Margin = new Thickness(0,0,0,0)};
                                    dp2.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                                    dp2.Height = 15;
                                    if (k == 0) //prvni zaznam v sekci
                                    {
                                        Ellipse el = new Ellipse();
                                        el.Width = 10;
                                        el.Height = 10;
                                        el.Margin = new Thickness(0, 0, 0, 0);
                                        el.Stroke = null;
                                        el.Fill = Brushes.DarkRed;
                    
                                        dp.Children.Add(el);

                                    }


                                    if (
                                        (k == pSection.Paragraphs.Count - 2 && j == pChapter.Sections.Count - 1)
                                        ||
                                        (k == pSection.Paragraphs.Count - 1 && j != pChapter.Sections.Count - 1)
                                        )//posledni zaznam v sekci
                                    {
                                        Ellipse el = new Ellipse();
                                        el.Width = 10;
                                        el.Height = 10;
                                        el.Margin = new Thickness(0, 0, 0, 0);
                                        el.Stroke = null;
                                        el.Fill = Brushes.DarkRed;
                                        dp2.Children.Add(el);
                                    }
                                    Label lab = new Label() { Content = pText, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 0) };
                                    lab.Padding = new Thickness(0, 0, 0, 0);
                                    lab.Height = 15;
                                    dp2.Children.Add(lab);
                                    dp.Children.Add(dp2);
                                    pMluvci.Content = dp;

                                    pMluvci.SizeChanged += new SizeChangedEventHandler(pMluvci_SizeChanged);

                                    this.bObelnikyMluvcich.Add(pMluvci);
                                }
                            }
                        }
                    }
                }
                MyParagraph pPredchozi = null;
                MyParagraph pNasledujici = null;
                for (int i = 0; i < this.bObelnikyMluvcich.Count; i++)
                {
                    Button pMluvci = this.bObelnikyMluvcich[i];
                    MyParagraph pPar = aDokument.VratOdstavec((pMluvci.Tag as MyTag));
                    if (i < this.bObelnikyMluvcich.Count - 1)
                    {
                        pNasledujici = aDokument.VratOdstavec((this.bObelnikyMluvcich[i + 1].Tag as MyTag));
                    }
                    if (pPredchozi != null && pPredchozi.end > pPar.begin && bObelnikyMluvcich[i - 1].Margin.Top < 5)
                    {
                        grid1.Children.Add(pMluvci);
                        grid1.UpdateLayout();
                        pMluvci.Margin = new Thickness(pMluvci.Margin.Left, pMluvci.ActualHeight, pMluvci.Margin.Right, pMluvci.Margin.Bottom);
                    }
                    else
                    {
                        grid1.Children.Add(pMluvci);
                    }
                    pPredchozi = pPar;
                    //grid1.UpdateLayout();
                }


                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }
        }

        void pMluvci_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Button)
            {
                Button sndr = (Button)sender;
                double width = sndr.Width - 10;
                (sndr.Content as DockPanel).Width = (width > 0.0) ? width : 0.0;
            }
        }

        void pMluvci_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MyTag pTag = (sender as Button).Tag as MyTag;
                VyberElement(pTag, false);
                new WinSpeakers(pTag, this.nastaveniAplikace, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();
                UpdateXMLData(false, true, true, false, true);
            }
            catch
            {

            }
        }

        /// <summary>
        /// co se deje pri kliknuti na tlacitko mluvciho v signalu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pMluvci_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MyTag pTag = (sender as Button).Tag as MyTag;
                bool pVybran = VyberElement(pTag, false);
                //if (pVybran)
                {
                    oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(pTag);
                    oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(pTag);
                    KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, -1);
                }
            }
            catch
            {

            }
        }


        delegate bool DelegatVyberElementu(MyTag aTag, bool aNezastavovatPrehravani);

        /// <summary>
        /// THREAD SAFE - vybere element xml
        /// </summary>
        /// <param name="aTagVyberu"></param>
        /// <returns></returns>
        private bool VyberElement(MyTag aTagVyberu, bool aNezastavovatPrehravani)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new DelegatVyberElementu(VyberElement), aTagVyberu, aNezastavovatPrehravani );

                return false;
            }
            bool pStav = nastaveniAplikace.SetupSkocitZastavit;
            try
            {

                if (aTagVyberu == null) return false;
                long pPocatekMS = myDataSource.VratCasElementuPocatek(aTagVyberu);
                long pKonecMS = myDataSource.VratCasElementuKonec(aTagVyberu);
                aTagVyberu.tSender = VratSenderTextboxu(aTagVyberu);
                if (aTagVyberu.tOdstavec != nastaveniAplikace.RichTag.tOdstavec || aTagVyberu.tSekce != nastaveniAplikace.RichTag.tSekce || aTagVyberu.tKapitola != nastaveniAplikace.RichTag.tKapitola)
                {

                    oVlna.KurzorVyberPocatekMS = pPocatekMS;
                    oVlna.KurzorVyberKonecMS = pKonecMS;
                    KresliVyber(pPocatekMS, pKonecMS, -1);

                    pPokracovatVprehravaniPoVyberu = aNezastavovatPrehravani;
                    this.pNeskakatNaZacatekElementu = aNezastavovatPrehravani;
                    nastaveniAplikace.SetupSkocitZastavit = !aNezastavovatPrehravani;
                    if (aTagVyberu.tSender == null) return false;
                    bool pFonFocus = tbFonetickyPrepis.IsFocused;
                    bool pRet = (aTagVyberu.tSender as TextBox).Focus();
                    if (pFonFocus) tbFonetickyPrepis.Focus();
                    return pRet;
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                pPokracovatVprehravaniPoVyberu = false;
                nastaveniAplikace.SetupSkocitZastavit = pStav;
            }

        }

        //nakresli vyber 
        public bool KresliVyber(long zacatek, long konec, long kurzor)
        {
            try
            {

                //if ((zacatek != -1 && konec != -1) || (konec != -1))
                if ((zacatek != -1 && konec != -1) || (zacatek != -1))
                {
                    //if (konec - zacatek < 0) return false;
                    if (kurzor >= 0 && kurzor >= oVlna.mSekundyVlnyZac && kurzor <= oVlna.mSekundyVlnyKon)
                    {
                        if (!pPokracovatVprehravaniPoVyberu)
                        {
                            NastavPoziciKurzoru(kurzor, true, true);
                        }
                    }
                    if (konec - zacatek < 0) return false;

                    if (zacatek < oVlna.mSekundyVlnyZac)
                    {
                        if (konec > oVlna.mSekundyVlnyKon)
                        {
                            rectangle2.Margin = new Thickness(0, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                            rectangle2.Width = myImage.ActualWidth;
                            rectangle2.Visibility = Visibility.Visible;
                        }
                        else if (konec > oVlna.mSekundyVlnyZac)
                        {
                            rectangle2.Margin = new Thickness(0, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                            rectangle2.Width = (double)(myImage.ActualWidth * (konec - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);
                            rectangle2.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            rectangle2.Visibility = Visibility.Hidden;
                        }
                    }
                    else if (konec > oVlna.mSekundyVlnyKon)//zacatek vyberu  je vetsi nez zacatek vlny
                    {
                        if (zacatek < oVlna.mSekundyVlnyKon)
                        {
                            double aLeft = (double)(myImage.ActualWidth * (double)(zacatek - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);
                            rectangle2.Margin = new Thickness(aLeft, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                            rectangle2.Width = myImage.ActualWidth - aLeft;
                            rectangle2.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            rectangle2.Visibility = Visibility.Hidden;
                        }
                    }
                    else //zac a kon v intervalu vlny
                    {
                        double aLeft = (double)(myImage.ActualWidth * (zacatek - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);

                        double aRight = myImage.ActualWidth - (double)(myImage.ActualWidth * (konec - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);
                        rectangle2.Margin = new Thickness(aLeft, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                        rectangle2.Width = myImage.ActualWidth - aLeft - aRight;
                        rectangle2.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    rectangle2.Visibility = Visibility.Hidden;
                }
                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }
        }

        public bool KresliVlnu2(double zacatek, double konec, bool pouzeCasovaOsa)
        {
            try
            {
                if (!pouzeCasovaOsa)
                {

                    GeometryGroup myGeometryGroup = new GeometryGroup();


                    int pPocetZobrazovanychPixelu = (int)this.ActualWidth;
                    if (pPocetZobrazovanychPixelu <= 0) pPocetZobrazovanychPixelu = 1600;

                    long pZacatek = (long)zacatek;
                    long pKonec = (long)konec;

                    float[] pData = oVlna.bufferCeleVlny.VratDataBuffer(ref pZacatek, ref pKonec, oVlna.DelkaVlnyMS, pPocetZobrazovanychPixelu);

                    oVlna.mSekundyVlnyZac = pZacatek;
                    oVlna.mSekundyVlnyKon = pKonec;
                    mSekundyKonec = oVlna.mSekundyVlnyKon;

                    int Xsouradnice = 0;
                    for (int i = 0; i < pData.Length - 1; i++)
                    {
                        myGeometryGroup.Children.Add(new LineGeometry(new Point(Xsouradnice, pData[Xsouradnice] * oVlna.ZvetseniVlnyYSmerProcenta), new Point(Xsouradnice, pData[Xsouradnice + 1] * oVlna.ZvetseniVlnyYSmerProcenta)));
                        i++;
                        Xsouradnice++;
                    }

                    // Create a GeometryDrawing and use the GeometryGroup to specify
                    // its geometry.
                    GeometryDrawing myGeometryDrawing = new GeometryDrawing();
                    myGeometryDrawing.Geometry = myGeometryGroup;
                    // Add the GeometryDrawing to a DrawingGroup.
                    DrawingGroup myDrawingGroup = new DrawingGroup();
                    myDrawingGroup.Children.Add(myGeometryDrawing);
                    // Create a Pen to add to the GeometryDrawing created above.
                    Pen myPen = new Pen();
                    myPen.Thickness = 1;
                    //myPen.LineJoin = PenLineJoin.Round;
                    //myPen.EndLineCap = PenLineCap.Round;

                    myPen.Brush = Brushes.Red;

                    myGeometryDrawing.Pen = myPen;
                    // Create an Image and set its DrawingImage to the Geometry created above.
                    //Image myImage = new Image();
                    myImage.Stretch = Stretch.Fill;
                    myImage.Stretch = Stretch.None;
                    //myImage.Margin = new Thickness(10);

                    DrawingImage myDrawingImage = new DrawingImage();
                    myDrawingImage.Drawing = myDrawingGroup;



                    myImage.Source = myDrawingImage;

                    //casova osa
                    KresliCasovouOsu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon);
                    if (!_playing)
                    {
                        KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, oVlna.KurzorPoziceMS);
                    }
                    else
                    {
                        KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, -1);

                    }
                }
                else
                {
                    //casova osa
                    KresliCasovouOsu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon);
                    KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, oVlna.KurzorPoziceMS);
                }

                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }


        }

        /// <summary>
        /// asynchronni vykresleni vlny - dodelat
        /// </summary>
        /// <param name="zacatek"></param>
        /// <param name="konec"></param>
        /// <param name="pouzeCasovaOsa"></param>
        /// <returns></returns>
        public bool KresliVlnu(double zacatek, double konec, bool pouzeCasovaOsa)
        {
            //Thread tKresleniVlny = new Thread(new ParameterizedThreadStart(KresliVlnuB));

            double[] pParametry = new double[3];
            pParametry[0] = zacatek;
            pParametry[1] = konec;
            pParametry[2] = -1;
            if (pouzeCasovaOsa) pParametry[2] = 1;

            //tKresleniVlny.Start(pParametry);

            KresliVlnuB(pParametry);
            return true;
        }

        /// <summary>
        /// THREAD SAFE - nakresli vlnu do formulare
        /// </summary>
        /// <param name="aParametry"></param>
        public void KresliVlnuB(object aParametry)
        {
            try
            {
                if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
                {
                    this.Dispatcher.Invoke(new Action<object>(KresliVlnuB), new object[] { aParametry });
                    //this.Dispatcher.Invoke(new DelegateKresliVlnuAOstatni(KresliVlnuAOstatni), new object[] { aImage });
                    return;
                }

                double zacatek = ((double[])aParametry)[0];
                double konec = ((double[])aParametry)[1];
                bool pouzeCasovaOsa = ((double[])aParametry)[2] > 0;

                if (!pouzeCasovaOsa)
                {

                    GeometryGroup myGeometryGroup = new GeometryGroup();

                    Int64 zac = (Int64)((zacatek / 1000.0) * oWav.pFrekvence - ((double)oVlna.bufferPrehravaniZvuku.PocatekMS) / 1000 * oWav.pFrekvence);
                    Int64 kon = (Int64)((konec / 1000.0) * oWav.pFrekvence - ((double)oVlna.bufferPrehravaniZvuku.PocatekMS) / 1000 * oWav.pFrekvence);


                    ///////////////////////////////////////////////////////////////////////////////
                    if (zac < -100)
                    {
                        return; //pockani na nacteni bufferu z disku o spravnem rozsahu
                    }

                    if (konec > oVlna.mSekundyVlnyKon && konec < oWav.DelkaSouboruMS)
                    {
                        //return; //pockani na nacteni bufferu z disku
                    }
                    //////////////////////////////////////////////////////////////////////////////

                    if (konec > oVlna.bufferPrehravaniZvuku.KonecMS)
                    {
                        kon = (Int64)(((double)(oVlna.bufferPrehravaniZvuku.KonecMS - oVlna.bufferPrehravaniZvuku.PocatekMS)) / 1000 * oWav.pFrekvence);// - 1;


                        zac = (Int64)(kon - (oVlna.DelkaVlnyMS / 1000.0) * oWav.pFrekvence);

                    }


                    oVlna.mSekundyVlnyZac = 1000 * zac / oWav.pFrekvence + oVlna.bufferPrehravaniZvuku.PocatekMS;
                    oVlna.mSekundyVlnyKon = (long)((double)1000 * kon / oWav.pFrekvence) + oVlna.bufferPrehravaniZvuku.PocatekMS;
                    mSekundyKonec = oVlna.mSekundyVlnyKon;
                    if (zac < 0)
                    {
                        zac = 0;
                        if (kon < 0) kon = zac + 30 * oWav.pFrekvence;//pokud je vse mimo rozsah bufferu,je nakreslena vlna delky 30s
                        oVlna.mSekundyVlnyZac = 1000 * zac / oWav.pFrekvence + oVlna.bufferPrehravaniZvuku.PocatekMS;
                        oVlna.mSekundyVlnyKon = 1000 * kon / oWav.pFrekvence + oVlna.bufferPrehravaniZvuku.PocatekMS;
                        mSekundyKonec = (int)(oVlna.mSekundyVlnyKon);
                        oVlna.NastavDelkuVlny((uint)((kon - zac) * 1000 / oWav.pFrekvence));


                    }

                    int pPocetZobrazovanychPixelu = (int)this.ActualWidth;
                    if (pPocetZobrazovanychPixelu <= 0) pPocetZobrazovanychPixelu = 1600;

                    int pKolikVzorkuKomprimovat = (int)((double)(kon - zac) / (double)pPocetZobrazovanychPixelu);
                    double[] pPoleVykresleni = new double[(kon - zac) / pKolikVzorkuKomprimovat];

                    double pMezivypocetK = 0;
                    int pocetK = 0;
                    double pMezivypocetZ = 0;
                    int pocetZ = 0;
                    int j = 1;
                    int Xsouradnice = 1;

                    double[] pPoleVykresleni2 = new double[pPoleVykresleni.Length * 4];
                    int pIndexKamKreslit = 0;
                    int[] pPoleVykresleni2Xsouradnice = new int[pPoleVykresleni2.Length];
                    double pMaxK = 0;
                    double pMinZ = 0;

                    for (int i = (int)zac; i < kon; i++)
                    {
                        if (oVlna.bufferPrehravaniZvuku.data[i] > 0)
                        {
                            pMezivypocetK += oVlna.bufferPrehravaniZvuku.data[i];
                            pocetK++;
                        }
                        else
                        {
                            pMezivypocetZ += oVlna.bufferPrehravaniZvuku.data[i];
                            pocetZ++;
                        }
                        if (j > pKolikVzorkuKomprimovat)
                        {
                            bool kreslenoK = false;
                            if (pocetK > 0)
                            {
                                pMezivypocetK = pMezivypocetK / pocetK / 32767;
                                pPoleVykresleni[Xsouradnice] = pMezivypocetK;
                                //myGeometryGroup.Children.Add(new LineGeometry(new Point(Xsouradnice - 1, pPoleVykresleni[Xsouradnice - 1]), new Point(Xsouradnice, pMezivypocetK)));
                                kreslenoK = true;

                                if (pMezivypocetK > pMaxK) pMaxK = pMezivypocetK;
                                pPoleVykresleni2[pIndexKamKreslit] = pPoleVykresleni[Xsouradnice - 1];
                                pPoleVykresleni2Xsouradnice[pIndexKamKreslit] = Xsouradnice - 1;
                                pIndexKamKreslit++;
                                pPoleVykresleni2[pIndexKamKreslit] = pMezivypocetK;
                                pPoleVykresleni2Xsouradnice[pIndexKamKreslit] = Xsouradnice;
                                pIndexKamKreslit++;
                            }

                            if (pocetZ > 0)
                            {
                                pMezivypocetZ = pMezivypocetZ / pocetZ / 32767;
                                pPoleVykresleni[Xsouradnice] = pMezivypocetZ;

                                if (pMezivypocetZ < pMinZ) pMinZ = pMezivypocetZ;
                                if (kreslenoK)
                                {
                                    //myGeometryGroup.Children.Add(new LineGeometry(new Point(Xsouradnice, pMezivypocetK), new Point(Xsouradnice, pMezivypocetZ)));

                                    pPoleVykresleni2[pIndexKamKreslit] = pMezivypocetK;
                                    pPoleVykresleni2Xsouradnice[pIndexKamKreslit] = Xsouradnice;
                                    pIndexKamKreslit++;
                                    pPoleVykresleni2[pIndexKamKreslit] = pMezivypocetZ;
                                    pPoleVykresleni2Xsouradnice[pIndexKamKreslit] = Xsouradnice;
                                    pIndexKamKreslit++;

                                }
                                else
                                {
                                    //myGeometryGroup.Children.Add(new LineGeometry(new Point(Xsouradnice - 1, pPoleVykresleni[Xsouradnice - 1]), new Point(Xsouradnice, pMezivypocetZ)));

                                    pPoleVykresleni2[pIndexKamKreslit] = pPoleVykresleni[Xsouradnice - 1];
                                    pPoleVykresleni2Xsouradnice[pIndexKamKreslit] = Xsouradnice - 1;
                                    pIndexKamKreslit++;
                                    pPoleVykresleni2[pIndexKamKreslit] = pMezivypocetZ;
                                    pPoleVykresleni2Xsouradnice[pIndexKamKreslit] = Xsouradnice;
                                    pIndexKamKreslit++;
                                }

                            }
                            pMezivypocetK = 0;
                            pMezivypocetZ = 0;
                            pocetK = 0;
                            pocetZ = 0;

                            Xsouradnice++;
                            j = 0;
                        }

                        j++;
                    }


                    if (pIndexKamKreslit > 0)
                    {
                        float pZvetseni = oVlna.ZvetseniVlnyYSmerProcenta;
                        if (oVlna.AutomatickeMeritko)
                        {
                            pZvetseni = (float)(pMaxK + Math.Abs(pMinZ));
                            if (Math.Abs(pZvetseni) < 0.0001) pZvetseni = 1;
                            //pZvetseni = (float)((rectangle1.ActualHeight - gCasovaOsa.ActualHeight) / pZvetseni);
                            pZvetseni = (float)((rectangle1.ActualHeight - gCasovaOsa.ActualHeight * 2) / pZvetseni);
                        }
                        //myGeometryGroup.Children.Clear();
                        for (int iii = 0; iii < pIndexKamKreslit; iii++)
                        {
                            myGeometryGroup.Children.Add(new LineGeometry(new Point(pPoleVykresleni2Xsouradnice[iii], pPoleVykresleni2[iii] * pZvetseni), new Point(pPoleVykresleni2Xsouradnice[iii + 1], pPoleVykresleni2[iii + 1] * pZvetseni)));
                            iii++;
                        }
                    }


                    // Create a GeometryDrawing and use the GeometryGroup to specify
                    // its geometry.
                    GeometryDrawing myGeometryDrawing = new GeometryDrawing();
                    myGeometryDrawing.Geometry = myGeometryGroup;
                    // Add the GeometryDrawing to a DrawingGroup.
                    DrawingGroup myDrawingGroup = new DrawingGroup();
                    myDrawingGroup.Children.Add(myGeometryDrawing);
                    // Create a Pen to add to the GeometryDrawing created above.
                    Pen myPen = new Pen();
                    myPen.Thickness = 1;
                    //myPen.LineJoin = PenLineJoin.Round;
                    //myPen.EndLineCap = PenLineCap.Round;

                    myPen.Brush = Brushes.Red;

                    myGeometryDrawing.Pen = myPen;
                    // Create an Image and set its DrawingImage to the Geometry created above.
                    //Image myImage = new Image();
                    //myImage.Stretch = Stretch.Fill;
                    //myImage.Stretch = Stretch.None;
                    //myImage.Margin = new Thickness(10);

                    DrawingImage myDrawingImage = new DrawingImage();
                    myDrawingImage.Drawing = myDrawingGroup;
                    KresliVlnuAOstatni(myDrawingImage);



                    ///myImage.Source = myDrawingImage;
                    ///myImage.UpdateLayout();

                    //this.Dispatcher.Invoke(DispatcherPriority.Normal, new DelegateKresliVlnuAOstatni(KresliVlnuAOstatni), myDrawingImage);


                }
                else
                {
                    KresliVlnuAOstatni(null);
                    //casova osa
                    ///KresliCasovouOsu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon);
                    ///KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, oVlna.KurzorPoziceMS);
                    ///KresliMluvciDoVlny(this.myDataSource, oVlna);
                }



                //return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                //return false;
            }


        }


        private delegate void DelegateKresliVlnuAOstatni(DrawingImage aImage);


        /// <summary>
        /// Thread SAFE - preda data source imagi vlny z threadu
        /// </summary>
        /// <param name="e"></param>
        private void KresliVlnuAOstatni(DrawingImage aImage)
        {
            try
            {
                if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
                {
                    //this.Dispatcher.Invoke(new DelegatKresliVlnu(KresliVlnuB), new object[] { aParametry });
                    this.Dispatcher.Invoke(new DelegateKresliVlnuAOstatni(KresliVlnuAOstatni), new object[] { aImage });
                    return;
                }
                if (aImage != null)
                {
                    myImage.Source = aImage;
                    myImage.UpdateLayout();
                }


                //casova osa
                KresliCasovouOsu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon);
                KresliMluvciDoVlny(this.myDataSource, oVlna);
                if (!_playing)
                {
                    KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, oVlna.KurzorPoziceMS);
                }
                else
                {
                    KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, -1);

                }
                if (oWav.Prevedeno)
                {
                    slPoziceMedia.SelectionStart = oVlna.mSekundyVlnyZac;
                    slPoziceMedia.SelectionEnd = oVlna.mSekundyVlnyKon;
                }

                //CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
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
                MWP = new MyWavePlayer(nastaveniAplikace.audio.VystupniZarizeniIndex,4800, WOP_ChciData);
                return true;
            }
            catch
            {
                return false;
            }

        }

        /// <summary>
        /// inicializuje MWR - recorder audia, pokud neni null, zavola dispose a opet ho vytvori
        /// </summary>
        /// <returns></returns>
        private bool InicializaceAudioRecorderu()
        {
            try
            {
                if (MWR != null)
                {
                    MWR.Dispose();
                    MWR = null;
                }
                MWR = new MyWaveRecorder(nastaveniAplikace.audio.VstupniZarizeniIndex, new WaveFormat(16000, 16, 1), (int)((4800 / nastaveniAplikace.ZpomalenePrehravaniRychlost) * 1.1), 3, new BufferDoneEventHandler(MWR_MamData));
                return true;
            }
            catch
            {
                return false;
            }

        }


        private void btPrehratZastavit_Click(object sender, RoutedEventArgs e)
        {

            if (MWP == null)
            {
                if (InicializaceAudioPrehravace()) pZacloPrehravani = false;
            }

            if (!_playing)
            {
                if (jeVideo) meVideo.Play();
                Playing = true;
            }
            else
            {
                if (jeVideo) meVideo.Pause();
                Playing = false;
            }
        }

        private void slPoziceMedia_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            /*if (mouseDown)
            {
                mediaElement1.Pause();
                if (jeVideo) meVideo.Pause();
                int SliderValue = (int)slPoziceMedia.Value;
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                oVlna.Kurzor = (long)slPoziceMedia.Value;
                mediaElement1.Position = ts;
                if (jeVideo) meVideo.Position = ts;
                if (playing)
                {
                    mediaElement1.Play();
                    if (jeVideo) meVideo.Play();
                }
            }*/


            if (mouseDown)
            {
                if (jeVideo) meVideo.Pause();
                int SliderValue = (int)slPoziceMedia.Value;
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                oVlna.KurzorPoziceMS = (long)slPoziceMedia.Value;
                pIndexBufferuVlnyProPrehrani = SliderValue;
                pCasZacatkuPrehravani = DateTime.Now.AddMilliseconds(-pIndexBufferuVlnyProPrehrani);
                if (jeVideo) meVideo.Position = ts;
                if (_playing)
                {
                    if (jeVideo) meVideo.Play();
                }
            }


        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            mediaElement1.Stop();
            if (jeVideo) meVideo.Stop();
            Playing = false;
            slPoziceMedia.Value = 0;
        }

        private void slPoziceMedia_LostMouseCapture(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void slPoziceMedia_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {

                mouseDown = true;
                nahled = true;


                //z value changed - pozor pri zmene
                if (mouseDown)
                {
                    if (jeVideo) meVideo.Pause();
                    int SliderValue = (int)slPoziceMedia.Value;
                    TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                    oVlna.KurzorPoziceMS = (long)slPoziceMedia.Value;
                    pIndexBufferuVlnyProPrehrani = SliderValue;
                    pCasZacatkuPrehravani = DateTime.Now.AddMilliseconds(-pIndexBufferuVlnyProPrehrani);
                    if (jeVideo) meVideo.Position = ts;
                    if (_playing)
                    {
                        if (jeVideo) meVideo.Play();
                    }
                }

            }
        }

        //menu----------------------------------------------------------------------

        #region menu Soubor
        private void MSoubor_Novy_Click(object sender, RoutedEventArgs e)
        {

            NoveTitulky();
        }

        private void MSoubor_Otevrit_Titulky_Click(object sender, RoutedEventArgs e)
        {
            OtevritTitulky(true, "", false);
            //refresh uz vykreslenych textboxu
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

        private void MSoubor_Otevrit_Video_Click(object sender, RoutedEventArgs e)
        {
            NactiVideo(null);
        }

        private void MSoubor_Ulozit_Click(object sender, RoutedEventArgs e)
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

        private void MSoubor_Ulozit_Titulky_Jako_Click(object sender, RoutedEventArgs e)
        {
            UlozitTitulky(true, "");
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
            try
            {
                if (nastaveniAplikace.RichFocus)
                {
                    new WinSpeakers(nastaveniAplikace.RichTag, this.nastaveniAplikace, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();

                    UpdateXMLData(false, true, true, false, true);
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

        }

        private void MNastroje_Nastaveni_Click(object sender, RoutedEventArgs e)
        {
            nastaveniAplikace = WinSetup.WinSetupNastavit(nastaveniAplikace, myDatabazeMluvcich);
            //pokus o ulozeni konfigurace
            nastaveniAplikace.Serializovat(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.KONFIGURACNI_SOUBOR, nastaveniAplikace);

            InicializaceAudioPrehravace();  //nove nastaveni prehravaciho zarizeni 

            UpdateXMLData();    //zobrazeni xml dat v pripade zmeny velikosti pisma
            UpdateXMLData();

            //nastaveni fonetickeho prepisu





        }

        private void MNapoveda_Popis_Programu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!oknoNapovedy.IsLoaded)
                {
                    oknoNapovedy = new WinHelp();
                    oknoNapovedy.Show();
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

        }

        private void MNapoveda_O_Programu_Click(object sender, RoutedEventArgs e)
        {
            new WinOProgramu(MyKONST.NAZEV_PROGRAMU).ShowDialog();
        }

        #endregion

        //obsluha tlacitek v toolbaru u vlny....----------------------------------------------------------------------------
        private void Toolbar1Btn5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button pButton = sender as Button;
                long pDelka = long.Parse(pButton.Tag.ToString());
                long pPocatek = oVlna.mSekundyVlnyZac;
                long pKonec = oVlna.mSekundyVlnyZac + pDelka;
                if (pKonec < oVlna.KurzorPoziceMS) pPocatek = oVlna.KurzorPoziceMS - pDelka / 2;
                if (pPocatek < 0) pPocatek = 0;
                pKonec = pPocatek + pDelka;
                timer1.IsEnabled = false;
                oVlna.mSekundyVlnyZac = pPocatek;
                oVlna.mSekundyVlnyKon = pKonec;
                oVlna.NastavDelkuVlny(pDelka);
                KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyZac + oVlna.DelkaVlnyMS, false);
                mSekundyKonec = oVlna.mSekundyVlnyZac + pDelka;
                timer1.IsEnabled = true;
            }
            catch
            {

            }
        }

        //obsluha klikani na image...////////////////////////////////////////////////////////////////////////////////////////

        bool posouvatL = false;
        bool posouvatP = false;
        /// <summary>
        /// info zda je pozadovano pri editaci vyberu ihned ukladat casy
        /// </summary>
        bool ukladatCasy = false;


        //posun prehravaneho zvuku na danou pozici
        private void myImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Point position;
                if (sender.GetType() == grid1.GetType())
                {
                    position = e.GetPosition((Grid)sender);
                }
                else
                {
                    position = e.GetPosition((Image)sender);
                    return;
                }
                double pX = position.X;
                double pY = position.Y;

                if (e.LeftButton == MouseButtonState.Pressed)
                {

                    //////////////if (leftCtrl || leftShift)
                    {
                        MyParagraph pp = myDataSource.VratOdstavec(nastaveniAplikace.RichTag);
                        if (pp != null)
                        {
                            if (Math.Abs(pX - rectangle2.Margin.Left) < 7)
                            {
                                oVlna.MouseLeftDown = true;
                                if (pp.begin == oVlna.KurzorVyberPocatekMS && pp.end == oVlna.KurzorVyberKonecMS)
                                {
                                    ukladatCasy = true;
                                }
                            }
                            if (Math.Abs(pX - (rectangle2.Margin.Left + rectangle2.Width)) < 7)
                            {
                                oVlna.MouseLeftDown = true;
                                if (pp.begin == oVlna.KurzorVyberPocatekMS && pp.end == oVlna.KurzorVyberKonecMS)
                                {
                                    ukladatCasy = true;
                                }
                            }
                        }
                    }



                    long celkMilisekundy = (long)(oVlna.mSekundyVlnyZac + pX / myImage.ActualWidth * (oVlna.DelkaVlnyMS));

                    if ((!posouvatL && !posouvatP) || !oVlna.MouseLeftDown)
                    {

                        oVlna.KurzorPoziceMS = celkMilisekundy;


                        NastavPoziciKurzoru(celkMilisekundy, true, true);
                        if (nastaveniAplikace.SetupSkocitZastavit)
                        {

                            //mediaElement1.Pause();
                            if (jeVideo) meVideo.Pause();
                            Playing = false;
                            prehratVyber = false;

                        }

                        ////////////if (leftCtrl || leftShift)
                        {
                            oVlna.MouseLeftDown = true;

                            //oVlna.KurzorVyberPocatekMS = celkMilisekundy;
                            //oVlna.KurzorVyberKonecMS = celkMilisekundy;
                            //rectangle2.Margin = new Thickness(pX, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                            //rectangle2.Width = 2;
                            //rectangle2.Visibility = Visibility.Visible;
                            //ZobrazInformaceVyberu();
                        }

                        //upravi cas pocatku pri dvjkliku
                        if (e.ClickCount == 2)
                        {
                            //UpravCasZobraz(nastaveniAplikace.RichTag, oVlna.KurzorVyberPocatekMS, -2);
                        }
                    }
                }
                else
                {
                    if (leftCtrl)
                    {


                    }
                }

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        private void myImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {

                leftShift = Keyboard.IsKeyDown(Key.LeftShift);
                leftCtrl = Keyboard.IsKeyDown(Key.LeftCtrl);
                Point position;
                if (sender.GetType() == grid1.GetType())
                {
                    position = e.GetPosition((Grid)sender);
                }
                else
                {
                    position = e.GetPosition((Image)sender);
                }
                double pX = position.X;
                double pY = position.Y;

                if (e.LeftButton == MouseButtonState.Released && oVlna.MouseLeftDown)
                {
                    long celkMilisekundy = (long)(oVlna.mSekundyVlnyZac + pX / myImage.ActualWidth * (oVlna.DelkaVlnyMS));

                    if (!posouvatL)
                    {
                        //oVlna.KurzorVyberKonecMS = celkMilisekundy;
                    }

                    //otoceni vyberu
                    if (oVlna.KurzorVyberKonecMS < oVlna.KurzorVyberPocatekMS)
                    {
                        long pom = oVlna.KurzorVyberKonecMS;
                        oVlna.KurzorVyberKonecMS = oVlna.KurzorVyberPocatekMS;
                        //oVlna.KurzorVyberPocatekMS = celkMilisekundy;
                        oVlna.KurzorVyberPocatekMS = pom;
                    }


                    if (ukladatCasy)
                    {
                        //MyTag pPredchoziTag = new MyTag(nastaveniAplikace.RichTag.tKapitola, nastaveniAplikace.RichTag.tSekce, nastaveniAplikace.RichTag.tOdstavec - 1);
                        MyTag pPredchoziTag = myDataSource.VratOdstavecPredchoziTag(nastaveniAplikace.RichTag);
                        MyParagraph pPredchozi = myDataSource.VratOdstavec(pPredchoziTag);
                        MyParagraph pAktualni = myDataSource.VratOdstavec(nastaveniAplikace.RichTag);
                        //MyTag pNasledujiciTag = new MyTag(nastaveniAplikace.RichTag.tKapitola, nastaveniAplikace.RichTag.tSekce, nastaveniAplikace.RichTag.tOdstavec + 1);
                        MyTag pNasledujiciTag = myDataSource.VratOdstavecNasledujiciTag(nastaveniAplikace.RichTag);
                        MyParagraph pNasledujici = myDataSource.VratOdstavec(pNasledujiciTag);

                        int sekce = nastaveniAplikace.RichTag.tSekce;
                        int sekcepred = pPredchoziTag.tSekce;
                        int sekceza = pNasledujiciTag.tSekce;

                        if (pAktualni != null)
                        {
                            if (posouvatL && pPredchozi != null && (!leftShift || sekcepred != sekce))
                            {
                                bool rozhrani = false;
                                if (sekcepred != sekce) //rozhrani sekci
                                {
                                    rozhrani = true;
                                    leftShift = false;
                                }
                                //upraveni predchoziho elementu
                                if ((pPredchozi.end == pAktualni.begin && !rozhrani) || pPredchozi.end > oVlna.KurzorVyberPocatekMS)
                                {
                                    UpravCasZobraz(pPredchoziTag, -2, oVlna.KurzorVyberPocatekMS, !leftShift);
                                }
                            }
                            if (posouvatP && pNasledujici != null && (!leftShift || sekceza != sekce))
                            {
                                bool rozhrani = false;
                                if (sekceza != sekce)//rozhrani sekci
                                {
                                    rozhrani = true;
                                    leftShift = false;
                                }

                                //upraveni nasl. elementu
                                if ((pNasledujici.begin == pAktualni.end && !rozhrani) || pNasledujici.begin < oVlna.KurzorVyberKonecMS)
                                {
                                    UpravCasZobraz(pNasledujiciTag, oVlna.KurzorVyberKonecMS, -2, !leftShift);
                                }
                            }
                            //upraveni aktualniho elementu
                            {
                                long pNovyPocatek = oVlna.KurzorVyberPocatekMS;
                                long pNovyKonec = oVlna.KurzorVyberKonecMS;
                                if (leftShift)
                                {
                                    if (!posouvatL) pNovyPocatek = -2;
                                    if (!posouvatP) pNovyKonec = -2;
                                }

                                bool aStav = UpravCasZobraz(nastaveniAplikace.RichTag, pNovyPocatek, pNovyKonec, !leftShift);
                                UpdateXMLData(false, true, false, false, true);
                                ZobrazInformaceElementu(nastaveniAplikace.RichTag);

                            }

                        }
                    }


                    oVlna.MouseLeftDown = false;

                    ZobrazInformaceVyberu();
                    //upravi cas pocatku
                    if (e.ClickCount == 2)
                    {
                        //UpravCasZobraz(nastaveniAplikace.RichTag, -2, oVlna.KurzorVyberKonec);

                    }
                }
                //posouvatL = false;
                //posouvatP = false;
                ukladatCasy = false;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        private void myImage_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                Point position;
                if (sender.GetType() == grid1.GetType())
                {
                    position = e.GetPosition((Grid)sender);
                }
                else
                {
                    position = e.GetPosition((Image)sender);
                }
                double pX = position.X;

                if (oVlna.MouseLeftDown && e.LeftButton == MouseButtonState.Pressed)
                {
                    long celkMilisekundy = (long)(oVlna.mSekundyVlnyZac + pX / myImage.ActualWidth * (oVlna.DelkaVlnyMS));

                    MyTag ptNasledujici = null;
                    MyTag ptPredchozi = null;
                    MyParagraph pPredchozi = null;
                    MyParagraph pAktualni = null;
                    MyParagraph pNasledujici = null;
                    if (nastaveniAplikace.RichTag.JeOdstavec)
                    {
                        ptPredchozi = myDataSource.VratOdstavecPredchoziTag(nastaveniAplikace.RichTag);
                        ptNasledujici = myDataSource.VratOdstavecNasledujiciTag(nastaveniAplikace.RichTag);
                        pPredchozi = myDataSource.VratOdstavec(ptPredchozi);
                        pAktualni = myDataSource.VratOdstavec(nastaveniAplikace.RichTag);
                        pNasledujici = myDataSource.VratOdstavec(ptNasledujici);
                    }
                    if (!posouvatL && !posouvatP && oVlna.KurzorPoziceMS != oVlna.KurzorVyberPocatekMS)
                    {
                        oVlna.KurzorVyberPocatekMS = oVlna.KurzorPoziceMS;
                    }

                    if (posouvatL)
                    {
                        if (pPredchozi != null && pAktualni != null)
                        {
                            double pXLevo = (pPredchozi.end - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / oVlna.DelkaVlnyMS;
                            if (Math.Abs(pX - pXLevo) < 7)
                            {
                                celkMilisekundy = pPredchozi.end;
                                //long celkMilisekundy = (long)(oVlna.mSekundyVlnyZac + pX / myImage.ActualWidth * (oVlna.DelkaVlnyMS));

                            }
                        }

                        oVlna.KurzorVyberPocatekMS = celkMilisekundy;
                    }
                    else
                    {
                        if (pNasledujici != null && pAktualni != null)
                        {
                            double pXPravo = (pNasledujici.begin - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / oVlna.DelkaVlnyMS;
                            if (Math.Abs(pX - pXPravo) < 7)
                            {
                                celkMilisekundy = pNasledujici.begin;
                            }
                        }


                        oVlna.KurzorVyberKonecMS = celkMilisekundy;
                    }



                    //zarazky predchozich a nasledujicich odstavcu
                    if (posouvatL && ukladatCasy)
                    {

                        if (oVlna.KurzorVyberPocatekMS >= oVlna.KurzorVyberKonecMS)
                        {
                            oVlna.KurzorVyberPocatekMS = oVlna.KurzorVyberKonecMS - 1;
                        }

                        if (nastaveniAplikace.RichTag.JeOdstavec)
                        {
                            if (pPredchozi != null)
                            {
                                if (pPredchozi.begin > oVlna.KurzorVyberPocatekMS)
                                {
                                    oVlna.KurzorVyberPocatekMS = pPredchozi.begin;
                                }
                            }

                        }

                        pX = (oVlna.KurzorVyberPocatekMS - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / (oVlna.DelkaVlnyMS);
                    }


                    if (posouvatP && ukladatCasy)
                    {
                        if (oVlna.KurzorVyberKonecMS <= oVlna.KurzorVyberPocatekMS)
                        {
                            oVlna.KurzorVyberKonecMS = oVlna.KurzorVyberPocatekMS + 1;
                        }

                        if (nastaveniAplikace.RichTag.JeOdstavec)
                        {
                            if (ptNasledujici != null)
                            {
                                if (pNasledujici.end < oVlna.KurzorVyberKonecMS)
                                {
                                    if (pNasledujici.end >= 0)
                                    {
                                        oVlna.KurzorVyberKonecMS = pNasledujici.end;
                                    }
                                }
                            }
                        }
                        pX = (oVlna.KurzorVyberKonecMS - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / (oVlna.DelkaVlnyMS);

                    }

                    double pSirka = pX - (oVlna.KurzorVyberPocatekMS - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / oVlna.DelkaVlnyMS;
                    if (posouvatL)
                    {
                        pSirka = -((oVlna.KurzorVyberKonecMS - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / oVlna.DelkaVlnyMS - pX);

                    }


                    if (pSirka < 0)
                    {
                        rectangle2.Margin = new Thickness(pX, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);

                    }
                    else
                    {
                        if (oVlna.KurzorVyberPocatekMS <= oVlna.KurzorVyberKonecMS)
                        {
                            pX = (oVlna.KurzorVyberPocatekMS - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / oVlna.DelkaVlnyMS;
                        }
                        else
                        {
                            pX = (oVlna.KurzorVyberPocatekMS - oVlna.mSekundyVlnyZac) * myImage.ActualWidth / oVlna.DelkaVlnyMS - pSirka;
                        }
                        rectangle2.Margin = new Thickness(pX, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);

                    }
                    rectangle2.Width = Math.Abs(pSirka);
                    rectangle2.Visibility = Visibility.Visible;
                    ZobrazInformaceVyberu();
                }
                else ////////if (leftCtrl || leftShift)
                {
                    if ((Math.Abs(pX - rectangle2.Margin.Left) < 7 || Math.Abs(pX - (rectangle2.Margin.Left + rectangle2.Width)) < 7) && oVlna.KurzorVyberKonecMS - oVlna.KurzorVyberPocatekMS > 1)
                    {

                        /////if (leftCtrl)
                        {
                            rectangle2.Cursor = Cursors.ScrollWE;
                            grid1.Cursor = Cursors.ScrollWE;
                        }


                        if (Math.Abs(pX - rectangle2.Margin.Left) < 7)
                        {
                            posouvatL = true;
                            posouvatP = false;
                        }
                        else if (Math.Abs(pX - (rectangle2.Margin.Left + rectangle2.Width)) < 7)
                        {
                            posouvatP = true;
                            posouvatL = false;
                        }

                        if (leftShift)
                        {
                            if (posouvatL)
                            {
                                rectangle2.Cursor = Cursors.ScrollW;
                                grid1.Cursor = Cursors.ScrollW;
                            }
                            if (posouvatP)
                            {
                                rectangle2.Cursor = Cursors.ScrollE;
                                grid1.Cursor = Cursors.ScrollE;
                            }
                        }
                    }
                    else
                    {
                        rectangle2.Cursor = Cursors.IBeam;
                        grid1.Cursor = Cursors.IBeam;
                        posouvatL = false;
                        posouvatP = false;

                    }

                }
                //////else
                //////{
                //////    rectangle2.Cursor = Cursors.Arrow;
                //////    grid1.Cursor = Cursors.Arrow;
                //////}
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        #region menu vlna events
        //obsluha kontextoveho menu image vlny
        private void menuItemVlna1_prirad_zacatek_Click(object sender, RoutedEventArgs e)
        {
            UpravCasZobraz(nastaveniAplikace.RichTag, oVlna.KurzorPoziceMS, -2);

            UpdateXMLData();
            ZobrazInformaceElementu(nastaveniAplikace.RichTag);
            oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(nastaveniAplikace.RichTag);
            oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(nastaveniAplikace.RichTag);
            KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, oVlna.KurzorPoziceMS);

        }
        private void menuItemVlna1_prirad_konec_Click(object sender, RoutedEventArgs e)
        {
            UpravCasZobraz(nastaveniAplikace.RichTag, -2, oVlna.KurzorPoziceMS);
            UpdateXMLData();
            ZobrazInformaceElementu(nastaveniAplikace.RichTag);

            oVlna.KurzorVyberPocatekMS = myDataSource.VratCasElementuPocatek(nastaveniAplikace.RichTag);
            oVlna.KurzorVyberKonecMS = myDataSource.VratCasElementuKonec(nastaveniAplikace.RichTag);
            KresliVyber(oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, oVlna.KurzorPoziceMS);
        }

        private void menuItemVlna1_prirad_vyber_Click(object sender, RoutedEventArgs e)
        {
            UpravCasZobraz(nastaveniAplikace.RichTag, oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS);
            UpdateXMLData();
            ZobrazInformaceElementu(nastaveniAplikace.RichTag);
        }

        private void menuItemVlna1_prirad_casovou_znacku_Click(object sender, RoutedEventArgs e)
        {
            ///FlowDocument flowDoc = ((RichTextBox)nastaveniAplikace.RichTag.tSender).Document;
            ///TextPointer kon = ((RichTextBox)(nastaveniAplikace.RichTag.tSender)).Selection.Start;
            ///TextPointer zac = flowDoc.ContentStart;
            ///TextRange trDelka = new TextRange(zac, kon);
            /////string trDelka = ((TextBox)nastaveniAplikace.RichTag.tSender).Text.Remove(((TextBox)nastaveniAplikace.RichTag.tSender).SelectionStart);

            ///int pPoziceKurzoru = kon.GetOffsetToPosition(zac);
            int pPoziceKurzoru = ((TextBox)nastaveniAplikace.RichTag.tSender).SelectionStart;

            ////MyCasovaZnacka pCZ = new MyCasovaZnacka(oVlna.KurzorPoziceMS, trDelka.Length - 1, trDelka.Length);
            MyCasovaZnacka pCZ = new MyCasovaZnacka(oVlna.KurzorPoziceMS, pPoziceKurzoru - 1, pPoziceKurzoru);

            MyParagraph pOdstavec = myDataSource.VratOdstavec(nastaveniAplikace.RichTag);
            pOdstavec.PridejCasovouZnacku(pCZ);

            nastaveniAplikace.CasoveZnacky = myDataSource.VratOdstavec(nastaveniAplikace.RichTag).VratCasoveZnackyTextu;

            //UpravCasZobraz(nastaveniAplikace.RichTag, oVlna.KurzorVyberPocatek, oVlna.KurzorVyberKonec);

            ///FlowDocument pFd = VytvorFlowDocumentOdstavce(myDataSource.VratOdstavec(nastaveniAplikace.RichTag));



            ///((RichTextBox)nastaveniAplikace.RichTag.tSender).Document = pFd;
            ((TextBox)nastaveniAplikace.RichTag.tSender).Text = myDataSource.VratOdstavec(nastaveniAplikace.RichTag).Text;
            //vraceni kurzoru do spravne pozice


            UpdateXMLData();
            ZobrazInformaceElementu(nastaveniAplikace.RichTag);

            try
            {
                ///((RichTextBox)nastaveniAplikace.RichTag.tSender).Selection.Select(pFd.ContentStart.GetPositionAtOffset(Math.Abs(pPoziceKurzoru), LogicalDirection.Forward), pFd.ContentStart.GetPositionAtOffset(Math.Abs(pPoziceKurzoru), LogicalDirection.Forward));
                ((TextBox)nastaveniAplikace.RichTag.tSender).Select(pPoziceKurzoru, 0);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        private void menuItemVlna1_automaticke_rozpoznavani_useku_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SpustRozpoznavaniVybranehoElementu(nastaveniAplikace.RichTag, oVlna.KurzorVyberPocatekMS, oVlna.KurzorVyberKonecMS, false);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }
        #endregion


        //co se stane kdyz je zmenena velikost image
        private void grid1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, true); //prekresleni casove osy
        }


        //osetreni ulozeni pri ukonceni aplikace
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            try
            {

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

                        if (oknoNapovedy!=null && !oknoNapovedy.IsLoaded)
                        {
                            oknoNapovedy.Close();
                        }


                    }
                    else if (mbr == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;

                    }


                }
                else if (oknoNapovedy!=null && oknoNapovedy.IsLoaded)
                {
                    oknoNapovedy.Close();
                }


                if (!e.Cancel)
                {
                    if (MWP != null)
                    {
                        MWP.Dispose();
                        MWP = null;
                    }
                    if (MWR != null)
                    {
                        MWR.Dispose();
                        MWR = null;
                    }

                    if (oPrepisovac != null)
                    {
                        oPrepisovac.Dispose();
                        oPrepisovac = null;
                    }
                    if (oHlasoveOvladani != null)
                    {
                        oHlasoveOvladani.Dispose();
                        oHlasoveOvladani = null;
                    }
                    if (oWav != null)
                    {
                        oWav.Dispose();
                    }

                    if (mWL != null) mWL.Close();

                    //ulozeni databaze mluvcich - i externi databaze
                    if (myDatabazeMluvcich != null)
                    {
                        if (myDatabazeMluvcich.JmenoSouboru != null && myDatabazeMluvcich.JmenoSouboru != nastaveniAplikace.CestaDatabazeMluvcich)
                        {
                            myDatabazeMluvcich.Serializovat(myDatabazeMluvcich.JmenoSouboru, myDatabazeMluvcich);
                        }
                        else
                        {
                            if (new FileInfo(nastaveniAplikace.CestaDatabazeMluvcich).Exists)
                            {
                                myDatabazeMluvcich.Serializovat(nastaveniAplikace.CestaDatabazeMluvcich, myDatabazeMluvcich);
                            }
                            else
                            {
                                myDatabazeMluvcich.Serializovat(nastaveniAplikace.absolutniCestaEXEprogramu + "\\data\\databazemluvcich.xml", myDatabazeMluvcich);
                            }
                        }

                    }
                    //ulozeni fonetickeho uzivatelskeho slovniku
                    if (bSlovnikFonetickehoDoplneni != null)
                    {
                        bSlovnikFonetickehoDoplneni.UlozitSlovnik(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_SLOVNIK_FONETIKA_UZIVATELSKY);
                    }

                    //pokus o ulozeni nastaveni
                    if (nastaveniAplikace != null)
                    {
                        if (this.WindowState == WindowState.Normal)
                        {
                            //nastaveni posledni zname souradnice okna a velikosti okna
                            nastaveniAplikace.OknoPozice = new Point(this.Left, this.Top);
                            nastaveniAplikace.OknoVelikost = new Size(this.Width, this.Height);
                        }
                        if (this.WindowState != WindowState.Minimized)
                        {
                            nastaveniAplikace.OknoStav = this.WindowState;
                        }

                        nastaveniAplikace.Serializovat(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.KONFIGURACNI_SOUBOR, nastaveniAplikace);
                    }



                }


                foreach (string f in Directory.GetFiles(MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU))
                {
                    File.Delete(f);
                }

                Directory.Delete(MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                throw (ex);
            }
        }



        //zavre okno s videem a video
        private void btCloseVideo_Click(object sender, RoutedEventArgs e)
        {
            meVideo.Close();
            meVideo.Source = null;
            jeVideo = false;
            gListVideo.ColumnDefinitions[1].Width = new GridLength(1);



        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, oVlna.PouzeCasovaOsa);
            popup.IsOpen = false;

        }

        private void button11_Click_1(object sender, RoutedEventArgs e)
        {
            if (nastaveniAplikace.BylFocus || nastaveniAplikace.RichFocus)
            {
                OdstranSekci(nastaveniAplikace.RichTag.tKapitola, nastaveniAplikace.RichTag.tSekce);


            }
        }

        #region menu uprava
        private void MUpravy_Nova_Kapitola_Click(object sender, RoutedEventArgs e)
        {
            MyTag mT = nastaveniAplikace.RichTag;
            int pIndex = -1;
            if (mT != null) pIndex = mT.tKapitola;
            PridejKapitolu(pIndex + 1, "");
        }

        private void MUpravy_Nova_Sekce_Click(object sender, RoutedEventArgs e)
        {
            if (nastaveniAplikace.RichFocus)
            {
                PridejSekci(nastaveniAplikace.RichTag.tKapitola, "", nastaveniAplikace.RichTag.tSekce, -1, -1, -1);
            }
        }

        private void MUpravy_Smazat_Polozku_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (nastaveniAplikace.RichFocus)
                {

                    MyTag mT = nastaveniAplikace.RichTag;
                    int index = spSeznam.Children.IndexOf((Grid)((TextBox)(mT.tSender)).Parent);
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
                        //OdstranKapitolu(mT.tKapitola);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        #endregion


        //nacteni celeho slovniku z souboru do hash tabulky
        private void LoadVocabulary()
        {
            try
            {
                MyTextBox.Vocabulary = new HashSet<string>();
                StreamReader reader = new StreamReader(nastaveniAplikace.absolutniCestaEXEprogramu+ MyKONST.CESTA_SLOVNIK_SPELLCHECK);
                while(reader.Peek() >=0)
                {
                    foreach( string s in reader.ReadLine().Split(' '))
                    {
                        MyTextBox.Vocabulary.Add(s.ToLower());
                    }
                }

                reader.Close();

                //refresh uz vykreslenych textboxu
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
                

            }catch(Exception)
            {
            
            }
        }

        
        //pokusi se zamerit textbox pri spousteni aplikace
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //inicializuje (asynchronni) nacitani slovniku
            Thread t = new Thread(LoadVocabulary);
            t.Start();
            HidInit();
            try
            {
                UpdateXMLData();
                ///((RichTextBox)((Grid)spSeznam.Children[2]).Children[0]).Focus();
                ((TextBox)((Grid)spSeznam.Children[2]).Children[0]).Focus();
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }


            string temppath = System.IO.Path.GetTempPath()+System.IO.Path.GetRandomFileName();
            Directory.CreateDirectory(temppath);
            MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU = temppath+"\\";
        }

        private void btDiktat_Click(object sender, RoutedEventArgs e)
        {
            if (MyKONST.VERZE == MyEnumVerze.Externi) return;
            if (!btDiktat.IsEnabled) return;
            if (recording)
            {
                if (MWR != null) MWR.Dispose();
                MWR = null;
                recording = false;
                //MessageBox.Show("Zastaveno nahravani");
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

        /// <summary>
        /// prectena data z asynchronniho read vyvolaji udalost a toto je jeji osetreni
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void oPrepisovac_HaveDataPrectena(object sender, EventArgs e)
        {
            try
            {
                MyEventArgsPrectenaData e2 = (MyEventArgsPrectenaData)e;
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<MyEventArgsPrectenaData>(ZobrazZpravuRozpoznavace), e2);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }


        /// <summary>
        /// zpracuje FIX_TEXTBLOCK a prevede ho na text a casove znacky jednotlivych slov
        /// </summary>
        /// <param name="aText"></param>
        /// <param name="aCasoveZnacky"></param>
        /// <returns></returns>
        public bool ZpracujRozpoznanyText(long aAbsolutniCasPocatku, int aPredchoziDelkaTextu, ref string aText, ref List<MyCasovaZnacka> aCasoveZnacky)
        {
            try
            {
                string pText = aText;
                aText = "";
                pText = pText.Replace("FIX_TEXTBLOCK", ""); //odstraneni fixblocku
                string[] pFormat = { "[", "]" };
                string[] pPole = pText.Split(pFormat, StringSplitOptions.None);
                long pPocatekMS = -1;
                long pKonecMS = -1;
                aCasoveZnacky = new List<MyCasovaZnacka>();
                if (pPole != null)
                {
                    for (int i = 1; i < pPole.Length - 1; i++)
                    {
                        string[] pPoleRozdeleneHvezdickou = pPole[i].Split('*');
                        if (pPoleRozdeleneHvezdickou != null && pPoleRozdeleneHvezdickou.Length == 2) //jedna se o casove znacky mezer
                        {
                            if (pPocatekMS == -1 && pKonecMS == -1)
                            {
                                pPocatekMS = long.Parse(pPoleRozdeleneHvezdickou[1]) * 10;
                                aCasoveZnacky.Add(new MyCasovaZnacka(aAbsolutniCasPocatku + pPocatekMS, aPredchoziDelkaTextu + aText.Length - 1, aPredchoziDelkaTextu + aText.Length));
                            }
                            else if (pPocatekMS >= 0 && pKonecMS == -1)
                            {

                                pKonecMS = long.Parse(pPoleRozdeleneHvezdickou[0]) * 10;
                                aCasoveZnacky.Add(new MyCasovaZnacka(aAbsolutniCasPocatku + pKonecMS, aPredchoziDelkaTextu + aText.Length - 2, aPredchoziDelkaTextu + aText.Length - 1));

                                pPocatekMS = long.Parse(pPoleRozdeleneHvezdickou[1]) * 10;
                                aCasoveZnacky.Add(new MyCasovaZnacka(aAbsolutniCasPocatku + pPocatekMS, aPredchoziDelkaTextu + aText.Length - 1, aPredchoziDelkaTextu + aText.Length));


                                pKonecMS = -1;
                            }
                            //aCasoveZnacky.Add(new MyCasovaZnacka(aAbsolutniCasPocatku + pPocatekMS, aText.Length - 1));
                            //aCasoveZnacky.Add(new MyCasovaZnacka(aAbsolutniCasPocatku + pKonecMS, aText.Length ));
                        }
                        else if (pPoleRozdeleneHvezdickou.Length == 1) //jedna se o text
                        {
                            aText += pPoleRozdeleneHvezdickou[0] + " ";
                        }
                    }
                    if (aCasoveZnacky.Count > 0)
                    {
                        aCasoveZnacky.RemoveAt(aCasoveZnacky.Count - 1);
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }
        }


        /// <summary>
        /// zpracuje show nebo fixtextblock a dekoduje makro
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
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

        private void ZobrazZpravuRozpoznavace(MyEventArgsPrectenaData e)
        {
            try
            {

                //label3.Content = e.zprava;
                //tbVideoSoubor.Text = e.zprava;
                //listBox1.Items.Add(e.zprava);
                string[] input = { "\r\n" };
                string[] pStringy = e.zprava.Split(input, StringSplitOptions.RemoveEmptyEntries);

                //prepocitani tagu rozpoznavace,pokud doslo ke zmene struktury dat mazanim nebo pridanim neceho jineho
                if (e.sender != null && e.sender.PrepisovanyElementTag != null)
                {
                    MyTag pT = null;
                    try
                    {
                        pT = (MyTag)((TextBox)e.sender.PrepisovanyElementTag.tSender).Tag;
                    }
                    catch
                    {
                        pT = null;
                    }

                }

                if (pStringy != null && e.sender != null)
                {
                    foreach (string s in pStringy)
                    {
                        if (mWL != null && !s.Contains("DELAY:")) mWL.listBox1.Items.Add(s);
                        if (s.Contains("FIX_TEXTBLOCK"))
                        {
                            List<MyCasovaZnacka> pZnacky = new List<MyCasovaZnacka>();
                            string pText = s;
                            if (e.sender.RozpoznavaniHlasu)
                            {
                                ZpracujRozpoznanyText(0, e.sender.PrepsanyText.Length, ref pText, ref pZnacky);
                            }
                            else
                            {
                                ZpracujRozpoznanyText(e.sender.bufferProPrepsani.PocatekMS, e.sender.PrepsanyText.Length, ref pText, ref pZnacky);
                            }
                            e.sender.PrepsanyText += pText;
                            e.sender.PrepsanyTextCasoveZnacky.AddRange(pZnacky);



                            MyParagraph pP = myDataSource.VratOdstavec(e.sender.PrepisovanyElementTag);
                            if (pP != null)
                            {
                                pP.UlozTextOdstavce(e.sender.PrepsanyText, e.sender.PrepsanyTextCasoveZnacky);
                                ///((RichTextBox)oPrepisovac.PrepisovanyElementTag.tSender).Document = VytvorFlowDocumentOdstavce(pP);
                                ((TextBox)e.sender.PrepisovanyElementTag.tSender).Text = pP.Text;

                                UpdateXMLData();
                                pP.UlozTextOdstavce(e.sender.PrepsanyText, e.sender.PrepsanyTextCasoveZnacky);

                                //pridano kvuli upravam rozpoznaneho textu
                                nastaveniAplikace.CasoveZnacky = pP.VratCasoveZnackyTextu;
                                nastaveniAplikace.CasoveZnackyText = pP.Text;
                            }
                            else
                            {
                                //chyba zapisu do textboxu
                            }
                            if (e.sender.TypRozpoznavani == MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI)
                            {
                                ZpracujPovelHlasovehoRozpoznavace(s);

                            }

                        }
                        else if (s.Contains("SHOW_TEXTBLOCK"))
                        {
                            string[] pFormat = { "#" };
                            string[] pPole = s.Split(pFormat, StringSplitOptions.RemoveEmptyEntries);
                            string pText = "";
                            if (pPole != null && pPole.Length > 1) pText = pPole[1];
                            MyParagraph pP = myDataSource.VratOdstavec(e.sender.PrepisovanyElementTag);
                            if (pP != null)
                            {
                                pP.UlozTextOdstavce(e.sender.PrepsanyText + pText, e.sender.PrepsanyTextCasoveZnacky);
                                //nastaveniAplikace.CasoveZnacky = oPrepisovac.PrepsanyTextCasoveZnacky;
                                //nastaveniAplikace.CasoveZnackyText = oPrepisovac.PrepsanyText + pText;
                                ///((RichTextBox)oPrepisovac.PrepisovanyElementTag.tSender).Document = VytvorFlowDocumentOdstavce(pP);
                                ((TextBox)e.sender.PrepisovanyElementTag.tSender).Text = pP.Text;
                                UpdateXMLData();
                                pP.UlozTextOdstavce(e.sender.PrepsanyText + pText, e.sender.PrepsanyTextCasoveZnacky);

                            }
                            else
                            {
                                //chyba zapisu do textboxu
                            }

                            if (e.sender.TypRozpoznavani == MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI)
                            {
                                ZpracujPovelHlasovehoRozpoznavace(s);

                            }

                        }
                        else if (s.Contains("DELAY:"))
                        {
                            long pZpozdeniMS = 0;
                            string pText = s.Replace("DELAY:", "");
                            try
                            {
                                pZpozdeniMS = long.Parse(pText) * 10;
                            }
                            catch
                            {

                            }
                            bool pZapisovano = false; //info aby nedoslo k ukonceni session
                            if (pZpozdeniMS < 1000)
                            {

                                if (e.sender.RozpoznavaniHlasu)
                                {

                                }
                                else
                                {
                                    if (e.sender.bufferProPrepsani.PocatekMS < e.sender.pomocnaCasPrepsaniMS)
                                    {
                                        long pDelta = e.sender.bufferProPrepsani.KonecMS - e.sender.pomocnaCasPrepsaniMS;
                                        if (pDelta > MyKONST.DELKA_POSILANYCH_DAT_PRI_OFFLINE_ROZPOZNAVANI_MS) pDelta = MyKONST.DELKA_POSILANYCH_DAT_PRI_OFFLINE_ROZPOZNAVANI_MS;

                                        if (e.sender.AsynchronniZapsaniDat(e.sender.bufferProPrepsani.VratDataBufferuByte(e.sender.pomocnaCasPrepsaniMS, pDelta, -1)))
                                        {
                                            e.sender.pomocnaCasPrepsaniMS += pDelta;
                                        }
                                    }
                                }

                            }
                            if (pZpozdeniMS == 0)
                            {
                                if (e.sender.RozpoznavaniHlasu)
                                {
                                    if (!pZapisovano && e.sender.bufferProHlasoveOvladani.DelkaMS == 0 && !recording)
                                    {
                                        e.sender.AsynchronniStop();
                                    }

                                }
                                else
                                {
                                    if (e.sender.bufferProPrepsani.KonecMS <= e.sender.pomocnaCasPrepsaniMS)
                                    {

                                        {
                                            e.sender.AsynchronniStop();
                                        }
                                    }
                                }
                            }

                            if (e.sender.RozpoznavaniHlasu)
                            {
                                if (e.sender.TypRozpoznavani == MyKONST.ROZPOZNAVAC_1_DIKTAT && e.sender.Rozpoznavani)
                                {
                                    pbZpozdeniPrepisu.Value = pZpozdeniMS;
                                }
                            }
                            else
                            {
                                pbZpozdeniPrepisu.Value = (pZpozdeniMS + e.sender.bufferProPrepsani.KonecMS - e.sender.pomocnaCasPrepsaniMS);
                                if (pZpozdeniMS < 1000 && e.sender.pomocnaCasPrepsaniMS < e.sender.bufferProPrepsani.KonecMS)
                                {
                                    pbZpozdeniPrepisu.Value += MyKONST.DELKA_POSILANYCH_DAT_PRI_OFFLINE_ROZPOZNAVANI_MS;
                                }
                            }
                        }
                        else if (s.Contains("END_SESSION"))//konec rozpoznavani
                        {
                            ///timerRozpoznavace.IsEnabled = false;

                            //zpristupneni textboxu pro upravy
                            try
                            {
                                ((TextBox)e.sender.PrepisovanyElementTag.tSender).IsReadOnly = false;
                            }
                            catch
                            {

                            }
                            //if (oPrepisovac != null) oPrepisovac.Rozpoznavani = false;

                            //nastaveni tlacitek pro pristup k diktovani a prepisu
                            if (e.sender.TypRozpoznavani == MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI)
                            {
                                ZmenStavTlacitekRozpoznavace(false, false, true, false);
                            }
                            else
                            {
                                ZmenStavTlacitekRozpoznavace(true, true, false, false);
                            }

                            //MessageBox.Show("Přepis dokončen!");
                            ZobrazStavProgramu("Konec automatického rozpoznávání");

                            if (pSeznamOdstavcuKRozpoznani != null && pSeznamOdstavcuKRozpoznani.Count > 0)
                            {
                                MyTag pTag = pSeznamOdstavcuKRozpoznani[0];
                                pSeznamOdstavcuKRozpoznani.RemoveAt(0);
                                SpustRozpoznavaniVybranehoElementu(pTag, -1, -1, true);
                            }


                            break;
                        }
                        else if (s.Contains("NEW_SESSION"))
                        {
                            string pText = s.Replace("NEW_SESSION", "");
                            if (this.pPozadovanyStavRozpoznavace == 2)
                            {

                                ZobrazStavProgramu("Pokus o makro...");
                            }
                            //else
                            {
                                if (e.sender.RozpoznavaniHlasu)
                                {
                                    recording = true;
                                    if (MWR == null) InicializaceAudioRecorderu();
                                    pbZpozdeniPrepisu.Maximum = 60000;
                                    ZobrazStavProgramu("Můžete začít diktovat/ovládat, probíhá nahrávání z mikrofonu...");
                                }
                                else
                                {

                                    pbZpozdeniPrepisu.Maximum = e.sender.bufferProPrepsani.DelkaMS;
                                    if (e.sender.AsynchronniZapsaniDat(e.sender.bufferProPrepsani.VratDataBufferuByte(e.sender.bufferProPrepsani.PocatekMS, MyKONST.DELKA_POSILANYCH_DAT_PRI_OFFLINE_ROZPOZNAVANI_MS, -1)))
                                    {
                                        e.sender.pomocnaCasPrepsaniMS = e.sender.bufferProPrepsani.PocatekMS + MyKONST.DELKA_POSILANYCH_DAT_PRI_OFFLINE_ROZPOZNAVANI_MS;
                                        //oPrepisovac.bufferProPrepsani.SmazDataZBufferuZeZacatku(5000);
                                    }
                                    ZobrazStavProgramu("Probíhá automatické rozpoznávání vybrané části audio souboru...");
                                }
                            }

                        }
                        else if (s.Contains("END_PROCESS"))
                        {
                            MessageBox.Show("Chyba inicializace rozpoznávače - Ověřte nastavení a licenci", "Varování!", MessageBoxButton.OK, MessageBoxImage.Error);
                            if (recording)
                            {
                                recording = false;
                                if (MWR != null) MWR.Dispose();
                                MWR = null;
                            }
                            ZmenStavTlacitekRozpoznavace(true, true, true, false);
                            ZobrazStavProgramu("Inicializace rozpoznávače se nezdařila");
                        }
                        else if (s.Contains("INITIALIZING"))
                        {


                        }
                        else if (s.Contains("INITIALIZED")) //zatim pouze pro hlasove ovladani
                        {
                            if (pPozadovanyStavRozpoznavace == MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI)   //zapise makro
                            {
                                pSeznamNahranychMaker = MyMakro.DeserializovatSeznamMaker(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_MAKRA_SOUBOR);
                                e.sender.ZapisMakra(pSeznamNahranychMaker);
                            }



                            if (e.sender.Start(pPozadovanyStavRozpoznavace) == 0)
                            {
                                if (timerRozpoznavace.IsEnabled == false) timerRozpoznavace.IsEnabled = true;
                                if (recording && pPozadovanyStavRozpoznavace > 0)
                                {
                                    recording = false;
                                    if (MWR != null) MWR.Dispose();
                                    MWR = null;
                                }

                                if (e.sender.bufferProHlasoveOvladani == null) e.sender.bufferProHlasoveOvladani = new MyBuffer16(60000);
                                e.sender.bufferProHlasoveOvladani.SmazBuffer();

                                e.sender.PrepsanyText = "";
                                e.sender.PrepsanyTextCasoveZnacky = new List<MyCasovaZnacka>();
                                //oPrepisovac.PrepisovanyElementTag = nastaveniAplikace.RichTag;

                                //nastaveni indexu dat na 0
                                this.pVyrovnavaciPametIndexVrcholu = 0;

                                //pozadavek na spusteni nahravani audio souboru
                                if (pPozadovanyStavRozpoznavace > 0)
                                {
                                    recording = true;
                                }



                            }


                        }

                    }

                }

                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

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
                this.Dispatcher.Invoke(new Action<string>(ZobrazStavProgramu),  aZprava );
                return;
            }
            tbStavProgramu.Text = aZprava;
        }

        /// <summary>
        /// zobrazi rozpoznany prikaz hlasoveho povelu
        /// </summary>
        /// <param name="aZprava"></param>
        private void ZobrazRozpoznanyPrikaz(string aZprava)
        {
            tbRozpoznanyPrikaz.Text = aZprava;
            pDelkaZobrazeniHlasovehoPovelu = 0;
        }

        private void button10_Click(object sender, RoutedEventArgs e)
        {
            if (MyKONST.VERZE == MyEnumVerze.Externi) return;
            if (oPrepisovac != null && oPrepisovac.Rozpoznavani)
            {
                if (MessageBox.Show("Opravdu chcete přerušit právě probíhající přepis?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (oPrepisovac.StopHned() == 0)
                    {
                        if (pSeznamOdstavcuKRozpoznani != null) pSeznamOdstavcuKRozpoznani.Clear();
                        //ZmenStavTlacitekRozpoznavace(true, false, false, false);
                    }
                }
            }
            else
            {
                SpustRozpoznavaniVybranehoElementu(nastaveniAplikace.RichTag, -1, -1, false);
                //{
                //    ZmenStavTlacitekRozpoznavace(true, false, false, true);
                //}
            }
        }


        /// <summary>
        /// spusti rozpoznavani hlasovych povelu-respektive prepis hlasu-Diktat
        /// </summary>
        /// <returns></returns>
        private bool SpustRozpoznavaniHlasu()
        {
            try
            {
                if (oPrepisovac != null && (oPrepisovac.Rozpoznavani || oPrepisovac.Ukoncovani))
                {
                    //ukonceni predchoziho prepisu
                    if (MessageBox.Show("Dochází k automatickému rozpoznávání jiného úkolu. Nejprve musíte zastavit předchozí rozpoznávání.\nChcete ho nyní přerušit?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (oPrepisovac.StopHned() == 0)
                        {

                        }
                    }
                    return false;
                }

                pPozadovanyStavRozpoznavace = MyKONST.ROZPOZNAVAC_1_DIKTAT;

                //vytvoreni instance prepisovace, pokud neexistuje
                if (oPrepisovac == null)
                {
                    oPrepisovac = new MyPrepisovac(nastaveniAplikace.AbsolutniAdresarRozpoznavace, nastaveniAplikace.rozpoznavac.Mluvci, nastaveniAplikace.rozpoznavac.JazykovyModel, nastaveniAplikace.rozpoznavac.PrepisovaciPravidla, nastaveniAplikace.rozpoznavac.LicencniServer, nastaveniAplikace.rozpoznavac.LicencniSoubor, nastaveniAplikace.rozpoznavac.DelkaInternihoBufferuPrepisovace, nastaveniAplikace.rozpoznavac.KvalitaRozpoznavaniDiktat, new EventHandler(oPrepisovac_HaveDataPrectena));
                }
                string pMluvci = nastaveniAplikace.rozpoznavac.Mluvci;
                string pJazykovyModel = nastaveniAplikace.rozpoznavac.JazykovyModel;
                if (nastaveniAplikace.diktatMluvci != null)
                {
                    if (nastaveniAplikace.diktatMluvci.RozpoznavacMluvci != null) pMluvci = nastaveniAplikace.rozpoznavac.MluvciRelativniAdresar + "/" + nastaveniAplikace.diktatMluvci.RozpoznavacMluvci;
                    if (nastaveniAplikace.diktatMluvci.RozpoznavacJazykovyModel != null) pJazykovyModel = nastaveniAplikace.rozpoznavac.JazykovyModelRelativniAdresar + "/" + nastaveniAplikace.diktatMluvci.RozpoznavacJazykovyModel;
                }
                oPrepisovac.InicializaceRozpoznavace(nastaveniAplikace.AbsolutniAdresarRozpoznavace, nastaveniAplikace.rozpoznavac.LicencniSoubor, pMluvci, pJazykovyModel, nastaveniAplikace.rozpoznavac.PrepisovaciPravidla, nastaveniAplikace.rozpoznavac.DelkaInternihoBufferuPrepisovace.ToString(), nastaveniAplikace.rozpoznavac.KvalitaRozpoznavaniDiktat.ToString());
                if (timerRozpoznavace != null && timerRozpoznavace.IsEnabled == false)
                {
                    InitializeTimerRozpoznavace(MyKONST.PERIODA_TIMERU_ROZPOZNAVACE_MS);
                }
                oPrepisovac.PrepisovanyElementTag = nastaveniAplikace.RichTag;
                //inicializace prepisovace a hlidaciho timeru

                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }


        }

        /// <summary>
        /// spusti rozpoznavani vybraneho elementu... pokud je element sekce a kapitola, zkusi rozpoznat jednotlive odstavce - DODELAT!!!!!!!!
        /// </summary>
        /// <param name="aTag"></param>
        /// <param name="aPocatekMS"></param>
        /// <param name="aKonecMS"></param>
        /// <returns></returns>
        private bool SpustRozpoznavaniVybranehoElementu(MyTag aTag, long aPocatekMS, long aKonecMS, bool aIgnorovatTextOdstavce)
        {
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

            long pPocatekMS;
            long pKonceMS;
            long pDelkaMS;



            if (aPocatekMS >= 0 && aKonecMS >= 0)
            {
                pPocatekMS = aPocatekMS;
                pKonceMS = aKonecMS;
                if (!UpravCasZobraz(aTag, aPocatekMS, aKonecMS))
                {
                    return false;
                }
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
            if (pDelkaMS <= 0)
            {
                MessageBox.Show("Vybraný element nemá přiřazena žádná audio data k přepsání. Nejprve je vyberte.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (pPocatekMS < 0 || pPocatekMS > oWav.DelkaSouboruMS)
            {
                MessageBox.Show("Počátek audio dat elementu je mimo audio soubor! Automatický přepis nebude spuštěn.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (pKonceMS < 0 || pKonceMS > oWav.DelkaSouboruMS)
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
                aTag = PridejSekci(aTag.tKapitola, "Sekce automatického přepisu", -1, -1, pChapter.begin, pChapter.end);
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
                /*
                if (pSection.hasParagraph)
                {
                    if (MessageBox.Show("Vybraná sekce již obsahuje odstavce. Chcete je všechny smazat a začít s přepisem?", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                    {
                        //odstrani predesle odstavce
                        for (int i = 0; i < pSection.paragraphs.Count; i++)
                        {
                            OdstranOdstavec(aTag.tKapitola, aTag.tSekce, i);
                        }
                    }
                    else return false;
                }*/
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
                    aTag = PridejOdstavec(aTag.tKapitola, aTag.tSekce, "", null, -1, pSection.begin, pSection.end, myDataSource.SeznamMluvcich.VratSpeakera(pSection.speaker));
                }
            }



            if (aTag.JeOdstavec)
            {
                MyParagraph pParagraph = myDataSource.VratOdstavec(aTag);
                if (pParagraph == null) return false;
                if (pParagraph.DelkaMS <= 0)
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




            if (oPrepisovac == null) oPrepisovac = new MyPrepisovac(nastaveniAplikace.AbsolutniAdresarRozpoznavace, nastaveniAplikace.rozpoznavac.Mluvci, nastaveniAplikace.rozpoznavac.JazykovyModel, nastaveniAplikace.rozpoznavac.PrepisovaciPravidla, nastaveniAplikace.rozpoznavac.LicencniServer, nastaveniAplikace.rozpoznavac.LicencniSoubor, nastaveniAplikace.rozpoznavac.DelkaInternihoBufferuPrepisovace, nastaveniAplikace.rozpoznavac.KvalitaRozpoznavaniDiktat, new EventHandler(oPrepisovac_HaveDataPrectena));

            string pMluvci = nastaveniAplikace.rozpoznavac.Mluvci;
            string pJazykovyModel = nastaveniAplikace.rozpoznavac.JazykovyModel;
            MySpeaker pSpeaker = myDataSource.VratSpeakera(aTag);
            if (pSpeaker != null)
            {
                if (pSpeaker.RozpoznavacMluvci != null) pMluvci = nastaveniAplikace.rozpoznavac.MluvciRelativniAdresar + "/" + pSpeaker.RozpoznavacMluvci;
                if (pSpeaker.RozpoznavacJazykovyModel != null) pJazykovyModel = nastaveniAplikace.rozpoznavac.JazykovyModelRelativniAdresar + "/" + pSpeaker.RozpoznavacJazykovyModel;
            }
            oPrepisovac.InicializaceRozpoznavace(nastaveniAplikace.AbsolutniAdresarRozpoznavace, nastaveniAplikace.rozpoznavac.LicencniSoubor, pMluvci, pJazykovyModel, nastaveniAplikace.rozpoznavac.PrepisovaciPravidla, nastaveniAplikace.rozpoznavac.DelkaInternihoBufferuPrepisovace.ToString(), null);

            //oPrepisovac.InicializaceRozpoznavace();
            //oPrepisovac.HaveDataPrectena += new DataReadyEventHandler(oPrepisovac_HaveDataPrectena);
            if (timerRozpoznavace != null && timerRozpoznavace.IsEnabled == false)
            {
                InitializeTimerRozpoznavace(MyKONST.PERIODA_TIMERU_ROZPOZNAVACE_MS);
            }

            //inicializace prepisovace a hlidaciho timeru
            //vrati vybrany odstavec
            MyParagraph pOdstavec = myDataSource.VratOdstavec(aTag);
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
            if (pOdstavec.DelkaMS > 0)
            {
                oWav.AsynchronniNacteniRamce2(pOdstavec.begin, pOdstavec.DelkaMS, MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU);
            }
            ZmenStavTlacitekRozpoznavace(true, false, false, true);
            //NASLEDNE SE ceka na INITIALIZED a pak je spusteno rozpoznavani
            return true;
        }

        /// <summary>
        /// enabled/disabled ovladaci tlacitka rozpoznavace - pozdeji snad i menu, hlasky v dolni liste
        /// </summary>
        /// <param name="aRozpoznavani"></param>
        /// <param name="aDiktat"></param>
        /// <param name="aHlasoveOvladani"></param>
        /// <param name="aCelkovyStav"></param>
        public void ZmenStavTlacitekRozpoznavace(bool aRozpoznavani, bool aDiktat, bool aHlasoveOvladani, bool aRozpoznavaniProbiha)
        {
            if (aRozpoznavani)
            {
                if (aRozpoznavaniProbiha)
                {
                    btAutomatickeRozpoznani.Content = "Zastavit přepisování";
                    btDiktat.IsEnabled = false;
                    //btHlasoveOvladani.IsEnabled = false;
                    ZobrazStavProgramu("Inicializace automatického rozpoznávače...");
                }
                else
                {
                    btAutomatickeRozpoznani.Content = "Automatický přepis (F5)";
                    btDiktat.IsEnabled = ((oHlasoveOvladani != null && oHlasoveOvladani.Rozpoznavani) || oHlasoveOvladani == null);
                    //btHlasoveOvladani.IsEnabled = true;
                    ZobrazStavProgramu("Rozpoznávání přerušeno");
                }

            }
            if (aDiktat)
            {
                if (aRozpoznavaniProbiha)
                {
                    btAutomatickeRozpoznani.IsEnabled = false;
                    btDiktat.Content = "Zastavit diktování"; ;
                    btHlasoveOvladani.IsEnabled = false;
                    ZobrazStavProgramu("Probíhá inicializace automatického rozpoznávače pro diktování...");
                }
                else
                {
                    btAutomatickeRozpoznani.IsEnabled = true;
                    btDiktat.Content = "Diktát (F6)";
                    btDiktat.IsEnabled = true;
                    btHlasoveOvladani.IsEnabled = true;
                    ZobrazStavProgramu(tbStavProgramu.Text = "Dokončování přepisu diktátu z vyrovnávací paměti...");
                }

            }
            if (aHlasoveOvladani)
            {
                if (aRozpoznavaniProbiha)
                {
                    //btAutomatickeRozpoznani.IsEnabled = false;
                    btDiktat.IsEnabled = false;
                    btHlasoveOvladani.Content = "Konec hlas. ovládání"; ;
                    ZobrazStavProgramu("Probíhá inicializace automatického rozpoznávače pro hlasové ovládání...");
                }
                else
                {
                    //btAutomatickeRozpoznani.IsEnabled = true;
                    btDiktat.IsEnabled = true;
                    btHlasoveOvladani.Content = "Hlasové ovládání (F7)";
                    ZobrazStavProgramu(tbStavProgramu.Text = "Dokončování rozpoznání povelů z vyrovnávací paměti...");
                }

            }

        }


        //+ vlny
        private void ToolBar2BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            oVlna.ZvetseniVlnyYSmerProcenta += 50;
            KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, oVlna.PouzeCasovaOsa);
            oVlna.AutomatickeMeritko = false;
        }

        //- vlny
        private void ToolBar2BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            oVlna.ZvetseniVlnyYSmerProcenta -= 40;
            oVlna.AutomatickeMeritko = false;
            KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, oVlna.PouzeCasovaOsa);

        }

        private void ToolBar2BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            oVlna.AutomatickeMeritko = true;
            KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, oVlna.PouzeCasovaOsa);
        }

        private void slPoziceMedia_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
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

                    case 1000:
                        btPrehratZastavit_Click(null, new RoutedEventArgs());
                        break;
                    case 1001:
                        btPrehratZastavit_Click(null, new RoutedEventArgs());
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// obsluha stisku klaves a zkratek k ovladani programu - pro cely formular
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            leftShift = Keyboard.IsKeyDown(Key.LeftShift);
            leftCtrl = Keyboard.IsKeyDown(Key.LeftCtrl);
            Key syskey;
            Key key;
            bool repeat;
            if (e == null)
            {
                syskey = HIDsystemkey;
                key = HIDkey;
                repeat = false;

            }
            else
            {
                syskey = e.SystemKey;
                key = e.Key;
                repeat = e.IsRepeat;
            }

            //systemove klavesy se stisklym alt
            switch (syskey)
            {
                case Key.Left:
                    NastavPoziciKurzoru(oVlna.KurzorPoziceMS - oVlna.mSekundyMalySkok, true, true);
                    if(e!=null)
                        e.Handled = true;
                    break;
                case Key.Right:
                    NastavPoziciKurzoru(oVlna.KurzorPoziceMS + oVlna.mSekundyMalySkok, true, true);
                    if (e != null)
                        e.Handled = true;
                    break;
                case Key.Return: //alt+enter = maximalizovat
                    if (this.WindowState == WindowState.Normal)
                    {
                        this.WindowState = WindowState.Maximized;
                    }
                    else if (this.WindowState == WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Normal;
                    }
                    if (e != null)
                        e.Handled = true;
                    break;
                case Key.F10:       //automaticky foneticky prepis
                    menuItemNastrojeFonetickyPrepis_Click(null, new RoutedEventArgs());
                    menuItemFonetickyPrepis_Click(null, new RoutedEventArgs());
                    if (e != null)
                        e.Handled = true;
                    break;

            }




            switch (key)
            {
                case Key.F5:       //rozpoznani aktualniho elementu
                    button10_Click(null, new RoutedEventArgs());
                    break;
                case Key.F6:       //diktat
                    btDiktat_Click(null, new RoutedEventArgs());
                    break;
                case Key.F7:       //hlasove ovladani
                    btHlasoveOvladani_Click(null, new RoutedEventArgs());
                    break;
                case Key.F9:       //normalizace textu
                    menuItemNastrojeNormalizovat_Click(null, new RoutedEventArgs());
                    break;
                case Key.F11:       //odstraneni nefonemu...
                    btOdstranitNefonemy_Click(null, new RoutedEventArgs());
                    if (e != null)
                        e.Handled = true;
                    break;
                case Key.F12:       //porizeni fotografie z videa a vyvolani spravce mluvcich
                    menuItemVideoPoriditFotku_Click(null, new RoutedEventArgs());
                    break;
                case Key.Tab:       //prehravani nebo pausnuti audia/videa
                    if (!repeat)
                    {
                        if (_playing)
                        {
                            if (jeVideo) meVideo.Pause();
                            prehratVyber = false;
                            if (MWP != null)
                            {
                                MWP.Pause();
                                Playing = false;
                            }
                        }
                        else
                        {

                            bool adjustspeed = false;
                            if (leftShift)
                            {
                                adjustspeed = true;
                                meVideo.SpeedRatio = nastaveniAplikace.ZpomalenePrehravaniRychlost;
                            }
                            else
                            {
                                meVideo.SpeedRatio = 1.0;
                            }

                            if (leftCtrl)
                            {
                                prehratVyber = true;
                                if (oVlna.KurzorVyberPocatekMS > -1)
                                {
                                    NastavPoziciKurzoru(oVlna.KurzorVyberPocatekMS, true, false);
                                }
                                else
                                { 
                                  
                                }
                            }
                            if (jeVideo) meVideo.Play();
                            //spusteni prehravani pomoci tlacitka-kvuli nacteni primeho prehravani
                            if (MWP == null)
                            {
                                btPrehratZastavit_Click(null, new RoutedEventArgs());
                            }

                            Playing = true;

                            if(adjustspeed)
                                MWP.Play(nastaveniAplikace.ZpomalenePrehravaniRychlost);
                            else
                                MWP.Play();

                        }
                        
                    }
                    if (e != null)
                        e.Handled = true;

                    break;
                case Key.LeftCtrl:
                    leftCtrl = true;
                    break;
                case Key.LeftShift:
                    leftShift = true;
                    break;
                case Key.N:
                    if (!e.IsRepeat && leftCtrl)
                    {
                        if (e != null)
                            e.Handled = true;
                        leftCtrl = false;
                        MSoubor_Novy_Click(null, new RoutedEventArgs());
                    }
                    break;
                case Key.O:
                    if (!e.IsRepeat && leftCtrl)
                    {
                        if (e != null)
                            e.Handled = true;
                        leftCtrl = false;
                        MSoubor_Otevrit_Titulky_Click(null, new RoutedEventArgs());
                    }
                    break;
                case Key.S:
                    if (!e.IsRepeat && leftCtrl)
                    {
                        if (e != null)
                            e.Handled = true;
                        leftCtrl = false;
                        MSoubor_Ulozit_Click(null, new RoutedEventArgs());
                    }
                    break;
                case Key.F1:
                    if (!e.IsRepeat)
                    {
                        if (leftCtrl) MNapoveda_O_Programu_Click(null, new RoutedEventArgs());
                        else
                            MNapoveda_Popis_Programu_Click(null, new RoutedEventArgs());
                        if (e != null)
                            e.Handled = true;
                    }
                    break;
                case Key.F2:
                    if (!e.IsRepeat && myDataSource != null && myDataSource.Chapters.Count == 0)
                    {
                        e.Handled = true;
                        MyTag pomTag = PridejKapitolu(-1, "");
                        if (pomTag != null)
                        {
                            pomTag.tSender = VratSenderTextboxu(pomTag);
                            if (pomTag.tSender != null)
                            {
                                ((TextBox)pomTag.tSender).Focus();
                            }
                        }
                    }
                    break;
                default:
                    break;
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
                case Key.LeftShift:
                    leftShift = false;
                    break;
                case Key.LeftCtrl:
                    leftCtrl = false;
                    //posouvatL = false;
                    //posouvatP = false;
                    break;
                case Key.F10:
                    if (e != null)
                        e.Handled = true;
                    break;
                default:
                    break;
            }
        }


        private bool SpustHlasoveOvladani()
        {
            try
            {
                if ((oHlasoveOvladani != null && (oHlasoveOvladani.Rozpoznavani || oHlasoveOvladani.Ukoncovani)))
                {
                    //ukonceni predchoziho prepisu
                    if (MessageBox.Show("Dochází k automatickému rozpoznávání jiného úkolu. Nejprve musíte zastavit předchozí rozpoznávání.\nChcete ho nyní přerušit?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (oHlasoveOvladani.StopHned() == 0)
                        {

                        }
                    }
                    return false;
                }
                if (oPrepisovac != null && oPrepisovac.TypRozpoznavani == MyKONST.ROZPOZNAVAC_1_DIKTAT && (oPrepisovac.Rozpoznavani || oPrepisovac.Ukoncovani))
                {
                    if (MessageBox.Show("Dochází k automatickému rozpoznávání jiného úkolu. Nejprve musíte zastavit předchozí rozpoznávání.\nChcete ho nyní přerušit?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (oPrepisovac.StopHned() == 0)
                        {

                        }
                    }
                    return false;
                }

                pPozadovanyStavRozpoznavace = MyKONST.ROZPOZNAVAC_2_HLASOVE_OVLADANI;

                //vytvoreni instance prepisovace, pokud neexistuje
                if (oHlasoveOvladani == null)
                {
                    oHlasoveOvladani = new MyPrepisovac(nastaveniAplikace.AbsolutniAdresarRozpoznavace, nastaveniAplikace.rozpoznavac.Mluvci, nastaveniAplikace.rozpoznavac.JazykovyModel, nastaveniAplikace.rozpoznavac.PrepisovaciPravidla, nastaveniAplikace.rozpoznavac.LicencniServer, nastaveniAplikace.rozpoznavac.LicencniSoubor, nastaveniAplikace.rozpoznavac.DelkaInternihoBufferuPrepisovace, nastaveniAplikace.rozpoznavac.KvalitaRozpoznavaniOvladani, new EventHandler(oPrepisovac_HaveDataPrectena));
                }
                string pMluvci = nastaveniAplikace.rozpoznavac.Mluvci;
                string pJazykovyModel = nastaveniAplikace.rozpoznavac.JazykovyModel;
                string pPrepisovaciPravidla = nastaveniAplikace.rozpoznavac.PrepisovaciPravidla;
                if (nastaveniAplikace.diktatMluvci != null)
                {
                    if (nastaveniAplikace.hlasoveOvladaniMluvci.RozpoznavacMluvci != null) pMluvci = nastaveniAplikace.rozpoznavac.MluvciRelativniAdresar + "/" + nastaveniAplikace.hlasoveOvladaniMluvci.RozpoznavacMluvci;
                    if (nastaveniAplikace.hlasoveOvladaniMluvci.RozpoznavacJazykovyModel != null) pJazykovyModel = nastaveniAplikace.rozpoznavac.JazykovyModelRelativniAdresar + "/" + nastaveniAplikace.hlasoveOvladaniMluvci.RozpoznavacJazykovyModel;
                    if (nastaveniAplikace.hlasoveOvladaniMluvci.RozpoznavacPrepisovaciPravidla != null) pPrepisovaciPravidla = nastaveniAplikace.rozpoznavac.PrepisovaciPravidlaRelativniAdresar + "/" + nastaveniAplikace.hlasoveOvladaniMluvci.RozpoznavacPrepisovaciPravidla;
                }


                oHlasoveOvladani.InicializaceRozpoznavace(nastaveniAplikace.AbsolutniAdresarRozpoznavace, nastaveniAplikace.rozpoznavac.LicencniSoubor, pMluvci, pJazykovyModel, pPrepisovaciPravidla, nastaveniAplikace.rozpoznavac.DelkaInternihoBufferuPrepisovace.ToString(), nastaveniAplikace.rozpoznavac.KvalitaRozpoznavaniOvladani.ToString());
                if (timerRozpoznavace != null && timerRozpoznavace.IsEnabled == false)
                {
                    InitializeTimerRozpoznavace(MyKONST.PERIODA_TIMERU_ROZPOZNAVACE_MS);
                }
                //oHlasoveOvladani.PrepisovanyElementTag = nastaveniAplikace.RichTag;
                oHlasoveOvladani.PrepisovanyElementTag = null;

                return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                return false;
            }


        }

        private void btHlasoveOvladani_Click(object sender, RoutedEventArgs e)
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


        /// <summary>
        /// porizeni obrazku z videa a poslani fotky do spravce mluvcich
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btPoriditObrazekZVidea_Click(object sender, RoutedEventArgs e)
        {
            if (jeVideo)
            {

                Size dpi = new Size(96, 96);

                RenderTargetBitmap bmp = new RenderTargetBitmap((int)gVideoPouze.ActualWidth, (int)gVideoPouze.ActualHeight + (int)gVideoPouze.Margin.Top, dpi.Width, dpi.Height, PixelFormats.Pbgra32);
                bmp.Render(gVideoPouze);


                BitmapFrame pFrame = BitmapFrame.Create(bmp);

                string pBase = MyKONST.PrevedJPGnaBase64String(pFrame);


                WinSpeakers.ZiskejMluvciho(nastaveniAplikace, this.myDatabazeMluvcich, null, pBase);
            }
            else
            {
                MessageBox.Show("Nelze vytvořit obrázek mluvčího, protože není načteno video", "Upozornění!");
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void menuItemFonetickyPrepis_Click(object sender, RoutedEventArgs e)
        {
            ZobrazitOknoFonetickehoPrepisu(true);
        }

        private void btFonetickyPrepis_Click(object sender, RoutedEventArgs e)
        {
            //menuItemFonetickyPrepis_Click(null, new RoutedEventArgs());
            try
            {
                MyFonetic mf = new MyFonetic(nastaveniAplikace.absolutniCestaEXEprogramu);
                tbFonetickyPrepis.Text = mf.VratFonetickyPrepis(myDataSource.VratOdstavec(nastaveniAplikace.RichTag).Text);
            }
            catch
            {

            }
        }


        /// <summary>
        /// delegat pro zobrazeni fonetickeho prepisu z jineho threadu
        /// </summary>
        /// <param name="aTag"></param>
        private delegate void ZobrazeniFonetickehoPrepisu(MyTag aTag);

        /// <summary>
        /// zobrazi foneticky prepis z datove struktury do prislusneho textboxu, Mozno volat z jineho threadu
        /// </summary>
        /// <param name="aTag"></param>
        /// <returns></returns>
        private void ZobrazitFonetickyPrepisOdstavce(MyTag aTag)
        {
            try
            {
                //return;
                if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
                {
                    //do invoke stuff here
                    this.Dispatcher.Invoke(new ZobrazeniFonetickehoPrepisu(ZobrazitFonetickyPrepisOdstavce), new object[] { aTag });
                    return;
                }

                MyTag pTag = new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec, MyEnumTypElementu.foneticky, tbFonetickyPrepis);
                MyParagraph pP = myDataSource.VratOdstavec(pTag);
                pUpravitOdstavec = false;
                if (pP == null)
                {
                    tbFonetickyPrepis.Text = "";
                }
                else
                {
                    tbFonetickyPrepis.Text = pP.Text;
                }
                pUpravitOdstavec = true;
                if (pTag.JeOdstavec)
                {
                    if (!tbFonetickyPrepis.IsFocused)
                    {
                        tbFonetickyPrepis.Background = nastaveniAplikace.BarvaTextBoxuFoneticky;
                    }
                }
                else
                {
                    tbFonetickyPrepis.Background = nastaveniAplikace.BarvaTextBoxuFonetickyZakazany;
                }
                tbFonetickyPrepis.Tag = pTag; //prirazeni tagu odstavce
                //return true;
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
                //return false;
            }

        }

        /// <summary>
        /// zobrazi nebo skryje foneticky prepis - respektive okno pro upravu
        /// </summary>
        /// <param name="aZobrazit"></param>
        /// <returns></returns>
        private bool ZobrazitOknoFonetickehoPrepisu(bool aZobrazit)
        {

            if (aZobrazit)
            {
                nastaveniAplikace.ZobrazitFonetickyPrepis = Math.Abs(nastaveniAplikace.ZobrazitFonetickyPrepis);

                d.RowDefinitions[1].Height = new GridLength(nastaveniAplikace.ZobrazitFonetickyPrepis);

                return true;
            }
            else
            {
                nastaveniAplikace.ZobrazitFonetickyPrepis = -Math.Abs(nastaveniAplikace.ZobrazitFonetickyPrepis);
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
            if (gPrepis.RowDefinitions[1].Height.Value > 40) nastaveniAplikace.ZobrazitFonetickyPrepis = (float)gPrepis.RowDefinitions[1].Height.Value;
        }





        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetKeyboardState(byte[] lpKeyState);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, byte[] pwszBuff, int cchBuff, uint wFlags);


        private void tbFonetickyPrepis_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            //char p = (char)pStavChar;
            lock (this)
            {

                if (e.Key == Key.Escape)
                {
                    TextBox pTb = (TextBox)VratSenderTextboxu(((MyTag)((TextBox)tbFonetickyPrepis).Tag));
                    pTb.Focus();
                    return;
                }

                char c = ' ';
                int pStav = -1;

                int VirtualKey = KeyInterop.VirtualKeyFromKey(e.Key);
                byte[] State = new byte[256];
                int pstav = GetKeyboardState(State);
                uint uVirtualKey = (uint)VirtualKey;
                uint pScanCode = MapVirtualKey(uVirtualKey, (uint)0x0);


                byte[] output = new byte[256];
                int pStavUnicode = 0;
                //pStavUnicode = ToUnicode(uVirtualKey, pScanCode, State, output, outBufferLength, flags);
                if (pStavUnicode > 0)
                {
                    c = (char)output[0];
                    pstav = 1;
                }



                if (pStav > 0)
                {
                    for (int i = 0; i < MyFonetic.ABECEDA_FONETICKA.Length; i++)
                    {

                        if (MyFonetic.ABECEDA_FONETICKA[i] == c)
                        {

                            return;
                        }
                    }
                    e.Handled = true;
                }
            }

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
        public int Normalizovat(MySubtitlesData aDokument, MyTag aElement, int aIndexNormalizace)
        {
            try
            {
                if (aElement == null) return -1;
                List<MyParagraph> pSeznamKNormalizaci = new List<MyParagraph>();
                if (aElement.JeKapitola)
                {
                    MyChapter pC = aDokument.VratKapitolu(aElement);
                    for (int i = 0; i < pC.Sections.Count; i++)
                    {
                        for (int j = 0; j < pC.Sections[i].Paragraphs.Count; j++)
                        {
                            pSeznamKNormalizaci.Add(pC.Sections[i].Paragraphs[j]);
                        }
                    }
                }
                else if (aElement.JeSekce)
                {
                    MySection pS = aDokument.VratSekci(aElement);
                    pSeznamKNormalizaci.AddRange(pS.Paragraphs);
                }
                else if (aElement.JeOdstavec)
                {
                    pSeznamKNormalizaci.Add(aDokument.VratOdstavec(aElement));
                }
                if (pSeznamKNormalizaci == null || pSeznamKNormalizaci.Count == 0) return -1;
                if (aIndexNormalizace < 0) aIndexNormalizace = WinNormalizace.ZobrazitVyberNormalizace(-1, this);
                if (aIndexNormalizace < 0) return -1;
                MyFonetic pFonetika = new MyFonetic(this.nastaveniAplikace.absolutniCestaEXEprogramu);
                for (int j = 0; j < pSeznamKNormalizaci.Count; j++)
                {
                    MyParagraph pOdstavec = pSeznamKNormalizaci[j];

                    for (int i = 0; i < pOdstavec.Phrases.Count; i++)
                    {
                        string pText = pOdstavec.Phrases[i].Text;
                        string pTextNormalizovany = pText;

                        pFonetika.NormalizaceTextu(aIndexNormalizace, pText, ref pTextNormalizovany);
                        pOdstavec.Phrases[i].Text = pTextNormalizovany;
                    }
                }
                if (aDokument == myDataSource)
                {
                    ZobrazXMLData();
                    //vybrani puvodniho elementu
                    try
                    {
                        aElement.tSender = VratSenderTextboxu(aElement);
                        (aElement.tSender as TextBox).Focus();
                    }
                    catch
                    {
                    }
                }


                return 0;
            }
            catch
            {
                return -1;
            }
        }

        private void btNormalizovat_Click(object sender, RoutedEventArgs e)
        {
            menuItemNastrojeNormalizovat_Click(null, new RoutedEventArgs());

        }

        private void FonetickyPrepisDokoncen(string aText, MyBuffer16 aBufferProPrepsani, int aStav)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new DelegatePhoneticOut(FonetickyPrepisDokoncen), aText, aBufferProPrepsani, aStav);
                return;
            }

            string pText = aText;
            List<MyCasovaZnacka> pZnacky = new List<MyCasovaZnacka>();
            ZpracujRozpoznanyText(aBufferProPrepsani.PocatekMS, 0, ref pText, ref pZnacky);
            pText = pText.Replace(' ', '_');
            if (pText != null && pText.Length > 0 && pText[pText.Length - 1] == '_')
            {
                pText = pText.Remove(pText.Length - 1);
            }
            MyTag pTagFoneticky = new MyTag(bFonetika.TagKPrepsani);
            pTagFoneticky.tTypElementu = MyEnumTypElementu.foneticky;
            pDokumentFonetickehoPrepisu.UpravElementOdstavce(pTagFoneticky, pText, pZnacky);
            pDokumentFonetickehoPrepisu.OznacTrenovaciData(bFonetika.TagKPrepsani, true);

            if (bFonetika.TagKPrepsani.tKapitola == nastaveniAplikace.RichTag.tKapitola && bFonetika.TagKPrepsani.tSekce == nastaveniAplikace.RichTag.tSekce && bFonetika.TagKPrepsani.tOdstavec == nastaveniAplikace.RichTag.tOdstavec)
            {
                ZobrazitFonetickyPrepisOdstavce(bFonetika.TagKPrepsani);
                /*
                if (pAutomaticky)
                {

                }
                else 
                 */
                if (nastaveniAplikace.fonetickyPrepis.PrehratAutomatickyRozpoznanyOdstavec)
                {
                    tbFonetickyPrepis.Focus();
                    prehratVyber = true;
                    //Playing = true;
                    btPrehratZastavit_Click(null, new RoutedEventArgs());


                }
            }
            AllignmentZpracovaniFonetickehoPrepisu(nastaveniAplikace.RichTag, bFonetika.TagKPrepsani);

            //zpracovani dalsich odstavcu na seznamu
            if (this.pSeznamOdstavcuKRozpoznaniFonetika != null && this.pSeznamOdstavcuKRozpoznaniFonetika.Count > 0)
            {
                MyParagraph pOdstavec = null;

                while (pSeznamOdstavcuKRozpoznaniFonetika.Count > 0)
                {
                    MyTag pTag = new MyTag(pSeznamOdstavcuKRozpoznaniFonetika[0]);
                    pSeznamOdstavcuKRozpoznaniFonetika.RemoveAt(0);
                    pTag.tTypElementu = MyEnumTypElementu.normalni;
                    pOdstavec = pDokumentFonetickehoPrepisu.VratOdstavec(pTag);
                    //ulozi tag vybraneho odstavce do promenne prepisovace a vymaze pripadna drivejsi data z pomocnych promennych
                    bFonetika.TagKPrepsani = pTag;
                    //pokus o uzamceni textboxu proti upravam psani
                    try
                    {
                        //((TextBox)bFonetika.TagKPrepsani.tSender).IsReadOnly = true;
                    }
                    catch
                    {

                    }
                    if (pOdstavec.DelkaMS < 20)
                    {
                        pOdstavec = null;
                    }
                    else
                    {
                        break;
                    }
                }
                if (pOdstavec == null) return;
                //spusti asynchronni nacteni bufferu
                if (pOdstavec.DelkaMS > 0)
                {
                    bFonetika.TextKPrepsani = pOdstavec.Text;
                    oWav.AsynchronniNacteniRamce2(pOdstavec.begin, pOdstavec.DelkaMS, MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS);
                }
            }
            else
            {
                this.ZobrazStavProgramu("Fonetický přepis byl dokončen...");
                this.UpdateXMLData(false, true, false, true, true);
                //davkove zpracovani - pokracovani
                if (pAutomaticky2)
                {
                    this.UlozitTitulky(false, this.myDataSource.JmenoSouboru);
                    if (pAutomatickyIndex < lbDavkoveNacteni.Items.Count - 1) pAutomatickyIndex++; else pAutomaticky2 = false;
                    if (pAutomaticky2)
                    {
                        NactiPolozkuDavkovehoZpracovani(pAutomatickyIndex);
                    }
                }
            }


        }


        /// <summary>
        /// Spusti foneticky prepis elementu, pokud je aPocatek a aKonec nastaven>=0 dojde k rozpoznani vyberu a ulozeni do vybraneho elementu
        /// </summary>
        /// <returns></returns>
        private bool SpustFonetickyPrepis(MySubtitlesData aDokument, MyTag aTag, long aPocatekMS, long aKonecMS)
        {
            if (bFonetika != null && bFonetika.Prepisovani)
            {
                MessageBoxResult pResult = MessageBox.Show("Dochází k automatické tvorbě fonetického přepisu. Chcete ho přerušit?", "", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                if (pResult == MessageBoxResult.Yes)
                {

                }

                return false;
            }


            if (aTag == null)
            {
                MessageBox.Show("Není vybrán ani nastaven žádný element k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            aTag = new MyTag(aTag);
            aTag.tTypElementu = MyEnumTypElementu.foneticky;

            long pPocatekMS;
            long pKonceMS;
            long pDelkaMS;



            if (aPocatekMS >= 0 && aKonecMS >= 0)
            {
                pPocatekMS = aPocatekMS;
                pKonceMS = aKonecMS;
                /*if (!UpravCasZobraz(aTag, aPocatekMS, aKonecMS))
                {
                    return false;
                }*/
            }
            else
            {
                //kontrola audio dat jestli jsou vybrana
                pPocatekMS = aDokument.VratCasElementuPocatek(aTag);
                pKonceMS = aDokument.VratCasElementuKonec(aTag);
            }
            pDelkaMS = pKonceMS - pPocatekMS;

            if (!oWav.Nacteno)
            {
                MessageBox.Show("Není načten žádný audio soubor pro přepis! Automatický přepis nebude spuštěn.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (pDelkaMS <= 0 && aTag.JeOdstavec)
            {
                MessageBox.Show("Vybraný element nemá přiřazena žádná audio data k přepsání. Nejprve je vyberte.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if ((pPocatekMS < 0 || pPocatekMS > oWav.DelkaSouboruMS) && aTag.JeOdstavec)
            {
                MessageBox.Show("Počátek audio dat elementu je mimo audio soubor! Automatický přepis nebude spuštěn.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if ((pKonceMS < 0 || pKonceMS > oWav.DelkaSouboruMS) && aTag.JeOdstavec)
            {
                //MessageBox.Show("Konec audio dat elementu je mimo audio soubor! Automatický přepis nebude spuštěn.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                //return false;
            }

            pSeznamOdstavcuKRozpoznaniFonetika = new List<MyTag>();
            bool aIgnorovatTextOdstavce = false;
            //testovani zda je element kapitola a dotazy na jeho plnost
            if (aTag.JeKapitola)
            {
                MyChapter pChapter = aDokument.VratKapitolu(aTag);
                if (pChapter == null)
                {
                    MessageBox.Show("Není vybrán ani nastaven žádný element k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }
                if (pChapter.hasSection)
                {
                    if (MessageBox.Show("Vybraná kapitola již obsahuje fonetické sekce. Chcete je všechny foneticky přepsat?", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                    {
                        for (int j = 0; j < pChapter.Sections.Count; j++)
                        {
                            MySection pSection = aDokument.VratSekci(new MyTag(aTag.tKapitola, j, -1));
                            if (pSection != null)
                            {
                                //odstavce k prepsani
                                for (int i = 0; i < pSection.PhoneticParagraphs.Count; i++)
                                {

                                    //uprava tagu, aby obsahoval i sender na textbox, pokud byly vytvareny
                                    MyTag ppTag = new MyTag(aTag.tKapitola, j, i, aTag.tTypElementu, null);
                                    //ppTag.tSender = VratSenderTextboxu(ppTag);
                                    //if (ppTag.tSender != null) ((TextBox)ppTag.tSender).Clear();
                                    pSeznamOdstavcuKRozpoznaniFonetika.Add(ppTag);
                                }
                            }
                        }
                        if (pSeznamOdstavcuKRozpoznaniFonetika.Count > 0)
                        {
                            aTag = pSeznamOdstavcuKRozpoznaniFonetika[0];
                            pSeznamOdstavcuKRozpoznaniFonetika.RemoveAt(0);
                            aIgnorovatTextOdstavce = true;
                        }
                        else
                        {
                            MessageBox.Show("Kapitola nema odstavce k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                            return false;
                        }

                    }
                    else return false;
                }

            }

            //testovani sekce
            if (aTag.JeSekce)
            {


                MySection pSection = aDokument.VratSekci(aTag);
                if (pSection == null)
                {
                    MessageBox.Show("Není vybrán ani nastaven žádný element k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }

                if (pSection.hasParagraph)
                {

                    if (!pSection.hasPhoneticParagraph || MessageBox.Show("Vybraná sekce již obsahuje fonetické odstavce. Chcete je všechny automaticky znovu přepsat? Text uvnitř bude smazán.", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                    {

                        //odstavce k prepsani
                        //for (int i = 0; i < pSection.PhoneticParagraphs.Count; i++)
                        for (int i = 0; i < pSection.Paragraphs.Count; i++)
                        {

                            //uprava tagu, aby obsahoval i sender na textbox, pokud byly vytvareny
                            MyTag ppTag = new MyTag(aTag.tKapitola, aTag.tSekce, i, aTag.tTypElementu, null);
                            //ppTag.tSender = VratSenderTextboxu(ppTag);
                            //if (ppTag.tSender != null) ((TextBox)ppTag.tSender).Clear();
                            pSeznamOdstavcuKRozpoznaniFonetika.Add(ppTag);
                        }
                        aTag = pSeznamOdstavcuKRozpoznaniFonetika[0];
                        pSeznamOdstavcuKRozpoznaniFonetika.RemoveAt(0);
                        aIgnorovatTextOdstavce = true;
                    }
                    else return false;
                }
                else
                {
                    MessageBox.Show("Sekce nema odstavce k přepsání!", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }
            }

            if (aTag.JeOdstavec)
            {
                aTag.tTypElementu = MyEnumTypElementu.normalni;
                MyParagraph pParagraph = aDokument.VratOdstavec(aTag);
                aTag.tTypElementu = MyEnumTypElementu.foneticky;
                MyParagraph pPhoneticPar = aDokument.VratOdstavec(aTag);

                if (pParagraph == null) return false;
                if (pParagraph.DelkaMS <= 0)
                {
                    MessageBox.Show("Vybraný element nemá přiřazena žádná audio data k přepsání. Nejprve je vyberte.", "Upozornění:", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return false;
                }
                if (pPhoneticPar != null && pPhoneticPar.Phrases.Count > 0 && pPhoneticPar.Text.Length > 0)
                {
                    if (aIgnorovatTextOdstavce)
                    {
                        pPhoneticPar.UlozTextOdstavce("", null);
                    }
                    else
                    {
                        if (MessageBox.Show("Vybraný odstavec obsahuje text. Chcete přesto začít s přepisem? Data budou přepsána", "Upozornění:", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            pPhoneticPar.UlozTextOdstavce("", null);
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


            if (bFonetika == null) bFonetika = new MyFonetic(nastaveniAplikace.absolutniCestaEXEprogramu);

            aTag.tTypElementu = MyEnumTypElementu.normalni;
            MyParagraph pOdstavec = aDokument.VratOdstavec(aTag);
            //ulozi tag vybraneho odstavce do promenne prepisovace a vymaze pripadna drivejsi data z pomocnych promennych
            bFonetika.TagKPrepsani = aTag;

            //pokus o uzamceni textboxu proti upravam psani
            try
            {
                //((TextBox)bFonetika.TagKPrepsani.tSender).IsReadOnly = true;
            }
            catch
            {

            }

            //spusti asynchronni nacteni bufferu
            if (pOdstavec.DelkaMS > 0)
            {
                pDokumentFonetickehoPrepisu = aDokument;
                bFonetika.TextKPrepsani = pOdstavec.Text;
                oWav.AsynchronniNacteniRamce2(pOdstavec.begin, pOdstavec.DelkaMS, MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS);
                this.ZobrazStavProgramu("Spuštěno vytváření fonetického přepisu... (HTK)");

            }

            return true;
        }


        private void MenuItemFonetickeVarianty_Click(object sender, RoutedEventArgs e)
        {
            if (nastaveniAplikace.RichFocus && nastaveniAplikace.RichTag.tTypElementu == MyEnumTypElementu.foneticky)
            {
                MyParagraph pp = myDataSource.VratOdstavec(nastaveniAplikace.RichTag);
                MyTag pTag = new MyTag(nastaveniAplikace.RichTag);
                if (pp == null) return;
                int pIndexKurzoru = tbFonetickyPrepis.CaretIndex;
                int pIndexPocatkuSlova = -1;
                string pText = "";
                for (int iii = 0; iii < pp.Phrases.Count; iii++)
                {

                    pText += pp.Phrases[iii].Text;
                    bool je = pText.Length >= pIndexKurzoru;
                    if (je)
                    {
                        string pSlovo = "";
                        int jjj = pIndexKurzoru;
                        if (jjj >= pText.Length) jjj = pText.Length - 1;
                        while (jjj >= 0 && jjj < pText.Length && pText[jjj] != '_')
                        {
                            pSlovo = pText[jjj] + pSlovo;
                            pIndexPocatkuSlova = jjj;
                            jjj--;
                        }
                        jjj = pIndexKurzoru + 1;
                        while (jjj < pText.Length && pText[jjj] != '_')
                        {
                            pSlovo += pText[jjj];
                            jjj++;
                        }

                        int pIndex = -1;

                        if (pSlovo.Length > 0)
                        {
                            if (pSlovo.Contains("{"))
                            {
                                pSlovo = pSlovo.Substring(1, pSlovo.Length - 2);
                                for (int j = 0; j < bSlovnikFonetickehoDoplneni.PridanaSlovaDocasna.Count; j++)
                                {
                                    if (bSlovnikFonetickehoDoplneni.PridanaSlovaDocasna[j].jeFonetickaVarianta(pSlovo))
                                    {
                                        //pSlovo = bSlovnikFonetickehoDoplneni.PridanaSlovaDocasna[j].Slovo;
                                        pIndex = j;
                                        break;
                                    }
                                }
                                //if (aNalezeno)
                                //break;

                            }

                            WinFonetickySlovnik ws = new WinFonetickySlovnik(pp.Phrases[iii].TextPrepisovany, pSlovo, pIndex, bSlovnikFonetickehoDoplneni);
                            ws.Owner = this;
                            if ((bool)ws.ShowDialog())
                            {
                                int pIndexSouctu = 0;
                                foreach (MyPhrase ph in pp.Phrases)
                                {
                                    pIndexSouctu += ph.Text.Length;
                                    if (pIndexPocatkuSlova < pIndexSouctu && ph.Text.Contains(pSlovo) && (pSlovo.Length == ph.Text.Length - 2 || pSlovo.Length == ph.Text.Length))
                                    {
                                        ph.Text = ws.tbFonetickyPrepis.Text;
                                    }
                                }
                            }
                            ZobrazitFonetickyPrepisOdstavce(pTag);
                            tbFonetickyPrepis.CaretIndex = pIndexPocatkuSlova;
                            return;

                        }

                    }

                }
            }
            return;
        }

        private void menuItemNastrojeNormalizovat_Click(object sender, RoutedEventArgs e)
        {
            MyTag pTag = nastaveniAplikace.RichTag;
            Normalizovat(myDataSource, pTag, -1);
        }

        /// <summary>
        /// spustu automaticky foneticky prepis - THREAD SAFE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItemNastrojeFonetickyPrepis_Click(object sender, RoutedEventArgs e)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                //do invoke stuff here
                
                this.Dispatcher.Invoke(new RoutedEventHandler(menuItemNastrojeFonetickyPrepis_Click), new object[] { sender, e });
                return;
            }


            MyTag pTag = new MyTag(nastaveniAplikace.RichTag);
            //btNormalizovat_Click(null, new RoutedEventArgs());
            Normalizovat(myDataSource, pTag, 1);

            SpustFonetickyPrepis(myDataSource, pTag, -1, -1);
            try
            {
                spSeznam.UpdateLayout();
                bool p = (VratSenderTextboxu(pTag) as TextBox).Focus();

            }
            catch
            {

            }
        }

        private void menuItemNastrojeAllignment_Click(object sender, RoutedEventArgs e)
        {
            MyTag pTag = new MyTag(nastaveniAplikace.RichTag);
            Allignment(myDataSource, pTag);

        }

        public int Allignment(MySubtitlesData aDokument, MyTag aTag)
        {
            try
            {
                aTag.tTypElementu = MyEnumTypElementu.normalni;
                if (aDokument == null) return -2;
                MySubtitlesData pKopieDokumentu = new MySubtitlesData(aDokument);
                if (aTag.JeSekce)
                {
                    MySection pS = pKopieDokumentu.VratSekci(aTag);
                    MySection novaSekce = new MySection(pS.name);
                    MyParagraph pPomocnyOdstavec = new MyParagraph();
                    for (int j = 0; j < pS.Paragraphs.Count; j++)
                    {
                        MyParagraph pP = new MyParagraph(pS.Paragraphs[j]);
                        if (aTag.tKapitola == 0 && aTag.tSekce == 0 && j == 0 && pP.begin < 0) pP.begin = 0; //nastaveni pocatku, pokud neni definovan a jsme u 1.elementu

                        if (j > 0 && pPomocnyOdstavec.end < 0 && pPomocnyOdstavec.begin >= 0 && pP.begin >= 0)
                        {
                            pPomocnyOdstavec.end = pP.begin;
                            novaSekce.Paragraphs.Add(pPomocnyOdstavec);
                            pPomocnyOdstavec = new MyParagraph();
                        }

                        pPomocnyOdstavec.UlozTextOdstavce(pPomocnyOdstavec.Text + pP.Text + " ", new List<MyCasovaZnacka>());
                        if (pPomocnyOdstavec.begin < 0 && pP.begin >= 0)
                        {
                            pPomocnyOdstavec.begin = pP.begin;
                        }
                        else if (pPomocnyOdstavec.begin >= 0 && pP.begin >= 0)
                        {
                            pPomocnyOdstavec.end = pP.begin;
                            novaSekce.Paragraphs.Add(pPomocnyOdstavec);
                            pPomocnyOdstavec = new MyParagraph();
                            pPomocnyOdstavec.begin = pP.begin;
                        }
                        if (pP.end >= 0) pPomocnyOdstavec.end = pP.end;

                        if (pPomocnyOdstavec.end >= 0)
                        {
                            novaSekce.Paragraphs.Add(pPomocnyOdstavec);
                            pPomocnyOdstavec = new MyParagraph();
                        }
                    }
                    if (pPomocnyOdstavec.begin >= 0 && pPomocnyOdstavec.end < 0 && pKopieDokumentu.Chapters[aTag.tKapitola].Sections.Count - 1 == aTag.tSekce)
                    {
                        pPomocnyOdstavec.end = oWav.DelkaSouboruMS;
                        novaSekce.Paragraphs.Add(pPomocnyOdstavec);
                    }
                    if (novaSekce.Paragraphs.Count != novaSekce.PhoneticParagraphs.Count)
                    {
                        novaSekce.PhoneticParagraphs.Clear();
                        for (int i = 0; i < novaSekce.Paragraphs.Count; i++)
                        {
                            novaSekce.PhoneticParagraphs.Add(new MyParagraph("", new List<MyCasovaZnacka>(), novaSekce.Paragraphs[i].begin, novaSekce.Paragraphs[i].end));
                        }
                    }

                    pKopieDokumentu.Chapters[aTag.tKapitola].Sections[aTag.tSekce] = novaSekce;
                    if (novaSekce.Paragraphs.Count == 0)
                    {
                        MessageBox.Show("Nejprve musíte přiřadit několik synchronizačních značek původnímu textu", "Chyba!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return -3;
                    }
                    if (Normalizovat(pKopieDokumentu, aTag, -1) < 0) return -2; //normalizace zrusena nebo skoncila chybou
                    SpustFonetickyPrepis(pKopieDokumentu, aTag, -1, -1);
                }
                return 0;
            }
            catch (Exception)
            {
                return -1;
            }

        }


        public int AllignmentZpracovaniFonetickehoPrepisu(MyTag aTagSekcePuvodniDokument, MyTag aTagOdstavecFonetickyDokument)
        {
            try
            {
                if (myDataSource == pDokumentFonetickehoPrepisu) return -2; //pro stejny dokument to nema smysl?????
                if (!aTagSekcePuvodniDokument.JeSekce) return -3;
                MySection pSekcePuvodni = myDataSource.VratSekci(aTagSekcePuvodniDokument);
                aTagOdstavecFonetickyDokument = new MyTag(aTagOdstavecFonetickyDokument);
                aTagOdstavecFonetickyDokument.tTypElementu = MyEnumTypElementu.foneticky;
                MyParagraph pOdstavecFoneticky = pDokumentFonetickehoPrepisu.VratOdstavec(aTagOdstavecFonetickyDokument);
                int pIndexPocatkuSlov = 0;
                long pBegin = -1;
                long pEnd = 0;
                bool pStopDekodovani = false;
                for (int i = 0; i < pSekcePuvodni.Paragraphs.Count; i++)
                {
                    if (pStopDekodovani) break;
                    MyParagraph pP = pSekcePuvodni.Paragraphs[i];
                    string pText = pP.Text;
                    MyParagraph pNovyOdstavec = new MyParagraph();
                    pNovyOdstavec.speakerID = pP.speakerID;
                    if (!pP.trainingElement)
                    {
                        //if (pBegin < 0 && i == 0) pBegin = 0;
                        if (pBegin < 0)
                        {
                            pBegin = pP.begin;
                        }

                        bool behat = true;
                        int pPosledniIndex = 0; //posledni index zapsany do noveho odstavce v puvodnim textu - info o preskoceni interpunkce atd...
                        //long pBegin = 0;
                        //long pEnd = 0;
                        while (behat)
                        {
                            MyPhrase pFraze = pOdstavecFoneticky.Phrases[pIndexPocatkuSlov];
                            if (pFraze.TextPrepisovany != null)
                            {
                                int pIndex = pText.ToLower().IndexOf(pFraze.TextPrepisovany, pPosledniIndex);
                                MyPhrase pFraze2 = new MyPhrase(pFraze.begin, pFraze.end, pFraze.TextPrepisovany, pP.speakerID, MyEnumTypElementu.normalni);
                                if (pPosledniIndex != pIndex)
                                {
                                    //pBegin = 0;
                                    pEnd = pFraze2.begin;
                                    int pDelka = pIndex - pPosledniIndex;
                                    if (pDelka <= 0)
                                    {
                                        int pDelkaKonec = pText.Length - pPosledniIndex;
                                        if (pDelkaKonec > 0)
                                        {
                                            string pTextKonec = pText.Substring(pPosledniIndex);
                                            MyPhrase pPhraseKonec = new MyPhrase(pBegin, pEnd, pTextKonec, pP.speakerID);
                                            pNovyOdstavec.Phrases.Add(pPhraseKonec);
                                        }
                                        if (pIndex >= 0)
                                            pIndexPocatkuSlov++;
                                        break;
                                    }
                                    string pText2 = pText.Substring(pPosledniIndex, pIndex - pPosledniIndex);
                                    MyPhrase pPhrase15 = new MyPhrase(pBegin, pEnd, pText2, pP.speakerID);
                                    pNovyOdstavec.Phrases.Add(pPhrase15);
                                    pBegin = pEnd;
                                }
                                pBegin = pFraze2.end;
                                if (pFraze.Text.Contains("rukavice"))
                                {

                                }
                                if (pFraze.Text.Contains("řikala"))
                                {

                                }
                                pNovyOdstavec.Phrases.Add(pFraze2);
                                pPosledniIndex = pIndex + pFraze2.Text.Length;

                            }
                            pIndexPocatkuSlov++;
                            if (pIndexPocatkuSlov + 1 >= pOdstavecFoneticky.Phrases.Count)
                            {
                                behat = false;
                                pStopDekodovani = true;
                                //doplneni konce odstavce
                                int pDelkaKonec = pText.Length - pPosledniIndex;
                                if (pDelkaKonec > 0)
                                {
                                    string pTextKonec = pText.Substring(pPosledniIndex);
                                    pEnd = pBegin;
                                    MyPhrase pPhraseKonec = new MyPhrase(pBegin, pEnd, pTextKonec, pP.speakerID);
                                    pNovyOdstavec.Phrases.Add(pPhraseKonec);
                                }
                            }
                        }
                        if (pNovyOdstavec.Phrases.Count > 0)
                        {
                            if (pNovyOdstavec.begin < 0) pNovyOdstavec.begin = pNovyOdstavec.Phrases[0].begin;
                            if (pNovyOdstavec.end < 0)
                            {
                                pNovyOdstavec.end = pNovyOdstavec.Phrases[pNovyOdstavec.Phrases.Count - 1].end;
                                pBegin = pNovyOdstavec.end;
                            }
                            pNovyOdstavec.trainingElement = true;
                        }
                        pP = pNovyOdstavec;
                        pSekcePuvodni.Paragraphs[i] = pP;

                    }
                }
                //ZobrazXMLData(nastaveniAplikace.RichTag);
                UpdateXMLData(true, true, true, true, true);
                return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void MSoubor_Exportovat_Click(object sender, RoutedEventArgs e)
        {
            ExportovatDokument(myDataSource, null, 0);
        }
        /// <summary>
        /// exportuje dokument do vybraneho formatu - pokud je cesta null, zavola savedialog
        /// </summary>
        /// <param name="aDokument"></param>
        /// <param name="aCesta"></param>
        /// <param name="aFormat"></param>
        /// <returns></returns>
        private int ExportovatDokument(MySubtitlesData aDokument, string aCesta, int aFormat)
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
                        FileStream fs = new FileStream(aCesta, FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("windows-1250"));
                        FileInfo fi = new FileInfo(aCesta);
                        string pNazev = fi.Name.ToUpper().Remove(fi.Name.Length - fi.Extension.Length);
                        string pHlavicka = "<" + pNazev + ";";
                        for (int i = 0; i < aDokument.SeznamMluvcich.Speakers.Count; i++)
                        {
                            if (i > 0) pHlavicka += ",";
                            pHlavicka += " " + aDokument.SeznamMluvcich.Speakers[i].FirstName;
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
                                        //pRadek += "[" + pFraze.begin.ToString() + "]" + pFraze.Text + "[" + pFraze.end.ToString() + "]";
                                        pRadek += "[" + pFraze.begin.ToString() + "]" + pFraze.Text;
                                    }
                                    sw.WriteLine(pRadek);
                                }

                            }
                        }

                        sw.Close();
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

        private void btPosunLevo_Click(object sender, RoutedEventArgs e)
        {
            timer1.IsEnabled = false;
            oVlna.mSekundyVlnyZac -= oVlna.DelkaVlnyMS / 10;
            oVlna.mSekundyVlnyKon = oVlna.mSekundyVlnyZac + oVlna.DelkaVlnyMS;
            mSekundyKonec = oVlna.mSekundyVlnyKon;
            KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, false);
            timer1.IsEnabled = true;
        }

        private void btPosunPravo_Click(object sender, RoutedEventArgs e)
        {
            timer1.IsEnabled = false;
            oVlna.mSekundyVlnyKon = oVlna.mSekundyVlnyKon + oVlna.DelkaVlnyMS / 10;
            if (oWav != null && oWav.DelkaSouboruMS < oVlna.mSekundyVlnyKon) oVlna.mSekundyVlnyKon = oWav.DelkaSouboruMS;
            oVlna.mSekundyVlnyZac = oVlna.mSekundyVlnyKon - oVlna.DelkaVlnyMS;
            mSekundyKonec = oVlna.mSekundyVlnyKon;
            KresliVlnu(oVlna.mSekundyVlnyZac, oVlna.mSekundyVlnyKon, false);
            timer1.IsEnabled = true;
        }

        private void btOdstranitNefonemy_Click(object sender, RoutedEventArgs e)
        {
            if (bFonetika == null) bFonetika = new MyFonetic(nastaveniAplikace.absolutniCestaEXEprogramu);

            bool pStav = bFonetika.OdstraneniNefonetickychZnakuZPrepisu(myDataSource, nastaveniAplikace.RichTag);
            if (pStav)
            {
                MyTag pTag = new MyTag(nastaveniAplikace.RichTag);
                pTag.tTypElementu = MyEnumTypElementu.foneticky;
                MyParagraph pP = myDataSource.VratOdstavec(pTag);
                nastaveniAplikace.CasoveZnackyText = pP.Text;
                nastaveniAplikace.CasoveZnacky = pP.VratCasoveZnackyTextu;
            }
            ZobrazitFonetickyPrepisOdstavce(nastaveniAplikace.RichTag);

        }

        private void btNacistAdresar_Click(object sender, RoutedEventArgs e)
        {

            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();

            folderDialog.Description = "vyberte adresář s přepisy a audio soubory...";
            try
            {
                DirectoryInfo di0 = new DirectoryInfo(tbDavkovyAdresar.Text);
                if (di0 != null && di0.Exists)
                {
                    folderDialog.SelectedPath = di0.FullName;
                }
            }
            catch
            {

            }


            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryInfo di = new DirectoryInfo(folderDialog.SelectedPath);
                if (di != null)
                {
                    lbDavkoveNacteni.Items.Clear();
                    tbDavkovyAdresar.Text = di.FullName;
                    FileInfo[] files = di.GetFiles("*.txt");
                    FileInfo[] filesXML = di.GetFiles("*.xml");
                    foreach (FileInfo fi in files)
                    {
                        string pNazevSouboru = fi.Name.Replace(fi.Extension, "").ToLower();
                        bool pNalezeno = false;
                        foreach (FileInfo fi2 in filesXML)
                        {
                            if (fi2.Name.ToLower().Contains(pNazevSouboru) && fi2.Name.ToLower().Contains("_phonetic"))
                            {
                                pNalezeno = true;
                                break;
                            }
                        }
                        CheckBox pChb = new CheckBox();
                        pChb.Focusable = false;
                        pChb.IsChecked = pNalezeno;
                        pChb.Content = fi.Name;
                        pChb.IsHitTestVisible = false;
                        lbDavkoveNacteni.Items.Add(pChb);
                    }

                }


            }

        }

        /// <summary>
        /// nacte polozku davkoveho zpracovani - THREAD SAFE
        /// </summary>
        /// <param name="aIndexPolozky"></param>
        private void NactiPolozkuDavkovehoZpracovani(int aIndexPolozky)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<int>(NactiPolozkuDavkovehoZpracovani),  aIndexPolozky);
                return;
            }
            if (lbDavkoveNacteni.Items.Count == 0) return;
            if (aIndexPolozky < 0 || aIndexPolozky > lbDavkoveNacteni.Items.Count - 1) aIndexPolozky = 0;
            lbDavkoveNacteni.SelectedItem = lbDavkoveNacteni.Items[aIndexPolozky];
            if (lbDavkoveNacteni.SelectedItem == null) return;
            string pStr = tbDavkovyAdresar.Text + "//" + (lbDavkoveNacteni.SelectedItem as CheckBox).Content.ToString();
            pAutomaticky = (bool)chbAutomatickyRozpoznat.IsChecked && !(bool)(lbDavkoveNacteni.SelectedItem as CheckBox).IsChecked;
            OtevritTitulky(false, pStr, true);
            if (!pAutomaticky && (bool)chbAutomatickyRozpoznat.IsChecked)
            {
                ZobrazitOknoFonetickehoPrepisu(true);
                tbFonetickyPrepis.Focus();
                btPrehratZastavit_Click(null, new RoutedEventArgs());
                //prehratVyber = true;
                //Playing = true;

            }
        }

        private void lbDavkoveNacteni_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NactiPolozkuDavkovehoZpracovani(lbDavkoveNacteni.SelectedIndex);
        }

        private void btPriraditVyber_Click(object sender, RoutedEventArgs e)
        {
            menuItemVlna1_prirad_vyber_Click(null, new RoutedEventArgs());
        }

        private void btDavkaExportHTK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i < lbDavkoveNacteni.Items.Count; i++)
                {
                    MySubtitlesData pExport = new MySubtitlesData();
                    string pJmenoSouboruXML = (lbDavkoveNacteni.Items[i] as CheckBox).Content.ToString();
                    FileInfo fi = new FileInfo(tbDavkovyAdresar.Text + "//" + pJmenoSouboruXML);
                    pJmenoSouboruXML = fi.FullName.Replace(fi.Extension, "") + "_phonetic.xml";
                    string pJmenoSouboruWAV = fi.FullName.Replace(fi.Extension, "") + ".wav";
                    FileInfo fi2 = new FileInfo(pJmenoSouboruXML);
                    if (fi2.Exists)
                    {
                        pExport = pExport.Deserializovat(pJmenoSouboruXML);
                        string prozsirit = "";
                        MyWav pWAV = new MyWav(nastaveniAplikace.absolutniCestaEXEprogramu);
                        if (pExport.Chapters[0].Sections[0].PhoneticParagraphs.Count > 1)
                        {
                            pWAV.ZacniPrevodSouboruNaDocasneWav(tbDavkovyAdresar.Text + "//" + pExport.audioFileName, "e://", 300000);
                        }

                        for (int kk = 0; kk < pExport.Chapters[0].Sections[0].PhoneticParagraphs.Count; kk++)
                        {
                            if (pExport.Chapters[0].Sections[0].PhoneticParagraphs.Count > 1)
                                prozsirit = kk.ToString();
                            MyParagraph pp = pExport.Chapters[0].Sections[0].PhoneticParagraphs[kk];
                            if (pp.trainingElement)
                            {
                                string pHTK = MyFonetic.PrevedFonetickyTextNaHTKFormat(pp.Text);
                                if (pHTK != null)
                                {

                                    string pJmenoSouboruLAB = pJmenoSouboruXML.Replace(".xml", prozsirit + ".lab");
                                    StreamWriter sw = new StreamWriter(pJmenoSouboruLAB, false, Encoding.GetEncoding(1250));
                                    sw.Write(pHTK);
                                    sw.Close();

                                    if (prozsirit == "")
                                    {
                                        pWAV.ZacniPrevodSouboruNaDocasneWav(pJmenoSouboruWAV, "e://", 60000);
                                    }
                                    if (pWAV.NactiRamecBufferu(pp.begin, pp.DelkaMS, -1))
                                    {
                                        MyBuffer16 pBuffer = new MyBuffer16(pp.DelkaMS);
                                        pBuffer.UlozDataDoBufferu(pWAV.NacitanyBufferSynchronne.data, pWAV.NacitanyBufferSynchronne.pocatecniCasMS, pWAV.NacitanyBufferSynchronne.koncovyCasMS);
                                        MyWav.VytvorWavSoubor(pBuffer,pJmenoSouboruWAV.Replace(".wav", "_phonetic" + prozsirit + ".wav"));
                                    }

                                }
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void cbJazyk_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            nastaveniAplikace.fonetickyPrepis.Jazyk = "";
            if (cbJazyk.SelectedIndex > 0) nastaveniAplikace.fonetickyPrepis.Jazyk = (cbJazyk.SelectedItem as ComboBoxItem).Content.ToString();
        }

        private void cbPohlavi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            nastaveniAplikace.fonetickyPrepis.Pohlavi = "";
            if (cbPohlavi.SelectedIndex > 0) nastaveniAplikace.fonetickyPrepis.Pohlavi = (cbPohlavi.SelectedItem as ComboBoxItem).Content.ToString();
        }

        private void chbPrehravatRozpoznane_Checked(object sender, RoutedEventArgs e)
        {
            nastaveniAplikace.fonetickyPrepis.PrehratAutomatickyRozpoznanyOdstavec = (bool)chbPrehravatRozpoznane.IsChecked;
        }


        /// <summary>
        /// automaticke foneticke rozpoznani celeho seznamu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btAutomaticky_Click_1(object sender, RoutedEventArgs e)
        {
            if (pAutomaticky2)
            {
                if (MessageBox.Show("Chcete přerušit automatickou tvorbu fonetických přepisů dávkového zpracování?", "Pozor:", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    pAutomaticky2 = false;
                    return;
                }
            }
            pAutomaticky2 = true;
            pAutomatickyIndex = lbDavkoveNacteni.SelectedIndex;
            chbAutomatickyRozpoznat.IsChecked = true;
            NactiPolozkuDavkovehoZpracovani(pAutomatickyIndex);
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
            if (nastaveniAplikace.RichFocus)
            {
                TextBox focused = nastaveniAplikace.RichTag.tSender as TextBox;
                if (focused != null)
                {

                    string insert = "[" + ((Button)sender).Content + "]";

                    string t = focused.Text; ;
                    int ix;
                    string beg;
                    string end;

                    if (focused.SelectionLength > 0)
                    {
                        ix = focused.SelectionStart;
                        beg = t.Substring(0, ix);
                        ix += focused.SelectionLength;
                        end = t.Substring(ix);
                    }
                    else
                    {
                        ix = focused.CaretIndex;
                        beg = t.Substring(0, ix);
                        end = t.Substring(ix);
                    }

                    focused.Text = beg + insert + end;
                    focused.SelectionLength = 0;
                    focused.CaretIndex = beg.Length + insert.Length;

                }
            }
        }

        #region nerecove znacky
       
        private void toolBar1_Loaded(object sender, RoutedEventArgs e)
        {
            int index = 1;
            foreach (string s in nastaveniAplikace.NerecoveUdalosti)
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
            MyTextBox.suggestions = nastaveniAplikace.NerecoveUdalosti;
            listboxpopupPopulate(nastaveniAplikace.NerecoveUdalosti);
        }


        public void listboxpopupPopulate(IEnumerable<string> blockmarks)
        {
            listboxPopup.Items.Clear();
            foreach (string s in blockmarks)
            {
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                Label lb1 = new Label();
                Label lb2 = new Label();
                lb1.FontWeight = FontWeights.Bold;
                Thickness t = lb1.Padding;
                t.Right = 0;
                lb1.Padding = t;

                t = lb2.Padding;
                t.Left = 0;
                lb2.Padding = t;

                int maxcnt = 0;
                foreach (string s2 in blockmarks)
                {
                    if (s != s2)
                    {
                        for (int i = 0; i < s.Length && i < s2.Length; i++)
                        {
                            if (s[i] != s2[i])
                            {
                                if (maxcnt < i)
                                    maxcnt = i;
                                break;
                            }
                        }
                    }
                }

                lb1.Content = s.Substring(0, maxcnt + 1);
                lb2.Content = s.Substring(maxcnt + 1);

                sp.Children.Add(lb1);
                sp.Children.Add(lb2);
                listboxPopup.Items.Add(sp);
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            popup.IsOpen = false;
        }

        private void ListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listboxPopup.SelectedItem == null)
                return;
            string value = "[" + listboxPopup.SelectedItem.ToString() + "]";
            if (value != null)
            {
                MyTextBox box = nastaveniAplikace.RichTag.tSender as MyTextBox;
                if (box != null)
                {
                    string beg;
                    string end;
                    if (box.SelectionLength > 0)
                    {
                        beg = box.Text.Substring(0, box.SelectionStart);
                        end = box.Text.Substring(box.SelectionStart + box.SelectionLength);
                    }
                    else
                    {
                        beg = box.Text.Substring(0, box.CaretIndex + 1 - popup_filter.Length);
                        end = box.Text.Substring(box.CaretIndex);
                    }

                    box.Text = beg + value + end;

                    box.CaretIndex = beg.Length + value.Length;
                    popup.IsOpen = false;
                    box.Focus();
                }

            }
        }

        string popup_filter;
        private void popup_filter_add(string s, TextCompositionEventArgs e)
        {
            popup_filter += s.ToLower();
            listboxPopup.Items.Clear();
            List<string> strings = new List<string>();
            foreach (string st in nastaveniAplikace.NerecoveUdalosti)
            {
                if (st.StartsWith(popup_filter))
                    strings.Add(st);
            }

            if (strings.Count == 0)
            {
                popup.IsOpen = false;
                listboxpopupPopulate(nastaveniAplikace.NerecoveUdalosti);
                popup_filter = "";
            }
            else if (strings.Count == 1)
            {
                popup.IsOpen = false;
                listboxPopup.Items.Add(strings[0]);
                listboxPopup.SelectedIndex = 0;
                ListBox_MouseDoubleClick(null, null);

                listboxpopupPopulate(nastaveniAplikace.NerecoveUdalosti);
                popup_filter = "";

                e.Handled = true;
                popup.IsOpen = false;
            }
            else
            {
                listboxpopupPopulate(strings);
            }
        }

        private void popup_Opened(object sender, EventArgs e)
        {
            popup_filter = "";
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
        }


        /// <summary>
        /// otevreni popup okna na aktualnim editu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void richX_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.TextComposition.Text == "/")
            {
                popup.IsOpen = true;
                e.Handled = true;
                popup_filter = "";
            }
            else if (popup.IsOpen)
            {
                popup_filter_add(e.TextComposition.Text, e);
            }
        }

        void richX_MouseDown(object sender, MouseButtonEventArgs e)
        {
            popup.IsOpen = false;
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
                myDataSource.Serializovat(ms, myDataSource, true);
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
                myDataSource = myDataSource.Deserializovat(ms);
                if (myDataSource != null)
                {
                    this.Title = MyKONST.NAZEV_PROGRAMU + " [" + myDataSource.JmenoSouboru + "]";
                    ZobrazXMLData();
                    //nacteni audio souboru pokud je k dispozici
                    if (myDataSource.audioFileName != null && myDataSource.JmenoSouboru != null)
                    {
                        FileInfo fiA = new FileInfo(myDataSource.audioFileName);
                        string pAudioFile = null;
                        if (fiA.Exists)
                        {
                            pAudioFile = fiA.FullName;
                        }
                        else
                        {
                            FileInfo fi = new FileInfo(myDataSource.JmenoSouboru);
                            pAudioFile = fi.Directory.FullName + "\\" + myDataSource.audioFileName;
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
                MyLog.LogujChybu(ex);
                MessageBox.Show("Chyba pri obnoveni stavu: " + ex.Message, "Chyba");
            }

        }
        #endregion

    }

}
