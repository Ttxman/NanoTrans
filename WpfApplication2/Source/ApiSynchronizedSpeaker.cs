using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans
{
    public class ApiSynchronizedSpeaker:Speaker
    {
        public string MainID { get; set; }
        public ApiSynchronizedSpeaker()
        { }
        public ApiSynchronizedSpeaker(Speaker copy):base(copy.Serialize())
        { 
        }

    }
}
