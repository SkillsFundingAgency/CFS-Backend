using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IPublishedFundingRetrievalService
    {
        /// <summary>
        /// Gets a single published funding feed document.
        /// </summary>
        /// <param name="absoluteDocumentPathUrl">Full url (normally in blob storage to the URL)</param>
        /// <param name="isForPreLoad"></param>
        /// <returns></returns>
        Task<string> GetFundingFeedDocument(string absoluteDocumentPathUrl, bool isForPreLoad = false);
    }
}
