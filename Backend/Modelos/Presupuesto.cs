using System.Diagnostics.CodeAnalysis;
namespace Bc3_WPF.backend.Modelos
{
    public partial class Presupuesto
    {
        public string? Id { get; set; }
        public string? InternalId { get; set; }
        public string? name { get; set; }
        public string? category { get; set; }
        public List<string>? medidores { get; set; }
        public List<Presupuesto>? hijos { get; set; }
        public float? quantity { get; set; }
        public Boolean? outdated { get; set; }
        public List<double>? values { get; set; }
        public List<double>? factores { get; set; }
        public decimal? display { get; set; }
        public decimal? percentage { get; set; }
        // Nueva propiedad para la base de datos seleccionada
        public string? database { get; set; }
        public Presupuesto? Parent { get; set; }

        // Método para establecer las referencias a los padres cuando se carga el árbol
        public static void SetParentReferences(List<Presupuesto> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.hijos != null && node.hijos.Count > 0)
                {
                    foreach (var child in node.hijos)
                    {
                        child.Parent = node;
                    }
                    SetParentReferences(node.hijos);
                }
            }
        }


        public void CalculateValues(string medidor)
        {
            if (hijos == null || !hijos.Any())
            {
                SetValues(medidor);
                return;
            }

            // Calcular los valores de todos los hijos primero
            foreach (var hijo in hijos)
            {
                hijo.CalculateValues(medidor);
            }

            // El display de este presupuesto es la suma del display de sus hijos
            display = hijos.Sum(h => h.display ?? 0);
        }

        private void SetValues(string medidor)
        {
            if (medidores != null && medidores.Contains(medidor))
            {
                int index = medidores.IndexOf(medidor);
                decimal Quantity = (decimal)(quantity ?? 0);
                decimal value = (decimal)values[index];
                display = value * Quantity * (decimal)factores[index];
                if (Parent != null && Parent.display != 0)
                    percentage = (display / Parent.display) * 100;
                else
                    percentage = 0;
            }
            else
            {
                display = 0;
                percentage = 0;
            }
        }

        public void NullValues()
        {
            hijos?.ForEach(h => h.NullValues());
            display = 0;
            database = null; // Reiniciar también la base de datos seleccionada
        }

        public void NullParents()
        {
            hijos?.ForEach(h => h.NullParents());
            Parent = null;
        }

        public static Presupuesto copy(Presupuesto p)
        {
            Presupuesto pr =  new Presupuesto
            {
                Id = p.Id,
                InternalId = p.InternalId,
                name = p.name,
                category = p.category,
                medidores = p.medidores,
                hijos = p.hijos,
                quantity = p.quantity,
                values = p.values,
                display = p.display,
                database = p.database
            };
            pr.NullParents();

            return pr;
        }
    }
}