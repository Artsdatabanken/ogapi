using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class NatureAreaType
    {
        public NatureAreaType(string code, string name)
        {
            Code = code;
            Name = name;
            NatureAreas = new List<NatureAreaPercentage>();
        }

        public string Code { get; set; }
        public string Name { get; set; }
        public List<NatureAreaPercentage> NatureAreas { get; set; }
    }
}