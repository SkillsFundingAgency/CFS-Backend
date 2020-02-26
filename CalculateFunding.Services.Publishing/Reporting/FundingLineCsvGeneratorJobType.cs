using System;

namespace CalculateFunding.Services.Publishing.Reporting
{
    [Flags]
    public enum FundingLineCsvGeneratorJobType
    {
        Undefined = 0,
        CurrentState = 1    
    }
}