using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace Bc3_WPF.Backend.Services
{
    public class DatabaseService
    {
        public static Dictionary<string, KeyValuePair<decimal, decimal>> LoadData(string dbName)
        {

            string connString = $"Host=localhost;Username=postgres;Password=r00t;Database={dbName}";
            Dictionary<string, KeyValuePair<decimal, decimal>> dataDictionary = new();

            using (var conn = new NpgsqlConnection(connString))
            {
                try
                {
                    conn.Open();
                    DataTable dataTable = GetDataFromDatabase(conn);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string id = (string)row["Id"];
                        decimal agua = Convert.ToDecimal(row["Agua"]);
                        decimal carbono = Convert.ToDecimal(row["Carbono"]);
                        dataDictionary[id] = new KeyValuePair<decimal, decimal>(agua, carbono);
                    }

                    return dataDictionary;
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Couldn't connect to the database", ex);
                }
            }
        }

        private static DataTable GetDataFromDatabase(NpgsqlConnection conn)
        {
            string query = "SELECT id, agua, carbono FROM environmental_data";
            NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, conn);
            DataTable dataTable = new DataTable();
            dataAdapter.Fill(dataTable);
            return dataTable;
        }
    }
}
