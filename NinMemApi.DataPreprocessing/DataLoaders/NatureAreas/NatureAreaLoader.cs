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
            const string sql = "SELECT id AS Id, ST_Area(g.geography) AS Area, ST_AsText(ST_Envelope(g.geography::geometry)) AS Envelope FROM data.geometry g";

            List<NatureAreaDto> natureAreaDtos = null;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                natureAreaDtos = (await conn.QueryAsync<NatureAreaDto>(sql)).ToList();
            }

            return natureAreaDtos;
        }
    }
}