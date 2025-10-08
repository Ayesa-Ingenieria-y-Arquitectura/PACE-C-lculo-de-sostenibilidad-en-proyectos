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
    }
}
