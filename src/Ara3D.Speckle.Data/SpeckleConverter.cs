using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Objects;
using Objects.Geometry;
using Objects.Other;
using Serilog;
using Speckle.Core.Models;

namespace Ara3D.Speckle.Data
{
    public class SpeckleConverter 
    {
        public static async Task<SpeckleConverter> Create(Base value, ILogger logger = null)
        {
            var r = new SpeckleConverter(logger);
            await r.ConvertBase(value);
            return r;
        }

        public SpeckleConverter(ILogger logger)
        {
            Logger = logger ?? new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        public readonly ILogger Logger;

        public readonly Dictionary<string, NativeObject> NativeObjects 
            = new Dictionary<string, NativeObject>();

        public readonly NativeObject Root = new NativeObject()
            { Name = "_root_" };

        public IEnumerable<NativeObject> ConvertObjects(object value, NativeObject parent)
        {
            if (value is Base b)
                yield return ConvertBase(b, parent).Result;

            if (value is IList list)
                foreach (var item in list)
                foreach (var r in ConvertObjects(item, parent))
                    yield return r;

            if (value is IDictionary dict)
                foreach (var item in dict.Values)
                foreach (var r in ConvertObjects(item, parent))
                    yield return r;
        }   

        public async Task<NativeObject> ConvertBase(Base value, NativeObject parent = null)
        {
            if (value == null)
                return null;

            parent = parent ?? Root;

            var id = value.id ?? value.GetId();

            if (NativeObjects.ContainsKey(id))
            {
                var result = NativeObjects[id];
                parent.Children.Add(result);
                return result;
            }

            var r = new NativeObject()
            {
                Id = id,
                Base = value,   
                Members = value.GetMembers(),
                SpeckleType = value.speckle_type,
                Name = value["name"]?.ToString(),
            };

            ///Logger.Write($"Creating object {id} of type {r.SpeckleType} named {r.Name}, this is child {parent.Children.Count}");
            ;
            parent.Children.Add(r);
            NativeObjects.Add(id, r);

            if (value is BlockDefinition block)
            {
                r.BasePoint = block.basePoint;
                foreach (var g in block.geometry)
                    await ConvertBase(g, r);
            }

            if (value is Collection collection)
            {
                r.CollectionType = collection.collectionType;
                r.Name = collection.name;
                foreach (var x in collection.elements)
                    await ConvertBase(x, r);
            }
            
            if (value is Instance instance)
            {
                var def = await ConvertBase(instance.definition);
                r.Transform = instance.transform; 
                r.InstanceDefinition = def;
                def.Instances.Add(r);
                def.SetTreeInstanced();
            }

            if (value is IDisplayValue<List<Mesh>> displayMeshList)
                foreach (var mesh in displayMeshList.displayValue)
                    await ConvertBase(mesh, r);

            foreach (var member in r.Members.Values)
                ConvertObjects(member, r).ToList();

            return r;
        }
    }
}