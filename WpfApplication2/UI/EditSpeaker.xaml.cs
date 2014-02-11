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
using System.Windows.Shapes;
using System.ComponentModel;
using NanoTrans.Core;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for EditSpeaker.xaml
    /// </summary>
    public partial class EditSpeakerWindow : Window
    {
        public Speaker Speaker
        {
            get { return bSpeaker; }
        }
        Speaker bSpeaker;
        Transcription myDataSource;
        string bStringBase64FotoExterni;
        string bStringBase64FotoInterni;
        public EditSpeakerWindow(Transcription subtitles, Speaker speaker)
        {
            bSpeaker = speaker;
            myDataSource = subtitles;
            InitializeComponent();
            cbLanguage.ItemsSource = Speaker.Langs;
            cbLanguage.SelectedIndex = speaker.DefaultLang;
            tbJmeno.Text = bSpeaker.FirstName;
            tbPrijmeni.Text = bSpeaker.Surname;
            imFotka.Source = MyKONST.PrevedBase64StringNaJPG(bSpeaker.ImgBase64);
            cbPohlavi.SelectedIndex = (int)bSpeaker.Sex;
            tbRemark.Text = speaker.Comment;

        }

        private void btPridejMluvciho_Click(object sender, RoutedEventArgs e)
        {   
            this.DialogResult = true;

            Speaker.Sexes pPohlavi = (Speaker.Sexes)cbPohlavi.SelectedIndex;
            if (cbPohlavi.SelectedIndex <= 0) pPohlavi = Speaker.Sexes.X;

            bSpeaker.FirstName = tbJmeno.Text;
            bSpeaker.Surname = tbPrijmeni.Text;
            bSpeaker.Sex =  pPohlavi;
            bSpeaker.ImgBase64 = bStringBase64FotoInterni;
            bSpeaker.Comment = tbRemark.Text;

            bSpeaker.DefaultLang = cbLanguage.SelectedIndex;

            Close();
        }

        private void btNacistObrazek_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Title = Properties.Strings.FileDialogLoadImageTitle;
            fileDialog.Filter = Properties.Strings.FileDialogLoadImageFilter;
            try
            {
                fileDialog.FilterIndex = 1;
                fileDialog.RestoreDirectory = true;
                if (fileDialog.ShowDialog() == true)
                {
                    BitmapFrame pFrame = BitmapFrame.Create(new Uri(fileDialog.FileName));

                    this.bStringBase64FotoExterni = MyKONST.JpgToBase64(pFrame);
                    this.bStringBase64FotoInterni = this.bStringBase64FotoExterni;

                    imFotka.Source = pFrame;

                }
            }
            catch
            {

            }

        }

        private void btSmazatObrazek_Click(object sender, RoutedEventArgs e)
        {
            if (bSpeaker != null)
            {
                bSpeaker.ImgBase64 = null;
                this.bStringBase64FotoInterni = null;
                imFotka.Source = MyKONST.PrevedBase64StringNaJPG(this.bStringBase64FotoExterni);
            }
        }

    }
}
