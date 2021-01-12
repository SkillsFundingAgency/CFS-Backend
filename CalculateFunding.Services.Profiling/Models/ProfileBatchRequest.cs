using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ProfileBatchRequest : ProfileRequestBase
    {
        [Required]
        public IEnumerable<decimal> FundingValues { get; set; }
    }
}