using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanoTrans;
using System.IO;
namespace TrsxV1Plugin
{
    public class TrsxV1
    {
        public static MySubtitlesData Import(Stream input)
        {
            return MySubtitlesData.Deserialize(input);
        }

        public static bool Export(MySubtitlesData data, Stream output)
        {
            return data.SerializeV1(output, data, false);
        }

    }
}
