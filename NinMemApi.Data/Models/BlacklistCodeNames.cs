using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public static class BlacklistCodeNames
    {
        public static IDictionary<string, string> _codeNames = new Dictionary<string, string>
        {
            { "SE", "Svært høy risiko" },
            { "HI", "Høy risiko" },
            { "PH", "Potensielt høy risiko" },
            { "LO", "Lav risiko" },
            { "NK", "Ingen kjent risiko" }
        };

        public static string GetName(string code)
        {
            return _codeNames.ContainsKey(code) ? _codeNames[code] : null;
        }

        public static IDictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(_codeNames);
        }
    }
}