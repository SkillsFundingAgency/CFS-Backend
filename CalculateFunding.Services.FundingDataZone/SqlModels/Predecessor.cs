using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("Predecessors")]
    public class Predecessor
    {
        [Key]
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public string UKPRN { get; set; }
    }
}
