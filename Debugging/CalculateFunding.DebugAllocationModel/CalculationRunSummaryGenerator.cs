using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Results;

namespace CalculateFunding.DebugAllocationModel
{
    public class CalculationRunSummaryGenerator
    {
        public void GenerateSummary(IEnumerable<CalculationResult> calculationResults, long ms, string specificationId, string providerId)
        {
            Console.WriteLine($"Total ms runtime for calculations: {ms}");
            if (calculationResults == null)
            {
                Console.WriteLine("Calculation results were null.");
                return;
            }

            if (!calculationResults.Any())
            {
                Console.WriteLine("No calculation results were generated");
                return;
            }

            Console.WriteLine($"Specification ID: {specificationId}");
            Console.WriteLine($"Provider ID: {providerId}");
            Console.WriteLine();


            Console.WriteLine($"Total of {calculationResults.Count()} calculations in the specification");
            Console.WriteLine($"\t{calculationResults.Count(r => !string.IsNullOrWhiteSpace(r.ExceptionMessage))} calculations throw exceptions");


            IEnumerable<CalculationResult> slowCalculations = calculationResults.OrderByDescending(c => c.ElapsedTime).Take(10);

            if (slowCalculations.Any() && slowCalculations.First().ElapsedTime > 0)
            {
                Console.WriteLine("Slowest calculations:");
                foreach (var calcs in slowCalculations)
                {
                    Console.WriteLine($"\t{calcs.Calculation.Name}: {calcs.ElapsedTime / 10000} milliseconds");
                }

                Console.WriteLine();
            }

            var exceptionsByType = calculationResults
                .GroupBy(c => new { c.ExceptionType, c.ExceptionMessage })
                .OrderByDescending(c => c.Count());

            foreach (var exceptionGroup in exceptionsByType)
            {
                Console.WriteLine();

                if (string.IsNullOrWhiteSpace(exceptionGroup.Key.ExceptionMessage))
                {
                    continue;
                }

                Console.WriteLine($"{exceptionGroup.Key.ExceptionType} ({exceptionGroup.Key.ExceptionMessage}) - Total calculations {exceptionGroup.Count()}");
                foreach (var calculation in exceptionGroup)
                {
                    Console.WriteLine($"\t{calculation.Calculation.Name}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Calculation results:");

            foreach (var calculation in calculationResults.OrderBy(c => c.Calculation.Name))
            {
                Console.WriteLine($"\t{calculation.Calculation.Name}");

                if (!string.IsNullOrWhiteSpace(calculation.ExceptionType) || !string.IsNullOrWhiteSpace(calculation.ExceptionMessage))
                {
                    Console.WriteLine($"\t\tException: {calculation.ExceptionType} - ({calculation.ExceptionMessage})");
                }
                else
                {
                    if (calculation.Value.HasValue)
                    {
                        Console.WriteLine($"\t\tResult: {calculation.Value}");
                    }
                    else
                    {
                        Console.WriteLine($"\t\tResult: Nothing/Excluded");
                    }
                }

                Console.WriteLine($"\t\tTime Taken: {calculation.ElapsedTime / 10000} milliseconds");

                Console.WriteLine();
            }
        }
    }
}
