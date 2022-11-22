using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting.ChannelLevelPublishedGroup
{
    public interface IChannelLevelPublishedGroupsCsvBatchProcessorServiceLocator
    {
        IFundingLineCsvBatchProcessor GetService(FundingLineCsvGeneratorJobType jobType);
    }
}
