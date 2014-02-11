using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.IO;

namespace NanoTrans
{
    public enum MyEnumVerze
    {
        /// <summary>
        /// bez podpory TTA a automatickeho prepisu
        /// </summary>
        Externi = 0,
        /// <summary>
        /// vsechny funkce
        /// </summary>
        Interni = 1
    }
    public enum MyEnumJazyk
    {
        cestina = 0,
        anglictina = 1
    }

    //staticka trida obsahujici metody pro praci s textem atd...
    public static class MyKONST
    {
        //public const string NAZEV_PROGRAMU = "Přepisovač 1.8.2b";
        public const string NAZEV_PROGRAMU = "NanoTrans";

        public static MyEnumVerze VERZE = MyEnumVerze.Interni;
                


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetKeyboardState(byte[] lpKeyState);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, byte[] pwszBuff, int cchBuff, uint wFlags);     
        public static  int VratZnak(Key aKey, out char c)
        {
            c=' ';
            try
            {
                
                int VirtualKey = KeyInterop.VirtualKeyFromKey(aKey);
                byte[] State = new byte[256];
                int pstav = GetKeyboardState(State);
                uint uVirtualKey = (uint)VirtualKey;
                uint pScanCode = MapVirtualKey(uVirtualKey, (uint)0x0);
                

                byte[] output = new byte[256];
                int outBufferLength = 256;
                uint flags = 1;
                int pStavUnicode = 0;
                lock (output)
                {
                    pStavUnicode = ToUnicode(uVirtualKey, pScanCode, State, output, outBufferLength, flags);
                }
                if (pStavUnicode > 0)
                {
                    c = (char)output[0];
                    return 1;
                }
                 
                return 0;
            }
            catch
            {
                return -1;
            }
        }


        public static string CESTA_SLOVNIK_SPELLCHECK = "/Data/Vocabulary.txt";
        public static string KONFIGURACNI_SOUBOR = "/Data/config.xml";
        /// <summary>
        /// konstanta k defaultnimu souboru s ruchy - relativni cesta
        /// </summary>
        public const string CESTA_RUCHOVY_SOUBOR = "/Data/ruchy.xml";
        public const string CESTA_MAKRA_SOUBOR = "/Data/makra.xml";

        
        public static int ID_ZOBRAZOVACIHO_BUFFERU_VLNY = 0;
        public static int ID_BUFFERU_PREPISOVANEHO_ELEMENTU = 1;
        public static int ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS = 2;

        public static long DELKA_DOCASNEHO_SOUBORU_ZVUKU_MS = 60000;
        public static string CESTA_DOCASNYCH_SOUBORU_ZVUKU = "/Prevod/Temp/"; 
        public static string CESTA_FFMPEG = "/Prevod/ffmpeg.exe"; 

        
        //prepis pomoci HTK
        /// <summary>
        /// kam ukladat wav soubory a ostatni docasne soubory
        /// </summary>
        public const string CESTA_FONETIKA_TEMP = "/Prevod/Fonetika/Temp/";
        public const string CESTA_FONETIKA = "/Prevod/Fonetika/";
        public const string CESTA_FONETIKA_PERL_SCRIPT = CESTA_FONETIKA + "TextToPhonetic4.pl";

        /// <summary>
        /// cesta kam ukladat docasny wav - jmeno souboru
        /// </summary>
        public const string CESTA_FONETIKA_DOCASNY_WAV = CESTA_FONETIKA_TEMP + "test.wav";
        
        //pouze interni verze
        /// <summary>
        /// cesta ke scriptu pro tta2xml prevod, pouze pro interni verzi
        /// </summary>
        public const string CESTA_TTA2XML = "/Prevod/tta/tta2xml.pl";
        public const string CESTA_TTA2XML_DIR = "/Prevod/tta/";
        public const string CESTA_TTASPLIT_EXE = "/Prevod/tta/ttasplit.exe";
        /// <summary>
        /// 
        /// </summary>
        public const string CESTA_TTASPLIT_TEMP = "/Prevod/tta/Temp/";

        public const string CESTA_KORPUS2XML = "/Prevod//korpus//korpus2xml.pl";
        public const string CESTA_KORPUS2XML_DIR = "/Prevod/korpus/";
        

        public const string CESTA_SLOVNIK_FONETIKA_UZIVATELSKY = CESTA_FONETIKA + "/lexuser.voc";
        public const string CESTA_SLOVNIK_FONETIKA_ZAKLADNI = CESTA_FONETIKA + "/lex.voc";
        
        public static string TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE = "[Výchozí nastavení rozpoznávače]";

