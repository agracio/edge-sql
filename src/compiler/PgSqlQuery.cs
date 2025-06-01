using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace DefaultNamespace;

public class PgSqlQuery: Query
{
    private void AddParameters(NpgsqlCommand command, IDictionary<string, object> parameters)
    {
        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
            }
        }
    }

    public override async Task<object> ExecuteQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null, bool nonQuery = false)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var command = new NpgsqlCommand(commandString, connection);
        // command.AllResultTypesAreUnknown = true;
        using (command)
        {
            AddParameters(command, parameters);
            return nonQuery ? await ExecuteNonQuery(command, connection, commandTimeout) : await ExecuteQuery(command, connection, commandTimeout);
        }
    }

    public override async Task<object> ExecuteNonQuery(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null)
    {
        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand(commandString, connection);
        AddParameters(command, parameters);
        return await ExecuteNonQuery(command, connection, commandTimeout);
    }

    public override async Task<object> ExecuteStoredProcedure(string connectionString, string commandString, IDictionary<string, object> parameters, int? commandTimeout = null, bool nonQuery = false)
    {
        throw new NotImplementedException();
    }
}