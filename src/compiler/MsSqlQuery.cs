using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DefaultNamespace;

public class MsSqlQuery: Query
{
    private void AddParameters(SqlCommand command, IDictionary<string, object> parameters)
    {
        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Key.StartsWith("@returnParam"))
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

    public override async Task<object> ExecuteQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null, bool nonQuery = false)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(commandString, connection);
        AddParameters(command, parameters);
        return nonQuery ? await ExecuteNonQuery(command, connection, commandTimeout) : await ExecuteQuery(command, connection, commandTimeout);
    }
    
    public override async Task<object> ExecuteNonQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(commandString, connection);
        AddParameters(command, parameters);
        return await ExecuteNonQuery(command, connection, commandTimeout);
    }
    
    public override async Task<object> ExecuteStoredProcedure(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null, bool nonQuery = false)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            var command = new SqlCommand(commandString, connection);
            AddParameters(command, parameters);
    
            if (parameters != null && parameters.Keys.Any(v => v.StartsWith("@returnParam")))
            {
                return await ExecuteStoredProcedureWithReturnParams(command, connection, parameters, commandTimeout);
            }
            using (command)
            {
                return await ExecuteStoredProcedure(command, connection, nonQuery, commandTimeout);
            }
    
        }
    }
}
