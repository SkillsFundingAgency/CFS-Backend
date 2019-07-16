using System.Threading.Tasks;
using FluentValidation.Results;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishSpecificationValidator
    {
        ValidationResult Validate(string specificationId);
    }
}