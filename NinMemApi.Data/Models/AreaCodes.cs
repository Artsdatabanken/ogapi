using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class AreaCodes
    {
        private static readonly Dictionary<string, AreaCode> _codes = new Dictionary<string, AreaCode>();

        static AreaCodes()
        {
            Add("NR", "Naturreservat");
            Add("NP", "Nasjonalpark");
            Add("LVO", "Landskapsvernområde");
            Add("D", "Dyrelivsfredning");
            Add("PD", "Plante- og dyrelivsfredning");
            Add("NM", "Naturminne");
            Add("LVOP", "Landskapsvernområde med plantelivsfredning");
            Add("DO", "Dyrefredningsområde");
            Add("LVOD", "Landskapsvernområde med dyrelivsfredning");
            Add("PO", "Plantefredningsområde");
            Add("LVOPD", "Landskapsvernområde med plante- og dyrelivsfredning");
            Add("PDO", "Plante- og dyrefredningsområde");
            Add("MIV", "Midlertidig verna område/objekt");
            Add("P", "Plantelivsfredning");
            Add("BVV", "Biotopvern etter viltloven");
            Add("NRS", "Naturreservat (Svalbardmiljøloven)");
            Add("NPS", "Nasjonalpark (Svalbardmiljøloven)");
            Add("GVS", "Geotopvern (Svalbardmiljøloven)");
            Add("BV", "Biotopvern");
            Add("MAV", "Marint verneområde (naturmangfoldloven)");
        }

        private static void Add(string code, string name)
        {
            _codes.Add(code, new AreaCode(code, name));
        }

        public static string CodeToName(string code)
        {
            if (!_codes.ContainsKey(code))
            {
                return $"Andre ({code})";
            }

            return _codes[code].Name;
        }
    }
}