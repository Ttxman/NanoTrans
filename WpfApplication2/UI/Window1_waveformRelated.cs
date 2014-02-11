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
            ToolBar2BtnAuto.IsChecked = false;
            waveform1.Scale += 0.5;
        }

        //- vlny
        private void ToolBar2BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            ToolBar2BtnAuto.IsChecked = false;
            waveform1.Scale -= 0.5;
        }

        private void ToolBar2BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            waveform1.Scale = 1;
            if (waveform1.ScaleAutomaticaly)
            {
                waveform1.ScaleAutomaticaly = false;
                ToolBar2BtnAuto.IsChecked = false;
            }
            else
            {
                waveform1.ScaleAutomaticaly = true;
                ToolBar2BtnAuto.IsChecked = true;
            }
        }



        #region menu vlna events
        //obsluha kontextoveho menu image vlny
        private void menuItemVlna1_prirad_zacatek_Click(object sender, RoutedEventArgs e)
        {
            CommandAssignElementStart.Execute(null, null);
        }
        private void menuItemVlna1_prirad_konec_Click(object sender, RoutedEventArgs e)
        {
            CommandAssignElementEnd.Execute(null, null);
        }

        private void menuItemVlna1_prirad_vyber_Click(object sender, RoutedEventArgs e)
        {
            CommandAssignElementTimeSelection.Execute(null, null);
        }

        private void menuItemVlna1_prirad_casovou_znacku_Click(object sender, RoutedEventArgs e)
        {
            //TODO:
        }

        private void menuItemVlna1_automaticke_rozpoznavani_useku_Click(object sender, RoutedEventArgs e)
        {
            SpustRozpoznavaniVybranehoElementu(VirtualizingListBox.ActiveTransctiption, waveform1.SelectionBegin, waveform1.SelectionEnd, false);
        }
        #endregion


        private void btPriraditVyber_Click(object sender, RoutedEventArgs e)
        {
            menuItemVlna1_prirad_vyber_Click(null, new RoutedEventArgs());
        }

    }
    
}