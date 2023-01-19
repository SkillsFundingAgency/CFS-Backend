namespace CalculateFunding.Migrations.Fdz.Copy.Models
{
    internal abstract class ProviderRelationship
    {
        public string TableName { get; protected set; }
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public string Ukprn { get; set; }
    }
}
