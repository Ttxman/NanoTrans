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


namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for OnlineTranscription.xaml
    /// </summary>
    public partial class OnlineTranscriptionWindow : Window, INotifyPropertyChanged
    {
        private string _path;

        HttpClient _client;

        public OnlineTranscriptionWindow(string path)
        {
            InitializeComponent();
            Service = _path = path;

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

        OnlineTranscriptionInfo nfo;
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _client = new HttpClient();
            Status = "Connecting to data server";
            try
            {
                //_client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes( string.Format("{0}:{1}","senat","senat2014tul!"))));

                string json = await _client.GetStringAsync(_path);
                nfo = JsonConvert.DeserializeObject<OnlineTranscriptionInfo>(json);

                Status = "Connected";
                Connected = true;
                Service = nfo.site;
                progress.IsIndeterminate = false;
            }
            catch
            {
                MessageBox.Show("Service unavailable", "CannotConnect", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.DialogResult = false;
                Close();
            }
        }

        public WPFTranscription Trans;

        public event PropertyChangedEventHandler PropertyChanged;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            progress.IsIndeterminate = true;
            Status = "Downloading transcription";
            string message = "Authtentication failed.";

            HttpResponseMessage trsxsresponse = null;
            try
            {
                _client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "senat", "senat2014tul!"))));
                trsxsresponse = await _client.GetAsync(nfo.trsxDownloadURL);
            }
            catch { message = "Error occuerd during transfer."; }

            if (trsxsresponse == null || trsxsresponse.StatusCode == System.Net.HttpStatusCode.Forbidden) //authentication failed
            { //authorization failed
                MessageBox.Show(message, "Problem with download", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                return;
            }


            Trans = WPFTranscription.Deserialize(await trsxsresponse.Content.ReadAsStreamAsync());

            if (string.IsNullOrWhiteSpace(Trans.MediaURI))
                try { Trans.MediaURI = Trans.Meta.Element("stream").Element("url").Value; }
                catch { };

            Trans.DocumentID = nfo.documentId;


            this.DialogResult = true;
            Close();
        }
    }


    class OnlineTranscriptionInfo
    {
        public string documentId;
        public string site;
        public string speakersAPI;
        public string trsxDownloadURL;
        public string responseURL;
    }

}
