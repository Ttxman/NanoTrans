using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranscriptionCore;

namespace FakeTrsxPlugin
{
    public class FakeTrsxPlugin
    {
        public static bool Import(Stream input, Transcription storage)
        {
            if (input is FileStream)
            { 
                storage.Add(new TranscriptionParagraph());
                storage.MediaURI = ((FileStream)input).Name;
                return true;
            }
            return false;
        }
    }
}
