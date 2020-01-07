using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace CalculateFunding.Models.External.V3.AtomItems
{
    [Serializable]
    public class AtomFeed<T> where T : class
    {
        public AtomFeed()
        {
        }

        public AtomFeed(string id, string title, External.AtomItems.AtomAuthor atomAuthor, DateTimeOffset updated, string rights, List<External.AtomItems.AtomLink> link, List<T> atomEntry)
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

        public External.AtomItems.AtomAuthor Author { get; set; }

        public DateTimeOffset Updated { get; set; }

        public string Rights { get; set; }

        public List<External.AtomItems.AtomLink> Link { get; set; }

	    public string Archive { get; set; } = string.Empty;

        public List<T> AtomEntry { get; set; }

		[IgnoreDataMember, XmlIgnore]
	    public bool IsArchived { get; set; }

	    public bool ShouldSerializeArchive()
	    {
		    return IsArchived;
	    }
    }
}