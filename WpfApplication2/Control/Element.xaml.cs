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
using System.ComponentModel;
using NanoTrans.Properties;
using TranscriptionCore;
using WeCantSpell.Hunspell;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Element.xaml
    /// </summary>
    public partial class Element : UserControl, INotifyPropertyChanged, IDisposable
    {

        private int _forceCaretpositionOnLoad = -1;
        public static readonly DependencyProperty ValueElementProperty =
        DependencyProperty.Register("ValueElement", typeof(TranscriptionElement), typeof(Element), new FrameworkPropertyMetadata(OnValueElementChanged));


        public bool DisableAutomaticElementVisibilityChanges { get; set; }
        public bool EditPhonetics { get; set; }

        public string ElementLanguage
        {
            get
            {
                if (ValueElement is TranscriptionParagraph par)
                {
                    return par.Language;
                }
                else
                    return "";
            }

            set
            {
                if (ValueElement is TranscriptionParagraph par)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElementLanguage)));
                }
            }

        }

        /// <summary>
        /// redraw all ui elements depending on speaker
        /// </summary>
        /// <returns>true if button visibility changed</returns>
        public bool RefreshSpeakerInfos()
        {
            var res = Element.RefreshSpeakerButton(this, this.ValueElement as TranscriptionParagraph);

            var be = BindingOperations.GetBindingExpressionBase(buttonSpeaker, Button.ContentProperty);
            if (be is { })
                be.UpdateTarget();

            be = BindingOperations.GetBindingExpressionBase(textBlockLanguage, TextBlock.TextProperty);
            if (be is { })
                be.UpdateTarget();

            return res;
        }

        private static bool RefreshSpeakerButton(Element el, TranscriptionParagraph val)
        {
            if (val is null)
                return false;

            var currentvis = el.buttonSpeaker.Visibility;
            var setvis = Visibility.Visible;
            if (val?.PreviousSibling() is TranscriptionParagraph previous)
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

            Element el = (Element)d;
            TranscriptionElement val = (TranscriptionElement)e.NewValue;

            if (el.EditPhonetics && !val.IsParagraph)
                val = null;

            if (e.OldValue is { })
                ((TranscriptionElement)e.OldValue).ContentChanged -= el.val_ContentChanged;


            el.IsEnabled = val != null;


            el.updating = true;

            el.Text = (el.EditPhonetics) ? val.Phonetics : val.Text;
            el.Text ??= "";

            el.updating = false;

            if (el.DisableAutomaticElementVisibilityChanges)
                return;


            Type t = e.NewValue?.GetType();

            if (t == typeof(TranscriptionParagraph))
            {
                TranscriptionParagraph p = (TranscriptionParagraph)val;
                el.textbegin.Visibility = Visibility.Visible;
                el.textend.Visibility = Visibility.Visible;

                Element.RefreshSpeakerButton(el, p);
                var vis = (Settings.Default.FeatureEnabler.SpeakerAttributes) ? Visibility.Visible : Visibility.Collapsed;

                el.stackPanelAttributes.Visibility = vis;
                el.checkBox1.Visibility = vis;
                el.checkBox1.IsChecked = ((TranscriptionParagraph)val).trainingElement;

            }
            else if (t == typeof(TranscriptionSection))
            {
                if (!Settings.Default.FeatureEnabler.ChaptersAndSections)
                    el.Visibility = Visibility.Collapsed;

                TranscriptionSection s = (TranscriptionSection)val;
                el.textbegin.Visibility = Visibility.Collapsed;
                el.textend.Visibility = Visibility.Collapsed;
                el.stackPanelAttributes.Visibility = Visibility.Collapsed;
                el.buttonSpeaker.Visibility = Visibility.Collapsed;
                el.checkBox1.Visibility = Visibility.Collapsed;
            }
            else if (t == typeof(TranscriptionChapter))
            {
                if (!Settings.Default.FeatureEnabler.ChaptersAndSections)
                    el.Visibility = Visibility.Collapsed;
                TranscriptionChapter c = (TranscriptionChapter)val;
                el.textbegin.Visibility = Visibility.Collapsed;
                el.textend.Visibility = Visibility.Collapsed;
                el.stackPanelAttributes.Visibility = Visibility.Collapsed;
                el.buttonSpeaker.Visibility = Visibility.Collapsed;
                el.checkBox1.Visibility = Visibility.Collapsed;
            }
            else
            {
                el.textbegin.Visibility = Visibility.Visible;
                el.textend.Visibility = Visibility.Visible;
                el.stackPanelAttributes.Visibility = Visibility.Visible;
                el.buttonSpeaker.Visibility = Visibility.Visible;
                el.checkBox1.Visibility = Visibility.Visible;
                el.checkBox1.IsChecked = false;
            }

            el.DataContext = val;
            if (val is { })
            {
                val.ContentChanged += el.val_ContentChanged;

                el.BeginChanged(el, null);
                el.EndChanged(el, null);

                el.RepaintAttributes();
            }

            el.UpdateCustomParamsFromSpeaker();
            el.updating = false;
        }

        void val_ContentChanged(object sender, TranscriptionElement.TranscriptionElementChangedEventArgs e)
        {
            Element el = this;

            //phrase changes
            if (el.ValueElement is TranscriptionParagraph && e.ActionsTaken.Any(a => a.ChangedElement.Parent is { } prnt && prnt == el.ValueElement))
                el.TextualContentChanged();

            if (el?.ValueElement is null || !e.ActionsTaken.Any(a => a.ChangedElement == el.ValueElement))
                return;

            if (e.ActionsTaken.Any(a => a is TextAction))
                el.TextualContentChanged();

            if (e.ActionsTaken.Any(a => a is BeginAction))
                el.BeginChanged(el, new EventArgs());

            if (e.ActionsTaken.Any(a => a is BeginAction))
                el.BeginChanged(el, new EventArgs());

            if (e.ActionsTaken.Any(a => a is EndAction))
                el.EndChanged(el, new EventArgs());

            if (e.ActionsTaken.Any(a => a is ParagraphSpeakerAction))
                el.RefreshSpeakerInfos();

            if (e.ActionsTaken.Any(a => a is ParagraphLanguageAction))
                el.textBlockLanguage.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();

            if (e.ActionsTaken.Any(a => a is ParagraphAttibutesAction))
                el.RepaintAttributes();





        }

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

        static readonly ParagraphAttributes[] all = (ParagraphAttributes[])Enum.GetValues(typeof(ParagraphAttributes));

        private void RepaintAttributes()
        {
            if (_IsPasiveElement)
                return;

            if (ValueElement is not TranscriptionParagraph par)
                return;

            if (this.stackPanelAttributes.Children.Count != all.Length - 1)
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


                        r.Tag = at;
                        r.MouseLeftButtonDown += new MouseButtonEventHandler(attributes_MouseLeftButtonDown);
                        stackPanelAttributes.Children.Add(r);
                    }
                }
            }

            foreach (Rectangle r in this.stackPanelAttributes.Children)
            {
                var attr = (ParagraphAttributes)r.Tag;
                if ((par.DataAttributes & attr) != 0)
                    r.Fill = Settings.Default.GetPAttributeColor(attr);
                else
                    r.Fill = Settings.Default.GetPAttributeBgColor(attr);
            }

        }

        void attributes_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //int i = ((sender as Rectangle).Parent as StackPanel).Children.IndexOf(sender as UIElement) + 1;
            if (ValueElement is TranscriptionParagraph par)
            {
                ParagraphAttributes attr = (ParagraphAttributes)(sender as Rectangle).Tag;

                par.DataAttributes ^= attr;

                foreach (Rectangle r in ((sender as Rectangle).Parent as StackPanel).Children)
                {
                    attr = (ParagraphAttributes)r.Tag;
                    if ((par.DataAttributes & attr) != 0)
                    {
                        r.Fill = Settings.Default.GetPAttributeColor(attr);
                    }
                    else
                    {
                        r.Fill = Settings.Default.GetPAttributeBgColor(attr);
                    }
                }
            }
        }

        private bool updating = false;


        public Element()
            : this(false)
        {

        }



        private readonly bool _IsPasiveElement;
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


            #region remove default bindings to free shortcuts
            List<ICommand> removeCommands = new List<ICommand>()
            {
                ApplicationCommands.Undo,//ctrl+Z
                ApplicationCommands.Redo,//ctrl + Y??
                AvalonEditCommands.IndentSelection,//ctrl+I
                AvalonEditCommands.DeleteLine,//shift delete


                EditingCommands.EnterLineBreak,//enter
                EditingCommands.EnterParagraphBreak,//shift enter
                EditingCommands.TabForward,//tab
                EditingCommands.TabBackward,//shift tab

                EditingCommands.MoveUpByPage, //pgup
                EditingCommands.MoveDownByPage,//pgdown
                EditingCommands.SelectUpByPage,//shift pgup
                EditingCommands.SelectDownByPage, //shift pgdown
            };


            // var used = editor.TextArea.DefaultInputHandler.CaretNavigation.InputBindings.Select(b => "" + ((KeyBinding)b).Key + ":" + ((KeyBinding)b).Modifiers).ToArray();

            List<CommandBinding> toremove = new List<CommandBinding>();
            foreach (var binding in editor.TextArea.DefaultInputHandler.Editing.CommandBindings)
            {
                if (removeCommands.Contains(binding.Command))
                {
                    toremove.Add(binding);
                }
            }

            foreach (var binding in editor.TextArea.DefaultInputHandler.CommandBindings)
            {
                if (removeCommands.Contains(binding.Command))
                {
                    toremove.Add(binding);
                }
            }

            foreach (var item in toremove)
            {
                editor.TextArea.DefaultInputHandler.Editing.CommandBindings.Remove(item);
                editor.TextArea.DefaultInputHandler.CommandBindings.Remove(item);
            }

            editor.Document.UndoStack.SizeLimit = 0;
            #endregion

            if (Settings.Default.FeatureEnabler.NonSpeechEvents)
                editor.TextArea.TextView.ElementGenerators.Add(DefaultNonEditableBlockGenerator);
            editor.TextArea.IndentationStrategy = new NoIndentationStrategy();
            editor.Options.InheritWordWrapIndentation = false;


            if (!_IsPasiveElement)
            {
                if (Settings.Default.FeatureEnabler.Spellchecking)
                    editor.TextArea.TextView.LineTransformers.Add(DefaultSpellchecker);

                editor.TextArea.TextEntering += new TextCompositionEventHandler(TextArea_TextEntering);
                editor.TextArea.TextEntered += new TextCompositionEventHandler(TextArea_TextEntered);
                editor.Document.Changed += new EventHandler<DocumentChangeEventArgs>(Document_Changed);

                editor.TextArea.TextView.MouseLeftButtonDown += new MouseButtonEventHandler(TextView_MouseLeftButtonDown);
                editor.TextArea.PreviewMouseDown += TextArea_PreviewMouseDown;
                editor.TextArea.TextView.MouseDown += TextView_MouseDown;

            }
        }

        void TextArea_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1)
            {
                PlayPauseRequest?.Invoke(this, new EventArgs());
                e.Handled = true;
            }
        }

        static readonly List<string[]> customparams = new List<string[]>()
        {
            new []{"channeltype", "mic", "tel", "other",  "unknown" },
            new []{"sessiontype", "news", "prescribed", "interview","conversation", "other",  "unknown" },
            new []{"quality", "good", "mediocre", "bad",  "unknown" },
            new []{"conditions", "clean", "music", "babble", "tech", "xtalk", "other",  "unknown" }

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
                cb.Focusable = false;

                if (ValueElement is TranscriptionParagraph par)
                {
                    if (par.Elements.TryGetValue(item[0], out string outval))
                        cb.SelectedItem = outval;
                    else
                        par.Elements.Add(item[0], item[1]);
                }
            }
        }


        private void UpdateCustomParamsFromSpeaker()
        {
            if (!Settings.Default.ShowCustomParams)
                return;
            if (customparams.Count == 0 || ValueElement is null)
                return;

            CustomParams.Children.Clear();
            if (!ValueElement.IsParagraph)
                return;
            AddCustomParams();

        }

        void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && ValueElement is TranscriptionParagraph par)
            {
                par.Elements[(string)cb.ToolTip] = (string)cb.SelectedItem;
            }
        }

        private void TextView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.XButton2 == MouseButtonState.Pressed && e.ChangedButton == MouseButton.XButton2)
                MoveToPhraze(e);

            if (e.MiddleButton != MouseButtonState.Pressed)
                return;

            if (e.ChangedButton != MouseButton.Middle)
                return;

            e.Handled = true;
            RaiseCorrector(editor.TextArea);
        }

        private void RaiseCorrector(TextArea ta)
        {
            if (ta is null)
                return;

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
                        Key.Up)
                      { RoutedEvent = routedEvent }
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
                        Key.Down)
                      { RoutedEvent = routedEvent }
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
                if (completionWindow is null)
                    return;

                if (!completionWindow.IsKeyboardFocusWithin && !closenotcorrect && completionWindow.CompletionList.SelectedItem is { })
                {
                    completionWindow.RaiseEvent(
                          new KeyEventArgs(
                            Keyboard.PrimaryDevice,
                            PresentationSource.FromVisual(target),
                            0,
                            Key.Enter)
                          { RoutedEvent = routedEvent }
                        );
                }
            };

            completionWindow.Closing += delegate
            {
                completionWindow = null;
            };
        }

        void TextView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                MoveToPhraze(e);
            }
        }

        private void MoveToPhraze(MouseButtonEventArgs e)
        {
            var t = editor.TextArea.TextView.GetPosition(e.GetPosition(editor));
            if (!t.HasValue)
                return;
            int pos = editor.Document.GetLineByNumber(t.Value.Line).Offset + t.Value.Column;
            if (ValueElement is TranscriptionParagraph p)
            {
                int ps = 0;
                foreach (var ph in p.Phrases)
                {
                    ps += EditPhonetics ? ph.Phonetics.Length : ph.Text.Length;
                    if (ps > pos)
                    {
                        SetTimeRequest?.Invoke(ph.Begin);
                        editor.TextArea.Caret.Location = t.Value.Location;
                        e.Handled = true;
                        return;
                    }
                }
            }
            return;
        }


        public static readonly SpellChecker DefaultSpellchecker = new SpellChecker();
        public static readonly NonEditableBlockGenerator DefaultNonEditableBlockGenerator = new NonEditableBlockGenerator();

        private PositionHighlighter? BackgroundHiglighter = null;

        void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (completionWindow is null)
                return;

            if (completionWindow.CompletionList.ListBox.SelectedItem is CodeCompletionData cd)
            {
                int offset = editor.TextArea.Caret.Offset;
                string entered = editor.TextArea.Document.GetText(completionWindow.StartOffset, offset - completionWindow.StartOffset);

                if (cd.uniquepart == entered)
                    completionWindow.CompletionList.RequestInsertion(new EventArgs());
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
                foreach (string s in Settings.Default.NonSpeechEvents)
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
            readonly int _from;
            readonly int _len;
            readonly Pen border;
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

                builder.AddSegment(textView, new TextSegment() { StartOffset = _from, Length = _len });
                builder.CloseFigure(); // prevent connecting the two segments

                if (builder.CreateGeometry() is Geometry geometry)
                    drawingContext.DrawGeometry(Brushes.LightGreen, border, geometry);
            }

            public KnownLayer Layer
            {
                get { return KnownLayer.Background; }
            }

            #endregion
        }

        public class CodeCompletionData : ICompletionData
        {
            readonly object uielement = null;
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
                foreach (string s2 in Settings.Default.NonSpeechEvents)
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

                lb1.Content = uniquepart = text[..(maxcnt + 1)];
                lb2.Content = text[(maxcnt + 1)..];

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

        private void BeginChanged(object sender, EventArgs value)
        {
            if (ValueElement is null)
                return;

            TimeSpan val = ValueElement.Begin;
            this.textbegin.Text = string.Format("{0}:{1:00}:{2:00},{3}", val.Hours, val.Minutes, val.Seconds, val.Milliseconds.ToString("000")[..2]);

        }

        private void EndChanged(object sender, EventArgs value)
        {
            if (ValueElement is null)
                return;
            if (!ValueElement.IsParagraph)
                textend.Visibility = System.Windows.Visibility.Collapsed;
            else if (ValueElement.End <= ValueElement.Begin)
                textend.Visibility = System.Windows.Visibility.Hidden;
            else
                textend.Visibility = System.Windows.Visibility.Visible;

            TimeSpan val = ValueElement.End;
            this.textend.Text = string.Format("{0}:{1:00}:{2:00},{3}", val.Hours, val.Minutes, val.Seconds, val.Milliseconds.ToString("000")[..2]);
        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            if (ValueElement is TranscriptionParagraph paragraph)
                paragraph.trainingElement = true;
        }

        private void checkBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ValueElement is TranscriptionParagraph paragraph)
                paragraph.trainingElement = false;
        }

        private void editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //TODO: somehow remove the commandbindings from avalon edit, and remove this hack, some commands were found (see constructor)
            // some shortcuts are consumed either by avalonedit, there can be romved in constructor if the corresponding command is found
            // or some shortcuts can be consumed by listbox or other WPF components - there have to be removed elsewhere
            if (e.Key == Key.Tab
                || (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.Delete)
                )
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

            if (ValueElement is not TranscriptionParagraph par || e.Handled)
                return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && completionWindow is null) //TODO: vyhodit korekce ven
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
                    if (TP == 1 && MoveUpRequest is { })
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
                    if (TP == editor.Document.LineCount && MoveDownRequest is { })
                    {
                        var vl = editor.TextArea.TextView.GetVisualLine(TP);
                        var tl = vl.GetTextLine(editor.TextArea.Caret.VisualColumn);
                        if (tl == vl.TextLines[^1])
                        {
                            MoveDownRequest(this, new EventArgs());
                            e.Handled = true;
                        }
                    }
                }
                else if (e.Key == Key.Right)
                {
                    if (editor.CaretOffset == editor.Document.TextLength && MoveRightRequest is { })
                    {
                        MoveRightRequest(this, new EventArgs());
                        e.Handled = true;
                    }

                }
                else if (e.Key == Key.Left)
                {
                    if (editor.CaretOffset == 0 && MoveLeftRequest is { })
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
                if (editor.CaretOffset == 0 && MergeWithPreviousRequest is { })
                {
                    MergeWithPreviousRequest(this, new EventArgs());
                    e.Handled = true;
                }


            }
            else if (e.Key == Key.Delete && editor.SelectionLength == 0)
            {
                if (editor.CaretOffset == editor.Document.TextLength && MergeWithnextRequest is { })
                {
                    MergeWithnextRequest(this, new EventArgs());
                    e.Handled = true;
                }

            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (editor.SelectionLength == 0)
                {
                    if (editor.CaretOffset == editor.Document.TextLength && editor.CaretOffset != 0 && NewRequest is { })
                        NewRequest(this, new EventArgs());
                    else if (editor.CaretOffset != 0 & SplitRequest is { })
                        SplitRequest(this, new EventArgs());
                }
                e.Handled = true;
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
        public event Action<TimeSpan> SetTimeRequest;

        public event EventHandler PlayPauseRequest;

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


        private void TextualContentChanged()
        {
            if (_TextChangedByUser)
                return;

            updating = true;
            var cp = editor.CaretOffset;
            if (EditPhonetics)
                editor.Text = ValueElement.Phonetics;
            else
                editor.Text = ValueElement.Text;

            if (cp >= 0)
                internal_setCaretOffset(cp, 0);
            updating = false;

        }

        bool _TextChangedByUser = false;

        void Document_Changed(object sender, DocumentChangeEventArgs e)
        {
            if (ValueElement is null || updating)
                return;

            if (ValueElement is not TranscriptionParagraph)
            {
                _TextChangedByUser = true;
                ValueElement.Text = editor.Text;
                _TextChangedByUser = false;
                return;
            }

            TranscriptionParagraph par = ValueElement as TranscriptionParagraph;
            string text = editor.Text;
            int offset = e.Offset;
            int removedl = e.RemovalLength;
            int addedl = e.InsertionLength;
            _TextChangedByUser = true;
            if (removedl > 0)
            {
                List<TranscriptionPhrase> todelete = new List<TranscriptionPhrase>();
                int pos = 0;
                foreach (TranscriptionPhrase p in par.Phrases)
                {

                    string etext = (EditPhonetics ? p.Phonetics : p.Text);
                    int len = etext.Length;

                    if (pos + len > offset) //end of phraze is after the deletion
                    {
                        int iidx = offset - pos;
                        if (removedl > len - iidx) //deletion continues after current phrase
                        {

                            if (iidx == 0) //delete whole phrase
                            {
                                removedl -= len;
                                offset = pos + len;
                                todelete.Add(p);
                            }
                            else //shorten phrase
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
                        else if (len == removedl) //remove exactly 1 phrase
                        {
                            removedl -= len;
                            offset = pos + len;
                            if (addedl <= 0)
                                todelete.Add(p);
                            else //in case of replace
                            {
                                if (EditPhonetics)
                                    p.Phonetics = "";
                                else
                                    p.Text = "";
                                offset -= etext.Length; ;
                            }
                            break;
                        }
                        else//deletion ends in phrase
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

                par.BeginUpdate();
                foreach (var v in todelete) //remove phrases fprom paragraph - should not redraw anything
                    par.Remove(v);
                par.EndUpdate();

            }
            offset = e.Offset;
            if (addedl > 0)
            {
                int pos = 0;
                if (par.Phrases.Count > 0)
                {
                    foreach (TranscriptionPhrase p in par.Phrases)
                    {
                        if (offset <= pos + (EditPhonetics ? p.Phonetics.Length : p.Text.Length)) //insertion
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
                }
            }

            _TextChangedByUser = false;
        }

        private void element_Loaded(object sender, RoutedEventArgs e)
        {
            if (_forceCaretpositionOnLoad >= 0)
            {
                editor.Focus();
                internal_setCaretOffset(_forceCaretpositionOnLoad, _forcesellength);
            }
        }

        private void internal_setCaretOffset(int offset, int length)
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
        public void SetSelection(int offset, int length)
        {
            _forceCaretpositionOnLoad = -1;
            _forcesellength = 0;
            if (offset < 0)
                return;
            if (IsLoaded)
            {
                if (!editor.IsFocused)
                    editor.Focus();

                if (offset <= editor.Document.TextLength)
                    internal_setCaretOffset(offset, length);
                else
                    internal_setCaretOffset(editor.Document.TextLength, _forcesellength);
            }
            else
            {
                _forceCaretpositionOnLoad = offset;
                _forcesellength = length;
            }
        }

        public void SetCaretOffset(int offset)
        {
            SetSelection(offset, 0);
        }

        public int TextLength
        {
            get { return editor.Document.TextLength; }
        }

        private void ButtonSpeaker_Click(object sender, RoutedEventArgs e)
        {

            ChangeSpeakerRequest?.Invoke(this, new EventArgs());
        }

        private TimeSpan _higlightedPosition = new TimeSpan(-1);
        public TimeSpan HiglightedPostion
        {
            set
            {
                _higlightedPosition = value;
                if (BackgroundHiglighter is { })
                    editor.TextArea.TextView.BackgroundRenderers.Remove(BackgroundHiglighter);
                BackgroundHiglighter = null;

                if (value < TimeSpan.Zero || ValueElement is not TranscriptionParagraph p || value < p.Begin || value > p.End)
                    return;

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

        public void Dispose()
        {
            this.ValueElement.ContentChanged -= val_ContentChanged;
        }


        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.ValueElement is { })
                this.ValueElement.ContentChanged -= val_ContentChanged;
        }
    }

    public class SpellChecker : DocumentColorizingTransformer
    {
        static readonly TextDecorationCollection defaultdecoration;
        static readonly TextDecorationCollection defaultdecorationSuggestion;

        //nacteni celeho slovniku z souboru do hash tabulky
        static WordList spell = null;


        public static WordList SpellEngine
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
                spell = WordList.CreateFromFiles(faff, fdic);
                return true;
            }

            return false;
        }

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
            if (spell is null)
                return true;

            if (!Element.wordIgnoreChecker.IsMatch(word) && !spell.Check(word))
                return false;

            return true;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (spell is null)
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
            if (value is TranscriptionParagraph paragraph)
                return new TextBlock() { Text = (paragraph.Speaker ?? Speaker.DefaultSpeaker).FullName };

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }



    public class NonEditableBlockGenerator : VisualLineElementGenerator
    {
        static bool _hidneNSE = false;

        /// <summary>
        /// hide non speech events
        /// </summary>
        public static bool HideNSE
        {
            get { return _hidneNSE; }
            set
            {
                _hidneNSE = value;
            }
        }

        Match FindMatch(int startOffset)
        {
            // fetch the end offset of the VisualLine being generated
            int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
            TextDocument document = CurrentContext.Document;
            string relevantText = document.GetText(startOffset, endOffset - startOffset);
            return Element.ignoredGroup.Match(relevantText);
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
                TextBlock t;
                if (_hidneNSE)
                    t = new TextBlock() { Text = "" };
                else
                    t = new TextBlock() { Text = m.Value };


                return new InlineObjectElement(m.Length, t);
            }
            return null;
        }
    }

}
