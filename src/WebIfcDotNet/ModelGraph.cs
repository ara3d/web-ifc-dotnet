using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ara3D.Logging;
using WebIfcClrWrapper;

namespace WebIfcDotNet
{
    /// <summary>
    /// This represents the IFC model as a graph of nodes and relations.
    /// It also contains the geometries and the property sets. 
    /// </summary>
    public class ModelGraph
    {
        public Model Model { get; }

        public Dictionary<uint, Geometry> Geometries { get; } 
        public Dictionary<uint, ModelNode> Nodes { get; } = new Dictionary<uint, ModelNode>();
        public Dictionary<uint, ModelRelation> Relations { get; } = new Dictionary<uint, ModelRelation>();
        public Dictionary<uint, ModelPropSets> PropSets { get; } = new Dictionary<uint, ModelPropSets>();
        public Dictionary<uint, List<uint>> NodeIdRelations { get; } = new Dictionary<uint, List<uint>>();

        public static ModelGraph Load(DotNetApi api, ILogger logger, string f)
        {
            logger.Log($"Opening file {f}");

            var model = api.Load(f);
            logger.Log($"Finished loading model {model.Id}");

            var lineIds = model.GetLineIds();
            logger.Log($"Id = {model.Id}, Size = {model.Size()}");

            var max = lineIds.Max(i => i);
            logger.Log($"# line ids = {lineIds.Count}, max = {max}");

            var g = new ModelGraph(model);
            logger.Log($"Created graph, # parts = {g.Nodes.Count}, # props = {g.PropSets.Count}, # of relations = {g.Relations.Count}");

            return g;
        }

        public ModelGraph(Model model)
        {
            Model = model;

            Geometries = model.GetGeometries().ToDictionary(g => g.ExpressId, g => g);

            var relAggregates = model.GetLines("IfcRelAggregates");
            foreach (var agg in relAggregates)
            {
                Debug.Assert(agg.Arguments.Count == 6);
                var relating = GetOrCreateNode(agg, 4);
                var related = GetOrCreateNodes(agg, 5);
                var rel = new ModelRelation(this, agg, relating, related);
                Relations.Add(agg.ExpressId, rel);
            }

            var relContainedInSpatialStructure = model.GetLines("IfcRelContainedInSpatialStructure");
            foreach (var agg in relContainedInSpatialStructure)
            {
                Debug.Assert(agg.Arguments.Count == 6);
                var related = GetOrCreateNodes(agg, 4);
                var structure = GetOrCreateNode(agg, 5);
                var rel = new ModelRelation(this, agg, structure, related);
                Relations.Add(agg.ExpressId, rel);
            }

            foreach (var prop in model.GetLines("IfcPropertySet"))
            {
                if (prop.Arguments.Count != 5)
                    throw new Exception("Expected five arguments");
                var guid = prop.Arguments[0] as string;
                var name = prop.Arguments[2] as string;
                var models = GetOrCreateNodes(prop, 4);
                var props = new ModelPropSets(this, prop, guid, name, models);
                PropSets.Add(props.Id, props);
            }

            foreach (var r in GetRelations())
            {
                if (!NodeIdRelations.ContainsKey(r.Relating.Id))
                    NodeIdRelations[r.Relating.Id] = new List<uint>();
                NodeIdRelations[r.Relating.Id].AddRange(r.Related.Select(x => x.Id));
            }
        }

        public ModelNode GetOrCreateNode(LineData lineData, int arg)
        {
            if (arg < 0 || arg >= lineData.Arguments.Count)
                throw new Exception("Argument index out of range");
            return GetOrCreateNode(lineData.Arguments[arg]);
        }

        public ModelNode GetOrCreateNode(object o)
            => GetOrCreateNode(o is RefValue rv
                ? rv.ExpressId
                : throw new Exception($"Expected a reference value, not {o}"));

        public ModelNode GetOrCreateNode(uint id)
        {
            if (Nodes.TryGetValue(id, out var node))
                return node;
            var lineData = Model.GetLineData(id);
            node = new ModelNode(this, lineData);
            Nodes[id] = node;
            return node;
        }

        public List<ModelNode> GetOrCreateNodes(List<object> list)
            => list.Select(GetOrCreateNode).ToList();

        public List<ModelNode> GetOrCreateNodes(LineData line, int arg)
        {
            if (arg < 0 || arg >= line.Arguments.Count)
                throw new Exception("Argument out of range");
            if (!(line.Arguments[arg] is List<object> list))
                throw new Exception("Expected a list");
            return GetOrCreateNodes(list);
        }

        public ModelNode GetNode(uint id)
            => Nodes[id];

        public IEnumerable<ModelRelation> GetRelations()
            => Relations.Values;

        public IEnumerable<uint> GetRelationSourceIds()
            => GetRelations().Select(r => r.Relating.Id).Distinct();

        public IEnumerable<uint> GetRelationSinkIds()
            => GetRelations().SelectMany(r => r.Related.Select(x => x.Id)).Distinct();

        public IEnumerable<ModelNode> GetSources()
            => GetRelationSourceIds().Except(GetRelationSinkIds()).Select(GetNode);

        public IEnumerable<ModelNode> GetSinks()
            => GetRelationSinkIds().Except(GetRelationSourceIds()).Select(GetNode);

        public IEnumerable<ModelNode> GetRelatedNodes(ModelNode node)
            => GetRelatedNodes(node.Id);

        public IEnumerable<ModelNode> GetRelatedNodes(uint id)
            => NodeIdRelations.ContainsKey(id) 
                ? NodeIdRelations[id].Select(GetNode) 
                : Enumerable.Empty<ModelNode>();
    }

    public class ModelPart
    {
        public LineData LineData { get; }
        public ModelGraph Graph { get; }
        public uint Id => LineData.ExpressId;
        public string Type => LineData.Type;

        public ModelPart(ModelGraph graph, LineData lineData)
        {
            Graph = graph;
            LineData = lineData;
        }

        public override bool Equals(object obj)
        {
            if (obj is ModelPart other)
                return Id == other.Id;
            return false;
        }

        public override int GetHashCode()
            => (int)Id;
    }

    public class ModelRelation : ModelPart
    {
        public ModelNode Relating { get; }
        public IReadOnlyList<ModelNode> Related { get; }

        public ModelRelation(ModelGraph graph, LineData lineData, ModelNode relating, List<ModelNode> related)
            : base(graph, lineData)
        {
            Relating = relating;
            Related = related;
        }
    }

    public class ModelPropSets : ModelPart
    {
        public readonly string Guid;
        public readonly string Name;
        public readonly IReadOnlyList<ModelNode> Properties;

        public ModelPropSets(ModelGraph graph, LineData lineData, string guid, string name, List<ModelNode> properties)
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

        public IEnumerable<ModelNode> GetRelatedNodes()
            => Graph.GetRelatedNodes(this);
    }
}