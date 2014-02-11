using System;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.DirectX.DirectSound;
using DS = Microsoft.DirectX.DirectSound;
using System.Windows;
using System.Collections.Generic;

namespace NanoTrans
{
    //public delegate short[] DataRequestProc();


    /// <summary>
    /// prepracovano na direct sound;
    /// </summary>
    public class MyWavePlayer:IDisposable
    {
        public static int DeviceCount
        {
            get 
            { 
                DevicesCollection devices = new DevicesCollection();
                return devices.Count;
            }
        }

        /// <summary>
        /// vrati seznam vsech zarizeni schopnych prehravat wav
        /// </summary>
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
            get { return m_playing; }
            set 
            {
                if (m_playing == value)
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


        int m_samplesPlayed = 0;


        public int SamplesPlayedThisBuffer
        {
            get
            {
                int actual = 0;
                int ppos = m_soundBuffer.PlayPosition;

                int pos = (ppos / m_buffersize) * m_buffersize;//integery nasobeni zaokrouhleni na buffery

                actual = ppos - pos;


                actual /= 2;
                return actual;

            }
        
        }


        public int MSplayedThisBufer
        {
            get 
            {
                return (int)(1000.0*SamplesPlayedThisBuffer / m_soundBuffer.Frequency);
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
                return m_samplesPlayed + SamplesPlayedThisBuffer; 
            }
        }

        public int MSPlayedThisSession
        {
            get { return SamplesPlayedThisSession / m_soundBuffer.Frequency; }
        }

        public TimeSpan PlayedThisSession
        {
            get { return TimeSpan.FromMilliseconds((double)SamplesPlayedThisSession / m_soundBuffer.Frequency); }
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
                    int ppos = m_soundBuffer.PlayPosition;
                    int pos = (ppos / m_buffersize); // index casti bufferu


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

                        int samplesPlayed = (ppos % m_buffersize) /2;
                        msplayed = (int)(1000.0 * samplesPlayed / m_soundBuffer.Frequency);

                        actual = timestamp.Peek().Value + msplayed;

                    return TimeSpan.FromMilliseconds(actual);
                }
            }
        }

        bool m_playing = false;
        DS.Device m_outputDevice;
        DS.SecondaryBuffer m_soundBuffer;
        AutoResetEvent m_synchronizer;
        Thread m_waitThread;
        DataRequestDelegate m_requestproc;
        BufferDescription m_buffDescription;
        DS.Notify m_notify;
        int m_buffersize;
        int m_bfpos = 0;

        private static readonly int InternalBufferSizeMultiplier = 10;

        public delegate short[] DataRequestDelegate(out int bufferStartMS);

