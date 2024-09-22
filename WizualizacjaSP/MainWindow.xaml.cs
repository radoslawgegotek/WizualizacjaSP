using System.Text;
using System.Windows;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WizualizacjaSP
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private float[] times = new float[3]; // Tablica do przechowywania czasów z Arduino
        private float totalTime; // Czas przejazdu od 1 do 4 fotokomórki

        public MainWindow()
        {
            InitializeComponent();
            // Wypełnij listę dostępnych portów szeregowych
            PortComboBox.ItemsSource = SerialPort.GetPortNames();
        }

        private void Polacz_Click(object sender, RoutedEventArgs e)
        {
            string selectedPort = PortComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedPort))
            {
                MessageBox.Show("Wybierz port szeregowy.");
                return;
            }

            // Ustawienie portu szeregowego
            _serialPort = new SerialPort(selectedPort, 9600);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
            MessageBox.Show("Połączono z " + selectedPort);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Odbieranie danych z Arduino
            string data = _serialPort.ReadLine();
            Application.Current.Dispatcher.Invoke(() =>
            {
                ParseArduinoData(data);
            });
        }

        private void ParseArduinoData(string data)
        {
            try
            {
                // Sprawdzamy, czy dane zawierają informacje o czasie przejazdu między fotokomórkami
                if (data.Contains("Czas miedzy fotokomorka"))
                {
                    // Dane są w formacie:
                    // Czas miedzy fotokomorka 1 a fotokomorka 2: <czas> s
                    // Pobieramy czas z linii
                    var splitData = data.Split(' ');
                    int index = int.Parse(splitData[3]) - 1; // Fotokomórki są numerowane od 1
                    float czas = float.Parse(splitData[7]);

                    times[index] = czas;
                }
                else if (data.Contains("Czas przejazdu miedzy 1 a 4 fotokomorka"))
                {
                    // Pobieramy czas przejazdu od fotokomórki 1 do 4
                    var splitData = data.Split(' ');
                    totalTime = float.Parse(splitData[5]);

                    // Gdy wszystkie dane są odebrane, automatycznie obliczamy prędkości
                    ObliczPredkosc();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd parsowania danych: " + ex.Message);
            }
        }

        private void ObliczPredkosc()
        {
            try
            {
                // Pobranie odległości między fotokomórkami (w milimetrach)
                float distance1To2 = float.Parse(Distance1To2.Text) / 1000.0f; // Konwersja na metry
                float distance2To3 = float.Parse(Distance2To3.Text) / 1000.0f; // Konwersja na metry
                float distance3To4 = float.Parse(Distance3To4.Text) / 1000.0f; // Konwersja na metry

                // Obliczenie prędkości chwilowej
                float speed1To2 = distance1To2 / times[0];
                float speed2To3 = distance2To3 / times[1];
                float speed3To4 = distance3To4 / times[2];

                // Obliczenie prędkości średniej
                float totalDistance = distance1To2 + distance2To3 + distance3To4;
                float averageSpeed = totalDistance / totalTime;

                // Wyświetlenie danych w tabeli
                ResultsDataGrid.ItemsSource = new[]
                {
                    new { Odcinek = "1-2", Czas = times[0], Predkosc = speed1To2 },
                    new { Odcinek = "2-3", Czas = times[1], Predkosc = speed2To3 },
                    new { Odcinek = "3-4", Czas = times[2], Predkosc = speed3To4 },
                    new { Odcinek = "Średnia", Czas = totalTime, Predkosc = averageSpeed }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd obliczeń: " + ex.Message);
            }
        }
    }
}