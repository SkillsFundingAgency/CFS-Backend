using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class CalculationSpecificationAttribute : Attribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

