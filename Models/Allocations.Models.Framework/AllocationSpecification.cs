namespace Allocations.Models.Framework
{
    public abstract class AllocationSpecification<TProvider, TProviderStatement> where TProvider : class where TProviderStatement : class
    {
        public abstract void UpdateStatement(TProvider provider, ref TProviderStatement statement);
    }
}