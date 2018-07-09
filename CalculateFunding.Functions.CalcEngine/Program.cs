using Microsoft.Azure.WebJobs;

namespace CalculateFunding.Functions.CalcEngine
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            JobHostConfiguration config = new JobHostConfiguration();
            config.UseServiceBus();
            JobHost host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
