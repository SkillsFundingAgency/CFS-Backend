using CalculateFunding.Common.Sql.Interfaces;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class CurrentQaRepository : BaseSqlRepository, IQaRepository
    {
        public override SqlExportSource SqlExportSource => SqlExportSource.CurrentPublishedProviderVersion;

        public CurrentQaRepository(ISqlConnectionFactory connectionFactory, ISqlPolicyFactory sqlPolicyFactory)
            : base(connectionFactory, sqlPolicyFactory)
        {
        }
    }
}
