using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using DefaultNamespace;
using MySql.Data.MySqlClient;

public class EdgeCompiler
{
    private enum QueryType{
        Select,
        NonQuery,
        Proc,
        Other
    }

    private static IDictionary<string, object> Deserialize(string json)
    {
        try
        {
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(json)))
            {

                var serializer = new DataContractJsonSerializer(typeof(IDictionary<string, object>), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true 
                });
                return serializer.ReadObject(stream) as IDictionary<string, object>;
            }
        }
        catch (Exception)
        {
            throw new Exception("Failed to convert string to IDictionary<string, object> ");
        }
    }
    public Func<object, Task<object>> CompileFunc(IDictionary<string, object> parameters)
    {
        QueryType queryType;
        var command = ((string)parameters["source"]).TrimStart();
        var connectionString = Environment.GetEnvironmentVariable("EDGE_SQL_CONNECTION_STRING");
        int? commandTimeout = null;
        string db = "mssql";
        bool nonQuery = false;
        Query query = new MsSqlQuery();

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
            int.TryParse(commandTimeoutTmp.ToString(), out var timeout);
            if (timeout != 0) commandTimeout = timeout;
        }
        
        if (parameters.TryGetValue("nonQuery", out var nonQueryTmp))
        {
            bool.TryParse(nonQueryTmp.ToString(), out var nonQueryOut);
            nonQuery = nonQueryOut;
        }
        
        if (parameters.TryGetValue("db", out var dbTmp))
        {
            if(dbTmp != null)
            {
                db = dbTmp.ToString().ToLower();
            }
            if (db != "mssql" && db != "mysql")
            {
                throw new ArgumentException("db must be either 'mssql' or 'mysql'.");
            }
        }

        if (db == "mysql")
        {
            query = new MySqlQuery();
        }

        if (command.StartsWith("select ", StringComparison.InvariantCultureIgnoreCase))
        {
            queryType = QueryType.Select;
            return async (queryParameters) => await 
                query.ExecuteQuery(
                connectionString, 
                command, 
                queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters, 
                commandTimeout);
        }
        if (command.StartsWith("insert ", StringComparison.InvariantCultureIgnoreCase)
            || command.StartsWith("update ", StringComparison.InvariantCultureIgnoreCase)
            || command.StartsWith("delete ", StringComparison.InvariantCultureIgnoreCase))
        {
            queryType = QueryType.NonQuery;
            return async (queryParameters) => await 
                query.ExecuteNonQuery(
                connectionString, 
                command, 
                queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters, 
                commandTimeout);
        }

        if (command.StartsWith("exec ", StringComparison.InvariantCultureIgnoreCase) || 
            command.StartsWith("execute ", StringComparison.InvariantCultureIgnoreCase) ||
            command.StartsWith("call ", StringComparison.InvariantCultureIgnoreCase))
        {
            var trim = command.StartsWith("execute ", StringComparison.InvariantCultureIgnoreCase) ? 8 : 5;
            queryType = QueryType.Proc;
            return async (queryParameters) => await
                query.ExecuteStoredProcedure(
                    connectionString,
                    command.Substring(trim).TrimEnd(),
                    queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters,
                    nonQuery,
                    commandTimeout);
        }

        queryType = QueryType.Other;
        // Use ExecuteQuery for any other SQL commands 
        return async (queryParameters) => await 
            query.ExecuteQuery(
                connectionString, 
                command, 
                queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters, 
                commandTimeout);
    }
}
