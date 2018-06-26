using CalculateFunding.Services.TestRunner.Vocab.Calculation;

namespace CalculateFunding.Services.TestRunner.Vocab
{
    public class CalculationGherkinVocabulary : StepFactory
    {
        public CalculationGherkinVocabulary() : base(
            new GivenSourceField(), 
            new ThenCalculationValue(),
            new ThenExceptionNotThrown(),
            new ThenSourceField())
        {
        }
    }
}