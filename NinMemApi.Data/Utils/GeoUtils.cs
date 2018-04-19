using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Globalization;
using System.Linq;

namespace NinMemApi.Data.Utils
{
    public static class GeoUtils
    {
        public static Envelope ToEnvelope(string bbox)
        {
            var bboxArray = bbox
               .Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
               .ToArray();

            return new Envelope(bboxArray[0], bboxArray[2], bboxArray[1], bboxArray[3]);
        }

        public static IGeometry ToGeometry(Envelope envelope)
        {
            return GeometryFactory.Default.ToGeometry(envelope);
        }
    }
}