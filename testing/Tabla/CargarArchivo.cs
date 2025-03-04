using Bc3_WPF.backend.Services;
using Bc3_WPF.backend.Modelos;

using System.Text.Json;

namespace testing.Tabla
{
    public class CargarArchivo
    {
        private string DataDir = "../../../Data";

        [Fact]
        public void loadFromBC3()
        {
            var presupuesto = presupuestoService.loadFromBC3($"{DataDir}/BM_ALMERIA.bc3");

            Assert.IsType<Presupuesto>(presupuesto);
            Assert.NotNull(presupuesto);
        }

        [Fact]
        public void loadFromBC3Bad()
        {
            Assert.Throws<NullReferenceException>(() => presupuestoService.loadFromBC3($"{DataDir}/BM_ALMERIA.json"));
        }

        [Fact]
        public void loadNullBc3()
        {
            Assert.Throws<ArgumentException>(() => presupuestoService.loadFromBC3(""));
        }

        [Fact]
        public void loadFromJson()
        {
            var presupuesto = presupuestoService.loadFromJson($"{DataDir}/BM_ALMERIA.json");

            Assert.IsType<Presupuesto>(presupuesto);
            Assert.NotNull(presupuesto);
        }

        [Fact]
        public void loadFromJsonBad()
        {
            Assert.Throws<JsonException>(() => presupuestoService.loadFromJson($"{DataDir}/BM_ALMERIA.bc3"));
        }

        [Fact]
        public void loadNullJson()
        {
            Assert.Throws<ArgumentException>(() => presupuestoService.loadFromJson(""));
        }
    }
}
