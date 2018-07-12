namespace CalculateFunding.Models.External
{
    public class AtomLink
    {
        public AtomLink(string href, string rel)
        {
            Href = href;
            Rel = rel;
        }

        public string Href { get; set; }

        public string Rel { get; set; }
    }
}