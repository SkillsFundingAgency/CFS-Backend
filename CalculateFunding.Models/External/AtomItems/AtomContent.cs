using System;

namespace CalculateFunding.Models.External.AtomItems
{
    [Serializable]
    public class AtomContent<T> where T: class
    {
        public AtomContent()
        {
        }

        public AtomContent(T allocation, string type)
        {
            Allocation = allocation;
            Type = type;
        }

        public T Allocation { get; set; }

        public string Type { get; set; }
    }
}