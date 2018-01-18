using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class CalculationAttribute : Attribute
    {
        public string Id { get; set; }
    }

