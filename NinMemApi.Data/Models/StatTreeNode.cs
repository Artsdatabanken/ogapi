using System.Collections.Generic;

namespace NinMemApi.Data.Models
{
    public class StatTreeNode
    {
        private readonly HashSet<string> _countedTaxons;
        private readonly HashSet<string> _countedNatureAreas;
        private readonly HashSet<(string, string)> _addedAreas;

        public StatTreeNode(string code, string name)
        {
            Code = code;
            Name = name;
            Children = new Dictionary<string, StatTreeNode>();
            _countedTaxons = new HashSet<string>();
            _countedNatureAreas = new HashSet<string>();
            _addedAreas = new HashSet<(string, string)>();
        }

        public string Code { get; set; }
        public string Name { get; set; }
        public int TaxonCount { get; private set; }
        public int NatureAreaCount { get; private set; }
        public double Area { get; private set; }
        public IDictionary<string, StatTreeNode> Children { get; set; }
        public StatTreeNode Parent { get; set; }

        public void IncrementTaxonCount(string code)
        {
            if (_countedTaxons.Contains(code))
            {
                return;
            }
            TaxonCount++;

            _countedTaxons.Add(code);

            if (Parent != null)
            {
                Parent.IncrementTaxonCount(code);
            }
        }

        public void IncrementNatureAreaCount(string code)
        {
            if (_countedNatureAreas.Contains(code))
            {
                return;
            }

            NatureAreaCount++;

            _countedNatureAreas.Add(code);

            if (Parent != null)
            {
                Parent.IncrementNatureAreaCount(code);
            }
        }

        public void AddToArea(string natureAreaCode, string natureAreaTypeCode, double area)
        {
            var key = (natureAreaCode, natureAreaTypeCode);

            if (_addedAreas.Contains(key))
            {
                return;
            }

            Area += area;

            _addedAreas.Add(key);

            if (Parent != null)
            {
                Parent.AddToArea(natureAreaCode, natureAreaTypeCode, area);
            }
        }
    }
}