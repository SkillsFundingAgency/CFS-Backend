namespace CalculateFunding.IntegrationTests.Common.Data
{
    public readonly struct BlobIdentity
    {
        public BlobIdentity(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}