using System;

namespace CalculateFunding.Models.Users
{
    public class UserPermissionCsvGenerationMessage
    {
        public string Environment { get; set; }
        public string FundingStreamId { get; set; }
        public DateTimeOffset ReportRunTime { get; set; }
    }
}
