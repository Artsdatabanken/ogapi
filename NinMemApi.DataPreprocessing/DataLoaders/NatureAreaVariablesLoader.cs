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
    public static class NatureAreaVariablesLoader
    {
        public static async Task<List<NatureAreaVariables>> Load(string connectionString)
        {
            var dict = await GetDict(connectionString);

            await AddDescriptionVariables(connectionString, dict);

            var validationDict = new Dictionary<int, double>();

            foreach (var nav in dict.Values)
            {
                if (!validationDict.ContainsKey(nav.NatureAreaId))
                {
                    validationDict.Add(nav.NatureAreaId, 0);
                }

                validationDict[nav.NatureAreaId] += nav.Percentage;

                if (validationDict[nav.NatureAreaId] > 1)
                {
                    Console.WriteLine($"{ nav.NatureAreaId } is {validationDict[nav.NatureAreaId]}");
                }
            }

            return dict.Values.ToList();
        }

        private static async Task AddDescriptionVariables(string connectionString, Dictionary<(int, string), NatureAreaVariables> dict)
        {
            const string descriptionVariableSql =
                            @"SELECT bv.kode AS DvCode, nomt.kode AS NatCode, bv.naturområde_id AS NatureAreaId
                    FROM Beskrivelsesvariabel bv
                    JOIN Naturområdetype nomt ON nomt.id = bv.naturområdetype_id
                    GROUP BY bv.kode, nomt.kode, bv.naturområde_id";

            IEnumerable<DescriptionVariableDto> descriptionVariableDtos;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                descriptionVariableDtos = await conn.QueryAsync<DescriptionVariableDto>(descriptionVariableSql);
            }

            foreach (var dto in descriptionVariableDtos)
            {
                var codes = dto.DvCode.Split(',', StringSplitOptions.RemoveEmptyEntries);

                var key = (dto.NatureAreaId, dto.NatCode);

                var variables = dict[key];

                foreach (var code in codes)
                {
                    variables.DescriptionVariables.Add(code);
                }
            }
        }

        private static async Task<Dictionary<(int, string), NatureAreaVariables>> GetDict(string connectionString)
        {
            const string natureAreaTypeCodeSql =
            @"SELECT nat.naturområde_id AS NatureAreaId, nat.kode AS Code, nat.andel AS Percentage, nat.kartlagt AS Mapped
FROM NaturområdeType nat";

            IEnumerable<NatureAreaTypeDto> natureAreaTypeDtos = null;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                natureAreaTypeDtos = await conn.QueryAsync<NatureAreaTypeDto>(natureAreaTypeCodeSql);
            }

            var dict = new Dictionary<(int, string), NatureAreaVariables>();

            foreach (var dto in natureAreaTypeDtos)
            {
                var key = (dto.NatureAreaId, dto.Code);

                if (dict.ContainsKey(key))
                {
                    var value = dict[key];

                    if ((!value.Mapped.HasValue && dto.Mapped.HasValue)
                        || (value.Mapped.HasValue && dto.Mapped.HasValue && dto.Mapped.Value > value.Mapped.Value))
                    {
                        dict[key] = ToNatureAreaVariables(dto);
                    }
                }
                else
                {
                    dict.Add(key, ToNatureAreaVariables(dto));
                }
            }

            return dict;
        }

        private static NatureAreaVariables ToNatureAreaVariables(NatureAreaTypeDto dto)
        {
            return new NatureAreaVariables
            {
                Mapped = dto.Mapped,
                NatureAreaId = dto.NatureAreaId,
                NatureAreaTypeCode = dto.Code,
                Percentage = dto.Percentage / 10
            };
        }
    }
}