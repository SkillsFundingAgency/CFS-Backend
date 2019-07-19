using System.Threading.Tasks;
using FluentValidation.Results;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ISpecificationIdServiceRequestValidator
    {
        ValidationResult Validate(string specificationId);
    }
}