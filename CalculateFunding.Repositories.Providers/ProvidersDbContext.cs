using CalculateFunding.Models.Providers;
using Microsoft.EntityFrameworkCore;

namespace CalculateFunding.Repositories.Providers
{
    public class ProvidersDbContext : DbContext
    {
        private DbContextOptions<ProvidersDbContext> options;

        public ProvidersDbContext(DbContextOptions<ProvidersDbContext> options)
        {
            this.options = options;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=.;Database=Providers;Trusted_Connection=True;MultipleActiveResultSets=true",
                b => b.MigrationsAssembly("CalculateFunding.Repositories.Providers.Migrations"));
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Provider> Providers { get; set; }
    }
}
