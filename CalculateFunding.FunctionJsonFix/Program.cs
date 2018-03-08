using System;
using System.IO;
using System.Linq;

namespace CalculateFunding.FunctionJsonFix
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args == null || !args.Any())
                return -1;

            foreach (var file in Directory.GetFiles(args.First(), "function.json", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Found {file}");
                var contents = File.ReadAllText(file);
                if (contents.Contains("\"path\""))
                {
                    File.WriteAllText(file, contents.Replace("\"path\"", "\"eventHubName\"").Replace("\"generatedBy\": \"Microsoft.NET.Sdk.Functions.Generator-1.0.6\",", ""));
                    Console.WriteLine($"Updated {file}");
                }
            }

            return 0;
        }
    }
}
