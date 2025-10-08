using NUnit.Framework;
using Bc3_WPF.Backend.Services;
using ClosedXML.Excel;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bc3_WPF.Tests.Services
{
    /// <summary>
    /// Tests de integración para ImportData que prueban métodos individuales
    /// usando reflection para acceder a métodos privados
    /// </summary>
    [TestFixture]
    public class ImportDataIntegrationTests
    {
        private string _testExcelFile;
        private string _testDirectory;

        [SetUp]
        public void SetUp()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "import_data_integration_tests");
            Directory.CreateDirectory(_testDirectory);
            _testExcelFile = Path.Combine(_testDirectory, "test_data.xlsx");
            CreateTestExcelFile();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region Excel File Creation

        private void CreateTestExcelFile()
        {
            using var workbook = new XLWorkbook();

            // Crear hoja CodeRelationship
            var codeRelSheet = workbook.Worksheets.Add("CodeRelationship");
            codeRelSheet.Cell(1, 1).Value = "InternalCode";
            codeRelSheet.Cell(1, 2).Value = "ExternalCode";
            codeRelSheet.Cell(1, 3).Value = "Factor";
            codeRelSheet.Cell(1, 4).Value = "Client";

            // Datos de prueba
            codeRelSheet.Cell(2, 1).Value = "INT001";
            codeRelSheet.Cell(2, 2).Value = "EXT001";
            codeRelSheet.Cell(2, 3).Value = 1.5;
            codeRelSheet.Cell(2, 4).Value = "Client1";

            codeRelSheet.Cell(3, 1).Value = "INT002";
            codeRelSheet.Cell(3, 2).Value = "EXT002";
            codeRelSheet.Cell(3, 3).Value = 2.0;
            codeRelSheet.Cell(3, 4).Value = "Client2";

            codeRelSheet.Cell(4, 1).Value = "INT003";
            codeRelSheet.Cell(4, 2).Value = "EXT003";
            codeRelSheet.Cell(4, 3).Value = ""; // Factor vacío (debería ser 1.0 por defecto)
            codeRelSheet.Cell(4, 4).Value = ""; // Client vacío

            // Crear hoja SustainabilityValues
            var sustainSheet = workbook.Worksheets.Add("SustainabilityValues");
            sustainSheet.Cell(1, 1).Value = "InternalCode";
            sustainSheet.Cell(1, 2).Value = "Source";
            sustainSheet.Cell(1, 3).Value = "Category";
            sustainSheet.Cell(1, 4).Value = "Subcategory";
            sustainSheet.Cell(1, 5).Value = "Unit";
            sustainSheet.Cell(1, 6).Value = "Description";
            sustainSheet.Cell(1, 7).Value = "SustainabilityIndicator";
            sustainSheet.Cell(1, 8).Value = "Value";

            // Datos de prueba
            sustainSheet.Cell(2, 1).Value = "INT001";
            sustainSheet.Cell(2, 2).Value = "Source1";
            sustainSheet.Cell(2, 3).Value = "Category1";
            sustainSheet.Cell(2, 4).Value = "Subcategory1";
            sustainSheet.Cell(2, 5).Value = "kg CO2";
            sustainSheet.Cell(2, 6).Value = "Test description 1";
            sustainSheet.Cell(2, 7).Value = "CO2";
            sustainSheet.Cell(2, 8).Value = 10.5;

            sustainSheet.Cell(3, 1).Value = "INT002";
            sustainSheet.Cell(3, 2).Value = "Source2";
            sustainSheet.Cell(3, 3).Value = "Category2";
            sustainSheet.Cell(3, 4).Value = ""; // Subcategory vacío
            sustainSheet.Cell(3, 5).Value = "kWh";
            sustainSheet.Cell(3, 6).Value = "Test description 2";
            sustainSheet.Cell(3, 7).Value = "Energy";
            sustainSheet.Cell(3, 8).Value = 25.0;

            sustainSheet.Cell(4, 1).Value = "INT004"; // Código que no existe en CodeRelationship
            sustainSheet.Cell(4, 2).Value = "Source3";
            sustainSheet.Cell(4, 3).Value = "Category3";
            sustainSheet.Cell(4, 4).Value = "Subcategory3";
            sustainSheet.Cell(4, 5).Value = "L";
            sustainSheet.Cell(4, 6).Value = "Test description 3";
            sustainSheet.Cell(4, 7).Value = "Water";
            sustainSheet.Cell(4, 8).Value = ""; // Value vacío

            workbook.SaveAs(_testExcelFile);
        }

        private void CreateEmptyExcelFile()
        {
            using var workbook = new XLWorkbook();
            var codeRelSheet = workbook.Worksheets.Add("CodeRelationship");
            var sustainSheet = workbook.Worksheets.Add("SustainabilityValues");

            // Solo headers
            codeRelSheet.Cell(1, 1).Value = "InternalCode";
            codeRelSheet.Cell(1, 2).Value = "ExternalCode";
            codeRelSheet.Cell(1, 3).Value = "Factor";
            codeRelSheet.Cell(1, 4).Value = "Client";

            sustainSheet.Cell(1, 1).Value = "InternalCode";
            sustainSheet.Cell(1, 2).Value = "Source";
            sustainSheet.Cell(1, 3).Value = "Category";
            sustainSheet.Cell(1, 4).Value = "Subcategory";
            sustainSheet.Cell(1, 5).Value = "Unit";
            sustainSheet.Cell(1, 6).Value = "Description";
            sustainSheet.Cell(1, 7).Value = "SustainabilityIndicator";
            sustainSheet.Cell(1, 8).Value = "Value";

            workbook.SaveAs(_testExcelFile);
        }

        private void CreateInvalidExcelFile()
        {
            using var workbook = new XLWorkbook();
            var codeRelSheet = workbook.Worksheets.Add("CodeRelationship");
            // Falta la hoja SustainabilityValues intencionalmente
            workbook.SaveAs(_testExcelFile);
        }

        #endregion

        #region Tests de métodos privados usando Reflection

        [Test]
        public void ReadCodeRelationshipsFromExcel_ValidFile_ReturnsCorrectData()
        {
            // Arrange
            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadCodeRelationshipsFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "El método ReadCodeRelationshipsFromExcel no fue encontrado");

            // Act
            var result = (List<CodeRelationship>)method.Invoke(null, new object[] { _testExcelFile });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));

            // Verificar primer registro
            Assert.That(result[0].InternalCode, Is.EqualTo("INT001"));
            Assert.That(result[0].ExternalCode, Is.EqualTo("EXT001"));
            Assert.That(result[0].Factor, Is.EqualTo(1.5));
            Assert.That(result[0].Client, Is.EqualTo("Client1"));

            // Verificar registro con factor vacío (debería ser 1.0 por defecto)
            var emptyFactorRecord = result.FirstOrDefault(r => r.InternalCode == "INT003");
            Assert.That(emptyFactorRecord, Is.Not.Null);
            Assert.That(emptyFactorRecord.Factor, Is.EqualTo(1.0)); // Valor por defecto
            Assert.That(emptyFactorRecord.Client, Is.Null);
        }

        [Test]
        public void ReadCodeRelationshipsFromExcel_EmptyFile_ReturnsEmptyList()
        {
            // Arrange
            CreateEmptyExcelFile();
            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadCodeRelationshipsFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (List<CodeRelationship>)method.Invoke(null, new object[] { _testExcelFile });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void ReadSustainabilityValuesFromExcel_ValidFile_ReturnsCorrectData()
        {
            // Arrange
            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadSustainabilityValuesFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "El método ReadSustainabilityValuesFromExcel no fue encontrado");

            // Act
            var result = (List<SustainabilityValue>)method.Invoke(null, new object[] { _testExcelFile });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));

            // Verificar primer registro
            Assert.That(result[0].InternalCode, Is.EqualTo("INT001"));
            Assert.That(result[0].Source, Is.EqualTo("Source1"));
            Assert.That(result[0].Category, Is.EqualTo("Category1"));
            Assert.That(result[0].Subcategory, Is.EqualTo("Subcategory1"));
            Assert.That(result[0].Unit, Is.EqualTo("kg CO2"));
            Assert.That(result[0].Description, Is.EqualTo("Test description 1"));
            Assert.That(result[0].SustainabilityIndicator, Is.EqualTo("CO2"));
            Assert.That(result[0].Value, Is.EqualTo(10.5));

            // Verificar registro con subcategory vacía
            var emptySubcategoryRecord = result.FirstOrDefault(r => r.InternalCode == "INT002");
            Assert.That(emptySubcategoryRecord, Is.Not.Null);
            Assert.That(emptySubcategoryRecord.Subcategory, Is.Null);

            // Verificar registro con value vacío
            var emptyValueRecord = result.FirstOrDefault(r => r.InternalCode == "INT004");
            Assert.That(emptyValueRecord, Is.Not.Null);
            Assert.That(emptyValueRecord.Value, Is.Null);
        }

        [Test]
        public void ReadSustainabilityValuesFromExcel_MissingWorksheet_ThrowsException()
        {
            // Arrange
            CreateInvalidExcelFile();
            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadSustainabilityValuesFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act & Assert
            var ex = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object[] { _testExcelFile }));

            Assert.That(ex.InnerException, Is.TypeOf<Exception>());
            Assert.That(ex.InnerException.Message, Contains.Substring("SustainabilityValues"));
        }

        [Test]
        public void ReadSustainabilityValuesFromExcel_EmptyFile_ReturnsEmptyList()
        {
            // Arrange
            CreateEmptyExcelFile();
            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadSustainabilityValuesFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (List<SustainabilityValue>)method.Invoke(null, new object[] { _testExcelFile });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region Tests de casos edge

        [Test]
        public void ReadCodeRelationshipsFromExcel_FileNotExists_ThrowsException()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.xlsx");
            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadCodeRelationshipsFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act & Assert
            Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object[] { nonExistentFile }));
        }

        [Test]
        public void ReadSustainabilityValuesFromExcel_FileNotExists_ThrowsException()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.xlsx");
            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadSustainabilityValuesFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act & Assert
            Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object[] { nonExistentFile }));
        }

        #endregion

        #region Tests de las clases modelo

        [Test]
        public void CodeRelationship_Properties_WorkCorrectly()
        {
            // Arrange & Act
            var codeRelationship = new CodeRelationship
            {
                InternalCode = "TEST_INTERNAL",
                ExternalCode = "TEST_EXTERNAL",
                Factor = 2.5,
                Client = "TEST_CLIENT"
            };

            // Assert
            Assert.That(codeRelationship.InternalCode, Is.EqualTo("TEST_INTERNAL"));
            Assert.That(codeRelationship.ExternalCode, Is.EqualTo("TEST_EXTERNAL"));
            Assert.That(codeRelationship.Factor, Is.EqualTo(2.5));
            Assert.That(codeRelationship.Client, Is.EqualTo("TEST_CLIENT"));
        }

        [Test]
        public void CodeRelationship_NullableProperties_WorkCorrectly()
        {
            // Arrange & Act
            var codeRelationship = new CodeRelationship
            {
                InternalCode = "TEST_INTERNAL",
                ExternalCode = "TEST_EXTERNAL",
                Factor = null,
                Client = null
            };

            // Assert
            Assert.That(codeRelationship.Factor, Is.Null);
            Assert.That(codeRelationship.Client, Is.Null);
        }

        [Test]
        public void SustainabilityValue_Properties_WorkCorrectly()
        {
            // Arrange & Act
            var sustainabilityValue = new SustainabilityValue
            {
                InternalCode = "TEST_INTERNAL",
                Source = "TEST_SOURCE",
                Category = "TEST_CATEGORY",
                Subcategory = "TEST_SUBCATEGORY",
                Unit = "kg",
                Description = "TEST_DESCRIPTION",
                SustainabilityIndicator = "CO2",
                Value = 15.75
            };

            // Assert
            Assert.That(sustainabilityValue.InternalCode, Is.EqualTo("TEST_INTERNAL"));
            Assert.That(sustainabilityValue.Source, Is.EqualTo("TEST_SOURCE"));
            Assert.That(sustainabilityValue.Category, Is.EqualTo("TEST_CATEGORY"));
            Assert.That(sustainabilityValue.Subcategory, Is.EqualTo("TEST_SUBCATEGORY"));
            Assert.That(sustainabilityValue.Unit, Is.EqualTo("kg"));
            Assert.That(sustainabilityValue.Description, Is.EqualTo("TEST_DESCRIPTION"));
            Assert.That(sustainabilityValue.SustainabilityIndicator, Is.EqualTo("CO2"));
            Assert.That(sustainabilityValue.Value, Is.EqualTo(15.75));
        }

        [Test]
        public void SustainabilityValue_NullableProperties_WorkCorrectly()
        {
            // Arrange & Act
            var sustainabilityValue = new SustainabilityValue
            {
                InternalCode = "TEST_INTERNAL",
                Source = null,
                Category = null,
                Subcategory = null,
                Unit = null,
                Description = null,
                SustainabilityIndicator = "CO2",
                Value = null
            };

            // Assert
            Assert.That(sustainabilityValue.Source, Is.Null);
            Assert.That(sustainabilityValue.Category, Is.Null);
            Assert.That(sustainabilityValue.Subcategory, Is.Null);
            Assert.That(sustainabilityValue.Unit, Is.Null);
            Assert.That(sustainabilityValue.Description, Is.Null);
            Assert.That(sustainabilityValue.Value, Is.Null);
            Assert.That(sustainabilityValue.InternalCode, Is.Not.Null);
            Assert.That(sustainabilityValue.SustainabilityIndicator, Is.Not.Null);
        }

        #endregion

        #region Tests de validación de Excel

        [Test]
        public void ReadCodeRelationshipsFromExcel_HandlesSpecialCharacters()
        {
            // Arrange - Crear archivo con caracteres especiales
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("CodeRelationship");

            sheet.Cell(1, 1).Value = "InternalCode";
            sheet.Cell(1, 2).Value = "ExternalCode";
            sheet.Cell(1, 3).Value = "Factor";
            sheet.Cell(1, 4).Value = "Client";

            sheet.Cell(2, 1).Value = "ÄÖÜ_123";
            sheet.Cell(2, 2).Value = "áéíóú_456";
            sheet.Cell(2, 3).Value = 1.5;
            sheet.Cell(2, 4).Value = "Cliente Ñoño";

            var specialFile = Path.Combine(_testDirectory, "special_chars.xlsx");
            workbook.SaveAs(specialFile);

            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadCodeRelationshipsFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (List<CodeRelationship>)method.Invoke(null, new object[] { specialFile });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].InternalCode, Is.EqualTo("ÄÖÜ_123"));
            Assert.That(result[0].ExternalCode, Is.EqualTo("áéíóú_456"));
            Assert.That(result[0].Client, Is.EqualTo("Cliente Ñoño"));
        }

        [Test]
        public void ReadSustainabilityValuesFromExcel_HandlesLargeNumbers()
        {
            // Arrange - Crear archivo con números grandes
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("SustainabilityValues");

            // Headers
            sheet.Cell(1, 1).Value = "InternalCode";
            sheet.Cell(1, 2).Value = "Source";
            sheet.Cell(1, 3).Value = "Category";
            sheet.Cell(1, 4).Value = "Subcategory";
            sheet.Cell(1, 5).Value = "Unit";
            sheet.Cell(1, 6).Value = "Description";
            sheet.Cell(1, 7).Value = "SustainabilityIndicator";
            sheet.Cell(1, 8).Value = "Value";

            // Datos con números grandes
            sheet.Cell(2, 1).Value = "LARGE_001";
            sheet.Cell(2, 2).Value = "Source";
            sheet.Cell(2, 3).Value = "Category";
            sheet.Cell(2, 4).Value = "Subcategory";
            sheet.Cell(2, 5).Value = "tons";
            sheet.Cell(2, 6).Value = "Large number test";
            sheet.Cell(2, 7).Value = "CO2";
            sheet.Cell(2, 8).Value = 1234567.89;

            var largeFile = Path.Combine(_testDirectory, "large_numbers.xlsx");
            workbook.SaveAs(largeFile);

            var importDataType = typeof(ImportData);
            var method = importDataType.GetMethod("ReadSustainabilityValuesFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (List<SustainabilityValue>)method.Invoke(null, new object[] { largeFile });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Value, Is.EqualTo(1234567.89));
        }

        #endregion
    }
}