using System;
using System.Collections.Generic;
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
using Microsoft.Windows.Controls;


namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSetup.xaml
    /// </summary>
    public partial class WinSetup : Window
    {
        public MySetup bNastaveni;
        private MySpeakers bDatabazeMluvcich;
        private MySpeaker bPomocnyMluvciDiktatPredUlozenim;
        private MySpeaker bPomocnyMluvciHlasovehoOvladaniPredUlozenim;

        public WinSetup(MySetup aNastaveni, MySpeakers aDatabazeMluvcich)
        {
            bNastaveni = aNastaveni;
            bDatabazeMluvcich = aDatabazeMluvcich;
            bPomocnyMluvciDiktatPredUlozenim = bNastaveni.diktatMluvci;
            bPomocnyMluvciHlasovehoOvladaniPredUlozenim = bNastaveni.hlasoveOvladaniMluvci;
            InitializeComponent();
            if (MyKONST.VERZE == MyEnumVerze.Externi)
            {
                tabControl1.Items.RemoveAt(1);
                tabControl1.Items.RemoveAt(1);
            }

            if (aNastaveni != null)
            {
                //audio
                cbVystupniAudioZarizeni.Items.Clear();
                string[] pZarizeni = MyWavePlayer.DeviceNamesOUT;
                if (pZarizeni != null)
                {
                    foreach (string s in pZarizeni)
                    {
                        cbVystupniAudioZarizeni.Items.Add(s);
                    }
                }
                if (bNastaveni.audio.VystupniZarizeniIndex < cbVystupniAudioZarizeni.Items.Count) cbVystupniAudioZarizeni.SelectedIndex = bNastaveni.audio.VystupniZarizeniIndex;

                cbVstupniAudioZarizeni.Items.Clear();
                if (pZarizeni != null)
                {
                    foreach (string s in pZarizeni)
                    {
                        cbVstupniAudioZarizeni.Items.Add(s);
                    }
                }
                if (bNastaveni.audio.VstupniZarizeniIndex < cbVstupniAudioZarizeni.Items.Count) cbVstupniAudioZarizeni.SelectedIndex = bNastaveni.audio.VstupniZarizeniIndex;

                //foneticky prepis
                if (bNastaveni.fonetickyPrepis.Jazyk == null || bNastaveni.fonetickyPrepis.Jazyk == "") cbFonPrepisJazyk.SelectedIndex = 0; else cbFonPrepisJazyk.Text = bNastaveni.fonetickyPrepis.Jazyk;
                if (bNastaveni.fonetickyPrepis.Pohlavi == null || bNastaveni.fonetickyPrepis.Pohlavi == "") cbFonPrepisPohlavi.SelectedIndex = 0; else cbFonPrepisPohlavi.Text = bNastaveni.fonetickyPrepis.Pohlavi;
                chbFonPrehratPoRozpoznani.IsChecked = bNastaveni.fonetickyPrepis.PrehratAutomatickyRozpoznanyOdstavec;


                //prepisovac

                //mluvci
                string[] str = bNastaveni.rozpoznavac.MluvciSeznamDostupnych(bNastaveni.AbsolutniAdresarRozpoznavace);
                for (int i = 0; i < str.Length; i++)
                {
                    cbMluvci.Items.Add(str[i]);
                }


                cbMluvci.SelectedItem = bNastaveni.rozpoznavac.MluvciVybrany;


                FileInfo fi2;
                //jazykovy model
                DirectoryInfo di = new DirectoryInfo(aNastaveni.AbsolutniAdresarRozpoznavace + "/" + aNastaveni.rozpoznavac.JazykovyModelRelativniAdresar);
                FileInfo[] fiPole;
                if (di != null && di.Exists)
                {
                    fiPole = di.GetFiles();
                    if (fiPole != null)
                    {
                        foreach (FileInfo fi in fiPole)
                        {
                            cbJazykovyModel.Items.Add(fi.Name);
                        }

                    }
                }
                fi2 = new FileInfo(aNastaveni.rozpoznavac.JazykovyModel);
                if (fi2 != null)
                {
                    cbJazykovyModel.SelectedItem = fi2.Name;
                }

                //prepisovaci pravidla
                di = new DirectoryInfo(aNastaveni.AbsolutniAdresarRozpoznavace + "/" + aNastaveni.rozpoznavac.PrepisovaciPravidlaRelativniAdresar);
                if (di != null && di.Exists)
                {
                    fiPole = di.GetFiles();
                    if (fiPole != null)
                    {
                        foreach (FileInfo fi in fiPole)
                        {
                            cbPrepisovaciPravidla.Items.Add(fi.Name);
                        }

                    }
                }
                fi2 = new FileInfo(aNastaveni.rozpoznavac.PrepisovaciPravidla);
                if (fi2 != null)
                {
                    cbPrepisovaciPravidla.SelectedItem = fi2.Name;
                }


                tbLicencniServer.Text = aNastaveni.rozpoznavac.LicencniServer;
                tbLicencniSoubor.Text = aNastaveni.rozpoznavac.LicencniSoubor;
                tbBuffer.Text = aNastaveni.rozpoznavac.DelkaInternihoBufferuPrepisovace.ToString();



                //mluvci diktatu
                VyplnTextHlasovehoOvladani(tbMluvciDiktatu, bNastaveni.diktatMluvci);
                VyplnTextHlasovehoOvladani(tbMluvciHlasovehoOvladani, bNastaveni.hlasoveOvladaniMluvci);
                slKvalitaRozpoznavaniDiktat.Value = aNastaveni.rozpoznavac.KvalitaRozpoznavaniDiktat;
                slKvalitaRozpoznavaniOvladani.Value = aNastaveni.rozpoznavac.KvalitaRozpoznavaniOvladani;



                //databaze mluvcich

                string pCesta = aNastaveni.CestaDatabazeMluvcich;
                try
                {
                    if (!pCesta.Contains(":"))
                    {
                        pCesta = aNastaveni.absolutniCestaEXEprogramu + "/" + aNastaveni.CestaDatabazeMluvcich;
                    }
                    //pCesta = new FileInfo(aNastaveni.CestaDatabazeMluvcich).FullName;
                    pCesta = new FileInfo(pCesta).FullName;

                }
                finally
                {
                    tbCestaDatabazeMluvcich.Text = pCesta;
                }

                //velikost pisma
                tbVelikostPisma.Text = aNastaveni.SetupTextFontSize.ToString();
                chbZobrazitFotku.IsChecked = aNastaveni.ZobrazitFotografieMluvcich;
                slVelikostFotografie.Value = aNastaveni.Fotografie_VyskaMax;

                //casove indexy
                chbZobrazitIndexyPocatku.IsChecked = aNastaveni.zobrazitCasBegin;
                chbZobrazitIndexyKonce.IsChecked = aNastaveni.zobrazitCasEnd;

                //prehravani

                UpDownSpeed.Value =  (decimal)aNastaveni.ZpomalenePrehravaniRychlost;
                UpDownJump.Value = (int)(aNastaveni.VlnaMalySkok);
            }
        }

        private void VyplnTextHlasovehoOvladani(TextBox aTbMluvciHlasovehoOvladani, MySpeaker aSpeaker)
        {
            if (aSpeaker == null || aSpeaker.FullName == null)
            {
                aTbMluvciHlasovehoOvladani.Text = MyKONST.TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE;
            }
            else
            {
                aTbMluvciHlasovehoOvladani.Text = "Jméno: " + aSpeaker.FullName + "\n";
                aTbMluvciHlasovehoOvladani.Text += "Typ mluvčího: ";
                if (aSpeaker.RozpoznavacMluvci != null)
                {
                    aTbMluvciHlasovehoOvladani.Text += aSpeaker.RozpoznavacMluvci + "\n";
                }
                else
                {
                    aTbMluvciHlasovehoOvladani.Text += MyKONST.TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE + "\n";
                }
                aTbMluvciHlasovehoOvladani.Text += "Jazykový model: ";
                if (aSpeaker.RozpoznavacJazykovyModel != null)
                {
                    aTbMluvciHlasovehoOvladani.Text += aSpeaker.RozpoznavacJazykovyModel + "\n";
                }
                else
                {
                    aTbMluvciHlasovehoOvladani.Text += MyKONST.TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE + "\n";
                }
                aTbMluvciHlasovehoOvladani.Text += "Přepisovací pravidla: ";
                if (aSpeaker.RozpoznavacPrepisovaciPravidla != null)
                {
                    aTbMluvciHlasovehoOvladani.Text += aSpeaker.RozpoznavacPrepisovaciPravidla + "\n";
                }
                else
                {
                    aTbMluvciHlasovehoOvladani.Text += MyKONST.TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE + "\n";
                }

            }

        }



        /// <summary>
        /// spusti okno a vrati hodnoty nastaveni
        /// </summary>
        /// <param name="aNastaveni"></param>
        /// <returns></returns>
        public static MySetup WinSetupNastavit(MySetup aNastaveni, MySpeakers aDatabazeMluvcich)
        {
            WinSetup ws = new WinSetup(aNastaveni, aDatabazeMluvcich);
            ws.ShowDialog();

            return ws.bNastaveni;
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            //ulozit vse do promenne!

            //audio
            bNastaveni.audio.VystupniZarizeniIndex = cbVystupniAudioZarizeni.SelectedIndex;
            if (bNastaveni.audio.VystupniZarizeniIndex < 0) bNastaveni.audio.VystupniZarizeniIndex = 0;

            bNastaveni.audio.VstupniZarizeniIndex = cbVstupniAudioZarizeni.SelectedIndex;
            if (bNastaveni.audio.VstupniZarizeniIndex < 0) bNastaveni.audio.VstupniZarizeniIndex = 0;

            //foneticky prepis
            bNastaveni.fonetickyPrepis.Jazyk = "";
            if (cbFonPrepisJazyk.SelectedIndex > 0) bNastaveni.fonetickyPrepis.Jazyk = cbFonPrepisJazyk.Text;
            bNastaveni.fonetickyPrepis.Pohlavi = "";
            if (cbFonPrepisPohlavi.SelectedIndex > 0) bNastaveni.fonetickyPrepis.Pohlavi = cbFonPrepisPohlavi.Text;
            bNastaveni.fonetickyPrepis.PrehratAutomatickyRozpoznanyOdstavec = (bool)chbFonPrehratPoRozpoznani.IsChecked;


            //prepisovac
            bNastaveni.rozpoznavac.Mluvci = bNastaveni.rozpoznavac.MluvciRelativniAdresar + "/" + cbMluvci.Text;
            bNastaveni.rozpoznavac.JazykovyModel = bNastaveni.rozpoznavac.JazykovyModelRelativniAdresar + "/" + cbJazykovyModel.Text;
            bNastaveni.rozpoznavac.PrepisovaciPravidla = bNastaveni.rozpoznavac.PrepisovaciPravidlaRelativniAdresar + "/" + cbPrepisovaciPravidla.Text;

            bNastaveni.rozpoznavac.LicencniServer = tbLicencniServer.Text;
            bNastaveni.rozpoznavac.LicencniSoubor = tbLicencniSoubor.Text;
            try
            {
                bNastaveni.rozpoznavac.DelkaInternihoBufferuPrepisovace = long.Parse(tbBuffer.Text);
            }
            catch
            {

            }


            //hlasove ovladani
            bNastaveni.diktatMluvci = this.bPomocnyMluvciDiktatPredUlozenim;
            bNastaveni.hlasoveOvladaniMluvci = this.bPomocnyMluvciHlasovehoOvladaniPredUlozenim;
            bNastaveni.rozpoznavac.KvalitaRozpoznavaniDiktat = (long)slKvalitaRozpoznavaniDiktat.Value;
            bNastaveni.rozpoznavac.KvalitaRozpoznavaniOvladani = (long)slKvalitaRozpoznavaniOvladani.Value;


            //databaze mluvcich
            bNastaveni.CestaDatabazeMluvcich = tbCestaDatabazeMluvcich.Text;

            //velikost fontu
            try
            {
                bNastaveni.SetupTextFontSize = double.Parse(tbVelikostPisma.Text);
            }
            catch
            {

            }
            bNastaveni.ZobrazitFotografieMluvcich = (bool)chbZobrazitFotku.IsChecked;
            bNastaveni.Fotografie_VyskaMax = slVelikostFotografie.Value;

            bNastaveni.zobrazitCasBegin = (bool)(chbZobrazitIndexyPocatku.IsChecked);
            bNastaveni.zobrazitCasEnd = (bool)(chbZobrazitIndexyKonce.IsChecked);

            bNastaveni.ZpomalenePrehravaniRychlost =(double)UpDownSpeed.Value;
            bNastaveni.VlnaMalySkok = (double)UpDownJump.Value;

            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Title = "Načíst cestu s databází mluvčích...";
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
                MySpeakers ms = new MySpeakers();
                ms = ms.Deserializovat(fileDialog.FileName);
                if (ms != null && ms.Ulozeno)
                {
                    tbCestaDatabazeMluvcich.Text = ms.JmenoSouboru;
                }
            }
        }

        private void btMluvciHlasovehoOvladani_Click(object sender, RoutedEventArgs e)
        {
            bPomocnyMluvciDiktatPredUlozenim = WinSpeakers.ZiskejMluvciho(this.bNastaveni, this.bDatabazeMluvcich, this.bPomocnyMluvciDiktatPredUlozenim, null);
            if (bPomocnyMluvciDiktatPredUlozenim != null) bPomocnyMluvciDiktatPredUlozenim.FotoJPGBase64 = null;
            VyplnTextHlasovehoOvladani(tbMluvciDiktatu, bPomocnyMluvciDiktatPredUlozenim);
        }

        private void btNacistLicenci_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Title = "Načíst licenci...";
            fileDialog.Filter = "Všechny soubory (*.*)|*.*";
            try
            {
                FileInfo fi = new FileInfo(bNastaveni.AbsolutniAdresarRozpoznavace + "/");
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
                tbLicencniSoubor.Text = fileDialog.FileName;
            }
        }

        private void btMluvciHlasoveOvladani_Click(object sender, RoutedEventArgs e)
        {
            bPomocnyMluvciHlasovehoOvladaniPredUlozenim = WinSpeakers.ZiskejMluvciho(this.bNastaveni, this.bDatabazeMluvcich, this.bPomocnyMluvciHlasovehoOvladaniPredUlozenim, null);
            if (bPomocnyMluvciDiktatPredUlozenim != null) bPomocnyMluvciDiktatPredUlozenim.FotoJPGBase64 = null;
            VyplnTextHlasovehoOvladani(tbMluvciHlasovehoOvladani, bPomocnyMluvciHlasovehoOvladaniPredUlozenim);
        }

    }
}
