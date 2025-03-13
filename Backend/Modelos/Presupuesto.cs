namespace Bc3_WPF.backend.Modelos
{
    public class Presupuesto
    {
        public required string Id { get; set; }
        public required string name { get; set; }
        public DateOnly? fecha { get; set; }
        public List<Presupuesto>? hijos { get; set; }
        public float? quantity { get; set; }
        public Boolean? outdated { get; set; }
        public decimal? Agua { get; set; }
        public decimal? Carbono { get; set; }

        public void CalculateValues(Dictionary<string, KeyValuePair<decimal, decimal>> DBdata)
        {
            if (hijos == null || !hijos.Any())
            {
                SetAguaAndCarbono(DBdata[Id], quantity ?? 0);
                return;
            }

            decimal totalAgua = 0;
            decimal totalCarbono = 0;

            foreach (var hijo in hijos)
            {
                hijo.CalculateValues(DBdata);
                totalAgua += hijo.Agua ?? 0;
                totalCarbono += hijo.Carbono ?? 0;
            }

            SetAguaAndCarbono(new KeyValuePair<decimal, decimal>(totalAgua, totalCarbono), quantity ?? 0);
        }

        private void SetAguaAndCarbono(KeyValuePair<decimal, decimal> values, float val)
        {
            var a = values.Key * (decimal)val;
            var c = values.Value * (decimal)val;

            Agua = Math.Round(a, 2);
            Carbono = Math.Round(c, 2);
        }

        public void NullValues()
        {
            hijos?.ForEach(h => h.NullValues());

            Agua = null;
            Carbono = null;
        }
    }
}
