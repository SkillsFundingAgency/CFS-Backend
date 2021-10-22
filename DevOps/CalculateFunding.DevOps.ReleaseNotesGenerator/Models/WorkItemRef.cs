using System;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Models
{
    public class WorkItemRef
    {
        public int Id { get; set; }
        public string URL { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public int? ParentWorkItemId { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }
    }
}
