﻿using System;
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

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for Waveform.xaml
    /// </summary>
    public partial class Waveform : UserControl
    {

        public TimeSpan WaveBegin
        {
            get { return TimeSpan.FromMilliseconds(oVlna.mSekundyVlnyZac); }
            set
            {
                oVlna.mSekundyVlnyZac = (long)value.TotalMilliseconds;
                InvalidateWaveform();
                InvalidateTimeLine();
            }
        }

        public TimeSpan WaveEnd
        {
            get { return TimeSpan.FromMilliseconds(oVlna.mSekundyVlnyKon); }
            set
            {
                oVlna.mSekundyVlnyKon = (long)value.TotalMilliseconds;
                InvalidateWaveform();
                InvalidateTimeLine();
            }
        }

        public TimeSpan WaveLength
        {
            get { return TimeSpan.FromMilliseconds(oVlna.DelkaVlnyMS); }
            set
            {
                oVlna.NastavDelkuVlny((long)value.TotalMilliseconds);
                InvalidateWaveform();
                InvalidateTimeLine();
            }
        }

        public TimeSpan WaveLengthDelta
        {
            get { return TimeSpan.FromMilliseconds(oVlna.MSekundyDelta); }
        }

        public TimeSpan SelectionBegin
        {
            get { return oVlna.KurzorVyberPocatek; }
            set
            {
                oVlna.KurzorVyberPocatek = value;


                InvalidateSelection();
            }
        }

        public TimeSpan SelectionEnd
        {
            get { return oVlna.KurzorVyberKonec; }
            set
            {
                oVlna.KurzorVyberKonec = value;
                InvalidateSelection();
            }
        }

        public TimeSpan CaretPosition
        {
            get { return TimeSpan.FromMilliseconds(oVlna.KurzorPoziceMS); }
            set
            {



                long pos = (long)value.TotalMilliseconds;
                if (pos == oVlna.KurzorPoziceMS)
                    return;

                this.AudioBufferCheck(value);
                oVlna.KurzorPoziceMS = pos;

                TimeSpan ts = value;
                string label = ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2") + "," + ((int)ts.Milliseconds / 10).ToString("D2");
                lAudioPozice.Content = label;

                InvalidateCarret();
                InvalidateSelection();

                if (m_updating > 0)
                    return;



                if (value < WaveBegin || value > WaveEnd)
                {
                    BeginUpdate();
                    SliderPostion = CaretPosition;
                    EndUpdate();
                }



                if (m_updating == 0)
                    if (CarretPostionChanged != null)
                        CarretPostionChanged(this, new TimeSpanEventArgs(value));
            }
        }


        public TimeSpan SliderPostion
        {
            get
            {
                return TimeSpan.FromMilliseconds(slPoziceMedia.Value);
            }
            set
            {
                slPoziceMedia.Value = value.TotalMilliseconds;
            }
        }

        public TimeSpan AudioLength
        {
            get { return TimeSpan.FromMilliseconds(slPoziceMedia.Maximum); }
            set
            {
                slPoziceMedia.Maximum = value.TotalMilliseconds;
                lAudioDelka2.Content = value.Hours.ToString("d1") + ":" + value.Minutes.ToString("d2") + ":" + value.Seconds.ToString("d2");
            }
        }

        public TimeSpan SmallJump
        {
            get { return TimeSpan.FromMilliseconds(oVlna.mSekundyMalySkok); }
            set
            {
                oVlna.mSekundyMalySkok = (long)value.TotalMilliseconds;
                InvalidateWaveform();
            }
        }

        public double Scale
        {
            get { return 0.01 * oVlna.ZvetseniVlnyYSmerProcenta; }
            set
            {
                oVlna.ZvetseniVlnyYSmerProcenta = (int)(value * 100);
                oVlna.AutomatickeMeritko = false;
                InvalidateWaveform();
            }
        }

        public bool ScaleAutomaticaly
        {
            get { return oVlna.AutomatickeMeritko; }
            set
            {
                oVlna.AutomatickeMeritko = value;
                InvalidateWaveform();
            }
        }

        public TimeSpan ProgressHighlightBegin
        {
            get { return TimeSpan.FromMilliseconds(slPoziceMedia.SelectionStart); }
            set
            {
                if (!m_autohighlight)
                    slPoziceMedia.SelectionStart = value.TotalMilliseconds;
            }
        }

        public TimeSpan ProgressHighlightEnd
        {
            get { return TimeSpan.FromMilliseconds(slPoziceMedia.SelectionEnd); }
            set
            {
                if (!m_autohighlight)
                    slPoziceMedia.SelectionEnd = value.TotalMilliseconds;
            }
        }

        public TimeSpan AudioBufferBegin
        {
            get { return TimeSpan.FromMilliseconds(oVlna.bufferPrehravaniZvuku.PocatekMS); }
        }

        public TimeSpan AudioBufferEnd
        {
            get { return TimeSpan.FromMilliseconds(oVlna.bufferPrehravaniZvuku.KonecMS); }
        }


        private bool m_autohighlight = false;
        public bool AutomaticProgressHighlight
        {
            get { return m_autohighlight; }
            set
            {
                m_autohighlight = value;
                if (value)
                {
                    slPoziceMedia.SelectionStart = AudioBufferBegin.TotalMilliseconds;
                    slPoziceMedia.SelectionEnd = AudioBufferEnd.TotalMilliseconds;
                }
            }
        }


        private int m_frequency = 16000;
        public int AudioFrequency
        {
            get { return m_frequency; }
            set { m_frequency = value; }
        }

        private bool m_playing;
        public bool Playing
        {
            get { return m_playing; }
            set
            {
                m_playing = value;


                if (this.Dispatcher.Thread == System.Threading.Thread.CurrentThread)
                {
                    if (m_playing)
                    {

                        BitmapImage bi3 = new BitmapImage();
                        bi3.BeginInit();
                        bi3.UriSource = new Uri("../icons/iPause.png", UriKind.Relative);
                        bi3.EndInit();

                        iPlayPause.Source = bi3;
                    }
                    else
                    {
                        BitmapImage bi3 = new BitmapImage();
                        bi3.BeginInit();
                        bi3.UriSource = new Uri("../icons/iPlay.png", UriKind.Relative);
                        bi3.EndInit();
                        iPlayPause.Source = bi3;
                    }
                }
            }
        }

        private Transcription m_subtitlesData;


        public static readonly DependencyProperty SubtitlesProperty = DependencyProperty.Register("Subtitles", typeof(Transcription), typeof(Waveform), new FrameworkPropertyMetadata(OnSubtitlesChanged));

        public static void OnSubtitlesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            ((Waveform)d).m_subtitlesData = (Transcription)e.NewValue;
            Transcription value = (Transcription)e.NewValue;
            if (value.Count > 0)
            {
                TranscriptionElement last = value.Last();
                if (last.Begin > last.End)
                    ((Waveform)d).slPoziceMedia.Maximum = last.Begin.TotalMilliseconds;
                else
                    ((Waveform)d).slPoziceMedia.Maximum = last.End.TotalMilliseconds;
            }

            ((Waveform)d).Invalidate();
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
                m_subtitlesData = value;
                Invalidate();
            }
        }




        /// <summary>
        /// informace o vyberu a pozici kurzoru ve "vlne", obsahuje buffery pro zobrazeni a prehrani vlny
        /// </summary>
        private MyVlna oVlna = new MyVlna(MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU_MS);

        public event RoutedEventHandler PlayPauseClick;
        public event EventHandler<TimeSpanEventArgs> SliderPositionChanged;
        public event EventHandler UpdateBegin;
        public event EventHandler UpdateEnd;


        public event EventHandler<TimeSpanEventArgs> CarretPostionChangedByUser;
        public event EventHandler<TimeSpanEventArgs> CarretPostionChanged;
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
        private bool invalidate_selection = true;
        private bool invalidate_timeline = true;
        private bool invalidate_carret = true;

        private Thread Invalidator;
        public Waveform()
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

        /// <summary>
        /// prekresluje interface z vlastniho vlakna (omezeni poctu prekresleni pri caste zmene hodnot - jako tahnuti mysi)
        /// </summary>
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

                    if (invalidate_selection)
                    {
                        this.Dispatcher.Invoke(new Action(iInvalidateSelection));
                    }

                    if (invalidate_timeline)
                    {
                        this.Dispatcher.Invoke(new Action(iInvalidateTimeLine));
                    }

                    if (invalidate_carret)
                    {
                        this.Dispatcher.Invoke(new Action(iInvalidateCarret));
                    }



                    //each drawing should know if it is drawn by itself
                    //invalidate_waveform = false;

                    invalidate_speakers = false;
                    invalidate_selection = false;
                    invalidate_timeline = false;
                    invalidate_carret = false;
                }
            }
            catch (ThreadInterruptedException) { }
        }


        private int m_updating = 0;
        public void BeginUpdate()
        {
            if (UpdateBegin != null)
                UpdateBegin(this, new EventArgs());
            m_updating++;
        }

        public void EndUpdate()
        {
            if (UpdateEnd != null)
                UpdateEnd(this, new EventArgs());
            m_updating--;
            if (m_updating < 0)
                m_updating = 0;
        }


        /// <summary>
        /// nakresli vlnu do formulare
        /// </summary>
        public void InvalidateWaveform()
        {
            invalidate_waveform = true;
        }

        public void Invalidate()
        {
            InvalidateSelection();
            InvalidateSpeakers();
            InvalidateTimeLine();
            InvalidateWaveform();
            InvalidateCarret();
        }


        private void iInvalidateWaveform()
        {
            Debug.WriteLine(WaveBegin);
            Debug.WriteLine(WaveEnd);

            Debug.WriteLine(AudioBufferBegin);
            Debug.WriteLine(AudioBufferEnd);

            bool notbuffered = WaveBegin < AudioBufferBegin || WaveEnd > AudioBufferEnd;


            Debug.WriteLine(notbuffered);
            Debug.WriteLine("-----------------------------------------");

            if (notbuffered)
            {
                myImage.Source = null;
                //invalidate_waveform = true; invalidate_waveform is true when this function is called
                return;
            }

            double zacatek = oVlna.mSekundyVlnyZac;
            double konec = oVlna.mSekundyVlnyKon;
            try
            {
                if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
                {
                    this.Dispatcher.Invoke(new Action(InvalidateWaveform));
                    return;
                }


                Int64 zac = (Int64)(0.001 * m_frequency * (zacatek - oVlna.bufferPrehravaniZvuku.PocatekMS));
                Int64 kon = (Int64)(0.001 * m_frequency * (konec - oVlna.bufferPrehravaniZvuku.PocatekMS));


                ///////////////////////////////////////////////////////////////////////////////
                if (zac < -100)
                {
                    return; //pockani na nacteni bufferu z disku o spravnem rozsahu
                }
                //////////////////////////////////////////////////////////////////////////////

                if (konec > oVlna.bufferPrehravaniZvuku.KonecMS)
                {
                    kon = (Int64)(((double)(oVlna.bufferPrehravaniZvuku.KonecMS - oVlna.bufferPrehravaniZvuku.PocatekMS)) / 1000 * m_frequency);// - 1;
                    zac = (Int64)(kon - (oVlna.DelkaVlnyMS / 1000.0) * m_frequency);
                }


                //oVlna.mSekundyVlnyZac = 1000 * zac / m_frequency + oVlna.bufferPrehravaniZvuku.PocatekMS;
                //oVlna.mSekundyVlnyKon = (long)((double)1000 * kon / m_frequency) + oVlna.bufferPrehravaniZvuku.PocatekMS;
                long mSekundyKonec = oVlna.mSekundyVlnyKon;
                if (zac < 0)
                {
                    zac = 0;
                    if (kon < 0) kon = zac + 30 * m_frequency;//pokud je vse mimo rozsah bufferu,je nakreslena vlna delky 30s
                    oVlna.mSekundyVlnyZac = 1000 * zac / m_frequency + oVlna.bufferPrehravaniZvuku.PocatekMS;
                    oVlna.mSekundyVlnyKon = 1000 * kon / m_frequency + oVlna.bufferPrehravaniZvuku.PocatekMS;
                    mSekundyKonec = (int)(oVlna.mSekundyVlnyKon);
                    oVlna.NastavDelkuVlny((uint)((kon - zac) * 1000 / m_frequency));
                }

                int pPocetZobrazovanychPixelu = (int)this.ActualWidth;
                if (pPocetZobrazovanychPixelu <= 0) pPocetZobrazovanychPixelu = 1600;
                int pKolikVzorkuKomprimovat = (int)((double)(kon - zac) / (double)pPocetZobrazovanychPixelu);




                //oVlna.bufferPrehravaniZvuku.data.Skip(zac).Take(kon-zac).

                double[] wavemax = new double[pPocetZobrazovanychPixelu];
                double[] wavemin = new double[pPocetZobrazovanychPixelu];
                for (int i = 0; i < pPocetZobrazovanychPixelu; i++)
                {
                    double min = short.MaxValue;
                    double max = short.MinValue;

                    long pos = zac + i * pKolikVzorkuKomprimovat;
                    for (long k = pos; k < pos + pKolikVzorkuKomprimovat; k++)
                    {
                        short val = oVlna.bufferPrehravaniZvuku.data[k];
                        if (val < min)
                            min = val;
                        if (val > max)
                            max = val;
                    }


                    //normalizace;
                    min /=((double)short.MaxValue);
                    max /= ((double)short.MaxValue);

                    wavemin[i] = min;
                    wavemax[i] = max;
                }

                float pZvetseni = oVlna.ZvetseniVlnyYSmerProcenta/100;

                if (!oVlna.AutomatickeMeritko)
                {
                    if (pZvetseni < 0.1)
                        pZvetseni = 0.1f;
                    if (pZvetseni > 100)
                        pZvetseni = 100f;

                    oVlna.ZvetseniVlnyYSmerProcenta = pZvetseni * 100;

                    double factor = pZvetseni;
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
                else //autozoom
                {
                    double min = Math.Abs(wavemin.Min());
                    double max = Math.Abs(wavemax.Max());
                    double factor = 1/Math.Max(min, max);
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
                for (int i = 0; i < wavemin.Length-1;i++)
                    myGeometryGroup.Children.Add(new LineGeometry(new Point(i,wavemax[i]),new Point(i,wavemin[i])));

                //scale height
                double imageheight = grid1.ActualHeight - this.myImage.Margin.Top - this.myImage.Margin.Bottom;

                GeometryDrawing myGeometryDrawing = new GeometryDrawing(Brushes.Red, new Pen(Brushes.Red, 1), myGeometryGroup);


                myGeometryGroup = new GeometryGroup();
                myGeometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(wavemin.Length, 0)));
                var hline = new GeometryDrawing(Brushes.Red, new Pen(Brushes.Red, 0.5 / imageheight), myGeometryGroup);
                



                //heighth line
                myGeometryGroup = new GeometryGroup();
                myGeometryGroup.Children.Add(new LineGeometry(new Point(0, 1), new Point(0, -1)));
                var vline = new GeometryDrawing(Brushes.Transparent,new Pen(Brushes.Transparent,1),myGeometryGroup);
                
                
                
                DrawingGroup myDrawingGroup = new DrawingGroup();
                myDrawingGroup.Children.Add(vline);
                myDrawingGroup.Children.Add(hline);
                myDrawingGroup.Children.Add(myGeometryDrawing);

                myDrawingGroup.Transform = new ScaleTransform(1, imageheight / 2);
                
                
                //myDrawingGroup.



                DrawingImage myDrawingImage = new DrawingImage(myDrawingGroup);
                InvalidateTimeLine();
                KresliVlnuAOstatni(myDrawingImage);
                invalidate_waveform = false;
            }
            catch// (Exception ex)
            {

            }


        }


        private void InvalidateSelection()
        {
            invalidate_selection = true;
        }
        //nakresli vyber 
        private void iInvalidateSelection()//long zacatek, long konec, long kurzor)
        {
            long zacatek = (long)oVlna.KurzorVyberPocatek.TotalMilliseconds;
            long konec = (long)oVlna.KurzorVyberKonec.TotalMilliseconds;
            if (konec < zacatek)
            {
                long bf = konec;
                konec = zacatek;
                zacatek = konec;
            }
            long kurzor = oVlna.KurzorPoziceMS;

            if ((zacatek != -1 && konec != -1) || (zacatek != -1))
            {
                if (zacatek < oVlna.mSekundyVlnyZac)
                {
                    if (konec > oVlna.mSekundyVlnyKon)
                    {
                        rectangle2.Margin = new Thickness(0, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                        rectangle2.Width = myImage.ActualWidth;
                        rectangle2.Visibility = Visibility.Visible;
                    }
                    else if (konec > oVlna.mSekundyVlnyZac)
                    {
                        rectangle2.Margin = new Thickness(0, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                        rectangle2.Width = (double)(myImage.ActualWidth * (konec - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);
                        rectangle2.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        rectangle2.Visibility = Visibility.Hidden;
                    }
                }
                else if (konec > oVlna.mSekundyVlnyKon)//zacatek vyberu  je vetsi nez zacatek vlny
                {
                    if (zacatek < oVlna.mSekundyVlnyKon)
                    {
                        double aLeft = (double)(myImage.ActualWidth * (double)(zacatek - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);
                        rectangle2.Margin = new Thickness(aLeft, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                        rectangle2.Width = myImage.ActualWidth - aLeft;
                        rectangle2.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        rectangle2.Visibility = Visibility.Hidden;
                    }
                }
                else //zac a kon v intervalu vlny
                {
                    double aLeft = (double)(myImage.ActualWidth * (zacatek - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);

                    double aRight = myImage.ActualWidth - (double)(myImage.ActualWidth * (konec - oVlna.mSekundyVlnyZac) / oVlna.DelkaVlnyMS);
                    rectangle2.Margin = new Thickness(aLeft, rectangle2.Margin.Top, rectangle2.Margin.Right, rectangle2.Margin.Bottom);
                    rectangle2.Width = myImage.ActualWidth - aLeft - aRight;
                    rectangle2.Visibility = Visibility.Visible;
                }
            }
            else
            {
                rectangle2.Visibility = Visibility.Hidden;
            }

        }

        private void iInvalidateTimeLine()
        {
            long zacatek = oVlna.mSekundyVlnyZac;
            long konec = oVlna.mSekundyVlnyKon;
            //casova osa
            gCasovaOsa.Children.Clear();
            double delka = (double)((double)(konec - zacatek) / 1000);

            double krokMaly = Math.Round(delka / 30);
            if (krokMaly < 1) krokMaly = 1;
            double krok = Math.Round(delka / 5);
            long pPocatekPrvniZnacky_S = zacatek / 1000;
            int pDelka_S = (int)Math.Round(delka);

            if (pDelka_S < 6)
            {
                krok = 1;
                krokMaly = 0.1;
            }
            else if (pDelka_S >= 6 && pDelka_S < 20)
            {
                krok = 2;
                krokMaly = 0.5;
            }
            else if (pDelka_S >= 20 && pDelka_S < 45)
            {
                krok = 5;
                krokMaly = 1;
            }
            else if (pDelka_S >= 45 && pDelka_S < 90)
            {
                krok = 10;
                krokMaly = 2;
            }
            else if (pDelka_S >= 90 && pDelka_S < 150)
            {
                krok = 15;
                krokMaly = 3;
            }
            else if (pDelka_S >= 150)
            {
                krok = 30;
                krokMaly = 5;

            }
            while (pPocatekPrvniZnacky_S % krok != 0 && pPocatekPrvniZnacky_S > 0)
            {
                pPocatekPrvniZnacky_S--;
            }



            if (krok < 1) krok = 1;

            if (Math.Abs((int)krok - 5) == 1)
            {
                krok = 5;
            }
            double pKonecKroku = ((double)konec / 1000) - pPocatekPrvniZnacky_S;
            //for (double i = krokMaly; i <= delka; i = i + krokMaly)
            for (double i = krokMaly; i <= pKonecKroku; i = i + krokMaly)
            {
                double aLeft = (pPocatekPrvniZnacky_S + i) * 1000 - oVlna.mSekundyVlnyZac;
                aLeft = aLeft / oVlna.DelkaVlnyMS * myImage.ActualWidth;
                double pozice = aLeft;
                Rectangle r1 = new Rectangle();
                r1.Margin = new Thickness(pozice - 2, 0, 0, gCasovaOsa.ActualHeight / 3 * 2.5);
                r1.Height = gCasovaOsa.ActualHeight;
                r1.Width = 1;
                r1.HorizontalAlignment = HorizontalAlignment.Left;
                r1.Fill = Brushes.Black;

                gCasovaOsa.Children.Add(r1);
            }

            for (double i = krok; i <= delka; i = i + krok)
            {
                double aLeft = (pPocatekPrvniZnacky_S + i) * 1000 - oVlna.mSekundyVlnyZac;
                aLeft = aLeft / oVlna.DelkaVlnyMS * myImage.ActualWidth;
                double pozice = aLeft;

                TimeSpan ts = new TimeSpan((long)(pPocatekPrvniZnacky_S * 1000 + i * 1000) * 10000);

                Label lX = new Label();
                lX.Content = Math.Floor(ts.TotalMinutes).ToString() + "m : " + ts.Seconds.ToString("D2") + "s";
                lX.Margin = new Thickness(pozice - 32, 0, 0, 0);
                lX.Padding = new Thickness(0, 5, 0, 0);

                Rectangle r1 = new Rectangle();
                r1.Margin = new Thickness(pozice - 2, 0, 0, gCasovaOsa.ActualHeight / 3 * 2);
                r1.Height = gCasovaOsa.ActualHeight;
                r1.Width = 2;
                r1.HorizontalAlignment = HorizontalAlignment.Left;
                r1.Fill = Brushes.Black;

                gCasovaOsa.Children.Add(lX);
                gCasovaOsa.Children.Add(r1);


            }
        }

        public void iInvalidateCarret()
        {
            double aLeft = (CaretPosition - WaveBegin).TotalMilliseconds;
            aLeft = aLeft / WaveLength.TotalMilliseconds * myImage.ActualWidth;

            rectangle1.Margin = new Thickness(aLeft + rectangle1.Width / 2, rectangle1.Margin.Top, rectangle1.Margin.Right, rectangle1.Margin.Bottom);

        }

        public void InvalidateCarret()
        {
            invalidate_carret = true;
        }
        public void InvalidateTimeLine()
        {
            invalidate_timeline = true;
        }

        public void InvalidateSpeakers()
        {
            invalidate_speakers = true;
        }


        private List<Button> bObelnikyMluvcich = new List<Button>();
        private void iInvalidateSpeakers()
        {
            try
            {
                Transcription aDokument = m_subtitlesData;
                MyVlna aZobrazenaVlna = oVlna;

                if (aDokument == null) return;
                if (aZobrazenaVlna == null) return;
                //smazani obdelniku mluvcich
                foreach (Button pL in bObelnikyMluvcich)
                {
                    grid1.Children.Remove(pL);
                }
                this.bObelnikyMluvcich.Clear();

                for (int i = 0; i < aDokument.Chapters.Count; i++)
                {
                    TranscriptionChapter pChapter = aDokument.Chapters[i];
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
                                if ((pBegin < aZobrazenaVlna.mSekundyVlnyZac && pEnd < aZobrazenaVlna.mSekundyVlnyZac) || (pBegin > aZobrazenaVlna.mSekundyVlnyKon))
                                {

                                }
                                else
                                {

                                    if (pBegin >= aZobrazenaVlna.mSekundyVlnyZac && pEnd <= aZobrazenaVlna.mSekundyVlnyKon)
                                    {

                                    }
                                    if (pBegin < aZobrazenaVlna.mSekundyVlnyZac) pBegin = aZobrazenaVlna.mSekundyVlnyZac;
                                    if (pEnd > aZobrazenaVlna.mSekundyVlnyKon) pEnd = aZobrazenaVlna.mSekundyVlnyKon;

                                    Button pMluvci = new Button();


                                    pMluvci.PreviewMouseMove += new MouseEventHandler(pMluvci_MouseMove);
                                    pMluvci.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(pMluvci_PreviewMouseLeftButtonUp);
                                    pMluvci.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(pMluvci_PreviewMouseLeftButtonDown);

                                    pMluvci.VerticalAlignment = VerticalAlignment.Top;
                                    pMluvci.HorizontalAlignment = HorizontalAlignment.Left;

                                    double aLeft = (double)(myImage.ActualWidth * (pBegin - aZobrazenaVlna.mSekundyVlnyZac) / aZobrazenaVlna.DelkaVlnyMS);
                                    double aRight = gCasovaOsa.ActualWidth - (double)(myImage.ActualWidth * (pEnd - aZobrazenaVlna.mSekundyVlnyZac) / aZobrazenaVlna.DelkaVlnyMS);
                                    pMluvci.Margin = new Thickness(aLeft, pMluvci.Margin.Top, pMluvci.Margin.Right, rectangle2.Margin.Bottom);
                                    pMluvci.Width = gCasovaOsa.ActualWidth - aLeft - aRight;

                                    pMluvci.Background = Brushes.LightPink;
                                    Speaker pSpeaker = aDokument.Speakers.VratSpeakera(pParagraph.speakerID);
                                    string pText = "";
                                    if (pSpeaker != null) pText = pSpeaker.FullName;
                                    //pMluvci.Content = pText;
                                    pMluvci.Visibility = Visibility.Visible;
                                    pMluvci.BringIntoView();
                                    pMluvci.Focusable = false;
                                    pMluvci.IsTabStop = false;
                                    pMluvci.Cursor = Cursors.Arrow;
                                    //pMluvci.IsHitTestVisible = false;
                                    if (pText != null && pText != "") pMluvci.ToolTip = pMluvci.Content;
                                    pMluvci.Tag = pParagraph;
                                    pMluvci.Click += new RoutedEventHandler(pMluvci_Click);
                                    pMluvci.MouseDoubleClick += new MouseButtonEventHandler(pMluvci_MouseDoubleClick);


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
                                        )//posledni zaznam v sekci
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
                                    pMluvci.Content = dp;

                                    pMluvci.SizeChanged += new SizeChangedEventHandler(pMluvci_SizeChanged);

                                    this.bObelnikyMluvcich.Add(pMluvci);
                                }
                            }
                        }
                    }
                }

                TranscriptionParagraph pPredchozi = null;
                TranscriptionParagraph pNasledujici = null;
                for (int i = 0; i < this.bObelnikyMluvcich.Count; i++)
                {
                    Button pMluvci = this.bObelnikyMluvcich[i];
                    TranscriptionParagraph pPar = pMluvci.Tag as TranscriptionParagraph;
                    if (i < this.bObelnikyMluvcich.Count - 1)
                    {
                        pNasledujici = this.bObelnikyMluvcich[i + 1].Tag as TranscriptionParagraph;
                    }
                    if (pPredchozi != null && pPredchozi.End > pPar.Begin && bObelnikyMluvcich[i - 1].Margin.Top < 5)
                    {
                        grid1.Children.Add(pMluvci);
                        grid1.UpdateLayout();
                        pMluvci.Margin = new Thickness(pMluvci.Margin.Left, pMluvci.ActualHeight, pMluvci.Margin.Right, pMluvci.Margin.Bottom);
                    }
                    else
                    {
                        grid1.Children.Add(pMluvci);
                    }
                    pPredchozi = pPar;
                }


            }
            catch { }
        }

        void pMluvci_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        /// <summary>
        /// Thread SAFE - preda data source imagi vlny z threadu
        /// </summary>
        /// <param name="e"></param>
        private void KresliVlnuAOstatni(DrawingImage aImage)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                //this.Dispatcher.Invoke(new DelegatKresliVlnu(KresliVlnuB), new object[] { aParametry });
                this.Dispatcher.Invoke(new Action<DrawingImage>(KresliVlnuAOstatni), new object[] { aImage });
                return;
            }
            if (aImage != null)
            {
                myImage.Source = aImage;
                myImage.UpdateLayout();
            }


            //casova osa
            InvalidateSpeakers();
        }

        private void btPosunLevo_Click(object sender, RoutedEventArgs e)
        {
            BeginUpdate();

            oVlna.mSekundyVlnyZac -= oVlna.DelkaVlnyMS / 10;


            if (oVlna.mSekundyVlnyZac < 0)
                oVlna.mSekundyVlnyZac = 0;

            oVlna.mSekundyVlnyKon = oVlna.mSekundyVlnyZac + oVlna.DelkaVlnyMS;

            double caretpos = CaretPosition.TotalMilliseconds - oVlna.DelkaVlnyMS / 10;
            if (caretpos < 0)
                caretpos = 0;
            this.CaretPosition = TimeSpan.FromMilliseconds(caretpos);
            if (CarretPostionChangedByUser != null)
                CarretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));
            InvalidateWaveform();

            EndUpdate();
        }

        private void btPosunPravo_Click(object sender, RoutedEventArgs e)
        {
            BeginUpdate();

            oVlna.mSekundyVlnyKon = oVlna.mSekundyVlnyKon + oVlna.DelkaVlnyMS / 10;

            if (AudioLength.TotalMilliseconds < oVlna.mSekundyVlnyKon)
                oVlna.mSekundyVlnyKon = (long)AudioLength.TotalMilliseconds;
            oVlna.mSekundyVlnyZac = oVlna.mSekundyVlnyKon - oVlna.DelkaVlnyMS;

            this.CaretPosition = TimeSpan.FromMilliseconds(CaretPosition.TotalMilliseconds + oVlna.DelkaVlnyMS / 10);
            if (CarretPostionChangedByUser != null)
                CarretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));

            InvalidateWaveform();


            EndUpdate();
        }


        double downpos = 0;
        TimeSpan downtime = TimeSpan.Zero;
        //posun prehravaneho zvuku na danou pozici
        private void myImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            downpos = e.GetPosition(myImage).X;
            double pos = e.GetPosition(myImage).X;
            double relative = pos / myImage.ActualWidth;
            downtime = TimeSpan.FromTicks((long)((WaveEnd - WaveBegin).Ticks * relative + WaveBegin.Ticks));
            System.Diagnostics.Debug.WriteLine("mousedown " + downtime);
        }


        private void myImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            double pos = e.GetPosition(myImage).X;
            if (Math.Abs(downpos - pos) < 1)
            {
                BeginUpdate();
                double relative = pos / myImage.ActualWidth;

                TimeSpan tpos = TimeSpan.FromTicks((long)((WaveEnd - WaveBegin).Ticks * relative + WaveBegin.Ticks));
                System.Diagnostics.Debug.WriteLine("mouseup0 " + tpos);
                this.CaretPosition = tpos;
                System.Diagnostics.Debug.WriteLine("mouseup " + this.CaretPosition);
                if (CarretPostionChangedByUser != null)
                    CarretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));
                System.Diagnostics.Debug.WriteLine("mouseup2 " + this.CaretPosition);
                EndUpdate();
            }
        }

        private void myImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (btndrag)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
                r2drag = false;
            double pos = e.GetPosition(myImage).X;
            if (Math.Abs(downpos - pos) > 1 && e.LeftButton == MouseButtonState.Pressed && !r2drag && !btndrag)
            {
                double relative = pos / myImage.ActualWidth;
                //double relative2 = downpos / myImage.ActualWidth;

                TimeSpan ts1 = TimeSpan.FromTicks((long)((WaveEnd - WaveBegin).Ticks * relative + WaveBegin.Ticks));
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


        //co se stane kdyz je zmenena velikost image
        private void grid1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateWaveform();
        }


        public void slPoziceMedia_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bool updating = m_updating > 0;

            if (!updating)
                if (SliderPositionChanged != null)
                    SliderPositionChanged(this, new TimeSpanEventArgs(TimeSpan.FromMilliseconds(slPoziceMedia.Value)));




            if (!updating)
                BeginUpdate();



            double wbg = e.NewValue - oVlna.DelkaVlnyMS / 2;
            double wed = e.NewValue + oVlna.DelkaVlnyMS / 2;

            if (wbg < 0)
            {
                wed -= wbg; // - a - je + :)
                wbg = 0;
            }

            if (wed > slPoziceMedia.Maximum)
            {
                wed -= slPoziceMedia.Maximum;
                wbg -= wed;
                wed = slPoziceMedia.Maximum;
            }

            WaveBegin = TimeSpan.FromMilliseconds(wbg);
            WaveEnd = TimeSpan.FromMilliseconds(wed);

            CaretPosition = TimeSpan.FromMilliseconds(e.NewValue);

            if (CarretPostionChangedByUser != null && !updating)
                CarretPostionChangedByUser(this, new TimeSpanEventArgs(this.CaretPosition));


            if (!updating)
                EndUpdate();
            InvalidateWaveform();
        }


        private void slPoziceMedia_LostMouseCapture(object sender, MouseEventArgs e)
        {
        }

        private void slPoziceMedia_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void slPoziceMedia_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            slPoziceMedia_ValueChanged(slPoziceMedia, new RoutedPropertyChangedEventArgs<double>(slPoziceMedia.Value, slPoziceMedia.Value));
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateWaveform();
        }

        public int SetAudioData(short[] data, TimeSpan begin, TimeSpan end)
        {

            int val = oVlna.bufferPrehravaniZvuku.UlozDataDoBufferu(data, (long)begin.TotalMilliseconds, (long)end.TotalMilliseconds);
            Invalidate();
            return val;
        }

        public short[] GetAudioData(TimeSpan from, TimeSpan to, TimeSpan max)
        {
            return oVlna.bufferPrehravaniZvuku.VratDataBufferuShort(from, to, max);
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


        private bool r2drag = false;
        private bool r2dragLeft = false;


        private void rectangle2_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(rectangle2);

            if (p.X <= 4)
            {
                rectangle2.Cursor = Cursors.ScrollWE;
            }
            else if (p.X >= rectangle2.Width - 4)
            {
                rectangle2.Cursor = Cursors.ScrollWE;
            }
            else
            {
                rectangle2.Cursor = Cursors.IBeam;
            }

            if (r2drag)
            {
                p = e.GetPosition(myImage);
                TimeSpan x = new TimeSpan((long)(p.X / myImage.ActualWidth * (WaveEnd - WaveBegin).Ticks + WaveBegin.Ticks));


                if (r2dragLeft)
                {
                    SelectionBegin = x;
                }
                else
                {
                    SelectionEnd = x;
                }
            }
        }

        private void rectangle2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TimeSpan selbeg = SelectionBegin;
            TimeSpan selend = SelectionEnd;

            Point p = e.GetPosition(rectangle2);
            rectangle2.CaptureMouse();
            if (p.X <= 4)
            {
                r2drag = true;
                r2dragLeft = true;
                Mouse.Capture(rectangle2, CaptureMode.Element);
            }
            else if (p.X >= rectangle2.Width - 4)
            {
                r2drag = true;
                r2dragLeft = false;
                Mouse.Capture(rectangle2, CaptureMode.Element);
            }
        }

        private void rectangle2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            r2drag = false;
            rectangle2.ReleaseMouseCapture();
        }

        bool btndrag = false;
        bool btndragleft = false;
        bool btnrozhr = false;
        void pMluvci_MouseMove(object sender, MouseEventArgs e)
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

            if (p.X > grid1.ActualWidth || p.X<0)
                return;

            int ix = bObelnikyMluvcich.IndexOf(b);

            Button bp = (ix == 0) ? null : bObelnikyMluvcich[ix - 1];
            Button bn = (ix == bObelnikyMluvcich.Count - 1) ? null : bObelnikyMluvcich[ix + 1];

            TranscriptionElement pAktualni = b.Tag as TranscriptionElement;
            TranscriptionElement pPredchozi = (bp == null) ? null : bp.Tag as TranscriptionElement;
            TranscriptionElement pNasledujici = (bn == null) ? null : bn.Tag as TranscriptionElement;

            int sekce = pAktualni.Parent.ParentIndex;
            int sekcepred = (bp == null) ? -10 : pPredchozi.Parent.ParentIndex;
            int sekceza = (bn == null) ? -10 : pNasledujici.Parent.ParentIndex;

            if (btndrag)
            {
                if (btndragleft)
                {
                   
                    double bwi = b.Width + (b.Margin.Left - p.X);
                    double bmarginl = p.X;

                    TimeSpan ts = new TimeSpan((long)(WaveLength.Ticks * (bmarginl / grid1.ActualWidth))) + WaveBegin;
                    if (pAktualni.End - TimeSpan.FromMilliseconds(100) < ts)
                        return;
                    if ((bp != null && bmarginl < bp.Margin.Left + bp.Width) || btnrozhr) //kolize
                    {
                        if (sekce == sekcepred)
                        {
                            btnrozhr = true;
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                            {
                                if (pPredchozi.Begin + TimeSpan.FromMilliseconds(100) > ts)
                                    return;
                                bp.Width = p.X - bp.Margin.Left;
                                pPredchozi.End = ts;
                                if (ElementChanged != null)
                                    ElementChanged(this, new MyTranscriptionElementEventArgs(pPredchozi));
                            }
                            else
                                btnrozhr = false;
                        }

                    }

                    pAktualni.Begin = ts;
                    b.Width = bwi;
                    b.Margin = new Thickness(bmarginl, b.Margin.Top, b.Margin.Right, b.Margin.Bottom);

                    if (ElementChanged != null)
                        ElementChanged(this, new MyTranscriptionElementEventArgs(pAktualni));
                }
                else
                {
                    double bwi = p.X - b.Margin.Left;

                    TimeSpan ts = new TimeSpan((long)(WaveLength.Ticks * ((bwi + b.Margin.Left) / grid1.ActualWidth))) + WaveBegin;
                    if (ts <= pAktualni.Begin + TimeSpan.FromMilliseconds(100))
                        return;

                    if ((bn != null && b.Margin.Left + b.Width > bn.Margin.Left) || btnrozhr)
                    {
                        if (sekce == sekceza)
                        {
                            btnrozhr = true;
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                            {
                                if (pNasledujici.End - TimeSpan.FromMilliseconds(100) <= ts)
                                    return;

                                bn.Width = bn.Width + (bn.Margin.Left - p.X);
                                bn.Margin = new Thickness(p.X, bn.Margin.Top, bn.Margin.Right, bn.Margin.Bottom);
                                pNasledujici.Begin = ts;
                                if (ElementChanged != null)
                                    ElementChanged(this, new MyTranscriptionElementEventArgs(pNasledujici));
                            }
                            else
                                btnrozhr = false;
                        }
                    }
                    pAktualni.End = ts;

                    b.Width = bwi;

                    if (ElementChanged != null)
                        ElementChanged(this, new MyTranscriptionElementEventArgs(pAktualni));
                }
            }


        }


        void pMluvci_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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
            btnrozhr = false;


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
            TimeSpan half = new TimeSpan(MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU.Ticks / 2);

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
                    end = MyKONST.DELKA_VYCHOZIHO_ZOBRAZOVACIHO_BUFFERU;
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
                                oVlna.bufferPrehravaniZvuku.UlozDataDoBufferu(data, (long)begin.TotalMilliseconds, (long)end.TotalMilliseconds);
                                bufferProcessThread = null;
                            }
                            if (AutomaticProgressHighlight)
                            {
                                this.Dispatcher.Invoke((Action)(() =>
                                {
                                    slPoziceMedia.SelectionStart = begin.TotalMilliseconds;
                                    slPoziceMedia.SelectionEnd = end.TotalMilliseconds;
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

    }
}
