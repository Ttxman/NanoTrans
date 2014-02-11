using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace NanoTrans
{
    class ButtonClickCommand:ICommand
    {
        private Button b;
        public ButtonClickCommand(Button but)
        {
            b = but;
            
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        #endregion
    }
}
