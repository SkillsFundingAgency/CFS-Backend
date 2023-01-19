using CalculateFunding.Migrations.Fdz.Copy.Models;
using CommandLine;

namespace CalculateFunding.Migrations.Fdz.Copy
{
    /// <summary>
    /// Console application to copy Fdz provider snapshot from one environment to another.
    /// This process would be handled better as a direct ETL between DBs, but needs must ...
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            ParserResult<CopyOptions>? parsedOptions = Parser.Default.ParseArguments<CopyOptions>(args);
            if (parsedOptions?.Value == null)
            {
                return;
            }

            Console.WriteLine($"Validated copy options. Begining copy of snapshot {parsedOptions.Value.SourceSnapshotId}");
            RunFdzCopy(parsedOptions.Value);
        }

        private static void RunFdzCopy(CopyOptions options)
        {
            var source = new DataSource(
                options.SourceConnectionString,
                options.SourceSnapshotId);

            var target = new DataTarget(
                options
                );

            var providerSnapshot = source.GetSnapshot();

            if (providerSnapshot == null)
            {
                Console.WriteLine($"Failed to retrieve snapshot {options.SourceSnapshotId} from source environment");
                return;
            }

            var providerSnapshotPeriod = source.GetSnapshotPeriod();
            if (providerSnapshotPeriod == null)
            {
                Console.Write($"No ProviderSnapshotPeriod found for snapshot {options.SourceSnapshotId}");
            }
            else
            {
                Console.WriteLine($"ProviderSnapshotPeriod {providerSnapshotPeriod.FundingPeriodName} found for snapshot {options.SourceSnapshotId}");
            }

            var providers = source.GetProviders();
            Console.WriteLine($"Retrieved {providers?.Count ?? 0} providers from source snapshot {options.SourceSnapshotId}");

            var predecessors = source.GetProviderPredecessors();
            Console.WriteLine($"Retrieved {predecessors?.Count ?? 0} predecessors from source snapshot {options.SourceSnapshotId}");

            var successors = source.GetSuccessors();
            Console.WriteLine($"Retrieved {successors?.Count ?? 0} successors from source snapshot {options.SourceSnapshotId}");

            var paymentOrganisations = source.GetPaymentOrganisations();
            Console.WriteLine($"Retrieved {paymentOrganisations?.Count ?? 0} payment organisations from source snapshot {options.SourceSnapshotId}");

            int snapshotId = target.AddSnapshot(providerSnapshot);
            Console.WriteLine($"New empty snapshot created {snapshotId}");

            Console.WriteLine($"Adding payment organisations to new snapshot");
            var paymentOrganisationLookup = target.AddPaymentOrganisations(snapshotId, paymentOrganisations);

            Console.WriteLine($"Adding providers to new snapshot");
            var providerLookup = target.AddProviders(snapshotId, paymentOrganisationLookup, providers);

            if (predecessors.Count > 0)
            {
                Console.WriteLine($"Adding predecessors to new snapshot");
                var predecessorLookup = target.AddProviderRelationships<Predecessor>(providerLookup, predecessors);
            }
            if (successors.Count > 0)
            {
                Console.WriteLine($"Adding successors to new snapshot");
                var successorLookup = target.AddProviderRelationships<Successor>(providerLookup, successors);
            }
            if (providerSnapshotPeriod != null)
            {
                Console.WriteLine($"Adding ProviderSnapshotPeriod");
                target.AddSnapshotPeriod(snapshotId, providerSnapshotPeriod);
            }

            Console.WriteLine($"New snapshot created {snapshotId}");
        }
    }
}