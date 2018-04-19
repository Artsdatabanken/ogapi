using Dapper;
using NinMemApi.DataPreprocessing.DataLoaders.Dtos;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace NinMemApi.DataPreprocessing.DataLoaders.NatureAreas
{
    public static class NatureAreaLoader
    {
        public static async Task<List<NatureAreaDto>> Load(string connectionString)
        {
            const string sql = "SELECT id AS Id, geometri.STArea() AS Area, geometri.STEnvelope().STAsText() AS Envelope FROM Naturområde";

            List<NatureAreaDto> natureAreaDtos = null;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                natureAreaDtos = (await conn.QueryAsync<NatureAreaDto>(sql)).ToList();
            }

            return natureAreaDtos;
        }
    }
}