using System;

namespace CalculateFunding.Models.Scenarios
{
    public class TestStepArgumentAttribute : Attribute
    {
        public StepArgumentType Type { get; }

        public TestStepArgumentAttribute(StepArgumentType type)
        {
            Type = type;
        }
    }
}