using Bc3_WPF.Backend.Services;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Bc3_WPF.Screens
{
    /// <summary>
    /// Lógica de interacción para UserControl1.xaml
    /// </summary>
    public partial class Databases : System.Windows.Controls.UserControl
    {
        public Databases()
        {
            InitializeComponent();
        }

        private void ExportDatabase_Click(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new() { Description = "Chose a folder to save the Excel" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = Path.Combine(dialog.SelectedPath, $"Pace-Database.xlsx");
                try
                {
                    ExportData.ExportDB(filePath);
                    System.Windows.MessageBox.Show($"File saved as {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportDatabase_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new()
            {
                Title = "Select the database Excel",
                Filter = "Excel Files (*.xlsx; *.xls; *.xlsm)|*.xlsx;*.xls;*.xlsm"
            };

            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                try
                {
                    LoadingProgressBar.Visibility = Visibility.Visible;
                    LoadingLabel.Content = "Importando datos...";

                    // Ejecutar en segundo plano
                    try
                    {
                        await Task.Run(() => ImportData.ImportDB(filePath));
                        System.Windows.MessageBox.Show($"Database Succesfully modified", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Close the excel before uploading it", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    }

                    LoadingProgressBar.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
