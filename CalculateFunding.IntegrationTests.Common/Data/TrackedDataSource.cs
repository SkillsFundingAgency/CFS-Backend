using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public abstract class TrackedDataSource<TIdentity> : IDisposable
    {
        protected readonly List<TIdentity> ImportedDocuments
            = new List<TIdentity>();

        public void TrackDocumentIdentity(TIdentity identity)
        {
            ImportedDocuments.Add(identity);
        }

        public void Dispose()
        {
            PerformExtraCleanUp();
        }

        protected void TraceInformation(string message) => Trace.TraceInformation(FormatMessage(message));

        protected void TraceError(Exception error,
            string message) => Trace.TraceError(FormatMessage($"{message}\n{error}"));

        protected virtual void PerformExtraCleanUp()
        {
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

        protected abstract void RunRemoveTask(TIdentity documentIdentity);
        
        private string FormatMessage(string message) => $"[{NewUtcNowJsonString()}]{GetType().Name}: {message}";
        
        protected string NewUtcNowJsonString() => DateTime.UtcNow.ToString("O");

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

        protected abstract void RunImportTask(ImportStream importStream);

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