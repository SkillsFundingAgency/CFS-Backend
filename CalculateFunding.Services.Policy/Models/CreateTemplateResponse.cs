using System;
using FluentValidation.Results;

namespace CalculateFunding.Services.Policy.Models
{
    public class CreateTemplateResponse
    {
        public bool Succeeded { get; set; }
        
        public string TemplateId { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public Exception Exception { get; set; }
        
        public ValidationResult ValidationResult { get; set; }
    }
}