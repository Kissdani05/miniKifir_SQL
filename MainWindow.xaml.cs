using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace WpfApp2
{
    
    public partial class MainWindow : Window
    {
        ObservableCollection<Felvetelizok> lista = new ObservableCollection<Felvetelizok>();

        private MySqlConnection connection;
        private readonly string sqlKapcsolat = "datasource=127.0.0.1;port=3306;username=root;password=;database=miniKozfelvir;";
        
        public MainWindow()
        {

            InitializeComponent();
            using (StreamReader sr = new StreamReader("felvetelizok.csv"))
            {
                while (!sr.EndOfStream)
                {
                    string sor = sr.ReadLine();
                    Felvetelizok sor1 = new Felvetelizok(sor);
                    lista.Add(sor1);
                }
            }
            myDataGrid.ItemsSource = lista;







        }


        private void NewStudentButton_Click(object sender, RoutedEventArgs e)
        {
            Felvetelizok ujdiak = new Felvetelizok();

            UjDiak hozzaadott = new UjDiak(ujdiak, true);
            hozzaadott.ShowDialog();
            lista.Add(ujdiak);

        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (myDataGrid.SelectedItems.Count > 0)
            {
                var selectedItems = myDataGrid.SelectedItems.Cast<Felvetelizok>().ToList();
                foreach (var selectedItem in selectedItems)
                {
                    lista.Remove(selectedItem);
                }
            }
        }

        private void ExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            var dataToExport = myDataGrid.ItemsSource as ObservableCollection<Felvetelizok>;
            if (dataToExport == null || !dataToExport.Any())
            {
                MessageBox.Show("No data to export.");
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv|JSON file (*.json)|*.json",
                DefaultExt = "csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();

                if (File.Exists(filePath))
                {
                    MessageBoxResult overwriteConfirmation = MessageBox.Show("The file already exists. Do you want to overwrite it?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (overwriteConfirmation != MessageBoxResult.Yes)
                    {
                        return; // Do not overwrite
                    }
                }

                try
                {
                    switch (fileExtension)
                    {
                        case ".csv":
                            ExportToCsv(filePath, dataToExport);
                            break;

                        case ".json":
                            ExportToJson(filePath, dataToExport);
                            break;

                        default:
                            MessageBox.Show("Selected file type is not supported for export.");
                            return;
                    }

                    MessageBox.Show("Export successful.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during export: {ex.Message}");
                }
            }
        }

        private void ExportToCsv(string filePath, ObservableCollection<Felvetelizok> data)
        {
            StringBuilder csvContent = new StringBuilder();
            var headers = myDataGrid.Columns.Select(column => column.Header.ToString());
            csvContent.AppendLine(string.Join(",", headers));

            foreach (Felvetelizok item in data)
            {
                var values = item.GetType().GetProperties().Select(pi => (pi.GetValue(item, null) ?? "").ToString());
                var line = string.Join(",", values.Select(value => $"\"{value.Replace("\"", "\"\"")}\"")); // Handle commas and quotes in values
                csvContent.AppendLine(line);
            }

            File.WriteAllText(filePath, csvContent.ToString());
        }

        private void ExportToJson(string filePath, ObservableCollection<Felvetelizok> data)
        {
            string jsonContent = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, jsonContent);
        }
        private void SaveToDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = new MySqlConnection(sqlKapcsolat))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "TRUNCATE TABLE felvetelizok"; 
                    command.ExecuteNonQuery();

                    foreach (var felvetelizo in lista)
                    {
                        command.CommandText = $"INSERT INTO felvetelizok (OszlopokNevei) VALUES ('{felvetelizo.Om_azonosito}', '{felvetelizo.Név}', '{felvetelizo.Ertesitesi_cim}', '{felvetelizo.Szuletesi_datum}', '{felvetelizo.Elerhetoseg_email}', '{felvetelizo.Matekpont}', '{felvetelizo.Irodalompont}');";
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Adatok sikeresen mentve az adatbázisba!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt az adatok mentése közben: {ex.Message}");
            }
        }
        private void ImportFromDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lista.Clear();
                using (var connection = new MySqlConnection(sqlKapcsolat))
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT * FROM felvetelizok", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var felvetelizo = new Felvetelizok
                            {
                                Om_azonosito = Convert.ToInt32(reader["Om_azonosito"]),
                                Név = reader["Név"].ToString(),
                                Ertesitesi_cim = reader["Erettségi_cím"].ToString(),
                                Szuletesi_datum = DateOnly.Parse(reader["Szuletesi_datum"].ToString()),
                                Elerhetoseg_email = reader["Elérhetőség_email"].ToString(),
                                Matekpont = Convert.ToInt32(reader["Matekpont"]),
                                Irodalompont = Convert.ToInt32(reader["Irodalompont"]),


                            };
                            lista.Add(felvetelizo);
                        }
                    }
                }
                MessageBox.Show("Adatok sikeresen importálva az adatbázisból!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt az adatok importálása közben: {ex.Message}");
            }
        }


    }
}   