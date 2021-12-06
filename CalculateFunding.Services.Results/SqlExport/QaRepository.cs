using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class QaRepository : SqlRepository, IQaRepository
    {
        public QaRepository(ISqlConnectionFactory connectionFactory, ISqlPolicyFactory sqlPolicyFactory)
            : base(connectionFactory, sqlPolicyFactory)
        {
        }

        public int ExecuteSql(string sql)
            => ExecuteNoneQuery(sql);

        public async Task<IEnumerable<TableForSpecification>> GetTablesForSpecification(string specificationId)
        {
            string specificationIds = specificationId;

            return await QuerySql<TableForSpecification>(@"SELECT [Objects].name AS [Name]
FROM 
    sys.tables AS [Tables] INNER JOIN 
    sys.all_objects [Objects] ON [Tables].object_id = [Objects].object_id LEFT JOIN 
    sys.extended_properties [Properties] ON [Properties].major_id = [Tables].object_id
WHERE
    [Properties].name = 'CFS_SpecificationId' and [Properties].value = @specificationIds",
                new
                {
                    specificationIds
                });
        }
    }
}
