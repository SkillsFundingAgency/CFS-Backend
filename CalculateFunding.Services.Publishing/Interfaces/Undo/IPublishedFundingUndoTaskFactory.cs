using CalculateFunding.Services.Publishing.Undo;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoTaskFactory
    {
        bool IsForJob(PublishedFundingUndoJobParameters parameters);

        IPublishedFundingUndoJobTask CreateContextInitialisationTask();
        
        IPublishedFundingUndoJobTask CreatePublishedProviderUndoTask();
        
        IPublishedFundingUndoJobTask CreatePublishedProviderVersionUndoTask();
        
        IPublishedFundingUndoJobTask CreatePublishedFundingUndoTask();
        
        IPublishedFundingUndoJobTask CreatePublishedFundingVersionUndoTask();
    }
}