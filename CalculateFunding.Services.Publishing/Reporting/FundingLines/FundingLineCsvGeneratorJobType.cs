namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public enum FundingLineCsvGeneratorJobType
    {
        Undefined = 0,
        CurrentState = 1,
        Released = 2,
        History = 3,
        ProfileValues = 4,
        CurrentProfileValues = 5
    }
}