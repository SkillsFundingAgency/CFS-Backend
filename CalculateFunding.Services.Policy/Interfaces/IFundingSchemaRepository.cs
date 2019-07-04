using CalculateFunding.Common.Models.HealthCheck;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingSchemaRepository
    {
        Task SaveFundingSchemaVersion(string blobName, byte[] schemaBytes);

        Task<bool> SchemaVersionExists(string blobName);

        Task<string> GetFundingSchemaVersion(string blobName);
    }
}
