﻿using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Compiler
{
    public interface ICompiler
    {
        Build GenerateCode(List<SourceFile> sourcefiles);
    }
}
