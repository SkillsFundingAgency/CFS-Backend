namespace CalculateFunding.Repositories.Providers
{
    public class ProviderCommandCandidateEntity : ProviderBaseEntity
    {
        public virtual ProviderEntity Provider { get; set; }
        public long ProviderCommandId { get; set; }

        public virtual ProviderCommandEntity ProviderCommand {get; set; }
    }
}