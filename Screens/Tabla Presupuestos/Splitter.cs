using Bc3_WPF.Backend.Auxiliar;
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;
using Bc3_WPF.Backend.Modelos;

namespace Bc3_WPF.Screens.Tabla_Presupuestos
{
    public partial class TablaDePresupuestos
    {

        /// <summary>
        /// Prepares and shows the split popup for a given item
        /// </summary>
        private void PrepareAndShowSplitPopup(Presupuesto item)
        {
            if (item.InternalId != null && item.InternalId != "")
            {
                string category = item.InternalId.Split("\\")[0].Trim();

                // Filtrar materiales por categoría
                var filteredMaterials = _materials.Where(e => e.internalId.Contains(category)).ToList();

                // Guardar la lista completa en Tag para usar en eventos
                SplitTable.Tag = filteredMaterials;

                // Asignar solo los IDs al ComboBox (mantener tu estructura original)
                IdField.ItemsSource = filteredMaterials.Select(e => e.internalId).ToList();

                SplitTable.ItemsSource = new List<Presupuesto> {
            new() {
                Id = item.Id,
                InternalId = item.InternalId, // Asegúrate de tener esta propiedad
                name = item.name,
                quantity = item.quantity
            }
        };

                SplitPopUp.IsOpen = true;
            }
        }

        // Evento para manejar cambios en el ComboBox
        private void SplitTable_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "InternalId")
            {
                var comboBox = e.EditingElement as System.Windows.Controls.ComboBox;
                if (comboBox?.SelectedItem != null)
                {
                    var availableMaterials = SplitTable.Tag as List<Material>; // Reemplaza con tu tipo
                    var currentItem = e.Row.Item as Presupuesto;

                    if (availableMaterials != null && currentItem != null)
                    {
                        string selectedInternalId = comboBox.SelectedItem.ToString();
                        var selectedMaterial = availableMaterials.FirstOrDefault(m => m.internalId == selectedInternalId);

                        if (selectedMaterial != null)
                        {
                            // Solo actualizar el valor - NO usar Refresh()
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                currentItem.name = selectedMaterial.description;
                                // Si implementas INotifyPropertyChanged, la UI se actualiza automáticamente
                            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        }
                    }
                }
            }
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
                UpdateSplitData(splitData, original);

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
        private void UpdateSplitData(List<Presupuesto> data, Presupuesto og)
        {
            foreach (Presupuesto p in data)
            {
                if (p.medidores == null || p.medidores.Count == 0)
                {
                    p.Id = og.Id;
                    p.medidores = og.medidores;
                    p.values = og.values;
                    p.category = og.category;
                }
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
            List<Presupuesto> duplicate = new List<Presupuesto>(data);
            foreach (Presupuesto p in data)
            {
                duplicate.Remove(p);
                List<Material> m = _materials.FindAll(e => e.internalId == p.InternalId).ToList();
                if (m.Count == 0)
                {
                    p.Id = m[0].id;
                    p.name = m[0].description;
                    p.category = m[0].category;
                    p.factores = [];
                    p.values = [];
                    p.medidores = [];

                    foreach (Material ma in m)
                    {
                        p.factores.Add(ma.factor);
                        p.values.Add(ma.value);
                        p.medidores.Add(ma.indicator);
                    }
                }

                duplicate.Add(p);

            }

            return duplicate.Where(p => !string.IsNullOrEmpty(p.InternalId) && p.quantity.HasValue && p.quantity.Value != 0).ToList();
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
    }
}
