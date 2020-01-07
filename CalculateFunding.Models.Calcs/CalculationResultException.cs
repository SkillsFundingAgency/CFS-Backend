namespace CalculateFunding.Models.Calcs
{
    public class CalculationResultException
    {
        public string ExceptionType { get; set; }

        public string Message { get; set; }

        public CalculationResultException InnerException { get; set; }
    }
}