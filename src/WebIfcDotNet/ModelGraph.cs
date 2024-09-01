using System;
using System.Collections.Generic;
using System.Linq;
using Ara3D.Logging;
using WebIfcClrWrapper;

namespace WebIfcDotNet
{
    /// <summary>
    /// This is a high-level representation of an IFC model as a graph of nodes and relations.
    /// It also contains the geometries, properties, and property sets. 
    /// Nodes and relations are created on demand. Only a subset of the file is actually converted into the ModelGraph  
    /// </summary>
    public class ModelGraph
    {
        public Model Model { get; }

        public Dictionary<uint, Geometry> Geometries { get; } 
        public Dictionary<uint, ModelNode> TypeDefinitions { get; } = new Dictionary<uint, ModelNode>();
        public Dictionary<uint, ModelNode> TypeInstanceToDefinition { get; } = new Dictionary<uint, ModelNode>();
        public Dictionary<uint, ModelNode> Nodes { get; } = new Dictionary<uint, ModelNode>();
        public Dictionary<uint, ModelRelation> Relations { get; } = new Dictionary<uint, ModelRelation>();
        public Dictionary<uint, List<ModelRelation>> RelationsTo { get; } 
        public Dictionary<uint, List<ModelRelation>> RelationsFrom { get; } 
        public Dictionary<uint, List<ModelPropSet>> ModelNodeToPropSets { get; } = new Dictionary<uint, List<ModelPropSet>>();
        public Dictionary<uint, List<ModelProp>> PropSetToProp { get; } = new Dictionary<uint, List<ModelProp>>();

        public IReadOnlyList<uint> SourceIds { get; }
        public IReadOnlyList<uint> SinkIds { get; }

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
            logger.Log($"Created graph, # parts = {g.Nodes.Count}, # of relations = {g.Relations.Count}");

            return g;
        }

        public ModelGraph(Model model)
        {
            Model = model;

            Geometries = model.GetGeometries().ToDictionary(g => g.ExpressId, g => g);

            // Get all simple properties
            // TODO: get complex properties
            foreach (var prop in model.GetLines("IfcPropertySingleValue"))
            {
                if (prop.Arguments.Count != 4)
                    throw new Exception("Expected four arguments to IfcPropertySingleValue");
                var propName = prop.Arguments[0] as string;
                var propVal = prop.Arguments[2];
                var p = new ModelProp(this, prop, propName, propVal);
                Nodes.Add(p.Id, p);
            }

            // Get all property sets
            // TODO: get all property sets derived from "IfcPropertySet"
            foreach (var propSet in model.GetLines("IfcPropertySet"))
            {
                if (propSet.Arguments.Count != 5)
                    throw new Exception("Expected five arguments to IfcPropertySet");
                var guid = propSet.Arguments[0] as string;
                var name = propSet.Arguments[2] as string;
                var models = GetOrCreateNodes(propSet, 4);
                var props = new ModelPropSet(this, propSet, guid, name, models);
                Nodes.Add(props.Id, props);
            }

            // Get all aggregate relations
            var relAggregates = model.GetLines("IfcRelAggregates");
            foreach (var agg in relAggregates)
            {
                if (agg.Arguments.Count != 6)
                    throw new Exception("Expected 6 arguments to IfcRelAggregates");
                var relating = GetOrCreateNode(agg, 4);
                var related = GetOrCreateNodes(agg, 5);
                var rel = new ModelAggregateRelation(this, agg, relating, related);
                Relations.Add(agg.ExpressId, rel);
            }

            // Get all spatial relations
            var relContainedInSpatialStructure = model.GetLines("IfcRelContainedInSpatialStructure");
            foreach (var agg in relContainedInSpatialStructure)
            {
                if (agg.Arguments.Count != 6)
                    throw new Exception("Expected 6 arguments to IfcRelContainedInSpatialStructure");
                var related = GetOrCreateNodes(agg, 4);
                var structure = GetOrCreateNode(agg, 5);
                var rel = new ModelSpatialRelation(this, agg, structure, related);
                Relations.Add(agg.ExpressId, rel);
            }

            // Get all property set relations (what things have what property sets)
            var relPropSets = model.GetLines("IfcRelDefinesByProperties");
            foreach (var rel in relPropSets)
            {
                if (rel.Arguments.Count != 6)
                    throw new Exception("Expected six arguments to IfcRelDefinesByProperties");
                var relObjects = GetOrCreateNodes(rel, 4);
                var propSet = GetOrCreateNode(rel, 5);
                
                // Some reference nodes might be quantities, and that is a whole other kettle of fish.
                // There are also some specialized property sets, and I am not convinced about how frequently they are used. 
                if (!(propSet is ModelPropSet))
                    continue;
                var r = new ModelPropSetRelation(this, rel, propSet, relObjects);
                Relations.Add(rel.ExpressId, r);
            }

            // Get all "type" relations (what things are of what type)
            var relDefByType = model.GetLines("IfcRelDefinesByType");
            foreach (var rel in relDefByType)
            {
                if (rel.Arguments.Count != 6) 
                    throw new Exception("Expected six arguments to IfcRelDefinesByType");
                var relatedObjects = GetOrCreateNodes(rel, 4);
                var relatedType = GetOrCreateNode(rel, 5);
                var r = new ModelTypeRelation(this, rel, relatedType, relatedObjects);
                TypeDefinitions.Add(relatedType.Id, relatedType);
                foreach (var ro in relatedObjects)
                    TypeInstanceToDefinition.Add(ro.Id, relatedType);
                Relations.Add(rel.ExpressId, r);
            }

            // Associate the relations with the ids they refer to. 
            // These are cache data structures 
            RelationsTo = GetNodes().ToDictionary(n => n.Id, _ => new List<ModelRelation>());
            RelationsFrom = GetNodes().ToDictionary(n => n.Id, _ => new List<ModelRelation>());
            foreach (var r in GetRelations())
            {
                RelationsFrom[r.From.Id].Add(r); 
                foreach (var related in r.To)   
                    RelationsTo[related.Id].Add(r); 
            }

            SourceIds = GetRelations().Select(r => r.From.Id).Distinct().ToList();
            SinkIds = GetRelations().SelectMany(r => r.To.Select(x => x.Id)).Distinct().ToList();
        }

