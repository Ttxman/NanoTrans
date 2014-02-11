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
        Key HIDsystemkey = Key.None;
        Key HIDkey = Key.None;
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
                                HIDkey = Key.System;
                                HIDsystemkey = Key.Left;
                                Window_PreviewKeyDown(null, null);
                                waveform1.Dispatcher.Invoke(new KeyEventHandler(Window_PreviewKeyDown), null, null);
                            }
                        }
                        else if ((((byte)FCPedal.Middle) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Middle & FCstatus) == 0) //down event
                            {
                                HIDkey = Key.Tab;
                                HIDsystemkey = Key.None;
                                waveform1.Dispatcher.Invoke(new KeyEventHandler(Window_PreviewKeyDown), null, null);

                            }

                        }
                        else if ((((byte)FCPedal.Right) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Right & FCstatus) == 0) //down event
                            {
                                HIDkey = Key.System;
                                HIDsystemkey = Key.Right;
                                waveform1.Dispatcher.Invoke(new KeyEventHandler(Window_PreviewKeyDown), null, null);
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
                        return;


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
                            if (pocatek + 20 >= waveform1.CarretPosition.TotalMilliseconds) //minuly elment nema konec
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
                            if (UpravCasZobraz(x, -2,(long) waveform1.CarretPosition.TotalMilliseconds))
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
                        if (waveform1.CarretPosition < TimeSpan.Zero)
                            return;

                        MyTag pMT = PridejOdstavec(x.tKapitola, x.tSekce, "", null, -2,(long) waveform1.CarretPosition.TotalMilliseconds, -1, new MySpeaker());
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
                                
                                waveform1.SelectionBegin = TimeSpan.FromMilliseconds(myDataSource.VratCasElementuPocatek(mT));
                                waveform1.SelectionEnd = TimeSpan.FromMilliseconds(myDataSource.VratCasElementuKonec(mT));

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

        private void waveform1_PlayPauseClick(object sender, RoutedEventArgs e)
        {
            //simulace klavesy, kdyz je obsluha v klavese
            HIDkey = Key.Tab;
            this.Dispatcher.Invoke(new KeyEventHandler(Window_PreviewKeyDown), null, null);
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
                    NastavPoziciKurzoru(waveform1.CarretPosition - waveform1.SmallJump, true, true);
                    if (e != null)
                        e.Handled = true;
                    break;
                case Key.Right:
                    NastavPoziciKurzoru(waveform1.CarretPosition - waveform1.SmallJump, true, true);
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
                            Playing = false;
                            if (MWP != null)
                            {

                                //TODO: proc enjsou tyhle 3 veci na jednom miste?
                                waveform1.CarretPosition = MWP.PausedAt;
                                pIndexBufferuVlnyProPrehrani = (int)MWP.PausedAt.TotalMilliseconds;
                            }
                        }
                        else
                        {

                            bool adjustspeed = false;
                            if (leftShift || ToolBar2BtnSlow.IsChecked == true)
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
                                if (waveform1.CarretPosition >= TimeSpan.Zero)
                                {

                                    long timems;
                                    if (waveform1.CarretPosition >= waveform1.SelectionBegin && waveform1.CarretPosition <= waveform1.SelectionEnd)
                                    {
                                        timems = (long)waveform1.CarretPosition.TotalMilliseconds;
                                        oldms = TimeSpan.Zero;
                                        List<MyTag> elementy = myDataSource.VratElementDanehoCasu(timems, null);
                                        NastavPoziciKurzoru(waveform1.SelectionBegin, true, false);
                                    }
                                    else
                                    {
                                        oldms = TimeSpan.Zero;
                                        int lastc = myDataSource.Chapters.Count - 1;
                                        int lasts = myDataSource.Chapters[lastc].Sections.Count - 1;
                                        int lastp = myDataSource.Chapters[lastc].Sections[lasts].Paragraphs.Count - 1;


                                        long konec = myDataSource.VratCasElementuPocatek(new MyTag(lastc, lasts, lastp)) + 5;
                                        NastavPoziciKurzoru(TimeSpan.FromMilliseconds(konec), true, true);
                                        waveform1.SelectionBegin = TimeSpan.FromMilliseconds(konec);
                                        waveform1.SelectionEnd = TimeSpan.FromMilliseconds(konec + 120000);
                                    }
                                }
                            }
                            if (jeVideo) meVideo.Play();
                            //spusteni prehravani pomoci tlacitka-kvuli nacteni primeho prehravani

                            Playing = true;

                            if (adjustspeed)
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

                    case 1000:
                        Audio_PlayPause();
                        break;
                    case 1001:
                        Audio_PlayPause();
                        break;
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