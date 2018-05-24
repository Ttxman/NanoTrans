using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{
    public struct TranscriptionIndex
    {
        private int _chapterindex;

        public int Chapterindex
        {
            get { return _chapterindex; }
        }
        private int _sectionindex;

        public int Sectionindex
        {
            get { return _sectionindex; }
        }
        private int _paragraphIndex;

        public int ParagraphIndex
        {
            get { return _paragraphIndex; }
        }
        private int _phraseIndex;

        public int PhraseIndex
        {
            get { return _phraseIndex; }
        }



        public TranscriptionIndex(int chapterindex = -1, int sectionindex = -1, int paragraphIndex = -1, int phraseIndex = -1)
        {
            _chapterindex = chapterindex;
            _sectionindex = sectionindex;
            _paragraphIndex = paragraphIndex;
            _phraseIndex = phraseIndex;
        }

        public TranscriptionIndex(int[] indexa)
        {
            _chapterindex = indexa[0];
            _sectionindex = indexa[1];
            _paragraphIndex = indexa[2];
            _phraseIndex = indexa[3];
        }

        public static readonly TranscriptionIndex FirstChapter = new TranscriptionIndex(0, -1, -1, -1);
        public static readonly TranscriptionIndex FirstSection = new TranscriptionIndex(0, 0, -1, -1);
        public static readonly TranscriptionIndex FirstParagraph = new TranscriptionIndex(0, 0, 0, -1);
        public static readonly TranscriptionIndex FirstPhrase = new TranscriptionIndex(0, 0, 0, -1);

        public static readonly TranscriptionIndex Invalid = new TranscriptionIndex(-1, -1, -1, -1);


        public int[] ToArray()
        {
            return new int[] { _chapterindex, _sectionindex, _paragraphIndex, _phraseIndex };
        }


        /// <summary>
        /// Is index valid ( starts with positives integers, negative integers from end and no mixing between positive and negative
        ///  for example you cannot index first paragraph of -1 section
        /// </summary>
        public bool IsValid
        {
            get
            {
                var ind = this.ToArray();

                var fromstart = ind.TakeWhile(i => i >= 0).Count();
                var fromend = ind.Reverse().TakeWhile(i => i < 0).Count();

                return fromstart != 0 && fromstart + fromend == 4;
            }
        }


        public bool IsPhraseIndex
        {
            get
            {
                return IsValid && _phraseIndex >= 0;
            }
        }

        public bool IsParagraphIndex
        {
            get
            {
                return IsValid && _paragraphIndex >= 0;
            }
        }

        public bool IsSectionIndex
        {
            get
            {
                return IsValid && _sectionindex >= 0;
            }
        }


        public bool IsChapterIndex
        {
            get
            {
                return IsValid && _chapterindex >= 0;
            }
        }


        /// <summary>
        /// type of element that is indexed by this paragraph - TranscriptionChapter, Section, Paragraph, Phrase
        /// </summary>
        public Type IndexedType
        {
            get
            {
                if (!IsValid)
                    return null;

                if (_phraseIndex >= 0)
                    return typeof(TranscriptionPhrase);
                else if (_paragraphIndex >= 0)
                    return typeof(TranscriptionParagraph);
                else if (_sectionindex >= 0)
                    return typeof(TranscriptionSection);
                else// if (_sectionindex >= 0)
                    return typeof(TranscriptionChapter);
            }
        }

        public override string ToString()
        {
            return string.Format("{4}: {0};{1};{2};{3}",_chapterindex,_sectionindex,_paragraphIndex,_phraseIndex,IsValid?"TIndex":"TInvalidIndex");
        }

    }
}
