using ClosedXML.Excel;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bc3_WPF.Backend.Services
{
    public class ExportData
    {
        public static void ExportDB(string filePath)
        {

            string _connection = "Host=localhost;Username=postgres;Password=r00t;Database=PACE";

            try
            {
                Console.WriteLine("Iniciando exportación de datos de PACE a Excel...");

                // Crear el archivo Excel con ClosedXML
                using (var workbook = new XLWorkbook())
                {
                    // Exportar tablas
                    ExportCodeRelationshipTable(workbook, _connection);
                    ExportSustainabilityValuesTable(workbook, _connection);

                    // Guardar el archivo Excel
                    workbook.SaveAs(filePath);
                }

                Console.WriteLine($"Exportación completada. Archivo guardado en: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante la exportación: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }


        private static void ExportCodeRelationshipTable(XLWorkbook workbook, string ConnectionString)
        {
            Console.WriteLine("Exportando tabla code_relationship...");

            // Crear hoja de trabajo para code_relationship
            var worksheet = workbook.Worksheets.Add("CodeRelationship");

            // Consulta SQL para obtener datos sin el campo ID
            string query = "SELECT internal_code, external_code FROM code_relationship";

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    // Configurar encabezados
                    worksheet.Cell(1, 1).Value = "internal_code";
                    worksheet.Cell(1, 2).Value = "external_code";

                    // Dar formato a encabezados
                    var headerRow = worksheet.Range(1, 1, 1, 2);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Llenar datos
                    int row = 2;
                    while (reader.Read())
                    {
                        worksheet.Cell(row, 1).Value = reader["internal_code"].ToString();
                        worksheet.Cell(row, 2).Value = reader["external_code"].ToString();
                        row++;
                    }

                    // Ajustar columnas automáticamente
                    worksheet.Columns().AdjustToContents();
                }
            }

            Console.WriteLine("Tabla code_relationship exportada exitosamente.");
        }

        private static void ExportSustainabilityValuesTable(XLWorkbook workbook, string ConnectionString)
        {
            Console.WriteLine("Exportando tabla sustainability_values...");

            // Crear hoja de trabajo para sustainability_values
            var worksheet = workbook.Worksheets.Add("SustainabilityValues");

            // Consulta SQL para obtener datos sin el campo ID
            string query = "SELECT internal_code, database_name, category, subcategory, description, " +
                          "sustainability_indicator, value FROM sustainability_values";

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    // Configurar encabezados
                    string[] headers = new string[]
                    {
                        "internal_code", "database_name", "category", "subcategory",
                        "description", "sustainability_indicator", "value"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                    }

                    // Dar formato a encabezados
                    var headerRow = worksheet.Range(1, 1, 1, headers.Length);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Llenar datos
                    int row = 2;
                    while (reader.Read())
                    {
                        worksheet.Cell(row, 1).Value = reader["internal_code"].ToString();
                        worksheet.Cell(row, 2).Value = reader["database_name"].ToString();
                        worksheet.Cell(row, 3).Value = reader["category"] != DBNull.Value ? reader["category"].ToString() : string.Empty;
                        worksheet.Cell(row, 4).Value = reader["subcategory"] != DBNull.Value ? reader["subcategory"].ToString() : string.Empty;
                        worksheet.Cell(row, 5).Value = reader["description"] != DBNull.Value ? reader["description"].ToString() : string.Empty;
                        worksheet.Cell(row, 6).Value = reader["sustainability_indicator"].ToString();

                        // Formatear el valor numérico
                        if (reader["value"] != DBNull.Value)
                        {
                            double numValue = Convert.ToDouble(reader["value"]);
                            worksheet.Cell(row, 7).Value = numValue;
                            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                        }

                        row++;
                    }

                    // Ajustar columnas automáticamente
                    worksheet.Columns().AdjustToContents();
                }
            }

            Console.WriteLine("Tabla sustainability_values exportada exitosamente.");
        }
    }
}
