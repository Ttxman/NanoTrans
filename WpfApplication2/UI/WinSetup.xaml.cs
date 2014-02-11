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
using Microsoft.Win32;
using Ionic.Zip;
using System.Linq;
using System.Configuration;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSetup.xaml
    /// </summary>
    public partial class WinSetup : Window
    {
        public MySetup bNastaveni;
        private MySpeakers bDatabazeMluvcich;

        public WinSetup(MySetup aNastaveni, MySpeakers aDatabazeMluvcich)
        {
            bNastaveni = aNastaveni;
            bDatabazeMluvcich = aDatabazeMluvcich;
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

                //databaze mluvcich

                string pCesta = aNastaveni.CestaDatabazeMluvcich;
                try
                {
                    if (!pCesta.Contains(":"))
                    {
                        pCesta = aNastaveni.CestaDatabazeMluvcich;
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



                //prehravani

                decimal val = (decimal)aNastaveni.ZpomalenePrehravaniRychlost;
                if (val >= UpDownSpeed.Minimum.Value && val <= UpDownSpeed.Maximum.Value)
                    UpDownSpeed.Value = val;

                val = (decimal)(aNastaveni.VlnaMalySkok);
                if (val >= UpDownJump.Minimum.Value && val <= UpDownJump.Maximum.Value)
                    UpDownJump.Value = val;

                checkBoxSaveShortened.IsChecked = aNastaveni.SaveInShortFormat;
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

            bNastaveni.ZpomalenePrehravaniRychlost =(double)UpDownSpeed.Value;
            bNastaveni.VlnaMalySkok = (double)UpDownJump.Value;


            bNastaveni.SaveInShortFormat = checkBoxSaveShortened.IsChecked == true;
            this.Close();
        }

        private void ButtonLoadSpeakersDatabase_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Title = "Načíst cestu s databází mluvčích...";
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
                MySpeakers ms = new MySpeakers();
                ms = ms.Deserializovat(fileDialog.FileName);
                if (ms != null && ms.Ulozeno)
                {
                    tbCestaDatabazeMluvcich.Text = ms.JmenoSouboru;
                }
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string URL = ConfigurationManager.AppSettings["OODictionarySource"];
            Stream s= DownloadOneFileWindow.DownloadFile(URL);
            if (s == null)
            {
                MessageBox.Show(this, "Soubor nebyl stažen, uživatel zastavil stahování, cílová adresa už neexistuje, nebo není dostupné připojení k internetu", "Problém se stahováním z internetu", MessageBoxButton.OK, MessageBoxImage.Warning); 
            }else
                GetDictionaryFromZip(s);
        }

        private void ButtonLoadOpenOfficeSpellchekingDictionaries(object sender, RoutedEventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Filter = "Rozšíření OpenOffice 3 {*.zip,*.oxt} |*.zip;*.oxt";

            if (of.ShowDialog(this)==true)
            {
                try
                {
                    GetDictionaryFromZip(of.OpenFile());
                }
                catch
                {
                    MessageBox.Show(this, "Soubor nelze otevřít, je poškozený, nebo to není rozšíření Open Office 3", "Problém s otevřením souboru",MessageBoxButton.OK,MessageBoxImage.Warning);   
                }
            }
        }

        private void GetDictionaryFromZip(Stream s)
        {
            ZipFile zf = ZipFile.Read(s);
            var entry = zf.Entries.Where(en => en.FileName.EndsWith(".aff")).ToArray();

            if (entry.Length > 0)
            {
                var aff = entry[0];
                var dic = zf.Entries.Where(en => en.FileName == System.IO.Path.GetFileNameWithoutExtension(aff.FileName) + ".dic").FirstOrDefault();

                if (dic != null)
                {
                    if (MessageBox.Show(this, "Načíst slovník " + System.IO.Path.GetFileNameWithoutExtension(aff.FileName) + "?", "Načtení slovníku", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var readme = zf.Entries.Where(en => en.FileName.ToLower().Contains("readme")).FirstOrDefault();
                        if (readme != null)
                        {
                            string ss = new StreamReader(readme.OpenReader()).ReadToEnd();
                            
                            if (TextWallWindow.ShowWall(true, "Pro instalaci slovníků si musíte být vědomi tohoto:", ss))
                            {
                                
                                if (SpellChecker.SpellEngine != null)
                                {
                                    SpellChecker.SpellEngine.Dispose();
                                    SpellChecker.SpellEngine = null;
                                }


                                File.WriteAllText(FilePaths.GetWritePath("data\\readme_slovniky.txt"), ss);
                                string p = FilePaths.GetWritePath("data\\cs_CZ.aff");
   
                                using (Stream fs = File.Create(p))
                                    aff.Extract(fs);
                                p = FilePaths.GetWritePath("data\\cs_CZ.dic");

                                using (Stream fs = File.Create(p))
                                    dic.Extract(fs);

                                SpellChecker.LoadVocabulary();
                                MessageBox.Show(this, "Slovníky pro kontrolu pravopisu byly nainstalovány", "Informace", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }

                    }
                }

            }
        }
    }

    public class CollapseOnNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
               System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return System.Windows.Visibility.Collapsed;
            else
                return System.Windows.Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
               System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("unexpected Convertback");
        }
    }

    public class CollapseOnNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
               System.Globalization.CultureInfo culture)
        {
            if (value != null)
                return System.Windows.Visibility.Collapsed;
            else
                return System.Windows.Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
               System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("unexpected Convertback");
        }
    }


}
