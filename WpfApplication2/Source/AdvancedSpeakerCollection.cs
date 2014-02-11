using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NanoTrans
{
    class AdvancedSpeakerCollection : SpeakerCollection
    {
        public override XElement Serialize()
        {
            var x = base.Serialize();
            return x;
        }

        Dictionary<string, Speaker> _slist;
        protected override void Initialize(XDocument doc)
        {
            _slist = _Speakers.ToDictionary(s => s.DBID);
        }

        /// <summary>
        /// Find speaker from user (or online) database to speaker from file.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>speaker or null when user (or online) database not contain Speaker that can be automatically synchronized</returns>
        internal Speaker SynchronizeSpeaker(Speaker s)
        {
            if (s.DataBase == DBType.Api)
                throw new NotImplementedException();//not yet implemented
            else if (s.DataBase == DBType.User && s.DBID!=null)//some user created it manually
            {
                Speaker ls;
                if(_slist.TryGetValue(s.DBID, out ls))
                {
                    return ls;
                }
            }
            else if (s.DataBase == DBType.File)//old format or export from some tool
                return null;


            return null;
        }
    }
}
