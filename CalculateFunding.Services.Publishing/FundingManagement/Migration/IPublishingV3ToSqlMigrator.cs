using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public interface IPublishingV3ToSqlMigrator : IJobProcessingService
    {
        Task<IActionResult> QueueReleaseManagementDataMigrationJob(Reference author, string correlationId, string[] fundingStreamIds = null);
    }
}