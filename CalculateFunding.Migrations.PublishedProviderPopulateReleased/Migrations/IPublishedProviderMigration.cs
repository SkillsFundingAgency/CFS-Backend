using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.PublishedProviderPopulateReleased.Migrations
{
    public interface IPublishedProviderMigration
    {
        Task Run();
    }
}
