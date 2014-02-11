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
using NanoTrans.Core;
using System.ComponentModel;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Element.xaml
    /// </summary>
    public partial class Element : UserControl, INotifyPropertyChanged
    {

        private int _forceCarretpositionOnLoad = -1;
        public static readonly DependencyProperty ValueElementProperty =
        DependencyProperty.Register("ValueElement", typeof(TranscriptionElement), typeof(Element), new FrameworkPropertyMetadata(OnValueElementChanged));

        public bool DisableAutomaticElementVisibilityChanges { get; set; }
        public bool EditPhonetics { get; set; }

        public string ElementLanguage
        {
            get
            {
                if (ValueElement != null && ValueElement.IsParagraph)
                {
                    return ((TranscriptionParagraph)ValueElement).Language;
                }
                else
                    return "";
            }

            set
            {
                if (ValueElement.IsParagraph)
                {
                    ((TranscriptionParagraph)ValueElement).Language = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("ElementLanguage"));
                }
            }

        }

        public bool RefreshSpeakerButton()
        {
            var res = Element.RefreshSpeakerButton(this, this.ValueElement as TranscriptionParagraph);

            var be = BindingOperations.GetBindingExpressionBase(buttonSpeaker, Button.ContentProperty);
            if (be != null)
                be.UpdateTarget();

            return res;
        }

        private static bool RefreshSpeakerButton(Element el, TranscriptionParagraph val)
        {
            if (val == null)
                return false;
            var currentvis = el.buttonSpeaker.Visibility;
            var setvis = Visibility.Visible;
            var previous = val.PreviousSibling() as TranscriptionParagraph;
            if (previous != null && val != null)
                if (val.Speaker == previous.Speaker && val.Language == previous.Language)
                    setvis = Visibility.Collapsed;

            if (currentvis != setvis)
            {
                el.buttonSpeaker.Visibility = setvis;
                return true;
            }

            return false;
        }


        public static void OnValueElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            TranscriptionElement val = (TranscriptionElement)e.NewValue;
            Element el = (Element)d;
            el._Element = val;

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


            if (t == typeof(TranscriptionParagraph))
            {
                TranscriptionParagraph p = (TranscriptionParagraph)val;
                el.textbegin.Visibility = Visibility.Visible;
                el.textend.Visibility = Visibility.Visible;
                el.stackPanelAttributes.Visibility = Visibility.Visible;
                el.Background = MySetup.Setup.BarvaTextBoxuOdstavce;
                Element.RefreshSpeakerButton(el, p);
                el.checkBox1.Visibility = Visibility.Visible;
                el.checkBox1.IsChecked = ((TranscriptionParagraph)val).trainingElement;
            }
            else if (t == typeof(TranscriptionSection))
            {
                TranscriptionSection s = (TranscriptionSection)val;
                el.textbegin.Visibility = Visibility.Collapsed;
                el.textend.Visibility = Visibility.Collapsed;
                el.stackPanelAttributes.Visibility = Visibility.Collapsed;
                el.Background = MySetup.Setup.SectionBackground;
                el.buttonSpeaker.Visibility = Visibility.Collapsed;
                el.checkBox1.Visibility = Visibility.Collapsed;
            }
            else if (t == typeof(TranscriptionChapter))
            {
                TranscriptionChapter c = (TranscriptionChapter)val;
                el.textbegin.Visibility = Visibility.Collapsed;
                el.textend.Visibility = Visibility.Collapsed;
                el.stackPanelAttributes.Visibility = Visibility.Collapsed;
                el.Background = MySetup.Setup.BarvaTextBoxuKapitoly;
                el.buttonSpeaker.Visibility = Visibility.Collapsed;
                el.checkBox1.Visibility = Visibility.Collapsed;
            }
            else
            {
                el.textbegin.Visibility = Visibility.Visible;
                el.textend.Visibility = Visibility.Visible;
                el.stackPanelAttributes.Visibility = Visibility.Visible;
                el.Background = MySetup.Setup.BarvaTextBoxuOdstavce;
                el.buttonSpeaker.Visibility = Visibility.Visible;
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

            el.UpdateCustomParamsFromSpeaker();
            el.updating = false;
        }

        TranscriptionElement _Element;
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

        static Brush GetRectangleBgColor(ParagraphAttributes param)
        {
            return Brushes.White;
        }

        static Brush GetRectangleInnenrColor(ParagraphAttributes param)
        {
            switch (param)
            {
                default:
                case ParagraphAttributes.None:
                    return Brushes.White;
                case ParagraphAttributes.Background_noise:
                    return Brushes.DodgerBlue;
                case ParagraphAttributes.Background_speech:
                    return Brushes.Chocolate;
                case ParagraphAttributes.Junk:
                    return Brushes.Crimson;
                case ParagraphAttributes.Narrowband:
                    return Brushes.Olive;
            }
        }
        static ParagraphAttributes[] all = (ParagraphAttributes[])Enum.GetValues(typeof(ParagraphAttributes));

        private void RepaintAttributes()
        {
            if (_IsPasiveElement)
                return;
            if (this.stackPanelAttributes.Children.Count != all.Length)
            {
                this.stackPanelAttributes.Children.Clear();
                foreach (ParagraphAttributes at in all)
                {
                    if (at != ParagraphAttributes.None)
                    {
                        string nam = Enum.GetName(typeof(ParagraphAttributes), at);

                        Rectangle r = new Rectangle();
                        r.Stroke = Brushes.Green;
                        r.Width = 10;
                        r.Height = 8;
                        r.ToolTip = nam;
                        r.Margin = new Thickness(0, 0, 0, 1);

                        TranscriptionParagraph par = ValueElement as TranscriptionParagraph;
                        if (par != null && (par.DataAttributes & at) != 0)
                            r.Fill = GetRectangleInnenrColor(at);
                        else
                            r.Fill = GetRectangleBgColor(at);
                        r.MouseLeftButtonDown += new MouseButtonEventHandler(attributes_MouseLeftButtonDown);
                        r.Tag = at;
                        stackPanelAttributes.Children.Add(r);
                    }
                }
            }
        }

        void attributes_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //int i = ((sender as Rectangle).Parent as StackPanel).Children.IndexOf(sender as UIElement) + 1;
            TranscriptionParagraph par = ValueElement as TranscriptionParagraph;
            if (par != null)
            {
                ParagraphAttributes attr = (ParagraphAttributes)(sender as Rectangle).Tag;

                par.DataAttributes ^= attr;

                foreach (Rectangle r in ((sender as Rectangle).Parent as StackPanel).Children)
                {
                    attr = (ParagraphAttributes)r.Tag;
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


        public Element()
            : this(false)
        {

        }



        private bool _IsPasiveElement;
        /// <summary>
        /// creates element, 
        /// </summary>
        /// <param name="isPasiveElement"></param>
        public Element(bool isPasiveElement)
        {
            _IsPasiveElement = isPasiveElement;
            InitializeComponent();

            if (!_IsPasiveElement)
                RepaintAttributes();


            editor.TextArea.TextView.ElementGenerators.Add(DefaultNonEditableBlockGenerator);
            editor.TextArea.IndentationStrategy = new NoIndentationStrategy();
            editor.Options.InheritWordWrapIndentation = false;

            if (!_IsPasiveElement)
            {
                editor.TextArea.TextView.LineTransformers.Add(DefaultSpellchecker);
                editor.TextArea.TextEntering += new TextCompositionEventHandler(TextArea_TextEntering);
                editor.TextArea.TextEntered += new TextCompositionEventHandler(TextArea_TextEntered);
                editor.Document.Changed += new EventHandler<DocumentChangeEventArgs>(Document_Changed);

                editor.TextArea.TextView.MouseLeftButtonDown += new MouseButtonEventHandler(TextView_MouseLeftButtonDown);
                editor.TextArea.TextView.MouseDown += TextView_MouseDown;
                
            }
        }

        static List<string[]> customparams = new List<string[]>()
        {
            new []{"channeltype", "mic",  "unknown", "tel", "other" },
            new []{"sessiontype", "news",  "unknown", "prescribed", "interview", "other" },
            new []{"quality", "good",  "unknown", "mediocre", "bad" },
            new []{"conditions", "clean",  "unknown", "music", "babble", "tech", "xtalk", "other" }

        };

        private void AddCustomParams()
        {
            foreach (var item in customparams)
            {
                var cb = new ComboBox();
                cb.ItemsSource = item.Skip(1).ToArray();
                cb.SelectedIndex = 0;
                CustomParams.Children.Add(cb);
                cb.SelectionChanged += cb_SelectionChanged;
                cb.ToolTip = item[0];
                cb.Width = 65;
                var par = (ValueElement as TranscriptionParagraph);
                if (par != null)
                {
                    string outval;
                    if(par.Elements.TryGetValue(item[0],out outval))
                        cb.SelectedItem = outval;
                    else
                        par.Elements.Add(item[0],item[1]);
                }
            }
        }

        private void UpdateCustomParamsFromSpeaker()
        {
            if (customparams == null || customparams.Count == 0)
                return;

            CustomParams.Children.Clear();
            if (!ValueElement.IsParagraph)
                return;
            AddCustomParams();
            
        }

        void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb != null && ValueElement.IsParagraph)
            {
                ((TranscriptionParagraph)ValueElement).Elements[(string)cb.ToolTip] = (string)cb.SelectedItem;
            }
        }

        private void TextView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
                return;

            if (e.ChangedButton != MouseButton.Middle)
                return;

            e.Handled = true;
            RaiseCorrector(editor.TextArea);
        }

        private void RaiseCorrector(TextArea ta)
        {
            if (ta != null)
            {
                if (ta.Selection.Length <= 0)
                    return;
                string selection = ta.Document.Text.Substring(ta.Selection.SurroundingSegment.Offset, ta.Selection.Length);

                if (selection.Length == 0)
                    return;

                var corrs = CorrectionsGenerator.GetCorrections(selection);
                if (corrs.Count() <= 1)
                    return;

                completionWindow = new CompletionWindow(editor.TextArea);

                completionWindow.ResizeMode = ResizeMode.NoResize;

                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                foreach (string s in corrs)
                    data.Add(new CodeCompletionDataCorretion(s));



                Visual target = completionWindow;    // Target element
                var routedEvent = Keyboard.KeyDownEvent; // Event to send
                bool closenotcorrect = false;
                completionWindow.KeyDown += (object sender, KeyEventArgs e) =>
                {


                    if (e.Key == Key.Q)
                    {
                        e.Handled = true;
                        completionWindow.RaiseEvent(
                          new KeyEventArgs(
                            Keyboard.PrimaryDevice,
                            PresentationSource.FromVisual(target),
                            0,
                            Key.Up) { RoutedEvent = routedEvent }
                        );

                    }
                    else if (e.Key == Key.W)
                    {
                        e.Handled = true;
                        completionWindow.RaiseEvent(
                          new KeyEventArgs(
                            Keyboard.PrimaryDevice,
                            PresentationSource.FromVisual(target),
                            0,
                            Key.Down) { RoutedEvent = routedEvent }
                        );
                    }
                    else if (e.Key == Key.Escape)
                    {
                        closenotcorrect = true;
                    }
                };


                completionWindow.Show();
                completionWindow.Focus();

                completionWindow.LostKeyboardFocus +=
                (sender, e) =>
                {
                    if (completionWindow == null)
                        return;

                    if (!completionWindow.IsKeyboardFocusWithin && !closenotcorrect && completionWindow != null && completionWindow.CompletionList.SelectedItem != null)
                    {
                        completionWindow.RaiseEvent(
                              new KeyEventArgs(
                                Keyboard.PrimaryDevice,
                                PresentationSource.FromVisual(target),
                                0,
                                Key.Enter) { RoutedEvent = routedEvent }
                            );
                    }
                };

                completionWindow.Closing += delegate
                {
                    completionWindow = null;
                };
            }
        }





        void TextView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                var t = editor.TextArea.TextView.GetPosition(e.GetPosition(editor));
                if (!t.HasValue)
                    return;
                int pos = editor.Document.GetLineByNumber(t.Value.Line).Offset + t.Value.Column;
                if (t.HasValue && ValueElement != null && ValueElement.IsParagraph)
                {
                    TranscriptionParagraph p = (TranscriptionParagraph)ValueElement;
                    int ps = 0;
                    foreach (var ph in p.Phrases)
                    {
                        ps += EditPhonetics ? ph.Phonetics.Length : ph.Text.Length;
                        if (ps > pos)
                        {
                            if (SetTimeRequest != null)
                                SetTimeRequest(ph.Begin);
                            editor.TextArea.Caret.Location = t.Value.Location;
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
            int _from;
            int _len;
            Pen border;
            public PositionHighlighter(int from, int len)
            {
                _from = from;
                _len = len;
                border = new Pen(Brushes.ForestGreen, 1);
                border.Freeze();
            }

            #region IBackgroundRenderer Members

            public void Draw(TextView textView, DrawingContext drawingContext)
            {
                BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();

                builder.CornerRadius = 1;
                builder.AlignToMiddleOfPixels = true;

                builder.AddSegment(textView, new TextSegment() { StartOffset = _from, Length = _len });
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
                EndChanged(this, null);//zmena zacatku muze ovlivnit konec...
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

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            if (ValueElement is TranscriptionParagraph)
            {
                ((TranscriptionParagraph)ValueElement).trainingElement = true;
            }
        }

        private void checkBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ValueElement is TranscriptionParagraph)
            {
                ((TranscriptionParagraph)ValueElement).trainingElement = false;
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


            TranscriptionParagraph par = ValueElement as TranscriptionParagraph;


            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && completionWindow == null) //TODO: vyhodit korekce ven
            {
                if (e.Key == Key.Q || e.Key == Key.W)
                {
                    RaiseCorrector(editor.TextArea);
                    return;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0) //pohyb v textu
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
            else if (e.Key == Key.Enter || e.Key == Key.Return)
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
            BindingOperations.ClearAllBindings(buttonSpeaker);
        }

        void Document_Changed(object sender, DocumentChangeEventArgs e)
        {
            if (ValueElement == null || updating)
                return;

            if (ContentChanged != null)
                ContentChanged(this, null);

            if (!(ValueElement is TranscriptionParagraph))
            {
                ValueElement.Text = editor.Text;
                return;
            }

            TranscriptionParagraph par = ValueElement as TranscriptionParagraph;
            string text = editor.Text;
            int offset = e.Offset;
            int removedl = e.RemovalLength;
            int addedl = e.InsertionLength;

            if (removedl > 0)
            {
                List<TranscriptionPhrase> todelete = new List<TranscriptionPhrase>();
                int pos = 0;
                foreach (TranscriptionPhrase p in par.Phrases)
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

                                removedl -= len - s.Length;
                                offset = pos + s.Length;


                            }
                        }
                        else if (len == removedl) //maze se presne 1 fraze
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
                if (par.Phrases.Count > 0)
                {
                    foreach (TranscriptionPhrase p in par.Phrases)
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
                else
                {
                    var phr = new TranscriptionPhrase() { Text = text };
                    par.BeginUpdate();
                    par.Phrases.Add(phr);
                    par.SilentEndUpdate();
                }
            }

            if (par.Text != editor.Text)
            {
                editor.Background = Brushes.Red;
            }

        }

        private void element_Loaded(object sender, RoutedEventArgs e)
        {
            if (_forceCarretpositionOnLoad >= 0)
            {
                editor.Focus();
                internal_setCarretOffset(_forceCarretpositionOnLoad, _forcesellength);
            }
        }

        private void internal_setCarretOffset(int offset, int length)
        {
            if (offset < 0)
                return;
            if (offset > editor.Text.Length)
                offset = editor.Text.Length;
            editor.CaretOffset = offset;
            if (length > 0 && length + offset < editor.Text.Length)
            {
                editor.Select(offset, length);
            }

        }

        int _forcesellength = 0;
        //int _forceselbegin = 0;
        public void SetSelection(int offset, int length, int carretoffset)
        {
            _forceCarretpositionOnLoad = -1;
            _forcesellength = 0;
            if (offset < 0)
                return;
            if (IsLoaded)
            {
                if (!editor.IsFocused)
                    editor.Focus();

                if (offset <= editor.Document.TextLength)
                    internal_setCarretOffset(offset, length);
                else
                    internal_setCarretOffset(editor.Document.TextLength, _forcesellength);
            }
            else
            {
                _forceCarretpositionOnLoad = offset;
                _forcesellength = length;
            }
        }

        public void SetCaretOffset(int offset)
        {
            SetSelection(offset, 0, offset);
        }

        public int TextLength
        {
            get { return editor.Document.TextLength; }
        }

        private void ButtonSpeaker_Click(object sender, RoutedEventArgs e)
        {

            if (ChangeSpeakerRequest != null)
                ChangeSpeakerRequest(this, new EventArgs());
        }

        private TimeSpan _higlightedPosition = new TimeSpan(-1);
        public TimeSpan HiglightedPostion
        {
            set
            {
                _higlightedPosition = value;
                if (BackgroundHiglighter != null)
                    editor.TextArea.TextView.BackgroundRenderers.Remove(BackgroundHiglighter);
                BackgroundHiglighter = null;
                if (value < TimeSpan.Zero || ValueElement == null || !ValueElement.IsParagraph || value < ValueElement.Begin || value > ValueElement.End)
                    return;
                TranscriptionParagraph p = (TranscriptionParagraph)ValueElement;
                int pos = 0;
                foreach (TranscriptionPhrase ph in p.Phrases)
                {
                    if (ph.Begin <= value && ph.End > value)
                    {
                        BackgroundHiglighter = new PositionHighlighter(pos, (EditPhonetics) ? ph.Phonetics.Length : ph.Text.Length);
                        editor.TextArea.TextView.BackgroundRenderers.Add(BackgroundHiglighter);
                        return;
                    }
                    pos += (EditPhonetics) ? ph.Phonetics.Length : ph.Text.Length;
                }
            }

            get
            {
                return _higlightedPosition;
            }
        }

        public override string ToString()
        {
            return base.ToString() + ":" + this.Text;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    public class SpellChecker : DocumentColorizingTransformer
    {
        static TextDecorationCollection defaultdecoration;
        static TextDecorationCollection defaultdecorationSuggestion;

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
            string faff = FilePaths.GetReadPath(@"data\cs_CZ.aff");
            string fdic = FilePaths.GetReadPath(@"data\cs_CZ.dic");
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
                context.BeginFigure(new Point(0, 2), false, false);
                context.BezierTo(new Point(2, 0), new Point(4, 4), new Point(6, 2), true, true);
            }

            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path() { Data = g, Stroke = Brushes.Red, StrokeThickness = 0.5, StrokeEndLineCap = PenLineCap.Square, StrokeStartLineCap = PenLineCap.Square };

            VisualBrush vb = new VisualBrush(p)
            {
                Viewbox = new Rect(0, 0, 6, 4),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, 6, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile

            };



            TextDecoration td = new TextDecoration()
            {
                Location = TextDecorationLocation.Underline,
                Pen = new Pen(vb, 3),
                PenThicknessUnit = TextDecorationUnit.Pixel,
                PenOffsetUnit = TextDecorationUnit.Pixel

            };
            tdc.Add(td);

            defaultdecoration = tdc;



            tdc = new TextDecorationCollection();

            g = new StreamGeometry();
            using (var context = g.Open())
            {
                context.BeginFigure(new Point(0, 2), false, false);
                context.BezierTo(new Point(2, 0), new Point(4, 4), new Point(6, 2), true, true);
                context.Close();
            }

            p = new System.Windows.Shapes.Path() { Data = g, Stroke = Brushes.Green, StrokeThickness = 0.5, StrokeEndLineCap = PenLineCap.Square, StrokeStartLineCap = PenLineCap.Square };

            vb = new VisualBrush(p)
           {
               Viewbox = new Rect(0, 0, 6, 4),
               ViewboxUnits = BrushMappingMode.Absolute,
               Viewport = new Rect(0, 0, 6, 4),
               ViewportUnits = BrushMappingMode.Absolute,
               TileMode = TileMode.Tile

           };



            td = new TextDecoration()
           {
               Location = TextDecorationLocation.Underline,
               Pen = new Pen(vb, 3),
               PenThicknessUnit = TextDecorationUnit.Pixel,
               PenOffsetUnit = TextDecorationUnit.Pixel

           };
            tdc.Add(td);

            defaultdecorationSuggestion = tdc;
        }


        public static bool Checkword(string word)
        {
            if (spell == null)
                return true;

            if (!Element.wordIgnoreChecker.IsMatch(word) && !spell.Spell(word))
            {
                return false;
            }

            return true;
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

                if (CorrectionsGenerator.GetCorrections(s).Take(2).Count() > 1)
                {
                    base.ChangeLinePart(
                            lineStartOffset + m.Index,
                            lineStartOffset + m.Index + m.Length,
                            (VisualLineElement element) =>
                            {
                                element.TextRunProperties.SetTextDecorations(defaultdecorationSuggestion);

                            });
                }
                else if (!Checkword(s))
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
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TranscriptionParagraph)
                return (((TranscriptionParagraph)value).Speaker ?? Speaker.DefaultSpeaker).FullName;

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
