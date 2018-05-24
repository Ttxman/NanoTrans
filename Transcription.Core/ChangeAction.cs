using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{

    /// <summary>
    /// Base class for structure change events and Undo
    /// </summary>
    public abstract class ChangeAction
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

        public ChangeAction(ChangeType changeType, TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
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


    public class InsertAction:ChangeAction
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


    public class RemoveAction : ChangeAction
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


    public class ReplaceAction : ChangeAction
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


    public class ParagraphSpeakerAction : ChangeAction
    {
        Speaker _oldSpeaker;

        public Speaker OldSpeaker
        {
            get { return _oldSpeaker; }
        }

        public ParagraphSpeakerAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex,int changeAbsoluteIndex, Speaker oldSpeaker)
            : base(ChangeType.Modify, changedParagraph, changeIndex, changeAbsoluteIndex)
        {
            _oldSpeaker = oldSpeaker;
        }

        public override void Revert(Transcription trans)
        {
            ((TranscriptionParagraph)trans[ChangeTranscriptionIndex]).Speaker = OldSpeaker;
        }
    }


    public class ParagraphAttibutesAction : ChangeAction
    {
        public ParagraphAttibutesAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex,int changeAbsoluteIndex, ParagraphAttributes oldAttributes)
            : base(ChangeType.Modify, changedParagraph, changeIndex, changeAbsoluteIndex)
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

    public class ParagraphLanguageAction : ChangeAction
    {
        public ParagraphLanguageAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex, int changeAbsoluteIndex, string oldLanguage)
            : base(ChangeType.Modify, changedParagraph, changeIndex, changeAbsoluteIndex)
        {
            _oldLanguage = oldLanguage;
        }

        public override void Revert(Transcription trans)
        {
            ((TranscriptionParagraph)trans[ChangeTranscriptionIndex]).Language = OldLanguage;
        }

        string _oldLanguage;

        public string OldLanguage
        {
            get { return _oldLanguage; }
        }
    }


    public class BeginAction : ChangeAction
    {
        public BeginAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, TimeSpan oldtime)
            : base(ChangeType.Modify, changedElement, changeIndex,changeAbsoluteIndex)
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


    public class EndAction : ChangeAction
    {
        public EndAction(TranscriptionElement changedelement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, TimeSpan oldtime)
            : base(ChangeType.Modify, changedelement, changeIndex, changeAbsoluteIndex)
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


    public class TextAction : ChangeAction
    {
        public TextAction(TranscriptionElement changedelement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, string oldtstring)
            : base(ChangeType.Modify, changedelement, changeIndex, changeAbsoluteIndex)
        {
            _oldtstring = oldtstring;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].Text = _oldtstring;
        }

        string _oldtstring;

        public string Oldtstringe
        {
            get { return _oldtstring; }
        }
    }


    public class PhrasePhoneticsAction : ChangeAction
    {
        public PhrasePhoneticsAction(TranscriptionPhrase changedelement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, string oldphonetics)
            : base(ChangeType.Modify, changedelement, changeIndex, changeAbsoluteIndex)
        {
            _oldtstring = oldphonetics;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].Phonetics = _oldtstring;
        }

        string _oldtstring;

        public string Oldtstringe
        {
            get { return _oldtstring; }
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
