using System;
using System.Collections.Generic;
using System.Linq;
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
using System.ComponentModel;
using System.Net;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for DownloadOneFileWindow.xaml
    /// </summary>
    public partial class DownloadOneFileWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string path;
        private Stream stream;
        private DownloadOneFileWindow()
        {
            InitializeComponent();
        }
        float m_TotalBytes = 1000;
        public float TotalBytes
        {
            get { lock (this) { return m_TotalBytes; } }
            set
            {
                lock (this)
                {
                    m_TotalBytes = (float)Math.Floor(value/1024f);
                }
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TotalBytes"));
            }
        }


        float m_Bytes = 0;
        public float BytesDownloaded
        {
            get { lock (this) { return m_Bytes; } }
            set
            {
                lock (this)
                {
                    m_Bytes = (float)Math.Floor(value / 1024f);
                }
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("BytesDownloaded"));
            }
        }

        public static Stream DownloadFile(string path)
        {
            try
            {
                var w = new DownloadOneFileWindow();
                w.path = path;
                w.Title += ": " + path;
                w.ShowDialog();
                if (w.DialogResult == true)
                {
                    return w.stream;
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            client.CancelAsync();
            this.DialogResult = false;
            stream = null;
            Close();
        }

        WebClient client = new WebClient();
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            client.DownloadProgressChanged += (ssender, ee) =>
                {
                    TotalBytes = ee.TotalBytesToReceive;
                    BytesDownloaded = ee.BytesReceived;

                };

            client.DownloadDataCompleted += (ssender, ee) =>
                {
                    if (ee.Cancelled)
                    {
                        return;
                    }
                    else
                    {
                        this.DialogResult = true;
                        stream = new MemoryStream(ee.Result);
                    }
                    Close();
                };


            try
            {
                client.DownloadDataAsync(new Uri(path));
            }
            catch
            {
                this.DialogResult = false;
                stream = null;
                Close();
            }
        }


    }
}
