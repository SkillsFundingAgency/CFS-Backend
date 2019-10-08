using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryCosmosRepository : ICosmosRepository
    {
        public Task BulkCreateAsync<T>(IList<T> entities, int degreeOfParallelism = 5) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkCreateAsync<T>(IEnumerable<KeyValuePair<string, T>> entities, int degreeOfParallelism = 5) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkDeleteAsync<T>(IEnumerable<T> entities, int degreeOfParallelism = 5, bool hardDelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkDeleteAsync<T>(IEnumerable<KeyValuePair<string, T>> entities, int degreeOfParallelism = 5, bool hardDelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> BulkUpdateAsync<T>(IEnumerable<T> entities, string storedProcedureName) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkUpsertAsync<T>(IList<T> entities, int degreeOfParallelism = 5, bool enableCrossPartitionQuery = false, bool maintainCreatedDate = true, bool undelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkUpsertAsync<T>(IEnumerable<KeyValuePair<string, T>> entities, int degreeOfParallelism = 5, bool enableCrossPartitionQuery = false, bool maintainCreatedDate = true, bool undelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> CreateAsync<T>(T entity, string partitionKey = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> CreateAsync<T>(KeyValuePair<string, T> entity) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<DocumentEntity<T>> CreateDocumentAsync<T>(T entity, string partitionKey = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<ResourceResponse<Document>> CreateWithResponseAsync<T>(T entity) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> DeleteAsync<T>(string id, bool enableCrossPartitionQuery = false, bool hardDelete = false, string partitionKey = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task DocumentsBatchProcessingAsync<T>(Func<List<DocumentEntity<T>>, Task> persistBatchToIndex, int itemsPerPage = 1000, Expression<Func<DocumentEntity<T>, bool>> query = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task DocumentsBatchProcessingAsync<T>(Func<List<T>, Task> persistBatchToIndex, string sql, int itemsPerPage = 1000) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task DocumentsBatchProcessingAsync<T>(Func<List<T>, Task> persistBatchToIndex, SqlQuerySpec sqlQuerySpec, int itemsPerPage = 1000) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public IQueryable<dynamic> DynamicQuery<dynamic>(string sql, bool enableCrossPartitionQuery = false)
        {
            throw new NotImplementedException();
        }

        public IQueryable<dynamic> DynamicQuery<dynamic>(SqlQuerySpec sqlQuerySpec, bool enableCrossPartitionQuery = false)
        {
            throw new NotImplementedException();
        }

        public IQueryable<dynamic> DynamicQueryPartionedEntity<dynamic>(string sql, string partitionEntityId = null)
        {
            throw new NotImplementedException();
        }

        public IQueryable<dynamic> DynamicQueryPartionedEntity<dynamic>(SqlQuerySpec sqlQuerySpec, string partitionEntityId = null)
        {
            return new dynamic[] { }.AsQueryable();
        }

        public Task EnsureCollectionExists()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DocumentEntity<T>>> GetAllDocumentsAsync<T>(int itemsPerPage = 1000, Expression<Func<DocumentEntity<T>, bool>> query = null, bool enableCrossPartitionQuery = true) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DocumentEntity<T>>> GetAllDocumentsAsync<T>(string sql, int itemsPerPage = 1000, bool enableCrossPartitionQuery = true) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DocumentEntity<T>>> GetAllDocumentsAsync<T>(SqlQuerySpec sqlQuerySpec, int itemsPerPage = 1000, bool enableCrossPartitionQuery = true) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<int> GetThroughput()
        {
            throw new NotImplementedException();
        }

        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> Query<T>(bool enableCrossPartitionQuery = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> Query<T>(string directSql, bool enableCrossPartitionQuery = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> Query<T>(SqlQuerySpec sqlQuerySpec, bool enableCrossPartitionQuery = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> QueryAsJson(int itemsPerPage = -1)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> QueryAsJson(string directSql, int itemsPerPage = -1)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> QueryAsJson(SqlQuerySpec sqlQuerySpec, int itemsPerPage = -1)
        {
            throw new NotImplementedException();
        }

        public IQueryable<DocumentEntity<T>> QueryDocuments<T>(int itemsPerPage = -1) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public IQueryable<DocumentEntity<T>> QueryDocuments<T>(string directSql, int itemsPerPage = -1) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public IQueryable<DocumentEntity<T>> QueryDocuments<T>(SqlQuerySpec sqlQuerySpec, int itemsPerPage = -1) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<dynamic>> QueryDynamic(string sql, bool enableCrossPartitionQuery = false, int itemsPerPage = 1000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<dynamic>> QueryDynamic(SqlQuerySpec sqlQuerySpec, bool enableCrossPartitionQuery = false, int itemsPerPage = 1000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> QueryPartitionedEntity<T>(string directSql, int itemsPerPage = -1, string partitionEntityId = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> QueryPartitionedEntity<T>(SqlQuerySpec sqlQuerySpec, int itemsPerPage = -1, string partitionEntityId = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> QuerySql<T>(string directSql, int itemsPerPage = -1, bool enableCrossPartitionQuery = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> QuerySql<T>(SqlQuerySpec sqlQuerySpec, int itemsPerPage = -1, bool enableCrossPartitionQuery = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> RawQuery<T>(string directSql, int itemsPerPage = -1, bool enableCrossPartitionQuery = false)
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> RawQuery<T>(SqlQuerySpec sqlQuerySpec, int itemsPerPage = -1, bool enableCrossPartitionQuery = false)
        {
            throw new NotImplementedException();
        }

        public IQueryable<DocumentEntity<T>> Read<T>(int itemsPerPage = 1000, bool enableCrossPartitionQuery = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<DocumentEntity<T>> ReadAsync<T>(string id, bool enableCrossPartitionQuery = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<T> ReadByIdAsync<T>(string id) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<T> ReadByIdPartitionedAsync<T>(string id, string partitionKey) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<DocumentEntity<T>> ReadDocumentByIdAsync<T>(string id) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task SetThroughput(int requestUnits)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> UpdateAsync<T>(T entity, bool undelete = false) where T : Reference
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> UpsertAsync<T>(T entity, string partitionKey = null, bool enableCrossPartitionQuery = false, bool undelete = false, bool maintainCreatedDate = true) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }
    }
}
