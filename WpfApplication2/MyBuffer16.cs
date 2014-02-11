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
        private int _FrekvenceVzorkovani;
        public int FrekvenceVzorkovani
        {
            get { return _FrekvenceVzorkovani; }
        }
        private int _PocetKanalu;
        public int PocetKanalu
        {
            get { return _PocetKanalu; }
        }


        private long _DelkaBufferuMS;
        /// <summary>
        /// zakladni delka bufferu jak byla definovana
        /// </summary>
        public long DelkaBufferuMS { get { return _DelkaBufferuMS; } }

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
            this._DelkaBufferuMS = aDelkaBufferuMS;
            this.Nacteny = false;
            this._PocatekMS = 0;
            this._KonecMS = 0;

            this._PocetKanalu = 1;
            this._FrekvenceVzorkovani = 16000;

            //data = new short[aDelkaBufferuMS * (FrekvenceVzorkovani / 1000)];
            data = new List<short>((int)aDelkaBufferuMS * (FrekvenceVzorkovani / 1000));
            //data.AddRange(new short[(int)aDelkaBufferuMS * (FrekvenceVzorkovani / 1000)]);
        }

        /// <summary>
        /// ulozi data bufferu do wav souboru
        /// </summary>
        /// <param name="aCestaWAV"></param>
        /// <returns></returns>
        public bool UlozBufferDoWavSouboru(string aCestaWAV)
        {
            return MyWav.VytvorWavSoubor(this, aCestaWAV);
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
                /*
                if (data.Length == aData.Length)
                {
                    this.data = (aData);
                }
                else if (aData.Length < data.Length)
                {

                    for (int i = 0; i < aData.Length; i++)
                    {
                        data[i] = aData[i];
                    }
                }
                else //data zkracena
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = aData[i];
                    }
                }
                */

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
        /// smaze z bufferu pocatecni data podle delky v MS
        /// </summary>
        /// <param name="aDelkaMS"></param>
        /// <returns></returns>
        public int SmazDataZBufferuZeZacatku(long aDelkaMS)
        {
            try
            {
                lock (this.data)
                {
                    this.Nacteny = false;
                    if (aDelkaMS > this.DelkaMS)
                    {
                        aDelkaMS = this.DelkaMS;
                    }
                    this._PocatekMS += aDelkaMS;

                    int pKolik = (int)aDelkaMS * (this.FrekvenceVzorkovani / 1000);

                    this.data.RemoveRange(0, pKolik);


                    if (this.DelkaMS > 0) this.Nacteny = true;
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
        public byte[] VratDataBufferuByte(long aPocatekMS, long aKolikMS, long aOmezeniMS)
        {
            try
            {
                if (aPocatekMS > this.KonecMS) aPocatekMS -= aKolikMS;
                if (aPocatekMS < this.PocatekMS) aPocatekMS = this.PocatekMS;




                long pKolikVzorku = aKolikMS * (this.FrekvenceVzorkovani / 1000);
                int pOdVzorku = (int)(aPocatekMS - this.PocatekMS) * (this.FrekvenceVzorkovani / 1000);
                byte[] pole = new byte[pKolikVzorku * 2];
                byte[] pole2 = new byte[2];

                long pDoVzorku = -1;
                if (aOmezeniMS > 0 && aPocatekMS + aKolikMS > aOmezeniMS)
                {
                    long pNewKolikMS = aOmezeniMS - aPocatekMS;
                    if (pNewKolikMS > 0)
                    {
                        pDoVzorku = pOdVzorku + pNewKolikMS * (this.FrekvenceVzorkovani / 1000);
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
                        pole2 = BitConverter.GetBytes(this.data[i]);
                        pole[(i - pOdVzorku) * 2] = pole2[0];
                        pole[(i - pOdVzorku) * 2 + 1] = pole2[1];
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
                return new byte[1];
            }
        }





        /// <summary>
        /// Vrati data v poli bytu, aOmezeniMS = -1 ignorovano, jinak vrati zvuk pouze do omezeni
        /// </summary>
        /// <returns></returns>
        public short[] VratDataBufferuShort(long aPocatekMS, long aKolikMS, long aOmezeniMS)
        {
            try
            {
                if (aPocatekMS > this.KonecMS) aPocatekMS -= aKolikMS;
                if (aPocatekMS < this.PocatekMS) aPocatekMS = this.PocatekMS;


                long pKolikVzorku = aKolikMS * (this.FrekvenceVzorkovani / 1000);
                int pOdVzorku = (int)(aPocatekMS - this.PocatekMS) * (this.FrekvenceVzorkovani / 1000);
                short[] pole = new short[pKolikVzorku];

                long pDoVzorku = -1;
                if (aOmezeniMS > 0 && aPocatekMS + aKolikMS > aOmezeniMS)
                {
                    long pNewKolikMS = aOmezeniMS - aPocatekMS;
                    if (pNewKolikMS > 0)
                    {
                        pDoVzorku = pOdVzorku + pNewKolikMS * (this.FrekvenceVzorkovani / 1000);
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

        private int _FrekvenceVzorkovani;
        public int FrekvenceVzorkovani
        {
            get { return _FrekvenceVzorkovani; }
        }
        private int _PocetKanalu;
        public int PocetKanalu
        {
            get { return _PocetKanalu; }
        }

        private long _DelkaBufferuMS;
        /// <summary>
        /// zakladni delka bufferu jak byla definovana
        /// </summary>
        public long DelkaBufferuMS { get { return _DelkaBufferuMS; } }

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


        public MyBufferVlny(long aDelkaBufferuMS, int aPocetPixeluZaSekundu)
        {
            this._DelkaBufferuMS = aDelkaBufferuMS;
            this.Nacteny = false;
            this._PocatekMS = -1;
            this._KonecMS = -1;
            this.PocetPixeluZaS = aPocetPixeluZaSekundu;

            this._PocetKanalu = 1;
            this._FrekvenceVzorkovani = 16000;

            dataF = new float[(int)(double)aDelkaBufferuMS / 1000 * aPocetPixeluZaSekundu * 2];
        }

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

        /// <summary>
        /// Ulozi data do bufferu - data a jejich pocatek a konec
        /// </summary>
        /// <param name="aData"></param>
        /// <param name="aPocatekMS"></param>
        /// <param name="aKonec"></param>
        /// <returns></returns>
        public int UlozDataDoBufferu(float[] aDataF, long aPocatekMS, long aKonecMS)
        {
            try
            {
                this.SmazBuffer();
                this._PocatekMS = aPocatekMS;
                this._KonecMS = aKonecMS;

                this.dataF = aDataF;
                this.Nacteny = true;
                return 0;
            }
            catch
            {
                return -1;
            }

        }

        /// <summary>
        /// Vrati pole floatu pro vykresleni vlny
        /// </summary>
        /// <param name="aPocatekMS"></param>
        /// <param name="aKonecMS"></param>
        /// <param name="aVychoziDelkaZobrazeneVlny"></param>
        /// <param name="aPocetPixeluVystupu"></param>
        /// <returns></returns>
        public float[] VratDataBuffer(ref long aPocatekMS, ref long aKonecMS, long aVychoziDelkaZobrazeneVlnyMS, int aPocetPixeluVystupu)
        {
            float[] ret = null;
            try
            {
                if (aPocetPixeluVystupu > 0) ret = new float[aPocetPixeluVystupu * 2];
                if (aPocatekMS > aKonecMS) return ret;  //vrati nuly 0 pokud je pocatek dale nez konec

                if (aPocatekMS < 0) aPocatekMS = 0;
                long pPozadovanaDelkaMS = aKonecMS - aPocatekMS;
                if (pPozadovanaDelkaMS < this.DelkaMS && aVychoziDelkaZobrazeneVlnyMS > pPozadovanaDelkaMS)
                {
                    aPocatekMS = (aPocatekMS + pPozadovanaDelkaMS / 2) - aVychoziDelkaZobrazeneVlnyMS / 2;
                    if (aPocatekMS < 0)
                    {
                        aPocatekMS = 0;
                        aKonecMS = aPocatekMS + aVychoziDelkaZobrazeneVlnyMS;
                    }
                    pPozadovanaDelkaMS = aKonecMS - aPocatekMS;
                }
                //spocteni indexu odkud kam prochazet data
                int pPocatecniIndexDat = (int)((double)aPocatekMS / 1000 * this.PocetPixeluZaS);
                int pKoncovyIndexDat = (int)(pPocatecniIndexDat + (double)pPozadovanaDelkaMS / 1000 * this.PocetPixeluZaS);
                if (pPocatecniIndexDat < 0) pPocatecniIndexDat = 0;
                if (pKoncovyIndexDat >= this.dataF.Length) pKoncovyIndexDat = this.dataF.Length - 1;

                int pDelkaDat = pKoncovyIndexDat - pPocatecniIndexDat;

                float pMezivypocetK = 0;
                float pMezivypocetZ = 0;
                int pocetK = 0;
                int pocetZ = 0;

                int pXsouradnice = 0;

                int j = 0;
                for (int i = pPocatecniIndexDat; i <= pKoncovyIndexDat; i++)
                {

                    if (dataF[i] > 0)
                    {
                        pMezivypocetK += dataF[i];
                        pocetK++;
                    }
                    else
                    {
                        pMezivypocetZ += dataF[i];
                        pocetZ++;
                    }
                    if (j >= ((float)pDelkaDat / aPocetPixeluVystupu))
                    {
                        if (pocetK > 0)
                        {
                            pMezivypocetK = pMezivypocetK / pocetK / 32767;
                        }
                        else
                        {
                            //pMezivypocetK = pPoleVykresleni[Xsouradnice - 2];
                            pMezivypocetK = 0;
                        }
                        ret[pXsouradnice] = pMezivypocetK;
                        if (pocetZ > 0)
                        {
                            pMezivypocetZ = pMezivypocetZ / pocetZ / 32767;

                        }
                        else
                        {
                            //pMezivypocetZ = pPoleVykresleni[Xsouradnice - 1];
                            pMezivypocetZ = 0;
                        }
                        ret[pXsouradnice + 1] = pMezivypocetZ;

                        pMezivypocetK = 0;
                        pMezivypocetZ = 0;
                        pocetK = 0;
                        pocetZ = 0;

                        pXsouradnice += 2;
                        j = 0;
                    }

                    j++;


                }

                return ret;
            }
            catch (Exception ex)
            {
                Window1.logAplikace.LogujChybu(ex);
                return ret;
            }

        }

    }
}
