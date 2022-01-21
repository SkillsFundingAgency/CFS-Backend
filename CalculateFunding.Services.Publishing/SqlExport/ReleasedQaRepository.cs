using CalculateFunding.Common.Sql.Interfaces;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ReleasedQaRepository : BaseSqlRepository, IQaRepository
    {
        public override SqlExportSource SqlExportSource => SqlExportSource.ReleasedPublishedProviderVersion;

        public ReleasedQaRepository(ISqlConnectionFactory connectionFactory, ISqlPolicyFactory sqlPolicyFactory)
            : base(connectionFactory, sqlPolicyFactory)
        {
        }
    }
}
