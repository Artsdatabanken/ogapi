using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class GeographicalAreaData
    {
        public List<County> Counties { get; set; }
        public List<AreaCategory> ConservationAreaCategories { get; set; }
    }
}