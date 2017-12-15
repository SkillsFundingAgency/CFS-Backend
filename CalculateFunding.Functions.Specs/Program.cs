using Microsoft.Azure.WebJobs;

namespace CalculateFunding.Functions.Specs
{
    public class Program
    {
        public static void Main()
        {
            JobHostConfiguration config = new JobHostConfiguration();
            config.UseServiceBus();
            JobHost host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}