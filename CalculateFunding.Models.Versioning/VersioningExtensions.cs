﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Versioning
{
    public static class VersioningExtensions
    {

        // Publish status defaults to draft, version 1 for new entities
        public static void Init<T>(this VersionContainer<T> container) where T : VersionedItem, new()
        {
            container.Current = new T
            {
                PublishStatus = PublishStatus.Draft,
                Version = 1
            };
        }

        public static void Publish<T>(this VersionContainer<T> container) where T : VersionedItem
        {
            container.History = container.History ?? new List<T>();

            container.Current.PublishStatus = PublishStatus.Approved;
            container.Published = container.Current.Clone() as T;
            container.History.Add(container.Published);
        }

        public static void PublishVersion<T>(this VersionContainer<T> container, int version) where T : VersionedItem
        {
            container.History = container.History ?? new List<T>();

            var previous = container.History.FirstOrDefault(x => x.Version == version);
            if (previous != null)
            {
                container.Current = previous.Clone() as T;
            }

            container.Publish();

        }

        public static void DiscardChanges<T>(this VersionContainer<T> container) where T : VersionedItem
        {
            if (container.Current.PublishStatus == PublishStatus.Updated)
            {
                container.Current = container.Published?.Clone() as T;
            }

        }

        // Archive will remove published version
        public static void Archive<T>(this VersionContainer<T> container) where T : VersionedItem
        {
            container.History = container.History ?? new List<T>();

            container.Current.PublishStatus = PublishStatus.Archived;
            container.History.Add(container.Current);
            container.Published = null;
        }

        public static T Save<T>(this VersionContainer<T> container, T item) where T : VersionedItem
        {
            container.History = container.History ?? new List<T>();
            int maxVersion = container.History.Count == 0 ? 0 : container.History.Max(x => x.Version); // If publish previous version current version may not be highest

            if (container.Current != null)
            {
                switch (container.Current.PublishStatus)
                {
                    case PublishStatus.Draft:
                        item.PublishStatus = PublishStatus.Draft;
                        break;

                    case PublishStatus.Approved:
                        if (item.PublishStatus == PublishStatus.Draft)
                            break;
                        else
                            item.PublishStatus = PublishStatus.Updated;
                        break;

                    default:
                        item.PublishStatus = PublishStatus.Updated;
                        break;
                }
            }
            else
            {
                item.PublishStatus = PublishStatus.Draft;
            }

            item.Version = maxVersion + 1;
            container.Current = item;
            container.History.Add(container.Current);
            item.Date = DateTimeOffset.Now.ToLocalTime();

            return container.Current;
        }

    }
}