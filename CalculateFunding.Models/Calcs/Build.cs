using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class Build 
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("assemblyBase64")]
        public string AssemblyBase64 { get; set; }
        [JsonProperty("compilerMessages")]
        public List<CompilerMessage> CompilerMessages { get; set; }
        [JsonProperty("sourceFiles")]
        public List<SourceFile> SourceFiles { get; set; }
    }

    public class SourceFile
    {
        public string FileName { get; set; }
        public string SourceCode { get; set; }
    }

}