        public IEnumerable<ModelNode> GetNodes()
            => Nodes.Values;

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

        public IEnumerable<ModelNode> GetSources()
            => SourceIds.Select(GetNode);

        public IEnumerable<ModelNode> GetSinks()
            => SinkIds.Select(GetNode);

        public IEnumerable<ModelPropSet> GetPropSets()
            => GetNodes().OfType<ModelPropSet>();

        public IEnumerable<ModelProp> GetProps()
            => GetNodes().OfType<ModelProp>();

        public IEnumerable<ModelSpatialRelation> GetSpatialRelations()
            => GetRelations().OfType<ModelSpatialRelation>();
        
        public IEnumerable<ModelAggregateRelation> GetAggregateRelations()
            => GetRelations().OfType<ModelAggregateRelation>();

        public IEnumerable<ModelNode> GetSpatialNodes()
            => GetSpatialRelations().SelectMany(r => r.GetNodeIds()).Distinct().Select(GetNode);
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

        public override string ToString()
            => $"{Type}#{Id}";
    }

    /// <summary>
    /// Always express a 1-to-many relation
    /// </summary>
    public class ModelRelation : ModelPart
    {
        public ModelNode From { get; }
        public IReadOnlyList<ModelNode> To { get; }

        public ModelRelation(ModelGraph graph, LineData lineData, ModelNode from, List<ModelNode> to)
            : base(graph, lineData)
        {
            From = from;
            To = to;
        }

        public IEnumerable<uint> GetNodeIds()
            => To.Select(t => t.Id).Prepend(From.Id);
    }

    public class ModelPropSetRelation : ModelRelation
    {
        public ModelPropSetRelation(ModelGraph graph, LineData lineData, ModelNode from, List<ModelNode> to)
            : base(graph, lineData, from, to)
        {
            if (!(from is ModelPropSet))
                throw new Exception($"Expected a ModelPropSet not {from}");
        }

        public ModelPropSet GetPropSet()
            => (ModelPropSet)From;
    }

    public class ModelSpatialRelation : ModelRelation
    {
        public ModelSpatialRelation(ModelGraph graph, LineData lineData, ModelNode from, List<ModelNode> to)
            : base(graph, lineData, from, to)
        {
        }
    }

    public class ModelAggregateRelation : ModelRelation
    {
        public ModelAggregateRelation(ModelGraph graph, LineData lineData, ModelNode from, List<ModelNode> to)
            : base(graph, lineData, from, to)
        {
        }
    }

    public class ModelTypeRelation : ModelRelation
    {
        public ModelTypeRelation(ModelGraph graph, LineData lineData, ModelNode from, List<ModelNode> to)
            : base(graph, lineData, from, to)
        {
        }
    }

    public class ModelProp : ModelNode
    {
        public readonly string Name;
        public readonly object Value;

        public ModelProp(ModelGraph graph, LineData lineData, string name, object value)
            : base(graph, lineData)
        {
            Name = name;
            Value = value;
        }
    }

    public class ModelPropSet : ModelNode
    {
        public readonly string Guid;
        public readonly string Name;
        public readonly IReadOnlyList<ModelProp> Properties;

        public ModelPropSet(ModelGraph graph, LineData lineData, string guid, string name, List<ModelNode> properties)
            : base(graph, lineData)
        {
            Guid = guid;
            Name = name;
            if (!properties.All(p => p is ModelProp))
                throw new Exception("Expected all properties to be of type ModelProp");
            Properties = properties.OfType<ModelProp>().ToList();
        }
    }

    public class ModelNode : ModelPart
    {
        public ModelNode(ModelGraph graph, LineData lineData)
            : base(graph, lineData)
        { }

        public bool HasType()
            => GetModelType() != null;

        public bool IsType()
            => Graph.TypeDefinitions.ContainsKey(Id);

        public ModelNode GetModelType()
            => Graph.TypeInstanceToDefinition.TryGetValue(Id, out var typeNode) ? typeNode : null;

        public IEnumerable<ModelRelation> GetRelationsFrom()
            => Graph.RelationsFrom[Id].Select(r => r);

        public IEnumerable<ModelRelation> GetRelationsTo()
            => Graph.RelationsTo[Id].Select(r => r);

        public IEnumerable<ModelPropSet> GetPropSets()
            => GetRelationsTo().OfType<ModelPropSetRelation>().Select(mpr => mpr.GetPropSet());

        public IEnumerable<ModelNode> GetSpatiallyContained()
            => GetRelationsFrom().OfType<ModelSpatialRelation>().SelectMany(r => r.To);

        // Normally we expect only one, but it would be hard to enforce.
        public IEnumerable<ModelNode> GetSpatialContainers()
            => GetRelationsTo().OfType<ModelSpatialRelation>().Select(r => r.From);

        public IEnumerable<ModelNode> GetAggregated()
            => GetRelationsFrom().OfType<ModelAggregateRelation>().SelectMany(r => r.To);

        public IEnumerable<ModelNode> GetAggregateContainers()
            => GetRelationsFrom().OfType<ModelAggregateRelation>().SelectMany(r => r.To);

        public IEnumerable<ModelNode> GetChildren()
            => GetAggregated().Concat(GetSpatiallyContained());
    }
}