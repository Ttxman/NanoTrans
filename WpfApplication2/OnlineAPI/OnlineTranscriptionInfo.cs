using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans.OnlineAPI
{
    public class OnlineTranscriptionInfo
    {
        public string DocumentId { get; set; }
        public Uri Site { get; set; }
        public Uri SpeakersAPI { get; set; }
        public Uri TrsxDownloadURL { get; set; }
        public Uri ResponseURL { get; set; }
        public Uri LoginURL { get; set; }
    }
}
