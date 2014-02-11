using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

using System.Diagnostics;
using System.Threading;

namespace NanoTrans
{

    public class MyEventArgs : EventArgs            //upravene eventargs pro predani dat dale
    {
        public MyEventArgs(short[] data, int size, long pocatekMS, long konecMS, int aIDBufferu)
        {
            this.data = data;
            this.size = size;
            this.pocatecniCasMS = pocatekMS;
            this.koncovyCasMS = konecMS;
            this.IDBufferu = aIDBufferu;
        }
        public short[] data;
        public int size;
        public long pocatecniCasMS; //pocatecni cas ramce v milisekundach
        public long koncovyCasMS;   //koncovy cas ramce v milisekundach
        public int IDBufferu;

    }

    public class MyEventArgs2 : EventArgs            //upravene eventargs pro predani cisla souboru dale
    {
        public MyEventArgs2(int souborCislo, long msCelkovyCas)
        {
            this.souborCislo = souborCislo;
            this.msCelkovyCas = msCelkovyCas;
        }

        public int souborCislo;
        public long msCelkovyCas;

    }



    /// <summary>
    /// trida starajici se o prevod souboru do docasnych wav a nacitani bufferu s daty pro rozpoznavani a prevod nahledovych dat vlny 
    /// </summary>
    public class MyWav : IDisposable
    {
        public event EventHandler HaveData;    //metoda have data je udalost,kterou objekt podporuje-datovy ramec zvuku
        public event EventHandler HaveFileNumber; //metoda podporujici objekt
        public event EventHandler TemporaryWavesDone;

        private bool _Nacteno;
        /// <summary>
        /// property s informaci zda je nacten souboru
        /// </summary>
        public bool Nacteno { get { return _Nacteno; } }
        private bool _Prevedeno;
        /// <summary>
        /// info o dokoncenem prevodu souboru a moznosti jeho nacitani do bufferu
        /// </summary>
        public bool Prevedeno { get { return this._Prevedeno; } }
        private long _DelkaSouboruMS;

        /// <summary>
        /// delka prevedeneho souboru v MS
        /// </summary>
        public long DelkaSouboruMS { get { return _DelkaSouboruMS; } }
        public MyEventArgs NacitanyBufferSynchronne = null;


        private bool _NacitaniBufferu;
        /// <summary>
        /// informace zda dochazi k nacitani bufferu pro zobrazeni
        /// </summary>
        public bool NacitaniBufferu
        {
            get
            {
                if (_NacitaniBufferu)
                {
                    if (this.tNacitaniBufferu1 == null || this.tNacitaniBufferu1.ThreadState != System.Threading.ThreadState.Running)
                    {
                        return false;
                    }

                }
                return _NacitaniBufferu;
            }
        }
        public UInt32 pFrekvence { get; set; }
        public UInt16 pVelikostVzorku { get; set; }
        public long pPocetVzorku { get; set; }
        public UInt16 pPocetKanalu { get; set; }


        private Int16[] pDataNacitana;                  //po nacteni do zalozniho bufferu,jsou data poslana ven z tridy pomoci udalosti

        private string _CestaSouboru;
        /// <summary>
        /// cesta multimedialniho souboru, ze ktereho jsou prevedeny jednotlive wavy
        /// </summary>
        public string CestaSouboru { get { return _CestaSouboru; } }
        /// <summary>
        /// adresar prekonvertovanych wav souboru
        /// </summary>
        private string CestaDocasnychWAV { get; set; }
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
        private Thread tNacitaniBufferu1 = null;
        /// <summary>
        /// thred c2 pro nacteni bufferu
        /// </summary>
        private Thread tNacitaniBufferu2 = null;
        /// <summary>
        /// process pro spusteni ffmeg na prevod wav souboru
        /// </summary>
        private Process prPrevod = null;

        private string _AbsolutniCestaAdresareProgramu;

