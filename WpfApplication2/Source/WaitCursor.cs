﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs("Waiting"));
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
            Application.Current.Dispatcher.Invoke(() =>
                {
                    _previousCursor = Mouse.OverrideCursor;
                    Mouse.OverrideCursor = Cursors.Wait;
                    InstanceCounter++;
                });
        }

        #region IDisposable Members

        public void Dispose()
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = _previousCursor;
                    InstanceCounter--;
                });
        }

        #endregion
    }
}
