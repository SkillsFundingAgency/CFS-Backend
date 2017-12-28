using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CalculateFunding.Repositories.Common.Sql
{
    public abstract class BaseDbContext : DbContext
    {
        protected BaseDbContext(DbContextOptions options) : base (options)
        {
        }

        public async Task BulkInsert<T>(string tableName, IEnumerable<T> entities)
        {
            var connection = Database.GetDbConnection() as SqlConnection;
            if (connection.State != ConnectionState.Open)
            {
                await Database.OpenConnectionAsync();
            }

            using (var bcp = new SqlBulkCopy(connection))
            {
                bcp.BulkCopyTimeout = 60 * 30;
                var columnMappings = entities.GetColumnMappings();
                foreach (var columnMapping in columnMappings)
                {
                    bcp.ColumnMappings.Add(columnMapping);
                }

                bcp.DestinationTableName = tableName;
                var table = entities.ToDataTable();
                await bcp.WriteToServerAsync(table);
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            AddTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AddTimestamps()
        {
            var referenceDate = DateTimeOffset.Now;
            var dateTrackedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);

            foreach (var entity in dateTrackedEntries)
            {
                if (entity.Entity is DbEntity dbEntity)
                {
                    if (entity.State == EntityState.Added)
                    {
                        dbEntity.CreatedAt = referenceDate;
                    }

                    dbEntity.UpdatedAt = referenceDate;
                }

            }
        }

    }
}