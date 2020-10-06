namespace CalculateFunding.Repositories.Common.Search
{
    public class FacetFilterType
    {
        public FacetFilterType(string name, bool isMulti = false, SearchFieldType fieldType = SearchFieldType.String)
        {
            Name = name;
            IsMulti = isMulti;
            FieldType = fieldType;
        }

        public string Name { get; set; }

        public bool IsMulti { get; set; }

        public SearchFieldType FieldType { get; set; }
    }
}
