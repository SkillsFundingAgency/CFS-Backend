﻿using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class ReferenceExtensions
    {
        public static UserProfile ToUserProfile(this Reference reference)
        {
            return new UserProfile(reference.Id, reference.Name);
        }
    }
}
