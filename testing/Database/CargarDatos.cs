using Bc3_WPF.Backend.Services;

namespace testing.Database
{
    public class CargarDatos
    {
        public static TheoryData<string> Ayesa => new TheoryData<string>
        {
            "Ayesa-Enviroment"
        };

        public static TheoryData<string> Endesa => new TheoryData<string>
        {
            "Endesa-Enviroment"
        };

        [Theory]
        [MemberData(nameof(Ayesa))]
        [MemberData(nameof(Endesa))]
        public void loadData(string data)
        {
            var res = DatabaseService.LoadData(data);

            Assert.IsType<Dictionary<string, KeyValuePair<decimal, decimal>>>(res);
            Assert.NotNull(res);
            Assert.True(res.Keys.Count() == 895);
        }
    }
}
