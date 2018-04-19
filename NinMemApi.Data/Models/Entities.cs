using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class RedlistCategory
    {
        public RedlistCategory()
        {
            NatureAreaIds = new HashSet<int>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public HashSet<int> NatureAreaIds { get; set; }
    }

    public class RedlistTheme
    {
        public RedlistTheme()
        {
            AssessmentUnits = new List<RedlistAssessmentUnit>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public List<RedlistAssessmentUnit> AssessmentUnits { get; set; }
    }

    public class RedlistAssessmentUnit
    {
        public RedlistAssessmentUnit()
        {
            NatureAreaIds = new HashSet<int>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public HashSet<int> NatureAreaIds { get; set; }
    }

    public class County
    {
        public County()
        {
            Municipalities = new List<Municipality>();
        }

        public int Number { get; set; }
        public string Name { get; set; }
        public List<Municipality> Municipalities { get; set; }
    }

    public class Municipality
    {
        public Municipality()
        {
            NatureAreaIds = new HashSet<int>();
        }

        public int Number { get; set; }
        public string Name { get; set; }
        public HashSet<int> NatureAreaIds { get; set; }
    }

    public class AreaCategory
    {
        public AreaCategory()
        {
            ConservationAreas = new List<ConservationArea>();
        }

        public string ShortName { get; set; }
        public string Name { get; set; }
        public List<ConservationArea> ConservationAreas { get; set; }
    }

    public class ConservationArea
    {
        public ConservationArea()
        {
            NatureAreaIds = new HashSet<int>();
        }

        public int Number { get; set; }
        public string Name { get; set; }
        public HashSet<int> NatureAreaIds { get; set; }
    }
}