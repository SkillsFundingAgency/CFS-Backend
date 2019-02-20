using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
	public class ProviderVariation
	{
		/// <summary>
		/// The reason for variation
		/// </summary>
		public Collection<string> VariationReasons { get; set; }
 
		/// <summary>
		/// Collection of successor providers
		/// </summary>
		public Collection<ProviderInformationModel> Successors { get; set; }

		/// <summary>
		/// Collection of predecessor providers
		/// </summary>
		public Collection<ProviderInformationModel> Predecessors { get; set; }

		/// <summary>
		/// Open reason
		/// </summary>
		public string OpenReason { get; set; }

		/// <summary>
		/// Close reason
		/// </summary>
		public string CloseReason { get; set; }
	}
}
