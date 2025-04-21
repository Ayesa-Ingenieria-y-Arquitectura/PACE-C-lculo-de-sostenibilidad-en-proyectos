using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // Para FolderBrowserDialog
using System.Windows.Media;
using System.Windows.Input;
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using Bc3_WPF.Backend.Auxiliar;
using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.Backend.Services;
using Button = System.Windows.Controls.Button;

namespace Bc3_WPF.Screens
{
    public partial class TablaDePresupuestos : System.Windows.Controls.UserControl
    {
        #region Private Fields
        private Presupuesto? presupuesto;
        private List<string> historial = new();
        private List<Presupuesto> currentData = new();
        private List<Presupuesto> showing = new();
        private List<KeyValuePair<string, Presupuesto>> previous = new();
        private List<KeyValuePair<string, List<KeyValuePair<string, decimal>>>> changes = new();
        private ObservableCollection<Presupuesto> treeInfo = new();
        private Dictionary<string, List<string>> idArray = new();
        private List<string> medidores = new();
        private string med = "";
        private int pageNumber = 1;
        private int rowsPerPage = 20;
        private decimal? pages;
        private string? fileName;
        private string? path;
        private string dbSelected = "DB1";
        private Presupuesto? currentSelectedItem;

        // Campos para la funcionalidad de búsqueda
        private List<Presupuesto> _filteredData = new();
        private string _lastSearchText = string.Empty;

        // Nuevo campo para almacenar los términos de búsqueda activos
        private HashSet<string> _searchTerms = new HashSet<string>();
        #endregion

        public TablaDePresupuestos()
        {
            InitializeComponent();

            // Inicializar campos
            _searchTerms = new HashSet<string>();
            _filteredData = new List<Presupuesto>();
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
                // Resetear búsquedas y filtros
                ResetSearch();

                fileName = Path.GetFileNameWithoutExtension(ofd.SafeFileName);
                string filePath = ofd.FileName;
                (Presupuesto, HashSet<string>, Dictionary<string, List<string>>) data;
                if (filePath.EndsWith(".bc3"))
                {
                    data = presupuestoService.loadFromBC3(filePath);
                }
                else
                {
                    data = presupuestoService.loadFromJson(filePath);
                    path = filePath;
                }

                presupuesto = data.Item1;
                medidores = data.Item2.ToList();
                idArray = data.Item3;

                medidores.Insert(0, "N/A");

                currentData = presupuesto?.hijos ?? new();
                _filteredData = new List<Presupuesto>(currentData); // Inicializar datos filtrados

                // Initialize UI components with data
                makePagination();
                SetupUI();
                getMedidores();

                // Ocultar información de filtro al cargar nuevo archivo
                FilterInfoBorder.Visibility = Visibility.Collapsed;
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

            // Update visibility of UI components
            RectangleInfo.Visibility = Visibility.Visible;
            TableSection.Visibility = Visibility.Visible;
            Paginator.Visibility = Visibility.Visible;

            // Hide initial components
            InitialOverlay.Visibility = Visibility.Collapsed;

            // Set data sources
            Tabla.ItemsSource = showing;
            SelectMedidor.ItemsSource = medidores;

            // Setup tree
            treeInfo.Clear();
            treeInfo.Add(presupuesto);
            Tree.ItemsSource = treeInfo;
            if (Tree.ItemsSource is IEnumerable<Presupuesto> rootNodes)
            {
                Presupuesto.SetParentReferences(rootNodes.ToList());
            }

            // Show save button if applicable
            SaveButton.Visibility = Visibility.Visible;

            // Make sure charts are hidden and table takes full space initially
            Grid.SetRow(TableSection, 2);
            Grid.SetRowSpan(TableSection, 2);
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
                // Crear la lista con la ruta completa (padres + nodo actual)
                List<Presupuesto> currentPath = new List<Presupuesto>();

                // Comenzar con el nodo actual
                Presupuesto current;

                if (item.hijos == null || item.hijos.Count == 0)
                {
                    current = item.Parent ?? null;
                }
                else
                {
                    current = item;
                }

                // Recorrer hacia arriba para agregar todos los padres a la lista
                while (current != null)
                {
                    // Insertar al principio para tener el orden correcto (raíz primero)
                    currentPath.Insert(0, current);
                    current = current.Parent;
                }
                currentPath.Remove(currentPath[0]);

                historial = currentPath.Select(e => e.Id).ToList();

                // Mantener la funcionalidad original
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
            // Set current data to item's children plus any previous entries with same parent ID
            currentData = item.hijos.Concat(previous
                .Where(p => p.Key == item.Id)
                .Select(p => p.Value))
                .OrderBy(p => p.Id)
                .ToList();

            historial.Add(item.Id);

            // Mantener los términos de búsqueda existentes y volver a aplicar el filtro
            ApplySearchFilter();

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
                List<Presupuesto> hijos = presupuesto.hijos;
                Presupuesto p = presupuesto;

                for (int i = 1; i < historial.Count; i++)
                {
                    if (i != 0)
                        p = hijos.Where(e => e.Id == historial[i - 1]).First();
                    hijos = p.hijos;
                }
                currentData = hijos.Concat(previous
                .Where(a => a.Key == p.Id)
                .Select(a => a.Value))
                .OrderBy(a => a.Id)
                .ToList();
                if (historial.Count > 0)
                    historial.RemoveAt(historial.Count - 1);

                // Mantener los términos de búsqueda existentes y volver a aplicar el filtro
                ApplySearchFilter();

                Tabla.ItemsSource = showing;

                // Actualizar visibilidad del botón de regreso
                BackButton.Visibility = historial.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            }
        }

