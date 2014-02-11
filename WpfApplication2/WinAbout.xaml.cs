using System;
//using System.Collections.Generic;
//using System.Linq;
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
    /// Interaction logic for WinOProgramu.xaml
    /// </summary>
    public partial class WinOProgramu : Window
    {
        public WinOProgramu(string aNazevProgramu)
        {
            InitializeComponent();
            this.label1.Content = aNazevProgramu;
            textBox1.Text = "Historie verzí:\n\n2.0.2b\n- Úprava struktury XML souborů.\n- Rozšíření informací o mluvčích (příjmení, pohlaví).\n- Možnost vyhledávání mluvčích.\n- Při přehrávání segmentu již dochází k přehrání pouze požadované části.\n- Automatické načtení audia při otevření video souboru.\n- Opravy při změně délek segmentů a nastavení kurzoru po smazání segmentu\n- Vylepšena časová osa zvukového signálu.";
            textBox1.Text += "\n\n2.0.3b\n- Oprava posunu segmentů po jejich rozdělení.\n- Změna výchozí přípony souboru s titulky na *.xml.\n- Zobrazení komentáře u mluvčích po přejetí kurzorem přes tlačítko mluvčích.";
            textBox1.Text += "\n\n2.0.4b\n- Změna rozmístění ovládání pro přehrávání zvukového signálu.\n- Vylepšena podpora převodu multimediálních formátů na audio signál (bez nutnosti instalovaných kodeků)\n- Přidána podpora fonetického přepisu pro úroveň odstavec - zatím pokusně.\n- Možnost zobrazení časových indexů jednotlivých elementů přímo v textovém přepisu (Nastavení ve vzhledu programu).\n- Zlepšeno zobrazování audio signálu.\n- Vylepšena časová osa audio signálu.";
            textBox1.Text += "\n\n2.0.5b\n- Podpora tvorby fonetického přepisu s využitím HTK.\n- Změna rozmístění plovoucích panelů programu (Video, Přepis, Fonetický přepis).\n- Pamatování pozice a rozměrů okna po ukončení a opětovném spuštění aplikace.";
            textBox1.Text += "\n\n2.0.6b\n- Změna struktury XML (zpětně kompatibilní) - Přidány atributy trakskripce: dateTime (datum a čas vzniku pořadu, jinak čas vzniku transkripce); source (zdroj dat - kanál rozhlasu, název televize, mikrofon, atd...); videoFileName (jméno video souboru, může být shodný se zdrojem audio souboru atributu audioFileName) \n- Při přehrávání je automaticky nastavován kurzor v přepisu. Při změně kurzoru v přepisu je nastaven kurzor v signálu.\n- V signálu jsou zobrazeni mluvčí a jejich překrývání. Lze měnit pomocí Ctrl a Shift.";
            textBox1.Text += "\n\n2.0.7b\n- Označování a výběru audio signálu bez klávesy CTRL.\n- Možnost dávkově vytvářet fonetické přepisy externích souborů.";
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
