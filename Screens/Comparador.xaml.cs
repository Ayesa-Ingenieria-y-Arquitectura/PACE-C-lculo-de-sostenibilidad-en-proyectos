using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.Backend.Services;
using Bc3_WPF.Backend.Modelos;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using Bc3_WPF.backend.Services;
using LiveChartsCore.SkiaSharpView.VisualElements;
using UserControl = System.Windows.Controls.UserControl;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Bc3_WPF.Screens
{
    public partial class Comparador : UserControl
    {
        #region Private Fields
        private List<FileData> loadedFiles = new List<FileData>();
        private List<string> availableMetrics = new List<string>();
        private List<string> availableDatabases = new List<string>();
        private string selectedMetric = "";
        private string selectedDatabase = "";
        // Servicio para cargar presupuestos
        private readonly presupuestoService _presupuestoService = new presupuestoService();
        // Datos de sostenibilidad
        private List<SustainabilityRecord> sustainabilityData;
        // Relaciones entre códigos
        private List<KeyValuePair<string, string>> codeRelations;
        // Variables para el gráfico
        private ObservableCollection<ISeries> Series { get; set; }
        private ObservableCollection<Axis> XAxes { get; set; }
        private ObservableCollection<Axis> YAxes { get; set; }
        private int topCategories = 10;
        private string chartType = "Barras";
        private string compareBy = "Categorías";
        private List<ComparisonData> comparisonChartData;
        #endregion

        public Comparador()
        {
            InitializeComponent();

            // Inicializar componentes del gráfico
            Series = new ObservableCollection<ISeries>();
            XAxes = new ObservableCollection<Axis>();
            YAxes = new ObservableCollection<Axis>();

            chartComparison.Series = Series;
            chartComparison.XAxes = XAxes;
            chartComparison.YAxes = YAxes;

            // Cargar datos de sostenibilidad al inicio
            LoadSustainabilityData();

            // Inicialmente las pestañas de comparación están deshabilitadas
            tabViews.Items.Remove(tabComparison);
            tabViews.Items.Remove(tabChartComparison);
        }

        private void LoadSustainabilityData()
        {
            try
            {
                // Obtener datos de sostenibilidad de la base de datos
                sustainabilityData = SustainabilityService.getFromDatabase();

                // Obtener relaciones entre códigos
                codeRelations = SustainabilityService.getCodeRelation(sustainabilityData);

                // Cargar métricas disponibles
                var metrics = SustainabilityService.medidores(sustainabilityData).ToList();
                metrics.Insert(0, "N/A"); // Añadir opción "N/A"
                availableMetrics = metrics;
                cmbMetrica.ItemsSource = availableMetrics;

                txtStatus.Text = $"Datos de sostenibilidad cargados: {sustainabilityData.Count} registros";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos de sostenibilidad: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error al cargar datos de sostenibilidad";

                // Inicializar listas vacías en caso de error
                sustainabilityData = new List<SustainabilityRecord>();
                codeRelations = new List<KeyValuePair<string, string>>();
                availableMetrics = new List<string> { "N/A" };
                availableDatabases = new List<string> { "N/A" };
                cmbMetrica.ItemsSource = availableMetrics;
                cmbMetrica.SelectedIndex = 0;
            }
        }

        #region Classes

        // Clase para almacenar información de un archivo cargado
        private class FileData
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public Presupuesto Presupuesto { get; set; }
            public Dictionary<string, CategoryData> CategoriesByMetric { get; set; } = new Dictionary<string, CategoryData>();

            // Método para calcular totales por métrica y categoría
            public void CalculateMetrics(string metric, string database, List<SustainabilityRecord> sustainabilityRecords)
            {
                if (Presupuesto == null || string.IsNullOrEmpty(metric) || metric == "N/A")
                    return;

                // Primero limpiar valores previos
                Presupuesto.NullValues();

                // Si hay base de datos seleccionada, usar ese método
                if (!string.IsNullOrEmpty(database) && database != "N/A")
                {
                    CalculateWithDatabase(metric, database, sustainabilityRecords);
                }
                else
                {
                    // Si no, usar el método estándar
                    Presupuesto.CalculateValues(metric);
                }

                // Ahora clasificar por categorías
                CategorizeData(metric);
            }

            // Método para calcular valores usando una base de datos específica
            private void CalculateWithDatabase(string metric, string database, List<SustainabilityRecord> records)
            {
                if (records == null || !records.Any())
                    return;

                // Filtrar registros por base de datos y métrica
                var filteredRecords = records
                    .Where(sr => sr.Indicator == metric)
                    .ToList();

                if (!filteredRecords.Any())
                    return;

                // Obtener relaciones de código
                var codeRelations = SustainabilityService.getCodeRelation(filteredRecords);

                // Actualizar valores en nodos hoja
                UpdateLeafNodeValues(Presupuesto, filteredRecords, codeRelations, database);

                // Propagar valores hacia arriba
                PropagateValuesUpward(Presupuesto);
            }

            // Actualiza valores en nodos hoja según base de datos
            private void UpdateLeafNodeValues(Presupuesto node, List<SustainabilityRecord> records,
                List<KeyValuePair<string, string>> codeRelations, string database)
            {
                // Si es nodo hoja
                if (node.hijos == null || !node.hijos.Any())
                {
                    // Buscar registros para este ID
                    var matchingRecords = records.Where(r => r.ExternalId == node.Id).ToList();

                    if (matchingRecords.Any())
                    {
                        // Tomar el primer registro como valor de referencia
                        var record = matchingRecords.First();

                        // Asignar valor y base de datos
                        node.display = (decimal)(record.Value * (node.quantity ?? 0));
                        node.database = database;
                    }
                }
                else
                {
                    // Procesar hijos recursivamente
                    foreach (var child in node.hijos)
                    {
                        UpdateLeafNodeValues(child, records, codeRelations, database);
                    }
                }
            }

            // Propaga valores hacia arriba sumando valores de hijos
            private void PropagateValuesUpward(Presupuesto node)
            {
                if (node.hijos == null || !node.hijos.Any())
                {
                    return;
                }

                // Procesar hijos primero
                foreach (var child in node.hijos)
                {
                    PropagateValuesUpward(child);
                }

                // Sumar valores de hijos
                node.display = node.hijos.Sum(h => h.display ?? 0);

                // Asignar base de datos al nodo padre si algún hijo la tiene
                if (node.hijos.Any(h => !string.IsNullOrEmpty(h.database)))
                {
                    node.database = node.hijos.First(h => !string.IsNullOrEmpty(h.database)).database;
                }
            }

            // Clasifica los datos por categoría para la métrica actual
            private void CategorizeData(string metric)
            {
                // Inicializar diccionario para esta métrica si no existe
                if (!CategoriesByMetric.ContainsKey(metric))
                {
                    CategoriesByMetric[metric] = new CategoryData();
                }

                // Obtener todos los conceptos
                var allConcepts = GetAllConcepts(Presupuesto);

                // Agrupar por categoría
                var categoriesData = allConcepts
                    .Where(c => c.display.HasValue && c.display > 0)
                    .GroupBy(c => c.category ?? "Sin categoría")
                    .Select(g => new CategoryInfo
                    {
                        Category = g.Key,
                        Value = (decimal)(g.Sum(c => c.display ?? 0)),
                        ConceptCount = g.Count(),
                        Database = g.FirstOrDefault(c => !string.IsNullOrEmpty(c.database))?.database ?? "N/A",
                        Concepts = g.ToList()
                    })
                    .ToList();

                // Actualizar datos de categoría
                CategoriesByMetric[metric].Categories = categoriesData;
                CategoriesByMetric[metric].TotalValue = (decimal)(categoriesData.Sum(c => c.Value));

                // Calcular porcentajes
                foreach (var category in categoriesData)
                {
                    category.Percentage = (double)(CategoriesByMetric[metric].TotalValue != 0 ?
                        category.Value / CategoriesByMetric[metric].TotalValue : 0);
                }
            }

            // Obtiene todos los conceptos de un presupuesto de forma recursiva
            private List<Presupuesto> GetAllConcepts(Presupuesto root)
            {
                var result = new List<Presupuesto>();

                if (root == null)
                    return result;

                // Añadir el nodo actual
                result.Add(root);

                // Añadir todos los hijos recursivamente
                if (root.hijos != null)
                {
                    foreach (var child in root.hijos)
                    {
                        result.AddRange(GetAllConcepts(child));
                    }
                }

                return result;
            }
        }

        // Clase para almacenar datos categorizados por métrica
        private class CategoryData
        {
            public List<CategoryInfo> Categories { get; set; } = new List<CategoryInfo>();
            public decimal TotalValue { get; set; }
        }

        // Clase para información de categoría
        private class CategoryInfo
        {
            public string Category { get; set; }
            public decimal Value { get; set; }
            public double Percentage { get; set; }
            public int ConceptCount { get; set; }
            public string Database { get; set; }
            public List<Presupuesto> Concepts { get; set; } = new List<Presupuesto>();
        }

        // Clase para el resumen de métricas por archivo
        private class MetricSummary
        {
            public string FileName { get; set; }
            public string Metric { get; set; }
            public decimal TotalValue { get; set; }
            public string Database { get; set; }
            public int CategoryCount { get; set; }
            public int ConceptCount { get; set; }
        }

        // Clase para mostrar categorías
        private class CategoryView
        {
            public string Category { get; set; }
            public double Value { get; set; }
            public double Percentage { get; set; }
            public int ConceptCount { get; set; }
            public string Database { get; set; }
        }

        // Clase para comparación entre archivos
        private class ComparisonData
        {
            public string Category { get; set; }
            public Dictionary<string, double> ValuesByFile { get; set; } = new Dictionary<string, double>();
            public double TotalValue { get; set; }
            public double Percentage { get; set; }
        }

        #endregion

        #region File Loading

        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar archivos JSON",
                Filter = "Archivos JSON | *.json",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadSelectedFiles(openFileDialog.FileNames);
            }
        }

        private void LoadSelectedFiles(string[] filePaths)
        {
            try
            {
                txtStatus.Text = "Cargando archivos...";
                int newFilesCount = 0;

                foreach (string filePath in filePaths)
                {
                    string fileName = Path.GetFileName(filePath);

                    // Verificar si ya existe
                    if (loadedFiles.Any(f => f.FilePath == filePath))
                    {
                        continue;
                    }

                    // Cargar el presupuesto según el tipo de archivo
                    (Presupuesto, HashSet<string>, Dictionary<string, List<string>>) data;
                    if (filePath.EndsWith(".bc3", StringComparison.OrdinalIgnoreCase))
                    {
                        data = presupuestoService.loadFromBC3(filePath);
                    }
                    else
                    {
                        data = presupuestoService.loadFromJson(filePath);
                    }

                    // Crear objeto FileData para el archivo
                    FileData fileData = new FileData
                    {
                        FilePath = filePath,
                        FileName = fileName,
                        Presupuesto = data.Item1
                    };

                    loadedFiles.Add(fileData);
                    newFilesCount++;
                }

                // Actualizar la información en la interfaz
                UpdateFilesInfo();

                // Actualizar datos con la métrica y base de datos seleccionadas
                if (!string.IsNullOrEmpty(selectedMetric) && selectedMetric != "N/A")
                {
                    UpdateDataWithSelectedMetricAndDB();
                }

                // Actualizar estado
                txtStatus.Text = $"Se cargaron {newFilesCount} archivo(s) nuevo(s). Total: {loadedFiles.Count}";

                // Habilitar comparación si hay más de un archivo
                btnCompareFiles.IsEnabled = loadedFiles.Count > 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar archivos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error al cargar archivos";
            }
        }

        private void UpdateFilesInfo()
        {
            if (loadedFiles.Count == 0)
            {
                txtSelectedFiles.Text = "No hay archivos seleccionados";
                cmbSelectedFile.ItemsSource = null;
                return;
            }

            txtSelectedFiles.Text = string.Join(", ", loadedFiles.Select(f => f.FileName));

            // Actualizar combobox para selección de archivos
            var fileNames = loadedFiles.Select(f => f.FileName).ToList();
            cmbSelectedFile.ItemsSource = fileNames;

            if (fileNames.Any())
            {
                cmbSelectedFile.SelectedIndex = 0;
            }
        }

        #endregion

        #region UI Event Handlers

        private void cmbMetrica_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMetrica.SelectedItem != null)
            {
                selectedMetric = cmbMetrica.SelectedItem.ToString();

                // Si está seleccionada la opción N/A, resetear
                if (selectedMetric == "N/A")
                {
                    selectedMetric = "";
                    ResetViews();
                    return;
                }

                UpdateDataWithSelectedMetricAndDB();
            }
        }

        private void cmbSelectedFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCategoriesView();
        }

        private void btnCompareFiles_Click(object sender, RoutedEventArgs e)
        {
            CompareFiles();
        }

        private void cmbCompareBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCompareBy.SelectedItem != null)
            {
                compareBy = (cmbCompareBy.SelectedItem as ComboBoxItem).Content.ToString();

                // Mostrar/ocultar opciones específicas para categorías
                if (panelCategoryOptions != null)
                {
                    panelCategoryOptions.Visibility = compareBy == "Categorías" ? Visibility.Visible : Visibility.Collapsed;
                }

                // Actualizar el gráfico
                if (compareBy == "Categorías" && comparisonChartData != null)
                {
                    UpdateChart();
                }
                else if (compareBy == "Total por Archivo")
                {
                    CreateTotalByFileChart();
                }
            }
        }

        private void cmbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbChartType.SelectedItem != null)
            {
                chartType = (cmbChartType.SelectedItem as ComboBoxItem).Content.ToString();

                if (compareBy == "Categorías" && comparisonChartData != null)
                {
                    UpdateChart();
                }
                else if (compareBy == "Total por Archivo")
                {
                    CreateTotalByFileChart();
                }
            }
        }

        private void cmbTopCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTopCategories.SelectedItem != null)
            {
                string selected = (cmbTopCategories.SelectedItem as ComboBoxItem).Content.ToString();

                if (selected == "Todas")
                {
                    topCategories = int.MaxValue;
                }
                else
                {
                    int.TryParse(selected, out topCategories);
                }

                UpdateChart();
            }
        }

        private void chkShowLegend_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (compareBy == "Categorías" && comparisonChartData != null)
            {
                UpdateChart();
            }
            else if (compareBy == "Total por Archivo")
            {
                CreateTotalByFileChart();
            }
        }

        private void ResetViews()
        {
            // Limpiar todas las vistas
            dgSummary.ItemsSource = null;
            dgCategories.ItemsSource = null;

            // Remover pestañas de comparación si existen
            if (tabViews.Items.Contains(tabComparison))
            {
                tabViews.Items.Remove(tabComparison);
            }

            if (tabViews.Items.Contains(tabChartComparison))
            {
                tabViews.Items.Remove(tabChartComparison);
            }

            txtStatus.Text = "Seleccione una métrica para ver los datos";
        }

        #endregion

        #region Data Update Methods

        private void UpdateDataWithSelectedMetricAndDB()
        {
            if (string.IsNullOrEmpty(selectedMetric) || selectedMetric == "N/A" || loadedFiles.Count == 0)
            {
                return;
            }

            txtStatus.Text = "Calculando valores...";

            // Calcular métricas para cada archivo
            foreach (var file in loadedFiles)
            {
                file.CalculateMetrics(selectedMetric, selectedDatabase, sustainabilityData);
            }

            // Actualizar vistas
            UpdateSummaryView();
            UpdateCategoriesView();

            txtStatus.Text = "Cálculos completados";
        }

        private void UpdateSummaryView()
        {
            var summaryData = new List<MetricSummary>();

            foreach (var file in loadedFiles)
            {
                if (file.CategoriesByMetric.TryGetValue(selectedMetric, out var categoryData))
                {
                    // Contar conceptos totales
                    int totalConcepts = categoryData.Categories.Sum(c => c.ConceptCount);

                    summaryData.Add(new MetricSummary
                    {
                        FileName = file.FileName,
                        Metric = selectedMetric,
                        TotalValue = categoryData.TotalValue,
                        Database = !string.IsNullOrEmpty(selectedDatabase) ? selectedDatabase : "N/A",
                        CategoryCount = categoryData.Categories.Count,
                        ConceptCount = totalConcepts
                    });
                }
            }

            dgSummary.ItemsSource = summaryData;
        }

        private void UpdateCategoriesView()
        {
            if (cmbSelectedFile.SelectedItem == null || string.IsNullOrEmpty(selectedMetric))
            {
                dgCategories.ItemsSource = null;
                return;
            }

            string selectedFileName = cmbSelectedFile.SelectedItem.ToString();
            var selectedFile = loadedFiles.FirstOrDefault(f => f.FileName == selectedFileName);

            if (selectedFile != null && selectedFile.CategoriesByMetric.TryGetValue(selectedMetric, out var categoryData))
            {
                var categoriesView = categoryData.Categories.Select(c => new CategoryView
                {
                    Category = c.Category,
                    Value = (double)c.Value,
                    Percentage = c.Percentage,
                    ConceptCount = c.ConceptCount,
                    Database = c.Database
                }).ToList();

                dgCategories.ItemsSource = categoriesView;
            }
            else
            {
                dgCategories.ItemsSource = null;
            }
        }

        private void CompareFiles()
        {
            if (loadedFiles.Count < 2 || string.IsNullOrEmpty(selectedMetric))
            {
                MessageBox.Show("Se necesitan al menos dos archivos y una métrica seleccionada para comparar",
                                "Comparación no disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Asegurarse de que las pestañas de comparación estén visibles
            if (!tabViews.Items.Contains(tabComparison))
            {
                tabViews.Items.Add(tabComparison);
            }

            if (!tabViews.Items.Contains(tabChartComparison))
            {
                tabViews.Items.Add(tabChartComparison);
            }

            // Crear estructura para la comparación
            var comparisonData = new List<ComparisonData>();

            // Obtener todas las categorías únicas de todos los archivos
            var allCategories = new HashSet<string>();
            foreach (var file in loadedFiles)
            {
                if (file.CategoriesByMetric.TryGetValue(selectedMetric, out var categoryData))
                {
                    foreach (var category in categoryData.Categories)
                    {
                        allCategories.Add(category.Category);
                    }
                }
            }

            // Crear filas para cada categoría
            foreach (var category in allCategories)
            {
                var row = new ComparisonData { Category = category };

                // Añadir valores por archivo
                foreach (var file in loadedFiles)
                {
                    if (file.CategoriesByMetric.TryGetValue(selectedMetric, out var categoryData))
                    {
                        var categoryInfo = categoryData.Categories.FirstOrDefault(c => c.Category == category);
                        row.ValuesByFile[file.FileName] = (double)(categoryInfo?.Value ?? 0);
                    }
                    else
                    {
                        row.ValuesByFile[file.FileName] = 0;
                    }
                }

                // Calcular el valor total y agregarlo a la colección
                row.TotalValue = row.ValuesByFile.Sum(v => v.Value);
                comparisonData.Add(row);
            }

            // Ordenar por valor total descendente
            comparisonData = comparisonData.OrderByDescending(c => c.TotalValue).ToList();

            // Calcular porcentajes
            double grandTotal = comparisonData.Sum(c => c.TotalValue);
            foreach (var row in comparisonData)
            {
                row.Percentage = grandTotal != 0 ? row.TotalValue / grandTotal : 0;
            }

            // Configurar tabla de comparación con los datos
            ConfigureComparisonTable(comparisonData);

            // Guardar datos para el gráfico
            this.comparisonChartData = comparisonData;

            // Actualizar gráfico dependiendo del tipo de comparación seleccionado
            if (compareBy == "Categorías")
            {
                UpdateChart();
            }
            else
            {
                CreateTotalByFileChart();
            }

            // Mostrar la pestaña de gráfico
            tabViews.SelectedItem = tabChartComparison;

            txtStatus.Text = $"Comparando {loadedFiles.Count} archivos por {(compareBy == "Categorías" ? "categoría" : "total")}";
        }

        private void ConfigureComparisonTable(List<ComparisonData> comparisonData)
        {
            dgComparison.Columns.Clear();

            // Columna de categoría
            dgComparison.Columns.Add(new DataGridTextColumn
            {
                Header = "Categoría",
                Binding = new System.Windows.Data.Binding("Category"),
                Width = 150
            });

            // Columnas dinámicas para cada archivo
            foreach (var file in loadedFiles)
            {
                var binding = new System.Windows.Data.Binding($"ValuesByFile[{file.FileName}]")
                {
                    StringFormat = "N2"
                };

                var column = new DataGridTextColumn
                {
                    Header = file.FileName,
                    Binding = binding,
                    Width = 120
                };

                dgComparison.Columns.Add(column);
            }

            // Columnas de total y porcentaje
            dgComparison.Columns.Add(new DataGridTextColumn
            {
                Header = "Total",
                Binding = new System.Windows.Data.Binding("TotalValue") { StringFormat = "N2" },
                Width = 100
            });

            dgComparison.Columns.Add(new DataGridTextColumn
            {
                Header = "% del Total",
                Binding = new System.Windows.Data.Binding("Percentage") { StringFormat = "P2" },
                Width = 90
            });

            // Asignar datos a la grilla
            dgComparison.ItemsSource = comparisonData;
        }

        #region Chart Methods

        private void UpdateChart()
        {
            // Si estamos comparando por archivo total, usamos otro método
            if (compareBy == "Total por Archivo")
            {
                CreateTotalByFileChart();
                return;
            }

            // El resto del código para el gráfico por categorías
            if (comparisonChartData == null || !comparisonChartData.Any())
                return;

            Series.Clear();
            XAxes.Clear();
            YAxes.Clear();

            // Filtrar para mostrar solo las top categorías
            var filteredData = comparisonChartData
                .OrderByDescending(d => d.TotalValue)
                .Take(topCategories)
                .ToList();

            // Generar series según el tipo de gráfico
            if (chartType == "Barras")
            {
                CreateBarChart(filteredData);
            }
            else if (chartType == "Columnas")
            {
                CreateColumnChart(filteredData);
            }
            else if (chartType == "Apilado")
            {
                CreateStackedChart(filteredData);
            }

            // Configurar ejes
            if (XAxes.Count == 0)
            {
                XAxes.Add(new Axis
                {
                    Name = chartType == "Barras" ? "Valor" : "Categoría",
                    NameTextSize = 14,
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                });
            }

            if (YAxes.Count == 0)
            {
                YAxes.Add(new Axis
                {
                    Name = chartType == "Barras" ? "Categoría" : "Valor",
                    NameTextSize = 14,
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                });
            }

            // Actualizar visibilidad de la leyenda
            chartComparison.LegendPosition = chkShowLegend.IsChecked == true ?
                LiveChartsCore.Measure.LegendPosition.Right :
                LiveChartsCore.Measure.LegendPosition.Hidden;
        }

        private void CreateBarChart(List<ComparisonData> data)
        {
            // En un gráfico de barras, cada archivo es una serie
            foreach (var file in loadedFiles)
            {
                var values = new List<double>();

                // Recoger los valores para cada categoría
                foreach (var categoryData in data)
                {
                    values.Add(categoryData.ValuesByFile.ContainsKey(file.FileName) ?
                               categoryData.ValuesByFile[file.FileName] : 0);
                }

                // Crear serie
                var series = new RowSeries<double>
                {
                    Values = values,
                    Name = file.FileName,
                    Stroke = null,
                    MaxBarWidth = 25,
                };

                Series.Add(series);
            }

            // Configurar etiquetas del eje Y para mostrar las categorías
            var labels = data.Select(d => d.Category).ToArray();
            YAxes.Add(new Axis
            {
                Labels = labels,
                TextSize = 10,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                TicksPaint = new SolidColorPaint(SKColors.Gray, 1),
                NamePaint = new SolidColorPaint(SKColors.Black, 1)
            });
        }

        private void CreateColumnChart(List<ComparisonData> data)
        {
            // En un gráfico de columnas, cada archivo es una serie
            foreach (var file in loadedFiles)
            {
                var values = new List<double>();

                // Recoger los valores para cada categoría
                foreach (var categoryData in data)
                {
                    values.Add(categoryData.ValuesByFile.ContainsKey(file.FileName) ?
                               categoryData.ValuesByFile[file.FileName] : 0);
                }

                // Crear serie
                var series = new ColumnSeries<double>
                {
                    Values = values,
                    Name = file.FileName,
                    Stroke = null,
                    MaxBarWidth = 25,
                };

                Series.Add(series);
            }

            // Configurar etiquetas del eje X para mostrar las categorías
            var labels = data.Select(d => d.Category).ToArray();
            XAxes.Add(new Axis
            {
                Labels = labels,
                TextSize = 10,
                LabelsRotation = 45,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                TicksPaint = new SolidColorPaint(SKColors.Gray, 1),
                NamePaint = new SolidColorPaint(SKColors.Black, 1)
            });
        }

        private void CreateStackedChart(List<ComparisonData> data)
        {
            // En un gráfico apilado, cada archivo es una serie
            foreach (var file in loadedFiles)
            {
                var values = new List<double>();

                // Recoger los valores para cada categoría
                foreach (var categoryData in data)
                {
                    values.Add(categoryData.ValuesByFile.ContainsKey(file.FileName) ?
                               categoryData.ValuesByFile[file.FileName] : 0);
                }

                // Crear serie
                var series = new StackedColumnSeries<double>
                {
                    Values = values,
                    Name = file.FileName,
                    Stroke = null,
                    MaxBarWidth = 40,
                };

                Series.Add(series);
            }

            // Configurar etiquetas del eje X para mostrar las categorías
            var labels = data.Select(d => d.Category).ToArray();
            XAxes.Add(new Axis
            {
                Labels = labels,
                TextSize = 10,
                LabelsRotation = 45,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                TicksPaint = new SolidColorPaint(SKColors.Gray, 1),
                NamePaint = new SolidColorPaint(SKColors.Black, 1)
            });
        }

        private void CreateTotalByFileChart()
        {
            if (loadedFiles == null || loadedFiles.Count == 0 || string.IsNullOrEmpty(selectedMetric))
                return;

            Series.Clear();
            XAxes.Clear();
            YAxes.Clear();

            // Obtener los totales por archivo
            var fileTotals = loadedFiles
                .Where(f => f.CategoriesByMetric.ContainsKey(selectedMetric))
                .Select(f => new
                {
                    FileName = f.FileName,
                    TotalValue = f.CategoriesByMetric[selectedMetric].TotalValue
                })
                .OrderByDescending(f => f.TotalValue)
                .ToList();

            // Crear series según el tipo de gráfico seleccionado
            if (chartType == "Barras")
            {
                // Gráfico de barras horizontales
                var series = new RowSeries<double>
                {
                    Values = fileTotals.Select(f => (double)f.TotalValue).ToArray(),
                    Name = selectedMetric,
                    Stroke = null,
                    MaxBarWidth = 40,
                    Fill = new SolidColorPaint(SKColors.DodgerBlue)
                };

                Series.Add(series);

                // Configurar etiquetas de los archivos en el eje Y
                YAxes.Add(new Axis
                {
                    Labels = fileTotals.Select(f => f.FileName).ToArray(),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                });

                // Configurar eje X (valores)
                XAxes.Add(new Axis
                {
                    Name = "Valor Total",
                    NameTextSize = 14,
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                });
            }
            else if (chartType == "Columnas" || chartType == "Apilado") // Apilado no tiene sentido para un solo valor
            {
                // Gráfico de columnas verticales
                var series = new ColumnSeries<double>
                {
                    Values = fileTotals.Select(f => (double)f.TotalValue).ToArray(),
                    Name = selectedMetric,
                    Stroke = null,
                    MaxBarWidth = 40,
                    Fill = new SolidColorPaint(SKColors.DodgerBlue)
                };

                Series.Add(series);

                // Configurar etiquetas de los archivos en el eje X
                XAxes.Add(new Axis
                {
                    Labels = fileTotals.Select(f => f.FileName).ToArray(),
                    TextSize = 12,
                    LabelsRotation = 45,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                });

                // Configurar eje Y (valores)
                YAxes.Add(new Axis
                {
                    Name = "Valor Total",
                    NameTextSize = 14,
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                });
            }

            // Título del gráfico
            chartComparison.Title = new LabelVisual
            {
                Text = $"Valor Total de {selectedMetric} por Archivo",
                TextSize = 16,
                Padding = new LiveChartsCore.Drawing.Padding(15),
                Paint = new SolidColorPaint(SKColors.Black)
            };

            // Actualizar visibilidad de la leyenda
            chartComparison.LegendPosition = chkShowLegend.IsChecked == true ?
                LiveChartsCore.Measure.LegendPosition.Right :
                LiveChartsCore.Measure.LegendPosition.Hidden;
        }

        #endregion

        #endregion
    }
}
