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
    }
}
