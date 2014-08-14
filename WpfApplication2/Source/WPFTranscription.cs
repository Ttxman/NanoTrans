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
            t.ClearUndo();

            return t;
        }

        new public static WPFTranscription Deserialize(Stream stream)
        {
            var t = new WPFTranscription();
            Transcription.Deserialize(stream, t);
            t.IsOnline = t.Elements.ContainsKey("Online") && t.Elements["Online"] == "True";
            t.ClearUndo();
            return t;
        }
        public WPFTranscription(string filename)
            : base(filename)
        {
            ClearUndo();
        }

        public WPFTranscription()
            : base()
        {

        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;



        public override bool OnContentChanged(params ChangeAction[] actions)
        {
            if (!base.OnContentChanged(actions))
                return false;

            Saved = false;

            if (actions == null || actions.Length < 0)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null, 0));
                return true;
            }

            if (!_undoing)
            {
                _UndoStack.Push(actions);
                _RedoStack.Clear();
            }
            else
                _RedoStack.Push(actions);

            actions = actions.Where(a => a.ChangeType != ChangeType.Modify && a.ChangedElement.GetType() != typeof(TranscriptionPhrase)).ToArray();
            if (CollectionChanged != null && actions.Length > 0)
            {
                if (actions.Length > 1)
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null, 0));
                else
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(MapEvent(actions[0].ChangeType), actions[0].ChangedElement, actions[0].ChangeAbsoluteIndex));
            }

            return true;
        }

        Stack<ChangeAction[]> _UndoStack = new Stack<ChangeAction[]>();
        Stack<ChangeAction[]> _RedoStack = new Stack<ChangeAction[]>();
        bool _undoing = false;
        public void Undo()
        {
            if (_UndoStack.Count > 0)
            {
                _undoing = true;
                BeginUpdate();
                var act = _UndoStack.Pop();
                for (int i = act.Length-1; i >=0; i--)
                {
                    act[i].Revert(this);
                }

                EndUpdate();
                _undoing = false;
            }
        }

        public void Redo()
        {
            if (_RedoStack.Count > 0)
            {
                BeginUpdate();
                var act = _RedoStack.Pop();
                for (int i = act.Length - 1; i >= 0; i--)
                {
                    act[i].Revert(this);
                }

                EndUpdate();
            }
        }


        public void ClearUndo()
        {
            _UndoStack.Clear();
            _RedoStack.Clear();
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
