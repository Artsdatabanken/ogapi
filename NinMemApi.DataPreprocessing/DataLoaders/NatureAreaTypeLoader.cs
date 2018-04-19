using Dapper;
using NinMemApi.Data.Models;
using NinMemApi.DataPreprocessing.DataLoaders.Dtos;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace NinMemApi.DataPreprocessing.DataLoaders
{
    public static class NatureAreaTypeLoader
    {
        public static async Task<List<NatureAreaType>> Load(string connectionString, Codes codes)
        {
            const string natureAreaTypeCodeSql =
@"SELECT nat.naturområde_id AS NatureAreaId, nat.kode AS Code, nat.andel AS Percentage
FROM NaturområdeType nat
GROUP BY nat.naturområde_id, nat.kode, nat.andel";

            IEnumerable<NatureAreaTypeDto> dtos = null;

            using (var conn = new SqlConnection(connectionString))
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