using Ara3D.Logging;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Other;
using WebIfcClrWrapper;
using WebIfcDotNet;
using Color = System.Drawing.Color;
using Mesh = Objects.Geometry.Mesh;

namespace WebIfcDotNetTests;

public static class SpeckleWriter
{
    [Test]
    public static void ConvertToSpeckle()
    {
        var api = new DotNetApi();
        var logger = new Logger(LogWriter.ConsoleWriter, "");
        var f = "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc";
        var g = ModelGraph.Load(api, logger, f);
        var b = g.ToSpeckle();
    }

    public static Base ToSpeckle(this ModelGraph g)
    {
        var b = new Base();
        AddChildren(b, g.GetSources());
        return b;
    }

    public static void AddChildren(Base b, IEnumerable<ModelNode> nodes)
    {
        var c = new Collection();
        foreach (var n in nodes)
            c.elements.Add(n.ToSpeckle());
        if (c.elements.Count == 0)
            return;
        b["children"] = c;
    }

    public static unsafe Mesh ToSpeckle(this TransformedMesh tm)
    {
        var r = new Mesh();
        var vertexData = tm.Mesh.GetVertexData();
        var indexData = tm.Mesh.GetIndexData();
        var m = tm.Transform;
        var vp = (double*)vertexData.DataPtr.ToPointer();
        var ip = (int*)indexData.DataPtr.ToPointer();
        
        for (var i=0; i < vertexData.Count; i += 6)
        {
            var x = vp[i];
            var y = vp[i + 1];
            var z = vp[i + 2];
            r.vertices.Add(m[0] * x + m[4] * y + m[8] * z + m[12]);
            r.vertices.Add(-(m[2] * x + m[6] * y + m[10] * z + m[14]));
            r.vertices.Add(m[1] * x + m[5] * y + m[9] * z + m[13]);
        }

        for (var i = 0; i < indexData.Count; i += 3)
        {
            var a = ip[i];
            var b = ip[i + 1];
            var c = ip[i + 2];
            r.faces.Add(0);
            r.faces.Add(a);
            r.faces.Add(b);
            r.faces.Add(c);
        }

        var rm = new RenderMaterial();
        rm.diffuseColor = Color.FromArgb((int)(tm.Color.A * 255), (int)(tm.Color.R * 255), (int)(tm.Color.G * 255), (int)(tm.Color.B * 255));
        r["renderMaterial"] = rm;
        return r;
    }

    public static Collection ToSpeckle(this Geometry geometry)
    {
        var c = new Collection();
        foreach (var tm in geometry.Meshes ?? [])
            c.elements.Add(tm.ToSpeckle());
        return c;
    }

    public static Base ToSpeckle(this ModelNode n)
    {
        var b = new Base();
        if (n is ModelPropSet ps)
        {
            b["Name"] = ps.Name;
            b["GlobalId"] = ps.Guid;
        }

        b["type"] = n.Type;
        b["expressID"] = n.Id;

        /* TODO: this is temporarily disabled. 
        if (n.Graph.Geometries.TryGetValue(n.Id, out var m))
        {
            var c = m.ToSpeckle();
            if (c.elements.Count > 0)
                b["@displayValue"] = c;
        }
        */

        // NOTE: there are too many children included! 
        AddChildren(b, n.GetChildren());
        
        return b;
    }
}