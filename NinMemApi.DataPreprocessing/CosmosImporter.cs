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
    public class CosmosImporter
    {
        private readonly string _host;
        private readonly string _authKey;

        public CosmosImporter(string host, string authKey)
        {
            _host = host;
            _authKey = authKey;
        }

        public async Task Import(GraphInput input)
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 10000;

            const string databaseName = "adb-og-graph-db";
            const string collectionName = "abd-og-graph";

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

                //var setupQueries = new[]
                //{
                //    //"g.E().drop()",
                //    //"g.V().drop()"
                //    //"g.V().hasLabel('nature_area_lol').drop()"
                //};

                //foreach (string query in setupQueries)
                //{
                //    await RunQuery<dynamic>(client, graph, query);
                //}

                await CreateVertex(client, graph, CVL.TN, CodeConsts.RootNodeCode, (CP.Name, "Katalog"));
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.AdministrativeArea, (CP.Name, "Fylker"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.AdministrativeArea, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.ConservationAreaCategories, (CP.Name, "Verneområder"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.ConservationAreaCategories, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.EnvironmentVariable, (CP.Name, "Miljøvariabler"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.EnvironmentVariable, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.DescriptionVariable, (CP.Name, "Beskrivelsesvariabler"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.DescriptionVariable, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.RedlistCategories, (CP.Name, "Truede arter"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.RedlistCategories, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.RedlistThemes, (CP.Name, "Rødlistetemaer"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.RedlistThemes, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.BlacklistCategories, (CP.Name, "Fremmede arter"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.BlacklistCategories, CodeConsts.RootNodeCode);
                await CreateVertex(client, graph, CVL.TN, CodePrefixes.Taxon, (CP.Name, "Liv"));
                await CreateEdge(client, graph, CEL.Barn, CodePrefixes.Taxon, CodeConsts.RootNodeCode);
                const string naVertexId = "na";
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

                var queryHandler = new CosmosQueryHandlerV2("adb-graph.gremlin.cosmosdb.azure.com"/*"adb-graph.documents.azure.com"*/, _authKey, databaseName, collectionName);

                await AddCodes(queryHandler, input.CodeTree.Children[naVertexId], CVL.NAT);
                await AddCodes(queryHandler, input.CodeTree.Children[CodePrefixes.DescriptionVariable], CVL.BS);
                await AddCodes(queryHandler, input.CodeTree.Children[CodePrefixes.EnvironmentVariable], CVL.MI);
                await AddCodes(queryHandler, input.CodeTree.Children[CodePrefixes.AdministrativeArea], CVL.AO);

                foreach (var na in input.NatureAreas)
                {
                    string code = CodePrefixes.GetNatureAreaCode(na.Id);

                    string query = CreateAddVertexQuery(CVL.NA, code, (CNAP.Area, na.Area));
                    await queryHandler.Add(query, false);
                }

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

                var connectedCodes = new HashSet<(string codeToConnect, string naCode)>();
                //input.CodeTree.Children[CodePrefixes.DescriptionVariable].GetAllDescendants().Keys.Select(code => codes.Add(code.ToLower()));
                //input.CodeTree.Children[CodePrefixes.EnvironmentVariable].GetAllDescendants().Keys.Select(code => codes.Add(code.ToLower()));
                int index = 0;
                foreach (var v in input.NatureAreaVariables)
                {
                    string naCode = CodePrefixes.GetNatureAreaCode(v.NatureAreaId);
                    string natCode = v.NatureAreaTypeCode.ToLower();

                    if (codes.Contains(natCode))
                    {
                        var natKey = (natCode, naCode);

                        if (!connectedCodes.Contains(natKey))
                        {
                            string query = CreateAddEdgeQuery(CEL.I, natCode, naCode, (CNATP.Percentage, v.Percentage));
                            await queryHandler.Add(query, false);

                            connectedCodes.Add(natKey);
                        }

                        foreach (var dv in v.DescriptionVariables)
                        {
                            var dvCode = CodePrefixes.GetDescriptionOrEnvironmentVariableCode(dv);

                            if (codes.Contains(dvCode))
                            {
                                var devKey = (dvCode, naCode);

                                if (!connectedCodes.Contains(devKey))
                                {
                                    string query = CreateAddEdgeQuery(CEL.I, dvCode, naCode);
                                    await queryHandler.Add(query, false);

                                    connectedCodes.Add(devKey);
                                }
                                else
                                {
                                }
                            }
                        }
                    }

                    if (++index % 1000 == 0)
                    {
                        Console.WriteLine($"{index}/{input.NatureAreaVariables.Count} of nature area variables");
                    }
                }

                await queryHandler.Flush();

                foreach (var redlistCategory in input.NatureAreaRedlistCategories)
                {
                    var categoryCode = CodePrefixes.GetRedlistCategoryCode(redlistCategory.Name);

                    await LinkNatureAreaIds(queryHandler, categoryCode, redlistCategory.NatureAreaIds.ToList());
                }

                await queryHandler.Flush();

                foreach (var redlistTheme in input.NatureAreaRedlistThemes)
                {
                    var themeCode = CodePrefixes.GetRedlistThemeCode(redlistTheme.Id);
                    string themeQuery = CreateAddVertexQuery(CVL.RT, themeCode, (CP.Name, redlistTheme.Name));
                    await queryHandler.Add(themeQuery, true);
                    string themeEdgeQuery = CreateAddEdgeQuery(CEL.Barn, themeCode, CodePrefixes.RedlistThemes);
                    await queryHandler.Add(themeEdgeQuery, false);

                    foreach (var au in redlistTheme.AssessmentUnits)
                    {
                        string auCode = CodePrefixes.GetRedlistAssessmentUnitCode(au.Id);
                        string auQuery = CreateAddVertexQuery(CVL.RV, auCode, (CP.Name, au.Name));
                        await queryHandler.Add(auQuery, true);
                        string auEdgeQuery = CreateAddEdgeQuery(CEL.Barn, auCode, themeCode);
                        await queryHandler.Add(auEdgeQuery, false);

                        await LinkNatureAreaIds(queryHandler, auCode, au.NatureAreaIds.ToList());
                    }
                }

                await queryHandler.Flush();

                foreach (var cac in input.NatureAreaGeographicalAreaData.ConservationAreaCategories)
                {
                    string cacCode = CodePrefixes.GetConservationAreaCategoryCode(cac.ShortName);
                    string cacQuery = CreateAddVertexQuery(CVL.VK, cacCode, (CP.Name, cac.Name));
                    await queryHandler.Add(cacQuery, true);
                    string cacEdgeQuery = CreateAddEdgeQuery(CEL.Barn, cacCode, CodePrefixes.ConservationAreas);
                    await queryHandler.Add(cacEdgeQuery, false);

                    foreach (var ca in cac.ConservationAreas)
                    {
                        string caCode = CodePrefixes.GetConservationAreaCode(ca.Number);
                        string caQuery = CreateAddVertexQuery(CVL.VV, caCode, (CP.Name, ca.Name));
                        await queryHandler.Add(caQuery, true);
                        string caEdgeQuery = CreateAddEdgeQuery(CEL.Barn, caCode, cacCode);
                        await queryHandler.Add(caEdgeQuery, false);

                        await LinkNatureAreaIds(queryHandler, caCode, ca.NatureAreaIds.ToList());
                    }
                }

                await queryHandler.Flush();

                var administrativeAreas = input.CodeTree.Children[CodePrefixes.AdministrativeArea].GetAllDescendants();
                var aaNumberDict = new Dictionary<int, string>();
                var aaNameDict = new Dictionary<string, string>();

                foreach (var aa in administrativeAreas)
                {
                    int number = int.Parse(aa.Key.Replace(CodePrefixes.AdministrativeArea + "_", string.Empty).Replace("-", string.Empty));
                    aaNumberDict.Add(number, aa.Key);

                    string name = aa.Value.Name.ToLower();

                    if (aa.Key.Contains("-"))
                    {
                        if (aaNameDict.ContainsKey(name))
                        {
                            aaNameDict.Remove(name); // Remove duplicate municipality name and let the code crash, if so.
                        }
                        else
                        {
                            aaNameDict.Add(name, aa.Key);
                        }
                    }
                }

                var aaOldNumberToCodeMapping = new Dictionary<int, string>();

                foreach (var county in input.NatureAreaGeographicalAreaData.Counties)
                {
                    foreach (var municipality in county.Municipalities)
                    {
                        string name = municipality.Name.ToLower();

                        if (name == "rissa" || name == "leksvik")
                        {
                            name = "indre fosen";
                        }
                        else if (name == "hof")
                        {
                            name = "holmestrand";
                        }
                        else if (name == "andebu" || name == "stokke")
                        {
                            name = "sandefjord";
                        }
                        else if (name == "tjøme" || name == "nøtterøy")
                        {
                            name = "færder";
                        }
                        else if (name == "lardal")
                        {
                            name = "larvik";
                        }

                        var aaCode = aaNumberDict.ContainsKey(municipality.Number) ? aaNumberDict[municipality.Number] : aaNameDict[name];
                        aaOldNumberToCodeMapping.Add(municipality.Number, aaCode);

                        await LinkNatureAreaIds(queryHandler, aaCode, municipality.NatureAreaIds.ToList());
                    }
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

                index = 0;
                var taxonsToCreate = input.Taxons.Where(t => t.EastNorths.Length > 0).ToList();
                var taxonsToProcessHierarchically = taxonsToCreate.ToList();

                // Add taxons with observations
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
                                taxonsToProcessHierarchically.Add(parent);
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
                foreach (var taxon in taxonsToProcessHierarchically)
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
                        Console.WriteLine($"{index}/{taxonsToCreate.Count} taxonParents");
                    }
                }

                await queryHandler.Flush();

                index = 0;

                foreach (var kvp in addedTaxons)
                {
                    string code = kvp.Key;
                    var taxon = kvp.Value;

                    if (!string.IsNullOrWhiteSpace(taxon.BlacklistCategory))
                    {
                        var blacklistCode = CodePrefixes.GetBlacklistCategoryCode(taxon.BlacklistCategory);
                        string blQuery = CreateAddEdgeQuery(CEL.I, blacklistCode, code);
                        await queryHandler.Add(blQuery, false);
                    }

                    await LinkTaxonVertex(queryHandler, code, taxon.NatureAreaTypeCodes.ToArray(), (id) => id);
                    await LinkTaxonVertex(queryHandler, code, taxon.RedlistCategories, (id) => CodePrefixes.GetRedlistCategoryCode(id));
                    //LinkTaxonVertex(g, vertex, taxon.NatureAreas, (id) => TagPrefixes.GetNatureAreaCode(id)); // TODO: Remove. Link directly to nat.
                    await LinkTaxonVertex(queryHandler, code, taxon.Municipalities, (id) => aaOldNumberToCodeMapping[id]);
                    await LinkTaxonVertex(queryHandler, code, taxon.ConservationAreas, (id) => CodePrefixes.GetConservationAreaCode(id));

                    if (++index % 1000 == 0)
                    {
                        Console.WriteLine($"{index}/{addedTaxons.Count} addedTaxons");
                    }
                }

                await queryHandler.Flush();

                Thread.CurrentThread.CurrentCulture = systemCulture;
            }
        }

        private static async Task AddTaxon(CosmosQueryHandlerV2 queryHandler, Dictionary<string, CodeTreeNode> taxonsFromCodeTree, Dictionary<string, Taxon> addedTaxons, Taxon taxon, string code, bool sync)
        {
            var taxonFromCodeTree = taxonsFromCodeTree[code.ToLower()];

            string taxonQuery = CreateAddVertexQuery(CVL.AR, code,
                (CP.Name, taxon.ScientificName),
                (CTP.TaxonId, taxon.TaxonId),
                (CTP.Names, JsonConvert.SerializeObject(taxonFromCodeTree.Names)));

            await queryHandler.Add(taxonQuery, sync);

            addedTaxons.Add(code, taxon);
        }

        private static async Task LinkTaxonVertex<T>(CosmosQueryHandlerV2 queryHandler, string taxonCode, T[] idsToConnect, Func<T, string> codeCreator)
        {
            if (idsToConnect == null || idsToConnect.Length == 0)
            {
                return;
            }

            foreach (var id in idsToConnect)
            {
                var codeToLink = codeCreator(id);

                string query = CreateAddEdgeQuery(CEL.I, codeToLink, taxonCode);
                await queryHandler.Add(query, false);
            }
        }

        private static async Task LinkNatureAreaIds(CosmosQueryHandlerV2 queryHandler, string code, List<int> natureAreaIds)
        {
            if (natureAreaIds == null || natureAreaIds.Count == 0)
            {
                return;
            }

            foreach (var natureAreaId in natureAreaIds)
            {
                string naCode = CodePrefixes.GetNatureAreaCode(natureAreaId);

                string query = CreateAddEdgeQuery(CEL.I, code, naCode);
                await queryHandler.Add(query, false);
            }
        }

        private static async Task AddCodes(CosmosQueryHandlerV2 queryHandler, CodeTreeNode treeNode, string label)
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