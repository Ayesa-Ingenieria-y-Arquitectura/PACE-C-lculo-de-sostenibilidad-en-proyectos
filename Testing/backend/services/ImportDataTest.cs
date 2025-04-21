using Bc3_WPF.Backend.Services;
using Npgsql;
using System.Reflection;
using ClosedXML.Excel;

namespace Testing.Backend.Services
{
    [TestFixture]
    public class ImportDataTests
    {
        private string _testExcelFilePath;
        private const string TEST_CONNECTION_STRING = "Host=localhost;Username=postgres;Password=r00t;Database=PACE_TEST";

        [SetUp]
        public void Setup()
        {
            // Create a test Excel file
            _testExcelFilePath = Path.Combine(Path.GetTempPath(), "TestImportData.xlsx");
            CreateTestExcelFile(_testExcelFilePath);

            // Make sure we have a clean database for each test
            CleanupTestDatabase();
        }

        [TearDown]
        public void Cleanup()
        {
            // Delete the test Excel file
            if (File.Exists(_testExcelFilePath))
            {
                File.Delete(_testExcelFilePath);
            }

            // Clean up the test database
            CleanupTestDatabase();
        }

        private void CleanupTestDatabase()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(TEST_CONNECTION_STRING))
            {
                connection.Open();
                using (NpgsqlCommand command = new NpgsqlCommand(
                    "TRUNCATE TABLE code_relationship_sustainability_values, code_relationship, sustainability_values RESTART IDENTITY CASCADE;",
                    connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void CreateTestExcelFile(string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                // Create CodeRelationship worksheet
                var crWorksheet = workbook.Worksheets.Add("CodeRelationship");
                crWorksheet.Cell(1, 1).Value = "InternalCode";
                crWorksheet.Cell(1, 2).Value = "ExternalCode";

                // Add sample data
                crWorksheet.Cell(2, 1).Value = "IC001";
                crWorksheet.Cell(2, 2).Value = "EC001";
                crWorksheet.Cell(3, 1).Value = "IC002";
                crWorksheet.Cell(3, 2).Value = "EC002";
                crWorksheet.Cell(4, 1).Value = "IC003";
                crWorksheet.Cell(4, 2).Value = "EC003";

                // Create SustainabilityValues worksheet
                var svWorksheet = workbook.Worksheets.Add("SustainabilityValues");
                svWorksheet.Cell(1, 1).Value = "InternalCode";
                svWorksheet.Cell(1, 2).Value = "DatabaseName";
                svWorksheet.Cell(1, 3).Value = "Category";
                svWorksheet.Cell(1, 4).Value = "Subcategory";
                svWorksheet.Cell(1, 5).Value = "Description";
                svWorksheet.Cell(1, 6).Value = "SustainabilityIndicator";
                svWorksheet.Cell(1, 7).Value = "Value";

                // Add sample data
                svWorksheet.Cell(2, 1).Value = "IC001";
                svWorksheet.Cell(2, 2).Value = "DB1";
                svWorksheet.Cell(2, 3).Value = "Cat1";
                svWorksheet.Cell(2, 4).Value = "SubCat1";
                svWorksheet.Cell(2, 5).Value = "Desc1";
                svWorksheet.Cell(2, 6).Value = "SI1";
                svWorksheet.Cell(2, 7).Value = 10.5;

                svWorksheet.Cell(3, 1).Value = "IC002";
                svWorksheet.Cell(3, 2).Value = "DB2";
                svWorksheet.Cell(3, 3).Value = "Cat2";
                svWorksheet.Cell(3, 4).Value = "";
                svWorksheet.Cell(3, 5).Value = "Desc2";
                svWorksheet.Cell(3, 6).Value = "SI2";
                svWorksheet.Cell(3, 7).Value = 20.5;

                workbook.SaveAs(filePath);
            }
        }

        [Test]
        public void ReadCodeRelationshipsFromExcel_ShouldReturnCorrectData()
        {
            // Use reflection to access private method
            MethodInfo method = typeof(ImportData).GetMethod("ReadCodeRelationshipsFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Method ReadCodeRelationshipsFromExcel not found");

            // Call the method
            var result = method.Invoke(null, new object[] { _testExcelFilePath }) as List<CodeRelationship>;

            // Assert
            Assert.NotNull(result, "Result should not be null");
            Assert.AreEqual(3, result.Count, "Should read 3 code relationships");

            // Check first row
            Assert.AreEqual("IC001", result[0].InternalCode);
            Assert.AreEqual("EC001", result[0].ExternalCode);

            // Check last row
            Assert.AreEqual("IC003", result[2].InternalCode);
            Assert.AreEqual("EC003", result[2].ExternalCode);
        }

        [Test]
        public void ReadSustainabilityValuesFromExcel_ShouldReturnCorrectData()
        {
            // Use reflection to access private method
            MethodInfo method = typeof(ImportData).GetMethod("ReadSustainabilityValuesFromExcel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Method ReadSustainabilityValuesFromExcel not found");

            // Call the method
            var result = method.Invoke(null, new object[] { _testExcelFilePath }) as List<SustainabilityValue>;

            // Assert
            Assert.NotNull(result, "Result should not be null");
            Assert.AreEqual(2, result.Count, "Should read 2 sustainability values");

            // Check first row
            Assert.AreEqual("IC001", result[0].InternalCode);
            Assert.AreEqual("DB1", result[0].DatabaseName);
            Assert.AreEqual("Cat1", result[0].Category);
            Assert.AreEqual("SubCat1", result[0].Subcategory);
            Assert.AreEqual("Desc1", result[0].Description);
            Assert.AreEqual("SI1", result[0].SustainabilityIndicator);
            Assert.AreEqual(10.5, result[0].Value);

            // Check second row with empty subcategory
            Assert.AreEqual("IC002", result[1].InternalCode);
            Assert.AreEqual("DB2", result[1].DatabaseName);
            Assert.AreEqual("Cat2", result[1].Category);
            Assert.IsNull(result[1].Subcategory, "Subcategory should be null for empty values");
            Assert.AreEqual("Desc2", result[1].Description);
            Assert.AreEqual("SI2", result[1].SustainabilityIndicator);
            Assert.AreEqual(20.5, result[1].Value);
        }

        [Test]
        public void ReadCodeRelationshipsFromExcel_ShouldThrowExceptionForMissingWorksheet()
        {
            // Create an Excel file without the required worksheets
            string tempFile = Path.Combine(Path.GetTempPath(), "MissingWorksheet.xlsx");

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("WrongName");
                    workbook.SaveAs(tempFile);
                }

                // Use reflection to access private method
                MethodInfo method = typeof(ImportData).GetMethod("ReadCodeRelationshipsFromExcel",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method, "Method ReadCodeRelationshipsFromExcel not found");

                // Call the method - should throw exception
                Assert.Throws<TargetInvocationException>(() =>
                    method.Invoke(null, new object[] { tempFile }));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }


        [Test]
        public void TruncateTables_ShouldClearAllTables()
        {
            // First insert some test data
            InsertTestData();

            // Use reflection to access private method
            MethodInfo? method = typeof(ImportData).GetMethod("TruncateTables",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Method TruncateTables not found");

            // Call the method - null check is already handled by the Assert above
            method!.Invoke(null, new object[] { TEST_CONNECTION_STRING });

            // Verify tables are empty
            using (NpgsqlConnection connection = new NpgsqlConnection(TEST_CONNECTION_STRING))
            {
                connection.Open();

                // Check code_relationship table
                using (NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM code_relationship", connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(0, count, "code_relationship table should be empty");
                }

                // Check sustainability_values table
                using (NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM sustainability_values", connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(0, count, "sustainability_values table should be empty");
                }

                // Check code_relationship_sustainability_values table
                using (NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM code_relationship_sustainability_values", connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(0, count, "code_relationship_sustainability_values table should be empty");
                }
            }
        }

        [Test]
        public void ImportCodeRelationships_ShouldInsertDataCorrectly()
        {
            // Create sample data
            var codeRelationships = new List<CodeRelationship>
            {
                new CodeRelationship { InternalCode = "IC001", ExternalCode = "EC001" },
                new CodeRelationship { InternalCode = "IC002", ExternalCode = "EC002" },
                new CodeRelationship { InternalCode = "IC001", ExternalCode = "EC003" } // Duplicate internal code
            };

            // Use reflection to access private method
            MethodInfo method = typeof(ImportData).GetMethod("ImportCodeRelationships",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Method ImportCodeRelationships not found");

            // Call the method
            var result = method.Invoke(null, new object[] { codeRelationships, TEST_CONNECTION_STRING }) as Dictionary<string, int>;

            // Assert
            Assert.NotNull(result, "Result should not be null");
            Assert.AreEqual(2, result.Count, "Should have 2 distinct internal codes");
            Assert.IsTrue(result.ContainsKey("IC001"), "Should contain IC001");
            Assert.IsTrue(result.ContainsKey("IC002"), "Should contain IC002");

            // Verify data in the database
            using (NpgsqlConnection connection = new NpgsqlConnection(TEST_CONNECTION_STRING))
            {
                connection.Open();

                // Check code_relationship table record count
                using (NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM code_relationship", connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(3, count, "Should have 3 records in code_relationship table");
                }

                // Check specific records
                using (NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM code_relationship WHERE internal_code = 'IC001' AND external_code = 'EC001'",
                    connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(1, count, "Should find the first IC001 record");
                }

                using (NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM code_relationship WHERE internal_code = 'IC001' AND external_code = 'EC003'",
                    connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(1, count, "Should find the duplicate IC001 record");
                }
            }
        }

        [Test]
        public void ImportSustainabilityValues_ShouldInsertDataCorrectly()
        {
            // Create sample data
            var sustainabilityValues = new List<SustainabilityValue>
            {
                new SustainabilityValue
                {
                    InternalCode = "IC001",
                    DatabaseName = "DB1",
                    Category = "Cat1",
                    Subcategory = "SubCat1",
                    Description = "Desc1",
                    SustainabilityIndicator = "SI1",
                    Value = 10.5
                },
                new SustainabilityValue
                {
                    InternalCode = "IC002",
                    DatabaseName = "DB2",
                    Category = null,
                    Subcategory = null,
                    Description = "Desc2",
                    SustainabilityIndicator = "SI2",
                    Value = null
                }
            };

            // Use reflection to access private method
            MethodInfo method = typeof(ImportData).GetMethod("ImportSustainabilityValues",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Method ImportSustainabilityValues not found");

            // Call the method
            var result = method.Invoke(null, new object[] { sustainabilityValues, TEST_CONNECTION_STRING })
                as Dictionary<string, int>;

            // Assert
            Assert.NotNull(result, "Result should not be null");
            Assert.AreEqual(2, result.Count, "Should have 2 sustainability values");
            Assert.IsTrue(result.ContainsKey("IC001"), "Should contain IC001");
            Assert.IsTrue(result.ContainsKey("IC002"), "Should contain IC002");

            // Verify data in the database
            using (NpgsqlConnection connection = new NpgsqlConnection(TEST_CONNECTION_STRING))
            {
                connection.Open();

                // Check sustainability_values table record count
                using (NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM sustainability_values", connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(2, count, "Should have 2 records in sustainability_values table");
                }

                // Check specific record with all values
                using (NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT * FROM sustainability_values WHERE internal_code = 'IC001'",
                    connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        Assert.IsTrue(reader.Read(), "Should find the IC001 record");
                        Assert.AreEqual("DB1", reader["database_name"]);
                        Assert.AreEqual("Cat1", reader["category"]);
                        Assert.AreEqual("SubCat1", reader["subcategory"]);
                        Assert.AreEqual("Desc1", reader["description"]);
                        Assert.AreEqual("SI1", reader["sustainability_indicator"]);
                        Assert.AreEqual(10.5, Convert.ToDouble(reader["value"]));
                    }
                }

                // Check specific record with null values
                using (NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT * FROM sustainability_values WHERE internal_code = 'IC002'",
                    connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        Assert.IsTrue(reader.Read(), "Should find the IC002 record");
                        Assert.AreEqual("DB2", reader["database_name"]);
                        Assert.IsTrue(reader.IsDBNull(reader.GetOrdinal("category")), "Category should be NULL");
                        Assert.IsTrue(reader.IsDBNull(reader.GetOrdinal("subcategory")), "Subcategory should be NULL");
                        Assert.AreEqual("Desc2", reader["description"]);
                        Assert.AreEqual("SI2", reader["sustainability_indicator"]);
                        Assert.IsTrue(reader.IsDBNull(reader.GetOrdinal("value")), "Value should be NULL");
                    }
                }
            }
        }














        private Dictionary<string, int> InsertTestCodeRelationships()
        {
            var codeRelationshipIds = new Dictionary<string, int>();

            using (NpgsqlConnection connection = new NpgsqlConnection(TEST_CONNECTION_STRING))
            {
                connection.Open();

                // Insert test code relationships
                using (NpgsqlCommand command = new NpgsqlCommand(
                    "INSERT INTO code_relationship (internal_code, external_code) VALUES (@internalCode, @externalCode) RETURNING id",
                    connection))
                {
                    // First relationship
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("internalCode", "IC001");
                    command.Parameters.AddWithValue("externalCode", "EC001");
                    int id1 = Convert.ToInt32(command.ExecuteScalar());
                    codeRelationshipIds["IC001"] = id1;

                    // Second relationship
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("internalCode", "IC002");
                    command.Parameters.AddWithValue("externalCode", "EC002");
                    int id2 = Convert.ToInt32(command.ExecuteScalar());
                    codeRelationshipIds["IC002"] = id2;
                }
            }

            return codeRelationshipIds;
        }

        private Dictionary<string, int> InsertTestSustainabilityValues()
        {
            var sustainabilityValueIds = new Dictionary<string, int>();

            using (NpgsqlConnection connection = new NpgsqlConnection(TEST_CONNECTION_STRING))
            {
                connection.Open();

                // Insert test sustainability values
                using (NpgsqlCommand command = new NpgsqlCommand(
                    "INSERT INTO sustainability_values (internal_code, database_name, category, subcategory, description, sustainability_indicator, value) " +
                    "VALUES (@internalCode, @databaseName, @category, @subcategory, @description, @sustainabilityIndicator, @value) " +
                    "RETURNING id",
                    connection))
                {
                    // First value
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("internalCode", "IC001");
                    command.Parameters.AddWithValue("databaseName", "DB1");
                    command.Parameters.AddWithValue("category", "Cat1");
                    command.Parameters.AddWithValue("subcategory", "SubCat1");
                    command.Parameters.AddWithValue("description", "Desc1");
                    command.Parameters.AddWithValue("sustainabilityIndicator", "SI1");
                    command.Parameters.AddWithValue("value", 10.5);
                    int id1 = Convert.ToInt32(command.ExecuteScalar());
                    sustainabilityValueIds["IC001"] = id1;

                    // Second value
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("internalCode", "IC002");
                    command.Parameters.AddWithValue("databaseName", "DB2");
                    command.Parameters.AddWithValue("category", DBNull.Value);
                    command.Parameters.AddWithValue("subcategory", DBNull.Value);
                    command.Parameters.AddWithValue("description", "Desc2");
                    command.Parameters.AddWithValue("sustainabilityIndicator", "SI2");
                    command.Parameters.AddWithValue("value", DBNull.Value);
                    int id2 = Convert.ToInt32(command.ExecuteScalar());
                    sustainabilityValueIds["IC002"] = id2;
                }
            }

            return sustainabilityValueIds;
        }

        private void InsertTestData()
        {
            var codeRelationshipIds = InsertTestCodeRelationships();
            var sustainabilityValueIds = InsertTestSustainabilityValues();

            // Create relationships
            using (NpgsqlConnection connection = new NpgsqlConnection(TEST_CONNECTION_STRING))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(
                    "INSERT INTO code_relationship_sustainability_values (code_relationship_id, sustainability_values_id) " +
                    "VALUES (@codeRelId, @sustainId)",
                    connection))
                {
                    // First relationship
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("codeRelId", codeRelationshipIds["IC001"]);
                    command.Parameters.AddWithValue("sustainId", sustainabilityValueIds["IC001"]);
                    command.ExecuteNonQuery();

                    // Second relationship
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("codeRelId", codeRelationshipIds["IC002"]);
                    command.Parameters.AddWithValue("sustainId", sustainabilityValueIds["IC002"]);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}