using CalculateFunding.Models.Calcs;
using FluentValidation.Results;

namespace CalculateFunding.Services.Calcs
{
    public class CreateCalculationResponse
    {
        public bool Succeeded { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public ValidationResult ValidationResult { get; set; }
        
        public Calculation Calculation { get; set; }
        
        public CreateCalculationErrorType ErrorType { get; set; }
    }
}