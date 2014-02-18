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
using System.IO;
using Microsoft.Win32;
using Ionic.Zip;
using System.Linq;
using System.Configuration;
using NanoTrans.Core;
using System.Globalization;
using System.Diagnostics;
using WPFLocalizeExtension.Engine;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSetup.xaml
    /// </summary>
    public partial class WinSetup : Window
    {
        public GlobalSetup _setup;
        private SpeakerCollection _speakersDatabase;

        public WinSetup(GlobalSetup setup, SpeakerCollection speakerDB)
        {
            _setup = setup;
            _speakersDatabase = speakerDB;
            InitializeComponent();

            if (setup != null)
            {
                //audio
                cbOutputAudioDevices.Items.Clear();
                string[] devices = DXWavePlayer.DeviceNamesOUT;
                if (devices != null)
                {
                    foreach (string s in devices)
                    {
                        cbOutputAudioDevices.Items.Add(s);
                    }
                }
                if (_setup.audio.OutputDeviceIndex < cbOutputAudioDevices.Items.Count) cbOutputAudioDevices.SelectedIndex = _setup.audio.OutputDeviceIndex;

                cbInputAudioDevices.Items.Clear();
                if (devices != null)
                {
                    foreach (string s in devices)
                    {
                        cbInputAudioDevices.Items.Add(s);
                    }
                }
                if (_setup.audio.InputDeviceIndex < cbInputAudioDevices.Items.Count) cbInputAudioDevices.SelectedIndex = _setup.audio.InputDeviceIndex;

                string path = setup.SpeakersDatabasePath;
                try
                {
                    if (!path.Contains(":")) //absolute
                    {
                        path = setup.SpeakersDatabasePath;
                    }
                    path = new FileInfo(path).FullName;

                }
                finally
                {
                    tbSpeakerDBPath.Text = path;
                }


                tbTextSize.Text = setup.SetupTextFontSize.ToString();
                chbShowSpeakerImage.IsChecked = setup.ShowSpeakerImage;
                slSpeakerImageSize.Value = setup.MaxSpeakerImageWidth;



                //playback

                decimal val = (decimal)setup.SlowedPlaybackSpeed;
                if (val >= UpDownSpeed.Minimum.Value && val <= UpDownSpeed.Maximum.Value)
                    UpDownSpeed.Value = val;

                val = (decimal)(setup.WaveformSmallJump);
                if (val >= UpDownJump.Minimum.Value && val <= UpDownJump.Maximum.Value)
                    UpDownJump.Value = val;


            }

            //setup.Localization
            int index = AvailableCultures.Select((c, i) => new { c, i }).FirstOrDefault(p => p.c.DisplayName == LocalizeDictionary.Instance.Culture.DisplayName).i;
            LocalizationSelection.SelectedItem = preselectionCulture = AvailableCultures[index];

        }

        private CultureInfo preselectionCulture = LocalizeDictionary.Instance.Culture;

        static CultureInfo[] _AvailableCultures = null;
        public static CultureInfo[] AvailableCultures
        {
            get
            {
                if (_AvailableCultures == null)
                {
                    var programLocation = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    var asname = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                    var resourceFileName = asname + ".resources.dll";

                    var resources = new DirectoryInfo(programLocation).GetFiles(resourceFileName, SearchOption.AllDirectories);
                    _AvailableCultures = resources.Select(f => new CultureInfo(f.Directory.Name)).OrderBy(c => c.NativeName).ToArray();
                }

                return _AvailableCultures;
            }
        }

        private void LocalizationSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocalizationSelection.SelectedItem != null)
            {
                LocalizeDictionary.Instance.Culture = (CultureInfo)LocalizationSelection.SelectedItem;
                _setup.Locale = LocalizeDictionary.Instance.Culture.IetfLanguageTag;
            }
        }

        public static GlobalSetup WinSetupShowDialog(GlobalSetup aNastaveni, SpeakerCollection aDatabazeMluvcich)
        {
            WinSetup ws = new WinSetup(aNastaveni, aDatabazeMluvcich);
            ws.ShowDialog();

            return ws._setup;
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            //properties on setup are from very old version and not binded... all items have to be saved manually

            //audio
            _setup.audio.OutputDeviceIndex = cbOutputAudioDevices.SelectedIndex;
            if (_setup.audio.OutputDeviceIndex < 0) _setup.audio.OutputDeviceIndex = 0;

            _setup.audio.InputDeviceIndex = cbInputAudioDevices.SelectedIndex;
            if (_setup.audio.InputDeviceIndex < 0) _setup.audio.InputDeviceIndex = 0;


            //spaker database
            _setup.SpeakersDatabasePath = tbSpeakerDBPath.Text;

            //fonts
            try
            {
                _setup.SetupTextFontSize = double.Parse(tbTextSize.Text);
            }
            catch
            {

            }

            //image
            _setup.ShowSpeakerImage = (bool)chbShowSpeakerImage.IsChecked;
            _setup.MaxSpeakerImageWidth = slSpeakerImageSize.Value;

            //playback
            _setup.SlowedPlaybackSpeed = (double)UpDownSpeed.Value;
            _setup.WaveformSmallJump = (double)UpDownJump.Value;

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

                tbSpeakerDBPath.Text = _setup.SpeakersDatabasePath = fileDialog.FileName;
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
