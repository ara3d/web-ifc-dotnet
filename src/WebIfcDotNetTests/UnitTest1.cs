using WebIfcClrWrapper;

namespace WebIfcDotNetTests
{
    public class Tests
    {
        

        [Test]
        public void Test2()
        {
            var api = new DotNetApi();

            //var model = api.Load("C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\examples\\example.ifc");
            var model = api.Load("C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc");

            Console.WriteLine($"Id = {model.Id}, Size = {model.Size()}");
            var lines = model.GetAllLines();
            Console.WriteLine($"Found {lines.Count} lines");

            /*
            for (var i = 0; i < lines.Count && i < 100; i++)
            {
                var lt = model.GetLineType((uint)lines[i]);
                var name = api.GetNameFromTypeCode(lt);
                Console.WriteLine($"Line {i}, id = {lines[i]}, type = {lt}, name = {name}");
            }
            */

            var meshLists = model.GetMeshes();
            foreach (var meshList in meshLists)
            {
                var lineType = model.GetLineType(meshList.ExpressID);
                var typeName = api.GetNameFromTypeCode(lineType);

                if (meshList.Meshes.Count == 0)
                    continue;

                Console.WriteLine($"MeshList: {meshList.ExpressID}, {typeName}, # meshes = {meshList.Meshes.Count}");

                foreach (var mesh in meshList.Meshes)
                {
                    var vertexData = mesh.Mesh.GetVertexData();
                    var indexData = mesh.Mesh.GetIndexData();
                    var numVerts = vertexData.Size / vertexData.ElementSize / 6;
                    var numFaces = indexData.Size / indexData.ElementSize / 3;
                    var meshId = mesh.Mesh.GetExpressID();
                    var meshType = model.GetLineType(meshId);
                    var meshTypeName = api.GetNameFromTypeCode(meshType);
                    Console.WriteLine($"  Mesh: {meshId}, Type {meshTypeName} " +
                                  $"# vertices {numVerts}, " +
                                  $"Vertex data size {vertexData.Size}, " +
                                  $"# faces {numFaces}, " +
                                  $"Index size {indexData.Size}");
                }
            }
        }
    }
} 