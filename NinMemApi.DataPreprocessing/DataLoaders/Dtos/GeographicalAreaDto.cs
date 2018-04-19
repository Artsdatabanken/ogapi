namespace NinMemApi.DataPreprocessing.DataLoaders.Dtos
{
    public class GeographicalAreaDto
    {
        public int? NatureAreaId { get; set; }
        public int GeometryTypeId { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
    }
}