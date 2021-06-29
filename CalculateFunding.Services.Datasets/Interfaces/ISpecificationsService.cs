using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface ISpecificationsService
    {
        Task<IActionResult> GetEligibleSpecificationsToReference(string specificationId);
        Task<IActionResult> PublishedSpecificationTemplateMetadata(string specificationId);
    }
}