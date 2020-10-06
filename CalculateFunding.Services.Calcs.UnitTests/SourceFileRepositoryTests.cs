using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    [TestClass, Ignore]
    public class SourceFileRepositoryTests
    {
        private BlobStorageOptions _options;
        private Mock<IBlobContainerRepository> _blobContainer;
        private Mock<ICloudBlob> _cloudBlob;

        private SourceFileRepository _sourceFileRepository;
        
        [TestInitialize]
        public void SetUp()
        {
            _options = new BlobStorageOptions
            {
                ConnectionString = NewRandomString(),
                ContainerName = NewRandomString()
            };
            
            _blobContainer = new Mock<IBlobContainerRepository>();
            _cloudBlob = new Mock<ICloudBlob>();
            
            _sourceFileRepository = new SourceFileRepository(_options,
                _blobContainer.Object);
        }

        [TestMethod]
        public async Task GetAssemblyETag_FetchesEtagPropertyForSpecificationBlob()
        {
            string specificationId = NewRandomString();
            string expectedETag = NewRandomString();
            
            //TODO; the blob client is currently untestable as it couples to a concrete container internally - needs rework before I can put this under test
        }
        
        // private void GivenTheSpecificationBlob(string specificationId)
        //     => _blobContainer.Setup(_ => _.)

        private string NewRandomString() => new RandomString();

    }
}