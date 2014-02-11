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

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for SubtitlesVizualizer
    /// </summary>
    public partial class SubtitlesVizualizer : UserControl
    {
        public static readonly DependencyProperty SubtitlesProperty =
        DependencyProperty.Register("Subtitles", typeof(MySubtitlesData), typeof(SubtitlesVizualizer), new FrameworkPropertyMetadata(OnSubtitlesChanged));

        public static void OnSubtitlesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MySubtitlesData data = (MySubtitlesData)e.NewValue;
            SubtitlesVizualizer vis = (SubtitlesVizualizer)d;

            MySubtitlesData olddata = e.OldValue as MySubtitlesData;
            if (olddata != null)
            {
                olddata.SubtitlesChanged -= vis.SubtitlesContentChanged;
            }
            if (data != null)
            {
                data.SubtitlesChanged += vis.SubtitlesContentChanged;
                
            }

            vis.RecalculateSizes();

            vis.RecreateElements(vis.gridscrollbar.Value);

            vis.SubtitlesContentChanged();
        }

        public void RecalculateSizes()
        {
            double totalh = 0;
            if (Subtitles != null)
            {
                Element lm = new Element(true);
                foreach (TranscriptionElement tr in Subtitles)
                {
                    
                    lm.editor.InvalidateMeasure();
                    lm.editor.InvalidateArrange();
                    lm.maingrid.InvalidateMeasure();
                    lm.maingrid.InvalidateArrange();
                    lm.InvalidateMeasure();
                    lm.InvalidateArrange();
                    lm.ValueElement = tr;
                    lm.maingrid.Measure(new Size(gridstack.ActualWidth, double.MaxValue));
                    lm.maingrid.Arrange(new Rect(0, 0, gridstack.ActualWidth, lm.maingrid.DesiredSize.Height));
                    lm.editor.UpdateLayout();
                    tr.height = lm.maingrid.ActualHeight;
                    totalh += lm.maingrid.ActualHeight;
                }
                lm.ValueElement = null;
                Subtitles.TotalHeigth = totalh;
            }
            updating = true;
            gridscrollbar.Maximum = totalh - this.ActualHeight;
            gridscrollbar.LargeChange = this.ActualHeight;
            gridscrollbar.SmallChange = 25;
            gridscrollbar.ViewportSize = gridscrollbar.LargeChange;
            updating = false;
        }
        bool updating = false;


        public MySubtitlesData Subtitles
        {
            get
            {
                return (MySubtitlesData)GetValue(SubtitlesProperty);
            }
            set
            {
                
                SetValue(SubtitlesProperty, value);
            }
        }

        public double PanelWidth
        {
            get { return gridstack.ActualWidth; }
        }

        public SubtitlesVizualizer()
        {
            InitializeComponent();
        }

        private bool m_Updating = false;
        private bool m_updated = false;
        public void BeginUpdate()
        {
            m_Updating = true;
        }

        public void EndUpdate()
        {
            m_Updating = false;
            if (m_updated)
                RecreateElements(gridscrollbar.Value);
            m_updated = false;
        }

        void l_NewRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender; 
            MyParagraph p = new MyParagraph();
            p.Add(new MyPhrase());
            if (el.ValueElement is MyParagraph)
            {
                p.speakerID = ((MyParagraph)el.ValueElement).speakerID;
                el.ValueElement.Parent.Insert(el.ValueElement.ParentIndex + 1, p);
            }
            else if (el.ValueElement is MySection)
            {
                el.ValueElement.Parent.Insert(0, p);
            }
            else if (el.ValueElement is MyChapter)
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
            Element el = (Element)sender;
            TranscriptionElement tr = el.ValueElement;
            if (tr == null)
                return;
            TextViewPosition twpos = el.editor.TextArea.Caret.Position;
            Element n = SetActiveTranscription(tr.Previous());
            el = GetVisualForTransctiption(tr);
            if (n != null)
            {
                Point p = el.editor.TextArea.TextView.GetVisualPosition(twpos,VisualYPosition.LineMiddle);
                p = el.editor.PointToScreen(p);

                VisualLine last = n.editor.TextArea.TextView.VisualLines.Last();
                int len = last.StartOffset + last.TextLines.Sum(x => x.Length) - last.TextLines.Last().Length;

                Point p2 = n.editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(n.editor.Document.GetLocation(len)), VisualYPosition.LineMiddle);
                p2 = n.editor.PointToScreen(p2);

                Point loc = new Point(p.X,p2.Y);

                p2 = n.editor.PointFromScreen(loc);
                var tpos = n.editor.TextArea.TextView.GetPosition(p2);

                int pos = 0;
                if (tpos.HasValue)
                {
                    pos = n.editor.Document.Lines[tpos.Value.Line-1].Offset + tpos.Value.Column -1;
                }
                else
                {
                    pos = n.editor.Document.TextLength;
                }
                n.SetCaretOffset(pos);
            }
        }

        void l_MoveDownRequest(object sender, EventArgs e)
        {
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
        }


        void l_MergeWithPreviousRequest(object sender, EventArgs e)
        {
            BeginUpdate();
            Element el = (Element)sender;
            TranscriptionElement t = el.ValueElement;
            TranscriptionElement p = el.ValueElement.Previous();

            if (t is MyParagraph && p is MyParagraph)
            {
                p.End = t.End;
                t.Children.ForEach(x => p.Children.Add(x));
                t.Parent.Remove(t);
            }
            EndUpdate();
        }

        void l_MergeWithnextRequest(object sender, EventArgs e)
        {
            BeginUpdate();
            Element el = (Element)sender;
            TranscriptionElement t = el.ValueElement;
            TranscriptionElement n = el.ValueElement.Next();

            if (t is MyParagraph && n is MyParagraph)
            {
                t.End = n.End;
                n.Children.ForEach(x => t.Children.Add(x));
                n.Parent.Remove(n);
            }
            EndUpdate();
        }

        void l_SplitRequest(object sender, EventArgs e)
        {
            try
            {
                Element el = (Element)sender;
                BeginUpdate();
                if (el.ValueElement is MyParagraph)
                {
                    MyParagraph par = (MyParagraph)el.ValueElement;
                    int where = el.editor.CaretOffset;

                    int sum = 0;
                    for (int i = 0; i < par.Phrases.Count; i++)
                    {
                        MyPhrase p = par.Phrases[i];
                        if (sum <= where && sum + p.Text.Length > where) //uvnitr fraze
                        {
                            int offs = where - sum;
                            double ratio = offs / (double)p.Text.Length;


                            TimeSpan length = p.End - p.Begin;

                            TimeSpan l1 = new TimeSpan((long)(ratio * length.Ticks));

                            MyPhrase p1 = new MyPhrase();
                            p1.Text = p.Text.Substring(0, offs);

                            p1.Begin = p.Begin;
                            p1.End = p1.Begin + l1;

                            int idx = i;
                            par.RemoveAt(i);
                            par.Insert(i, p1);

                            MyPhrase p2 = new MyPhrase();
                            p2.Text = p.Text.Substring(offs);
                            p2.Begin = p1.End;
                            p2.End = p.End;

                            MyParagraph par2 = new MyParagraph();
                            par2.Add(p2);
                            par2.Begin = p2.Begin;
                            par2.End = par.End;
                            par.End = p1.End;
                            par.BeginUpdate();
                            i++;
                            par.BeginUpdate();
                            while (par.Phrases.Count > i)
                            {
                                MyPhrase ph = par.Phrases[i];
                                par.RemoveAt(i);
                                par2.Add(ph);
                            }
                            par.EndUpdate();
                            par.Parent.Insert(par.ParentIndex + 1, par2);
                            return;
                        }
                        sum += p.Text.Length;
                    }

                }
            }
            finally
            {
                EndUpdate();
            }
        }

       
        void l_LostFocus(object sender, RoutedEventArgs e)
        {
            m_activeElement = null;
            (sender as Element).maingrid.Background = null;
        }

        void l_GotFocus(object sender, RoutedEventArgs e)
        {
            
            m_activeElement = sender as Element;
            m_activeElement.maingrid.Background = Brushes.Beige;
            if (m_activeTranscription == m_activeElement.ValueElement)
                return;
            m_activeTranscription = m_activeElement.ValueElement;
            
            if (SelectedElementChanged != null)
            {
                SelectedElementChanged(this, new EventArgs());
            }
        }

        void l_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Height > 0.001 && e.PreviousSize!=e.NewSize)
            {
                double delta = e.NewSize.Height - ((Element)sender).ValueElement.height;
                ((Element)sender).ValueElement.height = e.NewSize.Height;
                
                Subtitles.TotalHeigth += delta;
                gridscrollbar.Maximum = Subtitles.TotalHeigth - ActualHeight;

            }
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double value = gridscrollbar.Value;
            value -= (e.Delta > 0 ? 1 : -1) *gridscrollbar.SmallChange;

            if (value < gridscrollbar.Minimum)
                value = gridscrollbar.Minimum;

            if (value > gridscrollbar.Maximum)
                value = gridscrollbar.Maximum;

            gridscrollbar.Value = value;
        }

        private Element m_activeElement;
        public Element ActiveElement
        {
            get { return m_activeElement; }
        }

        private TranscriptionElement m_activeTranscription;
        public TranscriptionElement ActiveTransctiption
        {
            get 
            {
                if (m_activeElement == null)
                    return null;
                else
                    return m_activeElement.ValueElement;
            }

            set 
            {
                SetActiveTranscription(value);
            }
        }

        public Element GetVisualForTransctiption(TranscriptionElement el)
        {
            foreach (Element ee in gridstack.Children)
            {
                if (ee.ValueElement == el)
                {
                    return ee;
                }
            }

            return null;
        }

        public Element SetActiveTranscription(TranscriptionElement el)
        {
            if (el == null)
                return null;

            
            Element e = GetVisualForTransctiption(el);
            
            if (e != null)
            {
                TranscriptionElement t = e.ValueElement;
                double h = e.TransformToAncestor(this).Transform(new Point(0, 0)).Y + e.ValueElement.height;

                if (h  > ActualHeight)
                {
                    double delta = h - ActualHeight + 10;
                    gridscrollbar.Value += delta;
                    UpdateLayout();
                    e = GetVisualForTransctiption(t);
                }
                else if (h < el.height)
                {
                    double delta = h - el.height - 10;
                    gridscrollbar.Value += delta;
                    UpdateLayout();
                    e = GetVisualForTransctiption(t);
                }
                foreach (Element ee in gridstack.Children)
                    ee.SetCaretOffset(-1);
                if(e!=null)
                    e.SetCaretOffset(0);
                return e;
            }


            double totalh = 0;
            if (Subtitles != null)
            {
                foreach (TranscriptionElement tr in Subtitles)
                {
                    if (tr == el)
                        break;
                    totalh += tr.height; 
                }
            }
            gridscrollbar.Value = totalh;

            return gridstack.Children[0] as Element;
        }

        private Stack<Element> ElementCache = new Stack<Element>();
        private void RecycleElement(Element e)
        {
            e.SizeChanged -= l_SizeChanged;
            e.GotFocus -= l_GotFocus;
            e.LostFocus -= l_LostFocus;

            e.ClearEvents();
            e.ClearBindings();
            e.ValueElement = null;
            //e.editor.UpdateLayout();
            //ElementCache.Push(e);
        }

        private Element GetElement()
        {
            if (ElementCache.Count > 0)
            {
                return ElementCache.Pop();
            }
            else
                return new Element();
        }

        public void RecreateElements(double newpos)
        {
            if (gridstack == null || Subtitles == null)
                return;
            double maxh = this.ActualHeight;

            if (m_Updating)
            {
                m_updated = true;
                return;
            }

            double pos = 0;
            bool ffound = false;

            TranscriptionElement te = ActiveTransctiption;
            int offset = -1;

            if (ActiveElement != null)
                offset = ActiveElement.editor.CaretOffset;


            List<Element> elms = new List<Element>();
            foreach (Element el in gridstack.Children)
                elms.Add(el);
            gridstack.Children.Clear();

            foreach (Element el in elms)
                RecycleElement(el);

            m_activeElement = null;
            if (Subtitles != null)
            {
                double move = 0;
                foreach (TranscriptionElement el in Subtitles)
                {
                    Element l = null;
                    if (pos + el.height >= newpos && !ffound)
                    {
                        move = pos - newpos;
                        l = GetElement();
                        l.ValueElement = el;

                        gridstack.Margin = new Thickness(0, move, 0, 0);
                        ffound = true;
                    }
                    else if (ffound && pos < newpos + maxh - move + el.height)
                    {
                        l = GetElement();
                        l.ValueElement = el;

                    }


                    if (l != null)
                    {
                        l.GotFocus += l_GotFocus;
                        l.LostFocus += l_LostFocus;

                        l.MergeWithnextRequest += l_MergeWithnextRequest;
                        l.MergeWithPreviousRequest +=l_MergeWithPreviousRequest;
                        l.MoveDownRequest += l_MoveDownRequest;
                        l.MoveUpRequest += l_MoveUpRequest;
                        l.MoveLeftRequest += l_MoveLeftRequest;
                        l.MoveRightRequest += l_MoveRightRequest;
                        l.SplitRequest += l_SplitRequest;
                        l.NewRequest += l_NewRequest;
                        l.ChangeSpeakerRequest += l_ChangeSpeakerRequest;
                        l.SetTimeRequest += l_SetTimeRequest;
                        gridstack.Children.Add(l);
                        
                    }

                    pos += el.height;
                }
            }


            gridstack.UpdateLayout();
            foreach (Element l in gridstack.Children)
            {
                l.SizeChanged += l_SizeChanged;
                if (l.ValueElement == te)
                {
                    l.SetCaretOffset(offset);
                    l.HiglightedPostion = HiglightedPostion;
                }
                else
                {
                    l.SetCaretOffset(-1);
                }
            }
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

        public void SubtitlesContentChanged()
        {
            if (!updating)
            {
                RecalculateSizes();
                RecreateElements(gridscrollbar.Value);
            }
        }

        private void gridscrollbar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(!updating)
            RecreateElements(e.NewValue);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SubtitlesContentChanged();
        }


        public event EventHandler SelectedElementChanged;
        public event Action<TimeSpan> SetTimeRequest;


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
                foreach (Element l in gridstack.Children)
                    l.HiglightedPostion = value;
            }
        }
    }
}
