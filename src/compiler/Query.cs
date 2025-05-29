using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace DefaultNamespace;

public abstract class Query
{
    public abstract Task<object> ExecuteQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null, bool nonQuery = false);

    public abstract Task<object> ExecuteNonQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null);

    public abstract Task<object> ExecuteStoredProcedure(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null, bool nonQuery = false);
    
    protected async Task<object> ExecuteQuery(DbCommand command, DbConnection connection, int? commandTimeout = null)
    {
        if (commandTimeout.HasValue) command.CommandTimeout = commandTimeout.Value;
        var results = new Dictionary<string, object>();
        await connection.OpenAsync();
        var resultCount = new Dictionary<string, int>();
        using (var reader = await command.ExecuteReaderAsync(CommandBehavior.KeyInfo))
        {
            do
            {
                var tableRows = reader.GetSchemaTable()?.Rows;
                var table = string.Empty;
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
                        else if (type == typeof(Microsoft.SqlServer.Types.SqlGeometry) 
                                 || type == typeof(Microsoft.SqlServer.Types.SqlGeography)  
                                 )
                        {
                            resultRecord[i] = resultRecord[i].ToString();;
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
    
    protected async Task<object> ExecuteNonQuery(DbCommand command, DbConnection connection, int? commandTimeout = null)
    {
        if (commandTimeout.HasValue) command.CommandTimeout = commandTimeout.Value;
        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }
    
    protected async Task<object> ExecuteStoredProcedure(DbCommand command, DbConnection connection, bool nonQuery = false, int? commandTimeout = null)
    {
        command.CommandType = CommandType.StoredProcedure;
        return nonQuery ? await ExecuteNonQuery(command, connection, commandTimeout) : await ExecuteQuery(command, connection, commandTimeout);
    }
    
    protected async Task<object> ExecuteStoredProcedureWithReturnParams(DbCommand command, DbConnection connection, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        command.CommandType = CommandType.StoredProcedure;

        if (commandTimeout.HasValue) command.CommandTimeout = commandTimeout.Value;
        IDictionary<string, string> results = new Dictionary<string, string>();
        using (command)
        {
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            connection.Close();
            foreach (var key in parameters.Keys.Where(v =>v.ToString().StartsWith("@returnParam")))
            {
                var returnParameterName = "@" + parameters[key].ToString().TrimStart('@');
                results[parameters[key].ToString()] = command.Parameters[returnParameterName].Value.ToString();
            }
            return results;
        }

    }
}