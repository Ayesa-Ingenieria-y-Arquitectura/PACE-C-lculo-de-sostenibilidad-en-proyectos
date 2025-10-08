using Bc3_WPF.backend.Modelos;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Button = System.Windows.Controls.Button;
using System.Windows.Input;

namespace Bc3_WPF.Screens.Tabla_Presupuestos
{
    public partial class TablaDePresupuestos
    {

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
                        item.InternalId != null && allTerms.Any(term => item.InternalId.ToLower().Contains(term)))) ||                    // O tiene hijos y alguno de sus descendientes coincide con algún término
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

    }
}
