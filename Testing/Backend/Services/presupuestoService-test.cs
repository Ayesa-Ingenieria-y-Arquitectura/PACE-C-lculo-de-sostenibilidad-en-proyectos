using NUnit.Framework;
using Bc3_WPF.backend.Services;
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.Backend.Modelos;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
using System.Linq;

namespace Bc3_WPF.Tests.Services
{
    [TestFixture]
    public class presupuestoServiceTests
    {
        private string testDirectory;
        private string testJsonFile;
        private string testBC3File;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), "presupuesto_tests");
            Directory.CreateDirectory(testDirectory);
            testJsonFile = Path.Combine(testDirectory, "test_presupuesto.json");
            testBC3File = Path.Combine(testDirectory, "test_presupuesto.bc3");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        #region LoadFromJson Tests

        [Test]
        public void LoadFromJson_ValidJsonFile_ReturnsPresupuestoAndCollections()
        {
            // Arrange
            var testPresupuesto = CreateTestPresupuesto();
            var json = JsonSerializer.Serialize(testPresupuesto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(testJsonFile, json, System.Text.Encoding.GetEncoding("iso-8859-1"));

            // Act
            var result = presupuestoService.loadFromJson(testJsonFile);

            // Assert
            Assert.That(result.Item1, Is.Not.Null);
            Assert.That(result.Item1.Id, Is.EqualTo(testPresupuesto.Id));
            Assert.That(result.Item1.name, Is.EqualTo(testPresupuesto.name));
            Assert.That(result.Item2, Is.Not.Null); // HashSet<string>
            Assert.That(result.Item3, Is.Not.Null); // Dictionary<string, List<string>>
        }

        [Test]
        public void LoadFromJson_FileNotExists_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentFile = Path.Combine(testDirectory, "nonexistent.json");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => presupuestoService.loadFromJson(nonExistentFile));
        }

        [Test]
        public void LoadFromJson_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            File.WriteAllText(testJsonFile, "invalid json content", System.Text.Encoding.GetEncoding("iso-8859-1"));

            // Act & Assert
            Assert.Throws<JsonException>(() => presupuestoService.loadFromJson(testJsonFile));
        }

        #endregion

        #region SaveJson Tests

        [Test]
        public void SaveJson_ValidPresupuesto_CreatesJsonFile()
        {
            // Arrange
            var testPresupuesto = CreateTestPresupuesto();

            // Act
            presupuestoService.saveJson(testJsonFile, testPresupuesto);

            // Assert
            Assert.That(File.Exists(testJsonFile), Is.True);
            var savedJson = File.ReadAllText(testJsonFile);
            var deserializedPresupuesto = JsonSerializer.Deserialize<Presupuesto>(savedJson);
            Assert.That(deserializedPresupuesto.Id, Is.EqualTo(testPresupuesto.Id));
            Assert.That(deserializedPresupuesto.name, Is.EqualTo(testPresupuesto.name));
        }

        [Test]
        public void SaveJson_NullPresupuesto_ThrowsNullReferenceException()
        {
            // Act & Assert
            var ex = Assert.Throws<NullReferenceException>(() => presupuestoService.saveJson(testJsonFile, null));
            Assert.That(ex.Message, Is.EqualTo("No existe un presupuesto a descargar"));
        }

        #endregion

        #region FillPresupuesto Tests

        [Test]
        public void FillPresupuesto_PresupuestoWithHijos_FillsDataCorrectly()
        {
            // Arrange
            var presupuesto = CreateTestPresupuesto();
            var sustainabilityData = CreateTestSustainabilityData();

            // Act
            var result = presupuestoService.fillPresupuesto(presupuesto, sustainabilityData);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.hijos, Is.Not.Null);
            Assert.That(result.hijos.Count, Is.GreaterThan(0));
        }

        [Test]
        public void FillPresupuesto_PresupuestoWithLeafNodes_FillsSustainabilityData()
        {
            // Arrange
            var leafPresupuesto = new Presupuesto
            {
                Id = "LEAF01",
                name = "Leaf Node",
                hijos = new List<Presupuesto>
                {
                    new Presupuesto { Id = "CHILD01", name = "Child 1", hijos = null }
                }
            };
            var sustainabilityData = new List<SustainabilityRecord>
            {
                new SustainabilityRecord
                {
                    ExternalId = "CHILD01",
                    Source = "OnClick",
                    InternalId = "INT01",
                    Category = "Category1",
                    Indicator = "Indicator1",
                    Value = 10.5,
                    Factor = 2.0
                }
            };

            // Act
            var result = presupuestoService.fillPresupuesto(leafPresupuesto, sustainabilityData);

            // Assert
            Assert.That(result.hijos[0].values, Is.Not.Null);
            Assert.That(result.hijos[0].factores, Is.Not.Null);
            Assert.That(result.hijos[0].medidores, Is.Not.Null);
            Assert.That(result.hijos[0].InternalId, Is.EqualTo("INT01"));
            Assert.That(result.hijos[0].category, Is.EqualTo("Category1"));
            Assert.That(result.hijos[0].values.Count, Is.EqualTo(1));
            Assert.That(result.hijos[0].values[0], Is.EqualTo(10.5));
        }

        [Test]
        public void FillPresupuesto_EmptyPresupuesto_ReturnsEmptyPresupuesto()
        {
            // Arrange
            var emptyPresupuesto = new Presupuesto { Id = "EMPTY", name = "Empty", hijos = new List<Presupuesto>() };
            var sustainabilityData = new List<SustainabilityRecord>();

            // Act
            var result = presupuestoService.fillPresupuesto(emptyPresupuesto, sustainabilityData);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.hijos.Count, Is.EqualTo(0));
        }

        #endregion

        #region FindPresupuestoById Tests

        [Test]
        public void FindPresupuestoById_ExistingId_ReturnsCorrectPresupuesto()
        {
            // Arrange
            var testPresupuesto = CreateTestPresupuesto();
            var searchId = "CHILD01";

            // Act
            var result = presupuestoService.FindPresupuestoById(testPresupuesto, searchId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(searchId));
        }

        [Test]
        public void FindPresupuestoById_NonExistingId_ReturnsNull()
        {
            // Arrange
            var testPresupuesto = CreateTestPresupuesto();
            var searchId = "NONEXISTENT";

            // Act
            var result = presupuestoService.FindPresupuestoById(testPresupuesto, searchId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindPresupuestoById_RootId_ReturnsRootPresupuesto()
        {
            // Arrange
            var testPresupuesto = CreateTestPresupuesto();
            var searchId = testPresupuesto.Id;

            // Act
            var result = presupuestoService.FindPresupuestoById(testPresupuesto, searchId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(searchId));
            Assert.That(result.name, Is.EqualTo(testPresupuesto.name));
        }

        [Test]
        public void FindPresupuestoById_PresupuestoWithoutHijos_ReturnsNull()
        {
            // Arrange
            var presupuesto = new Presupuesto { Id = "ROOT", name = "Root", hijos = null };
            var searchId = "CHILD01";

            // Act
            var result = presupuestoService.FindPresupuestoById(presupuesto, searchId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region ToArray Tests

        [Test]
        public void ToArray_PresupuestoWithHijos_ReturnsAllNodesInArray()
        {
            // Arrange
            var testPresupuesto = CreateTestPresupuesto();

            // Act
            var result = presupuestoService.toArray(testPresupuesto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(1)); // Should include root + children
            Assert.That(result[0].Id, Is.EqualTo(testPresupuesto.Id)); // First element should be root
        }

        [Test]
        public void ToArray_PresupuestoWithoutHijos_ReturnsSingleElementArray()
        {
            // Arrange
            var presupuesto = new Presupuesto { Id = "SINGLE", name = "Single", hijos = null };

            // Act
            var result = presupuestoService.toArray(presupuesto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo("SINGLE"));
        }

        [Test]
        public void ToArray_PresupuestoWithEmptyHijos_ReturnsSingleElementArray()
        {
            // Arrange
            var presupuesto = new Presupuesto
            {
                Id = "EMPTY_CHILDREN",
                name = "Empty Children",
                hijos = new List<Presupuesto>()
            };

            // Act
            var result = presupuestoService.toArray(presupuesto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo("EMPTY_CHILDREN"));
        }

        [Test]
        public void ToArray_NestedPresupuesto_ReturnsAllLevelsFlattened()
        {
            // Arrange
            var grandChild = new Presupuesto { Id = "GRANDCHILD01", name = "Grand Child 1", hijos = null };
            var child = new Presupuesto
            {
                Id = "CHILD01",
                name = "Child 1",
                hijos = new List<Presupuesto> { grandChild }
            };
            var root = new Presupuesto
            {
                Id = "ROOT",
                name = "Root",
                hijos = new List<Presupuesto> { child }
            };

            // Act
            var result = presupuestoService.toArray(root);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3)); // Root + Child + GrandChild
            Assert.That(result.Any(p => p.Id == "ROOT"), Is.True);
            Assert.That(result.Any(p => p.Id == "CHILD01"), Is.True);
            Assert.That(result.Any(p => p.Id == "GRANDCHILD01"), Is.True);
        }

        #endregion

        #region Helper Methods

        private Presupuesto CreateTestPresupuesto()
        {
            return new Presupuesto
            {
                Id = "ROOT01",
                name = "Test Root Presupuesto",
                hijos = new List<Presupuesto>
                {
                    new Presupuesto
                    {
                        Id = "CHILD01",
                        name = "Child 1",
                        quantity = 10.0f,
                        hijos = null
                    },
                    new Presupuesto
                    {
                        Id = "CHILD02",
                        name = "Child 2",
                        quantity = 20.0f,
                        hijos = new List<Presupuesto>
                        {
                            new Presupuesto
                            {
                                Id = "GRANDCHILD01",
                                name = "Grand Child 1",
                                quantity = 5.0f,
                                hijos = null
                            }
                        }
                    }
                }
            };
        }

        private List<SustainabilityRecord> CreateTestSustainabilityData()
        {
            return new List<SustainabilityRecord>
            {
                new SustainabilityRecord
                {
                    ExternalId = "CHILD01",
                    Source = "OnClick",
                    InternalId = "INT01",
                    Category = "Category1",
                    Indicator = "CO2",
                    Value = 15.5,
                    Factor = 1.2
                },
                new SustainabilityRecord
                {
                    ExternalId = "CHILD01",
                    Source = "OnClick",
                    InternalId = "INT01",
                    Category = "Category1",
                    Indicator = "Energy",
                    Value = 25.0,
                    Factor = 2.0
                },
                new SustainabilityRecord
                {
                    ExternalId = "GRANDCHILD01",
                    Source = "OnClick",
                    InternalId = "INT02",
                    Category = "Category2",
                    Indicator = "Water",
                    Value = 8.5,
                    Factor = 0.8
                }
            };
        }

        #endregion
    }
}
