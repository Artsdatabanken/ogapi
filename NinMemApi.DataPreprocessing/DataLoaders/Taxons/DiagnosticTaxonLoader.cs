using System.IO;
using System.Linq;

namespace NinMemApi.DataPreprocessing.DataLoaders.Taxons
{
    public static class DiagnosticTaxonLoader
    {
        public static DiagnosticTaxon[] Load(string filePath = "Data\\DiagnosticTaxons\\diagnostiske_arter.csv")
        {
            return File.ReadLines(filePath)
                .Select(line =>
                {
                    string[] cells = line.Split(';');

                    return new DiagnosticTaxon
                    {
                        NatureAreaType = "NA_" + cells[1],
                        ScientificNameId = cells[2]
                    };
                })
                .ToArray();
        }
    }

    public class DiagnosticTaxon
    {
        public string NatureAreaType { get; set; }
        public string ScientificNameId { get; set; }
    }
}