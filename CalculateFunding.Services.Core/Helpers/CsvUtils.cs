using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Azure.Search;

namespace CalculateFunding.Services.Core.Helpers
{
    public class CsvUtils
    {
        private bool WriteHeaderBehaviour<T>(IEnumerable<T> documents, CsvHeaderBehaviour headerBehaviour)
        {
            switch (headerBehaviour)
            {
                case CsvHeaderBehaviour.WriteAlways:
                    return true;

                case CsvHeaderBehaviour.WriteIfData:
                    return (documents?.Count() ?? 0) > 0;

                case CsvHeaderBehaviour.WriteNever:
                    return false;

                default:
                    throw new ArgumentException($"Unhandled behaviour {headerBehaviour.ToString()}");
            }
        }

        private string BuildHeader(IEnumerable<string> fields, string qualifier)
        {
            string[] headerFields = fields.Select(d => $"{qualifier}{d}{qualifier}").ToArray();
            return string.Join(",", headerFields);
        }

        public string CreateCsv<T>(IEnumerable<T> documents,
            string qualifier = "\"",
            CsvHeaderBehaviour headerBehaviour = CsvHeaderBehaviour.WriteAlways)
        {
            Type t = typeof(T);

            StringBuilder result = new StringBuilder();

            object obj = Activator.CreateInstance(t);
            PropertyInfo[] props = obj.GetType().GetProperties();

            if (WriteHeaderBehaviour(documents, headerBehaviour))
            {
                result.AppendLine(BuildHeader(props.Select(x => x.Name), qualifier));
            }

            foreach (T item in documents)
            {
                string row = string.Join(",", props
                    .Select(d => item.GetType().GetProperty(d.Name).GetValue(item, null) == null
                        ? ""
                        : $"{qualifier}{item.GetType().GetProperty(d.Name).GetValue(item, null).ToString()}{qualifier}"
                    ).ToArray());

                result.AppendLine(row);
            }

            return result.ToString();
        }

        public string CreateCsvExpando(IEnumerable<ExpandoObject> documents,
            string qualifier = "\"",
            CsvHeaderBehaviour headerBehaviour = CsvHeaderBehaviour.WriteAlways)
        {
            if (!documents.Any()) return "";

            var keys = documents
                .FirstOrDefault()
                .Select(x => x.Key);

            StringBuilder result = new StringBuilder();

            if (WriteHeaderBehaviour(documents, headerBehaviour))
            {
                result.AppendLine(BuildHeader(keys, qualifier));
            }

            foreach (ExpandoObject item in documents)
            {
                result.AppendLine(string.Join(",", item.Select(x => $"{qualifier}{x.Value.ToString()}{qualifier}")));
            }

            return result.ToString();
        }
    }
}