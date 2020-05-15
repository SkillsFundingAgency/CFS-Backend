using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine
{
    public class DatasetAggregationsRepository : IDatasetAggregationsRepository
    {
        private readonly IDatasetsApiClient _datasetsApiClient;
        private readonly IMapper _mapper;

        public DatasetAggregationsRepository(IDatasetsApiClient datasetsApiClient, IMapper mapper)
        {
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _datasetsApiClient = datasetsApiClient;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DatasetAggregation>> GetDatasetAggregationsForSpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<IEnumerable<CalculateFunding.Common.ApiClient.DataSets.Models.DatasetAggregations>> datasetsApiClientResponse
                = await _datasetsApiClient.GetDatasetAggregationsBySpecificationId(specificationId);

            if (!datasetsApiClientResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"No dataset aggregation for specification '{specificationId}'";
                throw new RetriableException(errorMessage);
            }

            if (datasetsApiClientResponse.Content == null)
            {
                return Enumerable.Empty<DatasetAggregation>();
            }

            return _mapper.Map<IEnumerable<DatasetAggregation>>(datasetsApiClientResponse.Content);
        }
    }
}
