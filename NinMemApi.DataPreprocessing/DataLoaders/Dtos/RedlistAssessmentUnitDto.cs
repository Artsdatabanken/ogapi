namespace NinMemApi.DataPreprocessing.DataLoaders.Dtos
{
    public class RedlistAssessmentUnitDto
    {
        public int NatureAreaId { get; set; }
        public int AssessmentUnitId { get; set; }
        public string AssessmentUnitName { get; set; }
        public int ThemeId { get; set; }
        public string ThemeName { get; set; }
    }
}