using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public static class RedlistCodeNames
    {
        public static IDictionary<string, string> _codeNames = new Dictionary<string, string>
        {
            { "RE", "Regionalt utdødd" },
            { "CR", "Kritisk truet" },
            { "EN", "Sterkt truet" },
            { "VU", "Sårbar" },
            { "NT", "Nær truet" },
            { "DD", "Datamangel" },
            { "LC", "Livskraftig" }
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