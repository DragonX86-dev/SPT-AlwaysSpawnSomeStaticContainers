using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Servers;

namespace BringBackContainers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class BringBackContainersExtension(ModHelper modHelper, DatabaseServer databaseServer) : IOnLoad
{
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var containerIds = modHelper.GetJsonDataFromFile<MongoId[]>(pathToMod, "config.json");
        var locationsDict = databaseServer.GetTables().Locations.GetDictionary();

        foreach (var location in locationsDict.Select(e => e.Value))
        {
            var containersInGroupCount = new Dictionary<string, int>();
            
            if (location.Statics?.Containers == null) continue;
            if (location.Statics?.ContainersGroups == null) continue;
            if (location.StaticContainers?.Value == null) continue;
            
            foreach (var staticContainer in location.StaticContainers.Value.StaticContainers)
            {
                if (staticContainer.Probability.Equals(1.0f) 
                    || staticContainer.Template?.Id == null || staticContainer.Template?.Items == null
                    || !containerIds.Contains(staticContainer.Template.Items.First().Template))
                {
                    continue;
                }
                
                var groupId = location.Statics.Containers[staticContainer.Template.Id].GroupId!;
                if (!containersInGroupCount.TryAdd(groupId, 1))
                {
                    containersInGroupCount[groupId] += 1;
                }

                staticContainer.Probability = 1;
            }

            foreach (var containersInGroup in containersInGroupCount)
            {
                if (!location.Statics.ContainersGroups.TryGetValue(containersInGroup.Key, out var containersGroup))
                    continue;
                
                containersGroup.MinContainers += containersInGroup.Value;
                containersGroup.MaxContainers += containersInGroup.Value;
            }
        }

        return Task.CompletedTask;
    }
}
