using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using Bc3_WPF.Backend.Auxiliar;
using Bc3_WPF.Backend.Services;
using Bc3_WPF.Screens.Charts;
using LiveChartsCore.SkiaSharpView.WPF;

namespace Bc3_WPF.Screens
{
    public partial class TablaDePresupuestos : System.Windows.Controls.UserControl
    {
        private Presupuesto? presupuesto;
        private List<KeyValuePair<string, List<Presupuesto>>> historial = new();
        private List<Presupuesto> currentData = new();
        private List<Presupuesto> showing = new();
        private List<KeyValuePair<string, Presupuesto>> previous = new();
        private List<KeyValuePair<string, List<KeyValuePair<string, decimal>>>> changes = new();
        private List<string> dbs = ["Ayesa-Enviroment", "Endesa-Enviroment"];
        private ObservableCollection<Presupuesto> treeInfo = new();
        private List<string> idArray = new();
        private int pageNumber = 1;
        private int rowsPerPage = 20;
        private decimal? pages;
        private string? fileName;
        private Pie? chartData;

        public TablaDePresupuestos() => InitializeComponent();

        #region TABLE-SET_UI

        private void LoadBC3(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new()
            {
                Title = "Select a .bc3 or .json File",
                Filter = "All Supported Files (*.bc3;*.json)|*.bc3;*.json"
            };

            if (ofd.ShowDialog() == true)
            {
                fileName = Path.GetFileNameWithoutExtension(ofd.SafeFileName);
                string filePath = ofd.FileName;
                presupuesto = filePath.EndsWith(".bc3") ? presupuestoService.loadFromBC3(filePath) : presupuestoService.loadFromJson(filePath);
                idArray = presupuestoService.getConceptsSinHijos(filePath);
                
                currentData = presupuesto?.hijos ?? new();
                chartData = new Pie();

                DBSplitChanges("Initial");
                makePagination();
                SetupUI();
                updateDoughtChart();
                getMedidores();
            }
        }

        private void SetupUI()
        {
            Titulo.Text = $"🗃️  File: {fileName}";
            TitleTable.Text = presupuesto?.name;
            Chart.Title = chartData?.TitleChart;
            PieChart.Title = chartData?.TitlePie;
            
            RectangleInfo.Visibility = Visibility.Visible;
            //ChartSection.Visibility = Visibility.Visible;
            TableSection.Visibility = Visibility.Visible;
            Paginator.Visibility = Visibility.Visible;
            FileButton.Visibility = Visibility.Hidden;
            Tabla.ItemsSource = showing;
            treeInfo.Add(presupuesto);
            Tree.ItemsSource = treeInfo;
        }

        private void handleHijos(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Presupuesto item && item.hijos != null)
            {
                historial.Add(new(item.Id, currentData));
                currentData = item.hijos.Concat(previous.Where(p => p.Key == item.Id).Select(p => p.Value)).OrderBy(p => p.Id).ToList();
                
                makePagination();
                updateDoughtChart();
                Tabla.ItemsSource = showing;
                BackButton.Visibility = historial.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void TreeClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Presupuesto item)
            {
                if (item.hijos == null || item.hijos.Count == 0)
                {
                    IdField.ItemsSource = idArray;
                    SplitTable.ItemsSource = new List<Presupuesto> { new() { Id = item.Id, name = item.name, quantity = item.quantity, fecha = item.fecha } };
                    SplitPopUp.IsOpen = !SplitPopUp.IsOpen;
                }
                else
                {
                    historial.Add(new(item.Id, currentData));
                    currentData = item.hijos.Concat(previous.Where(p => p.Key == item.Id).Select(p => p.Value)).OrderBy(p => p.Id).ToList();

                    makePagination();
                    updateDoughtChart();
                    Tabla.ItemsSource = showing;
                    BackButton.Visibility = historial.Count > 0 ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        #endregion

        #region PAGES
        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            if (historial.Count > 0)
            {
                currentData = historial.Last().Value;
                historial.RemoveAt(historial.Count - 1);
                makePagination();
                updateDoughtChart();
                Tabla.ItemsSource = showing;
                BackButton.Visibility = historial.Count == 0 ? Visibility.Hidden : Visibility.Visible;
            }
        }

        private void ChangePage(int step)
        {
            pageNumber += step;
            int lowerBound = (pageNumber - 1) * rowsPerPage;
            showing = currentData.Skip(lowerBound).Take(rowsPerPage).ToList();
            Tabla.ItemsSource = showing;
            PageNumber.Text = $"Page {pageNumber} / {pages}";
        }

        private void PreviousPage(object sender, RoutedEventArgs e) { if (pageNumber > 1) ChangePage(-1); updateDoughtChart(); }
        private void NextPage(object sender, RoutedEventArgs e) { if (pageNumber < pages) ChangePage(1); updateDoughtChart(); }

        private void makePagination()
        {
            pages = Math.Ceiling((decimal)currentData.Count / rowsPerPage);
            pageNumber = 1;
            showing = currentData.Take(rowsPerPage).ToList();
            Next.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            Previous.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            PageNumber.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            PageNumber.Text = $"Page {pageNumber} / {pages}";

            updateDoughtChart();
        }

        #endregion

        #region SPLIT
        private void handleSplit(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Presupuesto item)
            {
                IdField.ItemsSource = idArray;
                SplitTable.ItemsSource = new List<Presupuesto> { new() { Id = item.Id, name = item.name, quantity = item.quantity, fecha = item.fecha } };
                SplitPopUp.IsOpen = !SplitPopUp.IsOpen;
            }
        }

