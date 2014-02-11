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
using dbg = System.Diagnostics.Debug;


namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window
    {
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


        bool slPoziceMedia_mouseDown = false; //promenna pro detekci stisknuti mysi na posuvniku videa
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
                        meVideo.Play();
                    else
                        meVideo.Stop();
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
                    waveform1.SubtitlesData = myDataSource;
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
                            MyParagraph pP = myDataSource[aTag];
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
                        waveform1.InvalidateSpeakers();
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
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }

        }

        #region kapitoly, sekce odstavce

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
                index = PridejTextBox(index, myDataSource[mT].Text, mT); //textbox do seznamu


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

        #endregion

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

                waveform1.ContextMenu = ContextMenuVlnaImage;

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
                oWav.HaveData += oWav_HaveData;
                oWav.HaveFileNumber += oWav_HaveFileNumber;
                oWav.TemporaryWavesDone += new EventHandler(oWav_TemporaryWavesDone);

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
        short[] WOP_ChciData(out int zacatekbufferums)
        {
            zacatekbufferums = -1;
            try
            {
                if (MWP != null)
                {
                    if (_playing && oWav != null && oWav.Nacteno)
                    {
                        long pOmezeniMS = -1;
                        if (prehratVyber)
                        {
                            pOmezeniMS = (long)waveform1.SelectionEnd.TotalMilliseconds;
                        }

                        short[] bfr = waveform1.GetAudioData(TimeSpan.FromMilliseconds(pIndexBufferuVlnyProPrehrani), TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(pOmezeniMS));
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
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
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
        /// metoda vracejici data z threadu, v argumentu e
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
                    waveform1.SetAudioData(me.data, TimeSpan.FromMilliseconds(me.pocatecniCasMS), TimeSpan.FromMilliseconds(me.koncovyCasMS));
                    if (me.pocatecniCasMS == 0)
                    {
                        if (!timer1.IsEnabled) InitializeTimer();
                        if (waveform1.WaveLength < TimeSpan.FromSeconds(30))
                        {
                            waveform1.WaveLength = TimeSpan.FromSeconds(30);
                        }
                        if (pNacitaniAudiaDavka && oWav.Nacteno)
                        {
                            MyTag pTag0 = new MyTag(0, 0, 0);
                            if (myDataSource.VratCasElementuKonec(pTag0) < 0)
                                myDataSource.UpravCasElementu(pTag0, -2, oWav.DelkaSouboruMS);
                            UpdateXMLData();
                            //spSeznam.UpdateLayout();
                            VyberElement(pTag0, true);
                            pNacitaniAudiaDavka = false;
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

                waveform1.Invalidate();
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }



        }

        //TODO: tohle odhadem moc fungovat nebude... opravit
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

            richX.HorizontalAlignment = HorizontalAlignment.Stretch;
            richX.Tag = new MyTag(aTag.tKapitola, aTag.tSekce, aTag.tOdstavec, richX);
            richX.TextChanged += new TextChangedEventHandler(RichText_TextChanged);
            richX.PreviewKeyDown += new KeyEventHandler(richX_PreviewKeyDown);
            richX.PreviewTextInput += new TextCompositionEventHandler(richX_PreviewTextInput);

            richX.LostFocus += new RoutedEventHandler(richX_LostFocus);
            richX.GotFocus += new RoutedEventHandler(richX_GotFocus);

            richX.PreviewMouseUp += new MouseButtonEventHandler(richX_PreviewMouseUp);
            richX.PreviewMouseDown += new MouseButtonEventHandler(richX_PreviewMouseDown);

            richX.SelectionChanged += new RoutedEventHandler(richX_SelectionChanged);
            richX.MouseDown += new MouseButtonEventHandler(richX_MouseDown);
            richX.CaretPositionJump += new EventHandler<MyTextBox.IntEventArgs>(richX_CaretPositionJump);

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
                richX.Margin = new Thickness(defaultLeftPositionRichX + 8, 0, 0, 0);
                //attrs.Margin = new Thickness(defaultLeftPositionRichX - 15, 0, 0, 0);
                //circleStartX.Margin = new Thickness(defaultLeftPositionRichX - 15, 0, 0, 0);
                buttonX.Visibility = Visibility.Collapsed;
            }
            else if (aTag.tOdstavec < 0 && aTag.tSekce > -1)
            {
                richX.Background = Brushes.LightGreen;
                richX.Margin = new Thickness(defaultLeftPositionRichX + 15 + 8, 0, 0, 0);
                // circleStartX.Margin = new Thickness(defaultLeftPositionRichX + 2, 0, 0, 0);
                //attrs.Margin = new Thickness(defaultLeftPositionRichX + 2, 0, 0, 0);

                buttonX.Visibility = Visibility.Visible;
            }
            else
            {
                richX.Margin = new Thickness(defaultLeftPositionRichX + 35 + 8, 0, 0, 0);
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
                        MyParagraph par = myDataSource[aTag];
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
                gridX.Children.Add(new Ellipse() { Width = 10, Height = 10, Visibility = System.Windows.Visibility.Hidden });


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

        void richX_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            
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

        void richX_CaretPositionJump(object sender, MyTextBox.IntEventArgs e)
        {
            MyTag tag = (MyTag)(sender as MyTextBox).Tag;


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
            int i = ((sender as Rectangle).Parent as StackPanel).Children.IndexOf(sender as UIElement) + 1;
            MyEnumParagraphAttributes[] attrs = (MyEnumParagraphAttributes[])Enum.GetValues(typeof(MyEnumParagraphAttributes));

            MyTag tag = (MyTag)(sender as Rectangle).Tag;
            MyParagraph par = myDataSource[tag];

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

        void gridX_MouseUp(object sender, MouseButtonEventArgs e)
        {

            bool cc = ((TextBox)((Grid)sender).Children[0]).Focus();
        }

        #region RclickMEnu

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
                MyTag mT;// = ((MyTag)((RichTextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                UpravCasZobraz(mT, -1, -2);
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
                MyTag mT;// = ((MyTag)((RichTextBox)ss.Children[0]).Tag);
                mT = nastaveniAplikace.RichTag;
                UpravCasZobraz(mT, -2, -1);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }

        static Encoding win1250 = Encoding.GetEncoding("windows-1250");

        void menuItemX7_Click(object sender, RoutedEventArgs e)
        {
            MyTag tag = new MyTag(nastaveniAplikace.RichTag);
            MyParagraph par = myDataSource[tag];
            tag.tTypElementu = MyEnumTypElementu.foneticky;
            MyParagraph parf = myDataSource[tag];
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

                File.WriteAllBytes(textf, win1250.GetBytes(par.Text));


                if (parf != null && !string.IsNullOrEmpty(parf.Text))
                {
                    textf = filename + ".phn";
                    File.WriteAllBytes(textf, win1250.GetBytes(parf.Text));
                }
            }

        }
        #endregion

        /// <summary>
        /// otevira okno mluvcich
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        #region richX  - textboxy...


        void richX_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                skocitNaPoziciSlovaTextboxu = true;
                TextBox pTB = ((TextBox)sender);

                pPocatecniIndexVyberu = spSeznam.Children.IndexOf((Grid)((TextBox)(sender)).Parent);

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
                        List<MyCasovaZnacka> pCZnacky = myDataSource[pTag].VratCasoveZnackyTextu;


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
                            NastavPoziciKurzoru(TimeSpan.FromMilliseconds(pTime), true, true);
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
                    MyParagraph pP = myDataSource[nastaveniAplikace.RichTag];
                    nastaveniAplikace.CasoveZnacky = pP.VratCasoveZnackyTextu;    //casove znacky, ktere jsou nastaveny v odstavci
                    nastaveniAplikace.CasoveZnackyText = ((TextBox)sender).Text;

                }

                //provede zvyrazneni vyberu ve vlne podle dat
                waveform1.CaretPosition = waveform1.SelectionBegin = TimeSpan.FromMilliseconds(myDataSource.VratCasElementuPocatek(nastaveniAplikace.RichTag));
                waveform1.SelectionEnd = TimeSpan.FromMilliseconds(myDataSource.VratCasElementuKonec(nastaveniAplikace.RichTag));

                //waveform1.SliderPostion = waveform1.SelectionEnd;

                //nastaveni pozice kurzoru a obsluha prehravani podle nastaveni
                if (nastaveniAplikace.SetupSkocitNaPozici && !pNeskakatNaZacatekElementu)
                {
                    waveform1.CaretPosition = waveform1.SelectionBegin;
                    pIndexBufferuVlnyProPrehrani = (int)waveform1.CaretPosition.TotalMilliseconds;
                    if (nastaveniAplikace.SetupSkocitZastavit)
                    {
                        if (jeVideo) meVideo.Play();
                        Playing = false;
                    }
                }
                if (pNeskakatNaZacatekElementu) pNeskakatNaZacatekElementu = false;
                if (nastaveniAplikace.RichTag.JeOdstavec)
                {
                    if (!Playing)
                        NastavPoziciKurzoru(waveform1.CaretPosition, true, true);
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
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
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
                        MyParagraph pP = myDataSource[pTag];
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
                            MyParagraph pP = myDataSource[pTag];
                            int carretpos = (pTb.SelectionLength <= 0) ? pTb.CaretIndex : pTb.SelectionStart;
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




        TimeSpan oldms = TimeSpan.Zero;

        //TODO: spatna zla a oskiliva metoda
        public void NastavPoziciKurzoru(TimeSpan position, bool nastavitMedia, bool aNeskakatNaZacatekElementu)
        {


            StackTrace st = new StackTrace(true);
            string trace = "";
            foreach (var frame in st.GetFrames())
            {
                trace += frame.GetMethod().Name + frame.GetFileLineNumber() + ">";
            }


            try
            {

                if (position < TimeSpan.Zero) return;
                waveform1.CaretPosition = position;
                if (!Playing)
                    oldms = TimeSpan.Zero;

                if (!Playing && jeVideo && Math.Abs(meVideo.Position.TotalMilliseconds) > 200)
                {
                    meVideo.Position = waveform1.CaretPosition;
                }

                if (nastavitMedia)
                {
                    //pIndexBufferuVlnyProPrehrani = (int)position.TotalMilliseconds;
                    if (jeVideo) meVideo.Position = waveform1.CaretPosition;
                    List<MyTag> pTagy = myDataSource.VratElementDanehoCasu((long)position.TotalMilliseconds, null);
                    for (int i = 0; i < pTagy.Count; i++)
                    {
                        if (pTagy[i].tKapitola == nastaveniAplikace.RichTag.tKapitola && pTagy[i].tSekce == nastaveniAplikace.RichTag.tSekce && pTagy[i].tOdstavec == nastaveniAplikace.RichTag.tOdstavec)
                        {
                            return;
                        }
                    }

                    if (pTagy.Count > 0 && !prehratVyber) // nechceme kazdy pruchod chybu...
                    {
                        VyberElement(pTagy[0], aNeskakatNaZacatekElementu);
                    }
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

        private void VyberFonetikuMeziCasovymiZnackami(long aPoziceKurzoru)
        {
            try
            {
                MyTag pTag = new MyTag(nastaveniAplikace.RichTag);
                pTag.tTypElementu = MyEnumTypElementu.foneticky;
                
                List<MyTag> pTagy = myDataSource.VratElementDanehoCasu(aPoziceKurzoru, pTag);
                if (pTagy != null && pTagy.Count > 0)
                {
                    pTag = pTagy[0];
                }
                if (pTag != null && !prehratVyber)
                {
                    VyberElement(pTag, true);
                }


                if (nastaveniAplikace.RichTag != null)
                {
                    MyParagraph pP = myDataSource[pTag];
                    List<MyCasovaZnacka> pCasZnacky = null;
                    if (pP != null) pCasZnacky = pP.VratCasoveZnackyTextu;
                    if (pCasZnacky != null && pCasZnacky.Count > 1)
                    {
                        TextBox pRTB = tbFonetickyPrepis;

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
                if (pTag != null && !prehratVyber)
                {
                    VyberElement(pTag, true);
                }


                if (nastaveniAplikace.RichTag != null)
                {
                    MyParagraph pP = myDataSource[nastaveniAplikace.RichTag];
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
            try
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
                    waveform1.CaretPosition = MWP.PlayPosition;
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

                    if (Playing) VyberTextMeziCasovymiZnackami((long)playpos.TotalMilliseconds);
                }

                long pPozadovanyPocatekVlny = (long)waveform1.WaveBegin.TotalMilliseconds;

                long celkMilisekundy = (long)playpos.TotalMilliseconds;
                if (playpos.TotalMilliseconds >= mSekundyKonec - waveform1.WaveLengthDelta.TotalMilliseconds && mSekundyKonec < oWav.DelkaSouboruMS && !nahled)
                {

                    if (celkMilisekundy < oWav.DelkaSouboruMS)// && celkMilisekundy < waveform1.AudioBuffer.KonecMS)
                    {
                        long pKonec = celkMilisekundy - (long)waveform1.WaveLengthDelta.TotalMilliseconds + rozdil;
                        long pPocatekMS = pKonec - rozdil;
                        if (pKonec > waveform1.WaveEnd.TotalMilliseconds)
                            pPocatekMS = (long)waveform1.CaretPosition.TotalMilliseconds - rozdil / 2;

                        if (pPocatekMS < 0)
                            pPocatekMS = 0;
                        pKonec = pPocatekMS + rozdil;
                        mSekundyKonec = pKonec;
                        waveform1.WaveBegin = TimeSpan.FromMilliseconds(pPocatekMS);
                        waveform1.WaveEnd = TimeSpan.FromMilliseconds(mSekundyKonec);
                    }
                }
                else if (playpos < waveform1.WaveBegin && waveform1.WaveBegin >= TimeSpan.Zero && !nahled)
                {
                    if (celkMilisekundy < oWav.DelkaSouboruMS && celkMilisekundy < waveform1.AudioBufferEnd.TotalMilliseconds && celkMilisekundy >= waveform1.AudioBufferBegin.TotalMilliseconds)
                    {
                        long pKonec = celkMilisekundy + (long)(rozdil * 0.3);
                        long pPocatekMS = pKonec - rozdil;
                        if (pKonec > waveform1.WaveEnd.TotalMilliseconds)
                            pPocatekMS = (long)waveform1.CaretPosition.TotalMilliseconds - rozdil / 2;

                        if (pPocatekMS < 0)
                            pPocatekMS = 0;
                        pKonec = pPocatekMS + rozdil;
                        mSekundyKonec = pKonec;
                        waveform1.WaveBegin = TimeSpan.FromMilliseconds(pPocatekMS);
                        waveform1.WaveEnd = TimeSpan.FromMilliseconds(mSekundyKonec);
                    }

                }
                else if (slPoziceMedia_mouseDown == false && nahled == true)
                {
                    nahled = false;
                }

                if (oWav.Nacteno && celkMilisekundy > waveform1.AudioBufferEnd.TotalMilliseconds - (waveform1.AudioBufferEnd - waveform1.AudioBufferBegin).TotalMilliseconds * 0.2)
                {
                    if (!oWav.NacitaniBufferu && !nahled) //pokud jiz neni nacitano vlakno,dojde k inicializaci threadu
                    {
                        if (waveform1.AudioBufferEnd.TotalMilliseconds < oWav.DelkaSouboruMS && !oWav.NacitaniBufferu)
                        {
                            oWav.AsynchronniNacteniRamce2((long)(celkMilisekundy - (waveform1.AudioBufferEnd - waveform1.AudioBufferBegin).TotalMilliseconds * 0.3), MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS, 0);
                        }
                    }
                }
                else if (oWav.Nacteno && (waveform1.AudioBufferBegin > TimeSpan.Zero && celkMilisekundy < waveform1.AudioBufferBegin.TotalMilliseconds + (waveform1.AudioBufferEnd - waveform1.AudioBufferBegin).TotalMilliseconds * 0.2) || pPozadovanyPocatekVlny < waveform1.AudioBufferBegin.TotalMilliseconds)
                {
                    if (!oWav.NacitaniBufferu && !nahled) //pokud jiz neni nacitano vlakno,dojde k inicializaci threadu
                    {
                        oWav.AsynchronniNacteniRamce2((long)(celkMilisekundy - (waveform1.AudioBufferEnd - waveform1.AudioBufferBegin).TotalMilliseconds * 0.6), MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
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

                        waveform1.CaretPosition = TimeSpan.Zero;
                        //  pIndexBufferuVlnyProPrehrani = 0;


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
                this.Dispatcher.Invoke(new DelegatVyberElementu(VyberElement), aTagVyberu, aNezastavovatPrehravani);

                return false;
            }
            bool pStav = nastaveniAplikace.SetupSkocitZastavit;
            try
            {

                if (aTagVyberu == null) return false;
                long pPocatekMS = myDataSource.VratCasElementuPocatek(aTagVyberu);
                long pKonecMS = myDataSource.VratCasElementuKonec(aTagVyberu);
                aTagVyberu.tSender = VratSenderTextboxu(aTagVyberu);


                waveform1.SelectionBegin = TimeSpan.FromMilliseconds(pPocatekMS);
                waveform1.SelectionEnd = TimeSpan.FromMilliseconds(pKonecMS);

                if (aTagVyberu.tOdstavec != nastaveniAplikace.RichTag.tOdstavec || aTagVyberu.tSekce != nastaveniAplikace.RichTag.tSekce || aTagVyberu.tKapitola != nastaveniAplikace.RichTag.tKapitola)
                {


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
                MWP = new MyWavePlayer(nastaveniAplikace.audio.VystupniZarizeniIndex, 4800, WOP_ChciData);
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
            waveform1.SmallJump = TimeSpan.FromSeconds(nastaveniAplikace.VlnaMalySkok);
            InicializaceAudioPrehravace();  //nove nastaveni prehravaciho zarizeni 

            UpdateXMLData();    //zobrazeni xml dat v pripade zmeny velikosti pisma
            UpdateXMLData();
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

                        if (oknoNapovedy != null && !oknoNapovedy.IsLoaded)
                        {
                            oknoNapovedy.Close();
                        }


                    }
                    else if (mbr == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;

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


                //smazani souboru...

                foreach (string f in Directory.GetFiles(MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU))
                {
                    File.Delete(f);
                }

                Directory.Delete(MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU);
                TempCheckMutex.Close();

                Environment.Exit(0); // Vynuti ukonceni vsech vlaken a uvolneni prostredku

            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);

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



        //pokusi se zamerit textbox pri spousteni aplikace
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            InitCommands();
            m_findDialog = new FindDialog(this);

            //inicializuje (asynchronni) nacitani slovniku

            Thread t = new Thread(
                delegate()
                {
                    if (MyTextBox.LoadVocabulary(nastaveniAplikace.absolutniCestaEXEprogramu + MyKONST.CESTA_SLOVNIK_SPELLCHECK))
                    {
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
                }
                ) { Name = "Spellchecking_Load"};
            t.Start();

            // foneticky prepis musi dostat oznaceni jinak pak nejsou videt zmeny kdyz neam focus (pri prehravani)
            tbFonetickyPrepis.LostFocus += new RoutedEventHandler(tbFonetickyPrepis_LostFocus);

            tbFonetickyPrepis.Text = " ";
            tbFonetickyPrepis.Focus();
            tbFonetickyPrepis.SelectionStart = 0;
            tbFonetickyPrepis.SelectionLength = 1;
            //refresh uz vykreslenych textboxu
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

            string foldername = System.IO.Path.GetRandomFileName();
            string temppath = System.IO.Path.GetTempPath() + "NanoTrans\\";

            Directory.CreateDirectory(temppath);
            DeleteUnusedTempFolders(temppath);
            temppath = temppath + foldername;
            Directory.CreateDirectory(temppath);
            TempCheckMutex = new Mutex(true, "NanoTransMutex_" + foldername);
            MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU = temppath + "\\";
        }

        void tbFonetickyPrepis_LostFocus(object sender, RoutedEventArgs e)
        {
           e.Handled = true;
        }
        private static Mutex TempCheckMutex;


        private void DeleteUnusedTempFolders(string foldername)
        {
            DirectoryInfo di = new DirectoryInfo(foldername);
            DirectoryInfo[] dirs = di.GetDirectories();

            foreach (DirectoryInfo dir in dirs)
            {
                try
                {
                    //ziskal sem nezaregistrovany pristup ke slozce.. smazat
                    bool isnew;
                    using (Mutex m = new Mutex(true, "NanoTransMutex_" + dir.Name, out isnew))
                    {
                        if (isnew)
                        {
                            foreach (var f in dir.GetFiles())
                            {
                                f.Delete();
                            }

                            dir.Delete();
                        }

                    }
                }
                catch
                { }

            }


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



                            MyParagraph pP = myDataSource[e.sender.PrepisovanyElementTag];
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
                            MyParagraph pP = myDataSource[e.sender.PrepisovanyElementTag];
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
                this.Dispatcher.Invoke(new Action<string>(ZobrazStavProgramu), aZprava);
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
                MyParagraph pParagraph = myDataSource[aTag];
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
                tbFonetickyPrepis.Text = mf.VratFonetickyPrepis(myDataSource[nastaveniAplikace.RichTag].Text);
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
                MyParagraph pP = myDataSource[pTag];
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
                    pSeznamKNormalizaci.Add(aDokument[aElement]);
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
                    Audio_PlayPause();


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
                    pOdstavec = pDokumentFonetickehoPrepisu[pTag];
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
                MyParagraph pParagraph = aDokument[aTag];
                aTag.tTypElementu = MyEnumTypElementu.foneticky;
                MyParagraph pPhoneticPar = aDokument[aTag];

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
            MyParagraph pOdstavec = aDokument[aTag];
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
                MyParagraph pp = myDataSource[nastaveniAplikace.RichTag];
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
                MyParagraph pOdstavecFoneticky = pDokumentFonetickehoPrepisu[aTagOdstavecFonetickyDokument];
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

        private void btOdstranitNefonemy_Click(object sender, RoutedEventArgs e)
        {
            if (bFonetika == null) bFonetika = new MyFonetic(nastaveniAplikace.absolutniCestaEXEprogramu);

            bool pStav = bFonetika.OdstraneniNefonetickychZnakuZPrepisu(myDataSource, nastaveniAplikace.RichTag);
            if (pStav)
            {
                MyTag pTag = new MyTag(nastaveniAplikace.RichTag);
                pTag.tTypElementu = MyEnumTypElementu.foneticky;
                MyParagraph pP = myDataSource[pTag];
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
                this.Dispatcher.Invoke(new Action<int>(NactiPolozkuDavkovehoZpracovani), aIndexPolozky);
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
                Audio_PlayPause();
                //prehratVyber = true;
                //Playing = true;

            }
        }

        private void lbDavkoveNacteni_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NactiPolozkuDavkovehoZpracovani(lbDavkoveNacteni.SelectedIndex);
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
                                        MyWav.VytvorWavSoubor(pBuffer, pJmenoSouboruWAV.Replace(".wav", "_phonetic" + prozsirit + ".wav"));
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

        private void waveform1_SliderPositionChanged(object sender, Waveform.TimeSpanEventArgs e)
        {
            if (!Playing)
            {
                TimeSpan ts = e.Value;
                pIndexBufferuVlnyProPrehrani = (int)ts.TotalMilliseconds;
                if (jeVideo) meVideo.Position = ts;
            }
        }

        private void waveform1_UpdateBegin(object sender, EventArgs e)
        {
        }

        private void waveform1_UpdateEnd(object sender, EventArgs e)
        {
        }

        private void waveform1_CarretPostionChangedByUser(object sender, Waveform.TimeSpanEventArgs e)
        {
            if (Playing)
            {
                Playing = false;
            }
            pIndexBufferuVlnyProPrehrani = (int)waveform1.CaretPosition.TotalMilliseconds;

            bool fixVisible = true;
            List<MyTag> tags = myDataSource.VratElementDanehoCasu(pIndexBufferuVlnyProPrehrani,null);

            for (int i = 0; i < spSeznam.Children.Count && fixVisible; i++)
            {
                MyTag tg = ((spSeznam.Children[i] as Grid).Children[0] as TextBox).Tag as MyTag;
                MyParagraph p = myDataSource[tg];

                foreach (MyTag mt in tags)
                {
                    MyParagraph mp = myDataSource[mt];
                    if (p != null && p.begin == mp.begin && p.end == mp.end)
                    {
                        fixVisible = false;
                        ((spSeznam.Children[i] as Grid).Children[0] as TextBox).Focus();
                        break;
                    }
                }
            }

            if (fixVisible)
                ZobrazXMLData();


        }

        private void waveform1_ParagraphClick(object sender, Waveform.MyTagEventArgs e)
        {
            bool pVybran = VyberElement(e.Value, false);
        }

        private void waveform1_ParagraphDoubleClick(object sender, Waveform.MyTagEventArgs e)
        {
            VyberElement(e.Value, false);
            new WinSpeakers(e.Value, this.nastaveniAplikace, this.myDatabazeMluvcich, myDataSource, null).ShowDialog();
            UpdateXMLData(false, true, true, false, true);
        }

        private void waveform1_CarretPostionChanged(object sender, Waveform.TimeSpanEventArgs e)
        {
            if (!Playing)
            {
                pIndexBufferuVlnyProPrehrani = (int)e.Value.TotalMilliseconds;
                oldms = TimeSpan.Zero;
            }
        }

        private void waveform1_ElementChanged(object sender, Waveform.MyTagEventArgs e)
        {
            UpdateXMLData(false, true, false, false, true);
            ZobrazInformaceElementu(nastaveniAplikace.RichTag);
        }

        private void waveform1_SelectionChanged(object sender, EventArgs e)
        {
            ZobrazInformaceVyberu();
        }
    }
}