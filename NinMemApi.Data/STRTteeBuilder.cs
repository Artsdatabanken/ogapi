using System;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Strtree;
using NinMemApi.Data.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace NinMemApi.Data
{
    public static class STRTteeBuilder
    {
        public static STRtree<string> Build(IEnumerable<NatureAreaDto> natureAreas)
        {
            var stRtree = new STRtree<string>();

            var parsedEnvelopes = new ConcurrentDictionary<string, Envelope>();

            Parallel.ForEach(natureAreas, (na) =>
            {
                var envelopeGeometry = ReadWkt(na.Envelope);
                parsedEnvelopes.TryAdd(CodePrefixes.GetNatureAreaCode(na.Id), envelopeGeometry);
            });

            foreach (var kvp in parsedEnvelopes)
            {
                stRtree.Insert(kvp.Value, kvp.Key);
            }

            return stRtree;
        }

        private static Envelope ReadWkt(string wkt)
        {
            var wktSplit = wkt.Split("((")[1];
            wktSplit = wktSplit.Split("))")[0];
            var coordinatesString = wktSplit.Split(',');

            var min = GetCoordinates(coordinatesString[0]);
            var max = GetCoordinates(coordinatesString[2]);

            return new Envelope(min[0], min[1], max[0], max[1]);
        }

        private static double[] GetCoordinates(string coordinatesString)
        {
            var coordinate = new double[2];
            var coordinatesStringSplit = coordinatesString.Trim().Split(' ');
            coordinate[0] = Convert.ToDouble(coordinatesStringSplit[0], CultureInfo.InvariantCulture);
            coordinate[1] = Convert.ToDouble(coordinatesStringSplit[1], CultureInfo.InvariantCulture);
            return coordinate;
        }
    }
}