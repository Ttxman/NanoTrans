using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NanoTrans.OnlineAPI
{
    class ApiException : Exception
    {
        public ApiException(string message)
            : base(message)
        { }
    }
}
