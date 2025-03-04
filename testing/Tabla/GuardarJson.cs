
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Services;
using System.Numerics;
using Xunit;

namespace testing.Tabla
{
    public class GuardarJson
    {
        private string DataDir = "../../../Data";
        public static Presupuesto pc = presupuestoService.loadFromBC3($"../../../Data/BM_ALMERIA.bc3");

        public static TheoryData<Presupuesto> simple => new TheoryData<Presupuesto>
        {
            new Presupuesto { Id = "simple", name = "PresupuestoPrueba" }
        };

        public static TheoryData<Presupuesto> complejo => new TheoryData<Presupuesto>
        {
            pc
        };

        [Theory]
        [MemberData(nameof(simple))]
        [MemberData(nameof(complejo))]
        public void savePresupuesto(Presupuesto p)
        {
            string path = $"{DataDir}/prueba.json";
            presupuestoService.saveJson(path, p);

            Assert.True(File.Exists(path));

            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        [Fact]
        public void saveNull()
        {
            string path = $"{DataDir}/prueba.json";

            Assert.Throws<NullReferenceException>(() => presupuestoService.saveJson(path, null));
        }

        [Fact]
        public void nulPath()
        {
            Assert.Throws<ArgumentException>(() => presupuestoService.saveJson("", pc));
        }
    }
}
