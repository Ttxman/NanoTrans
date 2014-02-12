/*
 * Created by SharpDevelop.
 * User: Ttxman
 * 
 */
using System;
using USBHIDDRIVER;
using System.Threading;
namespace programHID
{
	class Program
	{
		static USBHIDDRIVER.USBInterface usbI = new USBInterface("vid_05f3", "pid_00ff");
		//static USBHIDDRIVER.USBInterface usbI = new USBInterface("vid_3353", "pid_3713");
		public static void Main(string[] args)
		{
			bool conn = usbI.Connect();
			usbI.enableUsbBufferEvent(new EventHandler(handler));
			            Thread.Sleep(5);
            usbI.startRead();
			//Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
		
		public static void handler(object sender, System.EventArgs e)
		{
			USBHIDDRIVER.List.ListWithEvent ev = (USBHIDDRIVER.List.ListWithEvent)sender;
			foreach(object o in ev)
			{
				if(o is byte[])
				{
					byte[] data = (byte[])o;
					string s = "";
					for(int i=0;i<data.Length;i++)
					{
						s += data[i];
						if(i+1<data.Length)
							s+=", ";
					}
					Console.WriteLine("{"+s+"}");
				}
			}
			ev.Clear();
		}
	}
}