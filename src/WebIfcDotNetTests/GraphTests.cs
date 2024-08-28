using Ara3D.Logging;
using System.Reflection;
using WebIfcClrWrapper;

namespace WebIfcDotNetTests;

public static class GraphTests
{
    [Test]
    public static void TestModelGraph()
    {
        var logger = new Logger(LogWriter.ConsoleWriter, "");
        var f = "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc";

        var api = new DotNetApi();
        logger.Log($"Opening file {f}");

        var model = api.Load(f);
        logger.Log($"Finished loading model {model.Id}");

        var lineIds = model.GetLineIds();
        logger.Log($"Id = {model.Id}, Size = {model.Size()}");

        var max = lineIds.Max(i => i);
        logger.Log($"# line ids = {lineIds.Count}, max = {max}");
        
        var g = new ModelGraph(model);
        logger.Log($"Created graph, # parts = {g.Parts.Count}, # props = {g.Props.Count}");

        var relCount = g.Parts.Values.OfType<ModelRelation>().Count();
        logger.Log($"# of relations = {relCount}");
    }
}