namespace CalculateFunding.Models.External
{
    public class AtomFeed
    {
        public AtomFeed()
        {
        }

        public AtomFeed(string id, string title, AtomAuthor atomAuthor, string updated, string rights, AtomLink link,
            AtomEntry atomEntry)
        {
            Id = id;
            Title = title;
            AtomAuthor = atomAuthor;
            Updated = updated;
            Rights = rights;
            Link = link;
            AtomEntry = atomEntry;
        }

        public string Id { get; set; }

        public string Title { get; set; }

        public AtomAuthor AtomAuthor { get; set; }

        public string Updated { get; set; }

        public string Rights { get; set; }

        public AtomLink Link { get; set; }

        public AtomEntry AtomEntry { get; set; }
    }
}