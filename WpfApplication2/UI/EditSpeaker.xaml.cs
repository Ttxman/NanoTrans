﻿using System;
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

            tbJmeno.Text = bSpeaker.FirstName;
            tbPrijmeni.Text = bSpeaker.Surname;
            imFotka.Source = MyKONST.PrevedBase64StringNaJPG(bSpeaker.FotoJPGBase64);
            cbPohlavi.SelectedIndex = (int)bSpeaker.Sex;

        }

        private void btPridejMluvciho_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            string pMluvci = null;
            string pJazykovyModel = null;
            string pPrepisovaciPravidla = null;
            if (cbMluvci.SelectedIndex > 0) pMluvci = cbMluvci.SelectedItem.ToString();
            if (cbJazykovyModel.SelectedIndex > 0) pJazykovyModel = cbJazykovyModel.SelectedItem.ToString();
            if (cbPrepisovaciPravidla.SelectedIndex > 0) pPrepisovaciPravidla = cbPrepisovaciPravidla.SelectedItem.ToString();

            Speaker.Sexes pPohlavi = (Speaker.Sexes)cbPohlavi.SelectedIndex;
            if (cbPohlavi.SelectedIndex <= 0) pPohlavi = Speaker.Sexes.X;

            bSpeaker = new Speaker(tbJmeno.Text, tbPrijmeni.Text, pPohlavi, pMluvci, pJazykovyModel, pPrepisovaciPravidla, this.bStringBase64FotoInterni, tbVek.Text);

            Close();
        }

        private void btNacistObrazek_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Title = "Otevřít soubor s obrázkem mluvčího...";
            fileDialog.Filter = "Soubory obrázků|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Všechny soubory (*.*)|*.*";
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
                bSpeaker.FotoJPGBase64 = null;
                this.bStringBase64FotoInterni = null;
                imFotka.Source = MyKONST.PrevedBase64StringNaJPG(this.bStringBase64FotoExterni);
            }
        }

    }
}
