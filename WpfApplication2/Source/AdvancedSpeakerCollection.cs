using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NanoTrans
{
    public class AdvancedSpeakerCollection : SpeakerCollection
    {
        public AdvancedSpeakerCollection() 
        {
 
        }
        public AdvancedSpeakerCollection(string filename)
            : base(filename)
        { 
        
        }
        Dictionary<string, Speaker> _slist = new Dictionary<string,Speaker>();
        protected override void Initialize(XDocument doc)
        {
            _slist.Clear();
            _slist = _Speakers.ToDictionary(s => s.DBID);

            var merges = _Speakers.SelectMany(s => s.Merges.Select(m=>new { speaker = s, merged = m }));
            foreach (var m in merges)
            { 
                if(m.merged.DBtype!=DBType.File)
                    if(!_slist.ContainsKey(m.merged.DBID))
                        _slist[m.merged.DBID] = m.speaker;

            }



            base.Initialize(doc);
        }

        /// <summary>
        /// Find speaker from user (or online) database to speaker from file.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>speaker or null when user (or online) database not contain Speaker that can be automatically synchronized</returns>
        public Speaker SynchronizeSpeaker(Speaker s)
        {
            if (s.DataBaseType == DBType.Api)
            {
                var ss = new ApiSynchronizedSpeaker(s);
                return ss;
            }
            else if (s.DataBaseType == DBType.User && s.DBID!=null)//some user created it manually
            {
                Speaker ls;
                if(_slist.TryGetValue(s.DBID, out ls))
                {
                    return ls;
                }
            }
            else if (s.DataBaseType == DBType.File)//old format or export from some tool
                return null;


            return null;
        }


        public override void Add(Speaker item)
        {
            _slist.Add(item.DBID, item);
            base.Add(item);
        }

        public override void Clear()
        {
            _slist.Clear();
            base.Clear();
        }

        public override void Insert(int index, Speaker item)
        {
            _slist.Add(item.DBID, item);
            base.Insert(index, item);
        }

        public override void RemoveAt(int index)
        {
            _slist.Remove(base[index].DBID);
            base.RemoveAt(index);
        }

        public override bool Remove(Speaker item)
        {
            _slist.Remove(item.DBID);
            return base.Remove(item);
        }

        public override void AddRange(IEnumerable<Speaker> enumerable)
        {
            foreach (var itm in enumerable)
            {
                _slist.Add(itm.DBID, itm);    
            }

            base.AddRange(enumerable);
        }
    }
}
