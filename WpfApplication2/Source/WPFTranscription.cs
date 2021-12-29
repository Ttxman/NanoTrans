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
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Schema;
using TranscriptionCore;

namespace NanoTrans
{
    /// <summary>
    /// Wrap NanoTrans.Core.Transcription to collection with INotifyCollectionChanged
    /// </summary>
    public class WPFTranscription : Transcription, INotifyCollectionChanged, INotifyPropertyChanged
    {
        new public static WPFTranscription Deserialize(string path)
        {
            WPFTranscription t;
            using (var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                t = WPFTranscription.Deserialize(s);
            t.FileName = path;
            return t;
        }

        new public static WPFTranscription Deserialize(Stream stream)
        {
            //on some remote data storage this can be more effective than lot of small direct reads by XMLTextReader
            #region copy stream to internal buffer

            byte[] bfr = new byte[32 * 1024];
            int read = 0;

            int initsize = 500 * 1024;
            MemoryStream bufferStream;
            try
            {
                initsize = (int)stream.Length;
            }
            finally
            {
                bufferStream = new MemoryStream(initsize);
            }

            while ((read = stream.Read(bfr, 0, bfr.Length)) > 0)
                bufferStream.Write(bfr, 0, read);

            bufferStream.Seek(0, SeekOrigin.Begin);

            #endregion

#if DEBUG
            #region validation

            XmlSchemaSet schemas = new XmlSchemaSet();
            XNamespace xNamespace = XNamespace.Get("http://www.ite.tul.cz/TRSXSchema3.xsd");
            using (var s = File.Open(FilePaths.TrsxSchemaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                schemas.Add(null, XmlReader.Create(s));

            XDocument doc = XDocument.Load(bufferStream);

            foreach (XElement xElement in doc.Descendants())
            {
                // First make sure that the xmlns-attribute is changed
                xElement.SetAttributeValue("xmlns", xNamespace.NamespaceName);
                // Then also prefix the name of the element with the namespace
                xElement.Name = xNamespace + xElement.Name.LocalName;
            }


            doc.Validate(schemas, (o, e) =>
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0}", e.Message));
            });

            //restore stream position
            bufferStream.Seek(0, SeekOrigin.Begin);
            #endregion
#endif

            var t = new WPFTranscription();
            t.BeginUpdate();
            Transcription.Deserialize(bufferStream, t);
            t.IsOnline = t.Elements.ContainsKey("Online") && t.Elements["Online"] == "True";
            t.EndUpdate();
            t.ClearUndo();
            t.Saved = true;
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

            if (actions is null || actions.Length <= 0)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null, -1));
                return true;
            }

            if (!_undoing)
            {
                _UndoStack.Add(actions);
                if (!_redoing)
                    _RedoStack.Clear();
            }
            else
                _RedoStack.Add(actions);

            actions = actions.Where(a => a.ChangeType != ChangeType.Modify && a.ChangedElement.GetType() != typeof(TranscriptionPhrase)).ToArray();
            if (CollectionChanged is { } && actions.Length > 0)
            {
                var ev = MapEvent(actions[0].ChangeType);
                if (actions.Length > 5 || ev == NotifyCollectionChangedAction.Reset)
                {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null, -1));
                }
                else
                {
                    foreach (var a in actions)
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(MapEvent(a.ChangeType), a.ChangedElement, a.ChangeAbsoluteIndex));
                }
            }

            return true;
        }

        readonly ObservableCollection<ChangeAction[]> _UndoStack = new ObservableCollection<ChangeAction[]>();

        public ObservableCollection<ChangeAction[]> UndoStack
        {
            get { return _UndoStack; }
        }

        readonly ObservableCollection<ChangeAction[]> _RedoStack = new ObservableCollection<ChangeAction[]>();

        public ObservableCollection<ChangeAction[]> RedoStack
        {
            get { return _RedoStack; }
        }
        bool _undoing = false;
        public void Undo()
        {
            if (_UndoStack.Count > 0)
            {
                _undoing = true;
                BeginUpdate();
                var act = _UndoStack[_UndoStack.Count - 1];
                _UndoStack.RemoveAt(_UndoStack.Count - 1);
                for (int i = act.Length - 1; i >= 0; i--)
                {
                    act[i].Revert(this);
                }

                EndUpdate();
                _undoing = false;
            }
        }

        bool _redoing = false;
        public void Redo()
        {
            if (_RedoStack.Count > 0)
            {
                _redoing = true;
                BeginUpdate();

                var act = _RedoStack[_RedoStack.Count - 1];
                _RedoStack.RemoveAt(_RedoStack.Count - 1);
                for (int i = act.Length - 1; i >= 0; i--)
                {
                    act[i].Revert(this);
                }

                EndUpdate();
                _redoing = false;
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
                    //case ChangeType.Replace: //notify does not support replace
                    //    return NotifyCollectionChangedAction.Replace;
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

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsOnline"));
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
                if (value is null)
                {
                    var elm = Meta.Element("OnlineInfo");
                    elm?.Remove();
                }
                Meta.SetElementValue("OnlineInfo", JObject.FromObject(value).ToString());
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public OnlineAPI.SpeakersApi Api { get; set; }


    }
}
