namespace CalculateFunding.Api.Profiling.Logging
{
	public interface ICorrelationIdProvider
	{
		string GetCorrelationId();

		void SetCorrelationId(string correlationId);
	}
}
