using System.Collections.Generic;
using System.Linq;

namespace NinMemApi.DataPreprocessing.DataLoaders.Taxons
{
    public static class TaxonLoader
    {
        public static Dictionary<int, Data.Models.Taxon> Get(string ninConnectionString)
        {
            var ravenHelper = new RavenHelper();
            var fa3s = ravenHelper.GetFa3s();
            var ravenTaxons = ravenHelper.GetTaxons();

            var taxonSqlData = new ArtSqlHelper(ninConnectionString).Get();
            var taxonDict = new Dictionary<int, Data.Models.Taxon>();

            var taxonIdToScientificNameId = new Dictionary<int, int>();

            var diagnosticTaxons = DiagnosticTaxonLoader.Load();

            foreach (var sqlData in taxonSqlData)
            {
                var taxon = new Data.Models.Taxon
                {
                    ParentScientificNameId = int.Parse(sqlData.ParentScientificNameId.Remove(0,3)),
                    PopularName = sqlData.PopularName,
                    ScientificName = sqlData.ScientificName,
                    ScientificNameId = int.Parse(sqlData.ScientificNameId.Remove(0,3)),
                    TaxonId = int.Parse(sqlData.TaxonId.Remove(0,3)),
                    EastNorths = sqlData.EastNorths.Select(ll => new[] { ll.east, ll.north }).ToArray(),
                    Municipalities = sqlData.Municipalities.ToArray(),
                    ConservationAreas = sqlData.ConservationAreas.ToArray()
                };

                taxonDict.Add(taxon.ScientificNameId, taxon);
                taxonIdToScientificNameId.Add(taxon.TaxonId, taxon.ScientificNameId);
            }

            foreach (var dt in diagnosticTaxons)
            {
                if (!int.TryParse(dt.ScientificNameId, out int scientificNameId) || !taxonDict.ContainsKey(scientificNameId))
                {
                    continue;
                }

                taxonDict[scientificNameId].NatureAreaTypeCodes.Add(dt.NatureAreaType);
            }

            foreach (var taxon in ravenTaxons.Where(t => t.scientificNames.Any(sn => sn.taxonomicStatus == "accepted" && sn.dynamicProperties.Any(dp => dp.Name == "Kategori"))))
            {
                if (!taxonIdToScientificNameId.ContainsKey(taxon.taxonID))
                {
                    continue;
                }

                var acceptedScientificName = taxon.scientificNames.Single(sn => sn.taxonomicStatus == "accepted");

                int maxAar = acceptedScientificName.dynamicProperties.Where(dp => dp.Name == "Kategori").SelectMany(dp => dp.Properties.Where(p => p.Name == "Aar")).Select(p => int.Parse(p.Value)).Max(aar => aar);

                var redlistCategories = acceptedScientificName.dynamicProperties.Where(dp => dp.Name == "Kategori" && dp.Properties.Any(p => p.Name == "Aar" && p.Value == maxAar.ToString())).Select(dp => dp.Value).ToArray();

                taxonDict[taxonIdToScientificNameId[taxon.taxonID]].RedlistCategories = redlistCategories.Distinct().ToArray();
            }

            foreach (var fa3 in fa3s)
            {
                if (!taxonDict.ContainsKey(fa3.EvaluatedScientificNameId))
                {
                    continue;
                }

                var taxon = taxonDict[fa3.EvaluatedScientificNameId];

                if (taxon == null)
                {
                    continue;
                }

                taxon.BlacklistCategory = fa3.RiskAssessment.RiskLevelCode;
                foreach (string naCode in fa3.ImpactedNatureTypes.Select(nt => "NA_" + nt.NiNCode))
                {
                    taxon.NatureAreaTypeCodes.Add(naCode);
                }
            }

            return taxonDict;
        }
    }
}