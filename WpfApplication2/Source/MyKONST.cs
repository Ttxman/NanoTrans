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
                
        
        public static int ID_ZOBRAZOVACIHO_BUFFERU_VLNY = 0;
        public static int ID_BUFFERU_PREPISOVANEHO_ELEMENTU = 1;
        public static int ID_BUFFERU_PREPISOVANEHO_ELEMENTU_FONETICKY_PREPIS = 2;

        public static long DELKA_DOCASNEHO_SOUBORU_ZVUKU_MS = 60000;

        public static string TEXT_VYCHOZI_NASTAVENI_ROZPOZNAVACE = "[Výchozí nastavení rozpoznávače]";

        /// <summary>
        /// jak casto je volano prekreslovani kurzoru
        /// </summary>
        public static int PERIODA_TIMERU_VLNY_MS = 20;

        /// <summary>
        /// vychozi delka bufferu pro prehravani audio a presne kresleni vlny
        /// </summary>
        public static long DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS = 300000;//180000;
        public static TimeSpan DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU = TimeSpan.FromMilliseconds(DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS);
        public static long DELKA_PRVNIHO_RAMCE_ZOBRAZOVACIHO_BUFFERU_MS = 120000;
        //public static long DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS = 3600000;//180000;

        //typy rozpoznavani
        public static short ROZPOZNAVAC_0_OFFLINE_ROZPOZNAVANI = 0;
        public static short ROZPOZNAVAC_1_DIKTAT = 1;
        public static short ROZPOZNAVAC_2_HLASOVE_OVLADANI = 2;



        /// <summary>
        /// prevede bitmap frame do base64 stringu
        /// </summary>
        /// <param name="aCestaTMP"></param>
        /// <param name="aBMP"></param>
        /// <returns></returns>
        public static string JpgToBase64(BitmapFrame aBMP)
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
    }
}
