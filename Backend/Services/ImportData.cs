using ClosedXML.Excel;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bc3_WPF.Backend.Services
{
    internal class ImportData
    {
        public static void ImportDB(string filePath)
        {
            string _connection = "Host=localhost;Username=postgres;Password=r00t;Database=PACE";

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("El archivo especificado no existe. Por favor, verifique la ruta.");
                    return;
                }

                // Vaciar las tablas existentes
                TruncateTables(_connection);

                // Leer datos del Excel
                var codeRelationships = ReadCodeRelationshipsFromExcel(filePath);
                var sustainabilityValues = ReadSustainabilityValuesFromExcel(filePath);

                // Importar datos a PostgreSQL y obtener mapeo de los nuevos IDs
                var codeRelationshipIds = ImportCodeRelationships(codeRelationships, _connection);
                var sustainabilityValueIds = ImportSustainabilityValues(sustainabilityValues, _connection);

                // Crear relaciones entre tablas basadas en internal_code
                CreateRelationships(codeRelationshipIds, sustainabilityValueIds, _connection);

                Console.WriteLine("Importación completada exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante la importación: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Presiona cualquier tecla para salir...");
        }

        private static void TruncateTables(string ConnectionString)
        {
            Console.WriteLine("Vaciando tablas existentes...");

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                // Borrar en el orden correcto debido a las restricciones de clave foránea
                using (NpgsqlCommand command = new NpgsqlCommand(
                    "TRUNCATE TABLE code_relationship_sustainability_values, code_relationship, sustainability_values RESTART IDENTITY CASCADE;",
                    connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Tablas vaciadas exitosamente.");
        }

        private static List<CodeRelationship> ReadCodeRelationshipsFromExcel(string filePath)
        {
            Console.WriteLine("Leyendo datos de code_relationship desde Excel...");

            var codeRelationships = new List<CodeRelationship>();

            using (var workbook = new XLWorkbook(filePath))
            {
                // Asegurar que existe la hoja CodeRelationship
                if (!workbook.TryGetWorksheet("CodeRelationship", out var worksheet))
                {
                    throw new Exception("No se encontró la hoja 'CodeRelationship' en el archivo Excel.");
                }

                // Leer datos comenzando desde la fila 2 (saltando encabezados)
                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    codeRelationships.Add(new CodeRelationship
                    {
                        InternalCode = row.Cell(1).GetString(),
                        ExternalCode = row.Cell(2).GetString()
                    });
                }
            }

            Console.WriteLine($"Se leyeron {codeRelationships.Count} registros de code_relationship.");
            return codeRelationships;
        }

        private static List<SustainabilityValue> ReadSustainabilityValuesFromExcel(string filePath)
        {
            Console.WriteLine("Leyendo datos de sustainability_values desde Excel...");

            var sustainabilityValues = new List<SustainabilityValue>();

            using (var workbook = new XLWorkbook(filePath))
            {
                // Asegurar que existe la hoja SustainabilityValues
                if (!workbook.TryGetWorksheet("SustainabilityValues", out var worksheet))
                {
                    throw new Exception("No se encontró la hoja 'SustainabilityValues' en el archivo Excel.");
                }

                // Leer datos comenzando desde la fila 2 (saltando encabezados)
                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var sustainValue = new SustainabilityValue
                    {
                        InternalCode = row.Cell(1).GetString(),
                        DatabaseName = row.Cell(2).GetString(),
                        Category = row.Cell(3).IsEmpty() ? null : row.Cell(3).GetString(),
                        Subcategory = row.Cell(4).IsEmpty() ? null : row.Cell(4).GetString(),
                        Description = row.Cell(5).IsEmpty() ? null : row.Cell(5).GetString(),
                        SustainabilityIndicator = row.Cell(6).GetString()
                    };

                    // Manejar el valor double que podría estar vacío
                    if (!row.Cell(7).IsEmpty())
                    {
                        sustainValue.Value = row.Cell(7).GetDouble();
                    }

                    sustainabilityValues.Add(sustainValue);
                }
            }

            Console.WriteLine($"Se leyeron {sustainabilityValues.Count} registros de sustainability_values.");
            return sustainabilityValues;
        }

        private static Dictionary<string, int> ImportCodeRelationships(List<CodeRelationship> codeRelationships, string ConnectionString)
        {
            Console.WriteLine("Importando datos a tabla code_relationship...");

            // Nota: Ahora usamos el primer ID para cada internal_code como referencia
            // pero el método CreateRelationships consultará directamente a la DB para obtener todas las relaciones
            var internalCodeToIdMap = new Dictionary<string, int>();

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                // Preparar y ejecutar la inserción para cada registro
                foreach (var codeRel in codeRelationships)
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(
                        "INSERT INTO code_relationship (internal_code, external_code) VALUES (@internalCode, @externalCode) RETURNING id;",
                        connection))
                    {
                        command.Parameters.AddWithValue("internalCode", codeRel.InternalCode);
                        command.Parameters.AddWithValue("externalCode", codeRel.ExternalCode);

                        int newId = Convert.ToInt32(command.ExecuteScalar());

                        // Almacenar el primer ID encontrado para cada internal_code (solo como referencia)
                        if (!internalCodeToIdMap.ContainsKey(codeRel.InternalCode))
                        {
                            internalCodeToIdMap[codeRel.InternalCode] = newId;
                        }
                    }
                }
            }

            Console.WriteLine($"Se importaron {codeRelationships.Count} registros a code_relationship.");
            return internalCodeToIdMap;
        }

        private static Dictionary<string, int> ImportSustainabilityValues(List<SustainabilityValue> sustainabilityValues, string ConnectionString)
        {
            Console.WriteLine("Importando datos a tabla sustainability_values...");

            var sustainabilityValueIds = new Dictionary<string, int>();

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                // Preparar y ejecutar la inserción para cada registro
                foreach (var sustainValue in sustainabilityValues)
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(
                        "INSERT INTO sustainability_values (internal_code, database_name, category, subcategory, description, sustainability_indicator, value) " +
                        "VALUES (@internalCode, @databaseName, @category, @subcategory, @description, @sustainabilityIndicator, @value) " +
                        "RETURNING id;",
                        connection))
                    {
                        command.Parameters.AddWithValue("internalCode", sustainValue.InternalCode);
                        command.Parameters.AddWithValue("databaseName", sustainValue.DatabaseName);
                        command.Parameters.AddWithValue("category", sustainValue.Category ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("subcategory", sustainValue.Subcategory ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("description", sustainValue.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("sustainabilityIndicator", sustainValue.SustainabilityIndicator);
                        command.Parameters.AddWithValue("value", sustainValue.Value.HasValue ? sustainValue.Value.Value : (object)DBNull.Value);

                        int newId = Convert.ToInt32(command.ExecuteScalar());

                        // Crear una clave compuesta para identificar el registro
                        string key = sustainValue.InternalCode;
                        sustainabilityValueIds[key] = newId;
                    }
                }
            }

            Console.WriteLine($"Se importaron {sustainabilityValues.Count} registros a sustainability_values.");
            return sustainabilityValueIds;
        }

        private static void CreateRelationships(Dictionary<string, int> codeRelationshipIds, Dictionary<string, int> sustainabilityValueIds, string ConnectionString)
        {
            Console.WriteLine("Creando relaciones entre tablas...");

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                // Obtener todos los sustainability_values y sus internal_codes
                var sustainabilityValues = new List<(int Id, string InternalCode)>();

                using (NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT id, internal_code FROM sustainability_values",
                    connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string internalCode = reader.GetString(1);
                            sustainabilityValues.Add((id, internalCode));
                        }
                    }
                }

                int relationshipsCreated = 0;

                // Para cada sustainability_value, buscar un code_relationship correspondiente
                foreach (var sustainValue in sustainabilityValues)
                {
                    // Buscar un code_relationship con el mismo internal_code
                    using (NpgsqlCommand command = new NpgsqlCommand(
                        "SELECT id FROM code_relationship WHERE internal_code = @internalCode LIMIT 1",
                        connection))
                    {
                        command.Parameters.AddWithValue("internalCode", sustainValue.InternalCode);

                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            int codeRelId = Convert.ToInt32(result);

                            // Crear la relación
                            using (NpgsqlCommand insertCommand = new NpgsqlCommand(
                                "INSERT INTO code_relationship_sustainability_values (code_relationship_id, sustainability_values_id) " +
                                "VALUES (@codeRelId, @sustainId);",
                                connection))
                            {
                                insertCommand.Parameters.AddWithValue("codeRelId", codeRelId);
                                insertCommand.Parameters.AddWithValue("sustainId", sustainValue.Id);

                                insertCommand.ExecuteNonQuery();
                                relationshipsCreated++;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Advertencia: No se encontró code_relationship para internal_code '{sustainValue.InternalCode}'");
                        }
                    }
                }

                Console.WriteLine($"Se crearon {relationshipsCreated} relaciones entre las tablas.");
            }
        }
    }

    // Clases para almacenar los datos leídos del Excel
    public class CodeRelationship
    {
        public string InternalCode { get; set; }
        public string ExternalCode { get; set; }
    }

    public class SustainabilityValue
    {
        public string InternalCode { get; set; }
        public string DatabaseName { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string Description { get; set; }
        public string SustainabilityIndicator { get; set; }
        public double? Value { get; set; }
    }
}
