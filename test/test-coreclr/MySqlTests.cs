using NUnit.Framework;

namespace test;

[TestFixture]
public class MySqlTests
{
    private readonly SqlTests _tests = new();
    private readonly IDictionary<string, object> _parameters = new Dictionary<string, object>
    {
        { "connectionString", Environment.GetEnvironmentVariable("MYSQL")},
        { "commandTimeout", 30 },
        { "db", "mysql" },
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