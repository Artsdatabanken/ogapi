using NetTopologySuite.Index.KdTree;
using NinMemApi.Data.Models;
using System.Collections.Generic;

namespace NinMemApi.Data
{
    public static class KdTreeBuilder
    {
        public static KdTree<string> Build(IEnumerable<Taxon> taxons)
        {
            var kdTree = new KdTree<string>();

            foreach (var taxon in taxons)
            {
                var taxonCode = CodePrefixes.GetTaxonCode(taxon.ScientificNameId);

                foreach (var eastNorth in taxon.EastNorths)
                {
                    kdTree.Insert(new GeoAPI.Geometries.Coordinate(eastNorth[0], eastNorth[1]), taxonCode);
                }
            }

            return kdTree;
        }
    }
}