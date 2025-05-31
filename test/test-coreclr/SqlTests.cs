using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace test;

public class SqlTests
{
    private readonly EdgeCompiler _compiler = new();
    
    public void SelectTop(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke(null).Result;
        StringAssert.AreEqualIgnoringCase("[{\"Id\":1,\"Name\":\"Author - 1\",\"Country\":\"Country - 1\"},{\"Id\":2,\"Name\":\"Author - 2\",\"Country\":\"Country - 2\"}]", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void SelectById(IDictionary<string, object> parameters, int authorId = 1)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke("{ \"authorId\": " + authorId + " }").Result;
        StringAssert.AreEqualIgnoringCase("[{\"Id\":"+ authorId +",\"Name\":\"Author - "+ authorId +"\",\"Country\":\"Country - "+ authorId +"\"}]", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void SelectMultipleTables(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke(null).Result;
        StringAssert.AreEqualIgnoringCase("{\"Authors\":[{\"Id\":1,\"Name\":\"Author - 1\",\"Country\":\"Country - 1\"}],\"Books\":[{\"Id\":1,\"Author_Id\":1,\"Name\":\"Book - 1\",\"Price\":10}]}", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void SelectGeometryMsSql(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke(null).Result;
        StringAssert.AreEqualIgnoringCase("[{\"id\":1,\"GeomCol\":\"LINESTRING (100 100, 20 180, 180 180)\",\"GeomColSTA\":\"LINESTRING (100 100, 20 180, 180 180)\"},{\"id\":2,\"GeomCol\":\"POLYGON ((0 0, 150 0, 150 150, 0 150, 0 0))\",\"GeomColSTA\":\"POLYGON ((0 0, 150 0, 150 150, 0 150, 0 0))\"}]", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void SelectGeometry(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke(null).Result;
        StringAssert.AreEqualIgnoringCase("[{\"id\":1,\"GeomCol\":\"LINESTRING(100 100,20 180,180 180)\",\"GeomColSTA\":\"LINESTRING(100 100,20 180,180 180)\"},{\"id\":2,\"GeomCol\":\"POLYGON((0 0,150 0,150 150,0 150,0 0))\",\"GeomColSTA\":\"POLYGON((0 0,150 0,150 150,0 150,0 0))\"}]", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void Proc(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke("{ \"authorId\": 2 }").Result;
        StringAssert.AreEqualIgnoringCase("[{\"Id\":3,\"Author_Id\":2,\"Name\":\"Book - 1\",\"Price\":10},{\"Id\":4,\"Author_Id\":2,\"Name\":\"Book - 2\",\"Price\":20}]", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void ProcOut(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke("{ \"authorId\": 1, \"@returnParam1\" : \"AuthorName\", \"@returnParam2\" : \"AuthorCountry\"}").Result;
        StringAssert.AreEqualIgnoringCase("{\"AuthorName\":\"Author - 1\",\"AuthorCountry\":\"Country - 1\"}", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void ProcPgSqlOut(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke("{ \"authorId\": 1, \"@returnParam1\" : \"AuthorName\", \"@returnParam2\" : \"AuthorCountry\"}").Result;
        StringAssert.AreEqualIgnoringCase("[{\"AuthorName\":\"Author - 1\",\"AuthorCountry\":\"Country - 1\"}]", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
    
    public void Function(IDictionary<string, object> parameters)
    {
        var func = _compiler.CompileFunc(parameters);
        var result = func.Invoke("{ \"authorId\": 2 }").Result;
        StringAssert.AreEqualIgnoringCase("[{\"Id\":3,\"Author_Id\":2,\"Name\":\"Book - 1\",\"Price\":10},{\"Id\":4,\"Author_Id\":2,\"Name\":\"Book - 2\",\"Price\":20}]", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
}