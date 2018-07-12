namespace CalculateFunding.Models.External
{
    public class AtomEntry
    {
        public AtomEntry(string id, string title, string summary, string published, string version, AtomLink link,
            AtomContent content)
        {
            Id = id;
            Title = title;
            Summary = summary;
            Published = published;
            Version = version;
            Link = link;
            Content = content;
        }

        public string Id { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }

        public string Published { get; set; }

        public string Version { get; set; }

        public AtomLink Link { get; set; }

        public AtomContent Content { get; set; }

    }
}