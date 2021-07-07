using System.IO;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class CsvFilePath
    {
        private readonly string _root;
        private readonly string _filename;

        public CsvFilePath(string root, string specificationId)
        {
            _root = root;
            _filename = new CsvFileName(specificationId);
        }

        public static implicit operator string(CsvFilePath csvFilePath)
        {
            return Path.Combine(csvFilePath._root, csvFilePath._filename);
        }
    }
}
