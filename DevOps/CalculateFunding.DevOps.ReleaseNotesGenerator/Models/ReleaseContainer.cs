using System.Collections.Generic;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Models
{
    public class ReleaseContainer
    {
        public ReleaseDefinitionRef ReleaseDefinition { get; set; }
        public ReleaseRef Release { get; set; }
        public IEnumerable<WorkItemRef> LinkedWorkItems { get; set; }
    }
}
