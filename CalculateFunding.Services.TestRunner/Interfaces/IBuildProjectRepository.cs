﻿using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Scenarios;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IBuildProjectRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);
    }
}
