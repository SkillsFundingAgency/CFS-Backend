using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.External.AtomItems
{
    [Serializable]
    public class AtomFeed
    {
        public AtomFeed()
        {
        }

        public AtomFeed(string id, string title, AtomAuthor atomAuthor, DateTimeOffset updated, string rights, AtomLink[] link, AtomEntry[] atomEntry)
        {
            Id = id;
            Title = title;
            Author = atomAuthor;
            Updated = updated;
            Rights = rights;
            Link = link;
            AtomEntry = atomEntry;
        }

        public string Id { get; set; }

        public string Title { get; set; }

        public AtomAuthor Author { get; set; }

        public DateTimeOffset Updated { get; set; }

        public string Rights { get; set; }

        public AtomLink[] Link { get; set; }

        public AtomEntry[] AtomEntry { get; set; }
    }
}