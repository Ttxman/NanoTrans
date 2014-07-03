using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for TimeLine.xaml
    /// </summary>
    public partial class TimeLine : UserControl
    {
        public TimeSpan Begin
        {
            get { return ( TimeSpan)GetValue(BeginProperty); }
            set { SetValue(BeginProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Begin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BeginProperty =
            DependencyProperty.Register("Begin", typeof( TimeSpan), typeof(TimeLine), new FrameworkPropertyMetadata(OnValueElementChanged));


        public TimeSpan End
        {
            get { return (TimeSpan)GetValue(EndProperty); }
            set { SetValue(EndProperty, value); }
        }

        // Using a DependencyProperty as the backing store for End.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndProperty =
            DependencyProperty.Register("End", typeof(TimeSpan), typeof(TimeLine),new FrameworkPropertyMetadata(OnValueElementChanged));



        public static void OnValueElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var t = d as TimeLine;
            t.Redraw();
        }


        

        public TimeLine()
        {
            InitializeComponent();
        }


        private void Redraw()
        {
            double beginms = Begin.TotalMilliseconds;
            double endms = End.TotalMilliseconds;
            double lengthms = endms - beginms;

            maingrid.Children.Clear();
            double length = lengthms / 1000;

            double shortStep = Math.Round(length / 30);
            if (shortStep < 1) shortStep = 1;
            double step = Math.Round(length / 5);

            int length_S = (int)Math.Round(length);

            if (length_S < 6)
            {
                step = 1;
                shortStep = 0.1;
            }
            else if (length_S >= 6 && length_S < 20)
            {
                step = 2;
                shortStep = 0.5;
            }
            else if (length_S >= 20 && length_S < 45)
            {
                step = 5;
                shortStep = 1;
            }
            else if (length_S >= 45 && length_S < 90)
            {
                step = 10;
                shortStep = 2;
            }
            else if (length_S >= 90 && length_S < 150)
            {
                step = 15;
                shortStep = 3;
            }
            else if (length_S >= 150)
            {
                step = 30;
                shortStep = 5;

            }

            double firstMarkBegin = Math.Ceiling(beginms / 1000 / step) * step;
            if (step < 1) step = 1;

            if (Math.Abs((int)step - 5) == 1)
            {
                step = 5;
            }
            double stepend = ((double)endms / 1000) - firstMarkBegin;

            for (double i = shortStep; i <= stepend; i = i + shortStep)
            {
                double aLeft = (firstMarkBegin + i) * 1000 - beginms;
                aLeft = aLeft / lengthms * this.ActualWidth;
                double pozice = aLeft;
                Rectangle r1 = new Rectangle();
                r1.Margin = new Thickness(pozice - 2, 0, 0, maingrid.ActualHeight / 3 * 2.5);
                r1.Height = maingrid.ActualHeight;
                r1.Width = 1;
                r1.HorizontalAlignment = HorizontalAlignment.Left;
                r1.Fill = Foreground;

                maingrid.Children.Add(r1);
            }

            for (double i = step; i <= length; i = i + step)
            {
                double aLeft = (firstMarkBegin + i) * 1000 - beginms;
                aLeft = aLeft / lengthms * this.ActualWidth;
                double pozice = aLeft;

                TimeSpan ts = new TimeSpan((long)(firstMarkBegin * 1000 + i * 1000) * 10000);

                Label lX = new Label();
                lX.Content = Math.Floor(ts.TotalMinutes).ToString() + "m:" + ts.Seconds.ToString("D2") + "s";
                lX.Margin = new Thickness(pozice - 32, 0, 0, 0);
                lX.Padding = new Thickness(0, 5, 0, 0);

                Rectangle r1 = new Rectangle();
                r1.Margin = new Thickness(pozice - 2, 0, 0, maingrid.ActualHeight / 3 * 2);
                r1.Height = maingrid.ActualHeight;
                r1.Width = 2;
                r1.HorizontalAlignment = HorizontalAlignment.Left;
                r1.Fill = Foreground;

                maingrid.Children.Add(lX);
                maingrid.Children.Add(r1);


            }
        }

        private void control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

    }
}
