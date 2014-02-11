using System;
using System.Collections.Generic;
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
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Indentation;
using System.IO;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Element.xaml
    /// </summary>
    public partial class Element : UserControl
    {

        private int m_forceCarretpositionOnLoad = -1;
        public static readonly DependencyProperty ValueElementProperty =
        DependencyProperty.Register("ValueElement", typeof(TranscriptionElement), typeof(Element), new FrameworkPropertyMetadata(OnValueElementChanged));

        public bool DisableAutomaticElementVisibilityChanges{get; set;}
        public bool EditPhonetics{get;set;}
        public static void OnValueElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
           
            TranscriptionElement val = (TranscriptionElement)e.NewValue;
            Element el = (Element)d;
            el.m_Element = val;

            if (el.ValueElement != null)
            {
                el.ValueElement.BeginChanged -= el.BeginChanged;
                el.ValueElement.EndChanged -= el.EndChanged;
            }

            el.updating = true;
            if (val != null)
                el.Text = (el.EditPhonetics) ? val.Phonetics : val.Text;
            else
                el.Text = "";
            el.updating = false;


            if (el.DisableAutomaticElementVisibilityChanges)
            {
                return;
            }


            Type t = null;

            if (e.NewValue != null)
            {
                t = e.NewValue.GetType();
            }

            
            if (t == typeof(MyParagraph))
            {
                MyParagraph p = (MyParagraph)val;
                el.textbegin.Visibility = Visibility.Visible;
                el.textend.Visibility = Visibility.Visible;
                el.stackPanel1.Visibility = Visibility.Visible;
                el.Background = MySetup.Setup.BarvaTextBoxuOdstavce;
                el.button1.Visibility = Visibility.Visible;
                if (val.PreviousSibling() != null)
                {
                    if (val is MyParagraph)
                    {
                        if (val.PreviousSibling() is MyParagraph && ((MyParagraph)val).speakerID == ((MyParagraph)val.PreviousSibling()).speakerID)
                        {
                            el.button1.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                el.checkBox1.Visibility = Visibility.Visible;
                el.checkBox1.IsChecked = ((MyParagraph)val).trainingElement;
            }
            else if (t == typeof(MySection))
            {
                MySection s = (MySection)val;
                el.textbegin.Visibility = Visibility.Collapsed;
                el.textend.Visibility = Visibility.Collapsed;
                el.stackPanel1.Visibility = Visibility.Collapsed;
                el.Background = Brushes.LightGreen;
                el.button1.Visibility = Visibility.Collapsed;
                el.checkBox1.Visibility = Visibility.Collapsed;
            }
            else if (t == typeof(MyChapter))
            {
                MyChapter c = (MyChapter)val;
                el.textbegin.Visibility = Visibility.Collapsed;
                el.textend.Visibility = Visibility.Collapsed;
                el.stackPanel1.Visibility = Visibility.Collapsed;
                el.Background = Brushes.LightPink;
                el.button1.Visibility = Visibility.Collapsed;
                el.checkBox1.Visibility = Visibility.Collapsed;
            }
            else
            {
                el.textbegin.Visibility = Visibility.Visible;
                el.textend.Visibility = Visibility.Visible;
                el.stackPanel1.Visibility = Visibility.Visible;
                el.Background = MySetup.Setup.BarvaTextBoxuOdstavce;
                el.button1.Visibility = Visibility.Visible;
                el.checkBox1.Visibility = Visibility.Visible;
                el.checkBox1.IsChecked = false;
            }

            el.DataContext = val;
            if (val != null)
            {
                val.BeginChanged += el.BeginChanged;
                val.EndChanged += el.EndChanged;

                el.BeginChanged(el, null);
                el.EndChanged(el, null);

                el.RepaintAttributes();
            }

            el.updating = false;
        }

        TranscriptionElement m_Element;
        public TranscriptionElement ValueElement
        {
            get
            {
                return (TranscriptionElement)GetValue(ValueElementProperty);
            }
            set
            {
                SetValue(ValueElementProperty, value);
            }
        }

        public string Text
        {
            get
            {
                return editor.Text;
            }
            set
            {
                editor.Text = value;
            }
        }

        static Brush GetRectangleBgColor(MyEnumParagraphAttributes param)
        {
            return Brushes.White;
        }

        static Brush GetRectangleInnenrColor(MyEnumParagraphAttributes param)
        {
            switch (param)
            {
                default:
                case MyEnumParagraphAttributes.None:
                    return Brushes.White;
                case MyEnumParagraphAttributes.Background_noise:
                    return Brushes.DodgerBlue;
                case MyEnumParagraphAttributes.Background_speech:
                    return Brushes.Chocolate;
                case MyEnumParagraphAttributes.Junk:
                    return Brushes.Crimson;
                case MyEnumParagraphAttributes.Narrowband:
                    return Brushes.Olive;
            }
        }
        static MyEnumParagraphAttributes[] all = (MyEnumParagraphAttributes[])Enum.GetValues(typeof(MyEnumParagraphAttributes));

        private void RepaintAttributes()
        {
            if (m_IsPasiveElement)
                return;
            if (this.stackPanel1.Children.Count != all.Length)
            {
                this.stackPanel1.Children.Clear();
                foreach (MyEnumParagraphAttributes at in all)
                {
                    if (at != MyEnumParagraphAttributes.None)
                    {
                        string nam = Enum.GetName(typeof(MyEnumParagraphAttributes), at);

                        Rectangle r = new Rectangle();
                        r.Stroke = Brushes.Green;
                        r.Width = 10;
                        r.Height = 8;
                        r.ToolTip = nam;
                        r.Margin = new Thickness(0, 0, 0, 1);

                        MyParagraph par = ValueElement as MyParagraph;
                        if (par != null && (par.DataAttributes & at) != 0)
                            r.Fill = GetRectangleInnenrColor(at);
                        else
                            r.Fill = GetRectangleBgColor(at);
                        r.MouseLeftButtonDown += new MouseButtonEventHandler(attributes_MouseLeftButtonDown);
                        r.Tag = at;
                        stackPanel1.Children.Add(r);
                    }
                }
            }
        }

        void attributes_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //int i = ((sender as Rectangle).Parent as StackPanel).Children.IndexOf(sender as UIElement) + 1;
            MyParagraph par = ValueElement as MyParagraph;
            if (par != null)
            {
                MyEnumParagraphAttributes attr = (MyEnumParagraphAttributes)(sender as Rectangle).Tag;

                par.DataAttributes ^= attr;

                foreach (Rectangle r in ((sender as Rectangle).Parent as StackPanel).Children)
                {
                    attr = (MyEnumParagraphAttributes)r.Tag;
                    if ((par.DataAttributes & attr) != 0)
                    {
                        r.Fill = GetRectangleInnenrColor(attr);
                    }
                    else
                    {
                        r.Fill = GetRectangleBgColor(attr);
                    }
                }
            }
        }

        private bool updating = false;


        public Element():this(false)
        { 
        
        }



        private bool m_IsPasiveElement;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isPasiveElement"></param>
        public Element(bool isPasiveElement)
        {
            m_IsPasiveElement = isPasiveElement;
            InitializeComponent();

            if (!m_IsPasiveElement)
                RepaintAttributes();

            
            editor.TextArea.TextView.ElementGenerators.Add(DefaultNonEditableBlockGenerator);
            editor.TextArea.IndentationStrategy = new NoIndentationStrategy() ;
            editor.Options.InheritWordWrapIndentation = false;

            if (!m_IsPasiveElement)
            {
                editor.TextArea.TextView.LineTransformers.Add(DefaultSpellchecker);
                editor.TextArea.TextEntering += new TextCompositionEventHandler(TextArea_TextEntering);
                editor.TextArea.TextEntered += new TextCompositionEventHandler(TextArea_TextEntered);
                editor.Document.Changed += new EventHandler<DocumentChangeEventArgs>(Document_Changed);

                editor.TextArea.TextView.MouseLeftButtonDown += new MouseButtonEventHandler(TextView_MouseLeftButtonDown);
            }
        }

        void TextView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                var t = editor.TextArea.TextView.GetPosition(e.GetPosition(editor));
                if (!t.HasValue)
                    return;
                int pos = editor.Document.GetLineByNumber(t.Value.Line).Offset+t.Value.Column;
                if (t.HasValue && ValueElement != null && ValueElement.IsParagraph)
                {
                    MyParagraph p = (MyParagraph)ValueElement;
                    int ps = 0;
                    foreach (var ph in p.Phrases)
                    {
                        ps += EditPhonetics ? ph.Phonetics.Length : ph.Text.Length;
                        if (ps > pos)
                        {
                            if (SetTimeRequest != null)
                                SetTimeRequest(ph.Begin);
                            editor.TextArea.Caret.Location = t.Value;
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
        }


        public static readonly SpellChecker DefaultSpellchecker = new SpellChecker();
        public static readonly NonEditableBlockGenerator DefaultNonEditableBlockGenerator = new NonEditableBlockGenerator();

        private PositionHighlighter BackgroundHiglighter = null;

        void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (completionWindow != null)
            {
                if (completionWindow.CompletionList.ListBox.SelectedItem != null)
                {
                    CodeCompletionData cd = (CodeCompletionData)completionWindow.CompletionList.ListBox.SelectedItem;

                    int offset = editor.TextArea.Caret.Offset;
                    string entered = editor.TextArea.Document.GetText(completionWindow.StartOffset, offset - completionWindow.StartOffset);

                    if (cd.uniquepart == entered)
                    {
                        completionWindow.CompletionList.RequestInsertion(new EventArgs());
                    }
                }

            }
        }

        void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "/")
            {
                e.Handled = true;
                completionWindow = new CompletionWindow(editor.TextArea);
                completionWindow.ResizeMode = ResizeMode.NoResize;
                // provide AvalonEdit with the data:
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                foreach (string s in MySetup.Setup.NerecoveUdalosti)
                    data.Add(new CodeCompletionData(s));

                completionWindow.Show();

                completionWindow.Closed += delegate
                {
                    completionWindow = null;
                };
            }
        }

        public static readonly Regex wordSplitter = new Regex(@"(?:\w+|\[.*?\])", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex wordIgnoreChecker = new Regex(@"(?:\[.*?\]|\d+)", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex ignoredGroup = new Regex(@"\[.*?\]", RegexOptions.Singleline | RegexOptions.Compiled);


        public class PositionHighlighter : IBackgroundRenderer
        {
            int m_from;
            int m_len;
            Pen border;
            public PositionHighlighter(int from, int len)
            {
                m_from = from;
                m_len = len;
                border = new Pen(Brushes.ForestGreen,1);
                border.Freeze();
            }

            #region IBackgroundRenderer Members

            public void Draw(TextView textView, DrawingContext drawingContext)
            {
                BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();

                builder.CornerRadius = 1;
                builder.AlignToMiddleOfPixels = true;

                builder.AddSegment(textView, new TextSegment() { StartOffset = m_from, Length = m_len });
                builder.CloseFigure(); // prevent connecting the two segments

                Geometry geometry = builder.CreateGeometry();
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(Brushes.LightGreen, border, geometry);
                }
            }

            public KnownLayer Layer
            {
                get { return KnownLayer.Background; }
            }

            #endregion
        }

        public class NonEditableBlockGenerator : VisualLineElementGenerator
        {
            Match FindMatch(int startOffset)
            {
                // fetch the end offset of the VisualLine being generated
                int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
                TextDocument document = CurrentContext.Document;
                string relevantText = document.GetText(startOffset, endOffset - startOffset);
                return ignoredGroup.Match(relevantText);
            }

            /// Gets the first offset >= startOffset where the generator wants to construct
            /// an element.
            /// Return -1 to signal no interest.
            public override int GetFirstInterestedOffset(int startOffset)
            {
                Match m = FindMatch(startOffset);
                return m.Success ? (startOffset + m.Index) : -1;
            }

            /// Constructs an element at the specified offset.
            /// May return null if no element should be constructed.
            public override VisualLineElement ConstructElement(int offset)
            {
                Match m = FindMatch(offset);
                // check whether there's a match exactly at offset
                if (m.Success && m.Index == 0)
                {
                    return new InlineObjectElement(m.Length, new TextBlock() { Text = m.Value });
                }
                return null;
            }
        }

        public class CodeCompletionData : ICompletionData
        {
            object uielement = null;
            public string uniquepart = null;
            public CodeCompletionData(string text)
            {
                this.Text = text;

                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                Label lb1 = new Label();
                Label lb2 = new Label();
                lb1.FontWeight = FontWeights.Bold;
                Thickness t = lb1.Padding;
                t.Right = 0;
                lb1.Padding = t;

                t = lb2.Padding;
                t.Left = 0;
                lb2.Padding = t;

                int maxcnt = 0;
                foreach (string s2 in MySetup.Setup.NerecoveUdalosti)
                {
                    if (text != s2)
                    {
                        for (int i = 0; i < text.Length && i < s2.Length; i++)
                        {
                            if (text[i] != s2[i])
                            {
                                if (maxcnt < i)
                                    maxcnt = i;
                                break;
                            }
                        }
                    }
                }

                lb1.Content = uniquepart = text.Substring(0, maxcnt + 1);
                lb2.Content = text.Substring(maxcnt + 1);

                sp.Children.Add(lb1);
                sp.Children.Add(lb2);

                uielement = sp;
            }

            public System.Windows.Media.ImageSource Image
            {
                get { return null; }
            }

            public string Text { get; private set; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content
            {
                get { return uielement; }
            }

            public object Description
            {
                get { return null; }
            }

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, "[" + this.Text + "]");
            }

            public double Priority
            {
                get { return 0; }
            }
        }
        
        public class NoIndentationStrategy : IIndentationStrategy
        {
            public void IndentLine(TextDocument document, DocumentLine line)
            {
            }

            public void IndentLines(TextDocument document, int beginLine, int endLine)
            {
            }
        }

        static CompletionWindow completionWindow;

        private void BeginChanged(object sendet, EventArgs value)
        {
            if (ValueElement != null)
            {
                TimeSpan val = ValueElement.Begin;
                this.textbegin.Text = string.Format("{0}:{1:00}:{2:00},{3}", val.Hours, val.Minutes, val.Seconds, val.Milliseconds.ToString("00").Substring(0, 2));
                EndChanged(this,null);//zmena zacatku muze ovlivnit konec...
            }

        }

        private void EndChanged(object sendet, EventArgs value)
        {
            if (ValueElement != null)
            {
                if (!ValueElement.IsParagraph)
                    textend.Visibility = System.Windows.Visibility.Collapsed;
                else if (ValueElement.End <= ValueElement.Begin)
                    textend.Visibility = System.Windows.Visibility.Hidden;
                else
                    textend.Visibility = System.Windows.Visibility.Visible;

                TimeSpan val = ValueElement.End;
                this.textend.Text = string.Format("{0}:{1:00}:{2:00},{3}", val.Hours, val.Minutes, val.Seconds, val.Milliseconds.ToString("00").Substring(0, 2));
            }
        }

        internal void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ValueElement is MyParagraph)
            {
                editor.Background = MySetup.Setup.BarvaTextBoxuOdstavceAktualni;
            }
            else
            {
                editor.Background = null;
            }
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ValueElement is MyParagraph)
            {
                editor.Background = null;
            }

        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            if (ValueElement is MyParagraph)
            {
                ((MyParagraph)ValueElement).trainingElement = true;
            }
        }

        private void checkBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ValueElement is MyParagraph)
            {
                ((MyParagraph)ValueElement).trainingElement = false;
            }
        }

        private void editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Tab || ((e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) || e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt)) && (e.Key == Key.Home || e.Key == Key.End)) || (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.Delete)) // klavesy, ktere textbox krade, posleme rucne parentu...
            {
                KeyEventArgs kea = new KeyEventArgs((KeyboardDevice)e.Device, PresentationSource.FromVisual(this), e.Timestamp, e.Key) { RoutedEvent = Element.PreviewKeyDownEvent };
                RaiseEvent(kea);
                if (!kea.Handled)
                {
                    kea.RoutedEvent = Element.KeyDownEvent;
                    RaiseEvent(kea);
                }
                e.Handled = true;
                return;
            }

            this.OnPreviewKeyDown(e);

            if (ValueElement == null)
                return;


            MyParagraph par = ValueElement as MyParagraph;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0) //pohyb v textu
            {
                if ((e.Key == Key.Up))
                {
                    int TP = editor.TextArea.Document.GetLocation(editor.SelectionStart + editor.SelectionLength).Line;
                    if (TP == 1 && MoveUpRequest != null)
                    {
                        var vl = editor.TextArea.TextView.GetVisualLine(1);
                        var tl = vl.GetTextLine(editor.TextArea.Caret.VisualColumn);
                        if (tl == vl.TextLines[0])
                        {
                            MoveUpRequest(this, new EventArgs());
                            e.Handled = true;
                        }
                    }
                }
                else if ((e.Key == Key.Down))
                {
                    int TP = editor.TextArea.Document.GetLocation(editor.SelectionStart + editor.SelectionLength).Line;
                    if (TP == editor.Document.LineCount && MoveDownRequest != null)
                    {
                        var vl = editor.TextArea.TextView.GetVisualLine(TP);
                        var tl = vl.GetTextLine(editor.TextArea.Caret.VisualColumn);
                        if (tl == vl.TextLines[vl.TextLines.Count - 1])
                        {
                            MoveDownRequest(this, new EventArgs());
                            e.Handled = true;
                        }
                    }
                }
                else if (e.Key == Key.Right)
                {
                    if (editor.CaretOffset == editor.Document.TextLength && MoveRightRequest != null)
                    {
                        MoveRightRequest(this, new EventArgs());
                        e.Handled = true;
                    }

                }
                else if (e.Key == Key.Left)
                {
                    if (editor.CaretOffset == 0 && MoveLeftRequest != null)
                    {
                        MoveLeftRequest(this, new EventArgs());
                        e.Handled = true;
                    }
                }

                if (e.Handled)
                    return;
            }

            if (!(ValueElement is MyParagraph) && ValueElement !=null)
            {

                ValueElement.Text = editor.Text;
                return;
            }


            if (e.Key == Key.Back && editor.SelectionLength == 0)
            {
                if (editor.CaretOffset == 0 && MergeWithPreviousRequest != null)
                    MergeWithPreviousRequest(this, new EventArgs());

            }
            else if (e.Key == Key.Delete && editor.SelectionLength == 0)
            {
                if (editor.CaretOffset == editor.Document.TextLength && MergeWithnextRequest != null)
                    MergeWithnextRequest(this, new EventArgs());

            }
            else if (e.Key == Key.Enter || e.Key==Key.Return)
            {
                if (editor.SelectionLength == 0)
                {
                    if (editor.CaretOffset == editor.Document.TextLength && editor.CaretOffset != 0 && NewRequest != null)
                        NewRequest(this, new EventArgs());
                    else if (editor.CaretOffset != 0 & SplitRequest != null)
                        SplitRequest(this, new EventArgs());
                }
                e.Handled = true;
            }

        }

        private void editor_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageUp || e.Key == Key.PageDown) // klavesy, ktere textbox krade, posleme rucne parentu...
            {
                KeyEventArgs kea = new KeyEventArgs((KeyboardDevice)e.Device, PresentationSource.FromVisual(this), e.Timestamp, e.Key) { RoutedEvent = Element.PreviewKeyUpEvent };
                RaiseEvent(kea);
                if (!kea.Handled)
                {
                    kea.RoutedEvent = Element.KeyUpEvent;
                    RaiseEvent(kea);
                }
                return;
            }
        }


        public event EventHandler MoveUpRequest;
        public event EventHandler MoveDownRequest;

        public event EventHandler MoveRightRequest;
        public event EventHandler MoveLeftRequest;

        public event EventHandler MergeWithPreviousRequest;
        public event EventHandler MergeWithnextRequest;
        public event EventHandler SplitRequest;
        public event EventHandler NewRequest;

        public event EventHandler ChangeSpeakerRequest;
        public event EventHandler ContentChanged;
        public event Action<TimeSpan> SetTimeRequest;

        public void ClearEvents()
        {
            MoveUpRequest = null;
            MoveDownRequest = null;

            MoveRightRequest = null;
            MoveLeftRequest = null;

            MergeWithPreviousRequest = null;
            MergeWithnextRequest = null;
            SplitRequest = null;
            NewRequest = null;
            MoveDownRequest = null;
            ChangeSpeakerRequest = null;
            SetTimeRequest = null;
            ContentChanged = null;
        }

        public void ClearBindings()
        {
            BindingOperations.ClearAllBindings(this);
            BindingOperations.ClearAllBindings(maingrid);
            BindingOperations.ClearAllBindings(textbegin);
            BindingOperations.ClearAllBindings(textend);
            BindingOperations.ClearAllBindings(editor);
            BindingOperations.ClearAllBindings(grid2);
            BindingOperations.ClearAllBindings(button1);
        }

        void Document_Changed(object sender, DocumentChangeEventArgs e)
        {
            if (ValueElement == null || updating)
                return;

            if (ContentChanged != null)
                ContentChanged(this, null);

            if (!(ValueElement is MyParagraph))
            {
                ValueElement.Text = editor.Text;
                return;
            }

            MyParagraph par = ValueElement as MyParagraph;
            string text = editor.Text;
            int offset = e.Offset;
            int removedl = e.RemovalLength;
            int addedl = e.InsertionLength;

            if (removedl > 0)
            {
                List<MyPhrase> todelete = new List<MyPhrase>();
                int pos = 0;
                foreach (MyPhrase p in par.Phrases)
                {
                    
                    string etext = (EditPhonetics ? p.Phonetics : p.Text);
                    int len = etext.Length;

                    if (pos + len > offset) //konec fraze je za zacatkem mazani
                    {
                        int iidx = offset - pos;
                        if (removedl > len - iidx) //odmazani textu pokracuje i za aktualni frazi
                        {
                            
                            if (iidx == 0) //mazeme celou frazi
                            {
                                removedl -= len;
                                offset = pos + len;
                                todelete.Add(p);
                            }
                            else //zkracujeme frazy
                            {

                                string s = etext.Remove(iidx);
                                if (EditPhonetics)
                                    p.Phonetics = s;
                                else
                                    p.Text = s;

                                removedl -= len-s.Length;
                                offset = pos + s.Length;


                            }
                        }else if(len == removedl) //maze se presne 1 fraze
                        {
                            removedl -= len;
                            offset = pos + len;
                            if (addedl <= 0)
                                todelete.Add(p);
                            else //v pripade replace
                            {
                                if (EditPhonetics)
                                    p.Phonetics = "";
                                else
                                    p.Text = "";
                                offset -= etext.Length; ;
                            }
                            break;
                        }
                        else//odmazani konci ve frazi
                        {
                            string s = (EditPhonetics ? p.Phonetics : p.Text).Remove(iidx, removedl);
                            if (EditPhonetics)
                                p.Phonetics = s;
                            else
                                p.Text = s;
                            break;
                            
                        }
                    }

                    pos += (EditPhonetics ? p.Phonetics.Length : p.Text.Length);
                }

                foreach (var v in todelete)
                    par.Children.Remove(v); //bezeventovy mazani; nedojde k prekresleni celeho seznamu elementu

            }
            offset = e.Offset;
            if (addedl > 0)
            {
                int pos = 0;
                foreach (MyPhrase p in par.Phrases)
                {
                    if (offset <= pos + (EditPhonetics ? p.Phonetics.Length : p.Text.Length)) //vlozeni
                    {
                        string s = (EditPhonetics ? p.Phonetics : p.Text).Insert(offset - pos, text.Substring(offset, addedl));
                        if (EditPhonetics)
                            p.Phonetics = s;
                        else
                            p.Text = s;
                        break;
                    }
                    pos += (EditPhonetics ? p.Phonetics.Length : p.Text.Length);
                }
            }

            if (par.Text != editor.Text)
            {
              editor.Background = Brushes.Red;
            }

        }

        private void element_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_forceCarretpositionOnLoad >= 0)
            {
                editor.Focus();
                internal_setCarretOffset(m_forceCarretpositionOnLoad, m_forcesellength);
            }
        }

        private void internal_setCarretOffset(int offset, int length)
        {
            if (offset < 0)
                return;
            if (offset > editor.Text.Length)
                offset = editor.Text.Length;
            editor.CaretOffset = offset;
            if (length > 0 && length+offset < editor.Text.Length)
            { 
                editor.Select(offset,length);
            }
        
        }

        int m_forcesellength = 0;
        //int m_forceselbegin = 0;
        public void SetSelection(int offset, int length, int carretoffset)
        {
            m_forceCarretpositionOnLoad = -1;
            m_forcesellength = 0;
            if (offset < 0)
                return;
            if (IsLoaded)
            {
                if (!editor.IsFocused)
                    editor.Focus();

                if (offset <= editor.Document.TextLength)
                    internal_setCarretOffset(offset, length);
                else
                    internal_setCarretOffset(editor.Document.TextLength, m_forcesellength);
            }
            else
            {
                m_forceCarretpositionOnLoad = offset;
                m_forcesellength = length;
            }
        }

        public void SetCaretOffset(int offset)
        {
            SetSelection(offset, 0,offset);
        }

        public int TextLength
        {
            get { return editor.Document.TextLength; }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            if (ChangeSpeakerRequest != null)
                ChangeSpeakerRequest(this, new EventArgs());

            var be = BindingOperations.GetBindingExpressionBase(button1, Button.ContentProperty);
            if(be!=null)
                be.UpdateTarget();
        }

        private TimeSpan m_higlightedPosition = new TimeSpan(-1);
        public TimeSpan HiglightedPostion
        { 
            set
            {
                m_higlightedPosition = value;
                if (BackgroundHiglighter != null)
                    editor.TextArea.TextView.BackgroundRenderers.Remove(BackgroundHiglighter);
                BackgroundHiglighter = null;
                if(value < TimeSpan.Zero || ValueElement == null || !ValueElement.IsParagraph || value < ValueElement.Begin || value > ValueElement.End)
                    return;
                MyParagraph p = (MyParagraph)ValueElement;
                int pos = 0;
                foreach (MyPhrase ph in p.Phrases)
                {
                    if (ph.Begin <= value && ph.End > value)
                    {
                        BackgroundHiglighter = new PositionHighlighter(pos, (EditPhonetics) ? ph.Phonetics.Length : ph.Text.Length);
                        editor.TextArea.TextView.BackgroundRenderers.Add(BackgroundHiglighter);
                        return;
                    }
                    pos+=(EditPhonetics)?ph.Phonetics.Length:ph.Text.Length;
                }
            }

            get 
            {
                return m_higlightedPosition;
            }
        }

        public override string ToString()
        {
            return base.ToString()+":"+this.Text;
        }
    }
    public class SpellChecker : DocumentColorizingTransformer
    {
        static TextDecorationCollection defaultdecoration;

        //nacteni celeho slovniku z souboru do hash tabulky
        static NHunspell.Hunspell spell = null;


        public static NHunspell.Hunspell SpellEngine
        {
            get
            {
                return spell;
            }
            set
            {
                spell = null;
            }
        }

        public static bool LoadVocabulary()
        {
            string faff = FilePaths.GetReadPath(@"\data\cs_CZ.aff");
            string fdic = FilePaths.GetReadPath(@"\data\cs_CZ.dic");
            if (File.Exists(faff) && File.Exists(fdic))
            {
                spell = new NHunspell.Hunspell(faff, fdic);
                return true;
            }

            return false;
        }

        public static HashSet<string> Vocabulary = new HashSet<string>();

        static SpellChecker()
        {
            TextDecorationCollection tdc = new TextDecorationCollection();

            StreamGeometry g = new StreamGeometry();
            using (var context = g.Open())
            {
                context.BeginFigure(new Point(0, 1), false, false);
                context.BezierTo(new Point(1, 0), new Point(2, 2), new Point(3, 1), true, true);
            }

            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path() { Data = g, Stroke = Brushes.Red, StrokeThickness = 0.25, StrokeEndLineCap = PenLineCap.Square, StrokeStartLineCap = PenLineCap.Square };

            VisualBrush vb = new VisualBrush(p)
            {
                Viewbox = new Rect(0, 0, 3, 2),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0.8, 6, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile

            };



            TextDecoration td = new TextDecoration()
            {
                Location = TextDecorationLocation.Underline,
                Pen = new Pen(vb, 4),
                PenThicknessUnit = TextDecorationUnit.Pixel,
                PenOffsetUnit = TextDecorationUnit.Pixel

            };
            tdc.Add(td);

            defaultdecoration = tdc;
        }


        protected override void ColorizeLine(DocumentLine line)
        {
            if (spell == null)
                return;
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            MatchCollection matches = Element.wordSplitter.Matches(text, 0);
            foreach (Match m in matches)
            {
                string s = text.Substring(m.Index, m.Length).ToLower();

                if (!Element.wordIgnoreChecker.IsMatch(s) && !spell.Spell(s))
                {
                    base.ChangeLinePart(
                        lineStartOffset + m.Index,
                        lineStartOffset + m.Index + m.Length,
                        (VisualLineElement element) =>
                        {
                            element.TextRunProperties.SetTextDecorations(defaultdecoration);

                        });
                }
            }

        }
    }

    [ValueConversion(typeof(TranscriptionElement), typeof(string))]
    public class SpeakerConverter : IValueConverter
    {
        public static MySpeaker GetSpeaker(TranscriptionElement te)
        {
            TranscriptionElement x = te;
            Type t = x.GetType();

            while (x.Parent != null && t != typeof(MySubtitlesData))
            {
                x = x.Parent;
                t = x.GetType();
            }

            MySubtitlesData sd = x as MySubtitlesData;
            int id = int.MinValue;
            if (sd != null)
            {

                if (te.GetType() == typeof(MyParagraph))
                {
                    MyParagraph par = (MyParagraph)te;
                    id = par.speakerID;
                }
                else if (te.GetType() == typeof(MySection))
                {
                    MySection sec = (MySection)te;
                    id = sec.Speaker;
                }
            }

            return sd.GetSpeaker(id);
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "";
            return GetSpeaker((TranscriptionElement)value).FullName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