        public static long DELKA_POSILANYCH_DAT_PRI_OFFLINE_ROZPOZNAVANI_MS = 2000;
        /// <summary>
        /// jak moc je nahrano dat pri hlasovem ovladani programu... jinak uz neni nic nahravano, dokud neni buffer vyprazdnen
        /// </summary>
        public static long DELKA_MAXIMALNIHO_ZPOZDENI_PRI_ONLINE_ROZPOZNAVANI_MS = 10000;
        /// <summary>
        /// opetovne nahravani
        /// </summary>
        public static long DELKA_MAXIMALNIHO_ZPOZDENI_PRI_ONLINE_ROZPOZNAVANI_DELTA_MS = 2000;

        /// <summary>
        /// jak casto je volan rozpoznavac pro zobrazeni vysledku
        /// </summary>
        public static long PERIODA_TIMERU_ROZPOZNAVACE_MS = 500;
        /// <summary>
        /// jak casto je volano prekreslovani kurzoru
        /// </summary>
        public static int PERIODA_TIMERU_VLNY_MS = 20;

        /// <summary>
        /// rozliseni v pixelech za s pro zobrazeni vlny
        /// </summary>
        public static int ROZLISENI_ZOBRAZENI_VLNY_S = 160;

        /// <summary>
        /// vychozi delka bufferu pro prehravani audio a presne kresleni vlny
        /// </summary>
        public static long DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS = 300000;//180000;
        public static long DELKA_PRVNIHO_RAMCE_ZOBRAZOVACIHO_BUFFERU_MS = 120000;
        //public static long DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS = 3600000;//180000;

        //typy rozpoznavani
        public static short ROZPOZNAVAC_0_OFFLINE_ROZPOZNAVANI = 0;
        public static short ROZPOZNAVAC_1_DIKTAT = 1;
        public static short ROZPOZNAVAC_2_HLASOVE_OVLADANI = 2;

        
        

        
        
        //prevede string na flowdocument beze zmeny struktury
        //public static FlowDocument PrevedTextNaFlowDocument(string aText)
        public static string PrevedTextNaFlowDocument(string aText)
        {
            /*if (aText == null) return null;
            return new FlowDocument(new Paragraph(new Run(aText)));*/
            return aText;
        }


        //vraci index od kterym se novy string lisi od stareho
        [Obsolete]
        public static int VratIndexZmenyStringu(string aOriginal, string aUpraveny, int aAktualniIndexKurzoru)
        {
            if (aOriginal == null || aUpraveny == null) return -1;
            if (aOriginal.Length > 0 && aUpraveny.Length == 0) return 0;

            for (int i = 0; i < aOriginal.Length; i++)
            {
                if (i >= aUpraveny.Length) return i;    //string je stejny,ale novy je kratsi
                if ((aOriginal[i] != aUpraveny[i])||(aAktualniIndexKurzoru<i))
                {
                    if (aAktualniIndexKurzoru<i && i != aAktualniIndexKurzoru)
                    {
                        //return aAktualniIndexKurzoru;
                    }
                    return i; //stringy se lisi v nejake pozici
                }
            }
            if (aOriginal.Length < aUpraveny.Length) return aOriginal.Length;   //novy string je delsi a lisi se v nove casti


            return -1;
        }


