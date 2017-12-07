using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;
using CalculateFunding.Repositories.Common.Sql;
using FastMember;
using Microsoft.EntityFrameworkCore;

namespace CalculateFunding.Repositories.Providers
{
    public class ProvidersDbContext : DbContext
    {
        //public ProvidersDbContext(DbContextOptions options) : base(options)
        //{
        //}

        public ProvidersDbContext(DbContextOptions<ProvidersDbContext> options) : base(options)
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProviderEntity>()
                .HasKey(c => c.URN);


            modelBuilder.Entity<ProviderEntity>()
                .HasIndex(b => b.URN);

            modelBuilder.Entity<ProviderCommandEntity>()
                .HasKey(b => b.Id);

            modelBuilder.Entity<ProviderCommandCandidateEntity>()
                .HasKey(c => new { c.ProviderCommandId, c.URN });


            modelBuilder.Entity<ProviderEventEntity>()
                .HasIndex(b => new { b.ProviderCommandId, b.URN });

            modelBuilder.Entity<ProviderEventEntity>()
                .HasKey(c => new { c.ProviderCommandId, c.URN });


            modelBuilder.Entity<ProviderCommandCandidateEntity>()
                .HasIndex(b => new{ b.ProviderCommandId, b.URN});

        }

        public DbSet<ProviderEntity> Providers { get; set; }
        public DbSet<ProviderEventEntity> ProviderEvents { get; set; }
        public DbSet<ProviderCommandEntity> ProviderCommands { get; set; }
        public DbSet<ProviderCommandCandidateEntity> ProviderCommandCandidates { get; set; }

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
