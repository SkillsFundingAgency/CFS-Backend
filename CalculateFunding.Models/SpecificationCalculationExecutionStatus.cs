using System;

namespace CalculateFunding.Models
{
	public class SpecificationCalculationExecutionStatus
	{
		public SpecificationCalculationExecutionStatus()
		{
		}

		public SpecificationCalculationExecutionStatus(string specificationId, int percentageCompleted, CalculationProgressStatus calculationProgressStatus)
		{
			SpecificationId = specificationId;
			PercentageCompleted = percentageCompleted;
			CalculationProgress = calculationProgressStatus;
		}

		public string SpecificationId { get; set; }

		public int PercentageCompleted { get; set; }

		public CalculationProgressStatus CalculationProgress { get; set; }

		public string ErrorMessage { get; set; }

        public DateTimeOffset? PublishedResultsRefreshedAt { get; set; }

        protected bool Equals(SpecificationCalculationExecutionStatus other)
		{
			return string.Equals(SpecificationId, other.SpecificationId) && PercentageCompleted == other.PercentageCompleted && CalculationProgress == other.CalculationProgress && string.Equals(ErrorMessage, other.ErrorMessage);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			return obj is SpecificationCalculationExecutionStatus executionStatus && Equals(executionStatus);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (SpecificationId != null ? SpecificationId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ PercentageCompleted;
				hashCode = (hashCode * 397) ^ (int)CalculationProgress;
				hashCode = (hashCode * 397) ^ (ErrorMessage != null ? ErrorMessage.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}