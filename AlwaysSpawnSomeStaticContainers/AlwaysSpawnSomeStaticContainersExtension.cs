using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Servers;

namespace AlwaysSpawnSomeStaticContainers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class AlwaysSpawnContainersExtension(ModHelper modHelper, DatabaseServer databaseServer) : IOnLoad
{
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var containerIds = modHelper.GetJsonDataFromFile<MongoId[]>(pathToMod, "config.json");

        databaseServer
            .GetTables()
            .Locations
            .GetDictionary().Select(e => e.Value).ToList()
            .ForEach(location =>
            {
                if (location.StaticContainers?.Value == null) return;
                if (location.Statics?.ContainersGroups == null) return;

                location.StaticContainers.Value.StaticContainers.ToList().ForEach(staticContainer =>
                {
                    if (staticContainer.Template?.Items == null) return;

                    if (containerIds.Contains(staticContainer.Template.Items.First().Id))
                    {
                        staticContainer.Probability = 1;
                    }
                });

                location.Statics.ContainersGroups.ToList().ForEach(containerGroup =>
                {
                    containerGroup.Value.MaxContainers = 99;
                });
            });

        return Task.CompletedTask;
    }
}