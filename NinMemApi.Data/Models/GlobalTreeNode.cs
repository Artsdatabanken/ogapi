using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class GlobalTreeNode
    {
        public string Kode { get; set; }
        public string Forelder { get; set; }
        public Dictionary<string, string> Tittel { get; set; }
    }
}