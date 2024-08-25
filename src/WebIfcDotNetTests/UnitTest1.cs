using Ara3D.Logging;
using Ara3D.Utils;
using WebIfcClrWrapper;

namespace WebIfcDotNetTests
{
    public class Tests
    {
        public static string OutputFolder = "C:\\Users\\cdigg\\git\\3d-format-shootout\\data\\local-untracked";

        public static unsafe void OutputObj(TransformedMesh tm)
        {
            var id = tm.Mesh.GetExpressID();
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

        [Test]
        [TestCaseSource(nameof(InputFiles))]
        public void AllFilesTest(FilePath f)
        {
            var api = new DotNetApi();
            var logger = new Logger(LogWriter.ConsoleWriter, "");
            logger.Log($"Opening file {f}");
            var model = api.Load(f);
            logger.Log($"Finished loading model {model.Id}");
            var meshLists = model.GetMeshes();
            logger.Log($"Found {meshLists.Count} mesh lists");
        }

        [Test]
        public void MainTest()
        {
            var api = new DotNetApi();

            //var model = api.Load("C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\examples\\example.ifc");
            var model = api.Load("C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc");

            Console.WriteLine($"Id = {model.Id}, Size = {model.Size()}");
            var lines = model.GetAllLines();
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
                var lineType = model.GetLineType(meshList.ExpressID);
                var typeName = api.GetNameFromTypeCode(lineType);

                if (meshList.Meshes.Count == 0)
                    continue;

                Console.WriteLine($"MeshList: {meshList.ExpressID}, {typeName}, # meshes = {meshList.Meshes.Count}");

                foreach (var transformedMesh in meshList.Meshes)
                {
                    var mesh = transformedMesh.Mesh;
                    var vertexData = mesh.GetVertexData();
                    var indexData = mesh.GetIndexData();
                    var numVerts = vertexData.Size / 6;
                    var numFaces = indexData.Size / 3;
                    var meshId = mesh.GetExpressID();
                    var meshType = model.GetLineType(meshId);
                    var meshTypeName = api.GetNameFromTypeCode(meshType);
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