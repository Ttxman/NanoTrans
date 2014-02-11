using NanoTrans.Core;
using System.IO;
namespace TrsxV1Plugin
{
    public class TrsxV1
    {
        public static Transcription Import(Stream input)
        {
            return Transcription.Deserialize(input);
        }

        public static bool Export(Transcription data, Stream output)
        {
            return data.SerializeV1(output, data, false);
        }

    }
}
