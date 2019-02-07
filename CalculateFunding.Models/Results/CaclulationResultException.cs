namespace CalculateFunding.Models.Results
{
    public class CaclulationResultException
    {
        public string ExceptionType { get; set; }

        public string Message { get; set; }

        public CaclulationResultException InnerException { get; set; }
    }
}