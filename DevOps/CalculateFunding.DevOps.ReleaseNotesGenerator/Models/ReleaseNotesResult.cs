using System.Collections.Generic;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Models
{
    public class ReleaseNotesResult
    {
        public ReleaseNotesResult()
        {
            SourceReleases = new List<ReleaseContainer>();
            DestinationReleases = new List<ReleaseContainer>();
        }

        public string SourcePhase { get; set; }
        public string DestinationPhase { get; set; }


        public List<ReleaseContainer> SourceReleases { get; set; }
        public List<ReleaseContainer> DestinationReleases { get; set; }
        public IEnumerable<WorkItemRef> WorkItemsToRelease { get; set; }
    }
}
