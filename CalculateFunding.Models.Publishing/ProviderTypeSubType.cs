using System;

namespace CalculateFunding.Models.Publishing
{
    public class ProviderTypeSubType
    {
        public string ProviderType { get; set; }
        public string ProviderSubType { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is ProviderTypeSubType other))
            {
                return false;
            }

            return ProviderType == other.ProviderType && ProviderSubType == other.ProviderSubType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProviderType, ProviderSubType);
        }

        public override string ToString()
        {
            return $"{ProviderType}-{ProviderSubType}";
        }
    }
}