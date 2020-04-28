﻿using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Models;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface ITemplateBuilderService
    {
        Task<ServiceHealth> IsHealthOk();

        Task<CreateTemplateResponse> CreateTemplate(
            TemplateCreateCommand command,
            Reference author);
    }
}