namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public enum FundingLineCsvGeneratorJobType
    {
        Undefined = 0,
        CurrentState = 1,
        Released = 2,
        History = 3,
        HistoryProfileValues = 4,
        CurrentProfileValues = 5,
        CurrentOrganisationGroupValues = 6,
        HistoryOrganisationGroupValues = 7,
        HistoryPublishedProviderEstate = 8,
        PublishedGroups = 9,
        PublishedProviderStateSummary = 10,
        ChannelLevelPublishedGroup = 11
    }
} 