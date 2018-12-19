using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace NinMemApi.DataPreprocessing.DataLoaders.Taxons
{
    public static class DiagnosticTaxonLoader
    {
        public static DiagnosticTaxon[] Load(string url = "https://raw.githubusercontent.com/Artsdatabanken/kverna/master/kildedata/na_diagnostisk_art.json")
        {
            var jsonString = new WebClient().DownloadString(url);

            var json = JsonConvert.DeserializeObject<List<DiagnosticTaxonInput>>(jsonString);
            
            return json
                .Select(line => new DiagnosticTaxon
                {
                    NatureAreaType = "NA-" + line.Kartleggingsenhet,
                    ScientificNameId = line.ScientificNameId
                })
                .ToArray();
        }
    }

    public class DiagnosticTaxonInput
    {
        public string Kartleggingsenhet { get; set; }
        public string ScientificNameId { get; set; }
    }

    public class DiagnosticTaxon
    {
        public string NatureAreaType { get; set; }
        public string ScientificNameId { get; set; }
    }
}