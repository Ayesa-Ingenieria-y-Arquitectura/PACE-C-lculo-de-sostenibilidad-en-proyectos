using Bc3_WPF.Backend.Modelos;
using Npgsql;

namespace Bc3_WPF.Backend.Services
{
    public class SustainabilityService
    {
        private static string _connection = "Host=172.23.6.174;Port=30003;Username=medioambiente_user;Password=qeFw1rgASZaSmP3;Database=pace_medioambiente";

        public static List<SustainabilityRecord> getFromDatabase()
        {
            List<SustainabilityRecord> res = new();
            string getQuery = @"
                SELECT 
                    cr.external_code AS ExternalId,
                    cr.internal_code AS InternalId,
                    cr.factor,
                    sv.category || ' ' || sv.subcategory AS Category,
                    sv.sustainability_indicator AS Indicator,
                    sv.value,
                    sv.source AS Source
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
                            ExternalId = reader["ExternalId"] == DBNull.Value ? "" : reader["ExternalId"].ToString(),
                            InternalId = reader["InternalId"] == DBNull.Value ? "" : reader["InternalId"].ToString(),
                            Factor = reader["Factor"] == DBNull.Value ? 1.0 : Convert.ToDouble(reader["Factor"]),
                            Category = reader["Category"] == DBNull.Value ? "" : reader["Category"].ToString(),
                            Indicator = reader["Indicator"] == DBNull.Value ? "" : reader["Indicator"].ToString(),
                            Value = reader["value"] == DBNull.Value ? 0.0 : Convert.ToDouble(reader["value"]),
                            Source = reader["source"] == DBNull.Value ? "" : reader["source"].ToString() // Añadido el campo Database
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
    }
}