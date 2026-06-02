using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;

namespace SchemaDiscovery
{
    public class DbNode : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; } = string.Empty;
        public List<DbNode> Children { get; set; } = new List<DbNode>();
        public string Type { get; set; } = string.Empty; // Table, Column, etc.
        
        public bool IsSelected 
        { 
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }
        
        public bool IsSelectable => Type == "Table" || Type == "Column";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SchemaExplorer
    {
        public async Task<List<DbNode>> GetDatabaseSchemaAsync(string connectionString)
        {
            var schema = new List<DbNode>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    string tableQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
                    using (var tableCommand = new SqlCommand(tableQuery, connection))
                    {
                        using (var tableReader = await tableCommand.ExecuteReaderAsync())
                        {
                            var tables = new List<string>();
                            while (await tableReader.ReadAsync())
                            {
                                tables.Add(tableReader.GetString(0));
                            }
                            tableReader.Close();

                            foreach (var tableName in tables)
                            {
                                var tableNode = new DbNode { Name = tableName, Type = "Table" };
                                
                                string columnQuery = $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' ORDER BY ORDINAL_POSITION";
                                using (var columnCommand = new SqlCommand(columnQuery, connection))
                                {
                                    using (var columnReader = await columnCommand.ExecuteReaderAsync())
                                    {
                                        while (await columnReader.ReadAsync())
                                        {
                                            string colName = columnReader.GetString(0);
                                            string dataType = columnReader.GetString(1);
                                            tableNode.Children.Add(new DbNode { Name = $"{colName} ({dataType})", Type = "Column" });
                                        }
                                    }
                                }
                                schema.Add(tableNode);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Schema Error] {ex.Message}");
                throw;
            }
            return schema;
        }

        public async Task<(double Min, double Max)> GetColumnStatsAsync(string connectionString, string tableName, string columnName)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // Basic numeric check - simplified for this implementation
                    string query = $"SELECT MIN([{columnName}]), MAX([{columnName}]) FROM [{tableName}]";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                double min = reader.IsDBNull(0) ? 0 : Convert.ToDouble(reader.GetValue(0));
                                double max = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1));
                                return (min, max);
                            }
                        }
                    }
                }
            }
            catch { }
            return (0, 0);
        }

        public async Task<List<double>> GetColumnDataAsync(string connectionString, string tableName, string columnName, int count = 50)
        {
            var data = new List<double>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // Try to find an ordering column (ID or Timestamp)
                    string orderCol = "1";
                    string findOrderQuery = $@"
                        SELECT TOP 1 COLUMN_NAME 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = '{tableName}' 
                        AND (COLUMN_NAME LIKE '%ID%' OR COLUMN_NAME LIKE '%Time%' OR COLUMN_NAME LIKE '%Date%')
                        ORDER BY ORDINAL_POSITION";
                    
                    using (var cmd = new SqlCommand(findOrderQuery, connection))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) orderCol = $"[{result.ToString()}] DESC";
                    }

                    string query = $"SELECT TOP {count} [{columnName}] FROM [{tableName}] ORDER BY {orderCol}";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    data.Add(Convert.ToDouble(reader.GetValue(0)));
                                }
                            }
                        }
                    }
                    data.Reverse(); // Return in chronological order
                }
            }
            catch { }
            return data;
        }
        public async Task<System.Data.DataTable> GetTableDataAsync(string connectionString, string tableName, int count = 100)
        {
            var dataTable = new System.Data.DataTable();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = $"SELECT TOP {count} * FROM [{tableName}]";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Table Data Error] {ex.Message}");
                throw;
            }
            return dataTable;
        }
    }
}
