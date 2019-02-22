using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V2.Models.Examples
{
	public static class ProviderVariationExample
	{
		private static readonly ProviderVariation ProviderVariation = new ProviderVariation()
		{
			OpenReason = "Fresh Start",
			Predecessors = new Collection<ProviderInformationModel>()
			{
				new ProviderInformationModel()
				{
					Ukprn = "12345678"
				}
			},
			VariationReasons = new Collection<string>()
			{
				"LegalNameFieldUpdated",
				"LACodeFieldUpdated"
			}
		};

		public static ProviderVariation GetProviderVariationExample()
		{
			return ProviderVariation;
		}
	}
}
