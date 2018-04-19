using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class KodeNavn
    {
        public KodeNavn(string kode, IDictionary<string, string> navn)
        {
            Kode = kode;
            Navn = navn;
        }

        public string Kode { get; set; }
        public IDictionary<string, string> Navn { get; set; }
        public KodeNavn Forelder { get; set; }
    }
}