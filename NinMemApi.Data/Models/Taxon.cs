using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class Taxon
    {
        public Taxon()
        {
            NatureAreaTypeCodes = new HashSet<string>();
        }

        public int TaxonId { get; set; }

        public int ParentScientificNameId;

        public int ScientificNameId { get; set; }
        public string ScientificName { get; set; }
        public string PopularName { get; set; }

        public HashSet<string> NatureAreaTypeCodes { get; set; }
        public string BlacklistCategory { get; set; }
        public string[] RedlistCategories { get; set; }
        public int[][] EastNorths { get; set; }
        public int[] Municipalities { get; set; }
        public int[] ConservationAreas { get; set; }
    }
}