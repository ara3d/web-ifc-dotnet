using System.Collections;
using System.IO.Compression;
using System.Text;
using Ara3D.Logging;
using Ara3D.Utils;
using WebIfcClrWrapper;

namespace WebIfcDotNetTests
{


    public class MainTests
    {
        public static string OutputFolder = "C:\\Users\\cdigg\\git\\3d-format-shootout\\data\\local-untracked";

        public IEnumerable<LineData> GetLines(Model model, string name)
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

        public static unsafe void OutputObj(TransformedMesh tm)
        {
            var id = tm.Mesh.GetExpressId();
            var vertexData = tm.Mesh.GetVertexData();
            var indexData = tm.Mesh.GetIndexData();
            var numVerts = vertexData.Size / 6;
            var numFaces = indexData.Size / 3;

            var filePath = Path.Combine(OutputFolder, $"{id}.obj");

            var vp = (double*)vertexData.DataPtr.ToPointer();
            var fp = (int*)indexData.DataPtr.ToPointer();

            var lines = new List<string>();

            for (var v = 0; v < numVerts; v++)
            {
                var px = vp[v * 6 + 0];
                var py = vp[v * 6 + 1];
                var pz = vp[v * 6 + 2];
                lines.Add($"v {px} {py} {pz}");
            }

            /*
             // NOTE: this would be required if we wanted to add explicit normals
            for (var v = 0; v < numVerts; v++)
            {
                var nx = vp[v * 6 + 3];
                var ny = vp[v * 6 + 4];
                var nz = vp[v * 6 + 5];
                lines.Add($"vn {nx} {ny} {nz}");
            }
            */

            for (var f = 0; f < numFaces; f++)
            {
                var a = fp[f * 3 + 0] + 1;
                var b = fp[f * 3 + 1] + 1;
                var c = fp[f * 3 + 2] + 1;
                lines.Add($"f {a} {b} {c}");
            }

            File.WriteAllLines(filePath, lines);
        }

        public static IEnumerable<FilePath> InputFiles
            => new DirectoryPath(@"C:\Users\cdigg\dev\speckle\private-test-files").GetFiles();

        public static IEnumerable<LineData> GetPropertyLines(Model model)
        {
            foreach (var lineId in model.GetLineIds())
            {
                var type = model.GetLineType(lineId);
                var typeName = DotNetApi.GetNameFromTypeCode(type);
                if (typeName == "IfcPropertySingleValue" || typeName == "IfcPropertyComplexValue" ||
                    typeName == "IfcPropertyEnumeratedValue" || typeName == "IfcRelDefinesByProperty" ||
                    typeName == "IfcPropertySet")
                {
                    yield return model.GetLineData(lineId);
                }
            }
        }

        [Test]
        [TestCaseSource(nameof(InputFiles))]
        public static void AllFilesTest(FilePath f)
        {
            var api = new DotNetApi();
            var logger = new Logger(LogWriter.ConsoleWriter, "");
            logger.Log($"Opening file {f}");

            var model = api.Load(f);
            logger.Log($"Finished loading model {model.Id}");

            /*
            var meshLists = model.GetMeshes();
            logger.Log($"Found {meshLists.Count} mesh lists");
            */

            var propLines = GetPropertyLines(model).ToList();
            logger.Log($"Found {propLines.Count} property lines");
        }

        [Test]
        public static void TestLines()
        {
            var api = new DotNetApi();
            var model = api.Load(
                "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc");

            Console.WriteLine($"Id = {model.Id}, Size = {model.Size()}");
            var lineIds = model.GetLineIds();
            var i = 0;
            foreach (var id in lineIds)
            {
                try
                {
                    var line = model.GetLineData(id);
                    Console.WriteLine($"{i}: {id} = {IfcValToString(line)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{i}: {id} = ERROR: {e.Message}");
                }

                i++;
            }

            api.DisposeAll();
        }

        public static string IfcValToString(object obj)
        {
            switch (obj)
            {
                case List<object> list:
                    return IfcValToString(list);
                case LabelValue lv:
                    return $"{lv.Type}{IfcValToString(lv.Arguments)}";
                case EnumValue ev:
                    return $".{ev.Name ?? "??"}.";
                case RefValue rv:
                    return $"#{rv.ExpressId}";
                case string s:
                    return $"\"{s}\"";
                case null:
                    return "$";
                default:
                    return obj.ToString() ?? "";
            }
        }

        public static string IfcValToString(List<object> args)
        {
            var sb = new StringBuilder();
            var first = true;
            sb.Append('(');
            foreach (var a in args)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append(IfcValToString(a));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public static string IfcValToString(LineData lineData)
        {
            return $"#{lineData.ExpressId}={lineData.Type}({IfcValToString(lineData.Arguments)})";
            //return $"#{lineData.ExpressId}={lineData.Type}({lineData.Arguments?.Count ?? -1})";
        }

        [Test]
        public void MainTest()
        {
            var api = new DotNetApi();

            //var model = api.Load("C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\examples\\example.ifc");
            var model = api.Load(
                "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc");

            Console.WriteLine($"Id = {model.Id}, Size = {model.Size()}");
            var lines = model.GetLineIds();
            Console.WriteLine($"Found {lines.Count} lines");

            /* for (var i = 0; i < lines.Count && i < 100; i++)
            {
                var lt = model.GetLineType((uint)lines[i]);
                var name = api.GetNameFromTypeCode(lt);
                Console.WriteLine($"Line {i}, id = {lines[i]}, type = {lt}, name = {name}");
            } */

            var meshLists = model.GetMeshes();
            foreach (var meshList in meshLists)
            {
                var lineType = model.GetLineType(meshList.ExpressId);
                var typeName = DotNetApi.GetNameFromTypeCode(lineType);

                if (meshList.Meshes.Count == 0)
                    continue;

                Console.WriteLine($"MeshList: {meshList.ExpressId}, {typeName}, # meshes = {meshList.Meshes.Count}");

                foreach (var transformedMesh in meshList.Meshes)
                {
                    var mesh = transformedMesh.Mesh;
                    var vertexData = mesh.GetVertexData();
                    var indexData = mesh.GetIndexData();
                    var numVerts = vertexData.Size / 6;
                    var numFaces = indexData.Size / 3;
                    var meshId = mesh.GetExpressId();
                    var meshType = model.GetLineType(meshId);
                    var meshTypeName = DotNetApi.GetNameFromTypeCode(meshType);
                    Console.WriteLine($"  Mesh: {meshId}, Type {meshTypeName} " +
                                      $"# vertices {numVerts}, " +
                                      $"Vertex data size {vertexData.Size}, " +
                                      $"# faces {numFaces}, " +
                                      $"Index size {indexData.Size}");

                    OutputObj(transformedMesh);
                }
            }
        }
    }
} 