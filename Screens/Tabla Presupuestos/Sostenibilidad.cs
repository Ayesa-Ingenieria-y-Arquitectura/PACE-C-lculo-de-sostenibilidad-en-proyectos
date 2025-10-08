using Bc3_WPF.backend.Modelos;
using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.Backend.Services;
using System.Windows.Controls;
using Visibility = System.Windows.Visibility;

namespace Bc3_WPF.Screens.Tabla_Presupuestos
{
    public partial class TablaDePresupuestos
    {
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
                med = selectedContent;
                TablaMedidor.Header = selectedContent.Split("-")[1].Split("/")[0];
                TablaMedidor.Visibility = Visibility.Visible;
                TablaMedidorPercentage.Visibility = Visibility.Visible;

                if (dbSelected != "" && dbSelected != "N/A")
                {
                    UpdateValuesWithDatabaseAndMedidor();
                }
                else
                {
                    presupuesto.NullValues();
                    presupuesto.CalculateValues(selectedContent);
                }

                // NUEVO: Calcular porcentajes después de calcular valores
                CalculatePercentages(presupuesto);
            }
            else
            {
                presupuesto.NullValues();
                ResetPercentages(presupuesto); // NUEVO: Limpiar porcentajes
                med = "";
                TablaMedidor.Visibility = Visibility.Hidden;
                TablaMedidorPercentage.Visibility = Visibility.Hidden;
            }

            RefreshDataAndUI();
            ApplySearchFilter();
        }

        /// <summary>
        /// Actualiza los valores usando tanto la base de datos como el medidor seleccionados
        /// </summary>
        private void UpdateValuesWithDatabaseAndMedidor()
        {
            presupuesto.NullValues();
            var sustainabilityRecords = SustainabilityService.getFromDatabase();
            var filteredRecords = sustainabilityRecords
                .Where(sr => sr.Indicator == med)
                .ToList();
            var codeRelations = SustainabilityService.getCodeRelation(sustainabilityRecords);

            UpdateLeafNodeValues(presupuesto, filteredRecords, codeRelations);
            PropagateValuesUpward(presupuesto);

            // NUEVO: Calcular porcentajes después de propagar valores
            CalculatePercentages(presupuesto);
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
                        if (record.Value == 0)
                        {
                            node.display = 0;
                        }
                        else
                        {
                            node.display = (decimal)record.Value * (decimal)record.Factor * (decimal)(node.quantity ?? 0);
                        }

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
                QuantityTitle.Text = "Total " + med.Split("-")[1].Split("/")[0];
            }
        }
        
        /// <summary>
        /// Calcula el porcentaje del display de cada nodo con respecto a su padre
        /// </summary>
        private void CalculatePercentages(Presupuesto node)
        {
            // El nodo raíz tiene 100% por definición
            if (node.Parent == null)
            {
                node.percentage = 100m;
            }

            // Si el nodo tiene hijos, calcular sus porcentajes
            if (node.hijos != null && node.hijos.Count > 0)
            {
                decimal parentDisplay = node.display ?? 0;

                foreach (var child in node.hijos)
                {
                    // Calcular el porcentaje del hijo con respecto al padre
                    if (parentDisplay != 0)
                    {
                        decimal percentage = (((child.display ?? 0) / parentDisplay) * 100m);
                        child.percentage = Math.Round(percentage, 2);
                    }
                    else
                    {
                        child.percentage = 0m;
                    }

                    // Llamada recursiva para los descendientes
                    CalculatePercentages(child);
                }
            }
        }

        /// <summary>
        /// Limpia los porcentajes de todos los nodos
        /// </summary>
        private void ResetPercentages(Presupuesto node)
        {
            node.percentage = null;

            if (node.hijos != null && node.hijos.Count > 0)
            {
                foreach (var child in node.hijos)
                {
                    ResetPercentages(child);
                }
            }
        }
    }
}
