using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DefaultNamespace;

public class MySqlQuery: Query
{
    private void AddParameters(MySqlCommand command, IDictionary<string, object> parameters)
    {
        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Key.StartsWith("@returnParam"))
                {
                    var returnParameterName = "@" + parameter.Value.ToString().TrimStart('@');
                    command.Parameters.Add(returnParameterName, MySqlDbType.VarChar, -1);
                    command.Parameters[returnParameterName].Direction = ParameterDirection.Output;
                }
                else
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
            }
        }
    }
    public override async Task<object> ExecuteQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            using (var command = new MySqlCommand(commandString, connection))
            {
                AddParameters(command, parameters);
                return await ExecuteQuery(command, connection, commandTimeout);
            }
        }
    }
    
    public override async Task<object> ExecuteNonQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            using (var command = new MySqlCommand(commandString, connection))
            {
                AddParameters(command, parameters);
                return await ExecuteNonQuery(command, connection, commandTimeout);
            }
        }
    }

    public override async Task<object> ExecuteStoredProcedure(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            var command = new MySqlCommand(commandString, connection);
            AddParameters(command, parameters);

            if (parameters != null && parameters.Keys.Any(v => v.StartsWith("@returnParam")))
            {
                return await ExecuteStoredProcedureWithReturnParams(command, connection, parameters, commandTimeout);
            }
            using (command)
            {
                return await ExecuteStoredProcedure(command, connection, commandTimeout);
            }
        }
    }
}