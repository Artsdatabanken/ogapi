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
                            @"SELECT bv.code AS DvCode, nomt.code AS NatCode, bv.geometry_id AS NatureAreaId
                    FROM data.codes_geometry bv, data.codes_geometry nomt
                    WHERE nomt.geometry_id = bv.geometry_id
                    AND nomt.code LIKE 'NA%'
                    AND nomt.code NOT LIKE 'NA-BS%'
                    AND nomt.code NOT LIKE 'NA-LKM%'
                    AND bv.code LIKE 'NA-BS%'
                    GROUP BY bv.code, nomt.code, bv.geometry_id";

            IEnumerable<DescriptionVariableDto> descriptionVariableDtos;

            using (var conn = new NpgsqlConnection(connectionString))
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
            // TODO: Add in mapped if we get it in the database
            const string natureAreaTypeCodeSql =
            @"SELECT nat.geometry_id AS NatureAreaId, nat.code AS Code, nat.fraction AS Percentage, null AS Mapped
FROM data.codes_geometry nat where nat.code LIKE 'NA%' AND nat.code NOT LIKE 'NA-BS%' AND nat.code NOT LIKE 'NA-LKM%'";

            IEnumerable<NatureAreaTypeDto> natureAreaTypeDtos = null;

            using (var conn = new NpgsqlConnection(connectionString))
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