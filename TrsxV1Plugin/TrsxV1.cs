using NanoTrans.Core;
using System.IO;
namespace TrsxV1Plugin
{
    public class TrsxV1
    {
        public static bool Import(Stream input,Transcription storage)
        {
            Transcription.Deserialize(input,storage);
            return true;
        }

        public static bool Export(Transcription data, Stream output)
        {
            bool exc = true;
            try
            {
                Transcription.SerializeV1(output, data, false);
            }
            catch
            {
                exc = false;
            }

            return exc;
        }

    }
}
