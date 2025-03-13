using System.Diagnostics.CodeAnalysis;

namespace Bc3_WPF.backend.Modelos
{
    public class Presupuesto
    {
        public required string Id { get; set; }
        public string? InternalId { get; set; }
        public required string name { get; set; }
        public string? category { get; set; }
        public List<string>? medidores { get; set; }
        public List<Presupuesto>? hijos { get; set; }
        public float? quantity { get; set; }
        public Boolean? outdated { get; set; }
        public List<double>? values { get; set; }
        public double? display {  get; set; }

        public void CalculateValues(string medidor)
        {
            if (hijos == null || !hijos.Any())
            {
                SetValues(medidor);
                return;
            }

            double acum = 0;

            foreach (var hijo in hijos)
            {
                hijo.CalculateValues(medidor);
                acum += hijo.display ?? 0;
            }

            display = acum;
        }

        private void SetValues(string medidor)
        {
            if (medidores != null && medidores.Contains(medidor))
            {
                int index = medidores.IndexOf(medidor);
                float Quantity = quantity ?? 0;
                display = values[index] * Quantity;
            }
            else
            {
                display = 0;
            }
        }

        public void NullValues()
        {
            hijos?.ForEach(h => h.NullValues());

            display = 0;
        }
    }
}
