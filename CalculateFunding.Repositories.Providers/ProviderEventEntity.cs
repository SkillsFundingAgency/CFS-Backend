using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Repositories.Providers
{
    public class ProviderEventEntity : ProviderBaseEntity
    {
        public virtual ProviderEntity Provider { get; set; }
        public long ProviderCommandId { get; set; }
        public virtual ProviderCommandEntity ProviderCommand { get; set; }
        [Required]
        public string Action { get; set; }
    }
}