namespace CalculateFunding.Profiling.GWTs.Dtos
{
    public class AlternateOrganisation
    {
        public string IdentifierName { get; set; }

        public string Identifier { get; set; }

        protected bool Equals(AlternateOrganisation other)
        {
            return string.Equals(IdentifierName, other.IdentifierName) && string.Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlternateOrganisation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((IdentifierName != null ? IdentifierName.GetHashCode() : 0) * 397) ^ (Identifier != null ? Identifier.GetHashCode() : 0);
            }
        }
    }
}