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
                if (m_soundBuffer.PlayPosition > m_bfpos)
                    actual = m_soundBuffer.PlayPosition - m_bfpos;
                else
                    actual = (m_buffDescription.BufferBytes + m_soundBuffer.PlayPosition) - m_bfpos;

                actual /= 2;
                System.Diagnostics.Debug.WriteLine("s_"+actual);
                return actual;

            }
        
        }


        public int MSplayedThisBufer
        {
            get 
            {
                return (int)(1000.0*SamplesPlayedThisSession / m_soundBuffer.Frequency);
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


        bool m_playing = false;
        DS.Device m_outputDevice;
        DS.SecondaryBuffer m_soundBuffer;
        AutoResetEvent m_synchronizer;
        Thread m_waitThread;
        Func<short[]> m_requestproc;
        BufferDescription m_buffDescription;
        DS.Notify m_notify;


        public MyWavePlayer(int device, int BufferByteSize, Func<short[]> fillProc)
        {

            if (BufferByteSize < 500)
            { 
                throw new ArgumentOutOfRangeException("BufferByteSize","minimal size of buffer is 500 bytes");
            }


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
            m_buffDescription.BufferBytes = BufferByteSize*2;
            m_buffDescription.ControlFrequency = true;
            m_buffDescription.ControlEffects = true;
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

            BufferPositionNotify[] nots = new BufferPositionNotify[2];

            BufferPositionNotify not = new BufferPositionNotify();
            not.Offset = 500;
            not.EventNotifyHandle = m_synchronizer.SafeWaitHandle.DangerousGetHandle();
            nots[0] = not;

            not = new BufferPositionNotify();
            not.Offset = BufferByteSize + 500;
            not.EventNotifyHandle = m_synchronizer.SafeWaitHandle.DangerousGetHandle();
            nots[1] = not;
            m_notify = new Notify(m_soundBuffer);
            m_notify.SetNotificationPositions(nots);


            m_waitThread = new Thread(new ThreadStart(DataRequestThread));
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

        int m_bfpos = 0;
        object datalock = new object();
        private void WriteNextData(short[] data)
        {
            //lock (datalock)
            //{
                try
                {
                    m_soundBuffer.Write(m_bfpos, data, LockFlag.None);
                    m_samplesPlayed += data.Length;
                    m_bfpos += 2 * data.Length;
                    m_bfpos %= m_buffDescription.BufferBytes;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("DXWriteExc: dl:" + data.Length + " " + m_bfpos);
                }
            //}
        }


        private void ClearBuffer()
        { 
            byte[] one  = new byte[]{0};
            for (int i = 0; i < m_buffDescription.BufferBytes; i++)
            {
                m_soundBuffer.Write(i,one,LockFlag.None);
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
            if (m_requestproc != null)
            {
                short[] data = m_requestproc.Invoke();
                if (data != null && data.Length > 0)
                    WriteNextData(data);
            }
            m_bfpos = 0;
            m_samplesPlayed = -m_buffDescription.BufferBytes/4;

            m_soundBuffer.Frequency = (int)(spedmodification * m_buffDescription.Format.SamplesPerSecond);
            m_soundBuffer.Play(m_bfpos, BufferPlayFlags.Looping);
        }


        public void Play()
        {
            Play(1.0);
        }

        public void Pause()
        {
            m_soundBuffer.Stop();
            m_soundBuffer.SetCurrentPosition(0);
        }

        private void RetrieveData()
        {
            short[] data = m_requestproc.Invoke();
            if (data == null || data.Length == 0)
            {
                Pause();
            }
            else
            {
                WriteNextData(data);
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

