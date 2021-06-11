using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Tests.Common.Helpers;
using FormatWith;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public abstract class JsonDataSource<TIdentity> : TrackedDataSource<TIdentity>
    {
        private readonly string _templateFileName;
        private readonly Assembly _resourceAssembly;

        private string _documentTemplate;

        protected JsonDataSource(string templateResourceName,
            Assembly resourceAssembly)
        {
            Guard.ArgumentNotNull(resourceAssembly, nameof(resourceAssembly));
            Guard.IsNullOrWhiteSpace(templateResourceName, nameof(templateResourceName));

            _resourceAssembly = resourceAssembly;
            _templateFileName = $"{templateResourceName}.json";

            LoadEmbeddedTemplate();
        }

        public async Task CreateContextData(params dynamic[] documents)
        {
            int count = documents.Length;

            List<ImportStream> temporaryDocuments = new List<ImportStream>(count);
            List<TIdentity> batchIdentities = new List<TIdentity>(count);

            string now = NewUtcNowJsonString();

            foreach (dynamic documentData in documents)
            {
                string document = GetFormattedDocument(documentData, now);

                JsonDocument jsonDocument = ParseJsonDocument(document);

                CreateImportStream(jsonDocument, batchIdentities, temporaryDocuments, document);
            }

            await RemoveData(batchIdentities);
            await InsertContextData(temporaryDocuments);
        }

        public string GetFormattedDocument(dynamic documentData, string now)
        {
            object formatWith = GetFormatParametersForDocument(documentData, now);

            return GetDocumentFromTemplate(formatWith);
        }

        private void LoadEmbeddedTemplate()
        {
            _documentTemplate = _resourceAssembly.GetEmbeddedResourceFileContents(_templateFileName);
        }

        protected JsonElement GetElement(JsonElement element,
            string name) =>
            element.TryGetProperty(name, out JsonElement property)
                ? property
                : throw new InvalidOperationException($"Did not locate element {name}");

        protected string GetId(JsonElement document) => GetElement(document, "id")
            .GetString();

        protected bool GetDeletedFlag(JsonElement document) => GetElement(document, "deleted")
            .GetBoolean();

        protected string GetPartitionKey(JsonDocument document) => GetPartitionKey(GetContent(document));

        protected virtual string GetPartitionKey(JsonElement content) => GetElement(content, "partitionKey")
            .GetString();

        protected JsonElement GetContent(JsonDocument document) => GetContent(document.RootElement);

        protected JsonElement GetContent(JsonElement root) => GetElement(root, "content");

        protected static JsonDocument ParseJsonDocument(string document) => JsonDocument.Parse(document);

        protected string GetDocumentFromTemplate(object formatWith) =>
            _documentTemplate.FormatWith(formatWith,
                MissingKeyBehaviour.ReplaceWithFallback,
                "MISSING",
                '<',
                '>');

        protected abstract object GetFormatParametersForDocument(dynamic documentData,
            string now);

        protected abstract void CreateImportStream(JsonDocument jsonDocument,
            List<TIdentity> batchIdentities,
            List<ImportStream> temporaryDocuments,
            string document);
    }
}