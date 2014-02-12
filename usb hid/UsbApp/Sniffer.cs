using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UsbLibrary;

namespace UsbApp
{
    public partial class Sniffer : Form
    {
        public Sniffer()
        {
            InitializeComponent();
        }

        private void usb_OnDeviceArrived(object sender, EventArgs e)
        {
            this.lb_message.Items.Add("Found a Device");
        }

        private void usb_OnDeviceRemoved(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(usb_OnDeviceRemoved), new object[] { sender, e });
            }
            else
            {
                this.lb_message.Items.Add("Device was removed");
            }
        }

        private void usb_OnSpecifiedDeviceArrived(object sender, EventArgs e)
        {
            this.lb_message.Items.Add("My device was found");

            //setting string form for sending data
            string text = "";
            for (int i = 0; i < this.usb.SpecifiedDevice.OutputReportLength - 1; i++)
            {
                text += "000 ";
            }
            this.tb_send.Text = text;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            usb.RegisterHandle(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            usb.ParseMessages(ref m);
            base.WndProc(ref m);	// pass message on to base form
        }

        private void btn_ok_Click(object sender, EventArgs e)
        {
            try
            {
                this.usb.ProductId = Int32.Parse(this.tb_product.Text, System.Globalization.NumberStyles.HexNumber);
                this.usb.VendorId = Int32.Parse(this.tb_vendor.Text, System.Globalization.NumberStyles.HexNumber);
                this.usb.CheckDevicePresent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            try
            {
                string text = this.tb_send.Text + " ";
                text.Trim();
                string[] arrText = text.Split(' ');
                byte[] data = new byte[arrText.Length];
                for (int i = 0; i < arrText.Length; i++)
                {
                    if (arrText[i] != "")
                    {
                        int value = Int32.Parse(arrText[i], System.Globalization.NumberStyles.Number);
                        data[i] = (byte)Convert.ToByte(value);
                    }
                }

                if (this.usb.SpecifiedDevice != null)
                {
                    this.usb.SpecifiedDevice.SendData(data);
                }
                else
                {
                    MessageBox.Show("Sorry but your device is not present. Plug it in!! ");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void usb_OnSpecifiedDeviceRemoved(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(usb_OnSpecifiedDeviceRemoved), new object[] { sender, e });
            }
            else
            {
                this.lb_message.Items.Add("My device was removed");
            }
        }

        private void usb_OnDataRecieved(object sender, DataRecievedEventArgs args)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new DataRecievedEventHandler(usb_OnDataRecieved), new object[] { sender, args });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {

                string rec_data = "Data: ";
                foreach (byte myData in args.data)
                {
                    if (myData.ToString().Length == 1)
                    {
                        rec_data += "00";
                    }

                    if (myData.ToString().Length == 2)
                    {
                        rec_data += "0";
                    }

                    rec_data += myData.ToString() + " ";
                }

                this.lb_read.Items.Insert(0, rec_data);
            }
        }

        private void usb_OnDataSend(object sender, EventArgs e)
        {
            this.lb_message.Items.Add("Some data was send");
        }   
    }
}