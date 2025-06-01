using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using DefaultNamespace;

[assembly: InternalsVisibleTo("test-coreclr")]
public class EdgeCompiler
{
    private enum QueryType{
        Select,
        NonQuery,
        Proc,
        Other
    }
    
    private enum Db{
        MsSql,
        MySql,
        PgSql,
        Oracle
    }

    internal IDictionary<string, object> Deserialize(string json)
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
        Db db = Db.MsSql;
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
                if (Enum.TryParse(dbTmp.ToString(), true, out Db dbEnum))
                {
                    db = dbEnum;
                }
                else
                {
                    throw new ArgumentException($"'db' parameter value must be one of the following: {string.Join(",", Enum.GetNames(typeof(Db)))}.\nParameter is not case sensitive.");
                }
            }
        }

        if (db == Db.MySql)
        {
            query = new MySqlQuery();
        }
        else if (db == Db.PgSql)
        {
            query = new PgSqlQuery();
        }

        if (command.StartsWith("select ", StringComparison.InvariantCultureIgnoreCase))
        {
            queryType = QueryType.Select;
            return async (queryParameters) => await 
                query.ExecuteQuery(
                connectionString, 
                command, 
                queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters,
                commandTimeout,
                db == Db.PgSql || db == Db.Oracle ? nonQuery : false);
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

            if (db == Db.PgSql)
            {
                return async (queryParameters) => await 
                    query.ExecuteQuery(
                        connectionString, 
                        command, 
                        queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters,
                        commandTimeout,
                        nonQuery);
            }

            queryType = QueryType.Proc;
            return async (queryParameters) => await
                query.ExecuteStoredProcedure(
                    connectionString,
                    command.Substring(trim).TrimEnd(),
                    queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters,
                    commandTimeout,
                    nonQuery);
        }

        queryType = QueryType.Other;
        if (nonQuery)
        {
            return async (queryParameters) => await 
                query.ExecuteNonQuery(
                    connectionString, 
                    command, 
                    queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters, 
                    commandTimeout);
        }

        return async (queryParameters) => await 
            query.ExecuteQuery(
                connectionString, 
                command, 
                queryParameters is string ? Deserialize(queryParameters.ToString()) : (IDictionary<string, object>)queryParameters, 
                commandTimeout,
                nonQuery);
    }
}
