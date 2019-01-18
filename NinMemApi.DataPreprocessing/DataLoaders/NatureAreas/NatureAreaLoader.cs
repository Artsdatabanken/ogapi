using System;
using Dapper;
using NinMemApi.DataPreprocessing.DataLoaders.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace NinMemApi.DataPreprocessing.DataLoaders.NatureAreas
{
    public static class NatureAreaLoader
    {
        public static async Task<List<NatureAreaDto>> Load(string connectionString)
        {
            const string sql = "SELECT g.id AS Id, ST_Area(g.geography) AS Area, ST_AsText(ST_Envelope(st_transform(g.geography::geometry, 25833))) AS Envelope FROM data.geometry g, data.dataset d, data.prefix p where p.value like 'NA%' AND p.value NOT LIKE 'NA-BS%' AND p.value NOT LIKE 'NA-LKM%' AND d.prefix_id = p.id and g.dataset_id = d.id";

            List<NatureAreaDto> natureAreaDtos = null;

            Console.WriteLine("Loading NatureAreas");

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                natureAreaDtos = (await conn.QueryAsync<NatureAreaDto>(sql)).ToList();
            }

            Console.WriteLine("Finished loading NatureAreas");

            return natureAreaDtos;
        }
    }
}