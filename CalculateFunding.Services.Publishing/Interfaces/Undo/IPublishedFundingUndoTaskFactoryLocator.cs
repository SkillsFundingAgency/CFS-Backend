using CalculateFunding.Services.Publishing.Undo;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoTaskFactoryLocator
    {
        public IPublishedFundingUndoTaskFactory GetTaskFactoryFor(PublishedFundingUndoJobParameters parameters);
    }
}