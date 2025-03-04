namespace Bc3_WPF.backend.Modelos
{
    public class Presupuesto
    {
        public required string Id { get; set; }
        public required string name { get; set; }
        public DateOnly? fecha { get; set; }
        public List<Presupuesto>? hijos { get; set; }
        public float? quantity { get; set; }
    }
}
