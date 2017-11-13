using System;
using Allocations.Models.Specs;
using Allocations.Services.Compiler.CSharp;

namespace Allocations.Services.Compiler
{
    public class BudgetCompiler
    {
        public static BudgetCompilerOutput GenerateAssembly(Budget budget)
        {
            switch (budget.TargetLanguage)
            {
                case TargetLanguage.CSharp:
                    return CSharpBudgetCompiler.GenerateAssembly(budget);

                default:
                    throw new NotImplementedException($"Language {budget.TargetLanguage} not implemented");
            }

        }

        public static string GetIdentitier(string name, TargetLanguage targetLanguage)
        {
            switch (targetLanguage)
            {
                case TargetLanguage.CSharp:
                    return CSharpTypeGenerator.Identifier(name);

                default:
                    throw new NotImplementedException($"GetIndetifier for {targetLanguage} not implemented");
            }
        }
    }
}
