using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NanoTrans.Core
{
    public class NanoTransSerializationException:Exception
    {
        public NanoTransSerializationException(string message)
            : base(message)
        { }

            public NanoTransSerializationException(string message, Exception inner)
            : base(message,inner)
        { }
    }
}
