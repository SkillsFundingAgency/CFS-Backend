using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PolicySpecificationAttribute : Attribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
}

