using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Newtonsoft;
using System.Net.Http;
using Newtonsoft.Json;
using System.ComponentModel;
using NanoTrans.OnlineAPI;
using System.Xml.Linq;


namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for OnlineTranscription.xaml
    /// </summary>
    public partial class OnlineTranscriptionWindow : Window, INotifyPropertyChanged
    {

        public OnlineTranscriptionWindow(SpeakersApi speakersApi)
        {
            InitializeComponent();
            this._api = speakersApi;
        }

        string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Status"));
            }
        }

        string _Service = "...";
        public string Service
        {
            get { return _Service; }
            set
            {
                _Service = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Service"));
            }
        }

        bool _Connected = false;
        public bool Connected
        {
            get { return _Connected; }
            set
            {
                _Connected = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Connected"));
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Status = "Connecting to data server";
            try
            {
                await _api.LoadInfo();
                Status = "Connected";
                Connected = true;
                Service = _api.Info.Site.AbsoluteUri;
                progress.IsIndeterminate = false;
            }
            catch
            {
                MessageBox.Show("Service unavailable", "CannotConnect", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.DialogResult = false;
                Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private SpeakersApi _api;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            progress.IsIndeterminate = true;
            Status = "Downloading transcription";
            string message = "Authtentication failed.";



            _api.UserName = this.Login.Text;
            _api.Password = this.Password.Password;
            try
            {
                await _api.Login();
            }
            catch { message = "Error occured during login."; }

            if (!_api.LogedIn) //authentication failed
            { //authorization failed
                MessageBox.Show(message, "Login failed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            HttpResponseMessage trsxsresponse = await _api.GetUrl(_api.Info.TrsxDownloadURL);
            //string what = await trsxsresponse.Content.ReadAsStringAsync();
            if (trsxsresponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string mes = await trsxsresponse.Content.ReadAsStringAsync();
                MessageBox.Show("Problem with download", "Problem with download", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            try
            {
                _api.Trans = WPFTranscription.Deserialize(await trsxsresponse.Content.ReadAsStreamAsync());
                _api.Trans.IsOnline = true;
                _api.Trans.Api = _api;

                _api.Trans.Meta.Add(JsonConvert.DeserializeXNode(JsonConvert.SerializeObject(_api.Info),"OnlineInfo").Root);
                //_api.Trans.FileName = _api.Info.ResponseURL.AbsolutePath;
            }
            catch
            {
                MessageBox.Show("document is in wrong format", "document is in wrong format", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (string.IsNullOrWhiteSpace(_api.Trans.MediaURI))
                try { _api.Trans.MediaURI = _api.Trans.Meta.Element("stream").Element("url").Value; }
                catch { };

            _api.Trans.DocumentID = _api.Info.DocumentId;
            Status = "Loading speakers from databse";

            var sp1 = await _api.ListSpeakers(_api.Trans.Speakers.Select(s => s.DBID));
            var respeakers = sp1.ToArray();

            for (int i = 0; i < respeakers.Length; i++)
            {
                var replacement = respeakers[i];
                var dbs = _api.Trans.Speakers.GetSpeakerByDBID(replacement.DBID);
                if (dbs != null)
                {
                    replacement.PinnedToDocument = dbs.PinnedToDocument | replacement.PinnedToDocument;
                    if (replacement.MainID != null)
                        replacement.DBID = replacement.MainID;

                    _api.Trans.ReplaceSpeaker(dbs, respeakers[i]);
                }
            }

            this.DialogResult = true;
            
            Close();
        }
    }




}
