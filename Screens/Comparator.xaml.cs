using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Bc3_WPF.Screens
{
    /// <summary>
    /// Lógica de interacción para UserControl1.xaml
    /// </summary>
    public partial class Comparator : System.Windows.Controls.UserControl
    {
        #region PROPERTIES
        private Dictionary<char, KeyValuePair<int, int>?> pages = new Dictionary<char, KeyValuePair<int, int>?>();
        private Dictionary<char, List<Presupuesto>> currentData = new Dictionary<char, List<Presupuesto>>();
        private Dictionary<char, List<Presupuesto>> showing = new Dictionary<char, List<Presupuesto>>();

        private string[] FileNames = [];
        private List<Presupuesto?> presupuestos = [];
        
        private Dictionary<char, List<KeyValuePair<string, List<Presupuesto>>>> historial =
            new Dictionary<char, List<KeyValuePair<string, List<Presupuesto>>>>
            {
                { "1"[^1], [] },
                { "2"[^1], [] },
            };
        private int RowsPerPage = 5;

        public Comparator()
        {
            InitializeComponent();
        }

        #endregion

        #region LOAD
        private void SelectFiles(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "All Supported Files (*.bc3;*.json)|*.bc3;*.json"
            };

            if (ofd.ShowDialog() == true && ofd.FileNames.Length == 2)
            {
                string path1 = ofd.FileNames[0];
                string path2 = ofd.FileNames[1];

                FileNames = ofd.SafeFileNames;

                showData(path1, path2);
            }
        }

        private void showData(string path1, string path2)
        {
            Presupuesto? pr1 = loadPresupuesto(path1);
            Presupuesto? pr2 = loadPresupuesto(path2);

            presupuestos.Add(pr1);
            presupuestos.Add(pr2);

            char Id1 = "1".ToCharArray()[0];
            char Id2 = "2".ToCharArray()[0];

            if (pr1?.hijos != null)
                currentData[Id1] = pr1.hijos;

            if (pr2?.hijos != null)
                currentData[Id2] = pr2.hijos;

            makePagination(Id1);
            makePagination(Id2);

            Tabla1.ItemsSource = showing[Id1];
            Tabla2.ItemsSource = showing[Id2];
            TitleTabla1.Text = pr1?.name;
            TitleTabla2.Text = pr2?.name;

            Tabla1.Visibility = Visibility.Visible;
            Tabla2.Visibility = Visibility.Visible;

            FileButton.Visibility = Visibility.Hidden;
        }

        private Presupuesto? loadPresupuesto(string path)
        {
            Presupuesto res = null;
            if (path.EndsWith(".json"))
            {
               res = presupuestoService.loadFromJson(path); 
            }
            else if (path.EndsWith(".bc3"))
            {
                res = presupuestoService.loadFromBC3(path);
            }
            else
            {
                System.Windows.MessageBox.Show("You need to select 2 files", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return res;
        }
        #endregion

        #region NAVEGACION TABLA
        private void handleHijos(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            char id = button.Name[^1];

            Presupuesto Item = (Presupuesto)button.DataContext;
            List<Presupuesto> data = [];

            if (Item.hijos != null)
            {
                data.AddRange(Item.hijos);
                data.Sort((a, i) => a.Id.CompareTo(i.Id));

                historial[id].Add(new KeyValuePair<string, List<Presupuesto>>(Item.Id, currentData[id]));
                currentData[id] = data;

                makePagination(id);

                if (id == "1".ToCharArray()[0])
                    Tabla1.ItemsSource = showing[id];
                else
                    Tabla2.ItemsSource = showing[id];

                if (historial[id].Count > 0)
                {
                    if (id == "1".ToCharArray()[0])
                        BackButton1.Visibility = Visibility.Visible;
                    else
                        BackButton2.Visibility = Visibility.Visible;
                }
            }
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            char id = button.Name[^1];

            List<KeyValuePair<string, List<Presupuesto>>> hist = historial[id];

            if (hist.Count > 0)
            {
                currentData[id] = hist[hist.Count - 1].Value;
                hist.Remove(hist[hist.Count - 1]);
                makePagination(id);
                historial[id] = hist;


                if (id == "1".ToCharArray()[0])
                    Tabla1.ItemsSource = showing[id];
                else
                    Tabla2.ItemsSource = showing[id];
            }

            if (historial[id].Count == 0)
            {
                if (id == "1".ToCharArray()[0])
                    BackButton1.Visibility = Visibility.Hidden;
                else
                    BackButton2.Visibility = Visibility.Hidden;
            }
        }

        #endregion

        #region PAGINATION
        private void PreviousPage(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            char id = button.Name[^1];

            int pageNumber = pages[id].Value.Value;
            int numPages = pages[id].Value.Key;

            List<Presupuesto> current = currentData[id];

            if (pageNumber > 1)
            {
                pageNumber = pageNumber - 1;
                int lowerbound = (pageNumber - 1) * RowsPerPage;

                showing[id] = current.Slice(lowerbound, RowsPerPage);

                if(id == "1".ToCharArray()[0])
                {
                    Tabla1.ItemsSource = showing[id];
                    PageNumber1.Text = $"Page {pageNumber} / {numPages}";
                }
                else
                {
                    Tabla2.ItemsSource = showing[id];
                    PageNumber2.Text = $"Page {pageNumber} / {numPages}";
                }

                pages[id] = new KeyValuePair<int, int>(numPages, pageNumber);
            }
        }

        private void NextPage(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            char id = button.Name[^1];

            int pageNumber = pages[id].Value.Value;
            int numPages = pages[id].Value.Key;

            List<Presupuesto> current = currentData[id];

            if (pageNumber != numPages)
            {
                int lowerbound = pageNumber * RowsPerPage;
                pageNumber = pageNumber + 1;
                int upperbound = pageNumber * RowsPerPage;

                if (current.Count < upperbound)
                {
                    int sobrante = current.Count - lowerbound;
                    showing[id] = current.Slice(lowerbound, sobrante);
                }
                else
                {
                    showing[id] = current.Slice(lowerbound, RowsPerPage);
                }

                if (id == "1".ToCharArray()[0])
                {
                    Tabla1.ItemsSource = showing[id];
                    PageNumber1.Text = $"Page {pageNumber} / {numPages}";
                }
                else
                {
                    Tabla2.ItemsSource = showing[id];
                    PageNumber2.Text = $"Page {pageNumber} / {numPages}";
                }

                pages[id] = new KeyValuePair<int, int>(numPages, pageNumber);
            }
        }

        private void makePagination(char id)
        {
            decimal p = (decimal)currentData[id].Count / (decimal)RowsPerPage;
            decimal page = Math.Ceiling(p);
            int pageNumber = 1;

            if (page == 1)
            {
                showing[id] = currentData[id];
                if (id == "1".ToCharArray()[0])
                    Paginator1.Visibility = Visibility.Hidden;
                else
                    Paginator2.Visibility = Visibility.Hidden;
            }
            else
            {
                showing[id] = currentData[id].Slice(0, RowsPerPage);
                if (id == "1".ToCharArray()[0])
                {
                    Paginator1.Visibility = Visibility.Visible;
                    PageNumber1.Text = $"Page {pageNumber} / {page}";
                }
                else
                {
                    Paginator2.Visibility = Visibility.Visible;
                    PageNumber2.Text = $"Page {pageNumber} / {page}";
                }
            }
            pages[id] = new KeyValuePair<int, int>((int)page, pageNumber);
        }

        #endregion

        #region SAVE-FILES
        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            int id = (int)char.GetNumericValue(button.Name[^1]);

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose a folder to save the JSON file";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = System.IO.Path.Combine(dialog.SelectedPath, $"{FileNames[id - 1]}-1.json");

                    try
                    {
                        presupuestoService.saveJson(filePath, presupuestos[id - 1]);
                        System.Windows.MessageBox.Show($"file saved in {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed at saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        #endregion

        #region TODO

        private void handleSplit(object sender, RoutedEventArgs e)
        {

        }
        #endregion
    }
}
