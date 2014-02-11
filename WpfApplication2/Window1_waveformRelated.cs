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
        //+ vlny
        private void ToolBar2BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            waveform1.ScaleY += 0.5;
        }

        //- vlny
        private void ToolBar2BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            waveform1.ScaleY -= 0.5;
        }

        private void ToolBar2BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            waveform1.ScaleYAutomaticaly = true;
        }



        #region menu vlna events
        //obsluha kontextoveho menu image vlny
        private void menuItemVlna1_prirad_zacatek_Click(object sender, RoutedEventArgs e)
        {
            UpravCasZobraz(MySetup.Setup.RichTag,(long)waveform1.CaretPosition.TotalMilliseconds, -2);
        }
        private void menuItemVlna1_prirad_konec_Click(object sender, RoutedEventArgs e)
        {
           UpravCasZobraz(MySetup.Setup.RichTag, -2, (long)waveform1.CaretPosition.TotalMilliseconds);
        }

        private void menuItemVlna1_prirad_vyber_Click(object sender, RoutedEventArgs e)
        {
            UpravCasZobraz(MySetup.Setup.RichTag, (long)waveform1.SelectionBegin.TotalMilliseconds, (long)waveform1.SelectionEnd.TotalMilliseconds);
        }

        private void menuItemVlna1_prirad_casovou_znacku_Click(object sender, RoutedEventArgs e)
        {
           int pPoziceKurzoru = ((TextBox)MySetup.Setup.RichTag.tSender).SelectionStart;

            MyCasovaZnacka pCZ = new MyCasovaZnacka((long)waveform1.CaretPosition.TotalMilliseconds, pPoziceKurzoru - 1, pPoziceKurzoru);

            MyParagraph pOdstavec = myDataSource[MySetup.Setup.RichTag];
            pOdstavec.PridejCasovouZnacku(pCZ);

            MySetup.Setup.CasoveZnacky = myDataSource[MySetup.Setup.RichTag].VratCasoveZnackyTextu;
            ((TextBox)MySetup.Setup.RichTag.tSender).Text = myDataSource[MySetup.Setup.RichTag].Text;
            //vraceni kurzoru do spravne pozice


            UpdateXMLData();
            ZobrazInformaceElementu(MySetup.Setup.RichTag);

            try
            {
                ((TextBox)MySetup.Setup.RichTag.tSender).Select(pPoziceKurzoru, 0);
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
                SpustRozpoznavaniVybranehoElementu(MySetup.Setup.RichTag, (long)waveform1.SelectionBegin.TotalMilliseconds,(long)waveform1.SelectionEnd.TotalMilliseconds, false);
            }
            catch (Exception ex)
            {
                MyLog.LogujChybu(ex);
            }
        }
        #endregion


        private void btPriraditVyber_Click(object sender, RoutedEventArgs e)
        {
            menuItemVlna1_prirad_vyber_Click(null, new RoutedEventArgs());
        }


        private bool UpravCasZobraz(MyTag aTag, long aBegin, long aEnd)
        {
            return UpravCasZobraz(aTag, aBegin, aEnd, false);
        }


        private bool UpravCasZobraz(MyTag aTag, long aBegin, long aEnd, bool aIgnorovatPrekryv)
        {
            MyTag ret = myDataSource.UpravCasZobraz(aTag, aBegin, aEnd, aIgnorovatPrekryv);

            if (ret != null)
            {
                UpdateXMLData();
                ZobrazInformaceElementu(aTag);

                waveform1.SelectionBegin = TimeSpan.FromMilliseconds(myDataSource.VratCasElementuPocatek(aTag));
                waveform1.SelectionEnd = TimeSpan.FromMilliseconds(myDataSource.VratCasElementuKonec(aTag));
                return true;
            }

            return false;
        }

    }
    
}