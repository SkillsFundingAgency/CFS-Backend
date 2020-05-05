﻿using System.Threading.Tasks;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Core.Interfaces;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public interface ITemplateVersionRepository : IVersionRepository<TemplateVersion>
    {
        Task<TemplateVersion> GetTemplateVersion(string templateId, int versionNumber);
    }
}