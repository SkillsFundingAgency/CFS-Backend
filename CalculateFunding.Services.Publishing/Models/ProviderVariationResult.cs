using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ProviderVariationResult
    {
        public ICollection<VariationReason> VariationReasons { get; set; } = new List<VariationReason>();
        
        public bool HasProviderBeenVaried { get; set; }

        public void AddVariationReasons(params VariationReason[] variationReasons)
        {
            VariationReasons.AddRange(variationReasons.Except(VariationReasons));
        }
    }
}