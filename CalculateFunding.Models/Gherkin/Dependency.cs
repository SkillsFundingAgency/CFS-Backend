namespace CalculateFunding.Models.Gherkin
{
    public class Dependency
    {
        public Dependency(string datasetName, string fieldName, string value)
        {
            DatasetName = datasetName;
            FieldName = fieldName;
            Value = value;
        }

        public string DatasetName { get; }
        public string FieldName { get;}
        public string Value { get; }
    }
}