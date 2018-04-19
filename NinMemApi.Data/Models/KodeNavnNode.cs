namespace NinMemApi.Data.Models
{
    public class KodeNavnNode
    {
        public string Kode { get; set; }
        public string Navn { get; set; }

        public KodeNavnForelder Forelder { get; set; }

        public KodeNavnBarn[] Barn { get; set; }
    }
}