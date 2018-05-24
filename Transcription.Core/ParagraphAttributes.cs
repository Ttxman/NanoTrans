using System;

namespace TranscriptionCore
{
    [Flags]
    public enum ParagraphAttributes : int
    {
        None = 0x0,
        Narrowband = 0x1,
        Background_speech = 0x2,
        Background_noise = 0x4,
        Junk = 0x8,
    }
}