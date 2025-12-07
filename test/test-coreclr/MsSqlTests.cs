using NUnit.Framework;
using test;

[TestFixture]
public class MsSqlTests
{
    private readonly SqlTests _tests = new();
    private readonly IDictionary<string, object> _parameters = new Dictionary<string, object>()
    {
        { "connectionString", Environment.GetEnvironmentVariable("MSSQL")},
        { "commandTimeout", 30 },
        { "db", "mssql" },
    };
    
    [Test]
    public void SelectTop()
    {
        _parameters["source"] = "SELECT top 2 * FROM Authors";
        _tests.SelectTop(_parameters);
    }
    
    [Test]
    public void SelectById()
    {
        _parameters["source"] = "select * from Authors where Id = @authorId";
        _tests.SelectById(_parameters);
    }
    
    [Test]
    public void SelectMultipleTables()
    {
        _parameters["source"] = "select top 1 * from Authors; select top 1 * from Books";
        _tests.SelectMultipleTables(_parameters);
    }
    
    [Test]
    public void SelectGeometry()
    {
        _parameters["source"] = "select top 2 * from SpatialTable";
        _tests.SelectGeometryMsSql(_parameters);
    }
    
    [Test]
    public void Proc()
    {
        _parameters["source"] = "exec GetBooksByAuthor";
        _tests.Proc(_parameters);
    }
    
    [Test]
    public void ProcOut()
    {
        _parameters["source"] = "exec GetAuthorDetails";
        _tests.ProcOut(_parameters);
    }

}