using System;
using System.Threading;
using System.Runtime.InteropServices;
using SharpDX.DirectSound;
using DS = SharpDX.DirectSound;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Multimedia;

namespace NanoTrans
{
    /// <summary>
    /// DirectSound waw player
    /// </summary>
    public class DXWavePlayer : IDisposable
    {
        public static int DeviceCount
        {
            get
            {
                return DirectSound.GetDevices().Count;
            }
        }


        public static string[] DeviceNamesOUT
        {
            get
            {
                try
                {
                    return DirectSound.GetDevices().Select(d => d.Description).ToArray();
                }
                catch
                {
                    return null;
                }
            }
        }


        public bool Playing
        {
            get { return _playing; }
            set
            {
                if (_playing == value)
                    return;

                if (value)
                {
                    Play();
                }
                else
                {
                    Pause();
                }
            }
        }


        int _samplesPlayed = 0;


        public int SamplesPlayedThisBuffer
        {
            get
            {
                int actual = 0;
                int ppos, wpos;
                _soundBuffer.GetCurrentPosition(out ppos,out wpos);

                int pos = (ppos / _buffersize) * _buffersize;

                actual = ppos - pos;


                actual /= 2;
                return actual;

            }

        }


        public int MSplayedThisBufer
        {
            get
            {
                return (int)(1000.0 * SamplesPlayedThisBuffer / _soundBuffer.Frequency);
            }

        }

        public TimeSpan PlayedThisBuffer
        {
            get { return TimeSpan.FromMilliseconds(MSplayedThisBufer); }
        }

        public int SamplesPlayedThisSession
        {
            get
            {
                return _samplesPlayed + SamplesPlayedThisBuffer;
            }
        }

        public int MSPlayedThisSession
        {
            get { return SamplesPlayedThisSession / _soundBuffer.Frequency; }
        }

        public TimeSpan PlayedThisSession
        {
            get { return TimeSpan.FromMilliseconds((double)SamplesPlayedThisSession / _soundBuffer.Frequency); }
        }

        public TimeSpan PlayPosition
        {
            get
            {
                lock (this)
                {
                    if (timestamp == null || timestamp.Count == 0)
                        return TimeSpan.Zero;

                    int actual = 0;
                    int ppos, wpos;
                    _soundBuffer.GetCurrentPosition(out ppos, out wpos);
                    int pos = (ppos / _buffersize);


                    lock (timestamp)
                    {
                        if (timestamp.Count == 0)
                            return TimeSpan.Zero;
                        if (pos != timestamp.Peek().Key)
                        {
                            timestamp.Dequeue();
                        }

                        if (timestamp.Count == 0)
                            return TimeSpan.Zero;
                    }

                    int msplayed = 0;

                    int samplesPlayed = (ppos % _buffersize) / 2;
                    msplayed = (int)(1000.0 * samplesPlayed / _soundBuffer.Frequency);

                    actual = timestamp.Peek().Value + msplayed;

                    return TimeSpan.FromMilliseconds(actual);
                }
            }
        }

        bool _playing = false;
        DirectSound _outputDevice;
        SecondarySoundBuffer _soundBuffer;
        AutoResetEvent _synchronizer;
        Thread _waitThread;
        DataRequestDelegate _requestproc;
        SoundBufferDescription _buffDescription;
        int _buffersize;
        int _bfpos = 0;

        private static readonly int InternalBufferSizeMultiplier = 10;

        public delegate short[] DataRequestDelegate(out int bufferStartMS);

        public DXWavePlayer(int device, int BufferByteSize, DataRequestDelegate fillProc)
        {

            if (BufferByteSize < 1000)
            {
                throw new ArgumentOutOfRangeException("BufferByteSize", "minimal size of buffer is 1000 bytes");
            }

            _buffersize = BufferByteSize;
            _requestproc = fillProc;
            var devices = DirectSound.GetDevices();
            if (device <= 0 || device >= devices.Count)
            {
                device = 0;
            }

            _outputDevice = new DirectSound(devices[device].DriverGuid);


            System.Windows.Interop.WindowInteropHelper wh = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow);
            _outputDevice.SetCooperativeLevel(wh.Handle, CooperativeLevel.Priority);

