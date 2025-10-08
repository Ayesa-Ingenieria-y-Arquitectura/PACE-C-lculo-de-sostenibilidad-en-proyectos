using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using System.Windows.Controls;
using System.Windows;
using System.IO;

namespace Bc3_WPF.Screens.Tabla_Presupuestos
{
    public partial class TablaDePresupuestos
    {

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
                try
                {
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
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("El archivo seleccionado no tiene el formato esperado", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

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

    }
}
