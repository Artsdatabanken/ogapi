using Dapper;
using NinMemApi.Data.Models;
using NinMemApi.DataPreprocessing.DataLoaders.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace NinMemApi.DataPreprocessing.DataLoaders
{
    public static class NatureAreaTypeLoader
    {
        public static async Task<List<NatureAreaType>> Load(string connectionString, Codes codes)
        {
            const string natureAreaTypeCodeSql =
@"SELECT nat.geometry_id AS NatureAreaId, nat.code AS Code, nat.fraction AS Percentage
FROM data.codes_geometry nat
WHERE nat.code LIKE 'NA-%' and nat.code not LIKE 'NA-BS%' and nat.code not LIKE 'NA-LKM%'
GROUP BY nat.geometry_id, nat.code, nat.fraction";

            IEnumerable<NatureAreaTypeDto> dtos = null;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                dtos = await conn.QueryAsync<NatureAreaTypeDto>(natureAreaTypeCodeSql);
            }

            var dict = new Dictionary<string, NatureAreaType>();
            var natureAreaPercentages = new Dictionary<(int, string), NatureAreaPercentage>();

            foreach (var dto in dtos)
            {
                if (!codes.Contains(dto.Code))
                {
                    continue;
                }

                if (!dict.TryGetValue(dto.Code, out var natureAreaType))
                {
                    natureAreaType = new NatureAreaType(dto.Code, codes.GetCode(dto.Code).Name);

                    dict.Add(dto.Code, natureAreaType);
                }

                var key = (dto.NatureAreaId, dto.Code);

                if (natureAreaPercentages.TryGetValue(key, out var natureArea))
                {
                    double newPercentage = natureArea.Percentage + (dto.Percentage / 10);

                    if (newPercentage > 1)
                    {
                        Console.WriteLine($"{key.Code} {key.NatureAreaId} exceeds 1 as percentage.");
                    }
                    else
                    {
                        natureArea.Percentage = newPercentage;
                    }
                }
                else
                {
                    var natureAreaPercentage = new NatureAreaPercentage { NatureAreaId = dto.NatureAreaId, Percentage = dto.Percentage / 10 };
                    natureAreaType.NatureAreas.Add(natureAreaPercentage);
                    natureAreaPercentages.Add(key, natureAreaPercentage);
                }
            }

            return dict.Values.ToList();
        }
    }
}