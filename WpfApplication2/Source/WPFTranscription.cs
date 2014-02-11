using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoTrans
{
    /// <summary>
    /// Wrap NanoTrans.Core.Transcription to collection with INotifyCollectionChanged
    /// </summary>
    public class WPFTranscription:Transcription, INotifyCollectionChanged
    {
        new public static WPFTranscription Deserialize(string path)
        {
            var t = new WPFTranscription();
            Transcription.Deserialize(path, t);
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

        public WPFTranscription():base()
        { 
        
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public override void ElementChanged(TranscriptionElement element)
        {

        }

        public override void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, oldelement, newelement));
        }

        public override void ElementInserted(TranscriptionElement element, int absoluteindex)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, element, absoluteindex));
        }

        public override void ElementRemoved(TranscriptionElement element, int absoluteindex)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, element, absoluteindex));
        }

        public NotifyCollectionChangedAction MapEvent(ChangedAction action)
        {
            switch (action)
            {
                case ChangedAction.Add:
                    return NotifyCollectionChangedAction.Add;
                case ChangedAction.Move:
                    return NotifyCollectionChangedAction.Move;
                case ChangedAction.Remove:
                    return NotifyCollectionChangedAction.Remove;
                case ChangedAction.Replace:
                    return NotifyCollectionChangedAction.Replace;
                case ChangedAction.Reset:
                    return NotifyCollectionChangedAction.Reset;
            }

            return NotifyCollectionChangedAction.Reset;
        }

    }
}
