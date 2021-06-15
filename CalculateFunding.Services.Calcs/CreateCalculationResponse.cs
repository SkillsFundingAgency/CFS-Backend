using CalculateFunding.Models.Calcs;
using FluentValidation.Results;
using System.Linq;
using System;

namespace CalculateFunding.Services.Calcs
{
    public class CreateCalculationResponse
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public ValidationResult ValidationResult { get; set; }

        public Calculation Calculation { get; set; }

        public CreateCalculationErrorType ErrorType { get; set; }

        public string ErrorsSummary => GetAllErrorsSummary();

        private string GetAllErrorsSummary()
        {
            if (ValidationResult != null)
            {
                return string.Join(Environment.NewLine, ValidationResult.Errors.Select(_ => _.ToString()));
            }
            else
            {
                return ErrorMessage;
            }
        }
    }
}