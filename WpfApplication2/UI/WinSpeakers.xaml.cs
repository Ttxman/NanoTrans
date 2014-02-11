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
using System.Windows.Shapes;
using System.IO;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSpeakers.xaml
    /// </summary>
    public partial class WinSpeakers : Window
    {
        private TranscriptionParagraph bTag;
        public  Speaker bSpeaker;
        private MySetup bNastaveni;
        private MySpeakers bDatabazeMluvcich;
        private Transcription myDataSource;
        
        /// <summary>
        /// inicializace 
        /// </summary>
        /// <returns></returns>
        private bool Inicializace(TranscriptionParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, Transcription aDataSource, Speaker aVychoziMluvci)
        {

            this.bTag = aTag;
            this.bNastaveni = aNastaveniProgramu;
            this.bDatabazeMluvcich = aDatabazeMluvcich;
            this.myDataSource = aDataSource;
            if (this.myDataSource == null)
            {
                this.myDataSource = new Transcription();
                lbSeznamMluvcich.IsEnabled = false;
            }


            Updatebindings();
            return true;

            
        }

        private void Updatebindings()
        {
            lbDatabazeMluvcich.ItemsSource = null;
            lbDatabazeMluvcich.ItemsSource = bDatabazeMluvcich.Speakers;
            lbSeznamMluvcich.ItemsSource = null;
            lbSeznamMluvcich.ItemsSource = myDataSource.Speakers.Speakers;
        }
        
        public WinSpeakers(TranscriptionParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, Transcription aDataTitulky)
        {
            InitializeComponent();
            Inicializace(aTag, aNastaveniProgramu, aDatabazeMluvcich, aDataTitulky,null);
        }

        public WinSpeakers(TranscriptionParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, Transcription aDataTitulky, Speaker aVychoziMluvci)
        {
            InitializeComponent();
            Inicializace(aTag, aNastaveniProgramu, aDatabazeMluvcich, aDataTitulky, aVychoziMluvci);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Updatebindings();
        }


        private void btSmazSpeakera_Click(object sender, RoutedEventArgs e)
        {
            if (bSpeaker != null && bSpeaker.FullName != null)
            {
                if (lbSeznamMluvcich.SelectedItem != null)
                {
                    if (MessageBox.Show("Opravdu chcete smazat vybraného mluvčího z příslušného seznamu i datové struktury, pokud je v ní přítomen?", "Upozornění:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (myDataSource.OdstranSpeakera(lbSeznamMluvcich.SelectedItem as Speaker))
                        {
                            lbSeznamMluvcich.Focus();
                            Updatebindings();
                        }
                    }
                }
                else if (lbDatabazeMluvcich.SelectedItem != null)
                {
                    if (MessageBox.Show("Opravdu chcete smazat vybraného mluvčího z databáze programu?", "Upozornění:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (lbDatabazeMluvcich.SelectedItem != null)
                        {
                            if (this.bDatabazeMluvcich.OdstranSpeakera(lbSeznamMluvcich.SelectedItem as Speaker))
                            {
                                lbDatabazeMluvcich.Focus();
                                Updatebindings();
                            }
                        }
                    }
                }
            }
        }

        //odstrani aktualniho speakera
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            bSpeaker = null;
            bTag.speakerID = new Speaker().ID;
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

                    }
                }
            }
            catch
            {

            }
        }

        private void lbDatabazeMluvcich_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (myDataSource.NovySpeaker(this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(((Speaker)lbDatabazeMluvcich.SelectedItem).FullName)) >= 0)
            {
                Updatebindings();
            }
        }

        private void lbDatabazeMluvcich_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox plb = (ListBox)sender;
                
                if (plb.SelectedItem != null)
                {
                    Speaker pSpeaker;
                    if (plb.Name == lbDatabazeMluvcich.Name)
                    {
                        pSpeaker = this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(plb.SelectedItem.ToString());
                    }
                    else
                    {
                        pSpeaker = myDataSource.Speakers.NajdiSpeakeraSpeaker(plb.SelectedItem.ToString());
                    }
                    bSpeaker = new Speaker(pSpeaker);
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
            EditSpeakerWindow ew = new EditSpeakerWindow(myDataSource, new Speaker());
            if (ew.ShowDialog() == true)
            {
                myDataSource.Speakers.NovySpeaker(new Speaker(ew.Speaker));
                bDatabazeMluvcich.NovySpeaker(new Speaker(ew.Speaker));
                Updatebindings();
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
            if (bSpeaker == null)
                return;
            EditSpeakerWindow win = new EditSpeakerWindow(myDataSource,bSpeaker);
            if (win.ShowDialog() == false)
                return;

            Speaker pSpeaker = win.Speaker;
            Speaker pkopieSpeaker = new Speaker(bSpeaker);
            if (bSpeaker != null && pkopieSpeaker != null && pkopieSpeaker.FullName != null && pkopieSpeaker.FullName != "" && pSpeaker != null)
            {
                Speaker pSp = myDataSource.Speakers.NajdiSpeakeraSpeaker(pkopieSpeaker.FullName);
                if (pSp != null && pSp.ID != int.MinValue)
                {
                    if (myDataSource.Speakers.UpdatujSpeakera(pkopieSpeaker.FullName, pSpeaker))
                    {
                        bSpeaker = myDataSource.Speakers.NajdiSpeakeraSpeaker(pSpeaker.FullName);
                        Updatebindings();
                        lbSeznamMluvcich.SelectedItem = bSpeaker;
                    }
                }
                pSp = this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(pkopieSpeaker.FullName);
                if (pSp != null && pSp.ID >= 0)
                {
                    if (this.bDatabazeMluvcich.UpdatujSpeakera(pkopieSpeaker.FullName, pSpeaker))
                    {
                        bSpeaker = this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(pSpeaker.FullName);
                        Updatebindings();
                        lbDatabazeMluvcich.SelectedItem = bSpeaker;
                    }
                }
            }
            else
            {

                if (this.bDatabazeMluvcich.NovySpeaker(pSpeaker) > -1)
                {
                    bSpeaker = myDataSource.VratSpeakera(myDataSource.GetSpeaker(pSpeaker.FullName));
                    Updatebindings();
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
                    else fileDialog.InitialDirectory = FilePaths.DefaultDirectory;
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
        public static Speaker ZiskejMluvciho(MySpeakers aDatabazeMluvcich, Speaker aPuvodniMluvci, string aFotkaBase64)
        {
            return ZiskejMluvciho(MySetup.Setup, aDatabazeMluvcich, aPuvodniMluvci, aFotkaBase64);
        }
        /// <summary>
        /// zavola okno s editaci mluvcich a vrati vybraneho a nastaveneho mluvciho, referencneovlivni databazi mluvcich programu
        /// </summary>
        /// <returns></returns>
        public static Speaker ZiskejMluvciho(MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, Speaker aPuvodniMluvci, string aFotkaBase64)
        {
            try
            {
                
                Speaker ret = aPuvodniMluvci;
                Transcription ps = new Transcription();
                ps.NovySpeaker(aPuvodniMluvci);
                WinSpeakers ws = new WinSpeakers(new TranscriptionParagraph(), aNastaveniProgramu, aDatabazeMluvcich, ps, aPuvodniMluvci);
                ws.ShowDialog(); 
                if (ws.DialogResult == true)
                {
                    ret = ws.bSpeaker;
                }

                return ret;
            }
            catch (Exception)
            {
                return new Speaker();
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
                    else fileDialog.InitialDirectory = FilePaths.DefaultDirectory;
                }
                catch
                {
                    
                }
                fileDialog.FilterIndex = 1;
                fileDialog.RestoreDirectory = true;
                if (fileDialog.ShowDialog() == true)
                {
                    if (this.bDatabazeMluvcich.Serialize_V1(fileDialog.FileName, this.bDatabazeMluvcich))
                    {

                    }
                }

        }

        private void btSynchronizovat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Speaker i in myDataSource.Speakers.Speakers)
                {
                    if (bDatabazeMluvcich.NajdiSpeakeraID(i.FullName) < 0)
                        bDatabazeMluvcich.NovySpeaker(i);
                }
                Updatebindings();
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

        private void lbSeznamMluvcich_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btEditace_Click(null, null);
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            if (bTag != null && bSpeaker !=null)
                bTag.speakerID = bSpeaker.ID;
            Close();
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            Speaker sp = lbSeznamMluvcich.SelectedItem as Speaker;
            if (sp != null)
            {
                
            }
            else
            {
                MessageBox.Show("Není výbraný mluvčí na kterého neviditelné převést","Chyba",MessageBoxButton.OK,MessageBoxImage.Exclamation);
            }
        }

        private void buttonReplace_Click(object sender, RoutedEventArgs e)
        {
            ReplaceSpeakerWindow w = new ReplaceSpeakerWindow(myDataSource.Speakers);
            if (w.ShowDialog() == true)
            {
                if (w.From != null && w.To != null && w.From.ID != w.To.ID)
                {
                    if (w.From.ID == Speaker.DefaultID)
                    {
                        TranscriptionElement elm = myDataSource.First();
                        while (elm != null)
                        {
                            if (elm.IsParagraph)
                            {
                                TranscriptionParagraph p = (TranscriptionParagraph)elm;
                                if (myDataSource.Speakers.VratSpeakera(p.speakerID).ID == Speaker.DefaultID)
                                {
                                    p.speakerID = w.To.ID;
                                }
                            }
                            elm = elm.Next();
                        }

                    }
                    else
                    {
                        TranscriptionElement elm = myDataSource.First();
                        while (elm != null)
                        {
                            if (elm.IsParagraph)
                            {
                                TranscriptionParagraph p = (TranscriptionParagraph)elm;
                                if (p.speakerID == w.From.ID)
                                {
                                    p.speakerID = w.To.ID;
                                }
                            }
                            elm = elm.Next();
                        }
                    }
                    MessageBox.Show("Převedeno", "Převedeno", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Nelze Převést", "Nelze Převést", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                
            }
        }
    }
}
