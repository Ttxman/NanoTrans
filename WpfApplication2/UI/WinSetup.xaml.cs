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
using System.Globalization;
using System.Diagnostics;
using WPFLocalizeExtension.Engine;
using NanoTrans.Properties;
using System.ComponentModel;
using TranscriptionCore;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSetup.xaml
    /// </summary>
    public partial class WinSetup : Window , INotifyPropertyChanged
    {

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName]string caller = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        private SpeakerCollection _speakersDatabase;

        public Settings Settings
        {
            get { return Settings.Default; }
        }

        public WinSetup(SpeakerCollection speakerDB)
        {
            _speakersDatabase = speakerDB;
            InitializeComponent();

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
            if (Settings.OutputDeviceIndex < cbOutputAudioDevices.Items.Count) cbOutputAudioDevices.SelectedIndex = Settings.OutputDeviceIndex;


            string path = Settings.SpeakersDatabasePath;
            try
            {
                if (!path.Contains(":")) //absolute
                {
                    path = Settings.SpeakersDatabasePath;
                }
                path = new FileInfo(path).FullName;

            }
            finally
            {
                tbSpeakerDBPath.Text = path;
            }


            tbTextSize.Text = Settings.SetupTextFontSize.ToString();
            chbShowSpeakerImage.IsChecked = Settings.ShowSpeakerImage;
            slSpeakerImageSize.Value = Settings.MaxSpeakerImageWidth;



            //playback

            decimal val = (decimal)Settings.SlowedPlaybackSpeed;
            if (val >= UpDownSpeed.Minimum.Value && val <= UpDownSpeed.Maximum.Value)
                UpDownSpeed.Value = val;

            val = (decimal)(Settings.WaveformSmallJump.TotalMilliseconds);
            if (val >= UpDownJump.Minimum.Value && val <= UpDownJump.Maximum.Value)
                UpDownJump.Value = val;

            if (AvailableCultures != null && AvailableCultures.Length <= 1)
            {
                int index = AvailableCultures.Select((c, i) => new { c, i }).FirstOrDefault(p => p.c.DisplayName == LocalizeDictionary.Instance.Culture.DisplayName).i;
                LocalizationSelection.SelectedItem = preselectionCulture = AvailableCultures[index];
            }else
                LocalizationBox.Visibility = System.Windows.Visibility.Collapsed;

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
                Settings.Locale = LocalizeDictionary.Instance.Culture.IetfLanguageTag;

            }
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            //properties on setup are from very old version and not binded... all items have to be saved manually
            //spaker database
            Settings.SpeakersDatabasePath= tbSpeakerDBPath.Text;

            //fonts
            try
            {
                Settings.SetupTextFontSize = double.Parse(tbTextSize.Text);
            }
            catch
            {

            }

            //image
            Settings.ShowSpeakerImage = (bool)chbShowSpeakerImage.IsChecked;
            Settings.MaxSpeakerImageWidth = slSpeakerImageSize.Value;

            //playback
            Settings.SlowedPlaybackSpeed = (double)UpDownSpeed.Value;
            Settings.WaveformSmallJump = TimeSpan.FromMilliseconds((double)UpDownJump.Value);

            this.Close();
        }

        private void ButtonLoadSpeakersDatabase_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.SaveFileDialog();
            fileDialog.OverwritePrompt = false;
            fileDialog.Title = Properties.Strings.FileDialogLoadSpeakersDatabaseTitle;
            fileDialog.Filter = string.Format(Properties.Strings.FileDialogLoadSpeakersDatabaseFilter, "*.xml", "*.xml");

            FileInfo fi = new FileInfo(Settings.SpeakersDatabasePath);
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

                tbSpeakerDBPath.Text = Settings.SpeakersDatabasePath = fileDialog.FileName;
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

        public event PropertyChangedEventHandler PropertyChanged;
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
