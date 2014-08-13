using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NanoTrans.Core
{

    /// <summary>
    /// Base class for structure change events and Undo
    /// </summary>
    public abstract class ChangedAction
    {
        private ChangeType _changeType;
        public ChangeType ChangeType
        {
            get { return _changeType; }
        }

        private TranscriptionElement _changedElement;

        public TranscriptionElement ChangedElement
        {
            get { return _changedElement; }
        }

        private TranscriptionIndex _changeTranscriptionIndex;

        public TranscriptionIndex ChangeTranscriptionIndex
        {
            get { return _changeTranscriptionIndex; }
            set { _changeTranscriptionIndex = value; }
        }

        public ChangedAction(ChangeType changeType, TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
        {
            _changeType = changeType;
            _changedElement = changedElement;
            _changeTranscriptionIndex = changeIndex;
            _changeAbsoluteIndex = changeAbsoluteIndex;
        }

        public abstract void Revert(Transcription trans);
        int _changeAbsoluteIndex = -1;
        public int ChangeAbsoluteIndex { get { return _changeAbsoluteIndex; } }
    }


    public class InsertAction:ChangedAction
    {

        public InsertAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
            : base(ChangeType.Add, changedElement, changeIndex, changeAbsoluteIndex)
        {
        
        }

        public override void Revert(Transcription trans)
        {
            trans.RemoveAt(ChangeTranscriptionIndex);
        }
    }


    public class RemoveAction : ChangedAction
    {

        public RemoveAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
            : base(ChangeType.Remove, changedElement, changeIndex, changeAbsoluteIndex)
        {

        }

        public override void Revert(Transcription trans)
        {
            trans.Insert(ChangeTranscriptionIndex, ChangedElement);
        }
    }


    public class ReplaceAction : ChangedAction
    {

        public ReplaceAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
            : base(ChangeType.Replace, changedElement, changeIndex, changeAbsoluteIndex)
        {

        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex] = ChangedElement;
        }
    }


    public class ParagraphSpeakerAction : ChangedAction
    {
        Speaker _oldSpeaker;

        public Speaker OldSpeaker
        {
            get { return _oldSpeaker; }
        }

        public ParagraphSpeakerAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex,int changeAbsoluteIndex, Speaker oldSpeaker)
            : base(ChangeType.Replace, changedParagraph, changeIndex, changeAbsoluteIndex)
        {
            _oldSpeaker = oldSpeaker;
        }

        public override void Revert(Transcription trans)
        {
            ((TranscriptionParagraph)trans[ChangeTranscriptionIndex]).Speaker = OldSpeaker;
        }
    }


    public class ParagraphAttibutesAction : ChangedAction
    {
        public ParagraphAttibutesAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex,int changeAbsoluteIndex, ParagraphAttributes oldAttributes)
            : base(ChangeType.Replace, changedParagraph, changeIndex, changeAbsoluteIndex)
        {
            _oldAttributes = oldAttributes;
        }

        public override void Revert(Transcription trans)
        {
            ((TranscriptionParagraph)trans[ChangeTranscriptionIndex]).DataAttributes = OldAttributes;
        }

        ParagraphAttributes _oldAttributes;

        public ParagraphAttributes OldAttributes
        {
            get { return _oldAttributes; }
        }
    }


    public class BeginAction : ChangedAction
    {
        public BeginAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, TimeSpan oldtime)
            : base(ChangeType.Replace, changedElement, changeIndex,changeAbsoluteIndex)
        {
            _oldtime = oldtime;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].Begin = Oldtime;
        }

        TimeSpan _oldtime;

        public TimeSpan Oldtime
        {
            get { return _oldtime; }
        }
    }


    public class EndAction : ChangedAction
    {
        public EndAction(TranscriptionElement changedelement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, TimeSpan oldtime)
            : base(ChangeType.Replace, changedelement, changeIndex, changeAbsoluteIndex)
        {
            _oldtime = oldtime;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].End = Oldtime;
        }

        TimeSpan _oldtime;

        public TimeSpan Oldtime
        {
            get { return _oldtime; }
        }
    }



    /// <summary>
    /// Used for compatibility with collection changes in wpf
    /// </summary>
    public enum ChangeType: uint
    {
        Add,
        Remove,
        Replace,
        Modify,
    }
}
