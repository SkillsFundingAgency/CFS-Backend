using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Datasets
{
    public class Dataset : VersionContainer<DatasetVersion>
    {
        public void CreateNewVersion(Reference author,
            int rowCount,
            DatasetChangeType changeType)
        {
            Current = (DatasetVersion)Current.Clone();
            Current.Version = GetNextVersion();
            Current.Author = author;
            Current.RowCount = rowCount;
            Current.ChangeType = changeType;
            History.Add(Current);
        }
        
        public DatasetDefinitionVersion Definition { get; set; }

        public string Description { get; set; }

        public bool ConverterWizard { get; set; }
    }
}
