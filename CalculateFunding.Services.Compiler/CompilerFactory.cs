using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Compiler.CSharp;
using CalculateFunding.Services.Compiler.VisualBasic;

namespace CalculateFunding.Services.Compiler
{
    public class CompilerFactory 
    {
        private readonly Dictionary<string, ICompiler> _compilers = new Dictionary<string, ICompiler>();

        public CompilerFactory(CSharpCompiler cSharpCompiler, VisualBasicCompiler visualBasicCompiler)
        {
            _compilers.Add(".csproj", cSharpCompiler);
            _compilers.Add(".vbproj", visualBasicCompiler);
        }

        public ICompiler GetCompiler(IEnumerable<SourceFile> sourceFiles)
        {
            foreach (var extension in sourceFiles.Select(x => Path.GetExtension(x.FileName).ToLowerInvariant()))
            {
                if (_compilers.TryGetValue(extension, out var compiler))
                {
                    return compiler;
                }
            }
            throw new NotImplementedException($"No supported project file included (must be one of {string.Join(",", _compilers.Keys)})");
        }
    }
}
