using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public interface IPublishingV3ToSqlMigrator
    {
        Task PopulateReferenceData();
    }
}