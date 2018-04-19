using Newtonsoft.Json;

namespace NinMemApi.Data.Models
{
    public class TaxonTraits
    {
        public int ScientificNameId { get; set; }
        public int? TotalLifeSpan { get; set; }
        public string[] Habitat { get; set; }
        public string Terrestriality { get; set; }
        public string[] TrophicLevel { get; set; }
        public int[] FeedsOn { get; set; }
        public int[] PreysUpon { get; set; }
        public string[] SexualDimorphism { get; set; }
        public string MatingSystem { get; set; }
        public string[] SocialSystem { get; set; }
        public string[] PrimaryDiet { get; set; }

        [JsonIgnore]
        public bool IsEmpty
        {
            get
            {
                return
                    TotalLifeSpan == null
                    && Habitat == null
                    && Terrestriality == null
                    && TrophicLevel == null
                    && FeedsOn == null
                    && PreysUpon == null
                    && SexualDimorphism == null
                    && MatingSystem == null
                    && SocialSystem == null
                    && PrimaryDiet == null;
            }
        }
    }
}