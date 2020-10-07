namespace CalculateFunding.Profiling.GWTs.Dtos
{
    public class AllocationOrganisation
    {
        public string OrganisationIdentifier { get; set; }

        public AlternateOrganisation AlternateOrganisation { get; set; }

        protected bool Equals(AllocationOrganisation other)
        {
            return Equals(AlternateOrganisation, other.AlternateOrganisation) && string.Equals(OrganisationIdentifier, other.OrganisationIdentifier);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((AllocationOrganisation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((AlternateOrganisation != null ? AlternateOrganisation.GetHashCode() : 0) * 397) ^ (OrganisationIdentifier != null ? OrganisationIdentifier.GetHashCode() : 0);
            }
        }
    }
}