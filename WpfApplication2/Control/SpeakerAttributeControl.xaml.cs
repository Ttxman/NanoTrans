using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for SpeakerAttributeControl.xaml
    /// </summary>
    public partial class SpeakerAttributeControl : UserControl
    {


        public SpeakerAttributeContainer Attribute
        {
            get { return (SpeakerAttributeContainer)GetValue(AttributeProperty); }
            set { SetValue(AttributeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Attribute.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AttributeProperty =
            DependencyProperty.Register("Attribute", typeof(SpeakerAttributeContainer), typeof(SpeakerAttributeControl));

        
        public SpeakerAttributeControl()
        {
            InitializeComponent();
        }
    }

    public class SpeakerAttributeContainer:INotifyPropertyChanged
    {
        public string Name 
        {
            get
            {
                return _a.Name;
            }

            set 
            {
                _a.Name = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }
        public string Value 
        {
            get
            {
                return _a.Value;
            }

            set
            {
                _a.Value = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Value"));
            }
        
        }


        public DateTime Date 
        { 
            get; 
            set; 
        }

        SpeakerAttribute _a;
        public SpeakerAttribute SpeakerAttribute { get{return _a;} set{_a = value;} }
        public SpeakerAttributeContainer(SpeakerAttribute a)
        {
            _a = a;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
