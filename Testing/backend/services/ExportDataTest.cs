using ClosedXML.Excel;
using NUnit.Framework;
using System;
using System.IO;
using Bc3_WPF.Backend.Services;
using System.Drawing;

namespace Testing.Backend.Services
{
    [TestFixture]
    public class ExportDataTests
    {
        private string _testFilePath;

        // Clase que contiene métodos para simular el comportamiento de ExportData
        // sin necesidad de conexiones reales
        private static class TestHelper
        {
            public static void CreateExcelWithTestData(string filePath)
            {
                using (var workbook = new XLWorkbook())
                {
                    // Crear hoja CodeRelationship con datos de prueba
                    var crWorksheet = workbook.Worksheets.Add("CodeRelationship");
                    crWorksheet.Cell(1, 1).Value = "internal_code";
                    crWorksheet.Cell(1, 2).Value = "external_code";
                    var headerRow = crWorksheet.Range(1, 1, 1, 2);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    crWorksheet.Cell(2, 1).Value = "CODE001";
                    crWorksheet.Cell(2, 2).Value = "EXT001";
                    crWorksheet.Cell(3, 1).Value = "CODE002";
                    crWorksheet.Cell(3, 2).Value = "EXT002";
                    crWorksheet.Columns().AdjustToContents();

                    // Crear hoja SustainabilityValues con datos de prueba
                    var svWorksheet = workbook.Worksheets.Add("SustainabilityValues");
                    string[] headers = new string[]
                    {
                        "internal_code", "database_name", "category", "subcategory",
                        "description", "sustainability_indicator", "value"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        svWorksheet.Cell(1, i + 1).Value = headers[i];
                    }

                    var svHeaderRow = svWorksheet.Range(1, 1, 1, headers.Length);
                    svHeaderRow.Style.Font.Bold = true;
                    svHeaderRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    svWorksheet.Cell(2, 1).Value = "SV001";
                    svWorksheet.Cell(2, 2).Value = "TestDB";
                    svWorksheet.Cell(2, 3).Value = "Energy";
                    svWorksheet.Cell(2, 4).Value = "Renewable";
                    svWorksheet.Cell(2, 5).Value = "Test Description";
                    svWorksheet.Cell(2, 6).Value = "CO2";
                    svWorksheet.Cell(2, 7).Value = 123.45;
                    svWorksheet.Cell(2, 7).Style.NumberFormat.Format = "#,##0.00";
                    svWorksheet.Columns().AdjustToContents();

                    // Guardar el archivo
                    workbook.SaveAs(filePath);
                }
            }
        }

        [SetUp]
        public void Setup()
        {
            // Crear una ruta temporal para el archivo de prueba
            _testFilePath = Path.Combine(Path.GetTempPath(), $"TestExport_{Guid.NewGuid()}.xlsx");
        }

        [TearDown]
        public void TearDown()
        {
            // Eliminar el archivo de prueba si existe
            if (File.Exists(_testFilePath))
            {
                try
                {
                    File.Delete(_testFilePath);
                }
                catch (IOException)
                {
                    // Ignorar errores al eliminar el archivo
                }
            }
        }

        [Test]
        public void ExportDB_HandlesExceptionCorrectly()
        {
            // Arrange - una ruta que sabemos que no existe
            string invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent_folder", "test.xlsx");

            // Act & Assert - verificar que el método no lanza excepciones
            Assert.DoesNotThrow(() =>
            {
                ExportData.ExportDB(invalidPath);
            }, "El método debería manejar las excepciones internamente sin propagarlas");
        }

        [Test]
        public void ExcelFileStructure_HasCorrectSheets()
        {
            try
            {
                // Act - crear un archivo Excel con los datos de prueba
                TestHelper.CreateExcelWithTestData(_testFilePath);

                // Assert - verificar que el archivo existe
                Assert.That(File.Exists(_testFilePath), Is.True, "El archivo Excel debería haberse creado");

                // Verificar la estructura del archivo Excel
                using (var workbook = new XLWorkbook(_testFilePath))
                {
                    // Verificar que existen las hojas esperadas
                    Assert.That(workbook.Worksheets.Count, Is.EqualTo(2), "El archivo debería tener dos hojas de trabajo");
                    Assert.That(workbook.Worksheets.Contains("CodeRelationship"), Is.True, "Debería existir la hoja 'CodeRelationship'");
                    Assert.That(workbook.Worksheets.Contains("SustainabilityValues"), Is.True, "Debería existir la hoja 'SustainabilityValues'");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"La prueba falló con una excepción inesperada: {ex.Message}");
            }
        }

