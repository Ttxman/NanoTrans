using System;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.DirectX.DirectSound;
using DS = Microsoft.DirectX.DirectSound;
using System.Windows;
using System.Collections.Generic;

namespace NanoTrans
{
    /// <summary>
    /// DirectSound waw player
    /// </summary>
    public class DXWavePlayer:IDisposable
    {
        public static int DeviceCount
        {
            get 
            { 
                DevicesCollection devices = new DevicesCollection();
                return devices.Count;
            }
        }


        public static string[] DeviceNamesOUT
        {
            get
            {
                try
                {
                    DevicesCollection devices = new DevicesCollection();
                    string[] sdevices = new string[devices.Count];
                    int index=0;
                    foreach(DS.DeviceInformation device in devices)
                    {
                        sdevices[index] = device.Description;
                        index++;
                    }
                   

                    return sdevices;
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
                int ppos = _soundBuffer.PlayPosition;

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
                return (int)(1000.0*SamplesPlayedThisBuffer / _soundBuffer.Frequency);
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
                    int ppos = _soundBuffer.PlayPosition;
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

                        int samplesPlayed = (ppos % _buffersize) /2;
                        msplayed = (int)(1000.0 * samplesPlayed / _soundBuffer.Frequency);

                        actual = timestamp.Peek().Value + msplayed;

                    return TimeSpan.FromMilliseconds(actual);
                }
            }
        }

        bool _playing = false;
        DS.Device _outputDevice;
        DS.SecondaryBuffer _soundBuffer;
        AutoResetEvent _synchronizer;
        Thread _waitThread;
        DataRequestDelegate _requestproc;
        BufferDescription _buffDescription;
        DS.Notify _notify;
        int _buffersize;
        int _bfpos = 0;

        private static readonly int InternalBufferSizeMultiplier = 10;

        public delegate short[] DataRequestDelegate(out int bufferStartMS);

        public DXWavePlayer(int device, int BufferByteSize, DataRequestDelegate fillProc)
        {
            
            if (BufferByteSize < 1000)
            { 
                throw new ArgumentOutOfRangeException("BufferByteSize","minimal size of buffer is 500 bytes");
            }

            _buffersize = BufferByteSize;
            _requestproc = fillProc;
            DS.DevicesCollection devices = new DevicesCollection();
            if (device <= 0 || device >= devices.Count)
            {
                device = 0;
            }


            int cntr = 0;
            foreach (DeviceInformation inf in devices)
            {
                
                if (cntr == device)
                {
                    _outputDevice = new Device(inf.DriverGuid);
                }
                cntr++;
            }


            System.Windows.Interop.WindowInteropHelper wh = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow);
            _outputDevice.SetCooperativeLevel(wh.Handle, CooperativeLevel.Priority);

            _buffDescription = new BufferDescription();
            _buffDescription.ControlPositionNotify = true;
            _buffDescription.BufferBytes = BufferByteSize * InternalBufferSizeMultiplier;
            _buffDescription.ControlFrequency = true;
            _buffDescription.ControlEffects = true;
            _buffDescription.GlobalFocus = true;
            _buffDescription.CanGetCurrentPosition = true;
            DS.WaveFormat format = new DS.WaveFormat();
            format.BitsPerSample = 16;
            format.BlockAlign = 2;
            format.SamplesPerSecond = 16000;
            format.Channels = 1;
            format.FormatTag = WaveFormatTag.Pcm;
            format.AverageBytesPerSecond = format.SamplesPerSecond * format.BitsPerSample / 8;

            _buffDescription.Format = format;

            _soundBuffer = new SecondaryBuffer(_buffDescription, _outputDevice);
            _synchronizer = new AutoResetEvent(false);

            BufferPositionNotify[] nots = new BufferPositionNotify[InternalBufferSizeMultiplier];

            BufferPositionNotify not;
            int bytepos = 800;
            for (int i = 0; i < InternalBufferSizeMultiplier; i++)
            {
                not = new BufferPositionNotify();
                not.Offset = bytepos;
                not.EventNotifyHandle = _synchronizer.SafeWaitHandle.DangerousGetHandle();
                nots[i] = not;
                bytepos += BufferByteSize;
            }


            _notify = new Notify(_soundBuffer);
            _notify.SetNotificationPositions(nots);


            _waitThread = new Thread(new ThreadStart(DataRequestThread)) { Name = "MyWavePlayer.DataRequestThread" };
            _waitThread.Start();
        }

        ~DXWavePlayer()
        {
            Dispose();
        }
        bool _disposed= false;
        public void Dispose()
        {

            if (!_disposed)
            {
                _disposed = true;

                
                if(_waitThread!=null &&  _waitThread.IsAlive)
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
            _soundBuffer.Write(_bfpos, data, LockFlag.None);
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
                for (int i = 0; i < _buffDescription.BufferBytes-1; i+=2)
                {
                    _soundBuffer.Write(i, one, LockFlag.None);
                }
        }


        private double _speedmod = 1.0;
        public double PlaySpeed
        { 
            get{ return _speedmod;}
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
            _soundBuffer.SetCurrentPosition(0);
            _samplesPlayed = 0;
            _soundBuffer.Frequency = (int)(spedmodification * _buffDescription.Format.SamplesPerSecond);
            _soundBuffer.Play(0, BufferPlayFlags.Looping);
        }


        public void Play()
        {
            Play(1.0);
        }

        public void Pause()
        {
            _pausedAt = PlayPosition;
            _soundBuffer.Stop();
            _soundBuffer.SetCurrentPosition(0);
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
            }catch(ThreadInterruptedException){}
        }


    }
}

