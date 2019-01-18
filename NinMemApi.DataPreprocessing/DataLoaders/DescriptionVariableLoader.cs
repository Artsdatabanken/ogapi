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
    public static class DescriptionVariableLoader
    {
        public static async Task<List<DescriptionVariable>> Load(string connectionString, Codes descriptionVariableCodes)
        {
            Console.WriteLine("Loading DescriptionVariables");

            const string descriptionVariableSql =
                @"SELECT bv.code AS DvCode, nomt.code AS NatCode, bv.geometry_id AS NatureAreaId, nomt.fraction AS NatureAreaPercentage
                    FROM data.codes_geometry bv, data.codes_geometry nomt
                    WHERE bv.geometry_id = nomt.geometry_id and bv.code like 'NA-BS%' and nomt.code like 'NA-%' and nomt.code NOT like 'NA-BS%' and nomt.code NOT like 'NA-LKM%'
                    GROUP BY bv.code, nomt.code, bv.geometry_id, nomt.fraction";

            IEnumerable<DescriptionVariableDto> descriptionVariableDtos;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                descriptionVariableDtos = await conn.QueryAsync<DescriptionVariableDto>(descriptionVariableSql);
            }

            var dict = new Dictionary<string, DescriptionVariable>();
            var natureAreas = new Dictionary<(string, int, string), NatureAreaIdTypePercentage>();

            foreach (var dto in descriptionVariableDtos)
            {
                var codes = dto.DvCode.Split(',', StringSplitOptions.RemoveEmptyEntries);

                //dto.DvCode = dto.DvCode.Remove(0,3);

                foreach (var code in codes)
                {
                    if (!descriptionVariableCodes.Contains(dto.DvCode.ToUpper()))
                    {
                        // Ignored code as it doesn't exist in the code file.
                        continue;
                    }

                    if (!dict.TryGetValue(dto.DvCode, out var descriptionVariable))
                    {
                        descriptionVariable = new DescriptionVariable(dto.DvCode, descriptionVariableCodes.GetCode(dto.DvCode).Name);

                        dict.Add(dto.DvCode, descriptionVariable);
                    }

                    var key = (dto.DvCode, dto.NatureAreaId, dto.NatCode);
                    // descriptionVariable.NatureAreas.FirstOrDefault(na => na.NatureAreaId == dto.NatureAreaId && na.NatureAreaTypeCode == dto.NatCode);

                    if (natureAreas.TryGetValue(key, out var natureArea))
                    {
                        double newPercentage = natureArea.NatureAreaPercentage + (dto.NatureAreaPercentage / 10);

                        if (newPercentage > 1)
                        {
                            Console.WriteLine($"{key.DvCode} {key.NatCode} {key.NatureAreaId} exceeds 1 as percentage.");
                        }
                        else
                        {
                            natureArea.NatureAreaPercentage = newPercentage;
                        }
                    }
                    else
                    {
                        natureArea = new NatureAreaIdTypePercentage
                        {
                            NatureAreaId = dto.NatureAreaId,
                            NatureAreaTypeCode = dto.NatCode,
                            NatureAreaPercentage = dto.NatureAreaPercentage / 10
                        };

                        descriptionVariable.NatureAreas.Add(natureArea);

                        natureAreas.Add(key, natureArea);
                    }
                }
            }

            Console.WriteLine("Finished loading DescriptionVariables");

            return dict.Values.ToList();
        }
    }
}