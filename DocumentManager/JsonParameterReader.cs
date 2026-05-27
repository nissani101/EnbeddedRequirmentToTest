using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DocumentManager
{
    public class JsonParameterReader
    {
        public Dictionary<string, List<string>> ReadParametersFromJson(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                // We expect a format like { "Param_1": ["v1", "v2"], ... }
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, options);
                return result ?? new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading JSON parameters: " + ex.Message);
                return new Dictionary<string, List<string>>();
            }
        }
    }
}