        public MyWavePlayer(int device, int BufferByteSize, DataRequestDelegate fillProc)
        {
            
            if (BufferByteSize < 1000)
            { 
                throw new ArgumentOutOfRangeException("BufferByteSize","minimal size of buffer is 500 bytes");
            }

            m_buffersize = BufferByteSize;
            m_requestproc = fillProc;
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
                    m_outputDevice = new Device(inf.DriverGuid);
                }
                cntr++;
            }


            System.Windows.Interop.WindowInteropHelper wh = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow);
            m_outputDevice.SetCooperativeLevel(wh.Handle, CooperativeLevel.Priority);

            m_buffDescription = new BufferDescription();
            m_buffDescription.ControlPositionNotify = true;
            m_buffDescription.BufferBytes = BufferByteSize * InternalBufferSizeMultiplier;
            m_buffDescription.ControlFrequency = true;
            m_buffDescription.ControlEffects = true;
            m_buffDescription.GlobalFocus = true;
            m_buffDescription.CanGetCurrentPosition = true;
            DS.WaveFormat format = new DS.WaveFormat();
            format.BitsPerSample = 16;
            format.BlockAlign = 2;
            format.SamplesPerSecond = 16000;
            format.Channels = 1;
            format.FormatTag = WaveFormatTag.Pcm;
            format.AverageBytesPerSecond = format.SamplesPerSecond * format.BitsPerSample / 8;

            m_buffDescription.Format = format;

            m_soundBuffer = new SecondaryBuffer(m_buffDescription, m_outputDevice);
            m_synchronizer = new AutoResetEvent(false);

            BufferPositionNotify[] nots = new BufferPositionNotify[InternalBufferSizeMultiplier];

            BufferPositionNotify not;
            int bytepos = 800;
            for (int i = 0; i < InternalBufferSizeMultiplier; i++)
            {
                not = new BufferPositionNotify();
                not.Offset = bytepos;
                not.EventNotifyHandle = m_synchronizer.SafeWaitHandle.DangerousGetHandle();
                nots[i] = not;
                bytepos += BufferByteSize;
            }


            m_notify = new Notify(m_soundBuffer);
            m_notify.SetNotificationPositions(nots);


            m_waitThread = new Thread(new ThreadStart(DataRequestThread)) { Name = "MyWavePlayer.DataRequestThread" };
            m_waitThread.Start();
        }

        ~MyWavePlayer()
        {
            Dispose();
        }
        bool m_disposed= false;
        public void Dispose()
        {

            if (!m_disposed)
            {
                m_disposed = true;

                
                if(m_waitThread!=null &&  m_waitThread.IsAlive)
                {
                    m_soundBuffer.Stop();
                    m_waitThread.Interrupt();
                    m_waitThread.Join();   
                }
                m_outputDevice.Dispose();
            }
        }

        Queue<KeyValuePair<int, int>> timestamp = new Queue<KeyValuePair<int, int>>();
        private void WriteNextData(short[] data, int timems)
        {
            m_soundBuffer.Write(m_bfpos, data, LockFlag.None);
            lock (timestamp)
            {
                timestamp.Enqueue(new KeyValuePair<int, int>(m_bfpos / m_buffersize, timems));
            }
            m_samplesPlayed += data.Length;
            m_bfpos += 2 * data.Length;
            m_bfpos %= m_buffDescription.BufferBytes;

        }


        private void ClearBuffer()
        {
            try
            {
                timestamp.Clear();

                short[] one = new short[] { 0 };
                for (int i = 0; i < m_buffDescription.BufferBytes-1; i+=2)
                {
                    m_soundBuffer.Write(i, one, LockFlag.None);
                }
            }
            catch (Exception e)
            { 
                MyLog.LogujChybu(e); 
            }
        }


        private double m_speedmod = 1.0;
        public double PlaySpeed
        { 
            get{ return m_speedmod;}
        }


        public void Play(double spedmodification)
        {
            m_speedmod = spedmodification;
            ClearBuffer();
            m_bfpos = 0;
           
            if (m_requestproc != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    int timems;
                    short[] data = m_requestproc.Invoke(out timems);
                    if (data != null && data.Length > 0)
                        WriteNextData(data, timems);
                }
            }
            m_soundBuffer.SetCurrentPosition(0);
            m_samplesPlayed = 0;
            m_soundBuffer.Frequency = (int)(spedmodification * m_buffDescription.Format.SamplesPerSecond);
            m_soundBuffer.Play(0, BufferPlayFlags.Looping);
        }


        public void Play()
        {
            Play(1.0);
        }

        public void Pause()
        {
            m_pausedAt = PlayPosition;
            m_soundBuffer.Stop();
            m_soundBuffer.SetCurrentPosition(0);
        }

        private TimeSpan m_pausedAt = TimeSpan.Zero;
        public TimeSpan PausedAt
        {
            get { return m_pausedAt; }
        }

        private void RetrieveData()
        {
            int timems;
            short[] data = m_requestproc.Invoke(out timems);

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
                    m_synchronizer.WaitOne();
                    RetrieveData();
               
                }
            }catch(ThreadInterruptedException){}
        }


    }
}