        /// <summary>
        /// Recarga los datos después de un cambio
        /// </summary>
        private void reloadAfterChange()
        {
            List<Presupuesto> hijos = presupuesto.hijos;
            Presupuesto p = presupuesto;

            foreach (string s in historial)
            {
                p = hijos.Where(e => e.Id == s).First();
                hijos = p.hijos;
            }

            currentData = hijos.Concat(previous
                .Where(a => a.Key == p.Id)
                .Select(a => a.Value))
                .OrderBy(a => a.Id)
                .ToList();

            // Volver a aplicar el filtro actual con los términos de búsqueda existentes
            ApplySearchFilter();

            Tabla.Items.Refresh();
            Tabla.ItemsSource = showing;

            treeInfo.Clear();
            treeInfo.Add(presupuesto);
            Tree.ItemsSource = treeInfo;
            if (Tree.ItemsSource is IEnumerable<Presupuesto> rootNodes)
            {
                Presupuesto.SetParentReferences(rootNodes.ToList());
            }

            // Show back button if we have history
            BackButton.Visibility = historial.Count > 0 ? Visibility.Visible : Visibility.Hidden;
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

            // Usar los datos filtrados si hay un filtro activo
            List<Presupuesto> dataToUse = HasActiveSearchTerms() ? _filteredData : currentData;

            showing = dataToUse.Skip(lowerBound).Take(rowsPerPage).ToList();
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
            // Si hay un texto de búsqueda activo, usar los datos filtrados
            List<Presupuesto> dataToUse = HasActiveSearchTerms() ? _filteredData : currentData;

            pages = Math.Ceiling((decimal)dataToUse.Count / rowsPerPage);
            pageNumber = 1;
            showing = dataToUse.Take(rowsPerPage).ToList();

            // Update visibility of pagination controls
            UpdatePaginationControls();
        }

