using NinMemApi.Data.Models;
using System.Collections.Generic;

namespace NinMemApi.Data
{
    public class GraphInput
    {
        public List<NatureAreaDto> NatureAreas { get; set; }
        public List<RedlistCategory> NatureAreaRedlistCategories { get; set; }
        public List<RedlistTheme> NatureAreaRedlistThemes { get; set; }
        public GeographicalAreaData NatureAreaGeographicalAreaData { get; set; }
        public List<Taxon> Taxons { get; set; }
        public CodeTreeNode CodeTree { get; set; }
        public List<NatureAreaVariables> NatureAreaVariables { get; set; }
        public List<TaxonTraits> TaxonTraits { get; set; }
    }
}