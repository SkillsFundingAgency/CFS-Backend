using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class CompilerOptions : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get { return SpecificationId; }
        }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        /// <summary>
        /// Use VB.NET Option Strict
        /// </summary>
        [JsonProperty("optionStrictEnabled")]
        public bool OptionStrictEnabled { get; set; } = true;

        /// <summary>
        /// Generate legacy code functions and properties which come from The Store. eg LaToProv and IIf
        /// </summary>
        [JsonProperty("useLegacyCode")]
        public bool UseLegacyCode { get; set; }

        /// <summary>
        /// Generate the build with diagnostics.
        /// This is ignored from configuration as it shouldn't permanently be set.
        /// Enabling this option introduces the System.Diagnostics namespace and would allow users access to APIs which are a potential security risk
        /// </summary>
        [JsonIgnore]
        [JsonProperty("useDiagnosticsMode")]
        public bool UseDiagnosticsMode { get; set; }
    }
}
