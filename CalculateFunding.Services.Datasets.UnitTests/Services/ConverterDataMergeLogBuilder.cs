using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ConverterDataMergeLogBuilder : TestEntityBuilder
    {
        private int _datasetVersionCreated;
        private string _jobId;
        private string _parentJobId;
        private ConverterMergeRequest _request;
        private IEnumerable<RowCopyResult> _results;

        public ConverterDataMergeLogBuilder WithDatasetVersionCreated(int datasetVersionCreated)
        {
            _datasetVersionCreated = datasetVersionCreated;

            return this;
        }

        public ConverterDataMergeLogBuilder WithParentJobId(string parentJobId)
        {
            _parentJobId = parentJobId;

            return this;
        }

        public ConverterDataMergeLogBuilder WithJobId(string jobId)
        {
            _jobId = jobId;

            return this;
        }

        public ConverterDataMergeLogBuilder WithRequest(ConverterMergeRequest request)
        {
            _request = request;

            return this;
        }

        public ConverterDataMergeLogBuilder WithResults(IEnumerable<RowCopyResult> results)
        {
            _results = results;

            return this;
        }

        public ConverterDataMergeLog Build() =>
            new ConverterDataMergeLog
            {
                DatasetVersionCreated = _datasetVersionCreated,
                JobId = _jobId ?? NewRandomString(),
                ParentJobId = _parentJobId ?? NewRandomString(),
                Request = _request,
                Results = _results
            };
    }
}