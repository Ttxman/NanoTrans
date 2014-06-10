using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace NanoTrans
{
    public class WaitCursor : IDisposable
    {
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static int _InstanceCounter = 0;

        public static int InstanceCounter
        {
            get
            {
                return WaitCursor._InstanceCounter;
            }
            set
            {
                WaitCursor._InstanceCounter = value;
                if (StaticPropertyChanged != null)
                    StaticPropertyChanged(null, new PropertyChangedEventArgs("Waiting"));
            }
        }

        private Cursor _previousCursor = null;

        public static bool Waiting
        {
            get
            {
                return InstanceCounter > 0;
            }
        }

        public WaitCursor()
        {
            try
            {
                _previousCursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = Cursors.Wait;
            }
            catch { }
            InstanceCounter++;
        }

        #region IDisposable Members

        public void Dispose()
        {

            Mouse.OverrideCursor = _previousCursor;
            InstanceCounter--;
        }

        #endregion
    }
}
