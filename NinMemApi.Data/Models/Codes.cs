using System.Collections.Generic;
using System.Linq;

namespace NinMemApi.Data.Models
{
    public class Codes
    {
        private readonly IDictionary<string, CodeItem> _codes;

        public Codes()
        {
            _codes = new Dictionary<string, CodeItem>();
        }

        public void AddCode(string code, string parentCode, string name)
        {
            _codes.Add(code, new CodeItem
            {
                Code = code,
                ParentCode = parentCode,
                Name = name
            });
        }

        public CodeItem GetCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            return _codes.ContainsKey(code) ? _codes[code] : null;
        }

        public bool Contains(string code)
        {
            return _codes.ContainsKey(code);
        }

        public IDictionary<string, CodeItem> GetDictionary()
        {
            return new Dictionary<string, CodeItem>(_codes);
        }

        public List<CodeItem> GetValues()
        {
            return _codes.Values.ToList();
        }

        public static Codes Create(IEnumerable<KodeInstans> kodeinstanser)
        {
            var codes = new Codes();

            foreach (var instans in kodeinstanser)
            {
                codes.AddCode(instans.Kode.Id, instans.OverordnetKode.Id, instans.Navn);
            }

            return codes;
        }
    }

    public class CodeItem
    {
        public string Code { get; set; }
        public string ParentCode { get; set; }
        public string Name { get; set; }
    }

    public class KodeInstans
    {
        public string Navn { get; set; }
        public Kode Kode { get; set; }
        public Kode OverordnetKode { get; set; }
        public string Beskrivelse { get; set; }
    }

    public class Kode
    {
        public string Id { get; set; }
    }
}