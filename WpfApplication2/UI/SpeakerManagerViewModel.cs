using NanoTrans.Core;
using NanoTrans.OnlineAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;

namespace NanoTrans
{
    public class SpeakerManagerViewModel : INotifyPropertyChanged
    {

        private string _filterstring = "";

        List<SpeakerContainer> _allSpeakers;

        SpeakerCollection _local;
        List<Speaker> _online = new List<Speaker>();
        SpeakerCollection _document;
        List<Speaker> _temp = new List<Speaker>();



        ICollectionView _view;
        private SpeakersApi _api;
        public SpeakerManagerViewModel(SpeakerCollection documentSpeakers, SpeakerCollection localSpeakers, SpeakersApi api)
        {
            _api = api;
            if (_api != null)
            {
                _loadingTimer = new System.Timers.Timer(1000);
                _loadingTimer.AutoReset = false;
                _loadingTimer.Elapsed += _loadingTimer_Elapsed;

            }

            this._document = documentSpeakers;
            this._local = localSpeakers;

            ReloadSpeakers();
        }


        private void ReloadSpeakers()
        {
            var all = new List<SpeakerContainer>();

            if (_document != null && ShowDocument)
                all.AddRange(_document.Select(s => new SpeakerContainer(_document, s)));

            if (_local != null && ShowLocal)
                all.AddRange(_local.Select(s => new SpeakerContainer(_local, s)));

            if (_online != null && ShowOnline)
                all.AddRange(_online.Select(s => new SpeakerContainer(s)));

            if (_temp != null)
                all.AddRange(_temp.Select(s => new SpeakerContainer(s)));



            _allSpeakers = Deduplicate(all).ToList();


            _allSpeakers.Sort((x, y) => { int cmp = string.Compare(x.SurName, y.SurName); return (cmp != 0) ? cmp : string.Compare(x.FirstName, y.FirstName); });

            View = CollectionViewSource.GetDefaultView(_allSpeakers);
            UpdateFilters();
        }

        private IEnumerable<SpeakerContainer> Deduplicate(IEnumerable<SpeakerContainer> input)
        {
            var donotdeduplicate = input.Where(s => s.Speaker.DBType == DBType.File || s.Speaker.DBID == "");

            return input
                .Except(donotdeduplicate)
                .GroupBy(s => s.Speaker.DBID)
                .Select(g => g.FirstOrDefault(s => s.Speaker is ApiSynchronizedSpeaker) ?? g.FirstOrDefault(s => s.Speaker.DBType == DBType.User) ?? g.First())
                .Concat(donotdeduplicate);
        }

        private IEnumerable<Speaker> Deduplicate(IEnumerable<Speaker> input)
        {
            return input.GroupBy(s => s.DBID)
                .Select(g => g.FirstOrDefault(s => s is ApiSynchronizedSpeaker) ?? g.FirstOrDefault(s => s.DBType == DBType.User) ?? g.First());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string FilterString
        {
            get { return _filterstring; }
            set
            {
                _filterstring = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FilterString"));
                if (_api != null)
                {
                    UpdateOnlineSpeakers();
                }
                UpdateFilters();
            }
        }


        Visibility _LoadingVisible = Visibility.Collapsed;
        public Visibility LoadingVisible
        {
            get { return _LoadingVisible; }
            set
            {
                _LoadingVisible = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("LoadingVisible"));
            }
        }

        System.Timers.Timer _loadingTimer;
        bool _loadingFilterChanged = false;


        void _loadingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {

            if (_loadingFilterChanged)
            {
                UpdateOnlineSpeakers();
            }
            _loadingFilterChanged = false;
        }

        string _lastFilterString = null;
        private void UpdateOnlineSpeakers()
        {
            if (_lastFilterString != _filterstring)
            {
                _loadingFilterChanged = true;
                if (_filterstring != null && _filterstring.Length > 1 && !_loadingTimer.Enabled)
                {
                    _loadingTimer.Start();
                    _api.Cancel();
                    LoadingVisible = Visibility.Visible;
                    Task.Run(async () =>
                    {
                        var speakerst = await _api.SimpleSearch(_filterstring);
                        UpdateOnlineSpeakers(speakerst);
                        LoadingVisible = Visibility.Collapsed;
                    });

                    _lastFilterString = _filterstring;
                }
            }

        }

        private void UpdateOnlineSpeakers(IEnumerable<ApiSynchronizedSpeaker> speakerst)
        {
            _online = speakerst.Cast<Speaker>().ToList();
            ReloadSpeakers();
        }

        /// <summary>
        /// returns false if user cancels
        /// </summary>
        /// <param name="speaker"></param>
        /// <returns></returns>
        private async Task<bool> SaveOnlineSpeakersUsavedChanges(IEnumerable<Speaker> unsavedSpeakers, bool useMessagebox = true)
        {
            if (unsavedSpeakers.Count() <= 0)
                return true;

            bool update = !useMessagebox;
            if (useMessagebox)
            {
                var result = MessageBox.Show(Properties.Strings.SpeakersManagerSaveUnsavedOnlineSpeakersDialogQuestion, Properties.Strings.SpeakersManagerSaveUnsavedOnlineSpeakersDialogText, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                if (result != MessageBoxResult.Cancel)
                {
                    if (result == MessageBoxResult.Yes)
                        update = true;
                }
            }



            if (update)
            {
                foreach (ApiSynchronizedSpeaker speaker in unsavedSpeakers)
                {
                    if (speaker.IsSaved)
                        await _api.UpdateSpeaker(speaker);
                    else
                        await _api.AddSpeaker(speaker);
                }

                return true;
            }
            return false;
        }


        private class ApiSpeakerComparer : IEqualityComparer<ApiSynchronizedSpeaker>
        {

            public bool Equals(ApiSynchronizedSpeaker x, ApiSynchronizedSpeaker y)
            {
                return x.DBID == y.DBID;
            }

            public int GetHashCode(ApiSynchronizedSpeaker obj)
            {
                return obj.DBID.GetHashCode();
            }
        }


        public ICollectionView View
        {
            get
            {
                return _view;
            }
            set
            {
                _view = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("View"));
            }
        }
        #region filterproperties
        private bool _showDocument = true;
        private bool _showLocal = true;
        private bool _showOnline = true;

