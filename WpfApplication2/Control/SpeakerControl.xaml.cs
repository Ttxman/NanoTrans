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
using System.Globalization;
using System.IO;
using NanoTrans.Properties;
using TranscriptionCore;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for SpeakerControl.xaml
    /// </summary>
    public partial class SpeakerControl : UserControl
    {
        public static readonly DependencyProperty SpeakerProperty =
        DependencyProperty.Register("SpeakerContainer", typeof(SpeakerContainer), typeof(SpeakerControl), new FrameworkPropertyMetadata(OnSpeakerChanged));

        public static void OnSpeakerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

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



        public SpeakerControl()
        {
            InitializeComponent();
        }
        public delegate void SpeakerDelegate(SpeakerContainer spk);
        public event SpeakerDelegate SaveSpeakerClick;
        public event SpeakerDelegate RevertSpeakerClick;





        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RevertSpeakerClick?.Invoke(this.SpeakerContainer);
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            SaveSpeakerClick?.Invoke(this.SpeakerContainer);
        }

        private void ButtonAddAttributeClick(object sender, RoutedEventArgs e)
        {
            SpeakerAttribute sa = new SpeakerAttribute("", Settings.Default.SpeakerAttributteCategories[0], "");
            SpeakerContainer.AttributesAdd(sa);
        }

        private void ButtonRemoveAttributeClick(object sender, RoutedEventArgs e)
        {
            if (AttributeList.SelectedItem is SpeakerAttributeContainer a)
            {
                SpeakerContainer.AttributesRemove(a.SpeakerAttribute);
            }
        }

        private void SpeakerAttributeControl_GotFocus(object sender, RoutedEventArgs e)
        {
            AttributeList.SelectedValue = (sender as SpeakerAttributeControl).Attribute;
        }

        private void SpeakerAttributeControl_ContentChanged(object sender, RoutedEventArgs e)
        {
            SpeakerContainer.Changed = true;
        }
    }

    [ValueConversion(typeof(Speaker.Sexes), typeof(string))]
    public class SexConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (((string)value) == Properties.Strings.SexConversionFemale)
                return Speaker.Sexes.Female;
            else if (((string)value) == Properties.Strings.SexConversionMale)
                return Speaker.Sexes.Male;
            else
                return Speaker.Sexes.X;
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (Speaker.Sexes)value switch
            {
                Speaker.Sexes.Female => Strings.SexConversionFemale,
                Speaker.Sexes.Male => Strings.SexConversionMale,
                _ => "--",
            };
        }
    }


    [ValueConversion(typeof(string), typeof(Image))]
    public class JPGB64Converter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            string s = value as string;

            if (string.IsNullOrEmpty(s))
                return null;


            BitmapImage bim = new BitmapImage();
            bim.StreamSource = new MemoryStream(System.Convert.FromBase64String(s));

            return new Image() { Source = bim };

        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class MyBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool s = (bool)value;

            if (s)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;

        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(object), typeof(bool))]
    public sealed class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
