using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WebIfcClrWrapper;

namespace WebIfcDotNet
{
    public static class Extensions
    {
        public static IEnumerable<uint> GetLineIds(this Model model, uint typeId)
            => model.GetLineIds().Where(id => model.GetLineType(id) == typeId);

        public static IEnumerable<LineData> GetLines(this Model model, IEnumerable<uint> ids)
            => ids.Select(model.GetLineData);

        public static IEnumerable<LineData> GetLines(this Model model, uint typeId)
            => model.GetLines(model.GetLineIds(typeId));

        public static IEnumerable<LineData> GetLines(this Model model, string name)
            => model.GetLines(DotNetApi.GetTypeCodeFromName(name.ToUpperInvariant()));

        public static int GetNumVertices(this Mesh mesh)
            => mesh.GetVertexData().Count / 6;

        public static int GetNumFaces(this Mesh mesh)
            => mesh.GetIndexData().Count / 3;
    }
}
