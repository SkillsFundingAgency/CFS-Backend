using System;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Compiler.CSharp;
using CalculateFunding.Services.Compiler.VisualBasic;

namespace CalculateFunding.Services.Compiler
{
    public interface ILangugeSyntaxProvider
    {
        string GetIdentitier(string name, TargetLanguage targetLanguage);
    }
    public class BudgetCompiler : ILangugeSyntaxProvider
    {
        private readonly Dictionary<TargetLanguage, ICompiler> Compilers = new Dictionary<TargetLanguage, ICompiler>();

        public BudgetCompiler(CSharpCompiler cSharpCompiler, VisualBasicCompiler visualBasicCompiler)
        {
            Compilers.Add(TargetLanguage.CSharp, cSharpCompiler);
            Compilers.Add(TargetLanguage.VisualBasic, visualBasicCompiler);
        }
        public BudgetCompilerOutput GenerateAssembly(Budget budget)
        {
            if (Compilers.TryGetValue(budget.TargetLanguage, out var compiler))
            {
                return compiler.GenerateCode(budget);
            }
            throw new NotImplementedException($"Language {budget.TargetLanguage} not implemented");
        }

        public string GetIdentitier(string name, TargetLanguage targetLanguage)
        {
            if (Compilers.TryGetValue(targetLanguage, out var compiler))
            {
                return compiler.GetIdentifier(name);
            }
             throw new NotImplementedException($"GetIndentifier for {targetLanguage} not implemented");
        }
    }
}
