using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    public class BatchUploadRequestBuilder : TestEntityBuilder
    {
        private byte[] _stream;

        public BatchUploadRequestBuilder WithStream(byte[] stream)
        {
            _stream = stream;

            return this;
        }

        public BatchUploadRequest Build()
        {
            return new BatchUploadRequest
            {
                Stream = _stream
            };
        }
    }
}