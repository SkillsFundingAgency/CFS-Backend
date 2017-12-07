using System.ComponentModel.DataAnnotations.Schema;
using CalculateFunding.Repositories.Common.Sql;

namespace CalculateFunding.Repositories.Providers
{
    public class ProviderCommandEntity : DbEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public virtual ProviderEntity Provider { get; set; }
    }
}