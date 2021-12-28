﻿using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Interaction logic for SpeakerSmall.xaml
    /// </summary>
    public partial class SpeakerSmall : UserControl
    {
        public static readonly DependencyProperty SpeakerProperty =
        DependencyProperty.Register("SpeakerContainer", typeof(SpeakerContainer), typeof(SpeakerSmall), new FrameworkPropertyMetadata(OnSpeakerChanged));

        public static void OnSpeakerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpeakerSmall sender = (SpeakerSmall)d;
            BindingOperations.SetBinding(sender, LoadingProperty, new Binding("IsLoading") { Source = sender.SpeakerContainer });
            BindingOperations.SetBinding(sender, ModifiedProperty, new Binding("Changed") {Source = sender.SpeakerContainer , Mode= BindingMode.OneWay});
        }

        public SpeakerContainer SpeakerContainer
        {
            get
            {
                return (SpeakerContainer)GetValue(SpeakerProperty);
            }
            set
            {
                SetValue(SpeakerProperty, value);
              
            }
        }

        public static readonly DependencyProperty LoadingProperty =
        DependencyProperty.Register("IsLoading", typeof(bool), typeof(SpeakerSmall), new FrameworkPropertyMetadata(OnLoadingChanged));

        public static void OnLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpeakerSmall sender = (SpeakerSmall)d;
        }

        public static readonly DependencyProperty MiniatureVisibleProperty =
        DependencyProperty.Register("MiniatureVisible", typeof(bool), typeof(SpeakerSmall));


        public static readonly DependencyProperty ModifiedProperty =
        DependencyProperty.Register("Modified", typeof(bool), typeof(SpeakerSmall), new FrameworkPropertyMetadata(OnModified));

        public static void OnModified(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpeakerSmall sender = (SpeakerSmall)d;
            sender.SpeakerModified?.Invoke();
        }

        public event Action SpeakerModified;

        public bool Changed
        { 
            get
            {
                return (bool)GetValue(ModifiedProperty);
            }
            set
            {
                SetValue(ModifiedProperty, value);
            }
        }

        public bool IsLoading
        {
            get
            {
                return (bool)GetValue(LoadingProperty);
            }
            set
            {
                SetValue(LoadingProperty, value);
            }
        }

        public bool MiniatureVisible
        {
            get
            {
                return (bool)GetValue(MiniatureVisibleProperty);
            }
            set
            {
                SetValue(MiniatureVisibleProperty, value);
            }
        }

        
        public SpeakerSmall()
        {
            InitializeComponent();
        }
    }



    public sealed class EmptyStringToCollapseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            return string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
