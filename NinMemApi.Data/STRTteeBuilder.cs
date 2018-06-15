using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO;
using NinMemApi.Data.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.Data
{
    public static class STRTteeBuilder
    {
        public static STRtree<string> Build(IEnumerable<NatureAreaDto> natureAreas)
        {
            var stRtree = new STRtree<string>();

            //// TODO: Needed for NetTopologySuite > 1.14
            //NetTopologySuiteBootstrapper.Bootstrap();
            var reader = new WKTReader(new GeometryFactory());

            var parsedEnvelopes = new ConcurrentDictionary<string, Envelope>();

            Parallel.ForEach(natureAreas, (na) =>
            {
                var envelopeGeometry = reader.Read(na.Envelope);
                parsedEnvelopes.TryAdd(CodePrefixes.GetNatureAreaCode(na.Id), envelopeGeometry.EnvelopeInternal);
            });

            foreach (var kvp in parsedEnvelopes)
            {
                stRtree.Insert(kvp.Value, kvp.Key);
            }

            return stRtree;
        }
    }
}