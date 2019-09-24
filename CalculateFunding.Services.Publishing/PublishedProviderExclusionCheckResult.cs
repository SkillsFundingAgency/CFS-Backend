namespace CalculateFunding.Services.Publishing
{
    public struct PublishedProviderExclusionCheckResult
    {
        public PublishedProviderExclusionCheckResult(string providerId, 
            bool shouldBeExcluded)
        {
            ProviderId = providerId;
            ShouldBeExcluded = shouldBeExcluded;
        }

        public string ProviderId { get; }
        
        public bool ShouldBeExcluded { get; }
    }
}