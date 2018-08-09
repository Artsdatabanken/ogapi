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
    public static class GeographicalAreaLoader
    {
        public static async Task<GeographicalAreaData> Load(string connectionString)
        {
            const string geographicalAreaSql =
                @"SELECT 
                    g2.id AS NatureAreaId, c_g.code AS Number, c.title AS Name, '' AS Category
                    FROM 
                    data.codes_geometry c_g, 
                    data.codes_geometry c_g2, 
                    data.codes c, 
                    data.geometry g, 
                    data.geometry g2
                    WHERE 
                    c_g.codes_id = c.id
                    AND c_g.geometry_id = g.id
                    AND (c_g.code LIKE 'AO_%' OR c_g.code LIKE 'VV_%')
                    AND c_g2.geometry_id = g2.id
                    AND (c_g2.code LIKE 'NA_%')
                    AND ST_Intersects(g.geography, g2.geography)
                    ORDER by c_g.code";

            IEnumerable<GeographicalAreaDto> geographicalAreaDtos = null;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                geographicalAreaDtos = await conn.QueryAsync<GeographicalAreaDto>(geographicalAreaSql);
            }

            List<County> counties = new List<County>();
            List<AreaCategory> conservationAreaCategories = new List<AreaCategory>();

            foreach (var dto in geographicalAreaDtos)
            {
                dto.Category = dto.Number.Substring(0, 2);
                dto.Number = dto.Number.Remove(0, 3).Replace("-", "");
                
                switch (dto.Category)
                {
                    case "AO":
                        if(int.Parse(dto.Number) < 100)  MapEntityCounty(dto, counties);
                        else MapEntityMunicipality(dto, counties);
                        break;

                    case "VV":
                        MapEntityConservationArea(dto, conservationAreaCategories);
                        break;

                    default:
                        throw new Exception($"Unexpected Category: { dto.Category }");
                }
            }

            return new GeographicalAreaData
            {
                Counties = counties,
                ConservationAreaCategories = conservationAreaCategories
            };
        }

        private static void MapEntityMunicipality(GeographicalAreaDto dto, List<County> counties)
        {
            int countyNo = int.Parse(dto.Number) / 100;
            var county = counties.Single(na => na.Number == countyNo);

            var municipality = county.Municipalities.FirstOrDefault(m => m.Number == int.Parse(dto.Number));

            if (municipality == null)
            {
                municipality = new Municipality
                {
                    Number = int.Parse(dto.Number),
                    Name = dto.Name
                };

                county.Municipalities.Add(municipality);
            }

            if (dto.NatureAreaId.HasValue)
            {
                municipality.NatureAreaIds.Add(dto.NatureAreaId.Value);
            }
        }

        private static void MapEntityCounty(GeographicalAreaDto dto, List<County> counties)
        {
            var county = counties.FirstOrDefault(na => na.Number == int.Parse(dto.Number));

            if (county == null)
            {
                county = new County
                {
                    Number = int.Parse(dto.Number),
                    Name = dto.Name
                };

                counties.Add(county);
            }
        }

        private static void MapEntityConservationArea(GeographicalAreaDto dto, List<AreaCategory> conservationAreaCategories)
        {
            var conservationAreaCategory = conservationAreaCategories.FirstOrDefault(c => c.ShortName == dto.Category);

            if (conservationAreaCategory == null)
            {
                conservationAreaCategory = new AreaCategory
                {
                    ShortName = dto.Category,
                    Name = AreaCodes.CodeToName(dto.Category)
                };

                conservationAreaCategories.Add(conservationAreaCategory);
            }

            var conservationArea = conservationAreaCategory.ConservationAreas.FirstOrDefault(c => c.Number ==int.Parse( dto.Number));

            if (conservationArea == null)
            {
                conservationArea = new ConservationArea
                {
                    Number = int.Parse(dto.Number),
                    Name = dto.Name
                };

                conservationAreaCategory.ConservationAreas.Add(conservationArea);
            }

            if (dto.NatureAreaId.HasValue)
            {
                conservationArea.NatureAreaIds.Add(dto.NatureAreaId.Value);
            }
        }
    }
}