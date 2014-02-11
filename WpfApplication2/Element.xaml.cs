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

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Element.xaml
    /// </summary>
    public partial class Element : UserControl
    {


        public static readonly DependencyProperty ValueElementProperty =
        DependencyProperty.Register("ValueElement", typeof(TranscriptionElement), typeof(Element), new FrameworkPropertyMetadata(OnValueElementChanged));

        public static void OnValueElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranscriptionElement val = (TranscriptionElement)e.NewValue;
            Element el = (Element)d;
            el.m_Element = val;

            if (e.NewValue == null)
                return;

            Type t = e.NewValue.GetType();

            if (t == typeof(MyParagraph))
            {
                MyParagraph p = (MyParagraph)val;
                el.textbegin.Visibility = Visibility.Visible;
                el.textend.Visibility = Visibility.Visible;
                el.stackPanel1.Visibility = Visibility.Visible;
                el.Background = MySetup.Setup.BarvaTextBoxuOdstavce;
                el.button1.Visibility = Visibility.Visible;
                if (val.Previous() != null)
                {
                    if (val is MyParagraph)
                    {
                        if (val.Previous() is MyParagraph && ((MyParagraph)val).speakerID == ((MyParagraph)val.Previous()).speakerID)
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
                el.button1.Visibility = Visibility.Visible;
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

            el.updating = true;
            el.myTextBox1.Text = val.Text;
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
                updating = true;
                if (ValueElement != null)
                {
                    ValueElement.BeginChanged -= BeginChanged;
                    ValueElement.EndChanged -= EndChanged;
                }
                SetValue(ValueElementProperty, value);
                DataContext = value;
                if (value != null)
                {
                    value.BeginChanged += BeginChanged;
                    value.EndChanged += EndChanged;

                    BeginChanged(this, null);
                    EndChanged(this, null);

                    MyParagraph par = value as MyParagraph;
                    if (par != null)
                    {
                        int i = 0;
                        foreach (Rectangle r in stackPanel1.Children)
                        {
                            if ((par.DataAttributes & all[i]) != 0)
                            {
                                r.Fill = GetRectangleInnenrColor(all[i]);
                            }
                            else
                            {
                                r.Fill = GetRectangleBgColor(all[i]);
                            }
                            i++;
                        }
                    }
                }

                updating = false;
            }
        }

        public string Text
        {
            get
            {
                return myTextBox1.Text;
            }
            set
            {
                myTextBox1.Text = value;
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

                        r.Fill = GetRectangleBgColor(at);
                        r.MouseLeftButtonDown += new MouseButtonEventHandler(attributes_MouseLeftButtonDown);
                        stackPanel1.Children.Add(r);
                    }
                }
            }
        }

        void attributes_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int i = ((sender as Rectangle).Parent as StackPanel).Children.IndexOf(sender as UIElement) + 1;
            MyParagraph par = ValueElement as MyParagraph;

            if (par != null)
            {
                par.DataAttributes ^= all[i];
                if ((par.DataAttributes & all[i]) != 0)
                {
                    (sender as Rectangle).Fill = GetRectangleInnenrColor(all[i]);
                }
                else
                {
                    (sender as Rectangle).Fill = GetRectangleBgColor(all[i]);
                }
            }
        }

        private bool updating = false;

        public Element()
        {
            InitializeComponent();
            RepaintAttributes();
        }

        private void BeginChanged(object sendet, EventArgs value)
        {
            TimeSpan val = ValueElement.Begin;
            this.textbegin.Text = string.Format("{0}:{1:00}:{2:00},{3}", val.Hours, val.Minutes, val.Seconds, val.Milliseconds.ToString("00").Substring(0, 2));
        }

        private void EndChanged(object sendet, EventArgs value)
        {
            TimeSpan val = ValueElement.End;
            this.textend.Text = string.Format("{0}:{1:00}:{2:00},{3}", val.Hours, val.Minutes, val.Seconds, val.Milliseconds.ToString("00").Substring(0, 2));
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ValueElement is MyParagraph)
            {
                textboxcanvas.Background = MySetup.Setup.BarvaTextBoxuOdstavceAktualni;
            }
            else
            {
                textboxcanvas.Background = null;
            }
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ValueElement is MyParagraph)
            {
                textboxcanvas.Background = null;
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

        private void myTextBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ValueElement == null || updating)
                return;
            if (!(ValueElement is MyParagraph))
            {

                ValueElement.Text = myTextBox1.Text;
                return;
            }

            MyParagraph par = ValueElement as MyParagraph;
            string text = myTextBox1.Text;
            foreach (TextChange c in e.Changes)
            {
                int offset = c.Offset;
                int removedl = c.RemovedLength;
                int addedl = c.AddedLength;

                if (c.RemovedLength > 0)
                {
                    List<MyPhrase> todelete = new List<MyPhrase>();
                    int pos = 0;
                    foreach (MyPhrase p in par.Phrases)
                    {
                        if (pos + p.Text.Length > offset)
                        {
                            int iidx = offset - pos;
                            if(removedl > p.Text.Length - iidx)
                            {
                                removedl -= p.Text.Length - iidx;
                                offset = pos + p.Text.Length;
                                p.Text = p.Text.Remove(iidx);
                            }else
                            {
                                p.Text = p.Text.Remove(iidx, removedl);
                                break;
                            }
                        }
                        else if (pos + p.Text.Length == offset)
                        {
                            if (removedl > p.Text.Length)
                            {
                                todelete.Add(p);
                                removedl -= p.Text.Length;
                                offset = pos + p.Text.Length;
                            }
                            else if (removedl == p.Text.Length)
                            {
                                todelete.Add(p);
                                break;
                            }else
                            {
                                p.Text = p.Text.Remove(offset-pos,removedl);
                                break;
                            }
                        }

                        pos += p.Text.Length;
                    }

                    foreach (var v in todelete)
                        par.Phrases.Remove(v);

                }else if (c.AddedLength > 0)
                {
                    int pos = 0;
                    foreach (MyPhrase p in par.Phrases)
                    {
                        if (c.Offset <= pos + p.Text.Length) //vlozeni
                        { 
                            p.Text = p.Text.Insert(c.Offset-pos,text.Substring(c.Offset,c.AddedLength));
                            break;
                        }
                        pos += p.Text.Length;
                    }
                }
            }

           
        }

        private void myTextBox1_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.PageUp || e.Key == Key.PageDown) // klavesy, ktere textbox krade, posleme rucne parentu...
            {
                KeyEventArgs kea = new KeyEventArgs((KeyboardDevice)e.Device, PresentationSource.FromVisual(this), e.Timestamp, e.Key) { RoutedEvent = Element.PreviewKeyDownEvent };
                RaiseEvent(kea);
                if (!kea.Handled)
                {
                    kea.RoutedEvent = Element.KeyDownEvent;
                    RaiseEvent(kea);
                }


                return;
            }

            this.OnPreviewKeyDown(e);

            if (ValueElement == null)
                return;
            if (!(ValueElement is MyParagraph))
            {

                ValueElement.Text = myTextBox1.Text;
                return;
            }

            MyParagraph par = ValueElement as MyParagraph;


                if ((e.Key == Key.Up))
                {

                    int TP = myTextBox1.GetLineIndexFromCharacterIndex(myTextBox1.SelectionStart + myTextBox1.SelectionLength);
                    if (TP == 0 && MoveUpRequest!=null)
                    {
                        MoveUpRequest(this, new EventArgs());
                    }
                }
                else if ((e.Key == Key.Down))
                {
                    int TP = myTextBox1.GetLineIndexFromCharacterIndex(myTextBox1.SelectionStart + myTextBox1.SelectionLength);
                    if (TP ==  myTextBox1.GetLineIndexFromCharacterIndex(myTextBox1.Text.Length-1) && MoveDownRequest != null)
                    {
                        MoveDownRequest(this, new EventArgs());
                    }
                }
                else if (e.Key == Key.Right)
                {
                    if (myTextBox1.CaretIndex == myTextBox1.Text.Length && MoveRightRequest!=null)
                        MoveRightRequest(this, new EventArgs());

                }
                else if (e.Key == Key.Left)
                {
                    if (myTextBox1.CaretIndex == 0 && MoveLeftRequest!=null)
                        MoveLeftRequest(this, new EventArgs());

                }
                else if (e.Key == Key.Back)
                {
                    if (myTextBox1.CaretIndex == 0 && MergeWithPreviousRequest != null)
                        MergeWithPreviousRequest(this, new EventArgs());

                }
                else if (e.Key == Key.Delete)
                {
                    if (myTextBox1.CaretIndex == myTextBox1.Text.Length && MergeWithnextRequest != null)
                        MergeWithnextRequest(this, new EventArgs());

                }
                else if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    if (myTextBox1.CaretIndex != 0 && myTextBox1.CaretIndex != myTextBox1.Text.Length && SplitRequest!=null)
                    {
                        SplitRequest(this, new EventArgs());
                    }
                }
        }


        
        public event EventHandler MoveUpRequest;
        public event EventHandler MoveDownRequest;

        public event EventHandler MoveRightRequest;
        public event EventHandler MoveLeftRequest;

        public event EventHandler MergeWithPreviousRequest;
        public event EventHandler MergeWithnextRequest;
        public event EventHandler SplitRequest;
    }
}
