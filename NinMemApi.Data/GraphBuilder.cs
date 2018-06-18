using NinMemApi.Data.Elements;
using NinMemApi.Data.Elements.Properties;
using NinMemApi.Data.Models;
using NinMemApi.GraphDb;
using NinMemApi.GraphDb.Labels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NinMemApi.Data
{
    public static class GraphBuilder
    {
        public static void Build(G g, GraphInput input)
        {
            var rootVertex = g.AddV(VL.TN, CodeConsts.RootNodeCode).AddP(P.Name, "Katalog");
            var countyVertex = g.AddV(VL.TN, CodePrefixes.AdministrativeArea).AddP(P.Name, "Fylker");
            countyVertex.AddE(EL.Child, rootVertex);
            var conservationAreaVertex = g.AddV(VL.TN, CodePrefixes.ConservationAreas).AddP(P.Name, "Verneområder");
            conservationAreaVertex.AddE(EL.Child, rootVertex);
            var environmentVariableVertex = g.AddV(VL.TN, CodePrefixes.EnvironmentVariable).AddP(P.Name, "Miljøvariabler");
            environmentVariableVertex.AddE(EL.Child, rootVertex);
            var descriptionVariableVertex = g.AddV(VL.TN, CodePrefixes.DescriptionVariable).AddP(P.Name, "Beskrivelsesvariabler");
            descriptionVariableVertex.AddE(EL.Child, rootVertex);
            var redlistCategoryVertex = g.AddV(VL.TN, CodePrefixes.RedlistCategories).AddP(P.Name, "Truede arter");
            redlistCategoryVertex.AddE(EL.Child, rootVertex);
            var redlistThemeVertex = g.AddV(VL.TN, CodePrefixes.RedlistThemes).AddP(P.Name, "Rødlistetemaer");
            redlistThemeVertex.AddE(EL.Child, rootVertex);
            var blacklistCategoryVertex = g.AddV(VL.TN, CodePrefixes.BlacklistCategories).AddP(P.Name, "Fremmede arter");
            blacklistCategoryVertex.AddE(EL.Child, rootVertex);
            var taxonVertex = g.AddV(VL.TN, CodePrefixes.Taxon).AddP(P.Name, "Liv");
            taxonVertex.AddE(EL.Child, rootVertex);
            const string naTopCode = "na";
            var natVertex = g.AddV(VL.NAT, naTopCode).AddP(P.Name, "Naturområder");
            natVertex.AddE(EL.Child, rootVertex);

            foreach (var kvp in RedlistCodeNames.GetAll())
            {
                g.AddV(VL.RC, CodePrefixes.GetRedlistCategoryCode(kvp.Key)).AddP(P.Name, kvp.Value).AddE(EL.Child, redlistCategoryVertex);
            }

            foreach (var kvp in BlacklistCodeNames.GetAll())
            {
                g.AddV(VL.BC, CodePrefixes.GetBlacklistCategoryCode(kvp.Key)).AddP(P.Name, kvp.Value).AddE(EL.Child, blacklistCategoryVertex);
            }

            AddCodes(g, input.CodeTree.Children[naTopCode], VL.NAT);
            AddCodes(g, input.CodeTree.Children[CodePrefixes.DescriptionVariable], VL.DV);
            AddCodes(g, input.CodeTree.Children[CodePrefixes.EnvironmentVariable], VL.EV);
            AddCodes(g, input.CodeTree.Children[CodePrefixes.AdministrativeArea], VL.AA);

            foreach (var na in input.NatureAreas)
            {
                string code = CodePrefixes.GetNatureAreaCode(na.Id);
                g.AddV(VL.NA, code).AddP(NAP.Area, na.Area);
            }

            var connectedCodes = new HashSet<(string codeToConnect, string naCode)>();

            foreach (var v in input.NatureAreaVariables)
            {
                string naCode = CodePrefixes.GetNatureAreaCode(v.NatureAreaId);
                string natCode = v.NatureAreaTypeCode.ToLower();

                var naVertex = g.V(naCode);

                if (g.TryGetV(v.NatureAreaTypeCode, out natVertex))
                {
                    var natKey = (natCode, naCode);

                    if (!connectedCodes.Contains(natKey))
                    {
                        natVertex.AddE(EL.In, naVertex).AddP(NATP.Percentage, v.Percentage);
                        connectedCodes.Add(natKey);
                    }

                    foreach (var dv in v.DescriptionVariables)
                    {
                        var dvCode = CodePrefixes.GetDescriptionOrEnvironmentVariableCode(dv);

                        if (g.TryGetV(dvCode, out var dvVertex))
                        {
                            var devKey = (dvCode, naCode);

                            if (!connectedCodes.Contains(devKey))
                            {
                                dvVertex.AddE(EL.In, naVertex);
                                connectedCodes.Add(devKey);
                            }
                        }
                    }
                }
            }

            foreach (var redlistCategory in input.NatureAreaRedlistCategories)
            {
                var categoryVertex = g.V(CodePrefixes.GetRedlistCategoryCode(redlistCategory.Name));

                LinkNatureAreaIds(g, categoryVertex, redlistCategory.NatureAreaIds.ToList());
            }

            foreach (var redlistTheme in input.NatureAreaRedlistThemes)
            {
                var themeVertex = g.AddV(VL.RT, CodePrefixes.GetRedlistThemeCode(redlistTheme.Id)).AddP(P.Name, redlistTheme.Name);
                themeVertex.AddE(EL.Child, redlistThemeVertex);

                foreach (var au in redlistTheme.AssessmentUnits)
                {
                    var auVertex = g.AddV(VL.RAU, CodePrefixes.GetRedlistAssessmentUnitCode(au.Id)).AddP(P.Name, au.Name);

                    auVertex.AddE(EL.Child, themeVertex);

                    LinkNatureAreaIds(g, auVertex, au.NatureAreaIds.ToList());
                }
            }

            foreach (var cac in input.NatureAreaGeographicalAreaData.ConservationAreaCategories)
            {
                var cacVertex = g.AddV(VL.CAC, CodePrefixes.GetConservationAreaCategoryCode(cac.ShortName)).AddP(P.Name, cac.Name);
                cacVertex.AddE(EL.Child, conservationAreaVertex);

                foreach (var ca in cac.ConservationAreas)
                {
                    var caVertex = g.AddV(VL.CA, CodePrefixes.GetConservationAreaCode(ca.Number)).AddP(P.Name, ca.Name);

                    caVertex.AddE(EL.Child, cacVertex);

                    LinkNatureAreaIds(g, caVertex, ca.NatureAreaIds.ToList());
                }
            }

            var administrativeAreas = input.CodeTree.Children[CodePrefixes.AdministrativeArea].GetAllDescendants().Where(d => d.Key.Split('-').Length < 3);
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

                    var municipalityVertex = g.V(aaCode);

                    LinkNatureAreaIds(g, municipalityVertex, municipality.NatureAreaIds.ToList());
                }
            }

            var taxonDict = new Dictionary<int, Taxon>();

            foreach (var taxon in input.Taxons)
            {
                taxonDict.Add(taxon.ScientificNameId, taxon);
            }

            var taxonsFromCodeTree = input.CodeTree.Children[CodePrefixes.Taxon].GetAllDescendants().ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value);

            var taxonTraitsDict = new Dictionary<string, TaxonTraits>();

            //// TODO: uncomment this when TaxonTraits is back
            //foreach (var tt in input.TaxonTraits)
            //{
            //    string code = CodePrefixes.GetTaxonCode(tt.ScientificNameId);
            //    taxonTraitsDict.Add(code, tt);
            //}

            foreach (var taxon in input.Taxons)
            {
                if (taxon.EastNorths.Length == 0)
                {
                    continue;
                }

                string code = CodePrefixes.GetTaxonCode(taxon.ScientificNameId);

                if (!taxonsFromCodeTree.ContainsKey(code)) continue;

                if (!g.TryGetV(code, out var vertex))
                {
                    vertex = CreateTaxonVertex(g, taxon, code, aaOldNumberToCodeMapping, taxonsFromCodeTree, taxonTraitsDict);
                }

                if (vertex.Parent != null)
                {
                    continue;
                }

                if (taxon.ParentScientificNameId == 0)
                {
                    vertex.AddE(EL.Child, taxonVertex);
                }
                else
                {
                    int parentScientificNameId = taxon.ParentScientificNameId;
                    string parentCode = CodePrefixes.GetTaxonCode(parentScientificNameId);
                    var currentVertex = vertex;

                    while (true)
                    {
                        if (!taxonDict.TryGetValue(parentScientificNameId, out var parent))
                        {
                            currentVertex.AddE(EL.Child, taxonVertex);
                            break;
                        }
                        else if (g.HasV(parentCode))
                        {
                            var parentVertex = g.V(parentCode);
                            currentVertex.AddE(EL.Child, parentVertex);
                            break;
                        }
                        else
                        {
                            var parentVertex = CreateTaxonVertex(g, parent, parentCode, aaOldNumberToCodeMapping, taxonsFromCodeTree, taxonTraitsDict);
                            currentVertex.AddE(EL.Child, parentVertex);

                            parentScientificNameId = parent.ParentScientificNameId;

                            parentCode = CodePrefixes.GetTaxonCode(parentScientificNameId);
                            currentVertex = parentVertex;
                        }
                    }
                }
            }

            foreach (var taxonTraits in taxonTraitsDict.Values)
            {
                HandleTaxonTraits(g, g.V(CodePrefixes.GetTaxonCode(taxonTraits.ScientificNameId)), taxonTraits);
            }
        }

        private static void AddCodes(G g, CodeTreeNode treeNode, int label)
        {
            var descendants = treeNode.GetAllDescendants();

            foreach (var descendant in descendants)
            {
                g.AddV(label, descendant.Value.Code).AddP(P.Name, descendant.Value.Name);
            }

            var connectedParent = new HashSet<(string, string)>();

            foreach (var descendant in descendants)
            {
                var vertex = g.V(descendant.Value.Code);

                foreach (var parentCode in descendant.Value.Parents.Select(p => p.Key))
                {
                    var key = (vertex.Id, parentCode);

                    if (!connectedParent.Contains(key))
                    {
                        var parentVertex = g.V(parentCode);

                        vertex.AddE(EL.Child, parentVertex);

                        connectedParent.Add(key);
                    }
                }
            }
        }

        private static Vertex CreateTaxonVertex(
            G g,
            Taxon taxon,
            string code,
            Dictionary<int, string> aaOldNumberToCodeMapping,
            IDictionary<string, CodeTreeNode> taxonsFromCodeTree,
            IDictionary<string, TaxonTraits> taxonTraitsDict)
        {
            var taxonFromCodeTree = taxonsFromCodeTree[code.ToLower()];

            var vertex = g.AddV(VL.T, code)
                    .AddP(P.Name, taxon.ScientificName)
                    .AddP(TP.ScientificNameId, taxon.ScientificNameId)
                    .AddP(TP.ScientificName, taxon.ScientificName)
                    .AddP(TP.TaxonId, taxon.TaxonId)
                    .AddP(TP.Names, taxonFromCodeTree.Names);

            if (!string.IsNullOrWhiteSpace(taxon.PopularName))
            {
                vertex.AddP(TP.PopularName, taxon.PopularName);
            }

            if (!string.IsNullOrWhiteSpace(taxon.BlacklistCategory))
            {
                var blacklistVertex = g.V(CodePrefixes.GetBlacklistCategoryCode(taxon.BlacklistCategory));
                blacklistVertex.AddE(EL.In, vertex);
            }

            LinkTaxonVertex(g, vertex, taxon.NatureAreaTypeCodes.ToArray(), (id) => id);
            LinkTaxonVertex(g, vertex, taxon.RedlistCategories, (id) => CodePrefixes.GetRedlistCategoryCode(id));
            LinkTaxonVertex(g, vertex, taxon.Municipalities, (id) => aaOldNumberToCodeMapping[id]);
            LinkTaxonVertex(g, vertex, taxon.ConservationAreas, (id) => CodePrefixes.GetConservationAreaCode(id));

            return vertex;
        }

        private static void HandleTaxonTraits(G g, Vertex taxonVertex, TaxonTraits taxonTraits)
        {
            if (taxonTraits.FeedsOn != null)
            {
                foreach (var feedsOn in taxonTraits.FeedsOn)
                {
                    taxonVertex.AddE(TEL.Spiser, g.V(CodePrefixes.GetTaxonCode(feedsOn)));
                }
            }

            if (taxonTraits.PreysUpon != null)
            {
                foreach (var preysUpon in taxonTraits.PreysUpon)
                {
                    taxonVertex.AddE(TEL.Jakter, g.V(CodePrefixes.GetTaxonCode(preysUpon)));
                }
            }

            if (taxonTraits.Habitat != null)
            {
                foreach (var code in taxonTraits.Habitat)
                {
                    taxonVertex.AddE(TEL.Bor, g.V(code.ToLower()));
                }
            }

            if (!string.IsNullOrWhiteSpace(taxonTraits.MatingSystem))
            {
                AddTaxonTrait(g, taxonVertex, VL.AEMS, TEL.Har, CodePrefixes.GetMatingsSystemCode(taxonTraits.MatingSystem), taxonTraits.MatingSystem);
            }

            if (taxonTraits.PrimaryDiet != null)
            {
                foreach (var diet in taxonTraits.PrimaryDiet)
                {
                    AddTaxonTrait(g, taxonVertex, VL.AEPD, TEL.Spiser, CodePrefixes.GetPrimaryDietCode(diet), diet);
                }
            }

            if (taxonTraits.SexualDimorphism != null)
            {
                foreach (var sexualDimorphism in taxonTraits.SexualDimorphism)
                {
                    AddTaxonTrait(g, taxonVertex, VL.AESD, TEL.Har, CodePrefixes.GetSexualDimorphismCode(sexualDimorphism), sexualDimorphism);
                }
            }

            if (taxonTraits.SocialSystem != null)
            {
                foreach (var socialSystem in taxonTraits.SocialSystem)
                {
                    AddTaxonTrait(g, taxonVertex, VL.AESS, TEL.Har, CodePrefixes.GetSocialSystemCode(socialSystem), socialSystem);
                }
            }

            if (!string.IsNullOrWhiteSpace(taxonTraits.Terrestriality))
            {
                AddTaxonTrait(g, taxonVertex, VL.AET, TEL.Bor, CodePrefixes.GetTerrestrialityCode(taxonTraits.Terrestriality), taxonTraits.Terrestriality);
            }

            if (taxonTraits.TotalLifeSpan.HasValue)
            {
                taxonVertex.AddP(TP.TotalLifeSpan, taxonTraits.TotalLifeSpan.Value);
            }

            if (taxonTraits.TrophicLevel != null)
            {
                foreach (var trophicLevel in taxonTraits.TrophicLevel)
                {
                    AddTaxonTrait(g, taxonVertex, VL.AETL, TEL.Er, CodePrefixes.GetTrophicLevelCode(trophicLevel), trophicLevel);
                }
            }
        }

        private static void AddTaxonTrait(G g, Vertex taxonVertex, int traitVertexLabel, int edgeLabel, string traitCode, string traitName)
        {
            if (!g.TryGetV(traitCode, out var traitVertex))
            {
                traitVertex = g.AddV(traitVertexLabel, traitCode).AddP(P.Name, traitName);
            }

            taxonVertex.AddE(edgeLabel, traitVertex);
        }

        private static void LinkTaxonVertex<T>(G g, Vertex taxonVertex, T[] idsToConnect, Func<T, string> codeCreator)
        {
            if (idsToConnect == null || idsToConnect.Length == 0)
            {
                return;
            }

            foreach (var id in idsToConnect)
            {
                var code = codeCreator(id);

                if (!g.TryGetV(code, out var vertexToLink))
                {
                    // Log
                    continue;
                }

                vertexToLink.AddE(EL.In, taxonVertex);
            }
        }

        private static void ProcessCodes(G g, Codes codeMap, int vertexLabel, Vertex parentTreeNodeVertex)
        {
            var codes = codeMap.GetValues();

            foreach (var code in codes)
            {
                if (!g.TryGetV(code.Code, out var vertex))
                {
                    vertex = g.AddV(vertexLabel, code.Code).AddP(P.Name, code.Name);
                }

                if (!string.IsNullOrWhiteSpace(code.ParentCode))
                {
                    if (!g.TryGetV(code.ParentCode, out var parentVertex))
                    {
                        var parent = codeMap.GetCode(code.ParentCode);
                        parentVertex = g.AddV(vertexLabel, parent.Code).AddP(P.Name, parent.Name);
                    }

                    vertex.AddE(EL.Child, parentVertex);
                }
                else if (parentTreeNodeVertex != null)
                {
                    vertex.AddE(EL.Child, parentTreeNodeVertex);
                }
            }
        }

        private static void LinkNatureAreaIds(G g, Vertex vertex, List<int> natureAreaIds)
        {
            if (natureAreaIds == null || natureAreaIds.Count == 0)
            {
                return;
            }

            foreach (var natureAreaId in natureAreaIds)
            {
                string code = CodePrefixes.GetNatureAreaCode(natureAreaId);

                var naVertex = g.V(code);

                vertex.AddE(EL.In, naVertex);
            }
        }
    }
}