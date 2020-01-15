using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderVariationResultBuilder : TestEntityBuilder
    {
        private IEnumerable<VariationReason> _reasons;
        private bool? _hasVariations;

        public ProviderVariationResultBuilder WithVariationReasons(params VariationReason[] reasons)
        {
            _reasons = reasons;

            return this;
        }

        public ProviderVariationResultBuilder WithHasVariations(bool hasVariations)
        {
            _hasVariations = hasVariations;

            return this;
        }

        public ProviderVariationResult Build()
        {
            return new ProviderVariationResult
            {
                VariationReasons = _reasons?.ToArray() ?? new [] { NewRandomEnum<VariationReason>() },
                HasProviderBeenVaried = _hasVariations.GetValueOrDefault(NewRandomFlag())
            };
        }
    }
}