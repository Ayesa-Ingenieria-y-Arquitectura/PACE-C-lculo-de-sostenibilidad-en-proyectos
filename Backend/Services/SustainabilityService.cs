using Bc3_WPF.Backend.Modelos;
using Npgsql;

namespace Bc3_WPF.Backend.Services
{
    public class SustainabilityService
    {
        private static string _connection = "Host=localhost;Username=postgres;Password=r00t;Database=PACE";

        public static List<SustainabilityRecord> getFromDatabase()
        {
            List<SustainabilityRecord> res = new();
            string getQuery = @"
                SELECT 
                    cr.external_code AS ExternalId,
                    cr.internal_code AS InternalId,
                    sv.category || ' ' || sv.subcategory AS Category,
                    sv.sustainability_indicator AS Indicator,
                    sv.value,
                    sv.database_name AS Database
                FROM 
                    code_relationship cr
                JOIN 
                    code_relationship_sustainability_values cr_sv 
                ON 
                    cr.id = cr_sv.code_relationship_id
                JOIN 
                    sustainability_values sv 
                ON 
                    cr_sv.sustainability_values_id = sv.id
                WHERE
                    cr.internal_code = sv.internal_code;
            ";

            using (var connected = new NpgsqlConnection(_connection))
            {
                connected.Open();
                using (var cmd = new NpgsqlCommand(getQuery, connected))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = new SustainabilityRecord
                        {
                            ExternalId = reader["ExternalId"].ToString(),
                            InternalId = reader["InternalId"].ToString(),
                            Category = reader["Category"].ToString(),
                            Indicator = reader["Indicator"].ToString(),
                            Value = Convert.ToDouble(reader["value"]),
                            Database = reader["Database"].ToString() // Añadido el campo Database
                        };
                        res.Add(record);
                    }
                }
            }
            return res;
        }

        public static Dictionary<string, List<string>> getFromCategories(List<SustainabilityRecord> data)
        {
            Dictionary<string, List<string>> res = data.GroupBy(r => r.Category)
                .ToDictionary(r => r.Key, r => r.Select(r => r.ExternalId).Distinct().ToList());
            return res;
        }

        public static HashSet<string> medidores(List<SustainabilityRecord> data)
        {
            return data.Select(s => s.Indicator).ToHashSet();
        }

        public static List<KeyValuePair<string, string>> getCodeRelation(List<SustainabilityRecord> data)
        {
            return data.Select(s => new KeyValuePair<string, string>(s.ExternalId, s.InternalId))
                .Distinct()
                .ToList();
        }

        // Nuevo método para obtener bases de datos
        public static HashSet<string> getDatabases(List<SustainabilityRecord> data)
        {
            return data.Select(s => s.Database).ToHashSet();
        }

        // Nuevo método para agrupar por base de datos
        public static Dictionary<string, List<SustainabilityRecord>> groupByDatabase(List<SustainabilityRecord> data)
        {
            return data.GroupBy(r => r.Database)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}