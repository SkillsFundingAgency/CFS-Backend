﻿using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetVersion : VersionedItem
    {
        //AB: These 2 properties are not required yet, will be updated during the story
        public override string Id => "";

        public override string EntityId => "";

        public string BlobName { get; set; }

        public int RowCount { get; set; }

        public int NewRowCount { get; set; }

        public int AmendedRowCount { get; set; }
        
        public string UploadedBlobFilePath { get; set; }
        
        public DatasetChangeType ChangeType { get; set; }

        public Reference FundingStream { get; set; }

        public string ProviderVersionId { get; set; }

        public override VersionedItem Clone()
        {
            return new DatasetVersion
            {
                RowCount = RowCount,
                PublishStatus = PublishStatus,
                Version = Version,
                Date = Date,
                Author = Author,
                Comment = Comment,
                BlobName = BlobName,
                FundingStream = FundingStream,
                NewRowCount = NewRowCount,
                AmendedRowCount = AmendedRowCount,
                ChangeType = ChangeType,
                ProviderVersionId = ProviderVersionId
            };
        }
    }
}