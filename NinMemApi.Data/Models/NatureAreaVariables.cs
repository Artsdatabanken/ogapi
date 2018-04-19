using System;
using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class NatureAreaVariables
    {
        public NatureAreaVariables()
        {
            DescriptionVariables = new HashSet<string>();
        }

        public string NatureAreaTypeCode { get; set; }
        public int NatureAreaId { get; set; }
        public double Percentage { get; set; }
        public DateTime? Mapped { get; set; }
        public HashSet<string> DescriptionVariables { get; set; }
    }
}