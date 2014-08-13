using NanoTrans.Core;
using NanoTrans.OnlineAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NanoTrans
{
    /// <summary>
    /// Wrap NanoTrans.Core.Transcription to collection with INotifyCollectionChanged
    /// </summary>
    public class WPFTranscription : Transcription, INotifyCollectionChanged, INotifyPropertyChanged
    {
        new public static WPFTranscription Deserialize(string path)
        {
            var t = new WPFTranscription();
            Transcription.Deserialize(path, t);
            t.IsOnline = t.Elements.ContainsKey("Online") && t.Elements["Online"] == "True";
            return t;
        }

        new public static WPFTranscription Deserialize(Stream stream)
        {
            var t = new WPFTranscription();
            Transcription.Deserialize(stream, t);
            return t;
        }
        public WPFTranscription(string filename)
            : base(filename)
        {

        }

        public WPFTranscription()
            : base()
        {

        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        Stack<ChangeAction> _UndoStack = new Stack<ChangeAction>();

        public override void OnContentChanged(params ChangeAction[] actions)
        {
            foreach (var a in actions)
                _UndoStack.Push(a);
            
            actions = actions.Where(a=>a.ChangeType != ChangeType.Modify && a.ChangedElement.GetType() != typeof(TranscriptionPhrase)).ToArray();
            if (CollectionChanged != null && actions.Length >0)
            {
                if (actions.Length > 1)
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null, 0));
                else
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(MapEvent(actions[0].ChangeType), actions[0].ChangedElement, actions[0].ChangeAbsoluteIndex));
            }

            Saved = false;
        }

        public NotifyCollectionChangedAction MapEvent(ChangeType action)
        {
            switch (action)
            {
                case ChangeType.Add:
                    return NotifyCollectionChangedAction.Add;
                case ChangeType.Remove:
                    return NotifyCollectionChangedAction.Remove;
                case ChangeType.Replace:
                    return NotifyCollectionChangedAction.Replace;
            }

            return NotifyCollectionChangedAction.Reset;
        }

        bool _isonline = false;
        public bool IsOnline
        {
            get { return _isonline; }
            set
            {
                _isonline = value;
                if (value)
                {
                    this.Elements["Online"] = "True";
                }
                else
                {
                    this.Elements.Remove("Online");
                }

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsOnline"));
                }
            }
        }

        public OnlineTranscriptionInfo OnlineInfo
        {
            get
            {
                if (!IsOnline)
                    return null;

                return JObject.Parse(Meta.Element("OnlineInfo").Value).ToObject<OnlineTranscriptionInfo>();
            }
            set
            {
                if (value == null)
                {
                    var elm = Meta.Element("OnlineInfo");
                    if (elm != null)
                        elm.Remove();
                }
                Meta.SetElementValue("OnlineInfo", JObject.FromObject(value).ToString());
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public OnlineAPI.SpeakersApi Api { get; set; }
    }
}
