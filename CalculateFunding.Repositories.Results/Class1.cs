using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CalculateFunding.Repositories.Results
{
    public class BaseEntity
    {
        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
    public class ProviderProductResult
    {
        [Key]
        public string ProviderId { get; set; }
        [Key]
        public string BudgetId { get; set; }
        [Key]
        public string ProductId { get; set; }

        public int CalculationRunId { get; set; }

        public virtual CalculationRun CalculationRun {get; set; }

        public string Value { get; set; }
    }

    public class CalculationRun
    {
        [Key]
        public int Id { get; set; }
        public string DatasetCheckum { get; set; }
        public string AssemblyChecksum { get; set; }
        public DateTimeOffset Date { get; set; }
    }

    public class ResultsDbContext : DbContext
    {
        public DbSet<CalculationRun> CalculationRuns { get; set; }
        public DbSet<ProviderProductResult> ProviderProductCandidates { get; set; }
        public DbSet<ProviderProductResult> ProviderProductResults { get; set; }
    }
}
