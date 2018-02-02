using CalculateFunding.Models.Datasets.Schema;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDataSetsRepository
    {
        Task<HttpStatusCode> SaveDefinition(DatasetDefinition definition);
    }
}
