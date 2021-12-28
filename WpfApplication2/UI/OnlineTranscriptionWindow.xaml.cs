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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
            }
        }

        string _Service = "...";
        public string Service
        {
            get { return _Service; }
            set
            {
                _Service = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Service"));
            }
        }

        bool _Connected = false;
        public bool Connected
        {
            get { return _Connected; }
            set
            {
                _Connected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connected"));
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
                Login.Focus();
            }
            catch
            {
                MessageBox.Show("Service unavailable", "CannotConnect", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.DialogResult = false;
                Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private readonly SpeakersApi _api;

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

            Status = "Loading Transcription";
            await _api.DownloadTranscription();

            this.DialogResult = true;
            
            Close();
        }

        private void Password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click(this, null);
            }
        }
    }




}
