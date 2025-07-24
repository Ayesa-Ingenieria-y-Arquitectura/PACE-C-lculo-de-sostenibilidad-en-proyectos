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

            string _connection = "Host=172.23.6.174;Port=30003;Username=medioambiente_user;Password=qeFw1rgASZaSmP3;Database=pace_medioambiente";

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
        }


        private static void ExportCodeRelationshipTable(XLWorkbook workbook, string ConnectionString)
        {
            Console.WriteLine("Exportando tabla code_relationship...");

            // Crear hoja de trabajo para code_relationship
            var worksheet = workbook.Worksheets.Add("CodeRelationship");

            // Consulta SQL para obtener datos sin el campo ID
            string query = "SELECT internal_code, external_code, factor, Cliente FROM code_relationship";

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    // Configurar encabezados
                    worksheet.Cell(1, 1).Value = "internal_code";
                    worksheet.Cell(1, 2).Value = "external_code";
                    worksheet.Cell(1, 3).Value = "Factor";
                    worksheet.Cell(1, 4).Value = "Cliente";

                    // Llenar datos
                    int row = 2;
                    while (reader.Read())
                    {
                        worksheet.Cell(row, 1).Value = reader["internal_code"].ToString();
                        worksheet.Cell(row, 2).Value = reader["external_code"].ToString();
                        worksheet.Cell(row, 4).Value = reader["Cliente"] != DBNull.Value ? reader["Cliente"].ToString() : string.Empty;

                        if (reader["Factor"] != DBNull.Value)
                        {
                            double numValue = Convert.ToDouble(reader["Factor"]);
                            worksheet.Cell(row, 3).Value = numValue;
                            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                        }

                        row++;
                    }

                    // Crear tabla Excel con formato
                    var dataRange = worksheet.Range(1, 1, row - 1, 4);
                    var table = dataRange.CreateTable("CodeRelationshipTable");

                    // Aplicar estilo de tabla
                    table.Theme = XLTableTheme.TableStyleMedium2;

                    // Habilitar filtros automáticos (ya incluidos en CreateTable)
                    // Opcional: personalizar más el formato
                    table.HeadersRow().Style.Font.Bold = true;
                    table.HeadersRow().Style.Font.FontColor = XLColor.White;
                    table.HeadersRow().Style.Fill.BackgroundColor = XLColor.DarkBlue;

                    // Alternar colores en las filas
                    table.DataRange.Style.Fill.BackgroundColor = XLColor.AliceBlue;

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
            string query = "SELECT internal_code, source, category, subcategory, unit, description, " +
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
                        "internal_code", "source", "category", "subcategory", "unit",
                        "description", "sustainability_indicator", "value"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                    }

                    // Llenar datos
                    int row = 2;
                    while (reader.Read())
                    {
                        worksheet.Cell(row, 1).Value = reader["internal_code"].ToString();
                        worksheet.Cell(row, 2).Value = reader["source"] != DBNull.Value ? reader["source"].ToString() : string.Empty;
                        worksheet.Cell(row, 3).Value = reader["category"] != DBNull.Value ? reader["category"].ToString() : string.Empty;
                        worksheet.Cell(row, 4).Value = reader["subcategory"] != DBNull.Value ? reader["subcategory"].ToString() : string.Empty;
                        worksheet.Cell(row, 5).Value = reader["unit"] != DBNull.Value ? reader["unit"].ToString() : string.Empty;
                        worksheet.Cell(row, 6).Value = reader["description"] != DBNull.Value ? reader["description"].ToString() : string.Empty;
                        worksheet.Cell(row, 7).Value = reader["sustainability_indicator"].ToString();

                        // Formatear el valor numérico
                        if (reader["value"] != DBNull.Value)
                        {
                            double numValue = Convert.ToDouble(reader["value"]);
                            worksheet.Cell(row, 8).Value = numValue;
                            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                        }

                        row++;
                    }

                    // Crear tabla Excel con formato
                    var dataRange = worksheet.Range(1, 1, row - 1, headers.Length);
                    var table = dataRange.CreateTable("SustainabilityValuesTable");

                    // Aplicar estilo de tabla
                    table.Theme = XLTableTheme.TableStyleMedium6;

                    // Personalizar formato de encabezados
                    table.HeadersRow().Style.Font.Bold = true;
                    table.HeadersRow().Style.Font.FontColor = XLColor.White;
                    table.HeadersRow().Style.Fill.BackgroundColor = XLColor.DarkGreen;

                    // Alternar colores en las filas de datos
                    table.DataRange.Style.Fill.BackgroundColor = XLColor.Honeydew;

                    // Formato especial para la columna de valores numéricos
                    var valueColumn = table.DataRange.Column(8);
                    valueColumn.Style.NumberFormat.Format = "#,##0.00";
                    valueColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Ajustar columnas automáticamente
                    worksheet.Columns().AdjustToContents();
                }
            }

            Console.WriteLine("Tabla sustainability_values exportada exitosamente.");
        }
    }
}