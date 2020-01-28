using CalculateFunding.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsSearchService
    {
        Task<IActionResult> SearchSpecificationDatasetRelationships(SearchModel searchModel);

        Task<IActionResult> SearchSpecifications(SearchModel searchModel);
    }
}
