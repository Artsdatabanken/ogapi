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

            var ninConnectionString = configuration.GetConnectionString("Nin");
            var urlAlleKoder = configuration["UrlAlleKoder"];
            var urlVariasjoner = configuration["UrlVariasjon"];

            StoreData(ninConnectionString, urlAlleKoder, urlVariasjoner).Wait();

            Console.WriteLine("Done ...");
            Console.ReadKey();
        }

        private static async Task StoreData(string ninConnectionString, string urlAlleKoder, string urlVariasjoner)
        {
            var koder = GetKoder(urlAlleKoder, urlVariasjoner);

            var natureAreas = await DataLoaders.NatureAreas.NatureAreaLoader.Load(ninConnectionString);
            var descriptionVariables = await DescriptionVariableLoader.Load(ninConnectionString, Codes.Create(koder.variasjonKoder));
            var natureAreaTypes = await NatureAreaTypeLoader.Load(ninConnectionString, Codes.Create(koder.alleKoder));
            var redlistData = await RedlistLoader.Load(ninConnectionString);
            var geographicalData = await GeographicalAreaLoader.Load(ninConnectionString);
            //var taxons = TaxonLoader.Get();
            //var codetree = File.ReadAllText("..\\..\\..\\Data\\kodetre.json");
            var natureAreaVariables = await NatureAreaVariablesLoader.Load(ninConnectionString);
            //var taxonTraits = File.ReadAllText("..\\..\\..\\Data\\taxonTraits.json");

            await Task.WhenAll(
                FileSaver.Store(StorageKeys.NatureAreas, natureAreas),
                FileSaver.Store(StorageKeys.NatureAreaDescriptionVariables, descriptionVariables),
                FileSaver.Store(StorageKeys.NatureAreaTypes, natureAreaTypes),
                FileSaver.Store(StorageKeys.NatureAreaRedlistCategories, redlistData.categories),
                FileSaver.Store(StorageKeys.NatureAreaRedlistThemes, redlistData.themes),
                FileSaver.Store(StorageKeys.NatureAreaGeographicalAreaData, geographicalData),
                //FileSaver.Store(StorageKeys.Taxons, taxons.Values.ToList()),
                //FileSaver.Store(StorageKeys.CodeTree, codetree),
                FileSaver.Store(StorageKeys.NatureAreaVariables, natureAreaVariables)
                //FileSaver.Store(StorageKeys.TaxonTraits, taxonTraits)
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

    internal class FileSaver
    {
        private static readonly string CacheFolder = Path.GetTempPath();

        public static async Task Store<T>(string key, T value)
        {
            var file = File.Create(GetFileName(key));
            var fileWriter = new StreamWriter(file);

            var json = typeof(T) == typeof(string) ? value as string : JsonConvert.SerializeObject(value);

            fileWriter.WriteLine(json);
            fileWriter.Dispose();
        }

        public async Task<T> Get<T>(string key)
        {
            string json = null;
            var filePath = string.IsNullOrWhiteSpace(CacheFolder) ? null : GetFileName(key);

            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                json = File.ReadAllText(filePath);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)json;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        private static string GetFileName(string key)
        {
            return Path.Combine(CacheFolder, key + ".json");
        }
    }
}