        public static void ZobrazToolTip(string aText, System.Windows.Point aBod, int aZpozdeniMS)
        {

            System.Windows.Controls.ToolTip toolTip = new System.Windows.Controls.ToolTip();

            toolTip.Content = aText;
            toolTip.PlacementRectangle = new System.Windows.Rect(aBod, new System.Windows.Size(50, 20));

            //toolTip.PlacementTarget = (Button)sender;

            toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;

            //((Button)sender).ToolTip = toolTip;

            toolTip.IsOpen = true;

            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, aZpozdeniMS), IsEnabled = true };

            timer.Tick += new EventHandler(delegate(object timerSender, EventArgs timerArgs)
            {

                if (toolTip != null)
                {

                    toolTip.IsOpen = false;

                }

                toolTip = null;

                timer = null;

            });

        }


        /// <summary>
        /// overi zda dany text je mozny zapsat do textboxu
        /// </summary>
        /// <param name="aTb"></param>
        /// <param name="aPuvodniText"></param>
        /// <param name="e"></param>
        /// <param name="aTypElementu"></param>
        [Obsolete]
        public static void OverZmenyTextBoxu(System.Windows.Controls.TextBox aTb, string aPuvodniText, ref System.Windows.Controls.TextChangedEventArgs e, MyEnumTypElementu aTypElementu)
        {
            try
            {
                if (aTypElementu != MyEnumTypElementu.foneticky) return;

                int pIndex = MyKONST.VratIndexZmenyStringu(aPuvodniText, aTb.Text, aTb.CaretIndex);
                if (pIndex >= 0 && aTb.Text.Length >= aPuvodniText.Length)
                {
                    char c = aTb.Text[pIndex];
                    bool pNalezeno = false;
                    for (int i = 0; i < MyFonetic.ABECEDA_FONETICKA.Length; i++)
                    {

                        if (MyFonetic.ABECEDA_FONETICKA[i] == c)
                        {
                            pNalezeno = true;
                            break;
                        }
                    }
                    if (!pNalezeno)
                    {
                        e.Handled = true;
                        string pNovyText = aTb.Text;
                        pNovyText = pNovyText.Remove(pIndex, 1);
                        if (c == ' ' || c == '_')
                        {
                            pNovyText = pNovyText.Insert(pIndex, "_");
                            aTb.Text = pNovyText;
                            aTb.SelectionStart = pIndex + 1;
                        }
                        else
                        {
                            aTb.Text = pNovyText;
                            aTb.SelectionStart = pIndex;
                            System.Windows.Point ppp = new System.Windows.Point(aTb.GetRectFromCharacterIndex(pIndex).Left, aTb.GetRectFromCharacterIndex(pIndex).Top);
                            ppp = aTb.PointToScreen(ppp);
                            ZobrazToolTip("Jsou povoleny pouze symboly fonetické abecedy", ppp, 600);
                            System.Media.SystemSounds.Beep.Play();
                        }

                    }
                }
            }
            catch
            {

            }

        }

        /// <summary>
        /// prevede bitmap frame do base64 stringu
        /// </summary>
        /// <param name="aCestaTMP"></param>
        /// <param name="aBMP"></param>
        /// <returns></returns>
        public static string PrevedJPGnaBase64String(BitmapFrame aBMP)
        {
            try
            {
                string pBase64String = null;
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                //BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(aBMP);
                MemoryStream ms = new MemoryStream();
                
                //Convert.ToBase64String
                encoder.Save(ms);
                byte[] pPole = new byte[ms.Length];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(pPole, 0, pPole.Length);
                pBase64String = Convert.ToBase64String(pPole);
                ms.Close();
                return pBase64String;
            }
            catch (Exception)
            {

                return null;
            }

        }

        /// <summary>
        /// prevede base64string obrazku na bitmapImage
        /// </summary>
        /// <param name="aStringBase64"></param>
        /// <returns></returns>
        public static BitmapImage PrevedBase64StringNaJPG(string aStringBase64)
        {
            if (aStringBase64 == null || aStringBase64 == "") return null;
            BitmapImage bi;
            try
            {
                byte[] binaryData = Convert.FromBase64String(aStringBase64);
                bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();
                return bi;
            }
            catch (Exception)
            {

                return null;
            }
        }

        /// <summary>
        /// audio data prevede na pole,ktere je mozno vykreslit
        /// </summary>
        /// <param name="aData"></param>
        /// <param name="aPocetPixelu"></param>
        /// <returns></returns>
        public static float[] VytvorDataProVykresleniVlny(short[] aData, int aPocetPixeluNaSekundu)
        {
            try
            {

                int pPocetZobrazovanychPixelu = (int)((double)aPocetPixeluNaSekundu * (double)aData.Length / 16000);
                if (pPocetZobrazovanychPixelu <= 0) pPocetZobrazovanychPixelu = 1300;

                int pKolikVzorkuKomprimovat = (int)((double)(aData.Length) / (double)pPocetZobrazovanychPixelu);
                float[] pPoleVykresleni = new float[((aData.Length) / pKolikVzorkuKomprimovat) * 2];

                float pMezivypocetK = 0;
                int pocetK = 0;
                float pMezivypocetZ = 0;
                int pocetZ = 0;
                int j = 1;
                int Xsouradnice = 0;
                for (long i = 0; i < aData.Length; i++)
                {
                    if (aData[i] > 0)
                    {
                        pMezivypocetK += aData[i];
                        pocetK++;
                    }
                    else
                    {
                        pMezivypocetZ += aData[i];
                        pocetZ++;
                    }
                    if (j > pKolikVzorkuKomprimovat)
                    {
                        if (pocetK > 0)
                        {
                            pMezivypocetK = pMezivypocetK / pocetK;
                        }
                        else
                        {
                            //pMezivypocetK = pPoleVykresleni[Xsouradnice - 2];
                            pMezivypocetK = 0;
                        }
                        pPoleVykresleni[Xsouradnice] = pMezivypocetK;
                        if (pocetZ > 0)
                        {
                            pMezivypocetZ = pMezivypocetZ / pocetZ;

                        }
                        else
                        {
                            //pMezivypocetZ = pPoleVykresleni[Xsouradnice - 1];
                            pMezivypocetZ = 0;
                        }
                        pPoleVykresleni[Xsouradnice + 1] = pMezivypocetZ;

                        pMezivypocetK = 0;
                        pMezivypocetZ = 0;
                        pocetK = 0;
                        pocetZ = 0;

                        Xsouradnice += 2;
                        j = 0;
                    }

                    j++;
                }
                return pPoleVykresleni;
            }
            catch
            {
                return null;
            }
        }

    }
}
