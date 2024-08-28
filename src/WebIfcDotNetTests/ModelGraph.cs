using Ara3D.Utils;
using System.Diagnostics;
using WebIfcClrWrapper;

namespace WebIfcDotNetTests;

public class ModelGraph
{
    public Model Model { get; }

    public Dictionary<uint, ModelPart> Parts { get; } = new();
    public Dictionary<string, ModelProps> Props { get; } = new();

    public ModelGraph(Model model)
    {
        Model = model;

        // Relations 
        var relAggregates = model.GetLines("IfcRelAggregates");
        foreach (var agg in relAggregates)
        {
            Debug.Assert(agg.Arguments.Count == 6);
            var relating = GetOrCreateNode(agg, 4);
            var related = GetOrCreateNodes(agg, 5);
            var rel = new ModelRelation(this, agg, relating, related);
            Parts.Add(agg.ExpressId, rel);
        }

        var relContainedInSpatialStructure = model.GetLines("IfcRelContainedInSpatialStructure");
        foreach (var agg in relContainedInSpatialStructure)
        {
            Debug.Assert(agg.Arguments.Count == 6);
            var related = GetOrCreateNodes(agg, 4);
            var structure = GetOrCreateNode(agg, 5);
            var rel = new ModelRelation(this, agg, structure, related);
            Parts.Add(agg.ExpressId, rel);
        }

        //var relDefinesByProperties = model.GetLines("IfcRelDefinesByProperties");
        //var relAssociatesMaterial = model.GetLines("IfcRelAssociatesMaterial");
        //var relDefinesByType = model.GetLines("IfcRelDefinesByType");

        // Props
        //foreach (var prop in model.GetLines("IfcComplexProperty"))
        //    Parts.Add(prop.ExpressId, new ModelNode(this, prop));
        //
        //foreach (var prop in model.GetLines("IfcPropertySingleValue"))
        //    Parts.Add(prop.ExpressId, new ModelNode(this, prop));

        // TODO: 
        foreach (var prop in model.GetLines("IfcPropertySet"))
        {
            if (prop.Arguments.Count != 5)
                throw new Exception("Expected five arguments");
            var guid = prop.Arguments[0] as string;
            var name = prop.Arguments[2] as string;
            var models = GetOrCreateNodes(prop, 4);
            var props = new ModelProps(this, prop, guid, name, models);
            Parts.Add(prop.ExpressId, props);
            Props.Add(props.Guid, props);
        }
    }

    public ModelPart GetOrCreateNode(LineData lineData, int arg)
    {
        if (arg < 0 || arg >= lineData.Arguments.Count)
            throw new Exception("Argument index out of range");
        return GetOrCreateNode(lineData.Arguments[arg]);
    }

    public ModelPart GetOrCreateNode(object o)
    {
        if (o is RefValue rv)
            return GetOrCreateNode(rv.ExpressId);
        throw new Exception($"Expected a reference value, not {o}");
    }

    public ModelPart GetOrCreateNode(RefValue rv)
    {
        if (rv == null)
            throw new Exception("Expected a reference value");
        return GetOrCreateNode(rv.ExpressId);
    }

    public ModelPart GetOrCreateNode(uint id)
    {
        if (Parts.TryGetValue(id, out var node))
            return node;
        var lineData = Model.GetLineData(id);
        node = new ModelNode(this, lineData);
        Parts[id] = node;
   
        return node;
    }

    public List<ModelPart> GetOrCreateNodes(List<object> list)
    {
        return list.Select(GetOrCreateNode).ToList();
    }

    public List<ModelPart> GetOrCreateNodes(LineData line, int arg)
    {
        if (arg < 0 || arg >= line.Arguments.Count)
            throw new Exception("Argument out of range");
        if (!(line.Arguments[arg] is List<object> list))
            throw new Exception("Expected a list");
        return GetOrCreateNodes(list);
    }
}

public class ModelPart
{
    public LineData LineData { get; }
    public ModelGraph Graph { get; }

    public ModelPart(ModelGraph graph, LineData lineData)
    {
        Graph = graph;
        LineData = lineData;
    }
}

public class ModelRelation : ModelPart
{
    public ModelPart Relating { get; }
    public IReadOnlyList<ModelPart> Related { get; }

    public ModelRelation(ModelGraph graph, LineData lineData, ModelPart relating, List<ModelPart> related)
        : base(graph, lineData)
    {
        Relating = relating;
        Related = related;
    }
}

public class ModelProps : ModelPart
{
    public readonly string Guid;
    public readonly string Name;
    public readonly IReadOnlyList<ModelPart> Properties;

    public ModelProps(ModelGraph graph, LineData lineData, string guid, string name, List<ModelPart> properties)
        : base(graph, lineData)
    {
        Guid = guid;
        Name = name;
        Properties = properties;
    }
}

public class ModelNode : ModelPart
{
    public ModelNode(ModelGraph graph, LineData lineData)
         : base(graph, lineData)
    {
    }
}

public static class ModelExtensions
{
    public static IEnumerable<LineData> GetLines(this Model model, string name)
    {
        foreach (var lineId in model.GetLineIds())
        {
            var type = model.GetLineType(lineId);
            var typeName = DotNetApi.GetNameFromTypeCode(type);
            if (typeName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                yield return model.GetLineData(lineId);
            }
        }
    }
}