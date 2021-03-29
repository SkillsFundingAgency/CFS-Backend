using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public abstract class CsvBatchProcessBase
    {
        public const int BatchSize = 100;
        
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly ICsvUtils _csvUtils;

        protected CsvBatchProcessBase(IFileSystemAccess fileSystemAccess, ICsvUtils csvUtils)
        {
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            
            _fileSystemAccess = fileSystemAccess;
            _csvUtils = csvUtils;
        }

        protected bool AppendCsvFragment(string temporaryFilePath, IEnumerable<ExpandoObject> csvRows, bool outputHeaders)
        {
            string csv = _csvUtils.AsCsv(csvRows, outputHeaders);

            if (string.IsNullOrWhiteSpace(csv))
            {
                return false;
            }

            _fileSystemAccess.Append(temporaryFilePath, csv)
                .GetAwaiter()
                .GetResult();

            return true;
        }   
    }
}