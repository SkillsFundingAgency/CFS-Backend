using CalculateFunding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Repositories.Common.Cosmos.Interfaces
{
    public interface ICosmosRepository
    {
        IQueryable<T> Query<T>(string directSql = null, int maxItemCount = -1, bool enableCrossPartitionQuery = false) where T : IIdentifiable;

        Task<HttpStatusCode> CreateAsync<T>(T entity, string partitionKey = null) where T : IIdentifiable;

        IQueryable<dynamic> DynamicQuery<dynamic>(string sql, bool enableCrossPartitionQuery = false);
    }
}
