using Dapper;
using NinMemApi.Data.Models;
using NinMemApi.DataPreprocessing.DataLoaders.Dtos;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace NinMemApi.DataPreprocessing.DataLoaders
{
    public static class RedlistLoader
    {
        public static async Task<(List<RedlistCategory> categories, List<RedlistTheme> themes)> Load(string connectionString)
        {
            const string redlistSql = "SELECT rlk.naturområde_id AS NatureAreaId, rlk.rødlistevurderingsenhet_id AS AssessmentUnitId, rlk.kategori_id AS CategoryId FROM Rødlistekategori rlk";
            const string redlistCategorySql =
 @"SELECT no.id AS NatureAreaId, rlk.kategori_id AS CategoryId, rlkt.verdi AS CategoryName
FROM Rødlistekategori rlk
JOIN KategoriSet rlkt ON rlkt.Id = rlk.kategori_id
JOIN NaturOmråde no ON no.id = rlk.naturområde_id";
            const string redlistAssessmentUnitSql =
 @"SELECT no.id AS NatureAreaId, rlk.rødlistevurderingsenhet_id AS AssessmentUnitId, rlvt.verdi AS AssessmentUnitName, rlvt.Tema_Id AS ThemeId, rltt.verdi AS ThemeName
FROM Rødlistekategori rlk
JOIN RødlisteVurderingsenhetSet rlvt ON rlvt.Id = rlk.rødlistevurderingsenhet_id
JOIN TemaSet rltt ON rltt.Id = rlvt.Tema_Id
JOIN NaturOmråde no ON no.id = rlk.naturområde_id";

            IEnumerable<RedlistDto> redlistDtos = null;
            IEnumerable<RedlistCategoryDto> redlistCategoryDtos = null;
            IEnumerable<RedlistAssessmentUnitDto> redlistAssessmentUnitDtos = null;

            using (var conn = new SqlConnection(connectionString))
            using (var conn2 = new SqlConnection(connectionString))
            using (var conn3 = new SqlConnection(connectionString))
            {
                await Task.WhenAll(conn.OpenAsync(), conn2.OpenAsync(), conn3.OpenAsync());

                var redlistDtosTask = conn.QueryAsync<RedlistDto>(redlistSql);
                var redlistCategoryDtosTask = conn2.QueryAsync<RedlistCategoryDto>(redlistCategorySql);
                var redlistAssessmentUnitDtosTask = conn3.QueryAsync<RedlistAssessmentUnitDto>(redlistAssessmentUnitSql);

                await Task.WhenAll(redlistDtosTask, redlistCategoryDtosTask, redlistAssessmentUnitDtosTask);

                redlistDtos = redlistDtosTask.Result;
                redlistCategoryDtos = redlistCategoryDtosTask.Result;
                redlistAssessmentUnitDtos = redlistAssessmentUnitDtosTask.Result;
            }

            List<RedlistCategory> categories = MapCategories(redlistCategoryDtos);
            List<RedlistTheme> themes = MapThemesAndAssessmentUnits(redlistAssessmentUnitDtos);

            return (categories, themes);
        }

        private static List<RedlistTheme> MapThemesAndAssessmentUnits(IEnumerable<RedlistAssessmentUnitDto> redlistAssessmentUnitDtos)
        {
            var dict = new Dictionary<int, RedlistTheme>();

            foreach (var dto in redlistAssessmentUnitDtos)
            {
                if (!dict.TryGetValue(dto.ThemeId, out var theme))
                {
                    theme = new RedlistTheme
                    {
                        Id = dto.ThemeId,
                        Name = dto.ThemeName
                    };

                    dict.Add(dto.ThemeId, theme);
                }

                var assessmentUnit = theme.AssessmentUnits.FirstOrDefault(au => au.Id == dto.AssessmentUnitId);

                if (assessmentUnit == null)
                {
                    assessmentUnit = new RedlistAssessmentUnit
                    {
                        Id = dto.AssessmentUnitId,
                        Name = dto.AssessmentUnitName
                    };

                    theme.AssessmentUnits.Add(assessmentUnit);
                }

                assessmentUnit.NatureAreaIds.Add(dto.NatureAreaId);
            }

            return dict.Values.ToList();
        }

        private static List<RedlistCategory> MapCategories(IEnumerable<RedlistCategoryDto> redlistCategoryDtos)
        {
            var dict = new Dictionary<int, RedlistCategory>();

            foreach (var dto in redlistCategoryDtos)
            {
                if (!dict.TryGetValue(dto.CategoryId, out var category))
                {
                    category = new RedlistCategory
                    {
                        Id = dto.CategoryId,
                        Name = dto.CategoryName
                    };

                    dict.Add(dto.CategoryId, category);
                }

                category.NatureAreaIds.Add(dto.NatureAreaId);
            }

            return dict.Values.ToList();
        }
    }
}