        private void CloseSplit(object sender, RoutedEventArgs e) => SplitPopUp.IsOpen = false;

        private void MakeSplit(object sender, RoutedEventArgs e)
        {
            if (SplitTable.ItemsSource is List<Presupuesto> splitData && splitData.Any())
            {
                Presupuesto original = splitData[0];
                splitData.RemoveAt(0); // More efficient than Remove(splitData[0])

                string parentId = GetParentId();
                original.outdated = true;

                splitData = FilterSplitData(splitData);
                UpdateSplitDataDates(splitData);

                if (IsValidSplitData(original, splitData))
                {
                    ProcessValidSplitData(original, parentId, splitData);
                }
                else
                {
                    HandleInvalidSplitData(splitData);
                }
            }
        }

        private string GetParentId()
        {
            return historial.Count == 0 ? presupuesto?.Id ?? "" : historial.Last().Key;
        }

        private List<Presupuesto> FilterSplitData(List<Presupuesto> data)
        {
            return data.Where(p => !string.IsNullOrEmpty(p.Id) && p.quantity.HasValue && p.quantity.Value != 0).ToList();
        }

        private void UpdateSplitDataDates(List<Presupuesto> data)
        {
            data.ForEach(p => p.fecha = DateOnly.FromDateTime(DateTime.Now));
        }

        private bool IsValidSplitData(Presupuesto original, List<Presupuesto> splitData)
        {
            return original.quantity == splitData.Sum(p => p.quantity);
        }

        private void ProcessValidSplitData(Presupuesto original, string parentId, List<Presupuesto> splitData)
        {
            previous.Add(new(parentId, original));
            presupuesto = Romper.change(presupuesto, historial, splitData, original.Id, true);
            historial.Clear();
            currentData = presupuesto?.hijos?.Concat(previous.Where(p => p.Key == presupuesto.Id).Select(p => p.Value)).OrderBy(p => p.Id).ToList() ?? new();

            DBSplitChanges(original.Id);
            makePagination();
            updateDoughtChart();
            getMedidores();
            Tabla.ItemsSource = showing;
            SplitPopUp.IsOpen = false;
            SaveButton.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Hidden;
        }

