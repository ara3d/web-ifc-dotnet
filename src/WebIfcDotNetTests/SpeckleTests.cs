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
        public static void LoadSpeckleObject()
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

            var converter = SpeckleConverter.Create(baseRoot).Result;
            var convertedRoot = converter.Root;

            var tmp = PathUtil.CreateTempFile("json");
            var json = convertedRoot.ToJson();
            tmp.WriteAllText(json);
            Console.WriteLine($"Wrote json to: {tmp}");
            ProcessUtil.OpenFile(tmp);

            // NOTE: this was an old way to output the data to console without converting to JSON 
            //OutputNative(convertedRoot);
        }

        public static void OutputNative(NativeObject obj, string indent = "")
        {
            Console.WriteLine($"{indent}{obj.Id}:{obj.Name}:{obj.CollectionType}:{obj.SpeckleType}:{obj.DotNetType}");
            indent += "  ";
            Console.WriteLine($"{indent}CHILDREN:");
            foreach (var child in obj.Children)
                OutputNative(child, indent + "  ");
            Console.WriteLine($"{indent}MEMBERS:");
            foreach (var x in obj.Members)
                Console.WriteLine($"{indent + "  "}{x.Key}={x.Value}");
        }

        [Test]
        public static void LoadSpeckleObject_Deprecated()
        {
            // The id of the stream to work with (we're assuming it already exists in your default account's server)
            //var streamId = "51d8c73c9d";
            //var streamId = "97529188be"; 

            // Advanced Revit Project 
            var streamId = "8f64180899";
            var branchName = "main";

            // Default Speckle architecture 
            //var streamId = "3247bdd4ee"; var branchName = "base design";

            // Get default account on this machine
            // If you don't have Speckle Manager installed download it from https://speckle-releases.netlify.app
            var defaultAccount = AccountManager.GetDefaultAccount();

            // Or get all the accounts and manually choose the one you want
            // var accounts = AccountManager.GetAccounts();
            // var defaultAccount = accounts.ToList().FirstOrDefault();

            if (defaultAccount == null)
                throw new Exception(
                    "Could not find a default account. You may need to install and run the Speckle Manager");

            // Authenticate using the account
            using var client = new Client(defaultAccount);

            // Get the main branch with it's latest commit reference
            var branch = client.BranchGet(streamId, branchName, 1).Result;

            // Get the id of the object referenced in the commit
            var hash = branch.commits.items[0].referencedObject;

            // Create the server transport for the specified stream.
            var transport = new ServerTransport(defaultAccount, streamId);

            // Receive the object
            var root = Operations.Receive(hash, transport).Result;
            Console.WriteLine("Received object:" + root.id);

            var converter = SpeckleConverter.Create(root).Result;
            OutputNative(converter.Root);
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

            var converter = SpeckleConverter.Create(b).Result;
            var convertedRoot = converter.Root;

            var tmp = PathUtil.CreateTempFile("json");
            var json = convertedRoot.ToJson();
            tmp.WriteAllText(json);
            Console.WriteLine($"Wrote json to: {tmp}");
            ProcessUtil.OpenFile(tmp);
        }

        public static string ToJson(Base speckleBase)
            => SpeckleConverter.Create(speckleBase).Result.Root.ToJson();
    }
}
