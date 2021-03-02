using System;
using System.Collections.Generic;

namespace CalculateFunding.Generators.Schema12
{
    public static class CollectionExtensions
    {
        public static void AddIfIdentifierHasValue(this ICollection<dynamic> collection,
            string value,
            string type)
        {
            if (value.IsNullOrWhitespace())
            {
                return;
            }

            collection.Add(new
            {
                Type = type,
                Value = value,
            });
        }
    }
}