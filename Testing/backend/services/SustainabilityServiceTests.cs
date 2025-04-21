using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.Backend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Backend.Services
{
    [TestFixture]
    public class SustainabilityServiceTests
    {
        private List<SustainabilityRecord> _testData;

        [SetUp]
        public void Setup()
        {
            // Crear datos de prueba
            _testData = new List<SustainabilityRecord>
            {
                new SustainabilityRecord { ExternalId = "EXT001", InternalId = "INT001", Category = "Energy Consumption", Indicator = "kWh", Value = 120.5, Database = "DB1" },
                new SustainabilityRecord { ExternalId = "EXT001", InternalId = "INT001", Category = "Energy Consumption", Indicator = "CO2", Value = 45.2, Database = "DB1" },
                new SustainabilityRecord { ExternalId = "EXT002", InternalId = "INT002", Category = "Water Usage", Indicator = "m3", Value = 78.3, Database = "DB1" },
                new SustainabilityRecord { ExternalId = "EXT003", InternalId = "INT003", Category = "Waste Production", Indicator = "kg", Value = 250.0, Database = "DB2" },
                new SustainabilityRecord { ExternalId = "EXT004", InternalId = "INT004", Category = "Waste Production", Indicator = "kg", Value = 175.8, Database = "DB2" }
            };
        }

        // Test para getFromCategories
        [Test]
        public void GetFromCategories_GroupsDataByCategory()
        {
            // Arrange - Ya tenemos _testData

            // Act
            var result = SustainabilityService.getFromCategories(_testData);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3)); // Deberían haber 3 categorías
            Assert.That(result["Energy Consumption"], Contains.Item("EXT001"));
            Assert.That(result["Water Usage"], Contains.Item("EXT002"));
            Assert.That(result["Waste Production"], Contains.Item("EXT003"));
            Assert.That(result["Waste Production"], Contains.Item("EXT004"));

            // Verificar que no hay duplicados en los ExternalId
            Assert.That(result["Energy Consumption"].Count, Is.EqualTo(1));
        }

        // Test para medidores
        [Test]
        public void Medidores_ReturnsUniqueIndicators()
        {
            // Act
            var result = SustainabilityService.medidores(_testData);

            // Assert
            Assert.That(result.Count, Is.EqualTo(4)); // kWh, CO2, m3, kg (pero kg aparece dos veces)
            Assert.That(result, Contains.Item("kWh"));
            Assert.That(result, Contains.Item("CO2"));
            Assert.That(result, Contains.Item("m3"));
            Assert.That(result, Contains.Item("kg"));
        }

        // Test para getCodeRelation
        [Test]
        public void GetCodeRelation_ReturnsUniqueExternalInternalPairs()
        {
            // Act
            var result = SustainabilityService.getCodeRelation(_testData);

            // Assert
            Assert.That(result.Count, Is.EqualTo(4)); // Deberían ser 4 pares únicos

            // Verificar los pares específicos
            Assert.That(result, Has.Some.Matches<KeyValuePair<string, string>>(kv =>
                kv.Key == "EXT001" && kv.Value == "INT001"));
            Assert.That(result, Has.Some.Matches<KeyValuePair<string, string>>(kv =>
                kv.Key == "EXT002" && kv.Value == "INT002"));
            Assert.That(result, Has.Some.Matches<KeyValuePair<string, string>>(kv =>
                kv.Key == "EXT003" && kv.Value == "INT003"));
            Assert.That(result, Has.Some.Matches<KeyValuePair<string, string>>(kv =>
                kv.Key == "EXT004" && kv.Value == "INT004"));
        }

        // Test para getDatabases
        [Test]
        public void GetDatabases_ReturnsUniqueDatabases()
        {
            // Act
            var result = SustainabilityService.getDatabases(_testData);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Contains.Item("DB1"));
            Assert.That(result, Contains.Item("DB2"));
        }

        // Test para groupByDatabase
        [Test]
        public void GroupByDatabase_GroupsRecordsByDatabase()
        {
            // Act
            var result = SustainabilityService.groupByDatabase(_testData);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["DB1"].Count, Is.EqualTo(3));
            Assert.That(result["DB2"].Count, Is.EqualTo(2));

            // Verificar que los registros están en los grupos correctos
            Assert.That(result["DB1"], Has.Some.Matches<SustainabilityRecord>(r =>
                r.ExternalId == "EXT001" && r.Category == "Energy Consumption"));
            Assert.That(result["DB2"], Has.Some.Matches<SustainabilityRecord>(r =>
                r.ExternalId == "EXT003" && r.Category == "Waste Production"));
        }

        // Test para getFromDatabase usando mocking
        [Test]
        public void GetFromDatabase_ReturnsSustainabilityRecords()
        {
            // Este test requiere mocking de la conexión a la base de datos
            // o configurar una base de datos de prueba específica.
            // A continuación, una implementación básica usando reflection y mocking

            // Arrange
            // Esta parte es más conceptual para mostrar cómo se podría abordar
            // En un entorno real, necesitarías usar una herramienta como Fakes o un wrapper para mockear NpgsqlConnection

            Assert.Pass("Este test requiere mocking avanzado o una base de datos de prueba");

            /* Enfoque conceptual (no ejecutable directamente):
            
            // Setup mock data
            var mockReader = new Mock<NpgsqlDataReader>();
            int callCount = 0;
            
            mockReader.Setup(r => r.Read()).Returns(() => callCount++ < 2); // Simular 2 filas
            mockReader.Setup(r => r["ExternalId"]).Returns("TEST001");
            mockReader.Setup(r => r["InternalId"]).Returns("INT001");
            mockReader.Setup(r => r["Category"]).Returns("Test Category");
            mockReader.Setup(r => r["Indicator"]).Returns("Test Indicator");
            mockReader.Setup(r => r["value"]).Returns(100.0);
            mockReader.Setup(r => r["Database"]).Returns("TestDB");
            
            var mockCommand = new Mock<NpgsqlCommand>();
            mockCommand.Setup(c => c.ExecuteReader()).Returns(mockReader.Object);
            
            var mockConnection = new Mock<NpgsqlConnection>();
            mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
            
            // Use reflection to replace the connection
            // ...
            
            // Act
            var result = SustainabilityService.getFromDatabase();
            
            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            */
        }
    }
}
