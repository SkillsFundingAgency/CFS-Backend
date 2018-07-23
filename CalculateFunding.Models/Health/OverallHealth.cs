using System.Collections.Generic;

namespace CalculateFunding.Models.Health
{
    public class OverallHealth
    {
        public bool OverallHealthOk { get; set; }

        public ICollection<ServiceHealth> Services { get; } = new List<ServiceHealth>();
    }
}
