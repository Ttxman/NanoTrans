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
using System.Threading;
using System.Diagnostics;
using NanoTrans.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NanoTrans
{
    public partial class WaveForm : UserControl, INotifyPropertyChanged
    {

        private void OnPropertyChanged([CallerMemberName]string caller = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        public TimeSpan WaveBegin
        {
            get { return TimeSpan.FromMilliseconds(wave.BeginMS); }
            set
            {
                wave.BeginMS = (long)value.TotalMilliseconds;
                InvalidateWaveform();
                OnPropertyChanged();
            }
        }

        public TimeSpan WaveEnd
        {
            get { return TimeSpan.FromMilliseconds(wave.EndMS); }
            set
            {
                wave.EndMS = (long)value.TotalMilliseconds;
                InvalidateWaveform();
                OnPropertyChanged();
            }
        }

        public TimeSpan WaveLength
        {
            get { return TimeSpan.FromMilliseconds(wave.LengthMS); }
            set
            {
                wave.SetWaveLength((long)value.TotalMilliseconds);
                InvalidateWaveform();
                OnPropertyChanged();
            }
        }

        public TimeSpan WaveLengthDelta
        {
            get { return TimeSpan.FromMilliseconds(wave.DeltaMS); }
        }

        public TimeSpan SelectionBegin
        {
            get { return wave.SelectionStart; }
            set
            {
                wave.SelectionStart = value;
                OnPropertyChanged();
                OnPropertyChanged("SelectionMargin");
            }
        }

        public TimeSpan SelectionEnd
        {
            get { return wave.SelectionEnd; }
            set
            {
                wave.SelectionEnd = value;
                OnPropertyChanged();
                OnPropertyChanged("SelectionMargin");
            }
        }


        public Thickness CaretMargin
        {
            get
            {
                double aLeft = (CaretPosition - WaveBegin).TotalMilliseconds;
                aLeft = aLeft / WaveLength.TotalMilliseconds * this.ActualWidth;
                return new Thickness(aLeft + caretRectangle.Width / 2, caretRectangle.Margin.Top, caretRectangle.Margin.Right, caretRectangle.Margin.Bottom);
            }
        }

        public Thickness SelectionMargin
        {
            get
            {
                long begin = (long)wave.SelectionStart.TotalMilliseconds;
                long end = (long)wave.SelectionEnd.TotalMilliseconds;
                if (end < begin)
                {
                    long bf = end;
                    end = begin;
                    begin = end;
                }
                double aLeft = (double)(this.ActualWidth * (begin - wave.BeginMS) / wave.LengthMS);
                double aRight = (double)(this.ActualWidth * (1 - ((double)end - wave.BeginMS) / wave.LengthMS));

                if (end > wave.EndMS)
                    aRight = 0;

                if (begin < wave.BeginMS)
                    aLeft = 0;

                if ((begin != -1 && end != -1) || (begin != -1))
                {
                    return new Thickness(aLeft, 0, aRight, 0);
                }

                return new Thickness(0, 0, this.ActualWidth, 0);
            }
        }

        public TimeSpan CaretPosition
        {
            get { return TimeSpan.FromMilliseconds(wave.CaretPositionMS); }
            set
            {


                long pos = (long)value.TotalMilliseconds;
                if (pos == wave.CaretPositionMS)
                    return;

                Dispatcher.Invoke(() =>
                {
                    this.AudioBufferCheck(value);
                    wave.CaretPositionMS = pos;

                    TimeSpan ts = value;

                    if (_updating > 0)
                        return;



                    if (value < WaveBegin || value > WaveEnd)
                    {
                        BeginUpdate();
                        SliderPostion = CaretPosition;
                        EndUpdate();
                    }



                    if (_updating == 0)
                        if (CaretPostionChanged != null)
                            CaretPostionChanged(this, new TimeSpanEventArgs(value));
                });

                OnPropertyChanged();
                OnPropertyChanged("CaretMargin");
            }
        }


        public TimeSpan SliderPostion
        {
            get
            {
                return TimeSpan.FromMilliseconds(slMediaPosition.Value);
            }
            set
            {
                slMediaPosition.Value = value.TotalMilliseconds;
                OnPropertyChanged();
            }
        }

        TimeSpan i_AudioLength;
        public TimeSpan AudioLength
        {
            get { return i_AudioLength; }
            set
            {
                i_AudioLength = value;
                slMediaPosition.Maximum = value.TotalMilliseconds;
                OnPropertyChanged();
            }
        }

        public TimeSpan SmallJump
        {
            get { return TimeSpan.FromMilliseconds(wave.ShortJumpMS); }
            set
            {
                wave.ShortJumpMS = (long)value.TotalMilliseconds;
                InvalidateWaveform();
                OnPropertyChanged();
            }
        }

        public double Scale
        {
            get { return 0.01 * wave.YScalePercentage; }
            set
            {
                if (value > 20)
                    value = 20;
                wave.YScalePercentage = (int)(value * 100);
                wave.ScaleAutomaticaly = false;
                InvalidateWaveform();
                OnPropertyChanged();
            }
        }

        public bool ScaleAutomaticaly
        {
            get { return wave.ScaleAutomaticaly; }
            set
            {
                wave.ScaleAutomaticaly = value;
                InvalidateWaveform();
                OnPropertyChanged();
            }
        }

        public TimeSpan ProgressHighlightBegin
        {
            get { return TimeSpan.FromMilliseconds(slMediaPosition.SelectionStart); }
            set
            {
                if (!_autohighlight)
                    slMediaPosition.SelectionStart = value.TotalMilliseconds;

            }
        }

        public TimeSpan ProgressHighlightEnd
        {
            get { return TimeSpan.FromMilliseconds(slMediaPosition.SelectionEnd); }
            set
            {
                if (!_autohighlight)
                    slMediaPosition.SelectionEnd = value.TotalMilliseconds;
            }
        }

        public TimeSpan AudioBufferBegin
        {
            get { return TimeSpan.FromMilliseconds(wave.audioBuffer.StartMS); }
        }

        public TimeSpan AudioBufferEnd
        {
            get { return TimeSpan.FromMilliseconds(wave.audioBuffer.EndMS); }
        }


        private bool _autohighlight = false;
        public bool AutomaticProgressHighlight
        {
            get { return _autohighlight; }
            set
            {
                _autohighlight = value;
                if (value)
                {
                    slMediaPosition.SelectionStart = AudioBufferBegin.TotalMilliseconds;
                    slMediaPosition.SelectionEnd = AudioBufferEnd.TotalMilliseconds;
                }
            }
        }


        private int _frequency = 16000;
        public int AudioFrequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        private bool _playing;
        public bool Playing
        {
            get { return _playing; }
            set
            {
                _playing = value;
                OnPropertyChanged();
            }
        }

        private Transcription _subtitlesData;


        public static readonly DependencyProperty SubtitlesProperty = DependencyProperty.Register("Transcription", typeof(WPFTranscription), typeof(WaveForm), new FrameworkPropertyMetadata(OnSubtitlesChanged));

        public static void OnSubtitlesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            ((WaveForm)d)._subtitlesData = (WPFTranscription)e.NewValue;
            ((WaveForm)d).Invalidate();
        }


        public WPFTranscription Transcription
        {
            get
            {
                return (WPFTranscription)GetValue(SubtitlesProperty);
            }
            set
            {

                SetValue(SubtitlesProperty, value);
                _subtitlesData = value;
                Invalidate();
            }
        }





        private WaveformData wave = new WaveformData(Const.DISPLAY_BUFFER_LENGTH_MS);

        public event RoutedEventHandler PlayPauseClick;
        public event EventHandler<TimeSpanEventArgs> SliderPositionChanged;
        public event EventHandler UpdateBegin;
        public event EventHandler UpdateEnd;


        public event EventHandler<TimeSpanEventArgs> CaretPostionChangedByUser;
        public event EventHandler<TimeSpanEventArgs> CaretPostionChanged;
        public event EventHandler<MyTranscriptionElementEventArgs> ParagraphClick;
        public event EventHandler<MyTranscriptionElementEventArgs> ParagraphDoubleClick;
        public event EventHandler<MyTranscriptionElementEventArgs> ElementChanged;


        public class TimeSpanEventArgs : EventArgs
        {
            public TimeSpan Value;
            public TimeSpanEventArgs(TimeSpan value)
            {
                Value = value;
            }
        }

        public class MyTranscriptionElementEventArgs : EventArgs
        {
            public TranscriptionElement Value;
            public MyTranscriptionElementEventArgs(TranscriptionElement value)
            {
                Value = value;
            }
        }


        private bool invalidate_waveform = true;
        private bool invalidate_speakers = true;

        private Thread Invalidator;
        public WaveForm()
        {

            InitializeComponent();
            Invalidator = new Thread(ProcessInvalidates) { Name = this.Name + ":Invalidator" };
            Invalidator.Start();
            Application.Current.Exit += new ExitEventHandler(Current_Exit);
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            if (Invalidator.IsAlive)
                Invalidator.Interrupt();
        }

        private void ProcessInvalidates()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(20);
                    if (invalidate_waveform)
                    {
                        this.Dispatcher.Invoke(new Action(iInvalidateWaveform));
                    }

                    if (invalidate_speakers)
                    {
                        this.Dispatcher.Invoke(new Action(iInvalidateSpeakers));
                    }

                    invalidate_speakers = false;
                }
            }
            catch (ThreadInterruptedException) { }
        }


        private int _updating = 0;
        public void BeginUpdate()
        {
            if (UpdateBegin != null)
                UpdateBegin(this, new EventArgs());
            _updating++;
        }

        public void EndUpdate()
        {
            if (UpdateEnd != null)
                UpdateEnd(this, new EventArgs());
            _updating--;
            if (_updating < 0)
                _updating = 0;
        }



        public void InvalidateWaveform()
        {
            invalidate_waveform = true;
        }

        public void Invalidate()
        {
            InvalidateSpeakers();
            InvalidateWaveform();
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(null));
        }


        private void iInvalidateWaveform()
        {
            bool notbuffered = WaveBegin < AudioBufferBegin || WaveBegin > AudioBufferEnd;

            if (notbuffered)
            {
                myImage.Source = null;
                return;
            }

            double begin = wave.BeginMS;
            double end = wave.EndMS;
            try
            {
                if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
                {
                    this.Dispatcher.Invoke(new Action(InvalidateWaveform));
                    return;
                }


                Int64 bg = (Int64)(0.001 * _frequency * (begin - wave.audioBuffer.StartMS));
                Int64 en = (Int64)(0.001 * _frequency * (end - wave.audioBuffer.StartMS));


                //////////////////////////////// should not ever enter here... :)
                if (bg < -100)
                {
                    return;
                }
                //////////////////////////////////////////////////////////////////////////////

                if (end > wave.audioBuffer.EndMS)
                {
                    en = (Int64)(((double)(wave.audioBuffer.EndMS - wave.audioBuffer.StartMS)) / 1000 * _frequency);// - 1;
                    bg = (Int64)(en - (wave.LengthMS / 1000.0) * _frequency);
                }


                long mSekundyKonec = (wave.EndMS < wave.audioBuffer.EndMS) ? wave.EndMS : wave.audioBuffer.EndMS;
                if (bg < 0)
                {
                    bg = 0;
                    if (en < 0)
                        en = bg + 30 * _frequency;//set length to 30s if buffer is shorter than requested
                    wave.BeginMS = 1000 * bg / _frequency + wave.audioBuffer.StartMS;
                    wave.EndMS = 1000 * en / _frequency + wave.audioBuffer.StartMS;
                    mSekundyKonec = (int)(wave.EndMS);
                    wave.SetWaveLength((uint)((en - bg) * 1000 / _frequency));
                }

                int pixelWidth = (int)this.ActualWidth;
                if (pixelWidth <= 0) pixelWidth = 1600;
                int sampleGroupCount = (int)((double)(en - bg) / (double)pixelWidth);





                double[] wavemax = new double[pixelWidth];
                double[] wavemin = new double[pixelWidth];
                for (int i = 0; i < pixelWidth; i++)
                {
                    double min = short.MaxValue;
                    double max = short.MinValue;

                    long pos = bg + i * sampleGroupCount;
                    for (long k = pos; k < pos + sampleGroupCount; k++)
                    {
                        short val = wave.audioBuffer.Data[k];
                        if (val < min)
                            min = val;
                        if (val > max)
                            max = val;
                    }


                    //normalize;
                    min /= ((double)short.MaxValue);
                    max /= ((double)short.MaxValue);

                    wavemin[i] = min;
                    wavemax[i] = max;
                }

                float yscale = wave.YScalePercentage / 100;

                if (!wave.ScaleAutomaticaly)
                {
                    if (yscale < 0.1)
                        yscale = 0.1f;
                    if (yscale > 100)
                        yscale = 100f;

                    wave.YScalePercentage = yscale * 100;

                    double factor = yscale;
                    for (int i = 0; i < wavemin.Length - 1; i++)
                    {
                        double min = wavemin[i] * factor;
                        double max = wavemax[i] * factor;

                        if (min < -1)
                            min = -1;

                        if (max > 1)
                            max = 1;

                        wavemin[i] = min;
                        wavemax[i] = max;

                    }

                }
                else //auto scale
                {
                    double min = Math.Abs(wavemin.Min());
                    double max = Math.Abs(wavemax.Max());
                    double factor = 1 / Math.Max(min, max);
                    for (int i = 0; i < wavemin.Length - 1; i++)
                    {
                        wavemin[i] *= factor;
                        wavemax[i] *= factor;
                    }
                }

                double mmin = Math.Abs(wavemin.Min());
                double mmax = Math.Abs(wavemax.Max());


                //waveform
                GeometryGroup myGeometryGroup = new GeometryGroup();
                for (int i = 0; i < wavemin.Length - 1; i++)
                    myGeometryGroup.Children.Add(new LineGeometry(new Point(i, wavemax[i]), new Point(i, wavemin[i])));

                //scale height
                double imageheight = myImage.ActualHeight;

                GeometryDrawing myGeometryDrawing = new GeometryDrawing(Brushes.Red, new Pen(Brushes.Red, 1), myGeometryGroup);


                myGeometryGroup = new GeometryGroup();
                myGeometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(wavemin.Length, 0)));
                var hline = new GeometryDrawing(Brushes.Red, new Pen(Brushes.Red, 0.5 / imageheight), myGeometryGroup);




                //heighth line
                myGeometryGroup = new GeometryGroup();
                myGeometryGroup.Children.Add(new LineGeometry(new Point(0, 1), new Point(0, -1)));
                var vline = new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Transparent, 1), myGeometryGroup);



                DrawingGroup myDrawingGroup = new DrawingGroup();
                myDrawingGroup.Children.Add(vline);
                myDrawingGroup.Children.Add(hline);
                myDrawingGroup.Children.Add(myGeometryDrawing);

                myDrawingGroup.Transform = new ScaleTransform(1, imageheight / 2);


                //myDrawingGroup.
                WaveFormImage = new DrawingImage(myDrawingGroup);
                invalidate_waveform = false;
            }
            catch// (Exception ex)
            {

            }


        }

        public void InvalidateSpeakers()
        {
            invalidate_speakers = true;
        }


        private List<Button> speakerButtons = new List<Button>();
        private void iInvalidateSpeakers()
        {
            try
            {
                Transcription transc = _subtitlesData;
                WaveformData currentWave = wave;

                if (transc == null) return;
                if (currentWave == null) return;

                foreach (Button pL in speakerButtons)
                {
                    wavegrid.Children.Remove(pL);
                }
                this.speakerButtons.Clear();

                for (int i = 0; i < transc.Chapters.Count; i++)
                {
                    TranscriptionChapter pChapter = transc.Chapters[i];
                    for (int j = 0; j < pChapter.Sections.Count; j++)
                    {
                        TranscriptionSection pSection = pChapter.Sections[j];
                        for (int k = 0; k < pSection.Paragraphs.Count; k++)
                        {
                            TranscriptionParagraph pParagraph = pSection.Paragraphs[k];
                            long pBegin = (long)pParagraph.Begin.TotalMilliseconds;
                            long pEnd = (long)pParagraph.End.TotalMilliseconds;

                            if (pEnd < pBegin)
                                pEnd = pBegin;

                            if (pBegin >= 0 && pEnd != pBegin && pEnd >= 0 && pParagraph.End >= TimeSpan.Zero)
                            {
                                if ((pBegin < currentWave.BeginMS && pEnd < currentWave.BeginMS) || (pBegin > currentWave.EndMS))
                                {

                                }
                                else
                                {

                                    if (pBegin >= currentWave.BeginMS && pEnd <= currentWave.EndMS)
                                    {

                                    }
                                    if (pBegin < currentWave.BeginMS) pBegin = currentWave.BeginMS;
                                    if (pEnd > currentWave.EndMS) pEnd = currentWave.EndMS;

                                    Button speaker = new Button();


                                    speaker.PreviewMouseMove += new MouseEventHandler(pSpeaker_MouseMove);
                                    speaker.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(pSpeaker_PreviewMouseLeftButtonUp);
                                    speaker.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(pSpeaker_PreviewMouseLeftButtonDown);

                                    speaker.VerticalAlignment = VerticalAlignment.Top;
                                    speaker.HorizontalAlignment = HorizontalAlignment.Left;

                                    double aLeft = (double)(this.ActualWidth * (pBegin - currentWave.BeginMS) / currentWave.LengthMS);
                                    double aRight = gridTimeline.ActualWidth - (double)(this.ActualWidth * (pEnd - currentWave.BeginMS) / currentWave.LengthMS);
                                    speaker.Margin = new Thickness(aLeft, speaker.Margin.Top, speaker.Margin.Right, selectionRectangle.Margin.Bottom);
                                    speaker.Width = gridTimeline.ActualWidth - aLeft - aRight;

                                    speaker.Background = Brushes.LightPink;
                                    Speaker pSpeaker = pParagraph.Speaker;
                                    string pText = "";
                                    if (pSpeaker != null) pText = pSpeaker.FullName;
                                    speaker.Visibility = Visibility.Visible;
                                    speaker.BringIntoView();
                                    speaker.Focusable = false;
                                    speaker.IsTabStop = false;
                                    speaker.Cursor = Cursors.Arrow;
                                    if (pText != null && pText != "") speaker.ToolTip = speaker.Content;
                                    speaker.Tag = pParagraph;
                                    speaker.Click += new RoutedEventHandler(pMluvci_Click);
                                    speaker.MouseDoubleClick += new MouseButtonEventHandler(pMluvci_MouseDoubleClick);


                                    DockPanel dp = new DockPanel() { LastChildFill = true, Margin = new Thickness(0, 0, 0, 0) };
                                    dp.Height = 15;
                                    DockPanel dp2 = new DockPanel() { LastChildFill = true, Margin = new Thickness(0, 0, 0, 0) };
                                    dp2.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                                    dp2.Height = 15;
                                    if (k == 0) //prvni zaznam v sekci
                                    {
                                        Ellipse el = new Ellipse();
                                        el.Width = 10;
                                        el.Height = 10;
                                        el.Margin = new Thickness(0, 0, 0, 0);
                                        el.Stroke = null;
                                        el.Fill = Brushes.DarkRed;

                                        dp.Children.Add(el);

                                    }


                                    if (
                                        (k == pSection.Paragraphs.Count - 2 && j == pChapter.Sections.Count - 1)
                                        ||
                                        (k == pSection.Paragraphs.Count - 1 && j != pChapter.Sections.Count - 1)
                                        )
                                    {
                                        Ellipse el = new Ellipse();
                                        el.Width = 10;
                                        el.Height = 10;
                                        el.Margin = new Thickness(0, 0, 0, 0);
                                        el.Stroke = null;
                                        el.Fill = Brushes.DarkRed;
                                        dp2.Children.Add(el);
                                    }
                                    Label lab = new Label() { Content = pText, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 0) };
                                    lab.Padding = new Thickness(0, 0, 0, 0);
                                    lab.Height = 15;
                                    dp2.Children.Add(lab);
                                    dp.Children.Add(dp2);
                                    speaker.Content = dp;

                                    speaker.SizeChanged += new SizeChangedEventHandler(pMluvci_SizeChanged);

                                    //speaker.SetValue(Grid.RowProperty, 1);
                                    this.speakerButtons.Add(speaker);
                                }
                            }
                        }
                    }
                }

                TranscriptionParagraph previous = null;
                TranscriptionParagraph next = null;
                for (int i = 0; i < this.speakerButtons.Count; i++)
                {
                    Button pMluvci = this.speakerButtons[i];
                    Grid.SetRow(pMluvci, 1);
                    Grid.SetColumn(pMluvci, 0);
                    TranscriptionParagraph pPar = pMluvci.Tag as TranscriptionParagraph;
                    if (i < this.speakerButtons.Count - 1)
                    {
                        next = this.speakerButtons[i + 1].Tag as TranscriptionParagraph;
                    }
                    if (previous != null && previous.End > pPar.Begin && speakerButtons[i - 1].Margin.Top < 5)
                    {
                        wavegrid.Children.Add(pMluvci);

                        wavegrid.UpdateLayout();
                        pMluvci.Margin = new Thickness(pMluvci.Margin.Left, pMluvci.ActualHeight, pMluvci.Margin.Right, pMluvci.Margin.Bottom);
                    }
                    else
                    {
                        wavegrid.Children.Add(pMluvci);
                    }

                    previous = pPar;
                }


            }
            catch { }
        }

        void pSpeaker_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Button b = sender as Button;

            Point p = e.GetPosition(b);
            if (p.X <= 4)
            {
                btndrag = true;
                btndragleft = true;
                e.Handled = true;
                b.CaptureMouse();
            }
            else if (p.X >= b.Width - 4)
            {
                btndrag = true;
                btndragleft = false;
                e.Handled = true;
                b.CaptureMouse();
            }


        }


        void pMluvci_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TranscriptionParagraph pTag = (sender as Button).Tag as TranscriptionParagraph;
            if (ParagraphDoubleClick != null)
                ParagraphDoubleClick(this, new MyTranscriptionElementEventArgs(pTag));

        }




        void pMluvci_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Button)
            {
                Button sndr = (Button)sender;
                double width = sndr.Width - 10;
                (sndr.Content as DockPanel).Width = (width > 0.0) ? width : 0.0;
            }
        }

        /// <summary>
        /// co se deje pri kliknuti na tlacitko mluvciho v signalu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pMluvci_Click(object sender, RoutedEventArgs e)
        {

            TranscriptionParagraph pTag = (sender as Button).Tag as TranscriptionParagraph;
            if (ParagraphClick != null)
                ParagraphClick(this, new MyTranscriptionElementEventArgs(pTag));

        }

        private void btPrehratZastavit_Click(object sender, RoutedEventArgs e)
        {
            if (PlayPauseClick != null)
                PlayPauseClick(this, new RoutedEventArgs(Button.ClickEvent));
        }

        DrawingImage _Waveform = null; 
        public DrawingImage WaveFormImage 
        { 
            get
            {
                return  _Waveform;
            } 
            set
            {
                _Waveform = value;
                OnPropertyChanged();
            }
        }


        private void btMoveLeft_Click(object sender, RoutedEventArgs e)
        {
            BeginUpdate();

            wave.BeginMS -= wave.LengthMS / 10;


            if (wave.BeginMS < 0)
                wave.BeginMS = 0;

            wave.EndMS = wave.BeginMS + wave.LengthMS;

            double caretpos = CaretPosition.TotalMilliseconds - wave.LengthMS / 10;
            if (caretpos < 0)
                caretpos = 0;
            this.CaretPosition = TimeSpan.FromMilliseconds(caretpos);
            if (CaretPostionChangedByUser != null)
                CaretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));
            InvalidateWaveform();

            EndUpdate();
        }

        private void btMoveRight_Click(object sender, RoutedEventArgs e)
        {
            BeginUpdate();

            wave.EndMS = wave.EndMS + wave.LengthMS / 10;

            if (AudioLength.TotalMilliseconds < wave.EndMS)
                wave.EndMS = (long)AudioLength.TotalMilliseconds;
            wave.BeginMS = wave.EndMS - wave.LengthMS;

            this.CaretPosition = TimeSpan.FromMilliseconds(CaretPosition.TotalMilliseconds + wave.LengthMS / 10);
            if (CaretPostionChangedByUser != null)
                CaretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));

            InvalidateWaveform();


            EndUpdate();
        }


        double downpos = 0;
        TimeSpan downtime = TimeSpan.Zero;

        //posun prehravaneho zvuku na danou pozici
        private void myImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!seldrag)
            {
                downpos = e.GetPosition(this).X;
                downtime = PosToTime(downpos);
            }
            Mouse.Capture(myImage);
        }


        private void myImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            seldrag = false;
            double pos = e.GetPosition(this).X;
            if (Math.Abs(downpos - pos) < 1)
            {
                BeginUpdate();

                TimeSpan tpos = PosToTime(pos);
                this.CaretPosition = tpos;
                if (CaretPostionChangedByUser != null)
                    CaretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));
                EndUpdate();
            }
            if (Mouse.Captured == myImage)
                Mouse.Capture(null);
        }

        private void myImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (btndrag)
                return;


            double pos = e.GetPosition(myImage).X;
            if (Math.Abs(downpos - pos) > 1 && e.LeftButton == MouseButtonState.Pressed && !btndrag)
            {
                TimeSpan ts1 = PosToTime(pos);
                TimeSpan ts2 = downtime;
                if (ts1 < ts2)
                {
                    SelectionBegin = ts1;
                    SelectionEnd = ts2;
                }
                else
                {
                    SelectionBegin = ts2;
                    SelectionEnd = ts1;
                }
            }
        }


        private void grid1_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            InvalidateWaveform();
            if (e.WidthChanged)
            {
                InvalidateSpeakers();
            }
        }


        public void slMediaPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (slMediaPosition.Maximum == slMediaPosition.Minimum)
                return;

            bool updating = _updating > 0;

            if (!updating)
            {
                if (SliderPositionChanged != null)
                    SliderPositionChanged(this, new TimeSpanEventArgs(TimeSpan.FromMilliseconds(slMediaPosition.Value)));

                BeginUpdate();
            }

            double wbg = e.NewValue - wave.LengthMS / 2;
            double wed = e.NewValue + wave.LengthMS / 2;

            if (wbg < 0)
            {
                wed -= wbg; // - a - je + :)
                wbg = 0;
            }

            if (wed > slMediaPosition.Maximum)
            {
                wed -= slMediaPosition.Maximum;
                wbg -= wed;
                wed = slMediaPosition.Maximum;
            }

            WaveBegin = TimeSpan.FromMilliseconds(wbg);
            WaveEnd = TimeSpan.FromMilliseconds(wed);

            CaretPosition = TimeSpan.FromMilliseconds(e.NewValue);

            if (CaretPostionChangedByUser != null && !updating)
                CaretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));


            if (!updating)
                EndUpdate();
            InvalidateWaveform();
        }

        private void slMediaPosition_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            slMediaPosition_ValueChanged(slMediaPosition, new RoutedPropertyChangedEventArgs<double>(slMediaPosition.Value, slMediaPosition.Value));
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateWaveform();
        }

        public int SetAudioData(short[] data, TimeSpan begin, TimeSpan end)
        {

            int val = wave.audioBuffer.CopyDataToBuffer(data, (long)begin.TotalMilliseconds, (long)end.TotalMilliseconds);
            Invalidate();
            return val;
        }

        public short[] GetAudioData(TimeSpan from, TimeSpan to, TimeSpan max)
        {
            return wave.audioBuffer.CopyFromBuffer(from, to, max);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Invalidator.ThreadState == System.Threading.ThreadState.Running)
                Invalidator.Interrupt();

        }

        private void grid1_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Invalidator != null && Invalidator.IsAlive)
                Invalidator.Interrupt();
        }

        private void rectangle2_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(selectionRectangle);

            if (p.X <= 4)
            {
                selectionRectangle.Cursor = Cursors.ScrollWE;
            }
            else if (p.X >= selectionRectangle.ActualWidth - 4)
            {
                selectionRectangle.Cursor = Cursors.ScrollWE;
            }
            else
                selectionRectangle.Cursor = null;
        }

        private void rectangle2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(selectionRectangle);
            if (p.X <= 4)
            {
                seldrag = true;
                downtime = SelectionEnd;
                downpos = TimeToPos(downtime);
                myImage.CaptureMouse();
            }
            else if (p.X >= selectionRectangle.ActualWidth - 4)
            {
                seldrag = true;
                downtime = SelectionBegin;
                downpos = TimeToPos(downtime);
                myImage.CaptureMouse();
            }
        }

        private double TimeToPos(TimeSpan downtime)
        {
            return (this.ActualWidth / (WaveEnd - WaveBegin).Ticks) * downtime.Ticks;
        }

        private TimeSpan PosToTime(double position)
        {
            double relative = position / this.ActualWidth;
            return TimeSpan.FromTicks((long)((WaveEnd - WaveBegin).Ticks * relative + WaveBegin.Ticks));
        }

        bool btndrag = false;
        bool seldrag = false;
        bool btndragleft = false;
        bool mouseOverParagraphDrag = false;
        void pSpeaker_MouseMove(object sender, MouseEventArgs e)
        {
            Button b = sender as Button;
            Point p = e.GetPosition(b);

            if (p.X <= 4)
            {
                b.Cursor = Cursors.ScrollWE;
            }
            else if (p.X >= b.Width - 4)
            {
                b.Cursor = Cursors.ScrollWE;
            }
            else
            {
                b.Cursor = Cursors.IBeam;
            }
            p = e.GetPosition(grid1);

            if (p.X > grid1.ActualWidth || p.X < 0)
                return;

            int ix = speakerButtons.IndexOf(b);

            Button bp = (ix == 0) ? null : speakerButtons[ix - 1];
            Button bn = (ix == speakerButtons.Count - 1) ? null : speakerButtons[ix + 1];

            TranscriptionElement current = b.Tag as TranscriptionElement;
            TranscriptionElement previons = (bp == null) ? null : bp.Tag as TranscriptionElement;
            TranscriptionElement next = (bn == null) ? null : bn.Tag as TranscriptionElement;

            int section = current.Parent.ParentIndex;
            int previousSection = (bp == null) ? -10 : previons.Parent.ParentIndex;
            int nextSection = (bn == null) ? -10 : next.Parent.ParentIndex;

            if (btndrag)
            {
                if (btndragleft)
                {

                    double bwi = b.Width + (b.Margin.Left - p.X);
                    double bmarginl = p.X;

                    TimeSpan ts = new TimeSpan((long)(WaveLength.Ticks * (bmarginl / grid1.ActualWidth))) + WaveBegin;
                    if (current.End - TimeSpan.FromMilliseconds(100) < ts)
                        return;
                    if ((bp != null && bmarginl < bp.Margin.Left + bp.Width) || mouseOverParagraphDrag) //kolize
                    {
                        if (section == previousSection)
                        {
                            mouseOverParagraphDrag = true;
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                            {
                                if (previons.Begin + TimeSpan.FromMilliseconds(100) > ts)
                                    return;
                                bp.Width = p.X - bp.Margin.Left;
                                previons.End = ts;
                                if (ElementChanged != null)
                                    ElementChanged(this, new MyTranscriptionElementEventArgs(previons));
                            }
                            else
                                mouseOverParagraphDrag = false;
                        }

                    }

                    current.Begin = ts;
                    b.Width = bwi;
                    b.Margin = new Thickness(bmarginl, b.Margin.Top, b.Margin.Right, b.Margin.Bottom);

                    if (ElementChanged != null)
                        ElementChanged(this, new MyTranscriptionElementEventArgs(current));
                }
                else
                {
                    double bwi = p.X - b.Margin.Left;

                    TimeSpan ts = new TimeSpan((long)(WaveLength.Ticks * ((bwi + b.Margin.Left) / grid1.ActualWidth))) + WaveBegin;
                    if (ts <= current.Begin + TimeSpan.FromMilliseconds(100))
                        return;

                    if ((bn != null && b.Margin.Left + b.Width > bn.Margin.Left) || mouseOverParagraphDrag)
                    {
                        if (section == nextSection)
                        {
                            mouseOverParagraphDrag = true;
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                            {
                                if (next.End - TimeSpan.FromMilliseconds(100) <= ts)
                                    return;

                                bn.Width = bn.Width + (bn.Margin.Left - p.X);
                                bn.Margin = new Thickness(p.X, bn.Margin.Top, bn.Margin.Right, bn.Margin.Bottom);
                                next.Begin = ts;
                                if (ElementChanged != null)
                                    ElementChanged(this, new MyTranscriptionElementEventArgs(next));
                            }
                            else
                                mouseOverParagraphDrag = false;
                        }
                    }
                    current.End = ts;

                    b.Width = bwi;

                    if (ElementChanged != null)
                        ElementChanged(this, new MyTranscriptionElementEventArgs(current));
                }
            }


        }


        void pSpeaker_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            Point position;
            Button b = sender as Button;

            position = e.GetPosition((Button)sender);

            if (btndrag)
            {
                InvalidateSpeakers();
                b.ReleaseMouseCapture();
            }
            btndrag = false;
            mouseOverParagraphDrag = false;


        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateWaveform();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            InvalidateWaveform();
        }


        public delegate short[] DataRequestDelegate(TimeSpan begin, TimeSpan end);

        public DataRequestDelegate DataRequestCallBack;

        Thread bufferProcessThread = null;
        TimeSpan requestedBegin = TimeSpan.Zero;
        TimeSpan requestedEnd = TimeSpan.Zero;
        private object wavelock = new object();
        private void AudioBufferCheck(TimeSpan value)
        {
            TimeSpan half = new TimeSpan(Const.DISPLAY_BUFFER_LENGTH.Ticks / 2);

            TimeSpan checkarea = new TimeSpan((AudioBufferEnd - AudioBufferBegin).Ticks / 5);
            TimeSpan innercheckE = AudioBufferEnd - checkarea;
            if (innercheckE > AudioLength || AudioBufferEnd >= AudioLength - TimeSpan.FromMilliseconds(10)) //magic number kvuli zaokrouhlovani
                innercheckE = AudioLength;
            TimeSpan innercheckB = AudioBufferBegin + checkarea;
            if (innercheckB < TimeSpan.Zero || AudioBufferBegin <= TimeSpan.FromMilliseconds(10))
                innercheckB = TimeSpan.Zero;


            TimeSpan outercheckE = requestedEnd - checkarea;
            if (outercheckE > AudioLength || AudioBufferEnd >= AudioLength - TimeSpan.FromMilliseconds(10))
                outercheckE = AudioLength;
            TimeSpan outercheckB = requestedBegin + checkarea;
            if (outercheckB < TimeSpan.Zero || AudioBufferBegin <= TimeSpan.FromMilliseconds(10))
                outercheckB = TimeSpan.Zero;

            if (
                (value > innercheckE ||
                value < innercheckB)  //nevejdeme se do nacteneho

                                &&

                (bufferProcessThread == null || (value > outercheckE ||
                value < outercheckB))) // nenacitame, nebo se nevejdeme se ani do nacitaneho
            {

                System.Diagnostics.Debug.WriteLine("check" + value);
                lock (wavelock)
                {
                    if (bufferProcessThread != null)
                    {
                        bufferProcessThread.Abort();
                        bufferProcessThread = null;
                    }
                }

                TimeSpan begin = value - half;
                TimeSpan end = value + half;

                if (begin < TimeSpan.Zero)
                {
                    begin = TimeSpan.Zero;
                    end = begin + half + half;
                }

                if (end > AudioLength)
                {
                    end = AudioLength;
                    begin = AudioLength - half - half;
                }
                if (begin < TimeSpan.Zero)
                {
                    begin = TimeSpan.Zero;
                    end = Const.DISPLAY_BUFFER_LENGTH;
                }
                requestedBegin = begin;
                requestedEnd = end;

                if (DataRequestCallBack != null)
                {
                    lock (wavelock) //vytvareni noveho delegata pocka pokud se uz nastavuje stary
                    {
                        requestedBegin = begin;
                        requestedEnd = end;
                        bufferProcessThread = new Thread(delegate()
                        {
                            short[] data = this.DataRequestCallBack(begin, end);
                            lock (wavelock)
                            {
                                wave.audioBuffer.CopyDataToBuffer(data, (long)begin.TotalMilliseconds, (long)end.TotalMilliseconds);
                                bufferProcessThread = null;
                            }
                            if (AutomaticProgressHighlight)
                            {
                                this.Dispatcher.Invoke((Action)(() =>
                                {
                                    slMediaPosition.SelectionStart = begin.TotalMilliseconds;
                                    slMediaPosition.SelectionEnd = end.TotalMilliseconds;
                                }));

                            }

                            InvalidateWaveform();
                        });
                        bufferProcessThread.Name = "waveform.bufferProcessThread.";
                    }
                    bufferProcessThread.Start();

                }


            }

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimespanToHourStringConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan ts = (TimeSpan)value;
            string label = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2");
            return label;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(double))]
    public class TimespanToMilisecondsConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan ts = (TimeSpan)value;
            return ts.TotalMilliseconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
