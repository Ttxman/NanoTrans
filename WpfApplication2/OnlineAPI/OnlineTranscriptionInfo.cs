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

        private Uri _TrsxDownloadURL;
        public Uri TrsxDownloadURL 
        { 
            get
            {
                if (_TrsxDownloadURL == null)
                    return null;

                if (!_TrsxDownloadURL.IsAbsoluteUri)
                {
                    return new Uri(OriginalURL, _TrsxDownloadURL);
                }
                else
                    return _TrsxDownloadURL;
            }
            set
            {
                _TrsxDownloadURL = value;
            }
        }
        private Uri _TrsxUploadURL;
        public Uri TrsxUploadURL 
        {
            get
            {
                if (_TrsxUploadURL == null)
                    return null;

                if (!_TrsxUploadURL.IsAbsoluteUri)
                {
                    return new Uri(OriginalURL, _TrsxUploadURL);
                }
                else
                    return _TrsxUploadURL;
            }
            set
            {
                _TrsxUploadURL = value;
            }
        }
        public Uri OriginalURL { get; set; }

        public bool API2 { get; set; }
    }
}
