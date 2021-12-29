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
using System.Linq;
using System.Configuration;
using System.Globalization;
using System.Diagnostics;
using WPFLocalizeExtension.Engine;
using NanoTrans.Properties;
using System.ComponentModel;
using TranscriptionCore;
using ICSharpCode.SharpZipLib.Zip;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinSetup.xaml
    /// </summary>
    public partial class WinSetup : Window, INotifyPropertyChanged
    {

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        private readonly SpeakerCollection _speakersDatabase;

        internal Settings Settings
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

            foreach (string s in devices)
                cbOutputAudioDevices.Items.Add(s);

            if (Settings.OutputDeviceIndex < cbOutputAudioDevices.Items.Count) cbOutputAudioDevices.SelectedIndex = Settings.OutputDeviceIndex;


            string path = Settings.SpeakersDatabasePath;
            try
            {
                if (!Path.IsPathRooted(path)) //absolute
                    path = Settings.SpeakersDatabasePath;

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

            if (AvailableCultures is { } && AvailableCultures.Length <= 1)
            {
                int index = AvailableCultures.Select((c, i) => new { c, i }).FirstOrDefault(p => p.c.DisplayName == LocalizeDictionary.Instance.Culture.DisplayName).i;
                LocalizationSelection.SelectedItem = AvailableCultures[index];
            }
            else
                LocalizationBox.Visibility = Visibility.Collapsed;

        }

        static CultureInfo[]? _AvailableCultures = null;
        public static CultureInfo[] AvailableCultures
        {
            get
            {
                if (_AvailableCultures is null)
                {
                    var programLocation = Path.GetDirectoryName(Environment.ProcessPath);
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
            if (LocalizationSelection.SelectedItem is CultureInfo cnfo)
            {
                LocalizeDictionary.Instance.Culture = cnfo;
                Settings.Locale = LocalizeDictionary.Instance.Culture.IetfLanguageTag;

            }
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            //properties on setup are from very old version and not binded... all items have to be saved manually
            //spaker database
            Settings.SpeakersDatabasePath = tbSpeakerDBPath.Text;

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
            var fileDialog = new SaveFileDialog();
            fileDialog.OverwritePrompt = false;
            fileDialog.Title = Properties.Strings.FileDialogLoadSpeakersDatabaseTitle;
            fileDialog.Filter = string.Format(Properties.Strings.FileDialogLoadSpeakersDatabaseFilter, "*.xml", "*.xml");

            FileInfo fi = new FileInfo(Settings.SpeakersDatabasePath);
            if (fi.Directory.Exists)
                fileDialog.InitialDirectory = fi.DirectoryName;
            else
                fileDialog.InitialDirectory = FilePaths.DefaultDirectory;

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
            if (s is null)
                MessageBox.Show(this, Strings.MessageBoxDictionaryDownloadError, Strings.MessageBoxDictionaryDownloadErrorCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
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
            using ZipFile zf = new ZipFile(s);
            var entry = zf.Cast<ZipEntry>().Where(en => en.IsFile && en.Name.EndsWith(".aff")).ToArray();

            if (entry.Length > 0)
            {
                var aff = entry[0];
                var dic = zf.GetEntry(Path.GetFileNameWithoutExtension(aff.Name) + ".dic");

                if (dic is { })
                {
                    if (MessageBox.Show(this, string.Format(Properties.Strings.MessageBoxDictionaryConfirmLoad, Path.GetFileNameWithoutExtension(aff.Name)), Properties.Strings.MessageBoxQuestionCaption, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var readme = zf.Cast<ZipEntry>().Where(en => en.Name.ToLower().Contains("readme")).FirstOrDefault();
                        if (readme is { })
                        {
                            string ss = new StreamReader(zf.GetInputStream(readme)).ReadToEnd();

                            if (TextWallWindow.ShowWall(true, Properties.Strings.OODictionaryLicenceTitle, ss))
                            {
                                SpellChecker.SpellEngine = null;

                                File.WriteAllText(FilePaths.GetWritePath("data\\readme_slovniky.txt"), ss);
                                string p = FilePaths.GetWritePath("data\\cs_CZ.aff");

                                using (Stream fs = File.Create(p))
                                {
                                    using var ext = zf.GetInputStream(aff);
                                    ext.CopyTo(fs);
                                }
                                p = FilePaths.GetWritePath("data\\cs_CZ.dic");

                                using (Stream fs = File.Create(p))
                                {
                                    using var ext = zf.GetInputStream(dic);
                                    ext.CopyTo(fs);
                                }

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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                null => Visibility.Collapsed,
                _ => Visibility.Visible
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("unexpected Convertback");
        }
    }

    public class CollapseOnNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                { } => Visibility.Collapsed,
                _ => Visibility.Visible
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("unexpected Convertback");
        }
    }


}