        /// <summary>
        /// Actualiza los controles de paginación según el número de páginas
        /// </summary>
        private void UpdatePaginationControls()
        {
            bool hasMultiplePages = pages > 1;
            Next.Visibility = hasMultiplePages ? Visibility.Visible : Visibility.Hidden;
            Previous.Visibility = hasMultiplePages ? Visibility.Visible : Visibility.Hidden;
            PageNumber.Visibility = hasMultiplePages ? Visibility.Visible : Visibility.Hidden;
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

                    // Mantener la búsqueda después de dividir
                    ApplySearchFilter();
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
            return historial.Count == 0 ? presupuesto?.Id ?? "" : historial.Last();
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
            List<string> h = new List<string>(historial);
            presupuesto = Romper.change(presupuesto, h, splitData, original.Id, true);

            if (med != "" && med != "N/A")
            {
                presupuesto.CalculateValues(med);
            }

            // Update current data
            reloadAfterChange();

            getMedidores();
            SplitPopUp.IsOpen = false;
            SaveButton.Visibility = Visibility.Visible;
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
            Presupuesto pr = Presupuesto.copy(presupuesto);

            // Usar SaveFileDialog en lugar de FolderBrowserDialog
            using SaveFileDialog saveDialog = new()
            {
                Title = "Guardar archivo JSON",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                FileName = $"{fileName}-modified.json" // Nombre predeterminado
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveDialog.FileName;
                try
                {
                    presupuestoService.saveJson(filePath, pr);
                    System.Windows.MessageBox.Show($"Archivo guardado como {filePath}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al guardar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
            Concepts.Text = presupuestoService.toArray(presupuesto).Select(e => e.category).ToHashSet().Count.ToString();

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

            // Mantener los términos de búsqueda existentes y volver a aplicar el filtro
            ApplySearchFilter();
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
            reloadAfterChange();

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
        }

        #endregion

        #region Search Functionality

        /// <summary>
        /// Maneja los cambios en el texto del cuadro de búsqueda
        /// </summary>
        private void HandleSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Obtener el texto de búsqueda actual
            string currentSearch = SearchBox.Text.ToLower().Trim();

            // Mostrar/ocultar el botón de limpieza
            ClearSearchButton.Visibility = string.IsNullOrEmpty(currentSearch) ? Visibility.Collapsed : Visibility.Visible;

            // Si hay un cambio en el texto de búsqueda
            if (currentSearch != _lastSearchText)
            {
                _lastSearchText = currentSearch;

                // Aplicar el filtro con el texto actual y los términos guardados
                ApplySearchFilter();
            }
        }

        /// <summary>
        /// Maneja el evento KeyDown en el cuadro de búsqueda
        /// </summary>
        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                AddCurrentSearchTerm();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de añadir término
        /// </summary>
        private void AddSearchTerm_Click(object sender, RoutedEventArgs e)
        {
            AddCurrentSearchTerm();
        }

        /// <summary>
        /// Añade el término actual a la lista de términos de búsqueda
        /// </summary>
        private void AddCurrentSearchTerm()
        {
            string term = SearchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(term) && !_searchTerms.Contains(term))
            {
                _searchTerms.Add(term);
                UpdateSearchTermsDisplay();

                // Limpiar el cuadro de búsqueda
                SearchBox.Text = string.Empty;
                _lastSearchText = string.Empty;

                // Aplicar el filtro actualizado
                ApplySearchFilter();
            }
        }

        /// <summary>
        /// Crea un elemento visual para representar un término de búsqueda
        /// </summary>
        private UIElement CreateSearchTermTag(string term)
        {
            // Crear un borde con el estilo definido
            Border border = new Border
            {
                Style = FindResource("SearchTermTag") as Style
            };

            // Crear un grid para contener el texto y el botón de cerrar
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Texto del término
            TextBlock textBlock = new TextBlock
            {
                Text = term,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = FindResource("DarkTextColor") as SolidColorBrush
            };
            Grid.SetColumn(textBlock, 0);

            // Botón para quitar el término
            Button closeButton = new Button
            {
                Content = "✕",
                Style = FindResource("TagRemoveButton") as Style,
                Tag = term // Guardar el término para identificarlo
            };
            closeButton.Click += RemoveSearchTerm_Click;
            Grid.SetColumn(closeButton, 1);

            // Añadir elementos al grid
            grid.Children.Add(textBlock);
            grid.Children.Add(closeButton);

            // Establecer el contenido del borde
            border.Child = grid;

            return border;
        }

        /// <summary>
        /// Maneja la eliminación de un término de búsqueda
        /// </summary>
        private void RemoveSearchTerm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string term)
            {
                _searchTerms.Remove(term);
                UpdateSearchTermsDisplay();

                // Aplicar el filtro con los términos restantes
                ApplySearchFilter();
            }
        }

        /// <summary>
        /// Actualiza la visualización de los términos de búsqueda
        /// </summary>
        private void UpdateSearchTermsDisplay()
        {
            // Limpiar el panel
            SearchTermsPanel.Children.Clear();

            // Si no hay términos, ocultar el panel
            if (_searchTerms.Count == 0)
            {
                SearchTermsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Mostrar el panel
            SearchTermsPanel.Visibility = Visibility.Visible;

            // Añadir una etiqueta para cada término
            foreach (string term in _searchTerms)
            {
                SearchTermsPanel.Children.Add(CreateSearchTermTag(term));
            }

            // Añadir un botón "Limpiar todos" al final si hay múltiples términos
            if (_searchTerms.Count > 1)
            {
                Button clearAllButton = new Button
                {
                    Content = "Limpiar todos",
                    Style = FindResource("ModernButton") as Style,
                    Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#95A5A6")),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(5, 0, 0, 5),
                    FontSize = 12
                };
                clearAllButton.Click += (s, e) => {
                    _searchTerms.Clear();
                    UpdateSearchTermsDisplay();
                    ApplySearchFilter();
                };
                SearchTermsPanel.Children.Add(clearAllButton);
            }
        }

        /// <summary>
        /// Aplica el filtro con todos los términos de búsqueda activos
        /// </summary>
        private void ApplySearchFilter()
        {
            // Si no hay datos, no hacer nada
            if (currentData == null || currentData.Count == 0)
                return;

            // Si no hay términos de búsqueda activos ni texto actual, restaurar datos originales
            if (_searchTerms.Count == 0 && string.IsNullOrEmpty(_lastSearchText))
            {
                _filteredData = new List<Presupuesto>(currentData);
            }
            else
            {
                // Colección completa de términos de búsqueda
                HashSet<string> allTerms = new HashSet<string>(_searchTerms);

                // Añadir el texto actual si no está vacío
                if (!string.IsNullOrEmpty(_lastSearchText))
                {
                    allTerms.Add(_lastSearchText);
                }

                // Filtrar los elementos que coincidan con AL MENOS UNO de los términos (OR lógico)
                _filteredData = currentData.Where(item =>
                    // Es un nodo hoja y su Id o su nombre o su categoría coincide con algún término
                    ((item.hijos == null || item.hijos.Count == 0) &&
                        (item.Id != null && allTerms.Any(term => item.Id.ToLower().Contains(term)) ||
                        item.name != null && allTerms.Any(term => item.name.ToLower().Contains(term)) ||
                        item.category != null && allTerms.Any(term => item.category.ToLower().Contains(term)))) ||
                    // O tiene hijos y alguno de sus descendientes coincide con algún término
                    (item.hijos != null &&
                        item.hijos.Count > 0 &&
                        HasMatchingDescendant(item, allTerms))
                ).ToList();
            }

            // Actualizar paginación con los datos filtrados
            UpdatePaginationWithFilteredData();

            // Actualizar la visualización de información de filtro
            UpdateFilterInfoDisplay();
        }

        /// <summary>
        /// Verifica si un nodo tiene algún descendiente que coincida con alguno de los términos
        /// </summary>
        private bool HasMatchingDescendant(Presupuesto node, HashSet<string> searchTerms)
        {
            // Si es un nodo hoja, comprobar si coincide con algún término
            if (node.hijos == null || node.hijos.Count == 0)
            {
                return (node.Id != null && searchTerms.Any(term => node.Id.ToLower().Contains(term))) ||
                       (node.name != null && searchTerms.Any(term => node.name.ToLower().Contains(term))) ||
                       (node.category != null && searchTerms.Any(term => node.category.ToLower().Contains(term)));
            }

            // Comprobar si algún hijo directo es un nodo hoja que coincide
            bool hasMatchingLeafChild = node.hijos.Any(child =>
                (child.hijos == null || child.hijos.Count == 0) &&
                ((child.Id != null && searchTerms.Any(term => child.Id.ToLower().Contains(term))) ||
                 (child.name != null && searchTerms.Any(term => child.name.ToLower().Contains(term))) ||
                 (child.category != null && searchTerms.Any(term => child.category.ToLower().Contains(term))))
            );

            if (hasMatchingLeafChild)
            {
                return true;
            }

            // Comprobar recursivamente en los hijos
            return node.hijos.Any(child => HasMatchingDescendant(child, searchTerms));
        }

        /// <summary>
        /// Limpia todos los filtros de búsqueda
        /// </summary>
        private void ClearAllFilters_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar el texto de búsqueda actual
            SearchBox.Text = string.Empty;
            _lastSearchText = string.Empty;

            // Limpiar todos los términos guardados
            _searchTerms.Clear();

            // Actualizar visualización de términos
            UpdateSearchTermsDisplay();

            // Restaurar los datos originales
            _filteredData = new List<Presupuesto>(currentData);
            UpdatePaginationWithFilteredData();

            // Actualizar información de filtro
            UpdateFilterInfoDisplay();
        }

        /// <summary>
        /// Limpia el texto de búsqueda actual
        /// </summary>
        private void ClearSearchText(object sender, RoutedEventArgs e)
        {
            // Limpiar el texto actual
            SearchBox.Text = string.Empty;
            _lastSearchText = string.Empty;

            // Aplicar el filtro (puede haber términos activos todavía)
            ApplySearchFilter();
        }

        /// <summary>
        /// Reinicia la búsqueda completamente, limpiando todos los términos
        /// </summary>
        private void ResetSearch()
        {
            if (SearchBox != null)
            {
                // Limpiar el texto actual
                SearchBox.Text = string.Empty;
                _lastSearchText = string.Empty;

                // Limpiar todos los términos de búsqueda
                _searchTerms.Clear();

                // Actualizar la visualización
                UpdateSearchTermsDisplay();

                // Restaurar datos originales si hay datos
                if (currentData != null && currentData.Count > 0)
                {
                    _filteredData = new List<Presupuesto>(currentData);
                    UpdatePaginationWithFilteredData();
                }

                // Ocultar información de filtro
                if (FilterInfoBorder != null)
                {
                    FilterInfoBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Actualiza la paginación basada en los datos filtrados actuales
        /// </summary>
        private void UpdatePaginationWithFilteredData()
        {
            // Calcular el número total de páginas
            pages = Math.Ceiling((decimal)_filteredData.Count / rowsPerPage);

            // Asegurarse de que la página actual no exceda el número total de páginas
            if (pageNumber > pages && pages > 0)
            {
                pageNumber = (int)pages;
            }
            else if (pageNumber < 1 || pages == 0)
            {
                pageNumber = 1;
            }

            // Calcular el conjunto de datos a mostrar en la página actual
            int lowerBound = (pageNumber - 1) * rowsPerPage;
            showing = _filteredData.Skip(lowerBound).Take(rowsPerPage).ToList();

            // Actualizar la fuente de datos de la tabla
            Tabla.ItemsSource = showing;

            // Actualizar visibilidad y texto de los controles de paginación
            UpdatePaginationControls();

            // Mostrar información de filtrado si hay términos activos
            if (HasActiveSearchTerms())
            {
                // Aquí podríamos añadir un indicador visual de que hay un filtro activo
                // Por ejemplo, cambiar el color del borde del cuadro de búsqueda
                SearchBox.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3498DB"));
            }
            else
            {
                // Restaurar el estilo normal
                SearchBox.ClearValue(Border.BorderBrushProperty);
            }
        }

        /// <summary>
        /// Actualiza la visualización de información sobre filtros activos
        /// </summary>
        private void UpdateFilterInfoDisplay()
        {
            if (HasActiveSearchTerms())
            {
                // Construir el texto de información del filtro
                string filterDescription = GetSearchDescription();
                FilterInfoText.Text = filterDescription;

                // Mostrar el borde de información
                FilterInfoBorder.Visibility = Visibility.Visible;

                // Actualizar estadísticas si es necesario
                if (_filteredData.Count != currentData.Count)
                {
                    FilterInfoText.Text += $" | Mostrando {_filteredData.Count} de {currentData.Count} elementos";
                }
            }
            else
            {
                // Ocultar el borde de información si no hay filtros activos
                FilterInfoBorder.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Comprueba si hay algún término de búsqueda activo
        /// </summary>
        /// <returns>True si hay términos de búsqueda activos</returns>
        private bool HasActiveSearchTerms()
        {
            return _searchTerms.Count > 0 || !string.IsNullOrEmpty(_lastSearchText);
        }

        /// <summary>
        /// Muestra una descripción de los términos de búsqueda actuales
        /// </summary>
        /// <returns>Texto descriptivo de los términos de búsqueda</returns>
        private string GetSearchDescription()
        {
            if (!HasActiveSearchTerms())
            {
                return string.Empty;
            }

            List<string> terms = new List<string>(_searchTerms);
            if (!string.IsNullOrEmpty(_lastSearchText))
            {
                terms.Add(_lastSearchText);
            }

            if (terms.Count == 1)
            {
                return $"Filtrando por: {terms[0]}";
            }
            else
            {
                return $"Filtrando por {terms.Count} términos: {string.Join(" OR ", terms)}";
            }
        }

        /// <summary>
        /// Elimina un término específico de la búsqueda
        /// </summary>
        /// <param name="term">El término a eliminar</param>
        private void RemoveSearchTerm(string term)
        {
            if (_searchTerms.Contains(term))
            {
                _searchTerms.Remove(term);
                UpdateSearchTermsDisplay();
                ApplySearchFilter();
            }
        }

        /// <summary>
        /// Busca el ID del padre de un nodo a partir del historial
        /// </summary>
        private string FindParentId(string nodeId)
        {
            foreach (string entry in historial)
            {
                if (entry == nodeId)
                {
                    return entry;
                }
            }
            return string.Empty;
        }

        #endregion
    }
}