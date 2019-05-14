using System;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class ProviderVersionViewModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ProviderVersionType VersionType { get; set; }

        public DateTimeOffset Created { get; set; }
    }
}
