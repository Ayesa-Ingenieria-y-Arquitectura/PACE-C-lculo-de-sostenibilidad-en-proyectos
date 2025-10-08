using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
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
        /// Updates the measurement displays
        /// </summary>
        private void getMedidores()
        {
            // Sample values - replace with your actual calculation
            var carbono = 0; // Replace with actual carbon calculation
            var agua = 0;    // Replace with actual water calculation


            Quantity.Text = carbono.ToString();
            Concepts.Text = presupuestoService.toArray(presupuesto).Select(e => e.category).ToHashSet().Count.ToString();

            SetMedidorVisibility(true);
        }

        /// <summary>
        /// Calculates quantity and concepts recursively
        /// </summary>
        private (float TotalQuantity, int ConceptCount, HashSet<string> ids) CalculateQuantityAndConcepts(
            List<Presupuesto>? presupuestos,
            HashSet<string> uniqueConcepts)
        {
            if (presupuestos == null)
                return (0, 0, new HashSet<string>());

            float totalQuantity = 0;
            foreach (var p in presupuestos)
            {
                uniqueConcepts.Add(p.Id);
                totalQuantity += p.quantity ?? 0;
                var subResult = CalculateQuantityAndConcepts(p.hijos, uniqueConcepts);
                totalQuantity += subResult.TotalQuantity;
            }

            return (totalQuantity, uniqueConcepts.Count, uniqueConcepts);
        }

        /// <summary>
        /// Sets visibility of measurement UI elements
        /// </summary>
        private void SetMedidorVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Hidden;

            Concepts.Visibility = visibility;
            ConceptTitle.Visibility = visibility;
            Quantity.Visibility = visibility;
            QuantityTitle.Visibility = visibility;
        }
    }
}
