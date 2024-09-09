using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ara3D.Logging;
using Ara3D.Utils;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Ara3D.Speckle.Data
{
    public static class SpeckleUtils
    {
        public static Client LoginDefaultClient(ILogger logger)
        {
            var accounts = AccountManager.GetAccounts();
            foreach (var account in accounts)
                logger?.Log($"Account: {account.serverInfo.url} {account.userInfo.email}");

            logger?.Log($"Getting default account for this machine");
            var defaultAccount = AccountManager.GetDefaultAccount();
            if (defaultAccount == null)
                throw new Exception(
                    "Could not find a default account. You may need to install and run the Speckle Manager");

            logger?.Log($"Authenticating with this account");
            return new Client(defaultAccount);
        }

        public static FilePath WriteToSqlDatabase(this Base root)
            => root.WriteToSqlDatabase(Path.GetTempPath());

        public static FilePath WriteToSqlDatabase(this Base root, FilePath fp)
        {
            var localSql = new SQLiteTransport(fp);
            Operations.Send(root, new[] { localSql });
            return fp;
        }

        public static string ToJson(this Base speckleBase)
            => speckleBase.ToSpeckleObject().ToJson();

        public static Project GetProject(this Client client, string projectId)
            => client.Project.Get(projectId).Result;

        public static IEnumerable<Model> GetModels(this Client client, string projectId)
            => client.Model.GetModels(projectId).Result.items;

        public static Model CreateModel(this Client client, string projectId, string name, ILogger logger)
        {
            var input = new CreateModelInput(name, null, projectId);
            var model = client.Model.Create(input).Result;
            logger?.Log($"Created model {model.name}:{model.id}");
            return model;
        }

        public static Model GetModelOrDefault(this Client client, string projectId, string name)
            => client.Model.GetModels(projectId).Result.items.FirstOrDefault(m => m.name == name);

        public static Model GetModelOrCreate(this Client client, string projectId, string name, ILogger logger)
        {
            var model = client.GetModelOrDefault(projectId, name);
            if (model != null)
            {
                logger?.Log($"Found model {model.name}:{model.id}");
                return model;
            }

            return client.CreateModel(projectId, name, logger);
        }

        public static string PushModel(this Client client, string projectId, string name, Base root, ILogger logger)
        {
            logger?.Log($"Pushing model {name} to project {projectId}");
            var model = client.GetModelOrCreate(projectId, name, logger);
            logger?.Log($"Model Id = {model.id}");

            logger?.Log($"Sending the Base object to a transport and getting the object ID");
            var transport = new ServerTransport(client.Account, projectId);
            var objectId = Operations.Send(root, new List<ITransport> { transport }).Result;
            logger?.Log($"Sent object {objectId}");

            logger?.Log($"Creating a CommitCreateInput object with the required details");
            var commitInput = new CommitCreateInput
            {
                objectId = objectId,
                branchName = "main", // or any other branch name
                message = "Initial commit",
                sourceApplication = "Ara3D"
            };

            logger?.Log($"Creating a commit with the object ID");
            return client.Version.Create(commitInput).Result;
        }
    }
}
