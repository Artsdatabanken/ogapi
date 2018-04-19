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
    public static class DescriptionVariableLoader
    {
        public static async Task<List<DescriptionVariable>> Load(string connectionString, Codes descriptionVariableCodes)
        {
            const string descriptionVariableSql =
                @"SELECT bv.kode AS DvCode, nomt.kode AS NatCode, bv.naturområde_id AS NatureAreaId, nomt.andel AS NatureAreaPercentage
                    FROM Beskrivelsesvariabel bv
                    JOIN Naturområdetype nomt ON nomt.id = bv.naturområdetype_id
                    GROUP BY bv.kode, nomt.kode, bv.naturområde_id, nomt.andel";

            IEnumerable<DescriptionVariableDto> descriptionVariableDtos;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                descriptionVariableDtos = await conn.QueryAsync<DescriptionVariableDto>(descriptionVariableSql);
            }

            var dict = new Dictionary<string, DescriptionVariable>();
            var natureAreas = new Dictionary<(string, int, string), NatureAreaIdTypePercentage>();

            foreach (var dto in descriptionVariableDtos)
            {
                var codes = dto.DvCode.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var code in codes)
                {
                    if (!descriptionVariableCodes.Contains(dto.DvCode))
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

            return dict.Values.ToList();
        }
    }
}