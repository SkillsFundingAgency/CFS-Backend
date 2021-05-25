using OfficeOpenXml;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public readonly struct ExcelDocument
    {
        public ExcelDocument(string path,
            ExcelPackage document)
        {
            Path = path;
            Document = document;
        }

        public string Path { get;  }
        
        public ExcelPackage Document { get; }
    }
}