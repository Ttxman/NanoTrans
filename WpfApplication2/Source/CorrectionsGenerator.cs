﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Text.RegularExpressions;

namespace NanoTrans
{
    public static class CorrectionsGenerator
    {
        static readonly Tuple<Regex, string[]>[] rules = new[]
        {
            Tuple.Create(new Regex(@"(\S*)y(\s*)",RegexOptions.Compiled),new []{"$1i$2","$1í$2"}),
            Tuple.Create(new Regex(@"(\S*)ý(\s*)",RegexOptions.Compiled),new []{"$1i$2","$1í$2"}),
            Tuple.Create(new Regex(@"(\S*)i(\s*)",RegexOptions.Compiled),new []{"$1y$2","$1ý$2"}),
            Tuple.Create(new Regex(@"(\S*)í(\s*)",RegexOptions.Compiled),new []{"$1y$2","$1ý$2"}),
            Tuple.Create(new Regex(@"(\S+)ím(\s*)",RegexOptions.Compiled),new []{"$1im$2"}),
            Tuple.Create(new Regex(@"(\S+)(\s*)",RegexOptions.Compiled),new []{"$1ch$2"}),
            Tuple.Create(new Regex(@"(\S+)í(\s*)",RegexOptions.Compiled),new []{"$1ím$2"}),
            Tuple.Create(new Regex(@"(\S+)ou(\s*)",RegexOptions.Compiled),new []{"$1u$2"}),
            Tuple.Create(new Regex(@"(\S+)ch(\s*)",RegexOptions.Compiled),new []{"$1$2"}),
            Tuple.Create(new Regex(@"(\S+)ím(\s*)",RegexOptions.Compiled),new []{"$1í$2"}),
            Tuple.Create(new Regex(@"(\S+)u(\s*)",RegexOptions.Compiled),new []{"$1ou$2"}),
            Tuple.Create(new Regex(@"(\S+)é(\s*)",RegexOptions.Compiled),new []{"$1ej$2"}),
            Tuple.Create(new Regex(@"(\S+)(\s*)",RegexOptions.Compiled),new []{"$1ho$2"}),
            Tuple.Create(new Regex(@"(\S+)ně(\s*)",RegexOptions.Compiled),new []{"$1ní$2"}),
            Tuple.Create(new Regex(@"(\S+)ý(\s*)",RegexOptions.Compiled),new []{"$1ej$2"}),
            Tuple.Create(new Regex(@"(\S+)ti(\s*)",RegexOptions.Compiled),new []{"$1tí$2"}),
            Tuple.Create(new Regex(@"(\S+)(\s*)",RegexOptions.Compiled),new []{"$1te$2"}),
            Tuple.Create(new Regex(@"(\S+)ce(\s*)",RegexOptions.Compiled),new []{"$1ci$2"}),
            Tuple.Create(new Regex(@"(\S+)jí(\s*)",RegexOptions.Compiled),new []{"$1j$2"}),
            Tuple.Create(new Regex(@"(\S+)ci(\s*)",RegexOptions.Compiled),new []{"$1cí$2"}),
            Tuple.Create(new Regex(@"(\S+)li(\s*)",RegexOptions.Compiled),new []{"$1ly$2"}),
            Tuple.Create(new Regex(@"(\S+)e(\s*)",RegexOptions.Compiled),new []{"$1em$2"}),
            Tuple.Create(new Regex(@"(\S+)al(\s*)",RegexOptions.Compiled),new []{"$1at$2"}),
            Tuple.Create(new Regex(@"(\S+)á(\s*)",RegexOptions.Compiled),new []{"$1at$2"}),
            Tuple.Create(new Regex(@"(\S+)ci(\s*)",RegexOptions.Compiled),new []{"$1ce$2"}),
            Tuple.Create(new Regex(@"(\S+)ým(\s*)",RegexOptions.Compiled),new []{"$1ý$2"}),
            Tuple.Create(new Regex(@"(\S+)ly(\s*)",RegexOptions.Compiled),new []{"$1li$2"}),
            Tuple.Create(new Regex(@"(\S+)it(\s*)",RegexOptions.Compiled),new []{"$1í$2"}),
            Tuple.Create(new Regex(@"(\S+)ků(\s*)",RegexOptions.Compiled),new []{"$1ku$2"}),
            Tuple.Create(new Regex(@"(\S+)m(\s*)",RegexOptions.Compiled),new []{"$1um$2"}),
            Tuple.Create(new Regex(@"(\S+)ně(\s*)",RegexOptions.Compiled),new []{"$1ný$2"}),
            Tuple.Create(new Regex(@"(\S+)(\s*)",RegexOptions.Compiled),new []{"$1že$2"}),
            Tuple.Create(new Regex(@"(\S+)al(\s*)",RegexOptions.Compiled),new []{"$1á$2"}),
            Tuple.Create(new Regex(@"(\S+)ud(\s*)",RegexOptions.Compiled),new []{"$1uď$2"}),
        };


        public static IEnumerable<string> GetCorrections(string data)
        {
            return GetCorrectionPermutations(data).Where(w => w.Split(' ').All(one => SpellChecker.Checkword(one)));
        }


        private static IEnumerable<string> GetCorrectionPermutations(string data)
        {
            yield return data;

            foreach (var item in rules)
            {
                if (item.Item2 is null || item.Item2.Length < 1)
                    continue;
                Regex pattern = item.Item1;

                if (pattern.IsMatch(data))
                {
                    for (int i = 0; i < item.Item2.Length; i++)
                    {
                        var idx = i;
                        yield return pattern.Replace(data, item.Item2[idx]);
                    }
                }
            }
        }

    }

    public class CodeCompletionDataCorretion : ICompletionData
    {
        readonly string _text;
        public CodeCompletionDataCorretion(string text)
        {
            _text = text;
        }

        public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Selection.ReplaceSelectionWithText(_text);
        }

        public object Content
        {
            get { return _text; }
        }

        public object Description
        {
            get { return null; }
        }

        public System.Windows.Media.ImageSource Image
        {
            get { return null; }
        }

        public double Priority
        {
            get { return 0; }
        }

        public string Text
        {
            get { return _text; }
        }
    }
}
