using System;

namespace NinMemApi.DataPreprocessing.DataLoaders.Dtos
{
    public class NatureAreaTypeDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int NatureAreaId { get; set; }
        public double Percentage { get; set; }
        public DateTime? Mapped { get; set; }
    }
}