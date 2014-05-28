using NanoTrans.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NanoTrans
{
    /// <summary>
    /// audio data buffer stucture
    /// </summary>
    public class MyBuffer16
    {
        private long _StartMS;

        public long StartMS
        {
            get
            {
                return _StartMS;
            }
        }

        private long _EndMS;

        public long EndMS
        {
            get { return _EndMS; }
        }

        public long LengthMS
        {
            get { return EndMS - StartMS; }
        }

        public bool Loaded { get; set; }
        public short[] Data;

        private object datalock = new object();


        public MyBuffer16(long aDelkaBufferuMS)
        {
            this.Loaded = false;
            this._StartMS = 0;
            this._EndMS = 0;
            lock(datalock)
                Data = new short[(int)aDelkaBufferuMS * (1600 / 1000)];
        }


        public int CopyDataToBuffer(short[] data, long beginMS, long endMS)
        {
            try
            {
                
                //this.SmazBuffer();
                this._StartMS = beginMS;
                this._EndMS = endMS;

                if (data == null)
                    data = new short[0];

                lock (datalock)
                {
                    this.Loaded = false;
                    Data = data;
                }
                this.Loaded = true;
                return 0;
            }
            catch
            {
                return -1;
            }

        }

        public int AddDataToBuffer(short[] data, long lengthMS)
        {
            try
            {
                if (data != null && data.Length > 0)
                {
                    lock (datalock)
                    {
                        this.Loaded = false;

                        this._EndMS += lengthMS;

                        short[] dbf = new short[this.Data.Length + data.Length];
                        Array.Copy(Data, dbf, Data.Length);
                        Array.Copy(data, 0, dbf, Data.Length, Data.Length);
                        this.Data = dbf;


                        this.Loaded = true;
                    }
                }
                return 0;
            }
            catch
            {
                return -1;
            }

        }

        public short[] CopyFromBuffer(TimeSpan from, TimeSpan to, TimeSpan max)
        {
            try
            {
                long beginMS = (long)from.TotalMilliseconds;
                long lengthMS = (long)to.TotalMilliseconds;
                long limitMS = (long)max.TotalMilliseconds;

                if (max < TimeSpan.Zero)
                    limitMS = -1;

                if (beginMS > this.EndMS) beginMS -= lengthMS;
                if (beginMS < this.StartMS) beginMS = this.StartMS;


                long sampleCount = lengthMS * (16000 / 1000);
                int fromSample = (int)(beginMS - this.StartMS) * (16000/ 1000);
                short[] pole = new short[sampleCount];

                long endIndex = -1;
                if (limitMS > 0 && beginMS + lengthMS > limitMS)
                {
                    long newLength = limitMS - beginMS;
                    if (newLength > 0)
                    {
                        endIndex = fromSample + newLength * (16000 / 1000);
                    }
                    else
                    {
                        endIndex = 0;
                    }
                }

                lock (this.Data)
                {
                    for (int i = fromSample; i < (fromSample + sampleCount) && i < this.Data.Length; i++)
                    {
                        pole[(i - fromSample)] = this.Data[i];
                        if (endIndex > -1 && i > endIndex)
                        {
                            break;
                        }
                    }
                }
                return pole;
            }
            catch
            {
                return new short[1];
            }
        }
    }

}
