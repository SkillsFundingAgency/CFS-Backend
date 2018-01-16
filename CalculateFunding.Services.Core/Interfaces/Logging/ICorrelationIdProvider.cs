namespace CalculateFunding.Services.Core.Interfaces.Logging
{
    public interface ICorrelationIdProvider
    {
        string GetCorrelationId();

        void SetCorrelationId(string correlationId);
    }
}
