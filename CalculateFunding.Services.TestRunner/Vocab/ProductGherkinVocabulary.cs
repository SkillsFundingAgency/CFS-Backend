using CalculateFunding.Services.TestRunner.Vocab.Product;

namespace CalculateFunding.Services.TestRunner.Vocab
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
