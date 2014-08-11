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
        public Uri LoginURL { get; set; }
        public Uri LogoutURL { get; set; }
        public Uri RetranscribeURL { get; set; }
        public Uri Site { get; set; }
        public Uri SpeakerAPI_URL { get; set; }
        public Uri TrsxDownloadURL { get; set; }
        public Uri TrsxUploadURL { get; set; }
        public Uri OriginalURL { get; set; }
    }
}
