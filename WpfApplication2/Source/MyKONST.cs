using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.IO;

namespace NanoTrans
{
    public static class Const
    {
        public const string APP_NAME = "NanoTrans";
        
        public static int ID_BUFFER_WAVEFORMVISIBLE = 0;
        public static int ID_BUFFER_TRANSCRBED_ELEMENT = 1;
        public static int ID_BUFFER_TRANSCRIBED_ELEMENT_PHONETIC = 2;

        public static long TEMPORARY_AUDIO_FILE_LENGTH_MS = 60000;

        public static int WAVEFORM_CARET_REFRESH_MS = 20;


        public static long DISPLAY_BUFFER_LENGTH_MS = 300000;//180000;
        public static TimeSpan DISPLAY_BUFFER_LENGTH = TimeSpan.FromMilliseconds(DISPLAY_BUFFER_LENGTH_MS);
        public static long DELKA_PRVNIHO_RAMCE_ZOBRAZOVACIHO_BUFFERU_MS = 120000;


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

        public static BitmapImage Base64ToJpg(string aStringBase64)
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
