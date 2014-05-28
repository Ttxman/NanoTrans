using NanoTrans.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        Binding _oldb;
        public SpeakerAttributeContainer Attribute
        {
            get { return (SpeakerAttributeContainer)GetValue(AttributeProperty); }
            set
            {
                if (_oldb != null)
                {
                    BindingOperations.ClearBinding(this, IsChangedProperty);
                }

                _oldb = new Binding("Changed")
                {
                    Source = value,
                    Mode = BindingMode.OneWay,
                };
                BindingOperations.SetBinding(this, IsChangedProperty, _oldb);

                SetValue(AttributeProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Attribute.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AttributeProperty =
            DependencyProperty.Register("Attribute", typeof(SpeakerAttributeContainer), typeof(SpeakerAttributeControl));

        public bool IsChanged
        {
            get { return (bool)GetValue(IsChangedProperty); }
            set
            {
                SetValue(IsChangedProperty, value);
                RaiseEvent(new RoutedEventArgs(ContentChangedEvent, this));
            }
        }

        // Using a DependencyProperty as the backing store for Attribute.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsChangedProperty =
            DependencyProperty.Register("IsChanged", typeof(bool), typeof(SpeakerAttributeControl));


        // This event uses the bubbling routing strategy
        public static readonly RoutedEvent ContentChangedEvent = EventManager.RegisterRoutedEvent(
               "ContentChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SpeakerAttributeControl));

        public event RoutedEventHandler ContentChanged
        {
            add { AddHandler(ContentChangedEvent, value); }
            remove { RemoveHandler(ContentChangedEvent, value); }
        }



        public SpeakerAttributeControl()
        {
            InitializeComponent();
        }

    }

    public class SpeakerAttributeContainer : INotifyPropertyChanged
    {

        bool _changed;

        public bool Changed
        {
            get { return _changed; }
            set
            {
                _changed = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName]string caller = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        public string Name
        {
            get
            {
                return _a.Name;
            }

            set
            {
                _a.Name = value;
                OnPropertyChanged();
                Changed = true;
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
                OnPropertyChanged();
                Changed = true;
            }

        }


        public DateTime Date
        {
            get;
            set;
        }

        SpeakerAttribute _a;
        public SpeakerAttribute SpeakerAttribute { get { return _a; } set { _a = value; } }
        public SpeakerAttributeContainer(SpeakerAttribute a)
        {
            _a = a;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
