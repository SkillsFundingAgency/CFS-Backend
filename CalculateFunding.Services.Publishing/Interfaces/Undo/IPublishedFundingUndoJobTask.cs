using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Undo;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoJobTask
    {
        Task Run(PublishedFundingUndoTaskContext taskContext);
    }
}