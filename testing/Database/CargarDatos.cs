using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.Backend.Services;

namespace testing.Database
{
    public class CargarDatos
    {
        [Fact]
        public void LoadData()
        {
            var records = SustainabilityService.getFromDatabase();

            Assert.IsType<List<SustainabilityRecord>>(records);
            Assert.NotEmpty(records);
            Assert.NotNull(records);
            Assert.Equal(895, records.Count);
        }
    }
}
