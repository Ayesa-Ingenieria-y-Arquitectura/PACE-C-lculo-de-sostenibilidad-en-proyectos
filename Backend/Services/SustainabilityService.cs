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

            string getQuerry = @"
                SELECT 
                    cr.external_code AS ExternalId,
                    cr.internal_code AS InternalId,
                    sv.category || ' ' || sv.subcategory AS Category,
                    sv.sustainability_indicator AS Indicator,
                    sv.value
                FROM 
                    code_relationship cr
                JOIN 
                    code_relationship_sustainability_values cr_sv 
                ON 
                    cr.internal_code = cr_sv.code_relationship_internal_code
                JOIN 
                    sustainability_values sv 
                ON 
                    cr_sv.sustainability_values_internal_code = sv.internal_code;
            ";

            using (var connected = new NpgsqlConnection(_connection))
            {
                connected.Open();

                using (var cmd = new NpgsqlCommand(getQuerry, connected))
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
                            Value = Convert.ToDouble(reader["value"])
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
                .ToDictionary(r => r.Key, r=> r.Select(r => r.ExternalId).ToList());

            return res;
        }

        public static HashSet<string> medidores(List<SustainabilityRecord> data)
        {
            return data.Select(s => s.Indicator).ToHashSet();
        }

        public static List<KeyValuePair<string, string>> getCodeRelation(List<SustainabilityRecord> data)
        {
            return data.Select(s => new KeyValuePair<string, string>(s.ExternalId, s.InternalId)).ToList();
        }
    }
}
