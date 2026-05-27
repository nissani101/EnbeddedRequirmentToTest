using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ConnectionManager
{
    public class DatabaseManager
    {
        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Error] Connection failed: {ex.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, List<string>>> LoadParametersAsync(string connectionString)
        {
            var result = new Dictionary<string, List<string>>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // Assuming a standard table structure for test parameters
                    string query = "SELECT ParameterName, ParameterValues FROM TestParameters";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string name = reader.GetString(0);
                                string valuesStr = reader.GetString(1);
                                var values = valuesStr.Split(',').Select(v => v.Trim()).ToList();
                                result[name] = values;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Error] Failed to load parameters: {ex.Message}");
                throw;
            }
            return result;
        }
    }
}
