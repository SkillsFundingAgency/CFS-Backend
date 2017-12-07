using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CalculateFunding.Repositories.Providers.Migrations
{
    public class ProvidersDbContextFactory : IDesignTimeDbContextFactory<ProvidersDbContext>
    {
        public ProvidersDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ProvidersDbContext>();
            optionsBuilder.UseSqlServer("Server=.;Database=Providers;Trusted_Connection=True;MultipleActiveResultSets=true", b => b.MigrationsAssembly("CalculateFunding.Repositories.Providers.Migrations"));
            return new ProvidersDbContext(optionsBuilder.Options);
        }
    }
}
