using NUnit.Framework;

[TestFixture]
public class EdgeCompilerTests
{
    [Test]
    public void Deserialize()
    {
        var dictionary = new Dictionary<string, object>()
        {
            { "name", "test-coreclr" },
            { "type", "string" },
        };
        var compiler = new EdgeCompiler();
        var test = "{\"name\":\"test-coreclr\",\"type\":\"string\"}";
        var deserialized = compiler.Deserialize(test);
        Assert.That(dictionary, Is.EqualTo(deserialized));
    }
}