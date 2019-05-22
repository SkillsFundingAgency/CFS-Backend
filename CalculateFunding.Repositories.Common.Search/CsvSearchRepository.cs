using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Repositories.Common.Search
{
    public class CsvSearchRepository<T> : ISearchRepository<T> where T : class
    {
        public Task DeleteIndex()
        {
            throw new NotImplementedException();
        }

        public Task<ISearchIndexClient> GetOrCreateIndex()
        {
            throw new NotImplementedException();
        }

        public Task Initialize()
        {
            throw new NotImplementedException();
        }

        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<SearchResults<T>> Search(string searchText, SearchParameters searchParameters = null, bool allResults = false)
        {
            throw new NotImplementedException();
        }

        public Task<T> SearchById(string id, SearchParameters searchParameters = null, string IdFieldOverride = "")
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IndexError>> Index(IEnumerable<T> documents)
        {
            CreateCSVFromGenericList(documents, $"c:\\temp\\{typeof(T).Name}.csv");
            return Task.FromResult(Enumerable.Empty<IndexError>());
        }

        private void CreateCSVFromGenericList(IEnumerable<T> documents, string csvPath)
        {
            if (documents == null || documents.Count() == 0) return;

            Type t = documents.ElementAt(0).GetType();
            string newLine = Environment.NewLine;

            if (!Directory.Exists(Path.GetDirectoryName(csvPath))) Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            if (!File.Exists(csvPath)) File.Create(csvPath);

           
            using (StreamWriter sw = new StreamWriter(csvPath))
            {
                object obj = Activator.CreateInstance(t);
                PropertyInfo[] props = obj.GetType().GetProperties();
                sw.Write(string.Join(",", props.Select(d => d.Name).ToArray()) + newLine);

                foreach (T item in documents)
                {
                    string row = string.Join(",", props.Select(d => item.GetType().GetProperty(d.Name).GetValue(item, null) == null ?
                                    "" : item.GetType().GetProperty(d.Name).GetValue(item, null).ToString()).ToArray());
                    sw.Write(row + newLine);
                }
            }
             
        }


    }
}
