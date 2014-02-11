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

        public string Text
        {
            get { return myTextBox1.Text; }
            set { myTextBox1.Text = value; }
        }
        public Element()
        {
            InitializeComponent();
        }
    }
}
