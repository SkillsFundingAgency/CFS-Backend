using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CalculateFunding.Common.Sql.Interfaces;

namespace CalculateFunding.Services.SqlExport
{
    public class DataTableImporter : IDataTableImporter
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public DataTableImporter(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }

        public async Task ImportDataTable<T>(IDataTableBuilder<T> dataTableBuilder)
        {
            if (dataTableBuilder.HasNoData)
            {
                return;
            }

            await using SqlConnection connection = NewOpenConnection();
            using SqlBulkCopy bulkCopy = new(connection)
            {
                DestinationTableName = dataTableBuilder.TableName,
                BatchSize = 1000
            };

            await bulkCopy.WriteToServerAsync(dataTableBuilder.DataTable);
        }

        private SqlConnection NewOpenConnection()
        {
            IDbConnection connection = _sqlConnectionFactory.CreateConnection();

            connection.Open();

            return (SqlConnection) connection;
        }
    }
}