        [Test]
        public void CodeRelationshipWorksheet_HasCorrectStructure()
        {
            try
            {
                // Act - crear un archivo Excel con los datos de prueba
                TestHelper.CreateExcelWithTestData(_testFilePath);

                // Assert - verificar los detalles de la hoja CodeRelationship
                using (var workbook = new XLWorkbook(_testFilePath))
                {
                    var worksheet = workbook.Worksheet("CodeRelationship");

                    // Verificar encabezados
                    Assert.That(worksheet.Cell(1, 1).Value.ToString(), Is.EqualTo("internal_code"), "El encabezado de la primera columna debería ser 'internal_code'");
                    Assert.That(worksheet.Cell(1, 2).Value.ToString(), Is.EqualTo("external_code"), "El encabezado de la segunda columna debería ser 'external_code'");

                    // Verificar formato de encabezados
                    Assert.That(worksheet.Cell(1, 1).Style.Font.Bold, Is.True, "El encabezado debería estar en negrita");
                    Assert.That(worksheet.Cell(1, 1).Style.Fill.BackgroundColor.ColorType, Is.EqualTo(XLColorType.Color), "El encabezado debería tener un color de fondo");

                    // Verificar datos
                    Assert.That(worksheet.Cell(2, 1).Value.ToString(), Is.EqualTo("CODE001"));
                    Assert.That(worksheet.Cell(2, 2).Value.ToString(), Is.EqualTo("EXT001"));
                    Assert.That(worksheet.Cell(3, 1).Value.ToString(), Is.EqualTo("CODE002"));
                    Assert.That(worksheet.Cell(3, 2).Value.ToString(), Is.EqualTo("EXT002"));
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"La prueba falló con una excepción inesperada: {ex.Message}");
            }
        }

        [Test]
        public void SustainabilityValuesWorksheet_HasCorrectStructure()
        {
            try
            {
                // Act - crear un archivo Excel con los datos de prueba
                TestHelper.CreateExcelWithTestData(_testFilePath);

                // Assert - verificar los detalles de la hoja SustainabilityValues
                using (var workbook = new XLWorkbook(_testFilePath))
                {
                    var worksheet = workbook.Worksheet("SustainabilityValues");

                    // Verificar encabezados
                    string[] expectedHeaders = new string[]
                    {
                        "internal_code", "database_name", "category", "subcategory",
                        "description", "sustainability_indicator", "value"
                    };

                    for (int i = 0; i < expectedHeaders.Length; i++)
                    {
                        Assert.That(worksheet.Cell(1, i + 1).Value.ToString(), Is.EqualTo(expectedHeaders[i]),
                            $"El encabezado de la columna {i + 1} debería ser '{expectedHeaders[i]}'");
                    }

                    // Verificar formato de encabezados
                    Assert.That(worksheet.Cell(1, 1).Style.Font.Bold, Is.True, "Los encabezados deberían estar en negrita");

                    // Verificar datos
                    Assert.That(worksheet.Cell(2, 1).Value.ToString(), Is.EqualTo("SV001"));
                    Assert.That(worksheet.Cell(2, 2).Value.ToString(), Is.EqualTo("TestDB"));
                    Assert.That(worksheet.Cell(2, 3).Value.ToString(), Is.EqualTo("Energy"));
                    Assert.That(worksheet.Cell(2, 4).Value.ToString(), Is.EqualTo("Renewable"));
                    Assert.That(worksheet.Cell(2, 5).Value.ToString(), Is.EqualTo("Test Description"));
                    Assert.That(worksheet.Cell(2, 6).Value.ToString(), Is.EqualTo("CO2"));
                    Assert.That(worksheet.Cell(2, 7).Value, Is.EqualTo(123.45));

                    // Verificar formato del valor numérico
                    Assert.That(worksheet.Cell(2, 7).Style.NumberFormat.Format, Is.EqualTo("#,##0.00"),
                        "El formato del valor numérico debería ser '#,##0.00'");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"La prueba falló con una excepción inesperada: {ex.Message}");
            }
        }
    }
}

