using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NanoTrans
{
    /// <summary>
    /// trida starajici se o spravu dat audio vlny - buffery audio souboru atd...
    /// </summary>
    public class MyVlna
    {
        /// <summary>
        /// buffer pro podrobne zobrazeni a prevazne pro prehravani audio dat 16000, 2byty, mono
        /// </summary>
        public MyBuffer16 bufferPrehravaniZvuku = null;

        /// <summary>
        /// buffer pro rychle zobrazeni vsech dat vlny - obsahuje pole floatu,ktere je mozno zobrazit
        /// </summary>
        public MyBufferVlny bufferCeleVlny = null;

                
        /// <summary>
        /// property kolik milisekund je zobrazenych ve vlne
        /// </summary>
        public long DelkaVlnyMS { get; set; }               

        private long _mSekundyVlnyZac;
        /// <summary>
        /// ziskani a nastaveni zacatku vlny
        /// </summary>
        public long mSekundyVlnyZac
        {
            get { return _mSekundyVlnyZac; }
            set
            {
                if (value < 0) _mSekundyVlnyZac = 0;
                else
                {
                    _mSekundyVlnyZac = value;
                }
            }
        }
        
        public long mSekundyVlnyKon { get; set; }
        public long MSekundyDelta { get; set; }
        
        public bool PouzeCasovaOsa { get; set; }            //nakresli pouze casovou osu bez vlny

        public long mSekundyMalySkok { get; set; }              //o kolik se posune kurzor audia pri skoku pomoci alt a sipek

        /// <summary>
        /// udava kolikrat jak je zvetsena vlna v procentech v y smeru
        /// </summary>
        public float ZvetseniVlnyYSmerProcenta { get; set; }
        /// <summary>
        /// automaticke prizpusobeni osy
        /// </summary>
        public bool AutomatickeMeritko { get; set; }

        private long _KurzorPozice;
        public long KurzorPoziceMS 
        { 
            get
            {
                return _KurzorPozice; 
            } 
            set
            {

                _KurzorPozice = value; 
            } 
        }     //pozice kurzoru prehravani v ms
        private long _KurzorVyberPocatekMS;
        /// <summary>
        /// pozice vyberu vyberu v ms
        /// </summary>
        public long KurzorVyberPocatekMS
        {
            get { return _KurzorVyberPocatekMS; }
            set 
            { 
                _KurzorVyberPocatekMS = value;
                StackTrace st = new StackTrace(true);
                string trace = "";
                foreach (var frame in st.GetFrames())
                {
                    trace += frame.GetMethod().Name + frame.GetFileLineNumber() + ">";
                }
                System.Diagnostics.Debug.WriteLine(trace);
            
            }
        }
        public long KurzorVyberKonecMS { get; set; }     //pozice kurzoru vyberu v ms
        
        public bool MouseLeftDown { get; set; }         //info o vyberu...


        /// <summary>
        /// konstruktor - vytvori buffer pro zobrazeni vlny s vychozi velikosti
        /// </summary>
        /// <param name="aPocatecniVelikostBufferu"></param>
        public MyVlna(long aPocatecniVelikostBufferuMS)
        {
            this.bufferPrehravaniZvuku = new MyBuffer16(aPocatecniVelikostBufferuMS);
            this.bufferPrehravaniZvuku.UlozDataDoBufferuNaKonec(new short[480000], 30000);
            this.bufferCeleVlny = new MyBufferVlny(aPocatecniVelikostBufferuMS, MyKONST.ROZLISENI_ZOBRAZENI_VLNY_S);
            
            //constructor
            KurzorPoziceMS = 0;
            KurzorVyberPocatekMS = 0;
            KurzorVyberKonecMS = 0;
            MouseLeftDown = false;


            DelkaVlnyMS = 30000;

            mSekundyVlnyZac = 0;
            mSekundyVlnyKon = 30000;
            
            MSekundyDelta = 1000;

            mSekundyMalySkok = 1000;

            PouzeCasovaOsa = false;

            ZvetseniVlnyYSmerProcenta = 100;
            AutomatickeMeritko = true;
        }

        public void NastavDelkuVlny(long mSekundy)
        {
            DelkaVlnyMS = mSekundy;
            MSekundyDelta = DelkaVlnyMS / 60;
        }

    }
}
