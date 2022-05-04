using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Models.Publishing
{
    public class InformationFundingLineAggregateValue
    {
        public uint TemplateLineId { get; set; }

        public ProfilePeriod[] ProfilePeriods { get; set; }
    }
}
