using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionService : IHealthChecker
    {
        Task<IActionResult> DoesProviderVersionExist(string providerVersionId);
        Task<ProviderVersionByDate> GetProviderVersionByDate(int year, int month, int day);
        Task<MasterProviderVersion> GetMasterProviderVersion();
        Task<IActionResult> GetProviderVersionsByFundingStream(string fundingStream);
        Task<ProviderVersion> GetProvidersByVersion(string providerVersionId);
        Task<IActionResult> GetAllProviders(string providerVersionId);
        Task<IActionResult> GetAllProviders(int year, int month, int day);
        Task<IActionResult> SetProviderVersionByDate(int year, int month, int day, string providerVersionId);
        Task<IActionResult> UploadProviderVersion(string actionName, string controller, string providerVersionId, ProviderVersionViewModel providers);
        Task<(bool Success, IActionResult ActionResult)> UploadProviderVersion(string providerVersionId, ProviderVersionViewModel providers);
        Task<bool> Exists(string providerVersionId);
        Task<bool> Exists(ProviderVersionViewModel providerVersionViewModel);
        Task<IActionResult> GetProviderVersionMetadata(string providerVersionId);
    }
}
