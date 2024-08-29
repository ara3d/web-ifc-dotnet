using Ara3D.Logging;
using System.Reflection;
using Speckle.Core.Models;
using WebIfcClrWrapper;
using WebIfcDotNet;

namespace WebIfcDotNetTests;

public static class GraphTests
{
    [Test]
    public static void TestModelGraph()
    {
        var api = new DotNetApi();
        var logger = new Logger(LogWriter.ConsoleWriter, "");
        var f = "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc";
        var g = ModelGraph.Load(api, logger, f);

        var sinks = g.GetSinks().ToList();
        var sources = g.GetSources().ToList();
        logger.Log($"# of sinks = {sinks.Count}, # of sources = {sources.Count}");

        // Outputting the node tree. 
        var visited = new HashSet<ModelNode>();
        foreach (var source in sources)
            OutputNode(source, visited);
    }
    

    public static void OutputNode(this ModelNode n, HashSet<ModelNode> visited = null, string indent = "")
    {
        visited ??= new HashSet<ModelNode>();
        if (!visited.Add(n))
        {
            Console.WriteLine($"{indent}Cycle detected at {n.Id}={n.Type}");
            return;
        }

        var model = n.Graph.Model;
        var hasMesh = n.Graph.Geometries.ContainsKey(n.Id) ? "HAS GEOMETRY" : "";

        Console.WriteLine($"{indent}{n.Id}={n.Type} {hasMesh}");
        foreach (var n2 in n.GetRelatedNodes())
            OutputNode(n2, visited, indent + "  ");
    }
}