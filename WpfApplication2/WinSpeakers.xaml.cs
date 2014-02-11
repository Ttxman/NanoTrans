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
        private MyTag bTag;
        public  MySpeaker bSpeaker;
        private MySetup bNastaveni;
        private MySpeakers bDatabazeMluvcich;
        private MySubtitlesData myDataSource;

        private string bStringBase64FotoExterni;
        private string bStringBase64FotoInterni;

        
        /// <summary>
        /// inicializace 
        /// </summary>
        /// <returns></returns>
        private bool Inicializace(MyTag aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataSource, MySpeaker aVychoziMluvci, string aPredavanaFotkaStringBase64)
        {
            this.bStringBase64FotoExterni = aPredavanaFotkaStringBase64;
            //this.bStringBase64FotoInterni = aPredavanaFotkaStringBase64;
            if (aPredavanaFotkaStringBase64 != null)
            {
                imFotka.Source = MyKONST.PrevedBase64StringNaJPG(aPredavanaFotkaStringBase64);
            }

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



            cbMluvci.Items.Add(MyKONST.TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE);
            string[] pSeznamMluvcich = bNastaveni.rozpoznavac.MluvciSeznamDostupnych(bNastaveni.AbsolutniAdresarRozpoznavace);
            if (pSeznamMluvcich != null) foreach (string s in pSeznamMluvcich) cbMluvci.Items.Add(s);
            cbMluvci.SelectedIndex = 0;

            cbJazykovyModel.Items.Add(MyKONST.TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE);
            string[] pSeznamSlovniku = bNastaveni.rozpoznavac.JazykovyModelSeznamDostupnych(bNastaveni.AbsolutniAdresarRozpoznavace);
            if (pSeznamSlovniku != null) foreach (string s in pSeznamSlovniku) cbJazykovyModel.Items.Add(s);
            cbJazykovyModel.SelectedIndex = 0;
            
            cbPrepisovaciPravidla.Items.Add(MyKONST.TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE);
            string[] pSeznamPravidel = bNastaveni.rozpoznavac.PrepisovaciPravidlaSeznamDostupnych(bNastaveni.AbsolutniAdresarRozpoznavace);
            if (pSeznamPravidel != null) foreach (string s in pSeznamPravidel) cbPrepisovaciPravidla.Items.Add(s);
            cbPrepisovaciPravidla.SelectedIndex = 0;

           

            if (myDataSource.VratSpeakera(bTag).FullName != null)
            {
                bSpeaker = myDataSource.VratSpeakera(bTag);
                lbSeznamMluvcich.SelectedItem = bSpeaker.FullName;
            }
            else if (aVychoziMluvci != null)
            {
                bSpeaker = aVychoziMluvci;
                lbSeznamMluvcich.SelectedItem = bSpeaker.FullName;
            }

            if (lbSeznamMluvcich.Items.Count > 0)
            {
                //lbSeznamMluvcich.SelectedIndex = 0;
                //if (lbSeznamMluvcich.SelectedItem!=null)
                lbSeznamMluvcich.Items.MoveCurrentToPosition(lbSeznamMluvcich.SelectedIndex);
                lbSeznamMluvcich.Focus();
            }
            else if (lbDatabazeMluvcich.Items.Count > 0)
            {
                lbDatabazeMluvcich.UpdateLayout();
                //lbDatabazeMluvcich.SelectedIndex = 0;
                //lbDatabazeMluvcich.SelectAll();
                lbDatabazeMluvcich.Items.MoveCurrentToPosition(0);

                bool b = lbDatabazeMluvcich.Focus();
            }
            else
            {
                tbPridejMluvciho.Focus();
            }
            return true;

        }
        
        public WinSpeakers(MyTag aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataTitulky, string aFotkaBase64)
        {
            InitializeComponent();
            Inicializace(aTag, aNastaveniProgramu, aDatabazeMluvcich, aDataTitulky, null, aFotkaBase64);
            
            
            //this.Show();
        }

        public WinSpeakers(MyTag aTag, MySetup aNastaveniProgramu, MySpeakers aDatabazeMluvcich, MySubtitlesData aDataTitulky, MySpeaker aVychoziMluvci, string aFotkaBase64)
        {
            InitializeComponent();
            Inicializace(aTag, aNastaveniProgramu, aDatabazeMluvcich, aDataTitulky, aVychoziMluvci, aFotkaBase64);
            

            //this.Show();
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            btPridejMluvciho_Click(null, new RoutedEventArgs());

            MySpeaker pSpeaker = new MySpeaker(bSpeaker);
            //pSpeaker.speakerFotoJPGBase64 = null;
            if (pSpeaker == null) pSpeaker = new MySpeaker();
            int i = myDataSource.NajdiSpeakera(pSpeaker.FullName);
            if (i < 0) i = myDataSource.NovySpeaker(pSpeaker);
            if (i >= 0)
            {
                myDataSource.ZadejSpeakera(bTag, i);
            }
            else
            {
                if (bSpeaker == null) bSpeaker = new MySpeaker();
                i = myDataSource.NajdiSpeakera(bSpeaker.FullName);
                myDataSource.ZadejSpeakera(bTag, i);
            }

            this.DialogResult = true;
            this.Close();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        
        private void btPridejMluvciho_Click(object sender, RoutedEventArgs e)
        {
            string pMluvci = null;
            string pJazykovyModel = null;
            string pPrepisovaciPravidla = null;
            if (cbMluvci.SelectedIndex > 0) pMluvci = cbMluvci.SelectedItem.ToString();
            if (cbJazykovyModel.SelectedIndex > 0) pJazykovyModel = cbJazykovyModel.SelectedItem.ToString();
            if (cbPrepisovaciPravidla.SelectedIndex > 0) pPrepisovaciPravidla = cbPrepisovaciPravidla.SelectedItem.ToString();
            
            string pPohlavi = cbPohlavi.Text;
            if (cbPohlavi.SelectedIndex <= 0) pPohlavi = null;
            MySpeaker pSpeaker = new MySpeaker(tbPridejMluvciho.Text, tbPrijmeni.Text, pPohlavi, pMluvci, pJazykovyModel, pPrepisovaciPravidla, this.bStringBase64FotoInterni, tbVek.Text);
            

            //ulozi zmeny v mluvcim - v databazi i v datove strukture

            MySpeaker pkopieSpeaker = new MySpeaker(bSpeaker);
            if (pkopieSpeaker != null && pkopieSpeaker.FullName!=null && pkopieSpeaker.FullName!="" && pSpeaker != null)
            {
                MySpeaker pSp = myDataSource.SeznamMluvcich.NajdiSpeakeraSpeaker(pkopieSpeaker.FullName);
                if (pSp != null && pSp.ID >= 0)
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
                        bStringBase64FotoExterni = null;
                    }
                }
            }
            else
            {

                if (this.bDatabazeMluvcich.NovySpeaker(pSpeaker) > -1)
                {
                    if (myDataSource.NajdiSpeakera(pSpeaker.FullName) < 0)
                    {
                        if (myDataSource.NovySpeaker(pSpeaker) >= 0)
                        {
                            lbSeznamMluvcich.Items.Add(pSpeaker.FullName);
                        }
                    }
                    if (pSpeaker.FotoJPGBase64 == null) pSpeaker.FotoJPGBase64 = this.bStringBase64FotoExterni;
                    
                    lbDatabazeMluvcich.Items.Add(pSpeaker.FullName);

                    bSpeaker = myDataSource.VratSpeakera(myDataSource.NajdiSpeakera(pSpeaker.FullName));
                    //btOK_Click(null, new RoutedEventArgs());
                    
                }
            }

        }

        private void btSmazSpeakera_Click(object sender, RoutedEventArgs e)
        {
            if (bSpeaker != null && bSpeaker.FullName != null)
            {
                if (lbSeznamMluvcich.SelectedItem != null)
                {
                    if (MessageBox.Show("Opravdu chcete smazat vybraného mluvčího z příslušného seznamu i datové struktury, pokud je v ní přítomen?", "Upozornění:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (myDataSource.OdstranSpeakera(myDataSource.VratSpeakera(myDataSource.NajdiSpeakera(((string)lbSeznamMluvcich.SelectedItem)))))
                        {
                            lbSeznamMluvcich.Items.Clear();
                            //spSeznam.Children.Clear();
                            for (int i = 0; i < myDataSource.SeznamMluvcich.Speakers.Count; i++)
                            {
                                lbSeznamMluvcich.Items.Add((myDataSource.SeznamMluvcich.Speakers[i]).FullName);
                            }

                            if (myDataSource.VratSpeakera(bTag).FullName != null)
                            {
                                bSpeaker = myDataSource.VratSpeakera(bTag);
                                //spSeznam.Children.Add(this.bSpeaker.speakerName);   //prida do seznamu aktualniho uzivatele
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
            myDataSource.ZadejSpeakera(bTag, new MySpeaker().ID);
            this.DialogResult = true;
            this.Close();
        }

        private void tbPridejMluvciho_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.IsRepeat && e.Key == Key.Return)
            {
                
                btPridejMluvciho_Click(null, new RoutedEventArgs());
                
            }
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
                        bSpeaker = myDataSource.VratSpeakera(myDataSource.NajdiSpeakera(((string)lbSeznamMluvcich.SelectedItem)));
                        //spSeznam.Children.Clear();
                        //spSeznam.Children.Add(this.bSpeaker.speakerName);
                        btOK.Focus();
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
                bSpeaker = myDataSource.VratSpeakera(myDataSource.NajdiSpeakera(((string)lbDatabazeMluvcich.SelectedItem)));
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
                    if (pSpeaker != null)
                    {
                        tbPridejMluvciho.Text = pSpeaker.FirstName;
                        tbPrijmeni.Text = pSpeaker.Surname;
                        if (pSpeaker.Sex == null) cbPohlavi.SelectedIndex = 0; else cbPohlavi.Text = pSpeaker.Sex;
                        if (pSpeaker.RozpoznavacMluvci == null) cbMluvci.SelectedIndex = 0; else cbMluvci.SelectedItem = pSpeaker.RozpoznavacMluvci;
                        if (pSpeaker.RozpoznavacJazykovyModel == null) cbJazykovyModel.SelectedIndex = 0; else cbJazykovyModel.SelectedItem = pSpeaker.RozpoznavacJazykovyModel;
                        if (pSpeaker.RozpoznavacPrepisovaciPravidla == null) cbPrepisovaciPravidla.SelectedIndex = 0; else cbPrepisovaciPravidla.SelectedItem = pSpeaker.RozpoznavacPrepisovaciPravidla;
                        tbVek.Text = pSpeaker.Comment;

                        if (pSpeaker.FotoJPGBase64 != null)
                        {
                            imFotka.Source = MyKONST.PrevedBase64StringNaJPG(pSpeaker.FotoJPGBase64);
                            this.bStringBase64FotoInterni = pSpeaker.FotoJPGBase64;

                        }
                        else if (this.bStringBase64FotoExterni != null)
                        {
                            imFotka.Source = MyKONST.PrevedBase64StringNaJPG(this.bStringBase64FotoExterni);
                            this.bStringBase64FotoInterni = this.bStringBase64FotoExterni;
                        }
                        else
                        {
                            imFotka.Source = null;
                            this.bStringBase64FotoInterni = null;
                        }
                    }
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
            lbSeznamMluvcich.SelectedItem = null;
            lbDatabazeMluvcich.SelectedItem = null;
            tbPridejMluvciho.Text = "";
            tbPrijmeni.Text = "";
            tbPridejMluvciho.Focus();
            cbPohlavi.SelectedIndex = 0;
            cbMluvci.SelectedIndex = 0;
            cbJazykovyModel.SelectedIndex = 0;
            cbPrepisovaciPravidla.SelectedIndex = 0;
            tbVek.Text = "";
            bSpeaker = null;
            bStringBase64FotoInterni = null;
            imFotka.Source = MyKONST.PrevedBase64StringNaJPG(bStringBase64FotoExterni);
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
                if (!tbPridejMluvciho.IsFocused)
                {
                    btSmazSpeakera_Click(null, new RoutedEventArgs());
                }

            }
            else if (e.Key == Key.Enter)
            {
                if (tbPridejMluvciho.IsFocused || cbMluvci.IsFocused || cbMluvci.IsDropDownOpen || cbJazykovyModel.IsFocused || cbJazykovyModel.IsDropDownOpen || cbPrepisovaciPravidla.IsFocused || cbPrepisovaciPravidla.IsDropDownOpen || btPridejMluvciho.IsFocused || tbVek.IsFocused)
                {
                    btPridejMluvciho_Click(null, new RoutedEventArgs());
                    e.Handled = true;
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) btOK_Click(null, new RoutedEventArgs());
                }
                else
                {
                    e.Handled = true;
                    btOK_Click(null, new RoutedEventArgs());
                   
                }
                

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
            tbPridejMluvciho.Focus();
        }

        private void btExterniDatabaze_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
            }
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
                WinSpeakers ws = new WinSpeakers(new MyTag(), aNastaveniProgramu, aDatabazeMluvcich, ps, aPuvodniMluvci, aFotkaBase64);
                ws.ShowDialog(); 
                if (ws.DialogResult == true)
                {
                    ret = ws.bSpeaker;
                }

                return ret;
            }
            catch (Exception ex)
            {
                return new MySpeaker();
            }
        }

        private void btNacistObrazek_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Title = "Otevřít soubor s obrázkem mluvčího...";
            fileDialog.Filter = "Soubory obrázků|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Všechny soubory (*.*)|*.*";
            try
            {
                fileDialog.FilterIndex = 1;
                fileDialog.RestoreDirectory = true;
                if (fileDialog.ShowDialog() == true)
                {
                    BitmapFrame pFrame = BitmapFrame.Create(new Uri(fileDialog.FileName));

                    this.bStringBase64FotoExterni = MyKONST.PrevedJPGnaBase64String(pFrame);
                    this.bStringBase64FotoInterni = this.bStringBase64FotoExterni;

                    imFotka.Source = pFrame;

                }
            }
            catch
            {

            }
            
        }

        private void btExterniDatabazeUlozit_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
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

        private void btSmazatObrazek_Click(object sender, RoutedEventArgs e)
        {
            if (bSpeaker != null)
            {
                bSpeaker.FotoJPGBase64 = null;
                this.bStringBase64FotoInterni = null;
                imFotka.Source = MyKONST.PrevedBase64StringNaJPG(this.bStringBase64FotoExterni);
            }
        }

        public bool Contains(object de)
        {

            string polozka = de as string;
            if (polozka != null)
            {
                // Filter out products with price 25 or above
                if (polozka.ToLower().Contains(tbHledani.Text.ToLower()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }


        private void tbHledani_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbDatabazeMluvcich.Items.Filter += new Predicate<object>(Contains);
            if (bDatabazeMluvcich.Speakers.Count > 0 && lbDatabazeMluvcich.Items.IsEmpty)
            {
                tbHledani.Background = Brushes.LightPink;
            }
            else
            {
                tbHledani.Background = tbVek.Background;
            }
            
        }

        private void lbDatabazeMluvcich_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            char ch = (char)KeyInterop.VirtualKeyFromKey(e.Key);
            Key k = e.Key;

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && ch == 'F')
            {
                tbHledani.Focus();
            }
            
            //key
            //KeyConverter kk = new KeyConverter();
            
            if (tbHledani.Text != "" && e.Key== Key.Back)
            {
                //tbHledani.Text = tbHledani.Text.Remove(tbHledani.Text.Length - 1);
                tbHledani.Text = "";

            }
            if (e.Key.ToString().Length <= 2)
            {
                //tbHledani.Text += ch;
                //tbHledani.Text += (char)e.Key;

            }
        }

        private void tbHledani_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                lbDatabazeMluvcich.Focus();
            }
        }

        private void lbDatabazeMluvcich_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

    }
}
