using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

public class EdgeCompiler
{
    public Func<object, Task<object>> CompileFunc(IDictionary<string, object> parameters)
    {
        string command = ((string)parameters["source"]).TrimStart();
        string connectionString = Environment.GetEnvironmentVariable("EDGE_SQL_CONNECTION_STRING");
        int? commandTimeout = null;

        if (parameters.TryGetValue("connectionString", out var connectionStringTmp))
        {
            connectionString = (string)connectionStringTmp;
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("connectionString must be specified using EDGE_SQL_CONNECTION_STRING environmental variable or passed as parameter.");
        }

        if (parameters.TryGetValue("commandTimeout", out var commandTimeoutTmp))
        {
            commandTimeout = (int)commandTimeoutTmp;
        }

        if (command.StartsWith("select ", StringComparison.InvariantCultureIgnoreCase))
        {
            return async (queryParameters) => await 
            ExecuteQuery(
                connectionString, 
                command, 
                (IDictionary<string, object>)queryParameters, 
                commandTimeout);
        }
        if (command.StartsWith("insert ", StringComparison.InvariantCultureIgnoreCase)
            || command.StartsWith("update ", StringComparison.InvariantCultureIgnoreCase)
            || command.StartsWith("delete ", StringComparison.InvariantCultureIgnoreCase))
        {
            return async (queryParameters) => await 
            ExecuteNonQuery(
                connectionString, 
                command, (IDictionary<string, object>)queryParameters, 
                commandTimeout);
        }
        if (command.StartsWith("exec ", StringComparison.InvariantCultureIgnoreCase) || command.StartsWith("execute ", StringComparison.InvariantCultureIgnoreCase))
        {
            return async (queryParameters) => await
                ExecuteStoredProcedure(
                    connectionString,
                    command,
                    (IDictionary<string, object>)queryParameters,
                    commandTimeout);
        }
        // Use ExecuteQuery for any other SQL commands 
        return async (queryParameters) => await 
            ExecuteQuery(
                connectionString, 
                command, 
                (IDictionary<string, object>)queryParameters, 
                commandTimeout);
    }

    void AddParameters(SqlCommand command, IDictionary<string, object> parameters)
    {
        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Key.StartsWith("@"))
                {
                    var returnParameterName = "@" + parameter.Value.ToString().TrimStart('@');
                    command.Parameters.Add(returnParameterName, SqlDbType.NVarChar, -1);
                    command.Parameters[returnParameterName].Direction = ParameterDirection.Output;
                }
                else
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
            }
        }
    }

    async Task<object> ExecuteQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            using (var command = new SqlCommand(commandString, connection))
            {
                if (commandTimeout.HasValue) command.CommandTimeout = commandTimeout.Value;
                return await ExecuteQuery(parameters, command, connection);
            }
        }
    }

    async Task<object> ExecuteQuery(IDictionary<string, object> parameters, SqlCommand command, SqlConnection connection)
    {
        AddParameters(command, parameters);
        var results = new Dictionary<string, object>();
        await connection.OpenAsync();
        var resultCount = new Dictionary<string, int>();
        using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.KeyInfo))
        {
            do
            {
                var tableRows = reader.GetSchemaTable()?.Rows;
                string? table = string.Empty;
                if (tableRows?.Count != 0)
                {
                    table = tableRows?[0]["BaseTableName"]?.ToString();
                }

                var resultName = string.IsNullOrEmpty(table) ? "result" : table;
                if (!resultCount.ContainsKey(resultName))
                {
                    resultCount.Add(resultName, 0);
                }

                if (results.ContainsKey(resultName))
                {
                    resultCount[resultName]++;
                }
                
                if (results.ContainsKey(resultName))
                {
                    resultName = $"{resultName}-{resultCount[resultName]}";
                }
                
                var rows = new List<object>();
                IDataRecord record = reader;
                while (await reader.ReadAsync())
                {
                    var dataObject = new ExpandoObject() as IDictionary<string, object>;
                    var resultRecord = new object[record.FieldCount];
                    record.GetValues(resultRecord);

                    for (int i = 0; i < record.FieldCount; i++)
                    {      
                        Type type = record.GetFieldType(i);
                        if (resultRecord[i] is DBNull)
                        {
                            resultRecord[i] = null;
                        }
                        else if (type == typeof(short) || type == typeof(ushort)) {
                            resultRecord[i] = Convert.ToInt32(resultRecord[i]);
                        }
                        else if (type == typeof(Decimal)) {
                            resultRecord[i] = Convert.ToDouble(resultRecord[i]);
                        }
                        else if (type == typeof(byte[]) || type == typeof(char[]))
                        {
                            resultRecord[i] = Convert.ToBase64String((byte[])resultRecord[i]);
                        }
                        else if (type == typeof(Guid) || type == typeof(DateTime))
                        {
                            resultRecord[i] = resultRecord[i].ToString();
                        }
                        else if (type == typeof(IDataReader))
                        {
                            resultRecord[i] = "<IDataReader>";
                        }

                        dataObject.Add(record.GetName(i), resultRecord[i]);
                    }

                    rows.Add(dataObject);
                }
                results.Add(resultName, rows);
            } while (await reader.NextResultAsync());

            return results.Keys.Count == 1 ? results[results.Keys.First()] : results;
        }
    }

    async Task<object> ExecuteNonQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            using (var command = new SqlCommand(commandString, connection))
            {
                if (commandTimeout.HasValue) command.CommandTimeout = commandTimeout.Value;
                AddParameters(command, parameters);
                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync();
            }
        }
    }

    async Task<object> ExecuteStoredProcedure(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            var trim = commandString.StartsWith("execute ", StringComparison.InvariantCultureIgnoreCase) ? 8 : 5;
            
            SqlCommand command = new SqlCommand(commandString.Substring(trim).TrimEnd(), connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            if (commandTimeout.HasValue) command.CommandTimeout = commandTimeout.Value;

            if (parameters != null && parameters.Keys.Any(v => v.StartsWith("@")))
            {
                IDictionary<string, string> results = new Dictionary<string, string>();
                AddParameters(command, parameters);
                using (command)
                {
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    connection.Close();
                    foreach (var key in parameters.Keys.Where(v =>v.ToString().StartsWith("@")))
                    {
                        var returnParameterName = "@" + parameters[key].ToString().TrimStart('@');
                        results[parameters[key].ToString()] = command.Parameters[returnParameterName].Value.ToString();
                    }

                    return results;
                }
            }
            else
            {
                using (command)
                {
                    return await ExecuteQuery(parameters, command, connection);
                }
            }
            
        }
    }
}
