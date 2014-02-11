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
using NanoTrans.Core;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSetup.xaml
    /// </summary>
    public partial class WinSetup : Window
    {
        public GlobalSetup _setup;
        private SpeakerCollection _speakersDatabase;

        public WinSetup(GlobalSetup aNastaveni, SpeakerCollection aDatabazeMluvcich)
        {
            _setup = aNastaveni;
            _speakersDatabase = aDatabazeMluvcich;
            InitializeComponent();

            if (aNastaveni != null)
            {
                //audio
                cbVystupniAudioZarizeni.Items.Clear();
                string[] pZarizeni = DXWavePlayer.DeviceNamesOUT;
                if (pZarizeni != null)
                {
                    foreach (string s in pZarizeni)
                    {
                        cbVystupniAudioZarizeni.Items.Add(s);
                    }
                }
                if (_setup.audio.OutputDeviceIndex < cbVystupniAudioZarizeni.Items.Count) cbVystupniAudioZarizeni.SelectedIndex = _setup.audio.OutputDeviceIndex;

                cbVstupniAudioZarizeni.Items.Clear();
                if (pZarizeni != null)
                {
                    foreach (string s in pZarizeni)
                    {
                        cbVstupniAudioZarizeni.Items.Add(s);
                    }
                }
                if (_setup.audio.InputDeviceIndex < cbVstupniAudioZarizeni.Items.Count) cbVstupniAudioZarizeni.SelectedIndex = _setup.audio.InputDeviceIndex;

                //databaze mluvcich

                string pCesta = aNastaveni.SpeakersDatabasePath;
                try
                {
                    if (!pCesta.Contains(":"))
                    {
                        pCesta = aNastaveni.SpeakersDatabasePath;
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
                chbZobrazitFotku.IsChecked = aNastaveni.ShowSpeakerImage;
                slVelikostFotografie.Value = aNastaveni.MaxSpeakerImageWidth;



                //prehravani

                decimal val = (decimal)aNastaveni.SlowedPlaybackSpeed;
                if (val >= UpDownSpeed.Minimum.Value && val <= UpDownSpeed.Maximum.Value)
                    UpDownSpeed.Value = val;

                val = (decimal)(aNastaveni.WaveformSmallJump);
                if (val >= UpDownJump.Minimum.Value && val <= UpDownJump.Maximum.Value)
                    UpDownJump.Value = val;
            }

        }



        /// <summary>
        /// spusti okno a vrati hodnoty nastaveni
        /// </summary>
        /// <param name="aNastaveni"></param>
        /// <returns></returns>
        public static GlobalSetup WinSetupNastavit(GlobalSetup aNastaveni, SpeakerCollection aDatabazeMluvcich)
        {
            WinSetup ws = new WinSetup(aNastaveni, aDatabazeMluvcich);
            ws.ShowDialog();

            return ws._setup;
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            //ulozit vse do promenne!

            //audio
            _setup.audio.OutputDeviceIndex = cbVystupniAudioZarizeni.SelectedIndex;
            if (_setup.audio.OutputDeviceIndex < 0) _setup.audio.OutputDeviceIndex = 0;

            _setup.audio.InputDeviceIndex = cbVstupniAudioZarizeni.SelectedIndex;
            if (_setup.audio.InputDeviceIndex < 0) _setup.audio.InputDeviceIndex = 0;


            //databaze mluvcich
            _setup.SpeakersDatabasePath = tbCestaDatabazeMluvcich.Text;

            //velikost fontu
            try
            {
                _setup.SetupTextFontSize = double.Parse(tbVelikostPisma.Text);
            }
            catch
            {

            }
            _setup.ShowSpeakerImage = (bool)chbZobrazitFotku.IsChecked;
            _setup.MaxSpeakerImageWidth = slVelikostFotografie.Value;

            _setup.SlowedPlaybackSpeed = (double)UpDownSpeed.Value;
            _setup.WaveformSmallJump = (double)UpDownJump.Value;


            _setup.SaveInShortFormat = true;
            this.Close();
        }

        private void ButtonLoadSpeakersDatabase_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.SaveFileDialog();
            fileDialog.OverwritePrompt = false;
            fileDialog.Title = Properties.Strings.FileDialogLoadSpeakersDatabaseTitle;
            fileDialog.Filter = string.Format(Properties.Strings.FileDialogLoadSpeakersDatabaseFilter, "*.xml", "*.xml");

            FileInfo fi = new FileInfo(_setup.SpeakersDatabasePath);
            if (fi != null && fi.Directory.Exists) 
                fileDialog.InitialDirectory = fi.DirectoryName;
            else fileDialog.InitialDirectory = FilePaths.DefaultDirectory;

            fileDialog.FilterIndex = 1;

            if (fileDialog.ShowDialog() == true)
            {
                if (File.Exists(fileDialog.FileName))
                {
                    _speakersDatabase.Clear();
                    SpeakerCollection.Deserialize(fileDialog.FileName, _speakersDatabase);
                }
                else
                {
                    _speakersDatabase.FileName = fileDialog.FileName;
                    _speakersDatabase.Serialize();
                }

                tbCestaDatabazeMluvcich.Text = _setup.SpeakersDatabasePath = fileDialog.FileName;
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string URL = ConfigurationManager.AppSettings["OODictionarySource"];
            Stream s = DownloadOneFileWindow.DownloadFile(URL);
            if (s == null)
            {
                MessageBox.Show(this, Properties.Strings.MessageBoxDictionaryDownloadError, Properties.Strings.MessageBoxDictionaryDownloadErrorCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
                GetDictionaryFromZip(s);
        }

        private void ButtonLoadOpenOfficeSpellchekingDictionaries(object sender, RoutedEventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Filter = Properties.Strings.FileDialogLoadOODictionaryFilter;

            if (of.ShowDialog(this) == true)
            {
                try
                {
                    GetDictionaryFromZip(of.OpenFile());
                }
                catch
                {
                    MessageBox.Show(this, Properties.Strings.MessageBoxDictionaryFormatError, Properties.Strings.MessageBoxWarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    if (MessageBox.Show(this, string.Format(Properties.Strings.MessageBoxDictionaryConfirmLoad, System.IO.Path.GetFileNameWithoutExtension(aff.FileName)), Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var readme = zf.Entries.Where(en => en.FileName.ToLower().Contains("readme")).FirstOrDefault();
                        if (readme != null)
                        {
                            string ss = new StreamReader(readme.OpenReader()).ReadToEnd();

                            if (TextWallWindow.ShowWall(true, Properties.Strings.OODictionaryLicenceTitle, ss))
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
                                MessageBox.Show(this, Properties.Strings.MessageBoxDictinaryInstalled, Properties.Strings.MessageBoxInfoCaption, MessageBoxButton.OK, MessageBoxImage.Information);
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
