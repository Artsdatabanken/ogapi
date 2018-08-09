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
using NinMemApi.Data.Stores.Local;

namespace NinMemApi.DataPreprocessing
{
    public class Program
    {
        private static IConfigurationRoot _configuration;

        private static void Main(string[] args)
        {
            Run();
        }

        public static void Run()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();

            var ninConnectionString = _configuration.GetConnectionString("Nin");
            var urlAlleKoder = _configuration["UrlAlleKoder"];
            var urlVariasjoner = _configuration["UrlVariasjon"];
            var redList = _configuration["RedList"];

            StoreData(ninConnectionString, urlAlleKoder, urlVariasjoner, redList).Wait();
        }

        private static async Task StoreData(string ninConnectionString, string urlAlleKoder, string urlVariasjoner,
            string redList)
        {
            var koder = GetKoder(urlAlleKoder, urlVariasjoner);

            var natureAreas = await DataLoaders.NatureAreas.NatureAreaLoader.Load(ninConnectionString);
            var descriptionVariables = await DescriptionVariableLoader.Load(ninConnectionString, Codes.Create(koder.variasjonKoder));
            var natureAreaTypes = await NatureAreaTypeLoader.Load(ninConnectionString, Codes.Create(koder.alleKoder));
            var redlistData = await RedlistLoader.Load(ninConnectionString, redList);
            var geographicalData = await GeographicalAreaLoader.Load(ninConnectionString);
            var taxons = TaxonLoader.Get();
            var codetree = new WebClient().DownloadString("https://adb-typesystem.surge.sh/kodetre.json");
            var natureAreaVariables = await NatureAreaVariablesLoader.Load(ninConnectionString);
            //// TODO: uncomment this when TaxonTraits is back
            //var taxonTraits = File.ReadAllText("..\\..\\..\\Data\\taxonTraits.json");

            var ninMemApiDataLocation = _configuration["NinMemApiData"];

            if(!Directory.Exists(ninMemApiDataLocation)) throw new DirectoryNotFoundException("Directory given in appsettings.json for \"NinMemApiData\" not found");

            var localStorage = new LocalStorage(ninMemApiDataLocation);

            await Task.WhenAll(
                localStorage.Store(StorageKeys.NatureAreas, natureAreas),
                localStorage.Store(StorageKeys.NatureAreaDescriptionVariables, descriptionVariables),
                localStorage.Store(StorageKeys.NatureAreaTypes, natureAreaTypes),
                localStorage.Store(StorageKeys.NatureAreaRedlistCategories, redlistData.categories),
                localStorage.Store(StorageKeys.NatureAreaRedlistThemes, redlistData.themes),
                localStorage.Store(StorageKeys.NatureAreaGeographicalAreaData, geographicalData),
                localStorage.Store(StorageKeys.Taxons, taxons.Values.ToList()),
                localStorage.Store(StorageKeys.CodeTree, codetree),
                localStorage.Store(StorageKeys.NatureAreaVariables, natureAreaVariables)
            //// TODO: uncomment this when TaxonTraits is back
            //localStorage.Store(StorageKeys.TaxonTraits, taxonTraits)
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
                instans.Kode.Id = instans.Kode.Id.Replace(" ", "_").ToUpper();
                instans.OverordnetKode.Id = instans.OverordnetKode.Id?.Replace(" ", "_").ToUpper();
            }

            return kodeinstanser;
        }
    }
}