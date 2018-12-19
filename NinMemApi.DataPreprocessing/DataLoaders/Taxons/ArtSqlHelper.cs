using Dapper;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace NinMemApi.DataPreprocessing.DataLoaders.Taxons
{
    public class ArtSqlHelper
    {
        private readonly string _connectionString;

        public ArtSqlHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public TaxonSqlData[] Get()
        {
            const string sql =@"SELECT 
	s.code AS TaxonId, 
	ch.predecessor AS ParentScientificNameId, 
	s.title AS ScientificName,
	s.code AS ScientificNameId, 
	s.title AS PopularName
FROM 
	data.codeshierarchy ch, 
	data.codes p, 
	data.codes s
WHERE 
	ch.successor = s.code and 
	ch.predecessor = p.code and 
	p.level = s.level - 1 and
	s.code like 'AR_%' and
	p.code like 'AR_%'";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                var taxons = conn.Query<TaxonSqlData>(sql).ToArray();

                var taxonDict = taxons.ToDictionary(t => t.TaxonId.Remove(0,3), t => t);

                var eastNorths = conn.Query<EastNorthDto>("SELECT t.code AS TaxonId, st_X(st_centroid(st_transform(o.geography::geometry, 25833)))::integer as East, st_Y(st_centroid(st_transform(o.geography::geometry, 25833)))::integer as North FROM data.codes_geometry t JOIN data.geometry o ON o.id = t.geometry_id where t.code like 'AR_%'");

                foreach (var eastNorth in eastNorths)
                {
                    
                    if (!taxonDict.ContainsKey(eastNorth.TaxonId.Remove(0, 3)))
                        taxonDict[eastNorth.TaxonId.Remove(0, 3)] = new TaxonSqlData();

                    var set = taxonDict[eastNorth.TaxonId.Remove(0,3)].EastNorths;
                    
                    var tuple = (eastNorth.East, eastNorth.North);

                    set.Add(tuple);
                }

                var geographicalAreaConnectionSql = @"select 
cg_art.code as TaxonId,
cg_omr.code as Number
                from 
data.geometry art,
data.geometry omr,
data.codes_geometry cg_art,
data.codes_geometry cg_omr
where
cg_art.code like 'AR-%' AND
( cg_omr.code like 'VV-%' OR cg_omr.code like 'AO-%') AND
art.id = cg_art.geometry_id AND
omr.id = cg_omr.geometry_id AND
st_intersects(art.geography, omr.geography)";

                var geoAreaConnections = conn.Query<TaxonGeoAreaConnectionDto>(geographicalAreaConnectionSql, commandTimeout: 24 * 60 * 60);

                foreach (var geoAreaConnection in geoAreaConnections)
                {
                    var taxon = taxonDict[geoAreaConnection.TaxonId.Remove(0,3)];

                    if (geoAreaConnection.Number.StartsWith("AO-"))
                    {
                        taxon.Municipalities.Add(int.Parse(geoAreaConnection.Number.Remove(0,3).Replace("-","")));
                    }
                    else
                    {
                        taxon.ConservationAreas.Add(int.Parse(geoAreaConnection.Number.Remove(0,3)));
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

        public string TaxonId { get; set; }
        public string ParentScientificNameId { get; set; }
        public string ScientificNameId { get; set; }
        public string ScientificName { get; set; }
        public string PopularName { get; set; }
        public HashSet<(int east, int north)> EastNorths { get; set; }
        public HashSet<int> NatureAreas { get; set; }
        public HashSet<int> Municipalities { get; set; }
        public HashSet<int> ConservationAreas { get; set; }
    }

    public class EastNorthDto
    {
        public string TaxonId { get; set; }
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
        public string TaxonId { get; set; }
        public string Number { get; set; }
    }
}