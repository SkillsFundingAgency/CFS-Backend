using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.External.AtomItems;

namespace CalculateFunding.Models.External
{
    public class SearchFeedV3<T> where T : class
    {
        public string Id
        {
            get
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        public int PageRef { get; set; }

        public int Top { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages
        {
            get
            {
                if (TotalCount <= Top)
                {
                    return 1;
                }

                double pages = ((double)TotalCount / (double)Top);

                return (int)Math.Ceiling(pages);
            }
        }

        public int? Current
        {
            get
            {
                if (PageRef < Last)
                {
                    return PageRef;
                }

                return null;
            }
        }

        public int? Last => TotalPages;


        public int? NextArchive
        {
            get
            {
                if (PageRef + 1 < Last)
                {
                    return PageRef + 1;
                }

                return null;
            }
        }

        public int? PreviousArchive
        {
            get
            {
                if (PageRef == 1)
                {
                    return null;
                }

                return PageRef - 1;
            }
        }

        public bool IsArchivePage => PageRef != Last;

        public IList<AtomLink> GenerateAtomLinksForResultGivenBaseUrl(string notificationsUrl)
        {
            Dictionary<string, int?> atomLinksDictionary = new Dictionary<string, int?>()
            {
                {"prev-archive", PreviousArchive },
                {"next-archive", NextArchive},
                {"current", Current }
            };

            List<AtomLink> atomLinks = atomLinksDictionary
                .Where(a => a.Value != null)
                .Select(a => new AtomLink(string.Format(notificationsUrl, $@"/{a.Value.Value}"), a.Key))
                .ToList();

            atomLinks.Add(new AtomLink(string.Format(notificationsUrl, string.Empty), "self"));

            return atomLinks;
        }

        public IEnumerable<T> Entries { get; set; }
    }
}
