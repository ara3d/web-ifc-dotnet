using WebIfcClrWrapper;

namespace WebIfcDotNetTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var r = new ThisIsAManagedClass();
            Console.WriteLine(r.MyString);
        }

        [Test]
        public void Test2()
        {
            var api = new DotNetApi();
            var id = api.OpenModel("C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\examples\\example.ifc");
            Console.WriteLine($"Id = {id}");
            var size = api.GetModelSize((uint)id);
            Console.WriteLine($"Model size = {size}");
            var lines = new List<uint>();
            api.GetAllLines( (uint)id, lines );
            Console.WriteLine($"Found {lines.Count} lines");
        }

        [Test]
        public void Test3()
        {
            var api = new DotNetApi();
            var modelId = api.OpenModel("C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\examples\\example.ifc");
            var lines = new List<uint>();
            api.GetAllLines((uint)modelId, lines);
            for (var i = 0; i < lines.Count && i < 100; i++)
            {
                var lt = api.GetLineType((uint)modelId, lines[i]);
                Console.WriteLine($"Line {i}, id = {lines[i]}, type = {lt}");
            }
        }
    }
} 