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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Security.Cryptography;
using Ionic.Zip;
using System.Xml.Linq;
using System.Configuration;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        float m_TotalKBytes = 0;
        public float TotalKBytes
        {
            get { lock (this) { return m_TotalKBytes; } }
            set
            {
                lock (this)
                {
                    m_TotalKBytes = value;
                }
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TotalKBytes"));
            }
        }



        public float KBytesDownloaded
        {
            get
            {
                lock (this)
                {
                    if (downloadprogresses.Count>0)
                        return downloadprogresses.Sum(p => p.Value) /1024f;

                    return 0;
                }
            }
            set 
            {
                if (PropertyChanged!=null)
                    PropertyChanged(this, new PropertyChangedEventArgs("KBytesDownloaded"));
            }
        }

        public float KBytesUnpacked
        {
            get 
            { 
                lock (this) 
                {
                    if (unpackprogresses.Count > 0)
                        return unpackprogresses.Sum(p => p.Value) / 1024f;
                    else return 0;
                } 
            }
            set
            {

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("KBytesUnpacked"));
            }
        }


        string m_statusMessage;
        public string StatusMessage
        {
            get { lock (this) { return m_statusMessage; } }
            set
            {
                lock (this)
                {
                    m_statusMessage = value;
                }
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("StatusMessage"));
            }
        }

        bool m_preparingDownload = true;
        public bool PreparingDownload
        {
            get { lock (this) { return m_preparingDownload; } }
            set
            {
                lock (this)
                {
                    m_preparingDownload = value;
                }
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("PreparingDownload"));
            }
        }





        Dictionary<WebClient, long> downloadprogresses = new Dictionary<WebClient, long>();
        Dictionary<ZipFile, long> unpackprogresses = new Dictionary<ZipFile, long>();
        public MainWindow()
        {
            InitializeComponent();

            #region funcs
            downloaddefs = () =>
            {
                string URL = ConfigurationManager.AppSettings["UpdateDefinitions"];
                WebClient client = new WebClient();
                var document = XDocument.Load(new MemoryStream(client.DownloadData(URL)));
                return document.Descendants("Definition").OrderByDescending(d => int.Parse(d.Attribute("Build").Value)).First();
            };

            GetFilelist = () =>
            {
                List<XElement> flist = new List<XElement>();
                if (int.Parse(definition.Attribute("Build").Value) > App.version)
                {
                    var filelist = definition.Descendants("File").ToList();


                    SHA1Cng sha = new SHA1Cng();
                    foreach (var f in filelist)
                    {
                        string file = App.ExeDir + "\\" + f.Attribute("FileName").Value;
                        if (File.Exists(file))
                        {
                            using (var s = File.OpenRead(file))
                            {
                                string h = Convert.ToBase64String(sha.ComputeHash(s));
                                string h2 = f.Attribute("SHA1").Value;
                                if (h != h2)
                                {
                                    flist.Add(f);
                                }
                            }
                        }
                        else
                        {
                            flist.Add(f);
                        }
                    }

                }
                return flist;
            };

            DownloadAndUnpackFile = (f) =>
            {
                WebClient client = new WebClient();
                AutoResetEvent ae = new AutoResetEvent(false);

                int totalsize = int.Parse(f.Attribute("Size").Value);
                

                client.DownloadProgressChanged += (sender, e) =>
                    {
                        lock (this)
                        {
                            downloadprogresses[client] = e.BytesReceived;
                            if (PropertyChanged != null)
                                PropertyChanged(this, new PropertyChangedEventArgs("KBytesDownloaded"));
                        }
                    };
                string path = f.Attribute("FileName").Value;
                string sourcef = definition.Attribute("DataStoreURL").Value + "/" + path.Replace('\\', '/') + ".zip";


                client.DownloadDataCompleted += (_sender, _e) =>
                    {
                        var ms = new MemoryStream(_e.Result);
                        using (var zf = ZipFile.Read(ms))
                        {
                            client.Dispose();
                            zf.ExtractProgress += (sender, e) =>
                                {
                                    if (e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
                                        lock (this)
                                        {
                                            float percentage = (float)e.BytesTransferred / e.TotalBytesToTransfer;
                                            unpackprogresses[zf] = (long)(percentage * totalsize);
                                            if (PropertyChanged != null)
                                                PropertyChanged(this, new PropertyChangedEventArgs("KBytesUnpacked"));
                                        }
                                };
                            string targetf = System.IO.Path.Combine(App.ExeDir, path);
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targetf));
                            using (Stream s = File.Create(targetf))
                                zf.Entries.First().Extract(s);
                        }
                        ae.Set();
                    };


                client.DownloadDataAsync(new Uri(sourcef), new ValueContainer<long>(0));
                ae.WaitOne();
            };
            #endregion
        }

        private class ValueContainer<T>
        {
            public T Value;
            public ValueContainer(T val)
            {
                Value = val;
            }
        }

        #region funcs
        Func<XElement> downloaddefs;
        Func<List<XElement>> GetFilelist;
        Action<XElement> DownloadAndUnpackFile;
        XElement definition;
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StatusMessage = "Stahovani definic updatu...";
            var t1 = new Task<XElement>(downloaddefs, App.CancelWork.Token);
            t1.ContinueWith(DefsLoaded);
            t1.Start();
        }


        private void DefsLoaded(Task<XElement> val)
        {
            StatusMessage = "Analýza souborů k updatu...";
            definition = val.Result;

            var t = new Task<List<XElement>>(GetFilelist, App.CancelWork.Token);
            t.ContinueWith(FilesLoaded);
            t.Start();
        }

        int m_counter =0;
        private void FilesLoaded(Task<List<XElement>> val)
        {
            StatusMessage = "Stahuji soubory k aktualizaci...";
            var flist = val.Result;

            TotalKBytes = flist.Sum(x => int.Parse(x.Attribute("Size").Value) / 1024.0f);


            if (TotalKBytes <= 0)
            {
                RunNanoTransAndExit();
                return;
            }

            PreparingDownload = false;
            foreach (var f in flist)
            {
                XElement xl = f;
                var t = new Task(() => DownloadAndUnpackFile(xl), App.CancelWork.Token);
                t.ContinueWith((tsk) => Decrement());
                m_counter++;
                t.Start();
            }
        }
        private void Decrement()
        {
            lock (this)
            {
                m_counter--;
            }
            RunNanoTransAndExit();
        }

        private void RunNanoTransAndExit()
        {
            if (m_counter == 0)
            {
                StatusMessage = "Update dokončen spouštím NanoTrans";
                Process p = new Process();
                ProcessStartInfo si = new ProcessStartInfo();
                si.UseShellExecute = true;
                si.FileName = "NanoTrans.exe";
                p.StartInfo = si;
                p.Start();
                Thread.Sleep(1000);
                Dispatcher.Invoke(new Action(() => Close()));
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    class BoolToVisConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = !(bool)value;
            if (bValue)
                return Visibility.Visible;
            else
                return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility != Visibility.Visible)
                return true;
            else
                return false;
        }
        #endregion
    }
}
