namespace CalculateFunding.Repositories.Common.Search
{
    public class FacetFilterType
    {
        public FacetFilterType(string name, bool isMulti = false)
        {
            Name = name;
            IsMulti = isMulti;
        }

        public string Name { get; set; }

        public bool IsMulti { get; set; } = false;
    }
}
