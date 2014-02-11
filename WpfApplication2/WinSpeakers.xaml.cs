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
using System.Windows.Shapes;
using System.IO;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSpeakers.xaml
    /// </summary>
    public partial class WinSpeakers : Window
    {
        private MyParagraph bTag;
        public  MySpeaker bSpeaker;
        private MySetup bNastaveni;
        private MySpeakers bDatabazeMluvcich;
        private MySubtitlesData myDataSource;

        
        /// <summary>
        /// inicializace 
        /// </summary>
        /// <returns></returns>
        private bool Inicializace(MyParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataSource, MySpeaker aVychoziMluvci, string aPredavanaFotkaStringBase64)
        {

            this.bTag = aTag;
            this.bNastaveni = aNastaveniProgramu;
            this.bDatabazeMluvcich = aDatabazeMluvcich;
            this.myDataSource = aDataSource;
            if (this.myDataSource == null)
            {
                this.myDataSource = new MySubtitlesData();
                lbSeznamMluvcich.IsEnabled = false;
            }

            bSpeaker = new MySpeaker();
            for (int i = 0; i < myDataSource.SeznamMluvcich.Speakers.Count; i++)
            {
                lbSeznamMluvcich.Items.Add((myDataSource.SeznamMluvcich.Speakers[i]).FullName);
            }
            for (int i = 0; i < this.bDatabazeMluvcich.Speakers.Count; i++)
            {
                lbDatabazeMluvcich.Items.Add((this.bDatabazeMluvcich.Speakers[i]).FullName);
            }

            //synchronizace obrazku
            foreach (MySpeaker i in myDataSource.SeznamMluvcich.Speakers)
            {
                MySpeaker pS = bDatabazeMluvcich.NajdiSpeakeraSpeaker(i.FullName);
                if (pS.ID >= 0 && i.FotoJPGBase64 == null && pS.FotoJPGBase64 != null)
                {
                    i.FotoJPGBase64 = pS.FotoJPGBase64;
                }
            }


            if (myDataSource.VratSpeakera(bTag.speakerID).FullName != null)
            {
                bSpeaker = myDataSource.VratSpeakera(bTag.speakerID);
                lbSeznamMluvcich.SelectedItem = bSpeaker.FullName;
            }
            else if (aVychoziMluvci != null)
            {
                bSpeaker = aVychoziMluvci;
                lbSeznamMluvcich.SelectedItem = bSpeaker.FullName;
            }

            if (lbSeznamMluvcich.Items.Count > 0)
            {
                lbSeznamMluvcich.Items.MoveCurrentToPosition(lbSeznamMluvcich.SelectedIndex);
                lbSeznamMluvcich.Focus();
            }
            else if (lbDatabazeMluvcich.Items.Count > 0)
            {
                lbDatabazeMluvcich.UpdateLayout();
                lbDatabazeMluvcich.Items.MoveCurrentToPosition(0);

                bool b = lbDatabazeMluvcich.Focus();
            }
            else
            {
                lbSeznamMluvcich.Focus();
            }
            return true;

        }
        
        public WinSpeakers(MyParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataTitulky, string aFotkaBase64)
        {
            InitializeComponent();
            Inicializace(aTag, aNastaveniProgramu, aDatabazeMluvcich, aDataTitulky, null, aFotkaBase64);
            
            
            //this.Show();
        }

        public WinSpeakers(MyParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataTitulky, MySpeaker aVychoziMluvci, string aFotkaBase64)
        {
            InitializeComponent();
            Inicializace(aTag, aNastaveniProgramu, aDatabazeMluvcich, aDataTitulky, aVychoziMluvci, aFotkaBase64);
            

            //this.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
        }


        private void btSmazSpeakera_Click(object sender, RoutedEventArgs e)
        {
            if (bSpeaker != null && bSpeaker.FullName != null)
            {
                if (lbSeznamMluvcich.SelectedItem != null)
                {
                    if (MessageBox.Show("Opravdu chcete smazat vybraného mluvčího z příslušného seznamu i datové struktury, pokud je v ní přítomen?", "Upozornění:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (myDataSource.OdstranSpeakera(myDataSource.VratSpeakera(myDataSource.GetSpeaker(((string)lbSeznamMluvcich.SelectedItem)))))
                        {
                            lbSeznamMluvcich.Items.Clear();
                            //spSeznam.Children.Clear();
                            for (int i = 0; i < myDataSource.SeznamMluvcich.Speakers.Count; i++)
                            {
                                lbSeznamMluvcich.Items.Add((myDataSource.SeznamMluvcich.Speakers[i]).FullName);
                            }

                            if (myDataSource.VratSpeakera(bTag.speakerID).FullName != null)
                            {
                                bSpeaker = myDataSource.VratSpeakera(bTag.speakerID);
                                lbSeznamMluvcich.SelectedItem = bSpeaker.FullName;

                            }
                            else
                            {
                                bSpeaker = null;
                                btNovyMluvci_Click(null, new RoutedEventArgs());
                                lbSeznamMluvcich.Focus();
                            }
                        }
                    }
                }
                else if (lbDatabazeMluvcich.SelectedItem != null)
                {
                    if (MessageBox.Show("Opravdu chcete smazat vybraného mluvčího z databáze programu?", "Upozornění:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (this.bDatabazeMluvcich.OdstranSpeakera(bDatabazeMluvcich.NajdiSpeakeraSpeaker((string)lbDatabazeMluvcich.SelectedItem)))
                        {
                            lbDatabazeMluvcich.Items.Clear();
                            //spSeznam.Children.Clear();
                            for (int i = 0; i < this.bDatabazeMluvcich.Speakers.Count; i++)
                            {
                                lbDatabazeMluvcich.Items.Add((this.bDatabazeMluvcich.Speakers[i]).FullName);
                            }
                            bSpeaker = null;
                            btNovyMluvci_Click(null, new RoutedEventArgs());
                            lbDatabazeMluvcich.Focus();
                            
                        }
                    }
                }
            }
        }

        //odstrani aktualniho speakera
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //spSeznam.Children.Clear();
            bSpeaker = null;
            bTag.speakerID = new MySpeaker().ID;
            this.DialogResult = true;
            this.Close();
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!e.IsRepeat && e.Key == Key.Return)
                {
                    e.Handled = true;
                    if (lbSeznamMluvcich.SelectedItem != null)
                    {
                        bSpeaker = myDataSource.VratSpeakera(myDataSource.GetSpeaker(((string)lbSeznamMluvcich.SelectedItem)));
                        //spSeznam.Children.Clear();
                        //spSeznam.Children.Add(this.bSpeaker.speakerName);
                        //btOK.Focus();
                    }
                }
            }
            catch
            {

            }
        }

        private void lbDatabazeMluvcich_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (myDataSource.NovySpeaker(this.bDatabazeMluvcich.NajdiSpeakeraSpeaker((string)lbDatabazeMluvcich.SelectedItem)) >= 0)
            {
                lbSeznamMluvcich.Items.Add((string)lbDatabazeMluvcich.SelectedItem);
            }
           
            {
                bSpeaker = myDataSource.VratSpeakera(myDataSource.GetSpeaker(((string)lbDatabazeMluvcich.SelectedItem)));
                //spSeznam.Children.Clear();
                //spSeznam.Children.Add(this.bSpeaker.speakerName);
            }
        }

        private void lbDatabazeMluvcich_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox plb = (ListBox)sender;
                
                if (plb.SelectedItem != null)
                {
                    MySpeaker pSpeaker;
                    if (plb.Name == lbDatabazeMluvcich.Name)
                    {
                        pSpeaker = this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(plb.SelectedItem.ToString());
                    }
                    else
                    {
                        pSpeaker = myDataSource.SeznamMluvcich.NajdiSpeakeraSpeaker(plb.SelectedItem.ToString());
                    }
                    bSpeaker = new MySpeaker(pSpeaker);
                }
            }
            catch
            {

            }
        }

        private void lbDatabazeMluvcich_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lbSeznamMluvcich.SelectedItem = null;
            
        }

        private void lbSeznamMluvcich_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lbDatabazeMluvcich.SelectedItem = null;
        }

        private void lbDatabazeMluvcich_GotFocus(object sender, RoutedEventArgs e)
        {
            lbSeznamMluvcich.SelectedItem = null;
        }

        /// <summary>
        /// novy mluvci
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNovyMluvci_Click(object sender, RoutedEventArgs e)
        {
            EditSpeakerWindow ew = new EditSpeakerWindow(myDataSource, new MySpeaker());
            if (ew.ShowDialog() == true)
            {
                myDataSource.SeznamMluvcich.NovySpeaker(ew.Speaker);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                btNovyMluvci_Click(null, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F3)
            {
                btEditace_Click(null, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                btSmazSpeakera_Click(null, new RoutedEventArgs());
            }
        }

        private void lbSeznamMluvcich_GotFocus(object sender, RoutedEventArgs e)
        {
            if (lbSeznamMluvcich.Items.Count == 0)
            {
                lbDatabazeMluvcich.Focus();
            }
        }

        private void btEditace_Click(object sender, RoutedEventArgs e)
        {
            EditSpeakerWindow win = new EditSpeakerWindow(myDataSource,bSpeaker);
            if (win.ShowDialog() == false)
                return;

            MySpeaker pSpeaker = win.Speaker;
            MySpeaker pkopieSpeaker = new MySpeaker(bSpeaker);
            if (bSpeaker != null && pkopieSpeaker != null && pkopieSpeaker.FullName != null && pkopieSpeaker.FullName != "" && pSpeaker != null)
            {
                MySpeaker pSp = myDataSource.SeznamMluvcich.NajdiSpeakeraSpeaker(pkopieSpeaker.FullName);
                if (pSp != null && pSp.ID != int.MinValue)
                {
                    if (myDataSource.SeznamMluvcich.UpdatujSpeakera(pkopieSpeaker.FullName, pSpeaker))
                    {
                        lbSeznamMluvcich.Items.Clear();
                        for (int i = 0; i < myDataSource.SeznamMluvcich.Speakers.Count; i++)
                        {
                            lbSeznamMluvcich.Items.Add((myDataSource.SeznamMluvcich.Speakers[i]).FullName);
                        }
                        lbSeznamMluvcich.SelectedItem = pSpeaker.FullName;
                        bSpeaker = myDataSource.SeznamMluvcich.NajdiSpeakeraSpeaker(pSpeaker.FullName);

                    }
                }
                pSp = this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(pkopieSpeaker.FullName);
                if (pSp != null && pSp.ID >= 0)
                {
                    if (this.bDatabazeMluvcich.UpdatujSpeakera(pkopieSpeaker.FullName, pSpeaker))
                    {
                        lbDatabazeMluvcich.Items.Clear();
                        for (int i = 0; i < this.bDatabazeMluvcich.Speakers.Count; i++)
                        {
                            lbDatabazeMluvcich.Items.Add((this.bDatabazeMluvcich.Speakers[i]).FullName);
                        }
                        lbDatabazeMluvcich.SelectedItem = pSpeaker.FullName;
                        bSpeaker = this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(pSpeaker.FullName);
                    }
                }
            }
            else
            {

                if (this.bDatabazeMluvcich.NovySpeaker(pSpeaker) > -1)
                {
                    if (myDataSource.GetSpeaker(pSpeaker.FullName) != int.MinValue)
                    {
                        if (myDataSource.NovySpeaker(pSpeaker) >= 0)
                        {
                            lbSeznamMluvcich.Items.Add(pSpeaker.FullName);
                        }
                    }

                    lbDatabazeMluvcich.Items.Add(pSpeaker.FullName);
                    bSpeaker = myDataSource.VratSpeakera(myDataSource.GetSpeaker(pSpeaker.FullName));
                }
            }
        }

        private void btExterniDatabaze_Click(object sender, RoutedEventArgs e)
        {
                Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
                fileDialog.Title = "Otevřít soubor s databází mluvčích...";
                fileDialog.Filter = "Soubory mluvčích (*" + bNastaveni.PriponaDatabazeMluvcich + ")|*" + bNastaveni.PriponaDatabazeMluvcich + "|Všechny soubory (*.*)|*.*";
                try
                {
                    FileInfo fi = new FileInfo(bNastaveni.CestaDatabazeMluvcich);
                    if (fi != null && fi.Directory.Exists) fileDialog.InitialDirectory = fi.DirectoryName;
                    else fileDialog.InitialDirectory = bNastaveni.absolutniCestaEXEprogramu;
                }
                catch
                {

                }
                fileDialog.FilterIndex = 1;
                fileDialog.RestoreDirectory = true;
                if (fileDialog.ShowDialog() == true)
                {
                    MySpeakers ms = this.bDatabazeMluvcich.Deserializovat(fileDialog.FileName);
                    if (ms != null && ms.Ulozeno)
                    {
                        this.bDatabazeMluvcich = ms;
                        lbDatabazeMluvcich.Items.Clear();
                        for (int i = 0; i < this.bDatabazeMluvcich.Speakers.Count; i++)
                        {
                            lbDatabazeMluvcich.Items.Add((this.bDatabazeMluvcich.Speakers[i]).FullName);
                        }


                    }
                }
        }



        /// <summary>
        /// zavola okno s editaci mluvcich a vrati vybraneho a nastaveneho mluvciho, referencneovlivni databazi mluvcich programu
        /// </summary>
        /// <returns></returns>
        public static MySpeaker ZiskejMluvciho(MySpeakers aDatabazeMluvcich, MySpeaker aPuvodniMluvci, string aFotkaBase64)
        {
            return ZiskejMluvciho(MySetup.Setup, aDatabazeMluvcich, aPuvodniMluvci, aFotkaBase64);
        }
        /// <summary>
        /// zavola okno s editaci mluvcich a vrati vybraneho a nastaveneho mluvciho, referencneovlivni databazi mluvcich programu
        /// </summary>
        /// <returns></returns>
        public static MySpeaker ZiskejMluvciho(MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySpeaker aPuvodniMluvci, string aFotkaBase64)
        {
            try
            {
                
                MySpeaker ret = aPuvodniMluvci;
                MySubtitlesData ps = new MySubtitlesData();
                ps.NovySpeaker(aPuvodniMluvci);
                WinSpeakers ws = new WinSpeakers(new MyParagraph(), aNastaveniProgramu, aDatabazeMluvcich, ps, aPuvodniMluvci, aFotkaBase64);
                ws.ShowDialog(); 
                if (ws.DialogResult == true)
                {
                    ret = ws.bSpeaker;
                }

                return ret;
            }
            catch (Exception)
            {
                return new MySpeaker();
            }
        }



        private void btExterniDatabazeUlozit_Click(object sender, RoutedEventArgs e)
        {

                Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();
                fileDialog.Title = "Uložit databází mluvčích...";
                fileDialog.Filter = "Soubory mluvčích (*" + bNastaveni.PriponaDatabazeMluvcich + ")|*" + bNastaveni.PriponaDatabazeMluvcich + "|Všechny soubory (*.*)|*.*";
                //fileDialog.FileName = "SeznamMluvcich";
                try
                {
                    FileInfo fi = new FileInfo(bNastaveni.CestaDatabazeMluvcich);
                    if (fi != null && fi.Directory.Exists) fileDialog.InitialDirectory = fi.DirectoryName;
                    else fileDialog.InitialDirectory = bNastaveni.absolutniCestaEXEprogramu;
                }
                catch
                {

                }
                fileDialog.FilterIndex = 1;
                fileDialog.RestoreDirectory = true;
                if (fileDialog.ShowDialog() == true)
                {
                    if (this.bDatabazeMluvcich.Serializovat(fileDialog.FileName, this.bDatabazeMluvcich))
                    {

                    }
                }

        }

        private void btSynchronizovat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (MySpeaker i in myDataSource.SeznamMluvcich.Speakers)
                {
                    if (bDatabazeMluvcich.NajdiSpeakeraID(i.FullName)<0 &&  bDatabazeMluvcich.NovySpeaker(i) >= 0)
                    {
                        lbDatabazeMluvcich.Items.Add(i.FullName);
                    }
                }

            }
            catch
            {

            }
        }

        private void tbHledani_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                lbDatabazeMluvcich.Focus();
            }
        }

    }
}
