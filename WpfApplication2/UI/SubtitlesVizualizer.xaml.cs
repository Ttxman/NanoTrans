using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media.TextFormatting;
using System.Collections;
using System.Windows.Threading;
using System.Diagnostics;
using NanoTrans.Core;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for SubtitlesVizualizer
    /// </summary>
    public partial class SubtitlesVizualizer : UserControl
    {
        public static readonly DependencyProperty SubtitlesProperty =
        DependencyProperty.Register("Subtitles", typeof(Transcription), typeof(SubtitlesVizualizer), new FrameworkPropertyMetadata(OnSubtitlesChanged));

        public static void OnSubtitlesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Transcription data = (Transcription)e.NewValue;
            SubtitlesVizualizer vis = (SubtitlesVizualizer)d;

            Transcription olddata = e.OldValue as Transcription;
            //if (olddata != null)
            //{
            //    olddata.SubtitlesChanged -= vis.SubtitlesContentChanged;
            //}
            //if (data != null)
            //{
            //    data.SubtitlesChanged += vis.SubtitlesContentChanged;
            //}
        }

        public Transcription Subtitles
        {
            get
            {
                return (Transcription)GetValue(SubtitlesProperty);
            }
            set
            {

                SetValue(SubtitlesProperty, value);
            }
        }


        public delegate void TimespanRequestDelegate(out TimeSpan value);
        public TimespanRequestDelegate RequestTimePosition;

        public Action RequestPlayPause;

        public delegate void FlagStatusRequestDelegate(out bool value);
        public FlagStatusRequestDelegate RequestPlaying;

        public SubtitlesVizualizer()
        {
            InitializeComponent();
        }

        void l_NewRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            TranscriptionParagraph p = new TranscriptionParagraph();
            p.Add(new TranscriptionPhrase());
            if (el.ValueElement is TranscriptionParagraph)
            {
                TimeSpan pos;
                RequestTimePosition(out pos);
                el.ValueElement.End = pos;

                if ((el.ValueElement.End - el.ValueElement.Begin) < TimeSpan.FromMilliseconds(100))
                {
                    el.ValueElement.End = el.ValueElement.Begin + TimeSpan.FromMilliseconds(100);
                }

                if (el.ValueElement.Parent.End < el.ValueElement.End)
                    el.ValueElement.Parent.End = el.ValueElement.End;

                p.Begin = el.ValueElement.Parent.End;


                p.Speaker = ((TranscriptionParagraph)el.ValueElement).Speaker;
                el.ValueElement.Parent.Insert(el.ValueElement.ParentIndex + 1, p);
            }
            else if (el.ValueElement is TranscriptionSection)
            {
                el.ValueElement.Parent.Insert(0, p);
            }
            else if (el.ValueElement is TranscriptionChapter)
            {
                el.ValueElement.Children[0].Insert(0, p);
            }
            ActiveTransctiption = p;
        }

        void l_MoveRightRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            Element n = SetActiveTranscription(el.ValueElement.Next());
            if (n != null)
            {
                n.SetCaretOffset(0);
            }
        }

        void l_MoveLeftRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            Element n = SetActiveTranscription(el.ValueElement.Previous());

            if (n != null)
            {
                n.SetCaretOffset(n.TextLength);
            }
        }

        void l_MoveUpRequest(object sender, EventArgs e)
        {
            bool playing = false;
            if (RequestPlaying != null)
                RequestPlaying(out playing);

            if (playing && RequestPlayPause != null)
                RequestPlayPause();

            Element el = (Element)sender;
            TranscriptionElement tr = el.ValueElement;
            if (tr == null)
                return;
            TextViewPosition twpos = el.editor.TextArea.Caret.Position;
            Element n = SetActiveTranscription(tr.Previous());
            el = GetVisualForTransctiption(tr);
            if (n != null)
            {
                Point p = el.editor.TextArea.TextView.GetVisualPosition(twpos, VisualYPosition.LineMiddle);
                p = el.editor.PointToScreen(p);

                VisualLine last = n.editor.TextArea.TextView.VisualLines.Last();
                int len = last.StartOffset + last.TextLines.Sum(x => x.Length) - last.TextLines.Last().Length;

                Point p2 = n.editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(n.editor.Document.GetLocation(len)), VisualYPosition.LineMiddle);
                p2 = n.editor.PointToScreen(p2);

                Point loc = new Point(p.X, p2.Y);

                p2 = n.editor.PointFromScreen(loc);
                var tpos = n.editor.TextArea.TextView.GetPosition(p2);

                int pos = 0;
                if (tpos.HasValue)
                {
                    pos = n.editor.Document.Lines[tpos.Value.Line - 1].Offset + tpos.Value.Column - 1;
                }
                else
                {
                    pos = n.editor.Document.TextLength;
                }
                n.SetCaretOffset(pos);
            }

            if (playing && RequestPlayPause != null)
                RequestPlayPause();
        }

        void l_MoveDownRequest(object sender, EventArgs e)
        {
            bool playing = false;
            if (RequestPlaying != null)
                RequestPlaying(out playing);

            if (playing && RequestPlayPause != null)
                RequestPlayPause();

            Element el = (Element)sender;
            TranscriptionElement tr = el.ValueElement;

            if (tr == null)
                return;
            TextViewPosition twpos = el.editor.TextArea.Caret.Position;
            Element n = SetActiveTranscription(tr.Next());
            el = GetVisualForTransctiption(tr);
            if (n != null)
            {
                Point p = el.editor.TextArea.TextView.GetVisualPosition(twpos, VisualYPosition.LineMiddle);

                p = el.editor.PointToScreen(p);

                Point p2 = n.editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(1, 1), VisualYPosition.LineMiddle);
                p2 = n.editor.PointToScreen(p2);

                Point loc = new Point(p.X, p2.Y);

                p2 = n.editor.PointFromScreen(loc);
                var tpos = n.editor.TextArea.TextView.GetPosition(p2);

                int pos = 0;
                if (tpos.HasValue)
                    pos = n.editor.Document.Lines[tpos.Value.Line - 1].Offset + tpos.Value.Column - 1;
                else
                    pos = n.editor.Document.TextLength;

                n.SetCaretOffset(pos);
            }

            if (playing && RequestPlayPause != null)
                RequestPlayPause();
        }


        void l_MergeWithPreviousRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            TranscriptionElement t = el.ValueElement;
            TranscriptionElement p = el.ValueElement.Previous();

            int len =  p.Text.Length;
            if (t is TranscriptionParagraph && p is TranscriptionParagraph)
            {
                if (!(t.Children.Count == 1 && string.IsNullOrEmpty(t.Children[0].Text)))
                {
                    p.End = t.End;
                    t.Children.ForEach(x => p.Children.Add(x));
                    var cont = listbox.ItemContainerGenerator.ContainerFromItem(p) as ListBoxItem;
                    if (cont != null)
                    {
                        Element pel = cont.VisualFindChild<Element>();
                        pel.ValueElement = null;
                        pel.ValueElement = p;
                    }
                }
                t.Parent.Remove(t);
               
            }
            SpeakerChanged();
            ActiveTransctiption = p;
            ScrollToItem(p);
            var vis = GetVisualForTransctiption(p);
            vis.editor.CaretOffset = len;
        }

        void l_MergeWithnextRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            TranscriptionElement t = el.ValueElement;
            TranscriptionElement n = el.ValueElement.Next();
            int len = t.Text.Length;
            if (t is TranscriptionParagraph && n is TranscriptionParagraph)
            {
                t.End = n.End;
                n.Children.ForEach(x => t.Children.Add(x));
                var cont = listbox.ItemContainerGenerator.ContainerFromItem(t) as ListBoxItem;
                if (cont != null)
                {
                    Element pel = cont.VisualFindChild<Element>();
                    pel.ValueElement = null;
                    pel.ValueElement = t;
                }

                n.Parent.Remove(n);
            }
            SpeakerChanged();
            ActiveTransctiption = t;
            ScrollToItem(t);
            ColorizeBackground(t);
            var vis = GetVisualForTransctiption(t);
            vis.editor.CaretOffset = len;
            
        }

        void l_SplitRequest(object sender, EventArgs e)
        {
            try
            {
                Element el = (Element)sender;
                if (el.ValueElement is TranscriptionParagraph)
                {

                    TranscriptionParagraph par = (TranscriptionParagraph)el.ValueElement;
                    TimeSpan end = par.End;
                    TranscriptionParagraph par2 = new TranscriptionParagraph();
                    TranscriptionParagraph par1 = new TranscriptionParagraph();

                    par1.Speaker = par2.Speaker = par.Speaker;

                    par2.End = end;
                    int where = el.editor.CaretOffset;

                    int sum = 0;
                    for (int i = 0; i < par.Phrases.Count; i++)
                    {
                        TranscriptionPhrase p = par.Phrases[i];

                        if (sum + p.Text.Length <= where) //patri do prvniho
                        {
                            par1.Add(new TranscriptionPhrase(p));
                        }
                        else if (sum >= where)
                        {
                            par2.Add(new TranscriptionPhrase(p));
                        }
                        else if (sum <= where && sum + p.Text.Length > where) //uvnitr fraze
                        {
                            int offs = where - sum;
                            double ratio = offs / (double)p.Text.Length;


                            TimeSpan length = p.End - p.Begin;
                            TimeSpan l1 = new TimeSpan((long)(ratio * length.Ticks));

                            TranscriptionPhrase p1 = new TranscriptionPhrase();
                            p1.Text = p.Text.Substring(0, offs);

                            p1.Begin = p.Begin;
                            p1.End = p1.Begin + l1;
                            if (p1.End <= par.Begin)
                                p1.End = par.Begin + TimeSpan.FromMilliseconds(100); //pojistka kvuli nezarovnanejm textum
                            int idx = i;
                            par1.Add(p1);

                            TranscriptionPhrase p2 = new TranscriptionPhrase();
                            p2.Text = p.Text.Substring(offs);
                            p2.Begin = p1.End;
                            p2.End = p.End;


                            par2.Add(p2);
                            par2.Begin = p2.Begin;

                            par1.End = p1.End;

                        }
                        sum += p.Text.Length;
                    }//for

                    if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))//TODO: hodit to nejak jinak do funkci... :P
                    {

                        if (RequestTimePosition != null)
                        {
                            TimeSpan pos;
                            RequestTimePosition(out pos);

                            par1.End = pos;
                            par2.Begin = pos;
                        }

                    }
                    SpeakerChanged();
                    var parent = par.Parent;
                    int indx = par.ParentIndex;
                    parent.Remove(par);
                    parent.Insert(indx, par2);
                    parent.Insert(indx, par1);
                    ActiveTransctiption = par2;
                    return;
                }
            }
            finally
            {
            }
        }

        void l_GotFocus(object sender, RoutedEventArgs e)
        {
            var el = (sender as Element).ValueElement;
            if (m_activeTranscription == el || el == null)
                return;
            ActiveTransctiption = el;

            if (SelectedElementChanged != null)
            {
                SelectedElementChanged(this, new EventArgs());
            }

            ColorizeBackground(el);
        }

        public Element ActiveElement
        {
            get { return GetVisualForTransctiption(m_activeTranscription); }
        }

        private TranscriptionElement m_activeTranscription;
        private int m_activeTranscriptionOffset = -1;
        private int m_activetransctiptionSelectionLength = -1;
        private int m_activetransctiptionSelectionStart = -1;
        public TranscriptionElement ActiveTransctiption
        {
            get
            {
                return m_activeTranscription;
            }

            set
            {
                SetActiveTranscription(value);
            }
        }

        public Element GetVisualForTransctiption(TranscriptionElement el)
        {
            return listbox.ItemContainerGenerator.ContainerFromItem(el).VisualFindChild<Element>();
        }

        private bool ActivatingTranscription = false;
        public Element SetActiveTranscription(TranscriptionElement el)
        {
            ActivatingTranscription = true;
            if (el == null)
                return null;

            if (el == m_activeTranscription)
                return listbox.ItemContainerGenerator.ContainerFromItem(el).VisualFindChild<Element>();

            m_activeTranscription = el;
            m_activetransctiptionSelectionLength = -1;
            m_activetransctiptionSelectionStart = -1;


            foreach (Element ee in listbox.VisualFindChildren<Element>())
            {
                if (ee.ValueElement != el)
                {
                    ee.editor.Select(0, 0);
                }
            }

            listbox.ScrollIntoView(el);
            ListBoxItem cont = listbox.ItemContainerGenerator.ContainerFromItem(el) as ListBoxItem;
            if (cont == null)
            {
                listbox.UpdateLayout();
                listbox.ScrollIntoView(el);
                cont = listbox.ItemContainerGenerator.ContainerFromItem(el) as ListBoxItem;
            }

            if (cont != null)
            {
                var elm = cont.VisualFindChild<Element>();
                listbox.SelectedItem = el;

                //events may not be active yet
                if (elm.Background != MySetup.Setup.BarvaTextBoxuOdstavceAktualni)
                {
                    elm.editor.Focus();
                    ColorizeBackground(el);
                }
                ActivatingTranscription = false;
                return elm;
            }
            ActivatingTranscription = false;
            return null;
        }

        private void ScrollToItem(TranscriptionElement elm)
        {
            if (_scrollViewer == null)
                return;

            listbox.SelectedItem = elm;
            listbox.ScrollIntoView(listbox.Items[listbox.SelectedIndex]);
            listbox.UpdateLayout();
            Debug.WriteLine("scrollTO:" + elm.Text);
            if (elm == Subtitles.Last())
            {
                Debug.WriteLine("scrollToEnd " + _scrollViewer.VerticalOffset + "/" + _scrollViewer.ExtentHeight);
                _scrollViewer.ScrollToBottom();
            }
            else
            {
                var all = listbox.VisualFindChildren<Element>();
                var visible = all.Where(itm => listbox.VisualIsVisibleChild(itm));
                if (!visible.Any(itm => itm.ValueElement == elm))
                {
                    var element = all.First(itm => itm.ValueElement == elm);
                    var position = element.TransformToAncestor(_scrollViewer).Transform(new Point(0, element.ActualHeight));
                    Debug.WriteLine("scrollTO2:" + position.Y + " : " + _scrollViewer.ExtentHeight);
                    _scrollViewer.ScrollToVerticalOffset(position.Y);
                }
            }
        }

        private void ColorizeBackground(TranscriptionElement focused)
        {
            var all = listbox.VisualFindChildren<Element>();
            foreach (var elem in all)
            {
                if (elem.ValueElement == focused)
                {
                    elem.Background = MySetup.Setup.BarvaTextBoxuOdstavceAktualni;
                }
                else
                {
                    if (elem.ValueElement is TranscriptionSection)
                        elem.Background = MySetup.Setup.SectionBackground;
                    else if (elem.ValueElement is TranscriptionChapter)
                        elem.Background = MySetup.Setup.BarvaTextBoxuKapitoly;
                    else
                        elem.Background = null;
                }
            }
        }

        void l_PlayPauseRequest(object sender, EventArgs e)
        {
            if (PlayPauseRequest != null)
                PlayPauseRequest(this, null);
        }

        void Caret_PositionChanged(object sender, EventArgs e)
        {
            var l = sender as ICSharpCode.AvalonEdit.Editing.Caret;
            m_activeTranscriptionOffset = l.Offset;
        }

        void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            var l = sender as ICSharpCode.AvalonEdit.Editing.TextArea;
            m_activetransctiptionSelectionLength = l.Selection.Length;
            if (l.Selection.Length > 0 && l.Selection.Segments.Count() > 0)
                m_activetransctiptionSelectionStart = l.Selection.Segments.First().StartOffset;
            else
                m_activetransctiptionSelectionStart = 0;
        }

        void l_SetTimeRequest(TimeSpan obj)
        {
            if (SetTimeRequest != null)
                SetTimeRequest(obj);
        }

        public event EventHandler ChangeSpeaker;
        void l_ChangeSpeakerRequest(object sender, EventArgs e)
        {
            if (ChangeSpeaker != null)
            {
                ChangeSpeaker(sender, e);
            }
        }


        public void SpeakerChanged(Element e = null)
        {
            foreach (Element ee in listbox.VisualFindChildren<Element>())
            {
                ee.RefreshSpeakerButton();
            }
        }

        public void Reset()
        {
            var bf = listbox.ItemsSource;
            listbox.ItemsSource = null;

            listbox.ItemsSource = bf;
        }

        public event EventHandler SelectedElementChanged;
        public event Action<TimeSpan> SetTimeRequest;
        public event EventHandler PlayPauseRequest;


        private TimeSpan m_higlightedPostion = new TimeSpan(-1);
        public TimeSpan HiglightedPostion
        {
            get
            {
                return m_higlightedPostion;
            }
            set
            {
                m_higlightedPostion = value;
                foreach (Element l in listbox.VisualFindChildren<Element>())
                    l.HiglightedPostion = value;
            }
        }

        ScrollViewer _scrollViewer;
        private void Vizualizer_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = listbox.VisualFindChild<ScrollViewer>();
        }

        private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var elm = listbox.SelectedItem as TranscriptionElement;
            if (elm != null)
            {
                var elem = listbox.ItemContainerGenerator.ContainerFromItem(elm).VisualFindChild<Element>();
                if (elem != null && !elem.editor.IsFocused)
                {
                    elem.SetCaretOffset(0);
                }
            }
        }

        private void Vizualizer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ActiveElement == null)
                return;

            var trans = ActiveElement.TransformToAncestor(listbox);
            var topleft = trans.Transform(default(Point));

            if (e.Key == Key.PageDown)
            {
                e.Handled = true;

                _scrollViewer.PageDown();

                //end of document
                if (Math.Abs((_scrollViewer.ExtentHeight - _scrollViewer.VerticalOffset) - _scrollViewer.ViewportHeight) < 30)
                {
                    SetActiveTranscription(Subtitles.Last());
                    return;
                }

            }
            else if (e.Key == Key.PageUp)
            {
                e.Handled = true;
                _scrollViewer.PageUp();

                //start of dument
                if (Math.Abs(_scrollViewer.VerticalOffset) < 30)
                {
                    SetActiveTranscription(Subtitles.First());
                    return;
                }

            }

            if (e.Key == Key.PageUp || e.Key == Key.PageDown)
            {
                listbox.UpdateLayout();
                var visible = listbox.VisualFindChildren<Element>().Where(elm => listbox.VisualIsVisibleChild(elm));
                foreach (var el in visible)
                {
                    trans = el.TransformToAncestor(listbox);
                    var tl = trans.TransformBounds(new Rect(0.0, 0.0, el.ActualWidth, el.ActualHeight));
                    if (tl.Contains(topleft))
                    {
                        SetActiveTranscription(el.ValueElement);
                        return;
                    }
                }
            }
        }

        private void l_Element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ActivatingTranscription)
            {
                var value = ((Element)sender).ValueElement;

                Subtitles.ElementChanged(value);
               if (listbox.SelectedItem == value)
                    ScrollToItem(value);
            }
        }

        private void l_Element_ContentChanged(object sender, EventArgs e)
        {
            Subtitles.Saved = false;
        }

    }
}
