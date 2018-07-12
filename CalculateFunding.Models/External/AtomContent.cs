namespace CalculateFunding.Models.External
{
    public class AtomContent
    {
        public AtomContent(Allocation allocation, string type)
        {
            Allocation = allocation;
            Type = type;
        }

        public Allocation Allocation { get; set; }

        public string Type { get; set; }
    }
}