        private void HandleInvalidSplitData(List<Presupuesto> splitData)
        {
            SplitPopUp.IsOpen = false;

            string errorMessage = splitData.Count == 0
                ? "Every Row must have an Id and quantity"
                : "The sum of the new quantities doesn't match the original quantity";

            System.Windows.MessageBox.Show(errorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new() { Description = "Choose a folder to save the JSON file" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = Path.Combine(dialog.SelectedPath, $"{fileName}-1.json");
                try
                {
                    presupuestoService.saveJson(filePath, presupuesto);
                    System.Windows.MessageBox.Show($"File saved in {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed at saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region CHARTING
        private void updateDoughtChart()
        {
            var validData = showing.Where(e => e.outdated != true).ToList();
            var doughtData = validData.Select(e => new KeyValuePair<string, decimal?>(e.Id, e.Carbono)).ToList();
            var data = changes.Select(e => new KeyValuePair<string, decimal?> (e.Key, e.Value[0].Value)).ToList();
            var data2 = changes.Select(e => new KeyValuePair<string, decimal?>(e.Key, e.Value[1].Value)).ToList();

            Pie.setDoughtData(doughtData, chartData);
            Pie.updateLineChart(data, data2, chartData);
            PieChart.Series = chartData.Series;
            Chart.Series = chartData.Series2;
            Chart.XAxes = chartData.axes;

            SetChartVisibility(true);
        }

        private void SetChartVisibility(bool isVisible)
        {
            PieRectangle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            ChartRectangle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }

        private void ShowGraphs(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                bool isChartsVisible = ChartSection.Visibility == Visibility.Visible;
                ToggleGraphVisibility(!isChartsVisible);
            }
        }

        private void ToggleGraphVisibility(bool showGraphs)
        {
            rowsPerPage = showGraphs ? 5 : 20;
            Grid.SetRow(TableSection, showGraphs ? 3 : 2);
            makePagination();
            ChartSection.Visibility = showGraphs ? Visibility.Visible : Visibility.Hidden;
            ToggleGraphs.Content = showGraphs ? "Hide Graphs" : "Show Graphs";
            Tabla.ItemsSource = showing;
        }

        #endregion

        #region MEASUREMENTS
        private void getMedidores()
        {
            //var processedData = CalculateQuantityAndConcepts(presupuesto?.hijos, new HashSet<string> { presupuesto?.Id ?? string.Empty });
            var Carbono = presupuesto.hijos.Sum(e => e.Carbono) ?? 0;
            var Agua = presupuesto.hijos.Sum(e => e.Agua) ?? 0;

            Quantity.Text = ((int)Carbono).ToString();
            Concepts.Text = ((int)Agua).ToString();
            // idArray = processedData.ids;
            SetMedidorVisibility(true);
        }

        private (float TotalQuantity, int ConceptCount, HashSet<string> ids) CalculateQuantityAndConcepts(List<Presupuesto>? presupuestos, HashSet<string> uniqueConcepts)
        {
            if (presupuestos == null) return (0, 0, []);

            float totalQuantity = 0;
            foreach (var p in presupuestos)
            {
                uniqueConcepts.Add(p.Id);
                totalQuantity += p.quantity ?? 0;
                var subResult = CalculateQuantityAndConcepts(p.hijos, uniqueConcepts);
                totalQuantity += subResult.TotalQuantity;
            }
            return (totalQuantity, uniqueConcepts.Count, uniqueConcepts);
        }

        private void SetMedidorVisibility(bool isVisible)
        {
            Concepts.Visibility =  Visibility.Visible;
            ConceptTitle.Visibility =  Visibility.Visible;
            ConceptRectangle.Visibility =  Visibility.Visible;
            Quantity.Visibility = Visibility.Visible;
            QuantityRectangle.Visibility = Visibility.Visible;
            QuantityTitle.Visibility =  Visibility.Visible;
        }

        #endregion

        #region DataBase
        private void ChangeDB(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            /*
            if (e.RemovedItems.Count == 0 || SelectDB?.SelectedItem is not ComboBoxItem selectedItem)
                return;

            string selectedContent = selectedItem.Content.ToString();
            bool isNA = selectedContent == "N/A";

            if (presupuesto != null)
            {
                if (isNA)
                {
                    presupuesto.NullValues();
                }
                else
                {
                    Dictionary<string, KeyValuePair<decimal, decimal>> db = DatabaseService.LoadData($"{selectedContent}-Enviroment");
                    presupuesto.CalculateValues(db);
                }
            }

            getMedidores();
            UpdateDBUI(isNA);*/
        }

        private void UpdateDBUI(bool isNA)
        {
            /*currentData = currentData = presupuesto?.hijos?.Concat(previous.Where(p => p.Key == presupuesto.Id).Select(p => p.Value)).OrderBy(p => p.Id).ToList() ?? new();
            makePagination();
            Tabla.ItemsSource = showing;
            updateDoughtChart();

            Visibility columnVisibility = isNA ? Visibility.Hidden : Visibility.Visible;
            ColumnAgua.Visibility = columnVisibility;
            ColumnCarbono.Visibility = columnVisibility;*/
        }

        private void DBSplitChanges(string parentId)
        {
            /*List<KeyValuePair<string, decimal>> dbData = new();
            foreach (string db in dbs)
            {
                Dictionary<string, KeyValuePair<decimal, decimal>> data = DatabaseService.LoadData(db);
                presupuesto.CalculateValues(data);
                KeyValuePair<string, decimal> k = new KeyValuePair<string, decimal>(db, (decimal)presupuesto.hijos.Sum(p => p.Carbono));
                dbData.Add(k);
            }

            changes.Add(new KeyValuePair<string, List<KeyValuePair<string, decimal>>>(parentId, dbData));
            presupuesto.NullValues();*/
        }

        #endregion
    }
}
