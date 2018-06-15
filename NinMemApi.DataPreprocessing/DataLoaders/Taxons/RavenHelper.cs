using Newtonsoft.Json;
using Raven.Client;
using Raven.Client.Document;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NinMemApi.DataPreprocessing.DataLoaders.Taxons
{
    public class RavenHelper
    {
        public RavenHelper()
        {
        }

        public FA3[] GetFa3s(string url = "http://it-webadb03.it.ntnu.no:8181", string database = "FAB3DRIFT")
        {
            const string cacheDirectoryPath = "Data\\FA3s";
            var fa3s = ReadFromCache<FA3>(cacheDirectoryPath);

            if (fa3s != null)
            {
                return fa3s;
            }

            return GetEntities<FA3>(url, database, cacheDirectoryPath: cacheDirectoryPath);
        }

        public Taxon[] GetTaxons(string url = "http://it-webadb03.it.ntnu.no:8181", string database = "Databank1")
        {
            const string cacheDirectoryPath = "Data\\Taxons";
            var taxons = ReadFromCache<Taxon>(cacheDirectoryPath);

            if (taxons != null)
            {
                return taxons;
            }

            return GetEntities<Taxon>(url, database, cacheDirectoryPath: cacheDirectoryPath);
        }

        private static T[] ReadFromCache<T>(string directoryPath)
        {
            var directory = new DirectoryInfo(directoryPath);

            if (directory.Exists)
            {
                var files = directory.GetFiles();

                if (files.Length > 0)
                {
                    var list = new List<T>();

                    foreach (var file in files)
                    {
                        using (var sr = file.OpenText())
                        {
                            list.AddRange(JsonConvert.DeserializeObject<List<T>>(sr.ReadToEnd()));
                        }
                    }

                    return list.ToArray();
                }
            }

            return null;
        }

        private T[] GetEntities<T>(string url, string database, string cacheDirectoryPath = null)
        {
            if(string.IsNullOrWhiteSpace(cacheDirectoryPath)) throw new DirectoryNotFoundException();

            if (!Directory.Exists(cacheDirectoryPath)) Directory.CreateDirectory(cacheDirectoryPath);

            var list = new List<T>();

            using (IDocumentStore store = new DocumentStore
            {
                Url = url,
                DefaultDatabase = database
            })
            {
                store.Initialize();

                IDocumentSession session = store.OpenSession();

                try
                {
                    int start = 0;

                    while (true)
                    {
                        List<T> current = null;

                        try
                        {
                            current = session.Query<T>().Take(1024).Skip(start).ToList();
                        }
                        catch
                        {
                            try
                            {
                                current = session.Query<T>().Take(1024).Skip(start).ToList();
                            }
                            catch
                            {
                            }
                        }

                        if (current.Count == 0)
                        {
                            break;
                        }

                        File.WriteAllText($"{cacheDirectoryPath}\\{start}.json", JsonConvert.SerializeObject(current));

                        start += current.Count;

                        list.AddRange(current);

                        if (start % 5 == 0)
                        {
                            using (session) { }
                            session = store.OpenSession();
                        }
                    }
                }
                finally
                {
                    using (session) { }
                }
            }

            return list.ToArray();
        }
    }

    public class Taxon
    {
        public int taxonID { get; set; }
        public ScientificName[] scientificNames { get; set; }
    }

    public class ScientificName
    {
        public string taxonomicStatus { get; set; }
        public DynamicProperty[] dynamicProperties { get; set; }
    }

    public class DynamicProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public Property[] Properties { get; set; }
    }

    public class Property
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class FA3
    {
        public int TaxonId { get; set; }
        public int EvaluatedScientificNameId { get; set; }
        public string EvaluatedScientificName { get; set; }
        public List<ImpactedNatureType> ImpactedNatureTypes { get; set; }
        public RiskAssessment RiskAssessment { get; set; }
    }

    public class ImpactedNatureType
    {
        public string NiNCode { get; set; }
    }

    public class RiskAssessment
    {
        public string RiskLevelCode { get; set; }
        public string RiskLevelText { get; set; }
    }
}