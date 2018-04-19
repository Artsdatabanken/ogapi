using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using Microsoft.Azure.Graphs.Elements;
using Newtonsoft.Json;
using NinMemApi.Data;
using NinMemApi.Data.Elements;
using NinMemApi.Data.Elements.Properties;
using NinMemApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NinMemApi.DataPreprocessing
{
    public class TaxonTraitsCosmosImporter
    {
        private readonly string _host;
        private readonly string _authKey;

        public TaxonTraitsCosmosImporter(string host, string authKey)
        {
            _host = host;
            _authKey = authKey;
        }

        public async Task Import(GraphInput input)
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 10000;

            const string databaseName = "adb-og-graph-db-v5";
            const string collectionName = "abd-og-graph-v5";
            const string naVertexId = "na";

            using (DocumentClient client = new DocumentClient(new Uri(_host), _authKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Https,
                    RequestTimeout = new TimeSpan(1, 0, 0),
                    MaxConnectionLimit = 1000,
                    RetryOptions = new RetryOptions
                    {
                        MaxRetryAttemptsOnThrottledRequests = 10,
                        MaxRetryWaitTimeInSeconds = 60
                    }
                }))
            {
                await client.OpenAsync();

                Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
                DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(databaseName),
                    new DocumentCollection { Id = collectionName },
                    new RequestOptions { OfferThroughput = 1000 });

                await CreateVertex(client, graph, CVL.TN, CodeConsts.RootNodeCode, (CP.Name, "Katalog"));
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.EnvironmentVariable, (CP.Name, "Miljøvariabler"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.EnvironmentVariable, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.DescriptionVariable, (CP.Name, "Beskrivelsesvariabler"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.DescriptionVariable, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.RedlistCategories, (CP.Name, "Truede arter"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.RedlistCategories, CodeConsts.RootNodeCode);
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.RedlistThemes, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.BlacklistCategories, (CP.Name, "Fremmede arter"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.BlacklistCategories, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.Taxon, (CP.Name, "Liv"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.Taxon, CodeConsts.RootNodeCode);

                await CreateVertex(client, graph, CVL.NAT, naVertexId, (CP.Name, "Naturområder"));
                await CreateEdge(client, graph, CEL.Barn, naVertexId, CodeConsts.RootNodeCode);

                foreach (var kvp in RedlistCodeNames.GetAll())
                {
                    string code = CodePrefixes.GetRedlistCategoryCode(kvp.Key);
                    await CreateVertex(client, graph, CVL.RL, code, (CP.Name, kvp.Value));
                    await CreateEdge(client, graph, CEL.Barn, code, CodePrefixes.RedlistCategories);
                }

                foreach (var kvp in BlacklistCodeNames.GetAll())
                {
                    string code = CodePrefixes.GetBlacklistCategoryCode(kvp.Key);
                    await CreateVertex(client, graph, CVL.FA, code, (CP.Name, kvp.Value));
                    await CreateEdge(client, graph, CEL.Barn, code, CodePrefixes.BlacklistCategories);
                }

                var systemCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                var queryHandler = new CosmosQueryHandlerV3(client, graph);

                await AddCodes(queryHandler, input.CodeTree.Children[naVertexId], CVL.NAT);
                await AddCodes(queryHandler, input.CodeTree.Children[CodePrefixes.DescriptionVariable], CVL.BS);
                await AddCodes(queryHandler, input.CodeTree.Children[CodePrefixes.EnvironmentVariable], CVL.MI);
                await AddCodes(queryHandler, input.CodeTree.Children[CodePrefixes.AdministrativeArea], CVL.AO);

                await queryHandler.Flush();

                var codes = new HashSet<string>();

                foreach (var code in input.CodeTree.Children[naVertexId].GetAllDescendants().Keys)
                {
                    codes.Add(code.ToLower());
                }
                foreach (var code in input.CodeTree.Children[CodePrefixes.DescriptionVariable].GetAllDescendants().Keys)
                {
                    codes.Add(code.ToLower());
                }
                foreach (var code in input.CodeTree.Children[CodePrefixes.EnvironmentVariable].GetAllDescendants().Keys)
                {
                    codes.Add(code.ToLower());
                }

                await queryHandler.Flush();

                var taxonDict = new Dictionary<int, Taxon>();

                foreach (var taxon in input.Taxons)
                {
                    taxonDict.Add(taxon.ScientificNameId, taxon);
                }

                var taxonsFromCodeTree = input.CodeTree.Children[CodePrefixes.Taxon].GetAllDescendants().ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value);
                var addedTaxons = new Dictionary<string, Taxon>();
                var taxonsWithAddedParents = new HashSet<string>();

                int index = 0;
                var taxonsWithTraits = new HashSet<int>();

                foreach (var taxonTraits in input.TaxonTraits)
                {
                    taxonsWithTraits.Add(taxonTraits.ScientificNameId);
                }

                var taxonsToCreate = input.Taxons.Where(t => taxonsWithTraits.Contains(t.ScientificNameId)).ToList();

                foreach (var taxon in taxonsToCreate)
                {
                    index++;
                    string code = CodePrefixes.GetTaxonCode(taxon.ScientificNameId);

                    if (!addedTaxons.ContainsKey(code))
                    {
                        await AddTaxon(queryHandler, taxonsFromCodeTree, addedTaxons, taxon, code, false);

                        int parentId = taxon.ParentScientificNameId;

                        while (parentId != 0)
                        {
                            string parentCode = CodePrefixes.GetTaxonCode(parentId);
                            var parent = taxonDict[parentId];

                            if (!addedTaxons.ContainsKey(parentCode))
                            {
                                await AddTaxon(queryHandler, taxonsFromCodeTree, addedTaxons, parent, parentCode, false);
                            }

                            parentId = parent.ParentScientificNameId;
                        }
                    }

                    if (index % 1000 == 0)
                    {
                        Console.WriteLine($"{index}/{taxonsToCreate.Count} taxons");
                    }
                }

                await queryHandler.Flush();

                index = 0;

                // Add taxon parents
                foreach (var taxon in addedTaxons.Values)
                {
                    index++;
                    string code = CodePrefixes.GetTaxonCode(taxon.ScientificNameId);

                    if (taxonsWithAddedParents.Contains(code))
                    {
                        continue;
                    }

                    if (taxon.ParentScientificNameId == 0)
                    {
                        string edgeQuery = CreateAddEdgeQuery(CEL.Barn, code, CodePrefixes.Taxon);
                        await queryHandler.Add(edgeQuery, false);
                    }
                    else
                    {
                        int parentScientificNameId = taxon.ParentScientificNameId;
                        string parentCode = CodePrefixes.GetTaxonCode(parentScientificNameId);
                        string currentCode = code;

                        while (true)
                        {
                            if (!taxonDict.TryGetValue(parentScientificNameId, out var parent))
                            {
                                string edgeQuery = CreateAddEdgeQuery(CEL.Barn, currentCode, CodePrefixes.Taxon);
                                await queryHandler.Add(edgeQuery, false);
                                taxonsWithAddedParents.Add(currentCode);
                                break;
                            }
                            else if (addedTaxons.ContainsKey(parentCode))
                            {
                                string edgeQuery = CreateAddEdgeQuery(CEL.Barn, currentCode, parentCode);
                                await queryHandler.Add(edgeQuery, false);
                                taxonsWithAddedParents.Add(currentCode);

                                parentScientificNameId = parent.ParentScientificNameId;

                                currentCode = parentCode;
                                parentCode = CodePrefixes.GetTaxonCode(parentScientificNameId);
                                break;
                            }
                            else
                            {
                                throw new Exception($"{parentCode} should exist!");
                            }
                        }
                    }

                    if (index % 1000 == 0)
                    {
                        Console.WriteLine($"{index}/{addedTaxons.Count} taxonParents");
                    }
                }

                await queryHandler.Flush();

                index = 0;

                var addedCodes = new HashSet<string>();

                var taxonTraitsDict = new Dictionary<string, TaxonTraits>();

                foreach (var tt in input.TaxonTraits)
                {
                    string code = CodePrefixes.GetTaxonCode(tt.ScientificNameId);
                    taxonTraitsDict.Add(code, tt);
                }

                foreach (var kvp in addedTaxons)
                {
                    string taxonCode = kvp.Key;
                    var taxon = kvp.Value;

                    if (!string.IsNullOrWhiteSpace(taxon.BlacklistCategory))
                    {
                        var blacklistCode = CodePrefixes.GetBlacklistCategoryCode(taxon.BlacklistCategory);
                        string blQuery = CreateAddEdgeQuery(CEL.Har, taxonCode, blacklistCode);
                        await queryHandler.Add(blQuery, false);
                    }

                    await LinkTaxonVertex(queryHandler, taxonCode, taxon.NatureAreaTypeCodes.ToArray(), (id) => id);
                    await LinkTaxonVertex(queryHandler, taxonCode, taxon.RedlistCategories, (id) => CodePrefixes.GetRedlistCategoryCode(id));

                    if (taxonTraitsDict.TryGetValue(taxonCode, out var taxonTraits))
                    {
                        await HandleTaxonTraits(queryHandler, addedCodes, taxonCode, taxonTraits);
                    }

                    if (++index % 1000 == 0)
                    {
                        Console.WriteLine($"{index}/{addedTaxons.Count} addedTaxons");
                    }
                }

                await queryHandler.Flush();

                Thread.CurrentThread.CurrentCulture = systemCulture;
            }
        }

        private static async Task HandleTaxonTraits(CosmosQueryHandlerV3 queryHandler, HashSet<string> addedCodes, string taxonCode, TaxonTraits taxonTraits)
        {
            if (taxonTraits.FeedsOn != null)
            {
                foreach (var feedsOn in taxonTraits.FeedsOn)
                {
                    var feedsOnQuery = CreateAddEdgeQuery(CEL.Spiser, taxonCode, CodePrefixes.GetTaxonCode(feedsOn));
                    await queryHandler.Add(feedsOnQuery, false);
                }
            }

            if (taxonTraits.PreysUpon != null)
            {
                foreach (var preysUpon in taxonTraits.PreysUpon)
                {
                    var preysUponQuery = CreateAddEdgeQuery(CEL.Jakter, taxonCode, CodePrefixes.GetTaxonCode(preysUpon));
                    await queryHandler.Add(preysUponQuery, false);
                }
            }

            if (taxonTraits.Habitat != null)
            {
                foreach (var code in taxonTraits.Habitat)
                {
                    string edgeQuery = CreateAddEdgeQuery(CEL.Bor, taxonCode, code.ToLower());
                    await queryHandler.Add(edgeQuery, false);
                }
            }

            if (!string.IsNullOrWhiteSpace(taxonTraits.MatingSystem))
            {
                await AddTaxonTrait(queryHandler, addedCodes, CVL.AEMS, CEL.Har, taxonCode, CodePrefixes.GetMatingsSystemCode(taxonTraits.MatingSystem), taxonTraits.MatingSystem);
            }

            if (taxonTraits.PrimaryDiet != null)
            {
                foreach (var diet in taxonTraits.PrimaryDiet)
                {
                    await AddTaxonTrait(queryHandler, addedCodes, CVL.AEPD, CEL.Spiser, taxonCode, CodePrefixes.GetPrimaryDietCode(diet), diet);
                }
            }

            if (taxonTraits.SexualDimorphism != null)
            {
                foreach (var sexualDimorphism in taxonTraits.SexualDimorphism)
                {
                    await AddTaxonTrait(queryHandler, addedCodes, CVL.AESD, CEL.Har, taxonCode, CodePrefixes.GetSexualDimorphismCode(sexualDimorphism), sexualDimorphism);
                }
            }

            if (taxonTraits.SocialSystem != null)
            {
                foreach (var socialSystem in taxonTraits.SocialSystem)
                {
                    await AddTaxonTrait(queryHandler, addedCodes, CVL.AESS, CEL.Har, taxonCode, CodePrefixes.GetSocialSystemCode(socialSystem), socialSystem);
                }
            }

            if (!string.IsNullOrWhiteSpace(taxonTraits.Terrestriality))
            {
                await AddTaxonTrait(queryHandler, addedCodes, CVL.AET, CEL.Bor, taxonCode, CodePrefixes.GetTerrestrialityCode(taxonTraits.Terrestriality), taxonTraits.Terrestriality);
            }

            if (taxonTraits.TotalLifeSpan.HasValue)
            {
                string propertyQuery = CreateAddPropertyQuery(taxonCode, (CTP.TotalLifeSpan, taxonTraits.TotalLifeSpan.Value));
                await queryHandler.Add(propertyQuery, false);
            }

            if (taxonTraits.TrophicLevel != null)
            {
                foreach (var trophicLevel in taxonTraits.TrophicLevel)
                {
                    await AddTaxonTrait(queryHandler, addedCodes, CVL.AETL, CEL.Er, taxonCode, CodePrefixes.GetTrophicLevelCode(trophicLevel), trophicLevel);
                }
            }
        }

        private static async Task AddTaxonTrait(CosmosQueryHandlerV3 queryHandler, HashSet<string> addedCodes, string vertexLabel, string edgeLabel, string taxonCode, string traitCode, string traitName)
        {
            if (!addedCodes.Contains(traitCode))
            {
                string vertexQuery = CreateAddVertexQuery(vertexLabel, traitCode, (CP.Name, traitName));
                addedCodes.Add(traitCode);
                await queryHandler.Add(vertexQuery, true);
            }

            string edgeQuery = CreateAddEdgeQuery(edgeLabel, taxonCode, traitCode);
            await queryHandler.Add(edgeQuery, false);
        }

        private static async Task AddTaxon(CosmosQueryHandlerV3 queryHandler, Dictionary<string, CodeTreeNode> taxonsFromCodeTree, Dictionary<string, Taxon> addedTaxons, Taxon taxon, string code, bool sync)
        {
            var taxonFromCodeTree = taxonsFromCodeTree[code.ToLower()];

            string taxonQuery = CreateAddVertexQuery(CVL.AR, code,
                (CP.Name, taxon.ScientificName),
                (CTP.TaxonId, taxon.TaxonId),
                (CTP.Names, JsonConvert.SerializeObject(taxonFromCodeTree.Names)));

            await queryHandler.Add(taxonQuery, sync);

            addedTaxons.Add(code, taxon);
        }

        private static async Task LinkTaxonVertex<T>(CosmosQueryHandlerV3 queryHandler, string taxonCode, T[] idsToConnect, Func<T, string> codeCreator)
        {
            if (idsToConnect == null || idsToConnect.Length == 0)
            {
                return;
            }

            foreach (var id in idsToConnect)
            {
                var codeToLink = codeCreator(id);

                string query = CreateAddEdgeQuery(CEL.Har, taxonCode, codeToLink);
                await queryHandler.Add(query, false);
            }
        }

        private static async Task AddCodes(CosmosQueryHandlerV3 queryHandler, CodeTreeNode treeNode, string label)
        {
            var descendants = treeNode.GetAllDescendants();

            foreach (var descendant in descendants)
            {
                string query = CreateAddVertexQuery(label, descendant.Value.Code, (CP.Name, descendant.Value.Name));
                await queryHandler.Add(query, false);
            }

            await queryHandler.Flush();

            var connectedParent = new HashSet<(string, string)>();

            foreach (var descendant in descendants)
            {
                foreach (var parentCode in descendant.Value.Parents.Select(p => p.Key))
                {
                    var key = (descendant.Value.Code, parentCode);

                    if (!connectedParent.Contains(key))
                    {
                        string query = CreateAddEdgeQuery(CEL.Barn, descendant.Value.Code, parentCode);

                        await queryHandler.Add(query, false);

                        connectedParent.Add(key);
                    }
                }
            }

            await queryHandler.Flush();
        }

        private static async Task CreateVertex(DocumentClient client, DocumentCollection graph, string label, string id, params (string name, object value)[] properties)
        {
            string query = CreateAddVertexQuery(label, id, properties);
            await RunQuery<Vertex>(client, graph, query);
        }

        private static async Task CreateEdge(DocumentClient client, DocumentCollection graph, string label, string from, string to, params (string name, object value)[] properties)
        {
            string query = CreateAddEdgeQuery(label, from, to, properties);
            await RunQuery<Edge>(client, graph, query);
        }

        private static string CreateAddVertexQuery(string label, string id, params (string name, object value)[] properties)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"g.addV('{label}').property('id', '{id}')");

            AddProperties(properties, builder);

            return builder.ToString();
        }

        private static string CreateAddEdgeQuery(string label, string from, string to, params (string name, object value)[] properties)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"g.V('{from}').addE('{label}').to(g.V('{to}'))");

            AddProperties(properties, builder);

            return builder.ToString();
        }

        private static string CreateAddPropertyQuery(string vertexId, params (string name, object value)[] properties)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"g.V('{vertexId}')");

            AddProperties(properties, builder);

            return builder.ToString();
        }

        private static void AddProperties((string name, object value)[] properties, StringBuilder builder)
        {
            foreach (var prop in properties)
            {
                string value = null;

                if (prop.value is string)
                {
                    value = $"'{((string)prop.value).Replace("'", "\\'")}'";
                }
                else if (prop.value is double)
                {
                    value = ((double)prop.value).ToString();
                }
                else
                {
                    value = prop.value.ToString();
                }
                builder.Append($".property('{prop.name}', {value})");
            }
        }

        private static async Task<IEnumerable<T>> RunQuery<T>(DocumentClient client, DocumentCollection graph, string gremlinQuery)
        {
            List<T> results = new List<T>();

            using (IDocumentQuery<T> query = client.CreateGremlinQuery<T>(graph, gremlinQuery))
            {
                while (query.HasMoreResults)
                {
                    foreach (T result in await query.ExecuteNextAsync<T>())
                    {
                        results.Add(result);
                    }
                }
            }

            return results;
        }
    }
}