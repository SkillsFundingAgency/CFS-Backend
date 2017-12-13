namespace CalculateFunding.Repositories.Providers
{
    public class ProviderCandidateEntity : ProviderBaseEntity
    {
        public virtual ProviderEntity Provider { get; set; }
        public long ProviderCommandId { get; set; }

        public virtual ProviderCommandEntity ProviderCommand {get; set; }
    }
}