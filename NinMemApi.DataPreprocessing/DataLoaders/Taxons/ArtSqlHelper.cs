using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace NinMemApi.DataPreprocessing.DataLoaders.Taxons
{
    public class ArtSqlHelper
    {
        private readonly string _connectionString;

        public ArtSqlHelper(string connectionString = "Initial Catalog=Artskart2Index;Server=.;Integrated Security=true;")
        {
            _connectionString = connectionString;
        }

        public TaxonSqlData[] Get()
        {
            const string sql =
                @"SELECT t.Id AS TaxonId, p.ValidScientificNameId AS ParentScientificNameId, t.ValidScientificName AS ScientificName,
	                t.ValidScientificNameId AS ScientificNameId, t.PrefferedPopularname AS PopularName
	                FROM Taxon t
	                LEFT JOIN Taxon p ON p.Id = t.ParentTaxonId";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var taxons = conn.Query<TaxonSqlData>(sql).ToArray();

                var taxonDict = taxons.ToDictionary(t => t.TaxonId, t => t);

                var eastNorths = conn.Query<EastNorthDto>("SELECT t.Id AS TaxonId, o.East, o.North FROM Taxon t JOIN Observation o ON o.TaxonId = t.Id");

                foreach (var eastNorth in eastNorths)
                {
                    var set = taxonDict[eastNorth.TaxonId].EastNorths;

                    var tuple = (eastNorth.East, eastNorth.North);

                    set.Add(tuple);
                }

                //const string natureAreaConnectionSql =
                //    @"SELECT t.Id AS TaxonId, nom.id AS NatureAreaId
                //      FROM
                //      [Artskart2Index].[dbo].[Taxon] t,
                //      [Artskart2Index].[dbo].[Observation] obs,
                //      [Artskart2Index].[dbo].[Location] loc,
                //      [NiN_Init].[dbo].[Naturområde] nom
                //      WHERE t.Id = obs.TaxonId AND obs.LocationId = loc.Id AND loc.Geometry.STWithin(nom.geometri) = 1";

                //var natureAreaConnections = conn.Query<TaxonNatureAreaConnectionDto>(natureAreaConnectionSql, commandTimeout: 2 * 60);

                //foreach (var naConnection in natureAreaConnections)
                //{
                //    var set = taxonDict[naConnection.TaxonId].NatureAreas;

                //    set.Add(naConnection.NatureAreaId);
                //}

                const string geographicalAreaConnectionSql =
                    @"SELECT t.Id AS TaxonId, o.geometriType_id AS GeometryTypeId, o.nummer AS Number
	                FROM
	                [Artskart2Index].[dbo].[Taxon] t,
	                [Artskart2Index].[dbo].[Observation] obs,
	                [Artskart2Index].[dbo].[Location] loc,
	                [NiN_Init].[dbo].[Område] o
	                WHERE t.Id = obs.TaxonId AND obs.LocationId = loc.Id
	                AND loc.Geometry.STIntersects(o.geometri) = 1
	                AND (o.geometriType_id = 1 OR o.geometriType_id = 3)";

                var geoAreaConnections = conn.Query<TaxonGeoAreaConnectionDto>(geographicalAreaConnectionSql, commandTimeout: 10 * 60);

                foreach (var geoAreaConnection in geoAreaConnections)
                {
                    var taxon = taxonDict[geoAreaConnection.TaxonId];

                    if (geoAreaConnection.GeometryTypeId == 1)
                    {
                        taxon.Municipalities.Add(geoAreaConnection.Number);
                    }
                    else
                    {
                        taxon.ConservationAreas.Add(geoAreaConnection.Number);
                    }
                }

                return taxons;
            }
        }
    }

    public class TaxonSqlData
    {
        public TaxonSqlData()
        {
            EastNorths = new HashSet<(int east, int north)>();
            NatureAreas = new HashSet<int>();
            Municipalities = new HashSet<int>();
            ConservationAreas = new HashSet<int>();
        }

        public int TaxonId { get; set; }
        public int ParentScientificNameId { get; set; }
        public int ScientificNameId { get; set; }
        public string ScientificName { get; set; }
        public string PopularName { get; set; }
        public HashSet<(int east, int north)> EastNorths { get; set; }
        public HashSet<int> NatureAreas { get; set; }
        public HashSet<int> Municipalities { get; set; }
        public HashSet<int> ConservationAreas { get; set; }
    }

    public class EastNorthDto
    {
        public int TaxonId { get; set; }
        public int East { get; set; }
        public int North { get; set; }
    }

    public class TaxonNatureAreaConnectionDto
    {
        public int TaxonId { get; set; }
        public int NatureAreaId { get; set; }
    }

    public class TaxonGeoAreaConnectionDto
    {
        public int TaxonId { get; set; }
        public int GeometryTypeId { get; set; }
        public int Number { get; set; }
    }
}