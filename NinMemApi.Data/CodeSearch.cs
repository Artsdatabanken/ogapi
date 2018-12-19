using NetTopologySuite.Index.KdTree;
using NetTopologySuite.Index.Strtree;
using NinMemApi.Data.Elements;
using NinMemApi.Data.Elements.Properties;
using NinMemApi.Data.Models;
using NinMemApi.Data.Utils;
using NinMemApi.GraphDb;
using NinMemApi.GraphDb.Labels;
using System;
using System.Collections.Generic;
using System.Linq;
using Trees;

namespace NinMemApi.Data
{
    public class CodeSearch
    {
        private readonly Trie<string> _trie;
        private readonly STRtree<string> _stRtree;
        private readonly KdTree<string> _kdTree;
        private readonly G _g;

        public CodeSearch(G g, KdTree<string> kdTree, STRtree<string> stRtree)
        {
            _g = g;

            Trie<string> trie = new Trie<string>();

            foreach (var v in _g.V().Where(v => v.Label != VL.NA))
            {
                trie.Insert(v.Id, v.Id, TrieFields.Code);

                if (v.Label == VL.T)
                {
                    var names = v.Value<IDictionary<string, string>>(TP.Names);

                    foreach (var kvp in names)
                    {
                        trie.Insert(kvp.Value, v.Id, kvp.Key);
                    }
                }
                else
                {
                    trie.Insert(v.Name, v.Id, "nb");
                }
            }

            _trie = trie;
            _kdTree = kdTree;
            _stRtree = stRtree;
        }

        public KodeNavn[] GetCodeNamesByFreeText(string search, int limit = 10)
        {
            if (string.IsNullOrEmpty(search))
            {
                throw new ArgumentException("Søkestrengen kan ikke være tom.");
            }

            string[] resultSet = SearchByFreeText(search);

            if (resultSet.Length == 0)
            {
                return new KodeNavn[0];
            }

            return ToCodeNames(resultSet, limit);
        }

        public string[] GetNatureAreaTaxonCodesByCodesAndBbox(string codes, string bbox)
        {
            var resultSet = SearchByBbox(bbox); // taxon and/or nature areas

            // TODO: Optimalize to limit work to what's returned by the bbox

            var resultSetByCodes = SearchByCodes(codes); // taxon and/or nature areas

            if (resultSet.Count == 0)
            {
                resultSet = resultSetByCodes;
            }
            else if (resultSetByCodes.Count > 0)
            {
                resultSet.IntersectWith(resultSetByCodes);
            }

            return resultSet.ToArray();
        }

        private HashSet<string> SearchByCodes(string codes)
        {
            var resultSet = new HashSet<string>();

            var groupedCodes = GetGroupedSearchCodes(codes);

            if (groupedCodes.Count == 0)
            {
                return resultSet;
            }

            foreach (var pair in groupedCodes)
            {
                var hashSet = new HashSet<string>();

                foreach (var code in pair.Value)
                {
                    var set = GetNatureAreaAndTaxonCodes(code).All;

                    if (set.Count > 0)
                    {
                        hashSet.UnionWith(set);
                    }
                }

                if (resultSet.Count == 0)
                {
                    resultSet = hashSet;
                }
                else if (hashSet.Count > 0)
                {
                    resultSet.IntersectWith(hashSet);
                }
            }

            return resultSet;
        }

        public Dictionary<string, HashSet<string>> GetGroupedSearchCodes(string codes)
        {
            var groupedCodes = new Dictionary<string, HashSet<string>>();

            if (string.IsNullOrWhiteSpace(codes))
            {
                return groupedCodes;
            }

            var codeSet = new HashSet<string>();

            if (!string.IsNullOrWhiteSpace(codes))
            {
                var parsedCode = codes.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var code in parsedCode)
                {
                    codeSet.Add(code);
                }
            }

            if (codeSet.Count == 0)
            {
                return groupedCodes;
            }

            foreach (var code in codeSet)
            {
                char delimiter = Char.IsDigit(code[0]) ? '_' : '-';
                int indexOfDelimiter = code.IndexOf(delimiter);
                string prefix = indexOfDelimiter == -1 ? code : code.Substring(0, indexOfDelimiter);

                if (!groupedCodes.TryGetValue(prefix, out var set))
                {
                    set = new HashSet<string>();
                    groupedCodes.Add(prefix, set);
                }

                set.Add(code);
            }

            return groupedCodes;
        }

