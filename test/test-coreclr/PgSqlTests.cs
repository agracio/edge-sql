using NUnit.Framework;
using test;

[TestFixture]
public class PgSqlTests
{
    private readonly SqlTests _tests = new();
    private readonly IDictionary<string, object> _parameters = new Dictionary<string, object>
    {
        { "connectionString", Environment.GetEnvironmentVariable("PGSQL")},
        { "commandTimeout", 30 },
        { "db", "pgsql" },
    };
    
    [Test]
    public void SelectTop()
    {
        _parameters["source"] = "SELECT * FROM Authors limit 2";
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
        _parameters["source"] = "select * from Authors limit 1; select * from Books limit 1";
        _tests.SelectMultipleTables(_parameters);
    }
    
    [Test]
    public void SelectGeometry()
    {
        _parameters["source"] = "select id, ST_AsText(GeomCol) as GeomCol, GeomColSTA from SpatialTable limit 2";
        _tests.SelectGeometry(_parameters);
    }
    
    [Test]
    public void Function()
    {
        _parameters["source"] = "select * from GetBooksByAuthor(@authorId)";
        _tests.Function(_parameters);
    }
    
    [Test]
    public void ProcOut()
    {
        _parameters["source"] = "call GetAuthorDetails(@authorId, @returnParam1, @returnParam2)";
        _tests.ProcPgSqlOut(_parameters);
    }
}