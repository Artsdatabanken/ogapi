using Newtonsoft.Json;
using NinMemApi.Data.Interfaces;
using NinMemApi.Data.Models;
using NinMemApi.Data.Stores;
using NinMemApi.Data.Stores.Azure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.Data
{
    public class GraphInputGetter
    {
        private readonly IStorage _storage;

        public GraphInputGetter(IStorage storage)
        {
            _storage = storage;
        }

        public async Task<GraphInput> Get()
        {
            var natureAreasTask = _storage.Get<List<NatureAreaDto>>(StorageKeys.NatureAreas, containerName: StorageConstants.GraphContainerName);
            var natureAreaRedlistCategoriesTask = _storage.Get<List<RedlistCategory>>(StorageKeys.NatureAreaRedlistCategories, containerName: StorageConstants.GraphContainerName);
            var natureAreaRedlistThemesTask = _storage.Get<List<RedlistTheme>>(StorageKeys.NatureAreaRedlistThemes, containerName: StorageConstants.GraphContainerName);
            var natureAreaGeographicalAreaDataTask = _storage.Get<GeographicalAreaData>(StorageKeys.NatureAreaGeographicalAreaData, containerName: StorageConstants.GraphContainerName);
            var taxonsTask = _storage.Get<List<Taxon>>(StorageKeys.Taxons, containerName: StorageConstants.GraphContainerName);
            var codeTreeTask = _storage.Get<string>(StorageKeys.CodeTree, containerName: StorageConstants.GraphContainerName);
            var natureAreaVariablesTask = _storage.Get<List<NatureAreaVariables>>(StorageKeys.NatureAreaVariables, containerName: StorageConstants.GraphContainerName);
            var taxonTraitsTask = _storage.Get<string>(StorageKeys.TaxonTraits, containerName: StorageConstants.GraphContainerName);

            await Task.WhenAll(
                natureAreasTask,
                natureAreaRedlistCategoriesTask,
                natureAreaRedlistThemesTask,
                natureAreaGeographicalAreaDataTask,
                taxonsTask,
                codeTreeTask,
                natureAreaVariablesTask,
                taxonTraitsTask);

            return new GraphInput
            {
                NatureAreas = natureAreasTask.Result,
                NatureAreaRedlistCategories = natureAreaRedlistCategoriesTask.Result,
                NatureAreaRedlistThemes = natureAreaRedlistThemesTask.Result,
                NatureAreaGeographicalAreaData = natureAreaGeographicalAreaDataTask.Result,
                Taxons = taxonsTask.Result,
                CodeTree = CodeTreeBuilder.Build(codeTreeTask.Result),
                NatureAreaVariables = natureAreaVariablesTask.Result,
                TaxonTraits = JsonConvert.DeserializeObject<List<TaxonTraits>>(taxonTraitsTask.Result)
            };
        }
    }
}