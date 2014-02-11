using System;
using System.Collections.Generic;
using System.Text;

namespace NanoTrans
{
    /// <summary>
    /// trida ktera je bufferem pro audio data
    /// </summary>
    public class MyBuffer16
    {
        private long _PocatekMS;
        /// <summary>
        /// pocatecni cas ulozeny v bufferu - MS
        /// </summary>
        public long PocatekMS
        {
            get
            {
                return _PocatekMS;
            }
        }

        private long _KonecMS;
        /// <summary>
        /// koncovy cas ulozeny v bufferu - MS
        /// </summary>
        public long KonecMS
        {
            get { return _KonecMS; }
        }

        /// <summary>
        /// skutena delka ulozenych dat v MS
        /// </summary>
        public long DelkaMS
        {
            get { return KonecMS - PocatekMS; }
        }

        /// <summary>
        /// info zda je buffer naplnen daty
        /// </summary>
        public bool Nacteny { get; set; }
        //public short[] data;
        public List<short> data;


        public MyBuffer16(long aDelkaBufferuMS)
        {
            this.Nacteny = false;
            this._PocatekMS = 0;
            this._KonecMS = 0;


            //data = new short[aDelkaBufferuMS * (FrekvenceVzorkovani / 1000)];
            data = new List<short>((int)aDelkaBufferuMS * (1600 / 1000));
            //data.AddRange(new short[(int)aDelkaBufferuMS * (FrekvenceVzorkovani / 1000)]);
        }

        /// <summary>
        /// kompletne smaze data v bufferu, ale zachova jeho nastaveni jako je delka
        /// </summary>
        /// <returns></returns>
        public bool SmazBuffer()
        {
            //data = new short[data.Length];
            this.Nacteny = false;
            lock (this.data)
            {
                data.Clear();
            }

            this._PocatekMS = 0;
            this._KonecMS = 0;

            return true;
        }

        /// <summary>
        /// Ulozi data do bufferu - data a jejich pocatek a konec
        /// </summary>
        /// <param name="aData"></param>
        /// <param name="aPocatekMS"></param>
        /// <param name="aKonec"></param>
        /// <returns></returns>
        public int UlozDataDoBufferu(short[] aData, long aPocatekMS, long aKonecMS)
        {
            try
            {
                //this.SmazBuffer();
                this._PocatekMS = aPocatekMS;
                this._KonecMS = aKonecMS;

                lock (this.data)
                {
                    this.Nacteny = false;
                    List<short> pData2 = new List<short>(aData);
                    List<short> pomocny = this.data;

                    data = pData2;

                    pomocny.Clear();
                }
                this.Nacteny = true;
                return 0;
            }
            catch
            {
                return -1;
            }

        }

        /// <summary>
        /// Vlozi data do bufferu nakonec predchazejicich dat
        /// </summary>
        /// <param name="aData"></param>
        /// <param name="aDelkaMS"></param>
        /// <returns></returns>
        public int UlozDataDoBufferuNaKonec(short[] aData, long aDelkaMS)
        {
            try
            {
                lock (this.data)
                {
                    this.Nacteny = false;

                    this._KonecMS += aDelkaMS;


                    this.data.AddRange(aData);


                    this.Nacteny = true;
                }
                return 0;
            }
            catch
            {
                return -1;
            }

        }


        /// <summary>
        /// Vrati data v poli bytu, aOmezeniMS = -1 ignorovano, jinak vrati zvuk pouze do omezeni
        /// </summary>
        /// <returns></returns>
        public short[] VratDataBufferuShort(TimeSpan from, TimeSpan to, TimeSpan max)
        {
            try
            {
                long aPocatekMS = (long)from.TotalMilliseconds;
                long aKolikMS = (long)to.TotalMilliseconds;
                long aOmezeniMS = (long)max.TotalMilliseconds;

                if (max < TimeSpan.Zero)
                    aOmezeniMS = -1;

                if (aPocatekMS > this.KonecMS) aPocatekMS -= aKolikMS;
                if (aPocatekMS < this.PocatekMS) aPocatekMS = this.PocatekMS;


                long pKolikVzorku = aKolikMS * (1600 / 1000);
                int pOdVzorku = (int)(aPocatekMS - this.PocatekMS) * (1600/ 1000);
                short[] pole = new short[pKolikVzorku];

                long pDoVzorku = -1;
                if (aOmezeniMS > 0 && aPocatekMS + aKolikMS > aOmezeniMS)
                {
                    long pNewKolikMS = aOmezeniMS - aPocatekMS;
                    if (pNewKolikMS > 0)
                    {
                        pDoVzorku = pOdVzorku + pNewKolikMS * (1600 / 1000);
                    }
                    else
                    {
                        pDoVzorku = 0;
                    }
                }

                lock (this.data)
                {
                    for (int i = pOdVzorku; i < (pOdVzorku + pKolikVzorku) && i < this.data.Count; i++)
                    {
                        pole[(i - pOdVzorku)] = this.data[i];
                        if (pDoVzorku > -1 && i > pDoVzorku)
                        {
                            break; //zastaveni pro omezene prehravani
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



    /// <summary>
    /// buffer pro spravu vlny
    /// </summary>
    public class MyBufferVlny
    {
        public int PocetPixeluZaS { get; set; }
        private long _PocatekMS;
        /// <summary>
        /// pocatecni cas ulozeny v bufferu - MS
        /// </summary>
        public long PocatekMS
        {
            get { return _PocatekMS; }
        }

        private long _KonecMS;
        /// <summary>
        /// koncovy cas ulozeny v bufferu - MS
        /// </summary>
        public long KonecMS
        {
            get { return _KonecMS; }
        }

        /// <summary>
        /// skutena delka ulozenych dat v MS
        /// </summary>
        public long DelkaMS
        {
            get { return KonecMS - PocatekMS; }
        }

        /// <summary>
        /// info zda je buffer naplnen daty
        /// </summary>
        public bool Nacteny { get; set; }
        public float[] dataF;

        /// <summary>
        /// kompletne smaze data v bufferu, ale zachova jeho nastaveni jako je delka
        /// </summary>
        /// <returns></returns>
        public bool SmazBuffer()
        {
            dataF = new float[dataF.Length];
            this._PocatekMS = -1;
            this._KonecMS = -1;

            this.Nacteny = false;
            return true;
        }
    }
}
