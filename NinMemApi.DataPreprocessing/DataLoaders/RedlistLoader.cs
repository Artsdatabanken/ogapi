using Dapper;
using NinMemApi.Data.Models;
using NinMemApi.DataPreprocessing.DataLoaders.Dtos;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Npgsql;
using Newtonsoft.Json;

namespace NinMemApi.DataPreprocessing.DataLoaders
{
    public static class RedlistLoader
    {
        public static async Task<(List<RedlistCategory> categories, List<RedlistTheme> themes)> Load(
            string connectionString, string redList)
        {
            const string redlistCategorySql =
 @"SELECT c_g.geometry_id AS NatureAreaId, c.id AS CategoryId, c.code AS CategoryName
FROM data.codes_geometry c_g, data.codes c
WHERE c.code like 'RL%'
AND c.id = c_g.codes_id";

            IEnumerable<RedlistCategoryDto> redlistCategoryDtos = null;

            using (var conn2 = new NpgsqlConnection(connectionString))
            {
                await Task.WhenAll(conn2.OpenAsync());

                var redlistCategoryDtosTask = conn2.QueryAsync<RedlistCategoryDto>(redlistCategorySql);

                await Task.WhenAll(redlistCategoryDtosTask); 

                redlistCategoryDtos = redlistCategoryDtosTask.Result;
            }

            List<RedlistCategory> categories = MapCategories(redlistCategoryDtos);
            List<RedlistTheme> themes = MapThemesAndAssessmentUnits(redList);

            return (categories, themes);
        }

        private static List<RedlistTheme> MapThemesAndAssessmentUnits(string redList)
        {
            var themes = new List<RedlistTheme>();

            dynamic json = JsonConvert.DeserializeObject<List<ExpandoObject>>(
                new WebClient().DownloadString(redList));

            var id = 0;
            foreach (var tema in json)
            {
                var redlistTheme = new RedlistTheme
                {
                    AssessmentUnits = new List<RedlistAssessmentUnit>(),
                    Id = id++,
                    Name = tema.Navn
                };

                foreach (var vurderingsenhet in tema.VurderingsEnheter)
                {
                    var naturområder = new HashSet<int>();

                    foreach (var regel in vurderingsenhet.Regler)
                    foreach (var naturområde in regel.Naturområder)
                        naturområder.Add(int.Parse(naturområde));

                    redlistTheme.AssessmentUnits.Add(new RedlistAssessmentUnit
                    {
                        Id = id++,
                        Name = vurderingsenhet.Navn,
                        NatureAreaIds = naturområder
                    });
                }

                themes.Add(redlistTheme);
            }

            return themes;
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
                        Name = dto.CategoryName.Remove(0, 3)
                    };

                    dict.Add(dto.CategoryId, category);
                }

                category.NatureAreaIds.Add(dto.NatureAreaId);
            }

            return dict.Values.ToList();
        }
    }
}