using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace NanoTrans
{
    /// <summary>
    ///     <MyNamespace:MyTextbox/>
    /// </summary>
    public class MyTextBox : TextBox
    {
        private bool m_supressTextChangedEvent = false;
        public MyTextBox():base()
        {
            this.Background = new SolidColorBrush(Color.FromArgb(0,255,255,255));
            this.SizeChanged+=new SizeChangedEventHandler(MyTextBox_SizeChanged);
            
        }

        void  MyTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
    	    if(m_BGcanvas!=null)
            {
                m_BGcanvas.Height = this.Height;
                m_BGcanvas.Width = this.Width;
                m_BGcanvas.Margin = this.Margin;
            }
        }

        public Canvas m_BGcanvas = null;

        public Canvas BGcanvas
        {
            get{return m_BGcanvas;}
            set
            {
                m_BGcanvas = value;
            }
        }
        public static HashSet<string> Vocabulary = new HashSet<string>();
        
        public void RefreshTextMarking()
        {
            this.OnTextChanged(null);
        }

        public static Regex wordSplitter = new Regex(@"(?:\w+|\[.*?\])", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex wordIgnoreChecker = new Regex(@"(?:\[.*?\]|\d+)", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    
        public static Regex ignoredGroup = new Regex(@"\[.*?\]", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Key suggestionKey = Key.Divide;
        public static string[] suggestions = { };
        static MyTextBox()
        {
            
        }

        bool i_leftDown = false;
        int i_sindex = -1;

        int GetRepairedMouseIndex(Point pos)
        {
            
            int indx = GetCharacterIndexFromPoint(pos,true);
            if (indx < 0 || indx > this.Text.Length)
                return -1;


            Rect r1 = GetRectFromCharacterIndex(indx, false);
            Rect r2 = GetRectFromCharacterIndex(indx, true);

            if (pos.X >= (r1.Left + 0.5*(r2.Left-r1.Left)))
                indx++;

            return indx;
        }

        TextChangedEventArgs m_stroredTCEvent = null;
        private void RestoreTextChangedEvent()
        {
            m_supressTextChangedEvent = false;
            OnTextChanged(m_stroredTCEvent);
            m_stroredTCEvent = null;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!m_supressTextChangedEvent)
            {
                if (e != null)
                    base.OnTextChanged(e);
            }
            else
            {
                m_stroredTCEvent = e;
                return;
            }

            if(BGcanvas == null)
                return;

            if (Vocabulary.Count > 0)
            {
                BGcanvas.Children.Clear();


                foreach (Match m in wordSplitter.Matches(this.Text))
                {
                    string subst = this.Text.Substring(m.Index, m.Length).ToLower();
                    if (!wordIgnoreChecker.IsMatch(subst))
                    {
                        if (!Vocabulary.Contains(subst))
                        {
                            int i = m.Index + m.Length;
                            int beg = m.Index;
                            Line l = new Line();
                            l.Stroke = Brushes.Red;
                            l.StrokeThickness = -1;
                            l.StrokeDashArray = new DoubleCollection(new double[] { 2, 2 });

                            Rect r1 = this.GetRectFromCharacterIndex(beg);
                            Rect r2 = this.GetRectFromCharacterIndex(i);

                            Point bl = r1.BottomLeft;
                            Point br = r2.BottomRight;

                            l.X1 = bl.X;
                            l.X2 = br.X;
                            l.Y1 = l.Y2 = bl.Y;

                            BGcanvas.Children.Add(l);
                        }
                    }
                }

                BGcanvas.InvalidateVisual();
            }
        } 
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            
           // Console.WriteLine("down");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                i_backSelect = false;
                if (!this.IsFocused)
                    this.Focus();

                i_leftDown = true;
                Point pos = e.GetPosition(this);
                int indx = GetRepairedMouseIndex(pos);

                MatchCollection matches = ignoredGroup.Matches(Text);
                foreach (Match m in matches)
                {
                    int beginindex = m.Index;
                    int endindex = m.Index+m.Length;
                    if (indx >= beginindex && indx < endindex)
                    {
                        double l1 = GetRectFromCharacterIndex(beginindex, false).Left;
                        double l2 = GetRectFromCharacterIndex(endindex, true).Left;
                        
                        if (pos.X >= (l1 + 0.5 * (l2 - l1)))
                        {
                            indx = endindex;
                        }else 
                        {
                            indx = beginindex;
                        }


                        break;
                    }
                }

                i_sindex = indx;
                if(indx>=0)
                    this.CaretIndex = indx;
                e.Handled = true;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released)
            {
                i_leftDown = false;
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            //Console.WriteLine("move");   
            if ((Mouse.LeftButton == MouseButtonState.Pressed) && i_leftDown && i_sindex>=0)
            {
                int sindx = i_sindex;
                Point pos = e.GetPosition(this);
                int indx = GetRepairedMouseIndex(pos);

                MatchCollection matches = ignoredGroup.Matches(Text);
                foreach (Match m in matches)
                {
                    int beginindex = m.Index;
                    int endindex = m.Index + m.Length;
                    if (indx >= beginindex && indx < endindex)
                    {
                        double l1 = GetRectFromCharacterIndex(beginindex, false).Left;
                        double l2 = GetRectFromCharacterIndex(endindex, true).Left;

                        if (pos.X < (l1 + 0.5 * (l2 - l1)))//levy konec
                        {
                            indx = beginindex;
                        }
                        else //pravy konec
                        {
                            indx = endindex;
                        }


                        break;
                    }
                }

                if (indx >= 0)
                {
                    if (i_sindex > indx) //prehozeni
                    {
                        sindx = sindx ^ indx;
                        indx = sindx ^ indx;
                        sindx = sindx ^ indx;
                    }

                    SelectionStart = sindx;
                    SelectionLength = indx - sindx;
                    Cursor = Cursors.IBeam;
                    i_backSelect = i_sindex >= indx;
                }
            }

            base.OnMouseMove(e);
        }

        //oznacovani pomoci sipek
        bool i_backSelect = false;
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            //Console.WriteLine("key");
            base.OnPreviewKeyDown(e);

            if (e.Handled)
                return;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {

                switch (e.Key)
                {
                    case Key.Right:

                        if (SelectionLength == 0)
                            i_backSelect = false;

                        if (i_backSelect)
                        {
                            int step = GetStepLength(CaretIndex, false);
                            int len = SelectionLength - step;
                            CaretIndex += step;
                            SelectionStart = CaretIndex;
                            SelectionLength = len;
                        }
                        else
                        {
                            int len = SelectionLength + GetStepLength(SelectionStart + SelectionLength, false);
                            int start = SelectionStart;
                            CaretIndex = start+len;

                            SelectionStart = start;
                            SelectionLength = len;
                        }

                        e.Handled = true;
                        break;
                    case Key.Left:

                        if (!i_backSelect && SelectionStart == CaretIndex && SelectionLength > 0)//obracene
                        {
                            int step = GetStepLength(SelectionStart + SelectionLength, true);
                            SelectionLength -= step;
                        }
                        else if (SelectionStart == CaretIndex && SelectionLength == 0)
                        {
                            int step = GetStepLength(CaretIndex, true);
                            SelectionStart = CaretIndex - step;
                            SelectionLength = step;
                            i_backSelect = true;
                        }
                        else
                        {
                            int step = GetStepLength(CaretIndex, true);
                            int len = SelectionLength + step;
                            CaretIndex -= step;
                            SelectionStart = CaretIndex;
                            SelectionLength = len;
                        }

                        e.Handled = true;
                        break;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                switch (e.Key)
                {
                    case Key.Right:
                        int pos = this.CaretIndex;
                        if (pos >= 0 && pos < this.Text.Length)
                        {
                            MatchCollection mc= wordSplitter.Matches(this.Text);
                            for(int i=0;i<mc.Count;i++ )
                            {
                                Match m = mc[i];
                                if (i< mc.Count-1 && m.Index <= pos && m.Index + m.Length >= pos)
                                {
                                    //m = mc[i+1];
                                    this.CaretIndex = m.Index + m.Length;
                                    //this.SelectionLength = m.Length;
                                    break;
                                }
                            }
                        }
                       
                        break;
                    case Key.Left:
                        pos = this.CaretIndex;
                        if (pos >= 0 && pos < this.Text.Length)
                        {
                            MatchCollection mc= wordSplitter.Matches(this.Text);
                            for(int i=0;i<mc.Count;i++ )
                            {
                                Match m = mc[i];
                                if ( i>0 && m.Index <= pos && m.Index + m.Length >= pos)
                                {
                                    m = mc[i - 1];
                                    this.CaretIndex = m.Index + m.Length;
                                    //this.SelectionLength = m.Length;
                                    break;
                                }
                            }
                        }
                       
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Right:

                        if (SelectionLength > 0 && !i_backSelect)
                        {
                            CaretIndex = SelectionStart + SelectionLength;
                        }
                        
                        int step = GetStepLength(CaretIndex, false);
                        CaretIndex += step;

                        e.Handled = true;
                        break;
                    case Key.Left:

                        if (SelectionLength > 0 && !i_backSelect)
                        {
                            CaretIndex = SelectionStart + SelectionLength;
                        }

                        step = GetStepLength(CaretIndex, true);
                        CaretIndex -= step;
                        e.Handled = true;
                        break;
                    case Key.Delete:
                        step = GetStepLength(CaretIndex, false);
                        if (this.SelectionLength > 0)
                        {
                            string pre = this.Text.Substring(0, SelectionStart);
                            string post = this.Text.Substring(SelectionStart + SelectionLength);
                            m_supressTextChangedEvent = true;//blokace eventu, nejsou poradne nastaveny pozice carety
                            this.Text = pre + post; 
                            this.SelectionLength = 0;
                            CaretIndex = pre.Length;
                            RestoreTextChangedEvent(); //nastaveno odpalim event....
                        }
                        else
                        {
                            string pre = this.Text.Substring(0, CaretIndex);
                            string post = this.Text.Substring(CaretIndex + step);
                            m_supressTextChangedEvent = true;
                            this.Text = pre + post;
                            CaretIndex = pre.Length;
                            RestoreTextChangedEvent(); //nastaveno odpalim event....
                        }
                        e.Handled = true;
                        break;
                    case Key.Back:
                        if (CaretIndex == 0)
                            break;
                        step = GetStepLength(CaretIndex, true);

                        if (this.SelectionLength > 0)
                        {
                            string pre = this.Text.Substring(0, SelectionStart);
                            string post = this.Text.Substring(SelectionStart + SelectionLength);
                            m_supressTextChangedEvent = true;//blokace eventu, nejsou poradne nastaveny pozice carety
                            this.Text = pre + post;
                            this.SelectionLength = 0;
                            CaretIndex = pre.Length;
                            RestoreTextChangedEvent(); //nastaveno odpalim event....
                        }
                        else
                        { 
                            string pre = this.Text.Substring(0, CaretIndex - step);
                            string post = this.Text.Substring(CaretIndex);
                            m_supressTextChangedEvent = true;//blokace eventu, nejsou poradne nastaveny pozice carety
                            this.Text = pre + post;
                            CaretIndex = pre.Length;
                            RestoreTextChangedEvent();
                        }

                        e.Handled = true;
                        break;
                }
                
            }
        }

        int GetStepLength(int position, bool stepleft)
        {
            MatchCollection matches = ignoredGroup.Matches(Text);
            if (stepleft)
            {
                if (position == 0)
                    return 0;

                foreach (Match m in matches)
                {
                    if (m.Index + m.Length == position)
                        return m.Length;
                }

                return 1;
            }
            else
            {
                if (position == Text.Length)
                    return 0;

                foreach (Match m in matches)
                {
                    if (m.Index == position)
                        return m.Length;
                }

                return 1;
            }

        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            Point p = e.GetPosition(this);
            int pos = this.GetCharacterIndexFromPoint(p, false);
            if (pos >= 0 && pos < this.Text.Length)
            {
                foreach (Match m in wordSplitter.Matches(this.Text))
                {
                    if (m.Index <= pos && m.Index + m.Length >= pos)
                    {
                        this.SelectionStart = m.Index;
                        this.SelectionLength = m.Length;
                        break;
                    }
                }
            }
        }


        //nacteni celeho slovniku z souboru do hash tabulky
        public static bool LoadVocabulary(string filename)
        {
            try
            {
                MyTextBox.Vocabulary = new HashSet<string>();
                System.IO.StreamReader reader = new System.IO.StreamReader(filename);
                while (reader.Peek() >= 0)
                {
                    foreach (string s in reader.ReadLine().Split(' '))
                    {
                        MyTextBox.Vocabulary.Add(s.ToLower());
                    }
                }

                reader.Close();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        
    }
}
