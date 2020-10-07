namespace CalculateFunding.Services.Profiling.Models
{
    public class AllocationOrganisation
    {
	    public AllocationOrganisation()
	    {
	    }

	    public AllocationOrganisation(string organisationIdentifier, AlternateOrganisationIdentifier alternateOrganisation)
        {
            OrganisationIdentifier = organisationIdentifier;
            AlternateOrganisation = alternateOrganisation;
        }

        public string OrganisationIdentifier { get; set;  }

        public AlternateOrganisationIdentifier AlternateOrganisation { get; set; }
    }
}