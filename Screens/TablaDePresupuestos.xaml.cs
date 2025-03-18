using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // Para FolderBrowserDialog
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using Bc3_WPF.Backend.Auxiliar;
using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.Backend.Services;
using Bc3_WPF.Screens.Charts;
using LiveChartsCore.SkiaSharpView.WPF;

namespace Bc3_WPF.Screens
{
    public partial class TablaDePresupuestos : System.Windows.Controls.UserControl
    {
        #region Private Fields
        private Presupuesto? presupuesto;
        private List<KeyValuePair<string, List<Presupuesto>>> historial = new();
        private List<Presupuesto> currentData = new();
        private List<Presupuesto> showing = new();
        private List<KeyValuePair<string, Presupuesto>> previous = new();
        private List<KeyValuePair<string, List<KeyValuePair<string, decimal>>>> changes = new();
        private List<string> dbs = new();
        private ObservableCollection<Presupuesto> treeInfo = new();
        private Dictionary<string, List<string>> idArray = new();
        private List<string> medidores = new();
        private List<KeyValuePair<string, Dictionary<string, double?>>> chartNumber = new();
        private string med = "";
        private int pageNumber = 1;
        private int rowsPerPage = 20;
        private decimal? pages;
        private string? fileName;
        private Pie? chartData;
        private string dbSelected = "";
        #endregion

        public TablaDePresupuestos()
        {
            InitializeComponent();
        }

        #region File Loading & UI Setup

        /// <summary>
        /// Handles the BC3 file loading process
        /// </summary>
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
                var data = filePath.EndsWith(".bc3")
                    ? presupuestoService.loadFromBC3(filePath)
                    : presupuestoService.loadFromJson(filePath);

                presupuesto = data.Item1;
                medidores = data.Item2.ToList();
                idArray = data.Item3;

                medidores.Insert(0, "N/A");

                currentData = presupuesto?.hijos ?? new();
                chartData = new Pie();

                // Initialize UI components with data
                makePagination();
                SetupUI();
                updateDoughtChart();
                getMedidores();

                // Initialize chart data
                Dictionary<string, double?> dict = new();
                foreach (string s in medidores)
                {
                    presupuesto.CalculateValues(s);
                    dict[s] = presupuesto.hijos.Select(e => e.display).Sum();
                }

