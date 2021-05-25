using System.Collections.Generic;
using System.IO;
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
            Current = (DatasetVersion) Current.Clone();
            Current.Author = author;
            Current.RowCount = rowCount;
            Current.ChangeType = changeType;
            (History ??= new List<DatasetVersion>()).Add(Current);
            Current.Version = GetNextVersion();
            Current.BlobName = $"{Id}/v{Current.Version}/{Path.GetFileName(Current.BlobName)}";
        }

        public DatasetDefinitionVersion Definition { get; set; }

        public string Description { get; set; }

        public bool ConverterWizard { get; set; }
    }
}