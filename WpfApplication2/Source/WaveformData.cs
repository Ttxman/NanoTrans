using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NanoTrans
{

    public class WaveformData
    {
        public MyBuffer16 audioBuffer = null;
               
        public long LengthMS { get; set; }               

        private long _beginMS;
        public long BeginMS
        {
            get { return _beginMS; }
            set
            {
                if (value < 0) _beginMS = 0;
                else
                {
                    _beginMS = value;
                }
            }
        }

        private long _endMS;
        public long EndMS 
        { 
            get
            {
                return _endMS;
            } 
            set
            {
                _endMS = value;
            } 
        }



        public long DeltaMS { get; set; }
        

        public long ShortJumpMS { get; set; }      

        public float YScalePercentage { get; set; }

        public bool ScaleAutomaticaly { get; set; }


        private long _CaretPositionMS;
        public long CaretPositionMS 
        { 
            get
            {
                return _CaretPositionMS; 
            } 
            set
            {
                _CaretPositionMS = value; 
            } 
        } 
        private TimeSpan _SelectionStart;
        
        public TimeSpan SelectionStart
        {
            get { return _SelectionStart; }
            set 
            { 
                _SelectionStart = value;
                }
            }

        public TimeSpan SelectionEnd { get; set; }  
        
        public bool MouseLeftDown { get; set; } 


        public WaveformData(long initialBufferLengthMS)
        {
            this.audioBuffer = new MyBuffer16(initialBufferLengthMS);
            this.audioBuffer.AddDataToBuffer(new short[480000], 30000);
            
            //constructor
            CaretPositionMS = 0;
            SelectionStart = TimeSpan.Zero ;
            SelectionEnd = TimeSpan.Zero;
            MouseLeftDown = false;


            LengthMS = 30000;

            BeginMS = 0;
            EndMS = 30000;
            
            DeltaMS = 1000;

            ShortJumpMS = 1000;


            YScalePercentage = 100;
            ScaleAutomaticaly = true;
        }

        public void SetWaveLength(long mSekundy)
        {
            LengthMS = mSekundy;
            DeltaMS = LengthMS / 60;
        }
    }
}
