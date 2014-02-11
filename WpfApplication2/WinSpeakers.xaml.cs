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
        private MyParagraph bTag;
        public  MySpeaker bSpeaker;
        private MySetup bNastaveni;
        private MySpeakers bDatabazeMluvcich;
        private MySubtitlesData myDataSource;
        
        /// <summary>
        /// inicializace 
        /// </summary>
        /// <returns></returns>
        private bool Inicializace(MyParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataSource, MySpeaker aVychoziMluvci)
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
        
        public WinSpeakers(MyParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataTitulky)
        {
            InitializeComponent();
            Inicializace(aTag, aNastaveniProgramu, aDatabazeMluvcich, aDataTitulky,null);
        }

        public WinSpeakers(MyParagraph aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataTitulky, MySpeaker aVychoziMluvci)
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
                        if (myDataSource.OdstranSpeakera(lbSeznamMluvcich.SelectedItem as MySpeaker))
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
                            if (this.bDatabazeMluvcich.OdstranSpeakera(lbSeznamMluvcich.SelectedItem as MySpeaker))
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

                    }
                }
            }
            catch
            {

            }
        }

        private void lbDatabazeMluvcich_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (myDataSource.NovySpeaker(this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(((MySpeaker)lbDatabazeMluvcich.SelectedItem).FullName)) >= 0)
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
                    MySpeaker pSpeaker;
                    if (plb.Name == lbDatabazeMluvcich.Name)
                    {
                        pSpeaker = this.bDatabazeMluvcich.NajdiSpeakeraSpeaker(plb.SelectedItem.ToString());
                    }
                    else
                    {
                        pSpeaker = myDataSource.Speakers.NajdiSpeakeraSpeaker(plb.SelectedItem.ToString());
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
                myDataSource.Speakers.NovySpeaker(new MySpeaker(ew.Speaker));
                bDatabazeMluvcich.NovySpeaker(new MySpeaker(ew.Speaker));
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
            EditSpeakerWindow win = new EditSpeakerWindow(myDataSource,bSpeaker);
            if (win.ShowDialog() == false)
                return;

            MySpeaker pSpeaker = win.Speaker;
            MySpeaker pkopieSpeaker = new MySpeaker(bSpeaker);
            if (bSpeaker != null && pkopieSpeaker != null && pkopieSpeaker.FullName != null && pkopieSpeaker.FullName != "" && pSpeaker != null)
            {
                MySpeaker pSp = myDataSource.Speakers.NajdiSpeakeraSpeaker(pkopieSpeaker.FullName);
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
                WinSpeakers ws = new WinSpeakers(new MyParagraph(), aNastaveniProgramu, aDatabazeMluvcich, ps, aPuvodniMluvci);
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
                    else fileDialog.InitialDirectory = FilePaths.DefaultDirectory;
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
                foreach (MySpeaker i in myDataSource.Speakers.Speakers)
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
            if (bTag != null)
                bTag.speakerID = bSpeaker.ID;
            Close();
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            MySpeaker sp = lbSeznamMluvcich.SelectedItem as MySpeaker;
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
                    if (w.From.ID == MySpeaker.DefaultID)
                    {
                        TranscriptionElement elm = myDataSource.First();
                        while (elm != null)
                        {
                            if (elm.IsParagraph)
                            {
                                MyParagraph p = (MyParagraph)elm;
                                if (myDataSource.Speakers.VratSpeakera(p.speakerID).ID == MySpeaker.DefaultID)
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
                                MyParagraph p = (MyParagraph)elm;
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
