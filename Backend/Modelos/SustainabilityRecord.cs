namespace Bc3_WPF.Backend.Modelos
{
    public class SustainabilityRecord
    {
        public required string ExternalId { get; set; }
        public required string InternalId { get; set; }
        public required string Category { get; set; }
        public required string Indicator { get; set; }
        public required double Value { get; set; }
        public required double Factor { get; set; }
        public required string Source { get; set; } // Añadido el campo Database
    }
}
