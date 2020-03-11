using System;

namespace CalculateFunding.Services.Publishing.Models
{
    [Flags]
    public enum GeneratePublishingCsvJobsCreationAction
    {
        Undefined,
        Approve,
        Refresh,
        Release
    }
}