        /// <summary>
        /// konstruktor
        /// </summary>
        public MyWav(string aAbsolutniCestaAdresareProgramu)
        {
            this._AbsolutniCestaAdresareProgramu = aAbsolutniCestaAdresareProgramu;
            pFrekvence = 16000;
            pVelikostVzorku = 2;
            pPocetKanalu = 1;
            _Nacteno = false;
            _Prevedeno = false;
            _CestaSouboru = null;
            pPocetVzorku = 0;
            ///_dataProVykresleniNahleduVlny = null;
        }




        private void AsynchronniNacteniRamce()
        {
            this.NactiRamecBufferu(this.bPozadovanyPocatekRamce, this.bPozadovanaDelkaRamceMS, this.bIDBufferu);
        }


        private void NastavPocatecniCasNovehoRamce(long aCasMS, long aDelkaMS, int aIDBufferu)  //zavolat pred spustenim nacitani ramce, pote spustit thread
        {
            if (aCasMS < 0)
            {
                aCasMS = 0; //osetreni zaporneho casu
            }
            this.bPozadovanyPocatekRamce = aCasMS;
            this.bPozadovanaDelkaRamceMS = aDelkaMS;
            this.bIDBufferu = aIDBufferu;
            this._NacitaniBufferu = true;
        }
        /// <summary>
        /// vytvori thread, ktery nasledne zacne snacitanim do bufferu, aIDBufferu-aby bylo venku videt,pro ktery buffer jsou tato data
        /// </summary>
        public void AsynchronniNacteniRamce2(long aCasMS, long aDelkaMS, int aIDBufferu)
        {
            NastavPocatecniCasNovehoRamce(aCasMS, aDelkaMS, aIDBufferu);
            if (aIDBufferu == MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS)
            {
                this.tNacitaniBufferu2 = new Thread(AsynchronniNacteniRamce) { Name = "AsynchronniNacteniRamce2()" };
                this.tNacitaniBufferu2.Start();
            }
            if (aIDBufferu == MyKONST.ID_BUFFERU_PREPISOVANEHO_ELEMENTU)
            {
                this.tNacitaniBufferu2 = new Thread(AsynchronniNacteniRamce);
                this.tNacitaniBufferu2.Start();
            }
            else if (aIDBufferu == MyKONST.ID_ZOBRAZOVACIHO_BUFFERU_VLNY)
            {

                this.tNacitaniBufferu1 = new Thread(AsynchronniNacteniRamce);
                this.tNacitaniBufferu1.Start();
            }

        }



        private void AsynchronniPrevodMultimedialnihoSouboruNaDocasne()
        {
            this.ZacniPrevodSouboruNaDocasneWav(_CestaSouboru, MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU, MyKONST.DELKA_DOCASNEHO_SOUBORU_ZVUKU_MS);    //nastaveni bufferu
        }

        /// <summary>
        /// vytvori thread, ktery nasledne zacne s prevodem, dodelat moznost parametru????
        /// </summary>
        public void AsynchronniPrevodMultimedialnihoSouboruNaDocasne2(string aCestaSouboru)
        {
            this._CestaSouboru = aCestaSouboru;
            this._Nacteno = false;
            this._Prevedeno = false;
            this.tPrevodNaDocasneSoubory = new Thread(AsynchronniPrevodMultimedialnihoSouboruNaDocasne) { Name = "AsynchronniPrevodMultimedialnihoSouboruNaDocasne2" };
            this.tPrevodNaDocasneSoubory.Start(); //spusteni

        }


