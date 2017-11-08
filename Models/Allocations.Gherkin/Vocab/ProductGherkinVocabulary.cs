using System;
using System.Collections.Generic;
using System.Text;
using Allocations.Gherkin.Vocab.Product;

namespace Allocations.Gherkin.Vocab
{
    public class ProductGherkinVocabulary : GherkinVocabDefinition
    {
        protected ProductGherkinVocabulary() : base(new GivenSourceField(), new ThenProductValue())
        {
        }
    }
}
