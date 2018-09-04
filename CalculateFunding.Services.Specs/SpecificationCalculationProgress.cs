namespace CalculateFunding.Services
{
	public class SpecificationCalculationProgress
	{
		public SpecificationCalculationProgress(string specificationId, int percentageCompleted,
			CalculationProgressStatus calculationProgress)
		{
			SpecificationId = specificationId;
			PercentageCompleted = percentageCompleted;
			CalculationProgress = calculationProgress;
		}

		public string SpecificationId { get; set; }
		public int PercentageCompleted { get; set; }
		public CalculationProgressStatus CalculationProgress { get; set; }
		public string ErrorMessage { get; set; }

		public enum CalculationProgressStatus
		{
			NotStarted,
			Started,
			Error,
			Finished
		}
	}
}