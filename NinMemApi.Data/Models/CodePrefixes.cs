using System;

namespace NinMemApi.Data.Models
{
    public static class CodePrefixes
    {
        public const string AdministrativeArea = "ao";
        public const string ConservationAreaCategories = "vk";
        public const string ConservationAreas = "vv";
        public const string RedlistCategories = "rl";
        public const string RedlistAssessmentUnits = "rv";
        public const string RedlistThemes = "rt";
        public const string BlacklistCategories = "fa";
        public const string NatureArea = "no";
        public const string Taxon = "ar";
        public const string DescriptionVariable = "bs";
        public const string EnvironmentVariable = "mi";
        public const string MatingSystem = "aems";
        public const string PrimaryDiet = "aepd";
        public const string SexualDimorphism = "aesd";
        public const string SocialSystem = "aess";
        public const string Terrestriality = "aet";
        public const string TrophicLevel = "aetl";

        private static string[] _all = new[]
            {
                AdministrativeArea,
                ConservationAreas,
                ConservationAreaCategories,
                RedlistCategories,
                RedlistAssessmentUnits,
                RedlistThemes,
                BlacklistCategories,
                NatureArea,
                Taxon,
                DescriptionVariable,
                EnvironmentVariable,
                MatingSystem,
                PrimaryDiet,
                SexualDimorphism,
                SocialSystem,
                Terrestriality,
                TrophicLevel
            };

        public static string[] All => _all;

        public static string GetAdministrativeAreaCode(int number)
        {
            return Format(AdministrativeArea, number);
        }

        public static string GetConservationAreaCategoryCode(string shortName)
        {
            return Format(ConservationAreaCategories, shortName);
        }

        public static string GetConservationAreaCode(int number)
        {
            return Format(ConservationAreas, number);
        }

        public static string GetRedlistCategoryCode(int id)
        {
            return Format(RedlistCategories, id);
        }

        public static string GetRedlistCategoryCode(string name)
        {
            return Format(RedlistCategories, name);
        }

        public static string GetRedlistAssessmentUnitCode(int id)
        {
            return Format(RedlistAssessmentUnits, id);
        }

        public static string GetRedlistThemeCode(int id)
        {
            return Format(RedlistThemes, id);
        }

        public static string GetBlacklistCategoryCode(string name)
        {
            return Format(BlacklistCategories, name);
        }

        public static string GetNatureAreaCode(int id)
        {
            return Format(NatureArea, id);
        }

        public static string GetTaxonCode(int scientificNameId)
        {
            return Format(Taxon, scientificNameId);
        }

        public static string GetDescriptionVariableCode(string code)
        {
            return Format(DescriptionVariable, code);
        }

        public static string GetMatingsSystemCode(string matingSystem)
        {
            return Format(MatingSystem, matingSystem);
        }

        public static string GetEnvironmentVariableCode(string code)
        {
            return Format(EnvironmentVariable, code);
        }

        public static string GetPrimaryDietCode(string diet)
        {
            return Format(PrimaryDiet, diet);
        }

        public static string GetSexualDimorphismCode(string sexualDimorphism)
        {
            return Format(SexualDimorphism, sexualDimorphism);
        }

        public static string GetSocialSystemCode(string socialSystem)
        {
            return Format(SocialSystem, socialSystem);
        }

        public static string GetTerrestrialityCode(string terrestriality)
        {
            return Format(Terrestriality, terrestriality);
        }

        public static string GetTrophicLevelCode(string trophicLevel)
        {
            return Format(TrophicLevel, trophicLevel);
        }

        public static string GetDescriptionOrEnvironmentVariableCode(string code)
        {
            return Char.IsDigit(code[0]) ? GetDescriptionVariableCode(code) : GetEnvironmentVariableCode(code);
        }

        private static string Format<T>(string prefix, T value)
        {
            return $"{prefix}_{value}".Replace(" ", "_").ToLower();
        }

        public static string GetCodeForAdministrativeUnits(int id)
        {
            return id < 10 ? "ao_0" + id : id < 1000 ? "ao_0" + id.ToString()[0] + "-" + id.ToString().Remove(0, 1) : "ao_" + id.ToString().Remove(2) + "-" + id.ToString().Remove(0, 2);
        }
    }
}