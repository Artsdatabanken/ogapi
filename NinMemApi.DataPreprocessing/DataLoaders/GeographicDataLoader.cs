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
    public static class GeographicalAreaLoader
    {
        public static async Task<GeographicalAreaData> Load(string connectionString)
        {
            const string geographicalAreaSql =
                @"SELECT ol.naturområde_id AS NatureAreaId, o.geometriType_id AS GeometryTypeId, o.nummer AS Number, o.navn AS Name, o.kategori AS Category
                    FROM Område o
                    LEFT JOIN OmrådeLink ol ON ol.geometri_id = o.id";

            IEnumerable<GeographicalAreaDto> geographicalAreaDtos = null;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                geographicalAreaDtos = await conn.QueryAsync<GeographicalAreaDto>(geographicalAreaSql);
            }

            List<County> counties = new List<County>();
            List<AreaCategory> conservationAreaCategories = new List<AreaCategory>();

            foreach (var dto in geographicalAreaDtos)
            {
                switch (dto.GeometryTypeId)
                {
                    case 1:
                        MapEntityMunicipality(dto, counties);
                        break;

                    case 2:
                        MapEntityCounty(dto, counties);
                        break;

                    case 3:
                        MapEntityConservationArea(dto, conservationAreaCategories);
                        break;

                    default:
                        throw new Exception($"Unexpected geometryTypeId: { dto.GeometryTypeId }");
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
            int countyNo = dto.Number / 100;
            var county = counties.Single(na => na.Number == countyNo);

            var municipality = county.Municipalities.FirstOrDefault(m => m.Number == dto.Number);

            if (municipality == null)
            {
                municipality = new Municipality
                {
                    Number = dto.Number,
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
            var county = counties.FirstOrDefault(na => na.Number == dto.Number);

            if (county == null)
            {
                county = new County
                {
                    Number = dto.Number,
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

            var conservationArea = conservationAreaCategory.ConservationAreas.FirstOrDefault(c => c.Number == dto.Number);

            if (conservationArea == null)
            {
                conservationArea = new ConservationArea
                {
                    Number = dto.Number,
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