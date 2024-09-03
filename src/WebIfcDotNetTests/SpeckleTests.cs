using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Ara3D.Speckle.Data;
using Ara3D.Utils;
using Speckle.Core.Models;
using Ara3D.Logging;
using WebIfcClrWrapper;
using WebIfcDotNet;

namespace WebIfcDotNetTests
{
    public static class SpeckleTests
    {
        [Test]
        public static void LoadSpeckleObjectToJson()
        {
            // https://app.speckle.systems/projects/68da6db112/models/c78d273327@6e1954cfca

            var accounts = AccountManager.GetAccounts();
            foreach (var account in accounts)
                Console.WriteLine($"Account: {account.serverInfo.url} {account.userInfo.email}");

            Console.WriteLine($"Getting default account for this machine");
            var defaultAccount = AccountManager.GetDefaultAccount();
            if (defaultAccount == null)
                throw new Exception(
                    "Could not find a default account. You may need to install and run the Speckle Manager");

            Console.WriteLine($"Authenticating with this accoutn");
            using var client = new Client(defaultAccount);

            Console.WriteLine($"Getting the main branch and retrieving a model");
            var projectId = "68da6db112";
            var modelId = "c78d273327";
            var model = client.Model.Get(modelId, projectId).Result;
            Console.WriteLine($"Retrieved model {model.name}:{model.id}");

            // Create the server transport for the specified stream.
            var transport = new ServerTransport(defaultAccount, projectId);
            Console.WriteLine($"Created transport {transport.BaseUri}");

            // Receive the object

            var versionList = client.Version.GetVersions(modelId, projectId, 1).Result;
            var firstVersion = versionList.items.FirstOrDefault()?.id;
            if (firstVersion == null)
                throw new Exception("No versions found for this model");
            Console.WriteLine($"Found version {firstVersion}");

            var objectId = client.Version.Get(firstVersion, modelId, projectId).Result.referencedObject;
            Console.WriteLine($"Object ID: {objectId}");

            Console.WriteLine($"Receiving object: {objectId}");
            var baseRoot = Operations.Receive(objectId, transport).Result;
            Console.WriteLine($"Receipt successful: {baseRoot.id}");

            var convertedRoot = baseRoot.ToSpeckleObject();

            var tmp = PathUtil.CreateTempFile("json");
            var json = convertedRoot.ToJson();
            tmp.WriteAllText(json);
            Console.WriteLine($"Wrote json to: {tmp}");
            ProcessUtil.OpenFile(tmp);
        }

        public static void WriteToSqlDatabase(Base root, FilePath fp)
        {
            // Write to a local database 
            var tmp = Path.GetTempPath();
            var localSql = new SQLiteTransport(tmp);
            Operations.Send(root, new[] { localSql });
        }

        public static Base IfcFileToBase(FilePath fp)
        {
            var api = new DotNetApi();
            var logger = new Logger(LogWriter.ConsoleWriter, "");
            var g = ModelGraph.Load(api, logger, fp);
            return g.ToSpeckle();
        }

        [Test]
        public static void IfcFileToSpeckleToJson()
        {
            var f = "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc";
            var b = IfcFileToBase(f);

            var convertedRoot = b.ToSpeckleObject();

            var tmp = PathUtil.CreateTempFile("json");
            var json = convertedRoot.ToJson();
            tmp.WriteAllText(json);
            Console.WriteLine($"Wrote json to: {tmp}");
            ProcessUtil.OpenFile(tmp);
        }

        public static string ToJson(Base speckleBase)
            => speckleBase.ToSpeckleObject().ToJson();
    }
}
