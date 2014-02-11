using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinFonetickySlovnik.xaml
    /// </summary>
    public partial class WinFonetickySlovnik : Window
    {
        private string _Slovo = "";
        private string _SlovoNavrzenyPrepis = "";
        private int _indexDocasnePolozky = -1;
        private int _indexHlavnihoSlovniku = -1;
        private int _indexUzivatelskehoSlovniku = -1;
        private bool _novyPrepis = false;
        private bool _Editovat = false;
        private string _puvodniText = "";

        MyFoneticSlovnik _slovnikUzivatelsky = null;
        public WinFonetickySlovnik(string aSlovo, string aNavrzenyPrepis, int aIndexDocasnehoSlovniku, MyFoneticSlovnik aSlovnikUzivatelsky)
        {
            InitializeComponent();
            _Slovo = aSlovo;
            _SlovoNavrzenyPrepis = aNavrzenyPrepis;
            _slovnikUzivatelsky = aSlovnikUzivatelsky;
            tbSlovo.Text = aSlovo;
            lbFonetickeVarianty.Items.Clear();
            _indexDocasnePolozky = aIndexDocasnehoSlovniku;
            UpdateSeznamFonetickychVariant();
            

        }

        private bool UpdateSeznamFonetickychVariant()
        {
            try
            {
                lbFonetickeVarianty.Items.Clear();
                MyFoneticSlovnikPolozka fsp = null;
                bool pPridatOrig = false;
                if (_indexDocasnePolozky < 0)
                {
                    for (int i = 0; i < _slovnikUzivatelsky.PridanaSlovaDocasna.Count; i++)
                    {
                        if (_Slovo == _slovnikUzivatelsky.PridanaSlovaDocasna[i].Slovo)
                        {
                            fsp = _slovnikUzivatelsky.PridanaSlovaDocasna[i];
                            _indexDocasnePolozky = i;
                            pPridatOrig = true;
                            break;
                        }
                    }
                }

                if (_indexDocasnePolozky >= 0)
                {
                    if (pPridatOrig && fsp != null)
                    {
                        fsp.PridejFonetickouVariantu(_SlovoNavrzenyPrepis);
                    }
                    else
                    {
                        fsp = _slovnikUzivatelsky.PridanaSlovaDocasna[_indexDocasnePolozky];
                    }
                }
                else
                {
                    fsp = new MyFoneticSlovnikPolozka(_Slovo, "&" + _SlovoNavrzenyPrepis);
                }
                for (int j = 0; j < _slovnikUzivatelsky.SlovnikZakladni.Count; j++)
                {
                    MyFoneticSlovnikPolozka pPol = _slovnikUzivatelsky.SlovnikZakladni[j];
                    if (_Slovo == pPol.Slovo)
                    {
                        for (int i = 0; i < pPol.FonetickeVarianty.Count; i++)
                        {
                            if (pPol.FonetickeVarianty[i] != _SlovoNavrzenyPrepis)
                            {
                                fsp.PridejFonetickouVariantu("@" + pPol.FonetickeVarianty[i].Replace("&", ""));
                            }
                            else
                            {
                                fsp.FonetickeVarianty[0] = "@" + fsp.FonetickeVarianty[0].Replace("&", "");
                            }
                        }
                        _indexHlavnihoSlovniku = j;
                        break;
                    }
                }
                for (int j = 0; j < _slovnikUzivatelsky.PridanaSlova.Count; j++)
                {
                    MyFoneticSlovnikPolozka pPol = _slovnikUzivatelsky.PridanaSlova[j];
                    if (_Slovo == pPol.Slovo && pPol.FonetickeVarianty.Count > 0)
                    {
                        for (int i = 0; i < pPol.FonetickeVarianty.Count; i++)
                        {
                            for (int ii = 0; ii < fsp.FonetickeVarianty.Count; ii++)
                            {
                                if (fsp.FonetickeVarianty[ii].Replace("&", "") != pPol.FonetickeVarianty[i].Replace("&", ""))
                                {
                                    fsp.PridejFonetickouVariantu(pPol.FonetickeVarianty[i]);
                                }
                                else
                                {
                                    fsp.FonetickeVarianty[ii] = fsp.FonetickeVarianty[ii].Replace("&", "");
                                }
                            }

                        }
                        _indexUzivatelskehoSlovniku = j;
                        break;
                    }
                }

                if (_indexUzivatelskehoSlovniku < 0 && _indexHlavnihoSlovniku < 0)
                {
                    for (int i = 0; i < fsp.FonetickeVarianty.Count; i++)
                    {
                        fsp.FonetickeVarianty[i] = "&" + fsp.FonetickeVarianty[i].Replace("&", "");
                    }
                }

                foreach (string s in fsp.FonetickeVarianty)
                {
                    ListBoxItem pItem = new ListBoxItem();
                    Image im = new Image();

                    BitmapImage bi3 = new BitmapImage();
                    bi3.BeginInit();
                    bi3.UriSource = new Uri("icons/iSlovnik.png", UriKind.Relative);
                    bi3.EndInit();
                    BitmapImage bi4 = new BitmapImage();
                    bi4.BeginInit();
                    bi4.UriSource = new Uri("icons/iMluvci1.png", UriKind.Relative);
                    bi4.EndInit();


                    TextBlock tb = new TextBlock();
                    string pS = s;
                    if (pS.Contains("@"))
                    {
                        im.Source = null;
                        pS = pS.Replace("@", "");
                    }
                    else if (pS.Contains("&"))
                    {
                        im.Source = bi4;
                        pS = pS.Replace("&", "");
                    }
                    else
                    {
                        im.Source = bi3;
                    }
                    tb.Text = pS;

                    StackPanel sp = new StackPanel();
                    sp.Orientation = Orientation.Horizontal;
                    sp.Children.Add(im);
                    sp.Children.Add(tb);
                    pItem.Content = sp;

                    lbFonetickeVarianty.Items.Add(pItem);
                }
                if (fsp != null)
                {
                    for (int i = 0; i < fsp.FonetickeVarianty.Count; i++)
                    {
                        fsp.FonetickeVarianty[i] = fsp.FonetickeVarianty[i].Replace("&", "");
                    }
                }

                if (lbFonetickeVarianty.Items.Count > 0)
                {
                    if (_SlovoNavrzenyPrepis != null && _SlovoNavrzenyPrepis != "")
                    {
                        for (int i = 0; i < lbFonetickeVarianty.Items.Count; i++)
                        {
                            if (((TextBlock)(((StackPanel)(lbFonetickeVarianty.Items[i] as ListBoxItem).Content).Children[1])).Text == _SlovoNavrzenyPrepis)
                            {
                                lbFonetickeVarianty.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    if (lbFonetickeVarianty.SelectedIndex < 0)
                        lbFonetickeVarianty.SelectedIndex = 0;
                }

                if (lbFonetickeVarianty.Items.Count > 0)
                {
                    (lbFonetickeVarianty.Items[lbFonetickeVarianty.SelectedIndex] as ListBoxItem).Focus();
                    (lbFonetickeVarianty.Items[lbFonetickeVarianty.SelectedIndex] as ListBoxItem).IsSelected = true;
                    //lbFonetickeVarianty.Focus();
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Close();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                btOK_Click(null, new RoutedEventArgs());
            }
            else if (e.Key == Key.N && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                btNovyPrepis_Click(null, new RoutedEventArgs());
            }
            else if (e.Key == Key.F2)
            {
                e.Handled = true;
                btEditovat_Click(null, new RoutedEventArgs());
            }
            else if (e.Key == Key.F3)
            {
                e.Handled = true;
                btUlozitDoSlovniku_Click(null, new RoutedEventArgs());
            }
        }

        private void lbFonetickeVarianty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbFonetickeVarianty.SelectedIndex >= 0)
            {
                //tbFonetickyPrepis.Text = (lbFonetickeVarianty.SelectedItem as ListBoxItem).Content.ToString();
                tbFonetickyPrepis.Text = (((lbFonetickeVarianty.SelectedItem as ListBoxItem).Content as StackPanel).Children[1] as TextBlock).Text;

            }
            _novyPrepis = false;
        }

        private void btNovyPrepis_Click(object sender, RoutedEventArgs e)
        {
            _novyPrepis = true;
            tbFonetickyPrepis.Text = "";
            tbFonetickyPrepis.IsReadOnly = false;
            tbFonetickyPrepis.Focus();


        }

        private void btEditovat_Click(object sender, RoutedEventArgs e)
        {
            if (lbFonetickeVarianty.SelectedIndex >= 0)
            {
                _Editovat = true;
                tbFonetickyPrepis.IsReadOnly = false;
                tbFonetickyPrepis.Focus();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void btSmazat_Click(object sender, RoutedEventArgs e)
        {
            if (lbFonetickeVarianty.SelectedIndex < 0) return;
            if (_indexHlavnihoSlovniku >= 0)
            {
                if (_slovnikUzivatelsky.SlovnikZakladni[_indexHlavnihoSlovniku].jeFonetickaVarianta(tbFonetickyPrepis.Text))
                    return;
            }
            if (_indexUzivatelskehoSlovniku >= 0)
            {
                if (_slovnikUzivatelsky.PridanaSlova[_indexUzivatelskehoSlovniku].FonetickeVarianty.Remove(tbFonetickyPrepis.Text))
                {
                    _slovnikUzivatelsky.UlozitSlovnik(null);
                    
                    
                }
            }
            lbFonetickeVarianty.Items.RemoveAt(lbFonetickeVarianty.SelectedIndex);
            try
            {
                if (lbFonetickeVarianty.Items.Count > 0)
                {
                    (lbFonetickeVarianty.Items[0] as ListBoxItem).Focus();
                    (lbFonetickeVarianty.Items[0] as ListBoxItem).IsSelected = true;
                }
            }
            catch
            {
            }
        }

        private void lbFonetickeVarianty_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                btSmazat_Click(null, new RoutedEventArgs());
            }
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (_novyPrepis)
                {
                    _novyPrepis = false;
                }
                //if (_indexDocasnePolozky >= 0)
                    //_slovnikUzivatelsky.PridejPolozkuSlovniku(_slovnikUzivatelsky.PridanaSlovaDocasna[_indexDocasnePolozky]);

                if (_slovnikUzivatelsky.JeVZakladnimSlovniku(tbSlovo.Text, tbFonetickyPrepis.Text) == null && tbFonetickyPrepis.Text.Length > 0 && ((_indexDocasnePolozky<0 || _Editovat)&& _slovnikUzivatelsky.PridejPolozkuSlovniku(new MyFoneticSlovnikPolozka(tbSlovo.Text, tbFonetickyPrepis.Text)) == 0))
                {
                        _slovnikUzivatelsky.UlozitSlovnik(null);
                }
                else
                {
                    if (_Editovat)
                    {
                        MessageBox.Show("Polozka je ze zakladniho slovniku a nelze ji zmenit.");
                        return;
                    }
                }


                this.DialogResult = true;
                this.Close();
            }
            finally
            {
                if (_Editovat)
                {
                    _Editovat = false;

                }
            }
        }

        private void tbFonetickyPrepis_TextChanged(object sender, TextChangedEventArgs e)
        {
            MyKONST.OverZmenyTextBoxu(sender as TextBox, _puvodniText, ref e, MyEnumTypElementu.foneticky);
            _puvodniText = (sender as TextBox).Text;
        }

        private void btUlozitDoSlovniku_Click(object sender, RoutedEventArgs e)
        {
            if (_indexDocasnePolozky >= 0 || (_slovnikUzivatelsky.JeVZakladnimSlovniku(_Slovo, tbFonetickyPrepis.Text) == null && _slovnikUzivatelsky.JeVUzivatelskemSlovniku(_Slovo, tbFonetickyPrepis.Text) == null))
            {
                MyFoneticSlovnikPolozka pPol = null;
                if (_indexDocasnePolozky >= 0)
                {
                    pPol = _slovnikUzivatelsky.PridanaSlovaDocasna[_indexDocasnePolozky];
                    _slovnikUzivatelsky.PridanaSlovaDocasna.RemoveAt(_indexDocasnePolozky);
                    _indexDocasnePolozky = -1;
                }
                else
                {
                    pPol = new MyFoneticSlovnikPolozka(_Slovo, tbFonetickyPrepis.Text);
                }
                _slovnikUzivatelsky.PridejPolozkuSlovniku(pPol);
                _slovnikUzivatelsky.UlozitSlovnik(null);
                
                UpdateSeznamFonetickychVariant();
            }
        }

        

        
        
    }
}
