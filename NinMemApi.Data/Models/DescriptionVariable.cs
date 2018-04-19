using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class DescriptionVariable
    {
        public DescriptionVariable(string code, string name)
        {
            Code = code;
            Name = name;
            NatureAreas = new List<NatureAreaIdTypePercentage>();
        }

        public string Code { get; set; }
        public string Name { get; set; }
        public List<NatureAreaIdTypePercentage> NatureAreas { get; set; }
    }
}