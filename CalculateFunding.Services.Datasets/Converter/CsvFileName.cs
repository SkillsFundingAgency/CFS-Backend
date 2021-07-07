namespace CalculateFunding.Services.Datasets.Converter
{
    public class CsvFileName
    {
        private readonly string _specificationId;

        public CsvFileName(string specificationId)
        {
            _specificationId = specificationId;
        }

        public static implicit operator string(CsvFileName csvFileName)
        {
            return $"converter-wizard-activity-{csvFileName._specificationId}.csv";
        }
    }
}
