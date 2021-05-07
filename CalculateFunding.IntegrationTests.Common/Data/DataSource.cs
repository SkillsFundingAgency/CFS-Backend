using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Tests.Common.Helpers;
using FormatWith;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public abstract class DataSource<TIdentity> : IDisposable
    {
        private readonly string _templateFileName;
        private readonly Assembly _resourceAssembly;

        private string _documentTemplate;

        protected readonly List<TIdentity> ImportedDocuments
            = new List<TIdentity>();

        protected DataSource(string templateResourceName,
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
                object formatWith = GetFormatParametersForDocument(documentData, now);

                string document = GetDocumentFromTemplate(formatWith);

                JsonDocument jsonDocument = ParseJsonDocument(document);

                CreateImportStream(jsonDocument, batchIdentities, temporaryDocuments, document);
            }

            await RemoveData(batchIdentities);
            await InsertContextData(temporaryDocuments);
        }
        
        public void Dispose()
        {
            PerformExtraCleanUp();
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

        private JsonElement GetContent(JsonDocument document) => GetContent(document.RootElement);

        protected JsonElement GetContent(JsonElement root) => GetElement(root, "content");

        protected void TraceInformation(string message) => Trace.TraceInformation(FormatMessage(message));

        protected void TraceError(Exception error,
            string message) => Trace.TraceError(FormatMessage($"{message}\n{error}"));

        private string FormatMessage(string message) => $"[{NewUtcNowJsonString()}]{GetType().Name}: {message}";

        protected static JsonDocument ParseJsonDocument(string document) => JsonDocument.Parse(document);

        protected string NewUtcNowJsonString() => DateTime.UtcNow.ToString("O");

        protected string GetDocumentFromTemplate(object formatWith) =>
            _documentTemplate.FormatWith(formatWith,
                MissingKeyBehaviour.ReplaceWithFallback,
                "MISSING",
                '<',
                '>');

        protected virtual void PerformExtraCleanUp()
        {
        }

        protected async Task InsertContextData(IEnumerable<ImportStream> documents)
        {
            try
            {
                List<Task> importTasks = new List<Task>(documents.Count());

                TaskFactory taskFactory = Task.Factory;

                foreach (ImportStream importStream in documents)
                {
                    importTasks.Add(taskFactory.StartNew(() => 
                            RunImportTask(importStream),
                        TaskCreationOptions.AttachedToParent));
                }

                await TaskHelper.WhenAllAndThrow(importTasks.ToArray());
            }
            catch (Exception e)
            {
                TraceError(e, "Unable to create context data");

                throw;
            }
        }

        protected async Task RemoveContextData()
        {
            try
            {
                await RemoveData(ImportedDocuments);
            }
            catch (Exception e)
            {
                TraceError(e, "Unable to create context data");

                throw;
            }
        }

        protected async Task RemoveData(List<TIdentity> documentIdentities)
        {
            List<Task> deleteTasks = new List<Task>(documentIdentities.Count);
            TaskFactory taskFactory = Task.Factory;

            foreach (TIdentity document in documentIdentities)
            {
                deleteTasks.Add(taskFactory.StartNew(() =>
                    RunRemoveTask(document),
                    TaskCreationOptions.AttachedToParent));
            }

            await TaskHelper.WhenAllAndThrow(deleteTasks.ToArray());
        }

        protected abstract object GetFormatParametersForDocument(dynamic documentData,
            string now);

        protected abstract void RunImportTask(ImportStream importStream);

        protected abstract void RunRemoveTask(TIdentity documentIdentity);

        protected abstract void CreateImportStream(JsonDocument jsonDocument,
            List<TIdentity> batchIdentities,
            List<ImportStream> temporaryDocuments,
            string document);

        protected static void ThrowExceptionIfRequestFailed(bool requestSucceeded,
            string failedMessage)
        {
            if (!requestSucceeded)
            {
                throw new InvalidOperationException(failedMessage);
            }
        }
    }
}