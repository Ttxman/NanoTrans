using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

using System.Diagnostics;
using System.Threading;
using System.Globalization;

namespace NanoTrans.Audio
{

    public class AudioBufferEventArgs : EventArgs            //upravene eventargs pro predani dat dale
    {
        public AudioBufferEventArgs(short[] data, int size, long startMS, long endMS, int bufferID)
        {
            this.data = data;
            this.size = size;
            this.StartMS = startMS;
            this.EndMS = endMS;
            this.BufferID = bufferID;
        }
        public short[] data;
        public int size;
        public long StartMS; //pocatecni cas ramce v milisekundach
        public long EndMS;   //koncovy cas ramce v milisekundach
        public int BufferID;

    }

    public class AudioBufferEventArgs2 : EventArgs            //upravene eventargs pro predani cisla souboru dale
    {
        public AudioBufferEventArgs2(int fileNumber, long lengthMS)
        {
            this.FileNumber = fileNumber;
            this.LengthMS = lengthMS;
        }

        public int FileNumber;
        public long LengthMS;

    }


    public class WavReader : IDisposable
    {

        internal static bool SaveToWav(string filename, short[] Data)
        {
            try
            {
                BinaryWriter output = new BinaryWriter(new FileStream(filename, FileMode.Create));

                byte[] header = WavReader.GetWaveHeader(Data.Length * 2);
                output.Write(header);

                for (int i = 0; i < Data.Length; i++)
                {
                    output.Write(Data[i]);
                }
                output.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public event EventHandler HaveData;    //metoda have data je udalost,kterou objekt podporuje-datovy ramec zvuku
        public event EventHandler HaveFileNumber; //metoda podporujici objekt
        public event EventHandler TemporaryWavesDone;

        private bool _Loaded;
        /// <summary>
        /// property s informaci zda je nacten souboru
        /// </summary>
        public bool Loaded { get { return _Loaded; } }
        private bool _Converted;
        /// <summary>
        /// info o dokoncenem prevodu souboru a moznosti jeho nacitani do bufferu
        /// </summary>
        public bool Converted { get { return this._Converted; } }
        private long _FileLengthMS;

        /// <summary>
        /// delka prevedeneho souboru v MS
        /// </summary>
        public long FileLengthMS { get { return _FileLengthMS; } }
        public TimeSpan FileLength {get { return TimeSpan.FromMilliseconds(_FileLengthMS); } }

        public UInt32 Frequency { get; set; }
        public UInt16 SampleSize { get; set; }
        public long SampleCount { get; set; }
        public UInt16 ChannelCount { get; set; }


        private Int16[] _LoadedData;                  //po nacteni do zalozniho bufferu,jsou data poslana ven z tridy pomoci udalosti

        private string _FilePath;
        /// <summary>
        /// cesta multimedialniho souboru, ze ktereho jsou prevedeny jednotlive wavy
        /// </summary>
        public string FilePath { get { return _FilePath; } }
        /// <summary>
        /// adresar prekonvertovanych wav souboru
        /// </summary>
        private string TemporaryWAVsPath { get; set; }
        //public long VelikostZobrazovacihoBufferu { get; set; }   //jak velika cast wav souboru se nacita do pameti


        private long bPozadovanyPocatekRamce;    //promenna kvuli predani informace o nacitanem ramci do thredu
        private long bPozadovanaDelkaRamceMS;
        private int bIDBufferu;

        private List<string> docasneZvukoveSoubory = new List<string>();
        private long delkaDocasnehoWav = 0;
        private long delkaDocasnehoWavMS = 0;

        /// <summary>
        /// thread pro prevod docasnych souboru
        /// </summary>
        private Thread tPrevodNaDocasneSoubory = null;

        /// <summary>
        /// process pro spusteni ffmeg na prevod wav souboru
        /// </summary>
        private Process prPrevod = null;


        /// <summary>
        /// konstruktor
        /// </summary>
        public WavReader()
        {
            Frequency = 16000;
            SampleSize = 2;
            ChannelCount = 1;
            _Loaded = false;
            _Converted = false;
            _FilePath = null;
            SampleCount = 0;
            ///_dataProVykresleniNahleduVlny = null;
        }



        private void SetNewFrameStartTime(long aCasMS, long aDelkaMS, int aIDBufferu)  //zavolat pred spustenim nacitani ramce, pote spustit thread
        {
            if (aCasMS < 0)
            {
                aCasMS = 0; //osetreni zaporneho casu
            }
            this.bPozadovanyPocatekRamce = aCasMS;
            this.bPozadovanaDelkaRamceMS = aDelkaMS;
            this.bIDBufferu = aIDBufferu;
        }




        /// <summary>
        /// Converts audio file to temporary waves used by nanotrans
        /// </summary>
        public void ConvertAudioFileToWave(string filePath)
        {
            this._FilePath = filePath;
            this._Loaded = false;
            this._Converted = false;
            _conversionStopper = 0;
            this.tPrevodNaDocasneSoubory = new Thread(() => this.ZacniPrevodSouboruNaDocasneWav(_FilePath, FilePaths.TempDirectory, Const.TEMPORARY_AUDIO_FILE_LENGTH_MS)) { Name = "conversion to wav" };
            this.tPrevodNaDocasneSoubory.Start();

        }


        /// <summary>
        /// returns file length
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static TimeSpan ReturnAudioLength(string path)
        {
            Process prInfo = new Process();
            prInfo.StartInfo.FileName = FilePaths.FFmpegPath;
            prInfo.StartInfo.WorkingDirectory = new FileInfo(path).DirectoryName;
            prInfo.StartInfo.Arguments = "-i \"" + path + "\"";

            prInfo.StartInfo.RedirectStandardInput = true;
            prInfo.StartInfo.RedirectStandardOutput = true;
            prInfo.StartInfo.RedirectStandardError = true;
            prInfo.StartInfo.UseShellExecute = false;
            prInfo.StartInfo.CreateNoWindow = true;
            prInfo.Start();

            string pZprava = prInfo.StandardError.ReadToEnd();
            prInfo.Close();
            string durheader = "Duration: ";
            int pIndex = pZprava.IndexOf(durheader);

            var index2 = pZprava.IndexOf(',', pIndex);
            string timestamp = pZprava.Substring(pIndex + durheader.Length, index2 - durheader.Length - pIndex);

            var ts = TimeSpan.Parse(timestamp, CultureInfo.InvariantCulture);
            return ts;

        }

        public static byte[] GetWaveHeader(int dataSize)
        {
            byte[] header = new byte[44];
            byte[] buff;

            int pPozice = 0;

            //RIFF
            buff = ASCIIEncoding.ASCII.GetBytes("RIFF");
            buff.CopyTo(header, pPozice);
            pPozice += buff.Length;


            byte[] pole1;
            //Velikost
            buff = BitConverter.GetBytes(dataSize + 44 - 4);
            buff.CopyTo(header, pPozice);
            pPozice += buff.Length;

            //WAVEfmt            
            //pomocna += "WAVEfmt ";
            buff = ASCIIEncoding.ASCII.GetBytes("WAVEfmt ");
            buff.CopyTo(header, pPozice);
            pPozice += buff.Length;

            //do konce casti format
            Int32 fmtPocet = 16;
            pole1 = BitConverter.GetBytes(fmtPocet);
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;

            //mono/stereo
            short formatakanaly = 1;
            pole1 = BitConverter.GetBytes(formatakanaly);
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;

            //frekvence
            Int32 frekvence = 16000;
            pole1 = BitConverter.GetBytes(frekvence);
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;

            //prumerny pocet bitu za s
            Int32 prumbytu = 32000;
            pole1 = BitConverter.GetBytes(prumbytu);
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;

            //velikosti vzorku
            short velikostVzorkuB = 2;
            short velikostVzorkub = 16;
            pole1 = BitConverter.GetBytes(velikostVzorkuB);
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;
            pole1 = BitConverter.GetBytes(velikostVzorkub);
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;

            //data
            buff = ASCIIEncoding.ASCII.GetBytes("data");
            buff.CopyTo(header, pPozice);
            pPozice += buff.Length;

            //velikost dat
            pole1 = BitConverter.GetBytes(dataSize);
            pole1.CopyTo(header, pPozice);
            pPozice += pole1.Length;

            return header;
        }



        public int _conversionStopper = 0;
        //spusti prevod souboru na wav,ktery je mozno vykreslovat - skonci po naplneni zobrazovaciho bufferu, pot
        public bool ZacniPrevodSouboruNaDocasneWav(string aCesta, string aCestaDocasnychWAV, long aDelkaJednohoSouboruMS)
        {
            BinaryWriter output = null;
            try
            {
                //nulovani promennych
                this.docasneZvukoveSoubory.Clear();
                this._FileLengthMS = (long)WavReader.ReturnAudioLength(aCesta).TotalMilliseconds;


                if (aCesta == null || aCestaDocasnychWAV == null || FileLengthMS <= 0)
                    return false; //chybne nastaveni nebo delka souboru k prevodu

                this.TemporaryWAVsPath = aCestaDocasnychWAV;
                this.docasneZvukoveSoubory = new List<string>();

                delkaDocasnehoWav = (aDelkaJednohoSouboruMS / 1000) * this.Frequency;  //pocet vzorku v 1 docasnem souboru
                this.delkaDocasnehoWavMS = aDelkaJednohoSouboruMS;
                int pIndexDocasneho = -1;

                //vytvoren proces zapocato rucni vytvareni prevadeni docasneho souboru a plneni bufferu programu
                //po naplneni zobrazovaciho bufferu lze vykreslit vlnu v programu-

                prPrevod = new Process();
                prPrevod.StartInfo.FileName = FilePaths.FFmpegPath;
                prPrevod.StartInfo.WorkingDirectory = new FileInfo(aCesta).DirectoryName;
                prPrevod.StartInfo.Arguments = "-i \"" + aCesta + "\" -y -vn -ar 16000 -ac 1 -acodec pcm_s16le -f s16le -"; //nova data bez hlavicky
                prPrevod.StartInfo.RedirectStandardInput = true;
                prPrevod.StartInfo.RedirectStandardOutput = true;
                prPrevod.StartInfo.RedirectStandardError = false;
                prPrevod.StartInfo.UseShellExecute = false;
                prPrevod.StartInfo.CreateNoWindow = true;


                prPrevod.Start();
                //nacteni hlavicky - a alokovani pomocnych bufferu pro tuto operaci cteni

                byte[] buffer2 = new byte[4096];
                short[] buffer2s = new short[buffer2.Length / 2];

                ChannelCount = 1;

                this.Frequency = 16000;

                this.SampleSize = 2;

                long pPocetVzorku2Delta = Const.DELKA_PRVNIHO_RAMCE_ZOBRAZOVACIHO_BUFFERU_MS * (this.Frequency / 1000);
                SampleCount = pPocetVzorku2Delta;
                long pPocetVzorku2 = Const.DISPLAY_BUFFER_LENGTH_MS * (this.Frequency / 1000);

                //nacitani jednotlivych dat
                this._LoadedData = new Int16[pPocetVzorku2];

                int i = 0;


                short dato = 0;  //pomocna


                //prvni docasny wav soubor tmp
                pIndexDocasneho = 0;
                docasneZvukoveSoubory.Add(TemporaryWAVsPath + pIndexDocasneho.ToString() + ".wav");
                output = new BinaryWriter(new FileStream(docasneZvukoveSoubory[pIndexDocasneho], FileMode.Create));
                Stream outputbase = output.BaseStream;
                //prazdna hlavicka
                byte[] pPrazdnaHlavicka = new byte[44];
                output.Write(pPrazdnaHlavicka);
                //

                long pPocetNactenychVzorkuCelkem = 0;

                int pPocetNactenych = 1;    //pocatecni inicializace nacitani,aby neskoncil cyklus
                long j = 0; //pomocna pro indexaci v souborech

                Stream outstream = prPrevod.StandardOutput.BaseStream;
                for (i = 0; pPocetNactenych > 0; )
                {
                    //Termination
                    if (Interlocked.CompareExchange(ref _conversionStopper, 1, 1) == 1)
                    {
                        return false;
                    }


                    pPocetNactenych = outstream.Read(buffer2, 0, buffer2.Length);
                    Buffer.BlockCopy(buffer2, 0, buffer2s, 0, pPocetNactenych);
                    int writeoffset = 0;
                    int writecnt = 0;
                    for (int k = 0; k < pPocetNactenych / 2; k++, i++)
                    {
                        //dato = BitConverter.ToInt16(buffer2, k*2);
                        dato = buffer2s[k];
                        pPocetNactenychVzorkuCelkem++;

                        if (i == SampleCount) //nacten ramec prvni ramec
                        {
                            //poslani zpravy s daty bufferu
                            AudioBufferEventArgs e = new AudioBufferEventArgs(this._LoadedData, this._LoadedData.Length, 0, SampleCount / this.Frequency * 1000, Const.ID_BUFFER_WAVEFORMVISIBLE);
                            if (HaveData != null)
                                HaveData(this, e); // nacten ramec k zobrazeni a poslani tohoto ramce ven z tridy pomoci e
                            this._Loaded = true;
                            if (SampleCount + pPocetVzorku2Delta <= pPocetVzorku2 - 1)
                            {
                                SampleCount += pPocetVzorku2Delta;
                            }
                        }

                        if (i < SampleCount)
                        {
                            _LoadedData[i] = dato;
                        }


                        if (j >= delkaDocasnehoWav)
                        {
                            // zapsani bufferu jako bytu..
                            outputbase.Write(buffer2, writeoffset * 2, writecnt * 2);
                            writeoffset = writecnt;
                            writecnt = 0;


                            j = 0;
                            //zapsani skutecne hlavicky WAV
                            output.Seek(0, SeekOrigin.Begin);
                            output.Write((GetWaveHeader((Int32)output.BaseStream.Length - 44)));

                            output.Close();
                            output = null;
                            pIndexDocasneho++;
                            docasneZvukoveSoubory.Add(TemporaryWAVsPath + pIndexDocasneho.ToString() + ".wav");

                            AudioBufferEventArgs2 e2 = new AudioBufferEventArgs2(pIndexDocasneho, (pIndexDocasneho + 1) * this.delkaDocasnehoWavMS);
                            if (HaveFileNumber != null)
                                HaveFileNumber(this, e2);
                            output = new BinaryWriter(new FileStream(docasneZvukoveSoubory[pIndexDocasneho], FileMode.Create));
                            outputbase = output.BaseStream;
                            //pridani hlavicky-prazdne-zatim-pozdeji dodelat
                            output.Write(pPrazdnaHlavicka);
                            //
                        }

                        if (pPocetNactenych > 0)
                        {
                            writecnt++;
                            //output.Write(dato);     //zapsani do docasneho souboru
                        }
                        j++;
                    }

                    // zapsani bufferu jako bytu..
                    outputbase.Write(buffer2, writeoffset * 2, writecnt * 2);
                }

                prPrevod.Close();
                prPrevod = null;
                this._FileLengthMS = (long)((float)(pPocetNactenychVzorkuCelkem) / (this.Frequency / 1000));
                if (output != null)
                {
                    //zapsani skutecne hlavicky WAV
                    output.Seek(0, SeekOrigin.Begin);
                    output.Write((GetWaveHeader((Int32)output.BaseStream.Length - 44)));

                    output.Close();   //zavreni souboru pro zapis dat
                    output = null;
                    AudioBufferEventArgs2 e2 = new AudioBufferEventArgs2(pIndexDocasneho, this.FileLengthMS);
                    if (HaveFileNumber != null)
                        HaveFileNumber(this, e2);
                }

                this._Loaded = true;
                this._Converted = true;  //dokoncen prevod audio souboru
                this._FilePath = aCesta;

                if (i < SampleCount)  //soubor je kratsi nez zobrazovaci buffer
                {
                    short[] pPole = new short[i];
                    for (int k = 0; k < i - 1; k++)
                    {
                        pPole[k] = this._LoadedData[k];
                    }
                    AudioBufferEventArgs e = new AudioBufferEventArgs(pPole, pPole.Length, 0, (long)(((double)i * 1000) / this.Frequency), Const.ID_BUFFER_WAVEFORMVISIBLE);

                    //pokusne pridano
                    this.SampleCount = i;

                    if (HaveData != null)
                        HaveData(this, e); // nacten ramec k zobrazeni a poslani tohoto ramce ven z tridy pomoci e
                }
                this.SampleCount = i;

                if (TemporaryWavesDone != null)
                {
                    TemporaryWavesDone(this, new EventArgs());
                }
                return true;
            }
            catch (Exception ex)
            {
                if (output != null)
                {
                    output.Close();
                    output = null;
                }
                this._Converted = true; //i pres spadnuti je nacteni ok, doresit preteceni indexu!!!!!!
                if (TemporaryWavesDone != null && !(ex is ThreadAbortException))
                    TemporaryWavesDone(this, new EventArgs());

                return false;
            }
            finally
            { 
            
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public short[] NactiRamecBufferu(TimeSpan begin, TimeSpan end)
        {
            if (!this.Loaded || begin.TotalMilliseconds >= this.FileLengthMS)
                return null;

            TimeSpan bufferstart = begin;
            TimeSpan bufferLen = end - begin;
            long aPocatekMS = (long)begin.TotalMilliseconds;
            long aDelkaMS = (long)bufferLen.TotalMilliseconds;

            aDelkaMS = (aPocatekMS + aDelkaMS > this.FileLengthMS) ? this.FileLengthMS - aPocatekMS : aDelkaMS;

            short[] data = null;
            try
            {
                int j = 0;  //index pro vystupni data
                int delkadocasnehoMS = (int)(this.delkaDocasnehoWav / this.Frequency) * 1000;
                int pIndexDocasneho = (int)aPocatekMS / delkadocasnehoMS;

                if ((pIndexDocasneho >= 0) && (pIndexDocasneho < this.docasneZvukoveSoubory.Count - 1 || (pIndexDocasneho < this.docasneZvukoveSoubory.Count && this.Converted)))
                {
                    if (pIndexDocasneho >= docasneZvukoveSoubory.Count - 1 && !this.Converted)
                        //pokus o cteni souboru dalsich nez jsou prevedeny
                        return null;

                    FileInfo pfi = new FileInfo(docasneZvukoveSoubory[pIndexDocasneho]);
                    //long pPocetVzorkuDocasneho = this.delkaDocasnehoWav / 1000 * this.pFrekvence;   //pocet vzorku v docasnem souboru
                    long pPocetVzorkuDocasneho = (pfi.Length - 44) / 2;
                    long pPocetVzorkuNacist = aDelkaMS * (Frequency / 1000);
                    int pPocatecniIndexVSouboru = (int)((aPocatekMS - pIndexDocasneho * ((double)delkaDocasnehoWav / this.Frequency * 1000)) * (Frequency / 1000)) * this.SampleSize + 44;
                    int pNacistDat = (int)(pPocetVzorkuDocasneho - pPocatecniIndexVSouboru / this.SampleSize) + 22;
                    data = new short[pPocetVzorkuNacist];  //nove pole nacitanych obektu

                    long pPocetNactenych = 0;
                    while (true)
                    {
                        using (BinaryReader input = new BinaryReader(new FileStream(docasneZvukoveSoubory[pIndexDocasneho], FileMode.Open, FileAccess.Read, FileShare.Read)))
                        {

                            pIndexDocasneho++;

                            int pCount = (int)(pNacistDat) * this.SampleSize;

                            byte[] pBuffer = new byte[pCount];

                            input.BaseStream.Seek(pPocatecniIndexVSouboru, SeekOrigin.Begin);

                            pPocetNactenych += input.Read(pBuffer, 0, pCount);
                            input.Close();

                            //prevod do pole nacitanych dat

                            int cnt = (pBuffer.Length / 2 < data.Length - j) ? pBuffer.Length : (data.Length - j) * 2;
                            Buffer.BlockCopy(pBuffer, 0, data, j * 2, cnt);
                            j += pBuffer.Length / 2;
                            if (j >= pPocetVzorkuNacist)
                                break;
                            

                            if (pIndexDocasneho >= docasneZvukoveSoubory.Count - 1 && !this.Converted)
                                return null;

                            if (pIndexDocasneho < docasneZvukoveSoubory.Count)
                            {
                                pfi = new FileInfo(docasneZvukoveSoubory[pIndexDocasneho]);
                                pPocetVzorkuDocasneho = (pfi.Length - 44) / 2;
                                pPocatecniIndexVSouboru = 44;
                                pNacistDat = (int)(pPocetVzorkuDocasneho - pPocatecniIndexVSouboru);
                            }
                            else
                            {
                                break;

                            }
                        }
                    }
                }


            }
            catch
            {
                return null;
            }

            return data;
        }

        #region IDisposable Members

        /// <summary>
        /// zrusi vsechny procesy a thredy, ale trida je pripravena pro nove zpracovani souboru
        /// </summary>
        public void Stop()
        {
            try
            {

                //zruseni procesu prevodu souboru na wav
                if (prPrevod != null && !prPrevod.HasExited)
                {
                    prPrevod.Kill();
                }

                //zruseni threadu
                if (this.tPrevodNaDocasneSoubory != null && this.tPrevodNaDocasneSoubory.ThreadState == System.Threading.ThreadState.Running)
                {
                    Interlocked.Exchange(ref _conversionStopper, 1);
                    tPrevodNaDocasneSoubory.Join(1000);

                    if (tPrevodNaDocasneSoubory.ThreadState != System.Threading.ThreadState.Stopped)
                    {
                        tPrevodNaDocasneSoubory.Abort();
                        tPrevodNaDocasneSoubory.Join();
                    }
                    tPrevodNaDocasneSoubory = null;
                }

                //zruseni procesu prevodu souboru na wav
                if (prPrevod != null && !prPrevod.HasExited)
                {
                    prPrevod.WaitForExit();
                    prPrevod = null;
                }

                //smazani vsech docasnych souboru z temp adresare

                FileInfo fi = new FileInfo(FilePaths.TempDirectory);
                FileInfo[] pSoubory = fi.Directory.GetFiles("*.wav");
                foreach (FileInfo i in pSoubory)
                {
                    try
                    {
                        i.Delete();
                    }
                    catch //(Exception ex)
                    {
                    }
                }

                //uvodni nastaveni promennych
                this._Converted = false;
                this._Loaded = false;
                ///this._dataProVykresleniNahleduVlny = null;
                this.docasneZvukoveSoubory.Clear();
            }
            catch// (Exception ex)
            {
            }
        }

        public void Dispose()
        {
            Stop();
        }
        #endregion
    }
}
