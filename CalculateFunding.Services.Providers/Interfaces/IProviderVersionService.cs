using CalculateFunding.Models.Providers.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionService
    {
        Task<IActionResult> GetAllProviders(string providerVersionId);
        Task<IActionResult> GetAllProviders(int year, int month, int day);
        Task<IActionResult> UploadProviderVersion(string actionName, string controller, string providerVersionId, ProviderVersionViewModel providers);
    }
}
