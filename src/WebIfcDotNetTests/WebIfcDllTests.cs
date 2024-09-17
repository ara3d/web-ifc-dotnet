using Ara3D.Logging;
using WebIfcClrWrapper;

namespace WebIfcDotNetTests;

public static class WebIfcDllTests
{
    public static ILogger CreateLogger()
        => new Logger(LogWriter.ConsoleWriter, "");


    const string inputFile =
        "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc";

    [Test]
    public static void WebIfcTest()
    {
        var logger = CreateLogger();

        logger.Log($"Opening file {inputFile}");
        var api = new DotNetApi();
        var model = api.Load(inputFile);
        logger.Log($"Id = {model.Id}, Size = {model.Size()}");
        var lines = model.GetLineIds();
        logger.Log($"Found {lines.Count} lines");
        var geometries = model.GetGeometries();
        logger.Log($"Found {geometries.Count} geometries");

        logger.Log($"Initializing DLL API");
        var api2 = WebIfcDll.InitializeApi();
        Assert.IsTrue(api2 != IntPtr.Zero);
        logger.Log($"Retrieving model");
        var modelPtr = WebIfcDll.LoadModel(api2, inputFile);
        Assert.NotNull(modelPtr);

        foreach (var g in geometries)
        {
            var g2 = WebIfcDll.GetGeometry(api2, modelPtr, g.ExpressId);
            Assert.NotNull(g2);
        }

        logger.Log("Finished retrieving all geometries from 2nd API");

        WebIfcDll.FinalizeApi(api2);
    }
}