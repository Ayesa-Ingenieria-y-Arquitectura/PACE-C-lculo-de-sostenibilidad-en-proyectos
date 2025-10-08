using Bc3_WPF.backend.Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Bc3_WPF.Screens.Tabla_Presupuestos
{
    public partial class TablaDePresupuestos
    {
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
    }
}