        /// <summary>
        /// Is connected to online storage?
        /// </summary>
        public bool IsOnline
        {
            get { return _api != null; }
        }

        public bool ShowDocument
        {
            get { return _showDocument; }
            set
            {
                _showDocument = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowDocument"));
                ReloadSpeakers();
            }
        }

        public bool ShowLocal
        {
            get { return _showLocal; }
            set
            {
                _showLocal = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowLocal"));

                ReloadSpeakers();
            }
        }
        public bool ShowOnline
        {
            get { return _showOnline; }
            set
            {
                _showOnline = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowOnline"));
                ReloadSpeakers();
            }
        }
        #endregion


        public bool ContainsSpeaker(Speaker sp)
        {
            return _allSpeakers.Any(s => s.Speaker == sp);
        }

        int DeferCounter = 0;
        private class Deferer : IDisposable
        {
            IDisposable viewDefer = null;
            public Deferer(SpeakerManagerViewModel speakerManagerViewModel)
            {
                this.speakerManagerViewModel = speakerManagerViewModel;
                lock (locker)
                {
                    Interlocked.Increment(ref speakerManagerViewModel.DeferCounter);
                    viewDefer = speakerManagerViewModel._view.DeferRefresh();
                }
            }

            bool disposed = false;
            object locker = new object();
            private SpeakerManagerViewModel speakerManagerViewModel;
            public void Dispose()
            {
                if (!disposed)
                {
                    lock (locker)
                    {
                        if (!disposed)
                        {
                            disposed = true;
                            Interlocked.Decrement(ref speakerManagerViewModel.DeferCounter);
                            viewDefer.Dispose();
                            speakerManagerViewModel.Refresh();
                        }
                    }
                }
            }
        }

        public IDisposable DeferRefresh()
        {
            return new Deferer(this);
        }

        public void Refresh()
        {
            if (DeferCounter == 0)
                _view.Refresh();
        }


        public SpeakerContainer GetContainerForSpeaker(Speaker sp)
        {
            return _allSpeakers.FirstOrDefault(s => s.Speaker == sp);
        }

        public void DeleteSpeaker(Speaker s)
        {
            _local.Remove(s);
            _online.Remove(s);
            _document.Remove(s);
            _temp.Remove(s);

            var cont = _allSpeakers.FirstOrDefault(sc => sc.Speaker == s);

            if (cont != null)
            {
                _allSpeakers.Remove(cont);
                Refresh();
            }
        }

        internal void AddOnlineSpeaker(ApiSynchronizedSpeaker sp)
        {
            _online.Add(sp);
            _allSpeakers.Add(new SpeakerContainer(_document, sp));
            Refresh();
        }

        internal void AddLocalSpeaker(Speaker sp)
        {
            _local.Add(sp);
            _allSpeakers.Add(new SpeakerContainer(_local, sp));
            Refresh();
        }

        internal void AddTempSpeaker(Speaker sp)
        {
            _temp.Add(sp);
            _allSpeakers.Add(new SpeakerContainer(sp));
            Refresh();
        }

        private bool FilterItems(object item)
        {
            SpeakerContainer cont = item as SpeakerContainer;

            bool res = false;
            if (cont == null)
                return false;

            if (_showDocument && cont.IsDocument)
                res = true;

            if (_showLocal && cont.IsLocal)
                res = true;

            if (_showOnline && cont.IsOnline)
                res = true;

            if (!string.IsNullOrWhiteSpace(_filterstring))
            {


                string flover = _filterstring.ToLower();
                res &= (cont.FullName.ToLower().Contains(flover) |
                        cont.DegreeBefore.ToLower().Contains(flover) |
                        cont.DegreeAfter.ToLower().Contains(flover) |
                        cont.Language.ToLower().Contains(flover));
            }

            return res;
        }

        private void UpdateFilters()
        {
            using (DeferRefresh())
            {
                _view.Filter = null;
                _view.Filter = FilterItems;
            }

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("FilterString"));
        }

        internal async Task<bool> CloseConnection()
        {
            if (_api != null)
            {
                return await SaveOnlineSpeakersUsavedChanges(_allSpeakers.Where(sc => sc.IsOnline && sc.Changed).Select(sc => sc.Speaker));
            }

            return true;
        }


        internal async Task<Speaker[]> FindSimilar(SpeakerContainer spk)
        {
            List<Speaker> all = new List<Speaker>();
            if (IsOnline)
            {
                var result = await _api.SimpleSearch(spk.FullName);
                all.AddRange(result);
            }

            all.AddRange(_allSpeakers.Where(s => s.FullName == spk.FullName).Select(c=>c.Speaker).Except(_temp));
            return Deduplicate(all).ToArray();
        }

    }
}