        public HashSet<string> SearchByBbox(string bbox)
        {
            if (string.IsNullOrWhiteSpace(bbox))
            {
                return new HashSet<string>();
            }

            return GetCodesFromGeo(bbox);
        }

        private string[] SearchByFreeText(string search)
        {
            return _trie.Search(search);
        }

        private NatureAreaTaxonCodes GetNatureAreaAndTaxonCodes(params string[] codes)
        {
            var result = new NatureAreaTaxonCodes();

            foreach (string code in codes)
            {
                var vertex = _g.V(code);

                var stack = new Stack<Vertex>();
                stack.Push(vertex);
                var processedVertices = new HashSet<string>();

                while (stack.Count > 0)
                {
                    vertex = stack.Pop();
                    processedVertices.Add(vertex.Id);

                    AddNatureAreaOrTaxon(result, vertex);

                    var inVertices = vertex.Out(EL.In);

                    foreach (var inVertex in inVertices)
                    {
                        if (!processedVertices.Contains(inVertex.Id))
                        {
                            AddNatureAreaOrTaxon(result, inVertex);
                        }
                    }

                    var children = vertex.In(EL.Child);

                    foreach (var child in children)
                    {
                        if (!processedVertices.Contains(child.Id))
                        {
                            stack.Push(child);
                        }
                    }
                }
            }

            return result;
        }

        private static void AddNatureAreaOrTaxon(NatureAreaTaxonCodes result, Vertex vertex)
        {
            if (vertex.Label == VL.T)
            {
                result.Taxons.Add(vertex.Id);
            }
            else if (vertex.Label == VL.NA)
            {
                result.NatureAreas.Add(vertex.Id);

                AddTaxonVerticesIn(result, vertex);
            }
            else if ((vertex.Label == VL.AA && vertex.Id.Contains("-")) || vertex.Label == VL.CA)
            {
                AddTaxonVerticesIn(result, vertex);
            }
        }

        private static void AddTaxonVerticesIn(NatureAreaTaxonCodes result, Vertex vertex)
        {
            var taxonVerticeIdsIn = vertex.OutId(EL.In, VL.T);

            foreach (var taxonVertexId in taxonVerticeIdsIn)
            {
                result.Taxons.Add(taxonVertexId);
            }
        }

        private HashSet<string> GetCodesFromGeo(string bbox)
        {
            var envelope = GeoUtils.ToEnvelope(bbox);

            var codesFromPoints = _kdTree.Query(envelope);
            var codesFromEnvelopes = _stRtree.Query(envelope);

            HashSet<string> codesFromGeo = new HashSet<string>();

            foreach (var code in codesFromPoints)
            {
                codesFromGeo.Add(code.Data);
            }

            foreach (var code in codesFromEnvelopes)
            {
                codesFromGeo.Add(code);
            }

            return codesFromGeo;
        }

        private KodeNavn[] ToCodeNames(string[] resultSet, int? limit = null)
        {
            var reorderedResultSet = resultSet.Where(c => c.StartsWith("na-")).Concat(resultSet.Where(c => !c.StartsWith("na-")));

            var list = new List<KodeNavn>();

            foreach (string code in reorderedResultSet)
            {
                var vertex = _g.V(code);

                var kodenavn = CreateKodeNavn(vertex);

                if (vertex.Parent != null)
                {
                    kodenavn.Forelder = CreateKodeNavn(vertex.Parent);
                }

                list.Add(kodenavn);

                if (limit.HasValue && limit.Value == list.Count)
                {
                    break;
                }
            }

            return list.ToArray();
        }

        private static KodeNavn CreateKodeNavn(Vertex vertex)
        {
            IDictionary<string, string> names = null;

            if (vertex.Label == VL.T)
            {
                names = vertex.Value<IDictionary<string, string>>(TP.Names);
            }
            else
            {
                names = new Dictionary<string, string> { { "nb", vertex.Name } };
            }

            return new KodeNavn(vertex.Id, names);
        }
    }

    public class NatureAreaTaxonCodes
    {
        public NatureAreaTaxonCodes()
        {
            Taxons = new HashSet<string>();
            NatureAreas = new HashSet<string>();
        }

        public HashSet<string> Taxons { get; set; }
        public HashSet<string> NatureAreas { get; set; }

        public HashSet<string> All
        {
            get
            {
                return Taxons.Union(NatureAreas).ToHashSet();
            }
        }
    }
}