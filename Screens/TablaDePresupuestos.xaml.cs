using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using Bc3_WPF.Backend.Auxiliar;
using Bc3_WPF.Screens.Charts;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Win32;
using System.Windows.Forms;

namespace Bc3_WPF.Screens
{
    public partial class TablaDePresupuestos : System.Windows.Controls.UserControl
    {
        private Presupuesto? presupuesto;
        private List<KeyValuePair<string, List<Presupuesto>>> historial = new();
        private List<Presupuesto> currentData = new();
        private List<Presupuesto> showing = new();
        private List<KeyValuePair<string, Presupuesto>> previous = new();
        private int pageNumber = 1;
        private const int RowsPerPage = 5;
        private decimal pages;
        private string fileName;
        private Pie chartData;

        public TablaDePresupuestos() => InitializeComponent();

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
                
                currentData = presupuesto?.hijos ?? new();
                chartData = new Pie();

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
            Chart.Title = chartData.TitleChart;
            PieChart.Title = chartData.TitlePie;
            
            RectangleInfo.Visibility = Visibility.Visible;
            ChartSection.Visibility = Visibility.Visible;
            TableSection.Visibility = Visibility.Visible;
            Paginator.Visibility = Visibility.Visible;
            FileButton.Visibility = Visibility.Hidden;
            Tabla.ItemsSource = showing;
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
            int lowerBound = (pageNumber - 1) * RowsPerPage;
            showing = currentData.Skip(lowerBound).Take(RowsPerPage).ToList();
            Tabla.ItemsSource = showing;
            PageNumber.Text = $"Page {pageNumber} / {pages}";
        }

        private void PreviousPage(object sender, RoutedEventArgs e) { if (pageNumber > 1) ChangePage(-1); }
        private void NextPage(object sender, RoutedEventArgs e) { if (pageNumber < pages) ChangePage(1); }

        private void makePagination()
        {
            pages = Math.Ceiling((decimal)currentData.Count / RowsPerPage);
            pageNumber = 1;
            showing = currentData.Take(RowsPerPage).ToList();
            Next.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            Previous.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            PageNumber.Visibility = pages > 1 ? Visibility.Visible : Visibility.Hidden;
            PageNumber.Text = $"Page {pageNumber} / {pages}";
        }

        private void handleSplit(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Presupuesto item)
            {
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
                splitData.RemoveAt(0);
                
                string parentId = historial.Count == 0 ? presupuesto?.Id ?? "" : historial.Last().Key;
                original.outdated = true;
                splitData.ForEach(p => p.fecha = DateOnly.FromDateTime(DateTime.Now));

                if (original.quantity == splitData.Sum(p => p.quantity))
                {
                    previous.Add(new(parentId, original));
                    presupuesto = Romper.change(presupuesto, historial, splitData, original.Id, true);
                    historial.Clear();
                    currentData = presupuesto?.hijos?.Concat(previous.Where(p => p.Key == presupuesto.Id).Select(p => p.Value)).OrderBy(p => p.Id).ToList() ?? new();
                    
                    makePagination();
                    updateDoughtChart();
                    getMedidores();
                    Tabla.ItemsSource = showing;
                    SplitPopUp.IsOpen = false;
                    SaveButton.Visibility = Visibility.Visible;
                    BackButton.Visibility = Visibility.Hidden;
                }
                else
                {
                    SplitPopUp.IsOpen = false;
                    System.Windows.MessageBox.Show("The sum of the new quantities doesn't match the original quantity", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

        private void updateDoughtChart()
        {
            var validData = currentData.Where(e => e.outdated != true).ToList();
            var doughtData = validData.Select(e => new KeyValuePair<string, float?>(e.Id, e.quantity)).ToList();
            int dataCount = validData.Count;

            Pie.setDoughtData(doughtData, chartData);
            Pie.updateLineChart(dataCount, chartData);
            PieChart.Series = chartData.Series;
            Chart.Series = chartData.Series2;

            SetChartVisibility(true);
        }

        private void SetChartVisibility(bool isVisible)
        {
            PieRectangle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            ChartRectangle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }

        private void getMedidores()
        {
            var processedData = CalculateQuantityAndConcepts(presupuesto?.hijos, new HashSet<string> { presupuesto?.Id ?? string.Empty });

            Quantity.Text = processedData.TotalQuantity.ToString();
            Concepts.Text = processedData.ConceptCount.ToString();
            SetMedidorVisibility(true);
        }

        private (float TotalQuantity, int ConceptCount) CalculateQuantityAndConcepts(List<Presupuesto>? presupuestos, HashSet<string> uniqueConcepts)
        {
            if (presupuestos == null) return (0, 0);

            float totalQuantity = 0;
            foreach (var p in presupuestos)
            {
                uniqueConcepts.Add(p.Id);
                totalQuantity += p.quantity ?? 0;
                var subResult = CalculateQuantityAndConcepts(p.hijos, uniqueConcepts);
                totalQuantity += subResult.TotalQuantity;
            }
            return (totalQuantity, uniqueConcepts.Count);
        }

        private void SetMedidorVisibility(bool isVisible)
        {
            Concepts.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            ConceptTitle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            ConceptRectangle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            Quantity.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            QuantityRectangle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            QuantityTitle.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
