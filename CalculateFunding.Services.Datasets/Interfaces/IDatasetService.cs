using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetService
    {
        Task<IActionResult> CreateNewDataset(HttpRequest request);

        Task<IActionResult> GetDatasetByName(HttpRequest request);

        Task SaveNewDataset(ICloudBlob blob);
    }
}
