using Allocations.Services.TestRunner.Vocab.Product;

namespace Allocations.Services.TestRunner.Vocab
{
    public class ProductGherkinVocabulary : GherkinVocabDefinition
    {
        public ProductGherkinVocabulary() : base(
            new GivenSourceField(), 
            new ThenProductValue(),
            new ThenExceptionNotThrown(),
            new ThenSourceField())
        {
        }
    }
}
