using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;
using CalculateFunding.Repositories.Common.Sql;
using FastMember;
using Microsoft.EntityFrameworkCore;

namespace CalculateFunding.Repositories.Providers
{
    public class ProvidersDbContext : BaseDbContext
    {

        public ProvidersDbContext(DbContextOptions<ProvidersDbContext> options) : base(options)
        {
        }

        public async Task<IEnumerable<ProviderEventEntity>> Upsert(long commandId, IEnumerable<ProviderCandidateEntity> candidates)
        {
            var stopwatch = new Stopwatch();
            await BulkInsert("dbo.ProviderCandidates", candidates);

            stopwatch.Stop();
            Console.WriteLine($"Bulk Insert in {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            var merge = new MergeStatementGenerator
            {
                CommandIdColumnName = "ProviderCommandId",
                KeyColumnName = "URN",
                ColumnNames = typeof(ProviderEntity).GetProperties().Select(x => x.Name.ToString()).ToList(),
                SourceTableName = "ProviderCandidates",
                TargetTableName = "Providers"
            };
            var statement = merge.GetMergeStatement();
            var name = new SqlParameter("@CommandId", commandId);
            var events = ProviderEvents.FromSql(statement, name).ToList();

            await BulkInsert("dbo.ProviderEvents", events);

            return events;

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProviderEntity>()
                .HasKey(c => c.UKPRN);


            modelBuilder.Entity<ProviderEntity>()
                .HasIndex(b => b.UKPRN);

            modelBuilder.Entity<ProviderCommandEntity>()
                .HasKey(b => b.Id);

            modelBuilder.Entity<ProviderCandidateEntity>()
                .HasKey(c => new { c.ProviderCommandId, c.UKPRN });


            modelBuilder.Entity<ProviderEventEntity>()
                .HasIndex(b => new { b.ProviderCommandId, b.UKPRN });

            modelBuilder.Entity<ProviderEventEntity>()
                .HasKey(c => new { c.ProviderCommandId, c.UKPRN });


            modelBuilder.Entity<ProviderCandidateEntity>()
                .HasIndex(b => new{ b.ProviderCommandId, b.UKPRN});

        }

        public DbSet<ProviderEntity> Providers { get; set; }
        public DbSet<ProviderEventEntity> ProviderEvents { get; set; }
        public DbSet<ProviderCommandEntity> ProviderCommands { get; set; }
        public DbSet<ProviderCandidateEntity> ProviderCandidates { get; set; }

    }
}
