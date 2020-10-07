namespace CalculateFunding.Services.Profiling.Models
{
	public class AlternateOrganisationIdentifier
	{
		public AlternateOrganisationIdentifier()
		{
		}

		public AlternateOrganisationIdentifier(string identifierName, string identifier)
		{
			IdentifierName = identifierName;
			Identifier = identifier;
		}

		public string IdentifierName { get; set; }
		public string Identifier { get; set; }
	}
}