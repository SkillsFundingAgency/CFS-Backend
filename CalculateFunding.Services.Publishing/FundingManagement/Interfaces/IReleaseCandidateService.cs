﻿using CalculateFunding.Repositories.Common.Search.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseCandidateService
    {
        bool IsReleaseCandidate(int publishedProviderMajorVersion, IEnumerable<ReleaseChannel> releaseChannels);
    }
}