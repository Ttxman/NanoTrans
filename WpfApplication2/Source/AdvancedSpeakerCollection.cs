using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TranscriptionCore;

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
        Dictionary<string, Speaker> _slist = new Dictionary<string, Speaker>();
        protected override void Initialize(XDocument doc)
        {
            _slist.Clear();

            _slist = _Speakers.ToDictionary(s => s.DBID);

            var merges = _Speakers.SelectMany(s => s.Merges.Select(m => new { speaker = s, merged = m }));
            foreach (var m in merges)
            {
                if (m.merged.DBtype != DBType.File)
                    if (!_slist.ContainsKey(m.merged.DBID))
                        _slist[m.merged.DBID] = m.speaker;

            }

            base.Initialize(doc);
        }


        public override XElement Serialize(bool saveAll = false)
        {
            return base.Serialize(true);
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
            if (item.DBType == DBType.File || item.DBID is null)
                return false;

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
                if (_slist.ContainsKey(s.DBID))
                {
                    if (_slist[s.DBID].Synchronized < s.Synchronized)
                        _slist[s.DBID] = s;
                }
                else
                    _slist[s.DBID] = s;

                return ss;
            }
            else if (s.DataBaseType == DBType.User && s.DBID is { })//some user created it manually
            {
                if (_slist.TryGetValue(s.DBID, out Speaker ls))
                {

                    if (_slist[s.DBID].Synchronized < s.Synchronized)
                    {
                        this._Speakers.Remove(_slist[s.DBID]);
                        ls = _slist[s.DBID] = s;
                        this._Speakers.Add(ls);
                    }

                    return ls;
                }
                else
                    _slist[s.DBID] = s;
            }

            return null;//old format or export from some tool do not synchronize automatically
        }


        public static Speaker SynchronizedAdd(SpeakerCollection speakers, Speaker s)
        {

            var found = speakers.FirstOrDefault(sp => sp.DBID == s.DBID);

            if (found is null)
            {
                speakers.Add(s);

            }
            else
            {
                if (found.Synchronized < s.Synchronized)
                {
                    speakers.Remove(found);
                    speakers.Add(s);
                    return found;
                }
            }

            return null;
        }

        public void SynchronizeAdd(Speaker speaker)
        {
            SynchronizedAdd(this, speaker);
        }

    }
}
