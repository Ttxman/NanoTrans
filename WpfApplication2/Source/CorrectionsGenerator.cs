using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Text.RegularExpressions;

namespace NanoTrans
{
    public static class CorrectionsGenerator
    {
        static Tuple<Regex,string[]>[] rules = new[]{
            Tuple.Create(new Regex(@"(\S*)y(\s*)"),new []{"$1i$2","$1í$2"}),
            Tuple.Create(new Regex(@"(\S*)ý(\s*)"),new []{"$1i$2","$1í$2"}),
            Tuple.Create(new Regex(@"(\S*)i(\s*)"),new []{"$1y$2","$1ý$2"}),
            Tuple.Create(new Regex(@"(\S*)í(\s*)"),new []{"$1y$2","$1ý$2"}),
        };

        public static IEnumerable<string> GetCorrectionsFor(string data)
        {
            yield return data;

            foreach (var item in rules)
            {
                if (item.Item2 == null || item.Item2.Length < 1)
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
        string _text;
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
