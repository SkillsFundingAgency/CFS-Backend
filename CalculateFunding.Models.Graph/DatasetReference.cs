using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class DatasetReference
    {
        public DatasetReference()
        {
            Calculations = new List<Calculation>();
        }

        public string PropertyName { get; set; }
        public List<Calculation> Calculations { get; set; }
        public DataField DataField { get; set; }
        public Dataset Dataset { get; set; }
        public DatasetDefinition DatasetDefinition { get; set; }

        protected bool Equals(DatasetReference other)
        {
            return string.Equals(PropertyName, other.PropertyName, StringComparison.InvariantCultureIgnoreCase)
                && DataField != null && DataField.Equals(other.DataField);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DatasetReference)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PropertyName, DataField);
        }
    }
}
