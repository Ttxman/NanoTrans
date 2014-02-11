﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanoTrans;
using System.IO;
using NanoTrans.Core;
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
