using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{
    public class TranscriptionSerializationException:Exception
    {
        public TranscriptionSerializationException(string message)
            : base(message)
        { }

            public TranscriptionSerializationException(string message, Exception inner)
            : base(message,inner)
        { }
    }
}
