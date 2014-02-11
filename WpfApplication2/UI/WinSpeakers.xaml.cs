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
using NanoTrans.Core;

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
            var bfr = lbDatabazeMluvcich.SelectedItem;
            lbDatabazeMluvcich.ItemsSource = null;
            lbDatabazeMluvcich.ItemsSource = bDatabazeMluvcich.Speakers;
            lbDatabazeMluvcich.SelectedItem = bfr;

            bfr = lbSeznamMluvcich.SelectedItem;
            lbSeznamMluvcich.ItemsSource = null;
            lbSeznamMluvcich.ItemsSource = myDataSource.Speakers.Speakers;
            lbSeznamMluvcich.SelectedItem = bfr;
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
                    if (MessageBox.Show(Properties.Strings.MessageBoxConfirmUsedSpeakerDeletion, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (myDataSource.RemoveSpeaker(lbSeznamMluvcich.SelectedItem as Speaker))
                        {
                            lbSeznamMluvcich.Focus();
                            Updatebindings();
                        }
                    }
                }
                else if (lbDatabazeMluvcich.SelectedItem != null)
                {
                    if (MessageBox.Show(Properties.Strings.MessageBoxConfirmUsedSpeakerDeletion, Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (lbDatabazeMluvcich.SelectedItem != null)
                        {
                            if (this.bDatabazeMluvcich.RemoveSpeaker(lbSeznamMluvcich.SelectedItem as Speaker))
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
            bTag.Speaker = Speaker.DefaultSpeaker;
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
                        bSpeaker = myDataSource.GetSpeakerByName(((string)lbSeznamMluvcich.SelectedItem));

                    }
                }
            }
            catch
            {

            }
        }

        private void lbDatabazeMluvcich_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (myDataSource.AddSpeaker(this.bDatabazeMluvcich.GetSpeakerByName(((Speaker)lbDatabazeMluvcich.SelectedItem).FullName)))
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
                        pSpeaker = this.bDatabazeMluvcich.GetSpeakerByName((plb.SelectedItem as Speaker).FullName);
                    }
                    else
                    {
                        pSpeaker = myDataSource.Speakers.GetSpeakerByName((plb.SelectedItem as Speaker).FullName);
                    }
                    bSpeaker = pSpeaker;
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
                myDataSource.Speakers.AddSpeaker(ew.Speaker,true);
                bDatabazeMluvcich.AddSpeaker(ew.Speaker,true);
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

            Updatebindings();
        }

        private void btExterniDatabaze_Click(object sender, RoutedEventArgs e)
        {
                Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
                fileDialog.Title = Properties.Strings.FileDialogLoadSpeakersDatabaseTitle;
                fileDialog.Filter = string.Format(Properties.Strings.FileDialogLoadSpeakersDatabaseFilter, "*.xml", "*.xml");
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
                    MySpeakers ms = MySpeakers.Deserialize(fileDialog.FileName);
                    if (ms != null)
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
        /// zavola okno s editaci mluvcich a vrati vybraneho a nastaveneho mluvciho, reference neovlivni databazi mluvcich programu
        /// </summary>
        /// <returns></returns>
        public static Speaker ZiskejMluvciho(MySpeakers aDatabazeMluvcich, Speaker aPuvodniMluvci, string aFotkaBase64)
        {
            return ZiskejMluvciho(MySetup.Setup, aDatabazeMluvcich, aPuvodniMluvci, aFotkaBase64);
        }
        /// <summary>
        /// zavola okno s editaci mluvcich a vrati vybraneho a nastaveneho mluvciho, reference neovlivni databazi mluvcich programu
        /// </summary>
        /// <returns></returns>
        public static Speaker ZiskejMluvciho(MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, Speaker aPuvodniMluvci, string aFotkaBase64)
        {
            try
            {
                
                Speaker ret = aPuvodniMluvci;
                Transcription ps = new Transcription();

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
                fileDialog.Title = Properties.Strings.FileDialogSaveSpeakersDatabaseTitle;
                fileDialog.Filter = string.Format(Properties.Strings.FileDialogLoadSpeakersDatabaseFilter, "*.xml", "*.xml");
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
                    bDatabazeMluvcich.Serialize(fileDialog.FileName);
                }

        }

        private void btSynchronizovat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Speaker i in myDataSource.Speakers.Speakers)
                {
                    if (bDatabazeMluvcich.GetSpeakerByName(i.FullName).ID < 0)
                        bDatabazeMluvcich.AddSpeaker(i,true);
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
                bTag.Speaker = bSpeaker;
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
                MessageBox.Show(Properties.Strings.MessageBoxSpeakerReplaceTargetNotSelected,Properties.Strings.MessageBoxInfoCaption , MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                                if (myDataSource.Speakers.GetSpeakerByID(p.SpeakerID).ID == Speaker.DefaultID)
                                {
                                    p.Speaker = w.To;
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
                                if (p.SpeakerID == w.From.ID)
                                {
                                    p.Speaker = w.To;
                                }
                            }
                            elm = elm.Next();
                        }
                    }
                    MessageBox.Show(Properties.Strings.MessageBoxSpeakerReplaceDone, Properties.Strings.MessageBoxInfoCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Properties.Strings.MessageBoxSpeakerCannotReplace, Properties.Strings.MessageBoxInfoCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                
            }
        }
    }
}
