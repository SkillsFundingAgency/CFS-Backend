using CalculateFunding.Services.TestRunner.Services;
namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IStepParserFactory
    {
        IStepParser GetStepParser(StepType stepType);
    }
}
