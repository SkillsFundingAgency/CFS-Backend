using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class Build 
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonIgnore]
        public byte[] Assembly { get; set; }

        [JsonProperty("compilerMessages")]
        public List<CompilerMessage> CompilerMessages { get; set; }

        [JsonProperty("sourceFiles")]
        public List<SourceFile> SourceFiles { get; set; }
    }
}