using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NinMemApi.Data;
using NinMemApi.Data.Models;
using NinMemApi.Data.Stores;
using NinMemApi.Data.Stores.Azure;
using NinMemApi.DataPreprocessing.DataLoaders;
using NinMemApi.DataPreprocessing.DataLoaders.Taxons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NinMemApi.DataPreprocessing
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            string ninConnectionString = configuration.GetConnectionString("Nin");
            string artsdbconnectionString = configuration.GetConnectionString("artsdbstorage");
            string urlAlleKoder = configuration["UrlAlleKoder"];
            string urlVariasjoner = configuration["UrlVariasjon"];
            string cosmosDbHost = configuration["CosmosDbHost"];
            string cosmosDbAuthKey = configuration["CosmosDbAuthKey"];

            //PopulateCosmos(artsdbconnectionString, cosmosDbHost, cosmosDbAuthKey).Wait();
            UploadDataToAzure(ninConnectionString, artsdbconnectionString, urlAlleKoder, urlVariasjoner).Wait();

            Console.WriteLine("Done ...");
            Console.ReadKey();
        }

        private static async Task PopulateCosmos(string artsdbConnectionString, string host, string authKey)
        {
            var azureStorage = new AzureStorage(new ArtsdbStorageConnectionString { ConnectionString = artsdbConnectionString }, cacheFolder: "Cache");

            var graphInputGetter = new GraphInputGetter(azureStorage);
            GraphInput input = await graphInputGetter.Get();

            //var cosmosImporter = new CosmosImporter(host, authKey);

            var cosmosImporter = new TaxonTraitsCosmosImporter(host, authKey);

            await cosmosImporter.Import(input);
        }

        private static async Task UploadDataToAzure(string ninConnectionString, string artsdbconnectionString, string urlAlleKoder, string urlVariasjoner)
        {
            var koder = GetKoder(urlAlleKoder, urlVariasjoner);

            var natureAreas = await DataLoaders.NatureAreas.NatureAreaLoader.Load(ninConnectionString);
            var descriptionVariables = await DescriptionVariableLoader.Load(ninConnectionString, Codes.Create(koder.variasjonKoder));
            var natureAreaTypes = await NatureAreaTypeLoader.Load(ninConnectionString, Codes.Create(koder.alleKoder));
            var redlistData = await RedlistLoader.Load(ninConnectionString);
            var geographicalData = await GeographicalAreaLoader.Load(ninConnectionString);
            var taxons = TaxonLoader.Get();
            var codetree = File.ReadAllText("..\\..\\..\\Data\\kodetre.json");
            var natureAreaVariables = await NatureAreaVariablesLoader.Load(ninConnectionString);
            var taxonTraits = File.ReadAllText("..\\..\\..\\Data\\taxonTraits.json");

            var azureStorage = new AzureStorage(new ArtsdbStorageConnectionString { ConnectionString = artsdbconnectionString });

            await Task.WhenAll(
                    azureStorage.Store(StorageKeys.NatureAreas, natureAreas, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.NatureAreaDescriptionVariables, descriptionVariables, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.NatureAreaTypes, natureAreaTypes, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.NatureAreaRedlistCategories, redlistData.categories, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.NatureAreaRedlistThemes, redlistData.themes, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.NatureAreaGeographicalAreaData, geographicalData, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.Taxons, taxons.Values.ToList(), containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.CodeTree, codetree, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.NatureAreaVariables, natureAreaVariables, containerName: StorageConstants.GraphContainerName),
                    azureStorage.Store(StorageKeys.TaxonTraits, taxonTraits, containerName: StorageConstants.GraphContainerName)
                );
        }

        private static (List<KodeInstans> alleKoder, List<KodeInstans> variasjonKoder) GetKoder(string urlAlleKoder, string urlVariasjoner)
        {
            using (var webClient = new WebClient())
            {
                webClient.Encoding = Encoding.UTF8;

                var alleKoder = GetKodeinstanser(urlAlleKoder, webClient);

                var variasjonKoder = GetKodeinstanser(urlVariasjoner, webClient);

                return (alleKoder, variasjonKoder);
            }
        }

        private static List<KodeInstans> GetKodeinstanser(string urlAlleKoder, WebClient webClient)
        {
            string json = webClient.DownloadString(urlAlleKoder);

            var kodeinstanser = JsonConvert.DeserializeObject<List<KodeInstans>>(json);

            foreach (var instans in kodeinstanser)
            {
                instans.Kode.Id = instans.Kode.Id.Replace(" ", "_");
                instans.OverordnetKode.Id = instans.OverordnetKode.Id?.Replace(" ", "_");
            }

            return kodeinstanser;
        }
    }
}