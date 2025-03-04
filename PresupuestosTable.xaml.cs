using System.Windows;

using Bc3_WPF.backend.Services;
using Bc3_WPF.backend.Modelos;
using System.Text.Json;
using Bc3_WPF.Backend.Auxiliar;
using System.IO;

namespace Bc3_WPF
{
    /// <summary>
    /// Lógica de interacción para Window1.xaml
    /// </summary>
    public partial class PresupuestosTable : Window
    {
        private Presupuesto? presupuesto;
        private List<KeyValuePair<string, List<Presupuesto>>> historial = new List<KeyValuePair<string, List<Presupuesto>>>();
        private List<Presupuesto> currentData = new List<Presupuesto>();
        private int pageNumber = 1;
        private int RowsPerPage = 5;
        private List<Presupuesto> showing = new List<Presupuesto>();
        private decimal pages = 0;
 
        public PresupuestosTable()
        {
            InitializeComponent();
        }

        private void LoadBC3(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a .bc3 or .json File",
                Filter = "BC3 Files (*.bc3)|*.bc3|JSON Files (*.json)|*.json|All Supported Files (*.bc3;*.json)|*.bc3;*.json"
            };

            if (ofd.ShowDialog() == true )
            {
                string filePath = ofd.FileName;
                if (filePath.EndsWith(".bc3"))
                {
                    presupuesto = presupuestoService.loadFromBC3(filePath);
                }
                else
                {
                    presupuesto = presupuestoService.loadFromJson(filePath);
                }

                if (presupuesto.hijos != null)
                {
                    currentData = presupuesto.hijos;
                }

                makePagination();

                TitleTable.Text = presupuesto.name;

                TitleTable.Visibility = Visibility.Visible;
                SelectDB.Visibility = Visibility.Visible;
                Tabla.Visibility = Visibility.Visible;
                DB.Visibility = Visibility.Visible;
                SelectDB.Visibility = Visibility.Visible;
                FileButton.Visibility = Visibility.Hidden;

                Tabla.ItemsSource = showing;
            }
        }

        private void handleHijos(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            Presupuesto Item = (Presupuesto)button.DataContext;

            if (Item.hijos != null)
            {
                historial.Add(new KeyValuePair<String, List<Presupuesto>>(Item.Id, currentData));
                currentData = Item.hijos;
                makePagination();
                Tabla.ItemsSource = showing;

            }

            if (historial.Count > 0)
            {
                BackButton.Visibility = Visibility.Visible;
            }
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            if (historial.Count > 0)
            {
                currentData = historial[historial.Count - 1].Value;
                historial.Remove(historial[historial.Count - 1]);
                makePagination();
                Tabla.ItemsSource = showing;
            }

            if (historial.Count == 0)
            {
                BackButton.Visibility= Visibility.Hidden;
            }
        }

        private void PreviousPage(object sender, RoutedEventArgs e)
        {
            if (pageNumber > 1)
            {
                pageNumber = pageNumber - 1;
                int lowerbound = (pageNumber - 1) * RowsPerPage;

                showing = currentData.Slice(lowerbound, RowsPerPage);
                Tabla.ItemsSource = showing;
                PageNumber.Text = $"Page {pageNumber} / {pages}";
            }
        }

        private void NextPage(object sender, RoutedEventArgs e)
        {
            if (pageNumber != pages)
            {
                int lowerbound = pageNumber * RowsPerPage;
                pageNumber = pageNumber + 1;
                int upperbound = pageNumber * RowsPerPage;

                if (currentData.Count < upperbound)
                {
                    int sobrante = currentData.Count - lowerbound;
                    showing = currentData.Slice(lowerbound, sobrante);
                }
                else
                {
                    showing = currentData.Slice(lowerbound, RowsPerPage);
                }

                Tabla.ItemsSource = showing;
                PageNumber.Text = $"Page {pageNumber} / {pages}";
            }
        }

        private void makePagination()
        {
            decimal p = (decimal)currentData.Count / (decimal)RowsPerPage;
            pages = Math.Ceiling(p);
            pageNumber = 1;

            if (pages == 1)
            {
                showing = currentData;
                Paginator.Visibility = Visibility.Hidden;
            }
            else
            {
                showing = currentData.Slice(0, RowsPerPage);
                Paginator.Visibility = Visibility.Visible;
                PageNumber.Text = $"Page {pageNumber} / {pages}";
            }
        }


        private void handleSplit(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            Presupuesto Item = (Presupuesto)button.DataContext;
            List<Presupuesto> p = new List<Presupuesto>();
            p.Add(Item);

            SplitPopUp.IsOpen = !SplitPopUp.IsOpen;
            SplitTable.ItemsSource = p;
        }

        private void CloseSplit(object sender, RoutedEventArgs e)
        {
            SplitPopUp.IsOpen = false;
        }

        private void MakeSplit(object sender, RoutedEventArgs e)
        {
            List<Presupuesto> p = (List<Presupuesto>)SplitTable.ItemsSource;
            string Id = p[0].Id;
            Presupuesto og = p[0];
            p.Remove(p[0]);


            if (og.quantity == p.Sum(o => o.quantity))
            {
                Presupuesto res = Romper.change(presupuesto, historial, p, Id, true);
                presupuesto = res;
                historial = new List<KeyValuePair<string, List<Presupuesto>>>();
                if (presupuesto.hijos != null)
                {
                    currentData = presupuesto.hijos;
                }

                makePagination();
                Tabla.ItemsSource = showing;
                SplitPopUp.IsOpen = false;
                SaveButton.Visibility = Visibility.Visible;
            }
            else
            {
                SplitPopUp.IsOpen = false;
                System.Windows.MessageBox.Show("The sum of the new quantities doesnt match the original quantity","Alert", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose a folder to save the JSON file";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = Path.Combine(dialog.SelectedPath, $"{presupuesto.name}.json");

                    try
                    {
                        presupuestoService.saveJson(filePath, presupuesto);
                        System.Windows.MessageBox.Show($"file saved in {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed at saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
