using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderIndexerService
    {
        Task IndexPublishedProvider(PublishedProviderVersion publishedProviderVersion);
    }
}
