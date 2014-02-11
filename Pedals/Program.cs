using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using USBHIDDRIVER;
using System.Threading;
using System.Configuration;


namespace Pedals
{
    class Program
    {
        static string Left;
        static string Middle;
        static string Right;
        static string VKeyLeft;
        static string VKeyMiddle;
        static string VKeyRight;
        static bool VirtualKeys = true;
        static void Main(string[] args)
        {
            if (args.Length > 0)
                VirtualKeys = false;
            string VID = ConfigurationManager.AppSettings["VID"] ?? "vid_05f3";
            string PID = ConfigurationManager.AppSettings["PID"] ?? "pid_00ff";

            Left = ConfigurationManager.AppSettings["Left"] ?? "L";
            Middle = ConfigurationManager.AppSettings["Middle"] ?? "M";
            Right = ConfigurationManager.AppSettings["Right"] ?? "R";

            VKeyLeft = ConfigurationManager.AppSettings["Left"] ?? "{LEFT}";
            VKeyMiddle = ConfigurationManager.AppSettings["Middle"] ?? " ";
            VKeyRight = ConfigurationManager.AppSettings["Right"] ?? "{RIGHT}";
            usbI = new USBInterface(VID, PID);


            savehandle = new EventHandler(HIDhandler);
            bool conn = usbI.Connect();
            if (conn)
            {
                usbI.enableUsbBufferEvent(savehandle);
                Thread.Sleep(5);
                usbI.startRead();

            }
            else
                Console.WriteLine("Cannot connect to device");

            Console.Read();
            if (conn)
            {
                usbI.stopRead();
                try
                {
                    usbI.Disconnect();
                }
                catch { }
            }
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        static USBHIDDRIVER.USBInterface usbI;
        static EventHandler savehandle;

        static FCPedal FCstatus = FCPedal.None;
        [Flags]
        public enum FCPedal : byte
        {
            None = 0x0,
            Left = 0x1,
            Middle = 0x2,
            Right = 0x4,
            Invalid = 0xFF
        }

        static bool Bleft = false;
        static bool Bright = false;
        static bool Bmiddle = false;
        static void HIDhandler(object sender, System.EventArgs e)
        {
            USBHIDDRIVER.List.ListWithEvent ev = (USBHIDDRIVER.List.ListWithEvent)sender;
            foreach (object o in ev)
            {
                if (o is byte[])
                {
                    byte[] data = (byte[])o;
                    byte stat = data[1];
                    if (FCstatus != FCPedal.Invalid)
                    {
                        if ((((byte)FCPedal.Left) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Left & FCstatus) == 0) //down event
                            {
                                Bleft = true;
                                Console.WriteLine("+" + Left);
                                if (VirtualKeys)
                                {
                                    System.Windows.Forms.SendKeys.SendWait(VKeyLeft);
                                }
                            }

                        }
                        else if (Bleft)
                        {
                            Bleft = false;
                            Console.WriteLine("-" + Left);
                        }

                        if ((((byte)FCPedal.Middle) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Middle & FCstatus) == 0) //down event
                            {
                                Bmiddle = true;
                                Console.WriteLine("+" + Middle);
                                if (VirtualKeys)
                                {
                                    System.Windows.Forms.SendKeys.SendWait(VKeyMiddle);
                                }
                            }


                        }
                        else if (Bmiddle)
                        {
                            Bmiddle = false;
                            Console.WriteLine("-" + Middle);
                        }

                        if ((((byte)FCPedal.Right) & stat) != 0)
                        {
                            if ((byte)(FCPedal.Right & FCstatus) == 0) //down event
                            {
                                Bright = true;
                                Console.WriteLine("+" + Right);
                                if (VirtualKeys)
                                {
                                    System.Windows.Forms.SendKeys.SendWait(VKeyRight);
                                }
                            }

                        }
                        else if (Bright)
                        {
                            Bright = false;
                            Console.WriteLine("-" + Right);
                        }
                    }

                    FCstatus = (FCPedal)stat;
                }
            }
            ev.Clear();
        }

    }
}
