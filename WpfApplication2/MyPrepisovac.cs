using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace NanoTrans
{
   // public delegate void DataPrectenaEventHandler(object sender, EventArgs e); //delegovana udalostni metoda pro posilani prectene zpravy z rozpoznavace

    public class MyEventArgsPrectenaData : EventArgs            //upravene eventargs pro predani dat dale
    {
        public MyEventArgsPrectenaData(MyPrepisovac sender, string zprava)
        {
            this.zprava = zprava;
            this.sender = sender;
        }
        public string zprava;
        public MyPrepisovac sender;
    }
    
    /// <summary>
    /// trida starajici se o napojeni rozpoznavace a jeho nastaveni, samotne roypoynavani a vraceni textu
    /// </summary>
    public class MyPrepisovac : IDisposable
    {
        private bool pZapisZpravy = false;
        
        /// <summary>
        /// proces rozpoznavace, spusten v threadu
        /// </summary>
        private Process prRozpoznavac = null;
        private Thread threadCteniZpravy = null;
        private Thread threadZapisZpravy = null;
        private Thread threadZapisStop = null;
        private bool _IsDisposed = false;
        /// <summary>
        /// info zda je objekt ve stavu po zavolani Disposed()
        /// </summary>
        public bool IsDisposed { get { return _IsDisposed; } }

        public MyBuffer16 bufferProPrepsani = null;
        public long pomocnaCasPrepsaniMS = 0;//jaky cas je prepsan z bufferu

        public MyBuffer16 bufferProHlasoveOvladani = null;
        public long pomocnaCasPlneniMS = 0; //kam zapisovat

        public bool _Inicializovano = false;
        /// <summary>
        /// informace zda je rozpoznavac inicializovan
        /// </summary>
        public bool Inicializovano
        {
            get { if (prRozpoznavac == null || prRozpoznavac.HasExited == true) return false; else return true; }
        }
        
        private bool _Rozpoznavani = false;
        /// <summary>
        /// info zda rozpoznavac rozpoznava, pokud ne, lze zavolat start a zadavat data
        /// </summary>
        public bool Rozpoznavani
        {
            get
            {
                if (this.prRozpoznavac == null || this.prRozpoznavac.HasExited) return false;
                return _Rozpoznavani;
            }
        }

        private bool _RozpoznavaniHlasu = false;
        /// <summary>
        /// info zda dochazi k rozpoznavani hlasu z mikrofonu - diktat nebo vyhodnocovani povelu
        /// </summary>
        public bool RozpoznavaniHlasu
        {
            get
            {
                if (this.prRozpoznavac == null || this.prRozpoznavac.HasExited) return false;
                return _RozpoznavaniHlasu;
            }
        }

        private short _TypRozpoznavani = -1;
        /// <summary>
        /// konstatnta udavajici v jakem modu je rozpoznavac spusten
        /// </summary>
        public short TypRozpoznavani
        {
            get { return _TypRozpoznavani; }
        }
        


        private bool _Ukoncovani = false;
        /// <summary>
        /// informace, zda je dokoncovan prepis - prepisovac ukonci svoji cinnost
        /// </summary>
        public bool Ukoncovani
        {
            get
            {
                if (this.prRozpoznavac == null || this.prRozpoznavac.HasExited) return false;
                return _Ukoncovani;
            }
        }


        /// <summary>
        /// zde je uchovavan prepsany text od posledniho spusteni new_session - pomocna promenna pro externi zapis
        /// </summary>
        public string PrepsanyText;

        private string _CestaNanocore = null;
        
        private string CestaLicencniSoubor;
        private string CestaMluvci = "amodels/male.amd"; //default mluvci
        private string CestaJazykovyModel = "lmodels/spoken_20081128-20081130_153158-shd3-ubm.bin";
        private string CestaPrepisovaciPravidla = "drules/spoken_20081128.ppp";
        private string CestaLicencniServer = "quadira.ite.tul.cz";
        private string InterniVelikostBufferu = "6000";
        private string KvalitaPrepisu = "50";

        private EventHandler _EventPrectenaDataRozpoznavace = null;

        
        /// <summary>
        /// construktor - pozaduje cestu k nanocore-rozpoznavaci - pouze adresar, a nazev souboru licence
        /// </summary>
        public MyPrepisovac(string aCestaNanocore, string aCestaMluvci, string aCestaJazykovyModel, string aCestaPrepisovaciPravidla, string aLicencniServer, string aCestaLicencniSoubor, long aInterniBuffera, long aKvalitaPrepisu, EventHandler aEventPrectenaDataRozpoznavace)
        {
            this._CestaNanocore = aCestaNanocore;
            this.CestaLicencniSoubor = aCestaLicencniSoubor;
            FileInfo fi = new FileInfo(aCestaNanocore + "/nanocore.dll");
            if (!fi.Exists) this._CestaNanocore = null; //neexistence nanocore
            fi = new FileInfo(aCestaLicencniSoubor);
            //if (!fi.Exists) this.CestaLicencniSoubor = null; //neexistence licence
            this._EventPrectenaDataRozpoznavace = aEventPrectenaDataRozpoznavace;

            if (aCestaMluvci != "" && aCestaMluvci != null) this.CestaMluvci = aCestaMluvci;
            if (aCestaJazykovyModel != "" && aCestaJazykovyModel != null) this.CestaJazykovyModel = aCestaJazykovyModel;
            if (aCestaPrepisovaciPravidla != "" && aCestaPrepisovaciPravidla != null) this.CestaPrepisovaciPravidla = aCestaPrepisovaciPravidla;
            if (aLicencniServer != "" && aLicencniServer != null) this.CestaLicencniServer = aLicencniServer;
            if (aInterniBuffera > 0) this.InterniVelikostBufferu = aInterniBuffera.ToString();
            this.KvalitaPrepisu = aKvalitaPrepisu.ToString();
            
            //buffer ovladani
            this.bufferProHlasoveOvladani = new MyBuffer16(60000); //minutove zpozdeni
            this._IsDisposed = false;
        }

        /// <summary>
        /// inicializuje rozpoznavac s defaultnimi hodnotami
        /// </summary>
        /// <returns></returns>
        public int InicializaceRozpoznavace()
        {
            return InicializaceRozpoznavace(null, null, null, null, null, null, null);
        }
        
        /// <summary>
        /// Pokusi se znovu inicializovat proces nanocore rozpoznavace s danym nastavenim - null v hodnotach je pouzita defaultni hodnota
        /// </summary>
        /// <param name="aCestaNanocore"></param>
        /// <param name="aLicencniSouborNazev"></param>
        /// <returns></returns>
        public int InicializaceRozpoznavace(string aCestaNanocore, string aLicencniSouborNazev, string aCestaMluvci, string aCestaJazykovyModel, string aCestaPrepisovaciPravidla, string aInterniVelikostBufferu, string aKvalitaPrepisu)
        {
            try
            {
                //nova inicializace
                if (!Inicializovano||(this.Inicializovano && (aCestaNanocore != _CestaNanocore ||aLicencniSouborNazev!=CestaLicencniSoubor || aCestaMluvci != CestaMluvci || aCestaJazykovyModel != CestaJazykovyModel || aCestaPrepisovaciPravidla != CestaPrepisovaciPravidla || aInterniVelikostBufferu != InterniVelikostBufferu || aKvalitaPrepisu!=KvalitaPrepisu)))
                {
                    if (!this.IsDisposed)
                    {
                        this.Dispose();
                    }


                    if (aCestaNanocore != null) this._CestaNanocore = aCestaNanocore;
                    if (aLicencniSouborNazev != null) this.CestaLicencniSoubor = aLicencniSouborNazev;
                    if (aCestaMluvci != null) this.CestaMluvci = aCestaMluvci;
                    if (aCestaJazykovyModel != null) this.CestaJazykovyModel = aCestaJazykovyModel;
                    if (aCestaPrepisovaciPravidla != null) this.CestaPrepisovaciPravidla = aCestaPrepisovaciPravidla;
                    if (aInterniVelikostBufferu != null) this.InterniVelikostBufferu = aInterniVelikostBufferu;
                    if (aKvalitaPrepisu != null) this.KvalitaPrepisu = aKvalitaPrepisu;

                    this.bufferProHlasoveOvladani.SmazBuffer();


                    prRozpoznavac = new Process();
                    prRozpoznavac.Exited += new EventHandler(prRozpoznavac_Exited);
                    prRozpoznavac.StartInfo.FileName = this._CestaNanocore + "/nanocore.dll";
                    prRozpoznavac.StartInfo.WorkingDirectory = new FileInfo(this._CestaNanocore + "/").DirectoryName;

                    //-mpt 100-120             //rychlost online rozpoznavani
                    //nanocore.dll -am amodels/male.amd -lm lmodels/spoken_20081128-20081130_153158-shd3-ubm.bin -pm drules/spoken_20081128.ppp -lf martin.cickan@tul.cz1 -lsa quadira.ite.tul.cz -lbs 6000 < testvector.raw 
                    //prRozpoznavac.StartInfo.Arguments = "-am " + CestaMluvci + " -perestrojka" + " -lm " + CestaJazykovyModel + " -pm " + CestaPrepisovaciPravidla + " -lf " + "\"" + CestaLicencniSoubor + "\"" + " -lsa " + CestaLicencniServer + " -lbs " + this.InterniVelikostBufferu; //nova data bez hlavicky
                    prRozpoznavac.StartInfo.Arguments = "-am " + CestaMluvci + " -lm " + CestaJazykovyModel + " -pm " + CestaPrepisovaciPravidla + " -lf " + "\"" + CestaLicencniSoubor + "\"" + " -lsa " + CestaLicencniServer + " -lbs " + this.InterniVelikostBufferu; //nova data bez hlavicky
                    if (aKvalitaPrepisu != null) prRozpoznavac.StartInfo.Arguments += " -mpt " + aKvalitaPrepisu;
                    //prRozpoznavac.StartInfo.Arguments = "-am amodels/male.amd -lm lmodels/spoken_20081128-20081130_153158-shd3-ubm.bin -pm drules/spoken_20081128.ppp -lf martin.cickan@tul.cz1 -lsa quadira.ite.tul.cz -lbs 6000 < testvector.raw";

                    prRozpoznavac.StartInfo.RedirectStandardInput = true;
                    prRozpoznavac.StartInfo.RedirectStandardOutput = true;
                    prRozpoznavac.StartInfo.RedirectStandardError = false;
                    prRozpoznavac.StartInfo.UseShellExecute = false;
                    prRozpoznavac.StartInfo.CreateNoWindow = true;
                    prRozpoznavac.Start();
                    prRozpoznavac.PriorityClass = ProcessPriorityClass.Normal;

                    this.pomocnaCasPlneniMS = 0;
                    this.pomocnaCasPrepsaniMS = 0;
                    this.PrepsanyText = "";
                    this.bufferProHlasoveOvladani.SmazBuffer();


                    this._Rozpoznavani = false;
                    this._Ukoncovani = false;
                    this._IsDisposed = false;
                    MyEventArgsPrectenaData e = new MyEventArgsPrectenaData(this, "INITIALIZING");
                    this._EventPrectenaDataRozpoznavace(this, e);
                }
                else if (Inicializovano)
                {
                    //pokud je rozpoznavac jiz inicializovan beze zmeny je rucne veaceno initialized, aby mohl byt v programu spravne nastaven
                    MyEventArgsPrectenaData e = new MyEventArgsPrectenaData(this, "INITIALIZED");
                    this._EventPrectenaDataRozpoznavace(this, e);
                }
                return 0;
            }
            catch (Exception)
            {
                MessageBox.Show("Chybné nastavení přepisovacího softwaru - ověřte licenční soubor a jiné nastavení", "Varování", MessageBoxButton.OK, MessageBoxImage.Warning);
                return -1;
            }
        }

        void prRozpoznavac_Exited(object sender, EventArgs e)
        {
            try
            {
                if (!IsDisposed)
                {
                    MyEventArgsPrectenaData me = new MyEventArgsPrectenaData(this, "END_PROCESS");
                    this._Inicializovano = false;
                    this._EventPrectenaDataRozpoznavace(this, me);
                }
            }
            catch
            {
                
            }
        }


        /// <summary>
        /// vytvori novou session a je pripraven na rozpoznani
        /// </summary>
        /// <returns></returns>
        public int Start(short aStavRozpoznavace)
        {
            if (pZapisZpravy)
            {
                return -1;
            }

            if (prRozpoznavac == null) return 1;
            if (prRozpoznavac.HasExited) return 2;
            if (this.Rozpoznavani || this.Ukoncovani) return 3;


            Int32 TypZpravy = 0;
            if (aStavRozpoznavace == MyKONST.ROZPOZNAVAC_0_OFFLINE_ROZPOZNAVANI)
            {
                TypZpravy = 8;
            }
            Int32 DelkaZpravy = 0;

            byte[] pole1TypZpravy = BitConverter.GetBytes(TypZpravy);
            byte[] pole2DelkaZpravy = BitConverter.GetBytes(DelkaZpravy);

           

            //prRozpoznavac.StandardInput.BaseStream.Write(pole, 0, 8);
            string zprava = Encoding.ASCII.GetString(pole1TypZpravy) + Encoding.ASCII.GetString(pole2DelkaZpravy);

            //prRozpoznavac.StandardInput.Write(zprava);
            byte[] pPole = new byte[8];
            
            //pPole = Encoding.ASCII.GetBytes(zprava);

            //prRozpoznavac.StandardInput.BaseStream.Write(pPole, 0, 8);
            pole1TypZpravy.CopyTo(pPole, 0);
            pole2DelkaZpravy.CopyTo(pPole, 4);

            
            prRozpoznavac.StandardInput.BaseStream.Write(pPole, 0, pPole.Length);
            prRozpoznavac.StandardInput.BaseStream.Flush();
            
            this._Rozpoznavani = true;
            this._TypRozpoznavani = aStavRozpoznavace;
            this._RozpoznavaniHlasu = aStavRozpoznavace > 0;
            return 0;
        }

        /// <summary>
        /// okamzite zastavi rozpoznavani - musi se ale pockat na end_session
        /// </summary>
        /// <returns></returns>
        public int StopHned()
        {
            if (this.Rozpoznavani || this.Ukoncovani)
            {

                //this._Rozpoznavani = false;
                if (prRozpoznavac == null) return 1;
                if (prRozpoznavac.HasExited) return 2;
                if (this.threadZapisZpravy != null) threadZapisZpravy.Join(10000);

                Int32 TypZpravy = 3;
                Int32 DelkaZpravy = 0;
                byte[] pole1TypZpravy = BitConverter.GetBytes(TypZpravy);
                byte[] pole2DelkaZpravy = BitConverter.GetBytes(DelkaZpravy);
                byte[] zprava = new byte[8];
                pole1TypZpravy.CopyTo(zprava, 0);
                pole2DelkaZpravy.CopyTo(zprava, 4);

                prRozpoznavac.StandardInput.BaseStream.Write(zprava, 0, zprava.Length);
                prRozpoznavac.StandardInput.BaseStream.Flush();
                this._Rozpoznavani = false;
                this._Ukoncovani = true;
                return 0;
            }
            return -1;

        }


        /// <summary>
        /// Stop s dokonceni prepisu - asynchronni - hlida ostatni thredy zapisu
        /// </summary>
        public void AsynchronniStop()
        {
                if (this.threadZapisStop == null || this.threadZapisStop.ThreadState != System.Threading.ThreadState.Running)
                {
                    threadZapisStop = new Thread(Stop) { Name = "AsynchronniStop()" };
                    threadZapisStop.Start();
                }
        }
        
        /// <summary>
        /// Stop s dokonceni prepisu
        /// </summary>
        /// <returns></returns>
        private void Stop()
        {
                if (prRozpoznavac != null && !prRozpoznavac.HasExited && !Ukoncovani && Rozpoznavani)
                {
                    this._Ukoncovani = true;
                    if (threadZapisZpravy != null && threadZapisZpravy.ThreadState != System.Threading.ThreadState.Unstarted) threadZapisZpravy.Join();    //zapise info o ukonceni az po skonceni zapisu dat



                    Int32 TypZpravy = 6;
                    Int32 DelkaZpravy = 0;
                    byte[] pole1TypZpravy = BitConverter.GetBytes(TypZpravy);
                    byte[] pole2DelkaZpravy = BitConverter.GetBytes(DelkaZpravy);
                    byte[] zprava = new byte[8];
                    pole1TypZpravy.CopyTo(zprava, 0);
                    pole2DelkaZpravy.CopyTo(zprava, 4);

                    prRozpoznavac.StandardInput.BaseStream.Write(zprava, 0, zprava.Length);
                    prRozpoznavac.StandardInput.BaseStream.Flush();
                }

        }



        
        
        
        
        /// <summary>
        /// object - pole bytu
        /// </summary>
        /// <param name="aPoleDat"></param>
        private void Data(object aPoleDat)
        {

            try
            {
                this.pZapisZpravy = true;
                byte[] pPoleDat = (byte[])aPoleDat;
                for (int i = 0; i < (pPoleDat.Length / 320); i++)
                {


                    Int32 TypZpravy = 1;
                    Int32 DelkaZpravy = 320;
                    byte[] pole1TypZpravy = BitConverter.GetBytes(TypZpravy);
                    byte[] pole2DelkaZpravy = BitConverter.GetBytes(DelkaZpravy);
                    byte[] zprava = new byte[328];
                    pole1TypZpravy.CopyTo(zprava, 0);
                    pole2DelkaZpravy.CopyTo(zprava, 4);

                    for (int j = 0; j < 320 && (i * 320 + j) < pPoleDat.Length; j++)
                    {
                        zprava[j + 8] = pPoleDat[i * 320 + j];
                    }


                    
                    //zprava += Encoding.ASCII.GetString(pPoleDat, i * 320, 320);
                    //prRozpoznavac.StandardInput.Write(zprava);
                    if (prRozpoznavac.StandardInput == null || !prRozpoznavac.StandardInput.BaseStream.CanWrite)
                    {
                    }
                    else
                    {
                        prRozpoznavac.StandardInput.BaseStream.Write(zprava, 0, zprava.Length);
                    }

                }
                this.pZapisZpravy = false;
            }
            catch// (Exception ex)
            {
                this.pZapisZpravy = false;
            }

           
        }

       
        /// <summary>
        /// asynchronne zapise data v threadu - pokud jiz dochazi k zapisu, vraci false a data nejsou zapsana
        /// </summary>
        /// <param name="aData"></param>
        /// <returns></returns>
        public bool AsynchronniZapsaniDat(byte[] aData)
        {
            try
            {
                if (threadZapisZpravy == null || threadZapisZpravy.ThreadState != System.Threading.ThreadState.Running)
                {
                    if (!_Inicializovano)
                    {
                        return false;
                    }
                    if (pZapisZpravy)
                    {
                        return false;
                    }
                    threadZapisZpravy = new Thread(Data);
                    threadZapisZpravy.Start(aData);
                    return true;
                }
                else
                {
                    return false;
                    //Thread t1 = new Thread(Data);
                    //threadZapisZpravy.Join();
                    //t1.Start(aData);
                }
            }
            catch// (Exception ex)
            {
                return false;
            }

        }

        public int GetDelay()
        {
            //zruseni zpravy pokud se jeste zapisuji data v jinem threadu
            if (threadZapisZpravy == null || this.threadZapisZpravy.ThreadState == System.Threading.ThreadState.Running) return 1;
            if (!_Inicializovano)
            {
                return -1;
            }
            
            if (pZapisZpravy)
            {
                return -1;
            }
            else
            {
                pZapisZpravy = true;
                Int32 TypZpravy = 7;
                Int32 DelkaZpravy = 0;

                byte[] pole1TypZpravy = BitConverter.GetBytes(TypZpravy);
                byte[] pole2DelkaZpravy = BitConverter.GetBytes(DelkaZpravy);
                byte[] sp = new byte[8];
                pole1TypZpravy.CopyTo(sp, 0);
                pole2DelkaZpravy.CopyTo(sp, 4);

                //prRozpoznavac.StandardInput.BaseStream.Write(sp, 0, sp.Length);




                string zprava = Encoding.ASCII.GetString(pole1TypZpravy) + Encoding.ASCII.GetString(pole2DelkaZpravy);
                //prRozpoznavac.StandardInput.Write(zprava);
                prRozpoznavac.StandardInput.BaseStream.Write(sp, 0, 8);
                prRozpoznavac.StandardInput.BaseStream.Flush();
                pZapisZpravy = false;
                return 0;
            }
        }

        public int GetText()
        {
            //zruseni zpravy pokud se jeste zapisuji data v jinem threadu
            if (!this._Inicializovano)
            {
                return -1;
            }
            if (pZapisZpravy)
            {
                return -1;
            }
            else
            {
                if (threadZapisZpravy == null || this.threadZapisZpravy.ThreadState == System.Threading.ThreadState.Running) return 1;
                pZapisZpravy = true;
                Int32 TypZpravy = 2;
                Int32 DelkaZpravy = 0;

                byte[] pole1TypZpravy = BitConverter.GetBytes(TypZpravy);
                byte[] pole2DelkaZpravy = BitConverter.GetBytes(DelkaZpravy);
                byte[] sp = new byte[8];
                pole1TypZpravy.CopyTo(sp, 0);
                pole2DelkaZpravy.CopyTo(sp, 4);

                //prRozpoznavac.StandardInput.BaseStream.Write(sp, 0, sp.Length);
                string zprava = Encoding.ASCII.GetString(pole1TypZpravy) + Encoding.ASCII.GetString(pole2DelkaZpravy);
                //prRozpoznavac.StandardInput.Write(zprava);
                prRozpoznavac.StandardInput.BaseStream.Write(sp, 0, 8);
                prRozpoznavac.StandardInput.BaseStream.Flush();
                pZapisZpravy = false;
                return 0;
            }
        }

       
        /// <summary>
        /// precte aktualni zpravu od rozpoznavace
        /// </summary>
        private void ReadMessage()
        {


            byte[] buffer = new byte[1000];


            int delka = prRozpoznavac.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);

            string ret = Encoding.Default.GetString(buffer, 0, delka);

            if (ret.Contains("END_SESSION"))
            {
                this._Ukoncovani = false;
                this._Rozpoznavani = false;
            }
            if (ret.Contains("INITIALIZED"))
            {
                this._Inicializovano = true;
            }

            //ret = prRozpoznavac.StandardOutput.ReadLine();
            MyEventArgsPrectenaData e = new MyEventArgsPrectenaData(this, ret);
            this._EventPrectenaDataRozpoznavace(this, e);
            
            //HaveDataPrectena(this, e);
            
        }

        /// <summary>
        /// Spusti thread a ceka na nactena data - data vraci pomoci udalosti - thread je spusten pouze jeden
        /// </summary>
        public void AsynchronniRead()
        {
            if (threadCteniZpravy == null || threadCteniZpravy.ThreadState != System.Threading.ThreadState.Running)
            {
                threadCteniZpravy = new Thread(ReadMessage) { Name = "AsynchronniRead()" };
                threadCteniZpravy.Start();
            }
            
        }

        private byte[] StringToByteArray(string s)
        {
           //ASCIIEncoding encoding = new ASCIIEncoding();
            //return Encoding.Default.GetBytes(s);
            return Encoding.GetEncoding(1250).GetBytes(s);
        }

               

        public void Dispose()
        {
                _IsDisposed = true;
                if (threadCteniZpravy != null) threadCteniZpravy.Abort();
                if (threadZapisStop != null) threadZapisStop.Abort();
                if (threadZapisZpravy != null) threadZapisZpravy.Abort();
                if (this.prRozpoznavac != null && !this.prRozpoznavac.HasExited)
                {
                    prRozpoznavac.Kill();
                }

        }

        
    }
}
