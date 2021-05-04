using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Undo;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoJobTask
    {
        Task Run(PublishedFundingUndoTaskContext taskContext);

        // this flag is used to determine whether the documents in scope of the undo task are versions
        bool VersionDocuments => false;
    }
}