                chartNumber.Add(new KeyValuePair<string, Dictionary<string, double?>>("Initial", dict));
            }
        }

        /// <summary>
        /// Sets up the UI components after loading a file
        /// </summary>
        private void SetupUI()
        {
            // Update text elements
            Titulo.Text = $" File: {fileName}";
            TitleTable.Text = presupuesto?.name;

            if (chartData != null)
            {
                Chart.Title = chartData.TitleChart;
                PieChart.Title = chartData.TitlePie;
            }

            // Update visibility of UI components
            RectangleInfo.Visibility = Visibility.Visible;
            TableSection.Visibility = Visibility.Visible;
            Paginator.Visibility = Visibility.Visible;

            // Hide initial components
            InitialOverlay.Visibility = Visibility.Collapsed;

            // Set data sources
            Tabla.ItemsSource = showing;
            SelectMedidor.ItemsSource = medidores;

            LoadDatabases();

            // Set data sources
            SelectBaseDatos.ItemsSource = dbs;
            Tabla.ItemsSource = showing;
            SelectMedidor.ItemsSource = medidores;

            // Setup tree
            treeInfo.Clear();
            treeInfo.Add(presupuesto);
            Tree.ItemsSource = treeInfo;

            // Show save button if applicable
            SaveButton.Visibility = previous.Count > 0 ? Visibility.Visible : Visibility.Hidden;

            // Make sure charts are hidden and table takes full space initially
            ChartSection.Visibility = Visibility.Collapsed;
            Grid.SetRow(TableSection, 2);
            Grid.SetRowSpan(TableSection, 2);
            ToggleGraphs.Content = "Show Graphs";
        }

        /// <summary>
        /// Carga las bases de datos disponibles desde SustainabilityService
        /// </summary>
        private void LoadDatabases()
        {
            // Obtener las bases de datos desde SustainabilityService
            var sustainabilityRecords = SustainabilityService.getFromDatabase();
            dbs = SustainabilityService.getDatabases(sustainabilityRecords).ToList();

            // Añadir opción "N/A" al inicio
            dbs.Insert(0, "N/A");
        }

        #endregion

        #region Navigation & Tree Handling

        /// <summary>
        /// Handles clicking on a node with children in the data grid
        /// </summary>
        private void handleHijos(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Presupuesto item && item.hijos != null)
            {
                NavigateToChildren(item);
            }
        }

        /// <summary>
        /// Handles tree item click
        /// </summary>
        private void TreeClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Presupuesto item)
            {
                if (item.hijos == null || item.hijos.Count == 0)
                {
                    PrepareAndShowSplitPopup(item);
                }
                else
                {
                    NavigateToChildren(item);
                }
            }
        }

        /// <summary>
        /// Navigates to the children of a given item
        /// </summary>
        private void NavigateToChildren(Presupuesto item)
        {
            // Add current data to history
            historial.Add(new(item.Id, currentData));

            // Set current data to item's children plus any previous entries with same parent ID
            currentData = item.hijos.Concat(previous
                .Where(p => p.Key == item.Id)
                .Select(p => p.Value))
                .OrderBy(p => p.Id)
                .ToList();

            // Update UI
            makePagination();
            Tabla.ItemsSource = showing;

            // Show back button if we have history
            BackButton.Visibility = historial.Count > 0 ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Go back to previous level in navigation
        /// </summary>
        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            if (historial.Count > 0)
            {
                currentData = historial.Last().Value;
                historial.RemoveAt(historial.Count - 1);

                makePagination();
                Tabla.ItemsSource = showing;

                BackButton.Visibility = historial.Count == 0 ? Visibility.Hidden : Visibility.Visible;
            }
        }

        #endregion

        #region Pagination

        /// <summary>
        /// Changes the current page by the given step
        /// </summary>
        private void ChangePage(int step)
        {
            pageNumber += step;
            int lowerBound = (pageNumber - 1) * rowsPerPage;
            showing = currentData.Skip(lowerBound).Take(rowsPerPage).ToList();
            Tabla.ItemsSource = showing;
            PageNumber.Text = $"Page {pageNumber} of {pages}";
        }

        private void PreviousPage(object sender, RoutedEventArgs e)
        {
            if (pageNumber > 1)
            {
                ChangePage(-1);
            }
        }

        private void NextPage(object sender, RoutedEventArgs e)
        {
            if (pageNumber < pages)
            {
                ChangePage(1);
            }
        }

        /// <summary>
        /// Sets up pagination controls based on current data
        /// </summary>
        private void makePagination()
        {
            pages = Math.Ceiling((decimal)currentData.Count / rowsPerPage);
            pageNumber = 1;
            showing = currentData.Take(rowsPerPage).ToList();

            // Update visibility of pagination controls
            Next.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            Previous.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            PageNumber.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            PageNumber.Text = $"Page {pageNumber} of {pages}";

        }

        #endregion

        #region Split Functionality

        /// <summary>
        /// Prepares and shows the split popup for a given item
        /// </summary>
        private void PrepareAndShowSplitPopup(Presupuesto item)
        {
            IdField.ItemsSource = idArray[item.category];
            SplitTable.ItemsSource = new List<Presupuesto> {
                new() {
                    Id = item.Id,
                    name = item.name,
                    quantity = item.quantity
                }
            };
            SplitPopUp.IsOpen = true;
        }

        /// <summary>
        /// Handles split button click in the data grid
        /// </summary>
        private void handleSplit(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Presupuesto item)
            {
                PrepareAndShowSplitPopup(item);
            }
        }

        /// <summary>
        /// Closes the split popup
        /// </summary>
        private void CloseSplit(object sender, RoutedEventArgs e) => SplitPopUp.IsOpen = false;

        /// <summary>
        /// Handles the split creation process
        /// </summary>
        private void MakeSplit(object sender, RoutedEventArgs e)
        {
            if (SplitTable.ItemsSource is List<Presupuesto> splitData && splitData.Any())
            {
                Presupuesto original = splitData[0];
                splitData.RemoveAt(0); // More efficient than Remove(splitData[0])

                string parentId = GetParentId();
                original.outdated = true;

                splitData = FilterSplitData(splitData);
                UpdateSplitData(splitData);

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

        /// <summary>
        /// Updates split data with details from original items
        /// </summary>
        private void UpdateSplitData(List<Presupuesto> data)
        {
            foreach (Presupuesto p in data)
            {
                Presupuesto og = presupuestoService.FindPresupuestoById(presupuesto, p.Id);
                p.InternalId = og.InternalId;
                p.medidores = og.medidores;
                p.values = og.values;
                p.category = og.category;
            }
        }

        /// <summary>
        /// Gets the parent ID based on navigation history
        /// </summary>
        private string GetParentId()
        {
            return historial.Count == 0 ? presupuesto?.Id ?? "" : historial.Last().Key;
        }

        /// <summary>
        /// Filters out invalid items from split data
        /// </summary>
        private List<Presupuesto> FilterSplitData(List<Presupuesto> data)
        {
            return data.Where(p => !string.IsNullOrEmpty(p.Id) && p.quantity.HasValue && p.quantity.Value != 0).ToList();
        }

        /// <summary>
        /// Validates if split data quantities match original
        /// </summary>
        private bool IsValidSplitData(Presupuesto original, List<Presupuesto> splitData)
        {
            return original.quantity == splitData.Sum(p => p.quantity);
        }

        /// <summary>
        /// Processes valid split data and updates UI
        /// </summary>
        private void ProcessValidSplitData(Presupuesto original, string parentId, List<Presupuesto> splitData)
        {
            previous.Add(new(parentId, original));
            presupuesto = Romper.change(presupuesto, historial, splitData, original.Id, true);
            historial.Clear();

            // Update chart data
            Dictionary<string, double?> dict = new();
            foreach (string s in medidores)
            {
                presupuesto.CalculateValues(s);
                dict[s] = presupuesto.display;
            }

            chartNumber.Add(new KeyValuePair<string, Dictionary<string, double?>>("change " + original.Id, dict));

            if (med != "" && med != "N/A")
            {
                presupuesto.CalculateValues(med);
            }

            // Update current data
            currentData = presupuesto?.hijos?
                .Concat(previous
                    .Where(p => p.Key == presupuesto.Id)
                    .Select(p => p.Value))
                .OrderBy(p => p.Id)
                .ToList() ?? new();

            // Update UI
            makePagination();
            updateDoughtChart();
            getMedidores();
            Tabla.ItemsSource = showing;
            SplitPopUp.IsOpen = false;
            SaveButton.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Handles invalid split data with appropriate error message
        /// </summary>
        private void HandleInvalidSplitData(List<Presupuesto> splitData)
        {
            SplitPopUp.IsOpen = false;

            string errorMessage = splitData.Count == 0
                ? "Every Row must have an Id and quantity"
                : "The sum of the new quantities doesn't match the original quantity";

            System.Windows.MessageBox.Show(errorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Handles save button click to export data
        /// </summary>
        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new() { Description = "Choose a folder to save the JSON file" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = Path.Combine(dialog.SelectedPath, $"{fileName}-modified.json");
                try
                {
                    presupuestoService.saveJson(filePath, presupuesto);
                    System.Windows.MessageBox.Show($"File saved as {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Charting

        /// <summary>
        /// Updates the chart data and display
        /// </summary>
        private void updateDoughtChart()
        {
            if (chartData == null)
                return;

            var data2 = chartNumber
                .Select(e => new KeyValuePair<string, double?>(e.Key, e.Value.ContainsKey(med) ? e.Value[med] : 0))
                .ToList();

            var data = presupuestoService.toArray(presupuesto).Where(e => e.hijos == null || e.hijos.Count == 0)
                .GroupBy(e => e.category).ToDictionary(e => e.Key, e => e.Sum(e => e.display));

            Pie.updateLineChart(data2, chartData);
            Pie.setDoughtData(data, chartData);
            Pie.setChartTitle("Evolución de " + med, chartData);
            Pie.setDoughtTitle(med + " por Categoría", chartData);

            // Update chart controls
            PieChart.Series = chartData.Series;
            Chart.Series = chartData.Series2;
            Chart.XAxes = chartData.axes;
        }

        /// <summary>
        /// Toggles visibility of charts section
        /// </summary>
        private void ShowGraphs(object sender, RoutedEventArgs e)
        {
            bool isChartsVisible = ChartSection.Visibility == Visibility.Visible;
            ToggleGraphVisibility(!isChartsVisible);
        }

        /// <summary>
        /// Shows or hides the graphs section
        /// </summary>
        private void ToggleGraphVisibility(bool showGraphs)
        {
            // Adjust rows per page based on charts visibility
            rowsPerPage = showGraphs ? 5 : 20;

            // Set table section position and row span
            Grid.SetRow(TableSection, showGraphs ? 3 : 2);
            Grid.SetRowSpan(TableSection, showGraphs ? 1 : 2);

            // Update pagination
            makePagination();

            // Set charts visibility
            ChartSection.Visibility = showGraphs ? Visibility.Visible : Visibility.Hidden;

            // Update button text
            ToggleGraphs.Content = showGraphs ? "Hide Graphs" : "Show Graphs";

            // Refresh table
            Tabla.ItemsSource = showing;
        }

        #endregion

        #region Measurements

        /// <summary>
        /// Updates the measurement displays
        /// </summary>
        private void getMedidores()
        {
            // Sample values - replace with your actual calculation
            var carbono = 0; // Replace with actual carbon calculation
            var agua = 0;    // Replace with actual water calculation

            Quantity.Text = carbono.ToString();
            Concepts.Text = agua.ToString();

            SetMedidorVisibility(true);
        }

        /// <summary>
        /// Calculates quantity and concepts recursively
        /// </summary>
        private (float TotalQuantity, int ConceptCount, HashSet<string> ids) CalculateQuantityAndConcepts(
            List<Presupuesto>? presupuestos,
            HashSet<string> uniqueConcepts)
        {
            if (presupuestos == null)
                return (0, 0, new HashSet<string>());

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

        /// <summary>
        /// Sets visibility of measurement UI elements
        /// </summary>
        private void SetMedidorVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Hidden;

            Concepts.Visibility = visibility;
            ConceptTitle.Visibility = visibility;
            Quantity.Visibility = visibility;
            QuantityTitle.Visibility = visibility;
        }

        #endregion

        #region Medidor and BaseDatos Selection

        /// <summary>
        /// Handles change in base de datos selection
        /// </summary>
        private void handleChangeBaseDatos(object sender, SelectionChangedEventArgs e)
        {
            if (SelectBaseDatos?.SelectedItem == null || presupuesto == null)
                return;

            string selectedContent = SelectBaseDatos.SelectedItem.ToString();

            if (selectedContent != null && selectedContent != "N/A")
            {
                // Guardar la base de datos seleccionada
                dbSelected = selectedContent;

                // Mostrar la columna de base de datos
                TablaBaseDatos.Header = selectedContent;
                TablaBaseDatos.Visibility = Visibility.Visible;

                // Actualizar los valores si ya hay un medidor seleccionado
                if (med != "" && med != "N/A")
                {
                    UpdateValuesWithDatabaseAndMedidor();
                }
            }
            else
            {
                // Reset valores de base de datos
                dbSelected = "";
                TablaBaseDatos.Visibility = Visibility.Hidden;

                // Si hay un medidor seleccionado, usar el método original
                if (med != "" && med != "N/A")
                {
                    presupuesto.NullValues();
                    presupuesto.CalculateValues(med);
                }
            }

            // Actualizar datos y UI
            RefreshDataAndUI();
        }

        /// <summary>
        /// Maneja el cambio de selección de medidor
        /// </summary>
        private void handleChangeMedidor(object sender, SelectionChangedEventArgs e)
        {
            if (SelectMedidor?.SelectedItem == null || presupuesto == null)
                return;

            string selectedContent = SelectMedidor.SelectedItem.ToString();

            if (selectedContent != null && selectedContent != "N/A")
            {
                // Guardar el medidor seleccionado
                med = selectedContent;

                // Mostrar la columna de medidor
                TablaMedidor.Header = selectedContent;
                TablaMedidor.Visibility = Visibility.Visible;

                // Actualizar valores según si hay base de datos seleccionada
                if (dbSelected != "" && dbSelected != "N/A")
                {
                    UpdateValuesWithDatabaseAndMedidor();
                }
                else
                {
                    // Usar el método CalculateValues original si no hay base de datos seleccionada
                    presupuesto.NullValues();
                    presupuesto.CalculateValues(selectedContent);
                }
            }
            else
            {
                // Reset valores
                presupuesto.NullValues();
                med = "";
                TablaMedidor.Visibility = Visibility.Hidden;
            }

            // Actualizar datos y UI
            RefreshDataAndUI();
        }

        /// <summary>
        /// Actualiza los valores usando tanto la base de datos como el medidor seleccionados
        /// </summary>
        private void UpdateValuesWithDatabaseAndMedidor()
        {
            // Primero reiniciar valores
            presupuesto.NullValues();

            // Obtener registros de SustainabilityService
            var sustainabilityRecords = SustainabilityService.getFromDatabase();

            // Filtrar los registros por la base de datos y el medidor seleccionados
            var filteredRecords = sustainabilityRecords
                .Where(sr => sr.Database == dbSelected && sr.Indicator == med)
                .ToList();

            // Obtener las relaciones de código
            var codeRelations = SustainabilityService.getCodeRelation(sustainabilityRecords);

            // Actualizar los valores en los nodos hoja primero
            UpdateLeafNodeValues(presupuesto, filteredRecords, codeRelations);

            // Ahora propagar los valores hacia arriba para asegurar que las sumas sean correctas
            PropagateValuesUpward(presupuesto);
        }

        /// <summary>
        /// Actualiza los valores de los nodos hoja (sin hijos) según los registros de sostenibilidad
        /// </summary>
        private void UpdateLeafNodeValues(Presupuesto node, List<SustainabilityRecord> records, List<KeyValuePair<string, string>> codeRelations)
        {
            // Si es un nodo hoja (sin hijos)
            if (node.hijos == null || !node.hijos.Any())
            {
                // Buscar el código interno correspondiente al código externo
                var relation = codeRelations.FirstOrDefault(cr => cr.Key == node.Id);

                if (!string.IsNullOrEmpty(relation.Value))
                {
                    // Buscar el registro correspondiente
                    var record = records.FirstOrDefault(r => r.InternalId == relation.Value);

                    if (record != null)
                    {
                        // Asignar el valor y la base de datos
                        node.display = record.Value;
                        node.database = dbSelected;
                    }
                }
            }
            else
            {
                // Procesar los hijos recursivamente
                foreach (var child in node.hijos)
                {
                    UpdateLeafNodeValues(child, records, codeRelations);
                }
            }
        }

        /// <summary>
        /// Propaga los valores hacia arriba, sumando los valores de los hijos
        /// </summary>
        private void PropagateValuesUpward(Presupuesto node)
        {
            // Si no tiene hijos, ya tiene su valor asignado
            if (node.hijos == null || !node.hijos.Any())
            {
                return;
            }

            // Procesar recursivamente los hijos primero
            foreach (var child in node.hijos)
            {
                PropagateValuesUpward(child);
            }

            // El valor del nodo es la suma de los valores de sus hijos
            node.display = node.hijos.Sum(h => h.display ?? 0);

            // También asignar la base de datos al nodo padre
            if (node.hijos.Any(h => h.database == dbSelected))
            {
                node.database = dbSelected;
            }
        }

        /// <summary>
        /// Actualiza los datos y refresca la UI
        /// </summary>
        private void RefreshDataAndUI()
        {
            // Actualizar current data
            currentData = presupuesto?.hijos?
                .Concat(previous
                    .Where(p => p.Key == presupuesto.Id)
                    .Select(p => p.Value))
                .OrderBy(p => p.Id)
                .ToList() ?? new();

            makePagination();
            historial.Clear();
            Tabla.ItemsSource = showing;
            updateDoughtChart();

            // Actualizar los totales en las tarjetas informativas
            if (med != "" && med != "N/A")
            {
                // Aquí puedes actualizar los totales mostrados en las tarjetas según los nuevos valores
                Quantity.Text = presupuesto.display?.ToString("N2") ?? "0";

                // También podrías mostrar diferentes totales según el medidor seleccionado
                if (med.Contains("CO2") || med.Contains("CARBON"))
                {
                    QuantityTitle.Text = "Total Carbono";
                }
                else if (med.Contains("WATER") || med.Contains("AGUA"))
                {
                    QuantityTitle.Text = "Total Agua";
                }
                else
                {
                    QuantityTitle.Text = $"Total {med}";
                }
            }

            // Hide back button
            BackButton.Visibility = Visibility.Hidden;
        }
        #endregion
    }
}