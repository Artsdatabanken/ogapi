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
            var taxons = TaxonLoader.Get();
            var codetree = new WebClient().DownloadString("https://adb-typesystem.surge.sh/kodetre.json");
            var natureAreaVariables = await NatureAreaVariablesLoader.Load(ninConnectionString);
            //var taxonTraits = File.ReadAllText("..\\..\\..\\Data\\taxonTraits.json");

            var localStorage = new LocalStorage();

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
                instans.Kode.Id = instans.Kode.Id.Replace(" ", "_");
                instans.OverordnetKode.Id = instans.OverordnetKode.Id?.Replace(" ", "_");
            }

            return kodeinstanser;
        }
    }
}