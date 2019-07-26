﻿using System;
using System.Xml.Serialization;

namespace CalculateFunding.Models.External.V3.AtomItems
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class AtomEntry
    {
        public AtomEntry()
        {
        }

        public AtomEntry(string id, string title, string summary, DateTimeOffset published, string version, External.AtomItems.AtomLink link,
            object content, DateTimeOffset updated)
        {
            Id = id;
            Title = title;
            Summary = summary;
            Published = published;
            Version = version;
            Link = link;
            Content = content;
            Updated = updated;
        }

        public string Id { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }

        public DateTimeOffset? Published { get; set; }

        public DateTimeOffset Updated { get; set; }

        public string Version { get; set; }

        public External.AtomItems.AtomLink Link { get; set; }

        public object Content { get; set; }

    }
}