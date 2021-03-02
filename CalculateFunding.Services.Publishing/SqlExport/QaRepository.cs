using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class QaRepository : SqlRepository, IQaRepository
    {

        public QaRepository(ISqlConnectionFactory connectionFactory, ISqlPolicyFactory sqlPolicyFactory)
            : base(connectionFactory, sqlPolicyFactory)
        {
        }

        public int ExecuteSql(string sql)
        {
            return ExecuteNoneQuery(sql);
        }

        public async Task<IEnumerable<TableForStreamAndPeriod>> GetTablesForFundingStreamAndPeriod(string fundingStreamId,
            string fundingPeriodId)
        {
            string fundingStreamAndPeriodIds = $"{fundingStreamId}_{fundingPeriodId}";

            return await QuerySql<TableForStreamAndPeriod>(@"SELECT [Objects].name AS [Name]
FROM 
    sys.tables AS [Tables] INNER JOIN 
    sys.all_objects [Objects] ON [Tables].object_id = [Objects].object_id LEFT JOIN 
    sys.extended_properties [Properties] ON [Properties].major_id = [Tables].object_id
WHERE
    [Properties].name = 'CFS_FundingStreamId_FundingPeriodId' and [Properties].value = @fundingStreamAndPeriodIds",
                new
                {
                    fundingStreamAndPeriodIds
                });
        }
    }
}
