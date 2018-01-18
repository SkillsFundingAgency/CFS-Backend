using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class AllocationLineAttribute : Attribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
}

