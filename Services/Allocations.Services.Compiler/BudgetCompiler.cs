using System;
using System.Collections.Generic;
using Allocations.Models.Specs;
using Allocations.Services.Compiler.CSharp;

namespace Allocations.Services.Compiler
{
    public static class BudgetCompiler
    {
        private static readonly Dictionary<TargetLanguage, ICompiler> Compilers = new Dictionary<TargetLanguage, ICompiler>();

        static BudgetCompiler()
        {
            Compilers.Add(TargetLanguage.CSharp, new CSharpCompiler());
        }
        public static BudgetCompilerOutput GenerateAssembly(Budget budget)
        {
            if (Compilers.TryGetValue(budget.TargetLanguage, out var compiler))
            {
                return compiler.Compile(budget);
            }
            throw new NotImplementedException($"Language {budget.TargetLanguage} not implemented");
        }

        public static string GetIdentitier(string name, TargetLanguage targetLanguage)
        {
            if (Compilers.TryGetValue(targetLanguage, out var compiler))
            {
                return compiler.GetIdentifier(name);
            }
             throw new NotImplementedException($"GetIndetifier for {targetLanguage} not implemented");
        }
    }
}
