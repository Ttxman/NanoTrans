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

        public ApiSynchronizedSpeaker(string firstname, string surname,Sexes sex )
            : base(firstname,surname,sex,null)
        {
        }

        public ApiSynchronizedSpeaker(Speaker copy):base(copy.Serialize())
        { 
        }
        bool _saved = true;

        public bool IsSaved
        {
            get { return _saved; }
            set { _saved = value; }
        }
        
    }
}