        /// <summary>
        /// vytvori wav soubor z bufferu audio dat a pokusi se ho ulozit do zadane cesty
        /// </summary>
        /// <param name="aAudioData"></param>
        /// <param name="aCesta"></param>
        /// <returns></returns>
        public static bool VytvorWavSoubor(MyBuffer16 aAudioData, string aCesta)
        {
            try
            {
                BinaryWriter output = new BinaryWriter(new FileStream(aCesta, FileMode.Create));

                byte[] pHlavicka = VytvorHlavickukWav(aAudioData.data.Length * 2);
                output.Write(pHlavicka);
                //
                for (int i = 0; i < aAudioData.data.Length; i++)
                {
                    output.Write(aAudioData.data[i]);
                }
                output.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        /// <summary>
        /// vytvori hlavicku k wavu
        /// </summary>
        /// <returns></returns>
        private static byte[] VytvorHlavickukWav(int aVelikostDat)
        {

            byte[] hlavicka = new byte[44];
            //RIFF
            char pomocna = 'R';
            byte[] pole1 = BitConverter.GetBytes(pomocna);
            int pPozice = 0;
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'I';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'F';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'F';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;

            //Velikost
            pole1 = BitConverter.GetBytes(aVelikostDat + 44 - 4);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;

            //WAVEfmt            
            //pomocna += "WAVEfmt";
            pomocna = 'W';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'A';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'V';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'E';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'f';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'm';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 't';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = ' ';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;

            //do konce casti format
            Int32 fmtPocet = 16;
            pole1 = BitConverter.GetBytes(fmtPocet);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;

            //mono/stereo
            short formatakanaly = 1;
            pole1 = BitConverter.GetBytes(formatakanaly);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;

            //frekvence
            Int32 frekvence = 16000;
            pole1 = BitConverter.GetBytes(frekvence);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;

            //prumerny pocet bitu za s
            Int32 prumbytu = 32000;
            pole1 = BitConverter.GetBytes(prumbytu);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;

            //velikosti vzorku
            short velikostVzorkuB = 2;
            short velikostVzorkub = 16;
            pole1 = BitConverter.GetBytes(velikostVzorkuB);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;
            pole1 = BitConverter.GetBytes(velikostVzorkub);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;

            //data
            pomocna = 'd';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'a';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 't';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;
            pomocna = 'a';
            pole1 = BitConverter.GetBytes(pomocna);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length - 1;

            //velikost dat
            pole1 = BitConverter.GetBytes(aVelikostDat);
            pole1.CopyTo(hlavicka, pPozice);
            pPozice += pole1.Length;

            return hlavicka;

        }


        /// <summary>
        /// odstrani nuly zleva stringu
        /// </summary>
        /// <param name="aCislo"></param>
        /// <returns></returns>
        private string OdstranNulyZleva(string aCislo)
        {
            try
            {
                string ret = "";
                int i = 0;
                while (i < aCislo.Length && aCislo[i] == '0')
                {
                    i++;
                }

                if (i >= aCislo.Length)
                    return "0";

                for (int j = i; j < aCislo.Length; j++)
                {
                    ret += aCislo[j];
                }
                ret = ret.Replace('.', ',');
                return ret;
            }
            catch
            {
                return "0";
            }

        }

        /// <summary>
        /// vrati informace o delce multimedialnim souboru, -1 soubor je nepodporovany
        /// </summary>
        /// <param name="aCesta"></param>
        /// <returns></returns>
        public long VratDelkuSouboruMS(string aCesta)
        {
            long ret = -1;
            try
            {
                long pDelka = -1;
                Process prInfo = new Process();
                prInfo.StartInfo.FileName = this._AbsolutniCestaAdresareProgramu + MyKONST.CESTA_FFMPEG;
                prInfo.StartInfo.WorkingDirectory = new FileInfo(aCesta).DirectoryName;

                //prPrevod.StartInfo.Arguments = "-i " + aCesta + " -y -vn -ar 16000 -ac 1 -acodec pcm_s16le -f wav -";
                //prInfo.StartInfo.Arguments = "-i \"" + aCesta + "\" -";
                prInfo.StartInfo.Arguments = "-i \"" + aCesta + "\"";
                //prInfo.StartInfo.Arguments = "-i E:/prepisovac/OTV10628.xml.avi"; 

                prInfo.StartInfo.RedirectStandardInput = true;
                prInfo.StartInfo.RedirectStandardOutput = true;
                prInfo.StartInfo.RedirectStandardError = true;
                prInfo.StartInfo.UseShellExecute = false;
                prInfo.StartInfo.CreateNoWindow = true;


                prInfo.Start();

                string pZprava = prInfo.StandardError.ReadToEnd();

                prInfo.Close();
                int pIndex = pZprava.IndexOf("Duration: ");
                if (pIndex > 0)
                {
                    string pDuration = "";
                    for (int i = pIndex + 10; i < pZprava.Length; i++)
                    {
                        if (pZprava[i] == ',') break;
                        pDuration += pZprava[i];
                    }
                    char[] p = { ':' };
                    string[] pParse = pDuration.Split(p);
                    ret = 0;
                    for (int i = 0; i < pParse.Length; i++)
                    {
                        pParse[i] = OdstranNulyZleva(pParse[i]);
                    }
                    if (pParse.Length == 1)
                    {
                        pDelka = (long)(float.Parse(pParse[0]) * 1000);
                    }
                    else if (pParse.Length == 2)
                    {
                        pDelka = (long)(float.Parse(pParse[0]) * 1000 * 60) + (long)(float.Parse(pParse[1]) * 1000);
                    }
                    else if (pParse.Length == 3)
                    {
                        pDelka = (long)(float.Parse(pParse[0]) * 1000 * 3600) + (long)(float.Parse(pParse[1]) * 1000 * 60) + (long)(float.Parse(pParse[2]) * 1000);
                    }


                }
                ret = pDelka;

                return ret;
            }
            catch (Exception)
            {
                return ret;
            }

        }



        //spusti prevod souboru na wav,ktery je mozno vykreslovat - skonci po naplneni zobrazovaciho bufferu, pot
        public bool ZacniPrevodSouboruNaDocasneWav(string aCesta, string aCestaDocasnychWAV, long aDelkaJednohoSouboruMS)
        {
            BinaryWriter output = null;
            try
            {
                //nulovani promennych
                this.docasneZvukoveSoubory.Clear();

                this._DelkaSouboruMS = this.VratDelkuSouboruMS(aCesta);
                if (aCesta == null || aCestaDocasnychWAV == null || DelkaSouboruMS <= 0) return false; //chybne nastaveni nebo delka souboru k prevodu

                this.CestaDocasnychWAV = aCestaDocasnychWAV;
                this.docasneZvukoveSoubory = new List<string>();

                delkaDocasnehoWav = (aDelkaJednohoSouboruMS / 1000) * this.pFrekvence;  //pocet vzorku v 1 docasnem souboru
                this.delkaDocasnehoWavMS = aDelkaJednohoSouboruMS;
                int pIndexDocasneho = -1;

                //vytvoren proces zapocato rucni vytvareni prevadeni docasneho souboru a plneni bufferu programu
                //po naplneni zobrazovaciho bufferu lze vykreslit vlnu v programu-

                prPrevod = new Process();
                prPrevod.StartInfo.FileName = this._AbsolutniCestaAdresareProgramu + MyKONST.CESTA_FFMPEG;
                prPrevod.StartInfo.WorkingDirectory = new FileInfo(aCesta).DirectoryName;
                //-f s16le bez hlavicky
                //prPrevod.StartInfo.Arguments = "-i " + aCesta + " -y -vn -ar 16000 -ac 1 -acodec pcm_s16le -f wav -";
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

                pPocetKanalu = 1;

                this.pFrekvence = 16000;

                this.pVelikostVzorku = 2;

                //pPocetVzorku = this.VelikostZobrazovacihoBufferu;   //pozor!! skutecna delka souboru je jina!!!!je nastavena az po prevodu
                ///pPocetVzorku = MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS * (this.pFrekvence / 1000);
                long pPocetVzorku2Delta = MyKONST.DELKA_PRVNIHO_RAMCE_ZOBRAZOVACIHO_BUFFERU_MS * (this.pFrekvence / 1000);
                pPocetVzorku = pPocetVzorku2Delta;
                long pPocetVzorku2 = MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS * (this.pFrekvence / 1000);

                //nacitani jednotlivych dat
                this.pDataNacitana = new Int16[pPocetVzorku2];

                int i = 0;


                short dato = 0;  //pomocna


                //prvni docasny wav soubor tmp
                pIndexDocasneho = 0;
                docasneZvukoveSoubory.Add(CestaDocasnychWAV + pIndexDocasneho.ToString() + ".wav");

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
                    //musime dat vlaknu sanci se ukoncit

                    pPocetNactenych = outstream.Read(buffer2, 0, buffer2.Length);
                    Buffer.BlockCopy(buffer2, 0, buffer2s, 0, pPocetNactenych);
                    int writeoffset = 0;
                    int writecnt = 0;
                    for (int k = 0; k < pPocetNactenych / 2; k++, i++)
                    {
                        //dato = BitConverter.ToInt16(buffer2, k*2);
                        dato = buffer2s[k];
                        pPocetNactenychVzorkuCelkem++;

                        if (i == pPocetVzorku) //nacten ramec prvni ramec
                        {
                            //poslani zpravy s daty bufferu
                            MyEventArgs e = new MyEventArgs(this.pDataNacitana, this.pDataNacitana.Length, 0, pPocetVzorku / this.pFrekvence * 1000, MyKONST.ID_ZOBRAZOVACIHO_BUFFERU_VLNY);
                            if (HaveData != null)
                                HaveData(this, e); // nacten ramec k zobrazeni a poslani tohoto ramce ven z tridy pomoci e
                            this._Nacteno = true;
                            if (pPocetVzorku + pPocetVzorku2Delta <= pPocetVzorku2 - 1)
                            {
                                pPocetVzorku += pPocetVzorku2Delta;
                            }
                        }

                        if (i < pPocetVzorku)
                        {
                            pDataNacitana[i] = dato;
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
                            output.Write((VytvorHlavickukWav((Int32)output.BaseStream.Length - 44)));

                            output.Close();
                            output = null;
                            pIndexDocasneho++;
                            docasneZvukoveSoubory.Add(CestaDocasnychWAV + pIndexDocasneho.ToString() + ".wav");

                            MyEventArgs2 e2 = new MyEventArgs2(pIndexDocasneho, (pIndexDocasneho + 1) * this.delkaDocasnehoWavMS);
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
                this._DelkaSouboruMS = (long)((float)(pPocetNactenychVzorkuCelkem) / (this.pFrekvence / 1000));
                if (output != null)
                {
                    //zapsani skutecne hlavicky WAV
                    output.Seek(0, SeekOrigin.Begin);
                    output.Write((VytvorHlavickukWav((Int32)output.BaseStream.Length - 44)));

                    output.Close();   //zavreni souboru pro zapis dat
                    output = null;
                    MyEventArgs2 e2 = new MyEventArgs2(pIndexDocasneho, this.DelkaSouboruMS);
                    if (HaveFileNumber != null)
                        HaveFileNumber(this, e2);
                }

                this._Nacteno = true;
                this._Prevedeno = true;  //dokoncen prevod audio souboru
                this._CestaSouboru = aCesta;

                if (i < pPocetVzorku)  //soubor je kratsi nez zobrazovaci buffer
                {
                    short[] pPole = new short[i];
                    for (int k = 0; k < i - 1; k++)
                    {
                        pPole[k] = this.pDataNacitana[k];
                    }
                    MyEventArgs e = new MyEventArgs(pPole, pPole.Length, 0, (long)((double)(i * 1000) / this.pFrekvence), MyKONST.ID_ZOBRAZOVACIHO_BUFFERU_VLNY);

                    //pokusne pridano
                    this.pPocetVzorku = i;



                    if (HaveData != null)
                        HaveData(this, e); // nacten ramec k zobrazeni a poslani tohoto ramce ven z tridy pomoci e
                }
                this.pPocetVzorku = i;

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
                this._Prevedeno = true; //i pres spadnuti je nacteni ok, doresit preteceni indexu!!!!!!
                if (TemporaryWavesDone != null && !(ex is ThreadAbortException))
                    TemporaryWavesDone(this, new EventArgs());
                //MessageBox.Show("Načítání a převod audio souboru se nezdařily..."+ ex.Message);

                return false;
            }
        }



        public bool RamecSynchronne = false;
        /// <summary>
        /// nacte ramec bufferu od dane casove pozice s nastavenou delkou a posle ho ven z tridy pomoci havedata/nebo ho pouze ulozi do vnitrni hodnoty
        /// </summary>
        /// <param name="aPocatekMS"></param>
        /// <param name="aDelkaMS"></param>
        /// <param name="aBuffer"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public bool NactiRamecBufferu(long aPocatekMS, long aDelkaMS, int aIDBufferu)
        {
            BinaryReader input = null;
            try
            {
                TimeSpan bufferstart = TimeSpan.FromMilliseconds(aPocatekMS);
                TimeSpan bufferLen = TimeSpan.FromMilliseconds(aDelkaMS);

                this._NacitaniBufferu = true;
                if (this.Nacteno)
                {
                    if (aPocatekMS > this.DelkaSouboruMS) return false; //pozadavek o vraceni dat mimo soubor
                    if (aPocatekMS + aDelkaMS > this.DelkaSouboruMS)
                    {
                        aDelkaMS = this.DelkaSouboruMS - aPocatekMS;
                    }

                    this.bIDBufferu = aIDBufferu;
                    int j = 0;  //index pro vystupni data
                    int delkadocasnehoMS = (int)(this.delkaDocasnehoWav / this.pFrekvence) * 1000;

                    int pIndexDocasneho = (int)aPocatekMS / delkadocasnehoMS;

                    if ((pIndexDocasneho >= 0) && (pIndexDocasneho < this.docasneZvukoveSoubory.Count - 1 || (pIndexDocasneho < this.docasneZvukoveSoubory.Count && this.Prevedeno)))
                    {
                        if (pIndexDocasneho >= docasneZvukoveSoubory.Count - 1 && !this.Prevedeno)
                        {
                            //pokus o cteni souboru dalsich nez jsou prevedeny
                            this._NacitaniBufferu = false;
                            return false;

                        }
                        FileInfo pfi = new FileInfo(docasneZvukoveSoubory[pIndexDocasneho]);
                        //long pPocetVzorkuDocasneho = this.delkaDocasnehoWav / 1000 * this.pFrekvence;   //pocet vzorku v docasnem souboru
                        long pPocetVzorkuDocasneho = (pfi.Length - 44) / 2;
                        long pPocetVzorkuNacist = aDelkaMS * (pFrekvence / 1000);
                        int pPocatecniIndexVSouboru = (int)((aPocatekMS - pIndexDocasneho * ((double)delkaDocasnehoWav / this.pFrekvence * 1000)) * (pFrekvence / 1000)) * this.pVelikostVzorku + 44;
                        int pNacistDat = (int)(pPocetVzorkuDocasneho - pPocatecniIndexVSouboru / this.pVelikostVzorku) + 22;
                        bool hotovo = false;
                        this.pDataNacitana = new short[pPocetVzorkuNacist];  //nove pole nacitanych obektu


                        long pPocetNactenych = 0;
                        while (!hotovo)
                        {
                            input = new BinaryReader(new FileStream(docasneZvukoveSoubory[pIndexDocasneho], FileMode.Open, FileAccess.Read, FileShare.Read));

                            if (input != null)
                            {

                                pIndexDocasneho++;
                                int pCount = (int)(pNacistDat) * this.pVelikostVzorku;

                                byte[] pBuffer = new byte[pCount];
                                input.BaseStream.Seek(pPocatecniIndexVSouboru, SeekOrigin.Begin);

                                pPocetNactenych += input.Read(pBuffer, 0, pCount);
                                input.Close();
                                input = null;


                                //prevod do pole nacitanych dat
                                //TODO:(x) --- Buffer.BlockCopy(pBuffer, 0, pDataNacitana, j * 2, pCount);

                                for (int i = 0; i < pCount / 2; i++)
                                {
                                    this.pDataNacitana[j] = BitConverter.ToInt16(pBuffer, i * this.pVelikostVzorku);
                                    j++;
                                    if (j >= pPocetVzorkuNacist)
                                    {
                                        //prevod dokoncen
                                        hotovo = true;
                                        break;
                                    }
                                }

                                if (pIndexDocasneho >= docasneZvukoveSoubory.Count - 1 && !this.Prevedeno)
                                {
                                    //pokus o nacteni pozuzivaneho souboru behem prevodu
                                    this._NacitaniBufferu = false;
                                    return false;
                                }

                                if (pIndexDocasneho < docasneZvukoveSoubory.Count)
                                {
                                    pfi = new FileInfo(docasneZvukoveSoubory[pIndexDocasneho]);
                                    pPocetVzorkuDocasneho = (pfi.Length - 44) / 2;
                                    pPocatecniIndexVSouboru = 44;
                                    pNacistDat = (int)(pPocetVzorkuDocasneho - pPocatecniIndexVSouboru);
                                }
                                else
                                {
                                    hotovo = true;

                                }


                            }


                        }



                        MyEventArgs e = new MyEventArgs(this.pDataNacitana, this.pDataNacitana.Length, aPocatekMS, aPocatekMS + aDelkaMS, aIDBufferu);
                        if (HaveData != null && RamecSynchronne == false)
                        {
                            /*BinaryWriter bw = new BinaryWriter(new FileStream("C:\\buffer.pcm", FileMode.Create));
                            foreach (short s in pDataNacitana)
                                bw.Write(s);
                            bw.Close();*/
                            HaveData(this, e); // nacten ramec k zvuku a poslani tohoto ramce ven z tridy pomoci e
                        }
                        else
                        {
                            NacitanyBufferSynchronne = e;
                            RamecSynchronne = false;
                        }
                        this._NacitaniBufferu = false;   //nacitani bufferu bylo dokonceno
                        return true;
                    }

                }
                this._NacitaniBufferu = false;
                return false;
            }
            catch //(Exception ex)
            {
                if (input != null)
                {
                    input.Close();
                    input = null;
                }

                //MessageBox.Show("Chyba pri nacitani dalsich ramcu: " + ex.Message);
                this._NacitaniBufferu = false;
                return false;
            }

        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public short[] NactiRamecBufferu(TimeSpan begin, TimeSpan end)
        {
            short[] data = null;
            try
            {
                TimeSpan bufferstart = begin;
                TimeSpan bufferLen = end - begin;
                long aPocatekMS = (long)begin.TotalMilliseconds;
                long aDelkaMS = (long)bufferLen.TotalMilliseconds;
                this._NacitaniBufferu = true;
                if (this.Nacteno)
                {
                    if (aPocatekMS > this.DelkaSouboruMS) return null; //pozadavek o vraceni dat mimo soubor
                    if (aPocatekMS + aDelkaMS > this.DelkaSouboruMS)
                    {
                        aDelkaMS = this.DelkaSouboruMS - aPocatekMS;
                    }

                    int j = 0;  //index pro vystupni data
                    int delkadocasnehoMS = (int)(this.delkaDocasnehoWav / this.pFrekvence) * 1000;

                    int pIndexDocasneho = (int)aPocatekMS / delkadocasnehoMS;

                    if ((pIndexDocasneho >= 0) && (pIndexDocasneho < this.docasneZvukoveSoubory.Count - 1 || (pIndexDocasneho < this.docasneZvukoveSoubory.Count && this.Prevedeno)))
                    {
                        if (pIndexDocasneho >= docasneZvukoveSoubory.Count - 1 && !this.Prevedeno)
                        {
                            //pokus o cteni souboru dalsich nez jsou prevedeny
                            this._NacitaniBufferu = false;
                            return null;

                        }
                        FileInfo pfi = new FileInfo(docasneZvukoveSoubory[pIndexDocasneho]);
                        //long pPocetVzorkuDocasneho = this.delkaDocasnehoWav / 1000 * this.pFrekvence;   //pocet vzorku v docasnem souboru
                        long pPocetVzorkuDocasneho = (pfi.Length - 44) / 2;
                        long pPocetVzorkuNacist = aDelkaMS * (pFrekvence / 1000);
                        int pPocatecniIndexVSouboru = (int)((aPocatekMS - pIndexDocasneho * ((double)delkaDocasnehoWav / this.pFrekvence * 1000)) * (pFrekvence / 1000)) * this.pVelikostVzorku + 44;
                        int pNacistDat = (int)(pPocetVzorkuDocasneho - pPocatecniIndexVSouboru / this.pVelikostVzorku) + 22;
                        bool hotovo = false;
                        data = new short[pPocetVzorkuNacist];  //nove pole nacitanych obektu


                        long pPocetNactenych = 0;
                        while (!hotovo)
                        {
                            using (BinaryReader input = new BinaryReader(new FileStream(docasneZvukoveSoubory[pIndexDocasneho], FileMode.Open, FileAccess.Read, FileShare.Read)))
                            {

                                pIndexDocasneho++;
                                int pCount = (int)(pNacistDat) * this.pVelikostVzorku;

                                byte[] pBuffer = new byte[pCount];

                                input.BaseStream.Seek(pPocatecniIndexVSouboru, SeekOrigin.Begin);

                                pPocetNactenych += input.Read(pBuffer, 0, pCount);
                                input.Close();

                                //prevod do pole nacitanych dat

                                int cnt = (pBuffer.Length / 2 < data.Length - j) ? pBuffer.Length : (data.Length - j) * 2;
                                Buffer.BlockCopy(pBuffer, 0, data, j * 2, cnt);
                                j += pBuffer.Length / 2;
                                if (j >= pPocetVzorkuNacist)
                                {
                                    //prevod dokoncen
                                    hotovo = true;
                                    break;
                                }

                                if (pIndexDocasneho >= docasneZvukoveSoubory.Count - 1 && !this.Prevedeno)
                                {
                                    //pokus o nacteni pozuzivaneho souboru behem prevodu
                                    this._NacitaniBufferu = false;
                                    return null;
                                }

                                if (pIndexDocasneho < docasneZvukoveSoubory.Count)
                                {
                                    pfi = new FileInfo(docasneZvukoveSoubory[pIndexDocasneho]);
                                    pPocetVzorkuDocasneho = (pfi.Length - 44) / 2;
                                    pPocatecniIndexVSouboru = 44;
                                    pNacistDat = (int)(pPocetVzorkuDocasneho - pPocatecniIndexVSouboru);
                                }
                                else
                                {
                                    hotovo = true;

                                }
                            }
                        }
                    }

                }
                this._NacitaniBufferu = false;
            }
            catch
            { 
                data = null;
            }
            finally 
            { 
                this._NacitaniBufferu = false;
            }
            return data;
        }

        #region IDisposable Members

        /// <summary>
        /// zrusi vsechny procesy a thredy, ale trida je pripravena pro nove zpracovani souboru
        /// </summary>
        public void Dispose()
        {
            try
            {

                //zruseni threadu
                if (this.tPrevodNaDocasneSoubory != null && this.tPrevodNaDocasneSoubory.ThreadState == System.Threading.ThreadState.Running)
                {
                    tPrevodNaDocasneSoubory.Abort();
                    tPrevodNaDocasneSoubory.Join();

                }
                if (this.tNacitaniBufferu1 != null && tNacitaniBufferu1.ThreadState == System.Threading.ThreadState.Running)
                {
                    tNacitaniBufferu1.Abort();
                    if (tNacitaniBufferu2 != null && tNacitaniBufferu2.ThreadState == System.Threading.ThreadState.Running)
                    {
                        tNacitaniBufferu2.Abort();
                        tNacitaniBufferu2.Join();
                    }
                }
                //zruseni procesu prevodu souboru na wav
                if (prPrevod != null && !prPrevod.HasExited)
                {
                    prPrevod.Kill();
                    prPrevod.WaitForExit();
                }

                //smazani vsech docasnych souboru z temp adresare

                FileInfo fi = new FileInfo(MyKONST.CESTA_DOCASNYCH_SOUBORU_ZVUKU);
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
                this._Prevedeno = false;
                this._Nacteno = false;
                ///this._dataProVykresleniNahleduVlny = null;
                this.docasneZvukoveSoubory.Clear();
            }
            catch// (Exception ex)
            {
            }
        }

        #endregion
    }
}
