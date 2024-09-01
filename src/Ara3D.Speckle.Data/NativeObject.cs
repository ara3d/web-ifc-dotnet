using System.Collections.Generic;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;

namespace Ara3D.Speckle.Data
{
    public class NativeObject
    {
        public string Id { get; set; }
        public string CollectionType { get; set; }
        public string Name { get; set; }
        public string SpeckleType { get; set; }
        public string DotNetType => Base?.GetType().Name;
        public Transform Transform { get; set; }
        public Base Base { get; set; }
        public Dictionary<string, object> Members { get; set; } = new Dictionary<string, object>();
        public NativeObject InstanceDefinition { get; set; }
        public List<NativeObject> Children { get; } = new List<NativeObject>();
        public List<NativeObject> Instances { get; set; } = new List<NativeObject>();
        public Point BasePoint { get; set; }
        public bool IsInstanced { get; set; }
        public NativeObject AddChild(NativeObject child)
        {
            Children.Add(child);
            return child;
        }
        
        /// <summary>
        /// When a tree is marked as "instanced" we don't draw the mesh normally.
        /// </summary>
        public void SetTreeInstanced()
        {
            IsInstanced = true;
            foreach (var child in Children)
                child.SetTreeInstanced();
        }
    }
}