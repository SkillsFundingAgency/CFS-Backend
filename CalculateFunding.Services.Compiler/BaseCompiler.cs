using System.IO;
using System.Reflection;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;

namespace Allocations.Services.Compiler
{

    public abstract class BaseCompiler : ICompiler
    {
        public BudgetCompilerOutput Compile(Budget budget)
        {
            MetadataReference[] references = {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference()
            };


            using (var ms = new MemoryStream())
            {
                var compilerOutput = Compile(budget, references, ms);
                if (compilerOutput.Success)
                {
                    ms.Seek(0L, SeekOrigin.Begin);

                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);


                    compilerOutput.Assembly = Assembly.Load(data);

                }


                return compilerOutput;
            }
        }


        protected abstract BudgetCompilerOutput Compile(Budget budget, MetadataReference[] references, MemoryStream ms);

        public abstract string GetIdentifier(string name);
    }
}