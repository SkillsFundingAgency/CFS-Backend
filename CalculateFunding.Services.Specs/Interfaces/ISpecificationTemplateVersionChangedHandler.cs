using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationTemplateVersionChangedHandler
    {
        Task HandleTemplateVersionChanged(SpecificationVersion previousSpecificationVersion,
            SpecificationVersion specificationVersion,
           IDictionary<string, string> assignedTemplateIds,
            Reference user,
            string correlationId);
    }
}