            _buffDescription = new SoundBufferDescription();
            _buffDescription.Flags = BufferFlags.ControlPositionNotify | BufferFlags.ControlFrequency | BufferFlags.ControlEffects | BufferFlags.GlobalFocus | BufferFlags.GetCurrentPosition2;
            _buffDescription.BufferBytes = BufferByteSize * InternalBufferSizeMultiplier;

            WaveFormat format = new WaveFormat(16000, 16, 1);

            _buffDescription.Format = format;

            _soundBuffer = new SecondarySoundBuffer(_outputDevice, _buffDescription);
            _synchronizer = new AutoResetEvent(false);

            NotificationPosition[] nots = new NotificationPosition[InternalBufferSizeMultiplier];

            NotificationPosition not;
            int bytepos = 800;
            for (int i = 0; i < InternalBufferSizeMultiplier; i++)
            {
                not = new NotificationPosition();
                not.Offset = bytepos;
                not.WaitHandle = _synchronizer;
                nots[i] = not;
                bytepos += BufferByteSize;
            }

            _soundBuffer.SetNotificationPositions(nots);


            _waitThread = new Thread(new ThreadStart(DataRequestThread)) { Name = "MyWavePlayer.DataRequestThread" };
            _waitThread.Start();
        }

        ~DXWavePlayer()
        {
            Dispose();
        }
        bool _disposed = false;
        public void Dispose()
        {

            if (!_disposed)
            {
                _disposed = true;


                if (_waitThread != null && _waitThread.IsAlive)
                {
                    _soundBuffer.Stop();
                    _waitThread.Interrupt();
                    _waitThread.Join();
                }
                _outputDevice.Dispose();
            }
        }

        Queue<KeyValuePair<int, int>> timestamp = new Queue<KeyValuePair<int, int>>();
        private void WriteNextData(short[] data, int timems)
        {
            _soundBuffer.Write(data, _bfpos, LockFlags.None);
            lock (timestamp)
            {
                timestamp.Enqueue(new KeyValuePair<int, int>(_bfpos / _buffersize, timems));
            }
            _samplesPlayed += data.Length;
            _bfpos += 2 * data.Length;
            _bfpos %= _buffDescription.BufferBytes;

        }


        private void ClearBuffer()
        {
            timestamp.Clear();

            short[] one = new short[] { 0 };
            for (int i = 0; i < _buffDescription.BufferBytes - 1; i += 2)
            {
                _soundBuffer.Write(one, i, LockFlags.None);
            }
        }


        private double _speedmod = 1.0;
        public double PlaySpeed
        {
            get { return _speedmod; }
        }


        public void Play(double spedmodification)
        {
            _speedmod = spedmodification;
            ClearBuffer();
            _bfpos = 0;

            if (_requestproc != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    int timems;
                    short[] data = _requestproc.Invoke(out timems);
                    if (data != null && data.Length > 0)
                        WriteNextData(data, timems);
                }
            }
            _soundBuffer.CurrentPosition = 0;
            _samplesPlayed = 0;
            _soundBuffer.Frequency = (int)(spedmodification * _buffDescription.Format.SampleRate);
            _soundBuffer.Play(0, PlayFlags.Looping);
        }


        public void Play()
        {
            Play(1.0);
        }

        public void Pause()
        {
            _pausedAt = PlayPosition;
            _soundBuffer.Stop();
            _soundBuffer.CurrentPosition = 0;
        }

        private TimeSpan _pausedAt = TimeSpan.Zero;
        public TimeSpan PausedAt
        {
            get { return _pausedAt; }
        }

        private void RetrieveData()
        {
            int timems;
            short[] data = _requestproc.Invoke(out timems);

            if (data == null || data.Length == 0)
            {
                Pause();
            }
            else
            {
                WriteNextData(data, timems);
            }

        }

        private void DataRequestThread()
        {
            try
            {
                while (true)
                {
                    _synchronizer.WaitOne();
                    RetrieveData();

                }
            }
            catch (ThreadInterruptedException) { }
        }


    }
}

